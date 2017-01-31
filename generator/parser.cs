// Parser Model Generator: transforms the BNF-like Grammar-AST into a Parser-AST
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

using HumanParserGenerator.Grammars;

namespace HumanParserGenerator.Generator {

  // Parser Model classes
  // 
  // The Parser Model (PM) consists of
  // - "Entities"
  // - "Extractions"
  // Both are "Referable"
  //
  // Entities have Properties and a ParseAction
  // Extractions have a Name and Pattern to extract tokens
  //
  // Properties have a Name, Type and IsPlural indication
  // ParseParseActions are a tree structure of steps to parse into Properties

  // Entities and Extractions are distinct concepts, but both can be referred to
  // by Properties.
  public abstract class Referable {
    public          string Name { get; set; }
    public abstract string Type { get; }
  }

  public class Entity : Referable {
    // original rule as it was parsed
    public Rule Rule { get; set; }

    // a property accepts the parsing result of another Referable
    // setting should be done using that Add() method
    public Dictionary<string,Property> Properties { get; }

    // to populate the Properties, ParseActions have to be generated
    public ParseAction ParseAction { get; set; }

    // all ParseActions of type ConsumeEntity that refer to us
    public List<ConsumeEntity> Referrers { get; set; }
    
    // if an entity's RULE's EXPRESSION IS A AlternativesExpression, it is 
    // VIRTUAL, which means it can be referred to, but doesn't show up in 
    // the AST
    public bool IsVirtual {
      get {
        // TODO generalize: can we make this depent on something else ?
        // e.g. # properties.Count == 1
        //      property.Referred.Count > 1 ?
        return Rule.Expression is AlternativesExpression;     
      }
    }

    // Entities can "be" other Virtual Entities it is referenced by from their
    // (only) Property.
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

    // the type of an Entity is always its own Name
    public override string Type { get { return this.Name; } }

    // helper dictionary to track property.Names with the last given index
    private Dictionary<string, int> propertyIndices;

    public Entity() {
      this.Referrers       = new List<ConsumeEntity>();
      this.Properties      = new Dictionary<string,Property>();
      this.propertyIndices = new Dictionary<string, int>();
    }

    public void Add(Property property) {
      // set the Entity reference to point to us
      property.Entity = this;

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

    public override string ToString() {
      return
        (this.IsVirtual ? "Virtual": "") + "Entity(" +
          "Name=" + this.Name + "," +
          "Supers=" + "[" +
            string.Join(",", this.Supers.Select(x => x.Name)) +
          "]," +
          "Referrers=" + "[" +
            string.Join(",", this.Referrers.Select(x => x.Property.Label)) +
          "]," +
          "Properties=" + "[" +
            string.Join(",", this.Properties.Select(x => x.Value.ToString())) +
          "]" + "," +
          "ParseAction=" + this.ParseAction.ToString() +
        ")";
    }

    public bool HasPluralProperty() {
      return this.Properties.Where(x => x.Value.IsPlural).ToList().Count > 0;
    }
  }

  public class Extraction : Referable {
    public override string Type { get { return "string"; } }
    public string Pattern { get; set; }
    public override string ToString() {
      return"Extraction(" + 
        "Name="   + this.Name    + "," +
        "Pattern" + this.Pattern +
      ")";
    }
  }

  public class Property {
    // a (back-)reference to the Entity this property belongs to
    public Entity Entity { get; set; }

    // a property is populated by a ParseAction
    public List<ParseAction> Sources { get; set; }

    // a unique name to identify the property, used for variable emission
    public string Name { get; set; }

    // the type is by default the type of the entity it accepts the parsed 
    // result for. 
    // if the Entity, this Property belongs to, is Virtual, there can only be
    // one property of the type of that Entity, because that is the only thing
    // that is passed to upper-layer Properties.
    // a setter is therefore not available, as it is deduceable
    public string Type {
      get {
        if(this.Entity.IsVirtual) {
          return this.Entity.Type;
        }
        // TODO: this can't be zero :-)
        return this.Sources.Count > 0 ? this.Sources[0].Type : "UNKNOWN";
      }
    }

    // a property can me marked as Plural, meaning that it will contain a list
    // of Type parsing results
    public bool IsPlural { get; set; }

    public Property() {
      this.Sources = new List<ParseAction>();
    }

