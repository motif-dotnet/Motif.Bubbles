using System.Text.RegularExpressions;

namespace Motif.Bubbles;

/// <summary>
/// How a bubble's files are counted: lines of code, types, specs and an
/// interface's members, by reading the text, not compiling it.
/// </summary>
public static partial class Counting
{
    /// <summary>
    /// The lines that hold code: not blank, and not a comment.
    /// </summary>
    public static IEnumerable<string> CodeLines(CodeFile file)
    {
        var closing = "";
        foreach (var raw in file.Text.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0) continue;
            if (closing != "")
            {
                if (line.Contains(closing)) closing = "";
                continue;
            }
            if (line.StartsWith("//")) continue;
            if (Opens(file, line) is var (opener, closer))
            {
                if (!line[opener.Length..].Contains(closer)) closing = closer;
                continue;
            }
            yield return line;
        }
    }

    public static int LinesOfCode(CodeFile file) => CodeLines(file).Count();

    /// <summary>
    /// Classes, records, structs, interfaces and enums declared in C#. A Razor
    /// file is one component.
    /// </summary>
    public static int Types(CodeFile file) => file.Extension switch
    {
        ".cs" => CodeLines(file).Count(TypePattern().IsMatch),
        ".razor" => 1,
        _ => 0,
    };

    /// <summary>
    /// Facts and Theories, each counted once however many cases a Theory has.
    /// </summary>
    public static int Specs(CodeFile file) =>
        file.Extension == ".cs"
            ? CodeLines(file).Sum(line => SpecPattern().Count(line))
            : 0;

    /// <summary>
    /// The methods and properties declared in an interface. A member over
    /// several lines counts once, at the line that ends it.
    /// </summary>
    public static int Members(CodeFile file, string interfaceName)
    {
        var members = 0;
        var depth = -1;
        foreach (var line in CodeLines(file))
        {
            if (depth < 0)
            {
                if (Declares(line, interfaceName)) depth = 0;
                if (depth < 0 || !line.Contains('{')) continue;
            }
            if (depth == 1 && (line.EndsWith(");") || line.Contains("{ get")))
                members++;
            depth += line.Count(c => c == '{') - line.Count(c => c == '}');
            if (depth <= 0 && line.Contains('}')) break;
        }
        return members;
    }

    private static bool Declares(string line, string interfaceName) =>
        Regex.IsMatch(line, $@"\binterface\s+{Regex.Escape(interfaceName)}\b");

    /// <summary>
    /// The block comment a line starts with, if it does.
    /// </summary>
    private static (string Opener, string Closer)? Opens(
        CodeFile file, string line)
    {
        if (line.StartsWith("/*")) return ("/*", "*/");
        if (file.Extension != ".razor") return null;
        if (line.StartsWith("@*")) return ("@*", "*@");
        if (line.StartsWith("<!--")) return ("<!--", "-->");
        return null;
    }

    [GeneratedRegex(
        @"^(?:(?:public|internal|private|protected|sealed|static|abstract"
        + @"|partial|readonly|file|ref)\s+)*"
        + @"(?:class|record|struct|interface|enum)\s+(?:class\s+|struct\s+)?"
        + @"[A-Za-z_]\w*")]
    private static partial Regex TypePattern();

    [GeneratedRegex(@"\[(?:Fact|Theory)\b")]
    private static partial Regex SpecPattern();
}
