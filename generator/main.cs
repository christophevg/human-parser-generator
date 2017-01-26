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
    // program    ::= "PROGRAM" identifier
    //                "BEGIN"
    //                { assignment }
    //                "END."
    //                ;
    //
    // assignment ::= identifier
    //                ":="
    //                ( number | identifier | string )@'Expression'
    //                ";"
    //                ;
    //
    // identifier ::= /([A-Z][A-Z0-9]*)/ ;
    // number     ::= /(-?[1-9][0-9]*)/ ;
    // string     ::= /"([^"]*)"|'([^']*)'/ ;

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
              new AlternativesExpression() {
                Expressions = new List<Expression> {
                  new IdentifierExpression() { Id = "number" },
                  new IdentifierExpression() { Id = "identifier" },
                  new IdentifierExpression() { Id = "string" }
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
          Exp = new Extractor() { Pattern = "\\\"([^\\\"]*)\\\"|'([^']*)'" }
        }
      }
    };
  }
  
  public static Model CreateBNFGrammar() {
    // grammar                 ::= { rule } ;
    // rule                    ::= identifier  "::=" expression ";" ;
    // expression              ::= identifier-expression
    //                           | string-expression
    //                           | extractor-expression
    //                           | optional-expression
    //                           | repetition-expression
    //                           | group-expression
    //                           | alternatives-expression
    //                           | sequence-expression
    //                           ;
    // identifier-expression   ::= identifier ;
    // string-expression       ::= string ;
    // extractor-expression    ::= "/" regex "/"
    // optional-expression     ::= "[" expression "]" ;
    // repetition-expression   ::= "{" expression "}" ;
    // group-expression        ::= "(" expression ")" ;
    // alternatives-expression ::= expression "|" expression ;
    // sequence-expression     ::= expression     expression ;
    //
    // identifier              ::= /([A-Z][A-Z0-9]*)/ ;
    // string                  ::= /"([^"]*)"|'([^']*)'/ ;
    // regex                   ::= /([^/]*)/

    return new Model()  {
      Rules = new List<Rule>() {
        new Rule() {
          Id  = "grammar",
          Exp = new RepetitionExpression() {
            Exp = new IdentifierExpression() { Id = "rule" },
          }
        },
        new Rule() {
          Id  = "rule",
          Exp = new SequenceExpression() {
            Expressions = new List<Expression> {
              new IdentifierExpression() { Id     = "identifier" },
              new StringExpression()     { String = "::="        },
              new IdentifierExpression() { Id     = "expression" },
              new StringExpression()     { String = ";"          }
            }
          }
        },
        new Rule() {
          Id = "expression",
          Exp = new AlternativesExpression() {
            Expressions = new List<Expression> {
              new IdentifierExpression() { Id = "identifier-expression"   },
              new IdentifierExpression() { Id = "string-expression"       },
              new IdentifierExpression() { Id = "extractor-expression"    },
              new IdentifierExpression() { Id = "optional-expression"     },
              new IdentifierExpression() { Id = "repetition-expression"   },
              new IdentifierExpression() { Id = "group-expression"        },
              new IdentifierExpression() { Id = "alternatives-expression" },
              new IdentifierExpression() { Id = "sequence-expression"     }
            }
          }
        },
        new Rule() {
          Id = "identifier-expression",
          Exp = new IdentifierExpression() { Id = "identifier" }
        },
        new Rule() {
          Id = "string-expression",
          Exp = new IdentifierExpression() { Id = "string" }
        },
        new Rule() {
          Id = "extractor-expression",
          Exp = new SequenceExpression() {
            Expressions = new List<Expression> {
              new StringExpression()     { String = "/"          },
              new IdentifierExpression() { Id     = "extractor" },
              new StringExpression()     { String = "/"          }
            }
          }
        },
        new Rule() {
          Id = "optional-expression",
          Exp = new SequenceExpression() {
            Expressions = new List<Expression> {
              new StringExpression()     { String = "["          },
              new IdentifierExpression() { Id     = "expression" },
              new StringExpression()     { String = "]"          }
            }
          }
        },
        new Rule() {
          Id = "repetition-expression",
          Exp = new SequenceExpression() {
            Expressions = new List<Expression> {
              new StringExpression()     { String = "{"          },
              new IdentifierExpression() { Id     = "expression" },
              new StringExpression()     { String = "}"          }
            }
          }
        },
        new Rule() {
          Id = "group-expression",
          Exp = new SequenceExpression() {
            Expressions = new List<Expression> {
              new StringExpression()     { String = "("          },
              new IdentifierExpression() { Id     = "expression" },
              new StringExpression()     { String = ")"          }
            }
          }
        },
        new Rule() {
          Id = "alternatives-expression",
          Exp = new SequenceExpression() {
            Expressions = new List<Expression> {
              new IdentifierExpression() { Id     = "expression" },
              new StringExpression()     { String = "|"          },
              new IdentifierExpression() { Id     = "expression" }
            }
          }
        },
        new Rule() {
          Id = "sequence-expression",
          Exp = new SequenceExpression() {
            Expressions = new List<Expression> {
              new IdentifierExpression() { Id     = "expression" },
              new IdentifierExpression() { Id     = "expression" }
            }
          }
        },
        new Rule() {
          Id  = "identifier",
          Exp = new Extractor() { Pattern = "([A-Z][A-Z0-9]*)"             }
        },
        new Rule() {
          Id  = "string",
          Exp = new Extractor() { Pattern = "\\\"([^\\\"]*)\\\"|'([^']*)'" }
        },
        new Rule() {
          Id  = "extractor",
          Exp = new Extractor() { Pattern = "([^/]*)"                      }
        }
      }
    };
  }

  public static void Main(string[] args) {

    var grammarName = "pascal"; // default
    if(args.Length == 1) {
      grammarName = args[0];
    }

    Model grammar;

    switch(grammarName) {
      case "bnf": grammar = CreateBNFGrammar();    break;
      default:    grammar = CreatePascalGrammar(); break;
    }

    Parser.Model model = new Parser.Model().Import(grammar);

    // Console.WriteLine(model.ToString());
    // Console.WriteLine();

    Emitter.CSharp code = new Emitter.CSharp().Generate(model);
    Console.WriteLine(code.ToString());
  }
}