    public string Label { get { return this.Entity.Name + "." + this.Name; } }

    public override string ToString() {
      return "Property(" +
        "Name="     + this.Name     + "," + 
        "Type="     + this.Type     + "," + 
        "IsPlural=" + this.IsPlural + "," +
        "Sources="  + "[" + 
          string.Join(",", this.Sources.Select(x => x.Label)) +
        "]" +
      ")";
    }
  }

  // a ParseAction parses into a Property
  public abstract class ParseAction {
    private Property property;
    public virtual  Property Property {
      get { return this.property; }
      set {
        this.property = value;
        this.property.Sources.Add(this);
      }
    }
    public abstract string   Label    { get; }
    public abstract string   Type     { get; }
  }

  // ... to consume a literal sequence of characters, aka a string ;-)
  public class ConsumeLiteral : ParseAction {
    public          string Literal { get; set; }
    public override string Label   { get { return this.Literal; } }
    public override string Type    { get { return "string";     } }
    public override string ToString() {
      return "Consume(" + this.Literal + ")";
    }
  }

  // ... to consume a sequence of characters according to a regular expression
  public class ConsumeExtraction : ParseAction {
    public Extraction Extraction { get; set; }
    public override string Label { get { return this.Extraction.Name; } }
    public override string Type  { get { return "string";     } }
    public override string ToString() {
      return "Consume(" +
        this.Property.Name + ",Extraction=" + this.Extraction.Name +
      ")";
    }    
  }

  // ... to consume an Entity
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
    public override string ToString() {
      return "Consume(" +
        this.Property.Name + ",Entity=" + this.Entity.Name +
      ")";
    }
  }

  public class ConsumeAll : ParseAction {
    public List<ParseAction> Actions { get; set; }
    public override string   Type   { get { return "ALL"; } }

    public override string Label {
      get {
        return string.Join( " ", this.Actions.Select(x => x.Label ));
      }
    }

    public ConsumeAll() {
      this.Actions = new List<ParseAction>();
    }

    public override string ToString() {
      return "Consume([" + 
        string.Join(",", this.Actions.Select(x => x.ToString())) +
      "]);";
    }
  }

  // given a set of possible ParseActions, this tries each of these ParseActions
  // and passes on the first that parses
  public class ConsumeAny : ParseAction {

    public List<ParseAction> Options { get; set; }

    public override string Label {
      get {
        return string.Join( " | ", this.Options.Select(x => x.Label ));
      }
    }

    // TODO reverse the dependency and make child-actions check their parent
    public override Property Property {
      get {
        if(this.Options.Count > 0) {
          return this.Options[0].Property;
        } else {
          return null;
        }
      }
      set {
        foreach(var option in this.Options) {
          option.Property = value;
        }
      }
    }

    public ConsumeAny() {
      this.Options = new List<ParseAction>();
    }

    public override string   Type   { get { return "ANY"; } }

    public override string ToString() {
      return "Consume([" + 
        string.Join("|", this.Options.Select(x => x.ToString())) +
      "]);";
    }

  }

  public class ConsumeOptional : ParseAction {

    public ParseAction Optional { get; set; }

    public override string Label {
      get {
        return "Optional(" + this.Optional.Label + ")";
      }
    }

    public override string   Type   { get { return "OPT"; } }

    public override string ToString() {
      return "Consume?(" + this.Optional.ToString() + ");";
    }
  }

  // the Model can be considered a Parser-AST on steroids. it contains all info
  // in such a way that a recursive descent parser can be constructed with ease
  public class Model {
    // the original rules from the Grammar
    public List<Rule> Rules;

    // the entities and extractions that are
    public Dictionary<string,Entity>       Entities;
    public Dictionary<string,Extraction>   Extractions;

    // the first entity to start parsing, which is the Entity of the first Rule
    public Entity Root;

    // factory method to import a HPG-BNF-like grammar, extract Extractions,
    // Entities and their properties and ParseActions
    public Model Import(Grammar grammar) {
      this.ImportRules(grammar);
      this.ExtractExtractions();
      this.ExtractEntities();
      this.ExtractPropertiesAndActions();
      return this;
    }

    // just import the rules as-is
    private void ImportRules(Grammar grammar) {
      if(grammar.Rules.Count < 1) {
        throw new ArgumentException("grammar contains no rules");
      }
      this.Rules = grammar.Rules;
    }

