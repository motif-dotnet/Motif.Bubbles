using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Motif.Bubbles;

/// <summary>
/// One file in a Stryker report: its path as Stryker wrote it, the copy of
/// its text that was mutated, and what became of its mutants.
/// </summary>
public sealed record MutatedFile(
    string Path, string Source, int Caught, int Survived, int Unreached)
{
    /// <summary>
    /// Whether this is the report's entry for a file of the repo. Stryker
    /// writes full paths from wherever it ran, so the repo's path need only
    /// end it.
    /// </summary>
    public bool Is(CodeFile file)
    {
        var path = Path.Replace('\\', '/');
        return path == file.Path || path.EndsWith("/" + file.Path);
    }

    /// <summary>
    /// Whether the file has changed since Stryker ran, ignoring line endings.
    /// </summary>
    public bool IsStale(CodeFile file) =>
        Source.ReplaceLineEndings("\n") != file.Text.ReplaceLineEndings("\n");
}

/// <summary>
/// Stryker's JSON report (the json reporter's mutation-report.json), read
/// for what the rings need: each file's mutants and its source copy.
/// </summary>
public sealed record MutationReport(IReadOnlyList<MutatedFile> Files)
{
    public const string FileName = "mutation-report.json";

    /// <summary>
    /// Killed and Timeout are caught; NoCoverage is a mutant no spec reached.
    /// Mutants Stryker didn't run (Ignored, CompileError and the like) aren't
    /// counted.
    /// </summary>
    public static bool TryRead(
        string json,
        [NotNullWhen(true)] out MutationReport? report,
        out string why)
    {
        report = null;
        why = "";
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("files", out var files)
                || files.ValueKind != JsonValueKind.Object)
            {
                why = "It has no files: is it a Stryker mutation report?";
                return false;
            }
            report = new MutationReport(
                files.EnumerateObject().Select(Read).ToList());
            return true;
        }
        catch (JsonException e)
        {
            why = $"It isn't valid JSON ({e.Message}).";
            return false;
        }
    }

    private static MutatedFile Read(JsonProperty file)
    {
        var statuses = file.Value.TryGetProperty("mutants", out var mutants)
            ? mutants.EnumerateArray()
                .Select(m => m.TryGetProperty("status", out var s)
                    ? s.GetString()
                    : null)
                .ToList()
            : [];
        var source = file.Value.TryGetProperty("source", out var text)
            ? text.GetString() ?? ""
            : "";
        return new MutatedFile(
            file.Name,
            source,
            Caught: statuses.Count(s => s is "Killed" or "Timeout"),
            Survived: statuses.Count(s => s is "Survived"),
            Unreached: statuses.Count(s => s is "NoCoverage"));
    }
}
