namespace Motif.Bubbles.Specs.A_folder;

public sealed class picked
{
    [Fact]
    public async Task is_drawn()
    {
        var f = new Fixture();
        f.LocalFolders.WillPick("Motif.Tiny", TinyRepo.All);

        await f.Tasks.OpenFolder();

        f.Queries.Graph.Bubbles.Should().HaveCount(14);
        f.Queries.Source.Should().Be(
            new Source(SourceKind.Folder, "Motif.Tiny", ""));
        f.Queries.Repo.Should().Be("");
        f.Queries.RepoLinkFragment.Should().Be("");
    }

    [Fact]
    public async Task is_described_with_its_report()
    {
        var f = await Fixture.Picked(TinyRepo.With(new Stryker()
            .Mutated(TinyRepo.Order, killed: 1)
            .In("2026-11-21.19-17-41")));

        f.Queries.Source.About.Should().Be(
            "The Motif.Tiny folder, with its Stryker run 21 Nov, 19:17");
    }

    [Fact]
    public async Task with_no_report_is_described_so()
    {
        var f = await Fixture.Picked(TinyRepo.All);

        f.Queries.Source.About.Should().Be(
            "The Motif.Tiny folder, with no Stryker report");
    }
}

public sealed class not_picked
{
    [Fact]
    public async Task when_the_picker_is_closed_leaves_what_was_open()
    {
        var f = await Fixture.Picked(TinyRepo.All);
        var before = f.Queries.Graph;

        await f.Tasks.OpenFolder();

        f.Queries.Graph.Should().BeSameAs(before);
        f.Queries.Problem.Should().Be(Problem.None);
    }
}

public sealed class dropped
{
    [Fact]
    public async Task is_drawn()
    {
        var f = new Fixture();
        f.LocalFolders.Drop("Motif.Tiny", TinyRepo.All);

        await f.Tasks.OpenDroppedFolder();

        f.Queries.Graph.Bubbles.Should().HaveCount(14);
        f.Queries.Source.Name.Should().Be("Motif.Tiny");
    }

    [Fact]
    public async Task as_loose_files_is_drawn_as_the_files_dropped()
    {
        var f = new Fixture();
        f.LocalFolders.DropLoose(TinyRepo.Domain);

        await f.Tasks.OpenDroppedFolder();

        f.Queries.Graph.Bubbles.Select(b => b.Name).Should().Equal("Domain");
        f.Queries.Source.Should().Be(
            new Source(SourceKind.Files, "Files dropped", ""));
        f.Queries.Source.About.Should().Be(
            "The files dropped, with no Stryker report");
    }

    [Fact]
    public async Task as_loose_files_with_no_projects_says_so_of_them()
    {
        var f = new Fixture();
        f.LocalFolders.DropLoose(new CodeFile("Notes.cs", "class Notes { }"));

        await f.Tasks.OpenDroppedFolder();

        f.Queries.Problem.Should().Be(new Problem(
            "Those files can't be drawn",
            "No C# projects in the files dropped. Open a Motif repo's root "
            + "folder, the one with its .sln, or a public Motif repo."));
    }

    [Fact]
    public async Task is_opened_once_so_a_drop_of_text_after_it_changes_nothing()
    {
        var f = new Fixture();
        f.LocalFolders.Drop("Motif.Tiny", TinyRepo.All);
        await f.Tasks.OpenDroppedFolder();
        f.LocalFolders.WillPick("Other", TinyRepo.Domain);
        await f.Tasks.OpenFolder();

        // The page hears of a drop of text, but there's no new drop.
        await f.Tasks.OpenDroppedFolder();

        f.Queries.Source.Name.Should().Be("Other");
    }
}

