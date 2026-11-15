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
            RequirePresentationRule("private void Tick() { Forward(); } private void Forward() {}", null, "simulation delegation", "GameSimulation");
            RequirePresentationRule("private void Tick(int kind) { switch (kind) { case 1: break; } }", "species dispatch", "simulation species switch", "GameSimulation");
            RequirePresentationRule("private object Spawn() { return new Zombie(); }", "actor creation", "simulation actor factory", "GameSimulation");
            RequirePresentationRule("private bool Has(object value) { if (value == null) { return false; } return true; }", null, "simulation lifecycle guard", "GameSimulation");
            RequirePresentationRule("private void Tick(PlantKind kind) { _plants.Tick(kind); }", null, "simulation kind forwarding", "GameSimulation");
            RequirePresentationRule("private void Tick(PlantKind kind) { if (kind == PlantKind.Peashooter) { return; } }", "species dispatch", "simulation plant if dispatch", "GameSimulation");
            RequirePresentationRule("private void Tick(ZombieKind kind) { if (kind == global::PlantsVsZombies.Simulation.ZombieKind.Normal) { return; } }", "species dispatch", "qualified zombie if dispatch", "GameSimulation");
            RequirePresentationRule("private bool Has(PlantKind kind) { if (kind == default) { return true; } return false; }", "species dispatch", "simulation typed kind branch", "GameSimulation");
            RequirePresentationRule("private bool Has(ZombieKind kind) { return kind == ZombieKind.Normal ? true : false; }", "species dispatch", "simulation conditional dispatch", "GameSimulation");
            RequirePresentationRule("private object Spawn() { return new PlantsVsZombies.Simulation.Plant(); }", "actor creation", "qualified plant factory", "GameSimulation");
            RequirePresentationRule("private object Spawn() { return new global::PlantsVsZombies.Simulation.Zombie(); }", "actor creation", "global zombie factory", "GameSimulation");
            var aliasedSimulation = "#nullable enable\r\nusing Species = PlantsVsZombies.Simulation.ZombieKind;\r\nusing Actor = PlantsVsZombies.Simulation.Zombie;\r\nnamespace PlantsVsZombies.Example\r\n{\r\n    public class GameSimulation\r\n    {\r\n        private bool Has(Species kind) { if (kind == Species.Normal) { return true; } return false; }\r\n    }\r\n}\r\n";
            Require(ProjectArchitecture.InspectSource("GameSimulation.cs", aliasedSimulation).Any(message => message.Contains("species dispatch", StringComparison.Ordinal)), "aliased simulation kind dispatch");
            var aliasedFactory = aliasedSimulation.Replace("private bool Has(Species kind) { if (kind == Species.Normal) { return true; } return false; }", "private object Spawn() { return new Actor(); }", StringComparison.Ordinal);
            Require(ProjectArchitecture.InspectSource("GameSimulation.cs", aliasedFactory).Any(message => message.Contains("actor creation", StringComparison.Ordinal)), "aliased simulation actor factory");
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
            var coordinator = valid.Replace("Sample", "PvZGame", StringComparison.Ordinal);
            Require(ProjectArchitecture.InspectSource("PvZGame.cs", coordinator).Count == 0, "compact coordinator");
            var bloated = coordinator.Replace("    public class PvZGame", string.Concat(Enumerable.Repeat("    // filler\r\n", 710)) + "    public class PvZGame", StringComparison.Ordinal);
            Require(ProjectArchitecture.InspectSource("PvZGame.cs", bloated).Any(message => message.Contains("coordinates collaborators", StringComparison.Ordinal)), "oversized coordinator");
            RequirePresentationRule("private void OnGUI() {}", "OnGUI", "runtime GUI callback");
            RequirePresentationRule("private void Present() { GUI.DrawTexture(default, default); }", "IMGUI access", "runtime GUI drawing");
            RequirePresentationRule("private void Present() { UnityEngine.GUI.skin.ToString(); }", "IMGUI access", "runtime GUI skin access");
            RequirePresentationRule("private void ReadInput() { var pointer = Event.current.mousePosition; }", "IMGUI access", "GUI-event input");
            RequirePresentationRule("private void ReadInput() { var pointer = UnityEngine.Event.current.mousePosition; }", "IMGUI access", "qualified GUI-event input");
            RequirePresentationRule("private void Present() { var style = new GUIStyle(); }", null, "passive GUI style data");
            RequirePresentationRule("private void Present() { CanvasUI.DrawTexture(default, default); }", null, "Canvas presenter drawing");
            RequirePresentationRule("private void DrawBoard() {}", "delegate drawing", "coordinator drawing method", "PvZGame");
            RequirePresentationRule("private void Present() { _renderer.DrawQuad(default); }", "drawing commands", "coordinator rendering submission", "PvZGame");
            RequirePresentationRule("private void Update() { _presenter.Present(); }", null, "coordinator presenter delegation", "PvZGame");
            var aliasedGui = "#nullable enable\r\nusing Legacy = UnityEngine.GUI;\r\nnamespace PlantsVsZombies.Example\r\n{\r\n    public class Sample\r\n    {\r\n        private void Present() { Legacy.DrawTexture(default, default); }\r\n    }\r\n}\r\n";
            Require(ProjectArchitecture.InspectSource("Sample.cs", aliasedGui).Any(message => message.Contains("IMGUI access", StringComparison.Ordinal)), "aliased GUI drawing");
            var importedGui = aliasedGui.Replace("using Legacy = UnityEngine.GUI;", "using static UnityEngine.GUI;", StringComparison.Ordinal);
            Require(ProjectArchitecture.InspectSource("Sample.cs", importedGui).Any(message => message.Contains("IMGUI imports", StringComparison.Ordinal)), "static GUI import");
        }

        /// <summary>Exercises a presentation ownership rule with a minimal source declaration.</summary>
        /// <param name="member">The method or field under test.</param>
        /// <param name="expectedViolation">The required violation fragment, or null for a valid declaration.</param>
        /// <param name="name">The purpose shown when the fixture fails.</param>
        /// <param name="type">The class declaring the fixture member.</param>
        /// <exception cref="InvalidOperationException">The rule fails to distinguish the fixture.</exception>
        private static void RequirePresentationRule(string member, string? expectedViolation, string name, string type = "Sample")
        {
            var source = "#nullable enable\r\nnamespace PlantsVsZombies.Example\r\n{\r\n    public class " + type + "\r\n    {\r\n        " + member + "\r\n    }\r\n}\r\n";
            var violations = ProjectArchitecture.InspectSource(type + ".cs", source);
            Require(expectedViolation == null
                ? violations.Count == 0
                : violations.Any(message => message.Contains(expectedViolation, StringComparison.Ordinal)), name);
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
