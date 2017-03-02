// Parser Model Generator: transforms the Grammar AST into a Parser AST
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Linq;
using System.Diagnostics;

using Formatting=HumanParserGenerator.Emitter.Format;

namespace HumanParserGenerator.Generator {

  public class Entity {
    // the (original) Rule this Entity was constructed from
    public Rule Rule { get; set; }

    // a (back-)reference to the Model this Entity belongs to
    public Model Model { get; internal set; }

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
    
    public void Clear() {
      this.properties.Clear();
    }

    public Property this[string name] {
      get {
        return this.Properties.FirstOrDefault(p => p.Name.Equals(name));
      }
    }

    // to populate the Properties, ParseActions have to be generated
    // ParseActions are a tree-structure with a single top-level ParseAction
    public ParseAction ParseAction { get; set; }
    
    // a Virtual Entity is suppressed from the resulting AST
    // the general rule is that this is possible for entities with only 1 prop
    // with some exceptions:
    // TODO simplify -> Sibblings/Subs might be more precise/corrrect
    public bool IsVirtual {
      get {
        // exception 3: the root is always Real
        if( this == this.Model.Root) { return false; }

        // only with 1 non-plural property possible
        if( this.Properties.Count == 1 && ! this.Properties.First().IsPlural ) {
          // pure patterns extractors = Virtual IF they have no sibblings!
          // else we need their Entity for Typing of the Strings
          if(this.ParseAction is ConsumePattern && ! this.HasSibblings) { return true; }
        
          // if we have SubEntities, we must be virtual ;-)
          if(this.Subs.Count > 0) { return true; }
        }

        return false;
      }
    }

    public bool IsRoot { get { return this == this.Model.Root; } }

    // the Entity can be optional, if its top-level ParseAction is Optional
    public bool IsOptional { get { return this.ParseAction.IsOptional; } }

    // Inheritance Model  Super <|-- Sub
    // properties to get/set them by name
    public HashSet<string> Supers { get; set; }
    public HashSet<string> Subs   { get; set; }

    // properties to get them as objects, with setters that update leading names
    // properties
    public ReadOnlyCollection<Entity> SuperEntities {
      get {
        return this.Supers.Select(super => this.Model[super]).ToList().AsReadOnly();
      }
      set {
        this.Supers = new HashSet<string>(value.Select(super => super.Name));
      }
    }

    public ReadOnlyCollection<Entity> SubEntities {
      get {
        return this.Subs.Select(sub => this.Model[sub]).ToList().AsReadOnly();
      }
      set {
        this.Subs = new HashSet<string>(value.Select(sub => sub.Name));
      }
    }

    public bool HasSibblings {
      get {
        if(this.Supers.Count == 0) { return false; }
        // counts = list of # sibblings of supers
        List<int> counts = this.SuperEntities
          .Select(super => super.Subs.Count()).Distinct().ToList();
        // if supers have different # subs OR same but multiple
        return (counts.Count > 1 || counts.First() > 1);
      }
    }

    public bool IsA(Entity super) {
      if(this.SuperEntities.Contains(super)) { return true; }
      foreach(Entity parent in this.SuperEntities) {
        if(parent.IsA(super)) { return true; }
      }
      return false;
    }
    
    // a Type is always the Entity itself unless the Entity behaves in a Virtual
    // way, then it returns the type of its first property
    public string Type {
      get {
        return this.IsVirtual ? this.Properties.First().Type : this.DefaultType;
      }
    }
    
    public string DefaultType { get { return this.Name; } }

    public Entity() {
      this.properties = new List<Property>();
      this.Supers     = new HashSet<string>();
      this.Subs       = new HashSet<string>();
    }

    public override string ToString() {
      return
        (this.IsVirtual ? "// virtual\n" : "") +
        "new Entity() {\n" +
          "Rule = " + ( this.Rule == null ? "null" : this.Rule.ToString() ) + ",\n" +
          "Name = " + Formatting.CSharp.Literal(this.Name) + ",\n" +
          "Properties = new List<Property>()" + 
            (this.Properties.Count > 0 ?
            " {\n" +
              string.Join(",\n",
                this.Properties.Select(property => property.ToString())
              ) +
            "\n}"
            : "") + 
          ".AsReadOnly(),\n" +
          "ParseAction = " + this.ParseAction.ToString() + ",\n" +
          "Supers = new HashSet<string>()" + 
          ( this.Supers.Count > 0 ?
            " {\n" +
              string.Join(", ",
                this.Supers.Select(super => Formatting.CSharp.Literal(super))
              ) +
            " }"
            : "" ) + 
          ",\n" +
          "Subs = new HashSet<string>()" +
          ( this.Subs.Count > 0 ?
            " { " +
              string.Join(", ",
                this.Subs.Select(sub => Formatting.CSharp.Literal(sub))
              ) +
            " }"
            : "") +
         "\n" +
        "}";
    }