public sealed class with_Stryker_reports
{
    [Fact]
    public async Task rings_its_bubbles_from_the_latest()
    {
        var f = await Fixture.Picked(TinyRepo.With(
            new Stryker().Mutated(TinyRepo.Order, killed: 1)
                .In("2026-11-20.09-00-00"),
            new Stryker().Mutated(TinyRepo.Order, killed: 2)
                .In("2026-11-21.19-17-41"),
            new Stryker().Mutated(TinyRepo.Order, killed: 3)
                .In("2026-11-19.23-59-59")));

        f.Queries.Graph.Find("Domain").Mutation.Caught.Should().Be(2);
        f.Queries.Source.Report.Should().Be("Stryker run 21 Nov, 19:17");
    }

    [Fact]
    public async Task ignores_any_report_outside_StrykerOutput()
    {
        var f = await Fixture.Picked(TinyRepo.With(
            new CodeFile("docs/reports/mutation-report.json",
                new Stryker().Mutated(TinyRepo.Order, killed: 1).Json)));

        f.Queries.Graph.Find("Domain").Mutation.Should().Be(Mutation.None);
    }
}

public sealed class with_no_Stryker_report
{
    [Fact]
    public async Task has_no_rings_and_says_how_to_get_them()
    {
        var f = await Fixture.Picked(TinyRepo.All);

        f.Queries.Graph.Bubbles.Should().OnlyContain(
            b => b.Mutation == Mutation.None);
        f.Queries.RingsNote.Should().Be(new Note("No mutation rings",
            "There's no Stryker JSON report in this folder's StrykerOutput/. "
            + "Run dotnet stryker with the json reporter to see how well the "
            + "specs catch mistakes."));
    }
}

public sealed class with_a_report_that_cannot_be_read
{
    [Fact]
    public async Task has_no_rings_and_says_why_when_it_is_not_JSON()
    {
        var f = await Fixture.Picked(TinyRepo.With(new CodeFile(
            "StrykerOutput/2026-11-21.19-17-41/reports/mutation-report.json",
            "<html>")));

        f.Queries.RingsNote.Title.Should().Be(
            "The Stryker report couldn't be read");
        f.Queries.RingsNote.Text.Should().StartWith(
            "So the bubbles have no rings. It isn't valid JSON");
        f.Queries.Source.Report.Should().Be("");
    }

    [Fact]
    public async Task has_no_rings_and_says_why_when_it_has_no_files()
    {
        var f = await Fixture.Picked(TinyRepo.With(new CodeFile(
            "StrykerOutput/2026-11-21.19-17-41/reports/mutation-report.json",
            """{ "schemaVersion": "2" }""")));

        f.Queries.RingsNote.Text.Should().Be(
            "So the bubbles have no rings. It has no files: is it a Stryker "
            + "mutation report?");
    }
}

public sealed class with_a_report_for_other_code
{
    [Fact]
    public async Task has_no_rings_and_says_so()
    {
        var other = new CodeFile("Motif.Other/Other.cs", "class Other { }");
        var f = await Fixture.Picked(TinyRepo.With(new Stryker()
            .Mutated(other, killed: 3)
            .In("2026-11-21.19-17-41")));

        f.Queries.RingsNote.Should().Be(new Note(
            "The Stryker report doesn't match this code",
            "None of the files it mutated are here, so the bubbles have no "
            + "rings."));
    }
}

public sealed class with_no_projects
{
    [Fact]
    public async Task cannot_be_drawn_and_leaves_what_was_open()
    {
        var f = await Fixture.Picked(TinyRepo.All);
        f.LocalFolders.WillPick("Notes", [new("readme.cs", "class A { }")]);

        await f.Tasks.OpenFolder();

        f.Queries.Problem.Should().Be(new Problem(
            "That folder can't be drawn",
            "No C# projects in Notes. Open a Motif repo's root folder, the "
            + "one with its .sln, or a public Motif repo."));
        f.Queries.Source.Name.Should().Be("Motif.Tiny");
    }

    [Fact]
    public async Task says_so_until_the_problem_is_dismissed()
    {
        var f = new Fixture();
        f.LocalFolders.WillPick("Notes", [new("readme.cs", "class A { }")]);
        await f.Tasks.OpenFolder();

        f.Tasks.DismissProblem();

        f.Queries.Problem.Should().Be(Problem.None);
    }
}
