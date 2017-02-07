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

    // the name of the rule/Entity
    public string Name { get; set; }

    // properties on the Entity that have been created to hold information
    // extracted by the parsing rule's ParsActions
    private List<Property> properties;
    public ReadOnlyCollection<Property> Properties {
      get { return this.properties.AsReadOnly(); }
      set {
        this.properties.Clear();
        foreach(Property property in value) {
          this.Add(property);
        }
      }
    }

    public void Add(Property property) {
      this.properties.Add(property);
      // manage back-reference
      property.Entity = this;
    }

    public void Remove(Property property) {
      this.properties.Remove(property);
    }
    

    // to populate the Properties, ParseActions have to be generated
    // ParseActions are a tree-structure with a single top-level ParseAction
    public ParseAction ParseAction { get; set; }
    
    // a Virtual Entity is suppressed from the resulting AST
    // the general rule is that this is possible for entities with only 1 prop
    // exceptions to this rule are:
    // - the Root entity can never be Virtual
    // - leaf Entities (without subEntities) also can't be Virtual
    //   except for Extractors that are always Virtual Entities (of type string)
    public bool IsVirtual {
      get {
        // the Root can never be Virtual
        if( this.IsRoot ) { return false; }

        // Extractors are always Virtual
        if( this.ParseAction is ConsumePattern ) { return true; }
        
        // entities without sub-classes, are "leafs" and cannot be Virtual
        if( this.Subs.Count == 0 ) { return false; }
        
        // if this Entity has only one Property, it is Virtual (DEFAULT RULE)
        if( this.Properties.Count() == 1) { return true; }

        // everything else is an Entity that will appear in the AST
        return false;
      }
    }

    public bool IsRoot { get { return this == this.Model.Root; } }

    // the Entity can be optional, if its top-level ParseAction is Optional
    public bool IsOptional { get { return this.ParseAction.IsOptional; } }

    // Inheritance Model  Super <|-- Sub
    public List<Entity> Supers { get; set; }
    public List<Entity> Subs { get; set; }

    public bool IsA(Entity super) {
      if(this.Supers.Contains(super)) { return true; }
      foreach(Entity parent in this.Supers) {
        if(parent.IsA(super)) { return true; }
      }
      return false;
    }

    public string DefaultType { get { return this.Name; } }

    public string Type {
      get {
        if( this.IsVirtual ) {
          if( this.Properties.Count == 1 ) {
            return this.Properties[0].Type;
          } else {
            if( this.ParseAction != null ) {
              if( this.ParseAction.Type != null ) {
                return this.ParseAction.Type;
              }
            } else {
              throw new ArgumentException("missing ParseAction on " + this.Name);
            }
          }
        }
        return this.DefaultType;
      }
    }

    public Entity() {
      this.properties = new List<Property>();
      this.Supers     = new List<Entity>();
      this.Subs       = new List<Entity>();
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
          ( this.Subs.Count == 0 ? "" :
            ",Subs=" + "[" +
              string.Join(",", this.Subs.Select(x => x.Name)) +
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
    // a (back-)reference to the Entity this property belongs to, managed by
    // the Entity
    public Entity Entity { get; set; }
    
    // a unique name to identify the property, used for variable emission
    private string rawname;
    // to make sure the name is unique, an index is added - if needed
    public string Name {
      get {
        return this.rawname + (this.IsIndexed ? this.Index.ToString() : "");
      }
      set {
        this.rawname = value;
      }
    }

    // is this property indexed?
    public bool IsIndexed {
      get {
        return this.Entity.Properties
          .Where(property => property.rawname.Equals(this.rawname))
          .ToList().Count() > 1;
      }
    }

    // a Fully Qualified Name, including the Entity
    public string FQN { get { return this.Entity.Name + "." + this.Name; } }

    // a Label is an alias for the FQN for this Property
    public string Label { get { return this.FQN; } }

    // if multiple properties on an Entity have the same name, an index is 
    // computed to differentiate between them
    public int Index {
      get {
        return this.Entity.Properties
          .Where(property => property.rawname.Equals(this.rawname))
          .ToList()
          .IndexOf(this);
      }
    }

    // a property is populated by a ParseAction
    private ParseAction source;
    public ParseAction Source {
      get {
        if(this.source == null) {
          throw new ArgumentException(this.FQN + " has no Source! ");
        }
        return this.source;
      }
      set {
        this.source = value;
      }
    }

    // the Type of a Property is defined by the ParseAction
    public string Type { get { return this.Source.Type; } }

    // a Property can me marked as Plural, meaning that it will contain a list
    // of Type parsing results, which depends on the ParseAction
    public bool IsPlural { get { return this.Source.IsPlural; } }

    // a Property can be Optional, which depends on the ParseAction
    public bool IsOptional { get { return this.Source.IsOptional; } }

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
    
    // don't pass on the result, but the successfull outcome
    public bool ReportSuccess { get; set; }

    // the Parsing should be repeated as much as possible
    public bool IsPlural { get; set; }

    // Label can be used for external string representation, other than ToString
    public abstract string Label { get; }

    // Representation can be used for a more elaborate/technical label
    public virtual string Representation { get { return this.Label; } }

    // Name can be used for code-level representation, e.g. a variable name
    public abstract string Name { get; }

    // Type indicates what type of result this ParseAction will expose
    public abstract string Type  { get; }

    // (Optional) Property that receives parsing result from this ParseAction
    public Property Property { get; set; }

    public override string ToString() {
      return
        this.GetType().ToString().Replace("HumanParserGenerator.Generator.", "") +
        "(" + this.Representation + ")" +
        (this.IsPlural   ? "*" : "") +
        (this.IsOptional ? "?" : "") +
        (this.Property != null ? "->" + this.Property.Name : "");
    }
  }

  // ... to consume a literal sequence of characters, aka a string ;-)
  public class ConsumeString : ParseAction {
    public          string String { get; set; }
    public override string Label  { get { return this.String; } }
    public override string Type   {
      get { return  this.ReportSuccess ? "bool" : "string"; }
    }
    public override string Name   { get { return this.String.Replace(" ", "-"); }}
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
    public Entity Entity { get; set; }
    public override string Label  { get { return this.Entity.Name; } }
    public override string Type   {
      get { return this.ReportSuccess ? "bool" : this.Entity.Type; }
    }
    public override string Name   { get { return this.Entity.Name; } }
  }

  public class ConsumeAll : ParseAction {
    public List<ParseAction> Actions { get; set; }

    public ConsumeAll() {
      this.Actions = new List<ParseAction>();
    }
    
    // TODO if this All consists of one actual ConsumeEntity, we should behave
    //      as it was only that.
    public override string Type {
      get { return this.ReportSuccess ? "bool" : null; }
    }

    public override string Name   { get { return "all"; } }

    public override string Label {
      get {
        return "[" + string.Join(",", this.Actions.Select(x => x.Label)) + "]";
      }
    }
    public override string Representation {
      get {
        return
          "[" + string.Join(",", this.Actions.Select(x => x.ToString())) + "]";
      }
    }
  }

  // given a set of possible ParseActions, this tries each of these ParseActions
  // and passes on the first that parses
  // all of the alternatives MUST have the same type!
  public class ConsumeAny : ConsumeAll {
    public override string Name   { get { return "any"; } }

    public override string Type {
      get {
        if( this.ReportSuccess ) { return "bool"; }
        
        // case 1: if all alternatives expose the same Type (string or null
        // probably), we take on that type
        if( this.Actions.Select(a => a.Type).Distinct().Count() == 1) {
          return this.Actions[0].Type;
        }
        
        // default is simply the Default Entity Type, referring to Type would
        // cause an endless recursion ;-)
        if(this.Property != null) {
          return this.Property.Entity.DefaultType;
        }
        
        return null;
      }
    }

    public override string Label {
      get { return string.Join( " | ", this.Actions.Select(x => x.Label) ); }
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

    public Entity GetEntity(string name) {
      if( ! this.Model.Contains(name) ) {
        throw new ArgumentException("unknown Rule " + name);
      }
      return this.Model[name];
    }

    public Factory() {
      this.Model = new Model();
    }

    public Factory Import(Grammar grammar) {
      this.ImportEntities(grammar.Rules);
      this.ImportPropertiesAndActions();

      this.DetectInheritance();   // to temporary detect Leafs
      this.CollapseAlternatives();
      this.DetectInheritance();

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
        this.ImportPropertiesAndParseActions(entity);
      }
    }

    private void ImportPropertiesAndParseActions(Entity entity) {
      entity.ParseAction = this.ImportPropertiesAndParseActions(
        entity.Rule.Expression, entity
      );
    }

    private ParseAction ImportPropertiesAndParseActions(Expression exp,
                                                        Entity     entity)
    {
      this.Log("ImportPropertiesAndParseActions("+exp.GetType().ToString()+")" );
      try {
        return new Dictionary<string, Func<Expression,Entity,ParseAction>>() {
          { "SequentialExpression",   this.ImportSequentialExpression   },
          { "AlternativesExpression", this.ImportAlternativesExpression },
          { "OptionalExpression",     this.ImportOptionalExpression     },
          { "RepetitionExpression",   this.ImportRepetitionExpression   },
          { "GroupExpression",        this.ImportGroupExpression        },
          { "IdentifierExpression",   this.ImportIdentifierExpression   },
          { "StringExpression",       this.ImportStringExpression       },
          { "ExtractorExpression",    this.ImportExtractorExpression    }
        }[exp.GetType().ToString()](exp, entity);
      } catch(KeyNotFoundException e) {
        throw new NotImplementedException(
          "Importing not implemented for " + exp.GetType().ToString(), e
        );
      }
    }

    // helper method to wire Entityt -> Property -> ParseAction
    private ParseAction Add(Entity entity, Property prop, ParseAction consume) {
      entity.Add(prop);
      consume.Property = prop;
      prop.Source = consume;
      return consume;
    }

    private ParseAction ImportStringExpression(Expression exp,
                                               Entity     entity)
    {
      StringExpression str = ((StringExpression)exp);
      ParseAction consume = new ConsumeString() { String = str.String };

      // if a StringExpression has an explicit Name, we create a Property for it
      // with that name
      if(str.Name != null) {
        return this.Add(
          entity,
          new Property() { Name = str.Name },
          consume
        );
      }
      // a simple consumer of text, with no resulting information
      return consume;
    }    

    private ParseAction ImportIdentifierExpression(Expression exp,
                                                   Entity     entity)
    {
      IdentifierExpression id = ((IdentifierExpression)exp);

      Entity referred = this.GetEntity(id.Identifier);

      return this.Add(
        entity,
        new Property() { Name = id.Name != null ? id.Name : id.Identifier },
        new ConsumeEntity() { Entity = referred }
      );
    }

    private ParseAction ImportExtractorExpression(Expression exp,
                                                  Entity     entity)
    {
      ExtractorExpression extr = ((ExtractorExpression)exp);

      return this.Add(
        entity,
        new Property() { Name = extr.Name != null ? extr.Name : entity.Name },
        new ConsumePattern() { Pattern = extr.Regex }
      );
    }

    private ParseAction ImportOptionalExpression(Expression exp,
                                                 Entity     entity)
    {
      OptionalExpression optional = ((OptionalExpression)exp);

      // recurse down
      ParseAction consume = this.ImportPropertiesAndParseActions(
        optional.Expression,
        entity
      );
      // mark optional
      consume.IsOptional = true;

      // if the action doesn't have a Property reference, we create one now.
      // this is possible in case of simple String extraction without the need
      // to store it, aka Token consumption.
      if( consume.Property == null ) {
        consume.ReportSuccess = true;
        return this.Add(
          entity,
          new Property() { Name = "has-" + consume.Name },
          consume
        );
      }
      return consume;
    }

    private ParseAction ImportSequentialExpression(Expression exp,
                                                   Entity entity)
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
                                                     Entity entity)
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
                                              Entity     entity)
    {
      return this.ImportPropertiesAndParseActions(
        ((GroupExpression)exp).Expression, entity
      );
    }

    private ParseAction ImportRepetitionExpression(Expression exp,
                                                   Entity     entity)
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


    // After importing all Entitie, Properties and ParseActions, we apply our
    // transformation rules:

    // RULE 1: Alternatives that can be collapsed (e.g. multiple Properties can
    //         be replaced by a single one) are collapsed.

    private void CollapseAlternatives() {
      // we need to do this bottom-up, because lower-level alternatives might
      // collapse and change their Entity's type, causing a different decision
      // higher up the Entity hierarchy.

      // we start by doing a top-down scan of all Entities in the Model
      foreach(Entity entity in this.Model.Entities) {
        this.CollapseAlternatives(entity);
      }
    }
    
    private bool CollapseAlternatives(Entity entity) {
      // we're only interested in Alternatives
      // TODO ConsumeAll with only one active ConsumeAny might also be valid
      if( ! (entity.ParseAction is ConsumeAny) ) { return false; }

      ConsumeAny consume = (ConsumeAny)entity.ParseAction;

      // make sure that Entities referenced by our alternative ParseActions
      // are already collapsed
      foreach(ParseAction action in consume.Actions) {
        if(action is ConsumeEntity) {
          this.CollapseAlternatives(((ConsumeEntity)action).Entity);
        }
      }

      // if all Properties result from the ParseActions Alternatives, AND they 
      // are not a mix of strings and other Types, we can replace 
      // them by a single one...
      int all     = entity.Properties.Count();
      int props   = consume.Actions.Where(a => a.Property != null).Count();
      int strings = consume.Actions.Where(a => (a.Type != null && a.Type.Equals("string"))).Count();
      this.Log(entity.Name + " : # all: " + all.ToString() + " = # strings: " + strings.ToString() + " / # props: " + props.ToString() );
      this.Log("    - " + string.Join("   \n    - ", consume.Actions.Select(action=>action.Type)));
      if(all == 0 || props == all && (strings == all || strings == 0)) {
        // Add a new Property to the Entity that holds the outcome of the
        // Consumption
        Property property = new Property() {
          Name   = "alternative",
          Source = consume
        };
        consume.Property = property;
        property.Source  = consume;

        entity.Add(property);
      
        // make all original consumers point to the new alternative property
        foreach(ParseAction action in consume.Actions) {
          if(action.Property != null) {
            action.Property.Entity.Remove(action.Property);
            action.Property = property;          
          }
        }

        this.Log(
          "collapsed " + entity.Name + " : " + 
          entity.Type + " " + (entity.IsVirtual ? "virtual" : "")
        );
        this.Log("    - " + string.Join("   \n    - ",
          consume.Actions.Select(action=>action.Type)));        
      
        return true;
      }
      return false;
    }

    // RULE 2: detect inheritance in 3 cases: 
    //         1. Single reference to other NonPlural Entity
    //         2. Alternatives of only Entity references push their type down
    //         3. Sequences that contain only one Entity reference

    private void DetectInheritance() {
      foreach(Entity entity in this.Model.Entities) {
        entity.Supers.Clear();
        entity.Subs.Clear();
      }
      foreach(Entity entity in this.Model.Entities) {
        this.DetectInheritance(entity);
      }
    }

    private void DetectInheritance(Entity entity) {
      // 1-on-1 (TODO: actually in use/usefull?)
      if(entity.ParseAction is ConsumeEntity && ! entity.ParseAction.IsPlural) {
        this.AddInheritance(entity, ((ConsumeEntity)entity.ParseAction).Entity);
      }
      // Alternatives, 1-on-n x ConsumeEntity.Type != String
      if(entity.ParseAction is ConsumeAny) {
        List<ParseAction> actions = ((ConsumeAny)entity.ParseAction).Actions;
        // check if the alternative types are all non-strings or all strings
        // if there are any NULL Types (e.g. from Sequence), we don't collapse
        int all = actions.Count();
        int strings = actions.Where(a => a.Type != null && a.Type.Equals("string")).Count();
        int nulls = actions.Where(a => a.Type == null).Count();
        if( nulls == 0 && (strings == 0 || strings == all)) {
          foreach(ParseAction action in actions) {
            if(action is ConsumeEntity) {
              this.AddInheritance(entity, ((ConsumeEntity)action).Entity);
            }
          }
        }
      }
      // ConsumeAll that actually is a single ConsumeEntity and otherwise only
      // none-Property related Consumes
      if(entity.ParseAction is ConsumeAll) {
        List<ParseAction> actions = ((ConsumeAll)entity.ParseAction).Actions;
        if( actions.OfType<ConsumeEntity>().Count() == 1 ) {
          int other = actions.Where(action => action.Property == null).Count();
          if(actions.Count() == other + 1) {
            Entity sub = actions.OfType<ConsumeEntity>().ToList()[0].Entity;
            this.AddInheritance(entity, sub);
          }
        }
        
      }
    }

    private void AddInheritance(Entity parent, Entity child) {
      // avoid recursive inheritance relationships
      if( parent.IsA(child) ) { return; }
      // connect
      parent.Subs.Add(child);
      child.Supers.Add(parent);
      this.Log(parent.Name + " <|-- " + child.Name);
    }

    // Factory helper methods

    [ConditionalAttribute("DEBUG")]
    private void Log(string msg) {
      Console.Error.WriteLine("Factory: " + msg );
    }
  }

}
