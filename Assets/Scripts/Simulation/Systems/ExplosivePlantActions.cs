#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns ExplosivePlantActions responsibilities for one deterministic match.</summary>
    internal sealed class ExplosivePlantActions
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BossSystem operations.</summary>
        private BossSystem _bossSystem = null!;
        /// <summary>Provides CombatSystem operations.</summary>
        private CombatSystem _combatSystem = null!;
        /// <summary>Provides ExplosionEffects operations.</summary>
        private ExplosionEffects _explosionEffects = null!;
        /// <summary>Provides LegacySimulation operations.</summary>
        private LegacySimulation _legacySimulation = null!;
        /// <summary>Provides PlantingSystem operations.</summary>
        private PlantingSystem _plantingSystem = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal ExplosivePlantActions(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="bossSystem">The BossSystem collaborator.</param>
        /// <param name="combatSystem">The CombatSystem collaborator.</param>
        /// <param name="explosionEffects">The ExplosionEffects collaborator.</param>
        /// <param name="legacySimulation">The LegacySimulation collaborator.</param>
        /// <param name="plantingSystem">The PlantingSystem collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BossSystem bossSystem, CombatSystem combatSystem, ExplosionEffects explosionEffects, LegacySimulation legacySimulation, PlantingSystem plantingSystem, ZombieFeedbackSystem zombieFeedbackSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _bossSystem = bossSystem;
            _combatSystem = combatSystem;
            _explosionEffects = explosionEffects;
            _legacySimulation = legacySimulation;
            _plantingSystem = plantingSystem;
            _zombieFeedbackSystem = zombieFeedbackSystem;
        }
        // Explicit notifications survive a headless tick or a host attaching before its first
        // Update. The host drains them at the corresponding existing Planted/Explosion event.
        /// <summary>Performs try take adventure cherry bomb sound.</summary>
        /// <param name = "trigger">The trigger.</param>
        /// <param name = "sound">The sound.</param>
        /// <returns>The resulting try take adventure cherry bomb sound.</returns>
        internal bool TryTakeAdventureCherryBombSound(GameEvent trigger, out global::PlantsVsZombies.Adventure.AdventureCherryBombSound sound)
        {
            var queue = trigger == GameEvent.Planted ? _world._adventureCherryFuseSounds : trigger == GameEvent.Explosion ? _world._adventureCherryDetonationSounds : null;
            if (queue == null || queue.Count == 0)
            {
                sound = default(global::PlantsVsZombies.Adventure.AdventureCherryBombSound);
                return false;
            }

            sound = queue.Dequeue();
            return true;
        }

        // 5E7060 rounds after scaling the native 31-bit draw. NextDouble would round
        // a scale-1 result first, which changes some rate/shake/pitch results by one ULP.
        /// <summary>Performs adventure cherry random float.</summary>
        /// <param name = "range">The range.</param>
        /// <returns>The resulting adventure cherry random float.</returns>
        internal float AdventureCherryRandomFloat(float range)
        {
            return (float)(_world._random.Next() / 2147483647.0 * range);
        }

        /// <summary>Consumes an original Cherry Foley through the same admission and random state as other sounds.</summary>
        /// <param name="foley">The original reverse, boom or juicy Foley category.</param>
        /// <returns>The immutable original playback selection.</returns>
        private AdventureZombieSoundEvent SampleCherryFoley(int foley)
        {
            var sound = new AdventureZombieSoundEvent { Foley = foley, Tick = _world._nativeFeedbackTick };
            _world._foleyRandom.Resolve(sound, _world._random, _world.Rules.AdventureSoundMuted);
            return sound;
        }

        /// <summary>Performs initialize adventure cherry bomb.</summary>
        /// <param name = "plant">The plant.</param>
        internal void InitializeAdventureCherryBomb(Plant plant)
        {
            if (plant.CherryBombPresentation != null)
            {
                return;
            }

            var state = new global::PlantsVsZombies.Adventure.AdventureCherryBombState
            {
                Rate = plant._presentationKind == plant.Kind ? plant.PresentationRate : 10f + AdventureCherryRandomFloat(5f)
            };
            plant.CherryBombPresentation = state;
            plant._presentationKind = PlantKind.CherryBomb;
            plant.PresentationRate = state.Rate;
            plant.ActionTimer = global::PlantsVsZombies.Adventure.AdventureCherryBombState.FuseTicks * .01f;
            // 52A5A0 selects the only original variation with Rand(1), even though its
            // choice is fixed. Keep that draw on the replayable simulation stream.
            var fuse = SampleCherryFoley(57);
            _world._adventureCherryFuseSounds.Enqueue(new AdventureCherryBombSound { SuppressPrimary = fuse.SampleIndex < 0 });
        }

        /// <summary>Performs advance adventure cherry bomb.</summary>
        /// <param name = "plant">The plant.</param>
        /// <returns>The resulting advance adventure cherry bomb.</returns>
        internal bool AdvanceAdventureCherryBomb(Plant plant)
        {
            InitializeAdventureCherryBomb(plant);
            var state = plant.RequiredCherryBombPresentation;
            if (state.Detonated)
            {
                return true;
            }

            // PC 471815..471825 decrements the special counter before testing zero.
            if (state.Countdown > 0)
            {
                state.Countdown--;
            }

            plant.ActionTimer = state.Countdown * .01f;
            if (state.Countdown == 0)
            {
                DetonateAdventureCherryBomb(plant);
            }

            // PC Update 47230D..472317 still calls Animate after DoSpecial/Die.
            // Animate 47321B..47324D consumes two Rand(2)-1 offsets, including tick 100.
            state.ShakeX = AdventureCherryRandomFloat(2f) - 1f;
            state.ShakeY = AdventureCherryRandomFloat(2f) - 1f;
            state.AnimationTicks++;
            state.NormalizedTime = Math.Min(.999999f, state.NormalizedTime + .01f * state.Rate / global::PlantsVsZombies.Adventure.AdventureCherryBombState.ExplodeFrameCount);
            return true;
        }

        /// <summary>Performs detonate adventure cherry bomb.</summary>
        /// <param name = "plant">The plant.</param>
        internal void DetonateAdventureCherryBomb(Plant plant)
        {
            InitializeAdventureCherryBomb(plant);
            var state = plant.RequiredCherryBombPresentation;
            if (state.Detonated)
            {
                return;
            }

            state.Detonated = true;
            state.Countdown = 0;
            plant.ActionTimer = 0;
            // Local PC 474B83..474C32: Foley 55 (boom), then Foley 65 (juicy).
            // Foley table 74B3B8 gives pitch ranges 0 and 2 respectively.
            var boom = SampleCherryFoley(55);
            var juicy = SampleCherryFoley(65);
            _world._adventureCherryDetonationSounds.Enqueue(new AdventureCherryBombSound { Detonation = true,
                JuicyPitchSteps = juicy.ResolvedPitch, SuppressPrimary = boom.SampleIndex < 0, SuppressJuicy = juicy.SampleIndex < 0 });
            _legacySimulation.Raise(GameEvent.Explosion);
            _combatSystem.BlastAdventureAshPlant(plant, 115, 1);
            _explosionEffects.AdventurePresentationExplosion(PlantKind.CherryBomb, plant.Column + .5f, plant.Row);
            _plantingSystem.RemoveAdventurePlantAndStack(plant);
        }

        /// <summary>Starts the PC Jalapeno body on anim_explode with its original independent fuse.</summary>
        /// <param name="plant">The newly placed or transformed Jalapeno.</param>
        internal void InitializeAdventureJalapeno(Plant plant)
        {
            if (plant.JalapenoPresentation != null)
            {
                return;
            }

            // PC 46C07A..46C0A4 selects 10 + RandFloat(5); case 20 shares 46C615's special=100/once-and-hold branch.
            var state = new AdventureJalapenoState { Rate = plant._presentationKind == plant.Kind ? plant.PresentationRate : 10f + AdventureCherryRandomFloat(5f) };
            plant.JalapenoPresentation = state;
            plant._presentationKind = PlantKind.Jalapeno;
            plant.PresentationRate = state.Rate;
            plant.ActionTimer = AdventureJalapenoState.FuseTicks * .01f;
        }

        /// <summary>Advances the PC special countdown before animating the original Jalapeno pose and shake.</summary>
        /// <param name="plant">The Jalapeno whose fixed native update is due.</param>
        /// <returns>True because the native fuse owns this plant's action timer.</returns>
        internal bool AdvanceAdventureJalapeno(Plant plant)
        {
            InitializeAdventureJalapeno(plant);
            var state = plant.JalapenoPresentation!;
            if (state.Detonated)
            {
                return true;
            }

            if (state.Countdown > 0)
            {
                state.Countdown--;
            }

            plant.ActionTimer = state.Countdown * .01f;
            if (state.Countdown == 0)
            {
                DetonateAdventureJalapeno(plant);
            }

            // PC Animate 473200 explicitly includes seed 20; both RandFloat(2)-1 draws also occur on detonation.
            state.ShakeX = AdventureCherryRandomFloat(2f) - 1f;
            state.ShakeY = AdventureCherryRandomFloat(2f) - 1f;
            state.AnimationTicks++;
            state.NormalizedTime = Math.Min(.999999f, state.NormalizedTime + .01f * state.Rate / AdventureJalapenoState.ExplodeFrameCount);
            return true;
        }

        /// <summary>Runs the existing row burn exactly once when the independent native Jalapeno fuse reaches zero.</summary>
        /// <param name="plant">The Jalapeno consumed by its original row special.</param>
        internal void DetonateAdventureJalapeno(Plant plant)
        {
            InitializeAdventureJalapeno(plant);
            var state = plant.JalapenoPresentation!;
            if (state.Detonated)
            {
                return;
            }

            state.Detonated = true;
            state.Countdown = 0;
            plant.ActionTimer = 0f;
            _combatSystem.BurnAdventureRow(plant.Row);
            for (var i = _adventureModeRunner.Adventure._ice.Count - 1; i >= 0; i--)
            {
                if (_adventureModeRunner.Adventure._ice[i].Row == plant.Row)
                {
                    // PC DoSpecial 474B10 retains this row's ice for twenty final updates.
                    _adventureModeRunner.Adventure._ice[i].RemainingTicks = 20;
                }
            }

            for (var i = _adventureModeRunner.Adventure._balls.Count - 1; i >= 0; i--)
            {
                if (_adventureModeRunner.Adventure._balls[i].Row == plant.Row && _adventureModeRunner.Adventure._balls[i].IsIce)
                {
                    _adventureModeRunner.Adventure._balls.RemoveAt(i);
                }
            }
            if (_adventureModeRunner.Adventure.BossHeadVulnerable)
            {
                _bossSystem.DamageAdventureBoss(1800f);
            }

            _explosionEffects.AdventurePresentationExplosion(PlantKind.Jalapeno, plant.Column + .5f, plant.Row);
            _zombieFeedbackSystem.RequestAdventureZombieFoley(65);
            _plantingSystem.RemoveAdventurePlantAndStack(plant);
            _legacySimulation.Raise(GameEvent.JalapenoExplosion);
        }
    }
}
