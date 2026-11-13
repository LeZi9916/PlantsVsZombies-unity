#nullable enable

using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the original linked sled's initialization, boarding, durability, and debris lifecycle.</summary>
    public sealed class AdventureBobsledTests
    {
        /// <summary>Checks PC 53DD90 keeps four riders fifty pixels apart and gives only the leader sled armor.</summary>
        [Test]
        public void NativeTeamSpacingHeightsAndDurabilityAreRetained()
        {
            var game = Team();
            var leader = game.Zombies[0];
            Assert.That(AdventureZombieNativeRules.NativePositionX(leader.X), Is.EqualTo(880f).Within(.001f));
            for (var index = 0; index < 4; index++)
            {
                var member = game.Zombies[index];
                Assert.That((member.X - leader.X) * 80f, Is.EqualTo(index * 50f).Within(.001f));
                Assert.That(member.PresentationAltitude, Is.EqualTo(index == 0 ? -10f : index == 2 ? -7f : 9f));
                Assert.That(member.ArmorHealth, Is.EqualTo(index == 0 ? 300f : 0f));
                Assert.That(member.BobsledState!.Position, Is.EqualTo(index));
                Assert.That(member.BobsledState.LeaderId, Is.EqualTo(leader.Id));
                Assert.That(AdventureZombieTargetingRules.CanBeAutoTargeted(member), Is.EqualTo(index == 0));
            }
        }

        /// <summary>Checks five hundred native pushes lead to the original held twenty-fps boarding jump.</summary>
        [Test]
        public void BoardingStartsAtFiveHundredAndUsesOriginalAuthoredJump()
        {
            var game = Team();
            var leader = game.Zombies[0];
            for (var tick = 0; tick < 499; tick++)
            {
                Invoke(game, "AdvanceAdventureBobsled", leader);
            }

            Assert.That(leader.PresentationNativePhase, Is.EqualTo(17));
            Invoke(game, "AdvanceAdventureBobsled", leader);
            Assert.That(leader.PresentationNativePhase, Is.EqualTo(18));
            Assert.That(game.AdventurePresentationZombieAnimation(leader), Is.EqualTo("anim_jump"));
            Assert.That(game.AdventurePresentationZombieRate(leader), Is.EqualTo(20f));
            Assert.That(AdventureZombieGroundData.TryGet("Zombie_bobsled", "anim_jump", out var clip), Is.True);
            Assert.That(clip!.Count, Is.EqualTo(24));
            leader.PresentationAnimationAge = .6f;
            Invoke(game, "AdvanceAdventureBobsled", leader);
            Assert.That(leader.PresentationAltitude, Is.EqualTo(4.5f));
            leader.PresentationAnimationAge = 1.2f;
            Invoke(game, "AdvanceAdventureBobsled", leader);
            Assert.That(leader.PresentationAltitude, Is.EqualTo(18f));
        }

        /// <summary>Checks the native ice-edge wear is six silent armor points and refreshes the row for five seconds.</summary>
        [Test]
        public void LeavingIceWearsOnlySharedSledArmor()
        {
            var game = Team();
            var leader = game.Zombies[0];
            leader.X = AdventureZombieNativeRules.GridX(500f);
            game.Adventure._ice[0].MinX = 600;
            game.Adventure._ice[0].RemainingTicks = 100;
            Invoke(game, "AdvanceAdventureBobsled", leader);
            Assert.That(leader.ArmorHealth, Is.EqualTo(294f));
            Assert.That(leader.Health, Is.EqualTo(270f));
            Assert.That(leader.PresentationFlash, Is.Zero);
            Assert.That(game.Adventure._ice[0].RemainingTicks, Is.EqualTo(500));
            Invoke(game, "AdvanceAdventureBobsled", game.Zombies[1]);
            Assert.That(game.Zombies[1].Health, Is.EqualTo(270f));
            game.Adventure._ice.Clear();
            Invoke(game, "AdvanceAdventureBobsled", leader);
            Assert.That(leader.ArmorHealth, Is.EqualTo(288f), "A completely melted row retains the native eight-hundred-pixel boundary.");
            Assert.That(game.Adventure.IceTrails, Is.Empty);
        }

        /// <summary>Checks all four bodies survive the original one-hundred-fifty-update sled destruction.</summary>
        [Test]
        public void BrokenSledFadesThenDetachesFourWalkingZombies()
        {
            var game = Team();
            var leader = game.Zombies[0];
            Invoke(game, "HitAdventureZombie", leader, 300f, false, 0U);
            foreach (var member in game.Zombies)
            {
                Assert.That(member.Health, Is.EqualTo(270f));
                Assert.That(member.PresentationNativePhase, Is.EqualTo(19));
                Assert.That(member.BobsledState!.PhaseTicks, Is.EqualTo(150));
                Assert.That(member.PresentationAltitude, Is.Zero);
                Assert.That(game.AdventurePresentationZombieAnimation(member), Is.EqualTo("anim_walk"));
                for (var tick = 0; tick < 149; tick++)
                {
                    Invoke(game, "AdvanceAdventureBobsled", member);
                }

                Assert.That(member.BobsledState.PhaseTicks, Is.EqualTo(1));
                Invoke(game, "AdvanceAdventureBobsled", member);
                Assert.That(member.BobsledState, Is.Null);
                Assert.That(member.PresentationHasSled, Is.False);
                Assert.That(member.UsedAbility, Is.True);
                Assert.That(member.LinkedZombieId, Is.Zero);
                Assert.That(member.PresentationNativeVelocity, Is.InRange(.23f, .37f));
            }

            Assert.That(game.Zombies, Has.Count.EqualTo(4));
            Assert.That(AdventureBobsledRules.CrashOpacity(30), Is.EqualTo(1f));
            Assert.That(AdventureBobsledRules.CrashOpacity(15), Is.EqualTo(127f / 255f));
            Assert.That(AdventureBobsledRules.CrashOpacity(0), Is.Zero);
        }

        /// <summary>Checks the vehicle's front shell swaps from the middle member to the leader halfway through boarding.</summary>
        [Test]
        public void VehicleLayersFollowOriginalRiderPainterOrder()
        {
            Assert.That(new[] { AdventureBobsledRules.PositionAtRenderOrder(0), AdventureBobsledRules.PositionAtRenderOrder(1),
                AdventureBobsledRules.PositionAtRenderOrder(2), AdventureBobsledRules.PositionAtRenderOrder(3) },
                Is.EqualTo(new[] { 3, 1, 2, 0 }));
            Assert.That(AdventureBobsledRules.DrawLayers(2, 17, 0f, true), Is.EqualTo(3));
            Assert.That(AdventureBobsledRules.DrawLayers(2, 18, .49f, true), Is.EqualTo(3));
            Assert.That(AdventureBobsledRules.DrawLayers(2, 18, .5f, true), Is.Zero);
            Assert.That(AdventureBobsledRules.DrawLayers(3, 18, .5f, true), Is.EqualTo(2));
            Assert.That(AdventureBobsledRules.DrawLayers(0, 18, .5f, false), Is.EqualTo(1));
            Assert.That(AdventureBobsledRules.DrawLayers(0, 19, 0f, false), Is.EqualTo(1));
            Assert.That(AdventureBobsledRules.CrashOffsetX(150, .6f), Is.Zero);
            Assert.That(AdventureBobsledRules.CrashOffsetX(0, .6f), Is.EqualTo(-5f).Within(.001f));
        }

        /// <summary>Checks linked deaths leave no orphaned riders and preserve every burned body without extra loot.</summary>
        [Test]
        public void LinkedTeamDiesTogetherBeforeCrashDetachment()
        {
            var game = Team();
            Invoke(game, "KillAdventureZombie", game.Zombies[0], 0U);
            Assert.That(game.Zombies, Is.Empty);

            var burning = Team();
            var single = Team();
            single.Zombies.RemoveRange(1, 3);
            single.Zombies[0].BobsledState = null;
            Invoke(burning, "HitAdventureExplosionZombie", burning.Zombies[0], 1800f, true, 0x12U);
            Invoke(single, "HitAdventureExplosionZombie", single.Zombies[0], 1800f, true, 0x12U);
            Assert.That(burning.Zombies, Is.Empty);
            Assert.That(burning.Presentation.Corpses, Has.Count.EqualTo(4));
            foreach (var corpse in burning.Presentation.Corpses)
            {
                Assert.That(corpse.RequiredZombie.PresentationDeath, Is.EqualTo(AdventureZombieDeathPresentation.Burned));
                Assert.That(corpse.RequiredBurn.Visual, Is.EqualTo(AdventureBurnVisual.FrozenOriginal));
            }

            Assert.That(burning.BattleRandomDrawCount, Is.EqualTo(single.BattleRandomDrawCount),
                "The three linked burn deaths must not consume extra loot rolls.");
            Assert.That(burning.Adventure.Coins.Count, Is.EqualTo(single.Adventure.Coins.Count));
        }

        /// <summary>Spawns a real scheduled sled team on the only eligible ice row.</summary>
        /// <returns>The active deterministic board containing four original linked riders.</returns>
        private static GameSimulation Team()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 28 }, 7529);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            game.Adventure._ice.Add(new AdventureIceTrail { Row = 1, MinX = 600, RemainingTicks = 3000 });
            Invoke(game, "SpawnScheduledAdventureWave", new AdventureWave { Index = 0, Zombies = new[] { ZombieKind.Bobsled } });
            Assert.That(game.Zombies, Has.Count.EqualTo(4));
            return game;
        }

        /// <summary>Invokes an existing native production operation without adding a runtime test API.</summary>
        /// <param name="game">The board owning the native operation.</param>
        /// <param name="name">The nonpublic operation name.</param>
        /// <param name="arguments">The original operation arguments.</param>
        /// <returns>The production operation's result.</returns>
        private static object? Invoke(GameSimulation game, string name, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