    public bool HasPluralProperty() {
      return this.Properties.Where(x => x.IsPlural || x.Source.HasPluralParent).ToList().Count > 0;
    }
  }

  public class Property {
    // a (back-)reference to the Entity this property belongs to, managed by
    // the Entity
    public Entity Entity { get; set; }

    // a unique name to identify the property, used for variable emission
    public string RawName { get; private set; }
    // to make sure the name is unique, an index is added - if needed
    public string Name {
      get {
        return this.RawName + (this.IsIndexed ? this.Index.ToString() : "");
      }
      set {
        this.RawName = value;
      }
    }

    // is this property indexed == are there > 1 properties with our RawName
    public bool IsIndexed {
      get {
        return this.Entity.Properties
          .Where(property => property.RawName.Equals(this.RawName))
          .ToList().Count() > 1;
      }
    }

    // a Fully Qualified Name, including the Entity
    public string FQN { get { return this.Entity.Name + "." + this.Name; } }

    // a Label is an alias for the FQN for this Property
    public string Label { get { return this.FQN; } }

    // if multiple properties on an Entity have the same name, an index is 
    // computed to differentiate between them, if only one Property carries the
    // same name, it's "this" property, and it has index 0.
    public int Index {
      get {
        return this.Entity.Properties
          .Where(property => property.RawName.Equals(this.RawName))
          .ToList()
          .IndexOf(this);
      }
    }

