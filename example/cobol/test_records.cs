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

  private Copybook testSingleSentence(string src, Record expected) {
    Copybook book = this.parse(src);
    Assert.AreEqual(1, book.Records.Count);
    Assert.AreEqual( expected.ToString(), book.Records[0].ToString() );
    return book;
  }

  [Test]
  public void BasicRecordWithoutOptions() {
    Copybook book = this.testSingleSentence(
      "01 TOP.",
      new BasicRecord() {
        Level = new Int() { Value="01" },
        LevelName = new LevelName() {
          HasFiller = false,
          Identifier = new Identifier() { Name="TOP" }
        },
        Options = new List<Option>() {}
      }
    );
    Assert.AreEqual(((BasicRecord)book.Records[0]).Level.Value, "01");
  }

  [Test]
  public void BasicRecordWithPicWithCompOption() {
    this.testSingleSentence(
      "10 FIELD   PIC S9(05) COMP-5.",
      new BasicRecord() {
        Level      = new Int() { Value="10" },
        LevelName  = new LevelName() {
          HasFiller  = false,
          Identifier = new Identifier() { Name="FIELD" }
        },
        Options = new List<Option>() {
          new PictureFormatOption() {
            Type = "S9",
            Digits = new Int() { Value = "05" },
            DecimalType = null,
            DecimalDigits = null
          },
          new CompUsage() { Level="5" }
        }
      }
    );
  }

  [Test]
  public void BasicRecordWithPicAndSignOption() {
    this.testSingleSentence(
      "10 FIELD PIC S9(18) SIGN TRAILING SEPARATE.",
      new BasicRecord() {
        Level = new Int() { Value = "10" },
        LevelName = new LevelName() {
          HasFiller  = false,
          Identifier = new Identifier() { Name = "FIELD" },
        },
        Options = new List<Option>() {
          new PictureFormatOption() {
            Type = "S9",
            Digits = new Int() { Value="18" },
            DecimalType = null,
            DecimalDigits = null 
          },
          new SignOption() {
            HasLeading   = false,
            HasTrailing  = true,
            HasSeparate  = true,
            HasCharacter = false
          }
        }
      }
    );
  }

  [Test]
  public void BasicRecordWithOccursTimes() {
    this.testSingleSentence(
      "10 FIELDS OCCURS 10 TIMES.",
      new BasicRecord() {
        Level = new Int() { Value = "10" },
        LevelName = new LevelName() {
          HasFiller = false,
          Identifier = new Identifier() { Name = "FIELDS" },
        },
        Options = new List<Option> {
          new OccursOption() {
            Amount = new Int() { Value = "10" },
            UpperBound = null,
            DependsOn = null,
            Keys = new List<Key>() {},
            Indexes = new List<Identifier>() {}
          }
        }
      }
    );
  }
}
