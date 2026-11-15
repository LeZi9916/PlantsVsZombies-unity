#nullable enable
using PlantsVsZombies.Adventure;

using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns SeedBankSystem responsibilities for one deterministic match.</summary>
    internal sealed class SeedBankSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BoardQueries operations.</summary>
        private BoardQueries _boardQueries = null!;
        /// <summary>Provides CoinSystem operations.</summary>
        private CoinSystem _coinSystem = null!;
        /// <summary>Provides ConveyorSystem operations.</summary>
        private ConveyorSystem _conveyorSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal SeedBankSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="boardQueries">The BoardQueries collaborator.</param>
        /// <param name="coinSystem">The CoinSystem collaborator.</param>
        /// <param name="conveyorSystem">The ConveyorSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BoardQueries boardQueries, CoinSystem coinSystem, ConveyorSystem conveyorSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _boardQueries = boardQueries;
            _coinSystem = coinSystem;
            _conveyorSystem = conveyorSystem;
        }

        /// <summary>Initializes PC 49F5B0's purple packet startup cooldown while retaining selected survival packets between stages.</summary>
        /// <param name="selected">The newly confirmed seed choices before replacing the previous bank.</param>
        internal void InitializeUpgradePacketCooldowns(IReadOnlyList<PlantKind> selected)
        {
            var mode = _world.ModeSession;
            var nativeMode = mode?.Definition.Id ?? 0;
            var exempt = nativeMode == 20 || nativeMode == 24 || nativeMode == 23 || nativeMode == 31
                || mode?.Definition.Puzzle == true || mode?.Definition.Mechanic == global::PlantsVsZombies.Modes.ModeMechanic.Whack;
            foreach (var kind in selected)
            {
                if (!AdventureUpgradeSeedRules.IsUpgrade(kind))
                {
                    continue;
                }
                if (mode?.Definition.Survival == true && mode.Stage > 1)
                {
                    if (!ContainsSeed(_adventureModeRunner.Adventure.SelectedSeeds, kind))
                    {
                        _world._seedCooldownTicks[(int)kind] = 0;
                        _world._seedInitialCooldownThresholds[(int)kind] = 0;
                    }
                    continue;
                }
                _world._seedInitialCooldownThresholds[(int)kind] = exempt ? 0 : 3500;
                _world._seedCooldownTicks[(int)kind] = exempt ? 0 : 3501;
            }
        }

        /// <summary>Performs initialize replay seed choices.</summary>
        internal void InitializeReplaySeedChoices()
        {
            if (!_adventureModeRunner.Adventure.Level.IsReplay || !_adventureModeRunner.Adventure.Level.ChoosesSeeds)
            {
                return;
            }

            var level = _adventureModeRunner.Adventure.Level;
            var candidates = new List<PlantKind>();
            bool balloons = false, roofRaid = false;
            foreach (var wave in _adventureModeRunner.Adventure.Waves)
            {
                foreach (var z in wave.Zombies)
                {
                    if (z == ZombieKind.Balloon)
                    {
                        balloons = true;
                    }

                    if (z == ZombieKind.Bungee || z == ZombieKind.Catapult)
                    {
                        roofRaid = true;
                    }
                }
            }

            foreach (var kind in level.UnlockedSeeds)
            {
                if (!level.IsNight && PlantingSystem.Nocturnal(kind) || level.IsNight && kind == PlantKind.CoffeeBean)
                {
                    continue;
                }

                if (kind == PlantKind.GraveBuster && level.Terrain != AdventureTerrain.Night || kind == PlantKind.Plantern && level.Terrain != AdventureTerrain.Fog || kind == PlantKind.FlowerPot && !level.HasRoof)
                {
                    continue;
                }

                if (!level.HasPool && (kind == PlantKind.LilyPad || kind == PlantKind.TangleKelp || kind == PlantKind.SeaShroom))
                {
                    continue;
                }

                if (level.HasRoof && (kind == PlantKind.Spikeweed || kind == PlantKind.Torchwood))
                {
                    continue;
                }

                if (kind == PlantKind.UmbrellaLeaf && !roofRaid || kind == PlantKind.Blover && !balloons && level.Terrain != AdventureTerrain.Fog)
                {
                    continue;
                }

                candidates.Add(kind);
            }

            var rng = new global::PlantsVsZombies.Randomness.OriginalRandom(unchecked((uint)_world.Rules.AdventureDaveSeed));
            var required = new List<PlantKind>();
            for (var i = 0; i < 3 && candidates.Count > 0; i++)
            {
                var pick = (int)rng.NextBounded((uint)candidates.Count);
                required.Add(candidates[pick]);
                candidates.RemoveAt(pick);
            }

            _adventureModeRunner.Adventure.RequiredSeeds = required.AsReadOnly();
            var defaults = new List<PlantKind>(required);
            foreach (var k in level.DefaultSeeds)
            {
                if (defaults.Count < level.PacketSlots && !defaults.Contains(k))
                {
                    defaults.Add(k);
                }
            }

            foreach (var k in _adventureModeRunner.Adventure.AvailableSeeds)
            {
                if (defaults.Count < level.PacketSlots && !defaults.Contains(k) && k != PlantKind.Imitater)
                {
                    defaults.Add(k);
                }
            }

            _adventureModeRunner.Adventure.SelectedSeeds = defaults.AsReadOnly();
        }

        /// <summary>Performs bank adventure coins.</summary>
        /// <returns>The resulting bank adventure coins.</returns>
        internal bool BankAdventureCoins()
        {
            if (!_world.IsAdventure)
            {
                return false;
            }

            foreach (var coin in new PooledSnapshot<AdventureCoin>(_adventureModeRunner.Adventure._coins))
            {
                if (!_coinSystem.FinishAdventureCoin(coin))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Performs adventure packet context.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "playing">The playing.</param>
        /// <param name = "darken">The darken.</param>
        /// <param name = "anticipatedSun">The anticipated sun.</param>
        /// <param name="destination">An optional immediately consumed context to overwrite; null creates an independent snapshot.</param>
        /// <returns>The supplied destination or a newly allocated snapshot of the current packet state.</returns>
        internal AdventureSeedBankContext AdventurePacketContext(PlantKind kind, bool playing = true, int darken = 255, int anticipatedSun = -1, AdventureSeedBankContext? destination = null)
        {
            int left = _world._seedCooldownTicks[(int)kind], duration = _world._seedInitialCooldownThresholds[(int)kind];
            if (duration == 0)
            {
                duration = LegacySimulation.DefinitionFor(ResolveAdventureSeed(kind)).RechargeThresholdRaw;
            }
            var context = destination ?? new AdventureSeedBankContext();
            context.Reset();
            context.PacketKind = (int)kind;
            context.ImitaterKind = kind == PlantKind.Imitater && _adventureModeRunner.Adventure.ImitaterTarget.HasValue ? (int)_adventureModeRunner.Adventure.ImitaterTarget.Value : -1;
            context.Active = left <= 0;
            context.Refreshing = left > 0;
            context.RefreshTime = duration;
            context.RefreshCounter = left > 0 ? Math.Max(0, duration + 1 - left) : 0;
            context.Playing = playing && _world.State == MatchState.Playing && _adventureModeRunner.Adventure.Phase == AdventurePhase.Playing;
            context.Paused = _world.State == MatchState.Paused;
            context.TutorialState = _adventureModeRunner.Adventure.Tutorial == AdventureTutorial.ChooseSeed ? 1 : _adventureModeRunner.Adventure.Tutorial == AdventureTutorial.PlantSunflower ? 5 : _adventureModeRunner.Adventure.Tutorial == AdventureTutorial.RechargeSunflower ? 9 : 0;
            context.TutorialTimer = -1;
            context.MainCounter = _adventureModeRunner.Adventure._ticks;
            context.CutsceneDarken = darken;
            context.NativeMode = _world.ModeSession == null ? 0 : _world.ModeSession.Definition.Id;
            context.CanAfford = (anticipatedSun >= 0 ? anticipatedSun : AdventureSunAvailable) >= AdventureSeedCost(kind);
            context.Conveyor = _conveyorSystem.UsesNativeAdventureConveyor;
            context.PlantingRequirementsMet = AdventurePacketRequirements(kind);
            return context;
        }

        /// <summary>Performs adventure packet requirements.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting adventure packet requirements.</returns>
        internal bool AdventurePacketRequirements(PlantKind kind)
        {
            var resolved = ResolveAdventureSeed(kind);
            if (kind == PlantKind.Imitater && !_adventureModeRunner.Adventure.ImitaterTarget.HasValue)
            {
                return false;
            }

            if (resolved == PlantKind.CoffeeBean)
            {
                return _world.Plants.Exists(p => p.Asleep && p.WakeTimer <= 0 && _boardQueries.AdventurePlantAt(p.Row, p.Column, PlantLayer.Effect) == null);
            }

            if (resolved == PlantKind.GraveBuster)
            {
                return _adventureModeRunner.Adventure.Graves.Count > 0;
            }

            var prerequisite = UpgradePlantActions.UpgradePrerequisite(resolved);
            if (!prerequisite.HasValue)
            {
                return true;
            }

            foreach (Plant p in _world.Plants)
            {
                if (p.Kind == prerequisite.Value && (resolved != PlantKind.CobCannon || _boardQueries.HasPlantKindAt(PlantKind.KernelPult, p.Row, p.Column + 1)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Performs advance adventure packet feedback.</summary>
        /// <param name = "dt">The dt.</param>
        internal void AdvanceAdventurePacketFeedback(float dt)
        {
            _conveyorSystem.AdvanceAdventureConveyorPackets();
            for (var i = _adventureModeRunner.Adventure.SeedFlashes.Count - 1; i >= 0; i--)
            {
                _adventureModeRunner.Adventure.SeedFlashes[i].Age += dt;
                if (_adventureModeRunner.Adventure.SeedFlashes[i].Age >= .25f)
                {
                    _adventureModeRunner.Adventure.SeedFlashes.RemoveAt(i);
                }
            }

            if (_adventureModeRunner.Adventure._ticks == 1)
            {
                foreach (PlantKind kind in _adventureModeRunner.Adventure.SelectedSeeds)
                {
                    AdventurePacketReady(kind);
                }
            }
        }

        /// <summary>Performs adventure packet ready.</summary>
        /// <param name = "kind">The kind.</param>
        /// <param name = "anticipatedSun">The anticipated sun.</param>
        internal void AdventurePacketReady(PlantKind kind, int anticipatedSun = -1)
        {
            if (_conveyorSystem.UsesNativeAdventureConveyor || !ContainsSeed(_adventureModeRunner.Adventure.SelectedSeeds, kind))
            {
                return;
            }

            var flash = AdventureSeedBankPresentation.ReadyCheck(AdventurePacketContext(kind, anticipatedSun: anticipatedSun, destination: _world._packetFeedbackContext), 0, 0, 0, 0);
            if (flash.Spawn)
            {
                _adventureModeRunner.Adventure.SeedFlashes.Add(new AdventureSeedFlashState { Id = ++_adventureModeRunner.Adventure._seedFlashId, Kind = kind });
            }
        }

        /// <summary>Performs adventure sun selected for packet feedback.</summary>
        /// <param name = "added">The added.</param>
        internal void AdventureSunSelectedForPacketFeedback(global::PlantsVsZombies.Simulation.Sun added)
        {
            var collecting = 0;
            foreach (global::PlantsVsZombies.Simulation.Sun sun in _world.Suns)
            {
                if (sun.PresentationNative && sun.PresentationPickup.Collecting)
                {
                    collecting += sun.Value;
                }
            }

            foreach (PlantKind kind in _adventureModeRunner.Adventure.SelectedSeeds)
            {
                if (AdventureSeedBankPresentation.SunCrossesAffordability(_world.SunAmount - _world._adventureSunReservation, collecting, AdventureSeedCost(kind), added.Value))
                {
                    AdventurePacketReady(kind);
                }
            }
        }

        /// <summary>Performs contains seed.</summary>
        /// <param name = "seeds">The seeds.</param>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting contains seed.</returns>
        internal static bool ContainsSeed(IReadOnlyList<PlantKind> seeds, PlantKind kind)
        {
            for (var i = 0; i < seeds.Count; i++)
            {
                if (seeds[i] == kind)
                {
                    return true;
                }
            }

            return false;
        }
        /// <summary>Gets or sets adventure sun available.</summary>
        internal int AdventureSunAvailable
        {
            get
            {
                var total = _world.SunAmount - _world._adventureSunReservation;
                if (_world.IsAdventure)
                {
                    foreach (global::PlantsVsZombies.Simulation.Sun sun in _world.Suns)
                    {
                        if (sun.PresentationNative && sun.PresentationPickup.Collecting && !sun.PresentationPickup.Dead)
                        {
                            total += sun.Value;
                        }
                    }
                }

                return total;
            }
        }

        /// <summary>Performs spend adventure sun.</summary>
        /// <param name = "cost">The cost.</param>
        /// <returns>The resulting spend adventure sun.</returns>
        internal bool SpendAdventureSun(int cost)
        {
            if (cost < 0 || AdventureSunAvailable < cost)
            {
                return false;
            }

            var visible = Math.Min(_world.SunAmount, cost);
            _world.SunAmount -= visible;
            _world._adventureSunReservation += cost - visible;
            return true;
        }

        /// <summary>Performs settle adventure sun reservation.</summary>
        internal void SettleAdventureSunReservation()
        {
            var paid = Math.Min(_world.SunAmount, _world._adventureSunReservation);
            _world.SunAmount -= paid;
            _world._adventureSunReservation -= paid;
        }

        /// <summary>Performs initialize adventure seed access.</summary>
        /// <exception cref = "ArgumentException">The operation cannot satisfy its preconditions.</exception>
        internal void InitializeAdventureSeedAccess()
        {
            var values = new List<PlantKind>();
            foreach (var k in _adventureModeRunner.Adventure.Level.UnlockedSeeds)
            {
                values.Add(k);
            }

            var access = _world.Rules.AdventureSeedAccess;
            if (access != null)
            {
                foreach (var k in access.AdditionalSeeds)
                {
                    if (!values.Contains(k))
                    {
                        values.Add(k);
                    }
                }

                if (access.ImitaterTarget.HasValue)
                {
                    if (!ContainsSeed(_adventureModeRunner.Adventure.Level.UnlockedSeeds, access.ImitaterTarget.Value))
                    {
                        throw new ArgumentException("Imitater target is not yet earned on this level.");
                    }

                    _adventureModeRunner.Adventure.ImitaterTarget = access.ImitaterTarget;
                }
            }

            _adventureModeRunner.Adventure.AvailableSeeds = Array.AsReadOnly(values.ToArray());
        }

        /// <summary>Performs set adventure imitater target.</summary>
        /// <param name = "target">The target.</param>
        /// <returns>The resulting set adventure imitater target.</returns>
        internal bool SetAdventureImitaterTarget(PlantKind target)
        {
            if (!_world.IsAdventure || _world.State != MatchState.Playing || _adventureModeRunner.Adventure.Phase != AdventurePhase.ChoosingSeeds || !ContainsSeed(_adventureModeRunner.Adventure.AvailableSeeds, PlantKind.Imitater) || (int)target < 0 || (int)target > 39 || !ContainsSeed(_adventureModeRunner.Adventure.Level.UnlockedSeeds, target))
            {
                return false;
            }

            _adventureModeRunner.Adventure.ImitaterTarget = target;
            return true;
        }

        /// <summary>Performs adventure seed cost.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting adventure seed cost.</returns>
        internal int AdventureSeedCost(PlantKind kind)
        {
            var cost = LegacySimulation.Cost(ResolveAdventureSeed(kind));
            if (AdventureUpgradeSeedRules.UsesAcceleratedPricing(kind, _world.ModeSession?.Definition.Id ?? 0))
            {
                foreach (var plant in _world.Plants)
                {
                    if (plant.Kind == kind)
                    {
                        cost += 50;
                    }
                }
            }
            return cost;
        }

        /// <summary>Performs adventure seed recharge time.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting adventure seed recharge time.</returns>
        internal float AdventureSeedRechargeTime(PlantKind kind)
        {
            return LegacySimulation.RechargeTime(ResolveAdventureSeed(kind));
        }

        /// <summary>Performs resolve adventure seed.</summary>
        /// <param name = "kind">The kind.</param>
        /// <returns>The resulting resolve adventure seed.</returns>
        /// <exception cref = "InvalidOperationException">The operation cannot satisfy its preconditions.</exception>
        internal PlantKind ResolveAdventureSeed(PlantKind kind)
        {
            if (kind != PlantKind.Imitater)
            {
                return kind;
            }

            if (!_world.IsAdventure || !_adventureModeRunner.Adventure.ImitaterTarget.HasValue)
            {
                throw new InvalidOperationException("Select an earned basic plant as the Imitater target first.");
            }

            return _adventureModeRunner.Adventure.ImitaterTarget.Value;
        }
        /// <summary>Advances seed cooldowns at the caller's original position in its native update.</summary>
        /// <param name="feedback">Whether this mode emits native packet-ready feedback.</param>
        internal void AdvanceCooldowns(bool feedback)
        {
            for (var i = 0; i < _world._seedCooldownTicks.Length; i++)
            {
                if (_world._seedCooldownTicks[i] > 0)
                {
                    _world._seedCooldownTicks[i]--;
                    if (feedback && _world._seedCooldownTicks[i] == 0)
                    {
                        AdventurePacketReady((PlantKind)i);
                    }
                }
            }
        }
    }
}
