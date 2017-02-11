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

    // to populate the Properties, ParseActions have to be generated
    // ParseActions are a tree-structure with a single top-level ParseAction
    public ParseAction ParseAction { get; set; }
    
    // a Virtual Entity is suppressed from the resulting AST
    // the general rule is that this is possible for entities with only 1 prop
    // with some exceptions:
    // TODO simplify -> sibblings might be more precise/corrrect
    public bool IsVirtual {
      get {
        // exception 3: the root is always Real
        if( this == this.Model.Root) {
          return false;
        }

        // general rule: 1 NON Plural property == Virtual
        if( this.Properties.Count == 1 && ! this.Properties.First().IsPlural ) {
          // exception 1: leafs are Real
          if( this.Subs.Count == 0 ) {
            // exception 2: no super classes == allowed Virtual
            // OR superclasses have only me as sub
            if( this.Supers.Count == 0 || ! this.HasSibblings ) {
              return true; // exception 2: no super classes == allowed Virtual
            }
            return false;  // exception 1: leafs are Real
          }
          return true; // general rule: 1 property == Virtual
        }
        return false;  // general rule: > 1 property == Real
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
        // counts = list of # sibblings of supers
        List<int> counts =
          this.Supers.Select(super=>super.Subs.Count()).Distinct().ToList();
        // if supers have different # subs OR same but multiple
        return counts.Count > 1 || counts.First() > 1;
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
        // Console.Error.Write(this.Name +".Type ");
        // no properties
        if(this.Properties.Count == 0) {
          // this.Log();
          return null;
        }
        // one property
        if(this.IsVirtual){
          // this.Log(" is Virtual");
          return this.Properties.First().Type;
        }
        // default case
        // this.Log();
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
      // step 0:
      this.ImportEntities(grammar.Rules);

      if(this.Model.Entities.Count == 0) { return this; }

      // this.Log("===========================================");
      // this.Log("STEP 0:");
      // this.Log("-------------------------------------------");
      // this.Log(this.Model.ToString());
      // this.Log("===========================================");

      // step 1:
      this.ImportPropertiesAndActions();

      // this.Log("===========================================");
      // this.Log("STEP 1: imported");
      // this.Log("-------------------------------------------");
      // this.Log(this.Model.ToString());
      // this.Log("===========================================");

      // step 2:
      this.CollapseAlternatives();

      // this.Log("===========================================");
      // this.Log("STEP 2: collapsed alternatives");
      // this.Log("-------------------------------------------");
      // this.Log(this.Model.ToString());
      // this.Log("===========================================");
      
      // step 3:
      this.DetectInheritance();

      // this.Log("===========================================");
      // this.Log("STEP 3: with inheritance");
      // this.Log("-------------------------------------------");
      // this.Log(this.Model.ToString());
      // this.Log("===========================================");
      
      return this;
    }

    // STEP 0

    private void ImportEntities(List<Rule> rules) {
      this.Model.Entities = rules
        .Select(rule => new Entity() {
          Name    = rule.Identifier,
          Rule    = rule
        }).ToList();
    }

    // STEP 1

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

    private ParseAction ImportPropertiesAndParseActions(Expression  exp,
                                                        Entity      entity,
                                                        ParseAction parent=null,
                                                        bool        optional=false)
    {
      this.Log("ImportPropertiesAndParseActions("+exp.GetType().ToString()+")" );
      try {
        return new Dictionary<string, Func<Expression,Entity,ParseAction,bool,ParseAction>>() {
          { "SequentialExpression",   this.ImportSequentialExpression   },
          { "AlternativesExpression", this.ImportAlternativesExpression },
          { "OptionalExpression",     this.ImportOptionalExpression     },
          { "RepetitionExpression",   this.ImportRepetitionExpression   },
          { "GroupExpression",        this.ImportGroupExpression        },
          { "IdentifierExpression",   this.ImportIdentifierExpression   },
          { "StringExpression",       this.ImportStringExpression       },
          { "ExtractorExpression",    this.ImportExtractorExpression    }
        }[exp.GetType().ToString()](exp, entity, parent, optional);
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

    private ParseAction ImportStringExpression(Expression  exp,
                                               Entity      entity,
                                               ParseAction parent=null,
                                               bool        optional=false)
    {
      StringExpression str = ((StringExpression)exp);
      ParseAction consume = new ConsumeString() {
        String = str.String,
        Parent = parent
      };

      // default behavior for String Consumption is to just do it, it must be 
      // done to have succesful parsing
      
      // IF the string being parsed is within an optional context, we migt want
      // to know IF it was parsed. So we create a property to store a flag to
      // indicate whether the optional parse was successful or not.

      // IF the StringExpression has an explicit Name (name @ string-expression)
      // we _do_ create a property with that name.
      
      // IF the StringExpression has an Alternatives Parent we behave as if we
      // are optional.
      if( parent is ConsumeAny ) { optional = true; }

      // UNLESS the name is an underscore '_', then we don't create any Property
      if( str.Name != null && str.Name.Equals("_") ) { return consume; }

      if( str.Name != null || optional ) {
        if( optional ) { consume.ReportSuccess = true; }
        string name = (str.Name != null ? str.Name : consume.Name);
        // TODO improve
        if(name.Equals("-")) { name = "dash"; }
        name = name.Replace("(", "open-bracket");
        name = name.Replace(")", "close-bracket");
        name = name.Replace("[", "open-square-bracket");
        name = name.Replace("]", "close-square-bracket");
        name = name.Replace("{", "open-brace");
        name = name.Replace("}", "close-brace");
        name = name.Replace(":", "colon");
        name = name.Replace(";", "semi-colon");
        name = name.Replace(",", "comma");
        name = name.Replace(".", "dot");
        return this.Add(
          entity,
          new Property() {
            Name = (optional ? "has-" : "") + name                   
          },
          consume
        );
      }

      // a simple consumer of text, with no resulting information
      return consume;
    }    

    private ParseAction ImportIdentifierExpression(Expression  exp,
                                                   Entity      entity,
                                                   ParseAction parent=null,
                                                   bool        optional=false)
    {
      IdentifierExpression id = ((IdentifierExpression)exp);

      Entity referred = this.GetEntity(id.Identifier);

      return this.Add(
        entity,
        new Property() { Name = id.Name != null ? id.Name : id.Identifier },
        new ConsumeEntity() { Entity = referred, Parent = parent }
      );
    }

    private ParseAction ImportExtractorExpression(Expression  exp,
                                                  Entity      entity,
                                                  ParseAction parent=null,
                                                  bool        optional=false)
    {
      ExtractorExpression extr = ((ExtractorExpression)exp);

      return this.Add(
        entity,
        new Property() { Name = extr.Name != null ? extr.Name : entity.Name },
        new ConsumePattern() { Pattern = extr.Pattern, Parent = parent }
      );
    }

    private ParseAction ImportOptionalExpression(Expression  exp,
                                                 Entity      entity,
                                                 ParseAction parent=null,
                                                 bool        optional=false)
    {
      OptionalExpression option = ((OptionalExpression)exp);

      // recurse down
      ParseAction consume = this.ImportPropertiesAndParseActions(
        option.Expression, entity, parent, true
      );
      // mark optional
      consume.IsOptional = true;

      return consume;
    }

    private ParseAction ImportSequentialExpression(Expression  exp,
                                                   Entity      entity,
                                                   ParseAction parent=null,
                                                   bool        optional=false)
    {
      SequentialExpression sequence = ((SequentialExpression)exp);

      ConsumeAll consume = new ConsumeAll() { Parent = parent };

      // SequentialExpression is constructed recusively, unroll it...
      while(true) {
        // add first part
        consume.Actions.Add(this.ImportPropertiesAndParseActions(
          sequence.NonSequentialExpression, entity, consume, optional
        ));
        // add remaining parts
        if(sequence.Expression is NonSequentialExpression) {
          // last part
          consume.Actions.Add(this.ImportPropertiesAndParseActions(
            sequence.Expression, entity, consume, optional
          ));
          break;
        } else {
          // recurse
          sequence = (SequentialExpression)sequence.Expression;
        }
      }
      return consume;
    }

    private ParseAction ImportAlternativesExpression(Expression  exp,
                                                     Entity      entity,
                                                     ParseAction parent=null,
                                                     bool        optional=false)
    {
      AlternativesExpression alternative = ((AlternativesExpression)exp);

      ConsumeAny consume = new ConsumeAny() { Parent = parent };

      // AlternativesExpression is constructed recusively, unroll it...
      while(true) {
        // add first part
        consume.Actions.Add(this.ImportPropertiesAndParseActions(
          alternative.AtomicExpression, entity, consume, optional
        ));
        // add remaining parts
        if(alternative.NonSequentialExpression is AtomicExpression) {
          // last part
          consume.Actions.Add(this.ImportPropertiesAndParseActions(
            alternative.NonSequentialExpression, entity, consume, optional
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
    private ParseAction ImportGroupExpression(Expression  exp,
                                              Entity      entity,
                                              ParseAction parent=null,
                                              bool        optional=false)
    {
      return this.ImportPropertiesAndParseActions(
        ((GroupExpression)exp).Expression, entity, parent, optional
      );
    }

    private ParseAction ImportRepetitionExpression(Expression  exp,
                                                   Entity      entity,
                                                   ParseAction parent=null,
                                                   bool        optional=false)
    {
      RepetitionExpression repetition = ((RepetitionExpression)exp);
      // recurse down
      ParseAction action = this.ImportPropertiesAndParseActions(
        repetition.Expression, entity, parent, optional
      );
      // mark Plural
      action.IsPlural = true;

      return action;
    }

    // After importing all Entities, Properties and ParseActions, we apply our
    // transformation rules

    // STEP 2: collapse all Alternatives when all Properties belong to them

    private void CollapseAlternatives() {
      foreach(Entity entity in this.Model.Entities) {
        this.CollapseAlternatives(entity);
      }
    }

    private void CollapseAlternatives(Entity entity) {
      // we're only interested in Entities whose Properties belong to one 
      // Alternatives consuming action
      
      List<ParseAction> actions =
        entity.Properties.Select(prop => prop.Source.Parent).Distinct().ToList();

      // this should be ONLY 1 and it must BE CONSUMEANY
      if(actions.Count != 1 || ! (actions.First() is ConsumeAny)) { return; }

      // there CAN NOT be a mix of Entities and Basic Types or different Basic 
      // Types
      int entities = 0;
      int strings = 0;
      int bools = 0;
      foreach(Property prop in entity.Properties) {
        if( this.Model.Contains(prop.Type) ) { entities++; }
        else {
          if(prop.Type.Equals("<string>")) {
            strings++;
          } else {
            bools++;
          }
        }
      }
      // all entities or none
      if(entities != 0 && entities != entity.Properties.Count) { return; }
      // same basic types
      if(strings != 0 && bools != 0) { return; }

      this.Log("collapsing alternatives on " + entity.Name);

      ConsumeAny consume = (ConsumeAny)actions.First();
      
      // create a new Property to hold the outcome of the consumption
      PropertyProxy property = new PropertyProxy() {
        Name   = "alternative",
        Source = consume
      };
      // add the new Property as the target of the action
      consume.Property = property;
      
      // make all original consumers point to the new alternative property
      // this is the case for all existing properties
      foreach(Property prop in entity.Properties) {
        prop.Source.Property = property;
        property.Proxied.Add(prop);
        this.Log("  - " + prop.Type);
      }
      // now remove all old Properies
      entity.Clear();

      // add the new property to the Entity
      entity.Add(property);
    }

    // STEP 3: detect inheritance
    //         - all Entities with only ONE Property become Supers for all
    //           Entities that are referenced from it.
    //         - this can be a single Entity
    //         - or an alternatives

    private List<Entity> done = new List<Entity>();

    private void DetectInheritance() {
      // we need to recurse down through the hierarchy, starting at the Root
      this.DetectInheritance(this.Model.Root);
      this.Log("DetectInheritance ... DONE");
      this.ShowInheritance();
    }

    private void ShowInheritance() {
      this.Log("---------------------------------------------");
      foreach(Entity entity in this.Model.Entities) {
        string t = "";
        t += entity.IsVirtual ? "<" : "";
        t += entity.Name;
        t += entity.IsVirtual ? ">" : "";
        if(entity.Supers.Count > 0) {
          t += " : " + string.Join(",", entity.Supers.Select(s=>s.Name));
        }
        if(entity.Subs.Count > 0) {
          t += " <|-- " + string.Join(",", entity.Subs.Select(s=>s.Name));
        }
        this.Log(t);
      }
      this.Log("---------------------------------------------");
    }

    private void DetectInheritance(Entity entity) {
      this.Log("["+entity.Name+"] START");
      if( this.done.Contains(entity) ) {
        this.Log("["+entity.Name+"] SKIPPING");
        return;
      }
      // avoid recursion and doubles, keep track of what we've done
      this.done.Add(entity);
      
      // TODO CLEAN THIS UP :-(
      if(entity.Properties.Count == 1 && ! entity.Properties.First().IsPlural) {
        // only 1 Property? => we're the Super of the Entity/ies related to
        // the property.
        this.Log("["+entity.Name+"] is super");
        // PropertyProxy (Collapsed Alternatives)
        if(entity.Properties.First() is PropertyProxy) {
          // first push down our Superness to all children
          foreach(Property property in ((PropertyProxy)entity.Properties.First()).Proxied) {
            this.Log("["+entity.Name+"] processing proxied ");
            if(property.Source is ConsumeEntity) {
              this.AddInheritance(entity, ((ConsumeEntity)property.Source).Entity);
            } else {
              this.Log("["+entity.Name+"] not interested in " + property.Source.GetType().ToString());
            }
          }
          // recurse down
          foreach(Property property in ((PropertyProxy)entity.Properties.First()).Proxied) {
            this.Log("["+entity.Name+"] processing proxied ");
            if(property.Source is ConsumeEntity) {
              this.Log("["+entity.Name+"] recursing down proxied property's entity");
              this.DetectInheritance(((ConsumeEntity)property.Source).Entity);
            } else {
              this.Log("["+entity.Name+"] not interested in " + property.Source.GetType().ToString());
            }
          }

        } else {
          if(entity.Properties.First().Source is ConsumeEntity) {
            this.AddInheritance(entity, ((ConsumeEntity)entity.Properties.First().Source).Entity);
            this.Log("["+entity.Name+"] recursing down only property's entity");
            this.DetectInheritance(((ConsumeEntity)entity.Properties.First().Source).Entity);
          } else {
            this.Log("["+entity.Name+"] not interested in " + entity.Properties.First().Source.GetType().ToString());
          }
        }
      } else {
        this.Log("["+entity.Name+"] is not super, just recursing down...");
        // detect inheritance in all Entities on all properties
        foreach(Property property in entity.Properties) {
          if( property.Source is ConsumeEntity) {
            this.DetectInheritance( ((ConsumeEntity)property.Source).Entity );
          } else {
            this.Log("["+entity.Name+"] not interested in " + entity.Properties.First().Source.GetType().ToString());
          }
        }
      }
      this.Log("["+entity.Name+"] END");
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
