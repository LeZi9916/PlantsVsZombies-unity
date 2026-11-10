#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PlantsVsZombies.Tools.ProjectRefactor
{
    /// <summary>Rewrites declarations and references against the original semantic model.</summary>
    internal sealed class MigrationRewriter : CSharpSyntaxRewriter
    {
        /// <summary>Stores the original semantic model.</summary>
        private readonly SemanticModel _model;

        /// <summary>Stores migrated type names.</summary>
        private readonly IReadOnlyDictionary<ISymbol, TypeDestination> _destinations;

        /// <summary>Stores identifier replacements keyed by original symbols.</summary>
        private readonly IReadOnlyDictionary<ISymbol, string> _renames;

        /// <summary>Stores the declaration currently being extracted.</summary>
        private readonly SyntaxNode _entry;

        /// <summary>Initializes a semantic source rewriter.</summary>
        /// <param name="model">The original semantic model.</param>
        /// <param name="destinations">The migrated type destinations.</param>
        /// <param name="renames">The identifier changes.</param>
        /// <param name="entry">The declaration currently being extracted.</param>
        public MigrationRewriter(SemanticModel model, IReadOnlyDictionary<ISymbol, TypeDestination> destinations,
            IReadOnlyDictionary<ISymbol, string> renames, SyntaxNode entry)
        {
            _model = model;
            _destinations = destinations;
            _renames = renames;
            _entry = entry;
        }

        /// <summary>Removes extracted public nested classes from their original owner.</summary>
        /// <param name="node">The class declaration.</param>
        /// <returns>The rewritten class or null when independently extracted.</returns>
        public override SyntaxNode? VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            if (ShouldExtract(node))
            {
                return null;
            }
            return base.VisitClassDeclaration(node);
        }

        /// <summary>Removes extracted public nested structs from their original owner.</summary>
        /// <param name="node">The struct declaration.</param>
        /// <returns>The rewritten struct or null when independently extracted.</returns>
        public override SyntaxNode? VisitStructDeclaration(StructDeclarationSyntax node)
        {
            if (ShouldExtract(node))
            {
                return null;
            }
            return base.VisitStructDeclaration(node);
        }

        /// <summary>Checks whether a nested declaration has its own destination.</summary>
        /// <param name="node">The type declaration.</param>
        /// <returns>Whether the declaration should be removed from its owner.</returns>
        private bool ShouldExtract(BaseTypeDeclarationSyntax node) => node != _entry
            && _model.GetDeclaredSymbol(node) is INamedTypeSymbol symbol && symbol.ContainingType is not null
            && _destinations.ContainsKey(symbol);

        /// <summary>Rewrites namespaces in imports while preserving aliases.</summary>
        /// <param name="node">The original import.</param>
        /// <returns>The migrated import.</returns>
        public override SyntaxNode? VisitUsingDirective(UsingDirectiveSyntax node)
        {
            if (node.Name is null)
            {
                return node;
            }
            var value = SourceMigration.RewriteNamespace(node.Name.ToString());
            return node.WithName(SyntaxFactory.ParseName(value));
        }

        /// <summary>Rewrites fully qualified type and namespace names.</summary>
        /// <param name="node">The qualified name.</param>
        /// <returns>The migrated name.</returns>
        public override SyntaxNode? VisitQualifiedName(QualifiedNameSyntax node)
        {
            var symbol = _model.GetSymbolInfo(node).Symbol;
            if (symbol is INamedTypeSymbol type && TryDestination(type, out var destination))
            {
                return SyntaxFactory.ParseName(destination.QualifiedName).WithTriviaFrom(node);
            }
            if (symbol is INamespaceSymbol namespaceSymbol && SourceMigration.IsProjectNamespace(namespaceSymbol.ToDisplayString()))
            {
                return SyntaxFactory.ParseName(SourceMigration.RewriteNamespace(namespaceSymbol.ToDisplayString())).WithTriviaFrom(node);
            }
            return base.VisitQualifiedName(node);
        }

        /// <summary>Rewrites qualified type receivers used in expressions.</summary>
        /// <param name="node">The member access.</param>
        /// <returns>The migrated member access or qualified type name.</returns>
        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            var symbol = _model.GetSymbolInfo(node).Symbol;
            if (symbol is INamedTypeSymbol type && TryDestination(type, out var destination))
            {
                return SyntaxFactory.ParseExpression(destination.QualifiedName).WithTriviaFrom(node);
            }
            if (symbol is INamespaceSymbol namespaceSymbol && SourceMigration.IsProjectNamespace(namespaceSymbol.ToDisplayString()))
            {
                return SyntaxFactory.ParseExpression(SourceMigration.RewriteNamespace(namespaceSymbol.ToDisplayString())).WithTriviaFrom(node);
            }
            return base.VisitMemberAccessExpression(node);
        }

        /// <summary>Rewrites bound identifier references.</summary>
        /// <param name="node">The original identifier.</param>
        /// <returns>The renamed identifier.</returns>
        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            var symbol = _model.GetSymbolInfo(node).Symbol;
            if (symbol is INamedTypeSymbol { Name: "Sun" } type && TryDestination(type, out var destination)
                && destination.NamespaceName == "PlantsVsZombies.Simulation")
            {
                return SyntaxFactory.ParseName(destination.QualifiedName).WithTriviaFrom(node);
            }
            if (symbol is not null && _renames.TryGetValue(symbol, out var name))
            {
                return node.WithIdentifier(Identifier(name).WithTriviaFrom(node.Identifier));
            }
            if (symbol is INamespaceSymbol namespaceSymbol && namespaceSymbol.Name is "PvZReplica" or "PvZUnity")
            {
                return SyntaxFactory.IdentifierName("PlantsVsZombies").WithTriviaFrom(node);
            }
            return base.VisitIdentifierName(node);
        }

        /// <summary>Rewrites declared local and field identifiers.</summary>
        /// <param name="node">The variable declaration.</param>
        /// <returns>The rewritten declaration.</returns>
        public override SyntaxNode? VisitVariableDeclarator(VariableDeclaratorSyntax node)
        {
            var symbol = _model.GetDeclaredSymbol(node);
            var rewritten = (VariableDeclaratorSyntax)base.VisitVariableDeclarator(node)!;
            return symbol is not null && _renames.TryGetValue(symbol, out var name)
                ? rewritten.WithIdentifier(Identifier(name).WithTriviaFrom(node.Identifier)) : rewritten;
        }

        /// <summary>Rewrites declared parameter identifiers.</summary>
        /// <param name="node">The parameter declaration.</param>
        /// <returns>The rewritten parameter.</returns>
        public override SyntaxNode? VisitParameter(ParameterSyntax node)
        {
            var symbol = _model.GetDeclaredSymbol(node);
            var rewritten = (ParameterSyntax)base.VisitParameter(node)!;
            return symbol is not null && _renames.TryGetValue(symbol, out var name)
                ? rewritten.WithIdentifier(Identifier(name).WithTriviaFrom(node.Identifier)) : rewritten;
        }

        /// <summary>Rewrites declared pattern and deconstruction variables.</summary>
        /// <param name="node">The variable designation.</param>
        /// <returns>The renamed declaration.</returns>
        public override SyntaxNode? VisitSingleVariableDesignation(SingleVariableDesignationSyntax node)
        {
            var symbol = _model.GetDeclaredSymbol(node);
            var rewritten = (SingleVariableDesignationSyntax)base.VisitSingleVariableDesignation(node)!;
            return symbol is not null && _renames.TryGetValue(symbol, out var name)
                ? rewritten.WithIdentifier(Identifier(name).WithTriviaFrom(node.Identifier)) : rewritten;
        }

        /// <summary>Rewrites declared exception variables.</summary>
        /// <param name="node">The exception variable declaration.</param>
        /// <returns>The renamed declaration.</returns>
        public override SyntaxNode? VisitCatchDeclaration(CatchDeclarationSyntax node)
        {
            var symbol = _model.GetDeclaredSymbol(node);
            var rewritten = (CatchDeclarationSyntax)base.VisitCatchDeclaration(node)!;
            return symbol is not null && _renames.TryGetValue(symbol, out var name)
                ? rewritten.WithIdentifier(Identifier(name).WithTriviaFrom(node.Identifier)) : rewritten;
        }

        /// <summary>Rewrites declared foreach identifiers and expands embedded statements.</summary>
        /// <param name="node">The foreach statement.</param>
        /// <returns>The rewritten loop.</returns>
        public override SyntaxNode? VisitForEachStatement(ForEachStatementSyntax node)
        {
            var symbol = _model.GetDeclaredSymbol(node);
            var rewritten = (ForEachStatementSyntax)base.VisitForEachStatement(node)!;
            if (symbol is not null && _renames.TryGetValue(symbol, out var name))
            {
                rewritten = rewritten.WithIdentifier(Identifier(name).WithTriviaFrom(node.Identifier));
            }
            return rewritten.WithStatement(Block(rewritten.Statement));
        }

        /// <summary>Expands embedded if statements into explicit blocks.</summary>
        /// <param name="node">The if statement.</param>
        /// <returns>The block-form statement.</returns>
        public override SyntaxNode? VisitIfStatement(IfStatementSyntax node)
        {
            var rewritten = (IfStatementSyntax)base.VisitIfStatement(node)!;
            var otherwise = rewritten.Else;
            if (otherwise is not null && otherwise.Statement is not IfStatementSyntax)
            {
                otherwise = otherwise.WithStatement(Block(otherwise.Statement));
            }
            return rewritten.WithStatement(Block(rewritten.Statement)).WithElse(otherwise);
        }

        /// <summary>Expands embedded for statements into explicit blocks.</summary>
        /// <param name="node">The for statement.</param>
        /// <returns>The block-form loop.</returns>
        public override SyntaxNode? VisitForStatement(ForStatementSyntax node)
        {
            var rewritten = (ForStatementSyntax)base.VisitForStatement(node)!;
            return rewritten.WithStatement(Block(rewritten.Statement));
        }

        /// <summary>Expands embedded while statements into explicit blocks.</summary>
        /// <param name="node">The while statement.</param>
        /// <returns>The block-form loop.</returns>
        public override SyntaxNode? VisitWhileStatement(WhileStatementSyntax node)
        {
            var rewritten = (WhileStatementSyntax)base.VisitWhileStatement(node)!;
            return rewritten.WithStatement(Block(rewritten.Statement));
        }

        /// <summary>Expands embedded do statements into explicit blocks.</summary>
        /// <param name="node">The do statement.</param>
        /// <returns>The block-form loop.</returns>
        public override SyntaxNode? VisitDoStatement(DoStatementSyntax node)
        {
            var rewritten = (DoStatementSyntax)base.VisitDoStatement(node)!;
            return rewritten.WithStatement(Block(rewritten.Statement));
        }

        /// <summary>Expands embedded using statements into explicit blocks.</summary>
        /// <param name="node">The using statement.</param>
        /// <returns>The block-form statement.</returns>
        public override SyntaxNode? VisitUsingStatement(UsingStatementSyntax node)
        {
            var rewritten = (UsingStatementSyntax)base.VisitUsingStatement(node)!;
            return rewritten.WithStatement(Block(rewritten.Statement));
        }

        /// <summary>Converts inferable local variable declarations to var.</summary>
        /// <param name="node">The declaration.</param>
        /// <returns>The inferred declaration when safe.</returns>
        public override SyntaxNode? VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            var rewritten = (VariableDeclarationSyntax)base.VisitVariableDeclaration(node)!;
            if (node.Parent is LocalDeclarationStatementSyntax or ForStatementSyntax or UsingStatementSyntax
                && !(node.Parent is LocalDeclarationStatementSyntax local && local.Modifiers.Any(SyntaxKind.ConstKeyword))
                && node.Variables.Count == 1 && node.Variables[0].Initializer is { Value: var value }
                && !value.IsKind(SyntaxKind.NullLiteralExpression) && !value.IsKind(SyntaxKind.DefaultLiteralExpression)
                && value is not LambdaExpressionSyntax && value is not AnonymousMethodExpressionSyntax
                && !node.Type.IsVar)
            {
                var declaredType = _model.GetTypeInfo(node.Type).Type;
                var valueType = _model.GetTypeInfo(value).Type;
                if (declaredType is not null && SymbolEqualityComparer.Default.Equals(declaredType, valueType))
                {
                    return rewritten.WithType(SyntaxFactory.IdentifierName("var").WithTriviaFrom(rewritten.Type));
                }
            }
            return rewritten;
        }

        /// <summary>Finds the destination of a source type definition.</summary>
        /// <param name="type">The type reference.</param>
        /// <param name="destination">The mapped destination.</param>
        /// <returns>Whether the type is moved.</returns>
        private bool TryDestination(INamedTypeSymbol type, out TypeDestination destination)
            => _destinations.TryGetValue(type.OriginalDefinition, out destination!);

        /// <summary>Wraps a statement in a block when required.</summary>
        /// <param name="statement">The embedded statement.</param>
        /// <returns>A block containing the statement.</returns>
        private static BlockSyntax Block(StatementSyntax statement) => statement as BlockSyntax ?? SyntaxFactory.Block(statement);

        /// <summary>Creates a keyword-safe identifier token.</summary>
        /// <param name="value">The identifier text.</param>
        /// <returns>The escaped identifier token.</returns>
        private static SyntaxToken Identifier(string value) => SyntaxFactory.Identifier(SyntaxFacts.GetKeywordKind(value) == SyntaxKind.None ? value : "@" + value);
    }
}
