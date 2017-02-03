// Parser Model Generator: transforms the Grammar AST into a Parser AST
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;

using System.Collections.Generic;

using System.Text.RegularExpressions;

using System.Linq;

using System.Diagnostics;

using System.Collections.ObjectModel;

namespace HumanParserGenerator.Generator {

  public class Entity {
    // the (original) Rule this Entity was constructed from
    public Rule Rule { get; set; }

    // a (back-)reference to the Model this Entity belongs to
    public Model Model { get; set; }

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
    
    // A Virtual Entity, implements structure from the Grammar, but shouldn't
    // be part of the Entities that can actually be constructed to build the
    // Model. In general the rule boils down to the fact if the Entity would be
    // a Simple one or a Complex one. If its Properties can simply be passed on
    // to a incorporating Entity (e.g. only 1 Property), the Entity becomes
    // Virtual. Entities with multiple Properties, aka structures, can't be
    // simply passed on, as in a return value, and require actual Entities to be
    // created to create the object model hierarchy.
    public bool IsVirtual {
      get {
        return this.Properties.Count < 2 // 1 Property can be passed on
          && ! this.IsRoot;              // top-level Entity _must_ be there
      }
    }
    
    public bool IsRoot { get { return this == this.Model.Root; } }

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
    // BUT in some cases, there isn't a Property, e.g. when just parsing Strings
    // or patterns to parse fixed structure that doesn't need to be extracted.
    // in that case there isn't a local Property that catches that String, but
    // Properties on other Entities can refer to it. In that case we need to
    // retrieve the property of the Action directly.
    public string Type {
      get {
        if( this.IsVirtual ) {
          if( this.Properties.Count > 0 ) {
            return this.Properties[0].Type;
          } else {
            return this.ParseAction.Type;
          }
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
          "Name=" + this.Name +
          ",Type=" + this.Type +
          ( this.Supers.Count == 0 ? "" :
            ",Supers=" + "[" +
              string.Join(",", this.Supers.Select(x => x.Name)) +
            "]"
          ) +
          ( this.Referrers.Count == 0 ? "" :
            ",Referrers=" + "[" +
              string.Join(",", this.Referrers.Select(x => x.Property.Label)) +
            "]"
          ) +
          ( this.Properties.Count == 0 ? "" :
            ",Properties=" + "[" +
              string.Join(",", this.Properties.Select(x => x.ToString())) +
            "]"
          ) +
          ",ParseAction=" + this.ParseAction.ToString() +
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
    public ParseAction Source { get; set; }

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
        "Name="        + this.Name             +
        ",Type="       + this.Type             +
        (this.IsPlural   ? ",IsPlural"   : "") +
        (this.IsOptional ? ",IsOptional" : "") +
        ",Source="     + this.Source           +
      ")";
    }
  }

  // ParseActions implement the steps that are taken to parse all information
  // needed to construct an Entity.

  public abstract class ParseAction {
    // the Parsing is optional
    public bool IsOptional { get; set; }

    // the Parsing should be repeated as much as possible
    public bool IsPlural { get; set; }

    // Label can be used for external string representation, other than ToString
    public abstract string Label { get; }

    // Type indicates what type of result this ParseAction will expose
    public abstract string Type  { get; }

    // (Optional) Property that receives parsing result from this ParseAction
    private Property property;
    public Property Property {
      get { return this.property; }
      set {
        this.property = value;
        this.property.Source = this; // back-reference
      }
    }

    public override string ToString() {
      return
        this.GetType().ToString().Replace("HumanParserGenerator.Generator.", "") +
        "(" + this.Label + ")" +
        (this.IsPlural   ? "*" : "") +
        (this.IsOptional ? "?" : "") +
        (this.Property != null ? "->" + this.Property.Name : "");
    }
  }

  // ... to consume a literal sequence of characters, aka a string ;-)
  public class ConsumeString : ParseAction {
    public          string String { get; set; }
    public override string Label  { get { return this.String; } }
    public override string Type   { get { return "string";    } }
  }

  // ... to consume a sequence of characters according to a regular expression
  public class ConsumePattern : ConsumeString {
    // alias for String
    public string Pattern {
      get { return this.String; }
      set { this.String = value; }
    }
  }

  // ... to consume another Entity
  public class ConsumeEntity : ParseAction {
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
  public class ConsumeOutcome : ParseAction {
    public ParseAction Action { get; set; }

    public override string Label  { get { return this.Action.ToString(); } }
    public override string Type   { get { return "bool"; } }
  }

  public class ConsumeAll : ParseAction {
    protected List<ParseAction> actions = new List<ParseAction>();
    public ReadOnlyCollection<ParseAction> Actions {
      get { return this.actions.AsReadOnly(); }
      set {
        this.actions.Clear();
        foreach(var action in value) {
          this.Add(action);
        }
      }
    }
    public virtual void Add(ParseAction action) {
      this.actions.Add(action);
    }
    
    // TODO this shouldn't be possible, but we need to check ;-)
    public override string Type { get { return null; } }

    public override string Label {
      get {
        return
          "[" +
          string.Join( ",", this.Actions.Select(x => x.ToString()) ) +
          "]";
      }
    }
  }

  // given a set of possible ParseActions, this tries each of these ParseActions
  // and passes on the first that parses
  // all of the alternatives MUST have the same type!
  public class ConsumeAny : ConsumeAll {
    public override void Add(ParseAction action) {
      if(this.actions.Count > 1) {
        if( ! action.Type.Equals(this.Type) ) {
          throw new ArgumentException("all alternatives must have same type");
        }
      }
      this.actions.Add(action);
    }

    public override string Type {
      get {
        if( this.actions.Count > 0 ) { return this.actions[0].Type; }
        return null;
      }
    }
  }

  // the Model can be considered a Parser-AST on steroids. it contains all info
  // in such a way that a recursive descent parser can be constructed with ease

  public class Model {

    // the entities in the Model are stored in a Name->Entity Dictionary
    private Dictionary<string,Entity> entities;
    // public interface consists of a List of Entities
    public List<Entity> Entities {
      get { return this.entities.Values.ToList(); }
      set {
        this.entities.Clear();
        foreach(var entity in value) {
          this.Add(entity);
        }
      }
    }
    // the model behaves as a mix between List and Dictionary to the outside 
    // world offering access to the Entities in the actual underlying dictionary
    public Model Add(Entity entity) {
      entity.Model = this;
      this.entities.Add(entity.Name, entity);
      if(this.Entities.Count == 1) { this.Root = entity; } // First
      return this;
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
    public Entity Root { get; private set; }

    public Model() {
      this.entities = new Dictionary<string,Entity>();
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
          { "RepetitionExpression",   this.ImportRepetitionExpression   },
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
      return new ConsumeString() { String = str.String };
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
      return new ConsumePattern() { Pattern = extr.Regex };
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

      // if the action doesn't have a Property reference, we create one now.
      // this is possible in case of simple String extraction without the need
      // to store it, aka Token consumption.
      if( action.Property == null ) {
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
        consume.Add(this.ImportPropertiesAndParseActions(
          sequence.NonSequentialExpression, entity
        ));
        // add remaining parts
        if(sequence.Expression is NonSequentialExpression) {
          // last part
          consume.Add(this.ImportPropertiesAndParseActions(
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
        consume.Add(this.ImportPropertiesAndParseActions(
          alternative.AtomicExpression, entity
        ));
        // add remaining parts
        if(alternative.NonSequentialExpression is AtomicExpression) {
          // last part
          consume.Add(this.ImportPropertiesAndParseActions(
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

    private ParseAction ImportRepetitionExpression(Expression exp,
                                                   Entity     entity,
                                                   bool       opt=false)
    {
      RepetitionExpression repetition = ((RepetitionExpression)exp);
      // recurse down
      ParseAction action = this.ImportPropertiesAndParseActions(
        repetition.Expression,
        entity
      );
      // mark Plural
      action.IsPlural = true;

      return action;
    }

    // Factory helper methods

    [ConditionalAttribute("DEBUG")]
    private void Log(string msg) {
      Console.Error.WriteLine("Factory: " + msg );
    }
  }

}
