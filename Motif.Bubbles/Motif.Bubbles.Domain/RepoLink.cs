using System.Diagnostics.CodeAnalysis;

namespace Motif.Bubbles;

/// <summary>
/// A link ending #repo=owner/name that opens straight into the bubbles of that
/// repo. It carries only the repo's name, so it always shows the
/// latest code on the default branch.
/// </summary>
public static class RepoLink
{
    public const string Prefix = "repo=";

    public static string Fragment(RepoName repo) => Prefix + repo;

    public static bool TryRead(
        string? fragment, [NotNullWhen(true)] out RepoName? repo)
    {
        repo = null;
        var unescaped = Uri.UnescapeDataString(fragment?.TrimStart('#') ?? "");
        return unescaped.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
            && RepoName.TryParse(unescaped[Prefix.Length..], out repo);
    }
}
