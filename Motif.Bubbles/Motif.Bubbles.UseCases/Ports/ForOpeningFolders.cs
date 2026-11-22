namespace Motif.Bubbles;

/// <summary>
/// A folder the Reviewer opened or dropped in: its name, the files a scan
/// reads (<see cref="CodeFile.IsWanted"/>), and its latest Stryker report.
/// </summary>
/// <param name="Name">
/// The folder's name, or "" for files dropped loose.
/// </param>
public sealed record OpenedFolder(
    string Name, IReadOnlyList<CodeFile> Files, FoundReport Report)
{
    /// <summary>
    /// Nothing opened: the picker was closed, or nothing new was dropped.
    /// </summary>
    public static OpenedFolder None { get; } = new("", [], FoundReport.None);
}

/// <summary>
/// Reads the code in a folder the Reviewer opens or drops in, and the latest
/// Stryker report in its StrykerOutput/ folder.
/// </summary>
public interface ForOpeningFolders
{
    /// <summary>
    /// Shows the folder picker. <see cref="OpenedFolder.None"/> if the
    /// Reviewer closes it.
    /// </summary>
    Task<OpenedFolder> Pick();

    /// <summary>
    /// The most recent drop onto the page, read once.
    /// <see cref="OpenedFolder.None"/> if there's been no drop since.
    /// </summary>
    Task<OpenedFolder> TakeDropped();
}
