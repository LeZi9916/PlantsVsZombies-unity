#nullable enable
using System;
using System.IO;
using System.Linq;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.Progression;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.Checkpoints
{
    /// <summary>Runs focused deterministic board persistence regressions.</summary>
    internal static class Program
    {
        /// <summary>Runs the fixtures and returns a failing exit code on the first mismatch.</summary>
        /// <returns>Zero on success.</returns>
        private static int Main()
        {
            try
            {
                SimulationPoolRegression.Run();
                foreach (var level in new[] { 1, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50 })
                {
                    RoundTrip(new GameSimulation(new GameRules { AdventureLevel = level }, 1000 + level), "adventure " + level);
                }
                foreach (var definition in ModeCatalog.All)
                {
                    if (MatchCheckpointStore.SupportsMode(definition.Id))
                    {
                        var mode = new ModeSession(definition.Id);
                        RoundTrip(mode.Game, "mode " + definition.Id);
                    }
                }
                var battle = new ModeSession(13);
                battle.ChooseSeeds(new[] { PlantKind.Peashooter, PlantKind.Sunflower, PlantKind.Wallnut, PlantKind.PotatoMine, PlantKind.Garlic, PlantKind.KernelPult });
                for (var tick = 0; tick < 200; tick++)
                {
                    battle.Update(.01f);
                }
                var id = 500;
                foreach (PlantKind kind in Enum.GetValues(typeof(PlantKind)))
                {
                    var plant = new Plant { Id = ++id, Kind = kind, Row = 0, Column = 2, Health = 100 };
                    _ = plant.Behavior;
                    SimulationFixture.Plants(battle.Game).Add(plant);
                }
                foreach (ZombieKind kind in Enum.GetValues(typeof(ZombieKind)))
                {
                    var zombie = new Zombie(kind) { Id = ++id, Row = 0, X = 8f, Health = 200 };
                    _ = zombie.Behavior;
                    SimulationFixture.Zombies(battle.Game).Add(zombie);
                }
                var target = battle.Game.Plants.First(item => item.Kind == PlantKind.Garlic);
                target.Health = 275f;
                var cannon = battle.Game.Plants.First(item => item.Kind == PlantKind.CobCannon);
                cannon.CannonPhase = CobCannonPhase.Firing;
                cannon.CannonReloadRemaining = 1.5f;
                cannon.CannonTargetX = 7f;
                cannon.CannonTargetRow = 2f;
                cannon.CannonResetVersion = 7;
                battle.Game.Plants.First(item => item.Kind == PlantKind.Chomper).ChomperPresentation = new AdventureChomperState
                {
                    NativePhase = 13,
                    Countdown = 150,
                    PhaseTicks = 70,
                    Animation = "anim_chew"
                };
                SimulationFixture.Peas(battle.Game).Add(new Pea { Homing = true, TargetZombieId = battle.Game.Zombies[0].Id,
                    X = 3.25f, FlightRow = 2f, PresentationHomingY = 247f, VelocityX = 2.5f, SourceKind = PlantKind.Cattail });
                SimulationFixture.Suns(battle.Game).Add(new global::PlantsVsZombies.Simulation.Sun { X = 120f, Y = 100f, TargetY = 250f, Value = 50 });
                ((SimulationWorld)battle.Game.MemberOwner("Plants"))._seedCooldownTicks[(int)PlantKind.Garlic] = 1234;
                battle.Game.Zombies.First(item => item.Kind == ZombieKind.Bungee).BungeeState = new AdventureBungeeState
                {
                    Phase = 7,
                    PhaseTicks = 80,
                    TargetPlant = target,
                    AnimationSeconds = .2f
                };
                battle.Game.Presentation.MowerTimes[battle.Game.Mowers[0]] = .7f;
                var linked = GameSimulation.RestoreCheckpoint(battle.Game.CaptureCheckpoint());
                Require(ReferenceEquals(linked.Zombies.First(item => item.Kind == ZombieKind.Bungee).BungeeState!.TargetPlant,
                    linked.Plants.First(item => item.Id == target.Id)), "bungee cargo identity");
                Require(linked.Presentation.MowerTimes.ContainsKey(linked.Mowers[0]), "presentation dictionary entity identity");
                RoundTrip(battle.Game, "all capability owners");
                StoreChecks(battle.Game);
                CampaignChecks();
                EndLevelSaveChecks();
                LongSessionSoundHistory();
                Console.WriteLine("Checkpoint regressions passed.");
                return 0;
            }
            catch (Exception error)
            {
                Console.Error.WriteLine(error);
                return 1;
            }
        }

        /// <summary>Checks a long endless session cannot exhaust the graph limit through historical one-shot audio.</summary>
        private static void LongSessionSoundHistory()
        {
            var game = new ModeSession(13).Game;
            game.Presentation._plantSoundCues.Add(new AdventurePlantSoundCue { Ticks = 25, Foley = 3 });
            var before = game.CaptureCheckpoint();
            for (var index = 0; index < 130000; index++)
            {
                game.Presentation.ZombieSounds.Add(new AdventureZombieSoundEvent { Id = index, Foley = 3, Tick = index, Resolved = true });
            }
            var bytes = game.CaptureCheckpoint();
            Require(before.SequenceEqual(bytes), "one-shot history does not grow checkpoint payload");
            Require(game.Presentation.ZombieSounds.Count == 130000, "capture preserves live audio consumer state");
            var restored = GameSimulation.RestoreCheckpoint(bytes);
            Require(restored.Presentation.ZombieSounds.Count == 0, "restored history cannot replay old one-shots");
            Require(restored.Presentation._plantSoundCues.Count == 1 && restored.Presentation._plantSoundCues[0].Ticks == 25,
                "pending sound milestones remain authoritative");
            RoundTrip(game, "long endless sound history");
            StoreChecks(game);
        }

        /// <summary>Compares complete saved state and the subsequent deterministic simulation.</summary>
        /// <param name="game">The isolated board.</param>
        /// <param name="label">The fixture context.</param>
        private static void RoundTrip(GameSimulation game, string label)
        {
            var before = game.CaptureCheckpoint();
            var restored = GameSimulation.RestoreCheckpoint(before);
            Require(before.SequenceEqual(restored.CaptureCheckpoint()), label + " initial graph");
            if (restored.ModeSession != null)
            {
                Require(ReferenceEquals(restored.ModeSession.Game, restored), label + " facade link");
            }
            for (var tick = 0; tick < 30; tick++)
            {
                if (game.ModeSession != null)
                {
                    game.ModeSession.Update(.01f);
                    restored.ModeSession!.Update(.01f);
                }
                else
                {
                    game.Update(.01f);
                    restored.Update(.01f);
                }
            }
            Require(game.CaptureCheckpoint().SequenceEqual(restored.CaptureCheckpoint()), label + " continued graph");
            Console.WriteLine("PASS " + label);
        }

        /// <summary>Checks per-user/mode files, replacement and rejected corrupt reads.</summary>
        /// <param name="game">The isolated board to save.</param>
        private static void StoreChecks(GameSimulation game)
        {
            var directory = Path.Combine(Path.GetTempPath(), "pvz-checkpoint-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new MatchCheckpointStore(directory);
                var profile = Guid.NewGuid();
                var mode = game.ModeSession!.Definition.Id;
                store.Save(profile, mode, "same-attempt", game);
                Require(!store.Exists(Guid.NewGuid(), mode) && !store.Exists(profile, 0), "profile/mode isolation");
                var restored = store.Load(profile, mode, out var receipt);
                Require(receipt == "same-attempt" && restored.ModeSession!.RunId == game.ModeSession.RunId, "receipt continuation");
                Require(game.CaptureCheckpoint().SequenceEqual(restored.CaptureCheckpoint()), "disk graph");
                store.Save(profile, mode, "same-attempt", restored);
                var path = store.SavePath(profile, mode);
                var bytes = File.ReadAllBytes(path);
                var failedStore = new MatchCheckpointStore(directory, stage =>
                {
                    if (stage == ProfileSaveStage.BeforeCommit)
                    {
                        throw new IOException("Isolated disk failure before replace.");
                    }
                });
                try
                {
                    failedStore.Save(profile, mode, "must-not-publish", restored);
                    throw new InvalidOperationException("Expected a failed atomic write.");
                }
                catch (IOException)
                {
                }
                Require(bytes.SequenceEqual(File.ReadAllBytes(path)), "failed write preserves old checkpoint");
                Require(Directory.GetFiles(directory, "*.tmp").Length == 0, "failed write removes temporary file");
                bytes[bytes.Length / 2] ^= 1;
                File.WriteAllBytes(path, bytes);
                var rejected = false;
                try
                {
                    store.Load(profile, mode, out _);
                }
                catch (InvalidDataException)
                {
                    rejected = true;
                }
                Require(rejected, "checksum rejection");
                store.Delete(profile, mode);
                Require(!store.Exists(profile, mode), "delete checkpoint");
                for (var nativeMode = 0; nativeMode <= 72; nativeMode++)
                {
                    var excluded = nativeMode == 42 || nativeMode == 43 || nativeMode == 50 || nativeMode == 71 || nativeMode == 72;
                    Require(MatchCheckpointStore.SupportsMode(nativeMode) != excluded, "PC mode gate " + nativeMode);
                }
            }
            finally
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }
            }
        }

        /// <summary>Checks full adventure continuation rebinds receipts without purchasing another rake.</summary>
        private static void CampaignChecks()
        {
            var directory = Path.Combine(Path.GetTempPath(), "pvz-campaign-checkpoint-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var profile = Guid.NewGuid();
                var progress = new AdventureProgressStore(Path.Combine(directory, "campaign.adventure"));
                progress.Import(profile, 12, 1);
                var accounts = new PlayerAccountStore(Path.Combine(directory, "player.account"));
                var purchases = new int[80];
                purchases[24] = 3;
                accounts.Import(profile, 1000, purchases, new int[100], 0, Array.Empty<GardenPlant>());
                var checkpoints = new MatchCheckpointStore(directory);
                var campaign = new AdventureCampaign(progress, profile, accounts) { Checkpoints = checkpoints };
                var game = campaign.BeginOrResume();
                var rakeCount = accounts.Get(profile).PurchaseCount(24);
                Require(rakeCount == 2, "initial board consumes one rake");
                Require(game.Rules.AdventureCoinCommit!(10), "first coin receipt");
                checkpoints.Save(profile, 0, campaign.AttemptId, game);
                var next = new AdventureCampaign(new AdventureProgressStore(Path.Combine(directory, "campaign.adventure")), profile, accounts)
                {
                    Checkpoints = new MatchCheckpointStore(directory)
                };
                var restored = next.BeginOrResume();
                Require(next.WasRestored && next.AttemptId == campaign.AttemptId, "adventure attempt continuation");
                Require(accounts.Get(profile).PurchaseCount(24) == rakeCount, "continuation does not consume rake");
                Require(restored.Rules.AdventureCoinCommit!(10) && accounts.Get(profile).Coins == 1010, "continuation cannot double-credit coins");
                Require(restored.Rules.AdventureCoinCommit!(20) && accounts.Get(profile).Coins == 1020, "continued coin cumulative receipt");
                Require(game.CaptureCheckpoint().SequenceEqual(restored.CaptureCheckpoint()), "campaign restored graph");
                Console.WriteLine("PASS adventure receipts and rake continuation");
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        /// <summary>Checks PC 418020 does not score untouched drops and pays remaining mowers once.</summary>
        private static void EndLevelSaveChecks()
        {
            var game = new ModeSession(1).Game;
            game.Adventure._coins.Add(new AdventureCoin { Id = 1, Kind = AdventureCoinKind.Silver, IsBeingCollected = true });
            game.Adventure._coins.Add(new AdventureCoin { Id = 2, Kind = AdventureCoinKind.Diamond });
            var credited = 0;
            game.Rules.AdventureCoinCommit = total =>
            {
                credited = total;
                return true;
            };
            Require(game.FinishAdventureForSaving(), "finish end-level save");
            Require(credited == 26 && game.Adventure.Coins.Count == 0, "score collecting coin plus five gold mower bonuses");
            Require(game.FinishAdventureForSaving() && credited == 26, "end-level save retry is idempotent");
            Console.WriteLine("PASS native end-level save settlement");
        }

        /// <summary>Fails with the fixture context when a required condition is false.</summary>
        /// <param name="condition">The observed condition.</param>
        /// <param name="label">The diagnostic label.</param>
        private static void Require(bool condition, string label)
        {
            if (!condition)
            {
                throw new InvalidOperationException(label);
            }
        }
    }
}
