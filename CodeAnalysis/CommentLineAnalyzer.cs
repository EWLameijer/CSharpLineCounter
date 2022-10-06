using System.Text;
using LineCounter;

namespace CodeAnalysis;

public class CommentLineAnalyzer
{
    private int _multiLineCommentLines;

    private int _initCommentLines;

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
            isCommentLine = UpdateCommentLineCount(lines[index], isCommentLine);
        } while (isCommentLine);

        return (lines[index], index);
    }

    public (int lineCommCounts, int multiLineCommCounts) CountCommentLines(List<string> lines)
    {
        bool inMultiLineComment = false;
        for (int i = 0; i < lines.Count; i++)
        {
            inMultiLineComment = UpdateCommentLineCount(lines[i], inMultiLineComment);
        }

        return (_initCommentLines, _multiLineCommentLines);
    }

    private bool UpdateCommentLineCount(string line, bool status)
    {
        bool newStatus = status || new LineParser(line).HasActiveMultiLineCommentOpener();

        if (newStatus) RegisterMultilineComment(line);
        if (!newStatus && line.StartsWith("//")) RegisterLineComment(line);

        if (line.EndsWith("*/"))
        {
            newStatus = false;
        }
        return newStatus;
    }

    private void RegisterLineComment(string line)
    {
        _initCommentLines++;
        _report?.Comments.Add(line);
    }

    private void RegisterMultilineComment(string line)
    {
        _multiLineCommentLines++;
        _report?.Comments.Add(line);
    }

    public ClearedLines GetRegularCode(List<string> lines)
    {
        List<string> annotationLessLines = lines.Where(line => !IsAnnotation(line)).ToList();
        return new CommentAndStringRemover(annotationLessLines).Parse();
    }

    private bool IsAnnotation(string line)
    {
        if (line.Length < 2) return false;
        return line[0] == '[' && line[^1] == ']';
    }
}

internal class TextCollector
{
    private readonly StringBuilder _stringBuilder = new();

    public void Add(char ch)
    {
        _stringBuilder.Append(ch);
    }

    public ClearedLines Lines()
    {
        return new ClearedLines { Lines = _stringBuilder.ToString().Split('\n').Select(line => line.Trim()).ToList() };
    }
}

internal class TextProvider
{
    private readonly List<string> _lines;
    private int _lineIndex; // the index of the line I'm reading NOW
    private int _charIndex; // the index of the char I'm GOING to read
    private readonly TextCollector _textCollector;

    public TextProvider(List<string> lines, TextCollector textCollector)
    {
        _lines = lines;
        _lineIndex = 0;
        _charIndex = 0;
        _textCollector = textCollector;
    }

    public char Get()
    {
        if (!HasNext()) throw new EndOfStreamException();
        while (_charIndex == _lines[_lineIndex].Length)
        {
            _lineIndex++;
            _charIndex = 0;
            _textCollector.Add('\n');
        }
        char ch = _lines[_lineIndex][_charIndex];
        _charIndex++;
        return ch;
    }

    public char Peek()
    {
        if (!HasNext()) throw new EndOfStreamException();
        int peekLineIndex = _lineIndex;
        int peekCharIndex = _charIndex;
        while (_charIndex == _lines[_lineIndex].Length)
        {
            peekLineIndex++;
            peekCharIndex = 0;
        }
        return _lines[peekLineIndex][peekCharIndex];
    }

    public void SkipToEndOfLine() => _charIndex = _lines[_lineIndex].Length;

    public bool HasNext()
    {
        if (_charIndex < _lines[_lineIndex].Length) return true;
        int testCharIndex = 0;
        int testLineIndex = _lineIndex;
        do // okay, charIndex = last
        {
            testLineIndex++;
            if (testLineIndex == _lines.Count) return false;
        } while (testCharIndex == _lines[testLineIndex].Length);
        return true;
    }
}

internal class CommentAndStringRemover
{
    private readonly TextProvider _textProvider;
    private readonly TextCollector _textCollector = new();

    public CommentAndStringRemover(List<string> lines)
    {
        _textProvider = new TextProvider(lines, _textCollector);
    }

