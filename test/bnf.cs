// unit tests for parsing HPG BNF-like definitions
// author: Christophe VG <contact@christophe.vg>

using System;
using NUnit.Framework;

using System.Collections.Generic;


namespace HumanParserGenerator.Test {

  [TestFixture]
  public class Syntax {
    Parser parser;

    [SetUp]
    public void SetUp() {
      this.parser = new Parser();
    }

    [TearDown]
    public void TearDown() {
      this.parser = null;
    }

    private void parseAndCompare(string src, Grammar expected) {
      Assert.AreEqual(
        expected.ToString(),
        this.parser.Parse(src).AST.ToString()
      );
    }

    [Test]
    public void SingleCharacterRule() {
      this.parseAndCompare(
        "r ::= \"a\";",
        new Grammar() {
          Rules = new List<Rule>() {
            new Rule() {
              Identifier     = "r",
              ExpressionList = new ExpressionList() {
                Value= new SequenceExpression() {
                  Expressions = new List<Expression>() {
                    new Expression() {
                      Value = new StringExpression() {
                        String = "a"
                      }
                    }
                  }
                }
              }
            }
          }
        }
      );
    }
  }

}
