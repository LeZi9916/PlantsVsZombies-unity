#nullable enable
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using PlantsVsZombies.Game;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.Progression;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Tests.Shared;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode.Smoke
{
    /// <summary>Owns the real game host and disposable saves for migrated Player scenarios.</summary>
    [Category("PlayerSmoke")]
    public abstract class PlayerSmokeFixture
    {
        /// <summary>Stores the host created by this test.</summary>
        private GameObject? _host;
        /// <summary>Stores the camera target owned by this case.</summary>
        private RenderTexture? _target;
        /// <summary>Stores the previous production camera target.</summary>
        private RenderTexture? _previousTarget;
        /// <summary>Stores pre-existing game hosts suspended for the test.</summary>
        private PvZGame[] _previousHosts = Array.Empty<PvZGame>();
        /// <summary>Stores the previous background update policy.</summary>
        private bool _runInBackground;
        /// <summary>Stores the previous frame-rate limit.</summary>
        private int _targetFrameRate;
        /// <summary>Stores the previous cursor visibility.</summary>
        private bool _cursorVisible;
        /// <summary>Stores whether global application state was captured.</summary>
        private bool _configured;
        /// <summary>Gets the real controller created for this test.</summary>
        protected PvZGame Controller { get; private set; } = null!;
        /// <summary>Gets whether this case starts with the experimental single-row board.</summary>
        protected virtual bool ExperimentalLevelOne => false;
        /// <summary>Gets whether selector input opens the legacy demo board.</summary>
        protected virtual bool LegacyBoard => false;
        /// <summary>Gets whether the full original opening remains enabled in this isolated session.</summary>
        protected virtual bool PlayOpening => false;
        /// <summary>Gets whether the original loading presentation waits for explicit completion.</summary>
        protected virtual bool ShowLoading => false;
        /// <summary>Gets whether this case starts with an empty profile catalog.</summary>
        protected virtual bool FirstUser => false;

        /// <summary>Creates a complete session with original resources and test-owned save files.</summary>
        /// <returns>One frame for the initialized game host.</returns>
        [UnitySetUp]
        public IEnumerator CreateSession()
        {
            var root = Environment.GetEnvironmentVariable("PVZ_BOARD_STRESS_ASSETS");
            if (string.IsNullOrEmpty(root))
            {
                Assert.Ignore("Set PVZ_BOARD_STRESS_ASSETS to the complete original runtime resources.");
            }

            Assert.That(Directory.Exists(Path.Combine(root!, "reanim")), Is.True, "Original reanimation resources are required.");
            _runInBackground = Application.runInBackground;
            _targetFrameRate = Application.targetFrameRate;
            _cursorVisible = Cursor.visible;
            _configured = true;
            _previousHosts = UnityEngine.Object.FindObjectsByType<PvZGame>();
            foreach (var game in _previousHosts)
            {
                game.gameObject.SetActive(false);
            }

            var output = Environment.GetEnvironmentVariable("PVZ_PLAYER_SMOKE_OUTPUT")
                ?? Path.Combine(Application.dataPath, "..", ".unity-runs", "player-smoke");
            var directory = Path.GetFullPath(Path.Combine(output, TestContext.CurrentContext.Test.Name + "-" + Guid.NewGuid().ToString("N")));
            _target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            _target.Create();
            _previousTarget = ObjectSpriteRenderer.SceneCamera.targetTexture;
            ObjectSpriteRenderer.SceneCamera.targetTexture = _target;
            SmokeTestArtifacts.Begin(directory, _target);
            _host = new GameObject("Test Runner game session");
            _host.SetActive(false);
            Controller = _host.AddComponent<PvZGame>();
            Controller._root = Path.GetFullPath(root!);
            Controller._smokeMode = true;
            Controller._experimentalLevelOne = ExperimentalLevelOne;
            Controller.Campaign.UseLegacyBoard = LegacyBoard;
            Controller.Opening.PlayOpeningInIsolatedSession = PlayOpening;
            Controller._profiles = new ProfileCatalog(Path.Combine(directory, "profiles.dat"));
            if (!FirstUser)
            {
                Controller._profiles.Create("自动测试");
            }

            Controller.Accounts._accountStore = new PlayerAccountStore(Path.Combine(directory, "player.account"));
            GameMemberReflection.SetField(Controller, "_adventureSavePath", Path.Combine(directory, "campaign.adventure"));
            _host.SetActive(true);
            Controller.Loading._originalShowLoadingForCapture = ShowLoading;
            TestContext.Progress.WriteLine("Player smoke artifacts: " + directory);
            yield return null;
        }

        /// <summary>Waits for the real Update-driven two-millisecond preparation phase before a scenario measures gameplay.</summary>
        /// <returns>Actual frame boundaries until the currently attached battle is ready.</returns>
        protected IEnumerator WaitForBattlePreparation()
        {
            // The first real frame lets Update bind a newly attached world before observing its ready flag.
            yield return null;
            for (var frame = 0; frame < 3600 && !Controller.Preparation.IsReady; frame++)
            {
                yield return null;
            }
            Assert.That(Controller.Preparation.IsReady, Is.True,
                "The real frame-budgeted battle preparation must converge before gameplay timing or input assertions.");
        }

        /// <summary>Disposes the session after assertions and restores the previous application state.</summary>
        /// <returns>A frame for Unity to destroy the test's rendering and audio objects.</returns>
        [UnityTearDown]
        public IEnumerator DisposeSession()
        {
            if (_target != null)
            {
                ObjectSpriteRenderer.SceneCamera.targetTexture = _previousTarget;
                _target.Release();
                UnityEngine.Object.Destroy(_target);
                _target = null;
            }

            if (_host != null)
            {
                UnityEngine.Object.Destroy(_host);
                _host = null;
                yield return null;
            }

            foreach (var game in _previousHosts)
            {
                if (game != null)
                {
                    game.gameObject.SetActive(true);
                }
            }

            _previousHosts = Array.Empty<PvZGame>();
            if (_configured)
            {
                Application.runInBackground = _runInBackground;
                Application.targetFrameRate = _targetFrameRate;
                Cursor.visible = _cursorVisible;
                _configured = false;
            }
        }
    }
}
