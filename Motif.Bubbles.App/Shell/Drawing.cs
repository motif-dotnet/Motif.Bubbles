using System.Globalization;

namespace Motif.Bubbles.App;

/// <summary>
/// Where the graph's bubbles, rings and links go on the page, in the SVG's
/// own units: the view's half of the layout, over the columns and lanes the
/// graph gives each bubble.
/// </summary>
public sealed class Drawing
{
    public const double ColumnWidth = 210;

    // Lanes are at least this tall, and taller when there are few, so the
    // graph is never much wider than twice its height.
    private const double MinLaneHeight = 140;
    private const double MinHeightPerWidth = 0.5;

    // Room for the column headings above the first lane.
    public const double Top = 56;

    // From the bubble's edge to the middle of its ring, which is 5 wide.
    private const double RingGap = 4.5;

    // The smallest holds a two-figure count, such as "×79", inside.
    private const double SmallestRadius = 20;

    // Below a ringed bubble's edge: its name, when that's under it, to the
    // foot of its letters. Above it: its ring's outer edge.
    private const double Below = 26;
    private const double Above = 7;

    // How wide a letter of a name or a count is, at most on average, and
    // how much of a bubble is kept clear each side of what's inside.
    private const double Letter = 7.2;
    private const double Padding = 6;

    // Lines of text inside a bubble, baseline to baseline, and how far a
    // capital rises above its baseline.
    private const double LineHeight = 14;
    private const double CapHeight = 9;

    // Kept clear between a bubble's name and the bubble a lane below, and
    // between the rings of bubbles side by side.
    private const double Clearance = 10;
    private const double SideClearance = 30;

    private readonly Dictionary<Column, int> columns;
    private readonly int fewestLines;
    private readonly int mostLines;
    private readonly Dictionary<string, Bubble> byId;
    private readonly bool sized;
    private readonly bool rings;

    public Drawing(Graph graph, ForSeeing seeing)
    {
        Graph = graph;
        columns = graph.Columns
            .Select((column, i) => (column, i))
            .ToDictionary(x => x.column, x => x.i);
        byId = graph.Bubbles.ToDictionary(b => b.Id);
        var lines = graph.Bubbles.Select(b => b.Size.LinesOfCode)
            .DefaultIfEmpty().ToList();
        (fewestLines, mostLines) = (lines.Min(), lines.Max());
        sized = seeing.Shows(Layer.Size);
        rings = seeing.Shows(Layer.Rings);
        HasRings = graph.Bubbles.Any(b => b.Mutation.IsMutated);
        LaneHeight = Math.Max(MinLaneHeight,
            (Width * MinHeightPerWidth - Top) / Math.Max(1, graph.Lanes));
        BiggestRadius = Math.Max(SmallestRadius, Math.Min(
            (ColumnWidth - SideClearance) / 2, RoomUpAndDown()));
    }

    /// <summary>
    /// The biggest radius that keeps every bubble inside the graph, under
    /// the column headings, and clear of the next bubble down its column,
    /// with the rest graded from it.
    /// </summary>
    private double RoomUpAndDown()
    {
        // A bubble's radius is SmallestRadius + share * (biggest -
        // SmallestRadius), so room for radii adding up to "room" bounds the
        // biggest at SmallestRadius + (room - smallest radii) / shares.
        double Bound(double room, double shares, int bubbles) => shares > 0
            ? SmallestRadius + (room - bubbles * SmallestRadius) / shares
            : double.PositiveInfinity;

        var bounds = new List<double>();
        foreach (var bubble in Graph.Bubbles)
        {
            var y = Y(bubble);
            bounds.Add(Bound(y - Top - Above, Share(bubble), 1));
            bounds.Add(Bound(Height - y - Below, Share(bubble), 1));
        }
        foreach (var column in Graph.Bubbles.GroupBy(b => b.Column))
        {
            var down = column.OrderBy(b => b.Lane).ToList();
            foreach (var (upper, lower) in down.Zip(down.Skip(1)))
            {
                bounds.Add(Bound(
                    Y(lower) - Y(upper) - Below - Above - Clearance,
                    Share(upper) + Share(lower), 2));
            }
        }
        return bounds.DefaultIfEmpty(double.PositiveInfinity).Min();
    }

