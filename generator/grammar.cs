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
        // identifier-expression ::= [ name ] identifier ;
        new Rule() {
          Identifier = "identifier-expression",
          Expression = new SequentialExpression() {
            NonSequentialExpression = new OptionalExpression() {
              Expression = new IdentifierExpression() { Identifier = "name" }
            },
            Expression = new IdentifierExpression() { Identifier = "identifier" }
          }
        },
        // string-expression ::= [ name ] string ;
        new Rule() {
          Identifier = "string-expression",
          Expression = new SequentialExpression() {
            NonSequentialExpression = new OptionalExpression() {
              Expression = new IdentifierExpression() { Identifier = "name" }
            },
            Expression = new IdentifierExpression() { Identifier = "string" }
          }
        },
        // extractor-expression ::= [ name ] "/" pattern "/" ;
        new Rule() {
          Identifier = "extractor-expression",
          Expression = new SequentialExpression() {
            NonSequentialExpression = new OptionalExpression() {
              Expression = new IdentifierExpression() { Identifier = "name" }
            },
            Expression              = new SequentialExpression() {
              NonSequentialExpression = new StringExpression() { String = "/" },
              Expression              = new SequentialExpression() {
                NonSequentialExpression = new IdentifierExpression() { Identifier = "pattern" },
                Expression              = new StringExpression() { String = "/" }
              }
            }
          }
        },
        // name ::= identifier "@" ;
        new Rule() {
          Identifier = "name",
          Expression = new SequentialExpression() {
            NonSequentialExpression = new IdentifierExpression() { Identifier = "identifier" },
            Expression              = new StringExpression() { String = "@" }
          }
        },
        // identifier ::= /([A-Za-z][A-Za-z0-9-]*)/ ;
        new Rule() {
          Identifier = "identifier",
          Expression = new ExtractorExpression() { Pattern = @"([A-Za-z][A-Za-z0-9-]*)" }
        },
        // string ::= /"([^"]*)"|^'([^']*)'/ ;
        new Rule() {
          Identifier = "string",
          Expression = new ExtractorExpression() { Pattern = @"""([^""]*)""|^'([^']*)'" }
        },
        // pattern ::= /(.*?)(?<keep>/\s*;)/ ;
        new Rule() {
          Identifier = "pattern",
          Expression = new ExtractorExpression() { Pattern = @"(.*?)(?<keep>/\s*;)" }
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

// identifier-expression ::= [ name ] identifier ;
// name                  ::= identifier "@"
public class IdentifierExpression : TerminalExpression {
  public string Name { get; set; }  
  public string Identifier { get; set; }
  public override string ToString() {
    return
      "IdentifierExpression(" +
        "Name="       + this.Name       + "," +   
        "Identifier=" + this.Identifier +
      ")";
  }
}

// string-expression ::= [ name ] string ;
// name                 ::= identifier "@";
public class StringExpression : TerminalExpression {
  public string Name { get; set; }
  public string String     { get; set; }
  public override string ToString() {
    return
      "StringExpression(" +
        "Name="   + this.Name   + "," +
        "String=" + this.String +
      ")";
  }
}

// extractor-expression ::= [ name ] "/" regex "/" ;
// name                 ::= identifier "@";
public class ExtractorExpression : TerminalExpression {
  public string Name { get; set; }
  public string Pattern { get; set; }
  public override string ToString() {
    return
      "ExtractorExpression(" +
        "Name="  + this.Name + "," +
        "Pattern=" + this.Pattern +
      ")";
  }
}
