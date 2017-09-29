// Given a Parser Model, the Emitter generates CSharp code
// author: Christophe VG <contact@christophe.vg>
// revised by: Adam Simon <adamosimoni@gmail.com> 

using System;
using System.Text;

namespace HumanParserGenerator
{
    class CodeBuilder
    {
        readonly string _padding;
        int _indentLevel;

        public CodeBuilder() : this("    ") { }

        public CodeBuilder(string padding)
        {
            _padding = padding;
            StringBuilder = new StringBuilder();
        }

        public StringBuilder StringBuilder { get; }

        public void Indent()
        {
            _indentLevel++;
        }

        public void Unindent()
        {
            _indentLevel--;
        }

        public void AppendIndent()
        {
            for (var i = 0; i < _indentLevel; i++)
                StringBuilder.Append(_padding);
        }

        public void AppendIndented(string value)
        {
            AppendIndent();
            StringBuilder.Append(value);
        }

        public void AppendIndentedLine(string value)
        {
            AppendIndent();
            StringBuilder.AppendLine(value);
        }

        public void AppendBlockStart(string value = "{", bool indented = true)
        {
            if (indented)
                AppendIndentedLine(value);
            else
                AppendLine(value);

            Indent();
        }

        public void AppendBlockEnd(string value = "}", bool indented = true, bool appendEmptyLine = true)
        {
            Unindent();

            if (indented)
                AppendIndentedLine(value);
            else
                AppendLine(value);

            if (appendEmptyLine)
                StringBuilder.AppendLine();
        }

        public void AppendLine()
        {
            StringBuilder.AppendLine();
        }

        public void AppendLine(string value)
        {
            StringBuilder.AppendLine(value);
        }

        public void Append(string value)
        {
            StringBuilder.Append(value);
        }

        public void RemoveNewLine()
        {
            var n = Environment.NewLine.Length;
            if (StringBuilder.Length < n)
                return;

            for (int i = 0, j = StringBuilder.Length - n; i < n; i++, j++)
                if (Environment.NewLine[i] != StringBuilder[j])
                    return;

            StringBuilder.Remove(StringBuilder.Length - n, n);
        }

        public override string ToString()
        {
            return StringBuilder.ToString();
        }
    }
}
