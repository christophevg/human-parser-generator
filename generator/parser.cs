// Parser Model Generator - transforms the Grammar-AST into a Parser-AST
// author: Christophe VG <contact@christophe.vg>

// transformation rules:
//   1. all extractor-(non)-terminals are recorded/indexed
//   2. for each non-terminal, a ParserEntity is constructed, holding all 
//      first-level properties and information about the actual parsing, the 
//      actions

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace Parser {

  // Parser Model classes
  // 
  // The Parser Model (PM) consists of
  // - Entities
  // - Extractions
  //
  // Entities have Properties and ParseParseActions
  // Extractions have a Name and Pattern to extract tokens
  //
  // Properties have a Name, Type and IsPlural indication
  // ParseParseActions are an ordered list of steps to parse into Properties

  public class Property {
    public string Name { get; set; }
    public string Type { get; set; }
    public bool   IsPlural { get; set; }
    public override string ToString() {
      return "Property(" +
        "Name=" + this.Name + "," + 
        "Type=" + this.Type + "," + 
        "IsPlural=" + this.IsPlural +
      ")";
    }
  }

  public abstract class ParseAction {
    public virtual Property Prop { get; set; }
    public abstract string Label { get; }
  }

  public class ConsumeLiteral : ParseAction {
    public string Literal { get; set; }
    public override string Label { get { return this.Literal; } }
    public override string ToString() {
      return "Consume(" + this.Literal + ")";
    }
  }
  
  public class ConsumeExtraction : ParseAction {
    public Extraction Extr { get; set; }
    public override string Label { get { return this.Extr.Name; } }
    public override string ToString() {
      return "Consume(" + this.Prop.Name + ",Extractor.Id=" + this.Extr.Name + ")";
    }    
  }

  public class ConsumeEntity : ParseAction {
    public string Id { get; set; }
    public override string Label { get { return this.Id; } }
    public override string ToString() {
      return "Consume(" + this.Prop.Name + ",Entity.Id=" + this.Id + ")";
    }
  }

  public class ConsumeAny : ParseAction {
    public List<ParseAction> Options;
    public override string Label {
      get {
        return string.Join( " | ", this.Options.Select(x => x.Label ));
      }
    }
    public override Property Prop {
      get {
        if(this.Options.Count > 0) {
          return this.Options[0].Prop;
        } else {
          return null;
        }
      }
      set {
        foreach(var option in this.Options) {
          option.Prop = value;
        }
      }
    }
    public ConsumeAny() {
      this.Options = new List<ParseAction>();
    }
    public override string ToString() {
      return "Consume([" + 
        string.Join("|", this.Options.Select(x => x.ToString())) +
      "]);";
    }
  }

  public class Entity {
    public Grammar.Rule Rule { get; set; }
    public string Name { get; set; }
    // name -> entity information
    public Dictionary<string,Property> Properties;
    // ordered list of parsing actions to fill properties
    public List<ParseAction> ParseActions;

    public Entity() {
      this.Properties = new Dictionary<string,Property>();
      this.ParseActions    = new List<ParseAction>();
    }
    public override string ToString() {
      return
        "Entity(" +
          "Name=" + this.Name + "," +
          "Properties=" + "[" +
            string.Join(",",
              this.Properties.Select(x => x.Key + "=" + x.Value.ToString())
            ) +
          "]" + "," +
          "ParseActions=" + "[" +
            string.Join(",",
              this.ParseActions.Select(x => x.ToString())
            ) +
          "]" +
        ")";
    }
    public bool HasPluralProperty() {
      foreach(var property in this.Properties.Values) {
        if(property.IsPlural) { return true; }
      }
      return false;
    }
  }

  public class Extraction {
    public string Name { get; set; }
    public string Pattern { get; set; }
    public override string ToString() {
      return"Extraction(" + 
        "Name=" + this.Name + "," +
        "Pattern" + this.Pattern +
      ")";
    }
  }

  public class Model {
    public Dictionary<string,Grammar.Rule> Rules;

    public Dictionary<string,Entity>       Entities;
    public Dictionary<string,Extraction>   Extractions;

    public string Root;

    public Model Import(Grammar.Model grammar) {
      this.ImportRules(grammar);
      this.ExtractExtractions();
      this.ExtractEntities();
      this.ExtractPropertiesAndActions();
      return this;
    }

    private void ImportRules(Grammar.Model grammar) {
      if(grammar.Rules.Count < 1) {
        throw new ArgumentException("grammar contains no rules");
      }
      this.Rules = new Dictionary<string,Grammar.Rule>();
      foreach(var rule in grammar.Rules) {
        this.Rules.Add(rule.Id, rule);
      }
      this.Root = grammar.Rules[0].Id;
    }

    private void ExtractEntities() {
      this.Entities = this.Rules.Values
        .Where(rule => !( rule.Exp is Grammar.Extractor) )
        .Select(rule => new Entity() {
          Name = rule.Id,
          Rule = rule
        })
        .ToDictionary(
          entity => entity.Name,
          entity => entity
        );
    }

    private void ExtractPropertiesAndActions() {
      foreach(KeyValuePair<string, Entity> entity in this.Entities) {
        this.ExtractPropertiesAndParseActions(
          entity.Value.Rule.Exp, entity.Value
        );
      }
    }
    
    private void ExtractExtractions() {
      this.Extractions = this.Rules.Values
        .Where(rule => rule.Exp is Grammar.Extractor)
        .Select(rule => new Extraction() {
          Name    = rule.Id,
          Pattern = ((Grammar.Extractor)rule.Exp).Pattern
        })
       .ToDictionary(
          extraction => extraction.Name,
          extraction => extraction
        );
    }

    // Properties and ParseActions Extraction methods

    private void ExtractPropertiesAndParseActions(Grammar.Expression exp, Entity entity) {
      try {
        new Dictionary<string, Action<Grammar.Expression,Entity>>() {
          { "Grammar.IdentifierExpression",   this.ExtractIdentifierExpression   },
          { "Grammar.StringExpression",       this.ExtractStringExpression       },
          { "Grammar.Extractor",              this.ExtractExtractorExpression    },
          { "Grammar.OptionalExpression",     this.ExtractOptionalExpression     },
          { "Grammar.RepetitionExpression",   this.ExtractRepetitionExpression   },
          { "Grammar.GroupExpression",        this.ExtractGroupExpression        },
          { "Grammar.AlternativesExpression", this.ExtractAlternativesExpression },
          { "Grammar.SequenceExpression",     this.ExtractSequenceExpression     }
        }[exp.GetType().ToString()](exp, entity);
      } catch(KeyNotFoundException e) {
        throw new NotImplementedException(
          "extracting not implemented for " + exp.GetType().ToString(), e
        );
      }
    }

    private void ExtractIdentifierExpression(Grammar.Expression exp, Entity entity) {
      string id = ((Grammar.IdentifierExpression)exp).Id;
      if( this.IsTerminal(id) ) {
        // this is an extractor, the extraction is a "string"
        Property property = new Property() {
          Name = id,
          Type = "string",
          IsPlural = false
        };
        entity.Properties.Add(id, property);
        entity.ParseActions.Add(new ConsumeExtraction() {
          Prop = property,
          Extr = this.Extractions[id]
        });
      } else {
        // this is an entity, the identifier results in an other Entity
        Property property = new Property() {
          Name = id,
          Type = "Enity",
          IsPlural = false
        };
        entity.Properties.Add(id, property);
        entity.ParseActions.Add(new ConsumeEntity() {
          Id   = id,
          Prop = property
        });
      }
    }

    private void ExtractStringExpression(Grammar.Expression exp, Entity entity) {
      // this only requires an action
      entity.ParseActions.Add(new ConsumeLiteral() {
        Literal = ((Grammar.StringExpression)exp).String
      });
    }
    
    private void ExtractExtractorExpression(Grammar.Expression exp, Entity entity) {
      // this will be an Extractor
      // nothing TODO ?
    }

    private void ExtractOptionalExpression(Grammar.Expression exp, Entity entity) {
      throw new NotImplementedException("TODO: ExtractGroupExpression");
    }
    
    private void ExtractRepetitionExpression(Grammar.Expression exp, Entity entity) {
      var repetition = (Grammar.RepetitionExpression)exp;
      if( repetition.Exp is Grammar.IdentifierExpression) {
        string id   = ((Grammar.IdentifierExpression)repetition.Exp).Id;
        Property property = new Property() {
          Name = id + "s",
          Type = this.IsTerminal(id) ? "string" : id,
          IsPlural = true
        };
        entity.Properties.Add(id, property);
        entity.ParseActions.Add(new ConsumeEntity() {
          Id   = id,
          Prop = property
        });
      } else {
        throw new NotImplementedException();          
      }
    }

    private void ExtractGroupExpression(Grammar.Expression exp, Entity entity) {
      throw new NotImplementedException("TODO: ExtractGroupExpression");
    }

    private void ExtractAlternativesExpression(Grammar.Expression exp, Entity entity) {
      // TODO: expand to "value" + number
      Property property = new Property() {
        Name     = "value",
        Type     = "string",
        IsPlural = false
      };
      ConsumeAny consume = new ConsumeAny();
      foreach(var alt in ((Grammar.AlternativesExpression)exp).Expressions) {
        if( alt is Grammar.IdentifierExpression ) {
          consume.Options.Add(new ConsumeExtraction() {
            Prop = property,
            Extr = this.Extractions[((Grammar.IdentifierExpression)alt).Id]
          });
        } else {
          throw new NotImplementedException(
            "alternative is " + alt.GetType().ToString()
          );
        }
      }
      entity.Properties.Add(property.Name, property);
      entity.ParseActions.Add(consume);
    }

    private void ExtractSequenceExpression(Grammar.Expression exp, Entity entity) {
      foreach(var subExp in ((Grammar.SequenceExpression)exp).Expressions) {
        this.ExtractPropertiesAndParseActions(subExp, entity);
      }
    }

    private bool IsTerminal(string name) {
      return this.Rules.Keys.Contains(name)
          && this.Rules[name].Exp is Grammar.Extractor;
    }
    
    public override string ToString() {
      return
        "grammar rules\n-------------\n" +
        string.Join( "\n",
          this.Rules.Select(x => " * " + x.Key + "=" + x.Value.ToString())
        ) + "\n" +
        "entities\n--------\n" +
        string.Join( "\n",
          this.Entities.Select(x => " * " + x.Key + "=" + x.Value.ToString())
        ) + "\n" +
        "extractions\n-----------\n" +
        string.Join( "\n",
          this.Extractions.Select(x => " * " + x.Key + "=" + x.Value.ToString())
        ) + "\n";
    }
  }
}
