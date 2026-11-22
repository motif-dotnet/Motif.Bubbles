namespace Motif.Bubbles;

/// <summary>
/// The User port's query half: what the Reviewer sees.
/// </summary>
public interface ForSeeing
{
    /// <summary>
    /// The graph of the code that's open. <see cref="Graph.None"/> before
    /// anything is.
    /// </summary>
    Graph Graph { get; }

    /// <summary>
    /// Where the code came from. <see cref="Source.None"/> before anything is
    /// open.
    /// </summary>
    Source Source { get; }

    /// <summary>
    /// Why the graph has no rings, or why some may be out of date.
    /// <see cref="Note.None"/> when the rings are all there and current.
    /// </summary>
    Note RingsNote { get; }

    /// <summary>
    /// Every layer, with whether it's on.
    /// </summary>
    IReadOnlyList<LayerChoice> Layers { get; }

    bool Shows(Layer layer);

    /// <summary>
    /// The links whose layer is on.
    /// </summary>
    IReadOnlyList<Link> ShownLinks { get; }

    /// <summary>
    /// The bubble and those one shown link from it, as hovering it picks out.
    /// </summary>
    IReadOnlySet<string> NeighboursOf(string bubbleId);

    /// <summary>
    /// Why the last thing the Reviewer tried didn't open.
    /// <see cref="Problem.None"/> when all is well.
    /// </summary>
    Problem Problem { get; }

    /// <summary>
    /// True while a repo is being read.
    /// </summary>
    bool Loading { get; }

    /// <summary>
    /// The repo on screen, as owner/name, or "" when it isn't a repo.
    /// </summary>
    string Repo { get; }

    /// <summary>
    /// The repo link's fragment (repo=owner/name) when a repo is on screen,
    /// else "".
    /// </summary>
    string RepoLinkFragment { get; }
}
