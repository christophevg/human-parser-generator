// Parsable - a text wrapping class with helper functions and behaviour for
//            easier construction of (manually) crafted parsers.
// author: Christophe VG <contact@christophe.vg>

// implicit behaviour:
// - ignores whitespace

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Linq;
using System.CodeDom;
using System.CodeDom.Compiler;

public class ParseException : System.Exception {
  public Parsable Parsable { get; set; }
  public int Position      { get; set; }
  public int Line {
    get { return this.Parsable.LineOf(this.Position); }
  }
  public int LinePosition {
    get { return this.Parsable.LinePositionOf(this.Position); }
  }
  public int MaxPosition  {
    get {
      return (this.InnerException != null) ?
        ((ParseException)this.InnerException).MaxPosition : this.Position;
    }
  }
  public ParseException() : base() { }
  public ParseException(string message) : base(message) { }
  public ParseException(string message, System.Exception inner) :
    base(message, inner) {}

  public override string ToString() {
    return this.Message +
      " at line " + (this.Line + 1) + "/" + (this.LinePosition + 1);
  }
}

public class Parsable {

  // the parsable text
  private char[] _text;
  private string text {
    set {
      // keep list of cummulative line-lengths for easy mapping position to line
      int sum = 0;
      foreach(var line in value.Split('\n')) {
        sum += line.Length + 1;
        this.lines.Add(new Tuple<string,int>(line, sum));
      }
      // store string internally as a char[]
      this._text    = value.ToCharArray();
      this.Length   = value.Length;   // cache length
      this.Position = 0;              // start at the beginning
    }
  }

  public int Length { get; private set; }

  public int Position { get; set; }

  public int RemainingLength { get { return this.Length - this.Position; } }
  
  public string Remaining {
    get { return new string(this._text, this.Position, this.RemainingLength); }
  }

  public bool IsDone {
    get {
      this.Skip();
      return this.RemainingLength == 0;
    }
  }
  
  public char Head { get { return this._text[this.Position]; } }
  
  // support for accessing "lines" in the parsable text
  private List<Tuple<string,int>> lines = new List<Tuple<string,int>>();
  public string this[int index] { get {  return this.lines[index].Item1; } }
  public int Line { get { return this.LineOf(this.Position); } }
  public int LinePosition { get { return this.LinePositionOf(this.Position); } }
  public int LineOf(int position) {
    var index = this.lines.Select(x => x.Item2).ToList().BinarySearch(position);
    if(index < 0 ) {      // the position wasn't found, e.g. not start of line
      index = ~index;
    }
    return index;
  }
  public int LinePositionOf(int position) {
    return position - (this.LineOf(position) > 0 ?
      this.lines[this.LineOf(position) - 1].Item2 : 0
    );
  }

  // constructors + support for defining an to-be-ignored pattern

  public Parsable(string text) {
    this.text = text;
  }

  public Parsable Ignore(string pattern) {
    return this.Ignore(new Regex(pattern));
  }

  private Regex ignored = null;

  public Parsable Ignore(Regex pattern) {
    this.ignored = pattern;
    return this;
  }

  // skips any whitespace at the start of the current text buffer
  // whitespace is considered "all NOT printable characters AND space"
  // ASCII range goes from 0-127
  // 0-31 are non printable (including newline, carriage return, EOF,...)
  // 127 is DEL is another non printable
  // 32 is space is normal whitespace we also want to ignore
  public void SkipLeadingWhitespace() {
    while( this.RemainingLength > 0 &&
          (this.Head <= 32 || this.Head == 127))
    {
      this.Consume(1);
    }
  }

  public bool SkipIgnored() {
    if(this.ignored == null) { return false; }
    Match match = this.ignored.Match(this.Remaining);
    if(match.Success) {
      this.Consume(match.Length);
      return true;
    }
    return false;
  }

  public void Skip() {
    do { this.SkipLeadingWhitespace(); } while( this.SkipIgnored() );
  }

  // tries to consume a give string
  public bool Consume(string text) {
    this.Skip();
    if(this.RemainingLength < text.Length ||
       ! new string(this._text, this.Position, text.Length).Equals(text) )
    {
      this.Log("Consume(" + text + ") FAILED");
      throw this.GenerateParseException( "Expected '" + text + "'" );
    }
    // do actual consumption
    this.Log("Consume(" + text + ") SUCCESS");
    this.Consume(text.Length);
    return true;
  }

  public bool TryConsume(string text) {
    int pos = this.Position;   // begin
    try {
      this.Consume(text);
    } catch(ParseException) {
      this.Position = pos;     // rollback
      return false;
    }
    return true;               // commit
  }
  
  public string Consume(Regex pattern) {
    this.Skip();
    Match m = pattern.Match(this.Remaining);
    if(m.Success) {
      this.Log("Consume(" + pattern.ToString() + ") SUCCESS ");
      int length = m.Groups[0].Captures[0].ToString().Length; // total match
      this.Consume(length);
      // we "re-add" what is marked "to-keep"
      // TODO make more generic, for now, we rewind the length, expecting keep
      //      to be at the end
      this.Position -= m.Groups["keep"].Length;
      // temp solution for regexps with two groups of which only one "captures"
      try {
        return m.Groups[1].Value; // only selected part
      } catch {
        return m.Groups[2].Value; // only selected part
      }
    } else {
      this.Log("Consume(" + pattern.ToString() + ") FAILED ");
    }
    throw this.GenerateParseException( "Expected :" + pattern.ToString() );
  }

