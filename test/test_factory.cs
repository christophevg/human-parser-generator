// unit tests for Generator Model Factory
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using NUnit.Framework;

using System.Collections.Generic;

using HumanParserGenerator.Generator; // for Model, Factory

[TestFixture]
public class GeneratorModelFactoryTests {

  private void importAndCompare(Model model, string expected) {
    Assert.AreEqual(
      expected.Replace(" ", "").Replace("\n", ""),
      model.ToString()
    );
  }
  
  private void importAndCompare(Grammar grammar, string expected) {
    Model model = new Factory().Import(grammar).Model;
    Assert.AreEqual(
      expected.Replace(" ", "").Replace("\n", ""),
      model.ToString()
    );
  }

  [Test]
  public void testEmptyModel() {
    this.importAndCompare( new Factory().Model, "Model(Entities=[],Root=)" );
  }

  [Test]
  public void testMinimalModelWithoutProperty() {
    // rule ::= "a"
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule",
            Expression = new StringExpression() { String = "a" }
          }
        }
      },
      @"Model(
         Entities=[
           Entity(Name=rule,Type=rule,ParseAction=ConsumeString(a))
         ],
         Root=rule
       )"
    );
  }

  [Test]
  public void testMinimalModelWithProperty() {
    // rule ::= StringProperty@"a"
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule",
            Expression = new StringExpression() {
              Name   = "StringProperty",
              String = "a"
            }
          }
        }
      },
      @"Model(
         Entities=[
           Entity(
             Name=rule,Type=rule,
             Properties=[
               Property(
                 Name=StringProperty,Type=<string>,
                 Source=ConsumeString(a)->StringProperty
               )
             ],
             ParseAction=ConsumeString(a)->StringProperty
           )
        ],
        Root=rule
      )"
    );
  }

  [Test]
  public void testSimpleIdentifierExpressionIndirection() {
    // rule1 ::= rule2
    // rule2 ::= /a/
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule1",
            Expression = new IdentifierExpression() { Identifier = "rule2" }
          },
          new Rule() {
            Identifier = "rule2",
            Expression = new ExtractorExpression() { Pattern = "a" }
          }
        }
      },
      @"Model(
         Entities=[
           Entity(
             Name=rule1,Type=rule1,Subs=[rule2],
             Properties=[Property(Name=rule2,Type=<string>,Source=ConsumeEntity(rule2)->rule2)],
             ParseAction=ConsumeEntity(rule2)->rule2
           ),
           VirtualEntity(
             Name=rule2,Type=<string>,Supers=[rule1],
             Properties=[Property(Name=rule2,Type=<string>,Source=ConsumePattern(a)->rule2)],
             ParseAction=ConsumePattern(a)->rule2
           )
        ],
        Root=rule1
      )"
    );
  }

  [Test]
  public void testMinimalExtractorWithoutProperty() {
    // rule ::= /[A-Za-z0-9-]*/
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule",
            Expression = new ExtractorExpression() { Pattern = "[A-Za-z0-9-]*" }
          }
        }
      },
      @"Model(
         Entities=[
           Entity(
             Name=rule,Type=rule,
             Properties=[Property(Name=rule,Type=<string>,Source=ConsumePattern([A-Za-z0-9-]*)->rule)],
             ParseAction=ConsumePattern([A-Za-z0-9-]*)->rule
           )
        ],
        Root=rule
      )"
    );
  }

  [Test]
  public void testMinimalExtractorWithProperty() {
    // rule ::= PatternProperty/[A-Za-z0-9-]*/
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule",
            Expression = new ExtractorExpression() {
              Name  = "PatternProperty",
              Pattern = "[A-Za-z0-9-]*"
            }
          }
        }
      },
      @"Model(
         Entities=[
           Entity(
             Name=rule,Type=rule,
             Properties=[
               Property(
                 Name=PatternProperty,Type=<string>,
                 Source=ConsumePattern([A-Za-z0-9-]*)->PatternProperty
               )
             ],
             ParseAction=ConsumePattern([A-Za-z0-9-]*)->PatternProperty
           )
        ],
        Root=rule
      )"
    );
  }

  [Test]
  public void testNamedIdentifierExpression() {
    // rule1 ::= IdentifierProperty@rule2
    // rule2 ::= /a/
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule1",
            Expression = new IdentifierExpression() {
              Name       = "IdentifierProperty",
              Identifier = "rule2"
            }
          },
          new Rule() {
            Identifier = "rule2",
            Expression = new ExtractorExpression() { Pattern = "a" }
          }
        }
      },
      @"Model(
         Entities=[
           Entity(
             Name=rule1,Type=rule1,Subs=[rule2],
             Properties=[
               Property(Name=IdentifierProperty,Type=<string>,Source=ConsumeEntity(rule2)->IdentifierProperty)
             ],
             ParseAction=ConsumeEntity(rule2)->IdentifierProperty
           ),
           VirtualEntity(
             Name=rule2,Type=<string>,Supers=[rule1],
             Properties=[Property(Name=rule2,Type=<string>,Source=ConsumePattern(a)->rule2)],
             ParseAction=ConsumePattern(a)->rule2
           )
        ],
        Root=rule1
      )"
    );
  }

  [Test]
  public void testOptionalString() {
    // rule ::= [ "a" ]
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule",
            Expression = new OptionalExpression() {
              Expression = new StringExpression() { String = "a" }
            }
          }
        }
      },
      @"Model(
         Entities=[
           Entity(
             Name=rule,Type=rule,
             Properties=[
               Property(
                 Name=has-a,Type=<bool>,IsOptional,
                 Source=ConsumeString(a)?!->has-a
               )
             ],
             ParseAction=ConsumeString(a)?!->has-a
           )
        ],
        Root=rule
      )"
    );
  }

  [Test]
  public void testSimpleSequentialExpression() {
    // rule1 ::= rule2 "." rule2
    // rule2 ::= /[a-z]+/
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule1",
            Expression = new SequentialExpression() {
              AtomicExpression          = new IdentifierExpression() { Identifier = "rule2" },
              NonAlternativesExpression = new SequentialExpression() {
                AtomicExpression          = new StringExpression() { String = "." },
                NonAlternativesExpression = new IdentifierExpression() { Identifier = "rule2" }
              }
            }
          },
          new Rule() {
            Identifier = "rule2",
            Expression = new ExtractorExpression() { Pattern = "[a-z]+" }
          }
        }
      },
      @"Model(
         Entities=[
           Entity(
             Name=rule1,Type=rule1,
             Properties=[
               Property(Name=rule20,Type=<string>,Source=ConsumeEntity(rule2)->rule20),
               Property(Name=rule21,Type=<string>,Source=ConsumeEntity(rule2)->rule21)
             ],
             ParseAction=ConsumeAll([
               ConsumeEntity(rule2)->rule20,
               ConsumeString(.),
               ConsumeEntity(rule2)->rule21
             ])
           ),
           VirtualEntity(
             Name=rule2,Type=<string>,
             Properties=[Property(Name=rule2,Type=<string>,Source=ConsumePattern([a-z]+)->rule2)],
             ParseAction=ConsumePattern([a-z]+)->rule2
           )
        ],
        Root=rule1
      )"
    );
  }

  [Test]
  public void testNamedIdentifierDefinition() {
    // rule ::= [ name ] id
    // name ::= id "@"
    // id   ::= /[a-z]+/
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule",
            Expression = new SequentialExpression() {
              AtomicExpression = new OptionalExpression() {
                Expression = new IdentifierExpression() { Identifier = "name" }
              },
              NonAlternativesExpression = new IdentifierExpression() { Identifier = "id" }
            }
          },
          new Rule() {
            Identifier = "name",
            Expression = new SequentialExpression() {
              AtomicExpression          = new IdentifierExpression() { Identifier = "id" },
              NonAlternativesExpression = new StringExpression() { String = "@" }
            }
          },
          new Rule() {
            Identifier = "id",
            Expression = new ExtractorExpression() { Pattern = "[a-z]+" }
          }
        }
      },
      @"Model(
         Entities=[
           Entity(
             Name=rule,Type=rule,
             Properties=[
               Property(Name=name,Type=<string>,IsOptional,Source=ConsumeEntity(name)?->name),
               Property(Name=id,Type=<string>,Source=ConsumeEntity(id)->id)
             ],
             ParseAction=ConsumeAll([
               ConsumeEntity(name)?->name,
               ConsumeEntity(id)->id
             ])
           ),
           VirtualEntity(
             Name=name,Type=<string>,Subs=[id],
             Properties=[
               Property(Name=id,Type=<string>,Source=ConsumeEntity(id)->id)
             ],
             ParseAction=ConsumeAll([
               ConsumeEntity(id)->id,
               ConsumeString(@)
             ])
           ),
           VirtualEntity(
             Name=id,Type=<string>,Supers=[name],
             Properties=[Property(Name=id,Type=<string>,Source=ConsumePattern([a-z]+)->id)],
             ParseAction=ConsumePattern([a-z]+)->id
           )
        ],
        Root=rule
      )"
    );
  }

  [Test]
  public void testAlternativeCharacters() {
    // rule ::= "a" | "b" | "c" ;
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule",
            Expression = new AlternativesExpression() {
              NonAlternativesExpression = new StringExpression() { String = "a" },
              Expression                = new AlternativesExpression() {
                NonAlternativesExpression = new StringExpression() { String = "b" },
                Expression                = new StringExpression() { String = "c" }
              }
            }
          }
        }
      },
      @"
