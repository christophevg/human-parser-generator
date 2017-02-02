// Parser Model Generator: transforms the Grammar AST into a Parser AST
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;

using System.Collections.Generic;

using System.Text.RegularExpressions;

using System.Linq;

using System.Diagnostics;

namespace HumanParserGenerator.Generator {

  public class Entity {
    // the (original) Rule this Entity was constructed from
    public Rule Rule { get; set; }
    
    public string Name { get; set; }

    private Dictionary<string,Property> properties;
    public List<Property> Properties {
      get { return this.properties.Values.ToList(); }
      set {
        this.properties.Clear();
        foreach(var property in value) {
          this.Add(property);
        }
      }
    }

    // to populate the Properties, ParseActions have to be generated
    // ParseActions are a tree-structure with a single top-level ParseAction
    public ParseAction ParseAction { get; set; }

    // all ParseActions of type ConsumeEntity that refer to us
    private List<ConsumeEntity> referrers;
    public List<ConsumeEntity> Referrers {
      get {
        if( this.referrers == null ) {
          this.referrers = new List<ConsumeEntity>();
        }
        return this.referrers;
      }
      set { this.referrers = value; }
    }
    
    // an Entity that has a single Property is Virtual, since it can simply
    // pass on this Property.
    // TODO refine
    public bool IsVirtual {
      get {
        return this.Properties.Count < 2;
      }
    }

    // the Entity can be optional, if its top-level ParseAction is Optional
    public bool IsOptional { get { return this.ParseAction.IsOptional; } }

    // Entities can "be" (implement) other Virtual Entities it is referenced by 
    // from those Entities (only) Property.
    public bool HasSupers { get { return this.Supers.Count > 0; } }
    public List<Entity> Supers {
      get {
        return this.Referrers
          .Where (x => x.Property.Entity.IsVirtual)
          .Select(x => x.Property.Entity)
          .Cast<Entity>()
          .ToList();
      }
    }

    // be default, an Entity "is" its own Type
    // when an Entity is Virtual, its Type is that of its only Property
    public string Type {
      get {
        if( this.IsVirtual ) {
          return this.Properties.Count == 1 ? this.Properties[0].Type : null;
        }
        return this.Name;
      }
    }

    // helper dictionary to track property.Names with the last given index
    private Dictionary<string, int> propertyIndices;

    public Entity() {
      this.properties      = new Dictionary<string,Property>();
      this.propertyIndices = new Dictionary<string, int>();
    }

    public void Add(Property property) {
      // set the Entity reference to point to us (back-reference)
      property.Entity = this;

      // make sure the name of the property is unique
      if( ! this.propertyIndices.Keys.Contains(property.Name) ) {
        this.propertyIndices.Add(property.Name, 0);
        // for first one, just use it's name
      } else {
        // this is (at least) the second occurence, start using indices
        if(this.propertyIndices[property.Name] == 0) {
          // update the first property to match the naming scheme
          Property firstProperty = this.properties[property.Name]; // get
          this.properties.Remove(property.Name);                   // remove
          firstProperty.Name += "0";                               // update
          this.properties.Add(firstProperty.Name, firstProperty);  // re-add
        }
        this.propertyIndices[property.Name]++;
        property.Name += this.propertyIndices[property.Name].ToString();
      }
      this.properties.Add(property.Name, property);
    }

    public override string ToString() {
      return
        (this.IsVirtual ? "Virtual": "") + "Entity(" +
          "Name=" + this.Name + "," +
          "Type=" + this.Type + "," +
          "Supers=" + "[" +
            string.Join(",", this.Supers.Select(x => x.Name)) +
          "]," +
          "Referrers=" + "[" +
            string.Join(",", this.Referrers.Select(x => x.Property.Label)) +
          "]," +
          "Properties=" + "[" +
            string.Join(",", this.Properties.Select(x => x.ToString())) +
          "]" + "," +
          "ParseAction=" + this.ParseAction.ToString() +
        ")";
    }

