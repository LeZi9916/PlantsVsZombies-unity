#nullable enable

using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks PC 475AA0/4765B0 horizontal bite bounds independently of plant drawing offsets.</summary>
    public sealed class AdventureChomperRangeTests
    {
        /// <summary>Checks idle, active-bite and eating-target boundaries on both roof segments and ordinary ground.</summary>
        /// <param name="level">The original board background.</param>
        /// <param name="column">The plant's native column.</param>
        [TestCase(9, 4)]
        [TestCase(19, 4)]
        [TestCase(49, 0)]
        [TestCase(49, 4)]
        [TestCase(49, 8)]
        public void NativeBiteIntervalIsUnchangedByRoofOrFlowerPot(int level, int column)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
            game.ChooseDefaultAdventureSeeds();
            game.Plants.Clear();
            game.Zombies.Clear();
            if (level == 49)
            {
                Invoke(game, "PlaceAdventurePlant", PlantKind.FlowerPot, 1, column);
            }

            var plant = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.Chomper, 1, column)!;
            var zombie = (Zombie)Invoke(game, "SpawnAdventureBoardZombie", ZombieKind.Normal, 1, 5f, 0)!;
            // PC attack [mX+80,mX+120] intersects normal body [zX+36,zX+78], including touching edges.
            // An active bite or an eating target permits sixty extra pixels; EffectedByDamage still rejects body X > 800.
            foreach (var state in new[] { (false, false, 0), (true, false, 60), (false, true, 60) })
            {
                zombie.Eating = state.Item2;
                foreach (var probe in new[] { (1 - state.Item3, false), (2 - state.Item3, true),
                    (84 + state.Item3, true), (85 + state.Item3, false) })
                {
                    var nativeX = 40 + column * 80 + probe.Item1;
                    zombie.X = AdventureZombieNativeRules.GridX(nativeX);
                    Assert.That(Invoke(game, "FindAdventureChomperTarget", plant, state.Item1),
                        probe.Item2 && nativeX + 36 <= 800 ? Is.SameAs(zombie) : Is.Null,
                        "Level " + level + ", column " + column + ", native X " + nativeX + ", state " + state);
                }
            }
        }

        /// <summary>Invokes original production targeting without substituting collision logic.</summary>
        /// <param name="game">The original board.</param>
        /// <param name="method">The production entry point.</param>
        /// <param name="arguments">The original arguments.</param>
        /// <returns>The unmodified production result.</returns>
        private static object? Invoke(GameSimulation game, string method, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
