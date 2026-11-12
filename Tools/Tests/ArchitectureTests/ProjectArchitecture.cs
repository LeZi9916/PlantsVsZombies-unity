#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PlantsVsZombies.Tests.Architecture
{
    /// <summary>Validates source structure, Unity asset identity, and the selected render pipeline.</summary>
    internal static class ProjectArchitecture
    {
        /// <summary>Inspects production C# source, asset metadata, and render pipeline references.</summary>
        /// <param name="repository">The absolute repository directory.</param>
        /// <returns>All violations with source or asset paths.</returns>
        internal static List<string> Inspect(string repository)
        {
            var violations = new List<string>();
            var scripts = Path.Combine(repository, "Assets", "Scripts");
            var sources = Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories);
            var rendererAttached = false;
            var meshFilterAttached = false;
            foreach (var path in sources)
            {
                var relative = Path.GetRelativePath(repository, path);
                var text = File.ReadAllText(path);
                violations.AddRange(InspectSource(relative, text));
                var syntax = CSharpSyntaxTree.ParseText(text).GetRoot();
                foreach (var invocation in syntax.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    if (invocation.Expression is not MemberAccessExpressionSyntax member)
                    {
                        continue;
                    }

                    if (member.Name is GenericNameSyntax generic && generic.Identifier.ValueText == "AddComponent")
                    {
                        rendererAttached |= generic.TypeArgumentList.Arguments.Any(type => type.ToString() is "MeshRenderer" or "SpriteRenderer");
                        meshFilterAttached |= generic.TypeArgumentList.Arguments.Any(type => type.ToString() == "MeshFilter");
                    }

                    if (member.Expression.ToString() == "GL" || (member.Expression.ToString() == "Graphics" && member.Name.Identifier.ValueText == "DrawMeshNow"))
                    {
                        violations.Add(relative + ": immediate rendering bypasses the scene Renderer path.");
                    }
                }
            }

            if (!rendererAttached || !meshFilterAttached)
            {
                violations.Add("Assets/Scripts: no GameObject renderer and mesh ownership path was found.");
            }

            InspectMetadata(repository, scripts, violations);
            InspectPipeline(repository, violations);
            return violations;
        }

        /// <summary>Inspects C# syntax without loading Unity assemblies.</summary>
        /// <param name="path">The source path used for diagnostics.</param>
        /// <param name="text">The C# source text.</param>
        /// <returns>All source structure violations.</returns>
        internal static List<string> InspectSource(string path, string text)
        {
            var violations = new List<string>();
            var tree = CSharpSyntaxTree.ParseText(text, new CSharpParseOptions(LanguageVersion.CSharp9, DocumentationMode.Parse));
            var root = tree.GetCompilationUnitRoot();
            foreach (var diagnostic in tree.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            {
                violations.Add(path + ": " + diagnostic);
            }

            if (!Regex.IsMatch(text, @"^\s*#nullable\s+enable\b", RegexOptions.Multiline))
            {
                violations.Add(path + ": enable nullable analysis explicitly.");
            }

            if (Regex.IsMatch(text, @"(?<!\r)\n") || !text.EndsWith("\r\n", StringComparison.Ordinal))
            {
                violations.Add(path + ": use CRLF and a final newline.");
            }

            foreach (var name in root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>())
            {
                var qualified = string.Join(".", name.AncestorsAndSelf().OfType<BaseNamespaceDeclarationSyntax>().Reverse().Select(item => item.Name.ToString()));
                if (qualified != "PlantsVsZombies" && !qualified.StartsWith("PlantsVsZombies.", StringComparison.Ordinal))
                {
                    violations.Add(path + ": namespace must use the PlantsVsZombies root.");
                }

                if (name is FileScopedNamespaceDeclarationSyntax || name.Usings.Count != 0)
                {
                    violations.Add(path + ": use a block namespace with using directives outside it.");
                }
            }

            var declarations = root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().ToArray();
            var topLevel = declarations.Where(type => !type.Ancestors().OfType<BaseTypeDeclarationSyntax>().Any()).ToArray();
            if (topLevel.Length > 1)
            {
                violations.Add(path + ": keep one top-level type per file.");
            }

            if (topLevel.Any(type => !type.Ancestors().OfType<BaseNamespaceDeclarationSyntax>().Any()))
            {
                violations.Add(path + ": top-level types must declare a PlantsVsZombies namespace.");
            }

            foreach (var type in declarations)
            {
                if (type.Modifiers.Any(SyntaxKind.PartialKeyword))
                {
                    violations.Add(path + ": consolidate partial type " + type.Identifier.ValueText + ".");
                }

                if (type.Modifiers.Any(SyntaxKind.PublicKeyword) && type.Ancestors().OfType<BaseTypeDeclarationSyntax>().Any())
                {
                    violations.Add(path + ": move public nested type " + type.Identifier.ValueText + " to its own file.");
                }
            }

            if (topLevel.Length == 1 && Path.GetFileName(path) != topLevel[0].Identifier.ValueText + ".cs"
                && Path.GetFileName(path) != topLevel[0].Identifier.ValueText + ".generated.cs")
            {
                violations.Add(path + ": filename must match the top-level type.");
            }

            foreach (var node in root.DescendantNodes())
            {
                var statement = node switch
                {
                    IfStatementSyntax branch => branch.Statement,
                    ElseClauseSyntax branch when branch.Statement is not IfStatementSyntax => branch.Statement,
                    ForStatementSyntax loop => loop.Statement,
                    ForEachStatementSyntax loop => loop.Statement,
                    WhileStatementSyntax loop => loop.Statement,
                    DoStatementSyntax loop => loop.Statement,
                    UsingStatementSyntax resource => resource.Statement,
                    LockStatementSyntax synchronization => synchronization.Statement,
                    _ => null
                };
                if (statement != null && statement is not BlockSyntax)
                {
                    violations.Add(path + ": embedded statements must use braces.");
                }
            }

            InspectPresentationOwnership(path, root, violations);
            InspectCoordinatorSize(path, text, violations);
            return violations;
        }

        /// <summary>The longest allowed game coordinator source, so new responsibilities move to collaborators instead of accumulating there.</summary>
        private const int CoordinatorLineLimit = 700;

        /// <summary>Keeps the game coordinator thin by bounding the length of its source file.</summary>
        /// <param name="path">The source path used for diagnostics.</param>
        /// <param name="text">The C# source text.</param>
        /// <param name="violations">The destination for coordinator size violations.</param>
        private static void InspectCoordinatorSize(string path, string text, List<string> violations)
        {
            if (Path.GetFileName(path) != "PvZGame.cs")
            {
                return;
            }

            var lines = text.Split('\n').Length - 1;
            if (lines > CoordinatorLineLimit)
            {
                violations.Add(path + ": PvZGame coordinates collaborators and must stay under " + CoordinatorLineLimit + " lines (found " + lines + "); move new responsibilities into a collaborator.");
            }
        }

        /// <summary>Rejects runtime IMGUI callbacks and keeps drawing outside the game-flow coordinator.</summary>
        /// <param name="path">The source path used for diagnostics.</param>
        /// <param name="root">The parsed source whose declarations and accesses are inspected.</param>
        /// <param name="violations">The destination for presentation boundary violations.</param>
        private static void InspectPresentationOwnership(string path, CompilationUnitSyntax root, List<string> violations)
        {
            var aliases = root.Usings
                .Where(directive => directive.Alias != null)
                .ToDictionary(directive => directive.Alias!.Name.Identifier.ValueText, directive => directive.Name!.ToString(), StringComparer.Ordinal);
            foreach (var directive in root.Usings)
            {
                if (directive.StaticKeyword.IsKind(SyntaxKind.StaticKeyword)
                    && directive.Name != null
                    && IsImmediateInterfaceType(directive.Name.ToString(), aliases))
                {
                    violations.Add(path + ": runtime IMGUI imports are forbidden; use the scene presenter and Canvas controls.");
                }
            }

            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                if (method.Identifier.ValueText == "OnGUI")
                {
                    violations.Add(path + ": runtime OnGUI callbacks are forbidden; sample input and present from the frame lifecycle.");
                }

                if (method.Ancestors().OfType<ClassDeclarationSyntax>().Any(type => type.Identifier.ValueText == "PvZGame")
                    && method.Identifier.ValueText.StartsWith("Draw", StringComparison.Ordinal))
                {
                    violations.Add(path + ": PvZGame method " + method.Identifier.ValueText + " coordinates game flow and must delegate drawing to its presenter.");
                }
            }

            foreach (var access in root.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
            {
                var owner = ResolveAlias(access.Expression.ToString(), aliases).Replace("global::", string.Empty, StringComparison.Ordinal);
                if (IsImmediateInterfaceType(owner, aliases)
                    || ((owner == "Event" || owner == "UnityEngine.Event") && access.Name.Identifier.ValueText == "current"))
                {
                    violations.Add(path + ": runtime IMGUI access " + access + " is forbidden; use Canvas presentation and independent input.");
                }

                if (access.Ancestors().OfType<ClassDeclarationSyntax>().Any(type => type.Identifier.ValueText == "PvZGame")
                    && access.Parent is InvocationExpressionSyntax
                    && access.Name.Identifier.ValueText is "DrawTexture" or "DrawTextureWithTexCoords" or "DrawQuad" or "DrawMesh")
                {
                    violations.Add(path + ": PvZGame must not submit drawing commands directly.");
                }
            }
        }

        /// <summary>Identifies Unity's immediate-mode interface APIs without loading engine assemblies.</summary>
        /// <param name="name">The source type name, including any namespace or alias.</param>
        /// <param name="aliases">The aliases declared by this source file.</param>
        /// <returns>Whether the name identifies a runtime IMGUI API.</returns>
        private static bool IsImmediateInterfaceType(string name, IReadOnlyDictionary<string, string> aliases)
        {
            var resolved = ResolveAlias(name, aliases).Replace("global::", string.Empty, StringComparison.Ordinal);
            return resolved is "GUI" or "UnityEngine.GUI" or "GUILayout" or "UnityEngine.GUILayout" or "GUIUtility" or "UnityEngine.GUIUtility";
        }

        /// <summary>Resolves a directly named using alias for syntax-only ownership checks.</summary>
        /// <param name="name">The type name or alias to resolve.</param>
        /// <param name="aliases">The aliases declared by this source file.</param>
        /// <returns>The aliased type name, or the original name when no alias exists.</returns>
        private static string ResolveAlias(string name, IReadOnlyDictionary<string, string> aliases)
        {
            return aliases.TryGetValue(name, out var resolved) ? resolved : name;
        }

        /// <summary>Checks paired script metadata and unique GUIDs in the production source tree.</summary>
        /// <param name="repository">The repository root used for relative diagnostics.</param>
        /// <param name="scripts">The production source directory.</param>
        /// <param name="violations">The destination for violations.</param>
        private static void InspectMetadata(string repository, string scripts, List<string> violations)
        {
            var guids = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories))
            {
                if (!File.Exists(path + ".meta"))
                {
                    violations.Add(Path.GetRelativePath(repository, path) + ": missing script metadata.");
                }
            }

            foreach (var path in Directory.GetFiles(scripts, "*.meta", SearchOption.AllDirectories))
            {
                var asset = path.Substring(0, path.Length - ".meta".Length);
                if (!File.Exists(asset) && !Directory.Exists(asset))
                {
                    violations.Add(Path.GetRelativePath(repository, path) + ": orphan metadata.");
                }

                var match = Regex.Match(File.ReadAllText(path), @"^guid:\s*([0-9a-f]{32})\s*$", RegexOptions.Multiline);
                if (!match.Success)
                {
                    violations.Add(Path.GetRelativePath(repository, path) + ": missing or invalid GUID.");
                }
                else if (!guids.TryAdd(match.Groups[1].Value, path))
                {
                    violations.Add(Path.GetRelativePath(repository, path) + ": duplicate script GUID with " + guids[match.Groups[1].Value]);
                }
            }
        }

        /// <summary>Resolves the global URP asset and verifies quality settings inherit or select it.</summary>
        /// <param name="repository">The repository root.</param>
        /// <param name="violations">The destination for violations.</param>
        private static void InspectPipeline(string repository, List<string> violations)
        {
            using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(repository, "Packages", "manifest.json")));
            if (!manifest.RootElement.GetProperty("dependencies").TryGetProperty("com.unity.render-pipelines.universal", out _))
            {
                violations.Add("Packages/manifest.json: URP dependency is missing.");
            }

            var graphics = File.ReadAllText(Path.Combine(repository, "ProjectSettings", "GraphicsSettings.asset"));
            var selected = Regex.Match(graphics, @"m_CustomRenderPipeline:\s*\{fileID:\s*[1-9]\d*,\s*guid:\s*([0-9a-f]{32})");
            if (!selected.Success)
            {
                violations.Add("ProjectSettings/GraphicsSettings.asset: select a persisted URP asset.");
                return;
            }

            var selectedGuid = selected.Groups[1].Value;
            var assets = Directory.GetFiles(Path.Combine(repository, "Assets"), "*.asset.meta", SearchOption.AllDirectories);
            var pipelineMeta = assets.FirstOrDefault(path => Regex.IsMatch(File.ReadAllText(path), @"^guid:\s*" + selectedGuid + @"\s*$", RegexOptions.Multiline));
            if (pipelineMeta == null)
            {
                violations.Add("ProjectSettings/GraphicsSettings.asset: pipeline GUID does not resolve to a project asset.");
                return;
            }

            var pipeline = File.ReadAllText(pipelineMeta.Substring(0, pipelineMeta.Length - ".meta".Length));
            var renderer = Regex.Match(pipeline, @"m_RendererDataList:\s*\r?\n\s*-\s*\{fileID:\s*[1-9]\d*,\s*guid:\s*([0-9a-f]{32})");
            if (!renderer.Success || !assets.Any(path => Regex.IsMatch(File.ReadAllText(path), @"^guid:\s*" + renderer.Groups[1].Value + @"\s*$", RegexOptions.Multiline)))
            {
                violations.Add(Path.GetRelativePath(repository, pipelineMeta) + ": renderer data does not resolve to a project asset.");
            }

            var quality = File.ReadAllText(Path.Combine(repository, "ProjectSettings", "QualitySettings.asset"));
            foreach (Match entry in Regex.Matches(quality, @"customRenderPipeline:\s*\{fileID:\s*([0-9]+)(?:,\s*guid:\s*([0-9a-f]{32}))?"))
            {
                if (entry.Groups[1].Value != "0" && entry.Groups[2].Value != selectedGuid)
                {
                    violations.Add("ProjectSettings/QualitySettings.asset: a quality level overrides the selected URP asset.");
                }
            }
        }
    }
}
