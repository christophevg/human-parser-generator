// Given a Parser Model, the Emitter generates CSharp code
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace Emitter {
  
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
";
    }

    private string GenerateEntities() {
      return string.Join( "\n\n",
        this.Model.Entities.Values.Select(x => this.GenerateEntity(x))
      );
    }

    private string GenerateEntity(Parser.Entity entity) {
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

    private string GenerateSignature(Parser.Entity entity) {
      return "public class " + this.UCase(entity.Name) + " {";
    }

    private string GenerateProperties(Parser.Entity entity) {
      return string.Join("\n",
        entity.Properties.Values.Select(x => this.GenerateProperty(x))
      );
    }

    private string GenerateProperty(Parser.Property property) {
      return "  public " + this.GenerateType(property) + " " + 
        this.UCase(property.Name) + " { get; set; }";
    }

    private string GenerateType(Parser.Property property) {
      if(property.IsPlural) {
        return "List<" + this.UCase(property.Type) + ">";
      }
      return property.Type.Equals("string") ?
        property.Type : this.UCase(property.Type);
    }

    private string GenerateConstructor(Parser.Entity entity) {
      if( ! entity.HasPluralProperty() ) { return null; }
      return "  public " + this.UCase(entity.Name) + "() {\n" +
        string.Join("\n",
          entity.Properties.Values.Where(x => x.IsPlural).Select(x => 
            "    this." + this.UCase(x.Name) + " = new " + 
              this.GenerateType(x) + "();\n"
          )
        ) +
        "  }";
    }

    private string GenerateToString(Parser.Entity entity) {
      return "  public override string ToString() {\n" +
        "    return\n" +
        "      \"" + this.UCase(entity.Name) + "(\" +\n" + 
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
          this.UCase(property.Name)
        );
      } else {
        return string.Format(
          "        \"{0}=\" + this.{0}",
          this.UCase(property.Name)
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
                      "  public static Regex " + this.UCase(extraction.Name) +
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
  public " + this.UCase(this.Model.Root.Name) + @" AST { get; set; }

  public Parser Parse(string source) {
    this.source = new Parsable(source);
    this.AST    = this.Parse" + this.UCase(this.Model.Root.Name) + @"();
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
            entity.Actions.Select(action => this.GenerateAction(action))
          ),
          this.GenerateEntityParserFooter(entity)
        }
      );
    }

    private string GenerateEntityParserHeader(Parser.Entity entity) {
      return "  public " + this.UCase(entity.Name) + 
        " Parse" + this.UCase(entity.Name) + "() {\n" +
        string.Join("\n",
          entity.Properties.Values.Select(x =>
            "    " + this.GenerateType(x) + " " + x.Name.ToLower() + 
            ( x.IsPlural ? " = new " + this.GenerateType(x) + "()" : "") +
            ";"
          )
        ) + "\n\n" +
        "    int pos = this.source.position;\n" +
        "    try {";
    }

    private string GenerateAction(Parser.Action action) {
      if(action is Parser.ConsumeLiteral) {
        return "      this.source.Consume(\"" + ((Parser.ConsumeLiteral)action).Literal + "\");";
      }
      if(action is Parser.ConsumeExtraction) {
        var extraction = (Parser.ConsumeExtraction)action;
        var id         = extraction.Prop.Name;
        var extractor  = extraction.Extr.Name;
        return "      " + this.GenerateConsumeExtraction(id, extractor);
      }
      if(action is Parser.ConsumeEntity) {
        var consumption = (Parser.ConsumeEntity)action;
        var id          = consumption.Prop.Name;
        var entity      = consumption.Id;
        if(consumption.Prop.IsPlural) {
          return
            "      Assignment temp;\n" +
            "      while(true) {\n" +
            "        try {\n" +
            "          " + this.GenerateConsumeEntity("temp", entity) + "\n" +
            "        } catch(ParseException) {\n" +
            "          break;\n" +
            "        }\n" +
            "        assignments.Add(temp);\n" +
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
            indent + this.GenerateAction(option) + "\n" +
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
        this.UCase(extractor) + ");";
    }

    private string GenerateConsumeEntity(string id, string entity) {
      return id + " = this.Parse" + this.UCase(entity) + "();";
    }

    private string GenerateEntityParserFooter(Parser.Entity entity) {
      return
        "    } catch(ParseException e) {\n" +
        "        this.source.position = pos;\n" +
        "        throw this.source.GenerateParseException(\n" +
        "          \"Failed to parse " + this.UCase(entity.Name) + ".\", e\n" +
        "        );\n" +
        "    }\n\n" +
        "    return new " + this.UCase(entity.Name) + "() {\n" + 
        string.Join( ",\n",
          entity.Properties.Values.Select(x =>
            "      " + this.UCase(x.Name) + " = " + x.Name.ToLower()
          )
        ) + "\n" +
        "    };\n" +
        "  }";
    }

    private string GenerateParserFooter() {
      return "}";
    }

    private string UCase(string text) {
      return text.First().ToString().ToUpper() + text.Substring(1);
    }

  }
}
