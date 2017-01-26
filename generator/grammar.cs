// Grammar Model - The classes to model an EBNF-like parsed notation
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace Grammar {

  public class Model {
    public List<Rule> Rules { get; set; }
    public override string ToString() {
      return
        "Grammar(" + 
          "[" + string.Join(",", this.Rules.Select(x => x.ToString())) + "]" +
        ")";
    }
  }

  public class Rule {
    public string     Id { get; set; }
    public Expression Exp { get; set; }
    public override string ToString() {
      return
        "Rule(" +
          "Id=" + this.Id + "," + 
          "Exp=" + this.Exp.ToString() +
        ")";
    }
  }

  public abstract class Expression {}

  public class IdentifierExpression : Expression {
    public string Id;
    public override string ToString() {
      return
        "IdentifierExpression(" +
          "Id=" + this.Id +
        ")";
    }
  }

  public class StringExpression : Expression {
    public string String;
    public override string ToString() {
      return
        "StringExpression(" +
          "String=" + this.String +
        ")";
    }
  }

  public class OptionalExpression : Expression {
    public Expression Exp;
    public override string ToString() {
      return
        "OptionalExpression(" +
          "Exp=" + this.Exp.ToString() +
        ")";
    }
  }

  public class RepetitionExpression : Expression {
    public Expression Exp;
    public override string ToString() {
      return
        "RepetitionExpression(" +
          "Exp=" + this.Exp.ToString() +
        ")";
    }
  }

  public class GroupExpression : Expression {
    public Expression Exp;
    public override string ToString() {
      return
        "GroupExpression(" +
          "Exp=" + this.Exp.ToString() +
        ")";
    }
  }

  public class AlternativesExpression : Expression {
    public List<Expression> Expressions;
    public override string ToString() {
      return
        "AlernativesExpression(" +
          "Expressions=" + "[" + 
            string.Join("|", this.Expressions.Select(x => x.ToString())) +
          "]" +
        ")";
    }
  }

  public class SequenceExpression : Expression {
    public List<Expression> Expressions;
    public override string ToString() {
      return
        "SequenceExpression(" +
          "Expressions=" + "[" + 
            string.Join(",", this.Expressions.Select(x => x.ToString())) +
          "]" +
        ")";
    }
  }

  public class Extractor : Expression {
    public string Pattern { get; set; }
    public override string ToString() {
      return
        "Extractor(" +
          "Pattern=" + this.Pattern +
        ")";
    }
  }

}
