namespace Motif.Bubbles;

/// <summary>
/// The User port's command half: the Reviewer's ways in, and the layers.
/// </summary>
public interface ForDoing
{
    /// <summary>
    /// Shows the folder picker and draws the folder picked: a Motif repo's
    /// root, with its StrykerOutput/ folder if it has one.
    /// </summary>
    Task OpenFolder();

    /// <summary>
    /// Draws the folder just dropped onto the page.
    /// </summary>
    Task OpenDroppedFolder();

    /// <summary>
    /// Draws a public repo. Takes owner/name or a github.com URL.
    /// </summary>
    Task OpenRepo(string repo);

    /// <summary>
    /// Opens a repo link's fragment (#repo=owner/name). False if the fragment
    /// isn't one.
    /// </summary>
    Task<bool> OpenLink(string? fragment);

    void ShowLayer(Layer layer, bool on);

    void DismissProblem();
}
