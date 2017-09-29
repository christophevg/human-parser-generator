// code formatting support
// author: Christophe VG <contact@christophe.vg>
// revised by: Adam Simon <adamosimoni@gmail.com>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using HumanParserGenerator.Generator;

namespace HumanParserGenerator.Emitter.Format
{
    public class CSharp
    {
        public static string Class(Entity entity)
        {
            return PascalCase(entity.Name);
        }

        public static string Type(Entity entity)
        {
            return Type(entity.Type);
        }

        public static string Type(Property property)
        {
            if (property.IsPlural || property.Source.HasPluralParent)
            {
                return string.Concat("List<", Type(property.Type), ">");
            }
            return Type(property.Type);
        }

        // wrapping formatting functions
        // use these to correctly format variables in their functional context

        // function to make sure that Properties don't have the same name as their
        // Class.
        // this is most of the time due to some recursion in a rule
        // e.g. rule ::= something [ rule ]
        public static string Property(Property property)
        {
            var name = property.Name;

            if (property.Name.Equals(property.Entity.Name))
            {
                // FIX: rewritten name was not pluralized
                Warn("rewriting property name: " + property.Name);
                name = "next-" + property.Name;
            }

            return PascalCase(!property.IsPlural && !property.Source.HasPluralParent ?
                name :
                Pluralize(name));
        }

        public static string Variable(Property property)
        {
            var name = property.Name;
            // QnD solution to reserved words
            if (name.Equals("string")) { return "text"; }
            if (name.Equals("int")) { return "number"; }
            if (name.Equals("float")) { return "floating"; }

            return CamelCase(!property.IsPlural && !property.Source.HasPluralParent ?
                name :
                Pluralize(name));
        }

        public static string VerbatimStringLiteral(string text)
        {
            return string.Concat("@\"", text.Replace("\"", "\"\""), "\"");
        }

        // technical formatting functions
        // don't use these in emitter code, only use them from functional formatting
        // wrappers

        private static string Type(string type)
        {
            if (type == null) { return "Object"; }
            if (type.Equals("<string>")) { return "string"; }
            if (type.Equals("<bool>")) { return "bool"; }

            return PascalCase(type);
        }

        // this function makes sure that text is correctly case'd ;-)
        // Dashes are removed and the first letter of each part is uppercased
        private static string PascalCase(string text)
        {
            return string.Join("",
              text.Split('-').Select(x =>
                x.First().ToString().ToUpper() + x.ToLower().Substring(1)
              )
            );
        }

        private static string CamelCase(string text)
        {
            var x = PascalCase(text);
            return x.First().ToString().ToLower() + x.Substring(1);
        }

        static readonly Dictionary<string, string> pluralExceptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "man", "men" },
            { "woman", "women" },
            { "child", "children" },
            { "tooth", "teeth" },
            { "foot", "feet" },
            { "mouse", "mice" },
            { "belief", "beliefs" }
         };

        // author: andrewjk
        // source: https://gist.github.com/andrewjk/3186582
        private static string Pluralize(string text)
        {
            if (pluralExceptions.TryGetValue(text, out string plural))
                return plural;

            if (text.EndsWith("y", StringComparison.OrdinalIgnoreCase) &&
                !text.EndsWith("ay", StringComparison.OrdinalIgnoreCase) &&
                !text.EndsWith("ey", StringComparison.OrdinalIgnoreCase) &&
                !text.EndsWith("iy", StringComparison.OrdinalIgnoreCase) &&
                !text.EndsWith("oy", StringComparison.OrdinalIgnoreCase) &&
                !text.EndsWith("uy", StringComparison.OrdinalIgnoreCase))
            {
                return text.Substring(0, text.Length - 1) + "ies";
            }
            else if (text.EndsWith("us", StringComparison.InvariantCultureIgnoreCase))
            {
                // http://en.wikipedia.org/wiki/Plural_form_of_words_ending_in_-us
                return text + "es";
            }
            else if (text.EndsWith("ss", StringComparison.InvariantCultureIgnoreCase))
            {
                return text + "es";
            }
            else if (text.EndsWith("s", StringComparison.InvariantCultureIgnoreCase))
            {
                return text;
            }
            else if (text.EndsWith("x", StringComparison.InvariantCultureIgnoreCase) ||
                text.EndsWith("ch", StringComparison.InvariantCultureIgnoreCase) ||
                text.EndsWith("sh", StringComparison.InvariantCultureIgnoreCase))
            {
                return text + "es";
            }
            else if (text.EndsWith("f", StringComparison.InvariantCultureIgnoreCase) && text.Length > 1)
            {
                return text.Substring(0, text.Length - 1) + "ves";
            }
            else if (text.EndsWith("fe", StringComparison.InvariantCultureIgnoreCase) && text.Length > 2)
            {
                return text.Substring(0, text.Length - 2) + "ves";
            }
            else
            {
                return text + "s";
            }
        }

        // logging functionality

        private static void Warn(string msg)
        {
            Log("warning: " + msg);
        }

        private static void Log(string msg)
        {
            Console.Error.WriteLine("hpg-emitter: " + msg);
        }
    }

}