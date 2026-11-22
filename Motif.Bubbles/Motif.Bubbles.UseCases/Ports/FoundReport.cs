namespace Motif.Bubbles;

/// <summary>
/// A Stryker report found alongside the code, as JSON.
/// <see cref="None"/> when there isn't one.
/// </summary>
/// <param name="Run">
/// The folder Stryker wrote it in (2026-11-21.19-17-41), or "" when that
/// isn't known, as for a report published to Pages.
/// </param>
public sealed record FoundReport(string Json, string Run)
{
    public static FoundReport None { get; } = new("", "");
}
