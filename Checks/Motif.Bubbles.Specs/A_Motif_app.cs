namespace Motif.Bubbles.Specs.A_Motif_app;

public sealed class is_drawn_as_a_bubble_for_each_architectural_object
{
    [Fact]
    public async Task from_Domain_to_browser_specs()
    {
        var graph = await Fixture.Drawn(TinyRepo.All);

        graph.Bubbles.Select(b => b.Name).Should().BeEquivalentTo(
        [
            "Browser specs", "Web App", "Specs", "User Tasks", "User Queries",
            "Application", "Domain", "For sending mail", "For storing orders",
            "Smtp", "IndexedDB", "Fake mail server", "Fake shelf", "Full shelf",
        ]);
    }

    [Fact]
    public async Task in_the_columns_of_ports_and_adapters()
    {
        var graph = await Fixture.Drawn(TinyRepo.All);

        string[] Names(Column column) =>
            [.. graph.Bubbles.Where(b => b.Column == column).Select(b => b.Name)];
        Names(Column.BrowserChecks).Should().Equal("Browser specs");
        Names(Column.Driving).Should().BeEquivalentTo("Web App", "Specs");
        Names(Column.UserPort).Should().BeEquivalentTo(
            "User Tasks", "User Queries");
        Names(Column.Application).Should().BeEquivalentTo(
            "Application", "Domain");
        Names(Column.DrivenPorts).Should().BeEquivalentTo(
            "For sending mail", "For storing orders");
        Names(Column.RealAdapters).Should().BeEquivalentTo("Smtp", "IndexedDB");
        Names(Column.Fakes).Should().BeEquivalentTo(
            "Fake mail server", "Fake shelf", "Full shelf");
    }

    [Fact]
    public async Task with_only_the_columns_that_have_bubbles()
    {
        var graph = await Fixture.Drawn(
            [.. TinyRepo.All.Where(f => !f.Path.StartsWith("Checks/"))]);

        graph.Columns.Should().Equal(
            Column.Driving, Column.UserPort, Column.Application,
            Column.DrivenPorts, Column.RealAdapters);
    }

    [Fact]
    public void each_headed_in_words()
    {
        Columns.All.Select(c => c.Heading()).Should().Equal(
            "Browser checks", "Driving", "User port", "Application",
            "Driven ports", "Real adapters", "Fakes", "Shared fakes");
    }
}

public sealed class has_a_row_for_each_driven_port
{
    [Fact]
    public async Task sorted_by_name()
    {
        var graph = await Fixture.Drawn(TinyRepo.All);

        graph.Find("port:ForSendingMail").Lane.Should().BeLessThan(
            graph.Find("port:ForStoringOrders").Lane);
    }

    [Fact]
    public async Task with_its_adapters_and_fakes_in_it()
    {
        var graph = await Fixture.Drawn(TinyRepo.All);

        graph.Find("adapter:ForSendingMailViaSmtp").Lane.Should().Be(0);
        graph.Find("fake:FakeMailServer").Lane.Should().Be(0);
        graph.Find("adapter:ForStoringOrdersViaIndexedDB").Lane.Should().Be(1);
        graph.Find("fake:FakeShelf").Lane.Should().Be(1);
        graph.Find("fake:FullShelf").Lane.Should().Be(2);
    }

    [Fact]
    public async Task as_tall_as_its_adapters_or_fakes_with_the_port_in_the_middle()
    {
        var graph = await Fixture.Drawn(TinyRepo.All);

        graph.Find("port:ForSendingMail").Lane.Should().Be(0);
        graph.Find("port:ForStoringOrders").Lane.Should().Be(1.5);
        graph.Lanes.Should().Be(3);
    }

    [Fact]
    public async Task one_lane_tall_with_neither()
    {
        var graph = await Fixture.Drawn(TinyRepo.Without(
            [.. TinyRepo.SendingMail, .. TinyRepo.Fakes]));

        graph.Find("port:ForSendingMail").Lane.Should().Be(0);
        graph.Find("port:ForStoringOrders").Lane.Should().Be(1);
        graph.Lanes.Should().Be(2);
    }
}

