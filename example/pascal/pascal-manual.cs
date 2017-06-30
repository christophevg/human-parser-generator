using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

// program ::= "PROGRAM" identifier "BEGIN" { assignment ";" } "END." ;
public class Program {
  public Identifier Identifier { get; set; }
  public List<Assignment> Assignments { get; set; }
  public Program() {
    this.Assignments = new List<Assignment>();
  }
  public override string ToString() {
    return
    "new Program() { \n" +
    "Identifier = " + (this.Identifier == null ? "null" : this.Identifier.ToString()) + ",\n" +
    "Assignments = new List<Assignment>() {" +
    string.Join(",", this.Assignments.Select(x => x.ToString())) +
    "}" +
    "}";
  }
}

// assignment ::= identifier ":=" expression ;
public class Assignment {
  public Identifier Identifier { get; set; }
  public Expression Expression { get; set; }
  public override string ToString() {
    return
    "new Assignment() { \n" +
    "Identifier = " + (this.Identifier == null ? "null" : this.Identifier.ToString()) + ",\n" +
    "Expression = " + (this.Expression == null ? "null" : this.Expression.ToString()) +
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

public class Parser : ParserBase<Program> {


  // program ::= "PROGRAM" identifier "BEGIN" { assignment ";" } "END." ;
  public override Program Parse() {
    Program program = new Program();
    Log( "ParseProgram" );
    Parse( () => {
      Consume("PROGRAM");
      program.Identifier = ParseIdentifier();
      Consume("BEGIN");
      Repeat( () => {
        program.Assignments.Add( ParseAssignment() );
        Consume(";");
      });
      Consume("END.");
    }).OrThrow("Failed to parse Program");
    return program;
  }

  // assignment ::= identifier ":=" expression ;
  public Assignment ParseAssignment() {
    Assignment assignment = new Assignment();
    Log( "ParseAssignment" );
    Parse( () => {
      assignment.Identifier = ParseIdentifier();
      Consume(":=");
      assignment.Expression = ParseExpression();
    }).OrThrow("Failed to parse Assignment");
    return assignment;
  }

  // expression ::= identifier | string | number ;
  public Expression ParseExpression() {
    Expression expression = null;
    Log( "ParseExpression" );
    Parse( () => {
      Parse( () => {
        expression = ParseIdentifier();
      })
      .Or( () => {
        expression = ParseString();
      })
      .Or( () => {
        expression = ParseNumber();
      })
      .OrThrow("Expected: identifier | string | number");
    }).OrThrow("Failed to parse Expression");
    return expression;
  }

  // identifier ::= name @ /([A-Z][A-Z0-9]*)/ ;
  public Identifier ParseIdentifier() {
    Identifier identifier = new Identifier();
    Log( "ParseIdentifier" );
    Parse( () => {
      identifier.Name = Consume(Extracting.Identifier);
    }).OrThrow("Failed to parse Identifier");
    return identifier;
  }

  // string ::= text @ /"([^"]*)"|'([^']*)'/ ;
  public String ParseString() {
    String text = new String();
    Log( "ParseString" );
    Parse( () => {
      text.Text = Consume(Extracting.String);
    }).OrThrow("Failed to parse String");
    return text;
  }

  // number ::= value @ /(-?[1-9][0-9]*)/ ;
  public Number ParseNumber() {
    Number number = new Number();
    Log( "ParseNumber" );
    Parse( () => {
      number.Value = Consume(Extracting.Number);
    }).OrThrow("Failed to parse Number");
    return number;
  }

}

public class Extracting {
  public static Regex Identifier = new Regex(@"\G(([A-Z][A-Z0-9]*))");
  public static Regex String = new Regex(@"\G(""([^""]*)""|'([^']*)')");
  public static Regex Number = new Regex(@"\G((-?[1-9][0-9]*))");
}
