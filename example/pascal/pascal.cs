using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

// program ::= "PROGRAM" identifier "BEGIN" { assignment } "END." ;
public class Program {
  public Identifier Identifier { get; set; }
  public List<Assignment> Assignments { get; set; }
  public Program() {
    this.Assignments = new List<Assignment>();
  }
  public override string ToString() {
    return
    "new Program() { \n" +
    "Identifier = " + (this.Identifier == null ? "null" :
                       this.Identifier.ToString()) + ",\n" +
    "Assignments = new List<Assignment>() {" +
    string.Join(",", this.Assignments.Select(x => x.ToString())) +
    "}" +
    "}";
  }
}

// assignment ::= identifier ":=" expression ";" ;
public class Assignment {
  public Identifier Identifier { get; set; }
  public Expression Expression { get; set; }
  public override string ToString() {
    return
    "new Assignment() { \n" +
    "Identifier = " + (this.Identifier == null ? "null" :
                       this.Identifier.ToString()) + ",\n" +
    "Expression = " + (this.Expression == null ? "null" :
                       this.Expression.ToString()) +
    "}";
  }
}

// expression ::= identifier | string | number ;
public interface Expression {}

// identifier ::= name @ /([A-Z][A-Z0-9]*)/ ;
public class Identifier : Expression {
  public string Name { get; set; }
  public override string ToString() {
    return
    "new Identifier() { " +
    "Name = " + Format.Literal(this.Name)  +
    "}";
  }
}

// string ::= text @ /"([^"]*)"|'([^']*)'/ ;
public class String : Expression {
  public string Text { get; set; }
  public override string ToString() {
    return
    "new String() { " +
    "Text = " + Format.Literal(this.Text) +
    "}";
  }
}

// number ::= value @ /(-?[1-9][0-9]*)/ ;
public class Number : Expression {
  public string Value { get; set; }
  public override string ToString() {
    return
    "new Number() { " +
    "Value = " + Format.Literal(this.Value) +
    "}";
  }
}

public class Parser : ParserBase {
  public Program AST { get; set; }
  public Parser Parse(string source) {
    this.Source = new Parsable(source);
    try {
      this.AST    = this.ParseProgram();
    } catch(ParseException e) {
      this.Errors.Add(e);
      throw this.Source.GenerateParseException("Failed to parse.");
    }
    if( ! this.Source.IsDone ) {
      throw this.Source.GenerateParseException("Could not parse remaining data.");
    }
    return this;
  }

  // program ::= "PROGRAM" identifier "BEGIN" { assignment } "END." ;
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

  // assignment ::= identifier ":=" expression ";" ;
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

  // expression ::= identifier | string | number ;
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

  // identifier ::= name @ /([A-Z][A-Z0-9]*)/ ;
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

  // string ::= text @ /"([^"]*)"|'([^']*)'/ ;
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

  // number ::= value @ /(-?[1-9][0-9]*)/ ;
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

}

public class Extracting {
  public static Regex Identifier = new Regex(@"^([A-Z][A-Z0-9]*)");
  public static Regex String = new Regex(@"^""([^""]*)""|'([^']*)'");
  public static Regex Number = new Regex(@"^(-?[1-9][0-9]*)");
}
