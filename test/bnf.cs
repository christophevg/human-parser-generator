// unit tests for parsing HPG BNF-like definitions
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using NUnit.Framework;

using System.Collections.Generic;

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

  private void parseAndCompare(string src, string expected) {
    this.parser.Parse(src);
    Assert.AreEqual(
      expected.Replace(" ","").Replace("\n",""),
      this.parser.AST.ToString()
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
            Expressions = new SequentialExpressions() {
              Expressions = new List<Expression>() {
                new StringExpression() {
                  String = "a"
                }
              }
            }
          }
        }
      }
    );
  }

  [Test]
  public void TwoAlternativeCharactersRule() {
    this.parseAndCompare(
      "r ::= \"a\" | \"b\";",
      @"Grammar(
        Rules=[
          Rule(Identifier=r,
          Expressions=AlternativeExpressions(
            Expression=StringExpression(String=a),
            Expressions=SequentialExpressions(Expressions=[
              StringExpression(String=b)
            ])))])"
    );
  }

  [Test]
  public void CobolPictureClauseRule() {
    this.parseAndCompare(
      "p ::= [ \"PICTURE\" | \"PIC\" ] [ \"IS\" ] pt [ \"(\" int \")\" [ [ \"V\" | \".\" ] pt \"(\" int \")\" ] ];",
      @"Grammar(
        Rules=[
          Rule(Identifier=p,
            Expressions=SequentialExpressions(Expressions=[
              OptionalExpression(Expressions=
                AlternativeExpressions(
                  Expression=StringExpression(String=PICTURE),
                  Expressions=SequentialExpressions(Expressions=[
                    StringExpression(String=PIC)
                  ]))),
              OptionalExpression(Expressions=
                SequentialExpressions(Expressions=[
                  StringExpression(String=IS)
                ])),
              IdentifierExpression(Identifier=pt),
              OptionalExpression(Expressions=
                SequentialExpressions(Expressions=[
                  StringExpression(String=(),
                    IdentifierExpression(Identifier=int),
                    StringExpression(String=)),
                    OptionalExpression(Expressions=
                      SequentialExpressions(Expressions=[
                        OptionalExpression(Expressions=
                          AlternativeExpressions(Expression=
                            StringExpression(String=V),
                            Expressions=SequentialExpressions(Expressions=[
                              StringExpression(String=.)]))),
                        IdentifierExpression(Identifier=pt),
                        StringExpression(String=(),
                        IdentifierExpression(Identifier=int),
                        StringExpression(String=))
                      ]))]))]))])"
    );
  }

  // Future ;-)
  // [Test]
  // public void BNFSelfHostingTest() {
  //   this.parseAndCompare(
  //     System.IO.File.ReadAllText("../grammars/hpg.bnf"),
  //     HumanParserGenerator.Grammars.AsModel.BNF
  //   );
  // }
}




