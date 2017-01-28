// runner example - implements grammar for (small subset) of the Pascal language
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

using HumanParserGenerator.Grammars;
using HumanParserGenerator.Parser;
using HumanParserGenerator.Emitter;

public class Runner {

  public static void Main(string[] args) {

    var grammarName = "pascal"; // default
    if(args.Length == 1) {
      grammarName = args[0];
    }

    Grammar grammar;

    switch(grammarName) {
      case "bnf": grammar = AsModel.BNF;    break;
      default:    grammar = AsModel.Pascal; break;
    }

    Model model = new Model().Import(grammar);

    // Console.WriteLine(model.ToString());
    // Console.WriteLine();

    CSharp code = new CSharp().Generate(model);
    Console.WriteLine(code.ToString());
  }
}
