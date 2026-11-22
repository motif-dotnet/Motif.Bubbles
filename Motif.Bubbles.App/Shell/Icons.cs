using Microsoft.AspNetCore.Components;
using Motif.Suite;
using static Motif.Suite.SuiteIcons;

namespace Motif.Bubbles.App;

/// <summary>
/// The app's few icons, drawn with the suite's <see cref="SuiteIcons.Icon"/>
/// so they match its own.
/// </summary>
public static class Icons
{
    public static readonly MarkupString Folder = Icon(
        """<path d="M3 7a2 2 0 012-2h4l2 2h8a2 2 0 012 2v8a2 2 0 01-2 2H5a2 2 0 01-2-2z"/>""");

    public static readonly MarkupString Repo = Icon(
        """<path d="M5 4a2 2 0 012-2h12v16H7a2 2 0 00-2 2zM5 20a2 2 0 002 2h12v-4"/>"""
        + """<path d="M9 6h6"/>""");

    public static readonly MarkupString Report = Icon(
        """<circle cx="12" cy="12" r="8"/>"""
        + """<path d="M12 4a8 8 0 018 8h-8z"/>""");

    public static readonly MarkupString Back = Icon(
        """<path d="M9 14L4 9l5-5"/>"""
        + """<path d="M4 9h11a5 5 0 010 10h-4"/>""");

    public static readonly MarkupString Fit = Icon(
        """<path d="M4 9V4h5M20 9V4h-5M4 15v5h5M20 15v5h-5"/>""");
}
