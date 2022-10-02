namespace LineCounter;

internal static class FileProcessor
{
    public static LineReport Process(string filename)
    {
        Analyzer analyzer = new(filename);
        MethodLengthAnalyzer methodLengthAnal = new(filename);
        methodLengthAnal.Analyze();
        LineReport report = analyzer.Analyze();
        new Reporter().Report(filename, report);
        Console.WriteLine();
        return report;
    }
}