    public bool HasPluralProperty() {
      return this.Properties.Where(x => x.IsPlural).ToList().Count > 0;
    }
  }

  public class Property {
    // a unique name to identify the property, used for variable emission
    public string Name { get; set; }

    // a (back-)reference to the Entity this property belongs to
    public Entity Entity { get; set; }

    // a property is populated by a ParseAction
    public ParsePropertyAction Source { get; set; }

    // the Type of a Property is defined by the ParseAction
    public string Type { get { return this.Source.Type; } }

    // a Property can me marked as Plural, meaning that it will contain a list
    // of Type parsing results, which depends on the ParseAction
    public bool IsPlural { get { return this.Source.IsPlural; } }

    // a Property can be Optional, which depends on the ParseAction
    public bool IsOptional { get { return this.Source.IsOptional; } }

    // a Label is a FQN for this Property
    public string Label { get { return this.Entity.Name + "." + this.Name; } }

    public override string ToString() {
      return "Property(" +
        "Name="       + this.Name       + "," + 
        "Type="       + this.Type       + "," + 
        "IsPlural="   + this.IsPlural   + "," +
        "IsOptional=" + this.IsOptional + "," +
        "Source="     + this.Source     +
      ")";
    }
  }

  public abstract class ParseAction {
    // the Parsing is optional
    public bool IsOptional { get; set; }

    // Label can be used for external string representation, other than ToString
    public abstract string Label { get; }

    public override string ToString() {
      return "Consume" + (this.IsOptional ? "Optional" : "")+ "(" +
        this.Label +
      ")";
    }
  }

  // just consume a Token
  public class ConsumeToken : ParseAction {
    public string Token { get; set; }
    public override string Label { get { return this.Token; } }
  }

  // just consume a Token, based on a pattern
  public class ConsumePatternToken : ConsumeToken {}

  // a ParseAction parses text into a Property
  public abstract class ParsePropertyAction : ParseAction {
    // Property that receives parsing result from this ParseAction
    private Property property;
    public Property Property {
      get { return this.property; }
      set {
        this.property = value;
        this.property.Source = this; // back-reference
      }
    }

    // Type indicates what type of result this ParseAction will provide to the
    // Property
    public abstract string Type  { get; }
    
    // the ParseAction returns a list of parsed property values
    public bool IsPlural { get; set; }

    public override string ToString() {
      return "Consume(" + this.Label + "->" + this.Property.Name + ")";
    }
  }

  // ... to consume a literal sequence of characters, aka a string ;-)
  public class ConsumeString : ParsePropertyAction {
    public          string String { get; set; }
    public override string Label  { get { return this.String; } }
    public override string Type   { get { return "string";     } }
  }

  // ... to consume a sequence of characters according to a regular expression
  public class ConsumePattern : ConsumeString {
    // alias for String
    public string Pattern {
      get { return this.String; }
      set { this.String = value; }
    }
  }

  // ... to consume an Entity
  public class ConsumeEntity : ParsePropertyAction {
    private Entity entity;
    public Entity Entity {
      get { return this.entity; }
      set {
        this.entity = value;
        this.entity.Referrers.Add(this);
      }
    }
    public override string Label  { get { return this.Entity.Name; } }
    public override string Type   { get { return this.Entity.Type; } }
  }

  // ... to watch another ParseAction and inform the Property about the outcome
  public class ConsumeOutcome : ParsePropertyAction {
    public ParseAction Action { get; set; }

    public override string Label  { get { return this.Action.ToString() + "?"; } }
    public override string Type   { get { return "bool"; } }
  }

  public class ConsumeAll : ParseAction {
    public List<ParseAction> Actions { get; set; }

    public override string Label {
      get {
        return
          "[" +
          string.Join( ",", this.Actions.Select(x => x.ToString()) ) +
          "]";
      }
    }

    public ConsumeAll() {
      this.Actions = new List<ParseAction>();
    }
  }

