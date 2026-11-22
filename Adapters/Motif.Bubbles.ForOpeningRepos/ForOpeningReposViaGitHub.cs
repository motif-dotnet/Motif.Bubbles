using System.Net;
using System.Text.Json;

namespace Motif.Bubbles.Adapters;

/// <summary>
/// Reads a public repo at HEAD (its default branch): the list of its files
/// from api.github.com, then each file a scan needs from
/// raw.githubusercontent.com, which has no rate limit. The Stryker report
/// comes from the repo's Pages site, where its CI publishes
/// motif/mutation-report.json. All three allow cross-origin reads, so this
/// works from Pages, with no sign-in. Its Seam is the platform's
/// <see cref="HttpMessageHandler"/>: the browser's in the app, a fake GitHub
/// in specs.
/// </summary>
public sealed class ForOpeningReposViaGitHub(HttpMessageHandler github)
    : ForOpeningRepos
{
    private const string Api = "https://api.github.com";
    private const string Raw = "https://raw.githubusercontent.com";

    // Enough requests at once to be quick, few enough not to swamp the page.
    private const int AtOnce = 8;

    private readonly HttpClient http = new(github);

    public async Task<RepoCode> Read(
        RepoName repo, CancellationToken cancellationToken = default)
    {
        var listed = await List(repo, cancellationToken);
        if (listed.Outcome != RepoRead.Read) return listed;
        var report = Report(repo, cancellationToken);
        using var gate = new SemaphoreSlim(AtOnce);
        var files = await Task.WhenAll(listed.Files.Select(async file =>
        {
            await gate.WaitAsync(cancellationToken);
            try
            {
                return file with
                {
                    Text = await Fetch(Url(Raw, repo, "HEAD/" + Escaped(file.Path)),
                        cancellationToken),
                };
            }
            finally
            {
                gate.Release();
            }
        }));
        return listed with { Files = files, Report = await report };
    }

    /// <summary>
    /// The repo's files a scan wants, with no text yet.
    /// </summary>
    private async Task<RepoCode> List(
        RepoName repo, CancellationToken cancellationToken)
    {
        using var response = await http.GetAsync(
            Url(Api, repo, "git/trees/HEAD?recursive=1", "repos"),
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return RepoCode.NotFound;
        if (IsRateLimited(response)) return RepoCode.RateLimited;
        // An empty repo has no tree yet.
        if (response.StatusCode == HttpStatusCode.Conflict)
            return new RepoCode(RepoRead.Read, [], FoundReport.None);
        response.EnsureSuccessStatusCode();
        using var tree = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(cancellationToken));
        var paths = tree.RootElement.GetProperty("tree").EnumerateArray()
            .Where(e => e.GetProperty("type").GetString() == "blob")
            .Select(e => e.GetProperty("path").GetString() ?? "")
            .Where(CodeFile.IsWanted);
        return new RepoCode(
            RepoRead.Read, [.. paths.Select(p => new CodeFile(p, ""))],
            FoundReport.None);
    }

    /// <summary>
    /// The report on the repo's Pages site, or none if the site has no
    /// report or can't be reached: the code is drawn either way.
    /// </summary>
    private async Task<FoundReport> Report(
        RepoName repo, CancellationToken cancellationToken)
    {
        try
        {
            using var response =
                await http.GetAsync(PagesReport(repo), cancellationToken);
            return response.IsSuccessStatusCode
                ? new FoundReport(
                    await response.Content.ReadAsStringAsync(cancellationToken),
                    "")
                : FoundReport.None;
        }
        catch (HttpRequestException)
        {
            return FoundReport.None;
        }
    }

    /// <summary>
    /// A file's text, or "" if it has gone since the repo was listed.
    /// </summary>
    private async Task<string> Fetch(Uri url, CancellationToken token)
    {
        using var response = await http.GetAsync(url, token);
        if (response.StatusCode == HttpStatusCode.NotFound) return "";
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(token);
    }

    private static bool IsRateLimited(HttpResponseMessage response) =>
        response.StatusCode == HttpStatusCode.TooManyRequests
        || (response.StatusCode == HttpStatusCode.Forbidden
            && response.Headers.TryGetValues(
                "X-RateLimit-Remaining", out var left)
            && left.Contains("0"));

    /// <summary>
    /// A user's or organisation's own site (owner.github.io) is served at
    /// the root; any other repo's under its name.
    /// </summary>
    private static Uri PagesReport(RepoName repo)
    {
        var site = $"https://{repo.Owner.ToLowerInvariant()}.github.io";
        var own = repo.Name.Equals(
            $"{repo.Owner}.github.io", StringComparison.OrdinalIgnoreCase);
        var path = own ? "" : $"/{Uri.EscapeDataString(repo.Name)}";
        return new Uri($"{site}{path}/motif/{MutationReport.FileName}");
    }

    private static string Escaped(string path) =>
        string.Join('/', path.Split('/').Select(Uri.EscapeDataString));

    private static Uri Url(
        string host, RepoName repo, string rest, string under = "") =>
        new($"{host}/{(under == "" ? "" : under + "/")}"
            + $"{Uri.EscapeDataString(repo.Owner)}/"
            + $"{Uri.EscapeDataString(repo.Name)}/{rest}");
}
