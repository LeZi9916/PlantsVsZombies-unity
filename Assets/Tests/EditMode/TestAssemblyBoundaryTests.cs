#nullable enable
using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Game;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Protects the separation between shipping game code and Test Runner fixtures.</summary>
    public sealed class TestAssemblyBoundaryTests
    {
        /// <summary>Checks that production code contains neither the moved diagnostics nor test framework references.</summary>
        [Test]
        public void RuntimeDoesNotShipTestCasesOrReflectionHelpers()
        {
            var runtime = typeof(PvZGame).Assembly;
            Assert.That(runtime.GetTypes().Where(type => type.Namespace == "PlantsVsZombies.Diagnostics"), Is.Empty);
            foreach (var name in new[] { "SimulationFixture", "SimulationInspection", "SimulationMember" })
            {
                Assert.That(runtime.GetType("PlantsVsZombies.Simulation." + name), Is.Null);
            }
            Assert.That(runtime.GetReferencedAssemblies().Where(assembly =>
                assembly.Name!.IndexOf("nunit", StringComparison.OrdinalIgnoreCase) >= 0
                || assembly.Name.StartsWith("PlantsVsZombies.Tests", StringComparison.Ordinal)), Is.Empty);
        }

        /// <summary>Checks that each migrated scenario exposes an ordinary discoverable PlayMode test.</summary>
        [Test]
        public void MigratedSmokeScenariosAreDiscoverableWithoutRuntimeBootstrap()
        {
            var assembly = Assembly.Load("PlantsVsZombies.Tests.PlayMode");
            var names = new[] { "AdventurePlayerSmoke", "CombatFidelityPlayerSmoke", "FeedbackPlayerSmoke",
                "IntroAudioPlayerSmoke", "ProgressionPlayerSmoke", "VisualSmokeTest", "UISmokeTest", "LevelOneVisualSmokeTest" };
            foreach (var name in names)
            {
                var type = assembly.GetType("PlantsVsZombies.Tests.PlayMode.Smoke." + name, true)!;
                Assert.That(type.IsPublic, Is.True, name);
                Assert.That(type.GetMethods().Any(method => method.GetCustomAttributesData().Any(attribute =>
                    attribute.AttributeType.FullName == "UnityEngine.TestTools.UnityTestAttribute")), Is.True, name);
                Assert.That(type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Any(method => method.GetCustomAttributesData().Any(attribute =>
                        attribute.AttributeType.FullName == "UnityEngine.RuntimeInitializeOnLoadMethodAttribute")), Is.False, name);
            }
        }
    }
}
