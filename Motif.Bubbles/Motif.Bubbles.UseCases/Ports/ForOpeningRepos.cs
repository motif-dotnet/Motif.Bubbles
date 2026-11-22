namespace Motif.Bubbles;

public enum RepoRead
{
    Read,

    /// <summary>
    /// There's no such repo, or it isn't public: GitHub says the same for
    /// both.
    /// </summary>
    NotFound,

    /// <summary>
    /// GitHub won't list any more repos from this network for a while.
    /// </summary>
    RateLimited,
}

/// <summary>
/// What was read from a public repo: the files a scan reads
/// (<see cref="CodeFile.IsWanted"/>) from its default branch, and the Stryker
/// report on its Pages site.
/// </summary>
public sealed record RepoCode(
    RepoRead Outcome, IReadOnlyList<CodeFile> Files, FoundReport Report)
{
    public static RepoCode NotFound { get; } =
        new(RepoRead.NotFound, [], FoundReport.None);

    public static RepoCode RateLimited { get; } =
        new(RepoRead.RateLimited, [], FoundReport.None);
}

/// <summary>
/// Reads a public repo's code from its default branch, and its latest
/// Stryker report from its Pages site if it has one.
/// </summary>
public interface ForOpeningRepos
{
    /// <summary>
    /// Throws <see cref="HttpRequestException"/> when GitHub can't be reached
    /// at all.
    /// </summary>
    Task<RepoCode> Read(
        RepoName repo, CancellationToken cancellationToken = default);
}