Model(
  Entities=[
    Entity(
      Name=rule,Type=rule,Properties=[
        Property(Name=has-a,Type=<bool>,Source=ConsumeString(a)!->has-a),
        Property(Name=has-b,Type=<bool>,Source=ConsumeString(b)!->has-b),
        Property(Name=has-c,Type=<bool>,Source=ConsumeString(c)!->has-c)
      ],
      ParseAction=ConsumeAny([
        ConsumeString(a)!->has-a,
        ConsumeString(b)!->has-b,
        ConsumeString(c)!->has-c
      ]
    )
  )
  ],
  Root=rule
)"
    );
  }

  [Test]
  public void testAlternativeGroupedCharacters() {
    // rule ::= ( "a" "b" ) | ( "c" "d" )
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule",
            Expression = new AlternativesExpression() {
              NonAlternativesExpression = new GroupExpression() {
                Expression = new SequentialExpression() {
                  AtomicExpression          = new StringExpression() { String = "a" },
                  NonAlternativesExpression = new StringExpression() { String = "b" }
                }
              },
              Expression = new GroupExpression() {
                Expression = new SequentialExpression() {
                  AtomicExpression          = new StringExpression() { String = "c" },
                  NonAlternativesExpression = new StringExpression() { String = "d" }
                }
              }
            }
          }
        }
      },
      @"Model(
         Entities=[
           Entity(
             Name=rule,Type=rule,
             ParseAction=ConsumeAny([
               ConsumeAll([ConsumeString(a), ConsumeString(b)]),
               ConsumeAll([ConsumeString(c), ConsumeString(d)])
             ])
           )
        ],
        Root=rule
      )"
    );
  }

  [Test]
  public void testRepeatedString() {
    // rule ::= { "a" }
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule",
            Expression = new RepetitionExpression() {
              Expression = new StringExpression() { String = "a" }
            }
          }
        }
      },
      @"Model(
         Entities=[
           Entity(
             Name=rule,Type=rule,
             ParseAction=ConsumeString(a)*
           )
        ],
        Root=rule
      )"
    );
  }

  [Test]
  public void testRepeatedIdentifier() {
    // rules ::= { rule }
    // rule  ::= id "x" id = id
    // id    ::= /[a-z]+/
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rules",
            Expression = new RepetitionExpression() {
              Expression = new IdentifierExpression() { Identifier = "rule" }
            }
          },
          new Rule() {
            Identifier = "rule",
            Expression = new SequentialExpression() {
              AtomicExpression          = new IdentifierExpression() { Identifier = "id" },
              NonAlternativesExpression = new SequentialExpression() {
                AtomicExpression          = new StringExpression() { String = "x" },
                NonAlternativesExpression = new SequentialExpression() {
                  AtomicExpression          = new IdentifierExpression() { Identifier = "id" },
                  NonAlternativesExpression = new SequentialExpression() {
                    AtomicExpression          = new StringExpression() { String = "=" },
                    NonAlternativesExpression = new IdentifierExpression() { Identifier = "id" }
                  }
                }
              }
            }
          },
          new Rule() {
            Identifier = "id",
            Expression = new ExtractorExpression() { Pattern = "[a-z]+" }
          }
        }
      },
      @"Model(
         Entities=[
           Entity(
             Name=rules,Type=rules,
             Properties=[
               Property(Name=rule,Type=rule,IsPlural,
               Source=ConsumeEntity(rule)*->rule)
             ],
             ParseAction=ConsumeEntity(rule)*->rule
           ),
           Entity(
             Name=rule,Type=rule,
             Properties=[
               Property(Name=id0,Type=<string>,Source=ConsumeEntity(id)->id0),
               Property(Name=id1,Type=<string>,Source=ConsumeEntity(id)->id1),
               Property(Name=id2,Type=<string>,Source=ConsumeEntity(id)->id2)
             ],
             ParseAction=ConsumeAll([
               ConsumeEntity(id)->id0,
               ConsumeString(x),
               ConsumeEntity(id)->id1,
               ConsumeString(=),
               ConsumeEntity(id)->id2
             ])
           ),
           VirtualEntity(
             Name=id,Type=<string>,
             Properties=[Property(Name=id,Type=<string>,Source=ConsumePattern([a-z]+)->id)],
             ParseAction=ConsumePattern([a-z]+)->id
           )
        ],
        Root=rules
      )"
    );
  }

  [Test]
  public void testAlternativeCharactersIdentifier() {
    // rule1 ::= { rule2 } ;
    // rule2 ::= "a" | "b" | "c" ;
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule1",
            Expression = new RepetitionExpression() {
              Expression = new IdentifierExpression() { Identifier = "rule2" }
            }
          },
          new Rule() {
            Identifier = "rule2",
            Expression = new AlternativesExpression() {
              NonAlternativesExpression = new StringExpression() { String = "a" },
              Expression                = new AlternativesExpression() {
                NonAlternativesExpression = new StringExpression() { String = "b" },
                Expression =                new StringExpression() { String = "c" }
              }
            }
          }
        }
      },
      @"
