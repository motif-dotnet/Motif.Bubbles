namespace Motif.Bubbles.Adapters;

/// <summary>
/// The Local Folders Seam: what opening a folder needs from the Reviewer's
/// computer, one call each. Every Fitting must behave as these comments say.
/// </summary>
public interface LocalFolders
{
    /// <summary>
    /// Shows the folder picker and lists the folder picked: every file in it,
    /// at any depth. It may leave out what's inside folders named bin, obj,
    /// node_modules, .git or .vs. <see cref="Listing.None"/> when the
    /// Reviewer closes it.
    /// </summary>
    Task<Listing> Pick();

    /// <summary>
    /// The latest drop onto the page, listed, as often as it's asked for. A
    /// folder dropped is listed as a folder picked is, and files dropped loose
    /// by their names, with no folder name. Dropping something that isn't
    /// files, such as text, isn't a drop. Before the first,
    /// <see cref="Listing.None"/>.
    /// </summary>
    Task<Listing> LatestDrop();

    /// <summary>
    /// The text of a file in a listing, by its path there. "" if the listing
    /// is gone: only the latest few are kept.
    /// </summary>
    Task<string> Read(int listing, string path);
}
