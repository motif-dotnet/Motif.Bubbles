namespace Motif.Bubbles;

/// <summary>
/// How one bubble depends on another.
/// </summary>
public enum LinkKind
{
    /// <summary>Specs, or browser specs, check the app through it.</summary>
    Checks,

    Uses,

    Implements,

    /// <summary>A fake builds on code fakes share.</summary>
    Extends,

    /// <summary>
    /// The composition root, or the specs standing in for it, wires it up.
    /// </summary>
    Wires,
}

/// <summary>
/// A link between two bubbles, by their ids.
/// </summary>
public sealed record Link(string From, string To, LinkKind Kind);
