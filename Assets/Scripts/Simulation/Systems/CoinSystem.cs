#nullable enable
using PlantsVsZombies.Adventure;

using System;

namespace PlantsVsZombies.Simulation
{
    /// <summary>Owns CoinSystem responsibilities for one deterministic match.</summary>
    internal sealed class CoinSystem
    {
        /// <summary>Owns the state shared by this match.</summary>
        private readonly SimulationWorld _world;
        /// <summary>Provides AdventureModeRunner operations.</summary>
        private AdventureModeRunner _adventureModeRunner = null!;
        /// <summary>Provides BattlePresentationSystem operations.</summary>
        private BattlePresentationSystem _battlePresentationSystem = null!;
        /// <summary>Provides BoardQueries operations.</summary>
        private BoardQueries _boardQueries = null!;
        /// <summary>Provides BossSystem operations.</summary>
        private BossSystem _bossSystem = null!;
        /// <summary>Provides CombatSystem operations.</summary>
        private CombatSystem _combatSystem = null!;
        /// <summary>Provides LegacySimulation operations.</summary>
        private LegacySimulation _legacySimulation = null!;
        /// <summary>Provides ZombieFeedbackSystem operations.</summary>
        private ZombieFeedbackSystem _zombieFeedbackSystem = null!;
        /// <summary>Creates the match collaborator without advancing any simulation state.</summary>
        /// <param name="world">The state owned by this match.</param>
        internal CoinSystem(SimulationWorld world)
        {
            _world = world;
        }
        /// <summary>Connects the explicitly declared collaborators before match initialization.</summary>
        /// <param name="adventureModeRunner">The AdventureModeRunner collaborator.</param>
        /// <param name="battlePresentationSystem">The BattlePresentationSystem collaborator.</param>
        /// <param name="boardQueries">The BoardQueries collaborator.</param>
        /// <param name="bossSystem">The BossSystem collaborator.</param>
        /// <param name="combatSystem">The CombatSystem collaborator.</param>
        /// <param name="legacySimulation">The LegacySimulation collaborator.</param>
        /// <param name="zombieFeedbackSystem">The ZombieFeedbackSystem collaborator.</param>
        internal void Connect(AdventureModeRunner adventureModeRunner, BattlePresentationSystem battlePresentationSystem, BoardQueries boardQueries, BossSystem bossSystem, CombatSystem combatSystem, LegacySimulation legacySimulation, ZombieFeedbackSystem zombieFeedbackSystem)
        {
            _adventureModeRunner = adventureModeRunner;
            _battlePresentationSystem = battlePresentationSystem;
            _boardQueries = boardQueries;
            _bossSystem = bossSystem;
            _combatSystem = combatSystem;
            _legacySimulation = legacySimulation;
            _zombieFeedbackSystem = zombieFeedbackSystem;
        }

        /// <summary>Creates a board pickup using the original currency units and logical pixel position.</summary>
        /// <param name="kind">The currency or loot type to create.</param>
        /// <param name="x">The pickup's horizontal lawn coordinate.</param>
        /// <param name="row">The pickup's lawn row.</param>
        /// <returns>The new pickup, or null when the board or coordinates cannot accept it.</returns>
        internal AdventureCoin? SpawnAdventureCoin(AdventureCoinKind kind, float x, float row)
        {
            if (!_world.IsAdventure || _world.State != MatchState.Playing || _adventureModeRunner.Adventure.Phase != AdventurePhase.Playing || !Enum.IsDefined(typeof(AdventureCoinKind), kind) || float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(row) || float.IsInfinity(row) || x < 0 || x >= SimulationWorld.Columns || row < 0 || row >= _world.RowCount)
            {
                return null;
            }

            var coin = _world.LifetimePool.Rent<AdventureCoin>();
            coin.Id = ++_adventureModeRunner.Adventure._coinId;
            coin.Kind = kind;
            coin.X = x;
            coin.Row = row;
            var pitch = _adventureModeRunner.Adventure.Level.HasPool || _adventureModeRunner.Adventure.Level.HasRoof ? 85f : 100f;
            var slope = _adventureModeRunner.Adventure.Level.HasRoof ? Math.Max(0f, 5f - x) * 20f - 10f : 0f;
            coin.PresentationX = coin.PresentationPreviousX = 40f + x * 80f - 20f;
            coin.PresentationY = coin.PresentationPreviousY = 80f + (int)row * pitch + slope + 36f;
            if (coin.ValueUnits > 0)
            {
                if (kind == AdventureCoinKind.Silver || kind == AdventureCoinKind.Gold)
                {
                    coin.PresentationX -= 10f;
                    coin.PresentationY -= 8f;
                    // Original silver/gold reanimations have 21 frames at 30 fps.
                    coin.AnimationOffset = (float)_world._random.NextDouble() * .99f * 21f / 30f;
                    coin.AnimationRate = .6f + (float)_world._random.NextDouble() * .4f;
                }

                coin.FallVelocityY = -3f - (float)_world._random.NextDouble() * 2f;
                coin.FallVelocityX = -.5f + (float)_world._random.NextDouble();
                coin.GroundY = Math.Max(80, Math.Min(521, (int)coin.PresentationY + 45 + _world._random.Next(20)));
                coin.PresentationPreviousX = coin.PresentationX;
                coin.PresentationPreviousY = coin.PresentationY;
            }

            _adventureModeRunner.Adventure._coins.Add(coin);
            _adventureModeRunner.Adventure.Notice(AdventureNoticeKind.CoinCreated, coin.Id, (int)row, (int)x);
            return coin;
        }

