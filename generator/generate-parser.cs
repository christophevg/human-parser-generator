// runner example - implements grammar for (small subset) of the Pascal language
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

using Grammar = HumanParserGenerator.Grammar;
using Parser  = HumanParserGenerator.Parser;
using Emitter = HumanParserGenerator.Emitter;

public class Runner {

  public static void Main(string[] args) {

    var grammarName = "pascal"; // default
    if(args.Length == 1) {
      grammarName = args[0];
    }

    Grammar.Model grammar;

    switch(grammarName) {
      case "bnf": grammar = Grammar.AsModel.BNF();    break;
      default:    grammar = Grammar.AsModel.Pascal(); break;
    }

    Parser.Model model = new Parser.Model().Import(grammar);

    // Console.WriteLine(model.ToString());
    // Console.WriteLine();

    Emitter.CSharp code = new Emitter.CSharp().Generate(model);
    Console.WriteLine(code.ToString());
  }
}
