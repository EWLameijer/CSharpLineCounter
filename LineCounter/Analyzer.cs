namespace LineCounter;
/*this is a comment
*/

internal class Analyzer
{
    private readonly string _filename;

    public Analyzer(string filename)
    {
        _filename = filename;
    }

    internal LineReport Analyze()
    {
        // read lines
        string[] lines = ReadLines();

        // classify lines
        return new LineReport(lines.ToList());
    }

    private string[] ReadLines()
    {
        StreamReader reader = File.OpenText(_filename);
        ///* This is a multiline comment
        // of 2 lines
        // 3 lines
        // of 4 lines*/
        string result = reader.ReadToEnd();
        string[] lines = result.Split('\n');
        return lines;
    }
}