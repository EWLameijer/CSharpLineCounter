using LineCounter;

Console.Write("Geef de naam van de directory waarvan je de code-regels wilt tellen: ");
string pathname = Console.ReadLine()!;

List<string> csFiles = Directory.GetFiles(pathname, "*.cs", SearchOption.AllDirectories).ToList();
csFiles.ForEach(Console.WriteLine);
Console.WriteLine();
IEnumerable<string> relevantFileNames = csFiles.Where(
    fn => !fn.Contains(@"\Debug\") && !fn.Contains(@"\Migrations\") && !fn.Contains(@".Designer.cs"));
List<LineReport> reports = new();
foreach (string relevantFileName in relevantFileNames)
{
    LineReport newReport = FileProcessor.Process(relevantFileName);
    reports.Add(newReport);
}
Reporter.FinalReport(reports);