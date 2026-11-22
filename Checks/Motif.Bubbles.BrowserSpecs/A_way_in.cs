namespace Motif.Bubbles.BrowserSpecs.A_way_in;

/// <summary>
/// Through the real adapters and their Fittings, wired up as the app's
/// composition root wires them.
/// </summary>
[Collection(SiteCollection.Name)]
public sealed class draws_the_repo(Site site)
{
    [Fact]
    public async Task from_a_folder_picked_in_the_real_picker()
    {
        await using var profile = await site.Profile();
        var page = await new Modes(site).Open(profile, Showing.Folder);

        await Assertions.Expect(page.Locator(".appbar .pill").First)
            .ToHaveTextAsync("Motif.Bubbles");
        await Assertions.Expect(page.Locator(".node", new() { HasText = "Domain" }))
            .ToHaveCountAsync(1);
    }

    [Fact]
    public async Task from_GitHub_by_its_link()
    {
        await using var profile = await site.Profile();
        var page = await new Modes(site).Open(profile, Showing.RepoLink);

        await Assertions.Expect(page.Locator(".appbar .pill").First)
            .ToHaveTextAsync(Site.ThisRepo);
        await Assertions.Expect(page.Locator(".node", new() { HasText = "Domain" }))
            .ToHaveCountAsync(1);
        page.Url.Should().EndWith("#repo=" + Site.ThisRepo);
    }
}
