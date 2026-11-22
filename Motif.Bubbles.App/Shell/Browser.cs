using Microsoft.JSInterop;

namespace Motif.Bubbles.App;

/// <summary>
/// The Web App's own browser plumbing (wwwroot/js/app.js). Synchronous where
/// WebAssembly allows it.
/// </summary>
public sealed class Browser(IJSRuntime js)
{
    private IJSInProcessObjectReference? module;

    public async Task Load() =>
        module ??= await js.InvokeAsync<IJSInProcessObjectReference>(
            "import", "./js/app.js");

    private IJSInProcessObjectReference Module =>
        module ?? throw new InvalidOperationException("app.js isn't loaded yet.");

    public string Fragment() => Module.Invoke<string>("fragment");

    public string PageUrl() => Module.Invoke<string>("pageUrl");

    /// <summary>
    /// Sets the address bar's fragment, or clears it with "".
    /// </summary>
    public void SetFragment(string fragment) =>
        Module.InvokeVoid("setFragment", fragment);

    public void ListenForFragment<T>(DotNetObjectReference<T> target)
        where T : class =>
        Module.InvokeVoid("listenForFragment", target);

    public async Task<bool> Copy(string text) =>
        await Module.InvokeAsync<bool>("copy", text);

    public void Focus(string selector) => Module.InvokeVoid("focus", selector);

    public string Theme() => Module.Invoke<string>("theme");

    public void SetTheme(string theme) => Module.InvokeVoid("setTheme", theme);

    /// <summary>
    /// Lets the graph be panned by dragging and zoomed with the wheel, and
    /// keeps its tooltip by the pointer.
    /// </summary>
    public void PanAndZoom(string selector) =>
        Module.InvokeVoid("panAndZoom", selector);

    /// <summary>
    /// Shows the whole graph again, as it was before any pan or zoom.
    /// </summary>
    public void Fit(string selector) => Module.InvokeVoid("fit", selector);
}
