using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Motif.Bubbles;

/// <summary>
/// When Stryker ran, from the name of the folder it wrote the report in
/// (StrykerOutput/2026-11-21.19-17-41).
/// </summary>
public static class StrykerRun
{
    private const string Format = "yyyy-MM-dd.HH-mm-ss";

    public static bool TryRead(string folder, [NotNullWhen(true)] out string? when)
    {
        when = null;
        if (!DateTime.TryParseExact(folder, Format, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var run))
        {
            return false;
        }
        when = run.ToString("d MMM, HH:mm", CultureInfo.InvariantCulture);
        return true;
    }
}
