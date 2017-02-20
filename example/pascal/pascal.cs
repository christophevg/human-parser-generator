using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Diagnostics;


public class Program {
  public Identifier Identifier { get; set; }
  public List<Assignment> Assignments { get; set; }
  public Program() {
    this.Assignments = new List<Assignment>();
  }
  public override string ToString() {
    return
    "Program(" +
    "Identifier=" + this.Identifier + "," +
    "Assignments=" + "[" +
    string.Join(",", this.Assignments.Select(x => x.ToString())) +
    "]" +
    ")";
  }
}

public class Assignment {
  public Identifier Identifier { get; set; }
  public Expression Expression { get; set; }
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

public class Parser : ParserBase {
  public Program AST { get; set; }
  public Parser Parse(string source) {
    this.Source = new Parsable(source);
    try {
      this.AST = this.ParseProgram();
    } catch(ParseException e) {
      this.Errors.Add(e);
      throw this.Source.GenerateParseException("Failed to parse.");
    }
    if( ! this.Source.IsDone ) {
      throw this.Source.GenerateParseException("Could not parse remaining data.");
    }
    return this;
  }

  public Program ParseProgram() {
    Identifier identifier = null;
    List<Assignment> assignments = new List<Assignment>();
    this.Log( "ParseProgram" );
    Parse( () => {
      Consume("PROGRAM");
      identifier = ParseIdentifier();
      Consume("BEGIN");
      assignments = Many<Assignment>(ParseAssignment);
      Consume("END.");
    }).OrThrow("Failed to parse Program");
    return new Program() {
      Identifier = identifier,
      Assignments = assignments
    };
  }

  public Assignment ParseAssignment() {
    Identifier identifier = null;
    Expression expression = null;
    this.Log( "ParseAssignment" );
    Parse( () => {
      identifier = ParseIdentifier();
      Consume(":=");
      expression = ParseExpression();
      Consume(";");
    }).OrThrow("Failed to parse Assignment");
    return new Assignment() {
      Identifier = identifier,
      Expression = expression
    };
  }

  public Expression ParseExpression() {
    Expression alternative = null;
    this.Log( "ParseExpression" );
    Parse( () => {
      Parse( () => {
        alternative = ParseIdentifier();
      })
      .Or( () => {
        alternative = ParseString();
      })
      .Or( () => {
        alternative = ParseNumber();
      })
      .OrThrow("Expected: identifier | string | number");
    }).OrThrow("Failed to parse Expression");
    return alternative;
  }

  public Identifier ParseIdentifier() {
    string name = null;
    this.Log( "ParseIdentifier" );
    Parse( () => {
      name = Consume(Extracting.Identifier);
    }).OrThrow("Failed to parse Identifier");
    return new Identifier() {
      Name = name
    };
  }

  public String ParseString() {
    string text = null;
    this.Log( "ParseString" );
    Parse( () => {
      text = Consume(Extracting.String);
    }).OrThrow("Failed to parse String");
    return new String() {
      Text = text
    };
  }

  public Number ParseNumber() {
    string value = null;
    this.Log( "ParseNumber" );
    Parse( () => {
      value = Consume(Extracting.Number);
    }).OrThrow("Failed to parse Number");
    return new Number() {
      Value = value
    };
  }


  [ConditionalAttribute("DEBUG")]
  private void Log(string msg) {
    Console.Error.WriteLine("!!! " + msg + " @ " + this.Source.Peek(10).Replace('\n', 'n'));
  }
}

public class Extracting {
  public static Regex Identifier = new Regex(@"^([A-Z][A-Z0-9]*)");
  public static Regex String     = new Regex(@"^""([^""]*)""|'([^']*)'");
  public static Regex Number     = new Regex(@"^(-?[1-9][0-9]*)");
}


