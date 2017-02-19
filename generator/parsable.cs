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

public class ParseException : System.Exception {
  public int Position     { get; set; }
  public int Line         { get; set; }
  public int LinePosition { get; set; }
  public int MaxPosition  {
    get {
      return (this.InnerException != null) ?
        ((ParseException)this.InnerException).MaxPosition : this.Position;
    }
  }
  public ParseException() : base() { }
  public ParseException(string message) : base(message) { }
  public ParseException(string message, System.Exception inner) : base(message, inner) { }
}

public class Parsable {

  // the parsable text
  private string _text;
  private string text {
    set {
      this._text = value;
      // keep list of cummulative line-lengths for easy mapping position to line
      int sum = 0;
      foreach(var line in value.Split('\n')) {
        sum += line.Length + 1;
        this.lines.Add(new Tuple<string,int>(line, sum));
      }
    }
    get {
      return this._text;
    }
  }

  private List<Tuple<string,int>> lines = new List<Tuple<string,int>>();

  public int Position { get; set; }

  public int Line {
    get { return this.LineOf(this.Position); }
  }

  public int LinePosition {
    get {
      return this.Position -
        (this.Line > 0 ? this.lines[this.Line - 1].Item2 : 0);
    }
  }

  public int LineOf(int position) {
    var index = this.lines.Select(x => x.Item2).ToList().BinarySearch(position);
    if(index < 0 ) {      // the position wasn't found, e.g. not start of line
      index = ~index;
    }
    return index;
  }

  public string this[int index] {
    get {
      return this.lines[index].Item1;
    }
  }

  // returns the part of the text that still needs parsing
  private string head {
    get { return this.text.Substring(this.Position); }
  }
  
  // helper Regular Expressions for general clean-up
  // this regular expression only matches NOT printable characters minus space
  // ASCII range goes from 0-127
  // 0-31 are non printable (including newline, carriage return, EOF,...)
  // 127 is DEL is another non printable
  // 32 is space is normal whitespace we also want to ignore
  private static Regex leadingWhitespace = new Regex( @"^[^\u0021-\u007E]" );

  public Parsable(string text) {
    this.text = text;
  }

  // skips any whitespace at the start of the current text buffer
  public void SkipLeadingWhitespace() {
    while(Parsable.leadingWhitespace.Match(this.head).Success) {
      this.Consume(1);
    }
  }

  // tries to consume a give string
  public bool Consume(string text) {
    this.SkipLeadingWhitespace();
    if(! this.head.StartsWith(text) ) {
      this.Log("Consume(" + text + ") FAILED");
      throw this.GenerateParseException( "Expected '" + text + "'" );
    }
    // do actual consumption
    this.Log("Consume(" + text + ") SUCCESS");
    return this.Consume(text.Length).Equals(text);
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
    this.SkipLeadingWhitespace();
    Match m = pattern.Match(this.head);
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
    throw this.GenerateParseException( "Expected '" + pattern.ToString() + "'" );
  }

  // returns an amount of characters, without consuming, not trimming!
  public string Peek(int amount, int start=0) {
    return this.head.Substring(start, Math.Min(amount, this.head.Length));
  }

  // returns lines that cover [start,end] positions in the source
  public List<string> Context(int start, int end) {
    start = this.LineOf(start);
    end   = this.LineOf(end);
    Console.Error.WriteLine(start + " " + end);
    return this.lines.Select(x=>x.Item1).ToList().GetRange(start, end-start+1);
  }

  public ParseException GenerateParseException(string message, Exception inner=null) {
    // Console.Error.WriteLine("ParseException: " + message + " at " + this.Line + "/" + this.LinePosition + " " + this.Position);
    return new ParseException(message, inner ) {
      Position     = this.Position,
      Line         = this.Line,
      LinePosition = this.LinePosition
    };
  }

  public bool IsDone {
    get {
      this.SkipLeadingWhitespace();
      return this.Position == this.text.Length;
    }
  }

  // ACTUAL CONSUMPTION

  // consumes an amount of characters
  private string Consume(int amount) {
    amount = Math.Min(amount, this.head.Length);

    // extract
    string consumed = this.head.Substring(0, amount);

    // drop
    this.Position += amount;

    return consumed;
  }
  
  [ConditionalAttribute("DEBUG")]
  private void Log(string msg) {
    Console.Error.WriteLine("!!! " + msg + " @ " + this.Position);
  }
}
