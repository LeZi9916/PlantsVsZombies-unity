#nullable enable
using PlantsVsZombies.Adventure;

using PlantsVsZombies.Modes;

using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns BoardEnvironment responsibilities for one deterministic match.</summary>
    internal sealed class BoardEnvironment
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal BoardEnvironment(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, ZombieFeedbackSystem zombieFeedbackSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _zombieFeedbackSystem = zombieFeedbackSystem;
        }

        /// <summary>Applies the original storm state before the level introduction.</summary>
        internal void BeginAdventureStormIntro()
        {
            if (_world.IsAdventure && _adventureModeRunner.Adventure.Level.Mode == AdventureMode.Storm)
            {
                _adventureModeRunner.Adventure.Storm.BeginIntro();
            }
        }

        /// <summary>Applies the original storm countdown when the introduction finishes.</summary>
        internal void BeginAdventureStormPlaying()
        {
            if (_world.IsAdventure && _adventureModeRunner.Adventure.Level.Mode == AdventureMode.Storm)
            {
                _adventureModeRunner.Adventure.Storm.BeginPlaying();
            }
        }

        /// <summary>Advances the original storm challenge after the board objects for one native update.</summary>
        /// <param name="intro">Whether the native scene is still LevelIntro.</param>
        /// <param name="paused">Whether to freeze rain and board counters while allowing the flash to settle.</param>
        internal void AdvanceAdventureStormTick(bool intro = false, bool paused = false)
        {
            if (!_world.IsAdventure || _adventureModeRunner.Adventure.Level.Mode != AdventureMode.Storm)
            {
                return;
            }

            var yeti = false;
            foreach (var zombie in _world.Zombies)
            {
                if (zombie.Kind == ZombieKind.Yeti)
                {
                    yeti = true;
                    break;
                }
            }

            if (_world.State == MatchState.Lost && !_adventureModeRunner.Adventure.Storm.Lost)
            {
                _adventureModeRunner.Adventure.Storm.BeginLoss();
            }

            if (_adventureModeRunner.Adventure.Storm.Advance(_world._random, !intro && _world.State != MatchState.Lost, paused, _world.State == MatchState.Lost, yeti))
            {
                _zombieFeedbackSystem.RequestAdventureZombieFoley(17);
            }
        }

        /// <summary>Continues native storm updates while the regular simulation is suspended.</summary>
        /// <param name="delta">The elapsed presentation time in seconds.</param>
        /// <param name="paused">Whether the board is paused rather than showing the loss sequence.</param>
        /// <param name="intro">Whether the suspended board is in its introduction.</param>
        internal void AdvanceAdventureStormPresentation(float delta, bool paused, bool intro = false)
        {
            if (!_world.IsAdventure || _adventureModeRunner.Adventure.Level.Mode != AdventureMode.Storm || delta <= 0f || float.IsNaN(delta) || float.IsInfinity(delta))
            {
                return;
            }

            var storm = _adventureModeRunner.Adventure.Storm;
            if (_world.State == MatchState.Lost && !storm.Lost)
            {
                storm.BeginLoss();
            }

            storm.PresentationRemainder += delta;
            while (storm.PresentationRemainder + .0000001 >= SimulationWorld.SimulationStep)
            {
                _world._nativeFeedbackTick++;
                if (_world.State == MatchState.Lost && !paused)
                {
                    var foley = storm.AdvanceLossClock();
                    if (foley >= 0)
                    {
                        _zombieFeedbackSystem.RequestAdventureZombieFoley(foley, presentationTick: (int)Math.Round(_world.Elapsed * 100f) + storm.LostTicks);
                    }
                }

                AdvanceAdventureStormTick(intro, paused || storm.LossDialogReady);
                storm.PresentationRemainder -= SimulationWorld.SimulationStep;
                if (Math.Abs(storm.PresentationRemainder) < .0000001)
                {
                    storm.PresentationRemainder = 0d;
                }
            }
        }

        /// <summary>Gets whether the original stage uses fog, excluding Stormy Night and Invisighoul.</summary>
        internal bool AdventureHasFog => _world.IsAdventure && _adventureModeRunner.Adventure.Level.Terrain == AdventureTerrain.Fog
            && _adventureModeRunner.Adventure.Level.Mode != AdventureMode.Storm && (_world.ModeSession == null || _world.ModeSession.Definition.Id != 21);

        /// <summary>Gets the original stage-dependent first fog column.</summary>
        internal int AdventureFogLeftColumn => AdventureFogState.LeftColumn(_adventureModeRunner.Adventure.Level.Number,
            _world.ModeSession != null, _world.ModeSession != null && _world.ModeSession.Definition.Id == 41);

        /// <summary>Starts the original off-lawn fog state before the street preview and seed chooser.</summary>
        internal void BeginAdventureFogIntro()
        {
            if (AdventureHasFog)
            {
                _adventureModeRunner.Adventure.FogBlownRemaining = 2f;
                _adventureModeRunner.Adventure.Fog.BeginIntro(AdventureFogLeftColumn);
            }
        }

        /// <summary>Advances one original Board fog update and its playing or opening countdown.</summary>
        /// <param name="intro">Whether the level opening owns the countdown instead of gameplay.</param>
        /// <param name="rollIn">Whether the opening has reached TimeFogRollIn.</param>
        internal void AdvanceAdventureFog(bool intro = false, bool rollIn = false)
        {
            if (!AdventureHasFog)
            {
                return;
            }

            _adventureModeRunner.Adventure.Fog.Advance(AdventureFogLeftColumn, _adventureModeRunner.Adventure.FogBlownRemaining, intro, _world.Plants);
            if (!intro || rollIn)
            {
                if (_adventureModeRunner.Adventure.FogBlownRemaining < 100f)
                {
                    var ticks = (int)Math.Round(_adventureModeRunner.Adventure.FogBlownRemaining * 100f);
                    _adventureModeRunner.Adventure.FogBlownRemaining = Math.Max(0, ticks - 1) * .01f;
                }
            }
        }

        /// <summary>Performs is adventure cell visible.</summary>
        /// <param name = "row">The row.</param>
        /// <param name = "column">The column.</param>
        /// <returns>The resulting is adventure cell visible.</returns>
        internal bool IsAdventureCellVisible(int row, int column)
        {
            if (!_world.IsAdventure || !_adventureModeRunner.Adventure.Level.IsLawnRow(row) || column < 0 || column >= 9)
            {
                return false;
            }

            if (!AdventureHasFog || _adventureModeRunner.Adventure.FogBlownRemaining > 0f || column < AdventureFogLeftColumn)
            {
                return true;
            }

            foreach (var plant in _world.Plants)
            {
                var dx = Math.Abs(plant.Column - column);
                var dy = Math.Abs(plant.Row - row);
                if (plant.Health > 0f && (plant.Kind == PlantKind.Plantern && dx <= 3 && dy <= 2 && dx + dy != 5
                    || plant.Kind == PlantKind.Torchwood && dx + dy <= 1))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Advances the PC outer-board PoolEffect counter before gameplay objects update.</summary>
        /// <param name="survivalRepick">Whether PC CutScene::IsSurvivalRepick (0x446350) suppresses the water update.</param>
        internal void AdvanceAdventurePoolEffect(bool survivalRepick = false)
        {
            // PC Board::Update 0x421411..0x42143E: ice and survival repick stop the pool, not plant bobbing.
            if (_world.IsAdventure && _adventureModeRunner.Adventure.Level.HasPool && _adventureModeRunner.Adventure.IceTrapTicks == 0
                && _world.State != MatchState.Paused && _world.State != MatchState.Lost && !survivalRepick)
            {
                _adventureModeRunner.Adventure.PoolEffectTicks++;
            }
        }

        /// <summary>Advances only the original water clock while gameplay is held by a chooser or preparation UI.</summary>
        /// <param name="seconds">The elapsed unpaused board presentation time.</param>
        /// <param name="survivalRepick">Whether a survival stage is returning to seed selection.</param>
        internal void AdvanceAdventurePoolPresentation(float seconds, bool survivalRepick = false)
        {
            if (!_world.IsAdventure || _world.State == MatchState.Paused || _world.State == MatchState.Lost || seconds <= 0f
                || float.IsNaN(seconds) || float.IsInfinity(seconds))
            {
                return;
            }

            _world._idlePoolRemainder += seconds;
            while (_world._idlePoolRemainder + .0000001 >= SimulationWorld.SimulationStep)
            {
                _world._idlePoolRemainder -= SimulationWorld.SimulationStep;
                AdvanceAdventurePoolEffect(survivalRepick);
            }
        }

        /// <summary>Decrements PC Board::mIceTrapCounter after this tick's plants can reset it to 300.</summary>
        internal void FinishAdventureIceTrapTick()
        {
            // PC 0x4748CC sets 300 during UpdateGameObjects; 0x421042 then decrements it in that same tick.
            if (_adventureModeRunner.Adventure.IceTrapTicks > 0)
            {
                _adventureModeRunner.Adventure.IceTrapTicks--;
            }
        }

        /// <summary>Performs tick cell timers.</summary>
        /// <param name = "cells">The cells.</param>
        /// <param name = "dt">The dt.</param>
        internal static void TickCellTimers(List<AdventureCellTimer> cells, float dt)
        {
            for (var i = cells.Count - 1; i >= 0; i--)
            {
                // Original craters use integer centiseconds; repeated float subtraction extends the 18000-tick lifetime.
                var ticks = Math.Max(0d, Math.Round((double)cells[i].Remaining * 100d) - Math.Round(dt * 100d));
                cells[i].Remaining = (float)(ticks * .01d);
                if (ticks <= 0d)
                {
                    cells.RemoveAt(i);
                }
            }
        }

        /// <summary>Updates the original row countdowns before vehicles extend the ice during their action update.</summary>
        internal void AdvanceAdventureIceTrails()
        {
            for (var index = _adventureModeRunner.Adventure._ice.Count - 1; index >= 0; index--)
            {
                var trail = _adventureModeRunner.Adventure._ice[index];
                trail.RemainingTicks--;
                trail.AgeTicks++;
                if (trail.RemainingTicks <= 0)
                {
                    _adventureModeRunner.Adventure._ice.RemoveAt(index);
                }
            }
        }

        /// <summary>Extends the shared native ice row rather than generating independently expiring cell stamps.</summary>
        /// <param name="zombie">The vehicle advancing its original ice front.</param>
        internal void ExtendAdventureIceTrail(Zombie zombie)
        {
            var x = AdventureZombieNativeRules.NativePositionX(zombie.X, AdventureDancerRules.Scale(zombie));
            var permanent = _world.ModeSession != null && _world.ModeSession.Definition.Mechanic == ModeMechanic.Bobsled;
            foreach (var trail in _adventureModeRunner.Adventure._ice)
            {
                if (trail.Row == zombie.Row)
                {
                    trail.Extend(x, _adventureModeRunner.Adventure.Level.HasRoof, permanent);
                    return;
                }
            }

            if ((int)(x + 118f) >= 800)
            {
                return;
            }

            var created = new AdventureIceTrail { Row = zombie.Row };
            created.Extend(x, _adventureModeRunner.Adventure.Level.HasRoof, permanent);
            if (created.RemainingTicks > 0)
            {
                created.Id = ++_world.Presentation._effectId;
                _adventureModeRunner.Adventure._ice.Add(created);
            }
        }
    }
}
