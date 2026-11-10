#nullable enable
using System;
using System.IO;

namespace PlantsVsZombies.Tools.ProjectRefactor
{
    /// <summary>Runs the repository source migration without opening generated IDE projects.</summary>
    internal static class Program
    {
        /// <summary>Builds a migration plan and optionally writes its reviewed output.</summary>
        /// <param name="arguments">The repository root, followed by an optional --apply flag.</param>
        /// <returns>Zero on success; one when migration validation fails.</returns>
        private static int Main(string[] arguments)
        {
            try
            {
                var root = Path.GetFullPath(arguments.Length > 0 ? arguments[0] : ".");
                var apply = Array.IndexOf(arguments, "--apply") >= 0;
                if (Array.IndexOf(arguments, "--layout") >= 0)
                {
                    SourceLayout.Run(root, apply);
                    return 0;
                }
                var migration = new SourceMigration(root);
                migration.Run(apply, Array.IndexOf(arguments, "--cleanup-imports") >= 0);
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }
    }
}