public sealed class spreads_the_columns_on_the_left
{
    [Fact]
    public async Task evenly_down_the_whole_height()
    {
        var graph = await Fixture.Drawn(TinyRepo.All);

        graph.Find("BrowserSpecs").Lane.Should().Be(1);
        graph.Find("WebApp").Lane.Should().Be(0.25);
        graph.Find("Specs").Lane.Should().Be(1.75);
    }

    [Fact]
    public async Task as_tall_as_the_tallest_with_no_driven_ports()
    {
        var graph = await Fixture.Drawn(TinyRepo.Without(
        [
            TinyRepo.ForStoringOrders, TinyRepo.ForSendingMail,
            .. TinyRepo.StoringOrders, .. TinyRepo.SendingMail, .. TinyRepo.Fakes,
        ]));

        graph.Lanes.Should().Be(2);
        graph.Find("Specs").Lane.Should().Be(1);
    }

    [Fact]
    public async Task with_the_Web_App_User_Tasks_and_Application_on_top()
    {
        var graph = await Fixture.Drawn(TinyRepo.All);

        double Lane(string id) => graph.Find(id).Lane;
        Lane("WebApp").Should().BeLessThan(Lane("Specs"));
        Lane("UserTasks").Should().BeLessThan(Lane("UserQueries"));
        Lane("Application").Should().BeLessThan(Lane("Domain"));
    }
}

public sealed class links_its_bubbles
{
    private static async Task<string[]> Links(LinkKind kind)
    {
        var graph = await Fixture.Drawn(TinyRepo.All);
        return
        [
            .. graph.Links
                .Where(l => l.Kind == kind)
                .Select(l => $"{l.From} > {l.To}"),
        ];
    }

    [Fact]
    public async Task from_the_specs_to_what_they_check_through()
    {
        var checks = await Links(LinkKind.Checks);

        checks.Should().BeEquivalentTo(
            "Specs > UserTasks", "Specs > UserQueries",
            "BrowserSpecs > WebApp");
    }

    [Fact]
    public async Task from_the_specs_to_the_one_half_of_the_User_port_there_is()
    {
        var graph = await Fixture.Drawn(TinyRepo.Without(TinyRepo.ForSeeing));

        graph.Links.Should().Contain(
            new Link("Specs", "UserTasks", LinkKind.Checks));
    }

    [Fact]
    public async Task from_users_to_what_they_use()
    {
        var uses = await Links(LinkKind.Uses);

        uses.Should().BeEquivalentTo(
            "WebApp > UserTasks", "WebApp > UserQueries",
            "Application > Domain",
            "Application > port:ForSendingMail",
            "Application > port:ForStoringOrders");
    }

    [Fact]
    public async Task from_what_implements_a_port_to_the_port()
    {
        var implements = await Links(LinkKind.Implements);

        implements.Should().BeEquivalentTo(
            "Application > UserTasks", "Application > UserQueries",
            "adapter:ForSendingMailViaSmtp > port:ForSendingMail",
            "adapter:ForStoringOrdersViaIndexedDB > port:ForStoringOrders",
            "fake:FakeMailServer > port:ForSendingMail",
            "fake:FakeShelf > port:ForStoringOrders",
            "fake:FullShelf > port:ForStoringOrders");
    }

    [Fact]
    public async Task from_the_Web_App_to_real_adapters_and_from_specs_to_fakes()
    {
        var wires = await Links(LinkKind.Wires);

        wires.Should().BeEquivalentTo(
            "WebApp > adapter:ForSendingMailViaSmtp",
            "WebApp > adapter:ForStoringOrdersViaIndexedDB",
            "Specs > fake:FakeMailServer",
            "Specs > fake:FakeShelf",
            "Specs > fake:FullShelf");
    }

