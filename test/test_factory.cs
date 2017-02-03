// unit tests for Generator Model Factory
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using NUnit.Framework;

using System.Collections.Generic;

using HumanParserGenerator.Generator; // for Model, Factory

[TestFixture]
public class GeneratorModelFactoryTests {

  [Test]
  public void testEmptyModel() {
    Model model = new Factory().Model;

    Assert.AreEqual(
      @"Model(Entities=[],Root=)".Replace(" ", "").Replace("\n",""),
      model.ToString()
    );
  }

  [Test]
  public void testMinimalModelWithoutProperty() {
    // rule ::= "a"
    Grammar grammar = new Grammar() {
      Rules = new List<Rule>() {
        new Rule() {
          Identifier = "rule",
          Expression = new StringExpression() { String = "a" }
        }
      }
    };
    Model model = new Factory().Import(grammar).Model;

    Assert.AreEqual(
      @"Model(
         Entities=[
           Entity(
             Name=rule,Type=rule,
             ParseAction=Consume(a)
           )
        ],
        Root=rule
      )".Replace(" ", "").Replace("\n",""),
      model.ToString()
    );
  }

  [Test]
  public void testMinimalModelWithProperty() {
    // rule ::= StringProperty@"a"
    Grammar grammar = new Grammar() {
      Rules = new List<Rule>() {
        new Rule() {
          Identifier = "rule",
          Expression = new StringExpression() {
            Name   = "StringProperty",
            String = "a"
          }
        }
      }
    };
    Model model = new Factory().Import(grammar).Model;

    Assert.AreEqual(
      @"Model(
         Entities=[
           Entity(
             Name=rule,Type=rule,
             Properties=[
               Property(Name=StringProperty,Type=string,IsPlural=False,IsOptional=False,Source=Consume(a->StringProperty))
             ],
             ParseAction=Consume(a->StringProperty)
           )
        ],
        Root=rule
      )".Replace(" ", "").Replace("\n",""),
      model.ToString()
    );
  }

  [Test]
  public void testSimpleIdentifierExpressionIndirection() {
    // rule1 ::= rule2
    // rule2 ::= "a"
    Grammar grammar = new Grammar() {
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
    };
    Model model = new Factory().Import(grammar).Model;

    Assert.AreEqual(
      @"Model(
         Entities=[
           Entity(
             Name=rule1,Type=rule1,
             Properties=[Property(Name=rule2,Type=,IsPlural=False,IsOptional=False,Source=Consume(rule2->rule2))],
             ParseAction=Consume(rule2->rule2)
           ),
           VirtualEntity(
             Name=rule2,Type=,Referrers=[rule1.rule2],
             ParseAction=Consume(a)
           )
        ],
        Root=rule1
      )".Replace(" ", "").Replace("\n",""),
      model.ToString()
    );      
  }

  [Test]
  public void testMinimalExtractorWithoutProperty() {
    // rule ::= /[A-Za-z0-9-]*/
    Grammar grammar = new Grammar() {
      Rules = new List<Rule>() {
        new Rule() {
          Identifier = "rule",
          Expression = new ExtractorExpression() { Regex = "[A-Za-z0-9-]*" }
        }
      }
    };
    Model model = new Factory().Import(grammar).Model;

    Assert.AreEqual(
      @"Model(
         Entities=[
           Entity(
             Name=rule,Type=rule,
             ParseAction=Consume([A-Za-z0-9-]*)
           )
        ],
        Root=rule
      )".Replace(" ", "").Replace("\n",""),
      model.ToString()
    );
  }

  [Test]
  public void testMinimalExtractorWithProperty() {
    // rule ::= PatternProperty/[A-Za-z0-9-]*/
    Grammar grammar = new Grammar() {
      Rules = new List<Rule>() {
        new Rule() {
          Identifier = "rule",
          Expression = new ExtractorExpression() {
            Name  = "PatternProperty",
            Regex = "[A-Za-z0-9-]*"
          }
        }
      }
    };
    Model model = new Factory().Import(grammar).Model;

    Assert.AreEqual(
      @"Model(
         Entities=[
           Entity(
             Name=rule,Type=rule,
             Properties=[
               Property(Name=PatternProperty,Type=string,IsPlural=False,IsOptional=False,Source=Consume([A-Za-z0-9-]*->PatternProperty))
             ],
             ParseAction=Consume([A-Za-z0-9-]*->PatternProperty)
           )
        ],
        Root=rule
      )".Replace(" ", "").Replace("\n",""),
      model.ToString()
    );
  }

  [Test]
  public void testNamedIdentifierExpression() {
    // rule1 ::= IdentifierProperty@rule2
    // rule2 ::= "a"
    Grammar grammar = new Grammar() {
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
    };
    Model model = new Factory().Import(grammar).Model;

    Assert.AreEqual(
      @"Model(
         Entities=[
           Entity(
             Name=rule1,Type=rule1,
             Properties=[Property(Name=IdentifierProperty,Type=,IsPlural=False,IsOptional=False,Source=Consume(rule2->IdentifierProperty))],
             ParseAction=Consume(rule2->IdentifierProperty)
           ),
           VirtualEntity(
             Name=rule2,Type=,Referrers=[rule1.IdentifierProperty],
             ParseAction=Consume(a)
           )
        ],
        Root=rule1
      )".Replace(" ", "").Replace("\n",""),
      model.ToString()
    );      
  }

  [Test]
  public void testOptionalString() {
    // rule ::= [ "a" ]
    Grammar grammar = new Grammar() {
      Rules = new List<Rule>() {
        new Rule() {
          Identifier = "rule",
          Expression = new OptionalExpression() {
            Expression = new StringExpression() { String = "a" }
          }
        }
      }
    };
    Model model = new Factory().Import(grammar).Model;

    Assert.AreEqual(
      @"Model(
         Entities=[
           Entity(
             Name=rule,Type=rule,
             Properties=[
               Property(Name=has-a,Type=bool,IsPlural=False,IsOptional=False,Source=Consume(ConsumeOptional(a)?->has-a))
             ],
             ParseAction=Consume(ConsumeOptional(a)?->has-a)
           )
        ],
        Root=rule
      )".Replace(" ", "").Replace("\n",""),
      model.ToString()
    );
  }

  [Test]
  public void testSimpleSequentialExpression() {
    // rule1 ::= rule2 "." rule2
    // rule2 ::= /[a-z]+/
    Grammar grammar = new Grammar() {
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
    };
    Model model = new Factory().Import(grammar).Model;

    Assert.AreEqual(
      @"Model(
         Entities=[
           Entity(
             Name=rule1,Type=rule1,
             Properties=[
               Property(Name=rule20,Type=,IsPlural=False,IsOptional=False,Source=Consume(rule2->rule20)),
               Property(Name=rule21,Type=,IsPlural=False,IsOptional=False,Source=Consume(rule2->rule21))
             ],
             ParseAction=Consume([
                 Consume(rule2->rule20),
                 Consume(.),
                 Consume(rule2->rule21)
             ])
           ),
           VirtualEntity(
             Name=rule2,Type=,Referrers=[rule1.rule20,rule1.rule21],
             ParseAction=Consume([a-z]+)
           )
        ],
        Root=rule1
      )".Replace(" ", "").Replace("\n",""),
      model.ToString()
    );
  }

  [Test]
  public void testNamedIdentifierDefinition() {
    // rule ::= [ name ] id
    // name ::= id "@"
    // id   ::= /[a-z]+/
    Grammar grammar = new Grammar() {
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
    };
    Model model = new Factory().Import(grammar).Model;

    Assert.AreEqual(
      @"Model(
         Entities=[
           Entity(
             Name=rule,Type=rule,
             Properties=[
               Property(Name=name,Type=,IsPlural=False,IsOptional=True,Source=Consume(name->name)),
               Property(Name=id,Type=,IsPlural=False,IsOptional=False,Source=Consume(id->id))
             ],
             ParseAction=Consume([
               Consume(name->name),
               Consume(id->id)
             ])
           ),
           VirtualEntity(
             Name=name,Type=,Referrers=[rule.name],
             Properties=[
               Property(Name=id,Type=,IsPlural=False,IsOptional=False,Source=Consume(id->id))
             ],
             ParseAction=Consume([
               Consume(id->id),
               Consume(@)
             ])
           ),
           VirtualEntity(
             Name=id,Type=,Supers=[name],Referrers=[rule.id,name.id],
             ParseAction=Consume([a-z]+)
           )
        ],
        Root=rule
      )".Replace(" ", "").Replace("\n",""),
      model.ToString()
    );
  }

  [Test]
  public void testAlternativeCharacters() {
    // rule ::= "a" | "b" | "c"
    Grammar grammar = new Grammar() {
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
    };
    Model model = new Factory().Import(grammar).Model;

    Assert.AreEqual(
      @"Model(
         Entities=[
           Entity(
             Name=rule,Type=rule,
             ParseAction=Consume(a|b|c)
           )
        ],
        Root=rule
      )".Replace(" ", "").Replace("\n",""),
      model.ToString()
    );
  }

  [Test]
  public void testAlternativeGroupedCharacters() {
    // rule ::= ( "a" "b" ) | ( "c" "d" )
    Grammar grammar = new Grammar() {
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
    };
    Model model = new Factory().Import(grammar).Model;

    Assert.AreEqual(
      @"Model(
         Entities=[
           Entity(
             Name=rule,Type=rule,
             ParseAction=Consume([Consume(a),Consume(b)]|[Consume(c),Consume(d)])
           )
        ],
        Root=rule
      )".Replace(" ", "").Replace("\n",""),
      model.ToString()
    );
  }

  [Test]
  public void testRepeatedString() {
    // rule ::= { "a" }
    Grammar grammar = new Grammar() {
      Rules = new List<Rule>() {
        new Rule() {
          Identifier = "rule",
          Expression = new RepetitionExpression() {
            Expression = new StringExpression() { String = "a" }
          }
        }
      }
    };
    Model model = new Factory().Import(grammar).Model;

    Assert.AreEqual(
      @"Model(
         Entities=[
           Entity(
             Name=rule,Type=rule,
             ParseAction=Consume(a*)
           )
        ],
        Root=rule
      )".Replace(" ", "").Replace("\n",""),
      model.ToString()
    );
  }

  [Test]
  public void testRepeatedIdentifier() {
    // rules ::= { rule }
    // rule  ::= id "x" id = id
    // id    ::= /[a-z]+/
    Grammar grammar = new Grammar() {
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
    };
    Model model = new Factory().Import(grammar).Model;

    Assert.AreEqual(
      @"Model(
         Entities=[
           Entity(
             Name=rules,Type=rules,
             Properties=[
               Property(Name=rule,Type=rule,IsPlural=True,IsOptional=False,Source=Consume(rule->rule))
             ],
             ParseAction=Consume(rule->rule)
           ),
           Entity(
             Name=rule,Type=rule,Referrers=[rules.rule],
             Properties=[
               Property(Name=id0,Type=,IsPlural=False,IsOptional=False,Source=Consume(id->id0)),
               Property(Name=id1,Type=,IsPlural=False,IsOptional=False,Source=Consume(id->id1)),
               Property(Name=id2,Type=,IsPlural=False,IsOptional=False,Source=Consume(id->id2))
             ],
             ParseAction=Consume([Consume(id->id0),Consume(x),Consume(id->id1),Consume(=),Consume(id->id2)])
           ),
           VirtualEntity(
             Name=id,Type=,Referrers=[rule.id0,rule.id1,rule.id2],
             ParseAction=Consume([a-z]+)
           )
        ],
        Root=rules
      )".Replace(" ", "").Replace("\n",""),
      model.ToString()
    );
  }
}
