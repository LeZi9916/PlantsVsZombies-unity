#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns BossSystem responsibilities for one deterministic match.</summary>
    internal sealed class BossSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides LegacySimulation operations.</summary>
        private LegacySimulation _legacySimulation = null!;
        /// <summary>Provides ZombieSpawnSystem operations.</summary>
        private ZombieSpawnSystem _zombieSpawnSystem = null!;
        /// <summary>Applies the original plant squish semantics at boss contacts.</summary>
        private BiteSystem _biteSystem = null!;
        /// <summary>Queues the original boss action Foley.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal BossSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="legacySimulation">The LegacySimulation collaborator.</param>
        /// <param name="zombieSpawnSystem">The ZombieSpawnSystem collaborator.</param>
        /// <param name="biteSystem">The original plant squish owner.</param>
        /// <param name="zombieFeedbackSystem">The original Foley event owner.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, LegacySimulation legacySimulation, ZombieSpawnSystem zombieSpawnSystem, BiteSystem biteSystem, ZombieFeedbackSystem zombieFeedbackSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _legacySimulation = legacySimulation;
            _zombieSpawnSystem = zombieSpawnSystem;
            _biteSystem = biteSystem;
            _zombieFeedbackSystem = zombieFeedbackSystem;
        }

        /// <summary>Performs advance adventure boss.</summary>
        /// <param name = "dt">The dt.</param>
        internal void AdvanceAdventureBoss(float dt)
        {
            var a = _adventureModeRunner.Adventure;
            if (a.BossHealth <= 0)
            {
                a.BossHeadVulnerable = false;
                return;
            }

            AdvanceNativeBossBalls();
            var boss = a.Boss ??= new AdventureBossState();
            boss.Age++;
            boss.AdvanceAnimation();
            var frozen = boss.FrozenTicks > 0;
            if (!frozen)
            {
                if (boss.PhaseCountdown > 0)
                {
                    boss.PhaseCountdown--;
                }

                if (boss.SummonCountdown > 0)
                {
                    boss.SummonCountdown--;
                }

                if (boss.BungeeCountdown > 0)
                {
                    boss.BungeeCountdown--;
                }

                if (boss.StompCountdown > 0)
                {
                    boss.StompCountdown--;
                }

                if (boss.HeadCountdown > 0)
                {
                    boss.HeadCountdown--;
                }
            }

            AdvanceNativeBossAction(boss);
            a.BossHeadVulnerable = boss.HeadVulnerable;
            boss.FrozenTicks = Math.Max(0, boss.FrozenTicks - 1);
            boss.ChilledTicks = Math.Max(0, boss.ChilledTicks - 1);

        }

        /// <summary>Initializes the original boss allocation prefix before entering its native action scheduler.</summary>
        internal void InitializeAdventureBoss()
        {
            // PC AddZombieInRow -> 53DD90 -> 53FF50/54B050, including discarded ordinary walking values.
            _world._random.Next(5);
            _world._random.Next(40);
            _world._random.Next(101);
            AdventureSpawnRandom.NextRange(_world._random, .23f, .37f);
            AdventureSpawnRandom.NextRange(_world._random, .23f, .37f);
            _world._random.Next(2);
            _adventureModeRunner.Adventure.Boss = new AdventureBossState { Animation = "anim_enter" };
        }

        /// <summary>Runs PC 551AA0 before boss actions, including the native one-in-ten rolling trail gate.</summary>
        private void AdvanceNativeBossBalls()
        {
            var balls = _adventureModeRunner.Adventure._balls;
            for (var index = balls.Count - 1; index >= 0; index--)
            {
                var ball = balls[index];
                ball.X -= ball.Velocity / 80f;
                if (ball.NativeX < -180f)
                {
                    balls.RemoveAt(index);
                    continue;
                }

                var column = (int)Math.Floor((ball.NativeX + 75f - 40f) / 80f);
                foreach (var plant in new PooledSnapshot<Plant>(_world.Plants))
                {
                    if (plant.Row == ball.Row && plant.Column == column)
                    {
                        _biteSystem.SquishAdventurePlant(plant);
                    }
                }

                foreach (var mower in _world.Mowers)
                {
                    var x = 40f + mower.X * 80f;
                    if (mower.Row == ball.Row && x > ball.NativeX && x < ball.NativeX + 50f)
                    {
                        mower.Available = mower.Active = false;
                    }
                }

                if (!ball.Rolling && ball.AnimationSeconds >= 1f)
                {
                    ball.Rolling = true;
                    ball.AnimationSeconds = 0f;
                }

                if (ball.Rolling && _world._random.Next(10) == 0)
                {
                    var x = ball.NativeX + 100f + AdventureSpawnRandom.NextRange(_world._random, 0f, 20f);
                    var floor = 80f + ball.Row * 85f + Math.Max(0f, 440f - (x - 40f)) * .25f - 10f;
                    var y = floor + 90f + AdventureSpawnRandom.NextRange(_world._random, -50f, 0f);
                    var id = ++_world.Presentation._effectId;
                    _world.Presentation.ZomboniParticles.Add(new AdventureZomboniParticle { Id = id, Seed = id,
                        Row = ball.Row, X = x, Y = y, Particle = ball.IsIce ? "IceballTrail" : "FireballTrail" });
                }

                ball.AnimationSeconds += .01f;
            }
        }

        /// <summary>Runs PC 551ED0's action priority and original animation contacts.</summary>
        /// <param name="boss">The board's native boss state.</param>
        private void AdvanceNativeBossAction(AdventureBossState boss)
        {
            if (boss.Phase == 78)
            {
                EnterBossIdle(boss);
                return;
            }

            if (boss.Phase == 79)
            {
                if (boss.PhaseCountdown > 0)
                {
                    return;
                }

                var health = _adventureModeRunner.Adventure.BossHealth;
                var maximum = (int)_adventureModeRunner.Adventure.BossMaxHealth;
                var mode = health < maximum / 2 ? 2 : health < maximum * 4 / 5 ? 1 : 0;
                if (mode != boss.Mode)
                {
                    boss.Mode = mode;
                    if (mode == 1)
                    {
                        StartBossBungees(boss);
                    }
                    else
                    {
                        StartBossRv(boss);
                    }
                }
                else if (boss.StompCountdown == 0)
                {
                    StartBossStomp(boss);
                }
                else if (boss.BungeeCountdown == 0)
                {
                    if (_world._random.Next(_world.ModeSession == null ? 4 : 2) == 0)
                    {
                        boss.BungeeCountdown = 4000 + _world._random.Next(1001);
                        StartBossRv(boss);
                    }
                    else
                    {
                        StartBossBungees(boss);
                    }
                }
                else if (boss.HeadCountdown <= 0)
                {
                    boss.HeadCountdown = 4000 + _world._random.Next(1001);
                    boss.Enter(86, "anim_head_enter", 12f);
                    BossFoley(89);
                }
                else if (boss.SummonCountdown <= 0)
                {
                    boss.ChilledTicks = boss.FrozenTicks = 0;
                    boss.SummonCountdown = (boss.Mode == 0 ? 450 : boss.Mode == 1 ? 350 : 150) + _world._random.Next(101);
                    boss.TargetRow = _zombieSpawnSystem.ChooseAdventureZombieRow(ZombieKind.Normal);
                    boss.Enter(80, s_spawnAnimations[boss.TargetRow], 12f);
                    BossFoley(89);
                }
                else
                {
                    boss.PhaseCountdown = 100 + _world._random.Next(101);
                }

                return;
            }

            if (boss.Phase == 80 && boss.Triggered(.6f))
            {
                SpawnBossContact(boss);
            }
            else if (boss.Phase == 81 && boss.Triggered(boss.TargetRow < 2 ? .5f : .55f))
            {
                CrushBossRectangle(boss.TargetRow, 5, 4);
                _world.Presentation.ScreenShake.Start(1, 4);
                BossFoley(40);
            }
            else if (boss.Phase == 82 && boss.Triggered(.4f))
            {
                for (var index = 0; index < 3; index++)
                {
                    var zombie = _zombieSpawnSystem.SpawnAdventureBossBungee(boss.TargetColumn + index);
                    boss.BungeeIds[index] = zombie.Id;
                }

                boss.Phase = 83;
                return;
            }
            else if (boss.Phase == 83)
            {
                var done = BossBungeesFinished(boss);
                if (done)
                {
                    boss.Enter(84, "anim_bungee_1_leave", 18f);
                }

                return;
            }
            else if (boss.Phase == 85 && boss.Triggered(.65f))
            {
                CrushBossRectangle(boss.TargetRow, boss.TargetColumn, 3);
                _world.Presentation.ScreenShake.Start(1, 2);
                boss.SummonCountdown = 500;
                boss.HeadCountdown = 5000;
                if (boss.Mode >= 1)
                {
                    boss.StompCountdown = 4000;
                }

                if (boss.Mode >= 2)
                {
                    boss.BungeeCountdown = 6500;
                }
            }
            else if (boss.Phase == 86)
            {
                if (boss.Triggered(.55f))
                {
                    BossFoley(90);
                }

                if (boss.Finished)
                {
                    boss.Enter(87, "anim_head_idle", 12f);
                    boss.PhaseCountdown = 500;
                }

                return;
            }
            else if (boss.Phase == 87)
            {
                if (boss.PhaseCountdown == 0)
                {
                    boss.BallRow = _world._random.Next(5);
                    boss.BallIsIce = _world._random.Next(2) != 0;
                    boss.Enter(89, s_headAnimations[boss.BallRow], 12f);
                    SetBossDriver(boss, "anim_drive", 36f);
                }

                return;
            }
            else if (boss.Phase == 89)
            {
                if (boss.Triggered(.37f))
                {
                    BossFoley(93);
                }

                if (boss.Triggered(.42f))
                {
                    _adventureModeRunner.Adventure._balls.Add(new AdventureBossBall { Row = boss.BallRow, X = 455f / 80f, IsIce = boss.BallIsIce });
                    _adventureModeRunner.Adventure.Notice(AdventureNoticeKind.BossAttack, boss.BallIsIce ? 1 : 0);
                    SetBossDriver(boss, "anim_laugh", 18f);
                    BossFoley(89);
                }

                if (boss.Finished)
                {
                    boss.Enter(88, "anim_head_idle", 12f);
                    SetBossDriver(boss, "anim_idle", 18f);
                    boss.PhaseCountdown = 300;
                }

                return;
            }
            else if (boss.Phase == 88)
            {
                if (boss.PhaseCountdown == 0)
                {
                    boss.Enter(90, "anim_head_leave", 12f);
                }

                return;
            }

            if (boss.Phase == 90)
            {
                if (boss.Triggered(.23f))
                {
                    boss.ChilledTicks = 0;
                }

                if (boss.Triggered(.48f) || boss.Triggered(.8f))
                {
                    BossFoley(40);
                }
            }

            if (boss.Finished)
            {
                EnterBossIdle(boss);
            }
        }

        /// <summary>Starts an original independent driver clip without changing the boss body's action clock.</summary>
        /// <param name="boss">The native boss retaining its driver.</param>
        /// <param name="animation">The original driver marker.</param>
        /// <param name="rate">The original driver frame rate.</param>
        private static void SetBossDriver(AdventureBossState boss, string animation, float rate)
        {
            boss.DriverAnimation = animation;
            boss.DriverRate = rate;
            boss.DriverFrames = 0f;
        }

        /// <summary>Checks PC 551160's early rising-follower or all-followers-gone completion gate.</summary>
        /// <param name="boss">The boss retaining its original three followers.</param>
        /// <returns>Whether the original bungee leave action can start.</returns>
        private bool BossBungeesFinished(AdventureBossState boss)
        {
            var alive = 0;
            foreach (var id in boss.BungeeIds)
            {
                foreach (var zombie in _world.Zombies)
                {
                    if (zombie.Id == id)
                    {
                        if (zombie.BungeeState?.Phase == 8)
                        {
                            return true;
                        }

                        alive++;
                    }
                }
            }

            return alive == 0;
        }

        /// <summary>Stores PC's five original summon animation markers.</summary>
        private static readonly string[] s_spawnAnimations = { "anim_spawn_1", "anim_spawn_2", "anim_spawn_3", "anim_spawn_4", "anim_spawn_5" };
        /// <summary>Stores PC's four original stomp animation markers.</summary>
        private static readonly string[] s_stompAnimations = { "anim_stomp_1", "anim_stomp_2", "anim_stomp_3", "anim_stomp_4" };
        /// <summary>Stores PC's five original spit animation markers.</summary>
        private static readonly string[] s_headAnimations = { "anim_head_attack_1", "anim_head_attack_2", "anim_head_attack_3", "anim_head_attack_4", "anim_head_attack_5" };
        /// <summary>Stores the verified twelve-entry PC summon table at 747F28.</summary>
        private static readonly ZombieKind[] s_summonKinds = { ZombieKind.Conehead, ZombieKind.Buckethead, ZombieKind.Football,
            ZombieKind.PoleVaulter, ZombieKind.JackInTheBox, ZombieKind.Ladder, ZombieKind.Zomboni, ZombieKind.Catapult,
            ZombieKind.Pogo, ZombieKind.Newspaper, ZombieKind.ScreenDoor, ZombieKind.Gargantuar };

        /// <summary>Starts the original random one-hundred-to-two-hundred-update idle gap.</summary>
        /// <param name="boss">The boss returning from an original action.</param>
        private void EnterBossIdle(AdventureBossState boss)
        {
            boss.Enter(79, "anim_idle", 6f);
            boss.PhaseCountdown = 100 + _world._random.Next(101);
        }

        /// <summary>Starts PC 550900's RV target selection, preserving row-before-column draw order.</summary>
        /// <param name="boss">The native boss selecting its RV target.</param>
        private void StartBossRv(AdventureBossState boss)
        {
            boss.ChilledTicks = boss.FrozenTicks = 0;
            boss.TargetRow = _world._random.Next(4);
            boss.TargetColumn = _world._random.Next(3);
            boss.Enter(85, "anim_RV_1", 16f);
            BossFoley(89);
        }

        /// <summary>Starts PC 550EF0's three-column bungee action.</summary>
        /// <param name="boss">The native boss selecting its bungee columns.</param>
        private void StartBossBungees(AdventureBossState boss)
        {
            boss.ChilledTicks = boss.FrozenTicks = 0;
            boss.BungeeCountdown = 4000 + _world._random.Next(1001);
            boss.TargetColumn = _world._random.Next(3);
            boss.Enter(82, "anim_bungee_1_enter", 12f);
            BossFoley(89);
            BossFoley(44);
        }

        /// <summary>Preserves PC 550C60's eligible two-row stomp selection and its unconditional cooldown draw.</summary>
        /// <param name="boss">The native boss selecting its stomp target.</param>
        private void StartBossStomp(AdventureBossState boss)
        {
            boss.ChilledTicks = boss.FrozenTicks = 0;
            boss.StompCountdown = 5500 + _world._random.Next(1001);
            boss.Phase = 81;
            var eligible = 0;
            var mask = 0;
            for (var row = 0; row < 4; row++)
            {
                foreach (var plant in _world.Plants)
                {
                    if (plant.Health > 0f && !plant.PresentationBiteNotOnGround && plant.BungeeState != 2
                        && plant.Row >= row && plant.Row <= row + 1 && plant.Column >= 5)
                    {
                        mask |= 1 << row;
                        eligible++;
                        break;
                    }
                }
            }

            if (eligible == 0)
            {
                return;
            }

            var ticket = _world._random.Next(eligible);
            for (var row = 0; row < 4; row++)
            {
                if ((mask & (1 << row)) != 0 && ticket-- == 0)
                {
                    boss.TargetRow = row;
                    boss.Enter(81, s_stompAnimations[row], 12f);
                    BossFoley(89);
                    return;
                }
            }
        }

        /// <summary>Runs PC 550BF0's age gates and original table selection at the summon contact.</summary>
        /// <param name="boss">The native boss reaching its authored contact marker.</param>
        private void SpawnBossContact(AdventureBossState boss)
        {
            var kind = boss.Age < 3500 ? ZombieKind.Normal : boss.Age < 8000 ? ZombieKind.Conehead
                : boss.Age < 12500 ? ZombieKind.Buckethead : s_summonKinds[_world._random.Next(boss.TargetRow == 0 ? 11 : 12)];
            var zombie = _zombieSpawnSystem.SpawnAdventureBoardZombie(kind, boss.TargetRow, AdventureZombieNativeRules.GridX(600f), 0);
            zombie.X = AdventureZombieNativeRules.GridX(600f, AdventureDancerRules.Scale(zombie));
            zombie.PresentationPreviousX = zombie.X;
        }

        /// <summary>Squishes PC's two-row rectangle without replacing whole-stack plant damage semantics.</summary>
        /// <param name="row">The first of the two affected rows.</param>
        /// <param name="column">The first affected column.</param>
        /// <param name="width">The number of affected columns.</param>
        private void CrushBossRectangle(int row, int column, int width)
        {
            foreach (var plant in new PooledSnapshot<Plant>(_world.Plants))
            {
                if (plant.Row >= row && plant.Row <= row + 1 && plant.Column >= column && plant.Column < column + width)
                {
                    _biteSystem.SquishAdventurePlant(plant);
                }
            }
        }

        /// <summary>Queues an original boss action Foley at its native contact boundary.</summary>
        /// <param name="foley">The verified original Foley identifier.</param>
        private void BossFoley(int foley)
        {
            _zombieFeedbackSystem.RequestAdventureZombieFoley(foley);
        }

        /// <summary>Performs damage adventure boss.</summary>
        /// <param name = "damage">The damage.</param>
        internal void DamageAdventureBoss(float damage)
        {
            if (_adventureModeRunner.Adventure.Level.Mode != AdventureMode.Boss || _adventureModeRunner.Adventure.BossHealth <= 0 || !_adventureModeRunner.Adventure.BossHeadVulnerable)
            {
                return;
            }

            _adventureModeRunner.Adventure.BossHealth = Math.Max(0, _adventureModeRunner.Adventure.BossHealth - damage);
            _legacySimulation.Raise(GameEvent.ZombieHit);
            if (_adventureModeRunner.Adventure.BossHealth <= 0)
            {
                _adventureModeRunner.Adventure._balls.Clear();
                _adventureModeRunner.Adventure.Notice(AdventureNoticeKind.BossAttack, 4);
            }
        }
    }
}
