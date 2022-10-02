﻿using CodeAnalysis;
using CodeAnalysis.SmallScanners;

namespace LineCounter;

internal static class FileProcessor
{
    public static LineReport Process(string filepath)
    {
        List<string> lines = File.OpenText(filepath).ReadToEnd().
            Split("\n").Select(line => line.Trim()).ToList();
        string filename = Path.GetFileName(filepath);
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