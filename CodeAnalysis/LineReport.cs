using CodeAnalysis;

namespace LineCounter;

public class LineReport
{
    public int NonBlankLines { get; init; }

    private readonly int _initCommentLines;

    private readonly int _multiLineCommentLines;

    public int TotalCommentLines => _initCommentLines + _multiLineCommentLines;

    public int CodeLines => NonBlankLines - TotalCommentLines - BraceLines - OpeningLines;

    public int TotalLines { get; init; }

    public int BraceLines { get; init; }

    // usings + namespace
    public int OpeningLines { get; init; }

    public List<string> Warnings { get; } = new();

    public List<string> Comments { get; } = new();

    private bool IsRawStartingLine(string line) =>
        line.StartsWith("using") || line.StartsWith("namespace");

    public LineReport(List<string> lines)
    {
        TotalLines = lines.Count;

        List<string> trimmedLines = lines.Select(line => line.Trim()).ToList();
        List<string> usingPlusBlankLines = trimmedLines.
            TakeWhile(line => line == "" || IsRawStartingLine(line)).ToList();
        OpeningLines = usingPlusBlankLines.Count(IsRawStartingLine);
        NonBlankLines = trimmedLines.Count(line => line.Length > 0);
        BraceLines = trimmedLines.Count(line => line == "{" || line == "}");
        // multilinecomments: If line STARTS with /*, then whole is comment, until you find a */
        (_initCommentLines, _multiLineCommentLines) =
            new CommentLineAnalyzer(this).CountCommentLines(trimmedLines);
    }
}