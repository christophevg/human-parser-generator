// unit tests for the Parser Generator Model
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using NUnit.Framework;

using System.Collections.Generic;

using HumanParserGenerator.Generator;

[TestFixture]
public class M {
  Parser parser;

  [SetUp]
  public void SetUp() {
    this.parser = new Parser();
  }

  [TearDown]
  public void TearDown() {
    this.parser = null;
  }

  private void parseAndCompare(string src, string expected) {
    Assert.AreEqual(
      expected.Replace(" ","").Replace("\n",""),
      new Model().Import(this.parser.Parse(src).AST).ToString()
    );
  }

  private void parseAndCompare(string src, Grammar expected) {
    this.parseAndCompare(src, expected.ToString());
  }

  [Test]
  public void SingleCharacterRule() {
    this.parseAndCompare(
      "r ::= \"a\";",
      @"Model(
        Entities=[
          Entity(Name=r,Type=r,
            Supers=[],Referrers=[],Properties=[],
            ParseAction=Consume(a)
          )
        ],
        Extractions=[]"
    );
  }
}