    // a property is ALWAYS populated by a ParseAction
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
      return "new Property() {\n" +
        "Name = " + Formatting.CSharp.Literal(this.RawName) + ",\n" +
        "Source = " + this.Source.ToString() + "\n" +
      "}";
    }
  }

  public class PropertyProxy : Property {
    public List<Property> Proxied { get; set; }
    public PropertyProxy() {
      this.Proxied = new List<Property>();
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

    // an action can be embedded in a plural parent, while NOT plural itself
    public bool HasPluralParent {
      get {
        ParseAction parent = this.Parent;
        while(parent != null) {
          if( parent.IsPlural ) { return true; }
          parent = parent.Parent;
        }
        return false;
      }
    }

    // Label can be used for external string representation, other than ToString
    public abstract string Label { get; }

    // Name can be used for code-level representation, e.g. a variable name
    public abstract string Name { get; }

    // Type indicates what type of result this ParseAction will expose
    public abstract string Type  { get; }

    // (Optional) Property that receives parsing result from this ParseAction
    public Property Property { get; set; }

    // Back-reference to our Parent ParseAction, when null = top-level @ Entity
    public ParseAction Parent { get; set; }

    // returns only the common part as implemented by this abstract base class
    public override string ToString() {
      return string.Join(", ", new List<string>() {
        (this.IsOptional ? "IsOptional = true" : null ),
        (this.ReportSuccess ? "ReportSuccess = true" : null ),
        (this.IsPlural ? "IsPlural = true" : null )
      }.Where(p => p != null));
    }
  }

  // ... to consume a literal sequence of characters, aka a string ;-)
  public class ConsumeString : ParseAction {
    public          string String { get; set; }
    public override string Label  { get { return this.String; } }
    public override string Type   {
      get { return  this.ReportSuccess ? "<bool>" : "<string>"; }
    }
    public override string Name { get { return this.String.Replace(" ", "-"); }}
    
    public override string ToString() {
      string inherited = base.ToString();
      return ! this.GetType().ToString().Split('.').Last().Equals("ConsumeString") ? inherited :
        "new ConsumeString() {\n" +
        (inherited.Equals("") ? "" : inherited + ",\n" ) +
        "String = " + Formatting.CSharp.Literal(this.String) + "\n" + 
      "}";
    }
  }

  // ... to consume a sequence of characters according to a regular expression
  public class ConsumePattern : ConsumeString {
    // alias for String
    public string Pattern {
      get { return this.String; }
      set { this.String = value; }
    }
    public override string ToString() {
      string inherited = base.ToString();
      return "new ConsumePattern() {" +
        (inherited.Equals("") ? "" : inherited + ",\n" ) +
        "Pattern = " + Formatting.CSharp.Literal(this.Pattern) + "\n" + 
      "}";
    }
  }

  // ... to consume another Entity
  public class ConsumeEntity : ParseAction {
    // "set/get" the Reference(Name) and "get" the actual Entity
    public string Reference { get; set; }
    public Entity Entity {
      get { return this.Property.Entity.Model[this.Reference]; }
    }

    public override string Label  { get { return this.Entity.Name; } }
    public override string Type   {
      get { return this.ReportSuccess ? "<bool>" : this.Entity.Type; }
    }
    public override string Name   { get { return this.Entity.Name; } }
    public override string ToString() {
      string inherited = base.ToString();
      return "new ConsumeEntity() {\n" +
        (inherited.Equals("") ? "" : inherited + ",\n" ) +
        "Reference = " + Formatting.CSharp.Literal(this.Reference) + "\n" +
      "}";
    }
  }

  public class ConsumeAll : ParseAction {
    public List<ParseAction> Actions { get; set; }

    public ConsumeAll() {
      this.Actions = new List<ParseAction>();
    }
    
    public override string Type {
      get {
        if( this.ReportSuccess ) { return "<bool>"; }
        string type = null;
        foreach(ParseAction action in this.Actions) {
          if(action.Type != null) {
            if(type != null) { return null; } // more than one Type
            type = action.Type;
          }
        }
        return type; // only one type bubbled up
      }
    }

    public override string Name {
      get {
        // construct a name based on the names of the sub-Actions
        List<string> parts =
          this.Actions.Where(a => a is ConsumeEntity).Select(a=>a.Name).ToList();
        string name = parts.Count() > 0 ? string.Join("-", parts) : "all";
        return name;
      }
    }

    public override string Label {
      get {
        return "[" + string.Join(",", this.Actions.Select(x => x.Label)) + "]";
      }
    }

    public override string ToString() {
      string inherited = base.ToString();
      return ! this.GetType().ToString().Equals("HumanParserGenerator.Generator.ConsumeAll") ? base.ToString() :
        "new ConsumeAll() {\n" +
          (inherited.Equals("") ? "" : inherited + ",\n" ) +
          "Actions = new List<ParseAction>() {\n"
            + string.Join(",\n", this.Actions.Select(action => action.ToString())) + "\n" +
          "}\n" +
        "}";
    }
  }

  // given a set of possible ParseActions, this tries each of these ParseActions
  // and passes on the first that parses
  // all of the alternatives MUST have the same type!
  public class ConsumeAny : ConsumeAll {
    public override string Name   { get { return "any"; } }

    public override string Type {
      get {
        if( this.ReportSuccess ) { return "<bool>"; }
        
        // case 1: if all alternatives expose the same Type
        if( this.Actions.Select(a => a.Type).Distinct().Count() == 1) {
          return this.Actions.First().Type;
        }
        
        // case 2: if we're a Collapsed Alternatives Consumption
        if(this.Property != null) {
          return this.Property.Entity.DefaultType;
        }

        // TODO think this through
        return null;
      }
    }
    
    public override string Label {
      get { return string.Join( " | ", this.Actions.Select(x => x.Label) ); }
    }

    public override string ToString() {
      string inherited = base.ToString();
      return "new ConsumeAny() {\n" +
        (inherited.Equals("") ? "" : inherited + ",\n" ) +
        "Actions = new List<ParseAction>()" + 
          (this.Actions.Count > 0 ?
            " {\n" +
              string.Join( ",\n",
                this.Actions.Select(action => action.ToString())
              ) +
            "\n}" 
          : "") +
        "\n}";
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
      if(this.Entities.Count == 1) { this.RootName = entity.Name; } // First
      return this;
    }

    // offer a Dinctionary-like interface Model[name]=Entity
    public bool Contains(string key) {
      if(key == null) { return false; }
      return this.entities.Keys.Contains(key);
    }

    public Entity this[string key] {
      get { return this.Contains(key) ? this.entities[key] : null; }
    }

    // the first entity to start parsing
    public string RootName { get; set; }
    public Entity Root { get { return this[this.RootName]; } }

    public Model() {
      this.entities = new Dictionary<string,Entity>();
    }

    public override string ToString() {
      return
        "new Model() {\n" +
          "Entities = new List<Entity>()" + 
            (this.Entities.Count > 0 ?
              " {\n" +
               string.Join(",\n", this.Entities.Select(entity => entity.ToString())) +
              "\n}"
             : "" ) +
          "," +
          "\nRootName = " +
          (this.Root == null ? "null" : Formatting.CSharp.Literal(this.RootName) ) +
        "\n}";
    }
  }

}
