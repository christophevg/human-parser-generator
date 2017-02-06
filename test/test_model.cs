// unit tests for Generator Model
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using NUnit.Framework;

using System.Collections.Generic;

using HumanParserGenerator.Generator; // for Model

[TestFixture]
public class GeneratorModelTests {

  [Test]
  public void testEmptyModel() {
    Model model = new Model();

    Assert.AreEqual(
      @"Model(Entities=[],Root=)".Replace(" ", "").Replace("\n",""),
      model.ToString()
    );
  }

  [Test]
  public void testMinimalModelWithoutProperty() {
    // e.g. rule ::= "a"
    Model model = new Model() {
      Entities = new List<Entity> {
        new Entity() {
          Name        = "rule",
          ParseAction = new ConsumeString() { String = "a" }
        }
      }
    };

    Assert.AreEqual(
      @"Model(
         Entities=[
           Entity(Name=rule,Type=rule,ParseAction=ConsumeString(a))
         ],
         Root=rule
       )".Replace(" ", "").Replace("\n",""),
      model.ToString()
    );
  }

  [Test]
  public void testMinimalModelWithProperty() {
    // e.g. rule ::= "StringProperty"@"a"
    Property property = new Property() { Name = "StringProperty" };
    ParseAction consume = new ConsumeString() {
      Property = property,
      String = "a"
    };
    property.Source = consume;
    Model model = new Model() {
      Entities = new List<Entity> {
        new Entity() {
          Name        = "rule",
          Properties  = (new List<Property>  { property }).AsReadOnly(),
          ParseAction = consume
        }
      }
    };

    Assert.AreEqual(
      @"Model(
         Entities=[
           Entity(
             Name=rule,Type=rule,
             Properties=[
               Property(Name=StringProperty,Type=string,Source=ConsumeString(a)->StringProperty)
             ],
             ParseAction=ConsumeString(a)->StringProperty
           )
        ],
        Root=rule
      )".Replace(" ", "").Replace("\n",""),
      model.ToString()
    );
  }

}
