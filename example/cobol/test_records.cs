// unit tests for Generator Model
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using System.Collections.Generic;

using NUnit.Framework;

[TestFixture]
public class GeneratorModelTests {

  Parser parser;

  [SetUp]
  public void SetUp() {
    this.parser = new Parser();
  }

  [TearDown]
  public void TearDown() {
    this.parser = null;
  }

  private Copybook parse(string src) {
    return this.parser.Parse(src).AST;
  }

  private Copybook testSingleSentence(string src, string expected) {
    Copybook book = this.parse(src);
    Assert.AreEqual(1, book.Records.Count);
    Assert.AreEqual(
      expected.Replace("\n", "").Replace(" ", ""),
      book.Records[0].ToString()
    );
    return book;
  }

  [Test]
  public void BasicRecordWithoutOptions() {
    Copybook book = this.testSingleSentence(
      "01 TOP.",
      "BasicRecord(Int=Int(Value=01),LevelName=Identifier(Name=TOP),Options=[])"
    );
    Assert.AreEqual(((BasicRecord)book.Records[0]).Int.Value, "01");
  }

  [Test]
  public void BasicRecordWithPicWithCompOption() {
    this.testSingleSentence(
      "10 FIELD   PIC S9(05) COMP-5.",
      @"BasicRecord(Int=Int(Value=10),LevelName=Identifier(Name=FIELD),Options=[
        Picture(
          PictureHeader0=PictureHeader(
            PictureLabel=PictureLabel(),
            HasIs=False
          ),
          PictureType0=,
          Int0=Int(Value=05),
          HasAny=False,
          PictureType1=,
          Int1=,
          HasPictureTypeInt=False,
          HasInt=True,
          PictureHeader1=,
          String=
        ),
        UsageOption(
          HasIs=False,
          HasAll=False,
          Usage=CompUsage(CompLevel=5,HasDigit=True)
        )
      ])"
    );
  }

}
