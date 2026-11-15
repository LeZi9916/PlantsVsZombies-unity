#nullable enable
using PlantsVsZombies.Adventure;
using PlantsVsZombies.BoardUI;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns BowlingSystem responsibilities for one deterministic match.</summary>
    internal sealed class BowlingSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BiteSystem operations.</summary>
        private BiteSystem _biteSystem = null!;
        /// <summary>Provides CombatSystem operations.</summary>
        private CombatSystem _combatSystem = null!;
        /// <summary>Provides ExplosionEffects operations.</summary>
        private ExplosionEffects _explosionEffects = null!;
        /// <summary>Provides IndependentModeRunner operations.</summary>
        private IndependentModeRunner _independentModeRunner = null!;
        /// <summary>Provides LegacySimulation operations.</summary>
        private LegacySimulation _legacySimulation = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal BowlingSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="biteSystem">The BiteSystem collaborator.</param>
        /// <param name="combatSystem">The CombatSystem collaborator.</param>
        /// <param name="explosionEffects">The ExplosionEffects collaborator.</param>
        /// <param name="independentModeRunner">The IndependentModeRunner collaborator.</param>
        /// <param name="legacySimulation">The LegacySimulation collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BiteSystem biteSystem, CombatSystem combatSystem, ExplosionEffects explosionEffects, IndependentModeRunner independentModeRunner, LegacySimulation legacySimulation, ZombieFeedbackSystem zombieFeedbackSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _biteSystem = biteSystem;
            _combatSystem = combatSystem;
            _explosionEffects = explosionEffects;
            _independentModeRunner = independentModeRunner;
            _legacySimulation = legacySimulation;
            _zombieFeedbackSystem = zombieFeedbackSystem;
        }

        /// <summary>Performs initialize adventure bowling native.</summary>
        /// <param name = "nut">The nut.</param>
        /// <param name = "giant">The giant.</param>
        internal void InitializeAdventureBowlingNative(global::PlantsVsZombies.Adventure.AdventureBowlingNut nut, bool giant)
        {
            if (nut.NativeMotionInitialized)
            {
                return;
            }

            nut._bounceDelay = 0f; // Legacy field retained for old scene probes; native collision has no invented cooldown.
            nut.NativeMotionInitialized = true;
            nut.NativePixelX = (int)(nut.X * 80f);
            nut.NativePixelY = 80 + nut.Row * 100;
            global::PlantsVsZombies.Adventure.AdventureBowlingPresentation? pose;
            if (!_world.Presentation.Bowling.TryGetValue(nut, out pose))
            {
                pose = new global::PlantsVsZombies.Adventure.AdventureBowlingPresentation
                {
                    Rate = (giant ? 6f : 12f) + (float)_world._random.NextDouble() * (giant ? 4f : 6f)
                };
                _world.Presentation.Bowling.Add(nut, pose);
            }

            nut.NativeAge = 0f;
            pose.Age = 0f; // PC samples the initial ground pose before the first reanim update.
        }

        /// <summary>Performs find adventure bowling native target.</summary>
        /// <param name = "nut">The nut.</param>
        /// <param name = "giant">The giant.</param>
        /// <returns>The resulting find adventure bowling native target.</returns>
        internal Zombie? FindAdventureBowlingNativeTarget(global::PlantsVsZombies.Adventure.AdventureBowlingNut nut, bool giant)
        {
            // PC bowling attack rect uses plant mX, mWidth-20=60 and signed horizontal overlap>=0.
            var attack = new NativeRect(nut.NativePixelX, nut.NativePixelY, 60, 80);
            foreach (var z in _world.Zombies)
            {
                var phase = _zombieFeedbackSystem.AdventureZombieFeedbackPhase(z);
                if (z.Row != nut.Row || z.Hypnotized || z.Health <= 0 || phase >= 1 && phase <= 3 || z.Airborne || z.DropRemaining > 0 || z.Kind == ZombieKind.Boss || z.Kind == ZombieKind.Bungee || z.Kind == ZombieKind.Digger && z.AbilityPhase == 0 || phase == 12 && !giant || phase == 54 && !giant)
                {
                    continue;
                }

                var x = (int)global::PlantsVsZombies.Adventure.AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z));
                var backwards = z.PresentationWalkingBackwards ?? (z.Hypnotized || z.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(z));
                var body = global::PlantsVsZombies.Adventure.AdventureBowlingNativeRules.ZombieBounds((int)z.Kind, phase, x, (int)_biteSystem.AdventureBiteNativeY(z, x), backwards, z.PresentationBiteBodyY, z.PresentationBiteClipHeight);
                if (body.X > 800)
                {
                    continue;
                }

                if (global::PlantsVsZombies.Adventure.AdventureZombieBiteRules.HorizontalOverlap(attack, body) >= 0)
                {
                    return z;
                }
            }

            return null;
        }

        /// <summary>Performs hit adventure bowling native.</summary>
        /// <param name = "nut">The nut.</param>
        /// <param name = "z">The z.</param>
        /// <param name = "giant">The giant.</param>
        internal void HitAdventureBowlingNative(global::PlantsVsZombies.Adventure.AdventureBowlingNut nut, Zombie z, bool giant)
        {
            _zombieFeedbackSystem.RequestAdventureZombieFoley(20);
            if (giant || z.Kind == ZombieKind.ScreenDoor && z.ShieldHealth > 0 && nut.NativeDirection != 0)
            {
                _combatSystem.HitAdventureZombie(z, 1800f);
            }
            else if (z.ShieldHealth > 0)
            {
                _combatSystem.HitAdventureZombie(z, Math.Min(400f, z.ShieldHealth));
            }
            else if (z.ArmorHealth > 0)
            {
                if (z.Kind == ZombieKind.Buckethead)
                {
                    _zombieFeedbackSystem.RequestAdventureZombieFoley(46);
                }
                else if (z.Kind == ZombieKind.Conehead)
                {
                    _zombieFeedbackSystem.RequestAdventureZombieFoley(58);
                }

                _combatSystem.HitAdventureZombie(z, Math.Min(900f, z.ArmorHealth));
            }
            else
            {
                _combatSystem.HitAdventureZombie(z, 1800f);
            }

            nut._lastHitId = z.Id;
        }

        /// <summary>Performs advance adventure bowling native.</summary>
        /// <param name = "nut">The nut.</param>
        /// <param name = "dt">The dt.</param>
        /// <param name = "giant">The giant.</param>
        /// <returns>The resulting advance adventure bowling native.</returns>
        internal bool AdvanceAdventureBowlingNative(global::PlantsVsZombies.Adventure.AdventureBowlingNut nut, float dt, bool giant)
        {
            InitializeAdventureBowlingNative(nut, giant);
            var pose = _world.Presentation.Bowling[nut];
            nut._nativeTickRemainder += dt * 100f;
            var ticks = (int)Math.Floor(nut._nativeTickRemainder + .00001f);
            nut._nativeTickRemainder -= ticks;
            for (var tick = 0; tick < ticks; tick++)
            {
                nut.NativePixelX = global::PlantsVsZombies.Adventure.AdventureBowlingNativeRules.HorizontalStep(nut.NativePixelX, nut.NativeAge, pose.Rate, giant);
                nut.NativePixelY = global::PlantsVsZombies.Adventure.AdventureBowlingNativeRules.VerticalStep(nut.NativePixelY, nut.NativeDirection);
                nut.NativeAge += .01f;
                nut.X = nut.NativePixelX / 80f;
                var removed = nut.NativePixelX > 800;
                if (!global::PlantsVsZombies.Adventure.AdventureBowlingNativeRules.AtRow(nut.NativePixelY, nut.Row))
                {
                    if (removed)
                    {
                        pose.Age = nut.NativeAge;
                        return true;
                    }

                    continue;
                }

                var hit = FindAdventureBowlingNativeTarget(nut, giant);
                if (hit != null)
                {
                    if (nut.Explosive)
                    {
                        _explosionEffects.AdventurePresentationExplodeONut(nut.X, nut.Row);
                        _combatSystem.BlastAdventureRadius(nut.NativePixelX + 40, nut.NativePixelY + 40, nut.Row, 90, 1, 0x21);
                        _legacySimulation.Raise(GameEvent.Explosion);
                        return true;
                    }

                    HitAdventureBowlingNative(nut, hit, giant);
                }

                var choose = hit != null && !giant && nut.Row > 0 && nut.Row < 4 && nut.NativeDirection == 0;
                var direction = global::PlantsVsZombies.Adventure.AdventureBowlingNativeRules.NextDirection(nut.Row, nut.NativeDirection, hit != null, giant, choose ? _world._random.Next(2) : 0);
                nut.NativeDirection = direction;
                // Row is the next collision/render-order row; the visible Y remains continuous.
                if (direction == 28)
                {
                    nut.Row--;
                }
                else if (direction == 29)
                {
                    nut.Row++;
                }

                if (removed)
                {
                    pose.Age = nut.NativeAge;
                    return true;
                }
            }

            pose.Age = nut.NativeAge;
            return false;
        }

        /// <summary>Performs tick mode bowling.</summary>
        /// <param name = "dt">The dt.</param>
        internal void TickModeBowling(float dt)
        {
            var s = _independentModeRunner.RequiredModeSession;
            foreach (var nut in new PooledSnapshot<AdventureBowlingNut>(_adventureModeRunner.Adventure._nuts))
            {
                var giant = s._giantNuts.Contains(nut);
                if (AdvanceAdventureBowlingNative(nut, dt, giant))
                {
                    _adventureModeRunner.Adventure._nuts.Remove(nut);
                    s._giantNuts.Remove(nut);
                }
            }
        }
    }
}