    /// <summary>
    /// How far the bubble's lines of code are from the graph's fewest (0)
    /// to its most (1), by their square roots.
    /// </summary>
    private double Share(Bubble bubble)
    {
        var range = Math.Sqrt(mostLines) - Math.Sqrt(fewestLines);
        var above = Math.Sqrt(bubble.Size.LinesOfCode) - Math.Sqrt(fewestLines);
        return range > 0 ? above / range : 0;
    }

    public double LaneHeight { get; }

    /// <summary>
    /// How big the bubble with the most lines of code is: as big as the
    /// columns and the bubbles above and below leave room for.
    /// </summary>
    public double BiggestRadius { get; }

    public Graph Graph { get; }

    /// <summary>
    /// Whether any bubble has a ring, so a bubble with none stands out as
    /// not mutated.
    /// </summary>
    public bool HasRings { get; }

    public double Width => Math.Max(1, columns.Count) * ColumnWidth;

    public double Height => Top + Math.Max(1, Graph.Lanes) * LaneHeight;

    public double X(Column column) => (columns[column] + 0.5) * ColumnWidth;

    public double X(Bubble bubble) => X(bubble.Column);

    public double Y(Bubble bubble) => Top + (bubble.Lane + 0.5) * LaneHeight;

    /// <summary>
    /// Graded by lines of code: the graph's smallest bubble is just big
    /// enough to hold its count, its biggest is <see cref="BiggestRadius"/>,
    /// and the rest fall between, so area still grows with lines of code.
    /// </summary>
    public double Radius(Bubble bubble)
    {
        if (!sized) return 26;
        return SmallestRadius
            + Share(bubble) * (BiggestRadius - SmallestRadius);
    }

    public bool IsRinged(Bubble bubble) => rings && bubble.Mutation.IsMutated;

    /// <summary>
    /// Real code Stryker could have mutated but didn't, hatched.
    /// </summary>
    public bool IsUnmutated(Bubble bubble) =>
        HasRings && bubble.Kind == BubbleKind.Real && !bubble.Mutation.IsMutated;

    /// <summary>
    /// What's written inside the bubble, top to bottom, centred on its
    /// middle: its name, broken into lines at its spaces wherever that
    /// makes it fit, then its count if it has one.
    /// </summary>
    public IReadOnlyList<Label> Inside(Bubble bubble)
    {
        var count = bubble.Count == "" ? [] : new[] { bubble.Count };
        foreach (var name in Ways(bubble.Name))
        {
            string[] lines = [.. name, .. count];
            if (Fit(lines, Radius(bubble)))
                return Labels(bubble, lines, name.Count);
        }
        return Labels(bubble, count, 0);
    }

    /// <summary>
    /// Whether the bubble's name is inside it, else under it.
    /// </summary>
    public bool NameInside(Bubble bubble) =>
        Inside(bubble).Any(l => l.IsName);

    /// <summary>
    /// The baseline of the bubble's name when it's under the bubble and its
    /// ring.
    /// </summary>
    public double NameY(Bubble bubble) =>
        Y(bubble) + Radius(bubble) + (IsRinged(bubble) ? 22 : 17);

    // The ways to write a name inside a bubble, broken into lines at its
    // spaces, to try in turn: the fewest lines first, so words stay
    // together where they can, then the narrowest.
    private static IEnumerable<IReadOnlyList<string>> Ways(string name) =>
        Breaks(name.Split(' '))
            .OrderBy(lines => lines.Count)
            .ThenBy(lines => lines.Max(line => line.Length));

    // Every way to break the words into lines, keeping their order.
    private static IEnumerable<IReadOnlyList<string>> Breaks(string[] words)
    {
        yield return [string.Join(' ', words)];
        for (var i = 1; i < words.Length; i++)
        {
            var first = string.Join(' ', words[..i]);
            foreach (var rest in Breaks(words[i..]))
                yield return [first, .. rest];
        }
    }

