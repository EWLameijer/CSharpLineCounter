using CodeAnalysis;

namespace LineCounter;

public class MethodLengthAnalyzer
{
    private readonly IReadOnlyList<string> _lines;
    private int _indentationLevel = 0;
    private int? _lastBlankLineIndex = null;
    private int? _methodStartIndex = null;
    private readonly string _filename;
    private readonly FileCharacteristics _characteristics;

    private int MethodLevel() => _characteristics.MethodLevel - 1;

    public MethodLengthAnalyzer(string filename, ClearedLines clearedLines)
    {
        _filename = filename;
        _lines = clearedLines.Lines;
        _characteristics = new FileCharacteristics(clearedLines);
    }

    public void Analyze()
    {
        for (int i = 0; i < _lines.Count; i++)
        {
            string line = _lines[i];
            if (line.Length == 0 && _indentationLevel == MethodLevel()) _lastBlankLineIndex = i;
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
        _indentationLevel++;
        if (_indentationLevel == MethodLevel()) _lastBlankLineIndex = i;
        else if (_indentationLevel == MethodLevel() + 1)
        {
            CheckForLackingWhiteSpace(i);
        }
    }

    private void CheckForLackingWhiteSpace(int i)
    {
        string methodLine = "Unknown method";
        for (int lineIndexBefore = i - 1; lineIndexBefore >= 0; lineIndexBefore--)
        {
            string line = _lines[lineIndexBefore];
            if (line == "") break;
            if (line == "}")
            {
                WarningRepo.Warnings.Add($"No whitespace in {_filename} before {methodLine}");
                break;
            }
            else methodLine = line;
        }
        _methodStartIndex = i;
    }

    private void HandleClosingBrace(int i)
    {
        _indentationLevel--;
        if (_indentationLevel == MethodLevel())
        {
            string methodName = GetMethodName(_lastBlankLineIndex);
            if (methodName != "") AnalyzeCode(methodName, _methodStartIndex, i);
            _lastBlankLineIndex = i;
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

        return new CommentLineAnalyzer(false).FindFirstNonCommentLine(_lines, correctLineIndex).line;
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