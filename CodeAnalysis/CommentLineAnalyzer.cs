using System.Text;
using LineCounter;

namespace CodeAnalysis;

public class CommentLineAnalyzer
{
    private int _multiLineCommentLines;
    private int _initCommentLines;
    private readonly List<int> _commentLineIndices = new();
    private readonly LineReport? _report;

    public CommentLineAnalyzer(LineReport? report = null)
    {
        _report = report;
    }

    public (string line, int index) FindFirstNonCommentLine(IReadOnlyList<string> lines, int startIndex)
    {
        int index = startIndex;
        bool isCommentLine = false;
        do
        {
            index++;
            isCommentLine = UpdateCommentLineCount(lines[index], index, isCommentLine);
        } while (isCommentLine);
        return (lines[index], index);
    }

    public (int lineCommCounts, int multiLineCommCounts) CountCommentLines(List<string> lines)
    {
        bool inMultiLineComment = false;
        for (int i = 0; i < lines.Count; i++)
        {
            inMultiLineComment = UpdateCommentLineCount(lines[i], i, inMultiLineComment);
        }
        return (_initCommentLines, _multiLineCommentLines);
    }

    private bool UpdateCommentLineCount(string line, int index, bool status)
    {
        bool newStatus = status;

        if (new LineParser(line).HasActiveMultiLineCommentOpener())
        {
            newStatus = true;
        }

        if (newStatus) RegisterMultilineComment(line, index);
        if (!newStatus && line.StartsWith("//")) RegisterLineComment(line, index);

        if (line.EndsWith("*/"))
        {
            newStatus = false;
        }
        return newStatus;
    }

    private void RegisterLineComment(string line, int index)
    {
        _initCommentLines++;
        _commentLineIndices.Add(index);
        _report?.Comments.Add(line);
    }

    private void RegisterMultilineComment(string line, int index)
    {
        _multiLineCommentLines++;
        _commentLineIndices.Add(index);
        _report?.Comments.Add(line);
    }

    public ClearedLines GetRegularCode(List<string> lines)
    {
        StringBuilder result = new();
        bool inBlockComment = false; // block comment; line comments are immediately skipped
        bool inString = false;
        bool inCharString = false;
        bool inStringEscape = false;

        foreach (string line in lines)
        {
            int lineLength = line.Length;
            for (int i = 0; i < lineLength; i++)
            {
                char ch = line[i];
                if (inBlockComment)
                {
                    if (ch == '/' && i > 0 && line[i - 1] == '*') inBlockComment = false;
                }
                else if (inString)
                {
                    if (ch == '\\') inStringEscape = !inStringEscape; else inStringEscape = false;
                    if (ch == '"' && !inStringEscape)
                    {
                        inString = false;
                        result.Append('"');
                    }
                }
                else if (inCharString)
                {
                    if (ch == '\\') inStringEscape = !inStringEscape; else inStringEscape = false;
                    if (ch == '\'' && !inStringEscape)
                    {
                        inCharString = false;
                        result.Append('\'');
                    }
                }
                else
                {
                    if (ch == '"') inString = true;
                    if (ch == '\'') inCharString = true;
                    if (ch == '/' && i + 1 < lineLength)
                    {
                        if (line[i + 1] == '/') break; // line comment
                        if (line[i + 1] == '*') inBlockComment = true;
                    }
                    else result.Append(ch);
                }
            }
            result.Append('\n');
        }
        List<string> currentLines = result.ToString().Split("\n").ToList();
        List<string> withoutAnnotations = currentLines.Where(line => !line.StartsWith("[")).ToList();
        return new ClearedLines { Lines = withoutAnnotations };
    }
}

public class ClearedLines
{
    public IReadOnlyList<string> Lines { get; init; } = null!;
}