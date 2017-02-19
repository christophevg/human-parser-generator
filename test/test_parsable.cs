// unit tests for Generator Model
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using NUnit.Framework;

using System.Collections.Generic;

[TestFixture]
public class ParsableTests {

  [Test]
  public void testConsumeText() {
    var p = new Parsable("123  4567890");
    Assert.IsTrue(p.Consume("123"));
    Assert.IsTrue(p.Consume("456")); // skipping whitespace
  }

  [Test]
  public void testLineOf() {
    var p = new Parsable("1234567890\n1234567890\n1234567890\n1234567890");
    Assert.AreEqual(0, p.LineOf(0));
    Assert.AreEqual(0, p.LineOf(11));
    Assert.AreEqual(1, p.LineOf(12));
    Assert.AreEqual(1, p.LineOf(22));
    Assert.AreEqual(2, p.LineOf(23));
    Assert.AreEqual(2, p.LineOf(27));
  }

  [Test]
  public void testLinePosition() {
    var p = new Parsable("1234567890\n1234567890\n1234567890\n1234567890");
    p.Consume("1234567890"); // skipping also \n whitespace
    p.Consume("1234567890");
    p.Consume("12345");
    Assert.AreEqual(27, p.Position);
    Assert.AreEqual( 2, p.Line);
    Assert.AreEqual( 5, p.LinePosition);
  }

  [Test]
  public void testFailurePosition() {
    var p = new Parsable("1234567890\n1234567890\n1234567890\n1234567890");
    p.Consume("1234567890"); // skipping also \n whitespace
    p.Consume("1234567890");
    p.Consume("12345");
    try {
      p.Consume("FAIL");
      Assert.Fail("Should have thrown ParseException");
    } catch(ParseException e) {
      Assert.AreEqual(27, e.Position);
      Assert.AreEqual( 2, e.Line);
      Assert.AreEqual( 5, e.LinePosition);
    }
  }
}
