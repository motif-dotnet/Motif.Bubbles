namespace Motif.Bubbles.Adapters;

/// <summary>
/// The files of a folder picked or dropped, by their paths from the folder,
/// with "/" between folders. Numbered from 1 in the order they came, picks
/// and drops alike.
/// </summary>
/// <param name="Name">The folder's name, or "" for files dropped loose.</param>
public sealed record Listing(int Number, string Name, IReadOnlyList<string> Paths)
{
    public static Listing None { get; } = new(0, "", []);
}
