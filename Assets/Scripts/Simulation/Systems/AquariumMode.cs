#nullable enable
using PlantsVsZombies.Adventure;

using PlantsVsZombies.Modes;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns AquariumMode responsibilities for one deterministic match.</summary>
    internal sealed class AquariumMode
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides IndependentModeRunner operations.</summary>
        private IndependentModeRunner _independentModeRunner = null!;
        /// <summary>Provides LegacySimulation operations.</summary>
        private LegacySimulation _legacySimulation = null!;
        /// <summary>Provides SunSystem operations.</summary>
        private SunSystem _sunSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal AquariumMode(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="independentModeRunner">The IndependentModeRunner collaborator.</param>
        /// <param name="legacySimulation">The LegacySimulation collaborator.</param>
        /// <param name="sunSystem">The SunSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, IndependentModeRunner independentModeRunner, LegacySimulation legacySimulation, SunSystem sunSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _independentModeRunner = independentModeRunner;
            _legacySimulation = legacySimulation;
            _sunSystem = sunSystem;
        }

        /// <summary>Performs add mode fish.</summary>
        internal void AddModeFish()
        {
            var s = _independentModeRunner.RequiredModeSession;
            s.Fish.Add(new ModeFish {
                X = 100f + (float)_world._random.NextDouble() * 600f,
                Y = 140f + (float)_world._random.NextDouble() * 300f,
                Angle = (float)_world._random.NextDouble() * (float)Math.PI * 2,
                SunTimer = 10f + 5f * (float)_world._random.NextDouble()
            });
        }

        /// <summary>Performs buy mode fish.</summary>
        /// <returns>The resulting buy mode fish.</returns>
        internal bool BuyModeFish()
        {
            var s = _world.ModeSession;
            if (s == null || s.Definition.Mechanic != ModeMechanic.Aquarium || _world.State != MatchState.Playing || _world.SunAmount < 100 || s.Fish.Count > 100)
            {
                return false;
            }

            _world.SunAmount -= 100;
            AddModeFish();
            return true;
        }

        /// <summary>Performs feed mode fish.</summary>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <returns>The resulting feed mode fish.</returns>
        internal bool FeedModeFish(float x, float y)
        {
            var s = _world.ModeSession;
            if (s == null || s.Definition.Mechanic != ModeMechanic.Aquarium || _world.State != MatchState.Playing || _world.SunAmount < 5 || s.Food.Count >= 3 || x < 80 || x > 720 || y < 90 || y > 430)
            {
                return false;
            }

            _world.SunAmount -= 5;
            s.Food.Add(new ModeFood { X = x, Y = y });
            return true;
        }

        /// <summary>Performs buy mode aquarium trophy.</summary>
        /// <returns>The resulting buy mode aquarium trophy.</returns>
        internal bool BuyModeAquariumTrophy()
        {
            var s = _world.ModeSession;
            if (s == null || s.Definition.Mechanic != ModeMechanic.Aquarium || _world.State != MatchState.Playing || _world.SunAmount < 1000)
            {
                return false;
            }

            _world.SunAmount -= 1000;
            _independentModeRunner.ClearModeStage();
            return true;
        }

        /// <summary>Performs tick mode aquarium.</summary>
        /// <param name = "dt">The dt.</param>
        internal void TickModeAquarium(float dt)
        {
            var s = _independentModeRunner.RequiredModeSession;
            foreach (var food in new PooledSnapshot<ModeFood>(s.Food))
            {
                food.Age += dt;
                food.Y += 15f * dt;
                if (food.Y >= 500)
                {
                    s.Food.Remove(food);
                }
            }

            foreach (var fish in new PooledSnapshot<ModeFish>(s.Fish))
            {
                fish.Hunger -= dt;
                fish.SunTimer -= dt;
                if (fish.Hunger <= 0)
                {
                    s.Fish.Remove(fish);
                    continue;
                }

                ModeFood? closest = null;
                var dist = float.MaxValue;
                if (fish.Hunger <= 15f)
                {
                    foreach (var food in s.Food)
                    {
                        if (food.Age >= 0.15f)
                        {
                            float dx = food.X - fish.X, dy = food.Y - fish.Y;
                            var d = dx * dx + dy * dy;
                            if (d < dist)
                            {
                                dist = d;
                                closest = food;
                            }
                        }
                    }
                }

                fish.PhaseRemaining = Math.Max(0, fish.PhaseRemaining - dt);
                if (closest != null)
                {
                    if (dist < 2500)
                    {
                        fish.Hunger = Math.Min(27f, fish.Hunger + 20f);
                        s.Food.Remove(closest);
                        fish.SwimPhase = 1;
                        fish.PhaseRemaining = 1f;
                    }
                    else
                    {
                        fish.Angle = (float)Math.Atan2(closest.Y - fish.Y, closest.X - fish.X);
                        fish.SwimPhase = 0;
                    }
                }
                else if (fish.PhaseRemaining <= 0)
                {
                    var choice = _world._random.Next(7);
                    fish.PhaseRemaining = 3f + _world._random.Next(701) * 0.01f;
                    if (choice <= 4)
                    {
                        fish.SwimPhase = 0;
                        fish.Angle = (float)_world._random.NextDouble() * (float)Math.PI * 2;
                    }
                    else
                    {
                        fish.SwimPhase = 2;
                        fish.Angle = choice == 5 ? 0f : (float)Math.PI;
                    }
                }

                float vx = (float)Math.Cos(fish.Angle), vy = (float)Math.Sin(fish.Angle);
                var wall = fish.X < 50 && vx < 0 || fish.X > 730 && vx > 0 || fish.Y < 140 && vy < 0 || fish.Y > 440 && vy > 0;
                var maxSpeed = fish.SwimPhase == 0 ? 50f : fish.SwimPhase == 2 ? 30f : 5f;
                if (wall)
                {
                    maxSpeed = fish.Speed * 0.3f;
                    fish.PhaseRemaining = Math.Min(1f, fish.PhaseRemaining);
                }
                else if (fish.SwimPhase == 2)
                {
                    if (fish.X < 250 && vx < 0)
                    {
                        fish.Angle = 0;
                    }

                    if (fish.X > 600 && vx > 0)
                    {
                        fish.Angle = 3.1416f;
                    }
                }

                fish.Speed = Math.Min(maxSpeed, fish.Speed + 100f * dt);
                fish.X += vx * fish.Speed * dt;
                fish.Y += vy * fish.Speed * dt;
                if (fish.SunTimer <= 0)
                {
                    _world.Suns.Add(_sunSystem.CreateSun((fish.X - 40f) / 80f, (fish.Y - 80f) / (_adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f), (fish.Y - 80f) / (_adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f)));
                    fish.SunTimer = 10f + (float)_world._random.NextDouble() * 5f;
                }
            }

            if (s.Fish.Count == 0)
            {
                _world.State = MatchState.Lost;
                _adventureModeRunner.Adventure.Phase = AdventurePhase.Lost;
                s.Phase = ModePhase.Lost;
                _legacySimulation.Raise(GameEvent.Lost);
            }
        }
    }
}
