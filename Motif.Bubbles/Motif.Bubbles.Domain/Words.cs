using System.Text.RegularExpressions;

namespace Motif.Bubbles;

/// <summary>
/// A name from the code written as words, as a bubble shows it:
/// ForOpeningFiles is "For opening files", FakeGitHub is "Fake GitHub".
/// </summary>
public static partial class Words
{
    // Names that read as one word, though written in two humps.
    private static readonly string[] Whole =
        ["GitHub", "IndexedDB", "JavaScript", "WebAssembly", "PowerShell"];

    /// <summary>
    /// The name in sentence case. Acronyms (UI, DB) and the names that read
    /// as one word keep their capitals.
    /// </summary>
    public static string Sentence(string name)
    {
        var words = new List<string>();
        var rest = name;
        while (rest.Length > 0)
        {
            var whole = Whole.FirstOrDefault(w =>
                rest.StartsWith(w, StringComparison.Ordinal));
            var word = whole ?? HumpPattern().Match(rest).Value;
            if (word.Length == 0) word = rest[..1];
            words.Add(words.Count == 0 || whole is not null || IsAcronym(word)
                ? word
                : word.ToLowerInvariant());
            rest = rest[word.Length..];
        }
        return string.Join(" ", words);
    }

    /// <summary>
    /// The words with the first letter a capital: "tasks" is "Tasks".
    /// </summary>
    public static string Capitalised(string words) =>
        words.Length == 0 ? words : char.ToUpperInvariant(words[0]) + words[1..];

    private static bool IsAcronym(string word) =>
        word.Length > 1 && word.All(char.IsUpper);

    // An acronym that runs up to the next word (UI in UIButton), a word, or
    // a run of digits.
    [GeneratedRegex(@"^(?:[A-Z]+(?=[A-Z][a-z])|[A-Z]+(?![a-z])|[A-Z]?[a-z]+|\d+)")]
    private static partial Regex HumpPattern();
}
