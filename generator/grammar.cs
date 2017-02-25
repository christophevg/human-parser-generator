// This file contains the bootstrap HPG EBNF-like grammar in model form. It
// allows for generating a parser that then can parse the grammar in EBNF-like
// notation and generate itself to become self-hosting ;-)
// See also: hpg.bnf
// Below the BNF grammar, the Grammar Model is also provided - The classes to
// model an EBNF-like parsed notation while bootstrapping. Once the parser is
// generated, it will also include these generated classes.
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

public class AsModel {

  public static Grammar BNF =
    new Grammar() {
  	  Rules = new List<Rule>() {
        // grammar ::= { rule } ;
  	    new Rule() {
  	      Identifier = "grammar",
  	      Expression = new RepetitionExpression() {
  	        Expression = new IdentifierExpression() {
  	          Name = null,
  	          Identifier = "rule"
  	        }
  	      }
  	    },
        // rule ::= identifier ( _ @ "::=" | _ @ "=" ) expression
        //                                              ( _ @ ";" | _ @ "." ) ;
        new Rule() {
  	      Identifier = "rule",
  	      Expression = new SequentialExpression() {
  	        AtomicExpression = new IdentifierExpression() {
  	          Name = null,
  	          Identifier = "identifier"
  	        },
  	        NonAlternativesExpression = new SequentialExpression() {
  	          AtomicExpression = new GroupExpression() {
  	            Expression = new AlternativesExpression() {
  	              NonAlternativesExpression = new StringExpression() {
  	                Name = "_",
  	                String = "::="
  	              },
  	              Expression = new StringExpression() {
  	                Name = "_",
  	                String = "="
  	              }
  	            }
  	          },
  	          NonAlternativesExpression = new SequentialExpression() {
  	            AtomicExpression = new IdentifierExpression() {
  	              Name = null,
  	              Identifier = "expression"
  	            },
  	            NonAlternativesExpression = new GroupExpression() {
                  Expression = new AlternativesExpression() {
                    NonAlternativesExpression = new StringExpression() {
                      Name = "_",
                      String = ";"
                    },
                    Expression = new StringExpression() {
                      Name = "_",
                      String = "."
                    }
                  }
  	            }
  	          }
  	        }
  	      }
  	    },
        // expression ::= alternatives-expression
        //              | non-alternatives-expression
        //              ;
        new Rule() {
  	      Identifier = "expression",
  	      Expression = new AlternativesExpression() {
  	        NonAlternativesExpression = new IdentifierExpression() {
  	          Name = null,
  	          Identifier = "alternatives-expression"
  	        },
  	        Expression = new IdentifierExpression() {
  	          Name = null,
  	          Identifier = "non-alternatives-expression"
  	        }
  	      }
  	    },
        // alternatives-expression ::= non-alternatives-expression "|" 
        //                             expression ;
        
        new Rule() {
  	      Identifier = "alternatives-expression",
  	      Expression = new SequentialExpression() {
  	        AtomicExpression = new IdentifierExpression() {
  	          Name = null,
  	          Identifier = "non-alternatives-expression"
  	        },
  	        NonAlternativesExpression = new SequentialExpression() {
  	          AtomicExpression = new StringExpression() {
  	            Name = null,
  	            String = "|"
  	          },
  	          NonAlternativesExpression = new IdentifierExpression() {
  	            Name = null,
  	            Identifier = "expression"
  	          }
  	        }
  	      }
  	    },
        // non-alternatives-expression ::= sequential-expression
        //                               | atomic-expression
        //                               ;        
        new Rule() {
  	      Identifier = "non-alternatives-expression",
  	      Expression = new AlternativesExpression() {
  	        NonAlternativesExpression = new IdentifierExpression() {
  	          Name = null,
  	          Identifier = "sequential-expression"
  	        },
  	        Expression = new IdentifierExpression() {
  	          Name = null,
  	          Identifier = "atomic-expression"
  	        }
  	      }
  	    },
        // sequential-expression ::= atomic-expression [ _ @ "," ] 
        //                           non-alternatives-expression ;
        new Rule() {
  	      Identifier = "sequential-expression",
  	      Expression = new SequentialExpression() {
  	        AtomicExpression = new IdentifierExpression() {
  	          Name = null,
  	          Identifier = "atomic-expression"
  	        },
  	        NonAlternativesExpression = new SequentialExpression() {
  	          AtomicExpression = new OptionalExpression() {
  	            Expression = new StringExpression() {
  	              Name = "_",
  	              String = ","
  	            }
  	          },
  	          NonAlternativesExpression = new IdentifierExpression() {
  	            Name = null,
  	            Identifier = "non-alternatives-expression"
  	          }
  	        }
  	      }
  	    },
        // atomic-expression ::= nested-expression
        //                     | terminal-expression
        //                     ;
        new Rule() {
  	      Identifier = "atomic-expression",
  	      Expression = new AlternativesExpression() {
  	        NonAlternativesExpression = new IdentifierExpression() {
  	          Name = null,
  	          Identifier = "nested-expression"
  	        },
  	        Expression = new IdentifierExpression() {
  	          Name = null,
  	          Identifier = "terminal-expression"
  	        }
  	      }
  	    },
        // nested-expression ::= optional-expression
        //                     | repetition-expression
        //                     | group-expression
        //                     ;
        new Rule() {
  	      Identifier = "nested-expression",
  	      Expression = new AlternativesExpression() {
  	        NonAlternativesExpression = new IdentifierExpression() {
  	          Name = null,
  	          Identifier = "optional-expression"
  	        },
  	        Expression = new AlternativesExpression() {
  	          NonAlternativesExpression = new IdentifierExpression() {
  	            Name = null,
  	            Identifier = "repetition-expression"
  	          },
  	          Expression = new IdentifierExpression() {
  	            Name = null,
  	            Identifier = "group-expression"
  	          }
  	        }
  	      }
  	    },
        // optional-expression ::= "[" expression "]" ;
        new Rule() {
  	      Identifier = "optional-expression",
  	      Expression = new SequentialExpression() {
  	        AtomicExpression = new StringExpression() {
  	          Name = null,
  	          String = "["
  	        },
  	        NonAlternativesExpression = new SequentialExpression() {
  	          AtomicExpression = new IdentifierExpression() {
  	            Name = null,
  	            Identifier = "expression"
  	          },
  	          NonAlternativesExpression = new StringExpression() {
  	            Name = null,
  	            String = "]"
  	          }
  	        }
  	      }
  	    },
        // repetition-expression ::= "{" expression "}" ;
        new Rule() {
  	      Identifier = "repetition-expression",
  	      Expression = new SequentialExpression() {
  	        AtomicExpression = new StringExpression() {
  	          Name = null,
  	          String = "{"
  	        },
  	        NonAlternativesExpression = new SequentialExpression() {
  	          AtomicExpression = new IdentifierExpression() {
  	            Name = null,
  	            Identifier = "expression"
  	          },
  	          NonAlternativesExpression = new StringExpression() {
  	            Name = null,
  	            String = "}"
  	          }
  	        }
  	      }
  	    },
        // group-expression ::= "(" expression ")" ;
        new Rule() {
  	      Identifier = "group-expression",
  	      Expression = new SequentialExpression() {
  	        AtomicExpression = new StringExpression() {
  	          Name = null,
  	          String = "("
  	        },
  	        NonAlternativesExpression = new SequentialExpression() {
  	          AtomicExpression = new IdentifierExpression() {
  	            Name = null,
  	            Identifier = "expression"
  	          },
  	          NonAlternativesExpression = new StringExpression() {
  	            Name = null,
  	            String = ")"
  	          }
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
  	        NonAlternativesExpression = new IdentifierExpression() {
  	          Name = null,
  	          Identifier = "identifier-expression"
  	        },
  	        Expression = new AlternativesExpression() {
  	          NonAlternativesExpression = new IdentifierExpression() {
  	            Name = null,
  	            Identifier = "string-expression"
  	          },
  	          Expression = new IdentifierExpression() {
  	            Name = null,
  	            Identifier = "extractor-expression"
  	          }
  	        }
  	      }
  	    },
        // identifier-expression ::= [ name ] identifier ;
        new Rule() {
  	      Identifier = "identifier-expression",
  	      Expression = new SequentialExpression() {
  	        AtomicExpression = new OptionalExpression() {
  	          Expression = new IdentifierExpression() {
  	            Name = null,
  	            Identifier = "name"
  	          }
  	        },
  	        NonAlternativesExpression = new IdentifierExpression() {
  	          Name = null,
  	          Identifier = "identifier"
  	        }
  	      }
  	    },
        // string-expression ::= [ name ] string ;
        new Rule() {
  	      Identifier = "string-expression",
  	      Expression = new SequentialExpression() {
  	        AtomicExpression = new OptionalExpression() {
  	          Expression = new IdentifierExpression() {
  	            Name = null,
  	            Identifier = "name"
  	          }
  	        },
  	        NonAlternativesExpression = new IdentifierExpression() {
  	          Name = null,
  	          Identifier = "string"
  	        }
  	      }
  	    },
        // extractor-expression ::= [ name ] "/" pattern "/" ;
        new Rule() {
  	      Identifier = "extractor-expression",
  	      Expression = new SequentialExpression() {
  	        AtomicExpression = new OptionalExpression() {
  	          Expression = new IdentifierExpression() {
  	            Name = null,
  	            Identifier = "name"
  	          }
  	        },
  	        NonAlternativesExpression = new SequentialExpression() {
  	          AtomicExpression = new StringExpression() {
  	            Name = null,
  	            String = "/"
  	          },
  	          NonAlternativesExpression = new SequentialExpression() {
  	            AtomicExpression = new IdentifierExpression() {
  	              Name = null,
  	              Identifier = "pattern"
  	            },
  	            NonAlternativesExpression = new StringExpression() {
  	              Name = null,
  	              String = "/"
  	            }
  	          }
  	        }
  	      }
  	    },
        // name ::= identifier "@" ;
        new Rule() {
  	      Identifier = "name",
  	      Expression = new SequentialExpression() {
  	        AtomicExpression = new IdentifierExpression() {
  	          Name = null,
  	          Identifier = "identifier"
  	        },
  	        NonAlternativesExpression = new StringExpression() {
  	          Name = null,
  	          String = "@"
  	        }
  	      }
  	    },
        // identifier ::= /([A-Za-z_][A-Za-z0-9-_]*)/ ;
        new Rule() {
  	      Identifier = "identifier",
  	      Expression = new ExtractorExpression() {
  	        Name = null,
  	        Pattern = "([A-Za-z_][A-Za-z0-9-_]*)"
  	      }
  	    },
        // string ::= /"([^"]*)"|^'([^']*)'/ ;
        new Rule() {
  	      Identifier = "string",
  	      Expression = new ExtractorExpression() {
  	        Name = null,
  	        Pattern = "\"([^\"]*)\"|^'([^']*)'"
  	      }
  	    },
        // pattern ::= /(.*?)(?<keep>/\s*;)/ ;
        new Rule() {
  	      Identifier = "pattern",
  	      Expression = new ExtractorExpression() {
  	        Name = null,
  	        Pattern = "(.*?)(?<keep>/\\s*;)"
  	      }
  	    }
      } // Rules
    }; // grammar
}

// The classes below are also part of the bootstrap process, once a parser
// is generated, it also generates these classes.

// grammar ::= { rule } ;
public class Grammar {
  public List<Rule> Rules { get; set; }
  public Grammar() {
    this.Rules = new List<Rule>();
  }
  public override string ToString() {
    return
    "new Grammar() { " +
    "Rules = new List<Rule>() {" +
    string.Join(",", this.Rules.Select(x => x.ToString())) +
    "}" +
    "}";
  }
}

// rule ::= identifier ( _ @ \"::=\" | _ @ \"=\" ) expression \";\" ;
public class Rule {
  public string Identifier { get; set; }
  public Expression Expression { get; set; }
  public override string ToString() {
    return
    "new Rule() { \n" +
    "Identifier = " + Format.Literal(this.Identifier) + ",\n" +
    "Expression = " + ( this.Expression == null ? "null" :
                        this.Expression.ToString() ) +
    "}";
  }
}

// expression ::= alternatives-expression | non-alternatives-expression ;
public interface Expression {}

// alternatives-expression ::= non-alternatives-expression \"|\" expression ;
public class AlternativesExpression : Expression {
  public NonAlternativesExpression NonAlternativesExpression { get; set; }
  public Expression Expression { get; set; }
  public override string ToString() {
    return
    "new AlternativesExpression() { \n" +
    "NonAlternativesExpression = " + ( this.NonAlternativesExpression == null ?
                                       "null" : this.NonAlternativesExpression.ToString() ) + ",\n" +
    "Expression = " + ( this.Expression == null ? "null" :
                        this.Expression.ToString() ) +
    "}";
  }
}

// non-alternatives-expression ::= sequential-expression | atomic-expression ;
public interface NonAlternativesExpression : Expression {}

// sequential-expression ::= atomic-expression [ _ @ \",\" ] non-alternatives-expression ;
public class SequentialExpression : NonAlternativesExpression {
  public AtomicExpression AtomicExpression { get; set; }
  public NonAlternativesExpression NonAlternativesExpression { get; set; }
  public override string ToString() {
    return
    "new SequentialExpression() { \n" +
    "AtomicExpression = " + ( this.AtomicExpression == null ? "null" :
                              this.AtomicExpression.ToString() ) + ",\n" +
    "NonAlternativesExpression = " + ( this.NonAlternativesExpression == null ?
                                       "null" : this.NonAlternativesExpression.ToString() ) +
    "}";
  }
}

// atomic-expression ::= nested-expression | terminal-expression ;
public interface AtomicExpression : NonAlternativesExpression {}

// nested-expression ::= optional-expression | repetition-expression | group-expression ;
public interface NestedExpression : AtomicExpression {}

// optional-expression ::= \"[\" expression \"]\" ;
public class OptionalExpression : NestedExpression {
  public Expression Expression { get; set; }
  public override string ToString() {
    return
    "new OptionalExpression() { " +
    "Expression = " + ( this.Expression == null ? "null" :
                        this.Expression.ToString() ) +
    "}";
  }
}

// repetition-expression ::= \"{\" expression \"}\" ;
public class RepetitionExpression : NestedExpression {
  public Expression Expression { get; set; }
  public override string ToString() {
    return
    "new RepetitionExpression() { " +
    "Expression = " + ( this.Expression == null ? "null" :
                        this.Expression.ToString() ) +
    "}";
  }
}

// group-expression ::= \"(\" expression \")\" ;
public class GroupExpression : NestedExpression {
  public Expression Expression { get; set; }
  public override string ToString() {
    return
    "new GroupExpression() { " +
    "Expression = " + ( this.Expression == null ? "null" :
                        this.Expression.ToString() ) +
    "}";
  }
}

// terminal-expression ::= identifier-expression | string-expression | extractor-expression ;
public interface TerminalExpression : AtomicExpression {}

// identifier-expression ::= [ name ] identifier ;
public class IdentifierExpression : TerminalExpression {
  public string Name { get; set; }
  public string Identifier { get; set; }
  public override string ToString() {
    return
    "new IdentifierExpression() { \n" +
    "Name = " + Format.Literal(this.Name) + ",\n" +
    "Identifier = " + Format.Literal(this.Identifier) +
    "}";
  }
}

// string-expression ::= [ name ] string ;
public class StringExpression : TerminalExpression {
  public string Name { get; set; }
  public string String { get; set; }
  public override string ToString() {
    return
    "new StringExpression() { \n" +
    "Name = " + Format.Literal(this.Name) + ",\n" +
    "String = " + Format.Literal(this.String) +
    "}";
  }
}

// extractor-expression ::= [ name ] \"/\" pattern \"/\" ;
public class ExtractorExpression : TerminalExpression {
  public string Name { get; set; }
  public string Pattern { get; set; }
  public override string ToString() {
    return
    "new ExtractorExpression() { \n" +
    "Name = " + Format.Literal(this.Name) + ",\n" +
    "Pattern = " + Format.Literal(this.Pattern) +
    "}";
  }
}