  // given a set of possible ParseActions, this tries each of these ParseActions
  // and passes on the first that parses
  public class ConsumeAny : ConsumeAll {
    public override string Label {
      get {
        return string.Join( "|", this.Actions.Select(x => x.Label) );
      }
    }
  }

  // the Model can be considered a Parser-AST on steroids. it contains all info
  // in such a way that a recursive descent parser can be constructed with ease
  public class Model {

    // the list of entities in the Model
    public List<Entity> Entities { get; set; }

    // derived dictionary mapping from Entity.Name to Entity
    private Dictionary<string,Entity> entities {
      get {
        return this.Entities.ToDictionary(
          entity => entity.Name,
          entity => entity
        );
      }
    }

    public bool Contains(string key) {
      return this.entities.Keys.Contains(key);
    }
    
    public Entity this[string key] {
      get {
        return this.Contains(key) ? this.entities[key] : null;
      }
    }

    // the first entity to start parsing
    public Entity Root {
      get { return this.Entities.Count > 0 ? this.Entities[0] : null; }
    }

    public Model() {
      this.Entities = new List<Entity>();
    }

    public override string ToString() {
      return
        "Model(" +
          "Entities=[" +
             string.Join(",", this.Entities.Select(x => x.ToString())) +
          "]," +
          "Root=" + (this.Root != null ? this.Root.Name : "") +
        ")";
    }
  }

  public class Factory {
    public Model Model { get; set; }
    
    public Factory() {
      this.Model = new Model();
    }

    public Factory Import(Grammar grammar) {
      this.ImportEntities(grammar.Rules);
      this.ImportPropertiesAndActions();

      return this;
    }

    private void ImportEntities(List<Rule> rules) {
      this.Model.Entities = rules
        .Select(rule => new Entity() {
          Name    = rule.Identifier,
          Rule    = rule
        }).ToList();
    }

    private void ImportPropertiesAndActions() {
      foreach(Entity entity in this.Model.Entities) {
        entity.ParseAction = this.ImportPropertiesAndParseActions(
          entity.Rule.Expression, entity
        );
      }
    }

    private ParseAction ImportPropertiesAndParseActions(Expression exp,
                                                        Entity     entity,
                                                        bool       optional=false)
    {
      // this.Log("extracting from " + exp.GetType().ToString());
      try {
        return new Dictionary<string, Func<Expression,Entity,bool,ParseAction>>() {
          { "SequentialExpression",   this.ImportSequentialExpression   },
          { "AlternativesExpression", this.ImportAlternativesExpression },
          { "OptionalExpression",     this.ImportOptionalExpression     },
          // { "RepetitionExpression",   this.ExtractRepetitionExpression   },
          { "GroupExpression",        this.ImportGroupExpression        },
          { "IdentifierExpression",   this.ImportIdentifierExpression   },
          { "StringExpression",       this.ImportStringExpression       },
          { "ExtractorExpression",    this.ImportExtractorExpression    }
        }[exp.GetType().ToString()](exp, entity, optional);
      } catch(KeyNotFoundException e) {
        throw new NotImplementedException(
          "extracting not implemented for " + exp.GetType().ToString(), e
        );
      }
    }

    private ParseAction ImportStringExpression(Expression exp,
                                               Entity     entity,
                                               bool       optional=false)
    {
      StringExpression str = ((StringExpression)exp);
      // if a StringExpression has an explicit Name, we create a Property for it
      // with that name
      if(str.Name != null) {
        Property property = new Property() { Name = str.Name };
        entity.Add(property);
        return new ConsumeString() { Property = property, String = str.String };
      }
      // the simplest case: just a string, not optional, just consume it
      return new ConsumeToken() { Token = str.String };
    }    