    private void ExtractEntities() {
      this.Entities = this.Rules
        .Where(rule => !( rule.Expression is ExtractorExpression) )
        .Select(rule => new Entity() {
          Name    = rule.Identifier,
          Rule    = rule
        })
        .ToDictionary(
          entity => entity.Name,
          entity => entity
        );
        this.Root = this.Entities[this.Rules[0].Identifier];
    }

    private void ExtractPropertiesAndActions() {
      foreach(KeyValuePair<string, Entity> entity in this.Entities) {
        entity.Value.ParseAction = this.ExtractPropertiesAndParseActions(
          entity.Value.Rule.Expression, entity.Value
        );
      }
    }
    
    private void ExtractExtractions() {
      this.Extractions = this.Rules
        .Where(rule => rule.Expression is ExtractorExpression)
        .Select(rule => new Extraction() {
          Name    = rule.Identifier,
          Pattern = ((ExtractorExpression)rule.Expression).Regex
        })
       .ToDictionary(
          extraction => extraction.Name,
          extraction => extraction
        );
    }

    // Properties and ParseActions Extraction methods

    private ParseAction ExtractPropertiesAndParseActions(Expression exp,
                                                         Entity     entity,
                                                         bool       optional=false)
    {
      try {
        return new Dictionary<string, Func<Expression,Entity,bool,ParseAction>>() {
          { "SequentialExpression",   this.ExtractSequentialExpression   },
          { "AlternativesExpression", this.ExtractAlternativesExpression },
          { "OptionalExpression",     this.ExtractOptionalExpression     },
          { "RepetitionExpression",   this.ExtractRepetitionExpression   },
          { "GroupExpression",        this.ExtractGroupExpression        },
          { "IdentifierExpression",   this.ExtractIdentifierExpression   },
          { "StringExpression",       this.ExtractStringExpression       },
          { "ExtractorExpression",    this.ExtractExtractorExpression    },
        }[exp.GetType().ToString().Replace("HumanParserGenerator.Grammars.", "")]
          (exp, entity, optional);
      } catch(KeyNotFoundException e) {
        throw new NotImplementedException(
          "extracting not implemented for " + exp.GetType().ToString(), e
        );
      }
    }

    // an ID-Exp part of an Entity rule Expression requires the creation of a
    // Property to store the Referred Entity or Extraction.
    private ParseAction ExtractIdentifierExpression(Expression exp,
                                                    Entity     entity,
                                                    bool       optional=false)
    {
      IdentifierExpression id = (IdentifierExpression)exp;

      Property property = this.CreatePropertyFor(id);
      entity.Add(property);

      return this.CreateConsumerFor(property, id);
    }

    private ParseAction ExtractStringExpression(Expression exp,
                                                Entity     entity,
                                                bool       optional=false)
    {
      // if a string is part of an optional expression, we generate a bool   
      // flag propety to indicate _if_ this (literal) string expression has
      // been parsed.
      // TODO
      // Property flag = new Property() {
      //
      // };

      // if it's just a string that always needs to be parsed to match the rule,
      // we just generate a ParseAction
      return new ConsumeLiteral() { Literal = ((StringExpression)exp).String };
    }
    
    private ParseAction ExtractExtractorExpression(Expression exp,
                                                   Entity     entity,
                                                   bool       optional=false)
    {
      // nothing TODO because they are already extracted at the beginning
      return null;
    }

    private ParseAction ExtractOptionalExpression(Expression exp,
                                                  Entity     entity,
                                                  bool       optional=false)
    {
      // recurse further, now marking the scope "optional" explicitly
      return new ConsumeOptional() {
        Optional = this.ExtractPropertiesAndParseActions(
          ((OptionalExpression)exp).Expression, entity, true
        )
      };
    }
    
    private ParseAction ExtractRepetitionExpression(Expression exp,
                                                    Entity     entity,
                                                    bool       optional=false)
    {
      var repetition = (RepetitionExpression)exp;

      // TODO: remove limitation to only single ID-Exp repetitions, currelty
      //       because of the "naming" -> generalize so that an expression has
      //       a (generated) name
      if( repetition.Expression is IdentifierExpression) {
        string id  = ((IdentifierExpression)repetition.Expression).Identifier;
        Property property = new Property() {
          Name     = id + "s",
          IsPlural = true
        };
        entity.Add(property);
        // the property is marked plural, so this action will be reused to parse
        // all occurences
        return new ConsumeEntity() {
          Entity   = this.Entities[id],
          Property = property
        };
      } else {
        throw new NotImplementedException("only single ID-exp can be repeated");
      }
    }