  // returns an amount of characters (aka string), without consuming, untrimmed!
  public string Peek(int amount, int start=0) {
    return new string(this._text, start, Math.Min(amount, this.RemainingLength));
  }

  public ParseException GenerateParseException(string message,
                                               Exception inner=null)
  {
    return new ParseException(message, inner) {
      Parsable = this,
      Position = this.Position
    };
  }

  // ACTUAL CONSUMPTION

  // consumes an amount of characters, simply move forward the position
  private void Consume(int amount) {
    this.Position += Math.Min(amount, this.RemainingLength);
  }
  
  [ConditionalAttribute("DEBUG")]
  private void Log(string msg) {
    Console.Error.WriteLine(msg + " @ " + this.Peek(10).Replace('\n', 'n'));
  }
}

public abstract class ParserBase<RootType> {
  public RootType AST { get; set; }

  public string Ignoring = null;

  public ParserBase() {}
  public ParserBase(string ignoring) {
    this.Ignoring = ignoring;
  }

  public ParserBase<RootType> Parse(string source) {
    this.Source = new Parsable(source);
    if(this.Ignoring != null) { this.Source.Ignore(this.Ignoring); }
    try {
      this.AST = this.Parse();
    } catch(ParseException e) {
      this.Errors.Add(e);
      throw this.Source.GenerateParseException("Failed to parse.");
    }
    if( ! this.Source.IsDone ) {
      throw this.Source.GenerateParseException("Can't parse remaining data.");
    }
    return this;
  }

  public abstract RootType Parse();

  public Parsable Source { get; protected set; }
  public List<ParseException> Errors = new List<ParseException>();

  [ConditionalAttribute("DEBUG")]
  protected void Log(string msg) {
    Console.Error.WriteLine(
      msg + " @ " + this.Source.Peek(20).Replace('\n', 'n')
    );
  }

  protected bool Consume(string text) {
    return this.Source.Consume(text);
  }

  protected bool MaybeConsume(string text) {
    return this.Source.TryConsume(text);
  }

  protected string Consume(Regex pattern) {
    return this.Source.Consume(pattern);
  }

  protected void Maybe(Action what) {
    int pos = this.Source.Position;
    try {
      what();
    } catch {
      this.Source.Position = pos;
    }
  }

  private bool Success { get; set; }
  private ParseException Exception { get; set; }

  public ParserBase<RootType> Parse(Action what) {
    int pos = this.Source.Position;
    try {
      what();
      this.Success = true;
    } catch(ParseException e) {
      this.Source.Position = pos;
      this.Success = false;
      this.Exception = e;
    }
    return this;
  }

  public ParserBase<RootType> Or(Action what) {
    if( ! this.Success ) {
      return this.Parse(what);
    }
    return this;
  }

  public ParserBase<RootType> OrThrow(string message) {
    if( ! this.Success ) {
      this.Log(message);
      throw this.Source.GenerateParseException(message);
    }
    return this;
  }

  protected List<T> Many<T>(Func<T> what) {
    List<T> list = new List<T>();
    while(true) {
      try {
        list.Add(what());
      } catch(ParseException e) {
        // add the error to the errors list, because we shadow it
        // it still might be the best we've got ;-)
        this.Errors.Add(e);
        break;
      }
    }
    return list;
  }

  protected void Repeat(Action what) {
    while(true) {
      try {
        what();
      } catch(ParseException e) {
        this.Errors.Add(e);
        break;
      }
    }
  }

  public string AllErrorsToString() {
    string report = "";
    foreach(ParseException error in this.Errors) {
      var e = error;
      var indent = "";
      while(e!=null) {
        report += indent + e.Message +
          " at line " + e.Line + "/" + e.LinePosition +
          " (" + e.Position + ")";
        indent += "  ";
        e = e.InnerException as ParseException;
      }
    }
    return report;
  }

  public string BestErrorToString() {
    string report = "";
    // find best top-level exception, the one that parsed the farest
    var best = this.Errors.OrderByDescending(x => x.MaxPosition).First();
    // recurse down the exception tree down to the lowest detail
    report = best.ToString() + "\n";
    var indent = "";
    while(best.InnerException != null) {
      indent += "  ";
      best = best.InnerException as ParseException;
      report += indent + best.ToString() + "\n";
    }
    // dump relevant part of source
    var lineIndex    = best.Line;
    var line         = this.Source[lineIndex];
    var trimmedLine  = line.TrimStart();
    var trimmed      = line.Length - trimmedLine.Length;
    var linePosition = best.LinePosition - trimmed;

    // when positioned at the last char of a line (e.g. \n), show error at
    // beginning of next line
    if(linePosition >= trimmedLine.Length) {
      lineIndex++;
      line = this.Source[lineIndex];
      trimmedLine  = line.TrimStart();
      linePosition = 0;
    }

    report += (lineIndex + 1) + " : " + trimmedLine + "\n";
    report += new System.String(
        '\u2500', 3 + (lineIndex + 1).ToString().Length + linePosition
      ) + "\u256f";
    
    return report;
  }
}

// TODO merge with Emitter.Format.CSharp
public class Format {
  // via http://stackoverflow.com/questions/323640
  internal static string Literal(string input) {
    using( var writer = new StringWriter() ) {
      using( var provider = CodeDomProvider.CreateProvider("CSharp") ) {
        provider.GenerateCodeFromExpression(
          new CodePrimitiveExpression(input), writer, null
        );
        return writer.ToString();
      }
    }
  }
}