    [Fact]
    public async Task with_the_Application_using_only_the_ports_its_code_names()
    {
        var quiet = TinyRepo.Application with
        {
            Text = TinyRepo.Application.Text
                .Replace("ForSendingMail mail", "object mail"),
        };

        var graph = await Fixture.Drawn(
            [.. TinyRepo.Without(TinyRepo.Application), quiet]);

        graph.Links.Should().NotContain(
            new Link("Application", "port:ForSendingMail", LinkKind.Uses));
        graph.Links.Should().Contain(
            new Link("Application", "port:ForStoringOrders", LinkKind.Uses));
    }
}

public sealed class links_only
{
    [Fact]
    public async Task bubbles_that_are_there()
    {
        var graph = await Fixture.Drawn(TinyRepo.Without(
            [.. TinyRepo.BrowserSpecs, .. TinyRepo.Fakes, .. TinyRepo.Domain]));

        var ids = graph.Bubbles.Select(b => b.Id).ToHashSet();
        graph.Links.Should().NotBeEmpty();
        graph.Links.Should().OnlyContain(
            l => ids.Contains(l.From) && ids.Contains(l.To));
    }
}

public sealed class with_projects_inside_each_other
{
    [Fact]
    public async Task gives_each_file_to_the_project_nearest_it()
    {
        var graph = await Fixture.Drawn(
        [
            new("Motif.Tiny.App.csproj", "<Project />"),
            new("Program.cs", "var app = 1;\n"),
            new("Motif.Tiny.Domain/Motif.Tiny.Domain.csproj", "<Project />"),
            new("Motif.Tiny.Domain/Order.cs", "class Order { }\n"),
        ]);

        graph.Find("WebApp").Size.Files.Should().Be(1);
        graph.Find("Domain").Size.Files.Should().Be(1);
    }
}

public sealed class with_a_project_of_no_files
{
    [Fact]
    public async Task draws_it_with_no_code()
    {
        var graph = await Fixture.Drawn(TinyRepo.Without(TinyRepo.Order));

        graph.Find("Domain").Size.Should().Be(new Size(0, 0, 0));
    }
}

public sealed class with_use_cases_that_are_all_ports
{
    [Fact]
    public async Task has_no_Application()
    {
        var graph = await Fixture.Drawn(TinyRepo.Without(
            TinyRepo.Application, TinyRepo.Mail));

        graph.Bubbles.Should().NotContain(b => b.Id == "Application");
    }
}

public sealed class with_a_port_outside_Ports
{
    [Fact]
    public async Task counts_it_as_part_of_the_Application()
    {
        var graph = await Fixture.Drawn(TinyRepo.With(
            new CodeFile("Motif.Tiny/Motif.Tiny.UseCases/ForPaying.cs",
                "public interface ForPaying { }\n")));

        graph.Bubbles.Should().NotContain(b => b.Id == "port:ForPaying");
        graph.Find("Application").Size.Files.Should().Be(3);
    }
}

public sealed class with_an_adapter_project_of_no_Via_class
{
    [Fact]
    public async Task draws_one_adapter_named_for_its_port()
    {
        var graph = await Fixture.Drawn(
        [
            new("Adapters/X.ForPaying/X.ForPaying.csproj", "<Project />"),
            new("Adapters/X.ForPaying/Payments.cs", "class Payments { }\n"),
        ]);

        var adapter = graph.Find("adapter:ForPaying");
        adapter.Name.Should().Be("For paying adapter");
        adapter.Role.Should().Be("Real adapter (ForPaying)");
    }
}

public sealed class with_a_project_named_For_but_for_no_port
{
    [Theory]
    [InlineData("X.Format")]
    [InlineData("X.For")]
    public async Task draws_it_as_a_library(string name)
    {
        var graph = await Fixture.Drawn(
        [
            new($"{name}/{name}.csproj", "<Project />"),
            new($"{name}/Code.cs", "class Code { }\n"),
        ]);

        graph.Bubbles.Select(b => b.Id).Should().Equal("library:" + name);
    }

