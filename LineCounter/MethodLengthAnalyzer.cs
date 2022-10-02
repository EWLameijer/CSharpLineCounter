using CodeAnalysis;

namespace LineCounter;

internal class MethodLengthAnalyzer
{
    private readonly List<string> _lines;
    private int indentationLevel = 0;
    private int? lastBlankLineIndex = null;
    private int? methodStartIndex = null;
    private readonly string _filename;
    private readonly bool _isFileScoped;

    public MethodLengthAnalyzer(string filename)
    {
        _lines = File.OpenText(filename).ReadToEnd().
            Split("\n").Select(line => line.Trim()).ToList();
        _filename = Path.GetFileName(filename);
        _isFileScoped = false;
        foreach (string line in _lines)
        {
            if (line.StartsWith("namespace") && line.EndsWith(";")) _isFileScoped = true;
        }
    }

    private int MethodIndentationLevel() => _isFileScoped ? 1 : 2;

    public void Analyze()
    {
        for (int i = 0; i < _lines.Count; i++)
        {
            string line = _lines[i];
            if (line.Length == 0 && indentationLevel == MethodIndentationLevel()) lastBlankLineIndex = i;
            else
            {
                HandleBraces(i, line);
            }
        }
    }

    private void HandleBraces(int i, string line)
    {
        if (line == "{")
        {
            HandleOpeningBrace(i);
        }
        else if (line.StartsWith("}")) // do-while loops...
        {
            HandleClosingBrace(i);
        }
    }

    private void HandleOpeningBrace(int i)
    {
        indentationLevel++;
        if (indentationLevel == MethodIndentationLevel()) lastBlankLineIndex = i;
        else if (indentationLevel == MethodIndentationLevel() + 1) methodStartIndex = i;
    }

    private void HandleClosingBrace(int i)
    {
        indentationLevel--;
        if (indentationLevel == MethodIndentationLevel())
        {
            string methodName = GetMethodName(lastBlankLineIndex);
            if (methodName != "") AnalyzeCode(methodName, methodStartIndex, i);
            lastBlankLineIndex = i;
        }
    }

    private sealed class CapitalData
    {
        public bool CapitalUsed { get; set; }
        public int LastCapitalIndex { get; set; }
    }

    private string GetMethodName(int? lastBlankLineIndex)
    {
        string methodNameLine = GetMethodNameLine(lastBlankLineIndex);
        CapitalData capitalData = new();
        for (int i = 0; i < methodNameLine.Length; i++)
        {
            char ch = methodNameLine[i];
            UpdateCapitalData(capitalData, i, ch);
            if (ch == '(' && capitalData.CapitalUsed)
            {
                return methodNameLine[capitalData.LastCapitalIndex..i];
            }
        }

        return "";
    }

    private static void UpdateCapitalData(CapitalData capitalData, int i, char ch)
    {
        if (ch == ' ') capitalData.CapitalUsed = false;
        if (char.IsUpper(ch) && !capitalData.CapitalUsed)
        {
            capitalData.CapitalUsed = true;
            capitalData.LastCapitalIndex = i;
        }
    }

    private string GetMethodNameLine(int? lastBlankLineIndex)
    {
        if (lastBlankLineIndex == null) throw
                new ArgumentNullException(nameof(lastBlankLineIndex));
        int correctLineIndex = (int)lastBlankLineIndex;

        return new CommentLineAnalyzer(false).FindFirstNonCommentLine(_lines, correctLineIndex);
    }

    private void AnalyzeCode(string methodName, int? startIndex, int endIndex)
    {
        if (startIndex == null) throw
                new ArgumentNullException(nameof(startIndex));
        List<string> linesToAnalyze = new();
        for (int lineIndex = (int)startIndex; lineIndex <= endIndex; lineIndex++)
            linesToAnalyze.Add(_lines[lineIndex]);
        LineReport report = new(linesToAnalyze, false);
        int codeLines = report.CodeLines + report.BraceLines;
        string message = $"{methodName} {codeLines}";
        if (codeLines > 15)
        {
            WarningRepo.Warnings.Add($"TOO LONG METHOD: {_filename}/{message}");
        }
        Console.WriteLine(message);
    }
}