Model(
  Entities=[
    Entity(
      Name=rule1,Type=rule1,Properties=[
        Property(Name=rule2,Type=rule2,IsPlural,Source=ConsumeEntity(rule2)*->rule2)
      ],
      ParseAction=ConsumeEntity(rule2)*->rule2
    ),
    Entity(
      Name=rule2,Type=rule2,Properties=[
        Property(Name=has-a,Type=<bool>,Source=ConsumeString(a)!->has-a),
        Property(Name=has-b,Type=<bool>,Source=ConsumeString(b)!->has-b),
        Property(Name=has-c,Type=<bool>,Source=ConsumeString(c)!->has-c)
      ],
      ParseAction=ConsumeAny([
        ConsumeString(a)!->has-a,
        ConsumeString(b)!->has-b,
        ConsumeString(c)!->has-c]
      )
    )
  ],
  Root=rule1
)
"
    );
  }

  [Test]
  public void testAlternativeIdentifiers() {
    // assign ::= exp ;
    // exp ::= a | b ;
    // a ::= /aa/;
    // b ::= /bb/;
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "assign",
            Expression = new IdentifierExpression() { Identifier = "exp" }
          },
          new Rule() {
            Identifier = "exp",
            Expression = new AlternativesExpression() {
              NonAlternativesExpression = new IdentifierExpression() { Identifier = "a" },
              Expression                = new IdentifierExpression() { Identifier = "b"}
            }
          },
          new Rule() {
            Identifier = "a",
            Expression = new ExtractorExpression() { Pattern = "aa" }
          },
          new Rule() {
            Identifier = "b",
            Expression = new ExtractorExpression() { Pattern = "bb" }
          }
        }
      },
      @"Model(
         Entities=[
           Entity(
             Name=assign,Type=assign,Subs=[exp],Properties=[
               Property(Name=exp,Type=exp,Source=ConsumeEntity(exp)->exp)
             ],
             ParseAction=ConsumeEntity(exp)->exp
           ),
           VirtualEntity(
             Name=exp,Type=exp,Supers=[assign],Subs=[a,b],Properties=[
               Property(Name=alternative,Type=exp,Source=ConsumeAny([ConsumeEntity(a)->alternative,ConsumeEntity(b)->alternative])->alternative)
             ],
             ParseAction=ConsumeAny([
               ConsumeEntity(a)->alternative,
               ConsumeEntity(b)->alternative
             ])->alternative
           ),
           Entity(
             Name=a,Type=a,Supers=[exp],
             Properties=[Property(Name=a,Type=<string>,Source=ConsumePattern(aa)->a)],
             ParseAction=ConsumePattern(aa)->a
           ),
           Entity(
             Name=b,Type=b,Supers=[exp],
             Properties=[Property(Name=b,Type=<string>,Source=ConsumePattern(bb)->b)],
             ParseAction=ConsumePattern(bb)->b
           )
         ],
         Root=assign
       )"
    );
  }

  [Test]
  public void testPropertyTypesWithInheritance() {
    // assign ::= exp ;
    // exp ::= a | b ;
    // a ::= b exp ;
    // b ::= x | y ;
    // x ::= /xx/ ;
    // y ::= /yy/ ;
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "assign",
            Expression = new IdentifierExpression() { Identifier = "exp" }
          },
          new Rule() {
            Identifier = "exp",
            Expression = new AlternativesExpression() {
              NonAlternativesExpression = new IdentifierExpression() { Identifier = "a" },
              Expression                = new IdentifierExpression() { Identifier = "b"}
            }
          },
          new Rule() {
            Identifier = "a",
            Expression = new SequentialExpression() {
              AtomicExpression          = new IdentifierExpression() { Identifier = "b"   },
              NonAlternativesExpression = new IdentifierExpression() { Identifier = "exp" }
            }
          },
          new Rule() {
            Identifier = "b",
            Expression = new AlternativesExpression() {
              NonAlternativesExpression = new IdentifierExpression() { Identifier = "x" },
              Expression                = new IdentifierExpression() { Identifier = "y"}
            }
          },
          new Rule() {
            Identifier = "x",
            Expression = new ExtractorExpression() { Pattern = "xx" }
          },
          new Rule() {
            Identifier = "y",
            Expression = new ExtractorExpression() { Pattern = "yy" }
          }
        }
      },
      @"Model(
         Entities=[
           Entity(
             Name=assign,Type=assign,Subs=[exp],Properties=[
               Property(Name=exp,Type=exp,Source=ConsumeEntity(exp)->exp)
             ],
             ParseAction=ConsumeEntity(exp)->exp
           ),
           VirtualEntity(
             Name=exp,Type=exp,Supers=[assign],Subs=[a,b],Properties=[
               Property(Name=alternative,Type=exp,Source=ConsumeAny([
                 ConsumeEntity(a)->alternative,
                 ConsumeEntity(b)->alternative
               ])->alternative)
             ],
             ParseAction=ConsumeAny([
               ConsumeEntity(a)->alternative,
               ConsumeEntity(b)->alternative
             ])->alternative
           ),
           Entity(
             Name=a,Type=a,Supers=[exp],
             Properties=[
               Property(Name=b,Type=b,Source=ConsumeEntity(b)->b),
               Property(Name=exp,Type=exp,Source=ConsumeEntity(exp)->exp)
             ],
             ParseAction=ConsumeAll([
               ConsumeEntity(b)->b,
               ConsumeEntity(exp)->exp
             ])
           ),
           VirtualEntity(
             Name=b,Type=b,Supers=[exp],Subs=[x,y],
             Properties=[
               Property(Name=alternative,Type=b,Source=ConsumeAny([ConsumeEntity(x)->alternative,ConsumeEntity(y)->alternative])->alternative)
             ],
             ParseAction=ConsumeAny([
               ConsumeEntity(x)->alternative,
               ConsumeEntity(y)->alternative
             ])->alternative
           ),
           Entity(
             Name=x,Type=x,Supers=[b],
             Properties=[Property(Name=x,Type=<string>,Source=ConsumePattern(xx)->x)],
             ParseAction=ConsumePattern(xx)->x
           ),
           Entity(
             Name=y,Type=y,Supers=[b],
             Properties=[Property(Name=y,Type=<string>,Source=ConsumePattern(yy)->y)],
             ParseAction=ConsumePattern(yy)->y
           )
         ],
         Root=assign
       )"
    );
  }

  [Test]
  public void testMixedStringIdentifierAlternatives() {
    // record     ::= { value } ;
    // value      ::= literal | variable ;
    // literal    ::= number | string;
    // variable   ::= identifier "x" number
    // number     ::= /[0-9]+/
    // string     ::= /[a-z]+/
    // identifier ::= /[a-z]+/
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "record",
            Expression = new RepetitionExpression() {
              Expression = new IdentifierExpression() { Identifier = "value" }
            }
          },
          new Rule() {
            Identifier = "value",
            Expression = new AlternativesExpression() {
              NonAlternativesExpression = new IdentifierExpression() { Identifier = "literal" },
              Expression                = new IdentifierExpression() { Identifier = "variable"}
            }
          },
          new Rule() {
            Identifier = "literal",
            Expression = new AlternativesExpression() {
              NonAlternativesExpression = new IdentifierExpression() { Identifier = "number" },
              Expression                = new IdentifierExpression() { Identifier = "string"}
            }
          },
          new Rule() {
            Identifier = "variable",
            Expression = new SequentialExpression() {
              AtomicExpression          = new IdentifierExpression() { Identifier = "identifier"},
              NonAlternativesExpression = new SequentialExpression() {
                AtomicExpression          = new StringExpression() { String = "x" },
                NonAlternativesExpression = new IdentifierExpression() { Identifier = "number" }
              }
            }
          },
          new Rule() {
            Identifier = "number",
            Expression = new ExtractorExpression() { Pattern = "[0-9]+" }
          },
          new Rule() {
            Identifier = "string",
            Expression = new ExtractorExpression() { Pattern = "[a-z]+" }
          },
          new Rule() {
            Identifier = "identifier",
            Expression = new ExtractorExpression() { Pattern = "[a-z]+" }
          }
        }
      },
      @"Model(
         Entities=[
           Entity(
             Name=record,Type=record,Properties=[
               Property(
                 Name=value,Type=value,IsPlural,
                 Source=ConsumeEntity(value)*->value
               )
             ],
             ParseAction=ConsumeEntity(value)*->value
           ),
           VirtualEntity(
             Name=value,Type=value,Subs=[literal,variable],
             Properties=[
               Property(Name=alternative,Type=value,Source=ConsumeAny([
                 ConsumeEntity(literal)->alternative,
                 ConsumeEntity(variable)->alternative
               ])->alternative)
             ],
             ParseAction=ConsumeAny([
               ConsumeEntity(literal)->alternative,
               ConsumeEntity(variable)->alternative
             ])->alternative
           ),
           VirtualEntity(
             Name=literal,Type=literal,Supers=[value],Subs=[number,string],
             Properties=[
               Property(Name=alternative,Type=literal,Source=ConsumeAny([
                 ConsumeEntity(number)->alternative,
                 ConsumeEntity(string)->alternative
               ])->alternative)
             ],
             ParseAction=ConsumeAny([
               ConsumeEntity(number)->alternative,
               ConsumeEntity(string)->alternative
             ])->alternative
           ),
           Entity(
             Name=variable,Type=variable,Supers=[value],
             Properties=[
               Property(Name=identifier,Type=<string>,Source=ConsumeEntity(identifier)->identifier),
               Property(Name=number,Type=number,Source=ConsumeEntity(number)->number)
             ],
             ParseAction=ConsumeAll([
               ConsumeEntity(identifier)->identifier,
               ConsumeString(x),
               ConsumeEntity(number)->number
             ])
           ),
           Entity(
             Name=number,Type=number,Supers=[literal],
             Properties=[
               Property(Name=number,Type=<string>,Source=ConsumePattern([0-9]+)->number)
             ],
             ParseAction=ConsumePattern([0-9]+)->number
           ),
           Entity(
             Name=string,Type=string,Supers=[literal],
             Properties=[
               Property(Name=string,Type=<string>,Source=ConsumePattern([a-z]+)->string)
             ],
             ParseAction=ConsumePattern([a-z]+)->string
           ),
           VirtualEntity(
             Name=identifier,Type=<string>,
             Properties=[
               Property(Name=identifier,Type=<string>,Source=ConsumePattern([a-z]+)->identifier)
             ],
             ParseAction=ConsumePattern([a-z]+)->identifier
           )
         ],
         Root=record
       )"
    );
  }

}
