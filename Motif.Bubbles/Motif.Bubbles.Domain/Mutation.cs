namespace Motif.Bubbles;

/// <summary>
/// What Stryker found in a bubble's code: mutants caught by a spec, mutants
/// that survived every spec, and mutants no spec reached.
/// <see cref="None"/> when Stryker didn't mutate it.
/// </summary>
/// <param name="Stale">
/// The code has changed since the report was made, so the ring may be out of
/// date.
/// </param>
public sealed record Mutation(
    int Caught, int Survived, int Unreached, bool Stale = false)
{
    public static Mutation None { get; } = new(0, 0, 0);

    public int Total => Caught + Survived + Unreached;

    public bool IsMutated => Total > 0;

    /// <summary>
    /// The share of mutants caught, as a whole percentage.
    /// </summary>
    public int Score =>
        IsMutated ? (int)Math.Round(100.0 * Caught / Total) : 0;

    public IReadOnlyList<Detail> Details => IsMutated
        ?
        [
            new("Mutation score", $"{Score}%"),
            new("Caught", $"{Caught}"),
            new("Survived", $"{Survived}"),
            new("No spec reaches", $"{Unreached}"),
            .. Stale
                ? [new Detail("Ring", "older than the code")]
                : Array.Empty<Detail>(),
        ]
        : [new("Mutation score", "not mutated")];
}
