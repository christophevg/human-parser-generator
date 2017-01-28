// Given a Parser Model, the Emitter generates CSharp code
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

using HumanParserGenerator;

namespace HumanParserGenerator.Emitter {
  
  public class CSharp {

    private Parser.Model Model;

    public CSharp Generate(Parser.Model model) {
      this.Model = model;
      return this;
    }

    public override string ToString() {
      if( this.Model == null ) { return "// no model generated"; }
      return string.Join("\n\n", 
        new List<string>() { 
          this.GenerateHeader(),
          this.GenerateReferences(),
          this.GenerateEntities(),
          this.GenerateExtracting(),
          this.GenerateParsers()
        }
      );
    }

    private string GenerateHeader() {
      return @"// parser.cs";
    }

    private string GenerateReferences() {
      return @"using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Diagnostics;
";
    }

    private string GenerateEntities() {
      return string.Join( "\n\n",
        this.Model.Entities.Values.Select(x => this.GenerateEntity(x))
      );
    }

    private string GenerateEntity(Parser.Entity entity) {
      return entity.Virtual ?
        this.GenerateVirtualEntity(entity) :
        this.GenerateRealEntity(entity);
    }

    private string GenerateRealEntity(Parser.Entity entity) {
      return string.Join( "\n",
        new List<string>() {
          this.GenerateSignature(entity),
          this.GenerateProperties(entity),
          this.GenerateConstructor(entity),
          this.GenerateToString(entity),
          this.GenerateFooter(entity)
        }.Where(x => x != null)
      );
    }

    private string GenerateVirtualEntity(Parser.Entity entity) {
      return
        this.GenerateSignature(entity) +
        this.GenerateFooter(entity);
    }

    private string GenerateSignature(Parser.Entity entity) {
      return "public class " + this.PascalCase(entity.Name) + 
        (entity.HasSuper ? " : " + this.PascalCase(entity.Super.Name) : "") +
        " {";
    }

    private string GenerateProperties(Parser.Entity entity) {
      return string.Join("\n",
        entity.Properties.Values.Select(x => this.GenerateProperty(x))
      );
    }

    private string GenerateProperty(Parser.Property property) {
      return "  public " + this.GenerateType(property) + " " + 
        this.PascalCase(property.Name) + " { get; set; }";
    }

    private string GenerateType(Parser.Property property) {
      if(property.IsPlural) {
        return "List<" + this.PascalCase(property.Type) + ">";
      }
      if( property.Type == null ) { return "Object"; }
      if( property.Type.Equals("string") ) { return "string"; }
      return this.PascalCase(property.Type);
    }

    private string GenerateConstructor(Parser.Entity entity) {
      if( ! entity.HasPluralProperty() ) { return null; }
      return "  public " + this.PascalCase(entity.Name) + "() {\n" +
        string.Join("\n",
          entity.Properties.Values.Where(x => x.IsPlural).Select(x => 
            "    this." + this.PascalCase(x.Name) + " = new " + 
              this.GenerateType(x) + "();\n"
          )
        ) +
        "  }";
    }

    private string GenerateToString(Parser.Entity entity) {
      return "  public override string ToString() {\n" +
        "    return\n" +
        "      \"" + this.PascalCase(entity.Name) + "(\" +\n" + 
        string.Join(" + \",\" +\n",
          entity.Properties.Values.Select(x => this.GenerateToString(x))
        ) + " + \n" +
        "      \")\";\n" +
        "  }";
    }

    private string GenerateToString(Parser.Property property) {
      if(property.IsPlural) {
        return string.Format(
          "        \"{0}=\" + \"[\" + string.Join(\",\",\n" +
          "          this.{0}.Select(x => x.ToString())\n" +
          "        ) + \"]\"",
          this.PascalCase(property.Name)
        );
      } else {
        return string.Format(
          "        \"{0}=\" + this.{0}",
          this.PascalCase(property.Name)
        );
      }
    }

    private string GenerateFooter(Parser.Entity entity) {
      return "}";
    }

    private string GenerateExtracting() {
      return
        "public class Extracting {\n" +
        string.Join("\n",
          this.Model.Extractions.Values
                    .Select(extraction =>
                      "  public static Regex " + this.PascalCase(extraction.Name) +
                        " = new Regex(\"^" + extraction.Pattern + "\");"
                    )
        ) + "\n" +
        "}";
    }

    private string GenerateParsers() {
      return string.Join( "\n\n",
        this.GenerateParserHeader(),
        this.GenerateEntityParsers(),
        this.GenerateParserFooter()
      );
    }

    private string GenerateParserHeader() {
      return @"public class Parser {
  private Parsable source;
  public " + this.PascalCase(this.Model.Root) + @" AST { get; set; }

  public Parser Parse(string source) {
    this.source = new Parsable(source);
    this.AST    = this.Parse" + this.PascalCase(this.Model.Root) + @"();
    return this;
  }";
    }
  
    private string GenerateEntityParsers() {
      return string.Join("\n\n",
        this.Model.Entities.Values.Select(x => this.GenerateEntityParser(x))
      );
    }

    private string GenerateEntityParser(Parser.Entity entity) {
      return string.Join("\n\n",
        new List<string>() {
          this.GenerateEntityParserHeader(entity),
          string.Join("\n\n",
            entity.ParseActions.Select(action => this.GenerateParseAction(action))
          ),
          this.GenerateEntityParserFooter(entity)
        }
      );
    }
    
