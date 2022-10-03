using CodeAnalysis;

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
        WarningRepo warningRepo = new();
        ClearedLines clearedLines = new CommentLineAnalyzer(false, warningRepo).GetRegularCode(lines);
        IdentifierAnalyzer sut = new("", clearedLines, warningRepo);

        // act
        sut.Analyze();

        // assert
        Assert.Empty(warningRepo.Warnings);
    }

    [Fact]
    public void MethodLengthAnalyzer_should_report_long_unittests_too()
    {
        // arrange
        List<string> lines = Test1.Split("\n").Select(line => line.Trim()).ToList();
        WarningRepo warningRepo = new();
        ClearedLines clearedLines = new CommentLineAnalyzer(false, warningRepo).GetRegularCode(lines);
        MethodLengthAnalyzer sut = new("", clearedLines);

        // act
        sut.Analyze();

        // assert
        Assert.Single(warningRepo.Warnings);
    }

    private const string ParameterCheck = @"
namespace LineCounter;

internal static class FileProcessor
{
    public static LineReport Process(string Filepath)
    {
    }
}";

    // TEST: parameter-name-check seems broken?
    [Fact]
    public void Parameter_naming_violations_should_be_reported()
    {
        // arrange
        List<string> lines = ParameterCheck.Split("\n").Select(line => line.Trim()).ToList();
        WarningRepo warningRepo = new();
        ClearedLines clearedLines = new CommentLineAnalyzer(false, warningRepo).GetRegularCode(lines);
        IdentifierAnalyzer sut = new("", clearedLines, warningRepo);

        // act
        sut.Analyze();

        // assert
        Assert.Single(warningRepo.Warnings);
    }

}