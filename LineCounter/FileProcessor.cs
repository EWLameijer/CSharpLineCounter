namespace LineCounter;

internal static class FileProcessor
{
    public static LineReport Process(string filepath)
    {
        List<string> lines = File.OpenText(filepath).ReadToEnd().
            Split("\n").Select(line => line.Trim()).ToList();
        string filename = Path.GetFileName(filepath);
        Analyzer analyzer = new(filepath);

        AnalyzeMethodLength(filename, lines);

        IdentifierAnalyzer identifierAnalyzer = new(filename, lines);
        identifierAnalyzer.Analyze();
        LineReport report = analyzer.Analyze();
        new Reporter().Report(filename, report);
        Console.WriteLine();
        return report;
    }

    private static void AnalyzeMethodLength(string filename, List<string> lines)
    {
        MethodLengthAnalyzer methodLengthAnal = new(filename, lines);
        methodLengthAnal.Analyze();
    }
}