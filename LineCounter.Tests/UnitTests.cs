using CodeAnalysis;
using CodeAnalysis.DTOs;

namespace LineCounter.Tests;

public class UnitTests
{
    private const string Test1 = @"using System.Data.SqlClient;

namespace PhoneServiceTests;

public class Add
{
    [Fact]
    public void Should_AddPhone()
    {
        //arrange
        PhoneService phoneService = new();
        int nrOfPhones = phoneService.Get().Count();

        string testType = ""Test T125532"";
        string testDescription = ""TestTest3295808346720Test"";

        //act
        phoneService.Add(new()
        {
            Id = 0,
            Brand = new()
            {
                Id = 0,
                Name = ""Samsung""
            },
            Type = testType,
            Description = testDescription,
            Price = 198.00m,
            Stock = 1
        });

        //assert
        Assert.Equal(nrOfPhones + 1, phoneService.Get().Count());

        using SqlConnection connection = new(""Data Source=(localdb)\\"" +
            ""MSSQLLocalDB;Initial Catalog=phoneshop;Integrated Security=True;Connect Timeout=30;"" +
            ""Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;"" +
            ""MultiSubnetFailover=False"");
        SqlCommand command = new(
            ""DELETE FROM phones "" +
            $""WHERE [Type] = '{testType}' AND [Description] = '{testDescription}'"",
            connection);

        connection.Open();

        command.ExecuteNonQuery();
    }
}";

    [Fact]
    public void IdentifierDetector_should_not_report_in_string_types()
    {
        // arrange
        List<string> lines = Test1.Split("\n").Select(line => line.Trim()).ToList();
        LineReport report = new(lines);
        ClearedLines clearedLines = new CommentLineAnalyzer(report).GetRegularCode(lines);
        FileData fileData = new("testfile1.cs", clearedLines);
        IdentifierAnalyzer sut = new(fileData, report);

        // act
        sut.Analyze();

        // assert
        Assert.Empty(report.Warnings);
    }

    [Fact]
    public void MethodLengthAnalyzer_should_report_long_unittests_too()
    {
        // arrange
        List<string> lines = Test1.Split("\n").Select(line => line.Trim()).ToList();
        LineReport report = new(lines);
        ClearedLines clearedLines = new CommentLineAnalyzer().GetRegularCode(lines);
        FileData fileData = new("testfile2.cs", clearedLines);
        MethodLengthAnalyzer sut = new(fileData, report);

        // act
        sut.Analyze();

        // assert
        Assert.Single(report.Warnings);
    }

    private const string ParameterCheck = @"
namespace LineCounter;

internal static class FileProcessor
{
    public static LineReport Process(string Filepath)
    {
    }
}";

    [Fact]
    public void Parameter_naming_violations_should_be_reported()
    {
        // arrange
        List<string> lines = ParameterCheck.Split("\n").Select(line => line.Trim()).ToList();
        LineReport report = new(lines);
        ClearedLines clearedLines = new CommentLineAnalyzer().GetRegularCode(lines);
        FileData fileData = new("testfile3.cs", clearedLines);
        IdentifierAnalyzer sut = new(fileData, report);

        // act
        sut.Analyze();

        // assert
        Assert.Single(report.Warnings);
    }

    private const string ProgramCsTest = @"using LineCounter;

Console.Write(""Geef de naam van de directory waarvan je de code-regels wilt tellen: "");
string pathname = Console.ReadLine()!;

List<string> csFiles = Directory.GetFiles(pathname, ""*.cs"", SearchOption.AllDirectories).ToList();
csFiles.ForEach(Console.WriteLine);
Console.WriteLine();
IEnumerable<string> relevantFileNames = csFiles.Where(
    fn => !fn.Contains(@""\Debug\"") && !fn.Contains(@""\Migrations\"") && !fn.Contains(@"".Designer.cs""));
List<LineReport> reports = new();
foreach (string relevantFileName in relevantFileNames)
{
    LineReport newReport = FileProcessor.Process(relevantFileName);
    reports.Add(newReport);
}
Reporter.FinalReport(reports);";

    /// Misnamed parameter in Program.cs: ");" 
    [Fact]
    public void Program_cs_should_be_analyzed_properly()
    {
        // arrange
        List<string> lines = ProgramCsTest.Split("\n").Select(line => line.Trim()).ToList();
        LineReport report = new(lines);
        ClearedLines clearedLines = new CommentLineAnalyzer().GetRegularCode(lines);
        FileData fileData = new("testfile4.cs", clearedLines);
        IdentifierAnalyzer sut = new(fileData, report);

        // act
        sut.Analyze();

        // assert
        Assert.Empty(report.Warnings);
    }

    // Invalid field name line.StartsWith("namespace"); in LineReport.cs.
    private const string ExpressionBodiedError = @"
using CodeAnalysis;

namespace LineCounter;

public class LineReport
{
    private bool IsRawStartingLine(string line) =>
        line.StartsWith(""using"") || line.StartsWith(""namespace"");

}";
    [Fact]
    public void Expression_bodied_methods_should_be_analyzed_properly()
    {
        // arrange
        List<string> lines = ExpressionBodiedError.Split("\n").Select(line => line.Trim()).ToList();
        LineReport report = new(lines);
        ClearedLines clearedLines = new CommentLineAnalyzer().GetRegularCode(lines);
        FileData fileData = new("testfile5.cs", clearedLines);
        IdentifierAnalyzer sut = new(fileData, report);

        // act
        sut.Analyze();

        // assert
        Assert.Empty(report.Warnings);
    }

}