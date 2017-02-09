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

public class ParseException : System.Exception {
  public ParseException() : base() { }
  public ParseException(string message) : base(message) { }
  public ParseException(string message, System.Exception inner) : base(message, inner) { }
}

public class Parsable {

  // the parsable text
  private string text = "";

  public int position { get; set; }

  // returns the part of the text that still needs parsing
  private string head {
    get {
      return this.text.Substring(this.position);
    }
  }
  
  // helper Regular Expressions for general clean-up
  private static Regex leadingWhitespace = new Regex( "^\\s" );

  public Parsable(string text) {
    this.text = text;
  }

  public string Context {
    get {
      return (this.Peek(30) + "[...]").Replace("\n", "\\n");
    }
  }

  // skips any whitespace at the start of the current text buffer
  public void SkipLeadingWhitespace() {
    while(Parsable.leadingWhitespace.Match(this.head).Success) {
      this.Consume(1);
    }
  }

  // tries to consume a give string
  public string Consume(string text) {
    this.SkipLeadingWhitespace();
    if(! this.head.StartsWith(text) ) {
      this.Log("Consume(" + text + ") FAILED");
      throw new ParseException(
        "could not consume '" + text + "' at " + this.Context
      );
    }
    // do actual consumption
    this.Log("Consume(" + text + ") SUCCESS");
    return this.Consume(text.Length);
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
      this.position -= m.Groups["keep"].Length;
      // temp solution for regexps with two groups of which only one "captures"
      try {
        return m.Groups[1].Value; // only selected part
      } catch {
        return m.Groups[2].Value; // only selected part
      }
    } else {
      this.Log("Consume(" + pattern.ToString() + ") FAILED ");
    }
    throw new ParseException( "could not consume pattern " + pattern.ToString() + " at " + this.Context );    
  }

  // returns an amount of characters, without consuming, not trimming!
  public string Peek(int amount) {
    return this.head.Substring(0, Math.Min(amount, this.head.Length));
  }

  public ParseException GenerateParseException(string message) {
    return new ParseException( message + " at " + this.Context );
  }

  public ParseException GenerateParseException(string message, Exception inner) {
    return new ParseException( message + "\n  Context:" + this.Context + "\n", inner );
  }

  public bool IsDone {
    get {
      this.SkipLeadingWhitespace();
      return this.position == this.text.Length;
    }
  }

  // ACTUAL CONSUMPTION

  // consumes an amount of characters
  private string Consume(int amount) {
    amount = Math.Min(amount, this.head.Length);

    // extract
    string consumed = this.head.Substring(0, amount);

    // drop
    this.position += amount;

    return consumed;
  }
  
  [ConditionalAttribute("DEBUG")]
  private void Log(string msg) {
    Console.Error.WriteLine("!!! " + msg + " @ " + this.Context);
  }
}
