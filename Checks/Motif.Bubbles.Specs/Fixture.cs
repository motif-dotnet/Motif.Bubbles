using Motif.Bubbles.Adapters;
using Motif.Bubbles.Fakes;

namespace Motif.Bubbles.Specs;

/// <summary>
/// The application over its real adapters, with only their Fittings faked.
/// </summary>
public sealed class Fixture
{
    public FakeGitHub GitHub { get; } = new();
    public FakeLocalFolders LocalFolders { get; } = new();
    public MotifBubblesApplication App { get; }

    public Fixture() => App = new MotifBubblesApplication(
        new ForOpeningReposViaGitHub(GitHub),
        new ForOpeningFoldersViaPickerAndDrop(LocalFolders));

    public ForDoing Tasks => App;
    public ForSeeing Queries => App;

    /// <summary>
    /// The graph of these files, picked as a folder.
    /// </summary>
    public static async Task<Graph> Drawn(IEnumerable<CodeFile> files)
    {
        var f = await Picked(files);
        return f.Queries.Graph;
    }

    /// <summary>
    /// An app with these files picked as the Motif.Tiny folder.
    /// </summary>
    public static async Task<Fixture> Picked(IEnumerable<CodeFile> files)
    {
        var f = new Fixture();
        f.LocalFolders.WillPick("Motif.Tiny", files);
        await f.Tasks.OpenFolder();
        if (f.Queries.Problem != Problem.None)
            throw new InvalidOperationException(f.Queries.Problem.Text);
        return f;
    }

    /// <summary>
    /// This repo's own code, as it was when the specs were built.
    /// </summary>
    public static IReadOnlyList<CodeFile> OwnCode()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Own");
        return
        [
            .. Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                .Select(path => new CodeFile(
                    Path.GetRelativePath(root, path).Replace('\\', '/'),
                    File.ReadAllText(path))),
        ];
    }
}
