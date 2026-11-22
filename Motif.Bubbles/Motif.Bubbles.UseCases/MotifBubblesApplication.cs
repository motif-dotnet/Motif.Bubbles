namespace Motif.Bubbles;

/// <summary>
/// The application: reads a repo's code from a folder or GitHub, with its
/// latest Stryker report, and keeps its graph and the layers shown.
/// </summary>
public sealed class MotifBubblesApplication(
    ForOpeningRepos repos, ForOpeningFolders folders) : ForDoing, ForSeeing
{
    // Counts what the Reviewer has tried, so a repo read that something newer
    // has overtaken is dropped when it comes back.
    private int attempt;

    private IReadOnlyList<LayerChoice> layers = LayerChoice.Defaults;

    public Graph Graph { get; private set; } = Graph.None;
    public Source Source { get; private set; } = Source.None;
    public Note RingsNote { get; private set; } = Note.None;
    public Problem Problem { get; private set; } = Problem.None;
    public bool Loading { get; private set; }
    public IReadOnlyList<LayerChoice> Layers => layers;

    public string Repo => Source.Kind == SourceKind.Repo ? Source.Name : "";

    public string RepoLinkFragment =>
        RepoName.TryParse(Repo, out var repo) ? RepoLink.Fragment(repo) : "";

    public bool Shows(Layer layer) => layers.Single(l => l.Layer == layer).On;

    public IReadOnlyList<Link> ShownLinks =>
        [.. Graph.Links.Where(l => Shows(LayerChoice.For(l.Kind)))];

    public IReadOnlySet<string> NeighboursOf(string bubbleId) =>
        Graph.Neighbours(bubbleId, Enum.GetValues<LinkKind>()
            .Where(k => Shows(LayerChoice.For(k))));

    public async Task OpenFolder() => OpenLocal(await folders.Pick());

    public async Task OpenDroppedFolder() =>
        OpenLocal(await folders.TakeDropped());

    private void OpenLocal(OpenedFolder folder)
    {
        if (folder == OpenedFolder.None) return;
        Overtake();
        var source = folder.Name == ""
            ? new Source(SourceKind.Files, "Files dropped", "")
            : new Source(SourceKind.Folder, folder.Name, "");
        Show(source, folder.Files,
            folder.Report, new Note("No mutation rings",
                "There's no Stryker JSON report in this folder's "
                + "StrykerOutput/. Run dotnet stryker with the json reporter "
                + "to see how well the specs catch mistakes."));
    }

    public async Task OpenRepo(string repo)
    {
        if (!RepoName.TryParse(repo, out var name))
        {
            Overtake();
            Problem = Undrawable("repo",
                $"“{repo.Trim()}” isn't a GitHub repo. Give it as owner/name, "
                + "such as motif-dotnet/Motif.Glossary.");
            return;
        }
        await Open(name);
    }

    public async Task<bool> OpenLink(string? fragment)
    {
        if (!RepoLink.TryRead(fragment, out var repo)) return false;
        await Open(repo);
        return true;
    }

    private async Task Open(RepoName repo)
    {
        var mine = Overtake();
        Loading = true;
        Problem = Problem.None;
        try
        {
            var code = await repos.Read(repo);
            if (mine != attempt) return;
            if (code.Outcome == RepoRead.NotFound)
            {
                Problem = Undrawable("repo",
                    $"{repo} isn't a public GitHub repo, or GitHub can't "
                    + "find it.");
                return;
            }
            if (code.Outcome == RepoRead.RateLimited)
            {
                Problem = Undrawable("repo",
                    "GitHub lets this network list only 60 repos an hour, "
                    + "and that's been reached. Try again later, or open the "
                    + "repo's folder instead.");
                return;
            }
            Show(new Source(SourceKind.Repo, repo.ToString(), ""), code.Files,
                code.Report, new Note("No mutation rings",
                    $"{repo} has no Stryker report on its Pages site "
                    + "(motif/mutation-report.json). Open its folder instead "
                    + "to see the rings from its own StrykerOutput/."));
        }
        catch (HttpRequestException e)
        {
            if (mine == attempt)
            {
                Problem = Undrawable("repo",
                    $"GitHub couldn't be reached for {repo} ({e.Message}).");
            }
        }
        finally
        {
            if (mine == attempt) Loading = false;
        }
    }

    public void ShowLayer(Layer layer, bool on) =>
        layers = [.. layers.Select(l => l.Layer == layer ? l with { On = on } : l)];

    public void DismissProblem() => Problem = Problem.None;

    /// <summary>
    /// Starts something new the Reviewer tried, so a repo still being read is
    /// dropped and no longer loading. Returns the new attempt.
    /// </summary>
    private int Overtake()
    {
        Loading = false;
        return ++attempt;
    }

    /// <param name="noReport">What to say when there's no report.</param>
    private void Show(
        Source source, IReadOnlyList<CodeFile> files, FoundReport found,
        Note noReport)
    {
        var (report, ringsNote, about) = Read(source, found, noReport);
        var graph = Graph.Of(files, report);
        if (graph.Bubbles.Count == 0)
        {
            var title = source.Kind switch
            {
                SourceKind.Repo => "That repo can't be drawn",
                SourceKind.Files => "Those files can't be drawn",
                _ => "That folder can't be drawn",
            };
            var where = source.Kind == SourceKind.Files
                ? "the files dropped"
                : source.Name;
            Problem = new Problem(title,
                $"No C# projects in {where}. Open a Motif repo's root "
                + "folder, the one with its .sln, or a public Motif repo.");
            return;
        }
        if (ringsNote == Note.None) ringsNote = Matches(graph, report);
        Graph = graph;
        Source = source with { Report = about };
        RingsNote = ringsNote;
        Problem = Problem.None;
    }

    /// <summary>
    /// The report found with the code, what to say about it, and where it
    /// came from in a phrase.
    /// </summary>
    private static (MutationReport Report, Note Note, string About) Read(
        Source source, FoundReport found, Note noReport)
    {
        var none = new MutationReport([]);
        if (found == FoundReport.None) return (none, noReport, "");
        if (!MutationReport.TryRead(found.Json, out var report, out var why))
        {
            return (none, new Note("The Stryker report couldn't be read",
                $"So the bubbles have no rings. {why}"), "");
        }
        var about = StrykerRun.TryRead(found.Run, out var when)
            ? $"Stryker run {when}"
            : source.Kind == SourceKind.Repo
                ? "Stryker report from Pages"
                : "Stryker report";
        return (report, Note.None, about);
    }

    /// <summary>
    /// What to say when the report doesn't fit the code: it's for other code,
    /// or some of the code has changed since it was made.
    /// </summary>
    private static Note Matches(Graph graph, MutationReport report)
    {
        var ringed = graph.Bubbles.Where(b => b.Mutation.IsMutated).ToList();
        if (ringed.Count == 0)
        {
            return report.Files.Count == 0
                ? Note.None
                : new Note("The Stryker report doesn't match this code",
                    "None of the files it mutated are here, so the bubbles "
                    + "have no rings.");
        }
        var stale = ringed.Count(b => b.Mutation.Stale);
        if (stale == 0) return Note.None;
        var bubbles = stale == 1 ? "1 bubble has" : $"{stale} bubbles have";
        return new Note("Some rings are older than the code",
            $"{bubbles} changed since Stryker ran, so their faded rings may "
            + "be out of date.");
    }

    private static Problem Undrawable(string what, string text) =>
        new($"That {what} can't be drawn", text);
}
