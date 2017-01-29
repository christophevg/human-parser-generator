// This file contains the bootstrap HPG BNF-like grammar in model form. It
// allows for generating a parser that then can parse the grammar in BNF-like
// notation and generate itself to become self-hosting ;-)
// See also: ../grammars/hpg.bnf
// Below the BNF grammar, the Grammar Model is also provided - The classes to
// model an HPG BNF-like parsed notation while bootstrapping. Once the parser is
// generated, it will also include these generated classes.
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace HumanParserGenerator.Grammars {

  public class AsModel {

    public static Grammar BNF =
      new Grammar()  {
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
                new IdentifierExpression() { Id     = "expressions" },
                new StringExpression()     { String = ";"          }
              }
            }
          },
          new Rule() {
            Id      = "expressions",
            Exp     = new AlternativesExpression() {
              Expressions = new List<Expression>() {
                new IdentifierExpression() { Id = "alternative-expressions" },
                new IdentifierExpression() { Id = "sequential-expressions" }
              }
            }
          },
          new Rule() {
            Id = "alternative-expressions",
            Exp = new SequenceExpression() {
              Expressions = new List<Expression> {
                new IdentifierExpression() { Id     = "expression" },
                new StringExpression()     { String = "|"          },
                new IdentifierExpression() { Id     = "expressions" }
              }
            }
          },
          new Rule() {
            Id = "sequential-expressions",
            Exp = new RepetitionExpression() {
              Exp = new IdentifierExpression() { Id = "expression" },
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
                new IdentifierExpression() { Id = "group-expression"        }
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
                new IdentifierExpression() { Id     = "regex"      },
                new StringExpression()     { String = "/"          }
              }
            }
          },
          new Rule() {
            Id = "optional-expression",
            Exp = new SequenceExpression() {
              Expressions = new List<Expression> {
                new StringExpression()     { String = "["          },
                new IdentifierExpression() { Id     = "expressions" },
                new StringExpression()     { String = "]"          }
              }
            }
          },
          new Rule() {
            Id = "repetition-expression",
            Exp = new SequenceExpression() {
              Expressions = new List<Expression> {
                new StringExpression()     { String = "{"          },
                new IdentifierExpression() { Id     = "expressions" },
                new StringExpression()     { String = "}"          }
              }
            }
          },
          new Rule() {
            Id = "group-expression",
            Exp = new SequenceExpression() {
              Expressions = new List<Expression> {
                new StringExpression()     { String = "("          },
                new IdentifierExpression() { Id     = "expressions" },
                new StringExpression()     { String = ")"          }
              }
            }
          },
          new Rule() {
            Id  = "identifier",
            Exp = new Extractor() { Pattern = "([A-Za-z][A-Za-z0-9-]*)"       }
          },
          new Rule() {
            Id  = "string",
            Exp = new Extractor() { Pattern = "\\\"([^\\\"]*)\\\"|^'([^']*)'" }
          },
          new Rule() {
            Id  = "regex",
            Exp = new Extractor() { Pattern = "(.*?)(?<keep>/\\\\s*;)"                   }
          }
        }
      };

    // Temporary Pascal grammar
  
    public static Grammar Pascal =
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

      new Grammar() {
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
  
  // The classes below are also part of the bootstrap process, once a parser
  // is generated, it also generates these classes.

  public class Grammar {
    public List<Rule> Rules { get; set; }
    public override string ToString() {
      return
        "Grammar(" + 
          "[" + string.Join(",", this.Rules.Select(x => x.ToString())) + "]" +
        ")";
    }
  }

  public class Rule {
    public bool       IsVirtual { get; set; }
    public string     Id { get; set; }
    public Expression Exp { get; set; }
    public override string ToString() {
      return
        "Rule(" +
          "Id=" + this.Id + "," + 
          "Exp=" + this.Exp.ToString() +
        ")";
    }
  }

  public abstract class Expression {}

  public class IdentifierExpression : Expression {
    public string Id;
    public override string ToString() {
      return
        "IdentifierExpression(" +
          "Id=" + this.Id +
        ")";
    }
  }

  public class StringExpression : Expression {
    public string String;
    public override string ToString() {
      return
        "StringExpression(" +
          "String=" + this.String +
        ")";
    }
  }

  public class OptionalExpression : Expression {
    public Expression Exp;
    public override string ToString() {
      return
        "OptionalExpression(" +
          "Exp=" + this.Exp.ToString() +
        ")";
    }
  }

  public class RepetitionExpression : Expression {
    public Expression Exp;
    public override string ToString() {
      return
        "RepetitionExpression(" +
          "Exp=" + this.Exp.ToString() +
        ")";
    }
  }

  public class GroupExpression : Expression {
    public Expression Exp;
    public override string ToString() {
      return
        "GroupExpression(" +
          "Exp=" + this.Exp.ToString() +
        ")";
    }
  }

  public class AlternativesExpression : Expression {
    public List<Expression> Expressions;
    public override string ToString() {
      return
        "AlernativesExpression(" +
          "Expressions=" + "[" + 
            string.Join("|", this.Expressions.Select(x => x.ToString())) +
          "]" +
        ")";
    }
  }

  public class SequenceExpression : Expression {
    public List<Expression> Expressions;
    public override string ToString() {
      return
        "SequenceExpression(" +
          "Expressions=" + "[" + 
            string.Join(",", this.Expressions.Select(x => x.ToString())) +
          "]" +
        ")";
    }
  }

  public class Extractor : Expression {
    public string Pattern { get; set; }
    public override string ToString() {
      return
        "Extractor(" +
          "Pattern=" + this.Pattern +
        ")";
    }
  }

}
