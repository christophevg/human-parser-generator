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

public class AsModel {

  public static Grammar BNF =
    new Grammar()  {
      Rules = new List<Rule>() {
        // grammar ::= { rule } ;
        new Rule() {
          Identifier = "grammar",
          Expression = new RepetitionExpression() {
            Expression = new IdentifierExpression() { Identifier = "rule" },
          }
        },
        // rule ::= identifier "::=" expression ";" ;
        new Rule() {
          Identifier = "rule",
          Expression = new SequentialExpression() {
            NonSequentialExpression = new IdentifierExpression() { Identifier = "identifier" },
            Expression              = new SequentialExpression() {
              NonSequentialExpression = new StringExpression() { String = "::=" },
              Expression              = new SequentialExpression() {
                NonSequentialExpression = new IdentifierExpression() { Identifier = "expression" },
                Expression              = new StringExpression() { String = ";" }
              }
            }
          }
        },
        // expression ::= sequential-expression
        //              | non-sequential-expression
        //              ;
        new Rule() {
          Identifier = "expression",
          Expression = new AlternativesExpression() {
            AtomicExpression        = new IdentifierExpression() { Identifier = "sequential-expression" },
            NonSequentialExpression =  new IdentifierExpression() { Identifier = "non-sequential-expression" }
          }
        },
        // sequential-expression ::= non-sequential-expression expression ;
        new Rule() {
          Identifier = "sequential-expression",
          Expression = new SequentialExpression() {
            NonSequentialExpression = new IdentifierExpression() { Identifier = "non-sequential-expression" },
            Expression              = new IdentifierExpression() { Identifier = "expression" }
          }
        },
        // non-sequential-expression ::= alternatives-expression
        //                             | atomic-expression
        //                             ;
        new Rule() {
          Identifier = "non-sequential-expression",
          Expression = new AlternativesExpression() {
            AtomicExpression        = new IdentifierExpression() { Identifier = "alternatives-expression" },
            NonSequentialExpression = new IdentifierExpression() { Identifier = "atomic-expression" }
          }
        },
        // alternatives-expression ::= atomic-expression "|" 
        //                                     non-sequential-expression ;
        new Rule() {
          Identifier = "alternatives-expression",
          Expression = new SequentialExpression() {
            NonSequentialExpression = new IdentifierExpression() { Identifier = "atomic-expression" },
            Expression            = new SequentialExpression() {
              NonSequentialExpression = new StringExpression() { String = "|" },
              Expression              = new IdentifierExpression() { Identifier = "non-sequential-expression" }
            }
          }
        },
        // atomic-expression ::= nested-expression
        //                     | terminal-expression
        //                     ;
        new Rule() {
          Identifier = "atomic-expression",
          Expression = new AlternativesExpression() {
            AtomicExpression        = new IdentifierExpression() { Identifier = "nested-expression" },
            NonSequentialExpression = new IdentifierExpression() { Identifier = "terminal-expression" }
          }
        },
        // nested-expression ::= optional-expression
        //                     | repetition-expression
        //                     | group-expression
        //                     ;
        new Rule() {
          Identifier = "nested-expression",
          Expression = new AlternativesExpression() {
            AtomicExpression        = new IdentifierExpression() { Identifier = "optional-expression" },
            NonSequentialExpression = new AlternativesExpression() {
              AtomicExpression        = new IdentifierExpression() { Identifier = "repetition-expression" },
              NonSequentialExpression = new IdentifierExpression() { Identifier = "group-expression"    }
            }
          }
        },
        // optional-expression ::= "[" expression "]" ;
        new Rule() {
          Identifier = "optional-expression",
          Expression = new SequentialExpression() {
            NonSequentialExpression = new StringExpression() { String = "[" },
            Expression              = new SequentialExpression() {
              NonSequentialExpression = new IdentifierExpression() { Identifier = "expression" },
              Expression              = new StringExpression() { String = "]" }
            }
          }
        },
        // repetition-expression ::= "{" expression "}" ;
        new Rule() {
          Identifier = "repetition-expression",
          Expression = new SequentialExpression() {
            NonSequentialExpression = new StringExpression() { String = "{" },
            Expression              = new SequentialExpression() {
              NonSequentialExpression = new IdentifierExpression() { Identifier = "expression" },
              Expression              = new StringExpression() { String = "}" }
            }
          }
        },
        // group-expression ::= "(" expression ")" ;
        new Rule() {
          Identifier = "group-expression",
          Expression = new SequentialExpression() {
            NonSequentialExpression = new StringExpression() { String = "(" },
            Expression              = new SequentialExpression() {
              NonSequentialExpression = new IdentifierExpression() { Identifier = "expression" },
              Expression              = new StringExpression() { String = ")" }
            }
          }
        },
        // terminal-expression ::= identifier-expression
        //                       | string-expression
        //                       | extractor-expression
        //                       ;
        new Rule() {
          Identifier = "terminal-expression",
          Expression = new AlternativesExpression() {
            AtomicExpression        = new IdentifierExpression() { Identifier = "identifier-expression" },
            NonSequentialExpression = new AlternativesExpression() {
              AtomicExpression        = new IdentifierExpression() { Identifier = "string-expression" },
              NonSequentialExpression = new IdentifierExpression() { Identifier = "extractor-expression"    }
            }
          }
        },
        // identifier-expression ::= identifier ;
        new Rule() {
          Identifier = "identifier-expression",
          Expression = new IdentifierExpression() { Identifier = "identifier" }
        },
        // string-expression ::= string ;
        new Rule() {
          Identifier = "string-expression",
          Expression = new IdentifierExpression() { Identifier = "string" }
        },
        // extractor-expression ::= "/" regex "/" ;
        new Rule() {
          Identifier = "extractor-expression",
          Expression = new SequentialExpression() {
            NonSequentialExpression = new StringExpression() { String = "/" },
            Expression              = new SequentialExpression() {
              NonSequentialExpression = new IdentifierExpression() { Identifier = "regex" },
              Expression              = new StringExpression() { String = "/" }
            }
          }
        },
        // identifier ::= /([A-Za-z][A-Za-z0-9-]*)/ ;
        new Rule() {
          Identifier  = "identifier",
          Expression = new ExtractorExpression() { Regex = @"([A-Za-z][A-Za-z0-9-]*)" }
        },
        // string ::= /"([^"]*)"|^'([^']*)'/ ;
        new Rule() {
          Identifier = "string",
          Expression = new ExtractorExpression() { Regex = @"""([^""]*)""|^'([^']*)'" }
        },
        // regex ::= /(.*?)(?<keep>/\s*;)/ ;
        new Rule() {
          Identifier = "regex",
          Expression = new ExtractorExpression() { Regex = @"(.*?)(?<keep>/\s*;)" }
        }
      } // Rules
    }; // Grammar

  // Temporary Pascal grammar

  public static Grammar Pascal =
    new Grammar() {
      Rules = new List<Rule>() {
        // program ::= "PROGRAM" identifier
        //             "BEGIN"
        //             { assignments }
        //             "END."
        //           ;
        new Rule() {
          Identifier = "program",
          Expression = new SequentialExpression() {
            NonSequentialExpression = new StringExpression() { String = "PROGRAM" },
            Expression            = new SequentialExpression() {
              NonSequentialExpression = new IdentifierExpression() { Identifier = "identifier" },
              Expression              = new SequentialExpression() {
                NonSequentialExpression = new StringExpression() { String = "BEGIN" },
                Expression              = new SequentialExpression() {
                  NonSequentialExpression = new RepetitionExpression() {
                    Expression = new IdentifierExpression() { Identifier = "assignment" }
                  },
                  Expression              = new StringExpression() { String = "END." }
                }
              }
            }
          }
        },
        // assignment ::= identifier ":=" expression ";" ;
        new Rule() {
          Identifier = "assignment",
          Expression = new SequentialExpression() {
            NonSequentialExpression = new IdentifierExpression() { Identifier = "identifier" },
            Expression              = new SequentialExpression() {
              NonSequentialExpression = new StringExpression() { String = ":=" },
              Expression              = new SequentialExpression() {
                NonSequentialExpression = new IdentifierExpression { Identifier = "expression" },
                Expression              = new StringExpression { String = ";" }
              }
            }
          }
        },
        // expression ::= identifier-expression
        //              | string-expression
        //              | number-expression
        //              ;
        new Rule() {
          Identifier = "expression",
          Expression = new AlternativesExpression() {
            AtomicExpression        = new IdentifierExpression() { Identifier = "identifier-expression"},
            NonSequentialExpression = new AlternativesExpression() {
              AtomicExpression        = new IdentifierExpression() { Identifier = "string-expression" },
              NonSequentialExpression = new IdentifierExpression() { Identifier = "number-expression" }
            }
          }
        },
        // identifier-expression ::= identifier;
        new Rule() {
          Identifier = "identifier-expression",
          Expression = new IdentifierExpression() { Identifier = "identifier" }
        },
        // string-expression ::= string;
        new Rule() {
          Identifier = "string-expression",
          Expression = new IdentifierExpression() { Identifier = "string" }
        },
        // number-expression ::= number;
        new Rule() {
          Identifier = "number-expression",
          Expression = new IdentifierExpression() { Identifier = "number" }
        },
        // identifier ::= /([A-Z][A-Z0-9]*)/ ;
        new Rule() {
          Identifier = "identifier",
          Expression = new ExtractorExpression() { Regex = @"([A-Z][A-Z0-9]*)" }
        },
        // string ::= /"([^"]*)"|'([^']*)'/ ;
        new Rule() {
          Identifier = "string",
          Expression = new ExtractorExpression() { Regex = @"""([^""]*)""|'([^']*)'" }
        },
        // number ::= /(-?[1-9][0-9]*)/ ;
        new Rule() {
          Identifier = "number",
          Expression = new ExtractorExpression() { Regex = @"(-?[1-9][0-9]*)" }
        }
      } // Rules
    }; // Grammar
}

