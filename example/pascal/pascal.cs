// parser.cs

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Diagnostics;

  
public class Program {
  public Identifier       Identifier  { get; set; }
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
  public Identifier Identifier { get; set; }
  public Expression Expression   { get; set; }
  public override string ToString() {
    return 
      "Assignment(" +
        "Identifier=" + this.Identifier + "," +
        "Expression=" + this.Expression +
      ")";
  }
}

public interface Expression {}

public class Identifier : Expression {
  public string Name { get; set; }
  public override string ToString() {
    return
      "Identifier(" +
        "Name=" + this.Name +
      ")";
  }
}

public class String : Expression {
  public string Text { get; set; }
  public override string ToString() {
    return
      "String(" +
        "Text=" + this.Text +
      ")";
  }
}

public class Number : Expression {
  public string Value { get; set; }
  public override string ToString() {
    return
      "Number(" +
        "Value=" + this.Value +
      ")";
  }
}

public class Parser {
  private Parsable source;
  public Program AST { get; set; }

  public Parser Parse(string source) {
    this.source = new Parsable(source);
    this.AST    = this.ParseProgram();
    if( ! this.source.IsDone ) {
      throw this.source.GenerateParseException("Could not parse");
    }
    return this;
  }
  
  public Program ParseProgram() {
    Identifier identifier = null;
    List<Assignment> assignments = new List<Assignment>();
    
    this.Log("ParseProgram");
    int pos = this.source.position;
    try {

      this.source.Consume("PROGRAM");

      identifier = this.ParseIdentifier();

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
    Identifier identifier = null;
    Expression expression = null;

    this.Log("ParseAssignment");
    int pos = this.source.position;
    try {

      identifier = this.ParseIdentifier();

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

  public Expression ParseExpression() {
    Expression alternative = null;

    this.Log("ParseExpression");
    int pos = this.source.position;
    try {

      try {
        alternative = this.ParseIdentifier();
      } catch(ParseException) {
        try {
          alternative = this.ParseString();
        } catch(ParseException) {
          try {
            alternative = this.ParseNumber();
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

  public Identifier ParseIdentifier() {
    string name = null;
    
    this.Log("ParseIdentifier");
    int pos = this.source.position;
    try {

      name = this.source.Consume(Extracting.Identifier);

    } catch(ParseException e) {
      this.source.position = pos;
      throw this.source.GenerateParseException(
        "Failed to parse Identifier.", e
      );
    }
    
    return new Identifier() {
      Name = name
    };
  }

  public String ParseString() {
    string text = null;
    
    this.Log("ParseString");
    int pos = this.source.position;
    try {

      text = this.source.Consume(Extracting.String);

    } catch(ParseException e) {
      this.source.position = pos;
      throw this.source.GenerateParseException(
        "Failed to parse String.", e
      );
    }
    
    return new String() {
      Text = text
    };
  }

  public Number ParseNumber() {
    string value = null;
    
    this.Log("ParseNumber");
    int pos = this.source.position;
    try {

      value = this.source.Consume(Extracting.Number);

    } catch(ParseException e) {
      this.source.position = pos;
      throw this.source.GenerateParseException(
        "Failed to parse Number.", e
      );
    }
    
    return new Number() {
      Value = value
    };
  }


  [ConditionalAttribute("DEBUG")]
  private void Log(string msg) {
    Console.Error.WriteLine("!!! " + msg + " @ " + this.source.Peek(10).Replace('\n', 'n'));
  }
}

public class Extracting {
  public static Regex Identifier = new Regex( @"^([A-Z][A-Z0-9]*)" );
  public static Regex String = new Regex( @"^""([^""]*)""|'([^']*)'" );
  public static Regex Number = new Regex( @"^(-?[1-9][0-9]*)" );
}
