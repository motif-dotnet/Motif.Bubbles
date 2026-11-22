namespace Motif.Bubbles;

/// <summary>
/// Something worth knowing about the graph on screen, for as long as it's
/// shown, such as why it has no rings. <see cref="None"/> when there's
/// nothing to say.
/// </summary>
public sealed record Note(string Title, string Text)
{
    public static Note None { get; } = new("", "");
}
