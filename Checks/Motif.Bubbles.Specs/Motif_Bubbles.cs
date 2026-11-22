namespace Motif.Bubbles.Specs.Motif_Bubbles;

public sealed class pointed_at_itself
{
    [Fact]
    public async Task draws_itself()
    {
        var graph = await Fixture.Drawn(Fixture.OwnCode());

        graph.Bubbles.Select(b => b.Id).Should().Contain(
        [
            "WebApp", "Specs", "UserTasks", "UserQueries", "Application",
            "Domain", "port:ForOpeningFolders", "port:ForOpeningRepos",
            "adapter:ForOpeningFoldersViaPickerAndDrop",
            "adapter:ForOpeningReposViaGitHub",
            "fake:FakeLocalFolders", "fake:FakeGitHub",
        ]);
    }

    [Fact]
    public async Task with_its_fakes_in_the_rows_of_their_ports()
    {
        var graph = await Fixture.Drawn(Fixture.OwnCode());

        graph.Find("fake:FakeGitHub").Lane.Should().Be(
            graph.Find("adapter:ForOpeningReposViaGitHub").Lane);
        graph.Find("fake:FakeLocalFolders").Lane.Should().Be(
            graph.Find("adapter:ForOpeningFoldersViaPickerAndDrop").Lane);
    }
}