    [Fact]
    public async Task named_for_the_User_port_draws_it_as_a_library()
    {
        var graph = await Fixture.Drawn(
        [
            new("Adapters/X.ForDoing/X.ForDoing.csproj", "<Project />"),
            new("Adapters/X.ForDoing/ForDoingViaWeb.cs", "class A { }\n"),
        ]);

        graph.Bubbles.Select(b => b.Id).Should().Equal("library:X.ForDoing");
    }
}

public sealed class leaves_out
{
    [Theory]
    [InlineData("obj")]
    [InlineData(".git")]
    [InlineData(".vs")]
    [InlineData("node_modules")]
    [InlineData("StrykerOutput")]
    [InlineData("TestResults")]
    [InlineData("_TEMP_DO_NOT_MERGE")]
    public async Task what_is_in_tools_folders(string folder)
    {
        var graph = await Fixture.Drawn(TinyRepo.With(new CodeFile(
            $"Motif.Tiny/Motif.Tiny.Domain/{folder}/Extra.cs", "class Extra { }")));

        graph.Find("Domain").Size.Files.Should().Be(1);
    }

    [Fact]
    public async Task the_web_kits_copy_and_build_output()
    {
        var graph = await Fixture.Drawn(TinyRepo.All);

        graph.Find("WebApp").Size.Files.Should().Be(3);
    }

    [Fact]
    public async Task files_in_no_project()
    {
        var graph = await Fixture.Drawn(TinyRepo.All);

        graph.Bubbles.Sum(b => b.Size.Files).Should().Be(
            TinyRepo.All.Count(f => CodeFile.IsWanted(f.Path)
                && !f.IsProjectFile)
            - 2 // the web kit's copy
            - 1); // docs/Example.cs
    }

    [Fact]
    public async Task files_in_a_folder_whose_name_only_starts_as_a_projects()
    {
        var graph = await Fixture.Drawn(TinyRepo.With(new CodeFile(
            "Motif.Tiny/Motif.Tiny.Domain.Notes/Note.cs", "class Note { }\n")));

        graph.Find("Domain").Size.Files.Should().Be(1);
    }
}

public sealed class with_a_project_named_for_no_part
{
    [Fact]
    public async Task draws_it_as_a_library_beside_the_Application()
    {
        var graph = await Fixture.Drawn(TinyRepo.With(
            new("Motif.Tiny.Text/Motif.Tiny.Text.csproj", "<Project />"),
            new("Motif.Tiny.Text/Words.cs", "class Words { }\n")));

        var library = graph.Find("library:Motif.Tiny.Text");
        library.Name.Should().Be("Motif.Tiny.Text");
        library.Column.Should().Be(Column.Application);
    }

    [Fact]
    public async Task and_no_User_port_has_the_specs_check_the_library()
    {
        var graph = await Fixture.Drawn(
        [
            new("Motif.Words/Motif.Words.csproj", "<Project />"),
            new("Motif.Words/Words.cs", "class Words { }\n"),
            new("Checks/Motif.Words.Specs/Motif.Words.Specs.csproj",
                "<Project />"),
            new("Checks/Motif.Words.Specs/Spelling.cs", "class Spelling { }\n"),
        ]);

        graph.Links.Should().Equal(
            new Link("Specs", "library:Motif.Words", LinkKind.Checks));
    }
}

public sealed class with_fakes_in_a_folder_named_for_no_port
{
    [Fact]
    public async Task puts_them_in_a_last_row_of_their_own()
    {
        var graph = await Fixture.Drawn(TinyRepo.With(
            new CodeFile("Checks/Motif.Tiny.Fakes/Clocks/FixedClock.cs",
                "class FixedClock { }\n")));

        var fake = graph.Find("fake:FixedClock");
        fake.Lane.Should().Be(3);
        fake.Role.Should().Be("Fake (FixedClock)");
        graph.Lanes.Should().Be(4);
    }

    [Fact]
    public async Task or_in_no_folder_puts_them_there_too()
    {
        var graph = await Fixture.Drawn(TinyRepo.With(
            new CodeFile("Checks/Motif.Tiny.Fakes/FixedClock.cs",
                "class FixedClock { }\n")));

        graph.Find("fake:FixedClock").Lane.Should().Be(3);
    }