    private ParseAction ImportIdentifierExpression(Expression exp,
                                                   Entity     entity,
                                                   bool       optional=false)
    {
      IdentifierExpression id = ((IdentifierExpression)exp);

      if( ! this.Model.Contains(id.Identifier) ) {
        throw new ArgumentException("unknown Entity Identifier " + id.Identifier);
      }

      string name = id.Name != null ? id.Name : id.Identifier;
      Property property = new Property() { Name = name };
      entity.Add(property);

      return new ConsumeEntity() {
        Property = property,
        Entity   = this.Model[id.Identifier]
      };
    }

    private ParseAction ImportExtractorExpression(Expression exp,
                                                  Entity     entity,
                                                  bool       optional=false)
    {
      ExtractorExpression extr = ((ExtractorExpression)exp);
      // if an ExtractorExpression has an explicit Name, we create a Property
      // for it with that name
      if(extr.Name != null) {
        Property property = new Property() { Name = extr.Name };
        entity.Add(property);
        return new ConsumePattern() { Property = property, Pattern = extr.Regex };
      }
      // the simplest case: just a string, not optional, just consume it
      return new ConsumePatternToken() { Token = extr.Regex };
    }

    private ParseAction ImportOptionalExpression(Expression exp,
                                                 Entity     entity,
                                                 bool       opt=false)
    {
      OptionalExpression optional = ((OptionalExpression)exp);
      // recurse down
      ParseAction action = this.ImportPropertiesAndParseActions(
        optional.Expression,
        entity
      );
      // mark optional
      action.IsOptional = true;

      // if the action isn't a ParsePropertyAction, there is no Property for it
      // yet, so we create one to store the positive or negative outcome of this
      // parse attempt
      if( ! (action is ParsePropertyAction) ) {
        Property property = new Property() { Name = "has-" + action.Label };
        entity.Add(property);
        return new ConsumeOutcome() {
          Action   = action,
          Property = property
        };
      }
      return action;
    }

    private ParseAction ImportSequentialExpression(Expression exp,
                                                   Entity entity,
                                                   bool opt=false)
    {
      SequentialExpression sequence = ((SequentialExpression)exp);

      ConsumeAll consume = new ConsumeAll();

      // SequentialExpression is constructed recusively, unroll it...
      while(true) {
        // add first part
        consume.Actions.Add(this.ImportPropertiesAndParseActions(
          sequence.NonSequentialExpression, entity
        ));
        // add remaining parts
        if(sequence.Expression is NonSequentialExpression) {
          // last part
          consume.Actions.Add(this.ImportPropertiesAndParseActions(
            sequence.Expression, entity
          ));
          break;
        } else {
          // recurse
          sequence = (SequentialExpression)sequence.Expression;
        }
      }
      return consume;
    }

    private ParseAction ImportAlternativesExpression(Expression exp,
                                                     Entity entity,
                                                     bool opt=false)
    {
      AlternativesExpression alternative = ((AlternativesExpression)exp);

      ConsumeAny consume = new ConsumeAny();

      // AlternativesExpression is constructed recusively, unroll it...
      while(true) {
        // add first part
        consume.Actions.Add(this.ImportPropertiesAndParseActions(
          alternative.AtomicExpression, entity
        ));
        // add remaining parts
        if(alternative.NonSequentialExpression is AtomicExpression) {
          // last part
          consume.Actions.Add(this.ImportPropertiesAndParseActions(
            alternative.NonSequentialExpression, entity
          ));
          break;
        } else {
          // recurse
          alternative =
            (AlternativesExpression)alternative.NonSequentialExpression;
        }
      }
      return consume;
    }

    // just recurse and provide a ParseAction for the nested Expression
    private ParseAction ImportGroupExpression(Expression exp,
                                              Entity     entity,
                                              bool       optional=false)
    {
      return this.ImportPropertiesAndParseActions(
        ((GroupExpression)exp).Expression, entity
      );
    }

    // Factory helper methods

    [ConditionalAttribute("DEBUG")]
    private void Log(string msg) {
      Console.Error.WriteLine("Factory: " + msg );
    }
  }

}
