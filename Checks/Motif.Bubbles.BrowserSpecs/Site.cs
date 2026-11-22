using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

namespace Motif.Bubbles.BrowserSpecs;

[CollectionDefinition(Name)]
public sealed class SiteCollection : ICollectionFixture<Site>
{
    public const string Name = "Site";
}

/// <summary>
/// The app as it's deployed: published in Release (trimmed), served as
/// static files, and opened in a real browser. Each page gets a fresh
/// browser profile. Nothing leaves the machine: GitHub has only this repo,
/// made from its own code, with no Stryker report on its Pages site, and
/// every other request is blocked.
/// </summary>
public sealed class Site : IAsyncLifetime
{
    public const string ThisRepo = "motif-dotnet/Motif.Bubbles";

    private const string Api = "https://api.github.com/repos/";
    private const string Raw = "https://raw.githubusercontent.com/";

    private readonly string repo = RepoRoot();
    private WebApplication? server;
    private IPlaywright? playwright;
    private IBrowser? browser;

    public string Url { get; private set; } = "";

    /// <summary>
    /// This repo's own code, as a folder named for the repo, to pick.
    /// </summary>
    public static string Folder { get; } =
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Motif.Bubbles");

    public async Task InitializeAsync()
    {
        // MOTIF_SITE is a folder the app is already published to, as CI
        // publishes it once for the specs and the deploy. Otherwise the
        // specs publish it themselves.
        var site = Environment.GetEnvironmentVariable("MOTIF_SITE");
        if (string.IsNullOrEmpty(site))
        {
            site = Path.Combine(AppContext.BaseDirectory, "site");
            await Publish(site);
        }
        server = Serve(Path.Combine(site, "wwwroot"));
        await server.StartAsync();
        Url = server.Services.GetRequiredService<IServer>()
            .Features.GetRequiredFeature<IServerAddressesFeature>()
            .Addresses.Single() + "/";

        playwright = await Playwright.CreateAsync();
        // The Edge every Windows machine has, so there's nothing to download.
        // MOTIF_BROWSER picks another channel, such as "chrome".
        browser = await playwright.Chromium.LaunchAsync(new()
        {
            Channel = Environment.GetEnvironmentVariable("MOTIF_BROWSER")
                ?? "msedge",
        });
    }

    /// <summary>
    /// Where a failed step's Playwright trace is kept, for CI to upload.
    /// </summary>
    public static string Traces { get; } =
        Path.Combine(AppContext.BaseDirectory, "traces");

    /// <summary>
    /// A browser profile of its own, at this window width, traced so a step
    /// that fails can keep what the browser did.
    /// </summary>
    public async Task<IBrowserContext> Profile(int width = 1280)
    {
        var profile = await browser!.NewContextAsync(new()
        {
            ViewportSize = new() { Width = width, Height = 812 },
        });
        await profile.Tracing.StartAsync(new()
        {
            Screenshots = true,
            Snapshots = true,
        });
        await profile.RouteAsync("**/*", Route);
        return profile;
    }

    /// <summary>Keeps the profile's trace so far, under this name.</summary>
    public static Task KeepTrace(IBrowserContext profile, string name) =>
        profile.Tracing.StopAsync(new()
        {
            Path = Path.Combine(Traces, $"{name}.zip"),
        });

    /// <summary>Opens the app at this fragment and waits for it to start.</summary>
    public async Task<IPage> Open(
        IBrowserContext profile, string fragment = "")
    {
        var page = await profile.NewPageAsync();
        await page.GotoAsync(Url + fragment);
        await page.Locator("header.appbar").WaitForAsync();
        return page;
    }

    private async Task Route(IRoute route)
    {
        var url = route.Request.Url;
        if (url.StartsWith(Url, StringComparison.Ordinal))
            await route.ContinueAsync();
        else if (url == $"{Api}{ThisRepo}/git/trees/HEAD?recursive=1")
            await Answer(route, 200, Tree());
        else if (url.StartsWith($"{Raw}{ThisRepo}/HEAD/", StringComparison.Ordinal))
            await FileOnGitHub(route, url[$"{Raw}{ThisRepo}/HEAD/".Length..]);
        else if (url.Contains(".github.io/", StringComparison.Ordinal))
            await Answer(route, 404, "");
        else
            await route.AbortAsync();
    }

    /// <summary>Every file in this repo, as GitHub lists a tree.</summary>
    private static string Tree() => JsonSerializer.Serialize(new
    {
        tree = Directory
            .EnumerateFiles(Folder, "*", SearchOption.AllDirectories)
            .Select(f => new
            {
                path = Path.GetRelativePath(Folder, f).Replace('\\', '/'),
                type = "blob",
            }),
        truncated = false,
    });

    private static async Task FileOnGitHub(IRoute route, string path)
    {
        var file = Path.Combine(Folder, Uri.UnescapeDataString(path));
        if (File.Exists(file))
            await Answer(route, 200, await File.ReadAllTextAsync(file));
        else
            await Answer(route, 404, "");
    }

    private static Task Answer(IRoute route, int status, string body) =>
        route.FulfillAsync(new()
        {
            Status = status,
            Body = body,
            Headers = new Dictionary<string, string>
            {
                ["Access-Control-Allow-Origin"] = "*",
            },
        });

    private async Task Publish(string output)
    {
        var app = Path.Combine(repo, "Motif.Bubbles.App");
        using var publish = Process.Start(new ProcessStartInfo(
            "dotnet", ["publish", app, "-c", "Release", "-o", output])
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        })!;
        var log = publish.StandardOutput.ReadToEndAsync();
        var errors = publish.StandardError.ReadToEndAsync();
        await publish.WaitForExitAsync();
        if (publish.ExitCode != 0)
            throw new InvalidOperationException(
                "Publishing the app failed:\n" + await log + await errors);
    }

    private static WebApplication Serve(string root)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        var app = builder.Build();
        var files = new PhysicalFileProvider(root);
        app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = files,
            ContentTypeProvider = new FileExtensionContentTypeProvider(),
            ServeUnknownFileTypes = true,
        });
        return app;
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (!File.Exists(Path.Combine(dir.FullName, "Motif.Bubbles.sln")))
            dir = dir.Parent ?? throw new InvalidOperationException(
                "The specs must run inside the Motif.Bubbles repo.");
        return dir.FullName;
    }

    public async Task DisposeAsync()
    {
        if (browser is not null) await browser.DisposeAsync();
        playwright?.Dispose();
        if (server is not null) await server.DisposeAsync();
    }
}
