#nullable enable

using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks original PC boss entrance, damage, ball counters and complete death contacts.</summary>
    public sealed class AdventureBossFidelityTests
    {
        /// <summary>Checks PlayBossEnter occurs after the empty street preview at the original return boundary.</summary>
        /// <param name="dave">The original optional Dave delay.</param>
        [TestCase(0)]
        [TestCase(6500)]
        public void EntranceStartsAtOriginalCutsceneBoundaryAndHoldsUntilPlaying(int dave)
        {
            var game = Board(true);
            var plan = AdventureIntroPresentation.CreatePlan(new AdventureIntroContext { Level = 50, NativeBackground = 5,
                IsFinalBoss = true, IsConveyor = true, ChooseSeedsOverride = false, CrazyDaveTimeOverride = dave });
            var clock = new AdventureIntroClock(plan);
            Assert.That(game.Adventure.Boss, Is.Null, "The original roof preview contains no boss or street zombies.");
            for (var time = 10; time < plan.BossEnterStart; time += 10)
            {
                clock.Tick();
            }
            clock.Tick(emit: evt =>
            {
                if (evt == AdventureIntroEvent.BossEnterStarted)
                {
                    game.BeginAdventureBossEntrance();
                }
            });
            var boss = game.Adventure.Boss!;
            Assert.That(clock.NativeTime, Is.EqualTo(6550 + dave));
            Assert.That(boss.Animation, Is.EqualTo("anim_enter"));
            Assert.That(boss.FrameCount(), Is.EqualTo(40));
            for (var i = 0; i < 580; i++)
            {
                game.AdvanceNativeFeedbackClock();
                game.AdvanceAdventureBossIntro();
            }
            Assert.That(boss.Phase, Is.EqualTo(78));
            Assert.That(boss.SummonCountdown, Is.EqualTo(500));
            Assert.That(boss.HeadCountdown, Is.EqualTo(5000));
            Assert.That(game.Zombies, Is.Empty);
            Assert.That(game.Presentation.ZombieSounds.Count(sound => sound.Foley == 40), Is.EqualTo(2));
            Call(game, "AdvanceAdventureBoss", .01f);
            Assert.That(boss.Phase, Is.EqualTo(79));
            Assert.That(boss.Animation, Is.EqualTo("anim_idle"));
        }

        /// <summary>Checks PC 54D810 admits only the three exposed head phases.</summary>
        /// <param name="phase">The original phase number.</param>
        /// <param name="exposed">The original damage admission result.</param>
        [TestCase(78, false)]
        [TestCase(79, false)]
        [TestCase(86, false)]
        [TestCase(87, true)]
        [TestCase(88, true)]
        [TestCase(89, true)]
        [TestCase(90, false)]
        [TestCase(1, false)]
        public void OnlyOriginalHeadPhasesAdmitDamage(int phase, bool exposed)
        {
            Assert.That(new AdventureBossState { Phase = phase }.HeadVulnerable, Is.EqualTo(exposed));
        }

        /// <summary>Checks lethal damage waits for the original retreat, death, white flag and award boundary.</summary>
        /// <param name="phase">The original exposed idle phase at lethal contact.</param>
        [TestCase(87)]
        [TestCase(88)]
        public void LethalHeadDamageCompletesOriginalDeathBeforeReward(int phase)
        {
            var game = Board();
            var boss = game.Adventure.Boss!;
            boss.Enter(phase, "anim_head_idle", 12f, 0);
            boss.PhaseCountdown = 10000;
            boss.BallRow = 2;
            boss.BallIsIce = true;
            game.Adventure._balls.Add(new AdventureBossBall { Row = 2, X = 200f / 80f, IsIce = true, Rolling = true });
            game.Adventure.BossHeadVulnerable = true;
            Call(game, "SpawnAdventureBoardZombie", ZombieKind.Normal, 1, 5f, 0);
            Call(game, "DamageAdventureBossWithFlags", 50000f, 0x7FU);
            Assert.That(game.Adventure.BossHealth, Is.EqualTo(1f));
            Assert.That(game.State, Is.EqualTo(MatchState.Playing));
            game.Update(.01f);
            Assert.That(boss.Phase, Is.EqualTo(90));
            Assert.That(boss.AnimationRate, Is.EqualTo(24f));
            Assert.That(game.Zombies, Is.Empty);
            Assert.That(game.Adventure.BossBalls, Is.Empty);
            Assert.That(boss.Effects.Any(effect => effect.Particle == "IceballDeath" || effect.Particle == null), Is.False,
                "PC BossDie clears the ball before its counter helpers can create a destruction effect.");
            Assert.That(game.Presentation.ZombieSounds.Any(sound => sound.Foley == -10), Is.True);
            Assert.That(game.Presentation.ZombieSounds.Any(sound => sound.Foley == 91), Is.True);
            for (var tick = 0; tick < 1000 && !boss.DeathComplete; tick++)
            {
                game.Update(.01f);
            }
            Assert.That(boss.DeathComplete, Is.True);
            Assert.That(game.Adventure.BossHealth, Is.EqualTo(1f), "The original dying boss retains its health-one sentinel.");
            Assert.That(boss.Animation, Is.EqualTo("anim_death"));
            Assert.That(boss.AnimationRate, Is.EqualTo(18f));
            Assert.That(boss.DriverAnimation, Is.EqualTo("anim_flag"));
            Assert.That(game.State, Is.EqualTo(MatchState.Won));
            Assert.That(game.Adventure.RewardPresentation, Is.Not.Null);
            Call(game, "AdvanceAdventureRewardPresentation", 1f);
            Assert.That(boss.DriverAnimation, Is.EqualTo("anim_flag_loop"));
            Assert.That(boss.DriverRate, Is.EqualTo(17f));
            Assert.That(boss.Effects, Is.Empty, "The last native death particles must finish while the award remains on the board.");
        }

        /// <summary>Checks original world-space ice destruction and the six full fire-reanimation origins.</summary>
        /// <param name="ice">Whether Jalapeno destroys ice instead of Ice-shroom destroying fire.</param>
        [TestCase(false)]
        [TestCase(true)]
        public void BallCountersUseOriginalEffectsAndRemoveTrails(bool ice)
        {
            var game = Board();
            var boss = game.Adventure.Boss!;
            game.Adventure._balls.Add(new AdventureBossBall { Row = 1, X = 100f / 80f, IsIce = ice });
            game.Presentation.ZomboniParticles.Add(new AdventureZomboniParticle { Particle = ice ? "IceballTrail" : "FireballTrail" });
            Call(game, "DestroyBossBalls", ice, 1);
            Assert.That(game.Adventure.BossBalls, Is.Empty);
            Assert.That(game.Presentation.ZomboniParticles, Is.Empty);
            Assert.That(boss.Effects.Count, Is.EqualTo(ice ? 1 : 6));
            if (ice)
            {
                Assert.That(boss.Effects[0].Particle, Is.EqualTo("IceballDeath"));
                Assert.That(boss.Effects[0].X, Is.EqualTo(180f));
                Assert.That(boss.Effects[0].Y, Is.EqualTo(211.25f));
            }
            else
            {
                Assert.That(boss.Effects[0].Particle, Is.Null);
                Assert.That(boss.Effects[0].X, Is.EqualTo(240f).Within(.0001f));
                Assert.That(boss.Effects[0].Y, Is.EqualTo(171.25f).Within(.0001f));
                Assert.That(boss.Effects.All(effect => effect.Rate >= 20f && effect.Rate <= 25f), Is.True);
            }
        }

        /// <summary>Checks damage stages retain PC's distinct foot replacements and suppress ShieldHit for ash flags.</summary>
        [Test]
        public void DamageStagesRetainOriginalImagesAndSoundFlags()
        {
            var game = Board();
            var boss = game.Adventure.Boss!;
            boss.Enter(87, "anim_head_idle", 12f, 0);
            game.Adventure.BossHeadVulnerable = true;
            Call(game, "DamageAdventureBossWithFlags", 12000f, 0x7FU);
            Assert.That(boss.DamageIndex, Is.EqualTo(1));
            Assert.That(boss.DamagedInnerFoot, Is.True);
            Assert.That(game.Presentation.ZombieSounds.Any(sound => sound.Foley == 46), Is.False);
            Call(game, "DamageAdventureBoss", 12000f);
            Assert.That(boss.DamageIndex, Is.EqualTo(2));
            Assert.That(game.Presentation.ZombieSounds.Count(sound => sound.Foley == 46), Is.EqualTo(1));
            Assert.That(boss.Effects.Count(effect => effect.LiveAttachment), Is.EqualTo(2));
            Call(game, "DamageAdventureBossWithFlags", 15000f, 0x7FU);
            Assert.That(boss.Critical, Is.True);
            Assert.That(boss.Effects.Count(effect => effect.LiveAttachment), Is.EqualTo(3));
        }

        /// <summary>Checks the original boss rectangle participates in all five lawn-row lobber queries.</summary>
        [Test]
        public void ExposedHeadUsesOriginalBodyRectangleAndAllRows()
        {
            var game = Board();
            var boss = game.Adventure.Boss!;
            boss.Enter(87, "anim_head_idle", 12f, 0);
            for (var row = 0; row < 5; row++)
            {
                var target = (Zombie)Call(game, "FindAdventureLobberTarget", row, 1)!;
                var rect = AdventureZombieTargetingRules.BodyRectangle(target);
                Assert.That(rect.X, Is.EqualTo(700));
                Assert.That(rect.Width, Is.EqualTo(90));
                Assert.That(target, Is.SameAs(boss.Target));
            }
        }

        /// <summary>Checks the top and bottom lobbers use the original head target lead and actually reach the exposed boss rectangle.</summary>
        /// <param name="row">The first or last original roof lane.</param>
        [TestCase(0)]
        [TestCase(4)]
        public void EdgeRowLobbersReachTheOriginalBossRectangle(int row)
        {
            var game = Board();
            var boss = game.Adventure.Boss!;
            boss.Enter(87, "anim_head_idle", 12f, 0);
            game.Adventure.BossHeadVulnerable = true;
            Call(game, "PlaceAdventurePlant", PlantKind.FlowerPot, row, 1);
            var plant = (Plant)Call(game, "PlaceAdventurePlant", PlantKind.CabbagePult, row, 1)!;
            Call(game, "FireAdventureNativeTimedProjectile", plant);
            var shot = game.Peas[0];
            Assert.That(shot.PresentationLobNativeVelocityX, Is.EqualTo(590f / 120f).Within(.0001f));
            for (var tick = 0; tick < 250 && game.Peas.Count > 0; tick++)
            {
                Call(game, "AdvanceAdventureProjectiles", .01f, true);
            }
            Assert.That(game.Peas, Is.Empty);
            Assert.That(game.Adventure.BossHealth, Is.EqualTo(39960f));
            Assert.That(game.Presentation.Impacts.Single().AttachedZombie, Is.SameAs(boss.Target));
        }

        /// <summary>Checks original boss ball drive-over preserves spikes and leaves the five-second squished mower picture.</summary>
        [Test]
        public void BallDriveOverPreservesSpikesAndSquishesMowerOnce()
        {
            var game = Board();
            SimulationFixture.Plants(game).Clear();
            var boss = game.Adventure.Boss!;
            boss.Enter(79, "anim_idle", 6f, 0);
            boss.PhaseCountdown = 10000;
            var spike = (Plant)Call(game, "PlaceAdventurePlant", PlantKind.Spikerock, 1, 0)!;
            var mower = game.Mowers.First(item => item.Row == 1);
            mower.X = .2f;
            mower.Available = true;
            game.Adventure._balls.Add(new AdventureBossBall { Row = 1, X = 0f, IsIce = true });
            Call(game, "AdvanceAdventureBoss", .01f);
            Assert.That(game.Plants, Does.Contain(spike));
            Assert.That(mower.SquishedTicks, Is.EqualTo(500));
            Assert.That(mower.Available, Is.False);
            Assert.That(game.Presentation.ZombieSounds.Count(sound => sound.Foley == 21), Is.EqualTo(1));
            Call(game, "AdvanceAdventureBoss", .01f);
            Assert.That(game.Presentation.ZombieSounds.Count(sound => sound.Foley == 21), Is.EqualTo(1));
        }

        /// <summary>Checks Ice-shroom retains the original common and newly chilled boss Foley requests and native freeze range.</summary>
        [Test]
        public void IceShroomIncludesOriginalBossChillOnset()
        {
            var game = Board();
            var boss = game.Adventure.Boss!;
            boss.Enter(87, "anim_head_idle", 12f, 0);
            game.Adventure.BossHeadVulnerable = true;
            var plant = (Plant)Call(game, "PlaceAdventurePlant", PlantKind.IceShroom, 1, 0)!;
            Call(game, "DetonateAdventureIceShroom", plant);
            Assert.That(boss.ChilledTicks, Is.EqualTo(2000));
            Assert.That(boss.FrozenTicks, Is.InRange(400, 600));
            Assert.That(game.Adventure.BossHealth, Is.EqualTo(39980f));
            Assert.That(game.Presentation.ZombieSounds.Count(sound => sound.Foley == 18), Is.EqualTo(2));
        }

        /// <summary>Checks PC BurnRow clears the boss's cold counters before BurnBody and restores the unscaled body clock.</summary>
        /// <param name="phase">The original exposed head phase.</param>
        /// <param name="frozen">Whether the boss has an ice trap in addition to chill.</param>
        [TestCase(87, false)]
        [TestCase(87, true)]
        [TestCase(88, false)]
        [TestCase(88, true)]
        [TestCase(89, false)]
        [TestCase(89, true)]
        public void JalapenoThawsBossBeforeOriginalBurnDamage(int phase, bool frozen)
        {
            var game = Board();
            var boss = game.Adventure.Boss!;
            boss.Enter(phase, phase == 89 ? "anim_head_attack_1" : "anim_head_idle", 12f, 0);
            boss.PhaseCountdown = 10000;
            boss.AnimationSeconds = .2f;
            boss.Time = .2f * 12f / boss.FrameCount();
            boss.ChilledTicks = 2000;
            boss.FrozenTicks = frozen ? 400 : 0;
            boss.DriverRate = frozen ? 0f : 6f;
            game.Adventure.BossHeadVulnerable = true;
            var pepper = (Plant)Call(game, "PlaceAdventurePlant", PlantKind.Jalapeno, 4, 2)!;
            Call(game, "DetonateAdventureJalapeno", pepper);
            Assert.That(boss.FrozenTicks, Is.Zero);
            Assert.That(boss.ChilledTicks, Is.Zero);
            Assert.That(game.Adventure.BossHealth, Is.EqualTo(38200f));
            Assert.That(game.Presentation.ZombieSounds.Count(sound => sound.Foley == 46), Is.EqualTo(1),
                "PC BurnBody passes damage flags 0x12, retaining the boss's original ShieldHit request.");
            game.Update(.01f);
            Assert.That(boss.AnimationSeconds, Is.EqualTo(.21f).Within(.000001f), "The boss body must resume at its original unchilled rate.");
        }

        /// <summary>Checks BurnRow preserves the original damage admission gate instead of thawing a hidden head.</summary>
        /// <param name="phase">An original non-targetable head transition.</param>
        [TestCase(86)]
        [TestCase(90)]
        public void JalapenoDoesNotThawAnInvulnerableBossHead(int phase)
        {
            var game = Board();
            var boss = game.Adventure.Boss!;
            boss.Enter(phase, phase == 86 ? "anim_head_enter" : "anim_head_leave", 12f, 0);
            boss.ChilledTicks = 2000;
            boss.FrozenTicks = 400;
            game.Adventure.BossHeadVulnerable = false;
            var pepper = (Plant)Call(game, "PlaceAdventurePlant", PlantKind.Jalapeno, 1, 2)!;
            Call(game, "DetonateAdventureJalapeno", pepper);
            Assert.That(boss.FrozenTicks, Is.EqualTo(400));
            Assert.That(boss.ChilledTicks, Is.EqualTo(2000));
            Assert.That(game.Adventure.BossHealth, Is.EqualTo(40000f));
        }

        /// <summary>Creates an isolated native boss roof without distant waves changing the contact under test.</summary>
        /// <param name="defer">Whether the original cutscene owns boss creation.</param>
        /// <returns>The playing production simulation.</returns>
        private static GameSimulation Board(bool defer = false)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 50, DeferAdventureBossEntrance = defer }, 7529);
            if (game.Adventure.Phase == AdventurePhase.ChoosingSeeds)
            {
                game.ChooseDefaultAdventureSeeds();
            }
            return game;
        }

        /// <summary>Calls a production collaborator boundary using the existing test inspection facade.</summary>
        /// <param name="game">The production simulation.</param>
        /// <param name="method">The unambiguous native collaborator method.</param>
        /// <param name="arguments">The immediately consumed arguments.</param>
        /// <returns>The original method result.</returns>
        private static object? Call(GameSimulation game, string method, params object[] arguments)
        {
            return SimulationInspection.GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
        }
    }
}
