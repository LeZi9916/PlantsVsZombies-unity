#nullable enable

using System;
using System.Linq;

namespace PlantsVsZombies.Tests.Architecture
{
    /// <summary>Checks that architecture rules distinguish valid code from broken examples.</summary>
    internal static class ArchitectureFixtures
    {
        /// <summary>Exercises the source scanner against positive and negative examples.</summary>
        /// <exception cref="InvalidOperationException">A scanner rule does not behave as expected.</exception>
        internal static void Run()
        {
            var valid = "#nullable enable\r\nnamespace PlantsVsZombies.Example\r\n{\r\n    public class Sample\r\n    {\r\n        private void Execute(bool enabled)\r\n        {\r\n            if (enabled)\r\n            {\r\n                return;\r\n            }\r\n        }\r\n    }\r\n}\r\n";
            Require(ProjectArchitecture.InspectSource("Sample.cs", valid).Count == 0, "valid source");
            Require(ProjectArchitecture.InspectSource("Sample.cs", valid.Replace("PlantsVsZombies.Example", "Legacy.Example", StringComparison.Ordinal)).Any(message => message.Contains("namespace", StringComparison.Ordinal)), "legacy namespace");
            Require(ProjectArchitecture.InspectSource("Sample.cs", "#nullable enable\r\npublic class Sample {}\r\n").Any(message => message.Contains("namespace", StringComparison.Ordinal)), "missing namespace");
            Require(ProjectArchitecture.InspectSource("Sample.cs", valid.Replace("public class Sample", "public partial class Sample", StringComparison.Ordinal)).Any(message => message.Contains("partial", StringComparison.Ordinal)), "partial type");
            Require(ProjectArchitecture.InspectSource("Sample.cs", valid.Replace("private void Execute", "public class Nested {} private void Execute", StringComparison.Ordinal)).Any(message => message.Contains("nested", StringComparison.Ordinal)), "nested public type");
            Require(ProjectArchitecture.InspectSource("Wrong.cs", valid).Any(message => message.Contains("filename", StringComparison.Ordinal)), "type filename mismatch");
            Require(ProjectArchitecture.InspectSource("Sample.cs", valid.Replace("if (enabled)\r\n            {\r\n                return;\r\n            }", "if (enabled) return;", StringComparison.Ordinal)).Any(message => message.Contains("braces", StringComparison.Ordinal)), "unbraced branch");
            Require(ProjectArchitecture.InspectSource("Sample.cs", valid.Replace("#nullable enable\r\n", string.Empty, StringComparison.Ordinal)).Any(message => message.Contains("nullable", StringComparison.Ordinal)), "nullable directive");
            Require(ProjectArchitecture.InspectSource("Sample.cs", valid.Replace("\r\n", "\n", StringComparison.Ordinal)).Any(message => message.Contains("CRLF", StringComparison.Ordinal)), "line endings");
        }

        /// <summary>Fails a fixture with its purpose attached.</summary>
        /// <param name="condition">Whether the fixture succeeded.</param>
        /// <param name="name">The rule exercised by the fixture.</param>
        /// <exception cref="InvalidOperationException">The fixture failed.</exception>
        private static void Require(bool condition, string name)
        {
            if (!condition)
            {
                throw new InvalidOperationException("Architecture scanner fixture failed: " + name);
            }
        }
    }
}
