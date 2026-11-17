#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.Checkpoints
{
    /// <summary>Checks explicit reset coverage, shared-reference retirement and persistence-neutral preparation.</summary>
    internal static class SimulationPoolRegression
    {
        /// <summary>Runs model lifetime regressions without Unity or private user data.</summary>
        internal static void Run()
        {
            EveryPooledTypeRestoresConstructionDefaults();
            SharedCapabilitySurvivesItsFirstOwner();
            CorpseAndImpactKeepRemovedActorAlive();
            BungeeCargoSurvivesPlantRemoval();
            MagnetSourceSurvivesItsTemporaryList();
            PreparationPreservesCheckpointAndRandomStreams();
            ExistingCheckpointRemainsCompatible();
            Console.WriteLine("PASS explicit model reset, shared ownership and capacity preparation");
        }

        /// <summary>Pollutes every mutable field and retained array so future field additions require reset coverage.</summary>
        private static void EveryPooledTypeRestoresConstructionDefaults()
        {
            var types = typeof(GameSimulation).Assembly.GetTypes().Where(type => !type.IsAbstract
                && typeof(ISimulationPooledObject).IsAssignableFrom(type));
            foreach (var type in types)
            {
                var expected = Activator.CreateInstance(type, true)!;
                var actual = (ISimulationPooledObject)Activator.CreateInstance(type, true)!;
                for (var current = type; current != null && current != typeof(object); current = current.BaseType)
                {
                    foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    {
                        if (field.GetValue(actual) is Array array)
                        {
                            for (var index = 0; index < array.Length; index++)
                            {
                                array.SetValue(DirtyValue(array.GetType().GetElementType()!, array.GetValue(index)), index);
                            }
                        }
                        else if (!field.IsInitOnly)
                        {
                            field.SetValue(actual, DirtyValue(field.FieldType, field.GetValue(actual)));
                        }
                    }
                }
                actual.ResetForPool();
                for (var current = type; current != null && current != typeof(object); current = current.BaseType)
                {
                    foreach (var field in current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    {
                        var before = field.GetValue(expected);
                        var after = field.GetValue(actual);
                        if (before is Array first && after is Array second)
                        {
                            Require(first.Cast<object>().SequenceEqual(second.Cast<object>()), type.Name + "." + field.Name + " array defaults");
                        }
                        else
                        {
                            Require(Equals(before, after), type.Name + "." + field.Name + " construction defaults");
                        }
                    }
                }
            }
        }

        /// <summary>Constructs a deliberately nondefault test value without involving production random sources.</summary>
        /// <param name="type">The field or array element type.</param>
        /// <param name="value">The current construction value.</param>
        /// <returns>A changed value when the test can construct one, otherwise the original value.</returns>
        private static object? DirtyValue(Type type, object? value)
        {
            if (type == typeof(bool))
            {
                return !(bool)value!;
            }
            if (type == typeof(int))
            {
                return 87;
            }
            if (type == typeof(float))
            {
                return 87f;
            }
            if (type == typeof(double))
            {
                return 87d;
            }
            if (type == typeof(string))
            {
                return "polluted-pool-state";
            }
            if (type.IsEnum)
            {
                return Enum.ToObject(type, 1);
            }
            if (type == typeof(IPlantBehavior))
            {
                return new UpgradePlantBehavior();
            }
            if (type == typeof(IZombieBehavior))
            {
                return new GargantuarZombieBehavior();
            }
            if (type.GetConstructor(Type.EmptyTypes) != null && !type.IsAbstract)
            {
                return Activator.CreateInstance(type);
            }
            return value;
        }

        /// <summary>Prevents a shared kelp capability from being reset while the other actor still uses it.</summary>
        private static void SharedCapabilitySurvivesItsFirstOwner()
        {
            var game = CreateGame();
            var world = game.CheckpointState;
            var plant = world.LifetimePool.Rent<Plant>();
            plant.Kind = PlantKind.TangleKelp;
            var zombie = world.LifetimePool.Rent<Zombie>();
            var state = world.LifetimePool.Rent<AdventureTangleKelpState>();
            state.Countdown = 27;
            plant.TangleKelpPresentation = state;
            zombie.TangleKelpGrab = state;
            world.Plants.Add(plant);
            world.Zombies.Add(zombie);
            game.Update(0f);
            world.Plants.Remove(plant);
            CompleteFrames(game, 4);
            Require(state.Countdown == 27 && ReferenceEquals(zombie.TangleKelpGrab, state), "shared kelp state retained by zombie");
            world.Zombies.Remove(zombie);
            CompleteFrames(game, 4);
            Require(ReferenceEquals(world.LifetimePool.Rent<AdventureTangleKelpState>(), state), "shared state reused only after final owner leaves");
            Require(state.Countdown == 100 && state.TargetZombieId == 0, "shared state construction defaults");
        }

        /// <summary>Keeps removed actor state until every corpse and projectile-impact attachment expires.</summary>
        private static void CorpseAndImpactKeepRemovedActorAlive()
        {
            var game = CreateGame();
            var world = game.CheckpointState;
            var zombie = world.LifetimePool.Rent<Zombie>();
            zombie.Id = 123;
            zombie.Health = 77f;
            world.Zombies.Add(zombie);
            var corpse = world.LifetimePool.Rent<AdventureZombieCorpse>();
            corpse.Zombie = zombie;
            var impact = world.LifetimePool.Rent<AdventureImpact>();
            impact.AttachedZombie = zombie;
            world.Presentation.Corpses.Add(corpse);
            world.Presentation.Impacts.Add(impact);
            game.Update(0f);
            world.Zombies.Remove(zombie);
            CompleteFrames(game, 4);
            Require(zombie.Id == 123 && zombie.Health == 77f, "corpse actor remains unchanged after removal");
            world.Presentation.Corpses.Clear();
            CompleteFrames(game, 4);
            Require(zombie.Id == 123, "impact independently retains actor");
            world.Presentation.Impacts.Clear();
            CompleteFrames(game, 1);
            Require(zombie.Id == 123, "retirement waits for a complete frame grace period");
            CompleteFrames(game, 3);
            Require(ReferenceEquals(world.LifetimePool.Rent<Zombie>(), zombie), "actor reused after every presentation root expires");
            Require(zombie.Id == 0 && zombie.Health == 0f && zombie.PresentationNativePhase == new Zombie().PresentationNativePhase,
                "actor reset includes native defaults");
        }

        /// <summary>Keeps stolen cargo alive after occupancy ends while the bungee still references it.</summary>
        private static void BungeeCargoSurvivesPlantRemoval()
        {
            var game = CreateGame();
            var world = game.CheckpointState;
            var plant = world.LifetimePool.Rent<Plant>();
            plant.Kind = PlantKind.Sunflower;
            plant.Id = 12;
            var zombie = world.LifetimePool.Rent<Zombie>();
            zombie.Kind = ZombieKind.Bungee;
            var state = world.LifetimePool.Rent<AdventureBungeeState>();
            state.TargetPlant = plant;
            zombie.BungeeState = state;
            world.Zombies.Add(zombie);
            world.Plants.Add(plant);
            game.Update(0f);
            world.Plants.Remove(plant);
            CompleteFrames(game, 4);
            Require(plant.Id == 12, "bungee cargo cannot be recycled when lawn occupancy ends");
            state.TargetPlant = null;
            CompleteFrames(game, 4);
            Require(ReferenceEquals(world.LifetimePool.Rent<Plant>(), plant), "released cargo enters plant pool");
        }

        /// <summary>Keeps a detached source snapshot alive while a Magnet-shroom still displays it.</summary>
        private static void MagnetSourceSurvivesItsTemporaryList()
        {
            var game = CreateGame();
            var world = game.CheckpointState;
            var plant = world.LifetimePool.Rent<Plant>();
            plant.Kind = PlantKind.MagnetShroom;
            var state = world.LifetimePool.Rent<AdventureMagnetState>();
            var item = world.LifetimePool.Rent<AdventureMagnetItem>();
            var source = world.LifetimePool.Rent<AdventureZombiePartDrop>();
            source.NativeKind = 9;
            item.Source = source;
            state.Item = item;
            plant.MagnetPresentation = state;
            world.Plants.Add(plant);
            CompleteFrames(game, 5);
            Require(source.NativeKind == 9, "nested magnetic source remains live outside the part-drop list");
            state.Item = null;
            CompleteFrames(game, 4);
            Require(ReferenceEquals(world.LifetimePool.Rent<AdventureZombiePartDrop>(), source), "released magnetic source is reused");
        }

        /// <summary>Checks step budgets, no-random preparation and checkpoint schema independence.</summary>
        private static void PreparationPreservesCheckpointAndRandomStreams()
        {
            var game = CreateGame();
            game.ChooseDefaultAdventureSeeds();
            var before = game.CaptureCheckpoint();
            var draws = game.BattleRandomDrawCount;
            var requirements = game.GetCapacityRequirements();
            var complete = false;
            for (var step = 0; step < 1000 && !complete; step++)
            {
                var created = game.RuntimePoolStatistics.CreatedCount;
                complete = game.PrepareCapacityStep(requirements, 16);
                Require(game.RuntimePoolStatistics.CreatedCount - created <= 16, "preparation object budget");
            }
            Require(complete, "bounded preparation finishes");
            Require(game.BattleRandomDrawCount == draws, "preparation preserves battle randomness");
            Require(before.SequenceEqual(game.CaptureCheckpoint()), "runtime sidecar does not change checkpoint bytes");
            var restored = GameSimulation.RestoreCheckpoint(before);
            Require(before.SequenceEqual(restored.CaptureCheckpoint()), "checkpoint restore excludes pool metadata");
            var pool = game.CheckpointState.LifetimePool;
            var record = pool.Rent<AdventureImpact>();
            game.Presentation.Impacts.Add(record);
            CompleteFrames(game, 3);
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            CompleteFrames(game, 100);
            Require(GC.GetAllocatedBytesForCurrentThread() == allocated, "warmed graph marking allocates no managed bytes");
            game.Presentation.Impacts.Clear();
            CompleteFrames(game, 4);
            var createdBefore = game.RuntimePoolStatistics.CreatedCount;
            allocated = GC.GetAllocatedBytesForCurrentThread();
            for (var round = 0; round < 100; round++)
            {
                var reused = pool.Rent<AdventureImpact>();
                game.Presentation.Impacts.Add(reused);
                CompleteFrames(game, 1);
                game.Presentation.Impacts.Clear();
                CompleteFrames(game, 4);
            }
            Require(GC.GetAllocatedBytesForCurrentThread() == allocated, "prepared event rent and delayed return allocate no managed bytes");
            Require(game.RuntimePoolStatistics.CreatedCount == createdBefore, "repeated event bursts retain their capacity");
        }

        /// <summary>Optionally checks a private pre-change checkpoint without copying or changing the source file.</summary>
        private static void ExistingCheckpointRemainsCompatible()
        {
            var source = Environment.GetEnvironmentVariable("PVZ_POOL_COMPATIBILITY_SOURCE");
            if (string.IsNullOrEmpty(source))
            {
                return;
            }
            using var stream = File.OpenRead(source);
            using var reader = new BinaryReader(stream);
            Require(reader.ReadString() == "PVZRMATCH1", "existing checkpoint envelope");
            reader.ReadBytes(16);
            reader.ReadInt32();
            reader.ReadString();
            var payload = reader.ReadBytes(reader.ReadInt32());
            var game = GameSimulation.RestoreCheckpoint(payload);
            Require(payload.SequenceEqual(game.CaptureCheckpoint()), "pre-change private checkpoint has unchanged field schema");
            var requirements = game.GetCapacityRequirements();
            var complete = false;
            for (var step = 0; step < 1000 && !complete; step++)
            {
                complete = game.PrepareCapacityStep(requirements, 16);
            }
            Require(complete && payload.SequenceEqual(game.CaptureCheckpoint()), "private checkpoint capacity preparation preserves all model bytes");
            Console.WriteLine("PASS pre-change private checkpoint schema and preparation");
        }

        /// <summary>Creates an isolated deterministic board whose zero-delta updates only maintain runtime ownership.</summary>
        /// <returns>A new board without private runtime files.</returns>
        private static GameSimulation CreateGame()
        {
            return new GameSimulation(new GameRules { AdventureLevel = 9 }, 777);
        }

        /// <summary>Completes explicit facade boundaries without advancing simulation time.</summary>
        /// <param name="game">The board whose runtime ownership is checked.</param>
        /// <param name="count">The number of complete boundaries.</param>
        private static void CompleteFrames(GameSimulation game, int count)
        {
            for (var frame = 0; frame < count; frame++)
            {
                game.Update(0f);
            }
        }

        /// <summary>Rejects a failed lifecycle assertion.</summary>
        /// <param name="condition">The expected invariant.</param>
        /// <param name="message">The violated lifecycle rule.</param>
        /// <exception cref="InvalidOperationException">The invariant fails.</exception>
        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
