// HumanParserGenerator driver application
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Diagnostics;

using HumanParserGenerator.Generator;

namespace HumanParserGenerator {

  public class HPG {

    public static void Main(string[] args) {
      new HPG(args).Generate();
    }

    // configuration

    private string input          = null;

    enum Output { Parser, AST, Model, Grammar };
    private Output output = Output.Parser;

    enum Format { Text, Dot };
    private Format format = Format.Text;

    private bool         emitInfo      = true;
    private bool         emitRule      = true;
    private List<string> sources       = new List<string>();
    private string       emitNamespace = null;

    // argument processing
    
    private HPG(string[] args) {
      int i=0;
      while( i < args.Length ) {
        if(args[i].StartsWith("-")) {
          if( this.HasArgument(args[i]) ) {
            if( i+1 < args.Length ) {
              this.ProcessOption(args[i], args[i+1]);
              i++;
            } else { return; }
          } else {
            if( ! this.ProcessOption(args[i]) ) { return; }
          }
        } else {
          if( ! this.ProcessFile(args[i]) ) { return; }
        }
        i++;
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
    
    private bool HasArgument(string option) {
      return new List<string>() {
        "-n", "--namespace"
      }.Contains(option);
    }

    private bool ProcessOption(string option) {
      try {
        return new Dictionary<string, Func<bool>>() {
          { "-h",       this.ShowHelp      }, { "--help",    this.ShowHelp      },
          { "-p",       this.OutputParser  }, { "--parser",  this.OutputParser  },
          { "-a",       this.OutputAST     }, { "--ast",     this.OutputAST     },
          { "-m",       this.OutputModel   }, { "--model",   this.OutputModel   },
          { "-g",       this.OutputGrammar }, { "--grammar", this.OutputGrammar },
          { "-t",       this.FormatText    }, { "--text",    this.FormatText    },
          { "-d",       this.FormatDot     }, { "--dot",     this.FormatDot     },
          { "-i",       this.SuppressInfo  }, { "--info",    this.SuppressInfo  },
          { "-r",       this.SuppressRule  }, { "--rule",    this.SuppressRule  }
        }[option]();
      } catch(KeyNotFoundException) {}

      return this.Fail("Unknown option: " + option);
    }

    private bool ProcessOption(string option, string arg) {
      try {
        return new Dictionary<string, Func<string,bool>>() {
          { "-n",  this.UseNamespace }, { "--namespace", this.UseNamespace }
        }[option](arg);
      } catch(KeyNotFoundException) {}

      return this.Fail("Unknown option: " + option);      
    }
    
    private bool ProcessFile(string file) {
      if(! File.Exists(file)) {
        return this.Fail("Unknown file: " + file);
      }
      input += System.IO.File.ReadAllText(file);
      this.sources.Add(file);
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
      Console.WriteLine("    --grammar, -g           Show grammar");
      Console.WriteLine("Formatting options.");
      Console.WriteLine("    --text, -t              Generate textual output (DEFAULT).");
      Console.WriteLine("    --dot, -d               Generate Graphviz/Dot format output. (model)");
      Console.WriteLine("Emission options.");
      Console.WriteLine("    --info, -i              Suppress generation of info header");
      Console.WriteLine("    --rule, -r              Suppress generation of rule comment");
      Console.WriteLine("    --namespace, -n NAME    Embed parser in namespace");
      return false;
    }

    private bool OutputParser()  { this.output = Output.Parser;  return true; }
    private bool OutputAST()     { this.output = Output.AST;     return true; }
    private bool OutputModel()   { this.output = Output.Model;   return true; }
    private bool OutputGrammar() { this.output = Output.Grammar; return true; }
    private bool FormatText()    { this.format = Format.Text;    return true; }
    private bool FormatDot()     { this.format = Format.Dot;     return true; }
    private bool SuppressInfo()  { this.emitInfo = false;        return true; }
    private bool SuppressRule()  { this.emitRule = false;        return true; }
    private bool UseNamespace(string name) {
      this.emitNamespace = name;
      return true;
    }

    // Generation of Model and Parser

    private void Generate() {
      if(this.input == null) { return; }

      // EBNF-like file -> AST/Grammar Model
      Grammar grammar = new Parser().Parse(this.input).AST;

      if( this.output == Output.AST ) {
        Console.WriteLine(grammar.ToString());
        return;
      }

      if( this.output == Output.Grammar ) {
        Console.WriteLine(
          new Emitter.BNF() {
            EmitInfo  = this.emitInfo,
            Sources   = this.sources
          }
          .Generate(grammar)
          .ToString());
        return;
      }

      // Grammar Model -> Generator/Parser Model
      Model model = new Factory().Import(grammar).Model;

      if( this.output == Output.Model ) {
        if(this.format == Format.Dot) {
          Console.WriteLine(this.Dotify(model));
          return;
        }
        Console.WriteLine(model.ToString());
        return;
      }

      // Generator/Parser Model -> CSharp code
      Emitter.CSharp code = new Emitter.CSharp() {
        EmitInfo  = this.emitInfo,
        EmitRule  = this.emitRule,
        Namespace = this.emitNamespace,
        Sources   = this.sources
      }
      .Generate(model);

      Console.WriteLine(code.ToString());
    }
    
    private string Dotify(Model model) {
      string dot = "";
      dot += this.GenerateDotHeader();
      foreach(Entity entity in model.Entities) {
        dot += this.GenerateEntity(entity);
      }
      foreach(Entity entity in model.Entities) {
        dot += this.GenerateGeneralizations(entity);
      }
      dot += this.GenerateDotFooter();
      return dot;
    }

    private string GenerateDotHeader() {
      return @"
      digraph G {
        rankdir=BT;

        node [
          fontname = ""Bitstream Vera Sans""
          fontsize = 10
          shape = ""record""
        ]

        edge [
          fontname = ""Bitstream Vera Sans""
          fontsize = 8
          arrowhead = ""empty""
        ]
";
      }

    private string GenerateDotFooter() {
      return "\n}\n";
    }

    private string GenerateEntity(Entity entity) {
      // Entity [
      //   label = "{Entity|+ property : type\l ... |+ method() : void\l}"
      // ]
      string dot = "";
      dot = "\n" + this.PascalCase(entity.Name) + 
        " [ label = \"{" + (entity.IsVirtual ? " / " : "") +
        this.PascalCase(entity.Name) + "|";
      foreach(Property property in entity.Properties) {
        dot += "+ " + this.PascalCase(property.Name) +
          this.PluralSuffix(property) +
          " : " + (property.IsPlural ? "[" : "") +
          this.GenerateType(property.Type) +
          (property.IsPlural ? "]" : "") +  "\\l";
      }  
      dot += "}\" ]";
      return dot;
    }
    
    private string GenerateGeneralizations(Entity entity) {
      // Sub1 -> Entity
      // Sub2 -> Entity
      // { rank=same Sub1, Sub2 }
      if(entity.Subs.Count == 0) { return ""; } 
      string dot = "";
      foreach(Entity sub in entity.Subs) {
        dot += "\n" + this.PascalCase(sub.Name) + " -> " +
          this.PascalCase(entity.Name);
      }
      if(entity.Subs.Count > 1) {
        dot += "\n{ rank=same " +
          string.Join(",", entity.Subs.Select(s=>this.PascalCase(s.Name)))+ "}";
      }
      return dot;
    }

    // TODO reuse from Emitter ;-)
    private string PascalCase(string text) {
      return string.Join("",
        text.Split('-').Select(x =>
          x.First().ToString().ToUpper() + x.ToLower().Substring(1)
        )
      );
    }

    private string GenerateType(string type) {
      if( type == null ) { return "Object"; }
      if( type.Equals("<string>") ) { return "string"; }
      if( type.Equals("<bool>") ) { return "bool"; }
      return this.PascalCase(type);
    }

    private string PluralSuffix(Generator.Property property) {
      if(! property.IsPlural ) { return ""; }
      if( property.Name.EndsWith("x") ) { return "es"; }
      return "s";
    }

  }
}
