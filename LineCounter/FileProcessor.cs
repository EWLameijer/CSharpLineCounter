using CodeAnalysis;
using CodeAnalysis.DTOs;
using CodeAnalysis.SmallScanners;

namespace LineCounter;

internal static class FileProcessor
{
    public static LineReport Process(string filepath)
    {
        List<string> rawLines = File.OpenText(filepath).ReadToEnd().Split("\n").ToList();
        string filename = Path.GetFileName(filepath);

        List<string> lines = rawLines.Select(line => line.Trim()).ToList();
        Analyzer analyzer = new(filepath);
        LineReport report = analyzer.Analyze();
        ScanLineLengths(filename, rawLines, report);

        new Reporter().Report(filename, report);

        // Ideally, create a version that strips out comment lines

        PerformAdvancedAnalysis(filename, lines, report);
        return report;
    }

    private static void PerformAdvancedAnalysis(string filename, List<string> lines, LineReport report)
    {
        ClearedLines clearedLines = new CommentLineAnalyzer(report).GetRegularCode(lines);
        FileData fileData = new(filename, clearedLines);
        DelayedFeedbackAnalyzers(fileData, report);
        Console.WriteLine("---");
        AnalyzeMethodLength(fileData, report);
        Console.WriteLine();
    }

    private static void ScanLineLengths(string filename, List<string> rawLines, LineReport report)
    {
        const int MaxLineLength = 120;
        foreach (string rawLine in rawLines)
        {
            if (rawLine.Length > MaxLineLength)
                report.Warnings.Add($"Too long line in {filename}: {rawLine.Trim()}");
        }
    }

    private static void DelayedFeedbackAnalyzers(FileData fileData, LineReport report)
    {
        IdentifierAnalyzer identifierAnalyzer = new(fileData, report);
        identifierAnalyzer.Analyze();

        MrsMalaprop malaprop = new(fileData, report);
        malaprop.Analyze();
    }

    private static void AnalyzeMethodLength(FileData fileData, LineReport report)
    {
        MethodLengthAnalyzer methodLengthAnal = new(fileData, report);
        methodLengthAnal.Analyze();
    }
}