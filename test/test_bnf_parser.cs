// unit tests for Generator Model Factory
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using NUnit.Framework;

using System.Collections.Generic;

using HumanParserGenerator.Generator;

[TestFixture]
public class BNFParserTests {

  private Grammar parse(string input) {
    return new Parser().Parse(input).AST;
  }

  private Model transform(Grammar grammar) {
    return new Factory().Import(grammar).Model;
  }

  private void processAndCompare(string input, Grammar grammar, Model model ) {
    Grammar g = this.parse(input);
    if(grammar != null) { Assert.AreEqual( grammar.ToString(), g.ToString() ); }
    Model m = this.transform(g);
    if(model != null) {
      Assert.AreEqual( model.ToString(), m.ToString() );
    }
  }

  private Model process(string input) {
    return this.transform(this.parse(input));
  }

  [Test]
  public void testPascalGrammar() {
    this.processAndCompare(
      @"
program               ::= ""PROGRAM"" identifier
                          ""BEGIN""
                          { assignment "";"" }
                          ""END.""
                        ;

assignment            ::= identifier "":="" expression ;

expression            ::= identifier
                        | string
                        | number
                        ;

identifier            ::= name  @ ? /([A-Z][A-Z0-9]*)/ ? ;
string                ::= text  @ ? /""([^""]*)""|'([^']*)'/ ? ;
number                ::= value @ ? /(-?[1-9][0-9]*)/ ? ;",
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "program",
            Expression = new SequentialExpression() {
              AtomicExpression = new StringExpression() {
                Name = null,
                String = "PROGRAM"
              },
              NonAlternativesExpression = new SequentialExpression() {
                AtomicExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "identifier"
                },
                NonAlternativesExpression = new SequentialExpression() {
                  AtomicExpression = new StringExpression() {
                    Name = null,
                    String = "BEGIN"
                  },
                  NonAlternativesExpression = new SequentialExpression() {
                    AtomicExpression = new RepetitionExpression() {
                      Expression = new SequentialExpression() {
                        AtomicExpression = new IdentifierExpression() {
                          Name = null,
                          Identifier = "assignment"
                        },
                        NonAlternativesExpression = new StringExpression() {
                          Name = null,
                          String = ";"
                        }
                      }
                    },
                    NonAlternativesExpression = new StringExpression() {
                      Name = null,
                      String = "END."
                    }
                  }
                }
              }
            }
          },new Rule() {
            Identifier = "assignment",
            Expression = new SequentialExpression() {
              AtomicExpression = new IdentifierExpression() {
                Name = null,
                Identifier = "identifier"
              },
              NonAlternativesExpression = new SequentialExpression() {
                AtomicExpression = new StringExpression() {
                  Name = null,
                  String = ":="
                },
                NonAlternativesExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "expression"
                }
              }
            }
          },new Rule() {
            Identifier = "expression",
            Expression = new AlternativesExpression() {
              NonAlternativesExpression = new IdentifierExpression() {
                Name = null,
                Identifier = "identifier"
              },
              Expression = new AlternativesExpression() {
                NonAlternativesExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "string"
                },
                Expression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "number"
                }
              }
            }
          },new Rule() {
            Identifier = "identifier",
            Expression = new ExtractorExpression() {
              Name = "name",
              Pattern = "([A-Z][A-Z0-9]*)"
            }
          },new Rule() {
            Identifier = "string",
            Expression = new ExtractorExpression() {
              Name = "text",
              Pattern = "\"([^\"]*)\"|'([^']*)'"
            }
          },new Rule() {
            Identifier = "number",
            Expression = new ExtractorExpression() {
              Name = "value",
              Pattern = "(-?[1-9][0-9]*)"
            }
          }
        }
      },
      new Model() {
        Entities = new List<Entity>() {
          new Entity() {
            Rule = new Rule() {
              Identifier = "program",
              Expression = new SequentialExpression() {
                AtomicExpression = new StringExpression() {
                  Name = null,
                  String = "PROGRAM"
                },
                NonAlternativesExpression = new SequentialExpression() {
                  AtomicExpression = new IdentifierExpression() {
                    Name = null,
                    Identifier = "identifier"
                  },
                  NonAlternativesExpression = new SequentialExpression() {
                    AtomicExpression = new StringExpression() {
                      Name = null,
                      String = "BEGIN"
                    },
                    NonAlternativesExpression = new SequentialExpression() {
                      AtomicExpression = new RepetitionExpression() {
                        Expression = new SequentialExpression() {
                          AtomicExpression = new IdentifierExpression() {
                            Name = null,
                            Identifier = "assignment"
                          },
                          NonAlternativesExpression = new StringExpression() {
                            Name = null,
                            String = ";"
                          }
                        }
                      },
                      NonAlternativesExpression = new StringExpression() {
                        Name = null,
                        String = "END."
                      }
                    }
                  }
                }
              }
            },
            Name = "program",
            Properties = new List<Property>() {
              new Property() {
                Name = "identifier",
                Source = new ConsumeEntity() {
                  Reference = "identifier"
                }
              },
              new Property() {
                Name = "assignment",
                Source = new ConsumeEntity() {
                  Reference = "assignment"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAll() {
              Actions = new List<ParseAction>() {
                new ConsumeString() { String = "PROGRAM" },
                new ConsumeEntity() { Reference = "identifier" },
                new ConsumeString() { String = "BEGIN" },
                new ConsumeAll() {
                  IsPlural = true,
                  Actions = new List<ParseAction>() {
                    new ConsumeEntity() { Reference = "assignment" },
                    new ConsumeString() {
                      String = ";"
                    }
                  }
                },
                new ConsumeString() {
                  String = "END."
                }
              }
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>()
          },
          new Entity() {
            Rule = new Rule() {
              Identifier = "assignment",
              Expression = new SequentialExpression() {
                AtomicExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "identifier"
                },
                NonAlternativesExpression = new SequentialExpression() {
                  AtomicExpression = new StringExpression() {
                    Name = null,
                    String = ":="
                  },
                  NonAlternativesExpression = new IdentifierExpression() {
                    Name = null,
                    Identifier = "expression"
                  }
                }
              }
            },
            Name = "assignment",
            Properties = new List<Property>() {
              new Property() {
                Name = "identifier",
                Source = new ConsumeEntity() {
                  Reference = "identifier"
                }
              },
              new Property() {
                Name = "expression",
                Source = new ConsumeEntity() {
                  Reference = "expression"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAll() {
              Actions = new List<ParseAction>() {
                new ConsumeEntity() { Reference = "identifier" },
                new ConsumeString() { String = ":=" },
                new ConsumeEntity() {
                  Reference = "expression"
                }
              }
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>()
          },
          // virtual
          new Entity() {
            Rule = new Rule() {
              Identifier = "expression",
              Expression = new AlternativesExpression() {
                NonAlternativesExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "identifier"
                },
                Expression = new AlternativesExpression() {
                  NonAlternativesExpression = new IdentifierExpression() {
                    Name = null,
                    Identifier = "string"
                  },
                  Expression = new IdentifierExpression() {
                    Name = null,
                    Identifier = "number"
                  }
                }
              }
            },
            Name = "expression",
            Properties = new List<Property>() {
              new Property() {
                Name = "alternative",
                Source = new ConsumeAny() {
                  Actions = new List<ParseAction>() {
                    new ConsumeEntity() {
                      Reference = "identifier"
                    },
                    new ConsumeEntity() {
                      Reference = "string"
                    },
                    new ConsumeEntity() {
                      Reference = "number"
                    }
                  }
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAny() {
              Actions = new List<ParseAction>() {
                new ConsumeEntity() {
                  Reference = "identifier"
                },
                new ConsumeEntity() {
                  Reference = "string"
                },
                new ConsumeEntity() {
                  Reference = "number"
                }
              }
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>() {
              "identifier", "string", "number"
            }
          },
          new Entity() {
            Rule = new Rule() {
              Identifier = "identifier",
              Expression = new ExtractorExpression() {
                Name = "name",
                Pattern = "([A-Z][A-Z0-9]*)"
              }
            },
            Name = "identifier",
            Properties = new List<Property>() {
              new Property() {
                Name = "name",
                Source = new ConsumePattern() {
                  Pattern = "([A-Z][A-Z0-9]*)"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumePattern() {
              Pattern = "([A-Z][A-Z0-9]*)"
            },
            Supers = new HashSet<string>() {
              "expression"
            },
            Subs = new HashSet<string>()
          },
          new Entity() {
            Rule = new Rule() {
              Identifier = "string",
              Expression = new ExtractorExpression() {
                Name = "text",
                Pattern = "\"([^\"]*)\"|'([^']*)'"
              }
            },
            Name = "string",
            Properties = new List<Property>() {
              new Property() {
                Name = "text",
                Source = new ConsumePattern() {
                  Pattern = "\"([^\"]*)\"|'([^']*)'"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumePattern() {
              Pattern = "\"([^\"]*)\"|'([^']*)'"
            },
            Supers = new HashSet<string>() {
              "expression"
            },
            Subs = new HashSet<string>()
          },
          new Entity() {
            Rule = new Rule() {
              Identifier = "number",
              Expression = new ExtractorExpression() {
                Name = "value",
                Pattern = "(-?[1-9][0-9]*)"
              }
            },
            Name = "number",
            Properties = new List<Property>() {
              new Property() {
                Name = "value",
                Source = new ConsumePattern() {
                  Pattern = "(-?[1-9][0-9]*)"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumePattern() { Pattern = "(-?[1-9][0-9]*)" },
            Supers = new HashSet<string>() { "expression" },
            Subs = new HashSet<string>()
          }
        },
        RootName = "program"
      }
    );
  }

  [Test]
  public void testCobolValueDefinition() {
    Model model = this.process(
      @"
copybook         ::= { sentence };
sentence         ::= record ""."";
record           ::= renames-record | values-record ;

renames-record   ::= ""66"" level-name ""RENAMES"" identifier-range ;
level-name       ::= identifier | ""FILLER"" ;
identifier-range ::= identifier ""THRU"" identifier ;

values-record    ::= ""88"" level-name ""VALUES"" { value } ;
value            ::= literal | variable ;
literal          ::= int | string ;
variable         ::= identifier [ ""("" subset "")"" ];
subset           ::= numeric [ "":"" subset ];
numeric          ::= int | identifier;
identifier       ::= ? /([A-Z][A-Z0-9]*)/ ? ;
string           ::= ? /""([^""]*)""|'([^']*)'/ ? ;
int              ::= ? /(-?[1-9][0-9]*)/ ? ;"
    );

    Assert.IsTrue  (             model["literal"].IsVirtual );
    Assert.AreEqual( "literal",  model["literal"].Type      );

    Assert.IsFalse (             model["variable"].IsVirtual);
    Assert.AreEqual( "variable", model["variable"].Type      );

    Assert.IsTrue( model["value"].IsVirtual );
  }

  [Test]
  public void testPropertiesFromStringExpression() {
    this.processAndCompare( @"
sign-option ::=
   ""SIGN"" [ _ @ ""IS"" ]
   ( ""LEADING"" | ""TRAILING"" )
   [ ""SEPARATE"" [ ""CHARACTER"" ] ]
;",
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "sign-option",
            Expression = new SequentialExpression() {
              AtomicExpression = new StringExpression() {
                Name = null,
                String = "SIGN"
              },
              NonAlternativesExpression = new SequentialExpression() {
                AtomicExpression = new OptionalExpression() {
                  Expression = new StringExpression() {
                    Name = "_",
                    String = "IS"
                  }
                },
                NonAlternativesExpression = new SequentialExpression() {
                  AtomicExpression = new GroupExpression() {
                    Expression = new AlternativesExpression() {
                      NonAlternativesExpression = new StringExpression() {
                        Name = null,
                        String = "LEADING"
                      },
                      Expression = new StringExpression() {
                        Name = null,
                        String = "TRAILING"
                      }
                    }
                  },
                  NonAlternativesExpression = new OptionalExpression() {
                    Expression = new SequentialExpression() {
                      AtomicExpression = new StringExpression() {
                        Name = null,
                        String = "SEPARATE"
                      },
                      NonAlternativesExpression = new OptionalExpression() {
                        Expression = new StringExpression() {
                          Name = null,
                          String = "CHARACTER"
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      },
      new Model() {
        Entities = new List<Entity>() {
          new Entity() {
            Rule = new Rule() {
              Identifier = "sign-option",
              Expression = new SequentialExpression() {
                AtomicExpression = new StringExpression() {
                  Name = null,
                  String = "SIGN"
                },
                NonAlternativesExpression = new SequentialExpression() {
                  AtomicExpression = new OptionalExpression() {
                    Expression = new StringExpression() {
                      Name = "_",
                      String = "IS"
                    }
                  },
                  NonAlternativesExpression = new SequentialExpression() {
                    AtomicExpression = new GroupExpression() {
                      Expression = new AlternativesExpression() {
                        NonAlternativesExpression = new StringExpression() {
                          Name = null,
                          String = "LEADING"
                        },
                        Expression = new StringExpression() {
                          Name = null,
                          String = "TRAILING"
                        }
                      }
                    },
                    NonAlternativesExpression = new OptionalExpression() {
                      Expression = new SequentialExpression() {
                        AtomicExpression = new StringExpression() {
                          Name = null,
                          String = "SEPARATE"
                        },
                        NonAlternativesExpression = new OptionalExpression() {
                          Expression = new StringExpression() {
                            Name = null,
                            String = "CHARACTER"
                          }
                        }
                      }
                    }
                  }
                }
              }
            },
            Name = "sign-option",
            Properties = new List<Property>() {
              new Property() {
                Name = "has-LEADING",
                Source = new ConsumeString() {
                  ReportSuccess = true,
                  String = "LEADING"
                }
              },
              new Property() {
                Name = "has-TRAILING",
                Source = new ConsumeString() {
                  ReportSuccess = true,
                  String = "TRAILING"
                }
              },
              new Property() {
                Name = "has-SEPARATE",
                Source = new ConsumeString() {
                  ReportSuccess = true,
                  String = "SEPARATE"
                }
              },
              new Property() {
                Name = "has-CHARACTER",
                Source = new ConsumeString() {
                  IsOptional = true, ReportSuccess = true,
                  String = "CHARACTER"
                }
              }
            } .AsReadOnly(),
            ParseAction = new ConsumeAll() {
              Actions = new List<ParseAction>() {
                new ConsumeString() {
                  String = "SIGN"
                },
                new ConsumeString() {
                  IsOptional = true,
                  String = "IS"
                },
                new ConsumeAny() {
                  Actions = new List<ParseAction>() {
                    new ConsumeString() {
                      ReportSuccess = true,
                      String = "LEADING"
                    },
                    new ConsumeString() {
                      ReportSuccess = true,
                      String = "TRAILING"
                    }
                  }
                },
                new ConsumeAll() {
                  IsOptional = true,
                  Actions = new List<ParseAction>() {
                    new ConsumeString() {
                      ReportSuccess = true,
                      String = "SEPARATE"
                    },
                    new ConsumeString() {
                      IsOptional = true, ReportSuccess = true,
                      String = "CHARACTER"
                    }
                  }
                }
              }
            },
            Supers = new HashSet<string>(),
            Subs = new HashSet<string>()
          }
        },
        RootName = "sign-option"
      }
    );
  }

  [Test]
  public void testComplexRepetitions() {
    Model model = this.process(
      "grammar = { \"prefix\" rule }; rule = ? /a/ ? ;"
    );
    Assert.IsFalse( model["grammar"]["rule"].IsPlural);

    Assert.IsTrue ( model["grammar"]["rule"].Source.HasPluralParent);
  }

  [Test]
  public void testNestedRepetitions() {
    Model model = this.process(
      "grammar = { rule { postfix } }; rule = ? /a/ ?; postfix = ? /b/ ?;"
    );
    Assert.IsFalse( model["grammar"]["rule"].IsPlural);

    Assert.IsTrue ( model["grammar"]["postfix"].IsPlural);
    Assert.IsTrue ( model["grammar"]["rule"].Source.HasPluralParent);
    Assert.IsTrue ( model["grammar"]["postfix"].Source.HasPluralParent);
  }

  [Test]
  public void testAlternativeRuleTermination() {
    Model model = this.process(
      @"
grammar    ::= { rule } .
rule         = lhs @ identifier ""="" rhs @ identifier .
identifier ::= ? /([A-Z][A-Z0-9]*)/ ? ;
"
    );
    Assert.AreEqual(3, model.Entities.Count);
  }

  [Test]
  public void testComments() {
   this.process(
      @"
(* leading comment *)
grammar    ::= { rule } .
rule         = lhs @ identifier ""="" (* inline comment *) rhs @ identifier .
identifier ::= ? /([A-Z][A-Z0-9]*)/ ? ;
(* trailing comment *)
"
    );
  }

  [Test]
  public void testAlternativeSyntaxForRuleNames() {
    Model model = this.process(
      @"
<grammar>    ::= { <rule> } .
<rule>         = lhs @ <identifier> ""="" rhs @ identifier .
identifier ::= ? /([A-Z][A-Z0-9]*)/ ? ;
"
    );
    Assert.AreEqual(3, model.Entities.Count);
  }

}
