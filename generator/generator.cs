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
    public HashSet<Entity> Supers { get; set; }
    public HashSet<Entity> Subs   { get; set; }

    public bool HasSibblings {
      get {
        if(this.Supers.Count == 0) { return false; }
        // counts = list of # sibblings of supers
        List<int> counts =
          this.Supers.Select(super=>super.Subs.Count()).Distinct().ToList();
        // if supers have different # subs OR same but multiple
        return (counts.Count > 1 || counts.First() > 1);
      }
    }

    public bool IsA(Entity super) {
      if(this.Supers.Contains(super)) { return true; }
      foreach(Entity parent in this.Supers) {
        if(parent.IsA(super)) { return true; }
      }
      return false;
    }
    
    public void IsSubEntityOf(Entity parent) {
      this.Supers.Add(parent);
      parent.Subs.Add(this);
    }

    public void IsSuperEntityOf(Entity child) {
      this.Subs.Add(child);
      child.Supers.Add(this);
    }

    // a Type is always the Entity itself unless the Entity behaves in a Virtual
    // way, then it returns the type of its first property
    public string Type {
      get {
        if(this.IsVirtual){
          return this.Properties.First().Type;
        }
        return this.DefaultType;
      }
    }
    
    public string DefaultType { get { return this.Name; } }

    public Entity() {
      this.properties = new List<Property>();
      this.Supers     = new HashSet<Entity>();
      this.Subs       = new HashSet<Entity>();
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
          (this.ParseAction != null ? ",ParseAction=" + this.ParseAction.ToString() : "") +
        ")";
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

    // is this property indexed == are there > 1 properties with our rawname
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
    // computed to differentiate between them, if only one Property carries the
    // same name, it's "this" property, and it has index 0.
    public int Index {
      get {
        return this.Entity.Properties
          .Where(property => property.rawname.Equals(this.rawname))
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
      return "Property(" +
        "Name="        + this.Name             +
        ",Type="       + this.Type             +
        (this.IsPlural   ? ",IsPlural"   : "") +
        (this.IsOptional ? ",IsOptional" : "") +
        ",Source="     + this.Source           +
      ")";
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

    // Representation can be used for a more elaborate/technical label
    public virtual string Representation { get { return this.Label; } }

    // Name can be used for code-level representation, e.g. a variable name
    public abstract string Name { get; }

    // Type indicates what type of result this ParseAction will expose
    public abstract string Type  { get; }

    // (Optional) Property that receives parsing result from this ParseAction
    public Property Property { get; set; }

    // Back-reference to our Parent ParseAction, when null = top-level @ Entity
    public ParseAction Parent { get; set; }

    public override string ToString() {
      return
        this.GetType().ToString().Replace("HumanParserGenerator.Generator.", "") +
        "(" + this.Representation + ")" +
        (this.IsPlural      ? "*" : "") +
        (this.IsOptional    ? "?" : "") +
        (this.ReportSuccess ? "!" : "") +
        (this.Property != null ? "->" + this.Property.Name : "");
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
      get { return this.ReportSuccess ? "<bool>" : this.Entity.Type; }
    }
    public override string Name   { get { return this.Entity.Name; } }
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

    // offer a Dinctionary-like interface Model[name]=Entity
    public bool Contains(string key) {
      if(key == null) { return false; }
      return this.entities.Keys.Contains(key);
    }

    public Entity this[string key] {
      get { return this.Contains(key) ? this.entities[key] : null; }
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

}
