using System.Text.RegularExpressions;

namespace Motif.Bubbles;

/// <summary>
/// Maps a repo's files onto its architectural objects, by Motif's naming
/// conventions: a project is a folder with a .csproj, whose name says what
/// it holds (X.Domain, X.UseCases, X.App, X.ForSomething under Adapters/,
/// X.Fakes, X.Specs, X.BrowserSpecs). A repo that strays from them is mapped
/// badly, and the conventions are still moving.
/// </summary>
internal static partial class Scanner
{
    public const string UserTasks = "UserTasks";
    public const string UserQueries = "UserQueries";
    public const string Application = "Application";
    public const string Domain = "Domain";
    public const string WebApp = "WebApp";
    public const string Specs = "Specs";
    public const string BrowserSpecs = "BrowserSpecs";

    /// <summary>
    /// The row for fakes in a folder named for no driven port.
    /// </summary>
    public const string OtherRow = "￿";

    // The User port's two halves, by file name.
    private const string ForDoing = "ForDoing";
    private const string ForSeeing = "ForSeeing";

    public static IReadOnlyList<Part> Parts(IEnumerable<CodeFile> files)
    {
        var wanted = files.Where(f => CodeFile.IsWanted(f.Path)).ToList();
        // Each project's folder, and its name from its project file.
        var names = wanted
            .Where(f => f.IsProjectFile)
            .GroupBy(f => Folder(f.Path))
            .ToDictionary(g => g.Key, g => Stem(g.First()));
        var projects = names.Keys.OrderByDescending(p => p.Length).ToList();
        var byProject = wanted
            .Where(f => !f.IsProjectFile)
            .Select(f => (File: f, Project: projects.FirstOrDefault(p =>
                p == "" || f.Path.StartsWith(p + "/"))))
            .Where(x => x.Project is not null)
            .GroupBy(x => x.Project!, x => x.File)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<CodeFile>)[.. g]);

        var parts = new List<Part>();
        foreach (var project in projects.OrderBy(p => p, StringComparer.Ordinal))
        {
            var inside = byProject.GetValueOrDefault(project) ?? [];
            parts.AddRange(PartsOf(project, names[project], inside));
        }
        return parts;
    }

    private static IEnumerable<Part> PartsOf(
        string project, string name, IReadOnlyList<CodeFile> files)
    {
        var last = name[(name.LastIndexOf('.') + 1)..];
        if (name.EndsWith(".BrowserSpecs"))
        {
            return [new(BrowserSpecs, "Browser specs",
                "Browser specs: drive the Web App in a browser",
                BubbleKind.Specs, Column.BrowserChecks, "", files)];
        }
        if (name.EndsWith(".Specs"))
        {
            return [new(Specs, "Specs", "Specs: drive the User port",
                BubbleKind.Specs, Column.Driving, "", files)];
        }
        if (name.EndsWith(".Fakes")) return Fakes(project, files);
        if (name.EndsWith(".Domain"))
        {
            return [new(Domain, "Domain", "Domain: the app's own rules",
                BubbleKind.Real, Column.Application, "", files)];
        }
        if (name.EndsWith(".UseCases")) return UseCases(project, files);
        if (name.EndsWith(".App"))
        {
            return [new(WebApp, "Web App", $"Driving adapter ({name})",
                BubbleKind.Real, Column.Driving, "", [.. files.Where(
                    f => !IsKitCopy(Relative(project, f)))])];
        }
        if (IsPortName(last)) return Adapters(last, files);
        return [new("library:" + name, name, $"Library ({name})",
            BubbleKind.Real, Column.Application, "", files)];
    }

    /// <summary>
    /// The User port's halves, each driven port, and the Application: the
    /// rest of the use cases.
    /// </summary>
    private static IEnumerable<Part> UseCases(
        string project, IReadOnlyList<CodeFile> files)
    {
        var rest = new List<CodeFile>();
        foreach (var file in files)
        {
            var path = Relative(project, file);
            var stem = Stem(file);
            if (path == $"{ForDoing}.cs")
            {
                yield return Port(UserTasks, "User Tasks",
                    $"User port: the commands ({ForDoing})", Column.UserPort,
                    "", file, ForDoing, "tasks");
            }
            else if (path == $"{ForSeeing}.cs")
            {
                yield return Port(UserQueries, "User Queries",
                    $"User port: the queries ({ForSeeing})", Column.UserPort,
                    "", file, ForSeeing, "queries");
            }
            else if (path.StartsWith("Ports/") && IsPortName(stem))
            {
                yield return Port(PortId(stem), Words.Sentence(stem),
                    $"Driven port ({stem})", Column.DrivenPorts, stem, file,
                    stem, "members");
            }
            else
            {
                rest.Add(file);
            }
        }
        if (rest.Count == 0) yield break;
        var application = rest
            .Select(Stem)
            .Where(s => s.EndsWith("Application"))
            .Order(StringComparer.Ordinal)
            .FirstOrDefault();
        yield return new(Application, "Application",
            application is null ? "Use cases" : $"Use cases ({application})",
            BubbleKind.Real, Column.Application, "", rest);
    }

    private static Part Port(
        string id, string name, string role, Column column, string row,
        CodeFile file, string interfaceName, string memberWord) =>
        new(id, name, role, BubbleKind.Port, column, row, [file],
            Counting.Members(file, interfaceName), memberWord);

    /// <summary>
    /// One real adapter for each ForPortViaWay class in the project. With
    /// more than one, each of the project's other files (its Seams and
    /// Fittings) goes to the first adapter that names it, or else the first.
    /// </summary>
    private static IEnumerable<Part> Adapters(
        string port, IReadOnlyList<CodeFile> files)
    {
        var ways = files
            .Where(f => f.Extension == ".cs" && Stem(f).StartsWith(port + "Via"))
            .OrderBy(Stem, StringComparer.Ordinal)
            .ToList();
        if (ways.Count == 0)
        {
            return [new(AdapterId(port), Words.Sentence(port) + " adapter",
                $"Real adapter ({port})", BubbleKind.Real,
                Column.RealAdapters, port, files)];
        }
        var shares = ways.ToDictionary(w => w, w => new List<CodeFile> { w });
        foreach (var file in files.Except(ways))
        {
            var namer = ways.FirstOrDefault(w => Names(w, Stem(file)));
            shares[namer ?? ways[0]].Add(file);
        }
        return ways.Select(way => new Part(
            AdapterId(Stem(way)),
            Words.Sentence(Stem(way)[(port.Length + "Via".Length)..]),
            $"Real adapter ({Stem(way)})",
            BubbleKind.Real,
            Column.RealAdapters,
            port,
            shares[way]));
    }

    /// <summary>
    /// A fake for each file, in the row of the driven port its folder is
    /// named for.
    /// </summary>
    private static IEnumerable<Part> Fakes(
        string project, IReadOnlyList<CodeFile> files) =>
        files
            .Where(f => f.Extension == ".cs")
            .Select(file =>
            {
                var path = Relative(project, file);
                var folder = path.Contains('/') ? path[..path.IndexOf('/')] : "";
                var stem = Stem(file);
                var row = IsPortName(folder) ? folder : OtherRow;
                var role = row == OtherRow
                    ? $"Fake ({stem})"
                    : $"Fake {Words.Sentence(folder).ToLowerInvariant()}"
                        + $" ({stem})";
                return new Part("fake:" + stem, Words.Sentence(stem), role,
                    BubbleKind.Fake, Column.Fakes, row, [file]);
            });

    public static string PortId(string port) => "port:" + port;

    private static string AdapterId(string name) => "adapter:" + name;

    /// <summary>
    /// The web kit's copy in a Motif app: the suite's code, not the app's.
    /// </summary>
    private static bool IsKitCopy(string path) =>
        path.StartsWith("Suite/") || path == "wwwroot/css/motif.css";

    private static bool IsPortName(string name) =>
        name.Length > 3 && name.StartsWith("For") && char.IsUpper(name[3])
        && name is not ForDoing and not ForSeeing;

    private static bool Names(CodeFile file, string word) =>
        Regex.IsMatch(file.Text, $@"\b{Regex.Escape(word)}\b");

    private static string Folder(string path) =>
        path.Contains('/') ? path[..path.LastIndexOf('/')] : "";

    private static string Relative(string project, CodeFile file) =>
        project == "" ? file.Path : file.Path[(project.Length + 1)..];

    private static string Stem(CodeFile file) =>
        file.Name[..^file.Extension.Length];
}
