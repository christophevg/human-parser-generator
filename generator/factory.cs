// This Factory builds a Parser Generator Model given an EBNF-like Grammar/AST
// author: Christophe VG <contact@christophe.vg>

using System;
using System.IO;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

using System.Diagnostics;
using System.Collections.ObjectModel;

namespace HumanParserGenerator.Generator {

  public class ModelException : System.Exception {
    public ModelException() : base() { }
    public ModelException(string message) : base(message) { }
    public ModelException(string message, System.Exception inner) : base(message, inner) { }
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

      // step 1:
      this.ImportPropertiesAndActions();

      // step 2:
      this.CollapseAlternatives();
      
      // step 3:
      this.DetectInheritance();
      
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
          { "AlternativesExpression", this.ImportAlternativesExpression },
          { "SequentialExpression",   this.ImportSequentialExpression   },
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
        // TODO improve and extract
        if(name.Equals("-")) { name = "dash"; }
        name = name.Replace("(", "open-bracket");
        name = name.Replace(")", "close-bracket");
        name = name.Replace("[", "open-square-bracket");
        name = name.Replace("]", "close-square-bracket");
        name = name.Replace("{", "open-brace");
        name = name.Replace("}", "close-brace");
        name = name.Replace(":", "colon");
        name = name.Replace("=", "equals");
        name = name.Replace("&", "ampersand");
        name = name.Replace("<", "less-than");
        name = name.Replace(">", "greater-than");
        name = name.Replace("+", "plus");
        name = name.Replace("*", "star");
        name = name.Replace("!", "exclamation");
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

      if( ! this.Model.Contains(id.Identifier) ) {
        throw new ModelException("Unknown Entity reference: " + id.Identifier);
      }

      return this.Add(
        entity,
        new Property() { Name = id.Name != null ? id.Name : id.Identifier },
        new ConsumeEntity() { Reference = id.Identifier, Parent = parent }
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
          alternative.NonAlternativesExpression, entity, consume, optional
        ));
        // add remaining parts
        if(alternative.Expression is NonAlternativesExpression) {
          // last part
          consume.Actions.Add(this.ImportPropertiesAndParseActions(
            alternative.Expression, entity, consume, optional
          ));
          break;
        } else {
          // recurse
          alternative =
            (AlternativesExpression)alternative.Expression;
        }
      }

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
          sequence.AtomicExpression, entity, consume, optional
        ));
        // add remaining parts
        if(sequence.NonAlternativesExpression is AtomicExpression) {
          // last part
          consume.Actions.Add(this.ImportPropertiesAndParseActions(
            sequence.NonAlternativesExpression, entity, consume, optional
          ));
          break;
        } else {
          // recurse
          sequence = (SequentialExpression)sequence.NonAlternativesExpression;
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

      // all properties should be Consuming Entities
      if( entity.Properties.Where(p=>p.Source is ConsumeEntity).ToList().Count() != entity.Properties.Count) {
        return;
      }

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
          t += " : " + string.Join(",", entity.Supers);
        }
        if(entity.Subs.Count > 0) {
          t += " <|-- " + string.Join(",", entity.Subs);
        }
        this.Log(t);
      }
      this.Log("---------------------------------------------");
    }

    private void DetectInheritance(Entity entity) {
      if(entity.Properties == null ) {
        Console.Error.WriteLine("properties is null!!!");
        return;
      }
      
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
          // TODO simplify this ;-)
          if(
             entity.Properties.First().Source is ConsumeEntity &&
             (
               !(((ConsumeEntity)entity.Properties.First().Source).Entity.ParseAction is ConsumePattern)
               || entity.Supers.Count == 0
             )
          ){
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
      parent.Subs.Add(child.Name);
      child.Supers.Add(parent.Name);
      this.Log(parent.Name + " <|-- " + child.Name);
    }

    // Factory helper methods

    [ConditionalAttribute("DEBUG")]
    private void Log(string msg) {
      Console.Error.WriteLine("Factory: " + msg );
    }

    private void LogError(string msg) {
      Console.Error.WriteLine("Factory: ERROR: " + msg );
    }
  }

}