        /// <summary>Calls <see cref="SpawnAdventureCoin"/> where a result must exist, failing with a descriptive exception instead of returning null.</summary>
        /// <param name="kind">The <c>kind</c> argument forwarded to <see cref="SpawnAdventureCoin"/>.</param>
        /// <param name="x">The <c>x</c> argument forwarded to <see cref="SpawnAdventureCoin"/>.</param>
        /// <param name="row">The <c>row</c> argument forwarded to <see cref="SpawnAdventureCoin"/>.</param>
        /// <returns>The non-null spawn adventure coin result.</returns>
        /// <exception cref="System.InvalidOperationException">The method returned no result.</exception>
        internal AdventureCoin RequiredSpawnAdventureCoin(AdventureCoinKind kind, float x, float row) => SpawnAdventureCoin(kind, x, row) ?? throw new System.InvalidOperationException("'SpawnAdventureCoin' returned no result.");

        /// <summary>Starts the original manual money collection flight or commits noncurrency loot.</summary>
        /// <param name="coin">The live pickup clicked by the player.</param>
        /// <returns>Whether collection began; currency is persisted and credited when its flight arrives.</returns>
        internal bool CollectAdventureCoin(AdventureCoin? coin)
        {
            if (!_world.IsAdventure || _world.State != MatchState.Playing || _adventureModeRunner.Adventure.Phase != AdventurePhase.Playing || coin == null || coin.IsBeingCollected || !_adventureModeRunner.Adventure._coins.Contains(coin))
            {
                return false;
            }

            if (coin.ValueUnits == 0)
            {
                return FinishAdventureCoin(coin);
            }

            coin.IsBeingCollected = true;
            coin.CollectorPlantId = 0;
            coin.PersistencePending = false;
            _zombieFeedbackSystem.RequestAdventureZombieFoley(coin.Kind == AdventureCoinKind.Diamond ? -8 : 31);
            _adventureModeRunner.Adventure.Notice(AdventureNoticeKind.CoinCollectionStarted, (int)coin.Kind);
            return true;
        }

        /// <summary>Performs finish adventure coin.</summary>
        /// <param name = "coin">The coin.</param>
        /// <returns>The resulting finish adventure coin.</returns>
        internal bool FinishAdventureCoin(AdventureCoin coin)
        {
            if (!_adventureModeRunner.Adventure._coins.Contains(coin))
            {
                return false;
            }

            if (coin.ValueUnits == 0 && (_world.Rules.AdventureLootCommit == null || !_world.Rules.AdventureLootCommit(coin)))
            {
                coin.PersistencePending = true;
                _adventureModeRunner.Adventure.Notice(AdventureNoticeKind.CoinCollectionFailed, coin.Id);
                return false;
            }

            var total = (int)Math.Min(int.MaxValue, (long)_adventureModeRunner.Adventure.MoneyCollectedUnits + coin.ValueUnits);
            if (_world.Rules.AdventureCoinCommit != null && !_world.Rules.AdventureCoinCommit(total))
            {
                coin.PersistencePending = true;
                coin.IsBeingCollected = false;
                coin.CollectorPlantId = 0;
                _adventureModeRunner.Adventure.Notice(AdventureNoticeKind.CoinCollectionFailed, coin.Id);
                return false;
            }

            _adventureModeRunner.Adventure._coins.Remove(coin);
            _adventureModeRunner.Adventure.MoneyCollectedUnits = (int)Math.Min(int.MaxValue, (long)_adventureModeRunner.Adventure.MoneyCollectedUnits + coin.ValueUnits);
            _adventureModeRunner.Adventure.Notice(AdventureNoticeKind.CoinCollected, coin.ValueUnits);
            return true;
        }

