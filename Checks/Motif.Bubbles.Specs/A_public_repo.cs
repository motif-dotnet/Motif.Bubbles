namespace Motif.Bubbles.Specs.A_public_repo;

file static class Tiny
{
    public const string Repo = "motif-dotnet/Motif.Tiny";

    public const string PagesReport = "motif/mutation-report.json";

    /// <summary>
    /// An app with Motif.Tiny committed to GitHub.
    /// </summary>
    public static Fixture Committed()
    {
        var f = new Fixture();
        f.GitHub.Commit(Repo, TinyRepo.All);
        return f;
    }
}

public sealed class named
{
    [Fact]
    public async Task is_drawn_from_its_default_branch()
    {
        var f = Tiny.Committed();

        await f.Tasks.OpenRepo(Tiny.Repo);

        f.Queries.Graph.Bubbles.Should().HaveCount(14);
        f.Queries.Source.Should().Be(
            new Source(SourceKind.Repo, Tiny.Repo, ""));
        f.Queries.Loading.Should().BeFalse();
    }

    [Fact]
    public async Task is_described_as_its_code_on_its_default_branch()
    {
        var f = Tiny.Committed();

        await f.Tasks.OpenRepo(Tiny.Repo);

        f.Queries.Source.About.Should().Be(
            "motif-dotnet/Motif.Tiny's code on its default branch, with no "
            + "Stryker report");
    }

    [Fact]
    public async Task gives_a_link_straight_to_it()
    {
        var f = Tiny.Committed();

        await f.Tasks.OpenRepo(Tiny.Repo);

        f.Queries.Repo.Should().Be(Tiny.Repo);
        f.Queries.RepoLinkFragment.Should().Be("repo=motif-dotnet/Motif.Tiny");
    }

    [Theory]
    [InlineData("https://github.com/motif-dotnet/Motif.Tiny")]
    [InlineData("github.com/motif-dotnet/Motif.Tiny/tree/main/docs")]
    [InlineData("  motif-dotnet/Motif.Tiny.git ")]
    public async Task by_a_github_com_address_is_drawn_too(string address)
    {
        var f = Tiny.Committed();

        await f.Tasks.OpenRepo(address);

        f.Queries.Repo.Should().Be(Tiny.Repo);
    }

    [Fact]
    public async Task wrongly_cannot_be_drawn()
    {
        var f = new Fixture();

        await f.Tasks.OpenRepo(" just words ");

        f.Queries.Problem.Should().Be(new Problem(
            "That repo can't be drawn",
            "“just words” isn't a GitHub repo. Give it as owner/name, such "
            + "as motif-dotnet/Motif.Glossary."));
    }
}

public sealed class linked_to
{
    [Fact]
    public async Task is_drawn()
    {
        var f = Tiny.Committed();

        var opened = await f.Tasks.OpenLink("#repo=motif-dotnet%2FMotif.Tiny");

        opened.Should().BeTrue();
        f.Queries.Repo.Should().Be(Tiny.Repo);
    }

    [Fact]
    public async Task by_a_fragment_that_is_not_a_repo_link_is_not_opened()
    {
        var f = Tiny.Committed();

        var opened = await f.Tasks.OpenLink("#layers");

        opened.Should().BeFalse();
        f.Queries.Source.Should().Be(Source.None);
    }
}

public sealed class with_a_report_on_its_Pages_site
{
    [Fact]
    public async Task rings_its_bubbles()
    {
        var f = Tiny.Committed();
        f.GitHub.Publish(Tiny.Repo, Tiny.PagesReport,
            new Stryker().Mutated(TinyRepo.Order, killed: 3).Json);

        await f.Tasks.OpenRepo(Tiny.Repo);

        f.Queries.Graph.Find("Domain").Mutation.Should().Be(
            new Mutation(3, 0, 0));
        f.Queries.RingsNote.Should().Be(Note.None);
    }

    [Fact]
    public async Task says_where_the_rings_came_from()
    {
        var f = Tiny.Committed();
        f.GitHub.Publish(Tiny.Repo, Tiny.PagesReport,
            new Stryker().Mutated(TinyRepo.Order, killed: 3).Json);

        await f.Tasks.OpenRepo(Tiny.Repo);

        f.Queries.Source.About.Should().Be(
            "motif-dotnet/Motif.Tiny's code on its default branch, with its "
            + "Stryker report from Pages");
    }

