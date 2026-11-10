#nullable enable
using Microsoft.CodeAnalysis;

namespace PlantsVsZombies.Tools.ProjectRefactor
{
    /// <summary>Describes the namespace and source file selected for a migrated type.</summary>
    internal sealed class TypeDestination
    {
        /// <summary>Initializes a destination for an original source type.</summary>
        /// <param name="symbol">The original source type.</param>
        /// <param name="namespaceName">The destination namespace.</param>
        /// <param name="path">The repository-relative source path.</param>
        public TypeDestination(INamedTypeSymbol symbol, string namespaceName, string path)
        {
            Symbol = symbol;
            NamespaceName = namespaceName;
            Path = path;
        }

        /// <summary>Gets the original source type.</summary>
        public INamedTypeSymbol Symbol { get; }

        /// <summary>Gets the destination namespace.</summary>
        public string NamespaceName { get; }

        /// <summary>Gets the repository-relative source path.</summary>
        public string Path { get; }

        /// <summary>Gets the fully qualified migrated type name.</summary>
        public string QualifiedName => "global::" + NamespaceName + "." + Symbol.Name;
    }
}
