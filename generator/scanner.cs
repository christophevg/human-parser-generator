// Parsable - a text wrapping class with helper functions and behaviour for
//            easier construction of (manually) crafted parsers.
// author: Christophe VG <contact@christophe.vg>
// revised by: Adam Simon <adamosimoni@gmail.com> 

// implicit behaviour:
// - ignores whitespace

using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

public class ParseError
{
    readonly Scanner _scanner;
    readonly Lazy<string> _message;

    public ParseError(Scanner scanner, int position)
        : this(scanner, position, null) { }

    public ParseError(Scanner scanner, int position, Func<string> messageFactory)
    {
        _scanner = scanner;
        Position = position;
        _message = new Lazy<string>(messageFactory, isThreadSafe: false);
    }

    public string Message => _message.Value;
    public int Position { get; }
    public int Line => _scanner.LineIndexOf(Position);
    public int LinePosition => _scanner.LinePositionOf(Position);

    public override string ToString()
    {
        return $"{Message} at line {Line + 1},{LinePosition + 1}";
    }
}

public class Scanner
{
    static readonly int newLineLength = Environment.NewLine.Length;

    readonly Lazy<(string[] Lines, int[] EndPositions)> _lineInfo;

    public Scanner(string text)
    {
        if (text == null)
            throw new ArgumentNullException(nameof(text));

        Text = text;
        Length = text.Length;
        _lineInfo = new Lazy<(string[], int[])>(() =>
        {
            var lines = text.Split(Environment.NewLine);
            var n = lines.Length;
            var endPositions = new int[n];
            var sum = 0;
            for (var i = 0; i < n; i++)
            {
                sum += lines[i].Length;
                endPositions[i] = sum + 1;
                sum += newLineLength;
            }
            return (lines, endPositions);
        }, isThreadSafe: false);
    }

    // the parsable text
    public string Text { get; }

    public int Length { get; }

    public int Position { get; set; }

    public int RemainingLength => Length - Position;

    public string Remaining => Text.Substring(Position, RemainingLength);

    public string Context => Text.Substring(Position, Math.Min(30, RemainingLength));

    public bool IsConsumed
    {
        get
        {
            Skip();
            return RemainingLength == 0;
        }
    }

    public char Head => Text[Position];

    // support for accessing "lines" in the parsable text
    public string this[int index] => _lineInfo.Value.Lines[index];

    public int Line => LineIndexOf(Position);

    public int LinePosition => LinePositionOf(Position);

    public int LineIndexOf(int position)
    {
        var index = Array.BinarySearch(_lineInfo.Value.EndPositions, position);
        if (index < 0)
            // the position wasn't found, e.g. not start of line
            index = ~index;

        return index;
    }

    public int LinePositionOf(int position)
    {
        var lineIndex = LineIndexOf(position);
        return position - (lineIndex > 0 ? _lineInfo.Value.EndPositions[lineIndex - 1] : 0);
    }

    // support for defining an to-be-ignored pattern

    Regex _ignored;
    public Scanner Ignore(Regex pattern)
    {
        _ignored = pattern;
        return this;
    }

    public Scanner Ignore(string pattern)
    {
        return Ignore(new Regex(pattern));
    }

    // skips any whitespace at the start of the current text buffer
    public void SkipLeadingWhitespace()
    {
        while (RemainingLength > 0 && char.IsWhiteSpace(Head))
            Consume(1);
    }

    public bool SkipIgnored()
    {
        if (_ignored == null)
            return false;

        var match = _ignored.Match(Text, Position, RemainingLength);
        if (!match.Success)
            return false;

        Consume(match.Length);
        return true;
    }

    public void Skip()
    {
        do { SkipLeadingWhitespace(); }
        while (SkipIgnored());
    }

    // tries to consume a given string
    public ParseError TryConsume(string text)
    {
        Skip();
        if (RemainingLength < text.Length ||
            string.Compare(Text, Position, text, 0, text.Length) != 0)
        {
            Log($"Consume({text}) FAILED");
            return CreateError(() => $"Expected: '{text}'");
        }
        // do actual consumption
        Log($"Consume({text}) SUCCESS");
        Consume(text.Length);
        return null;
    }

    public ParseError TryConsume(Regex pattern, out string value)
    {
        Skip();
        var match = pattern.Match(Text, Position, RemainingLength);
        if (!match.Success)
        {
            Log($"Consume({pattern}) FAILED");
            value = default(string);
            return CreateError(() => $"Expected: '{pattern}'"); ;
        }

        var length = match.Length; // total match
        Consume(length);

        // we "re-add" what is marked "to-keep"
        // TODO make more generic, for now, we rewind the length, expecting keep
        //      to be at the end
        this.Position -= match.Groups["keep"].Length;

        var extractedValue = GetExtractedGroup(match).Value;

        Log($"Consume({pattern}) SUCCESS ({extractedValue})");
        value = extractedValue;
        return null;

        Group GetExtractedGroup(Match m)
        {
            Group group;
            var n = m.Groups.Count;
            if (n > 1)
                for (var i = 1; i < n; i++)
                    if ((group = match.Groups[i]).Success)
                        return group;

            return m.Groups[0];
        }
    }

    // ACTUAL CONSUMPTION

    // consumes an amount of characters, simply move forward the position
    void Consume(int amount)
    {
        Position += Math.Min(amount, RemainingLength);
    }

    public ParseError CreateError(Func<string> messageFactory)
    {
        return new ParseError(this, Position, messageFactory);
    }

    [Conditional("DEBUG")]
    private void Log(string msg)
    {
#if NETCOREAPP2_0
        Debug.WriteLine(msg + " @ " + Context.Replace(Environment.NewLine, "\\n"));
#else
        Console.Error.WriteLine(msg + " @ " + Context.Replace(Environment.NewLine, "\\n"));
#endif
    }
}
