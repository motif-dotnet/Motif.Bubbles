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

public sealed class leaves_out
{
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
}
