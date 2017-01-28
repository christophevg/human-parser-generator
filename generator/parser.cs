// Parser Model Generator - transforms the Grammar-AST into a Parser-AST
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

using HumanParserGenerator.Grammars;

namespace HumanParserGenerator.Parser {

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
    public string Name { get; set; }    // unique identification
    private string _type;
    public string Type {
      get {
        if(this.Parent.IsVirtual) {
          return this.Parent.Type;
        }
        return this._type;
      }
      set {
        this._type = value;
      }
    }
    public bool   IsPlural { get; set; }
    public Entity Parent { get; set; }
    public override string ToString() {
      return "Property(" +
        "Name=" + this.Name + "," + 
        "Type=" + this.Type + "," + 
        "IsPlural=" + this.IsPlural +
      ")";
    }
  }

  // A ParseAction parses into a Property
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
    public Entity Ent { get; set; }
    public override string Label { get { return this.Ent.Name; } }
    public override string ToString() {
      return "Consume(" + this.Prop.Name + ",Entity.Name=" + this.Ent.Name + ")";
    }
  }

  // given a set of possible ParseActions, this tries each of these ParseActions
  // and parses the first that matches in the Property
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

  // TODO improve name (what is true semantic meaning?)
  public abstract class Referable {
    public string Name { get; set; }
    public abstract string Type { get; }
  }

  public class Entity : Referable {
    public bool IsVirtual {
      get {
        // implicit:
        // if an entity's RULE's EXPRESSION IS A AlternativeExpressions, it is 
        // VIRTUAL, which means it can be referred to, but doesn't show up in 
        // the AST.
        return Rule.Exp is AlternativesExpression;     
      }
    }
    // By default an entity doens't have a super; all Entities are top-level.
    // If this Entity is Part of a Rule of a IsVirtual Entity, it has a 
    // SuperEntity.
    public bool HasSuper { get { return this.Super != null; } }
    private Entity super;
    public Entity Super { 
      get {
        return this.super;
      }
      set {
        if( this.super != null && this.super != value ) {
          throw new ArgumentException("can't assign more than one super. previous super was: " + this.super.ToString() + " // new super is: " + value.ToString());
        }
        this.super = value;
      }
    }

    public Rule Rule { get; set; }
    public override string Type { get { return this.Name; } }
    // name -> entity information
    public Dictionary<string,Property> Properties;
    // ordered list of parsing actions to fill properties
    public List<ParseAction> ParseActions;

    // To store propertyNames with the last given index
    private Dictionary<string, int> propertyIndices;

    public Entity() {
      this.Properties    = new Dictionary<string,Property>();
      this.ParseActions  = new List<ParseAction>();
      
      this.propertyIndices = new Dictionary<string, int>();
    }

    public void Add(Property property) {
      // set the Parent reference to point to us
      property.Parent = this;
      // make sure the name of the property is unique
      if( ! this.propertyIndices.Keys.Contains(property.Name) ) {
        this.propertyIndices.Add(property.Name, 0);
        // for first one, just use it's name
      } else {
        // this is (at least) the second occurence, start using indices
        if(this.propertyIndices[property.Name] == 0) {
          // update the first property to match the naming scheme
          Property firstProperty = this.Properties[property.Name]; // get
          this.Properties.Remove(property.Name);                   // remove
          firstProperty.Name += "0";                               // update
          this.Properties.Add(firstProperty.Name, firstProperty);  // re-add
        }
        this.propertyIndices[property.Name]++;
        property.Name += this.propertyIndices[property.Name].ToString();
      }
      this.Properties.Add(property.Name, property);
    }

    public void Add(ParseAction parseAction) {
      this.ParseActions.Add(parseAction);
    }

    public override string ToString() {
      return
        (this.IsVirtual ? "IsVirtual": "") + "Entity(" +
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

  public class Extraction : Referable {
    public override string Type { get { return "string"; } }
    public string Pattern { get; set; }
    public override string ToString() {
      return"Extraction(" + 
        "Name=" + this.Name + "," +
        "Pattern" + this.Pattern +
      ")";
    }
  }

  public class Model {
    public Dictionary<string,Rule> Rules;

    public Dictionary<string,Entity>       Entities;
    public Dictionary<string,Extraction>   Extractions;

    public string Root;

    public Model Import(Grammar grammar) {
      this.ImportRules(grammar);
      this.ExtractExtractions();
      this.ExtractEntities();
      this.ExtractPropertiesAndActions();
      return this;
    }

    private void ImportRules(Grammar grammar) {
      if(grammar.Rules.Count < 1) {
        throw new ArgumentException("grammar contains no rules");
      }
      this.Rules = new Dictionary<string,Rule>();
      foreach(var rule in grammar.Rules) {
        this.Rules.Add(rule.Id, rule);
      }
      this.Root = grammar.Rules[0].Id;
    }

    private void ExtractEntities() {
      this.Entities = this.Rules.Values
        .Where(rule => !( rule.Exp is Extractor) )
        .Select(rule => new Entity() {
          Name    = rule.Id,
          Rule    = rule
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
        .Where(rule => rule.Exp is Extractor)
        .Select(rule => new Extraction() {
          Name    = rule.Id,
          Pattern = ((Extractor)rule.Exp).Pattern
        })
       .ToDictionary(
          extraction => extraction.Name,
          extraction => extraction
        );
    }

    // Properties and ParseActions Extraction methods

    private void ExtractPropertiesAndParseActions(Expression exp, Entity entity) {
      try {
        new Dictionary<string, Action<Expression,Entity>>() {
          { "IdentifierExpression",   this.ExtractIdentifierExpression   },
          { "StringExpression",       this.ExtractStringExpression       },
          { "Extractor",              this.ExtractExtractorExpression    },
          { "OptionalExpression",     this.ExtractOptionalExpression     },
          { "RepetitionExpression",   this.ExtractRepetitionExpression   },
          { "GroupExpression",        this.ExtractGroupExpression        },
          { "AlternativesExpression", this.ExtractAlternativesExpression },
          { "SequenceExpression",     this.ExtractSequenceExpression     }
        }[exp.GetType().ToString().Replace("HumanParserGenerator.Grammars.", "")]
          (exp, entity);
      } catch(KeyNotFoundException e) {
        throw new NotImplementedException(
          "extracting not implemented for " + exp.GetType().ToString(), e
        );
      }
    }

    // an IDExp part of an Entity rule Expression requires the creation of a
    // Property to store the Referred Entity or Extraction.
    private void ExtractIdentifierExpression(Expression exp, Entity entity) {
      IdentifierExpression id = (IdentifierExpression)exp;

      Property property = this.CreatePropertyFor(id);
      entity.Add(property);

      ParseAction consumer = this.CreateConsumerFor(property, id);
      entity.Add(consumer);
    }

    private void ExtractStringExpression(Expression exp, Entity entity) {
      // this only requires an action
      entity.ParseActions.Add(new ConsumeLiteral() {
        Literal = ((StringExpression)exp).String
      });
    }
    
    private void ExtractExtractorExpression(Expression exp, Entity entity) {
      // this will be an Extractor
      // nothing TODO ?
    }

    private void ExtractOptionalExpression(Expression exp, Entity entity) {
      throw new NotImplementedException("TODO: ExtractGroupExpression");
    }
    
    private void ExtractRepetitionExpression(Expression exp, Entity entity) {
      var repetition = (RepetitionExpression)exp;
      if( repetition.Exp is IdentifierExpression) {
        string id   = ((IdentifierExpression)repetition.Exp).Id;
        Property property = new Property() {
          Name = id + "s",
          Type = this.IsTerminal(id) ? "string" : id,
          IsPlural = true
        };
        entity.Add(property);
        entity.ParseActions.Add(new ConsumeEntity() {
          Ent  = this.Entities[id],
          Prop = property
        });
      } else {
        throw new NotImplementedException();          
      }
    }

    private void ExtractGroupExpression(Expression exp, Entity entity) {
      throw new NotImplementedException("TODO: ExtractGroupExpression");
    }
    
    private Referable GetReferred(string name) {
      if( this.IsEntityName(name) ) {
        return this.Entities[name];
      } else if( this.IsExtractionName(name) ) {
        return this.Extractions[name];
      }
      throw new ArgumentException(
        name + " doesn't refer to known Entity or Extraction."
      );
    }

    private Property CreatePropertyFor(IdentifierExpression exp) {
      return new Property() {
        Name = exp.Id,
        Type = this.GetReferred(exp.Id).Type,
        IsPlural = false
      };
    }

    private ParseAction CreateConsumerFor(Property property,
                                          IdentifierExpression exp)
    {
      Referable referred = this.GetReferred(exp.Id);
      if( referred is Entity ) {
        return new ConsumeEntity() {
          Prop = property,
          Ent  = (Entity)referred
        };
      } else if( referred is Extraction ) {
        return new ConsumeExtraction() {
          Prop = property,
          Extr = (Extraction)referred
        };
      }
      throw new ArgumentException(
        "IdentifierExpression doesn't refer to known Entity or Extraction."
      );
    }

    private bool IsEntityName(string name) {
      return this.Entities.Keys.Contains(name);
    }

    private bool IsExtractionName(string name) {
      return this.Extractions.Keys.Contains(name);
    }

    private void ExtractAlternativesExpression(Expression exp,
                                               Entity entity)
    {
      // Alternative expressions should all return the same type.
      // We create a property on the Entity to store the result.
      // The Type of this property must be a superclass for all alternatives.
      Property property = new Property() {
        Name     = "value",
        Type     = null, // default, cause we don't know, other "rules" fix this
        IsPlural = false
      };
      entity.Add(property);

      ConsumeAny consume = new ConsumeAny();
      foreach(var alt in ((AlternativesExpression)exp).Expressions) {
        if( alt is IdentifierExpression ) {
          consume.Options.Add(this.CreateConsumerFor(
            property, (IdentifierExpression)alt)
          );

          // TODO: is the best place?
          // if this entity is viertual, make sure that the alternatives have 
          // this entity as Super
          if(entity.IsVirtual) {
            this.Entities[((IdentifierExpression)alt).Id].Super = entity;
          }
        } else {
          throw new NotImplementedException(
            "alternative is " + alt.GetType().ToString()
          );
        }
      }
      entity.Add(consume);
    }

    // a sequence consists of one or more Expressions that all are consumed
    // into properties of the entity
    private void ExtractSequenceExpression(Expression exp, Entity entity) {
      foreach(var subExp in ((SequenceExpression)exp).Expressions) {
        this.ExtractPropertiesAndParseActions(subExp, entity);
      }
    }

    private bool IsTerminal(string name) {
      return this.Rules.Keys.Contains(name)
          && this.Rules[name].Exp is Extractor;
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
