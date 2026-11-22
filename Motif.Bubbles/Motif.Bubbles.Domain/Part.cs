namespace Motif.Bubbles;

/// <summary>
/// A part of the repo that becomes a bubble, before it's placed on the graph
/// and given its ring.
/// </summary>
/// <param name="Row">
/// The driven port whose row it sits in, by name, or "" for the columns left
/// of the driven ports.
/// </param>
internal sealed record Part(
    string Id,
    string Name,
    string Role,
    BubbleKind Kind,
    Column Column,
    string Row,
    IReadOnlyList<CodeFile> Files,
    int Members = 0,
    string MemberWord = "")
{
    public int Specs => Files.Sum(Counting.Specs);

    public Size Size => new(
        Files.Sum(Counting.LinesOfCode), Files.Count, Files.Sum(Counting.Types));

    public Bubble Placed(double lane, Mutation mutation) => new(
        Id, Name, Role, Kind, Column, lane, Size, Specs, Members, MemberWord,
        mutation);
}
