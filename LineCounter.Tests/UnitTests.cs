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

    private const string TooLongMethodError = @"
using System.Text;
using LineCounter;

namespace CodeAnalysis;

public class CommentLineAnalyzer
{
    private int _multiLineCommentLines;
    private int _initCommentLines;
    private readonly List<int> _commentLineIndices = new();
    private readonly LineReport? _report;

    public CommentLineAnalyzer(LineReport? report = null)
    {
        _report = report;
    }

    public (string line, int index) FindFirstNonCommentLine(IReadOnlyList<string> lines, int startIndex)
    {
        int index = startIndex;
        bool isCommentLine = false;
        do
        {
            index++;
            isCommentLine = UpdateCommentLineCount(lines[index], index, isCommentLine);
        } while (isCommentLine);
        return (lines[index], index);
    }

    public (int lineCommCounts, int multiLineCommCounts) CountCommentLines(List<string> lines)
    {
        bool inMultiLineComment = false;
        for (int i = 0; i < lines.Count; i++)
        {
            inMultiLineComment = UpdateCommentLineCount(lines[i], i, inMultiLineComment);
        }
        return (_initCommentLines, _multiLineCommentLines);
    }

    private bool UpdateCommentLineCount(string line, int index, bool status)
    {
        bool newStatus = status;

        if (new LineParser(line).HasActiveMultiLineCommentOpener())
        {
            newStatus = true;
        }

        if (newStatus) RegisterMultilineComment(line, index);
        if (!newStatus && line.StartsWith(""//"")) RegisterLineComment(line, index);

        if (line.EndsWith(""*/""))
        {
            newStatus = false;
        }
        return newStatus;
    }

    private void RegisterLineComment(string line, int index)
    {
        _initCommentLines++;
        _commentLineIndices.Add(index);
        _report?.Comments.Add(line);
    }

    private void RegisterMultilineComment(string line, int index)
    {
        _multiLineCommentLines++;
        _commentLineIndices.Add(index);
        _report?.Comments.Add(line);
    }

    public ClearedLines GetRegularCode(List<string> lines)
    {
        StringBuilder result = new();
        bool inBlockComment = false; // block comment; line comments are immediately skipped
        bool inString = false;
        bool inStringEscape = false;

        foreach (string line in lines)
        {
            int lineLength = line.Length;
            for (int i = 0; i < lineLength; i++)
            {
                char ch = line[i];
                if (inBlockComment)
                {
                    if (ch == '/' && i > 0 && line[i - 1] == '*') inBlockComment = false;
                }
                else if (inString)
                {
                    if (ch == '\\') inStringEscape = !inStringEscape; else inStringEscape = false;
                    if (ch == '""' && !inStringEscape)
                    {
                        inString = false;
                        result.Append('""');
                    }
                }
                else
                {
                    if (ch == '""') inString = true;
                    if (ch == '/' && i + 1 < lineLength)
                    {
                        if (line[i + 1] == '/') break; // line comment
                        if (line[i + 1] == '*') inBlockComment = true;
                    }
                    else result.Append(ch);
                }
            }
            result.Append('\n');
        }
        List<string> currentLines = result.ToString().Split(""\n"").ToList();
        List<string> withoutAnnotations = currentLines.Where(line => !line.StartsWith(""["")).ToList();
        return new ClearedLines { Lines = withoutAnnotations };
    }
}";

    [Fact]
    public void Too_long_methods_should_still_be_called()
    {
        // arrange
        List<string> lines = TooLongMethodError.Split("\n").Select(line => line.Trim()).ToList();
        LineReport report = new(lines);
        ClearedLines clearedLines = new CommentLineAnalyzer().GetRegularCode(lines);
        FileData fileData = new("testfile6.cs", clearedLines);
        MethodLengthAnalyzer sut = new(fileData, report);

        // act
        sut.Analyze();

        // assert
        Assert.Single(report.Warnings);
    }

}