    // TODO QnD mapping of reserved words
    private string GenerateLocalVariable(string name) {
      if( name.Equals("string") ) {
        return "str";
      }
      return this.CamelCase(name);
    }

    private string GenerateEntityParserHeader(Parser.Entity entity) {
      return "  public " + this.PascalCase(entity.Name) + 
        " Parse" + this.PascalCase(entity.Name) + "() {\n" +
        string.Join("\n",
          entity.Properties.Values.Select(x =>
            "    " + this.GenerateType(x) + " " + 
              this.GenerateLocalVariable(x.Name.ToLower()) + 
            ( x.IsPlural ? " = new " + this.GenerateType(x) + "()" : "") +
            ";"
          )
        ) + "\n\n" +
        "    this.Log(\"Parse" + this.PascalCase(entity.Name) + "\");\n" +
        "    int pos = this.source.position;\n" +
        "    try {";
    }

    private string GenerateParseAction(Parser.ParseAction action) {
      if(action is Parser.ConsumeLiteral) {
        return "      this.source.Consume(\"" + ((Parser.ConsumeLiteral)action).Literal + "\");";
      }
      if(action is Parser.ConsumeExtraction) {
        var extraction = (Parser.ConsumeExtraction)action;
        var id         = this.GenerateLocalVariable(extraction.Prop.Name);
        var extractor  = extraction.Extr.Name;
        return "      " + this.GenerateConsumeExtraction(id, extractor);
      }
      if(action is Parser.ConsumeEntity) {
        var consumption = (Parser.ConsumeEntity)action;
        var id          = this.GenerateLocalVariable(consumption.Prop.Name);
        var entity      = consumption.Ent;
        if(consumption.Prop.IsPlural) {
          return
            "      " + this.PascalCase(entity.Type) + " temp;\n" +
            "      while(true) {\n" +
            "        try {\n" +
            "          " + this.GenerateConsumeEntity("temp", entity) + "\n" +
            "        } catch(ParseException) {\n" +
            "          break;\n" +
            "        }\n" +
            "        " + this.GenerateLocalVariable(consumption.Prop.Name) + ".Add(temp);\n" +
            "      }";
        } else {
          return "      " + this.GenerateConsumeEntity(id, entity);
        }
      }
      if(action is Parser.ConsumeAny)  {
        var consumption = (Parser.ConsumeAny)action;
        var id          = consumption.Prop.Name;
        var code        = "";
        var indent      = "      ";

        foreach(var option in consumption.Options) {
          code +=
            indent + "try {\n" +
            indent + this.GenerateParseAction(option) + "\n" +
            indent + "} catch(ParseException) {\n";
          indent += "  ";
        }
        code +=
          indent + "throw this.source.GenerateParseException(\n" +
          indent + "  \"Expected: " + consumption.Label + "\"\n" +
          indent + ");\n";

        foreach(var option in consumption.Options) {
          indent = indent.Substring(2);
          code += indent + "}\n";
        }
        
        return code;
      }

      throw new NotImplementedException();
    }

    private string GenerateConsumeExtraction(string id, string extractor) {
      return id + " = this.source.Consume(Extracting." + 
        this.PascalCase(extractor) + ");";
    }

    private string GenerateConsumeEntity(string id, Parser.Entity entity) {
      return id + " = this.Parse" + this.PascalCase(entity.Name) + "();";
    }

    private string GenerateEntityParserFooter(Parser.Entity entity) {
      return
        "    } catch(ParseException e) {\n" +
        "        this.source.position = pos;\n" +
        "        throw this.source.GenerateParseException(\n" +
        "          \"Failed to parse " + this.PascalCase(entity.Name) + ".\", e\n" +
        "        );\n" +
        "    }\n\n" +
        this.GenerateEntityParserReturn(entity);
    }

    private string GenerateEntityParserReturn(Parser.Entity entity) {
      return entity.Virtual ?
        this.GenerateVirtualEntityParserReturn(entity) :
        this.GenerateRealEntityParserReturn(entity);
    }
    
    private string GenerateRealEntityParserReturn(Parser.Entity entity) {
      return "    return new " + this.PascalCase(entity.Name) + "() {\n" + 
        string.Join( ",\n",
          entity.Properties.Values.Select(x =>
            "      " + this.PascalCase(x.Name) + " = " + 
              this.GenerateLocalVariable(x.Name.ToLower())
          )
        ) + "\n" +
        "    };\n" +
        "  }";
    }

    private string GenerateVirtualEntityParserReturn(Parser.Entity entity) {
      string name = entity.Properties.Keys.ToList()[0];
      
      return "    return " + this.CamelCase(name) + ";\n" +
             "  }"; 
    }

    private string GenerateParserFooter() {
      return @"
  [ConditionalAttribute(""DEBUG"")]
  private void Log(string msg) {
    Console.WriteLine(""!!! "" + msg + "" @ "" + this.source.Peek(10).Replace('\n', 'n'));
  }
}";
    }

    // this function makes sure that text is correctly case'd ;-)
    // Dashes are removed and the first letter of each part is uppercased
    private string PascalCase(string text) {
      return string.Join("",
        text.Split('-').Select(x =>
          x.First().ToString().ToUpper() + x.ToLower().Substring(1)
        )
      );
    }

    private string CamelCase(string text) {
      var x = this.PascalCase(text);
      return x.First().ToString().ToLower() + x.Substring(1);
    }

  }
}
