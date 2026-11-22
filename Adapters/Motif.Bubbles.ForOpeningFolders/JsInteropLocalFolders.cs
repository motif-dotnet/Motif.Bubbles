using Microsoft.JSInterop;

namespace Motif.Bubbles.Adapters;

/// <summary>
/// The browser's Local Folders Fitting: its folder picker, and drops onto
/// the page (wwwroot/folders.js). The module catches drops itself, before
/// the page's own handler runs, so call <see cref="Listen"/> once at startup.
/// </summary>
public sealed class JsInteropLocalFolders(IJSRuntime js)
    : LocalFolders, IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> module = new(() => js
        .InvokeAsync<IJSObjectReference>(
            "import", "./_content/Motif.Bubbles.ForOpeningFolders/folders.js")
        .AsTask());

    /// <summary>
    /// Starts listening for drops onto the page.
    /// </summary>
    public async Task Listen() =>
        await (await module.Value).InvokeVoidAsync("listen");

    public async Task<Listing> Pick() =>
        await (await module.Value).InvokeAsync<Listing>("pick");

    public async Task<Listing> LatestDrop() =>
        await (await module.Value).InvokeAsync<Listing>("latestDrop");

    public async Task<string> Read(int listing, string path) =>
        await (await module.Value).InvokeAsync<string>("read", listing, path);

    public async ValueTask DisposeAsync()
    {
        if (module.IsValueCreated) await (await module.Value).DisposeAsync();
    }
}
