namespace CodeAnalysis;

internal class FileCharacteristics
{
    public int MethodLevel { get; private set; }

    public bool IsTopLevelFile { get; private set; }

    public FileCharacteristics(ClearedLines clearedLines)
    {
        IReadOnlyList<string> lines = clearedLines.Lines;
        bool isTopLevelStatement = !lines.Any(line => line.StartsWith("namespace"));
        if (isTopLevelStatement)
        {
            MethodLevel = 1;
            IsTopLevelFile = true;
        }
        else
        {
            AssessNamespaceType(lines);
        }
    }

    private void AssessNamespaceType(IReadOnlyList<string> lines)
    {
        bool isFileScoped = false;
        foreach (string line in lines)
        {
            if (line.StartsWith("namespace") && line.EndsWith(";")) isFileScoped = true;
        }
        MethodLevel = isFileScoped ? 2 : 3;
    }
}