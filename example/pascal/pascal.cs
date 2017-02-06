// parser.cs

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Diagnostics;

  
public class Program {
  public string           Identifier  { get; set; }
  public List<Assignment> Assignments { get; set; }
  public Program() {
    this.Assignments = new List<Assignment>();
  }
  public override string ToString() {
    return
      "Program(" +
        "Identifier=" + this.Identifier + "," +
        "Assignments=" + "[" +
          string.Join(",",this.Assignments.Select(x => x.ToString())) +
        "]" +
      ")";
  }
}

public class Assignment {
  public string Identifier { get; set; }
  public string Expression   { get; set; }
  public override string ToString() {
    return 
      "Assignment(" +
        "Identifier=" + this.Identifier + "," +
        "Expression=" + this.Expression +
      ")";
  }
}

public interface Expression {}

public class Extracting {
  public static Regex Identifier = new Regex( @"^([A-Z][A-Z0-9]*)" );
  public static Regex String = new Regex( @"^""([^""]*)""|'([^']*)'" );
  public static Regex Number = new Regex( @"^(-?[1-9][0-9]*)" );
}

public class Parser {
  private Parsable source;
  public Program AST { get; set; }

  public Parser Parse(string source) {
    this.source = new Parsable(source);
    this.AST    = this.ParseProgram();
    return this;
  }
  
  public Program ParseProgram() {
    string identifier = null;
    List<Assignment> assignments = new List<Assignment>();
    
    this.Log("ParseProgram");
    int pos = this.source.position;
    try {

      this.source.Consume("PROGRAM");

      identifier = this.source.Consume(Extracting.Identifier);

      this.source.Consume("BEGIN");

      {
      Assignment temp;
      while(true) {
        try {
            temp = this.ParseAssignment();
        } catch(ParseException) {
          break;
        }
        assignments.Add(temp);
      }
      }

      this.source.Consume("END.");

    } catch(ParseException e) {
      this.source.position = pos;
      throw this.source.GenerateParseException(
        "Failed to parse Program.", e
      );
    }

    return new Program() {
      Identifier  = identifier,
      Assignments = assignments
    };
  }

  public Assignment ParseAssignment() {
    string identifier = null;
    string expression = null;

    this.Log("ParseAssignment");
    int pos = this.source.position;
    try {

      identifier = this.source.Consume(Extracting.Identifier);

      this.source.Consume(":=");

      expression = this.ParseExpression();

      this.source.Consume(";");

    } catch(ParseException e) {
      this.source.position = pos;
      throw this.source.GenerateParseException(
        "Failed to parse Assignment.", e
      );
    }

    return new Assignment() {
      Identifier = identifier,
      Expression = expression
    };
  }

  public string ParseExpression() {
    string alternative = null;

    this.Log("ParseExpression");
    int pos = this.source.position;
    try {

      try {
        alternative = this.source.Consume(Extracting.Identifier);
      } catch(ParseException) {
        try {
          alternative = this.source.Consume(Extracting.String);
        } catch(ParseException) {
          try {
            alternative = this.source.Consume(Extracting.Number);
          } catch(ParseException) {
            throw this.source.GenerateParseException(
              "Expected: identifier | string | number"
            );
          }
        }
      }

    } catch(ParseException e) {
      this.source.position = pos;
      throw this.source.GenerateParseException(
        "Failed to parse Expression.", e
      );
    }

    return alternative;
  }


  [ConditionalAttribute("DEBUG")]
  private void Log(string msg) {
    Console.Error.WriteLine("!!! " + msg + " @ " + this.source.Peek(10).Replace('\n', 'n'));
  }
}
