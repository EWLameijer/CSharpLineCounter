using CodeAnalysis;
using CodeAnalysis.DTOs;

namespace LineCounter;

public class IdentifierAnalyzer
{
    private readonly string _filename;
    private readonly IReadOnlyList<string> _lines;
    private readonly FileCharacteristics _characteristics;
    private readonly LineReport _report;
    private string _className = "";

    private int MethodLevel => _characteristics.MethodLevel;

    private bool IsTopLevelFile => _characteristics.IsTopLevelFile;

    private int _indentationLevel;

    public IdentifierAnalyzer(FileData fileData, LineReport lineReport)
    {
        _filename = fileData.Filename;
        ClearedLines clearedLines = fileData.ClearedLines;
        _lines = clearedLines.Lines;
        _characteristics = new FileCharacteristics(clearedLines);
        _report = lineReport;
    }

    public void Analyze()
    {
        for (int i = 0; i < _lines.Count; i++)
        {
            string line = _lines[i];
            if (line.StartsWith("class")) _className = line.Split()[1];
            if (line.StartsWith("{")) _indentationLevel++;
            else if (line.StartsWith("}")) _indentationLevel--;
            else if (_indentationLevel >= MethodLevel) FindTypingErrors(line);
            else if (_indentationLevel == MethodLevel - 1) FindFieldErrors(line);
            if (_indentationLevel == MethodLevel - 1) i = ProcessNonMethodLine(i);
        }
    }

    // TODO: still need: private readonly int initCommentLines;
    private void FindFieldErrors(string line)
    {
        if (IsTopLevelFile) return;
        if (MethodHeaderAnalyzer.IsMethod(line, _className).isMethod) return;
        List<string> components = line.Split(' ').ToList();
        if (components.Contains("namespace") || components.Contains("using") ||
            components.Contains("class") || components.Contains("record")) return;
        if (HandleUninitializedField(components)) return;
        int assignIndex = components.IndexOf("=");
        if (assignIndex < 0) return;
        string identifier = components[assignIndex - 1];
        if (identifier == "}") return; // property!
        if (!StartsWithRightCharacter(components, identifier))
            _report.Warnings.Add($"Invalid field name {identifier} in {_filename}.");
    }

    private bool HandleUninitializedField(List<string> components)
    {
        if (components.Count is 2 or 3 or 4)
        {
            string identifier = components[^1];
            if (identifier.EndsWith(")") || identifier.EndsWith(");") ||
                !char.IsLetterOrDigit(identifier[^1])) return true;

            if (!StartsWithRightCharacter(components, identifier))
                _report.Warnings.Add($"Invalid field name {identifier} in {_filename}.");
        }

        return false;
    }

    private bool StartsWithRightCharacter(List<string> components, string identifier)
    {
        bool shouldStartWithCapital = components.Contains("const") ||
            components.Contains("public") || components.Contains("protected");
        char startCh = identifier[0];
        if (shouldStartWithCapital) return char.IsUpper(startCh);
        if (IsTopLevelFile) return char.IsLower(startCh);
        return startCh == '_';
    }

    /* Can you
     * Do this
     // correctly?
    */

    private int ProcessNonMethodLine(int lineIndex)
    {
        (string line, int newLineIndex) =
            new CommentLineAnalyzer().FindFirstNonCommentLine(_lines, lineIndex - 1);
        (bool isMethod, int position) = MethodHeaderAnalyzer.IsMethod(line, _className);
        if (isMethod) newLineIndex = CheckParameters(newLineIndex, position);

        return newLineIndex;
    }

    private int CheckParameters(int lineIndex, int position)
    {
        int lineIndexToTest = lineIndex;
        string currentLine = _lines[lineIndexToTest];
        do
        {
            CheckParametersPerLine(lineIndexToTest, position);
            position = 0;
            lineIndexToTest++;
        } while (!IsEndOfMethodHeader(currentLine, position));

        return lineIndexToTest - 1;
    }

    private static bool IsEndOfMethodHeader(string currentLine, int position)
    {
        int depth = 0;
        for (int i = position; i < currentLine.Length; i++)
        {
            char ch = currentLine[position];
            if (ch == '(') depth++;
            if (ch == ')') depth--;
            if (depth == 0) return true;
        }
        return false;
    }

    private void CheckParametersPerLine(int lineIndex, int i)
    {
        List<string> bulkParams = new ParameterParser().GetParameters(_lines[lineIndex][(i + 1)..]).ToList();
        foreach (string param in bulkParams)
        {
            if (param.Contains("=>")) return;
            List<string> splitParams = param.Split(' ').ToList();
            string parameterName = splitParams[^1].Trim(')');
            if (splitParams.Count >= 2 && !char.IsLower(parameterName[0]) && parameterName != "=>")
                _report.Warnings.Add($"Misnamed parameter in {_filename}: {parameterName}");
        }
    }

    private void FindTypingErrors(string line)
    {
        if (line.StartsWith("(")) return; // cannot handle tuples yet
        int endIndex = GetPartUntilStringStartIfAny(line);
        if (endIndex < 1) return;
        List<string> lineElements = line[..endIndex].Split(' ').ToList();
        int assignIndex = lineElements.FindIndex(le => le == "=");
        if (!ReservedWord(lineElements[0]) && assignIndex == 2 && !char.IsLower(lineElements[1][0]))
            _report.Warnings.Add($"Wrong identifier case: {_filename}: {lineElements[1]}.");
    }

    private bool ReservedWord(string word)
    {
        return word == "else";
    }

    private static int GetPartUntilStringStartIfAny(string line)
    {
        int endIndex = line.Length;
        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == '"') // prevent SQL strings problems
            {
                endIndex = i - 1;
                break;
            }
        }

        return endIndex;
    }
}

internal class ParameterParser
{
    private int _depth = 0;
    private int _startOfParam = 0;

    // against (Action<string, Dictionary<string, string>> callback)
    internal IEnumerable<string> GetParameters(string v)
    {
        for (int i = 0; i < v.Length; i++)
        {
            char ch = v[i];
            _depth = UpdateDepth(_depth, ch);
            if (_depth == -1) break;
            if (ch == ',' && _depth == 0)
            {
                yield return v[_startOfParam..i];
                _startOfParam = i + 1;
            }
        }
        yield return v[_startOfParam..];
    }

    private static int UpdateDepth(int depth, char ch)
    {
        if (ch == '<' || ch == '(') depth++;
        if (ch == '>' || ch == ')') depth--;
        return depth;
    }
}