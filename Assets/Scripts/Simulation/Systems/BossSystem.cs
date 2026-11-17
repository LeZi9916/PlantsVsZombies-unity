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
        /// <summary>Applies the original immediate follower deaths and loot when BossDie runs.</summary>
        private ZombieLifecycle _zombieLifecycle = null!;
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
        /// <param name="zombieLifecycle">The original instant-death and loot owner.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, LegacySimulation legacySimulation, ZombieSpawnSystem zombieSpawnSystem, BiteSystem biteSystem, ZombieFeedbackSystem zombieFeedbackSystem, ZombieLifecycle zombieLifecycle)
        {
            _adventureModeRunner = adventureModeRunner;
            _legacySimulation = legacySimulation;
            _zombieSpawnSystem = zombieSpawnSystem;
            _biteSystem = biteSystem;
            _zombieFeedbackSystem = zombieFeedbackSystem;
            _zombieLifecycle = zombieLifecycle;
        }

        /// <summary>Performs advance adventure boss.</summary>
        /// <param name = "dt">The dt.</param>
        internal void AdvanceAdventureBoss(float dt)
        {
            var a = _adventureModeRunner.Adventure;
            if (a.Boss == null)
            {
                InitializeAdventureBoss();
            }

            var boss = a.Boss!;
            boss.AdvanceEffects();
            if (boss.DeathStarted)
            {
                boss.DeathTicks++;
            }

            if (boss.Phase == 1)
            {
                AdvanceBossDeath(boss);
                a.BossHeadVulnerable = false;
                return;
            }

            AdvanceNativeBossBalls();
            boss.Age++;
            boss.ColorTick = _world._nativeFeedbackTick;
            boss.FlashTicks = Math.Max(0, boss.FlashTicks - 1);
            if (boss.SmokeTicks >= 0)
            {
                boss.SmokeTicks++;
            }
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
            var boss = new AdventureBossState();
            boss.Enter(78, "anim_enter", 12f, 0);
            _adventureModeRunner.Adventure.Boss = boss;
        }

        /// <summary>Advances PC 551ED0's LevelIntro-only entrance contacts without running the combat scheduler.</summary>
        internal void AdvanceAdventureBossIntro()
        {
            var boss = _adventureModeRunner.Adventure.Boss;
            if (boss == null || boss.Phase != 78)
            {
                return;
            }
            var shake = _world.Presentation.ScreenShake;
            shake.Tick(shake.TicksRemaining > 1 && _world._random.Next(2) == 0);
            boss.Age++;
            boss.AdvanceAnimation();
            if (boss.Triggered(.24f) || boss.Triggered(.79f))
            {
                BossFoley(40);
                shake.Start(1, 4);
            }
        }

        /// <summary>Creates one original boss particle at its native animation event.</summary>
        /// <param name="boss">The owner retaining the effect.</param>
        /// <param name="particle">The unchanged original particle definition.</param>
        /// <param name="x">The native board X.</param>
        /// <param name="y">The native board Y.</param>
        /// <returns>The source effect record, optionally completed with an original image or jaw snapshot.</returns>
        private AdventureBossEffect AddBossEffect(AdventureBossState boss, string particle, float x, float y)
        {
            var effect = new AdventureBossEffect { Id = ++_world.Presentation._effectId, Particle = particle, X = x, Y = y };
            boss.Effects.Add(effect);
            return effect;
        }

        /// <summary>Runs PC 552920's two/three original smoke attachments and fifty-update detach cross-fade.</summary>
        /// <param name="boss">The boss retaining its original head particles.</param>
        /// <param name="enable">Whether to attach new smoke after detaching any existing systems.</param>
        private void SetBossSmoke(AdventureBossState boss, bool enable)
        {
            foreach (var effect in boss.Effects)
            {
                if (effect.Particle == "ZamboniSmoke" && effect.LiveAttachment)
                {
                    effect.LiveAttachment = false;
                    effect.StopTick = effect.Ticks;
                    effect.JawAnimation = boss.Animation;
                    effect.JawSeconds = boss.AnimationSeconds;
                    effect.JawRate = boss.AnimationRate;
                }
            }
            boss.SmokeTicks = enable ? 0 : -1;
            if (!enable)
            {
                return;
            }
            for (var i = 0; i < (boss.Critical ? 3 : 2); i++)
            {
                var effect = AddBossEffect(boss, "ZamboniSmoke", i == 0 ? 60f : i == 1 ? 100f : 80f,
                    i == 0 ? 30f : i == 1 ? 58f : 27f);
                effect.Track = "Boss_head";
                effect.LiveAttachment = true;
            }
        }

        /// <summary>Runs PC 5524F0's immediate enemy cleanup, counter reset and two-second music fade.</summary>
        /// <param name="boss">The dying original boss.</param>
        private void BossDie(AdventureBossState boss)
        {
            if (!boss.DeathStarted)
            {
                boss.DeathStarted = true;
                boss.DeathTicks = 0;
            }
            // PC 552543..55255A kills and clears the ball ID before the counter helpers run.
            // Their subsequent lookup is empty: lethal cleanup does not create counter-death effects.
            _adventureModeRunner.Adventure._balls.Clear();
            foreach (var zombie in new PooledSnapshot<Zombie>(_world.Zombies))
            {
                _zombieLifecycle.KillAdventureZombieInstantWithLoot(zombie);
            }
            boss.ChilledTicks = boss.FrozenTicks = 0;
        }

        /// <summary>Runs PC 551E00's lethal exposed-head retreat before the main death clip.</summary>
        /// <param name="boss">The boss whose health has been clamped to one.</param>
        private void StartBossHeadDeath(AdventureBossState boss)
        {
            boss.Enter(90, "anim_head_leave", 24f, 0);
            AddBossEffect(boss, "BossExplosion", 700f, 150f);
            BossFoley(-10);
            BossFoley(91);
            BossDie(boss);
        }

        /// <summary>Starts PC PlayDeathAnim's original eighteen-FPS body and driver death clips.</summary>
        /// <param name="boss">The boss returning to idle after lethal damage.</param>
        private void StartBossMainDeath(AdventureBossState boss)
        {
            BossDie(boss);
            boss.Enter(1, "anim_death", 18f);
            boss.SetDriver("anim_death", 18f);
            _world._random.Next(100);
        }

        /// <summary>Runs PC UpdateDeath 54F4A0's explosion contacts, falling thump and white flag sequence.</summary>
        /// <param name="boss">The boss playing its original main death.</param>
        private void AdvanceBossDeath(AdventureBossState boss)
        {
            boss.AdvanceAnimation();
            foreach (var marker in s_deathExplosions)
            {
                if (boss.Triggered(marker))
                {
                    AddBossEffect(boss, "BossExplosion", AdventureSpawnRandom.NextRange(_world._random, 600f, 750f),
                        AdventureSpawnRandom.NextRange(_world._random, 50f, 300f));
                    BossFoley(45);
                    break;
                }
            }
            if (boss.Triggered(.93f))
            {
                _world.Presentation.ScreenShake.Start(1, 2);
                BossFoley(45);
                BossFoley(40);
            }
            if (boss.Triggered(.99f))
            {
                boss.SetDriver("anim_flag", 30f);
            }
            if (boss.Finished)
            {
                boss.DeathComplete = true;
            }
        }

        /// <summary>Stores PC 54F4A0's original timed boss explosions.</summary>
        private static readonly float[] s_deathExplosions = { .1f, .12f, .15f, .19f, .2f, .26f, .3f, .4f, .42f, .5f, .58f, .61f, .71f };

        /// <summary>Runs PC 551460/551510's original Ice-shroom or Jalapeno ball destruction and removes old trails.</summary>
        /// <param name="ice">Whether to destroy ice instead of fire.</param>
        /// <param name="row">The required native row, or minus one for every active ball.</param>
        internal void DestroyBossBalls(bool ice, int row = -1)
        {
            var board = _adventureModeRunner.Adventure;
            var boss = board.Boss;
            if (boss == null)
            {
                return;
            }
            for (var i = board._balls.Count - 1; i >= 0; i--)
            {
                var ball = board._balls[i];
                if (ball.IsIce != ice || row >= 0 && ball.Row != row)
                {
                    continue;
                }
                var y = 80f + ball.Row * 85f + Math.Max(0f, 440f - ball.NativeX - 75f) * .25f - 10f - 90f;
                if (ice)
                {
                    AddBossEffect(boss, "IceballDeath", ball.NativeX + 80f, y + 80f);
                }
                else
                {
                    for (var n = 0; n < 6; n++)
                    {
                        var angle = Math.PI * .5d + Math.PI * 2d * n / 6d;
                        boss.Effects.Add(new AdventureBossEffect { Id = ++_world.Presentation._effectId,
                            X = ball.NativeX + 80f + 60f * (float)Math.Sin(angle), Y = y + 40f + 60f * (float)Math.Cos(angle),
                            Rate = AdventureSpawnRandom.NextRange(_world._random, 20f, 25f) });
                    }
                }
                board._balls.RemoveAt(i);
                _world.Presentation.ZomboniParticles.RemoveAll(effect => effect.Particle == (ice ? "IceballTrail" : "FireballTrail"));
            }
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
                _biteSystem.CrushAdventurePlantCell(ball.Row, column, true);

                foreach (var mower in _world.Mowers)
                {
                    var x = 40f + mower.X * 80f - 21f;
                    if ((mower.Available || mower.Active) && mower.Row == ball.Row && x > ball.NativeX && x < ball.NativeX + 50f)
                    {
                        mower.SquishedWhileActive = mower.Active;
                        mower.SquishedTicks = 500;
                        mower.Available = mower.Active = false;
                        BossFoley(21);
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
                    var effect = _world.LifetimePool.Rent<AdventureZomboniParticle>();
                    effect.Id = id;
                    effect.Seed = id;
                    effect.Row = ball.Row;
                    effect.X = x;
                    effect.Y = y;
                    effect.Particle = ball.IsIce ? "IceballTrail" : "FireballTrail";
                    _world.Presentation.ZomboniParticles.Add(effect);
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
                if (_adventureModeRunner.Adventure.BossHealth == 1f)
                {
                    StartBossMainDeath(boss);
                    return;
                }

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
                    foreach (var zombie in new PooledSnapshot<Zombie>(_world.Zombies))
                    {
                        if (Array.IndexOf(boss.BungeeIds, zombie.Id) >= 0 && zombie.PresentationButter > 0f)
                        {
                            _zombieLifecycle.KillAdventureZombieInstantWithLoot(zombie);
                        }
                    }
                    boss.Enter(84, "anim_bungee_1_leave", 18f);
                }

                return;
            }
            else if (boss.Phase == 85 && boss.Triggered(.65f))
            {
                CrushBossRectangle(boss.TargetRow, boss.TargetColumn, 3);
                _world.Presentation.ScreenShake.Start(1, 2);
                BossFoley(-9);
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
                if (boss.DamageIndex == 2 && boss.Triggered(.37f))
                {
                    SetBossSmoke(boss, true);
                }
                if (boss.Triggered(.55f))
                {
                    BossFoley(90);
                }

                if (boss.Finished)
                {
                    boss.Enter(87, "anim_head_idle", 12f, 0);
                    boss.PhaseCountdown = 500;
                }

                return;
            }
            else if (boss.Phase == 87)
            {
                if (_adventureModeRunner.Adventure.BossHealth == 1f)
                {
                    StartBossHeadDeath(boss);
                    return;
                }
                if (boss.PhaseCountdown == 0)
                {
                    _adventureModeRunner.Adventure._balls.Clear();
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
                    var effect = AddBossEffect(boss, "Zombie_boss_fireball", 0f, 0f);
                    effect.Image = boss.BallIsIce ? "IMAGE_ZOMBIE_BOSS_ICEBALL_PARTICLES" : null;
                    effect.JawAnimation = boss.Animation;
                    effect.JawSeconds = boss.AnimationSeconds;
                    effect.JawRate = boss.AnimationRate;
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
                    boss.Enter(88, "anim_head_idle", 12f, 0);
                    SetBossDriver(boss, "anim_idle", 18f);
                    boss.PhaseCountdown = 300;
                }

                return;
            }
            else if (boss.Phase == 88)
            {
                if (_adventureModeRunner.Adventure.BossHealth == 1f)
                {
                    StartBossHeadDeath(boss);
                    return;
                }
                if (boss.PhaseCountdown == 0)
                {
                    boss.Enter(90, "anim_head_leave", 12f, 0);
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
                if (boss.Phase == 90)
                {
                    SetBossSmoke(boss, false);
                }
                EnterBossIdle(boss);
            }
        }

        /// <summary>Starts an original independent driver clip without changing the boss body's action clock.</summary>
        /// <param name="boss">The native boss retaining its driver.</param>
        /// <param name="animation">The original driver marker.</param>
        /// <param name="rate">The original driver frame rate.</param>
        private static void SetBossDriver(AdventureBossState boss, string animation, float rate)
        {
            boss.SetDriver(animation, rate);
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
            boss.Enter(79, "anim_idle", 6f, 0);
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

        /// <summary>Runs PC BurnRow 474920's cold removal before the boss-specific BurnBody 54E920 damage.</summary>
        internal void BurnAdventureBoss()
        {
            var board = _adventureModeRunner.Adventure;
            var boss = board.Boss;
            if (board.Level.Mode != AdventureMode.Boss || !board.BossHeadVulnerable || board.BossHealth <= 0f
                || boss == null || boss.DeathStarted)
            {
                return;
            }

            boss.FrozenTicks = 0;
            boss.ChilledTicks = 0;
            // BurnBody uses damage flags 0x12; 0x7F is BurnRow's target-range mask.
            DamageAdventureBossWithFlags(1800f, 0x12U);
        }

        /// <summary>Performs damage adventure boss.</summary>
        /// <param name = "damage">The damage.</param>
        internal void DamageAdventureBoss(float damage)
        {
            DamageAdventureBossWithFlags(damage, 0U);
        }

        /// <summary>Applies PC HitIceTrap to the original first-allocated boss before ordinary summoned zombies.</summary>
        internal void ApplyAdventureBossIceTrap()
        {
            var boss = _adventureModeRunner.Adventure.Boss;
            if (boss == null || !AdventureIceRules.CanChill(25, boss.Phase, false, false, false))
            {
                return;
            }
            var cold = boss.ChilledTicks > 0 || boss.FrozenTicks > 0;
            if (boss.ChilledTicks <= 0)
            {
                BossFoley(18);
                _world.Presentation.ChillOnsetSoundSerial++;
            }
            boss.ChilledTicks = Math.Max(boss.ChilledTicks, 2000);
            boss.FrozenTicks = (cold ? 300 : 400) + _world._random.Next(cold ? 101 : 201);
            DamageAdventureBoss(20f);
        }

        /// <summary>Applies the original projectile chill onset to the separate boss state.</summary>
        /// <param name="shot">The original winter-melon projectile reaching the exposed head.</param>
        internal void ApplyAdventureBossProjectileCold(Pea shot)
        {
            var boss = _adventureModeRunner.Adventure.Boss;
            if (shot.IsSnow && boss?.HeadVulnerable == true)
            {
                if (boss.ChilledTicks <= 0)
                {
                    BossFoley(18);
                }
                boss.ChilledTicks = Math.Max(1000, boss.ChilledTicks);
            }
        }

        /// <summary>Applies PC TakeDamageBody's boss-specific sound flags, damage images and health-one lethal clamp.</summary>
        /// <param name="damage">The original body damage.</param>
        /// <param name="flags">The original damage flags, including bit eight's hit-sound suppression.</param>
        internal void DamageAdventureBossWithFlags(float damage, uint flags)
        {
            if (_adventureModeRunner.Adventure.Level.Mode != AdventureMode.Boss || _adventureModeRunner.Adventure.BossHealth <= 0 || !_adventureModeRunner.Adventure.BossHeadVulnerable)
            {
                return;
            }

            var board = _adventureModeRunner.Adventure;
            var boss = board.Boss;
            if (boss == null || boss.DeathStarted)
            {
                return;
            }

            var old = board.BossHealth;
            board.BossHealth = Math.Max(1f, old - damage);
            boss.FlashTicks = 25;
            if ((flags & 8U) == 0)
            {
                BossFoley(46);
            }
            var maximum = (int)board.BossMaxHealth;
            var index = board.BossHealth < maximum / 2 ? 2 : board.BossHealth < maximum * 4 / 5 ? 1 : 0;
            if (index != boss.DamageIndex)
            {
                boss.DamageIndex = index;
                if (index == 1)
                {
                    boss.DamagedInnerFoot = true;
                }
                if (index == 2)
                {
                    SetBossSmoke(boss, true);
                }
            }
            if (old >= maximum / 10 && board.BossHealth < maximum / 10)
            {
                boss.Critical = true;
                AddBossEffect(boss, "BossExplosion", 770f, 260f);
                BossFoley(45);
                SetBossSmoke(boss, true);
            }
            _legacySimulation.Raise(GameEvent.ZombieHit);
        }
    }
}
