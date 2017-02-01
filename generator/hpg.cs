// HumanParserGenerator driver application
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

using HumanParserGenerator;

namespace HumanParserGenerator {

  public class Runner {

    public static void Main(string[] args) {

      if(args.Length != 1) {
        Console.WriteLine("USAGE: main.exe <filename>");
        return;
      }

      bool showAst   = false;
      bool showModel = false;

      if(! File.Exists(args[0])) {
        Console.WriteLine("Unknown file");
        return;
      }

      // EBNF-like file -> AST/Grammar Model
      string input  = System.IO.File.ReadAllText(args[0]);
      Grammar grammar = new Parser().Parse(input).AST;

      if(showAst) {
        Console.WriteLine(grammar.ToString());
        Console.WriteLine();
      }

      // Grammar Model -> Generator/Parser Model
      Generator.Model model = new Generator.Model().Import(grammar);

      if(showModel) {
        Console.WriteLine(model.ToString());
        Console.WriteLine();      
      }

      // Generator/Parser Model -> CSharp code
      Emitter.CSharp code = new Emitter.CSharp().Generate(model);
      Console.WriteLine(code.ToString());
    }
  }

}
