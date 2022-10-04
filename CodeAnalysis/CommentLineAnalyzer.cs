using LineCounter;

namespace CodeAnalysis;

public class CommentLineAnalyzer
{
    private int _multiLineCommentLines;
    private int _initCommentLines;
    private readonly List<int> _commentLineIndices = new();
    private readonly LineReport? _report;

    public CommentLineAnalyzer(LineReport report = null)
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

    // basically: you want regular code
    // So need to parse code:
    // Save collection of ranges of real code. (so no "" or comments) 
    // Copy code in those ranges to new lines.
    public ClearedLines GetRegularCode(List<string> lines)
    {
        /*
        //CountCommentLines(lines);
        List<int> linesToFilterOut = new();
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].StartsWith("[")) linesToFilterOut.Add(i);
        }
        List<StringCoordinates> stringLines = GetStringLines(lines);
        foreach (StringCoordinates coords in stringLines)
        {
            // single-line-string
            if (coords.Start.Line == coords.End.Line)
            {
                string currentLineContents = lines[coords.Start.Line];
                lines[coords.Start.Line] = currentLineContents[0..]
            }
        }
        linesToFilterOut.AddRange(_commentLineIndices);
        return new ClearedLines
        {
            Lines = lines.Where((line, index) => !linesToFilterOut.Contains(index)).ToList()
        };*/
    }

    private record CodeCoordinate(int Line, int Position);

    private record CodeCoordinates(CodeCoordinate Start, CodeCoordinate End);

    private List<CodeCoordinates> GetCode(List<string> lines)
    {
        List<CodeCoordinates> stringCoordinates = new();

        bool inString = false;
        bool inEscape = false;
        CodeCoordinate stringStart = new(0, 0);
        CodeCoordinate stringEnd;
        for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
        {
            string line = lines[lineIndex];
            for (int i = 0; i < line.Length; i++)
            {
                char ch = line[i];
                if (ch == '"' && !inEscape)
                {
                    inString = !inString;
                    if (inString)
                    {
                        stringStart = new CodeCoordinate(lineIndex, i);
                    }
                    else
                    {
                        stringEnd = new CodeCoordinate(lineIndex, i);
                        stringCoordinates.Add(new CodeCoordinates(stringStart, stringEnd));
                    }
                }
                if (ch == '\\' && inString) inEscape = !inEscape;
                else inEscape = false;

            }
        }
        return stringCoordinates;
    }
}

public class ClearedLines
{
    public IReadOnlyList<string> Lines { get; init; } = null!;
}