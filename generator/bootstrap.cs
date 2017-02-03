// Bootstrapping using a Code-based Grammar Model
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;

using HumanParserGenerator;

namespace HumanParserGenerator {

  public class Bootstrap {

    public static void Main(string[] args) {

      Grammar grammar = AsModel.BNF;
      Generator.Model model = new Generator.Factory().Import(grammar).Model;

      // outputs the intermediate Parser Model
      Console.Error.WriteLine(model.ToString());
      Console.Error.WriteLine();

      Emitter.CSharp code = new Emitter.CSharp().Generate(model);
      Console.WriteLine(code.ToString());
    }
  }

}
