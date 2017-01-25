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
  
    string input = System.IO.File.ReadAllText(args[0]);
  
    // create new DDL parsing object and parse the input
    Parser parser = new Parser();
    Console.WriteLine(parser.Parse(input).AST.ToString());
  }
}
