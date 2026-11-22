using System.Net;
using System.Text.Json;

namespace Motif.Bubbles.Fakes;

/// <summary>
/// GitHub with no network, behind the platform's
/// <see cref="HttpMessageHandler"/> Seam. It answers GETs as GitHub does:
/// <list type="bullet">
/// <item>https://api.github.com/repos/owner/name/git/trees/HEAD?recursive=1
/// is 200 with every file and folder committed on the default branch, as
/// JSON; 404 when the repo isn't there or isn't public; 409 for an empty
/// repo; and 403, with an X-RateLimit-Remaining of 0, once the hourly limit
/// is reached.</item>
/// <item>https://raw.githubusercontent.com/owner/name/HEAD/path is 200 with
/// the file's text, and 404 when it isn't there.</item>
/// <item>https://owner.github.io/name/path is 200 with a file published to
/// the repo's Pages site, and 404 when it isn't there. The owner's own repo,
/// owner.github.io, is served at the site's root.</item>
/// </list>
/// Owners and repo names are matched in any case, paths exactly.
/// </summary>
public sealed class FakeGitHub : HttpMessageHandler
{
    private readonly Dictionary<string, Dictionary<string, string>> repos = [];
    private readonly Dictionary<string, string> pages = [];

    /// <summary>
    /// Every request fails as a browser's fetch does with no network.
    /// </summary>
    public bool Unreachable { get; set; }

    /// <summary>
    /// The hourly limit on listing repos has been reached.
    /// </summary>
    public bool RateLimited { get; set; }

    /// <summary>
    /// The URLs asked for, in order.
    /// </summary>
    public List<string> Requests { get; } = [];

    private TaskCompletionSource held = Released();

    /// <summary>
    /// Puts files on a public repo's default branch.
    /// </summary>
    public FakeGitHub Commit(string repo, IEnumerable<CodeFile> files)
    {
        var committed = Repo(repo);
        foreach (var file in files) committed[file.Path] = file.Text;
        return this;
    }

    /// <summary>
    /// Makes a public repo with nothing committed yet.
    /// </summary>
    public FakeGitHub CreateEmpty(string repo)
    {
        Repo(repo);
        return this;
    }

    /// <summary>
    /// Publishes a file to the repo's Pages site, as its CI would.
    /// </summary>
    public FakeGitHub Publish(string repo, string path, string text)
    {
        pages[Key(repo) + "|" + path] = text;
        return this;
    }

    /// <summary>
    /// Holds every answer back until <see cref="Release"/>, as a slow network
    /// would.
    /// </summary>
    public FakeGitHub Hold()
    {
        held = new(TaskCreationOptions.RunContinuationsAsynchronously);
        return this;
    }

    public void Release() => held.TrySetResult();

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri!;
        lock (Requests) Requests.Add(url.AbsoluteUri);
        await held.Task;
        if (Unreachable) throw new HttpRequestException("Failed to fetch");
        if (request.Method != HttpMethod.Get || url.Scheme != "https")
            return await Answer(HttpStatusCode.NotFound);
        var parts = url.AbsolutePath.TrimStart('/').Split('/')
            .Select(Uri.UnescapeDataString)
            .ToArray();
        return await (url.Host switch
        {
            "api.github.com" => Tree(parts, url.Query),
            "raw.githubusercontent.com" => File(parts),
            var host when host.EndsWith(".github.io") =>
                Page(host[..^".github.io".Length], parts),
            _ => Answer(HttpStatusCode.NotFound),
        });
    }

    private Task<HttpResponseMessage> Tree(string[] parts, string query)
    {
        if (parts is not ["repos", var owner, var name, "git", "trees", "HEAD"]
            || query != "?recursive=1"
            || !repos.TryGetValue(Key($"{owner}/{name}"), out var files))
        {
            return Answer(HttpStatusCode.NotFound);
        }
        if (RateLimited)
        {
            var limited = new HttpResponseMessage(HttpStatusCode.Forbidden);
            limited.Headers.Add("X-RateLimit-Remaining", "0");
            return Task.FromResult(limited);
        }
        if (files.Count == 0) return Answer(HttpStatusCode.Conflict);
        var folders = files.Keys
            .SelectMany(Folders)
            .Distinct()
            .Select(p => new { path = p, type = "tree" });
        var blobs = files.Keys.Select(p => new { path = p, type = "blob" });
        var json = JsonSerializer.Serialize(new
        {
            sha = "f00d",
            tree = folders.Concat(blobs).OrderBy(e => e.path),
            truncated = false,
        });
        return Answer(HttpStatusCode.OK, json);
    }

    private Task<HttpResponseMessage> File(string[] parts)
    {
        if (parts is not [var owner, var name, "HEAD", .. var path]
            || !repos.TryGetValue(Key($"{owner}/{name}"), out var files)
            || !files.TryGetValue(string.Join('/', path), out var text))
        {
            return Answer(HttpStatusCode.NotFound);
        }
        return Answer(HttpStatusCode.OK, text);
    }

    private Task<HttpResponseMessage> Page(string owner, string[] parts)
    {
        // The owner's own site, owner.github.io, is served at the root.
        var own = Key($"{owner}/{owner}.github.io") + "|"
            + string.Join('/', parts);
        if (pages.TryGetValue(own, out var text))
            return Answer(HttpStatusCode.OK, text);
        if (parts is not [var name, .. var path]
            || name.Equals($"{owner}.github.io", StringComparison.OrdinalIgnoreCase))
        {
            return Answer(HttpStatusCode.NotFound);
        }
        var key = Key($"{owner}/{name}") + "|" + string.Join('/', path);
        return pages.TryGetValue(key, out text)
            ? Answer(HttpStatusCode.OK, text)
            : Answer(HttpStatusCode.NotFound);
    }

    private Dictionary<string, string> Repo(string repo)
    {
        if (!repos.TryGetValue(Key(repo), out var files))
            repos[Key(repo)] = files = [];
        return files;
    }

    private static IEnumerable<string> Folders(string path)
    {
        for (var i = path.IndexOf('/'); i >= 0; i = path.IndexOf('/', i + 1))
            yield return path[..i];
    }

    private static string Key(string repo) => repo.ToLowerInvariant();

    private static TaskCompletionSource Released()
    {
        var released = new TaskCompletionSource();
        released.SetResult();
        return released;
    }

    private static Task<HttpResponseMessage> Answer(
        HttpStatusCode status, string text = "") =>
        Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(text),
        });
}
