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
            Expressions = new Expressions() {
              Value= new SequentialExpressions() {
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

  [Test]
  public void TwoAlternativeCharactersRule() {
    this.parseAndCompare(
      "r ::= \"a\" | \"b\";",
      @"Grammar(
        Rules=[
          Rule(Identifier=r,
          Expressions=Expressions(Value=
            AlternativeExpressions(
              Expression=Expression(Value=StringExpression(String=a)),
              Expressions=Expressions(Value=SequentialExpressions(
                Expressions=[
                  Expression(Value=StringExpression(String=b))
                ])))))])"
    );
  }

  [Test]
  public void CobolPictureClauseRule() {
    this.parseAndCompare(
      "p ::= [ \"PICTURE\" | \"PIC\" ] [ \"IS\" ] pt [ \"(\" int \")\" [ [ \"V\" | \".\" ] pt \"(\" int \")\" ] ];",
      @"Grammar(
        Rules=[
          Rule(Identifier=p,
            Expressions=Expressions(Value=
              SequentialExpressions(Expressions=[
                Expression(Value=
                  OptionalExpression(Expressions=
                    Expressions(Value=
                      AlternativeExpressions(
                        Expression=Expression(Value=
                          StringExpression(String=PICTURE)),
                        Expressions=Expressions(Value=
                          SequentialExpressions(Expressions=[
                            Expression(Value=
                              StringExpression(String=PIC))])))))),
                Expression(Value=
                  OptionalExpression(Expressions=
                    Expressions(Value=
                      SequentialExpressions(Expressions=[
                        Expression(Value=StringExpression(String=IS))])))),
                Expression(Value=IdentifierExpression(Identifier=pt)),
                Expression(Value=
                  OptionalExpression(Expressions=
                    Expressions(Value=
                      SequentialExpressions(Expressions=[
                        Expression(Value=StringExpression(String=()),
                        Expression(Value=IdentifierExpression(Identifier=int)),
                        Expression(Value=StringExpression(String=))),
                        Expression(Value=
                          OptionalExpression(Expressions=
                            Expressions(Value=
                              SequentialExpressions(Expressions=[
                                Expression(Value=
                                  OptionalExpression(Expressions=
                                    Expressions(Value=
                                      AlternativeExpressions(Expression=
                                        Expression(Value=StringExpression(String=V)),
                                        Expressions=Expressions(Value=
                                          SequentialExpressions(Expressions=[
                                            Expression(Value=StringExpression(String=.))])))))),
                                Expression(Value=IdentifierExpression(Identifier=pt)),
                                Expression(Value=StringExpression(String=()),
                                Expression(Value=IdentifierExpression(Identifier=int)),
                                Expression(Value=StringExpression(String=)))
                              ]))))]))))])))])"
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




