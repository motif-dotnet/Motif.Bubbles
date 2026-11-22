namespace Motif.Bubbles.Specs.A_bubble;

public sealed class counts_its_lines_of_code
{
    [Fact]
    public async Task leaving_out_blank_lines_and_comments()
    {
        var graph = await Fixture.Drawn(TinyRepo.All);

        graph.Find("Domain").Size.LinesOfCode.Should().Be(7);
    }

    [Fact]
    public async Task across_every_kind_of_file_it_holds()
    {
        var graph = await Fixture.Drawn(TinyRepo.All);

        // Program.cs 4, Shell.razor 3 and app.css 1.
        graph.Find("WebApp").Size.LinesOfCode.Should().Be(8);
    }

    [Fact]
    public async Task leaving_out_block_comments()
    {
        var graph = await Fixture.Drawn(TinyRepo.With(
            new CodeFile("Motif.Tiny/Motif.Tiny.Domain/Note.cs",
                """
                /* A note
                   over three
                   lines. */
                class Note { }
                """)));

        graph.Find("Domain").Size.LinesOfCode.Should().Be(7 + 1);
    }
}

public sealed class counts_its_files_and_types
{
    [Fact]
    public async Task with_a_Razor_component_as_one_type()
    {
        var graph = await Fixture.Drawn(TinyRepo.All);

        graph.Find("Domain").Size.Should().Be(new Size(7, 1, 2));
        graph.Find("WebApp").Size.Types.Should().Be(1);
    }

    [Fact]
    public async Task across_its_files()
    {
        var graph = await Fixture.Drawn(TinyRepo.All);

        // MotifTinyApplication.cs and Mail.cs, a type each.
        graph.Find("Application").Size.Types.Should().Be(2);
    }
}

public sealed class of_specs_counts_them
{
    [Fact]
    public async Task with_a_Theory_once_however_many_cases_it_has()
    {
        var graph = await Fixture.Drawn(TinyRepo.All);

        graph.Find("Specs").Specs.Should().Be(3);
        graph.Find("BrowserSpecs").Specs.Should().Be(1);
    }

    [Fact]
    public async Task across_its_files()
    {
        var graph = await Fixture.Drawn(TinyRepo.With(new CodeFile(
            "Checks/Motif.Tiny.Specs/Mailing.cs",
            "[Fact] public void sends() { }\n[Fact] public void retries() { }\n")));

        graph.Find("Specs").Specs.Should().Be(3 + 2);
    }
}

public sealed class of_a_port_counts_its_members
{
    [Fact]
    public async Task with_one_over_several_lines_once()
    {
        var graph = await Fixture.Drawn(TinyRepo.All);

        graph.Find("UserTasks").Members.Should().Be(2);
        graph.Find("UserQueries").Members.Should().Be(1);
        graph.Find("port:ForStoringOrders").Members.Should().Be(2);
    }

    [Fact]
    public async Task and_not_those_of_other_types_in_its_file()
    {
        var port = new CodeFile(
            "Motif.Tiny/Motif.Tiny.UseCases/Ports/ForPaying.cs",
            """
            namespace Motif.Tiny;

            public sealed record Payment(int Pence)
            {
                public static Payment None { get; } = new(0);
            }

            public interface ForPaying
            {
                Task Pay(Payment payment);

                bool CanPay { get; }
            }

            public sealed class Receipt
            {
                public void Print();
            }
            """);

        var graph = await Fixture.Drawn(TinyRepo.With(port));

        graph.Find("port:ForPaying").Members.Should().Be(2);
    }

    [Fact]
    public async Task with_its_brace_on_the_same_line()
    {
        var port = new CodeFile(
            "Motif.Tiny/Motif.Tiny.UseCases/Ports/ForPaying.cs",
            """
            public interface ForPaying {
                Task Pay(int pence);
            }
            """);

        var graph = await Fixture.Drawn(TinyRepo.With(port));

        graph.Find("port:ForPaying").Members.Should().Be(1);
    }

    [Fact]
    public async Task in_its_own_words()
    {
        var graph = await Fixture.Drawn(TinyRepo.All);

        graph.Find("UserTasks").Stats.Should().Be("7 loc · 2 tasks");
        graph.Find("UserQueries").Stats.Should().Be("5 loc · 1 queries");
        graph.Find("port:ForSendingMail").Stats.Should().Be(
            "5 loc · 1 members");
    }
}

