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
                 Name=StringProperty,Type=string,
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
    // rule2 ::= "a"
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule1",
            Expression = new IdentifierExpression() { Identifier = "rule2" }
          },
          new Rule() {
            Identifier = "rule2",
            Expression = new StringExpression() { String = "a" }
          }
        }
      },
      @"Model(
         Entities=[
           Entity(
             Name=rule1,Type=rule1,
             Properties=[Property(Name=rule2,Type=string,Source=ConsumeEntity(rule2)->rule2)],
             ParseAction=ConsumeEntity(rule2)->rule2
           ),
           VirtualEntity(
             Name=rule2,Type=string,Referrers=[rule1.rule2],
             ParseAction=ConsumeString(a)
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
            Expression = new ExtractorExpression() { Regex = "[A-Za-z0-9-]*" }
          }
        }
      },
      @"Model(
         Entities=[
           Entity(
             Name=rule,Type=rule,
             ParseAction=ConsumePattern([A-Za-z0-9-]*)
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
              Regex = "[A-Za-z0-9-]*"
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
                 Name=PatternProperty,Type=string,
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
    // rule2 ::= "a"
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
            Expression = new StringExpression() { String = "a" }
          }
        }
      },
      @"Model(
         Entities=[
           Entity(
             Name=rule1,Type=rule1,
             Properties=[
               Property(Name=IdentifierProperty,Type=string,Source=ConsumeEntity(rule2)->IdentifierProperty)
             ],
             ParseAction=ConsumeEntity(rule2)->IdentifierProperty
           ),
           VirtualEntity(
             Name=rule2,Type=string,Referrers=[rule1.IdentifierProperty],
             ParseAction=ConsumeString(a)
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
               Property(Name=has-a,Type=bool,
               Source=ConsumeOutcome(ConsumeString(a)?)->has-a)
             ],
             ParseAction=ConsumeOutcome(ConsumeString(a)?)->has-a
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
              NonSequentialExpression = new IdentifierExpression() { Identifier = "rule2" },
              Expression              = new SequentialExpression() {
                NonSequentialExpression = new StringExpression() { String = "." },
                Expression              = new IdentifierExpression() { Identifier = "rule2" }
              }
            }
          },
          new Rule() {
            Identifier = "rule2",
            Expression = new ExtractorExpression() { Regex = "[a-z]+" }
          }
        }
      },
      @"Model(
         Entities=[
           Entity(
             Name=rule1,Type=rule1,
             Properties=[
               Property(Name=rule20,Type=string,Source=ConsumeEntity(rule2)->rule20),
               Property(Name=rule21,Type=string,Source=ConsumeEntity(rule2)->rule21)
             ],
             ParseAction=ConsumeAll([
               ConsumeEntity(rule2)->rule20,
               ConsumeString(.),
               ConsumeEntity(rule2)->rule21
             ])
           ),
           VirtualEntity(
             Name=rule2,Type=string,Referrers=[rule1.rule20,rule1.rule21],
             ParseAction=ConsumePattern([a-z]+)
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
              NonSequentialExpression = new OptionalExpression() {
                Expression = new IdentifierExpression() { Identifier = "name" }
              },
              Expression = new IdentifierExpression() { Identifier = "id" }
            }
          },
          new Rule() {
            Identifier = "name",
            Expression = new SequentialExpression() {
              NonSequentialExpression = new IdentifierExpression() { Identifier = "id" },
              Expression = new StringExpression() { String = "@" }
            }
          },
          new Rule() {
            Identifier = "id",
            Expression = new ExtractorExpression() { Regex = "[a-z]+" }
          }
        }
      },
      @"Model(
         Entities=[
           Entity(
             Name=rule,Type=rule,
             Properties=[
               Property(Name=name,Type=string,IsOptional,Source=ConsumeEntity(name)?->name),
               Property(Name=id,Type=string,Source=ConsumeEntity(id)->id)
             ],
             ParseAction=ConsumeAll([
               ConsumeEntity(name)?->name,
               ConsumeEntity(id)->id
             ])
           ),
           VirtualEntity(
             Name=name,Type=string,Referrers=[rule.name],
             Properties=[
               Property(Name=id,Type=string,Source=ConsumeEntity(id)->id)
             ],
             ParseAction=ConsumeAll([
               ConsumeEntity(id)->id,
               ConsumeString(@)
             ])
           ),
           VirtualEntity(
             Name=id,Type=string,Supers=[name],Referrers=[rule.id,name.id],
             ParseAction=ConsumePattern([a-z]+)
           )
        ],
        Root=rule
      )"
    );
  }

  [Test]
  public void testAlternativeCharacters() {
    // rule ::= "a" | "b" | "c"
    this.importAndCompare(
      new Grammar() {
        Rules = new List<Rule>() {
          new Rule() {
            Identifier = "rule",
            Expression = new AlternativesExpression() {
              AtomicExpression = new StringExpression() { String = "a" },
              NonSequentialExpression = new AlternativesExpression() {
                AtomicExpression = new StringExpression() { String = "b" },
                NonSequentialExpression = new StringExpression() { String = "c" }
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
               ConsumeString(a),
               ConsumeString(b),
               ConsumeString(c)
             ])
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
              AtomicExpression = new GroupExpression() {
                Expression = new SequentialExpression() {
                  NonSequentialExpression = new StringExpression() { String = "a" },
                  Expression = new StringExpression() { String = "b" }
                }
              },
              NonSequentialExpression = new GroupExpression() {
                Expression = new SequentialExpression() {
                  NonSequentialExpression = new StringExpression() { String = "c" },
                  Expression = new StringExpression() { String = "d" }
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
              NonSequentialExpression = new IdentifierExpression() { Identifier = "id" },
              Expression = new SequentialExpression() {
                NonSequentialExpression = new StringExpression() { String = "x" },
                Expression = new SequentialExpression() {
                  NonSequentialExpression = new IdentifierExpression() { Identifier = "id" },
                  Expression = new SequentialExpression() {
                    NonSequentialExpression = new StringExpression() { String = "=" },
                    Expression = new IdentifierExpression() { Identifier = "id" }
                  }
                }
              }
            }
          },
          new Rule() {
            Identifier = "id",
            Expression = new ExtractorExpression() { Regex = "[a-z]+" }
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
             Name=rule,Type=rule,Referrers=[rules.rule],
             Properties=[
               Property(Name=id0,Type=string,Source=ConsumeEntity(id)->id0),
               Property(Name=id1,Type=string,Source=ConsumeEntity(id)->id1),
               Property(Name=id2,Type=string,Source=ConsumeEntity(id)->id2)
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
             Name=id,Type=string,Referrers=[rule.id0,rule.id1,rule.id2],
             ParseAction=ConsumePattern([a-z]+)
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
              AtomicExpression = new StringExpression() { String = "a" },
              NonSequentialExpression = new AlternativesExpression() {
                AtomicExpression = new StringExpression() { String = "b" },
                NonSequentialExpression = new StringExpression() { String = "c" }
              }
            }
          }
        }
      },
      @"Model(
         Entities=[
           Entity(
             Name=rule1,Type=rule1,
             Properties=[
               Property(
                 Name=rule2,Type=string,IsPlural,
                 Source=ConsumeEntity(rule2)*->rule2
               )
             ],
             ParseAction=ConsumeEntity(rule2)*->rule2
           ),
           VirtualEntity(
             Name=rule2,Type=string,Referrers=[rule1.rule2],
             ParseAction=ConsumeAny([
               ConsumeString(a),
               ConsumeString(b),
               ConsumeString(c)
             ])
           )
        ],
        Root=rule1
      )"
    );
  }

}