    [Fact]
    public async Task at_the_root_of_an_owners_own_site_rings_its_bubbles()
    {
        var f = new Fixture();
        f.GitHub.Commit("tiny/tiny.github.io", TinyRepo.All);
        f.GitHub.Publish("tiny/tiny.github.io", Tiny.PagesReport,
            new Stryker().Mutated(TinyRepo.Order, killed: 3).Json);

        await f.Tasks.OpenRepo("tiny/tiny.github.io");

        f.Queries.Graph.Find("Domain").Mutation.Caught.Should().Be(3);
    }
}

public sealed class with_no_report_on_its_Pages_site
{
    [Fact]
    public async Task has_no_rings_and_says_where_else_to_find_them()
    {
        var f = Tiny.Committed();

        await f.Tasks.OpenRepo(Tiny.Repo);

        f.Queries.RingsNote.Should().Be(new Note("No mutation rings",
            "motif-dotnet/Motif.Tiny has no Stryker report on its Pages site "
            + "(motif/mutation-report.json). Open its folder instead to see "
            + "the rings from its own StrykerOutput/."));
    }
}

public sealed class is_read
{
    [Fact]
    public async Task fetching_only_the_files_a_scan_needs()
    {
        var f = new Fixture();
        f.GitHub.Commit(Tiny.Repo, TinyRepo.With(
            new CodeFile("README.md", "# Motif.Tiny"),
            new CodeFile("StrykerOutput/x/reports/mutation-report.json", "{}")));

        await f.Tasks.OpenRepo(Tiny.Repo);

        var fetched = f.GitHub.Requests
            .Where(r => r.StartsWith("https://raw.githubusercontent.com/"))
            .ToList();
        fetched.Should().HaveCount(
            TinyRepo.All.Count(file => CodeFile.IsWanted(file.Path)));
        fetched.Should().NotContain(r => r.Contains("README")
            || r.Contains("StrykerOutput") || r.Contains("/bin/"));
    }
}

public sealed class that_is_not_there
{
    [Fact]
    public async Task cannot_be_drawn()
    {
        var f = new Fixture();

        await f.Tasks.OpenRepo("motif-dotnet/Nowhere");

        f.Queries.Problem.Should().Be(new Problem(
            "That repo can't be drawn",
            "motif-dotnet/Nowhere isn't a public GitHub repo, or GitHub can't "
            + "find it."));
        f.Queries.Loading.Should().BeFalse();
    }
}

public sealed class that_is_empty
{
    [Fact]
    public async Task cannot_be_drawn()
    {
        var f = new Fixture();
        f.GitHub.CreateEmpty("motif-dotnet/Empty");

        await f.Tasks.OpenRepo("motif-dotnet/Empty");

        f.Queries.Problem.Should().Be(new Problem(
            "That repo can't be drawn",
            "No C# projects in motif-dotnet/Empty. Open a Motif repo's root "
            + "folder, the one with its .sln, or a public Motif repo."));
    }
}

public sealed class when_GitHub_cannot_be_reached
{
    [Fact]
    public async Task cannot_be_drawn_and_leaves_what_was_open()
    {
        var f = await Fixture.Picked(TinyRepo.All);
        f.GitHub.Unreachable = true;

        await f.Tasks.OpenRepo(Tiny.Repo);

        f.Queries.Problem.Should().Be(new Problem(
            "That repo can't be drawn",
            "GitHub couldn't be reached for motif-dotnet/Motif.Tiny (Failed to "
            + "fetch)."));
        f.Queries.Source.Name.Should().Be("Motif.Tiny");
        f.Queries.Loading.Should().BeFalse();
    }
}

public sealed class when_GitHub_has_had_enough_for_the_hour
{
    [Fact]
    public async Task cannot_be_drawn_and_says_what_to_do()
    {
        var f = Tiny.Committed();
        f.GitHub.RateLimited = true;

        await f.Tasks.OpenRepo(Tiny.Repo);

        f.Queries.Problem.Should().Be(new Problem(
            "That repo can't be drawn",
            "GitHub lets this network list only 60 repos an hour, and that's "
            + "been reached. Try again later, or open the repo's folder "
            + "instead."));
    }
}
