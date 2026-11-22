namespace Motif.Bubbles.BrowserSpecs.The_app_bar;

/// <summary>
/// At a phone's width, between the phone and narrow breakpoints, and on a
/// computer, in each mode the app shows.
/// </summary>
[Collection(SiteCollection.Name)]
public sealed class fits_with_nothing_overlapping(Site site)
{
    public static TheoryData<int, Showing> Cases()
    {
        var cases = new TheoryData<int, Showing>();
        foreach (var width in new[] { 375, 637, 1280 })
            foreach (var showing in Enum.GetValues<Showing>())
                cases.Add(width, showing);
        return cases;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task at_each_width_in_each_mode(int width, Showing showing)
    {
        await using var profile = await site.Profile(width);
        var page = await new Modes(site).Open(profile, showing);

        var bar = await Measuring.Measure(page, "header.appbar");

        bar.ContentWidth.Should().BeLessThanOrEqualTo(bar.Width);
        bar.Overlapping.Should().BeEmpty();
    }
}
