#nullable enable

using System;
using System.IO;
using System.Linq;

namespace PlantsVsZombies.Tests.Architecture
{
    /// <summary>Runs repository architecture checks without compiling a generated Unity project.</summary>
    internal static class Program
    {
        /// <summary>Runs the scanner and its positive and negative syntax fixtures.</summary>
        /// <param name="args">An optional repository root or the self-test-only switch.</param>
        /// <returns>Zero when all selected checks pass; one otherwise.</returns>
        private static int Main(string[] args)
        {
            try
            {
                ArchitectureFixtures.Run();
                if (args.Contains("--self-test", StringComparer.Ordinal))
                {
                    Console.WriteLine("PASS: architecture scanner positive and negative fixtures.");
                    return 0;
                }

                var root = args.Length == 0 ? FindRepository() : Path.GetFullPath(args[0]);
                var violations = ProjectArchitecture.Inspect(root);
                foreach (var violation in violations)
                {
                    Console.Error.WriteLine(violation);
                }

                Console.WriteLine($"Architecture: {violations.Count} violation(s).");
                return violations.Count == 0 ? 0 : 1;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
                return 1;
            }
        }

        /// <summary>Finds the repository containing the test executable.</summary>
        /// <returns>The absolute repository path.</returns>
        /// <exception cref="DirectoryNotFoundException">The repository cannot be found.</exception>
        private static string FindRepository()
        {
            for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, ".editorconfig"))
                    && File.Exists(Path.Combine(directory.FullName, "ProjectSettings", "ProjectVersion.txt")))
                {
                    return directory.FullName;
                }
            }

            throw new DirectoryNotFoundException("Pass the repository root as the first argument.");
        }
    }
}
