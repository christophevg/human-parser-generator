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

  private void processAndCompare(string input, Grammar grammar, string model ) {
    Grammar g = this.parse(input);
    if(grammar != null) { Assert.AreEqual( grammar.ToString(), g.ToString() ); }
    Model m = this.transform(g);
    if(model != null) {
      Assert.AreEqual( model.Replace(" ", "").Replace("\n", ""), m.ToString() );
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
                          { assignment }
                          ""END.""
                        ;

assignment            ::= identifier "":="" expression "";"" ;

expression            ::= identifier
                        | string
                        | number
                        ;

identifier            ::= name  @ /([A-Z][A-Z0-9]*)/ ;
string                ::= text  @ /""([^""]*)""|'([^']*)'/ ;
number                ::= value @ /(-?[1-9][0-9]*)/ ;

      ",
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "program",
            Expression = new SequentialExpression() {
              NonSequentialExpression = new StringExpression() {
                Name = null,
                String = "PROGRAM"
              },
              Expression = new SequentialExpression() {
                NonSequentialExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "identifier"
                },
                Expression = new SequentialExpression() {
                  NonSequentialExpression = new StringExpression() {
                    Name = null,
                    String = "BEGIN"
                  },
                  Expression = new SequentialExpression() {
                    NonSequentialExpression = new RepetitionExpression() {
                      Expression = new IdentifierExpression() {
                        Name = null,
                        Identifier = "assignment"
                      }
                    },
                    Expression = new StringExpression() {
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
              NonSequentialExpression = new IdentifierExpression() {
                Name = null,
                Identifier = "identifier"
              },
              Expression = new SequentialExpression() {
                NonSequentialExpression = new StringExpression() {
                  Name = null,
                  String = ":="
                },
                Expression = new SequentialExpression() {
                  NonSequentialExpression = new IdentifierExpression() {
                    Name = null,
                    Identifier = "expression"
                  },
                  Expression = new StringExpression() {
                    Name = null,
                    String = ";"
                  }
                }
              }
            }
          },new Rule() {
            Identifier = "expression",
            Expression = new AlternativesExpression() {
              AtomicExpression = new IdentifierExpression() {
                Name = null,
                Identifier = "identifier"
              },
              NonSequentialExpression = new AlternativesExpression() {
                AtomicExpression = new IdentifierExpression() {
                  Name = null,
                  Identifier = "string"
                },
                NonSequentialExpression = new IdentifierExpression() {
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
      @"
Model(
  Entities=[
    Entity(
      Name=program,Type=program,Properties=[
        Property(Name=identifier,Type=identifier,Source=ConsumeEntity(identifier)->identifier),
        Property(Name=assignment,Type=assignment,IsPlural,Source=ConsumeEntity(assignment)*->assignment)
      ],ParseAction=ConsumeAll([
        ConsumeString(PROGRAM),
        ConsumeEntity(identifier)->identifier,
        ConsumeString(BEGIN),
        ConsumeEntity(assignment)*->assignment,
        ConsumeString(END.) 
      ])
    ),
    Entity(
      Name=assignment,Type=assignment,Properties=[
        Property(Name=identifier,Type=identifier,Source=ConsumeEntity(identifier)->identifier),
        Property(Name=expression,Type=expression,Source=ConsumeEntity(expression)->expression)
      ],ParseAction=ConsumeAll([
        ConsumeEntity(identifier)->identifier,
        ConsumeString(:=),
        ConsumeEntity(expression)->expression,
        ConsumeString(;)
      ])
    ),
    VirtualEntity(
      Name=expression,Type=expression,Subs=[identifier,string,number],Properties=[
        Property(Name=alternative,Type=expression,Source=ConsumeAny([
          ConsumeEntity(identifier)->alternative,
          ConsumeEntity(string)->alternative,
          ConsumeEntity(number)->alternative
        ])->alternative)
      ],ParseAction=ConsumeAny([
        ConsumeEntity(identifier)->alternative,
        ConsumeEntity(string)->alternative,
        ConsumeEntity(number)->alternative
      ])->alternative
    ),
    Entity(
      Name=identifier,Type=identifier,Supers=[expression],Properties=[
       Property(Name=name,Type=<string>,Source=ConsumePattern(([A-Z][A-Z0-9]*))->name)
      ],ParseAction=ConsumePattern(([A-Z][A-Z0-9]*))->name
    ),
    Entity(
      Name=string,Type=string,Supers=[expression],Properties=[
       Property(Name=text,Type=<string>,Source=ConsumePattern(""([^""]*)""|'([^']*)')->text)
      ],ParseAction=ConsumePattern(""([^""]*)""|'([^']*)')->text
    ),
    Entity(
      Name=number,Type=number,Supers=[expression],Properties=[
       Property(Name=value,Type=<string>,Source=ConsumePattern((-?[1-9][0-9]*))->value)
      ],ParseAction=ConsumePattern((-?[1-9][0-9]*))->value
    )
  ],
  Root=program
)
      "
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
identifier       ::= /([A-Z][A-Z0-9]*)/ ;
string           ::= /""([^""]*)""|'([^']*)'/ ;
int              ::= /(-?[1-9][0-9]*)/ ;"
    );

    Assert.IsTrue  (             model["literal"].IsVirtual );
    Assert.AreEqual( "literal",  model["literal"].Type      );

    Assert.IsFalse (             model["variable"].IsVirtual);
    Assert.AreEqual( "variable", model["variable"].Type      );

    Assert.IsTrue( model["value"].IsVirtual );
  }

  [Test]
  public void testPropertiesFromStringExpression() {
    this.processAndCompare(
      @"
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
            NonSequentialExpression = new StringExpression() {
              Name = null,
              String = "SIGN"
            },
            Expression = new SequentialExpression() {
              NonSequentialExpression = new OptionalExpression() {
                Expression = new StringExpression() {
                  Name = "_",
                  String = "IS"
                }
              },
              Expression = new SequentialExpression() {
                NonSequentialExpression = new GroupExpression() {
                  Expression = new AlternativesExpression() {
                    AtomicExpression = new StringExpression() {
                      Name = null,
                      String = "LEADING"
                    },
                    NonSequentialExpression = new StringExpression() {
                      Name = null,
                      String = "TRAILING"
                    }
                  }
                },
                Expression = new OptionalExpression() {
                  Expression = new SequentialExpression() {
                    NonSequentialExpression = new StringExpression() {
                      Name = null,
                      String = "SEPARATE"
                    },
                    Expression = new OptionalExpression() {
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
      @"
Model(
  Entities=[
    Entity(
      Name=sign-option,Type=sign-option,Properties=[
        Property(Name=has-LEADING,Type=<bool>,Source=ConsumeString(LEADING)!->has-LEADING),
        Property(Name=has-TRAILING,Type=<bool>,Source=ConsumeString(TRAILING)!->has-TRAILING),
        Property(Name=has-SEPARATE,Type=<bool>,Source=ConsumeString(SEPARATE)!->has-SEPARATE),
        Property(Name=has-CHARACTER,Type=<bool>,IsOptional,Source=ConsumeString(CHARACTER)?!->has-CHARACTER)
      ],
      ParseAction=ConsumeAll([
        ConsumeString(SIGN),
        ConsumeString(IS)?,
        ConsumeAny([
          ConsumeString(LEADING)!->has-LEADING,
          ConsumeString(TRAILING)!->has-TRAILING]),
          ConsumeAll([
            ConsumeString(SEPARATE)!->has-SEPARATE,
            ConsumeString(CHARACTER)?!->has-CHARACTER
          ])?
      ])
    )
  ],
  Root=sign-option
)"
    );
  }

  [Test]
  public void testComplexRepetitions() {
    Model model = this.process(
      "grammar = { \"prefix\" rule }; rule = /a/;"
    );
    Assert.IsFalse( model["grammar"]["rule"].IsPlural);

    Assert.IsTrue ( model["grammar"]["rule"].Source.HasPluralParent);
  }

  [Test]
  public void testNestedRepetitions() {
    Model model = this.process(
      "grammar = { rule { postfix } }; rule = /a/; postfix = /b/;"
    );
    Assert.IsFalse( model["grammar"]["rule"].IsPlural);

    Assert.IsTrue ( model["grammar"]["postfix"].IsPlural);
    Assert.IsTrue ( model["grammar"]["rule"].Source.HasPluralParent);
    Assert.IsTrue ( model["grammar"]["postfix"].Source.HasPluralParent);
  }

}