    public ClearedLines Parse()
    {
        IParserState state = new RegularCodeProcessor();
        do
        {
            state = state.Parse(_textProvider, _textCollector);
        } while (_textProvider.HasNext());
        return _textCollector.Lines();
    }
}

internal interface IParserState
{
    IParserState Parse(TextProvider textProvider, TextCollector textCollector);
}

internal class StringProcessor : IParserState
{
    private bool _isInEscape = false;

    public IParserState Parse(TextProvider textProvider, TextCollector textCollector)
    {
        char currentChar = textProvider.Get();
        if (currentChar == '\\') _isInEscape = !_isInEscape;
        else if (currentChar == '"' && !_isInEscape)
        {
            textCollector.Add(currentChar);
            return new RegularCodeProcessor();
        }
        else _isInEscape = false; // for /n etc
        return this;
    }
}

internal class VerbatimStringProcessor : IParserState
{
    private bool _isInEscape = false;

    public IParserState Parse(TextProvider textProvider, TextCollector textCollector)
    {
        char currentChar = textProvider.Get();
        if (currentChar == '"') _isInEscape = !_isInEscape;
        else if (currentChar == '"' && !_isInEscape)
        {
            textCollector.Add(currentChar);
            return new RegularCodeProcessor();
        }
        else _isInEscape = false;
        return this;
    }
}

internal class CharProcessor : IParserState
{
    private bool _isInEscape = false;

    public IParserState Parse(TextProvider textProvider, TextCollector textCollector)
    {
        char currentChar = textProvider.Get();
        if (currentChar == '\\') _isInEscape = !_isInEscape;
        else if (currentChar == '\'' && !_isInEscape)
        {
            textCollector.Add(currentChar);
            return new RegularCodeProcessor();
        }
        else _isInEscape = false; // for /n etc
        return this;
    }
}

internal class BlockCommentProcessor : IParserState
{
    public IParserState Parse(TextProvider textProvider, TextCollector textCollector)
    {
        char currentChar = textProvider.Get();
        if (currentChar == '*')
        {
            char nextChar = textProvider.Peek();
            if (nextChar == '/')
            {
                _ = textProvider.Get(); // skip the '/'
                return new RegularCodeProcessor();
            }
        }
        return this;
    }
}

internal class RegularCodeProcessor : IParserState
{
    public IParserState Parse(TextProvider textProvider, TextCollector textCollector)
    {
        char currentChar = textProvider.Get();
        IParserState? newState = currentChar switch
        {
            '@' => HandleAt(textProvider, textCollector),
            '"' => HandleDoubleQuotes(textCollector),
            '\'' => HandleSingleQuotes(textCollector),
            '/' => HandleSlash(textProvider),
            _ => null
        };
        if (newState != null) return newState;
        textCollector.Add(currentChar);
        return this;
    }

    private static IParserState HandleSingleQuotes(TextCollector textCollector)
    {
        textCollector.Add('\'');
        return new CharProcessor();
    }

    private static IParserState HandleDoubleQuotes(TextCollector textCollector)
    {
        textCollector.Add('"');
        return new StringProcessor();
    }

    private IParserState? HandleSlash(TextProvider textProvider)
    {
        char nextChar = textProvider.Peek();
        if (nextChar == '/')
        {
            textProvider.SkipToEndOfLine();
            return this;
        }
        if (nextChar == '*')
        {
            return new BlockCommentProcessor();
        }
        return null;
    }

    private IParserState? HandleAt(TextProvider textProvider, TextCollector textCollector)
    {
        char nextChar = textProvider.Peek();
        if (nextChar == '"')
        {
            textCollector.Add(nextChar);
            return new VerbatimStringProcessor();
        }
        if (nextChar == '$')
        {
            return CheckInterPolVerbaString(textProvider, textCollector);
        }
        return null;
    }

    private VerbatimStringProcessor? CheckInterPolVerbaString(TextProvider textProvider, TextCollector textCollector)
    {
        textProvider.Get();
        char nextChar = textProvider.Peek();
        if (nextChar == '"')
        {
            textCollector.Add(nextChar);
            return new VerbatimStringProcessor();
        }
        return null;
    }
}

public class ClearedLines
{
    public IReadOnlyList<string> Lines { get; init; } = null!;
}