public sealed class is_named
{
    [Fact]
    public async Task for_its_part_with_its_type_in_its_role()
    {
        var graph = await Fixture.Drawn(TinyRepo.All);

        graph.Find("UserTasks").Role.Should().Be(
            "User port: the commands (ForDoing)");
        graph.Find("Application").Role.Should().Be(
            "Use cases (MotifTinyApplication)");
        graph.Find("port:ForStoringOrders").Role.Should().Be(
            "Driven port (ForStoringOrders)");
        graph.Find("adapter:ForStoringOrdersViaIndexedDB").Role.Should().Be(
            "Real adapter (ForStoringOrdersViaIndexedDB)");
        graph.Find("fake:FakeShelf").Role.Should().Be(
            "Fake for storing orders (FakeShelf)");
        graph.Find("WebApp").Role.Should().Be(
            "Driving adapter (Motif.Tiny.App)");
        graph.Find("UserQueries").Role.Should().Be(
            "User port: the queries (ForSeeing)");
        graph.Find("Domain").Role.Should().Be("Domain: the app's own rules");
        graph.Find("Specs").Role.Should().Be("Specs: drive the User port");
        graph.Find("BrowserSpecs").Role.Should().Be(
            "Browser specs: drive the Web App in a browser");
    }

    [Fact]
    public async Task as_use_cases_when_no_class_is_the_Application()
    {
        var graph = await Fixture.Drawn(TinyRepo.Without(TinyRepo.Application));

        graph.Find("Application").Role.Should().Be("Use cases");
    }

    [Fact]
    public async Task as_a_library_by_its_project()
    {
        var graph = await Fixture.Drawn(
        [
            new("Motif.Words/Motif.Words.csproj", "<Project />"),
            new("Motif.Words/Words.cs", "class Words { }\n"),
        ]);

        graph.Find("library:Motif.Words").Role.Should().Be(
            "Library (Motif.Words)");
    }

    [Theory]
    [InlineData("ForOpeningFilesViaUIPicker", "UI picker")]
    [InlineData("ForSavingViaGitHub", "GitHub")]
    [InlineData("ForSavingViaJavaScriptAndWebAssembly",
        "JavaScript and WebAssembly")]
    public async Task with_the_way_after_Via_in_words(string type, string name)
    {
        var port = type[..type.IndexOf("Via")];
        var graph = await Fixture.Drawn(
        [
            new($"Adapters/X.{port}/X.{port}.csproj", "<Project />"),
            new($"Adapters/X.{port}/{type}.cs", $"class {type} {{ }}\n"),
        ]);

        graph.Find("adapter:" + type).Name.Should().Be(name);
    }
}

public sealed class of_an_adapter_project_with_two_ways
{
    private static readonly CodeFile[] Saving =
    [
        new("Adapters/X.ForSaving/X.ForSaving.csproj", "<Project />"),
        new("Adapters/X.ForSaving/ForSavingViaFolder.cs",
            "class ForSavingViaFolder(Disk disk) { }\n"),
        new("Adapters/X.ForSaving/ForSavingViaGist.cs",
            "class ForSavingViaGist(GistApi api) { }\n"),
        new("Adapters/X.ForSaving/Disk.cs", "interface Disk { }\n"),
        new("Adapters/X.ForSaving/GistApi.cs", "interface GistApi { }\n"),
        new("Adapters/X.ForSaving/Shared.cs", "class Shared { }\n"),
    ];

    [Fact]
    public async Task is_a_bubble_for_each()
    {
        var graph = await Fixture.Drawn(Saving);

        graph.Bubbles.Select(b => b.Name).Should().Equal("Folder", "Gist");
    }

    [Fact]
    public async Task with_each_other_file_in_the_first_that_names_it()
    {
        var graph = await Fixture.Drawn(Saving);

        // Shared.cs is named by neither, so it goes to the first.
        graph.Find("adapter:ForSavingViaFolder").Size.Files.Should().Be(3);
        graph.Find("adapter:ForSavingViaGist").Size.Files.Should().Be(2);
    }
}

public sealed class shows_one_number_inside
{
    [Fact]
    public async Task its_specs_or_its_members()
    {
        var graph = await Fixture.Drawn(TinyRepo.All);

        graph.Find("Specs").Inside.Should().Be("3");
        graph.Find("UserTasks").Inside.Should().Be("2");
        graph.Find("Domain").Inside.Should().Be("");
    }
}

public sealed class shows_its_details
{
    [Fact]
    public async Task with_how_many_links_reach_it_and_leave_it()
    {
        var graph = await Fixture.Drawn(TinyRepo.All);

        graph.Details(graph.Find("port:ForStoringOrders")).Should().Equal(
            new Detail("Lines of code", "6"),
            new Detail("Files", "1"),
            new Detail("Types", "1"),
            new Detail("Members", "2"),
            new Detail("Mutation score", "not mutated"),
            new Detail("Depended on by", "4"),
            new Detail("Depends on", "0"));
    }
}
