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
            Identifier = "r",
            Expression = new StringExpression() {
              String = "a"
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
          Expression=AlternativesExpression(
            AtomicExpression=StringExpression(String=a),
            NonSequentialExpression=StringExpression(String=b)
          ))])"
    );
  }

  [Test]
  public void CobolPictureClauseRule() {
    this.parseAndCompare(
      "p ::= [ \"PICTURE\" | \"PIC\" ] [ \"IS\" ] pt [ \"(\" int \")\" [ [ \"V\" | \".\" ] pt \"(\" int \")\" ] ];",
      @"Grammar(
        Rules=[
          Rule(Identifier=p,
            Expression=SequentialExpression(
              NonSequentialExpression=OptionalExpression(
                Expression=AlternativesExpression(
                  AtomicExpression=StringExpression(String=PICTURE),
                  NonSequentialExpression=StringExpression(String=PIC)
                )
              ),
              Expression=SequentialExpression(
                NonSequentialExpression=OptionalExpression(
                  Expression=StringExpression(String=IS)
                ),
                Expression=SequentialExpression(
                  NonSequentialExpression=IdentifierExpression(Identifier=pt),
                  Expression=OptionalExpression(
                    Expression=SequentialExpression(
                      NonSequentialExpression=StringExpression(String=(),
                      Expression=SequentialExpression(
                        NonSequentialExpression=IdentifierExpression(Identifier=int),
                        Expression=SequentialExpression(
                          NonSequentialExpression=StringExpression(String=)),
                          Expression=OptionalExpression(
                            Expression=SequentialExpression(
                              NonSequentialExpression=OptionalExpression(
                                Expression=AlternativesExpression(
                                  AtomicExpression=StringExpression(String=V),
                                  NonSequentialExpression=StringExpression(String=.)
                                )
                              ),
                              Expression=SequentialExpression(
                                NonSequentialExpression=IdentifierExpression(Identifier=pt),
                                Expression=SequentialExpression(
                                  NonSequentialExpression=StringExpression(String=(),
                                  Expression=SequentialExpression(
                                    NonSequentialExpression=IdentifierExpression(Identifier=int),
                                    Expression=StringExpression(String=))
                                  )
                                )
                              )
                            )
                          )
                        )
                      )
                    )
                  )
                )
              )
            )
          )
        ]
      )"
    );
  }

  [Test]
  public void IdentifierExtractorTest() {
    this.parseAndCompare(
      "identifier                ::= /([A-Za-z][A-Z0-9a-z-]*)/ ;",
      @"Grammar(Rules=[
        Rule(Identifier=identifier,
          Expression=ExtractorExpression(Regex=([A-Za-z][A-Z0-9a-z-]*)))
      ])"
    );
  }

  [Test]
  public void BNFSelfHostingTest() {
    this.parseAndCompare(
      System.IO.File.ReadAllText("../../generator/grammars/hpg.bnf"),
      @"Grammar(
        Rules=[
          Rule(Identifier=grammar,
            Expression=RepetitionExpression(
              Expression=IdentifierExpression(Identifier=rule)
            )
          ),
          Rule(Identifier=rule,
            Expression=SequentialExpression(
              NonSequentialExpression=IdentifierExpression(Identifier=identifier),
              Expression=SequentialExpression(
                NonSequentialExpression=StringExpression(String=::=),
                Expression=SequentialExpression(
                  NonSequentialExpression=IdentifierExpression(Identifier=expression),
                  Expression=StringExpression(String=;)
                )
              )
            )
          ),
          Rule(Identifier=expression,
            Expression=AlternativesExpression(
              AtomicExpression=IdentifierExpression(Identifier=sequential-expression),
              NonSequentialExpression=IdentifierExpression(Identifier=non-sequential-expression)
            )
          ),
          Rule(Identifier=sequential-expression,
            Expression=SequentialExpression(
              NonSequentialExpression=IdentifierExpression(Identifier=non-sequential-expression),
              Expression=IdentifierExpression(Identifier=expression)
            )
          ),
          Rule(Identifier=non-sequential-expression,
            Expression=AlternativesExpression(
              AtomicExpression=IdentifierExpression(Identifier=alternatives-expression),
              NonSequentialExpression=IdentifierExpression(Identifier=atomic-expression)
            )
          ),
          Rule(Identifier=alternatives-expression,
            Expression=SequentialExpression(
              NonSequentialExpression=IdentifierExpression(Identifier=atomic-expression),
              Expression=SequentialExpression(
                NonSequentialExpression=StringExpression(String=|),
                Expression=IdentifierExpression(Identifier=non-sequential-expression)
              )
            )
          ),
          Rule(Identifier=atomic-expression,
            Expression=AlternativesExpression(
              AtomicExpression=IdentifierExpression(Identifier=nested-expression),
              NonSequentialExpression=IdentifierExpression(Identifier=terminal-expression)
            )
          ),
          Rule(Identifier=nested-expression,
            Expression=AlternativesExpression(
              AtomicExpression=IdentifierExpression(Identifier=optional-expression),
              NonSequentialExpression=AlternativesExpression(
                AtomicExpression=IdentifierExpression(Identifier=repetition-expression),
                NonSequentialExpression=IdentifierExpression(Identifier=group-expression)
              )
            )
          ),
          Rule(Identifier=optional-expression,
            Expression=SequentialExpression(
              NonSequentialExpression=StringExpression(String=[),
              Expression=SequentialExpression(
                NonSequentialExpression=IdentifierExpression(Identifier=expression),
                Expression=StringExpression(String=])
              )
            )
          ),
          Rule(Identifier=repetition-expression,
            Expression=SequentialExpression(
              NonSequentialExpression=StringExpression(String={),
              Expression=SequentialExpression(
                NonSequentialExpression=IdentifierExpression(Identifier=expression),
                Expression=StringExpression(String=})
              )
            )
          ),
          Rule(Identifier=group-expression,
            Expression=SequentialExpression(
              NonSequentialExpression=StringExpression(String=(),
              Expression=SequentialExpression(
                NonSequentialExpression=IdentifierExpression(Identifier=expression),
                Expression=StringExpression(String=))
              )
            )
          ),
          Rule(Identifier=terminal-expression,
            Expression=AlternativesExpression(
              AtomicExpression=IdentifierExpression(Identifier=identifier-expression),
              NonSequentialExpression=AlternativesExpression(
                AtomicExpression=IdentifierExpression(Identifier=string-expression),
                NonSequentialExpression=IdentifierExpression(Identifier=extractor-expression)
              )
            )
          ),
          Rule(Identifier=identifier-expression,
            Expression=IdentifierExpression(Identifier=identifier)
          ),
          Rule(Identifier=string-expression,
            Expression=IdentifierExpression(Identifier=string)
          ),
          Rule(Identifier=extractor-expression,
            Expression=SequentialExpression(
              NonSequentialExpression=StringExpression(String=/),
              Expression=SequentialExpression(
                NonSequentialExpression=IdentifierExpression(Identifier=regex),
                Expression=StringExpression(String=/)
              )
            )
          ),
          Rule(Identifier=identifier,
            Expression=ExtractorExpression(Regex=([A-Za-z][A-Z0-9a-z-]*))
          ),
          Rule(Identifier=string,
            Expression=ExtractorExpression(Regex=""([^""]*)""|^'([^']*)')
          ),
          Rule(Identifier=regex,
            Expression=ExtractorExpression(Regex=(.*?)(?<keep>/\s*;))
          )
        ])"
    );
  }
}




