using CodeAnalysis;
using CodeAnalysis.SmallScanners;

namespace LineCounter;

internal static class FileProcessor
{
    public static LineReport Process(string filepath)
    {
        List<string> rawLines = File.OpenText(filepath).ReadToEnd().Split("\n").ToList();
        string filename = Path.GetFileName(filepath);
        ScanLineLengths(filename, rawLines);
        List<string> lines = rawLines.Select(line => line.Trim()).ToList();

        Analyzer analyzer = new(filepath);
        LineReport report = analyzer.Analyze();
        new Reporter().Report(filename, report);

        // Ideally, create a version that strips out comment lines
        ClearedLines clearedLines = new CommentLineAnalyzer(false).GetRegularCode(lines);

        DelayedFeedbackAnalyzers(filename, clearedLines);

        Console.WriteLine("---");
        AnalyzeMethodLength(filename, clearedLines);
        Console.WriteLine();
        return report;
    }

    private static void ScanLineLengths(string filename, List<string> rawLines)
    {
        const int MaxLineLength = 120;
        foreach (string rawLine in rawLines)
        {
            if (rawLine.Length > MaxLineLength)
                WarningRepo.Warnings.Add($"Too long line in {filename}: {rawLine.Trim()}");
        }
    }

    private static void DelayedFeedbackAnalyzers(string filename, ClearedLines clearedLines)
    {
        IdentifierAnalyzer identifierAnalyzer = new(filename, clearedLines);
        identifierAnalyzer.Analyze();

        MrsMalaprop malaprop = new(filename, clearedLines);
        malaprop.Analyze();
    }

    private static void AnalyzeMethodLength(string filename, ClearedLines clearedLines)
    {
        MethodLengthAnalyzer methodLengthAnal = new(filename, clearedLines);
        methodLengthAnal.Analyze();
    }
}