    [Fact]
    public async Task or_in_one_whose_name_only_looks_like_a_ports()
    {
        var graph = await Fixture.Drawn(TinyRepo.With(
            new CodeFile("Checks/Motif.Tiny.Fakes/OldClocks/FixedClock.cs",
                "class FixedClock { }\n")));

        graph.Find("fake:FixedClock").Role.Should().Be("Fake (FixedClock)");
    }
}

public sealed class with_its_Fakes_project_at_the_root
{
    [Fact]
    public async Task puts_each_fake_in_the_row_of_its_port()
    {
        var graph = await Fixture.Drawn(
        [
            new("X.UseCases/X.UseCases.csproj", "<Project />"),
            new("X.UseCases/Ports/ForPaying.cs", "interface ForPaying { }\n"),
            new("X.Fakes.csproj", "<Project />"),
            new("ForPaying/FakePayments.cs", "class FakePayments { }\n"),
        ]);

        graph.Find("fake:FakePayments").Role.Should().Be(
            "Fake for paying (FakePayments)");
    }
}

public sealed class with_an_adapter_file_named_for_its_port_but_no_way
{
    [Fact]
    public async Task draws_it_as_part_of_the_adapter_not_one_of_its_own()
    {
        var graph = await Fixture.Drawn(
        [
            new("Adapters/X.ForSaving/X.ForSaving.csproj", "<Project />"),
            new("Adapters/X.ForSaving/ForSavingViaFolder.cs",
                "class ForSavingViaFolder { }\n"),
            new("Adapters/X.ForSaving/ForSavingOptions.cs",
                "class ForSavingOptions { }\n"),
        ]);

        graph.Bubbles.Select(b => b.Id).Should().Equal(
            "adapter:ForSavingViaFolder");
    }
}

public sealed class with_fake_code_that_other_fakes_extend
{
    /// <summary>
    /// Motif.Tiny with a FakeStore in a folder named for no port, which the
    /// fake shelf and the fake mail server extend.
    /// </summary>
    private static readonly IReadOnlyList<CodeFile> Shared =
    [
        .. TinyRepo.All.Select(f => f.Name is "FakeShelf.cs" or "FakeMailServer.cs"
            ? f with { Text = f.Text.Replace(" { }", " : FakeStore { }") }
            : f),
        new("Checks/Motif.Tiny.Fakes/Stores/FakeStore.cs",
            "public abstract class FakeStore { }\n"),
    ];

    [Fact]
    public async Task puts_it_in_a_column_of_its_own()
    {
        var graph = await Fixture.Drawn(Shared);

        var shared = graph.Find("fake:FakeStore");
        shared.Column.Should().Be(Column.SharedFakes);
        shared.Kind.Should().Be(BubbleKind.Fake);
        shared.Role.Should().Be("Shared by fakes (FakeStore)");
    }

    [Fact]
    public async Task halfway_down_between_the_fakes_that_extend_it()
    {
        var graph = await Fixture.Drawn(Shared);

        // The fake mail server is in lane 0, the fake shelf in lane 1.
        graph.Find("fake:FakeStore").Lane.Should().Be(0.5);
        graph.Lanes.Should().Be(3);
    }

    [Fact]
    public async Task linked_from_each_fake_that_extends_it()
    {
        var graph = await Fixture.Drawn(Shared);

        graph.Links.Where(l => l.Kind == LinkKind.Extends)
            .Select(l => $"{l.From} > {l.To}")
            .Should().BeEquivalentTo(
                "fake:FakeMailServer > fake:FakeStore",
                "fake:FakeShelf > fake:FakeStore");
    }

    [Fact]
    public async Task and_neither_wired_up_nor_implementing_a_port()
    {
        var graph = await Fixture.Drawn(Shared);

        graph.Links
            .Where(l => l.From == "fake:FakeStore" || l.To == "fake:FakeStore")
            .Should().OnlyContain(l => l.Kind == LinkKind.Extends);
    }
}
