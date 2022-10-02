using CodeAnalysis;

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
        List<string> commentLessLines = new CommentLineAnalyzer(false).GetRegularCode(lines);

        IdentifierAnalyzer identifierAnalyzer = new(filename, commentLessLines);
        identifierAnalyzer.Analyze();

        Console.WriteLine("---");
        AnalyzeMethodLength(filename, commentLessLines);
        Console.WriteLine();
        return report;
    }

    private static void AnalyzeMethodLength(string filename, List<string> lines)
    {
        MethodLengthAnalyzer methodLengthAnal = new(filename, lines);
        methodLengthAnal.Analyze();
    }
}