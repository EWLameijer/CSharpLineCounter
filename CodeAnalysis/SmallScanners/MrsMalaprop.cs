using LineCounter;

namespace CodeAnalysis.SmallScanners;

public class MrsMalaprop
{
    private readonly string _filename;
    private readonly IReadOnlyList<string> _lines;

    public MrsMalaprop(string filename, ClearedLines clearedLines)
    {
        _filename = filename;
        _lines = clearedLines.Lines;
    }

    public void Analyze()
    {
        List<string> malapropisms = new() { "String", "Decimal", "Char", "Int32", "Double" };
        foreach (string line in _lines)
        {
            foreach (string malaprop in malapropisms)
            {
                int index = line.IndexOf(malaprop);
                if (index >= 0 && ProperBoundaries(line, malaprop, index))
                    WarningRepo.Warnings.Add(
                            $"In {_filename} use regular type instead of '{malaprop}'");
            }
        }
    }

    private bool ProperBoundaries(string line, string malaprop, int index)
    {
        bool startBoundary = StartBoundary(line, index);
        bool endBoundary = EndBoundary(line, malaprop, index);
        return startBoundary && endBoundary;
    }

    private static bool StartBoundary(string line, int index)
    {
        bool startBoundary = true;
        int prePosition = index - 1;
        if (prePosition >= 0)
        {
            char preCh = line[prePosition];
            if (char.IsLetter(preCh) || preCh == '"') startBoundary = false;
        }

        return startBoundary;
    }

    private static bool EndBoundary(string line, string malaprop, int index)
    {
        bool endBoundary = true;
        int postPosition = index + malaprop.Length;
        if (postPosition < line.Length)
        {
            char postCh = line[postPosition];
            if (char.IsLetter(line[postPosition]) || postCh == '"') endBoundary = false;
        }

        return endBoundary;
    }
}