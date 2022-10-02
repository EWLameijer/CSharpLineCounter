namespace CodeAnalysis;

internal class FileCharacteristics
{
    public int MethodLevel { get; init; }

    public FileCharacteristics(ClearedLines clearedLines)
    {
        IReadOnlyList<string> lines = clearedLines.Lines;
        bool isTopLevelStatement = !lines.Any(line => line.StartsWith("namespace"));
        if (isTopLevelStatement) MethodLevel = 1;
        else
        {
            bool isFileScoped = false;
            foreach (string line in lines)
            {
                if (line.StartsWith("namespace") && line.EndsWith(";")) isFileScoped = true;
            }
            MethodLevel = isFileScoped ? 2 : 3;
        }
    }
}