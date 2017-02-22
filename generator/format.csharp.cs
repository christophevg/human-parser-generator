// code formatting support
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using System.Linq;

using HumanParserGenerator.Generator;

namespace HumanParserGenerator.Emitter.Format {

  public class CSharp {
    
    public static string Class(Entity entity) {
      return PascalCase(entity.Name);
    }

    public static string Type(Entity entity) {
      return Type(entity.Type);
    }

    public static string Type(Property property) {
      if(property.IsPlural || property.Source.HasPluralParent) {
        return "List<" + Type(property.Type) + ">";
      }
      return Type(property.Type);
    }

    // wrapping formatting functions
    // use these to correctly format variables in their functional context
    
    // function to make sure that Properties don't have the same name as their
    // Class.
    // this is most of the time due to some recursion in a rule
    // e.g. rule ::= something [ rule ]
    public static string Property(Property property) {
      if(property.Name.Equals(property.Entity.Name)) {
        Warn("rewriting property name: " + property.Name);
        return PascalCase("next-" + property.Name);
      }
      return PascalCase(property.Name + PluralSuffix(property));
    }

    // TODO refactor: there is too much logic in here
    public static string EntityProperty(Property property) {
      return Variable(property.Entity) + "." + Property(property);
    }

    public static string Variable(Property property) {
      string name = property.Name;
      // QnD solution to reserved words
      if( name.Equals("string") ) { return "text";     }
      if( name.Equals("int")    ) { return "number";   }
      if( name.Equals("float")  ) { return "floating"; }
      if( name.Equals("null")   ) { return "nul";      }
      return CamelCase( name + PluralSuffix(property) );
    }

    public static string Variable(Entity entity) {
      string name = entity.Name;
      // QnD solution to reserved words
      if( name.Equals("string") ) { return "text";     }
      if( name.Equals("int")    ) { return "number";   }
      if( name.Equals("float")  ) { return "floating"; }
      if( name.Equals("null")   ) { return "nul";      }
      return CamelCase( name );
    }

    public static string VerbatimStringLiteral(string text) {
      return "@\"" + text.Replace( "\"", "\"\"" ) + "\"";
    }
    
    // technical formatting functions
    // don't use these in emitter code, only use them from functional formatting
    // wrappers

    private static string Type(string type) {
      if( type == null )            { return "Object"; }
      if( type.Equals("<string>") ) { return "string"; }
      if( type.Equals("<bool>") )   { return "bool"; }

      return PascalCase(type);
    }

    // this function makes sure that text is correctly case'd ;-)
    // Dashes are removed and the first letter of each part is uppercased
    private static string PascalCase(string text) {
      return string.Join("",
        text.Split('-').Select(x =>
          x.First().ToString().ToUpper() + x.ToLower().Substring(1)
        )
      );
    }

    private static string CamelCase(string text) {
      var x = PascalCase(text);
      return x.First().ToString().ToLower() + x.Substring(1);
    }

    private static string PluralSuffix(Property property) {
      if(! property.IsPlural && ! property.Source.HasPluralParent) { return "";    }
      if( property.Name.EndsWith("x") ) { return "es";  }
      return "s";
    }

    // logging functionality

    private static void Warn(string msg) {
      Log("warning: " + msg);
    }

    private static void Log(string msg) {
      Console.Error.WriteLine("hpg-emitter: " + msg);
    }    
  }

}