    // just recurse and provide a ParseAction for the Expression inside the
    // group
    private ParseAction ExtractGroupExpression(Expression exp,
                                               Entity     entity,
                                               bool       optional=false)
    {
      return this.ExtractPropertiesAndParseActions(
        ((GroupExpression)exp).Expression, entity, optional
      );
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
        Name = exp.Identifier,
        IsPlural = false
      };
    }

    private ParseAction CreateConsumerFor(Property property,
                                          IdentifierExpression exp)
    {
      Referable referred = this.GetReferred(exp.Identifier);
      if( referred is Entity ) {
        return new ConsumeEntity() {
          Property = property,
          Entity   = (Entity)referred
        };
      } else if( referred is Extraction ) {
        return new ConsumeExtraction() {
          Property   = property,
          Extraction = (Extraction)referred
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

    private ParseAction ExtractAlternativesExpression(Expression exp,
                                                      Entity     entity,
                                                      bool       optional=false)
    {
      // Alternative expressions should all return the same type.
      // We create a sigle property on the Entity to store the result.
      // The Type of this property must be a superclass for all alternatives.
      Property property = new Property() {
        Name     = "alternative",
        IsPlural = false
      };
      entity.Add(property);

      ConsumeAny consume = new ConsumeAny();

      // AlternativesExpressions are recursively constructed -> unroll
      // alternatives-expression ::=
      //              atomic-expression "|" non-sequential-expression;
      // non-sequential-expression ::= alternatives-expression
      //                             | atomic-expression
      //                             ;
      // atomic-expression ::= nested-expression
      //                     | terminal-expression
      //                     ;
      AlternativesExpression alternatives = (AlternativesExpression)exp;
      ParseAction action; // temporary helper action
      while(true) {
        // add AtomicExpression option
        action = this.ExtractPropertiesAndParseActions(
          alternatives.AtomicExpression, entity, optional
        );
        // replace the action's property with the common property
        // also remove the original action property from the entity
        entity.Properties.Remove(action.Property.Name);
        action.Property = property;
        consume.Options.Add(action);
        // recurse on the NonSequentialExpression part
        // case AtomicExpression
        if( alternatives.NonSequentialExpression is AtomicExpression) {
          // add AtomicExpression option
          action = this.ExtractPropertiesAndParseActions(
            alternatives.NonSequentialExpression, entity, optional
          );
          // replace the action's property with the common property
          // also remove the original action property from the entity
          entity.Properties.Remove(action.Property.Name);
          action.Property = property;
          consume.Options.Add(action);
          break;
        } else {
          alternatives =
            (AlternativesExpression)alternatives.NonSequentialExpression;
        }
      }
      return consume;
    }

    // a sequence consists of one or more Expressions that all are consumed
    // into properties of the entity
    private ParseAction ExtractSequentialExpression(Expression exp,
                                                    Entity     entity,
                                                    bool       optional=false)
    {
      ConsumeAll consume = new ConsumeAll();

      // SequentialExpression are recursively constructed -> unroll
      // sequential-expression ::= non-sequential-expression expression ;
      // non-sequential-expression ::= alternatives-expression
      //                             | atomic-expression
      //                             ;
      SequentialExpression sequence = (SequentialExpression)exp;
      while(true) {
        // add NonSequential sequence
        consume.Actions.Add(this.ExtractPropertiesAndParseActions(
          sequence.NonSequentialExpression, entity, optional
        ));
        // recurse on the Expression part
        // case NonSequential
        if( sequence.Expression is NonSequentialExpression) {
          // add final NonSequentialExpression action
          consume.Actions.Add(this.ExtractPropertiesAndParseActions(
            sequence.Expression, entity, optional
          ));
          break;
        } else {
          sequence =
            (SequentialExpression)sequence.Expression;
        }
      }

      return consume;
    }

    public override string ToString() {
      return
        "grammar rules\n-------------\n" +
        string.Join( "\n",
          this.Rules.Select(x => " * " + x.ToString())
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
