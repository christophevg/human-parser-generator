// HumanParserGenerator driver application
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Diagnostics;

using HumanParserGenerator;

namespace HumanParserGenerator {

  public class Runner {

    public static void Main(string[] args) {

      if(args.Length != 1) {
        Console.WriteLine("USAGE: main.exe <filename> [options]");
        return;
      }

      if(! File.Exists(args[0])) {
        Console.WriteLine("Unknown file");
        return;
      }


      new Runner().Generate(args[0]);
    }
    
    private void Generate(string file) {
      // EBNF-like file -> AST/Grammar Model
      string input  = System.IO.File.ReadAllText(file);
      Grammar grammar = new Parser().Parse(input).AST;

      this.Log(grammar.ToString());

      // Grammar Model -> Generator/Parser Model
      Generator.Model model = new Generator.Factory().Import(grammar).Model;

      this.Log(model.ToString());

      // Generator/Parser Model -> CSharp code
      Emitter.CSharp code = new Emitter.CSharp().Generate(model);
      Console.WriteLine(code.ToString());
    }

    [ConditionalAttribute("DEBUG")]
    private void Log(string msg) {
      Console.Error.WriteLine("### " + msg );
    }

  }
}
