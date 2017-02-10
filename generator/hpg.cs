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

      string input = "";

      if(args.Length == 1) {
        if(! File.Exists(args[0])) {
          Console.WriteLine("Unknown file");
          return;
        }
        input = System.IO.File.ReadAllText(args[0]);
      } else {
        string s;
        while( (s = Console.ReadLine()) != null ) {
          input += s;
        }
      }

      new Runner().Generate(input);
    }
    
    private void Generate(string input) {
      // EBNF-like file -> AST/Grammar Model
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
