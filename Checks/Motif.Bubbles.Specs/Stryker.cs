using System.Text.Json;

namespace Motif.Bubbles.Specs;

/// <summary>
/// A Stryker JSON report, made up for a spec: each file's mutants by status,
/// and the copy of the file that was mutated. Paths are written in full, as
/// Stryker writes them, from a Windows checkout.
/// </summary>
public sealed class Stryker
{
    public const string Root = @"C:\work\Motif.Tiny";

    private readonly Dictionary<string, object> files = [];

    /// <param name="source">
    /// The text that was mutated, if not the file's own.
    /// </param>
    public Stryker Mutated(
        CodeFile file,
        int killed = 0,
        int survived = 0,
        int noCoverage = 0,
        int timeout = 0,
        int ignored = 0,
        int compileError = 0,
        string? source = null)
    {
        var statuses = Repeat("Killed", killed)
            .Concat(Repeat("Survived", survived))
            .Concat(Repeat("NoCoverage", noCoverage))
            .Concat(Repeat("Timeout", timeout))
            .Concat(Repeat("Ignored", ignored))
            .Concat(Repeat("CompileError", compileError));
        files[Root + @"\" + file.Path.Replace('/', '\\')] = new
        {
            language = "cs",
            source = source ?? file.Text,
            mutants = statuses.Select((status, i) => new
            {
                id = $"{i}",
                mutatorName = "Statement mutation",
                status,
            }),
        };
        return this;
    }

    public string Json => JsonSerializer.Serialize(new
    {
        schemaVersion = "2",
        thresholds = new { high = 80, low = 60 },
        projectRoot = Root,
        files,
    });

    /// <summary>
    /// The report as a file in the folder Stryker writes it in, for the run
    /// at this time (2026-11-21.19-17-41).
    /// </summary>
    public CodeFile In(string run) =>
        new($"StrykerOutput/{run}/reports/mutation-report.json", Json);

    private static IEnumerable<string> Repeat(string status, int count) =>
        Enumerable.Repeat(status, count);
}
