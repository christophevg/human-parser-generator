// small runner example to load a file, parse it and dump the AST
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;

public class Runner {
  public static void Main(string[] args) {

    string input = "";
    if(args.Length == 1) {
      if(! File.Exists(args[0])) {
        Console.WriteLine("Unknown file.");
        return;
      }
      input  = System.IO.File.ReadAllText(args[0]);
    } else {
      string s;
      while( (s = Console.ReadLine()) != null ) {
        input += s;
      }
    }
  
      Parser parser = new Parser();
      try {
        Console.WriteLine(parser.Parse(input).AST.ToString());
      } catch(ParseException e) {
        // recurse down the Exception tree, to reach the most specific one
        while(e.InnerException != null) {
          e = e.InnerException as ParseException;
        } 
        Console.Error.WriteLine(e.Message);
      }
  }
}
