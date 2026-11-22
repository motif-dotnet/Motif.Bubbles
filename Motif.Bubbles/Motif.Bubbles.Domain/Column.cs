namespace Motif.Bubbles;

/// <summary>
/// The graph's columns, left to right, following ports and adapters.
/// </summary>
public enum Column
{
    BrowserChecks,
    Driving,
    UserPort,
    Application,
    DrivenPorts,
    RealAdapters,
    Fakes,
}

public static class Columns
{
    public static IReadOnlyList<Column> All { get; } = Enum.GetValues<Column>();

    public static string Heading(this Column column) => column switch
    {
        Column.BrowserChecks => "Browser checks",
        Column.Driving => "Driving",
        Column.UserPort => "User port",
        Column.Application => "Application",
        Column.DrivenPorts => "Driven ports",
        Column.RealAdapters => "Real adapters",
        _ => "Fakes",
    };
}
