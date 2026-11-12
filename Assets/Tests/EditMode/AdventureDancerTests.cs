#nullable enable

using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the local PC dancer entrance, linked summons, shared timing, and original size overrides.</summary>
    public sealed class AdventureDancerTests
    {
        /// <summary>Checks both dancer previews select the authored raised-arm clip and native scale.</summary>
        /// <param name="kind">The original dancer identifier.</param>
        /// <param name="small">Whether the later Little Trouble override applies.</param>
        /// <param name="scale">The original resulting scale.</param>
        [TestCase(8, false, .8f)]
        [TestCase(9, false, .8f)]
        [TestCase(8, true, .5f)]
        [TestCase(9, true, .5f)]
        public void StreetPreviewUsesOriginalArmRaiseAndSize(int kind, bool small, float scale)
        {
            var preview = AdventureIntroPresentation.PlaceStreetZombie(new AdventureIntroContext { IsLittleTrouble = small },
                kind, 0, 1, _ => 0, _ => 0f, (_, clip) => clip == "anim_armraise");
            Assert.That(preview.Animation, Is.EqualTo("anim_armraise"));
            Assert.That(preview.AnimationRate, Is.EqualTo(12f));
            Assert.That(preview.InitialPhase, Is.Zero);
            Assert.That(preview.Loop, Is.True);
            Assert.That(preview.Scale, Is.EqualTo(scale));
        }

        /// <summary>Checks the local executable's merged raised-arm phases at every beat boundary.</summary>
        /// <param name="ticks">The shared app-counter value in native updates.</param>
        /// <param name="frame">The expected original dance frame.</param>
        /// <param name="phase">The expected local PC phase.</param>
        [TestCase(0, 0, 44)]
        [TestCase(239, 11, 44)]
        [TestCase(240, 12, 45)]
        [TestCase(259, 12, 45)]
        [TestCase(260, 13, 46)]
        [TestCase(379, 18, 46)]
        [TestCase(380, 19, 48)]
        [TestCase(459, 22, 48)]
        [TestCase(460, 0, 44)]
        public void DanceBeatFollowsLocalPcCounter(int ticks, int frame, int phase)
        {
            var zombie = new Zombie { Kind = ZombieKind.BackupDancer };
            AdventureDancerRules.Initialize(zombie, 0);
            Assert.That(AdventureDancerRules.Frame(zombie, ticks), Is.EqualTo(frame));
            Assert.That(AdventureDancerRules.Phase(frame), Is.EqualTo(phase));
            zombie.FrozenRemaining = 1f;
            Assert.That(AdventureDancerRules.Frame(zombie, ticks), Is.Zero);
        }

        /// <summary>Checks the production spawn and tick path plays entrance and pointing before four backups emerge.</summary>
        [Test]
        public void EntrancePointsBeforeSummoningFourOwnedBackups()
        {
            var game = CreateGame();
            var leader = Spawn(game, ZombieKind.Dancing, 2, 7f);
            Assert.That(leader.PresentationNativePhase, Is.EqualTo(40));
            Assert.That(leader.DancerState!.PhaseTicks, Is.InRange(300, 311));
            Assert.That(game.AdventurePresentationZombieAnimation(leader), Is.EqualTo("anim_moonwalk"));
            Assert.That(game.AdventurePresentationZombieRate(leader), Is.EqualTo(24f));
            Assert.That(AdventureDancerRules.FacesRight(leader), Is.True);
            Assert.That(AdventureDancerRules.Scale(leader), Is.EqualTo(.8f));

            leader.DancerState.PhaseTicks = 1;
            Tick(game);
            Assert.That(leader.PresentationNativePhase, Is.EqualTo(41));
            Assert.That(game.AdventurePresentationZombieAnimation(leader), Is.EqualTo("anim_point"));
            var pointX = leader.X;
            for (var tick = 0; tick < 37; tick++)
            {
                Tick(game);
            }

            Assert.That(game.Zombies.Count, Is.EqualTo(1));
            Tick(game);
            Assert.That(leader.PresentationNativePhase, Is.EqualTo(43));
            Assert.That(leader.DancerState.PhaseTicks, Is.EqualTo(200));
            Assert.That(leader.X, Is.EqualTo(pointX), "Pointing holds the original horizontal position.");
            Assert.That(game.Zombies.Count, Is.EqualTo(5));
            Assert.That(game.Presentation.ZombieRiseEffects.Count, Is.EqualTo(4));
            Assert.That(game.Presentation.ZombieSounds.Exists(sound => sound.Foley == 100), Is.True);

            var nativeX = (int)AdventureZombieNativeRules.NativePositionX(leader.X, .8f);
            for (var slot = 0; slot < 4; slot++)
            {
                var backup = AdventureDancerRules.Find(game.Zombies, leader.DancerState.Followers[slot])!;
                Assert.That(backup.DancerState!.LeaderId, Is.EqualTo(leader.Id));
                Assert.That(backup.PresentationNativePhase, Is.EqualTo(50));
                Assert.That(backup.DancerState.PhaseTicks, Is.EqualTo(150));
                Assert.That(backup.Row, Is.EqualTo(leader.Row + (slot == 0 ? -1 : slot == 1 ? 1 : 0)));
                Assert.That(AdventureZombieNativeRules.NativePositionX(backup.X, .8f),
                    Is.EqualTo(nativeX + (slot == 2 ? -100 : slot == 3 ? 100 : 0)).Within(.001f));
                Assert.That(AdventureZombieTargetingRules.CanBeAutoTargeted(backup), Is.False);
            }
        }

        /// <summary>Checks a backup remains stationary and immune to ordinary hits until its original emergence ends.</summary>
        [Test]
        public void BackupRisesForOneHundredFiftyUpdatesBeforeWalking()
        {
            var game = CreateGame();
            var backup = Spawn(game, ZombieKind.BackupDancer, 2, 6f);
            backup.PresentationNativePhase = 50;
            backup.DancerState!.PhaseTicks = 150;
            var x = backup.X;
            var health = backup.Health;
            Invoke(game, "HitAdventureZombie", backup, 20f, false, 0U);
            Assert.That(backup.Health, Is.EqualTo(health));
            Invoke(game, "HitAdventureZombie", backup, 20f, true, 0x12U);
            Assert.That(backup.Health, Is.EqualTo(health - 20f), "The original 0x10 transient-phase damage mask remains effective.");
            Assert.That(AdventureDancerRules.RiseAltitude(150), Is.EqualTo(-200));
            Assert.That(AdventureDancerRules.RiseAltitude(75), Is.EqualTo(-100));
            Assert.That(AdventureDancerRules.RiseAltitude(0), Is.Zero);
            for (var tick = 0; tick < 149; tick++)
            {
                Tick(game);
            }

            Assert.That(backup.PresentationNativePhase, Is.EqualTo(50));
            Assert.That(backup.X, Is.EqualTo(x));
            Tick(game);
            Assert.That(backup.PresentationNativePhase, Is.EqualTo(44));
            Assert.That(backup.DancerState.PhaseTicks, Is.Zero);
            Assert.That(AdventureZombieTargetingRules.CanBeAutoTargeted(backup), Is.True);
        }

        /// <summary>Checks a leader owns its own slots and retains dying followers until their removal.</summary>
        [Test]
        public void VacantSlotsDoNotAdoptNearbyDancersOrReplaceDyingFollowers()
        {
            var game = CreateGame();
            var leader = Spawn(game, ZombieKind.Dancing, 2, 6f);
            var unrelated = Spawn(game, ZombieKind.BackupDancer, 2, 6f);
            leader.PresentationNativePhase = 44;
            leader.DancerState!.PhaseTicks = 0;
            Assert.That(AdventureDancerRules.NeedsBackup(leader, game.Zombies, game.Presentation.Corpses, game.Adventure.Level), Is.True);
            for (var slot = 0; slot < 4; slot++)
            {
                leader.DancerState.Followers[slot] = unrelated.Id;
            }

            Assert.That(AdventureDancerRules.NeedsBackup(leader, game.Zombies, game.Presentation.Corpses, game.Adventure.Level), Is.False);
            game.Zombies.Remove(unrelated);
            game.Presentation.Corpses.Add(new AdventureZombieCorpse { Zombie = unrelated });
            Assert.That(AdventureDancerRules.NeedsBackup(leader, game.Zombies, game.Presentation.Corpses, game.Adventure.Level), Is.False);
            game.Presentation.Corpses.Clear();
            Assert.That(AdventureDancerRules.NeedsBackup(leader, game.Zombies, game.Presentation.Corpses, game.Adventure.Level), Is.True);
            AdventureDancerRules.Tick(leader, 0, true, false);
            Assert.That(leader.DancerState.SummonTicks, Is.EqualTo(100));
            leader.DancerState.SummonTicks = 1;
            AdventureDancerRules.Tick(leader, 239, true, false);
            Assert.That(leader.PresentationNativePhase, Is.EqualTo(44));
            AdventureDancerRules.Tick(leader, 240, true, false);
            Assert.That(leader.PresentationNativePhase, Is.EqualTo(42));
            Assert.That(game.AdventurePresentationZombieAnimation(leader), Is.EqualTo("anim_point"));
        }

        /// <summary>Checks the original asymmetric mind-control detach and shared freeze/chill relationship.</summary>
        [Test]
        public void MindControlAndLeaderRemovalPreserveOriginalLinkSemantics()
        {
            var leader = new Zombie { Id = 1, Kind = ZombieKind.Dancing };
            var backup = new Zombie { Id = 2, Kind = ZombieKind.BackupDancer };
            AdventureDancerRules.Initialize(leader, 0);
            AdventureDancerRules.Initialize(backup, 0);
            var zombies = new List<Zombie> { leader, backup };
            leader.PresentationNativePhase = 44;
            leader.DancerState!.Followers[0] = backup.Id;
            backup.DancerState!.LeaderId = leader.Id;
            backup.FrozenRemaining = 1f;
            Assert.That(AdventureDancerRules.CanWalk(leader, zombies), Is.False);
            backup.FrozenRemaining = 0f;
            backup.ChillRemaining = 1f;
            Assert.That(AdventureDancerRules.GroupAffected(leader, zombies, true), Is.True);

            leader.Hypnotized = true;
            AdventureDancerRules.DetachForMindControl(leader, zombies);
            Assert.That(leader.DancerState.Followers, Is.EqualTo(new int[4]));
            Assert.That(backup.DancerState.LeaderId, Is.EqualTo(leader.Id), "PC 0x54B7E0 clears only the leader's four slots.");
            leader.FrozenRemaining = 1f;
            Assert.That(AdventureDancerRules.CanWalk(backup, zombies), Is.False);
            zombies.Remove(leader);
            Assert.That(AdventureDancerRules.CanWalk(backup, zombies), Is.True);
            AdventureDancerRules.DetachForMindControl(backup, zombies);
            Assert.That(backup.DancerState.LeaderId, Is.Zero);
        }

        /// <summary>Checks the original left-edge row restrictions and inherited mind control of new backups.</summary>
        [Test]
        public void HypnotizedLeaderSummonsFriendlyBackupsOnlyOnExistingLandRows()
        {
            var game = CreateGame();
            var leader = Spawn(game, ZombieKind.Dancing, 0, 6f);
            leader.Hypnotized = true;
            leader.PresentationNativePhase = 42;
            leader.DancerState!.PhaseAge = 1d;
            Tick(game);
            Assert.That(game.Zombies.Count, Is.EqualTo(4));
            foreach (var backup in game.Zombies)
            {
                Assert.That(backup.Hypnotized, Is.True);
            }

            leader.Headless = true;
            leader.PresentationNativePhase = 44;
            leader.DancerState.SummonTicks = 0;
            AdventureDancerRules.Tick(leader, 0, true, false);
            Assert.That(leader.DancerState.SummonTicks, Is.Zero, "A headless leader cannot begin a new summon.");
        }

        /// <summary>Checks losing the head during pointing suppresses the summon and normal death leaves dance phases.</summary>
        [Test]
        public void HeadlessPointDoesNotSummonAndNormalDeathStopsMirroring()
        {
            var game = CreateGame();
            var leader = Spawn(game, ZombieKind.Dancing, 2, 6f);
            leader.Headless = true;
            leader.PresentationNativePhase = 42;
            leader.DancerState!.PhaseAge = 1d;
            Tick(game);
            Assert.That(leader.PresentationNativePhase, Is.EqualTo(43));
            Assert.That(game.Zombies.Count, Is.EqualTo(1));

            leader.PresentationNativePhase = 40;
            Invoke(game, "HitAdventureZombie", leader, 10000f, true, 0U);
            Assert.That(game.Presentation.Corpses.Count, Is.EqualTo(1));
            Assert.That(leader.PresentationNativePhase, Is.EqualTo(1));
            Assert.That(AdventureDancerRules.FacesRight(leader), Is.False);
        }

        /// <summary>Creates a night board with no automatic waves or plants near the tested dancers.</summary>
        /// <returns>The isolated production simulation.</returns>
        private static GameSimulation CreateGame()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 14 }, 4231);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            game.Zombies.Clear();
            game.Plants.Clear();
            return game;
        }

        /// <summary>Creates a dancer through the production spawn path.</summary>
        /// <param name="game">The isolated board fixture.</param>
        /// <param name="kind">The dancer species.</param>
        /// <param name="row">The requested lawn row.</param>
        /// <param name="x">The requested horizontal grid coordinate.</param>
        /// <returns>The live spawned dancer.</returns>
        private static Zombie Spawn(GameSimulation game, ZombieKind kind, int row, float x)
        {
            return (Zombie)Invoke(game, "SpawnAdventureZombie", kind, row, x, 0)!;
        }

        /// <summary>Advances the production zombie update by one native tick.</summary>
        /// <param name="game">The isolated simulation to advance.</param>
        private static void Tick(GameSimulation game)
        {
            Invoke(game, "AdvanceAdventureZombies", .01f);
        }

        /// <summary>Invokes a production simulation hook without widening its runtime visibility.</summary>
        /// <param name="game">The isolated board fixture.</param>
        /// <param name="name">The private production hook.</param>
        /// <param name="arguments">The hook's original arguments.</param>
        /// <returns>The hook's optional return value.</returns>
        private static object? Invoke(GameSimulation game, string name, params object[] arguments)
        {
            return typeof(GameSimulation).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
