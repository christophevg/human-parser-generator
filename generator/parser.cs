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

  public abstract class Action {}

  public class ConsumeLiteral : Action {
    public string Literal { get; set; }
    public override string ToString() {
      return "Consume(" + this.Literal + ")";
    }
  }
  
  public abstract class ConsumeProperty : Action {
    public virtual Property Prop { get; set; }
    public abstract string Label { get; }
  }

  public class ConsumeExtraction : ConsumeProperty {
    public Extraction Extr { get; set; }
    public override string Label { get { return this.Extr.Name; } }
    public override string ToString() {
      return "Consume(" + this.Prop.Name + ",Extractor.Id=" + this.Extr.Name + ")";
    }    
  }

  public class ConsumeEntity : ConsumeProperty {
    public string Id { get; set; }
    public override string Label { get { return this.Id; } }
    public override string ToString() {
      return "Consume(" + this.Prop.Name + ",Entity.Id=" + this.Id + ")";
    }
  }

  public class ConsumeAny : ConsumeProperty {
    public List<ConsumeProperty> Options;
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
      this.Options = new List<ConsumeProperty>();
    }
    public override string ToString() {
      return "Consume([" + 
        string.Join("|", this.Options.Select(x => x.ToString())) +
      "]);";
    }
  }

  public class Entity {
    public string Name { get; set; }
    // name -> entity information
    public Dictionary<string,Property> Properties;
    // ordered list of parsing actions to fill properties
    public List<Action> Actions;
    public Entity() {
      this.Properties = new Dictionary<string,Property>();
      this.Actions    = new List<Action>();
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
          "Actions=" + "[" +
            string.Join(",",
              this.Actions.Select(x => x.ToString())
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
    public Dictionary<string,Grammar.Rule>       Rules;
    public Dictionary<string,Entity>     Entities;
    public Dictionary<string,Extraction> Extractions;
    public Entity Root;

    public Model Import(Grammar.Model grammar) {
      this.ImportRules(grammar);
      this.ExtractExtractions();
      this.ExtractEntities();
      return this;
    }

    private void ImportRules(Grammar.Model grammar) {
      this.Rules = new Dictionary<string,Grammar.Rule>();
      foreach(var rule in grammar.Rules) {
        this.Rules.Add(rule.Id, rule);
      }
    }

    private void ExtractEntities() {
      this.Entities = new Dictionary<string,Entity>();
      foreach(KeyValuePair<string, Grammar.Rule> rule in this.Rules) {
        Entity entity = new Entity() { Name = rule.Key };
        this.ExtractPropertiesAndActions(rule.Value.Exp, entity);
        if( entity.Properties.Count + entity.Actions.Count > 0) {
          this.Entities.Add(rule.Key, entity);
          if(this.Root == null) { this.Root = entity; }
        }
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
          extraction => extraction.Name.ToLower(),
          extraction => extraction
        );
    }
    
    // TODO: refactor
    // TODO: evaluate if polymorphism or Visitor pattern results in nicer 
    //       interface/code
    private void ExtractPropertiesAndActions(Grammar.Expression exp, Entity entity) {
      if( exp is Grammar.SequenceExpression) {
        // recurse
        foreach(var subExp in ((Grammar.SequenceExpression)exp).Expressions) {
          this.ExtractPropertiesAndActions(subExp, entity);
        }
      } else if(exp is Grammar.StringExpression){
        // this only requires an action
        entity.Actions.Add(new ConsumeLiteral() {
          Literal = ((Grammar.StringExpression)exp).String
        });
      } else if(exp is Grammar.Extractor){
        // this will be an Extracting entry
      } else if(exp is Grammar.IdentifierExpression) {
        string id = ((Grammar.IdentifierExpression)exp).Id;
        if( ! this.IsTerminal(id) ) { throw new NotImplementedException(); }
        Property property = new Property() {
          Name = id,
          Type = "string",
          IsPlural = false
        };
        entity.Properties.Add(id, property);
        entity.Actions.Add(new ConsumeExtraction() {
          Prop = property,
          Extr = this.Extractions[id]
        });
      } else if(exp is Grammar.RepetitionExpression) {
        var repetition = (Grammar.RepetitionExpression)exp;
        if( repetition.Exp is Grammar.IdentifierExpression) {
          string id   = ((Grammar.IdentifierExpression)repetition.Exp).Id;
          Property property = new Property() {
            Name = id + "s",
            Type = this.IsTerminal(id) ? "string" : id,
            IsPlural = true
          };
          entity.Properties.Add(id, property);
          entity.Actions.Add(new ConsumeEntity() {
            Prop = property,
            Id   = id
          });
        } else {
          throw new NotImplementedException();          
        }
      } else if(exp is Grammar.OrExpression) {
        // TODO: expand to "value" + number
        Property property = new Property() {
          Name     = "value",
          Type     = "string",
          IsPlural = false
        };
        ConsumeAny consume = new ConsumeAny();
        var o = (Grammar.OrExpression)exp;
        while(true) { // loop to recurse through Or(Id,[Id|Or(...)])
          if( o.Exp1 is Grammar.IdentifierExpression ) {
            consume.Options.Add(new ConsumeExtraction() {
              Prop = property,
              Extr = this.Extractions[((Grammar.IdentifierExpression)o.Exp1).Id]
            });
            if( o.Exp2 is Grammar.IdentifierExpression ) {
              consume.Options.Add(new ConsumeExtraction() {
                Prop = property,
                Extr = this.Extractions[((Grammar.IdentifierExpression)o.Exp2).Id]
              });              
              break; // stop recusion
            } else if( o.Exp2 is Grammar.OrExpression ) {
              // recurse
              o = (Grammar.OrExpression)o.Exp2;
            } else {
              throw new NotImplementedException();
            }
          } else {
            throw new NotImplementedException();
          }
        }
        entity.Properties.Add(property.Name, property);
        entity.Actions.Add(consume);
      } else {
        throw new NotImplementedException();
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
