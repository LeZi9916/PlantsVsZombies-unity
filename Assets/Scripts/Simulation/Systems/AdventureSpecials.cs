#nullable enable
using PlantsVsZombies.Adventure;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns AdventureSpecials responsibilities for one deterministic match.</summary>
    internal sealed class AdventureSpecials
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BowlingSystem operations.</summary>
        private BowlingSystem _bowlingSystem = null!;
        /// <summary>Provides ProjectileSystem operations.</summary>
        private ProjectileSystem _projectileSystem = null!;
        /// <summary>Provides PurchaseSystem operations.</summary>
        private PurchaseSystem _purchaseSystem = null!;
        /// <summary>Provides SunSystem operations.</summary>
        private SunSystem _sunSystem = null!;
        /// <summary>Provides VaseSystem operations.</summary>
        private VaseSystem _vaseSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal AdventureSpecials(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="bowlingSystem">The BowlingSystem collaborator.</param>
        /// <param name="projectileSystem">The ProjectileSystem collaborator.</param>
        /// <param name="purchaseSystem">The PurchaseSystem collaborator.</param>
        /// <param name="sunSystem">The SunSystem collaborator.</param>
        /// <param name="vaseSystem">The VaseSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BowlingSystem bowlingSystem, ProjectileSystem projectileSystem, PurchaseSystem purchaseSystem, SunSystem sunSystem, VaseSystem vaseSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _bowlingSystem = bowlingSystem;
            _projectileSystem = projectileSystem;
            _purchaseSystem = purchaseSystem;
            _sunSystem = sunSystem;
            _vaseSystem = vaseSystem;
        }

        /// <summary>Performs advance adventure specials.</summary>
        /// <param name = "dt">The dt.</param>
        internal void AdvanceAdventureSpecials(float dt)
        {
            if (_world.ModeSession == null && _adventureModeRunner.Adventure.Level.Mode == AdventureMode.Vasebreaker && _adventureModeRunner.Adventure.VaseInterludeActive)
            {
                if (_adventureModeRunner.Adventure._vaseInterludeTicks > 0 && --_adventureModeRunner.Adventure._vaseInterludeTicks == 0)
                {
                    var message = _adventureModeRunner.Adventure.VaseStage == 0 ? 2700 : 2800;
                    _adventureModeRunner.Adventure.VaseStage++;
                    _adventureModeRunner.Adventure._packets.Clear();
                    _world.Plants.Clear();
                    _projectileSystem.ClearPeas();
                    _sunSystem.ClearSuns();
                    _adventureModeRunner.Adventure.RakeRow = -1;
                    _adventureModeRunner.Adventure.RakeAttracting = false;
                    _adventureModeRunner.Adventure.RakeTriggered = false;
                    _adventureModeRunner.Adventure.RakeAge = 0f;
                    _adventureModeRunner.Adventure._rakeDamageDone = false;
                    _vaseSystem.StartVaseScreenFlash();
                    _adventureModeRunner.Adventure.Notice(AdventureNoticeKind.VaseInterludeStarted, message);
                    if (!_adventureModeRunner.Adventure.VaseInterludeDialogueEnabled)
                    {
                        PopulateAdventureVaseInterlude(message == 2700 ? 2702 : 2801);
                        FinishAdventureVaseInterlude();
                    }
                }
            }
            else if (_world.ModeSession == null && _adventureModeRunner.Adventure.Level.Mode == AdventureMode.Vasebreaker && _adventureModeRunner.Adventure._vases.Count == 0 && _world.Zombies.TrueForAll(zombie => zombie.Hypnotized) && _adventureModeRunner.Adventure.VaseStage < 2)
            {
                _world.Zombies.Clear();
                // Board::UpdateLevelEndSequence starts at 500 and presents Dave at 300.
                _adventureModeRunner.Adventure.VaseInterludeActive = true;
                _adventureModeRunner.Adventure._vaseInterludeTicks = 200;
                _adventureModeRunner.Adventure._vaseInterludePopulated = false;
            }

            for (var i = _adventureModeRunner.Adventure._nuts.Count - 1; i >= 0; i--)
            {
                var nut = _adventureModeRunner.Adventure._nuts[i];
                if (_bowlingSystem.AdvanceAdventureBowlingNative(nut, dt, false))
                {
                    _adventureModeRunner.Adventure._nuts.RemoveAt(i);
                }
            }
        }

        /// <summary>Populates the next adventure vase stage at the original dialogue entry.</summary>
        /// <param name="messageIndex">The original Crazy Dave message being presented.</param>
        /// <returns>Whether this entry populated the next stage.</returns>
        internal bool PopulateAdventureVaseInterlude(int messageIndex)
        {
            if (!_adventureModeRunner.Adventure.VaseInterludeActive || _adventureModeRunner.Adventure._vaseInterludeTicks != 0 || _adventureModeRunner.Adventure._vaseInterludePopulated
                || (_adventureModeRunner.Adventure.VaseStage == 1 ? messageIndex != 2702 : _adventureModeRunner.Adventure.VaseStage == 2 ? messageIndex != 2801 : true))
            {
                return false;
            }

            _adventureModeRunner.Adventure._vaseInterludePopulated = true;
            _vaseSystem.InitializeVaseStage();
            _purchaseSystem.InitializeAdventurePurchases();
            return true;
        }

        /// <summary>Finishes the vase transition after Dave has departed.</summary>
        /// <returns>Whether an already populated transition was finished.</returns>
        internal bool FinishAdventureVaseInterlude()
        {
            if (!_adventureModeRunner.Adventure.VaseInterludeActive || !_adventureModeRunner.Adventure._vaseInterludePopulated)
            {
                return false;
            }

            _adventureModeRunner.Adventure.VaseInterludeActive = false;
            return true;
        }
    }
}
