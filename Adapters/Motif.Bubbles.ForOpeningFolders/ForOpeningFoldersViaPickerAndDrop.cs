namespace Motif.Bubbles.Adapters;

/// <summary>
/// A folder picked with the folder picker, or dropped onto the page, over
/// the <see cref="LocalFolders"/> Seam. It reads only the files a scan needs,
/// and the latest report Stryker wrote in the folder.
/// </summary>
public sealed class ForOpeningFoldersViaPickerAndDrop(LocalFolders local)
    : ForOpeningFolders
{
    private const string StrykerOutput = "StrykerOutput";

    private int taken;

    public async Task<OpenedFolder> Pick() => await Open(await local.Pick());

    public async Task<OpenedFolder> TakeDropped()
    {
        var drop = await local.LatestDrop();
        // The page hears of every drop, files or not, so a drop of text
        // would otherwise open the folder dropped before it again.
        if (drop.Number == taken) return OpenedFolder.None;
        taken = drop.Number;
        return await Open(drop);
    }

    private async Task<OpenedFolder> Open(Listing listing)
    {
        if (listing.Number == 0) return OpenedFolder.None;
        var files = await Task.WhenAll(listing.Paths
            .Where(CodeFile.IsWanted)
            .Select(async path => new CodeFile(
                path, await local.Read(listing.Number, path))));
        return new OpenedFolder(
            listing.Name, files, await LatestReport(listing));
    }

    /// <summary>
    /// The report in the newest of Stryker's run folders: it names each one
    /// for when it ran (StrykerOutput/2026-11-21.19-17-41), so the newest
    /// sorts last.
    /// </summary>
    private async Task<FoundReport> LatestReport(Listing listing)
    {
        var latest = listing.Paths
            .Select(path => (Path: path, Run: Run(path)))
            .Where(x => x.Run != "")
            .OrderBy(x => x.Run, StringComparer.Ordinal)
            .LastOrDefault();
        if (latest.Path is null) return FoundReport.None;
        return new FoundReport(
            await local.Read(listing.Number, latest.Path), latest.Run);
    }

    /// <summary>
    /// The run folder of a Stryker report's path, or "" if it isn't one.
    /// </summary>
    private static string Run(string path) =>
        path.Split('/') is [.., StrykerOutput, var run, "reports",
            MutationReport.FileName]
            ? run
            : "";
}
