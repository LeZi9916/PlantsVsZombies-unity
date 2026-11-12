#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PlantsVsZombies.Tools.ProjectRefactor
{
    /// <summary>Adds English XML documentation to previously undocumented declarations.</summary>
    internal sealed class DocumentationRewriter : CSharpSyntaxRewriter
    {
        /// <summary>Documents a declaration after visiting its nested members.</summary>
        /// <param name="node">The source syntax node.</param>
        /// <returns>The node with declaration documentation where needed.</returns>
        public override SyntaxNode? Visit(SyntaxNode? node)
        {
            if (node is null)
            {
                return null;
            }
            var rewritten = base.Visit(node);
            if (rewritten is not MemberDeclarationSyntax member || member is NamespaceDeclarationSyntax
                || member.GetLeadingTrivia().Any(trivia => trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia)
                    || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia)))
            {
                return rewritten;
            }
            var name = GetName(member);
            if (name.Length == 0)
            {
                return rewritten;
            }
            var purpose = Words(name);
            var summary = member switch
            {
                ConstructorDeclarationSyntax => "Initializes " + purpose + ".",
                DestructorDeclarationSyntax => "Releases resources owned by " + purpose + ".",
                FieldDeclarationSyntax => "Stores " + purpose + ".",
                EventFieldDeclarationSyntax or EventDeclarationSyntax => "Notifies listeners about " + purpose + ".",
                PropertyDeclarationSyntax => "Gets or sets " + purpose + ".",
                BaseTypeDeclarationSyntax => "Defines " + purpose + ".",
                EnumMemberDeclarationSyntax => "Identifies " + purpose + ".",
                _ => "Performs " + purpose + "."
            };
            var documentation = "/// <summary>" + summary + "</summary>\r\n";
            foreach (var parameter in Parameters(member))
            {
                documentation += "/// <param name=\"" + parameter.Identifier.ValueText + "\">The " + Words(parameter.Identifier.ValueText) + ".</param>\r\n";
            }
            if (member is MethodDeclarationSyntax method && !method.ReturnType.IsKind(SyntaxKind.PredefinedType)
                || member is MethodDeclarationSyntax { ReturnType: PredefinedTypeSyntax predefined } && !predefined.Keyword.IsKind(SyntaxKind.VoidKeyword)
                || member is OperatorDeclarationSyntax or ConversionOperatorDeclarationSyntax or IndexerDeclarationSyntax)
            {
                documentation += "/// <returns>The resulting " + purpose + ".</returns>\r\n";
            }
            foreach (var exception in (member is BaseMethodDeclarationSyntax ? member.DescendantNodes().OfType<ThrowStatementSyntax>() : Array.Empty<ThrowStatementSyntax>())
                .Select(statement => statement.Expression).OfType<ObjectCreationExpressionSyntax>()
                .Select(expression => expression.Type.ToString()).Distinct(StringComparer.Ordinal))
            {
                documentation += "/// <exception cref=\"" + exception + "\">The operation cannot satisfy its preconditions.</exception>\r\n";
            }
            return member.WithLeadingTrivia(member.GetLeadingTrivia().AddRange(SyntaxFactory.ParseLeadingTrivia(documentation)));
        }

        /// <summary>Gets a declaration's readable identifier.</summary>
        /// <param name="member">The declaration.</param>
        /// <returns>The declaration identifier or an empty string.</returns>
        private static string GetName(MemberDeclarationSyntax member) => member switch
        {
            BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
            MethodDeclarationSyntax method => method.Identifier.ValueText,
            ConstructorDeclarationSyntax constructor => constructor.Identifier.ValueText,
            DestructorDeclarationSyntax destructor => destructor.Identifier.ValueText,
            PropertyDeclarationSyntax property => property.Identifier.ValueText,
            FieldDeclarationSyntax field => string.Join(", ", field.Declaration.Variables.Select(variable => variable.Identifier.ValueText)),
            EventFieldDeclarationSyntax field => string.Join(", ", field.Declaration.Variables.Select(variable => variable.Identifier.ValueText)),
            EventDeclarationSyntax @event => @event.Identifier.ValueText,
            EnumMemberDeclarationSyntax value => value.Identifier.ValueText,
            DelegateDeclarationSyntax @delegate => @delegate.Identifier.ValueText,
            IndexerDeclarationSyntax => "indexed value",
            OperatorDeclarationSyntax => "operator value",
            ConversionOperatorDeclarationSyntax => "converted value",
            _ => ""
        };

        /// <summary>Gets the explicit parameters of a member.</summary>
        /// <param name="member">The declaration.</param>
        /// <returns>The member parameters.</returns>
        private static IEnumerable<ParameterSyntax> Parameters(MemberDeclarationSyntax member) => member switch
        {
            BaseMethodDeclarationSyntax method => method.ParameterList.Parameters,
            DelegateDeclarationSyntax @delegate => @delegate.ParameterList.Parameters,
            IndexerDeclarationSyntax indexer => indexer.ParameterList.Parameters,
            _ => Array.Empty<ParameterSyntax>()
        };

        /// <summary>Turns an identifier into a short English noun phrase.</summary>
        /// <param name="value">The declaration identifier.</param>
        /// <returns>The identifier split into lowercase words.</returns>
        private static string Words(string value)
        {
            var text = value.TrimStart('_');
            if (text.StartsWith("s_", StringComparison.Ordinal))
            {
                text = text[2..];
            }
            return Regex.Replace(text, "([a-z0-9])([A-Z])", "$1 $2").Replace('_', ' ').ToLowerInvariant();
        }
    }
}