        /// <summary>Advances existing money pickups through combat and the visible reward sequence.</summary>
        /// <param name="dt">The duration of one native simulation tick in seconds.</param>
        internal void AdvanceAdventureCoins(float dt)
        {
            for (var i = _adventureModeRunner.Adventure._coins.Count - 1; i >= 0; i--)
            {
                var coin = _adventureModeRunner.Adventure._coins[i];
                coin.Age += dt;
                if (coin.IsBeingCollected)
                {
                    if (coin.CollectorPlantId == 0)
                    {
                        if (coin.TickCollection())
                        {
                            FinishAdventureCoin(coin);
                        }

                        continue;
                    }

                    var collector = _boardQueries.FindPlantById(coin.CollectorPlantId);
                    if (collector == null || collector.Kind != PlantKind.GoldMagnet)
                    {
                        coin.IsBeingCollected = false;
                        coin.CollectorPlantId = 0;
                        continue;
                    }

                    coin._collectionRemaining -= dt;
                    if (coin._collectionRemaining <= 0.000001f)
                    {
                        if (FinishAdventureCoin(coin))
                        {
                            _zombieFeedbackSystem.RequestAdventureZombieFoley(31);
                        }
                    }
                }
                else
                {
                    coin.TickFall();
                    if (coin.PresentationX > 740f)
                    {
                        coin.PresentationX = 740f;
                        coin.FallVelocityX = -.4f - (float)_world._random.NextDouble() * .4f;
                    }
                    else if (coin.PresentationX < 0f)
                    {
                        coin.PresentationX = 0f;
                        coin.FallVelocityX = .4f + (float)_world._random.NextDouble() * .4f;
                    }

                    if (coin.Age >= 30f && !coin.PersistencePending)
                    {
                        _adventureModeRunner.Adventure._coins.RemoveAt(i);
                    }
                }
            }
        }

        /// <summary>Performs advance adventure upgrade world.</summary>
        /// <param name = "dt">The dt.</param>
        internal void AdvanceAdventureUpgradeWorld(float dt)
        {
            if (_adventureModeRunner.Adventure.SelectedCobCannonId.HasValue && _boardQueries.FindPlantById(_adventureModeRunner.Adventure.SelectedCobCannonId.Value) == null)
            {
                _adventureModeRunner.Adventure.SelectedCobCannonId = null;
            }

            _adventureModeRunner.Adventure.CobCannonAimDelayTicks = Math.Max(0,
                _adventureModeRunner.Adventure.CobCannonAimDelayTicks - (int)Math.Round(dt * 100f));
            AdvanceAdventureCoins(dt);

            for (var i = _adventureModeRunner.Adventure._cobShells.Count - 1; i >= 0; i--)
            {
                var shell = _adventureModeRunner.Adventure._cobShells[i];
                shell.FlightRemaining = Math.Max(0, (int)Math.Round(shell.FlightRemaining * 100f) - (int)Math.Round(dt * 100f)) * .01f;
                var row = (int)Math.Round(shell.TargetRow);
                var contact = false;
                // PC 47C880 admits dry-row body contacts at z > -60 (tick 169), then ground at z > -40 (172).
                // Water adds forty to the first threshold, so both gates open at tick 174 there.
                if (shell.FlightRemaining <= .030001f && !_adventureModeRunner.Adventure.Level.IsWaterRow(row))
                {
                    // PC 47BB30 gives the cob its 162-pixel image width; 47DFF0 expands around mX + 81.
                    var left = (int)(40f + shell.TargetX * 80f) - 114;
                    foreach (var zombie in _world.Zombies)
                    {
                        if ((zombie.Row == row || zombie.Kind == ZombieKind.Boss) && _combatSystem.CanTakeAdventureAshDamage(zombie, 0x7F))
                        {
                            var body = AdventureZombieTargetingRules.BodyRectangle(zombie);
                            if (Math.Min(left + 230, body.X + body.Width) >= Math.Max(left, body.X))
                            {
                                contact = true;
                                break;
                            }
                        }
                    }
                }
                if (shell.FlightRemaining > 0.000001f && !contact)
                {
                    continue;
                }

                _combatSystem.BlastAdventureRadius((int)(40f + shell.TargetX * 80f), (int)(shell.TargetNativeY + 40f), row, 115, 1, 0x7F);
                _battlePresentationSystem.AdventurePresentationCobImpact(shell.TargetX, shell.TargetRow);
                var impact = _world.Presentation.Impacts[_world.Presentation.Impacts.Count - 1];
                impact.NativeIcePoint = true;
                impact.NativeParticleX = 40f + shell.TargetX * 80f;
                impact.NativeParticleY = shell.TargetNativeY + 40f;
                if (shell.TargetX >= 7.5f && Math.Abs(shell.TargetRow - 2f) <= 1f)
                {
                    _bossSystem.DamageAdventureBossWithFlags(1800f, 0x7FU);
                }

                _adventureModeRunner.Adventure._cobShells.RemoveAt(i);
                // PC 0x47D410 uses the same direct Doom-shroom sample for the cob blast.
                _legacySimulation.Raise(GameEvent.DoomExplosion);
                _adventureModeRunner.Adventure.Notice(AdventureNoticeKind.CobImpact, shell.SourcePlantId, row, (int)shell.TargetX);
            }
        }
    }
}
