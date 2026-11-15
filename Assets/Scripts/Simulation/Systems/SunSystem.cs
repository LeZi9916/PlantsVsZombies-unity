#nullable enable

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns SunSystem responsibilities for one deterministic match.</summary>
    internal sealed class SunSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides LegacySimulation operations.</summary>
        private LegacySimulation _legacySimulation = null!;
        /// <summary>Provides LevelOneModeRunner operations.</summary>
        private LevelOneModeRunner _levelOneModeRunner = null!;
        /// <summary>Provides SeedBankSystem operations.</summary>
        private SeedBankSystem _seedBankSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal SunSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="legacySimulation">The LegacySimulation collaborator.</param>
        /// <param name="levelOneModeRunner">The LevelOneModeRunner collaborator.</param>
        /// <param name="seedBankSystem">The SeedBankSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, LegacySimulation legacySimulation, LevelOneModeRunner levelOneModeRunner, SeedBankSystem seedBankSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _legacySimulation = legacySimulation;
            _levelOneModeRunner = levelOneModeRunner;
            _seedBankSystem = seedBankSystem;
        }

        /// <summary>Performs collect sun.</summary>
        /// <param name = "sun">The sun.</param>
        /// <returns>The resulting collect sun.</returns>
        internal bool CollectSun(global::PlantsVsZombies.Simulation.Sun sun)
        {
            bool handled = false, collected = false;
            if (_world.IsAdventure)
            {
                CollectAdventureSunMotion(sun, ref handled, ref collected);
            }

            if (handled)
            {
                return collected;
            }

            if (_world.State != MatchState.Playing || !_world.Suns.Remove(sun))
            {
                return false;
            }

            _legacySimulation.Raise(GameEvent.SunPickupStarted);
            _world.SunAmount = _world.IsAdventure ? Math.Min(9990, _world.SunAmount + sun.Value) : _world.SunAmount + sun.Value;
            if (_world.IsAdventure)
            {
                _adventureModeRunner.CollectAdventureSun();
            }

            if (_world.IsExperimentalLevelOne)
            {
                _levelOneModeRunner.OnExperimentalLevelOneSunCollected();
            }

            _legacySimulation.Raise(GameEvent.SunCollected);
            _world._sunPool.Return(sun);
            return true;
        }

        /// <summary>Takes a sun with clean pickup, motion, and lifetime state.</summary>
        /// <param name="x">The initial board x coordinate.</param>
        /// <param name="y">The initial board y coordinate.</param>
        /// <param name="targetY">The landing y coordinate.</param>
        /// <param name="value">The credited sunlight value.</param>
        /// <returns>A reusable sun initialized for its next appearance.</returns>
        internal global::PlantsVsZombies.Simulation.Sun CreateSun(float x, float y, float targetY, int value = 25)
        {
            var sun = _world._sunPool.Rent();
            sun.Reset();
            sun.X = x;
            sun.Y = y;
            sun.TargetY = targetY;
            sun.Value = value;
            return sun;
        }

        /// <summary>Retires a sun after its collection or disappearance finishes.</summary>
        /// <param name="index">The index of the active sun to retire.</param>
        internal void RemoveSunAt(int index)
        {
            var sun = _world.Suns[index];
            _world.Suns.RemoveAt(index);
            _world._sunPool.Return(sun);
        }

        /// <summary>Returns all active suns before a mode or stage clears its board.</summary>
        internal void ClearSuns()
        {
            for (var i = _world.Suns.Count - 1; i >= 0; i--)
            {
                RemoveSunAt(i);
            }
        }

        /// <summary>Performs update suns.</summary>
        /// <param name = "dt">The dt.</param>
        internal void UpdateSuns(float dt)
        {
            var handled = false;
            if (_world.IsAdventure)
            {
                AdvanceAdventureSunMotion(dt, ref handled);
            }

            if (handled)
            {
                return;
            }

            for (var i = _world.Suns.Count - 1; i >= 0; i--)
            {
                var sun = _world.Suns[i];
                sun.Age += dt;
                sun.Y = Math.Min(sun.TargetY, sun.Y + dt * 0.75f);
                if (sun.Age > 14f)
                {
                    RemoveSunAt(i);
                }
            }
        }

        /// <summary>Performs create adventure presentation sun.</summary>
        /// <param name = "producer">The producer.</param>
        /// <param name = "value">The value.</param>
        /// <returns>The resulting create adventure presentation sun.</returns>
        internal global::PlantsVsZombies.Simulation.Sun CreateAdventurePresentationSun(Plant? producer, int value = 25)
        {
            var sun = CreateSun(0f, 0f, 0f, value);
            AdventurePresentationSunSpawn(sun, producer);
            return sun;
        }

        /// <summary>Performs adventure presentation sun spawn.</summary>
        /// <param name = "sun">The sun.</param>
        /// <param name = "producer">The producer.</param>
        internal void AdventurePresentationSunSpawn(global::PlantsVsZombies.Simulation.Sun sun, Plant? producer)
        {
            var pitch = _adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f;
            if (producer != null)
            {
                float nativeX = 40 + producer.Column * 80 + producer.PositionOffsetX;
                var nativeY = 80 + producer.Row * pitch + producer.PositionOffsetY + (_adventureModeRunner.Adventure.Level.HasRoof ? Math.Max(0, 5 - producer.Column) * 20 - 10 : 0);
                InitializeAdventurePresentationPlantSunAtPoint(sun, nativeX, nativeY);
                return;
            }

            var kind = sun.Value == 15 ? global::PlantsVsZombies.Sun.SunKind.Small : sun.Value == 50 ? global::PlantsVsZombies.Sun.SunKind.Large : global::PlantsVsZombies.Sun.SunKind.Normal;
            float x = 100 + _world._random.Next(550), y = 60;
            sun.PresentationMotion = global::PlantsVsZombies.Adventure.AdventureSunMotion.Sky;
            sun.PresentationFromPlant = false;
            sun.PresentationVelocityX = 0;
            sun.PresentationLandingY = 300 + _world._random.Next(250);
            sun.PresentationVelocityY = .67f;
            sun.PresentationNative = true;
            sun.PresentationScale = kind == global::PlantsVsZombies.Sun.SunKind.Small ? .5f : kind == global::PlantsVsZombies.Sun.SunKind.Large ? 2f : 1f;
            sun.PresentationPickup = new global::PlantsVsZombies.Sun.SunPickupState(kind, x, y);
            sun.PresentationPreviousX = x;
            sun.PresentationPreviousY = y;
            sun.X = (x + 30 - 40) / 80f;
            sun.Y = (y + 30 - 80) / pitch;
        }

        // Inputs are original Coin top-left pixel coordinates, not the reanim centre.
        /// <summary>Performs create adventure presentation sun from point.</summary>
        /// <param name = "nativeX">The native x.</param>
        /// <param name = "nativeY">The native y.</param>
        /// <param name = "value">The value.</param>
        /// <returns>The resulting create adventure presentation sun from point.</returns>
        internal global::PlantsVsZombies.Simulation.Sun CreateAdventurePresentationSunFromPoint(float nativeX, float nativeY, int value = 25)
        {
            var sun = CreateSun(0f, 0f, 0f, value);
            InitializeAdventurePresentationPlantSunAtPoint(sun, nativeX, nativeY);
            return sun;
        }

        /// <summary>Performs initialize adventure presentation plant sun at point.</summary>
        /// <param name = "sun">The sun.</param>
        /// <param name = "nativeX">The native x.</param>
        /// <param name = "nativeY">The native y.</param>
        internal void InitializeAdventurePresentationPlantSunAtPoint(global::PlantsVsZombies.Simulation.Sun sun, float nativeX, float nativeY)
        {
            var pitch = _adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f;
            var kind = sun.Value == 15 ? global::PlantsVsZombies.Sun.SunKind.Small : sun.Value == 50 ? global::PlantsVsZombies.Sun.SunKind.Large : global::PlantsVsZombies.Sun.SunKind.Normal;
            sun.PresentationMotion = global::PlantsVsZombies.Adventure.AdventureSunMotion.Plant;
            sun.PresentationFromPlant = true;
            sun.PresentationVelocityY = -1.7f - (float)_world._random.NextDouble() * 1.7f;
            sun.PresentationVelocityX = -.4f + (float)_world._random.NextDouble() * .8f;
            sun.PresentationLandingY = (int)(nativeY + 15 + _world._random.Next(20));
            sun.PresentationScale = .4f * (kind == global::PlantsVsZombies.Sun.SunKind.Small ? .5f : kind == global::PlantsVsZombies.Sun.SunKind.Large ? 2f : 1f);
            sun.PresentationPickup = new global::PlantsVsZombies.Sun.SunPickupState(kind, nativeX, nativeY);
            sun.PresentationPreviousX = nativeX;
            sun.PresentationPreviousY = nativeY;
            sun.X = (nativeX + 30 - 40) / 80f;
            sun.Y = (nativeY + 30 - 80) / pitch;
            sun.PresentationNative = true;
        }

        // Existing mode drops use board-space centres. Only the explicit sky factory may
        // randomize their birth position. PC 0x43BE43: motion 3, -3-R(2), R(1)-.5,
        // birthY+45+R(20), clamped to 80..521; 0x43C1F0 applies gravity .15.
        /// <summary>Performs initialize adventure presentation dropped sun.</summary>
        /// <param name = "sun">The sun.</param>
        internal void InitializeAdventurePresentationDroppedSun(global::PlantsVsZombies.Simulation.Sun sun)
        {
            if (sun == null || sun.PresentationNative)
            {
                return;
            }

            var pitch = _adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f;
            float x = 40f + sun.X * 80f - 30f, y = 80f + sun.Y * pitch - 30f;
            var kind = sun.Value == 15 ? global::PlantsVsZombies.Sun.SunKind.Small : sun.Value == 50 ? global::PlantsVsZombies.Sun.SunKind.Large : global::PlantsVsZombies.Sun.SunKind.Normal;
            sun.PresentationMotion = global::PlantsVsZombies.Adventure.AdventureSunMotion.Coin;
            sun.PresentationFromPlant = false;
            sun.PresentationVelocityY = -3f - (float)_world._random.NextDouble() * 2f;
            sun.PresentationVelocityX = (float)_world._random.NextDouble() - .5f;
            sun.PresentationLandingY = Math.Max(80, Math.Min(521, (int)(y + 45f + _world._random.Next(20))));
            sun.PresentationScale = kind == global::PlantsVsZombies.Sun.SunKind.Small ? .5f : kind == global::PlantsVsZombies.Sun.SunKind.Large ? 2f : 1f;
            sun.PresentationPickup = new global::PlantsVsZombies.Sun.SunPickupState(kind, x, y);
            sun.PresentationPreviousX = x;
            sun.PresentationPreviousY = y;
            sun.PresentationNative = true;
        }

        /// <summary>Performs create adventure presentation dropped sun.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "row">The row.</param>
        /// <param name = "value">The value.</param>
        /// <returns>The resulting create adventure presentation dropped sun.</returns>
        internal global::PlantsVsZombies.Simulation.Sun CreateAdventurePresentationDroppedSun(float x, float row, int value = 25)
        {
            global::PlantsVsZombies.Simulation.Sun sun = CreateSun(x, row, row, value);
            InitializeAdventurePresentationDroppedSun(sun);
            return sun;
        }

        /// <summary>Performs try collect adventure presentation sun.</summary>
        /// <param name = "sun">The sun.</param>
        /// <returns>The resulting try collect adventure presentation sun.</returns>
        internal bool TryCollectAdventurePresentationSun(global::PlantsVsZombies.Simulation.Sun sun)
        {
            if (!_world.IsAdventure || _world.State != MatchState.Playing || !_world.Suns.Contains(sun))
            {
                return false;
            }

            if (!sun.PresentationNative)
            {
                InitializeAdventurePresentationDroppedSun(sun);
            }

            global::PlantsVsZombies.Sun.SunPickupState clicked;
            if (!global::PlantsVsZombies.Sun.SunPickupRules.TryClick(sun.PresentationPickup, (int)sun.PresentationPickup.X + 30, (int)sun.PresentationPickup.Y + 30, 0, true, false, sun.PresentationSpecialExpansion, out clicked))
            {
                return false;
            }

            sun.PresentationPickup = clicked;
            // PC Coin::MouseDown 0x43D6E0 plays the sun foley before collection flight.
            _legacySimulation.Raise(GameEvent.SunPickupStarted);
            _seedBankSystem.AdventureSunSelectedForPacketFeedback(sun);
            return true;
        }

        /// <summary>Performs advance adventure presentation suns.</summary>
        /// <param name = "dt">The dt.</param>
        internal void AdvanceAdventurePresentationSuns(float dt)
        {
            var pitch = _adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f;
            for (var i = _world.Suns.Count - 1; i >= 0; i--)
            {
                var sun = _world.Suns[i];
                if (!sun.PresentationNative)
                {
                    InitializeAdventurePresentationDroppedSun(sun);
                }

                sun.Age += dt;
                var pickup = sun.PresentationPickup;
                sun.PresentationPreviousX = pickup.X;
                sun.PresentationPreviousY = pickup.Y;
                var nativeScale = pickup.Kind == global::PlantsVsZombies.Sun.SunKind.Small ? .5f : pickup.Kind == global::PlantsVsZombies.Sun.SunKind.Large ? 2f : 1f;
                var ground = false;
                if (!pickup.Collecting && pickup.FadeCountdown == 0 && !pickup.Dead)
                {
                    float x = pickup.X, y = pickup.Y + sun.PresentationVelocityY;
                    ground = y >= sun.PresentationLandingY;
                    if (ground)
                    {
                        y = sun.PresentationLandingY;
                        x = (float)(x >= 0 ? Math.Floor(x + .5f) : Math.Ceiling(x - .5f));
                    }
                    else
                    {
                        if (sun.PresentationMotion == global::PlantsVsZombies.Adventure.AdventureSunMotion.Plant)
                        {
                            sun.PresentationVelocityY += .09f;
                        }
                        else if (sun.PresentationMotion == global::PlantsVsZombies.Adventure.AdventureSunMotion.Coin)
                        {
                            sun.PresentationVelocityY += .15f;
                        }

                        x += sun.PresentationVelocityX;
                        if (x > 740)
                        {
                            x = 740;
                            sun.PresentationVelocityX = -.4f - (float)_world._random.NextDouble() * .4f;
                        }
                        else if (x < 0)
                        {
                            x = 0;
                            sun.PresentationVelocityX = .4f + (float)_world._random.NextDouble() * .4f;
                        }
                    }

                    pickup = pickup.WithPosition(x, y);
                    if (sun.PresentationFromPlant)
                    {
                        sun.PresentationScale = sun.PresentationScale < nativeScale ? sun.PresentationScale + .02f : nativeScale;
                    }
                }

                if (pickup.Collecting)
                {
                    float dx = pickup.X - 15f, dy = pickup.Y;
                    sun.PresentationScale = Math.Max(.5f, Math.Min(1f, (float)Math.Sqrt(dx * dx + dy * dy) * .05f)) * nativeScale;
                }

                var mode = _world.ModeSession == null ? 0 : _world.ModeSession.Definition.Id;
                var tick = global::PlantsVsZombies.Sun.SunPickupRules.Tick(pickup, _world.SunAmount, true, ground, true, false, mode);
                sun.PresentationPickup = tick.State;
                sun.X = (tick.State.X + 30 - 40) / 80f;
                sun.Y = (tick.State.Y + 30 - 80) / pitch;
                if (tick.Credited)
                {
                    _world.SunAmount = tick.Balance;
                    _adventureModeRunner.CollectAdventureSun();
                    _legacySimulation.Raise(GameEvent.SunCollected);
                }

                if (tick.State.Dead)
                {
                    RemoveSunAt(i);
                }
            }
        }

        /// <summary>Performs advance adventure sun motion.</summary>
        /// <param name = "dt">The dt.</param>
        /// <param name = "handled">The handled.</param>
        internal void AdvanceAdventureSunMotion(float dt, ref bool handled)
        {
            handled = true;
            AdvanceAdventurePresentationSuns(dt);
        }

        /// <summary>Performs collect adventure sun motion.</summary>
        /// <param name = "sun">The sun.</param>
        /// <param name = "handled">The handled.</param>
        /// <param name = "collected">The collected.</param>
        internal void CollectAdventureSunMotion(global::PlantsVsZombies.Simulation.Sun sun, ref bool handled, ref bool collected)
        {
            handled = true;
            collected = TryCollectAdventurePresentationSun(sun);
        }
    }
}
