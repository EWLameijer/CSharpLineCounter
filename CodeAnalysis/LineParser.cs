namespace LineCounter;

public class LineParser
{
    private bool _isInString = false;
    private bool _isInEscape = false;
    private readonly string _line;

    public LineParser(string line) => _line = line;

    public bool HasActiveMultiLineCommentOpener()
    {
        for (int i = 0; i < _line.Length; i++)
        {
            char ch = _line[i];
            bool? returnValue = MultiLineCommentStartsAfter(i);
            if (returnValue != null) return (bool)returnValue;
            if (ch == '/' && _isInString) _isInEscape = !_isInEscape;
            if (ch == '"' && !_isInEscape) _isInString = !_isInString;
            else if (_isInString && _isInEscape) _isInEscape = false;
        }
        return false;
    }

    private bool? MultiLineCommentStartsAfter(int i)
    {
        char ch = _line[i];
        if (ch == '/' && !_isInString && i + 1 < _line.Length)
        {
            char nextChar = _line[i + 1];
            if (nextChar == '/') return false;
            if (nextChar == '*') return true;
        }
        return null;
    }
}