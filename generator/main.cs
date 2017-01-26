// runner example - implements grammar for (small subset) of the Pascal language
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

using Grammar;

public class Runner {

  public static Model CreatePascalGrammar() {
    // program = 'PROGRAM', identifier,
    //           'BEGIN',
    //           { assignment },
    //           'END.' ;
    // assignment = identifier , ":=" , ( number | identifier | string ), ";";
    // identifier = /([A-Z][A-Z0-9]*)/;
    // number     = /(-?[1-9][0-9]*)/ ;
    // string     = /"([^"]*)"/;

    return new Model() {
      Rules = new List<Rule>() {
        new Rule() {
          Id   = "program",
          Exp = new SequenceExpression() {
            Expressions = new List<Expression> {
              new StringExpression() { String = "PROGRAM" },
              new IdentifierExpression() { Id = "identifier" },
              new StringExpression() { String = "BEGIN" },
              new RepetitionExpression() {
                Exp = new IdentifierExpression() { Id = "assignment" },
              },
              new StringExpression() { String = "END." }
            }
          }
        },
        new Rule() {
          Id  = "assignment",
          Exp = new SequenceExpression() {
            Expressions = new List<Expression> {
              new IdentifierExpression() { Id = "identifier" },
              new StringExpression() { String = ":=" },
              new OrExpression() {
                Exp1 = new IdentifierExpression() { Id = "number" },
                Exp2 = new OrExpression() {
                  Exp1 = new IdentifierExpression() { Id = "identifier" },
                  Exp2 = new IdentifierExpression() { Id = "string" }
                }
              },
              new StringExpression() { String = ";" }
            }
          }
        },
        new Rule() {
          Id  = "identifier",
          Exp = new Extractor() { Pattern = "([A-Z][A-Z0-9]*)" }
        },
        new Rule() {
          Id  = "number",
          Exp = new Extractor() { Pattern = "(-?[1-9][0-9]*)" }
        },
        new Rule() {
          Id  = "string",
          Exp = new Extractor() { Pattern = "\\\"([^\\\"]*)\\\"" }
        }
      }
    };
  }

  public static void Main(string[] args) {
    Model grammar = CreatePascalGrammar();
    Parser.Model model = new Parser.Model().Import(grammar);

    // Console.WriteLine(model.ToString());
    // Console.WriteLine();

    Emitter.CSharp code = new Emitter.CSharp().Generate(model);
    Console.WriteLine(code.ToString());
  }
}
