// unit tests for Generator Model Factory
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using NUnit.Framework;

using System.Collections.Generic;

using HumanParserGenerator.Generator;

[TestFixture]
public class BNFParserTests {

  private Grammar parse(string input) {
    return new Parser().Parse(input).AST;
  }

  private Model transform(Grammar grammar) {
    return new Factory().Import(grammar).Model;
  }

  private void compare(string result, string expected) {
    Assert.AreEqual(
      expected.Replace(" ", "").Replace("\n", ""),
      result
    );
  }

  private void processAndCompare(string input, string grammar, string model ) {
    Grammar g = this.parse(input);
    if(grammar != null) { this.compare(g.ToString(), grammar); }
    Model m = this.transform(g);
    if(model != null) { this.compare(m.ToString(), model); }
  }

  private Model process(string input) {
    return this.transform(this.parse(input));
  }

  [Test]
  public void testPascalGrammar() {
    this.processAndCompare(
      @"
program               ::= ""PROGRAM"" identifier
                          ""BEGIN""
                          { assignment }
                          ""END.""
                        ;

assignment            ::= identifier "":="" expression "";"" ;

expression            ::= identifier
                        | string
                        | number
                        ;

identifier            ::= name  @ /([A-Z][A-Z0-9]*)/ ;
string                ::= text  @ /""([^""]*)""|'([^']*)'/ ;
number                ::= value @ /(-?[1-9][0-9]*)/ ;

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
      Expression=ExtractorExpression(Name=name,Pattern=([A-Z][A-Z0-9]*))
    ),
    Rule(
      Identifier=string,
      Expression=ExtractorExpression(Name=text,Pattern=""([^""]*)""|'([^']*)')
    ),
    Rule(
      Identifier=number,
      Expression=ExtractorExpression(Name=value,Pattern=(-?[1-9][0-9]*))
    )
  ]
)
      ",
      @"
