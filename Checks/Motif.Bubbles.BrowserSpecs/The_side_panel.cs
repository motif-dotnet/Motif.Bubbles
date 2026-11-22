namespace Motif.Bubbles.BrowserSpecs.The_side_panel;

[Collection(SiteCollection.Name)]
public sealed class starts_closed(Site site)
{
    [Fact]
    public async Task leaving_the_graph_the_whole_width()
    {
        await using var profile = await site.Profile();
        var page = await new Modes(site).Open(profile, Showing.RepoLink);

        await Assertions.Expect(page.Locator("aside.side")).ToHaveCountAsync(0);
        var graph = await page.Locator(".graph").BoundingBoxAsync();
        graph!.Width.Should().Be(1280);
    }
}

[Collection(SiteCollection.Name)]
public sealed class opened(Site site)
{
    private async Task<IPage> Opened(IBrowserContext profile)
    {
        var page = await new Modes(site).Open(profile, Showing.RepoLink);
        await page.GetByTitle("Show the layers and key").ClickAsync();
        await page.Locator("aside.side").WaitForAsync();
        return page;
    }

    [Fact]
    public async Task sits_beside_the_graph_on_a_computer()
    {
        await using var profile = await site.Profile(1280);
        var page = await Opened(profile);

        var graph = await page.Locator(".graph").BoundingBoxAsync();
        var side = await page.Locator("aside.side").BoundingBoxAsync();
        side!.X.Should().BeApproximately(graph!.X + graph.Width, 1);
    }

    [Fact]
    public async Task sits_under_the_graph_on_a_narrow_screen()
    {
        await using var profile = await site.Profile(637);
        var page = await Opened(profile);

        var graph = await page.Locator(".graph").BoundingBoxAsync();
        var side = await page.Locator("aside.side").BoundingBoxAsync();
        side!.Y.Should().BeApproximately(graph!.Y + graph.Height, 1);
    }

    [Fact]
    public async Task closes_again()
    {
        await using var profile = await site.Profile();
        var page = await Opened(profile);

        await page.GetByTitle("Hide the layers and key").ClickAsync();

        await Assertions.Expect(page.Locator("aside.side")).ToHaveCountAsync(0);
    }
}
