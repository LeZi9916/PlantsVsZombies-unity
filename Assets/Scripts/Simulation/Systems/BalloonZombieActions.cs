#nullable enable
using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns BalloonZombieActions responsibilities for one deterministic match.</summary>
    internal sealed class BalloonZombieActions
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides LootSystem operations.</summary>
        private LootSystem _lootSystem = null!;
        /// <summary>Provides VaultZombieActions operations.</summary>
        private VaultZombieActions _vaultZombieActions = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Provides ZombieLifecycle operations.</summary>
        private ZombieLifecycle _zombieLifecycle = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal BalloonZombieActions(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="lootSystem">The LootSystem collaborator.</param>
        /// <param name="vaultZombieActions">The VaultZombieActions collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        /// <param name="zombieLifecycle">The ZombieLifecycle collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, LootSystem lootSystem, VaultZombieActions vaultZombieActions, ZombieFeedbackSystem zombieFeedbackSystem, ZombieLifecycle zombieLifecycle)
        {
            _adventureModeRunner = adventureModeRunner;
            _lootSystem = lootSystem;
            _vaultZombieActions = vaultZombieActions;
            _zombieFeedbackSystem = zombieFeedbackSystem;
            _zombieLifecycle = zombieLifecycle;
        }

        /// <summary>Marks the original live flying targets of Plant::BlowAwayFliers without removing their visible bodies.</summary>
        internal void BlowAdventureFliersAway()
        {
            foreach (var zombie in _world.Zombies)
            {
                if (zombie.Health > 0f && zombie.Airborne && zombie.PresentationNativePhase != 74
                    && !(zombie.PresentationNativePhase >= 1 && zombie.PresentationNativePhase <= 3))
                {
                    zombie.BlowingAway = true;
                }
            }
        }

        /// <summary>Applies PC 546A20's original ten-pixel wind displacement and integer-position exit check.</summary>
        /// <param name="zombie">The living actor whose original BlowingAway flag may be set.</param>
        /// <returns>Whether the native right-edge exit removed the actor this update.</returns>
        internal bool AdvanceAdventureBalloonWind(Zombie zombie)
        {
            if (!zombie.BlowingAway)
            {
                return false;
            }

            // PC checks the previous integer mX, then adds ten to float mPosX independently of frozen walking.
            var nativeX = (int)AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
            zombie.X += 10f / 80f;
            if (nativeX <= 850)
            {
                return false;
            }

            // The original endpoint is DieWithLoot (54C060), with immediate body removal and one ordinary loot roll.
            _lootSystem.DropAdventureLoot(zombie);
            zombie.PresentationDeath = AdventureZombieDeathPresentation.Removed;
            _zombieLifecycle.KillAdventureZombie(zombie);
            return true;
        }

        /// <summary>Runs PC LandFlyer's original pop transition and immediate pool-row removal.</summary>
        /// <param name="zombie">The airborne balloon whose attachment health has reached zero.</param>
        /// <param name="damageFlags">The original damage flags, including suppression of pop artwork and sound.</param>
        internal void LandAdventureBalloon(Zombie zombie, uint damageFlags)
        {
            if (zombie.Kind != ZombieKind.Balloon)
            {
                return;
            }

            if ((damageFlags & 16U) == 0 && zombie.PresentationNativePhase == 73)
            {
                // PC 5415F0 selects the original twenty-eight-frame pop at 24 fps, with a twenty-update blend.
                _zombieFeedbackSystem.RequestAdventureZombieFoley(22);
                _vaultZombieActions.SetAdventureZombieMotionPhase(zombie, 74);
                zombie.PresentationBalloonBlendTicks = 20;
            }

            if (_adventureModeRunner.Adventure.Level.IsWaterRow(zombie.Row))
            {
                // PC removes a popped balloon immediately on any pool row, retaining its one original loot roll.
                _lootSystem.DropAdventureLoot(zombie);
                zombie.PresentationDeath = AdventureZombieDeathPresentation.Removed;
                _zombieLifecycle.KillAdventureZombie(zombie);
                return;
            }

            zombie.PresentationNativeHeight = 7;
        }

        /// <summary>Completes PC UpdateZombieFlyer's pop before releasing the original walking body.</summary>
        /// <param name="zombie">The balloon whose once-and-hold pop clock may have completed.</param>
        internal void AdvanceAdventureBalloon(Zombie zombie)
        {
            if (zombie.Kind != ZombieKind.Balloon || zombie.PresentationNativePhase != 74 || zombie._presentationWalkLoops <= 0)
            {
                return;
            }

            zombie.Airborne = false;
            zombie.UsedAbility = true;
            _vaultZombieActions.SetAdventureZombieMotionPhase(zombie, 75);
            zombie.PresentationBalloonBlendTicks = 0;
        }
    }
}
