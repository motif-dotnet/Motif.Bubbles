using System.Text.RegularExpressions;
using static Motif.Bubbles.Scanner;

namespace Motif.Bubbles;

/// <summary>
/// A repo's architecture as bubbles and the links between them, placed in
/// fixed columns, with each driven port in a row of its own, sorted by name,
/// so the same repo always looks the same.
/// </summary>
/// <param name="Lanes">How many lanes the graph is, top to bottom.</param>
public sealed record Graph(
    IReadOnlyList<Bubble> Bubbles, IReadOnlyList<Link> Links, int Lanes)
{
    public static Graph None { get; } = new([], [], 0);

    /// <summary>
    /// The repo's files mapped onto bubbles, with rings from the report.
    /// </summary>
    /// <param name="report">
    /// The latest Stryker report, or one with no files for no rings.
    /// </param>
    public static Graph Of(IEnumerable<CodeFile> files, MutationReport report)
    {
        var parts = Parts(files);
        var lanes = Place(parts);
        return new Graph(
            [.. parts.Select(p => p.Placed(lanes.Of[p], Ring(p, report)))],
            LinksBetween(parts),
            lanes.Count);
    }

    public Bubble Find(string id) => Bubbles.Single(b => b.Id == id);

    /// <summary>
    /// The columns that have a bubble in them, left to right.
    /// </summary>
    public IReadOnlyList<Column> Columns =>
        [.. Bubbles.Select(b => b.Column).Distinct().Order()];

    /// <summary>
    /// The bubble and every bubble one link from it, over links of these
    /// kinds.
    /// </summary>
    public IReadOnlySet<string> Neighbours(
        string id, IEnumerable<LinkKind> kinds)
    {
        var shown = kinds.ToHashSet();
        var near = new HashSet<string> { id };
        foreach (var link in Links.Where(l => shown.Contains(l.Kind)))
        {
            if (link.From == id) near.Add(link.To);
            if (link.To == id) near.Add(link.From);
        }
        return near;
    }

    /// <summary>
    /// The bubble's own numbers, and how many links reach it and leave it.
    /// </summary>
    public IReadOnlyList<Detail> Details(Bubble bubble) =>
    [
        .. bubble.Details,
        new("Depended on by", $"{Links.Count(l => l.To == bubble.Id)}"),
        new("Depends on", $"{Links.Count(l => l.From == bubble.Id)}"),
    ];

    private static IReadOnlyList<Link> LinksBetween(IReadOnlyList<Part> parts)
    {
        var ids = parts.Select(p => p.Id).ToHashSet();
        var links = new List<Link>();
        void Add(string from, string to, LinkKind kind)
        {
            if (ids.Contains(from) && ids.Contains(to))
                links.Add(new(from, to, kind));
        }

        string[] userPort = [UserTasks, UserQueries];
        var checkedThrough = userPort.Any(ids.Contains)
            ? userPort
            : [.. parts.Where(p => p.Id.StartsWith("library:")).Select(p => p.Id)];
        foreach (var to in checkedThrough) Add(Specs, to, LinkKind.Checks);
        Add(BrowserSpecs, WebApp, LinkKind.Checks);
        foreach (var to in userPort)
        {
            Add(WebApp, to, LinkKind.Uses);
            Add(Application, to, LinkKind.Implements);
        }
        Add(Application, Domain, LinkKind.Uses);

        var application = parts.SingleOrDefault(p => p.Id == Application);
        foreach (var port in parts.Where(p => p.Column == Column.DrivenPorts))
        {
            if (application is not null && Mentions(application, port.Row))
                Add(Application, port.Id, LinkKind.Uses);
        }
        foreach (var part in parts)
        {
            if (part.Column is Column.RealAdapters or Column.Fakes)
                Add(part.Id, PortId(part.Row), LinkKind.Implements);
            if (part.Column == Column.RealAdapters)
                Add(WebApp, part.Id, LinkKind.Wires);
            if (part.Column == Column.Fakes)
                Add(Specs, part.Id, LinkKind.Wires);
        }
        return links;
    }

    private static bool Mentions(Part part, string word) =>
        part.Files.Any(f =>
            Regex.IsMatch(f.Text, $@"\b{Regex.Escape(word)}\b"));

    /// <summary>
    /// Each driven port's row is as many lanes as it has real adapters or
    /// fakes, whichever is more, and at least one: the port sits in the
    /// middle, the rest one to a lane. The columns to the left spread their
    /// bubbles evenly down the whole height.
    /// </summary>
    private static (Dictionary<Part, double> Of, int Count) Place(
        IReadOnlyList<Part> parts)
    {
        var of = new Dictionary<Part, double>();
        var top = 0;
        var rows = parts
            .Where(p => p.Row != "")
            .GroupBy(p => p.Row)
            .OrderBy(g => g.Key, StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var adapters = row.Where(p => p.Column == Column.RealAdapters).ToList();
            var fakes = row.Where(p => p.Column == Column.Fakes).ToList();
            var height = Math.Max(1, Math.Max(adapters.Count, fakes.Count));
            foreach (var port in row.Where(p => p.Column == Column.DrivenPorts))
                of[port] = top + (height - 1) / 2.0;
            for (var i = 0; i < adapters.Count; i++) of[adapters[i]] = top + i;
            for (var i = 0; i < fakes.Count; i++) of[fakes[i]] = top + i;
            top += height;
        }

        var left = parts
            .Where(p => p.Row == "")
            .GroupBy(p => p.Column)
            .ToDictionary(g => g.Key, g => g.OrderBy(Order).ToList());
        var tallest = left.Count == 0 ? 0 : left.Values.Max(c => c.Count);
        var count = Math.Max(top, tallest);
        foreach (var column in left.Values)
        {
            for (var i = 0; i < column.Count; i++)
                of[column[i]] = (i + 0.5) * count / column.Count - 0.5;
        }
        return (of, count);
    }

    // Down each column on the left: the Web App above the Specs, User Tasks
    // above User Queries, and the Application above any library and the
    // Domain.
    private static int Order(Part part) => part.Id switch
    {
        WebApp or UserTasks or Application => 0,
        Domain => 2,
        _ => 1,
    };

    /// <summary>
    /// The part's mutants, from the report's entries for its files. The ring
    /// is stale when any of its C# files has changed since the report, or is
    /// new since then.
    /// </summary>
    private static Mutation Ring(Part part, MutationReport report)
    {
        var found = report.Files.Where(e => part.Files.Any(e.Is)).ToList();
        var mutation = new Mutation(
            found.Sum(e => e.Caught),
            found.Sum(e => e.Survived),
            found.Sum(e => e.Unreached));
        if (!mutation.IsMutated) return Mutation.None;
        var stale = part.Files.Any(file => IsStale(file, found));
        return mutation with { Stale = stale };
    }

    /// <summary>
    /// Whether the file has changed since the report, or is a C# file new
    /// since then.
    /// </summary>
    private static bool IsStale(CodeFile file, IEnumerable<MutatedFile> found)
    {
        var entry = found.FirstOrDefault(e => e.Is(file));
        return entry is null ? file.Extension == ".cs" : entry.IsStale(file);
    }
}
