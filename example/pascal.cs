// parser.cs

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;


public class Program {
  public string Identifier { get; set; }
  public List<Assignment> Assignments { get; set; }
  public Program() {
    this.Assignments = new List<Assignment>();
  }
  public override string ToString() {
    return
      "Program(" +
        "Identifier=" + this.Identifier + "," +
        "Assignments=" + "[" + string.Join(",",
          this.Assignments.Select(x => x.ToString())
        ) + "]" +
      ")";
  }
}

public class Assignment {
  public string Identifier { get; set; }
  public string Value { get; set; }
  public override string ToString() {
    return 
      "Assignment(" +
        "Identifier=" + this.Identifier + "," +
        "Value=" + this.Value +
      ")";
  }
}

public class Extracting {
  public static Regex Identifier = new Regex("^([A-Z][A-Z0-9]*)");
  public static Regex Number     = new Regex("^(-?[1-9][0-9]*)");
  public static Regex String     = new Regex("^\"([^\"]*)\"|'([^']*)'");
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
    string identifier;
    List<Assignment> assignments = new List<Assignment>();
    
    int pos = this.source.position;
    try {

      this.source.Consume("PROGRAM");

      identifier = this.source.Consume(Extracting.Identifier);

      this.source.Consume("BEGIN");

      Assignment temp;
      while(true) {
        try {
          temp = this.ParseAssignment();
        } catch(ParseException) {
          break;
        }
        assignments.Add(temp);
      }

      this.source.Consume("END.");

    } catch(ParseException e) {
      this.source.position = pos;
      throw this.source.GenerateParseException(
        "Failed to parse Program.", e
      );
    }

    return new Program() {
      Identifier = identifier,
      Assignments = assignments
    };
  }

  public Assignment ParseAssignment() {
    string identifier;
    string value;

    int pos = this.source.position;
    try {

      identifier = this.source.Consume(Extracting.Identifier);

      this.source.Consume(":=");

      try {
        value = this.source.Consume(Extracting.Number);
      } catch(ParseException) {
        try {
          value = this.source.Consume(Extracting.Identifier);
        } catch(ParseException) {
          try {
            value = this.source.Consume(Extracting.String);
            } catch(ParseException) {
              throw this.source.GenerateParseException(
                "Expected: number | identifier | string"
              );
            }
        }
      }


      this.source.Consume(";");

    } catch(ParseException e) {
      this.source.position = pos;
      throw this.source.GenerateParseException(
        "Failed to parse Assignment.", e
      );
    }
    
    return new Assignment() {
      Identifier = identifier,
      Value = value
    };
  }

}
