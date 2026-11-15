#nullable enable
using PlantsVsZombies.Adventure;

using PlantsVsZombies.Modes;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns PortalMode responsibilities for one deterministic match.</summary>
    internal sealed class PortalMode
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides IndependentModeRunner operations.</summary>
        private IndependentModeRunner _independentModeRunner = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal PortalMode(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="independentModeRunner">The IndependentModeRunner collaborator.</param>
        internal void Connect(IndependentModeRunner independentModeRunner)
        {
            _independentModeRunner = independentModeRunner;
        }

        /// <summary>Performs initialize mode portals.</summary>
        internal void InitializeModePortals()
        {
            var s = _independentModeRunner.RequiredModeSession;
            s.Portals.Clear();
            s.Portals.Add(new ModePortal { Column = 2, Row = 0, Pair = 0 });
            s.Portals.Add(new ModePortal { Column = 9, Row = 1, Pair = 0 });
            s.Portals.Add(new ModePortal { Column = 9, Row = 3, Pair = 1 });
            s.Portals.Add(new ModePortal { Column = 2, Row = 4, Pair = 1 });
            s._portalTimer = 90f;
        }

        /// <summary>Performs tick mode portals.</summary>
        /// <param name = "dt">The dt.</param>
        internal void TickModePortals(float dt)
        {
            var s = _independentModeRunner.RequiredModeSession;
            s._portalTimer -= dt;
            if (s._portalTimer <= 0)
            {
                var portal = s.Portals[_world._random.Next(s.Portals.Count)];
                using var cellsLease = _world.IntegerScratch.Rent();
                var cells = cellsLease.Items;
                for (var r = 0; r < 5; r++)
                {
                    for (var c = 0; c < 10; c++)
                    {
                        if (!s.Portals.Exists(p => p != portal && p.Row == r) && !_world.Plants.Exists(p => p.Row == r && p.Column == c))
                        {
                            cells.Add(r * 10 + c);
                        }
                    }
                }

                if (cells.Count > 0)
                {
                    var cell = cells[_world._random.Next(cells.Count)];
                    portal.Row = cell / 10;
                    portal.Column = cell % 10;
                }

                s._portalTimer = 60f;
            }

            foreach (var z in _world.Zombies)
            {
                z._modePortalCooldown = Math.Max(0, z._modePortalCooldown - dt);
                if (z._modePortalCooldown > 0)
                {
                    continue;
                }

                foreach (var entrance in s.Portals)
                {
                    if (z.Row == entrance.Row && Math.Abs(z.X - (entrance.Column + 0.5f)) < 0.12f)
                    {
                        var exit = s.Portals.Find(p => p != entrance && p.Pair == entrance.Pair);
                        if (exit != null)
                        {
                            z.Row = exit.Row;
                            z.X = exit.Column + (z.Hypnotized || z.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(z) ? 0.65f : 0.35f);
                            z._modePortalCooldown = 0f;
                        }

                        break;
                    }
                }
            }

            foreach (var pea in _world.Peas)
            {
                foreach (var entrance in s.Portals)
                {
                    if (pea.Row == entrance.Row && Math.Abs(pea.X - (entrance.Column + 0.5f)) < 0.055f)
                    {
                        var exit = s.Portals.Find(p => p != entrance && p.Pair == entrance.Pair);
                        if (exit != null)
                        {
                            pea.Row = exit.Row;
                            pea.FlightRow = exit.Row;
                            pea.X = exit.Column + 0.65f;
                        }

                        break;
                    }
                }
            }
        }
    }
}
