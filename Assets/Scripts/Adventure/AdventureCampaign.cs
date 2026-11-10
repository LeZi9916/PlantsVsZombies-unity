#nullable enable
using PlantsVsZombies.Progression;
using PlantsVsZombies.Simulation;
using System;
using System.Collections.Generic;
using System.IO;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Owns a current attempt. Persistent reward commit precedes advancing the board or unlocking the next level.</summary>
    public sealed class AdventureCampaign
    {
        /// <summary>Stores store.</summary>
        private readonly AdventureProgressStore _store;
        /// <summary>Stores accounts.</summary>
        private readonly PlayerAccountStore _accounts;
        /// <summary>Stores seed access.</summary>
        private AdventureSeedAccess _seedAccess;
        /// <summary>Stores packet slots.</summary>
        private int _packetSlots = 6;
        /// <summary>Stores attempt id.</summary>
        private string _attemptId;
        /// <summary>Stores coin save retry utc.</summary>
        private long _coinSaveRetryUtc;
        /// <summary>Gets or sets last persistence error.</summary>
        public string LastPersistenceError { get; private set; }

        /// <summary>Stores roof cleaners.</summary>
        private bool _roofCleaners;
        /// <summary>Stores pool cleaners.</summary>
        private bool _poolCleaners;
        /// <summary>Gets or sets profile id.</summary>
        public Guid ProfileId { get; private set; }
        /// <summary>Gets or sets game.</summary>
        public GameSimulation Game { get; private set; }

        /// <summary>Gets or sets progress.</summary>
        public AdventureProgress Progress
        {
            get
            {
                return _store.Get(ProfileId);
            }
        }

        /// <summary>Initializes adventure campaign.</summary>
        /// <param name = "store">The store.</param>
        /// <param name = "profileId">The profile id.</param>
        /// <param name = "accounts">The accounts.</param>
        /// <exception cref = "ArgumentNullException">The operation cannot satisfy its preconditions.</exception>
        public AdventureCampaign(AdventureProgressStore store, Guid profileId, PlayerAccountStore accounts = null)
        {
            if (store == null)
            {
                throw new ArgumentNullException("store");
            }

            this._store = store;
            this._accounts = accounts;
            ProfileId = profileId;
            store.Get(profileId);
        }

        /// <summary>Performs begin or resume.</summary>
        /// <param name = "roofCleaners">The roof cleaners.</param>
        /// <param name = "poolCleaners">The pool cleaners.</param>
        /// <returns>The resulting begin or resume.</returns>
        public GameSimulation BeginOrResume(bool roofCleaners = false, bool poolCleaners = false)
        {
            var p = Progress;
            this._roofCleaners = roofCleaners;
            this._poolCleaners = poolCleaners;
            var account = _accounts == null ? null : _accounts.Get(ProfileId);
            _seedAccess = account == null ? null : account.SeedAccess(p.ImitaterTarget);
            _packetSlots = account == null ? 6 : account.PacketSlots;
            _attemptId = Guid.NewGuid().ToString("N");
            LastPersistenceError = null;
            Game = new GameSimulation(new GameRules { AdventureLevel = p.NextLevel, AdventureCycle = p.Cycle, AdventureDaveSeed = unchecked(p.AttemptSeed + p.NextLevel + (p.Cycle - 1) * 101 + ProfileId.GetHashCode()), AdventurePacketSlots = _packetSlots, AdventureSeedAccess = _seedAccess, AdventureRoofCleaners = roofCleaners || account != null && account.PurchaseCount(23) > 0, AdventurePoolCleaners = poolCleaners || account != null && account.PurchaseCount(22) > 0, AdventureMinigamesUnlocked = account != null && account.MinigamesUnlocked, AdventurePuzzleUnlocked = account != null && account.PuzzleUnlocked, AdventurePottedLoot = account != null && p.CompletedRuns > 0 && account.Plants.Count < 32, AdventureChocolateLoot = account != null && account.PurchaseCount(20) > 0 && account.PurchaseCount(26) < 10, AdventureLootCommit = _accounts == null ? (Func<AdventureCoin, bool>)null : CommitLoot, AdventureFirstAid = account != null && account.PurchaseCount(29) > 0, AdventurePlaceRake = account != null && account.PurchaseCount(24) > 0 ? (Func<bool>)(() =>
            {
                _accounts.ConsumeRake(ProfileId);
                return true;
            }) : null, AdventureCoinCommit = _accounts == null ? (Func<int, bool>)null : CommitCoins }, p.AttemptSeed);
            if (Game.Adventure.Phase == AdventurePhase.ChoosingSeeds && p.SelectedSeeds.Count > 0)
            {
                Game.ChooseAdventureSeeds(p.SelectedSeeds);
            }

            return Game;
        }

        /// <summary>Performs choose seeds.</summary>
        /// <param name = "seeds">The seeds.</param>
        /// <returns>The resulting choose seeds.</returns>
        public bool ChooseSeeds(IReadOnlyList<PlantKind> seeds)
        {
            if (Game == null || seeds == null)
            {
                return false;
            }

            // Capture the caller's possibly mutable collection before validation or disk IO.
            var selected = new List<PlantKind>(seeds);
            if (!Game.CanChooseAdventureSeeds(selected))
            {
                return false;
            }

            _store.SaveAttempt(ProfileId, Progress.AttemptSeed, selected, _seedAccess, _packetSlots, Game.Adventure.ImitaterTarget);
            return Game.ChooseAdventureSeeds(selected);
        }

        /// <summary>Performs collect reward.</summary>
        /// <returns>The resulting collect reward.</returns>
        public bool CollectReward()
        {
            if (Game == null || Game.Adventure.Phase != AdventurePhase.Reward || Game.State != MatchState.Won)
            {
                return false;
            }

            if (!Game.BankAdventureCoins())
            {
                return false;
            }

            if (_accounts != null)
            {
                var unused = 0;
                foreach (var mower in Game.Mowers)
                {
                    if (mower.Available)
                    {
                        unused++;
                    }
                }

                _accounts.ClaimAdventureReward(ProfileId, Progress.Cycle, Game.Adventure.Level.Number, unused);
            }

            _store.RecordReward(ProfileId, Game.Adventure.Level.Number);
            return Game.CollectAdventureReward();
        }

        /// <summary>Performs commit loot.</summary>
        /// <param name = "coin">The coin.</param>
        /// <returns>The resulting commit loot.</returns>
        private bool CommitLoot(AdventureCoin coin)
        {
            try
            {
                if (coin.Kind == AdventureCoinKind.MinigameUnlock || coin.Kind == AdventureCoinKind.PuzzleUnlock)
                {
                    _accounts.CollectModeUnlock(ProfileId, _attemptId + ":" + coin.Id, coin.Kind == AdventureCoinKind.PuzzleUnlock);
                }
                else
                {
                    _accounts.CollectBattleLoot(ProfileId, _attemptId + ":" + coin.Id, coin.Kind == AdventureCoinKind.Chocolate, coin.GiftPlant ?? PlantKind.Peashooter, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                }

                LastPersistenceError = null;
                return true;
            }
            catch (IOException error)
            {
                LastPersistenceError = error.Message;
                _coinSaveRetryUtc = DateTime.UtcNow.Ticks + TimeSpan.TicksPerSecond;
                return false;
            }
            catch (InvalidOperationException error)
            {
                LastPersistenceError = error.Message;
                return false;
            }
        }

        /// <summary>Performs commit coins.</summary>
        /// <param name = "total">The total.</param>
        /// <returns>The resulting commit coins.</returns>
        private bool CommitCoins(int total)
        {
            if (LastPersistenceError != null && DateTime.UtcNow.Ticks < _coinSaveRetryUtc)
            {
                return false;
            }

            try
            {
                _accounts.CreditBattleCoins(ProfileId, _attemptId, total);
                LastPersistenceError = null;
                return true;
            }
            catch (IOException error)
            {
                LastPersistenceError = error.Message;
                return false;
            }
            catch (UnauthorizedAccessException error)
            {
                LastPersistenceError = error.Message;
                _coinSaveRetryUtc = DateTime.UtcNow.Ticks + TimeSpan.TicksPerSecond;
                return false;
            }
        }

        /// <summary>Performs start next level.</summary>
        /// <returns>The resulting start next level.</returns>
        public bool StartNextLevel()
        {
            if (Game == null || Game.Adventure.Phase != AdventurePhase.Finished || Progress.Completed)
            {
                return false;
            }

            BeginOrResume(_roofCleaners, _poolCleaners);
            return true;
        }
    }
}
