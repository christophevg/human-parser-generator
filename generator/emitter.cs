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

    private Generator.Model Model;

    public CSharp Generate(Generator.Model model) {
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

    private string GenerateEntity(Generator.Entity entity) {
      return entity.IsVirtual ?
        this.GenerateVirtualEntity(entity) :
        this.GenerateRealEntity(entity);
    }

    private string GenerateRealEntity(Generator.Entity entity) {
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

    private string GenerateVirtualEntity(Generator.Entity entity) {
      return
        this.GenerateSignature(entity) +
        this.GenerateFooter(entity);
    }

    private string GenerateSignature(Generator.Entity entity) {
      return "public " +
        ( entity.IsVirtual ? "interface" : "class" ) + " " +
        this.PascalCase(entity.Name) + 
        (entity.HasSupers ?
          " : " + string.Join( ", ",
            entity.Supers.Select(x => this.PascalCase(x.Name))
          )
          : "") +
        " {";
    }

    private string GenerateProperties(Generator.Entity entity) {
      return string.Join("\n",
        entity.Properties.Values.Select(x => this.GenerateProperty(x))
      );
    }

    private string GenerateProperty(Generator.Property property) {
      return "  public " + this.GenerateType(property) + " " + 
        this.PascalCase(property.Name) + " { get; set; }";
    }

    private string GenerateType(Generator.Property property) {
      if(property.IsPlural) {
        return "List<" + this.PascalCase(property.Type) + ">";
      }
      if( property.Type == null ) { return "Object"; }
      if( property.Type.Equals("string") ) { return "string"; }
      return this.PascalCase(property.Type);
    }

    private string GenerateConstructor(Generator.Entity entity) {
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

    private string GenerateToString(Generator.Entity entity) {
      return "  public override string ToString() {\n" +
        "    return\n" +
        "      \"" + this.PascalCase(entity.Name) + "(\" +\n" + 
        string.Join(" + \",\" +\n",
          entity.Properties.Values.Select(x => this.GenerateToString(x))
        ) + " + \n" +
        "      \")\";\n" +
        "  }";
    }

    private string GenerateToString(Generator.Property property) {
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

    private string GenerateFooter(Generator.Entity entity) {
      return "}";
    }

    private string GenerateExtracting() {
      return
        "public class Extracting {\n" +
        string.Join("\n",
          this.Model.Extractions.Values
                    .Select(extraction =>
                      "  public static Regex " + this.PascalCase(extraction.Name) +
                        " = new Regex(@\"^" + extraction.Pattern.Replace("\"", "\"\"") + "\");"
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
  public " + this.PascalCase(this.Model.Root.Name) + @" AST { get; set; }

  public Parser Parse(string source) {
    this.source = new Parsable(source);
    this.AST    = this.Parse" + this.PascalCase(this.Model.Root.Name) + @"();
    return this;
  }";
    }
  
    private string GenerateEntityParsers() {
      return string.Join("\n\n",
        this.Model.Entities.Values.Select(x => this.GenerateEntityParser(x))
      );
    }

    private string GenerateEntityParser(Generator.Entity entity) {
      return string.Join("\n\n",
        new List<string>() {
          this.GenerateEntityParserHeader(entity),
          this.GenerateParseAction(entity.ParseAction),
          this.GenerateEntityParserFooter(entity)
        }
      );
    }
    
    // TODO QnD mapping of reserved words
    private string GenerateLocalVariable(string name) {
      // this would become string, which is reserved
      if( name.Equals("string") ) {
        return "str";
      }
      return this.CamelCase(name);
    }

    private string GenerateEntityParserHeader(Generator.Entity entity) {
      return "  public " + this.PascalCase(entity.Name) + 
        " Parse" + this.PascalCase(entity.Name) + "() {\n" +
        string.Join("\n",
          entity.Properties.Values.Select(x =>
            "    " + this.GenerateType(x) + " " + 
              this.GenerateLocalVariable(x.Name.ToLower()) + 
            ( x.IsPlural ? " = new " + this.GenerateType(x) + "()" : " = null") +
            ";"
          )
        ) + "\n\n" +
        "    this.Log(\"Parse" + this.PascalCase(entity.Name) + "\");\n" +
        "    int pos = this.source.position;\n" +
        "    try {";
    }

    private string GenerateParseAction(Generator.ParseAction action) {
      if(action is Generator.ConsumeLiteral) {
        return "      this.source.Consume(\"" + ((Generator.ConsumeLiteral)action).Literal + "\");";
      }
      if(action is Generator.ConsumeExtraction) {
        var extraction = (Generator.ConsumeExtraction)action;
        var id         = this.GenerateLocalVariable(extraction.Property.Name);
        var extractor  = extraction.Extraction.Name;
        return "      " + this.GenerateConsumeExtraction(id, extractor);
      }
      if(action is Generator.ConsumeEntity) {
        var consumption = (Generator.ConsumeEntity)action;
        var id          = this.GenerateLocalVariable(consumption.Property.Name);
        var entity      = consumption.Entity;
        if(consumption.Property.IsPlural) {
          return
            "      " + this.PascalCase(entity.Type) + " temp;\n" +
            "      while(true) {\n" +
            "        try {\n" +
            "          " + this.GenerateConsumeEntity("temp", entity) + "\n" +
            "        } catch(ParseException) {\n" +
            "          break;\n" +
            "        }\n" +
            "        " + this.GenerateLocalVariable(consumption.Property.Name) + ".Add(temp);\n" +
            "      }";
        } else {
          return "      " + this.GenerateConsumeEntity(id, entity);
        }
      }
      if(action is Generator.ConsumeAny)  {
        var consumption = (Generator.ConsumeAny)action;
        var id          = consumption.Property.Name;
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
      if(action is Generator.ConsumeAll) {
        return string.Join("\n\n",
          (((Generator.ConsumeAll)action).Actions).Select(a =>
            this.GenerateParseAction(a)
          )
        );
        
      }

      throw new NotImplementedException("ParseAction<" + action.GetType().ToString() + ">");
    }

    private string GenerateConsumeExtraction(string id, string extractor) {
      return id + " = this.source.Consume(Extracting." + 
        this.PascalCase(extractor) + ");";
    }

    private string GenerateConsumeEntity(string id, Generator.Entity entity) {
      return id + " = this.Parse" + this.PascalCase(entity.Name) + "();";
    }

    private string GenerateEntityParserFooter(Generator.Entity entity) {
      return
        "    } catch(ParseException e) {\n" +
        "        this.source.position = pos;\n" +
        "        throw this.source.GenerateParseException(\n" +
        "          \"Failed to parse " + this.PascalCase(entity.Name) + ".\", e\n" +
        "        );\n" +
        "    }\n\n" +
        this.GenerateEntityParserReturn(entity);
    }

    private string GenerateEntityParserReturn(Generator.Entity entity) {
      return entity.IsVirtual ?
        this.GenerateVirtualEntityParserReturn(entity) :
        this.GenerateRealEntityParserReturn(entity);
    }
    
    private string GenerateRealEntityParserReturn(Generator.Entity entity) {
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

    private string GenerateVirtualEntityParserReturn(Generator.Entity entity) {
      string name = entity.Properties.Keys.ToList()[0];
      
      return "    return " + this.CamelCase(name) + ";\n" +
             "  }"; 
    }

    private string GenerateParserFooter() {
      return @"
  [ConditionalAttribute(""DEBUG"")]
  private void Log(string msg) {
    Console.Error.WriteLine(""!!! "" + msg + "" @ "" + this.source.Peek(10).Replace('\n', 'n'));
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
