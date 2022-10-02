using LineCounter;

namespace CodeAnalysis;

public class CommentLineAnalyzer
{
    private readonly bool _reportCommentLines;
    private int _multiLineCommentLines;
    private int _initCommentLines;

    public CommentLineAnalyzer(bool reportCommentLines)
    {
        _reportCommentLines = reportCommentLines;
    }

    public (string line, int index) FindFirstNonCommentLine(List<string> lines, int startIndex)
    {
        int index = startIndex;
        bool isCommentLine = false;
        do
        {
            index++;
            isCommentLine = UpdateCommentLineCount(lines[index], isCommentLine);
        } while (isCommentLine);
        return (lines[index], index);
    }

    public (int lineCommCounts, int multiLineCommCounts) CountCommentLines(List<string> lines)
    {
        bool inMultiLineComment = false;
        foreach (string line in lines)
        {
            inMultiLineComment = UpdateCommentLineCount(line, inMultiLineComment);
        }
        return (_initCommentLines, _multiLineCommentLines);
    }

    private bool UpdateCommentLineCount(string line, bool status)
    {
        bool newStatus = status;

        if (new LineParser(line).HasActiveMultiLineCommentOpener())
        {
            newStatus = true;
        }

        if (newStatus) RegisterMultilineComment(line);
        if (!newStatus && line.StartsWith("//")) RegisterLineComment(line);

        if (line.EndsWith("*/"))
        {
            newStatus = false;
        }
        return newStatus;
    }

    private void RegisterLineComment(string line)
    {
        _initCommentLines++;
        if (_reportCommentLines) WarningRepo.Comments.Add(line);
    }

    private void RegisterMultilineComment(string line)
    {
        _multiLineCommentLines++;
        if (_reportCommentLines) WarningRepo.Comments.Add(line);
    }
}