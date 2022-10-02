using CodeAnalysis;

namespace LineCounter;

public class IdentifierAnalyzer
{
    private readonly string _filename;
    private readonly List<string> _lines;
    private readonly FileCharacteristics _characteristics;

    private int MethodLevel => _characteristics.MethodLevel;

    private int _indentationLevel;

    public IdentifierAnalyzer(string filename, List<string> lines)
    {
        _filename = filename;
        _lines = lines;
        _characteristics = new FileCharacteristics(lines);
    }

    public void Analyze()
    {
        for (int i = 0; i < _lines.Count; i++)
        {
            string line = _lines[i];
            if (line.StartsWith("{")) _indentationLevel++;
            else if (line.StartsWith("}")) _indentationLevel--;
            else if (_indentationLevel >= MethodLevel) FindTypingErrors(line);
            else i = ProcessNonMethodLine(i);
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
            if (ch == '=') break;
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
        currentLine.EndsWith(")") || currentLine.EndsWith("=>") || currentLine.EndsWith(";");

    private void CheckParametersPerLine(int lineIndex, int i)
    {
        List<string> bulkParams = _lines[lineIndex][i..].Split(',').ToList();
        foreach (string param in bulkParams)
        {
            if (param.Contains("=>")) return;
            List<string> splitParams = param.Split(' ').ToList();
            string parameterName = splitParams[^1];
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
        List<string> lineElements = line.Split(' ').ToList();
        int assignIndex = lineElements.FindIndex(le => le == "=");
        if (assignIndex == 2 && !char.IsLower(lineElements[1][0]))
            WarningRepo.Warnings.Add($"Wrong identifier case: {_filename}: {lineElements[1]}.");
    }
}