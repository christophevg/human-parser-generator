// Given a Parser Model, the Emitter generates CSharp code
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

using HumanParserGenerator.Generator;

namespace HumanParserGenerator.Emitter {
  
  public class BNF {
    
    public bool         EmitInfo  { get; set; }
    public List<string> Sources   { get; set; }

    private Grammar grammar;

    public BNF Generate(Grammar grammar) {
      this.grammar = grammar;
      return this;
    }

    public override string ToString() {
      if( this.grammar == null )         { return "";    }
      if( this.grammar.Rules.Count == 0) { return ""; }
      return this.GenerateGrammar();
    }

    public string GenerateGrammar() {
      return string.Join("\n", this.grammar.Rules.Select(r => this.GenerateRule(r)));
    }

    public string GenerateRule(Rule rule) {
      return rule.Identifier + " ::= " + 
             this.GenerateExpression(rule.Expression) + " ;";
    }

    private string GenerateExpression(Expression expression) {
      try {
        return new Dictionary<string, Func<Expression,string>>() {
          { "AlternativesExpression", this.GenerateAlternativesExpression },
          { "SequentialExpression",   this.GenerateSequentialExpression   },
          { "OptionalExpression",     this.GenerateOptionalExpression     },
          { "RepetitionExpression",   this.GenerateRepetitionExpression   },
          { "GroupExpression",        this.GenerateGroupExpression        },
          { "IdentifierExpression",   this.GenerateIdentifierExpression   },
          { "StringExpression",       this.GenerateStringExpression       },
          { "ExtractorExpression",    this.GenerateExtractorExpression    }
        }[expression.GetType().ToString().Split('.').Last()](expression);
      } catch(KeyNotFoundException e) {
        throw new NotImplementedException(
          "extracting not implemented for " + expression.GetType().ToString(), e
        );
      }
    }

    private string GenerateAlternativesExpression(Expression expression) {
      AlternativesExpression alternatives = expression as AlternativesExpression;
      return this.GenerateExpression(alternatives.NonAlternativesExpression) + " | " +
        this.GenerateExpression(alternatives.Expression);
    }

    private string GenerateSequentialExpression(Expression expression) {
      SequentialExpression sequence = expression as SequentialExpression;
      return this.GenerateExpression(sequence.AtomicExpression) + " " +
        this.GenerateExpression(sequence.NonAlternativesExpression);
    }

    private string GenerateOptionalExpression(Expression expression) {
      OptionalExpression optional = expression as OptionalExpression;
      return "[ " + this.GenerateExpression(optional.Expression) + " ]";
    }

    private string GenerateRepetitionExpression(Expression expression) {
      RepetitionExpression repetition = expression as RepetitionExpression;
      return "{ " + this.GenerateExpression(repetition.Expression) + " }";
    }

    private string GenerateGroupExpression(Expression expression) {
      GroupExpression group = expression as GroupExpression;
      return "( " + this.GenerateExpression(group.Expression) + " )";
    }

    private string GenerateIdentifierExpression(Expression expression) {
      IdentifierExpression identifier = expression as IdentifierExpression;
      return ( identifier.Name != null ? identifier.Name + " @ " : "") +
        identifier.Identifier;
    }

    private string GenerateStringExpression(Expression expression) {
      StringExpression text = expression as StringExpression;
      return ( text.Name != null ? text.Name + " @ " : "") +
        "\"" + text.String + "\"";
    }

    private string GenerateExtractorExpression(Expression expression) {
      ExtractorExpression extractor = expression as ExtractorExpression;
      return ( extractor.Name != null ? extractor.Name + " @ " : "") +
        "/" + extractor.Pattern + "/";
    }

    // logging functionality

    private void Warn(string msg) {
      this.Log("warning: " + msg);
    }

    private void Log(string msg) {
      Console.Error.WriteLine("hpg-emitter: " + msg);
    }
  }
}
