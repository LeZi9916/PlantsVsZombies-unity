#nullable enable
using System;
using System.IO;

namespace PlantsVsZombies.Tests
{
    /// <summary>Locates the repository and separately stored original PC evidence for independent tests.</summary>
    internal static class OriginalReferencePaths
    {
        /// <summary>Finds the runtime source repository from the test executable or current working directory.</summary>
        /// <returns>The absolute repository root.</returns>
        /// <exception cref="DirectoryNotFoundException">No repository ancestor contains the runtime source and tests.</exception>
        internal static string RepositoryRoot()
        {
            foreach (var start in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
            {
                for (DirectoryInfo? current = new DirectoryInfo(start); current != null; current = current.Parent)
                {
                    if (Directory.Exists(Path.Combine(current.FullName, "Assets", "Scripts")) &&
                        Directory.Exists(Path.Combine(current.FullName, "Tools", "Tests")))
                    {
                        return current.FullName;
                    }
                }
            }

            throw new DirectoryNotFoundException("Cannot locate the runtime source repository.");
        }

        /// <summary>Finds the original executable and analysis, honoring an explicit evidence root first.</summary>
        /// <returns>The absolute original-evidence root.</returns>
        /// <exception cref="DirectoryNotFoundException">The configured root or searched ancestors lack the original executable and analysis.</exception>
        internal static string OriginRoot()
        {
            var configured = Environment.GetEnvironmentVariable("PVZ_ORIGIN_ROOT");
            if (!string.IsNullOrWhiteSpace(configured))
            {
                var root = Path.GetFullPath(configured);
                if (IsOriginRoot(root))
                {
                    return root;
                }

                throw new DirectoryNotFoundException("PVZ_ORIGIN_ROOT must contain PlantsVsZombies.exe and analysis/ida.");
            }

            foreach (var start in new[] { AppContext.BaseDirectory, Environment.CurrentDirectory })
            {
                for (DirectoryInfo? current = new DirectoryInfo(start); current != null; current = current.Parent)
                {
                    if (IsOriginRoot(current.FullName))
                    {
                        return current.FullName;
                    }

                    var sibling = Path.Combine(current.FullName, "pvz-origin");
                    if (IsOriginRoot(sibling))
                    {
                        return sibling;
                    }
                }
            }

            throw new DirectoryNotFoundException("Set PVZ_ORIGIN_ROOT to the read-only original PC evidence directory.");
        }

        /// <summary>Checks the minimum original-executable and analysis layout without reading personal saves.</summary>
        /// <param name="path">The candidate evidence root.</param>
        /// <returns>Whether the candidate contains the original executable and IDA evidence directory.</returns>
        private static bool IsOriginRoot(string path)
        {
            return File.Exists(Path.Combine(path, "PlantsVsZombies.exe")) &&
                Directory.Exists(Path.Combine(path, "analysis", "ida"));
        }
    }
}
