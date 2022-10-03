namespace LineCounter;

public class Reporter
{
    private int _totalLines;
    private int _totalCommentLines;
    private int _blankLines;
    private int _braceLines;
    private int _openingLines;
    private int _codeLines;

    public void Report(string title, params LineReport[] reports)
    {
        AnalyzeData(reports);
        ReportData(title);
    }

    private void AnalyzeData(LineReport[] reports)
    {
        _totalLines = reports.Sum(r => r.TotalLines);
        _totalCommentLines = reports.Sum(r => r.TotalCommentLines);
        int nonBlankLines = reports.Sum(r => r.NonBlankLines);
        _blankLines = _totalLines - nonBlankLines;
        _braceLines = reports.Sum(r => r.BraceLines);
        _openingLines = reports.Sum(r => r.OpeningLines);
        _codeLines = reports.Sum(r => r.CodeLines);
    }

    private void ReportData(string title)
    {
        Console.WriteLine("**" + title);
        Console.WriteLine($"Total lines:         {_totalLines}");
        Console.WriteLine($"Using dir+namespace: {_openingLines}");
        Console.WriteLine($"Lines of comments:   {_totalCommentLines}");
        Console.WriteLine($"Blank lines:         {_blankLines}");
        Console.WriteLine($"Lines with braces:   {_braceLines}");
        Console.WriteLine($"Lines of code:       {_codeLines}");
    }

    public static void FinalReport(List<LineReport> reports)
    {
        Console.WriteLine();
        new Reporter().Report("TOTAL:", reports.ToArray());
        Console.WriteLine();
        Console.WriteLine("Comments - check for commented-out code!");
        List<string> allComments = reports.SelectMany(r => r.Comments).ToList();
        foreach (string line in allComments) Console.WriteLine(line);
        Console.WriteLine();
        Console.WriteLine("WARNINGS: please check and possibly address these!");
        List<string> allWarnings = reports.SelectMany(r => r.Warnings).ToList();
        foreach (string line in allWarnings) Console.WriteLine(line);
        Console.WriteLine($"--END OF WARNINGS: total is {allWarnings.Count} warnings.");
    }
}