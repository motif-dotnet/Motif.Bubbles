using Motif.Bubbles.Adapters;

namespace Motif.Bubbles.Fakes;

/// <summary>
/// A Local Folders Fitting with no browser: the folder the Reviewer will
/// pick, and what's dropped onto the page, set up by the spec. Picking with
/// nothing set up is closing the picker. It keeps every file, even in bin
/// and obj, which the Seam allows.
/// </summary>
public sealed class FakeLocalFolders : LocalFolders
{
    private readonly Dictionary<int, IReadOnlyList<CodeFile>> listings = [];
    private Listing toPick = Listing.None;
    private Listing latest = Listing.None;
    private int count;

    /// <summary>
    /// The folder the picker will hand back next.
    /// </summary>
    public FakeLocalFolders WillPick(string name, IEnumerable<CodeFile> files)
    {
        toPick = Keep(name, files);
        return this;
    }

    /// <summary>
    /// Drops a folder onto the page.
    /// </summary>
    public FakeLocalFolders Drop(string name, IEnumerable<CodeFile> files)
    {
        latest = Keep(name, files);
        return this;
    }

    /// <summary>
    /// Drops files onto the page loose, not in a folder.
    /// </summary>
    public FakeLocalFolders DropLoose(params CodeFile[] files) =>
        Drop("", files);

    public Task<Listing> Pick()
    {
        var picked = toPick;
        toPick = Listing.None;
        return Task.FromResult(picked);
    }

    public Task<Listing> LatestDrop() => Task.FromResult(latest);

    public Task<string> Read(int listing, string path) => Task.FromResult(
        listings.GetValueOrDefault(listing)?
            .FirstOrDefault(f => f.Path == path)?.Text ?? "");

    private Listing Keep(string name, IEnumerable<CodeFile> files)
    {
        var all = files.ToList();
        listings[++count] = all;
        return new Listing(count, name, [.. all.Select(f => f.Path)]);
    }
}
