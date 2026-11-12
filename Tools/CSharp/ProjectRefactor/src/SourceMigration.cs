#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PlantsVsZombies.Tools.ProjectRefactor
{
    /// <summary>Plans a symbol-aware, one-type-per-file migration of the Unity sources.</summary>
    internal sealed class SourceMigration
    {
        /// <summary>Stores the repository root.</summary>
        private readonly string _root;

        /// <summary>Stores each migrated source type's destination.</summary>
        private readonly Dictionary<ISymbol, TypeDestination> _destinations = new(SymbolEqualityComparer.Default);

        /// <summary>Stores symbols whose identifiers need editorconfig naming.</summary>
        private readonly Dictionary<ISymbol, string> _renames = new(SymbolEqualityComparer.Default);

        /// <summary>Initializes a migration for a repository.</summary>
        /// <param name="root">The absolute repository root.</param>
        public SourceMigration(string root)
        {
            _root = root;
        }

        /// <summary>Validates a complete migration and optionally applies it.</summary>
        /// <param name="apply">Whether to write migrated source and asset metadata.</param>
        /// <param name="cleanupImports">Whether to remove compiler-confirmed unnecessary imports only.</param>
        /// <exception cref="InvalidOperationException">The migration is ambiguous or produces invalid syntax.</exception>
        public void Run(bool apply, bool cleanupImports = false)
        {
            var responses = FindCompilerResponses();
            var lines = responses.SelectMany(File.ReadAllLines).ToArray();
            var defines = lines.Where(line => line.StartsWith("-define:", StringComparison.Ordinal))
                .Select(line => line[8..]).Distinct(StringComparer.Ordinal);
            var options = new CSharpParseOptions(LanguageVersion.CSharp9, preprocessorSymbols: defines);
            var paths = Directory.EnumerateFiles(Path.Combine(_root, "Assets"), "*.cs", SearchOption.AllDirectories)
                .Where(path => !Path.GetRelativePath(_root, path).Replace('\\', '/').Contains("/OriginalPvZ/", StringComparison.Ordinal))
                .Where(path => !Path.GetRelativePath(_root, path).Replace('\\', '/').StartsWith("Assets/Tests/", StringComparison.Ordinal))
                .OrderBy(path => path, StringComparer.Ordinal).ToArray();
            var trees = paths.Select(path => CSharpSyntaxTree.ParseText(File.ReadAllText(path), options, path)).ToArray();
            var referencePaths = lines.Where(line => line.StartsWith("-r:", StringComparison.Ordinal))
                .Select(line => line[3..].Trim('"')).Select(path => Path.GetFullPath(path, _root))
                .Where(File.Exists)
                .Where(path => Path.GetFileNameWithoutExtension(path) is not "PlantsVsZombies.Runtime" and not "PlantsVsZombies.Editor"
                    and not "Assembly-CSharp" and not "Assembly-CSharp-Editor")
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (referencePaths.Length == 0)
            {
                referencePaths = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? typeof(object).Assembly.Location)
                    .Split(Path.PathSeparator);
            }
            var references = referencePaths.Select(path => MetadataReference.CreateFromFile(path)).ToArray();
            var compilation = CSharpCompilation.Create("SourceMigration", trees, references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
            if (cleanupImports)
            {
                CleanImports(compilation, apply);
                return;
            }
            CollectTypes(compilation);
            CollectRenames(compilation);
            var output = CreateOutput(compilation, options);
            var migratedPaths = _destinations.Values.SelectMany(item => item.Symbol.DeclaringSyntaxReferences)
                .Select(reference => reference.SyntaxTree.FilePath).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var reportPath = Path.Combine(_root, "Temp", "ProjectRefactor", "migration-plan.json");
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath)!);
            var baselineErrors = compilation.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error).ToArray();
            var migratedTrees = output.Select(pair => CSharpSyntaxTree.ParseText(pair.Value, options, pair.Key)).ToArray();
            var migrated = compilation.RemoveSyntaxTrees(compilation.SyntaxTrees.Where(tree => migratedPaths.Contains(tree.FilePath)))
                .AddSyntaxTrees(migratedTrees);
            var migratedErrors = migrated.GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error).ToArray();
            File.WriteAllLines(Path.Combine(Path.GetDirectoryName(reportPath)!, "baseline-diagnostics.txt"), baselineErrors.Select(item => item.ToString()));
            File.WriteAllLines(Path.Combine(Path.GetDirectoryName(reportPath)!, "migration-diagnostics.txt"), migratedErrors.Select(item => item.ToString()));
            var report = _destinations.Values.OrderBy(item => item.Path, StringComparer.Ordinal).Select(item => new
            {
                OriginalType = item.Symbol.ToDisplayString(),
                Namespace = item.NamespaceName,
                item.Path,
                Sources = item.Symbol.DeclaringSyntaxReferences.Select(reference => Path.GetRelativePath(_root, reference.SyntaxTree.FilePath).Replace('\\', '/')).ToArray()
            });
            File.WriteAllText(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine($"Validated {paths.Length} source files, {_destinations.Count} types, {_renames.Count} identifier changes.");
            Console.WriteLine($"Plan: {reportPath}");
            Console.WriteLine($"Semantic diagnostics: {baselineErrors.Length} baseline errors; {migratedErrors.Length} migrated errors. Review ignored diagnostic reports before apply.");
            if (apply)
            {
                ApplyOutput(paths.Where(migratedPaths.Contains).ToArray(), output);
                Console.WriteLine($"Wrote {output.Count} source files and preserved surviving asset GUIDs.");
            }
        }

        /// <summary>Removes only compiler-confirmed unnecessary imports from existing sources.</summary>
        /// <param name="compilation">The complete source compilation.</param>
        /// <param name="apply">Whether to write the cleaned files.</param>
        private static void CleanImports(CSharpCompilation compilation, bool apply)
        {
            var count = 0;
            var allDiagnostics = compilation.GetDiagnostics();
            var hasErrors = allDiagnostics.Any(item => item.Severity == DiagnosticSeverity.Error);
            var diagnostics = allDiagnostics.Where(item => item.Id is "CS8019" or "CS0105")
                .GroupBy(item => item.Location.SourceTree);
            foreach (var group in diagnostics)
            {
                if (group.Key is null)
                {
                    continue;
                }
                var root = group.Key.GetRoot();
                var imports = group.Select(item => root.FindNode(item.Location.SourceSpan).FirstAncestorOrSelf<UsingDirectiveSyntax>())
                    .OfType<UsingDirectiveSyntax>().Distinct()
                    .Where(item => !hasErrors || (item.Name?.ToString().StartsWith("PlantsVsZombies.", StringComparison.Ordinal) == true
                        && compilation.GetSemanticModel(group.Key).GetSymbolInfo(item.Name).Symbol is INamespaceSymbol)).ToArray();
                count += imports.Length;
                if (apply && imports.Length > 0)
                {
                    var rewritten = root.RemoveNodes(imports, SyntaxRemoveOptions.KeepExteriorTrivia)!;
                    var text = rewritten.ToFullString().Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\n", "\r\n", StringComparison.Ordinal);
                    text = Regex.Replace(text, "(?:\\r\\n){3,}", "\r\n\r\n");
                    File.WriteAllText(group.Key.FilePath, text, new UTF8Encoding(false));
                }
            }
            Console.WriteLine($"{(apply ? "Removed" : "Found")} {count} compiler-confirmed unnecessary imports.");
        }

        /// <summary>Finds current runtime and Editor response files, with fallback for the pre-asmdef project.</summary>
        /// <returns>The newest response file for each available project assembly.</returns>
        private string[] FindCompilerResponses()
        {
            var bee = Path.Combine(_root, "Library", "Bee");
            if (!Directory.Exists(bee))
            {
                return Array.Empty<string>();
            }
            var all = Directory.EnumerateFiles(bee, "*.rsp", SearchOption.AllDirectories).ToArray();
            var names = new[] { "PlantsVsZombies.Runtime.rsp", "PlantsVsZombies.Editor.rsp" };
            var selected = names.Select(name => all.Where(path => Path.GetFileName(path) == name)
                .OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()).OfType<string>().ToArray();
            if (selected.Length > 0)
            {
                return selected;
            }
            return new[] { "Assembly-CSharp.rsp", "Assembly-CSharp-Editor.rsp" }
                .Select(name => all.Where(path => Path.GetFileName(path) == name)
                    .OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault()).OfType<string>().ToArray();
        }

        /// <summary>Collects top-level types and public nested classes or structs.</summary>
        /// <param name="compilation">The original Unity source compilation.</param>
        private void CollectTypes(CSharpCompilation compilation)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                var model = compilation.GetSemanticModel(tree);
                foreach (var declaration in tree.GetRoot().DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
                {
                    if (model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol symbol
                        || (symbol.ContainingType is not null && symbol.DeclaredAccessibility != Accessibility.Public))
                    {
                        continue;
                    }
                    var originalNamespace = symbol.ContainingNamespace.ToDisplayString();
                    if (!IsProjectNamespace(originalNamespace))
                    {
                        continue;
                    }
                    var namespaceName = GetDestinationNamespace(symbol, tree.FilePath);
                    var suffix = namespaceName == "PlantsVsZombies" ? "" : namespaceName["PlantsVsZombies.".Length..].Replace('.', '/');
                    var folder = tree.FilePath.Replace('\\', '/').Contains("/Assets/Editor/", StringComparison.OrdinalIgnoreCase)
                        ? "Assets/Editor" : "Assets/Scripts" + (suffix.Length == 0 ? "" : "/" + suffix);
                    var path = folder + "/" + symbol.Name + (Path.GetFileName(tree.FilePath).EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase) ? ".generated.cs" : ".cs");
                    _destinations[symbol] = new TypeDestination(symbol, namespaceName, path);
                }
            }
            var duplicate = _destinations.Values.GroupBy(item => item.Path, StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);
            if (duplicate is not null)
            {
                throw new InvalidOperationException("Multiple types target " + duplicate.Key);
            }
        }

        /// <summary>Chooses a namespace that keeps root simulation models separate from feature namespaces.</summary>
        /// <param name="symbol">The original source type.</param>
        /// <param name="path">The original source path.</param>
        /// <returns>The destination namespace.</returns>
        private static string GetDestinationNamespace(INamedTypeSymbol symbol, string path)
        {
            var originalNamespace = symbol.ContainingNamespace.ToDisplayString();
            if (symbol.ContainingType is not null)
            {
                return GetDestinationNamespace(symbol.ContainingType, path);
            }
            if (path.Replace('\\', '/').Contains("/Assets/Editor/", StringComparison.OrdinalIgnoreCase))
            {
                return "PlantsVsZombies.Editor";
            }
            if (originalNamespace is "PvZReplica" or "PvZUnity")
            {
                if (symbol.Name == "PvZGame")
                {
                    return "PlantsVsZombies.Game";
                }
                if (symbol.Name.Contains("Smoke", StringComparison.Ordinal))
                {
                    return "PlantsVsZombies.Diagnostics";
                }
                return "PlantsVsZombies.Simulation";
            }
            return RewriteNamespace(originalNamespace);
        }

        /// <summary>Collects private field and local identifier changes using original symbols.</summary>
        /// <param name="compilation">The original source compilation.</param>
        private void CollectRenames(CSharpCompilation compilation)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                var model = compilation.GetSemanticModel(tree);
                foreach (var node in tree.GetRoot().DescendantNodes())
                {
                    var symbol = node switch
                    {
                        VariableDeclaratorSyntax variable => model.GetDeclaredSymbol(variable),
                        ParameterSyntax parameter => model.GetDeclaredSymbol(parameter),
                        ForEachStatementSyntax statement => model.GetDeclaredSymbol(statement),
                        CatchDeclarationSyntax declaration => model.GetDeclaredSymbol(declaration),
                        SingleVariableDesignationSyntax designation => model.GetDeclaredSymbol(designation),
                        _ => null
                    };
                    if (symbol is IFieldSymbol field && field.DeclaredAccessibility is Accessibility.Private or Accessibility.Internal)
                    {
                        var core = field.Name.TrimStart('_');
                        if (core.StartsWith("s_", StringComparison.Ordinal))
                        {
                            core = core[2..];
                        }
                        var name = field.IsConst ? PascalCase(core) : (field.IsStatic ? "s_" : "_") + CamelCase(core);
                        if (name != field.Name && !field.ContainingType.GetMembers(name).Any())
                        {
                            _renames[field] = name;
                        }
                    }
                    else if (symbol is ILocalSymbol or IParameterSymbol)
                    {
                        var name = symbol is ILocalSymbol { IsConst: true } ? PascalCase(symbol.Name) : CamelCase(symbol.Name);
                        if (name != symbol.Name)
                        {
                            _renames[symbol] = name;
                        }
                    }
                }
            }
        }

        /// <summary>Creates and syntax-checks each resulting type file.</summary>
        /// <param name="compilation">The original source compilation.</param>
        /// <param name="options">The source preprocessor and language options.</param>
        /// <returns>The complete output keyed by repository-relative source path.</returns>
        private Dictionary<string, string> CreateOutput(CSharpCompilation compilation, CSharpParseOptions options)
        {
            var output = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var destination in _destinations.Values)
            {
                var references = destination.Symbol.DeclaringSyntaxReferences.OrderBy(reference =>
                    Path.GetFileNameWithoutExtension(reference.SyntaxTree.FilePath) == destination.Symbol.Name ? 0 : 1)
                    .ThenBy(reference => reference.SyntaxTree.FilePath, StringComparer.Ordinal).ToArray();
                var declarations = new List<BaseTypeDeclarationSyntax>();
                var usings = new Dictionary<string, UsingDirectiveSyntax>(StringComparer.Ordinal);
                var requiredNamespaces = new HashSet<string>(StringComparer.Ordinal);
                foreach (var reference in references)
                {
                    var model = compilation.GetSemanticModel(reference.SyntaxTree);
                    var rewriter = new MigrationRewriter(model, _destinations, _renames, reference.GetSyntax());
                    declarations.Add((BaseTypeDeclarationSyntax)rewriter.Visit(reference.GetSyntax())!);
                    foreach (var identifier in reference.GetSyntax().DescendantNodes().OfType<IdentifierNameSyntax>())
                    {
                        if (model.GetSymbolInfo(identifier).Symbol is INamedTypeSymbol referencedType
                            && _destinations.TryGetValue(referencedType.OriginalDefinition, out var dependency)
                            && dependency.NamespaceName != destination.NamespaceName)
                        {
                            requiredNamespaces.Add(dependency.NamespaceName);
                        }
                    }
                    foreach (var directive in reference.SyntaxTree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>())
                    {
                        var rewritten = (UsingDirectiveSyntax)rewriter.Visit(directive)!;
                        usings[rewritten.WithoutTrivia().ToString()] = rewritten.WithoutTrivia();
                    }
                }
                foreach (var name in new[] { "PlantsVsZombies.Simulation", "PlantsVsZombies.Game", "PlantsVsZombies.Diagnostics" })
                {
                    if (requiredNamespaces.Contains(name))
                    {
                        usings["using " + name + ";"] = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(name));
                    }
                }
                var primary = declarations[0];
                if (primary is TypeDeclarationSyntax type)
                {
                    var members = declarations.OfType<TypeDeclarationSyntax>().SelectMany(item => item.Members).ToArray();
                    var implementations = members.OfType<MethodDeclarationSyntax>().Where(item => item.Modifiers.Any(SyntaxKind.PartialKeyword) && item.Body is not null)
                        .Select(item => item.Identifier.ValueText).ToHashSet(StringComparer.Ordinal);
                    members = members.Where(item => item is not MethodDeclarationSyntax method || !method.Modifiers.Any(SyntaxKind.PartialKeyword) || method.Body is not null || !implementations.Contains(method.Identifier.ValueText))
                        .Select(RemovePartialMethod).ToArray();
                    primary = type.WithMembers(SyntaxFactory.List(members)).WithModifiers(SyntaxFactory.TokenList(type.Modifiers.Where(token => !token.IsKind(SyntaxKind.PartialKeyword))));
                }
                primary = (BaseTypeDeclarationSyntax)new DocumentationRewriter().Visit(primary)!;
                var unit = SyntaxFactory.CompilationUnit()
                    .WithUsings(SyntaxFactory.List(usings.Values.OrderBy(item => item.Name?.ToString(), StringComparer.Ordinal)))
                    .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(destination.NamespaceName))
                        .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(primary))));
                var text = SourceLayout.Format("#nullable enable\r\n" + unit.NormalizeWhitespace("    ", "\r\n").ToFullString().Trim() + "\r\n");
                var errors = CSharpSyntaxTree.ParseText(text, options).GetDiagnostics().Where(item => item.Severity == DiagnosticSeverity.Error).ToArray();
                if (errors.Length > 0)
                {
                    throw new InvalidOperationException(destination.Path + ": " + string.Join("; ", errors.Select(item => item.ToString())));
                }
                output.Add(destination.Path, text);
            }
            return output;
        }

        /// <summary>Converts a partial implementation into an ordinary private method.</summary>
        /// <param name="member">The merged member.</param>
        /// <returns>The non-partial member.</returns>
        private static MemberDeclarationSyntax RemovePartialMethod(MemberDeclarationSyntax member)
        {
            if (member is not MethodDeclarationSyntax method || !method.Modifiers.Any(SyntaxKind.PartialKeyword))
            {
                return member;
            }
            if (method.Body is null && method.ExpressionBody is null)
            {
                return method.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)))
                    .WithBody(SyntaxFactory.Block()).WithSemicolonToken(default);
            }
            return method.WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)));
        }

        /// <summary>Writes source files while transferring existing primary script metadata.</summary>
        /// <param name="originalPaths">The original Unity source paths.</param>
        /// <param name="output">The validated migrated source.</param>
        private void ApplyOutput(string[] originalPaths, Dictionary<string, string> output)
        {
            var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in originalPaths.OrderBy(path =>
                _destinations.Values.Any(item => item.Symbol.Name == Path.GetFileNameWithoutExtension(path)) ? 0 : 1))
            {
                if (!File.Exists(path + ".meta"))
                {
                    continue;
                }
                var candidates = _destinations.Values.Where(item => item.Symbol.DeclaringSyntaxReferences.Any(reference => reference.SyntaxTree.FilePath == path)).ToArray();
                var primary = candidates.FirstOrDefault(item => item.Symbol.Name == Path.GetFileNameWithoutExtension(path)) ?? candidates.FirstOrDefault();
                if (primary is not null && !metadata.ContainsKey(primary.Path))
                {
                    metadata[primary.Path] = File.ReadAllText(path + ".meta");
                }
            }
            foreach (var pair in output)
            {
                var target = Path.Combine(_root, pair.Key);
                if (File.Exists(target) && !originalPaths.Contains(target, StringComparer.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Refusing to replace unrelated source at " + pair.Key);
                }
            }
            foreach (var pair in output)
            {
                var target = Path.Combine(_root, pair.Key);
                EnsureAssetDirectory(Path.GetDirectoryName(target)!);
                File.WriteAllText(target, pair.Value, new UTF8Encoding(false));
                var meta = metadata.GetValueOrDefault(pair.Key) ?? CreateMetadata("MonoImporter");
                File.WriteAllText(target + ".meta", meta, new UTF8Encoding(false));
            }
            foreach (var path in originalPaths)
            {
                var relative = Path.GetRelativePath(_root, path).Replace('\\', '/');
                if (!output.ContainsKey(relative))
                {
                    File.Delete(path);
                    File.Delete(path + ".meta");
                }
            }
        }

        /// <summary>Creates destination folders and their Unity metadata.</summary>
        /// <param name="path">The absolute destination directory.</param>
        private void EnsureAssetDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                return;
            }
            EnsureAssetDirectory(Path.GetDirectoryName(path)!);
            Directory.CreateDirectory(path);
            if (!File.Exists(path + ".meta"))
            {
                var guid = Guid.NewGuid().ToString("N");
                File.WriteAllText(path + ".meta", $"fileFormatVersion: 2\r\nguid: {guid}\r\nfolderAsset: yes\r\nDefaultImporter:\r\n  externalObjects: {{}}\r\n  userData:\r\n  assetBundleName:\r\n  assetBundleVariant:\r\n");
            }
        }

        /// <summary>Creates metadata for a newly extracted script.</summary>
        /// <param name="importer">The Unity importer name.</param>
        /// <returns>The script metadata.</returns>
        private static string CreateMetadata(string importer)
        {
            var guid = Guid.NewGuid().ToString("N");
            return $"fileFormatVersion: 2\r\nguid: {guid}\r\n{importer}:\r\n  externalObjects: {{}}\r\n  serializedVersion: 2\r\n  defaultReferences: []\r\n  executionOrder: 0\r\n  icon: {{instanceID: 0}}\r\n  userData:\r\n  assetBundleName:\r\n  assetBundleVariant:\r\n";
        }

        /// <summary>Checks whether a namespace belongs to this project.</summary>
        /// <param name="value">The original namespace.</param>
        /// <returns>Whether the namespace is migrated.</returns>
        internal static bool IsProjectNamespace(string value) => value is "PvZReplica" or "PvZUnity" or "PlantsVsZombies"
            || value.StartsWith("PvZReplica.", StringComparison.Ordinal) || value.StartsWith("PvZUnity.", StringComparison.Ordinal)
            || value.StartsWith("PlantsVsZombies.", StringComparison.Ordinal);

        /// <summary>Replaces only the project namespace root.</summary>
        /// <param name="value">The original namespace.</param>
        /// <returns>The namespace with the PlantsVsZombies root.</returns>
        internal static string RewriteNamespace(string value)
        {
            if (value == "PvZReplica" || value == "PvZUnity")
            {
                return "PlantsVsZombies";
            }
            if (value.StartsWith("PvZReplica.", StringComparison.Ordinal))
            {
                return "PlantsVsZombies" + value["PvZReplica".Length..];
            }
            return value.StartsWith("PvZUnity.", StringComparison.Ordinal) ? "PlantsVsZombies" + value["PvZUnity".Length..] : value;
        }

        /// <summary>Converts the first letter of an identifier to lowercase.</summary>
        /// <param name="value">The source identifier.</param>
        /// <returns>The camel-case identifier.</returns>
        internal static string CamelCase(string value) => value.Length == 0 ? value : char.ToLowerInvariant(value[0]) + value[1..];

        /// <summary>Converts the first letter of an identifier to uppercase.</summary>
        /// <param name="value">The source identifier.</param>
        /// <returns>The Pascal-case identifier.</returns>
        private static string PascalCase(string value) => value.Length == 0 ? value : char.ToUpperInvariant(value[0]) + value[1..];
    }
}
