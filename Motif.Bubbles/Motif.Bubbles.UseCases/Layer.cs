namespace Motif.Bubbles;

/// <summary>
/// One of the graph's visual layers, each of which can be turned off to see
/// what it adds.
/// </summary>
public enum Layer
{
    Size,
    Rings,
    Checks,
    Uses,
    Implements,
    Extends,
    Wires,
    Bands,
}

/// <summary>
/// A layer, its wording, and whether it's on.
/// </summary>
public sealed record LayerChoice(Layer Layer, string Label, bool On)
{
    /// <summary>
    /// Every layer, in the order they're offered, as they start: all on but
    /// the wiring links, which cross the rest.
    /// </summary>
    public static IReadOnlyList<LayerChoice> Defaults { get; } =
    [
        new(Layer.Size, "Size by lines of code", true),
        new(Layer.Rings, "Mutation rings", true),
        new(Layer.Checks, "Check links", true),
        new(Layer.Uses, "Uses links", true),
        new(Layer.Implements, "Implements links", true),
        new(Layer.Extends, "Extends links", true),
        new(Layer.Wires, "Wiring links", false),
        new(Layer.Bands, "Column bands", true),
    ];

    /// <summary>
    /// The layer that shows links of this kind.
    /// </summary>
    public static Layer For(LinkKind kind) => kind switch
    {
        LinkKind.Checks => Layer.Checks,
        LinkKind.Uses => Layer.Uses,
        LinkKind.Implements => Layer.Implements,
        LinkKind.Extends => Layer.Extends,
        _ => Layer.Wires,
    };
}
