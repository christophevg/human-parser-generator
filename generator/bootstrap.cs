// Bootstrapping using a Code-based Grammar Model
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using System.Diagnostics;

using HumanParserGenerator;

namespace HumanParserGenerator {

  public class Bootstrap {

    public static void Main(string[] args) {
      new Bootstrap().Generate(args[0]);
    }

    private void Generate(string file) {
      Grammar grammar = AsModel.BNF;
      Generator.Model model = new Generator.Factory().Import(grammar).Model;

      this.Log(model.ToString());

      Emitter.CSharp code = new Emitter.CSharp().Generate(model);
      Console.WriteLine(code.ToString());
    }


    [ConditionalAttribute("DEBUG")]
    private void Log(string msg) {
      Console.Error.WriteLine("### " + msg );
    }

  }
}
