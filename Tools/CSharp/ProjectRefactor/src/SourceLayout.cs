#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace PlantsVsZombies.Tools.ProjectRefactor
{
    /// <summary>Expands long initializers without rewriting their expressions.</summary>
    internal static class SourceLayout
    {
        /// <summary>Formats long single-line initializers in handwritten runtime sources.</summary>
        /// <param name="root">The repository root.</param>
        /// <param name="apply">Whether to write the formatted sources.</param>
        public static void Run(string root, bool apply)
        {
            var count = 0;
            foreach (var path in Directory.EnumerateFiles(Path.Combine(root, "Assets", "Scripts"), "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase))
                .Where(path => !path.Replace('\\', '/').Contains("/Rendering/", StringComparison.Ordinal)))
            {
                var original = File.ReadAllText(path);
                var formatted = Format(original);
                if (original != formatted)
                {
                    count++;
                    if (apply)
                    {
                        File.WriteAllText(path, formatted, new UTF8Encoding(false));
                    }
                }
            }
            Console.WriteLine($"{(apply ? "Formatted" : "Found")} {count} source files with long initializers.");
        }

        /// <summary>Adds line breaks around initializer elements while preserving expressions and comments.</summary>
        /// <param name="text">The source text.</param>
        /// <returns>The source with readable initializer layout.</returns>
        public static string Format(string text)
        {
            var tree = CSharpSyntaxTree.ParseText(text);
            var source = tree.GetText();
            var edits = new List<(int Start, int Length, string Text)>();
            foreach (var initializer in tree.GetRoot().DescendantNodes().OfType<InitializerExpressionSyntax>())
            {
                var location = initializer.GetLocation().GetLineSpan();
                if (initializer.Expressions.Count < 2 || location.StartLinePosition.Line != location.EndLinePosition.Line
                    || (initializer.Expressions.Count <= 3 && initializer.Span.Length < 120)
                    || initializer.DescendantTrivia().Any(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineCommentTrivia)))
                {
                    continue;
                }
                var line = source.Lines.GetLineFromPosition(initializer.SpanStart).ToString();
                var indentation = line.TakeWhile(char.IsWhiteSpace).Count() + (initializer.Ancestors().OfType<InitializerExpressionSyntax>().Count() * 4);
                var elementIndent = new string(' ', indentation + 4);
                edits.Add((initializer.OpenBraceToken.Span.End,
                    initializer.Expressions[0].SpanStart - initializer.OpenBraceToken.Span.End, "\r\n" + elementIndent));
                var separators = initializer.Expressions.GetSeparators().ToArray();
                for (var index = 0; index < initializer.Expressions.Count - 1; index++)
                {
                    var separator = separators[index];
                    edits.Add((separator.Span.End, initializer.Expressions[index + 1].SpanStart - separator.Span.End, "\r\n" + elementIndent));
                }
                var final = separators.Length == initializer.Expressions.Count ? separators[^1].Span.End : initializer.Expressions[^1].Span.End;
                edits.Add((final, initializer.CloseBraceToken.SpanStart - final, "\r\n" + new string(' ', indentation)));
            }
            var result = new StringBuilder(text);
            foreach (var edit in edits.OrderByDescending(item => item.Start))
            {
                result.Remove(edit.Start, edit.Length).Insert(edit.Start, edit.Text);
            }
            return Regex.Replace(result.ToString(), "(?:\\r\\n){3,}", "\r\n\r\n");
        }
    }
}
