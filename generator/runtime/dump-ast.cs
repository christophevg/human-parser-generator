// small runner example to load a file, parse it and dump the AST
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;

public class Runner {
  public static void Main(string[] args) {

    if(args.Length != 1) {
      Console.WriteLine("USAGE: main.exe <filename>");
      return;
    }
  
    if(! File.Exists(args[0])) {
      Console.WriteLine("Unknown file");
      return;
    }
  
    string input  = System.IO.File.ReadAllText(args[0]);
    Parser parser = new Parser();
    string code   = parser.Parse(input).AST.ToString();

    Console.WriteLine(code);
  }
}
