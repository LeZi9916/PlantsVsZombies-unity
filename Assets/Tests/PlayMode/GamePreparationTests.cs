#nullable enable
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using PlantsVsZombies.Game;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Simulation;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks cooperative preparation never advances the model and becomes allocation-free once ready.</summary>
    public sealed class GamePreparationTests
    {
        /// <summary>Prepares an isolated board across real frames and preserves its complete serialized model.</summary>
        /// <returns>Player frame boundaries between preparation steps.</returns>
        [UnityTest]
        public IEnumerator PreparationPreservesCheckpointAndReadyPollingDoesNotAllocate()
        {
            var root = Path.Combine(Path.GetTempPath(), "PvZPreparation-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "images"));
            Directory.CreateDirectory(Path.Combine(root, "reanim"));
            var host = new GameObject("Inactive preparation regression");
            host.SetActive(false);
            var owner = host.AddComponent<PvZGame>();
            var renderer = new ReanimationRenderer(root);
            var presenter = new GamePresenter(owner);
            try
            {
                owner._root = root;
                owner._animations = renderer;
                owner._presenter = presenter;
                owner._smokeMode = true;
                owner._currentView = PvZGame.ScreenView.Board;
                owner._game = new GameSimulation(new GameRules { AdventureLevel = 9 }, 7529);
                var before = owner.RequiredGame.CaptureCheckpoint();
                var random = owner.RequiredGame.BattleRandomDrawCount;
                var preparation = owner.Preparation;
                var steps = 0;
                do
                {
                    preparation.Advance();
                    steps++;
                    Assert.That(steps, Is.LessThan(2000), "Bounded preparation must converge without advancing the board.");
                    if (!preparation.IsReady)
                    {
                        Assert.That(preparation.BlocksGameplay, Is.True);
                        owner._choosingSeeds = true;
                        Assert.That(preparation.BlocksGameplay, Is.False, "Preparation must allow the seed chooser to remain interactive.");
                        owner._choosingSeeds = false;
                    }
                    yield return null;
                }
                while (!preparation.IsReady);
                Assert.That(owner.RequiredGame.BattleRandomDrawCount, Is.EqualTo(random));
                Assert.That(owner.RequiredGame.CaptureCheckpoint(), Is.EqualTo(before));
                Assert.That(preparation.BlocksGameplay, Is.False);
                using var recorder = new ProfilerRecorder(ProfilerCategory.Internal, "GC.Alloc", 4096,
                    ProfilerRecorderOptions.CollectOnlyOnCurrentThread);
                Assert.That(recorder.Valid, Is.True);
                recorder.Start();
                var sentinel = new byte[1024];
                recorder.Stop();
                GC.KeepAlive(sentinel);
                Assert.That(recorder.Count, Is.GreaterThan(0));
                recorder.Reset();
                recorder.Start();
                for (var i = 0; i < 1000; i++)
                {
                    preparation.Advance();
                }
                recorder.Stop();
                Assert.That(recorder.Count, Is.Zero);
                var generation = renderer.PresentationPreparationGeneration;
                owner._currentView = PvZGame.ScreenView.Selector;
                preparation.Advance();
                Assert.That(preparation.IsReady, Is.True);
                Assert.That(preparation.BlocksGameplay, Is.False);
                Assert.That(owner.RequiredGame.CaptureCheckpoint(), Is.EqualTo(before));
                owner._currentView = PvZGame.ScreenView.Board;
                preparation.Advance();
                Assert.That(renderer.PresentationPreparationGeneration, Is.EqualTo(generation + 1),
                    "Returning to the same world must reacquire its released stage resources.");
                Assert.That(owner.RequiredGame.BattleRandomDrawCount, Is.EqualTo(random));
                Assert.That(owner.RequiredGame.CaptureCheckpoint(), Is.EqualTo(before));
            }
            finally
            {
                presenter.Dispose();
                renderer.Dispose();
                UnityEngine.Object.Destroy(host);
                Directory.Delete(root, true);
            }
        }
    }
}
