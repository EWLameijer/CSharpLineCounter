using CodeAnalysis;

namespace LineCounter;

public class IdentifierAnalyzer
{
    private readonly string _filename;
    private readonly IReadOnlyList<string> _lines;
    private readonly FileCharacteristics _characteristics;

    private int MethodLevel => _characteristics.MethodLevel;

    private int _indentationLevel;

    public IdentifierAnalyzer(string filename, ClearedLines clearedLines)
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
            if (line.StartsWith("{")) _indentationLevel++;
            else if (line.StartsWith("}")) _indentationLevel--;
            else if (_indentationLevel >= MethodLevel) FindTypingErrors(line);
            else if (_indentationLevel > 0) i = ProcessNonMethodLine(i);
        }
    }

    private sealed class CapitalData
    {
        public bool CapitalUsed { get; set; }
        public int LastCapitalIndex { get; set; }
    }

    /* Can you
     * Do this
     // correctly?
    */

    private int ProcessNonMethodLine(int lineIndex)
    {
        (string line, int newLineIndex) = new CommentLineAnalyzer(false).FindFirstNonCommentLine(_lines, lineIndex - 1);
        (bool isMethod, int position) = IsMethod(line);
        if (isMethod) newLineIndex = CheckParameters(newLineIndex, position);

        return newLineIndex;
    }

    private static (bool isMethod, int position) IsMethod(string line)
    {
        CapitalData capitalData = new();
        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];
            UpdateCapitalData(capitalData, i, ch);
            if (ch == '=' || ch == '[') break; // against annotations
            if (ch == '(' && capitalData.CapitalUsed)
            {
                return (true, i);
            }
        }
        return (false, 0);
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
        } while (!IsEndOfMethodHeader(currentLine));

        return lineIndexToTest - 1;
    }

    private static bool IsEndOfMethodHeader(string currentLine) =>
        currentLine.EndsWith(")") || currentLine.EndsWith("=>")
        || currentLine.EndsWith(";") || currentLine.Contains("//");

    private void CheckParametersPerLine(int lineIndex, int i)
    {
        List<string> bulkParams = new ParameterParser().GetParameters(_lines[lineIndex][(i + 1)..]).ToList();
        foreach (string param in bulkParams)
        {
            if (param.Contains("=>")) return;
            List<string> splitParams = param.Split(' ').ToList();
            string parameterName = splitParams[^1].Trim(')');
            if (splitParams.Count >= 2 && !char.IsLower(parameterName[0]) && parameterName != "=>")
                WarningRepo.Warnings.Add($"Misnamed parameter in {_filename}: {parameterName}");
        }
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

    private void FindTypingErrors(string line)
    {
        int endIndex = GetPartUntilStringStartIfAny(line);
        if (endIndex < 1) return;
        List<string> lineElements = line[..endIndex].Split(' ').ToList();
        int assignIndex = lineElements.FindIndex(le => le == "=");
        if (assignIndex == 2 && !char.IsLower(lineElements[1][0]))
            WarningRepo.Warnings.Add($"Wrong identifier case: {_filename}: {lineElements[1]}.");
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