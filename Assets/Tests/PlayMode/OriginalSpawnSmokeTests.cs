#nullable enable
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Simulation;
using System.Collections;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Exercises repaired wave generation and row selection through public mode sessions during Play Mode.</summary>
    public sealed class OriginalSpawnSmokeTests
    {
        /// <summary>Checks real opening waves without synthetic kills, injected entities, or adjusted health.</summary>
        /// <returns>The Unity coroutine that yields between independently seeded mode sessions.</returns>
        [UnityTest]
        public IEnumerator IndependentModesSpawnThroughTheirPublicUpdateLoop()
        {
            for (var mode = 1; mode <= 34; mode++)
            {
                if (mode == 23)
                {
                    continue;
                }

                var session = new ModeSession(mode, randomSeed: 12345);
                if (session.Phase == ModePhase.ChoosingSeeds)
                {
                    Assert.That(session.ChooseSeeds(session.Game.Adventure.SelectedSeeds), Is.True, "Mode " + mode + " accepts its default cards.");
                }

                if (session.Phase == ModePhase.Preparing)
                {
                    Assert.That(session.BeginDefense(), Is.True, "Last Stand begins through its public action.");
                }

                // Without defensive actions, Pogo Party can lose before its second wave.
                // Only the two native 750-tick modes require a second wave in this input script.
                var requiredWaves = mode == 27 || mode == 31 ? 2 : 1;
                var deadline = requiredWaves == 2 ? 1600 : 6000;
                for (var tick = 0; tick < deadline && session.Game.Adventure.CurrentWave < requiredWaves && session.Game.State == MatchState.Playing; tick++)
                {
                    session.Update(0.01f);
                }

                Assert.That(session.Game.Adventure.CurrentWave, Is.GreaterThanOrEqualTo(requiredWaves), "Mode " + mode + " advances its opening waves within the native timing script.");
                Assert.That(session.Game.SpawnedZombies, Is.GreaterThan(0), "Mode " + mode + " creates real zombie entities.");
                foreach (var zombie in session.Game.Zombies)
                {
                    Assert.That(zombie.Row, Is.InRange(0, session.Game.RowCount - 1), "Mode " + mode + " creates entities in board lanes.");
                }

                yield return null;
            }
        }
    }
}