// The classes below are also part of the bootstrap process, once a parser
// is generated, it also generates these classes.

//  grammar ::= { rule } ;
public class Grammar {
  public List<Rule> Rules { get; set; }
  public override string ToString() {
    return
      "Grammar(" + 
        "[" + string.Join(",", this.Rules.Select(x => x.ToString())) + "]" +
      ")";
  }
}

// rule ::= identifier "::=" expression ";" ;
public class Rule {
  public string     Identifier { get; set; }
  public Expression Expression { get; set; }
  public override string ToString() {
    return
      "Rule(" +
        "Identifier=" + this.Identifier + "," + 
        "Expression=" + this.Expression +
      ")";
  }
}

// expression ::= sequential-expression
//              | non-sequential-expression
//              ;
public interface Expression {}

// sequential-expression ::= non-sequential-expression expression ;
public class SequentialExpression : Expression {
  public NonSequentialExpression NonSequentialExpression { get; set; }
  public Expression              Expression              { get; set; }
  public override string ToString() {
    return
      "SequentialExpression("+
        "NonSequentialExpression=" + this.NonSequentialExpression + "," +
        "Expression=" + this.Expression +
      ")";
  }
}

// non-sequential-expression ::= alternatives-expression
//                             | atomic-expression
//                             ;
public interface NonSequentialExpression : Expression {}

