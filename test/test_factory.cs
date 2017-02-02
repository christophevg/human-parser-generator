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
           VirtualEntity(
             Name=rule,Type=,Supers=[],Referrers=[],
             Properties=[],
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
    // rule ::= "StringProperty"@"a"
    Grammar grammar = new Grammar() {
      Rules = new List<Rule>() {
        new Rule() {
          Identifier = "rule",
          Expression = new StringExpression() {
            Identifier = "StringProperty",
            String     = "a"
          }
        }
      }
    };
    Model model = new Factory().Import(grammar).Model;

    Assert.AreEqual(
      @"Model(
         Entities=[
           VirtualEntity(
             Name=rule,Type=string,Supers=[],Referrers=[],
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
           VirtualEntity(
             Name=rule1,Type=,Supers=[],Referrers=[],
             Properties=[Property(Name=rule2,Type=,IsPlural=False,IsOptional=False,Source=Consume(rule2->rule2))],
             ParseAction=Consume(rule2->rule2)
           ),
           VirtualEntity(
             Name=rule2,Type=,Supers=[rule1],Referrers=[rule1.rule2],
             Properties=[],
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
           VirtualEntity(
             Name=rule,Type=,Supers=[],Referrers=[],
             Properties=[],
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
    // rule ::= "PatternProperty"/[A-Za-z0-9-]*/
    Grammar grammar = new Grammar() {
      Rules = new List<Rule>() {
        new Rule() {
          Identifier = "rule",
          Expression = new ExtractorExpression() {
            Identifier = "PatternProperty",
            Regex      = "[A-Za-z0-9-]*"
          }
        }
      }
    };
    Model model = new Factory().Import(grammar).Model;

    Assert.AreEqual(
      @"Model(
         Entities=[
           VirtualEntity(
             Name=rule,Type=string,Supers=[],Referrers=[],
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
}
