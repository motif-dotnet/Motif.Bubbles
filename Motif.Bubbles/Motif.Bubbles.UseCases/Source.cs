namespace Motif.Bubbles;

public enum SourceKind
{
    None,
    Folder,
    Repo,
}

/// <summary>
/// Where the code on screen came from: a folder or a public repo.
/// <see cref="None"/> before anything is open.
/// </summary>
/// <param name="Name">The folder's name, or the repo's owner/name.</param>
/// <param name="Report">
/// Where its rings came from, in a phrase ("Stryker run 21 Nov, 19:17"), or
/// "" when it has none.
/// </param>
public sealed record Source(SourceKind Kind, string Name, string Report)
{
    public static Source None { get; } = new(SourceKind.None, "", "");

    /// <summary>
    /// Where the code came from, in a sentence.
    /// </summary>
    public string About => Kind switch
    {
        SourceKind.Repo => $"{Name}'s code on its default branch"
            + (Report == "" ? ", with no Stryker report" : $", with its {Report}"),
        SourceKind.Folder => $"The {Name} folder"
            + (Report == "" ? ", with no Stryker report" : $", with its {Report}"),
        _ => "",
    };
}
