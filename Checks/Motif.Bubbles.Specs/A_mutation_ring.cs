namespace Motif.Bubbles.Specs.A_mutation_ring;

file static class Ringed
{
    public const string Run = "2026-11-21.19-17-41";

    /// <summary>
    /// The graph of Motif.Tiny picked as a folder, with this report in its
    /// StrykerOutput/.
    /// </summary>
    public static async Task<Fixture> Picked(Stryker report) =>
        await Fixture.Picked(TinyRepo.With(report.In(Run)));

    public static async Task<Graph> Drawn(Stryker report) =>
        (await Picked(report)).Queries.Graph;
}

public sealed class counts_the_mutants_in_its_bubbles_files
{
    [Fact]
    public async Task as_caught_survived_or_unreached()
    {
        var graph = await Ringed.Drawn(new Stryker()
            .Mutated(TinyRepo.Order, killed: 7, survived: 2, noCoverage: 1));

        graph.Find("Domain").Mutation.Should().Be(new Mutation(7, 2, 1));
    }

    [Fact]
    public async Task with_timeouts_as_caught()
    {
        var graph = await Ringed.Drawn(new Stryker()
            .Mutated(TinyRepo.Order, killed: 3, timeout: 2));

        graph.Find("Domain").Mutation.Caught.Should().Be(5);
    }

    [Fact]
    public async Task leaving_out_those_Stryker_did_not_run()
    {
        var graph = await Ringed.Drawn(new Stryker()
            .Mutated(TinyRepo.Order, killed: 4, ignored: 3, compileError: 2));

        graph.Find("Domain").Mutation.Total.Should().Be(4);
    }

    [Fact]
    public async Task across_all_its_files()
    {
        var graph = await Ringed.Drawn(new Stryker()
            .Mutated(TinyRepo.Application, killed: 5)
            .Mutated(TinyRepo.Mail, survived: 1));

        graph.Find("Application").Mutation.Should().Be(new Mutation(5, 1, 0));
    }
}

public sealed class shows_its_score
{
    [Fact]
    public async Task inside_its_bubble_as_a_whole_percentage()
    {
        var graph = await Ringed.Drawn(new Stryker()
            .Mutated(TinyRepo.Order, killed: 2, survived: 1));

        graph.Find("Domain").Inside.Should().Be("67%");
    }

    [Fact]
    public async Task with_what_got_away_under_its_name()
    {
        var graph = await Ringed.Drawn(new Stryker()
            .Mutated(TinyRepo.Order, killed: 8, survived: 1, noCoverage: 2));

        graph.Find("Domain").Stats.Should().Be(
            "7 loc · 1 survived · 2 unreached");
    }

    [Fact]
    public async Task with_every_count_in_its_details()
    {
        var graph = await Ringed.Drawn(new Stryker()
            .Mutated(TinyRepo.Order, killed: 8, survived: 1, noCoverage: 1));

        graph.Find("Domain").Details.Should().ContainInOrder(
            new Detail("Mutation score", "80%"),
            new Detail("Caught", "8"),
            new Detail("Survived", "1"),
            new Detail("No spec reaches", "1"));
    }
}

public sealed class is_missing
{
    [Fact]
    public async Task from_bubbles_Stryker_did_not_mutate()
    {
        var graph = await Ringed.Drawn(new Stryker()
            .Mutated(TinyRepo.Order, killed: 1));

        graph.Find("WebApp").Mutation.Should().Be(Mutation.None);
        graph.Find("WebApp").Details.Should().Contain(
            new Detail("Mutation score", "not mutated"));
    }

    [Fact]
    public async Task when_Stryker_ran_none_of_its_mutants()
    {
        var graph = await Ringed.Drawn(new Stryker()
            .Mutated(TinyRepo.Order, ignored: 4));

        graph.Find("Domain").Mutation.Should().Be(Mutation.None);
    }
}

public sealed class is_stale
{
    [Fact]
    public async Task when_a_file_has_changed_since_Stryker_ran()
    {
        var graph = await Ringed.Drawn(new Stryker()
            .Mutated(TinyRepo.Order, killed: 1, source: "namespace Old;"));

        graph.Find("Domain").Mutation.Stale.Should().BeTrue();
    }

    [Fact]
    public async Task when_a_CSharp_file_is_new_since_Stryker_ran()
    {
        var graph = await Ringed.Drawn(new Stryker()
            .Mutated(TinyRepo.Application, killed: 1));

        // Mail.cs isn't in the report.
        graph.Find("Application").Mutation.Stale.Should().BeTrue();
    }

    [Fact]
    public async Task and_says_so_in_its_details()
    {
        var graph = await Ringed.Drawn(new Stryker()
            .Mutated(TinyRepo.Order, killed: 1, source: "namespace Old;"));

        graph.Find("Domain").Details.Should().Contain(
            new Detail("Ring", "older than the code"));
    }
}

public sealed class is_current
{
    [Fact]
    public async Task when_its_files_are_as_Stryker_mutated_them()
    {
        var graph = await Ringed.Drawn(new Stryker()
            .Mutated(TinyRepo.Order, killed: 1));

        graph.Find("Domain").Mutation.Stale.Should().BeFalse();
    }

    [Fact]
    public async Task whatever_the_line_endings()
    {
        var graph = await Ringed.Drawn(new Stryker().Mutated(
            TinyRepo.Order,
            killed: 1,
            source: TinyRepo.Order.Text.ReplaceLineEndings("\r\n")));

        graph.Find("Domain").Mutation.Stale.Should().BeFalse();
    }
}

public sealed class that_are_out_of_date
{
    [Fact]
    public async Task are_noted_with_how_many()
    {
        var app = await Ringed.Picked(new Stryker()
            .Mutated(TinyRepo.Order, killed: 1, source: "namespace Old;")
            .Mutated(TinyRepo.Application, killed: 1)
            .Mutated(TinyRepo.Mail, killed: 1, source: "namespace Old;")
            .Mutated(TinyRepo.Smtp, killed: 1));

        app.Queries.RingsNote.Should().Be(new Note(
            "Some rings are older than the code",
            "2 bubbles have changed since Stryker ran, so their faded rings "
            + "may be out of date."));
    }

    [Fact]
    public async Task are_noted_for_one_bubble()
    {
        var app = await Ringed.Picked(new Stryker()
            .Mutated(TinyRepo.Order, killed: 1, source: "namespace Old;"));

        app.Queries.RingsNote.Text.Should().StartWith("1 bubble has changed");
    }

    [Fact]
    public async Task are_not_noted_when_all_are_current()
    {
        var app = await Ringed.Picked(new Stryker()
            .Mutated(TinyRepo.Order, killed: 1));

        app.Queries.RingsNote.Should().Be(Note.None);
    }
}
