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

  public class HPG {

    public static void Main(string[] args) {
      new HPG(args).Generate();
    }

    private string input          = null;

    enum Output { Parser, AST, Model };
    private Output output = Output.Parser;
    
    private HPG(string[] args) {
      foreach(string arg in args) {
        if(arg.StartsWith("-")) {
          if( ! this.ProcessOption(arg) ) { return; }
        } else {
          if( ! this.ProcessFile(arg) ) { return; }
        }
      }

      // no file-based input? => try stdin
      if(input == null) {
        string s;
        while( (s = Console.ReadLine()) != null ) {
          input += s;
        }
      }
      
      if(input == null) {
        this.Fail("No input detected.");
      }
    }
    
    private bool ProcessOption(string option) {
      // try switches first
      try {
        return new Dictionary<string, Func<bool>>() {
          { "-h",       this.ShowHelp       },
          { "--help",   this.ShowHelp       },
          { "-p",       this.GenerateParser },
          { "--parser", this.GenerateParser },
          { "-a",       this.ShowAST        },
          { "--ast",    this.ShowAST        },
          { "-m",       this.ShowModel      },
          { "--model",  this.ShowModel      },
        }[option]();
      } catch(KeyNotFoundException) {}

      return this.Fail("Unknown option: " + option);
    }
    
    private bool ProcessFile(string file) {
      if(! File.Exists(file)) {
        return this.Fail("Unknown file: " + file);
      }
      input += System.IO.File.ReadAllText(file);
      return true;
    }

    private bool Fail(string msg) {
      Console.Error.WriteLine(msg);
      return this.ShowHelp();
    }

    private bool ShowHelp() {
      Console.WriteLine("Usage: hpg.exe [options] [file ...]");
      Console.WriteLine();
      Console.WriteLine("    --help, -h              Show usage information");
      Console.WriteLine();
      Console.WriteLine("Output options.");
      Console.WriteLine("Select one of the following:");
      Console.WriteLine("    --parser, -p            Generate parser (DEFAULT)");
      Console.WriteLine("    --ast, -a               Show AST");
      Console.WriteLine("    --model, -m             Show parser model");
      return false;
    }

    private bool GenerateParser() {
      this.output = Output.Parser;
      return true;
    }
    
    private bool ShowAST() {
      this.output = Output.AST;
      return true;
    }
    
    private bool ShowModel() {
      this.output = Output.Model;
      return true;
    }

    private void Generate() {
      if(this.input == null) { return; }

      // EBNF-like file -> AST/Grammar Model
      Grammar grammar = new Parser().Parse(this.input).AST;

      if( this.output == Output.AST ) {
        Console.WriteLine(grammar.ToString());
        return;
      }

      // Grammar Model -> Generator/Parser Model
      Generator.Model model = new Generator.Factory().Import(grammar).Model;

      if( this.output == Output.Model ) {
        Console.WriteLine(model.ToString());
        return;
      }

      // Generator/Parser Model -> CSharp code
      Emitter.CSharp code = new Emitter.CSharp().Generate(model);

      Console.WriteLine(code.ToString());
    }

  }
}
