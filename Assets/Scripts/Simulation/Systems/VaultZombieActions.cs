#nullable enable
using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns VaultZombieActions responsibilities for one deterministic match.</summary>
    internal sealed class VaultZombieActions
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BoardQueries operations.</summary>
        private BoardQueries _boardQueries = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal VaultZombieActions(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="boardQueries">The BoardQueries collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BoardQueries boardQueries, ZombieFeedbackSystem zombieFeedbackSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _boardQueries = boardQueries;
            _zombieFeedbackSystem = zombieFeedbackSystem;
        }

        /// <summary>Restarts a native one-shot phase, including transitions that reuse the same clip.</summary>
        /// <param name="z">The zombie changing phase.</param>
        /// <param name="phase">The original PC phase identifier.</param>
        internal void SetAdventureZombieMotionPhase(Zombie z, int phase)
        {
            z.PresentationNativePhase = phase;
            if (z.Kind == ZombieKind.PoleVaulter)
            {
                z.PresentationPoleVaultBlendTicks = phase == 13 ? 0 : 20;
            }

            z.PresentationAnimation = string.Empty;
            _zombieFeedbackSystem.SynchronizeAdventureZombieAnimation(z);
            _zombieFeedbackSystem.RefreshAdventureZombieNativeSpeed(z);
        }

        /// <summary>Starts the authored vault without teleporting the zombie across its target.</summary>
        /// <param name="z">The pole vaulter beginning its jump.</param>
        /// <param name="target">The original vault target, including a possible Tall-nut.</param>
        internal void BeginAdventurePoleVault(Zombie z, Plant target)
        {
            SetAdventureZombieMotionPhase(z, 12);
            var nativeX = (int)AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z));
            var distance = _adventureModeRunner.Adventure.Level.Mode == AdventureMode.Bowling ? 0 : nativeX - (40 + target.Column * 80) - 80;
            // Original anim_jump contains 43 frames and runs at 24 fps.
            z._poleVaultVelocity = distance / (43f / 24f * 100f);
            z.UsedAbility = true;
            z.Eating = false;
        }

        /// <summary>Moves a pole vault and dispatches its original normalized animation events.</summary>
        /// <param name="z">The zombie whose vault may be active.</param>
        /// <param name="dt">The fixed simulation interval in seconds.</param>
        /// <returns>Whether this tick is consumed by the vault.</returns>
        internal bool AdvanceAdventurePoleVault(Zombie z, float dt)
        {
            if (z.Kind != ZombieKind.PoleVaulter || z.PresentationNativePhase != 12)
            {
                return false;
            }

            var before = z._presentationPreviousWalkCycle;
            var progress = z._presentationWalkLoops + (double)z._presentationWalkPhase;
            if (AdventureZombieBiteRules.Crossed(before, progress, .2f))
            {
                _zombieFeedbackSystem.RequestAdventureZombieFoley(74);
            }

            if (AdventureZombieBiteRules.Crossed(before, progress, .4f))
            {
                _zombieFeedbackSystem.RequestAdventureZombieFoley(78);
            }

            if (progress > .6f && progress <= .7f)
            {
                var barrier = _boardQueries.FindAdventureNativePlantTarget(z, AdventureBiteAttack.Vault);
                if (barrier != null && barrier.Kind == PlantKind.TallNut)
                {
                    _zombieFeedbackSystem.RequestAdventureZombieFoley(48);
                    var pooledRecord1 = _world.LifetimePool.Rent<AdventurePlantBiteEffect>();
                    pooledRecord1.Id = ++_world.Presentation._effectId;
                    pooledRecord1.Row = z.Row;
                    pooledRecord1.ParticleId = 29;
                    pooledRecord1.NativeX = 40 + barrier.Column * 80 + 60;
                    pooledRecord1.NativeY = 80 + barrier.Row * (_adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85 : 100) - 20;
                    pooledRecord1.Seed = z.Id;
                    _world.Presentation.PlantBiteEffects.Add(pooledRecord1);
                    z.X = AdventureZombieNativeRules.GridX(40 + barrier.Column * 80, AdventureDancerRules.Scale(z));
                    // PC 0x541A98 changes mPosY, not mAltitude; the next position update restores the row floor.
                    z.PresentationVaultPositionY = -30f;
                    z.PresentationNativeHeight = 7;
                    SetAdventureZombieMotionPhase(z, 13);
                    return true;
                }
            }

            if (progress >= 1d)
            {
                // The native jump artwork has moved 150 pixels relative to the walking origin.
                z.X -= 150f / 80f;
                // The authored jump displacement becomes the walking origin, including its interpolation history.
                z.PresentationPreviousX -= 150f / 80f;
                z.PresentationVaultPositionY = 0f;
                SetAdventureZombieMotionPhase(z, 13);
                return true;
            }

            // PC vault velocity ignores chill; the authored reanimation clock still slows down.
            z.X -= z._poleVaultVelocity * (dt * 100f) / 80f;
            if (_adventureModeRunner.Adventure.Level.HasRoof)
            {
                var nativeX = AdventureZombieNativeRules.NativePositionX(z.X, AdventureDancerRules.Scale(z));
                z.PresentationVaultPositionY = AdventureZombieMotionRules.PoleVaultRoofOffset(nativeX, (float)progress);
            }

            return true;
        }
    }
}