    // Whether every line fits across the bubble where its top or its
    // baseline is furthest from the middle.
    private static bool Fit(IReadOnlyList<string> lines, double r) =>
        lines.Select((line, i) => (line, i)).All(x =>
        {
            var baseline = Baseline(x.i, lines.Count);
            var far = Math.Max(
                Math.Abs(baseline), Math.Abs(baseline - CapHeight));
            return r > far && x.line.Length * Letter + 2 * Padding
                <= 2 * Math.Sqrt(r * r - far * far);
        });

    // A line's baseline below the bubble's middle, with the lines centred
    // on it.
    private static double Baseline(int line, int lines) =>
        (line - lines / 2.0) * LineHeight + (LineHeight + CapHeight) / 2;

    private IReadOnlyList<Label> Labels(
        Bubble bubble, IReadOnlyList<string> lines, int nameLines) =>
        [.. lines.Select((line, i) => new Label(
            line, Y(bubble) + Baseline(i, lines.Count), i < nameLines))];

    /// <summary>
    /// The ring's arcs, clockwise from the top: caught, survived, then
    /// unreached, each with a small gap.
    /// </summary>
    public IReadOnlyList<Arc> Ring(Bubble bubble)
    {
        var m = bubble.Mutation;
        var (cx, cy, r) = (X(bubble), Y(bubble), Radius(bubble) + RingGap);
        var arcs = new List<Arc>();
        var start = 0.0;
        const double gap = 0.04;
        foreach (var (count, part) in new[]
                 {
                     (m.Caught, "caught"), (m.Survived, "survived"),
                     (m.Unreached, "unreached"),
                 })
        {
            if (count == 0) continue;
            var span = 2 * Math.PI * count / m.Total;
            if (span >= 2 * Math.PI - 1e-6)
            {
                arcs.Add(new Arc(Circle(cx, cy, r), part));
                break;
            }
            var trim = span > 2 * gap ? gap / 2 : 0;
            arcs.Add(new Arc(
                ArcPath(cx, cy, r, start + trim, start + span - trim), part));
            start += span;
        }
        return arcs;
    }

    /// <summary>
    /// A link's line, from the edge of one bubble to just short of the
    /// other's, so its arrow shows.
    /// </summary>
    public (double X1, double Y1, double X2, double Y2) Line(Link link)
    {
        var (a, b) = (byId[link.From], byId[link.To]);
        var (ax, ay, bx, by) = (X(a), Y(a), X(b), Y(b));
        var (dx, dy) = (bx - ax, by - ay);
        var d = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
        var from = Radius(a) + (IsRinged(a) ? 6 : 2);
        var to = Radius(b) + (IsRinged(b) ? 8 : 4);
        return (ax + dx * from / d, ay + dy * from / d,
            bx - dx * to / d, by - dy * to / d);
    }

    /// <summary>
    /// A number as SVG wants it, whatever the culture.
    /// </summary>
    public static string N(double value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);

    private static string ArcPath(
        double cx, double cy, double r, double from, double to)
    {
        var (x0, y0) = (cx + r * Math.Sin(from), cy - r * Math.Cos(from));
        var (x1, y1) = (cx + r * Math.Sin(to), cy - r * Math.Cos(to));
        var large = to - from > Math.PI ? 1 : 0;
        return $"M{N(x0)} {N(y0)} A{N(r)} {N(r)} 0 {large} 1 {N(x1)} {N(y1)}";
    }

    // A whole ring, as two half arcs: one arc can't start and end at the
    // same point.
    private static string Circle(double cx, double cy, double r) =>
        $"M{N(cx)} {N(cy - r)} A{N(r)} {N(r)} 0 1 1 {N(cx)} {N(cy + r)} "
        + $"A{N(r)} {N(r)} 0 1 1 {N(cx)} {N(cy - r)}";
}

/// <summary>
/// One arc of a mutation ring, as an SVG path, and which of the mutants it
/// stands for: "caught", "survived" or "unreached".
/// </summary>
public sealed record Arc(string Path, string Part);

/// <summary>
/// One line written inside a bubble, at its baseline: part of its name, or
/// its count.
/// </summary>
public sealed record Label(string Text, double Y, bool IsName);
