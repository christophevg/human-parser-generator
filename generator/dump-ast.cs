// small runner example to load a file, parse it and dump the AST
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

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
      Console.WriteLine(parser.Errors.Count);
    } catch(ParseException) {
      // foreach(ParseException error in parser.Errors) {
      //   var e = error;
      //   var indent = "";
      //   while(e!=null) {
      //     Console.Error.WriteLine(indent + e.Message + " at line " + e.Line + "/" + e.LinePosition + " (" + e.Position + ")" + " " + e.MaxPosition);
      //     indent += "  ";
      //     e = e.InnerException as ParseException;
      //   }
      // }
      // Console.Error.WriteLine();

      Console.Error.WriteLine("Parsing failed, best effort parser error:");
      // find best top-level exception, the one that parsed the farest
      ParseException best =
        parser.Errors.OrderByDescending(x => x.MaxPosition).First();
      // recurse down the exception tree down to the lowest detail
      Console.Error.WriteLine(
        best.Message +
        " at line " + (best.Line + 1) + "/" + (best.LinePosition + 1)
      );
      var indent = "";
      while(best.InnerException != null) {
        indent += "  ";
        best = best.InnerException as ParseException;
        Console.Error.WriteLine(indent +
          best.Message +
          " at line " + (best.Line + 1) + "/" + (best.LinePosition + 1)
        );
      }
      // dump relevant part of source
      var lineIndex    = best.Line;
      var line         = parser.Source[lineIndex];
      var trimmedLine  = line.TrimStart();
      var trimmed      = line.Length - trimmedLine.Length;
      var linePosition = best.LinePosition - trimmed;

      if(linePosition >= trimmedLine.Length) {
        lineIndex++;
        line = parser.Source[lineIndex];
        trimmedLine  = line.TrimStart();
        linePosition = 0;
      }

      Console.Error.WriteLine((lineIndex + 1) + " : " + trimmedLine);
      Console.Error.WriteLine(
        new System.String(
          '\u2500', 3 + (lineIndex + 1).ToString().Length + linePosition
        ) + "\u256f"
      );
    }
  }
}
