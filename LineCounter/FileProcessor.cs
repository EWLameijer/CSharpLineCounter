using CodeAnalysis;
using CodeAnalysis.SmallScanners;

namespace LineCounter;

internal static class FileProcessor
{
    public static LineReport Process(string filepath)
    {
        List<string> rawLines = File.OpenText(filepath).ReadToEnd().Split("\n").ToList();
        string filename = Path.GetFileName(filepath);
        WarningRepo warningRepo = new();
        ScanLineLengths(filename, rawLines, warningRepo);
        List<string> lines = rawLines.Select(line => line.Trim()).ToList();

        Analyzer analyzer = new(filepath);
        LineReport report = analyzer.Analyze();
        new Reporter().Report(filename, report);

        // Ideally, create a version that strips out comment lines

        ClearedLines clearedLines = new CommentLineAnalyzer(false, warningRepo).GetRegularCode(lines);

        DelayedFeedbackAnalyzers(filename, clearedLines, warningRepo);

        Console.WriteLine("---");
        AnalyzeMethodLength(filename, clearedLines, warningRepo);
        Console.WriteLine();
        return report;
    }

    private static void ScanLineLengths(string filename, List<string> rawLines,
        WarningRepo warningRepo)
    {
        const int MaxLineLength = 120;
        foreach (string rawLine in rawLines)
        {
            if (rawLine.Length > MaxLineLength)
                warningRepo.Warnings.Add($"Too long line in {filename}: {rawLine.Trim()}");
        }
    }

    private static void DelayedFeedbackAnalyzers(string filename, ClearedLines clearedLines,
        WarningRepo warningRepo)
    {
        IdentifierAnalyzer identifierAnalyzer = new(filename, clearedLines, warningRepo);
        identifierAnalyzer.Analyze();

        MrsMalaprop malaprop = new(filename, clearedLines, warningRepo);
        malaprop.Analyze();
    }

    private static void AnalyzeMethodLength(string filename, ClearedLines clearedLines,
        WarningRepo warningRepo)
    {
        MethodLengthAnalyzer methodLengthAnal = new(filename, clearedLines, warningRepo);
        methodLengthAnal.Analyze();
    }
}