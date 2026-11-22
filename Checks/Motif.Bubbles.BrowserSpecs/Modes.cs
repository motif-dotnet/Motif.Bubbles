namespace Motif.Bubbles.BrowserSpecs;

/// <summary>What the page is showing when it is measured.</summary>
public enum Showing
{
    /// <summary>The ways in, with no graph yet.</summary>
    Start,

    /// <summary>
    /// A repo link: the repo's long name in a pill, and a banner, as it has
    /// no Stryker report.
    /// </summary>
    RepoLink,

    /// <summary>A folder picked in the real picker.</summary>
    Folder,
}

/// <summary>Opens the app in each mode it shows.</summary>
public sealed class Modes(Site site)
{
    /// <summary>
    /// Opens the app showing this, keeping a trace when that fails.
    /// </summary>
    public async Task<IPage> Open(IBrowserContext profile, Showing showing)
    {
        try
        {
            return await Show(profile, showing);
        }
        catch
        {
            var width = profile.Pages.FirstOrDefault()?.ViewportSize?.Width;
            await Site.KeepTrace(profile, $"{showing}-{width}");
            throw;
        }
    }

    private async Task<IPage> Show(IBrowserContext profile, Showing showing)
    {
        switch (showing)
        {
            case Showing.Start:
                return await site.Open(profile);
            case Showing.RepoLink:
                return await Drawn(
                    await site.Open(profile, "#repo=" + Site.ThisRepo));
            default:
                var page = await site.Open(profile);
                // The picker is the browser's own, so a spec can't drive it:
                // it hands over the folder instead, as the Reviewer would.
                var chooser = await page.RunAndWaitForFileChooserAsync(
                    () => page.Locator("button.way").ClickAsync());
                await chooser.SetFilesAsync(Site.Folder);
                return await Drawn(page);
        }
    }

    /// <summary>Waits for the graph to show.</summary>
    public static async Task<IPage> Drawn(IPage page)
    {
        await page.Locator(".graph .node").First.WaitForAsync();
        return page;
    }
}
