namespace Motif.Bubbles;

/// <summary>
/// The kind of object a bubble is, which its outline shows.
/// </summary>
public enum BubbleKind
{
    /// <summary>Real code: the Domain, the Application, an adapter.</summary>
    Real,

    /// <summary>A port: an interface, with nothing to mutate.</summary>
    Port,

    /// <summary>A fake, for specs.</summary>
    Fake,

    /// <summary>Specs or browser specs, which check the rest.</summary>
    Specs,
}

/// <summary>
/// How big a bubble's code is. Lines of code don't count blank lines or
/// comments.
/// </summary>
public sealed record Size(int LinesOfCode, int Files, int Types);

/// <summary>
/// One label and value of what hovering a bubble shows.
/// </summary>
public sealed record Detail(string Label, string Value);

/// <summary>
/// One architectural object of the repo: the Domain, User Tasks, User
/// Queries, the Application, a driven port, a real adapter, a fake, or the
/// specs.
/// </summary>
/// <param name="Id">Stable across loads, such as "port:ForOpeningRepos".</param>
/// <param name="Role">What it is, in a phrase, with its type's name.</param>
/// <param name="Lane">
/// Its place down the graph, in lanes from the top. A driven port's row is
/// one lane per adapter or fake, so this can fall between whole lanes.
/// </param>
/// <param name="Specs">How many specs it holds; 0 unless it's specs.</param>
/// <param name="Members">
/// How many members its interface has; 0 unless it's a port.
/// </param>
/// <param name="MemberWord">What its members are: "tasks", "queries".</param>
public sealed record Bubble(
    string Id,
    string Name,
    string Role,
    BubbleKind Kind,
    Column Column,
    double Lane,
    Size Size,
    int Specs,
    int Members,
    string MemberWord,
    Mutation Mutation)
{
    /// <summary>
    /// How many specs or members it has, as shown inside it, such as "×79":
    /// the one number the rest of the graph doesn't show, as its size shows
    /// its lines of code and its ring its mutants. Empty when it has none.
    /// </summary>
    public string Count =>
        Specs > 0 ? $"×{Specs}"
        : Members > 0 ? $"×{Members}"
        : "";

    /// <summary>
    /// Every number it has, as hovering shows them.
    /// </summary>
    public IReadOnlyList<Detail> Details
    {
        get
        {
            List<Detail> rows = [new("Lines of code", $"{Size.LinesOfCode}")];
            if (Specs > 0) rows.Add(new("Specs", $"{Specs}"));
            if (Members > 0)
                rows.Add(new(Words.Capitalised(MemberWord), $"{Members}"));
            rows.AddRange(Mutation.Details);
            return rows;
        }
    }
}
