namespace Motif.Bubbles;

/// <summary>
/// A file of a repo, at its path from the repo's root, with "/" between
/// folders.
/// </summary>
public sealed record CodeFile(string Path, string Text)
{
    // Project files mark where each project is; the rest are counted as code.
    private static readonly string[] Extensions =
        [".cs", ".razor", ".js", ".css", ".csproj"];

    // Build output, tools' working folders and other people's code.
    private static readonly HashSet<string> Skipped = new(
        ["bin", "obj", ".git", ".vs", "node_modules", "StrykerOutput",
            "TestResults", "_TEMP_DO_NOT_MERGE"],
        StringComparer.OrdinalIgnoreCase);

    public string Name => Path[(Path.LastIndexOf('/') + 1)..];

    public string Extension =>
        Name.LastIndexOf('.') is var dot and >= 0
            ? Name[dot..].ToLowerInvariant()
            : "";

    public bool IsProjectFile => Extension == ".csproj";

    /// <summary>
    /// Whether a scan reads the file at this path, so a way in fetches only
    /// those.
    /// </summary>
    public static bool IsWanted(string path)
    {
        var parts = path.Split('/');
        return Extensions.Any(e =>
                path.EndsWith(e, StringComparison.OrdinalIgnoreCase))
            && !parts[..^1].Any(Skipped.Contains);
    }
}
