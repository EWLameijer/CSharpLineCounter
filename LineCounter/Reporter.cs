﻿namespace LineCounter;

public class Reporter
{
    private int totalLines;
    private int totalCommentLines;
    private int blankLines;
    private int braceLines;
    private int openingLines;
    private int codeLines;

    public void Report(string title, params LineReport[] reports)
    {
        AnalyzeData(reports);
        ReportData(title);
    }

    private void AnalyzeData(LineReport[] reports)
    {
        totalLines = reports.Sum(r => r.TotalLines);
        totalCommentLines = reports.Sum(r => r.TotalCommentLines);
        int nonBlankLines = reports.Sum(r => r.NonBlankLines);
        blankLines = totalLines - nonBlankLines;
        braceLines = reports.Sum(r => r.BraceLines);
        openingLines = reports.Sum(r => r.OpeningLines);
        codeLines = reports.Sum(r => r.CodeLines);
    }

    private void ReportData(string title)
    {
        Console.WriteLine(title);
        Console.WriteLine($"Total lines:         {totalLines}");
        Console.WriteLine($"Using dir+namespace: {openingLines}");
        Console.WriteLine($"Lines of comments:   {totalCommentLines}");
        Console.WriteLine($"Blank lines:         {blankLines}");
        Console.WriteLine($"Lines with braces:   {braceLines}");
        Console.WriteLine($"Lines of code:       {codeLines}");
    }
}