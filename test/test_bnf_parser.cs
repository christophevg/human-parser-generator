// unit tests for Generator Model Factory
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using NUnit.Framework;

using System.Collections.Generic;

using HumanParserGenerator.Generator;

[TestFixture]
public class BNFParserTests {

  private void parseAndCompare(string input, string grammar, string parser ) {
    Grammar g = new Parser().Parse(input).AST;
    Assert.AreEqual(
      grammar.Replace(" ", "").Replace("\n", ""),
      g.ToString()
    );
    
    Model p = new Factory().Import(g).Model;
    Assert.AreEqual(
      parser.Replace(" ", "").Replace("\n", ""),
      p.ToString()
    );
  }

  [Test]
  public void testPascalGrammar() {
    this.parseAndCompare(
      @"
program     ::= ""PROGRAM"" identifier
                ""BEGIN""
                { assignment }
                ""END.""
              ;

assignment  ::= identifier "":="" expression "";"" ;

expression  ::= identifier
              | string
              | number
              ;

identifier  ::= /([A-Z][A-Z0-9]*)/ ;
string      ::= /""([^""]*)""|'([^']*)'/ ;
number      ::= /(-?[1-9][0-9]*)/ ;
      ",
      @"
Grammar(
  Rules=[
    Rule(
      Identifier=program,
      Expression=SequentialExpression(
        NonSequentialExpression=StringExpression(Name=,String=PROGRAM),
        Expression=SequentialExpression(
          NonSequentialExpression=IdentifierExpression(Name=,Identifier=identifier),
          Expression=SequentialExpression(
            NonSequentialExpression=StringExpression(Name=,String=BEGIN),
            Expression=SequentialExpression(
              NonSequentialExpression=RepetitionExpression(
                Expression=IdentifierExpression(Name=,Identifier=assignment)
              ),
              Expression=StringExpression(Name=,String=END.)
            )
          )
        )
      )
    ),
    Rule(
      Identifier=assignment,
      Expression=SequentialExpression(
        NonSequentialExpression=IdentifierExpression(Name=,Identifier=identifier),
        Expression=SequentialExpression(
          NonSequentialExpression=StringExpression(Name=,String=:=),
          Expression=SequentialExpression(
            NonSequentialExpression=IdentifierExpression(Name=,Identifier=expression),
            Expression=StringExpression(Name=,String=;)
          )
        )
      )
    ),
    Rule(
      Identifier=expression,
      Expression=AlternativesExpression(
        AtomicExpression=IdentifierExpression(Name=,Identifier=identifier),
        NonSequentialExpression=AlternativesExpression(
          AtomicExpression=IdentifierExpression(Name=,Identifier=string),
          NonSequentialExpression=IdentifierExpression(Name=,Identifier=number)
        )
      )
    ),
    Rule(
      Identifier=identifier,
      Expression=ExtractorExpression(Name=,Regex=([A-Z][A-Z0-9]*))
    ),
    Rule(
      Identifier=string,
      Expression=ExtractorExpression(Name=,Regex=""([^""]*)""|'([^']*)')
    ),
    Rule(
      Identifier=number,
      Expression=ExtractorExpression(Name=,Regex=(-?[1-9][0-9]*))
    )
  ]
)
      ",
      @"
Model(
  Entities=[
    Entity(
      Name=program,Type=program,Properties=[
        Property(Name=identifier,Type=string,Source=ConsumeEntity(identifier)->identifier),
        Property(Name=assignment,Type=assignment,IsPlural,Source=ConsumeEntity(assignment)*->assignment)
      ],ParseAction=ConsumeAll([
        ConsumeString(PROGRAM),
        ConsumeEntity(identifier)->identifier,
        ConsumeString(BEGIN),
        ConsumeEntity(assignment)*->assignment,
        ConsumeString(END.) 
      ])
    ),
    Entity(
      Name=assignment,Type=assignment,Properties=[
        Property(Name=identifier,Type=string,Source=ConsumeEntity(identifier)->identifier),
        Property(Name=expression,Type=string,Source=ConsumeEntity(expression)->expression)
      ],ParseAction=ConsumeAll([
        ConsumeEntity(identifier)->identifier,
        ConsumeString(:=),
        ConsumeEntity(expression)->expression,
        ConsumeString(;)
      ])
    ),
    VirtualEntity(
      Name=expression,Type=string,Subs=[identifier,string,number],Properties=[
        Property(Name=alternative,Type=string,Source=ConsumeAny([
          ConsumeEntity(identifier)->alternative,
          ConsumeEntity(string)->alternative,
          ConsumeEntity(number)->alternative
        ])->alternative)
      ],ParseAction=ConsumeAny([
        ConsumeEntity(identifier)->alternative,
        ConsumeEntity(string)->alternative,
        ConsumeEntity(number)->alternative
      ])->alternative
    ),
    VirtualEntity(
      Name=identifier,Type=string,Supers=[expression],Properties=[
       Property(Name=identifier,Type=string,Source=ConsumePattern(([A-Z][A-Z0-9]*))->identifier)
      ],ParseAction=ConsumePattern(([A-Z][A-Z0-9]*))->identifier
    ),
    VirtualEntity(
      Name=string,Type=string,Supers=[expression],Properties=[
       Property(Name=string,Type=string,Source=ConsumePattern(""([^""]*)""|'([^']*)')->string)
      ],ParseAction=ConsumePattern(""([^""]*)""|'([^']*)')->string
    ),
    VirtualEntity(
      Name=number,Type=string,Supers=[expression],Properties=[
       Property(Name=number,Type=string,Source=ConsumePattern((-?[1-9][0-9]*))->number)
      ],ParseAction=ConsumePattern((-?[1-9][0-9]*))->number
    )
  ],
  Root=program
)
      "
    );
  }

}
