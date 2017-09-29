using System;
using System.Collections.Generic;
using System.Text;

namespace HumanParserGenerator
{
    class AstPrinter : Visitor
    {
        readonly CodeBuilder _builder;

        public AstPrinter(CodeBuilder builder)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            _builder = builder;
        }

        string FormatString(string value)
        {
            return value ?? "null";
        }

        void InsertComma()
        {
            _builder.StringBuilder.Insert(_builder.StringBuilder.Length - 2, ",");
        }

        protected override void Visit(ISyntaxNode node, ISyntaxNode parent)
        {
            if (node == null)
                _builder.Append("null");
            else
                base.Visit(node, parent);
        }

        public override void VisitGrammar(Grammar node)
        {
            _builder.AppendBlockStart(indented: false);

            _builder.AppendBlockStart($"\"{nameof(node.Rules)}\": [");
            for (var i = 0; i < node.Rules.Count; i++)
            {
                _builder.AppendIndent();
                Visit(node.Rules[i], node);
                if (i < node.Rules.Count - 1)
                    InsertComma();
            }
            _builder.AppendBlockEnd("]", appendEmptyLine: false);

            _builder.AppendBlockEnd(appendEmptyLine: false);
        }

        public override void VisitRule(Rule node)
        {
            _builder.AppendBlockStart(indented: false);

            _builder.AppendIndented($"\"{nameof(node.Expression)}\": ");
            Visit(node.Expression, node);

            InsertComma();

            _builder.AppendIndentedLine($"\"{nameof(node.Identifier)}\": \"{FormatString(node.Identifier)}\"");

            _builder.AppendBlockEnd(appendEmptyLine: false);
        }

        public override void VisitAlternativesExpression(AlternativesExpression node)
        {
            _builder.AppendBlockStart(indented: false);

            _builder.AppendIndented($"\"{nameof(node.AtomicExpression)}\": ");
            Visit(node.AtomicExpression, node);

            InsertComma();

            _builder.AppendIndented($"\"{nameof(node.NonSequentialExpression)}\": ");
            Visit(node.NonSequentialExpression, node);

            _builder.AppendBlockEnd(appendEmptyLine: false);
        }

        public override void VisitExtractorExpression(ExtractorExpression node)
        {
            _builder.AppendBlockStart(indented: false);

            _builder.AppendIndentedLine($"\"{nameof(node.Name)}\": \"{FormatString(node.Name)}\"");

            InsertComma();

            _builder.AppendIndentedLine($"\"{nameof(node.Pattern)}\": \"{FormatString(node.Pattern)}\"");

            _builder.AppendBlockEnd(appendEmptyLine: false);
        }

        public override void VisitGroupExpression(GroupExpression node)
        {
            _builder.AppendBlockStart(indented: false);

            _builder.AppendIndented($"\"{nameof(node.Expression)}\": ");
            Visit(node.Expression, node);

            _builder.AppendBlockEnd(appendEmptyLine: false);
        }

        public override void VisitIdentifierExpression(IdentifierExpression node)
        {
            _builder.AppendBlockStart(indented: false);

            _builder.AppendIndentedLine($"\"{nameof(node.Name)}\": \"{FormatString(node.Name)}\"");

            InsertComma();

            _builder.AppendIndentedLine($"\"{nameof(node.Identifier)}\": \"{FormatString(node.Identifier)}\"");

            _builder.AppendBlockEnd(appendEmptyLine: false);
        }

        public override void VisitOptionalExpression(OptionalExpression node)
        {
            _builder.AppendBlockStart(indented: false);

            _builder.AppendIndented($"\"{nameof(node.Expression)}\": ");
            Visit(node.Expression, node);

            _builder.AppendBlockEnd(appendEmptyLine: false);
        }

        public override void VisitRepetitionExpression(RepetitionExpression node)
        {
            _builder.AppendBlockStart(indented: false);

            _builder.AppendIndented($"\"{nameof(node.Expression)}\": ");
            Visit(node.Expression, node);

            _builder.AppendBlockEnd(appendEmptyLine: false);
        }

        public override void VisitSequentialExpression(SequentialExpression node)
        {
            _builder.AppendBlockStart(indented: false);

            _builder.AppendIndented($"\"{nameof(node.Expression)}\": ");
            Visit(node.Expression, node);

            InsertComma();

            _builder.AppendIndented($"\"{nameof(node.NonSequentialExpression)}\": ");
            Visit(node.Expression, node);

            _builder.AppendBlockEnd(appendEmptyLine: false);
        }

        public override void VisitStringExpression(StringExpression node)
        {
            _builder.AppendBlockStart(indented: false);

            _builder.AppendIndentedLine($"\"{nameof(node.Name)}\": \"{FormatString(node.Name)}\"");

            InsertComma();

            _builder.AppendIndentedLine($"\"{nameof(node.String)}\": \"{FormatString(node.String)}\"");

            _builder.AppendBlockEnd(appendEmptyLine: false);
        }
    }
}