Model(
  Entities=[
    Entity(
      Name=program,Type=program,Properties=[
        Property(Name=identifier,Type=identifier,Source=ConsumeEntity(identifier)->identifier),
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
        Property(Name=identifier,Type=identifier,Source=ConsumeEntity(identifier)->identifier),
        Property(Name=expression,Type=expression,Source=ConsumeEntity(expression)->expression)
      ],ParseAction=ConsumeAll([
        ConsumeEntity(identifier)->identifier,
        ConsumeString(:=),
        ConsumeEntity(expression)->expression,
        ConsumeString(;)
      ])
    ),
    VirtualEntity(
      Name=expression,Type=expression,Subs=[identifier,string,number],Properties=[
        Property(Name=alternative,Type=expression,Source=ConsumeAny([
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
    Entity(
      Name=identifier,Type=identifier,Supers=[expression],Properties=[
       Property(Name=name,Type=<string>,Source=ConsumePattern(([A-Z][A-Z0-9]*))->name)
      ],ParseAction=ConsumePattern(([A-Z][A-Z0-9]*))->name
    ),
    Entity(
      Name=string,Type=string,Supers=[expression],Properties=[
       Property(Name=text,Type=<string>,Source=ConsumePattern(""([^""]*)""|'([^']*)')->text)
      ],ParseAction=ConsumePattern(""([^""]*)""|'([^']*)')->text
    ),
    Entity(
      Name=number,Type=number,Supers=[expression],Properties=[
       Property(Name=value,Type=<string>,Source=ConsumePattern((-?[1-9][0-9]*))->value)
      ],ParseAction=ConsumePattern((-?[1-9][0-9]*))->value
    )
  ],
  Root=program
)
      "
    );
  }

  [Test]
  public void testCobolValueDefinition() {
    Model model = this.process(
      @"
copybook         ::= { sentence };
sentence         ::= record ""."";
record           ::= renames-record | values-record ;

renames-record   ::= ""66"" level-name ""RENAMES"" identifier-range ;
level-name       ::= identifier | ""FILLER"" ;
identifier-range ::= identifier ""THRU"" identifier ;

values-record    ::= ""88"" level-name ""VALUES"" { value } ;
value            ::= literal | variable ;
literal          ::= int | string ;
variable         ::= identifier [ ""("" subset "")"" ];
subset           ::= numeric [ "":"" subset ];
numeric          ::= int | identifier;
identifier       ::= /([A-Z][A-Z0-9]*)/ ;
string           ::= /""([^""]*)""|'([^']*)'/ ;
int              ::= /(-?[1-9][0-9]*)/ ;"
    );

    Assert.IsTrue  (             model["literal"].IsVirtual );
    Assert.AreEqual( "literal",  model["literal"].Type      );

    Assert.IsFalse (             model["variable"].IsVirtual);
    Assert.AreEqual( "variable", model["variable"].Type      );

    Assert.IsTrue( model["value"].IsVirtual );
  }

  [Test]
  public void testPropertiesFromStringExpression() {
    this.processAndCompare(
      @"
sign-option ::=
   ""SIGN"" [ _ @ ""IS"" ]
   ( ""LEADING"" | ""TRAILING"" )
   [ ""SEPARATE"" [ ""CHARACTER"" ] ]
;",
      @"
Grammar(
  Rules=[
    Rule(
      Identifier=sign-option,
      Expression=SequentialExpression(
        NonSequentialExpression=StringExpression(Name=,String=SIGN),
        Expression=SequentialExpression(
          NonSequentialExpression=OptionalExpression(
            Expression=StringExpression(Name=_,String=IS)
          ),
          Expression=SequentialExpression(
            NonSequentialExpression=GroupExpression(
              Expression=AlternativesExpression(
                AtomicExpression=StringExpression(Name=,String=LEADING),
                NonSequentialExpression=StringExpression(Name=,String=TRAILING)
              )
            ),
            Expression=OptionalExpression(
              Expression=SequentialExpression(
                NonSequentialExpression=StringExpression(Name=,String=SEPARATE),
                Expression=OptionalExpression(
                  Expression=StringExpression(Name=,String=CHARACTER))))))))])",
      @"
Model(
  Entities=[
    Entity(
      Name=sign-option,Type=sign-option,Properties=[
        Property(Name=has-LEADING,Type=<bool>,Source=ConsumeString(LEADING)!->has-LEADING),
        Property(Name=has-TRAILING,Type=<bool>,Source=ConsumeString(TRAILING)!->has-TRAILING),
        Property(Name=has-SEPARATE,Type=<bool>,Source=ConsumeString(SEPARATE)!->has-SEPARATE),
        Property(Name=has-CHARACTER,Type=<bool>,IsOptional,Source=ConsumeString(CHARACTER)?!->has-CHARACTER)
      ],
      ParseAction=ConsumeAll([
        ConsumeString(SIGN),
        ConsumeString(IS)?,
        ConsumeAny([
          ConsumeString(LEADING)!->has-LEADING,
          ConsumeString(TRAILING)!->has-TRAILING]),
          ConsumeAll([
            ConsumeString(SEPARATE)!->has-SEPARATE,
            ConsumeString(CHARACTER)?!->has-CHARACTER
          ])?
      ])
    )
  ],
  Root=sign-option
)"
    );
  }

  [Test]
  public void testComplexRepetitions() {
    Model model = this.process(
      "grammar = { \"prefix\" rule }; rule = /a/;"
    );
    Assert.IsFalse( model["grammar"]["rule"].IsPlural);

    Assert.IsTrue ( model["grammar"]["rule"].Source.HasPluralParent);
  }

  [Test]
  public void testNestedRepetitions() {
    Model model = this.process(
      "grammar = { rule { postfix } }; rule = /a/; postfix = /b/;"
    );
    Assert.IsFalse( model["grammar"]["rule"].IsPlural);

    Assert.IsTrue ( model["grammar"]["postfix"].IsPlural);
    Assert.IsTrue ( model["grammar"]["rule"].Source.HasPluralParent);
    Assert.IsTrue ( model["grammar"]["postfix"].Source.HasPluralParent);
  }

}
