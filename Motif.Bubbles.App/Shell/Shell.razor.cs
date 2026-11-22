using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Motif.Suite;

namespace Motif.Bubbles.App;

/// <summary>
/// The Web App's one page: the ways in, then the graph. All the logic is
/// behind the User port.
/// </summary>
public partial class Shell : IDisposable
{
    private enum Menu { None, Theme }

    private enum Dialog { None, Repo }

    private bool started;
    private Menu menu;
    private Dialog dialog;
    private string theme = "system";
    private string repoText = "";
    private int dragDepth;
    private bool copied;
    private bool helpOpen;
    private DotNetObjectReference<Shell>? self;

    [Inject] private ForDoing Tasks { get; set; } = null!;
    [Inject] private ForSeeing Queries { get; set; } = null!;
    [Inject] private Browser Browser { get; set; } = null!;
    [Inject] private Toaster Toaster { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private bool Drawn => Queries.Graph != Graph.None;

    private string PageTitle => Drawn
        ? $"{Queries.Source.Name} · Motif Bubbles"
        : "Motif Bubbles";

    private string RepoLinkUrl => Queries.RepoLinkFragment is var fragment
        && fragment != ""
            ? BaseUrl() + "#" + fragment
            : "";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender) await Start();
    }

    private async Task Start()
    {
        await Browser.Load();
        theme = Browser.Theme();
        Toaster.Changed += Refresh;
        self = DotNetObjectReference.Create(this);
        Browser.ListenForFragment(self);
        started = true;
        StateHasChanged();
        await OpenFragment(Browser.Fragment());
    }

    [JSInvokable]
    public async Task OnFragment(string fragment) => await OpenFragment(fragment);

    private async Task OpenFragment(string fragment)
    {
        if (!RepoLink.TryRead(fragment, out _)) return;
        var opening = Tasks.OpenLink(fragment);
        StateHasChanged(); // show the progress bar while GitHub is read
        await opening;
        Opened();
    }

    private void Opened()
    {
        copied = false;
        // A repo's graph carries its repo link in the address bar; anything
        // else clears a stale one.
        if (Queries.RepoLinkFragment != "")
        {
            Browser.SetFragment(Queries.RepoLinkFragment);
        }
        else if (Queries.Problem == Problem.None
                 && RepoLink.TryRead(Browser.Fragment(), out _))
        {
            Browser.SetFragment("");
        }
        // What went wrong is the outcome of a command, so it's a toast, as in
        // every Motif tool.
        if (Queries.Problem != Problem.None)
        {
            Toaster.Problem(Queries.Problem.Title, [Queries.Problem.Text]);
            Tasks.DismissProblem();
        }
        StateHasChanged();
    }

    private async Task OpenFolder()
    {
        await Tasks.OpenFolder();
        Opened();
    }

    private void ShowRepo()
    {
        dialog = Dialog.Repo;
        _ = FocusSoon("#repo-box");
    }

    private async Task FocusSoon(string selector)
    {
        await Task.Yield();
        StateHasChanged();
        await Task.Delay(1);
        Browser.Focus(selector);
    }

    private void CloseDialog() => dialog = Dialog.None;

    private async Task SubmitRepo()
    {
        if (string.IsNullOrWhiteSpace(repoText)) return;
        dialog = Dialog.None;
        var opening = Tasks.OpenRepo(repoText);
        StateHasChanged();
        await opening;
        if (Queries.Problem == Problem.None) repoText = "";
        Opened();
    }

    private void StartAgain()
    {
        // Back to the ways in. The layers start as they were, so a fresh
        // application is simply a reload.
        Browser.SetFragment("");
        Navigation.NavigateTo(Navigation.BaseUri, forceLoad: true);
    }

    private async Task CopyRepoLink()
    {
        if (RepoLinkUrl != "") copied = await Browser.Copy(RepoLinkUrl);
    }

    private void DragEnter() => dragDepth++;

    private void DragLeave() => dragDepth = Math.Max(0, dragDepth - 1);

    private async Task Drop()
    {
        dragDepth = 0;
        await Tasks.OpenDroppedFolder();
        Opened();
    }

    private void Toggle(Menu name)
    {
        menu = menu == name ? Menu.None : name;
        helpOpen = false;
    }

    private void ToggleHelp()
    {
        helpOpen = !helpOpen;
        menu = Menu.None;
    }

    private void Refresh() => InvokeAsync(StateHasChanged);

    private void CloseMenus() => menu = Menu.None;

    private void SetTheme(string value)
    {
        theme = value;
        Browser.SetTheme(value);
        menu = Menu.None;
    }

    private string BaseUrl()
    {
        var url = Browser.PageUrl();
        var hash = url.IndexOf('#');
        return hash < 0 ? url : url[..hash];
    }

    private static MarkupString SourceIcon(SourceKind kind) =>
        kind == SourceKind.Repo ? Icons.Repo : Icons.Folder;

    public void Dispose()
    {
        Toaster.Changed -= Refresh;
        self?.Dispose();
    }
}
