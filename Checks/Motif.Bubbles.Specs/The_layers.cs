namespace Motif.Bubbles.Specs.The_layers;

public sealed class start
{
    [Fact]
    public void all_on_but_the_wiring_links()
    {
        var f = new Fixture();

        f.Queries.Layers.Where(l => !l.On).Select(l => l.Layer)
            .Should().Equal(Layer.Wires);
        f.Queries.Layers.Should().HaveCount(Enum.GetValues<Layer>().Length);
    }

    [Fact]
    public async Task showing_every_link_but_the_wiring()
    {
        var f = await Fixture.Picked(TinyRepo.All);

        f.Queries.ShownLinks.Should().NotBeEmpty();
        f.Queries.ShownLinks.Should().OnlyContain(l => l.Kind != LinkKind.Wires);
    }
}

public sealed class are_offered
{
    [Fact]
    public void in_order_each_in_words()
    {
        var f = new Fixture();

        f.Queries.Layers.Select(l => l.Label).Should().Equal(
            "Size by lines of code", "Mutation rings", "Check links",
            "Uses links", "Implements links", "Extends links",
            "Wiring links", "Column bands");
    }
}

public sealed class turned_on
{
    [Fact]
    public async Task show_their_links()
    {
        var f = await Fixture.Picked(TinyRepo.All);

        f.Tasks.ShowLayer(Layer.Wires, true);

        f.Queries.Shows(Layer.Wires).Should().BeTrue();
        f.Queries.ShownLinks.Should().Contain(
            new Link("WebApp", "adapter:ForSendingMailViaSmtp", LinkKind.Wires));
    }

    [Fact]
    public async Task pick_out_neighbours_over_their_links()
    {
        var f = await Fixture.Picked(TinyRepo.All);

        f.Tasks.ShowLayer(Layer.Wires, true);

        f.Queries.NeighboursOf("fake:FakeShelf").Should().BeEquivalentTo(
            "fake:FakeShelf", "port:ForStoringOrders", "Specs");
    }
}

public sealed class turned_off
{
    [Fact]
    public async Task hide_their_links()
    {
        var f = await Fixture.Picked(TinyRepo.All);

        f.Tasks.ShowLayer(Layer.Checks, false);

        f.Queries.Shows(Layer.Checks).Should().BeFalse();
        f.Queries.ShownLinks.Should().NotContain(l => l.Kind == LinkKind.Checks);
    }

    [Fact]
    public async Task no_longer_pick_out_neighbours_over_them()
    {
        var f = await Fixture.Picked(TinyRepo.All);

        f.Tasks.ShowLayer(Layer.Implements, false);

        f.Queries.NeighboursOf("fake:FakeShelf").Should().Equal(
            "fake:FakeShelf");
    }

    [Fact]
    public async Task leave_the_other_layers_as_they_were()
    {
        var f = await Fixture.Picked(TinyRepo.All);

        f.Tasks.ShowLayer(Layer.Rings, false);

        f.Queries.Layers.Where(l => !l.On).Select(l => l.Layer)
            .Should().BeEquivalentTo([Layer.Rings, Layer.Wires]);
    }
}