// alternatives-expression ::= atomic-expression "|" non-sequential-expression;
 public class AlternativesExpression : NonSequentialExpression {
   public AtomicExpression        AtomicExpression        { get; set; }
   public NonSequentialExpression NonSequentialExpression { get; set; }
   public override string ToString() {
     return
       "AlternativesExpression("+
         "AtomicExpression=" + this.AtomicExpression + "," +
         "NonSequentialExpression=" + this.NonSequentialExpression +
       ")";
   }     
 }

// atomic-expression ::= nested-expression
//                     | terminal-expression
//                     ;
public interface AtomicExpression : NonSequentialExpression {}

// nested-expression ::= optional-expression
//                     | repetition-expression
//                     | group-expression
//                     ;
public interface NestedExpression : AtomicExpression {}

// optional-expression ::= "[" expression "]" ;
public class OptionalExpression : NestedExpression {
  public Expression Expression;
  public override string ToString() {
    return
      "OptionalExpression(" +
        "Expression=" + this.Expression +
      ")";
  }
}

// repetition-expression ::= "{" expression "}" ;
public class RepetitionExpression : NestedExpression {
  public Expression Expression;
  public override string ToString() {
    return
      "RepetitionExpression(" +
        "Expression=" + this.Expression +
      ")";
  }
}

// group-expression ::= "(" expression ")" ;
public class GroupExpression : NestedExpression {
  public Expression Expression { get; set; }
  public override string ToString() {
    return
      "GroupExpression(" +
        "Expression=" + this.Expression +
      ")";
  }
}

// terminal-expression ::= identifier-expression
//                       | string-expression
//                       | extractor-expression
//                       ;
public interface TerminalExpression : AtomicExpression {}

// identifier-expression ::= identifier ;
public class IdentifierExpression : TerminalExpression {
  public string Identifier { get; set; }
  public override string ToString() {
    return
      "IdentifierExpression(" +
        "Identifier=" + this.Identifier +
      ")";
  }
}

// string-expression ::= [ identifier "@" ] string ;
public class StringExpression : TerminalExpression {
  public string Identifier { get; set; }
  public string String     { get; set; }
  public override string ToString() {
    return
      "StringExpression(" +
        "Identifier=" + this.Identifier + "," +
        "String="     + this.String +
      ")";
  }
}

// extractor-expression ::= "/" regex "/" ;
public class ExtractorExpression : TerminalExpression {
  public string Regex { get; set; }
  public override string ToString() {
    return
      "ExtractorExpression(" +
        "Regex=" + this.Regex +
      ")";
  }
}
