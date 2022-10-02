using LineCounter;

namespace CodeAnalysis;

public class CommentLineAnalyzer
{
    private readonly bool _reportCommentLines;
    private int _multiLineCommentLines;
    private int _initCommentLines;
    private readonly List<int> _commentLineIndices = new();

    public CommentLineAnalyzer(bool reportCommentLines)
    {
        _reportCommentLines = reportCommentLines;
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
        if (_reportCommentLines) WarningRepo.Comments.Add(line);
    }

    private void RegisterMultilineComment(string line, int index)
    {
        _multiLineCommentLines++;
        _commentLineIndices.Add(index);
        if (_reportCommentLines) WarningRepo.Comments.Add(line);
    }

    public ClearedLines GetRegularCode(List<string> lines)
    {
        CountCommentLines(lines);
        List<int> linesToFilterOut = new();
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].StartsWith("[")) linesToFilterOut.Add(i);
        }
        linesToFilterOut.AddRange(_commentLineIndices);
        return new ClearedLines
        {
            Lines = lines.Where((line, index) => !linesToFilterOut.Contains(index)).ToList()
        };
    }
}

public class ClearedLines
{
    public IReadOnlyList<string> Lines { get; init; } = null!;
}