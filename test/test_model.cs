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
      @"new Model() {
Entities = new List<Entity>(),
RootName = null
}",
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
      @"new Model() {
Entities = new List<Entity>() {
new Entity() {
Rule = null,
Name = ""rule"",
Properties = new List<Property>().AsReadOnly(),
ParseAction = new ConsumeString() {
String = ""a""
},
Supers = new HashSet<string>(),
Subs = new HashSet<string>()
}
},
RootName = ""rule""
}",
      model.ToString()
    );
  }

  [Test]
  public void testMinimalModelWithProperty() {
    // e.g. rule ::= StringProperty @ "a"
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
      @"new Model() {
Entities = new List<Entity>() {
new Entity() {
Rule = null,
Name = ""rule"",
Properties = new List<Property>() {
new Property() {
Name = ""StringProperty"",
Source = new ConsumeString() {
String = ""a""
}
}
}.AsReadOnly(),
ParseAction = new ConsumeString() {
String = ""a""
},
Supers = new HashSet<string>(),
Subs = new HashSet<string>()
}
},
RootName = ""rule""
}",
      model.ToString()
    );
  }

}
