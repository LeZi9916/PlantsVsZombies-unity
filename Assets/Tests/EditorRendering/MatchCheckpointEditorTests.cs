#nullable enable
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Editor;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.Simulation;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.EditorRendering
{
    /// <summary>Checks offline checkpoint editing, retained model links, atomic writes and native window recovery.</summary>
    public sealed class MatchCheckpointEditorTests
    {
        /// <summary>The disposable save directory owned by this fixture.</summary>
        private string _directory = string.Empty;
        /// <summary>The profile selected in the game.</summary>
        private Guid _first;
        /// <summary>A second independently saved profile.</summary>
        private Guid _second;
        /// <summary>The untouched source board used to compare RNG continuation.</summary>
        private GameSimulation _original = null!;
        /// <summary>The fixture's real checkpoint backend.</summary>
        private MatchCheckpointStore _store = null!;
        /// <summary>The synthetic profile catalog path.</summary>
        private string CatalogPath => Path.Combine(_directory, "profiles.dat");

        /// <summary>Creates three actual checkpoints and unrelated sidecars using only temporary files.</summary>
        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "pvz-checkpoint-editor-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            var profiles = new ProfileCatalog(CatalogPath);
            _first = profiles.Create("First").Id;
            _second = profiles.Create("Second").Id;
            profiles.Select(_first);
            _store = new MatchCheckpointStore(_directory);
            var mode = new ModeSession(1);
            mode.ChooseSeeds(new[] { PlantKind.Peashooter, PlantKind.Sunflower, PlantKind.Wallnut, PlantKind.PotatoMine, PlantKind.SnowPea, PlantKind.Chomper });
            _original = mode.Game;
            var plant = new Plant { Id = 6001, Kind = PlantKind.Wallnut, Row = 2, Column = 3, Health = 1234f };
            SimulationFixture.Plants(_original).Add(plant);
            SimulationFixture.Zombies(_original).Add(new Zombie(ZombieKind.Bungee)
            {
                Id = 6002,
                Row = 2,
                X = 4f,
                Health = 200f,
                BungeeState = new AdventureBungeeState { TargetPlant = plant, Phase = 7, PhaseTicks = 50 }
            });
            _original.Presentation.PlantingEffects.Add(new AdventurePlantingEffect { Id = 60, X = 300f, Age = .1f });
            _original.Presentation.MowerTimes[_original.Mowers[0]] = .75f;
            SimulationFixture.Suns(_original).Add(new global::PlantsVsZombies.Simulation.Sun
            {
                PresentationNative = true,
                PresentationPickup = new global::PlantsVsZombies.Sun.SunPickupState(global::PlantsVsZombies.Sun.SunKind.Normal, 100f, 200f)
            });
            _store.Save(_first, 1, mode.RunId.ToString("N"), _original);
            _store.Save(_first, 0, "adventure-attempt", new GameSimulation(new GameRules { AdventureLevel = 2 }));
            var other = new ModeSession(6);
            _store.Save(_second, 6, other.RunId.ToString("N"), other.Game);
            File.WriteAllBytes(Path.Combine(_directory, "player.account"), new byte[] { 1, 2, 3 });
            File.WriteAllBytes(Path.Combine(_directory, "campaign.adventure"), new byte[] { 4, 5, 6 });
        }

        /// <summary>Deletes only the unique temporary fixture directory.</summary>
        [TearDown]
        public void TearDown()
        {
            Directory.Delete(_directory, true);
        }

        /// <summary>Checks passive discovery and profile selection neither write nor select the game user.</summary>
        [Test]
        public void ReadingAndSelectingArePassiveAndDoNotRequireProgressionSidecars()
        {
            var catalog = File.ReadAllBytes(CatalogPath);
            var files = Directory.GetFiles(_directory);
            var checkpoint = File.ReadAllBytes(_store.SavePath(_first, 1));
            var session = new MatchCheckpointEditorSession(_directory);
            Assert.That(session.SelectedProfileId, Is.EqualTo(_first));
            Assert.That(session.AvailableModes, Is.EqualTo(new[] { 0, 1 }));
            session.SelectMode(1);
            session.Game!.CheckpointState.SunAmount = 999;
            session.SelectProfile(_second);
            Assert.That(session.AvailableModes, Is.EqualTo(new[] { 6 }));
            session.SelectMode(6);
            session.SelectProfile(_first);
            session.SelectMode(1);
            Assert.That(session.Game!.SunAmount, Is.EqualTo(_original.SunAmount));
            Assert.That(File.ReadAllBytes(_store.SavePath(_first, 1)), Is.EqualTo(checkpoint));
            Assert.That(File.ReadAllBytes(CatalogPath), Is.EqualTo(catalog));
            Assert.That(new ProfileCatalog(CatalogPath).SelectedId, Is.EqualTo(_first));
            Assert.That(Directory.GetFiles(_directory), Is.EquivalentTo(files));
        }

        /// <summary>Checks the editor's validation and write path accept every implemented native mode without changing unedited state.</summary>
        [Test]
        public void AllImplementedModesPreserveTheirCompleteUneditedState()
        {
            foreach (var definition in ModeCatalog.All)
            {
                if (!MatchCheckpointStore.SupportsMode(definition.Id))
                {
                    continue;
                }
                var mode = new ModeSession(definition.Id);
                var expected = mode.Game.CaptureCheckpoint();
                _store.Save(_first, definition.Id, mode.RunId.ToString("N"), mode.Game);
                var editor = new MatchCheckpointEditorSession(_directory);
                editor.SelectMode(definition.Id);
                editor.Save();
                var restored = _store.Load(_first, definition.Id, out var receipt);
                Assert.That(restored.CaptureCheckpoint(), Is.EqualTo(expected), definition.Name);
                Assert.That(receipt, Is.EqualTo(mode.RunId.ToString("N")));
            }
        }

        /// <summary>Checks real field adapters persist nested structs, multidimensional arrays, linked cargo and untouched RNG.</summary>
        [Test]
        public void FieldEditsRoundTripWithReferencesReceiptsBackupAndOtherFilesIntact()
        {
            var session = Open();
            var path = session.CheckpointPath;
            var before = File.ReadAllBytes(path);
            var other = File.ReadAllBytes(_store.SavePath(_second, 6));
            var adventure = File.ReadAllBytes(_store.SavePath(_first, 0));
            var catalog = File.ReadAllBytes(CatalogPath);
            var game = session.Game!;
            var root = Root(game.CheckpointState);
            Child(root, "SunAmount").SetValue(1777);
            var plantNode = Child(root, "Plants").Children(0, 1)[0];
            var plant = game.Plants[0];
            Assert.That(SimulationFixture.Plants(game).InRow(2), Has.Member(plant));
            Child(plantNode, "Row").SetValue(3);
            Child(plantNode, "Health").SetValue(876f);
            Assert.That(SimulationFixture.Plants(game).InRow(2), Has.No.Member(plant));
            Assert.That(SimulationFixture.Plants(game).InRow(3), Has.Member(plant));
            Child(Child(plantNode, "ProducerGlow"), "FlashTicks").SetValue(40);
            Child(root, "SeedCooldownTicks").Children((int)PlantKind.Wallnut, 1)[0].SetValue(123);
            var boardCells = Child(root, "BoardCells");
            Child(boardCells, "Opacity").Children(8, 1)[0].SetValue(123);
            var presentation = Child(root, "Presentation");
            Child(Child(presentation, "PlantingEffects").Children(0, 1)[0], "Age").SetValue(.3f);
            Child(presentation, "MowerTimes").Children(0, 1)[0].SetValue(1.25f);
            var pickup = Child(Child(root, "Suns").Children(0, 1)[0], "PresentationPickup");
            Child(pickup, "X").SetValue(222f);
            Assert.Throws<InvalidOperationException>(() => Child(plantNode, "Id").SetValue(123));
            Assert.Throws<InvalidOperationException>(() => Child(plantNode, "Kind").SetValue(PlantKind.Garlic));
            Assert.That(Child(root, "Random").ReadOnly, Is.True);
            Assert.Throws<ArgumentException>(() => Child(plantNode, "Health").SetValue(float.NaN));
            session.Save();
            var restored = _store.Load(_first, 1, out var receipt);
            Assert.That(restored.SunAmount, Is.EqualTo(1777));
            Assert.That(restored.Plants[0].Row, Is.EqualTo(3));
            Assert.That(restored.Plants[0].Health, Is.EqualTo(876f));
            Assert.That(restored.Plants[0].ProducerGlow.FlashTicks, Is.EqualTo(40));
            Assert.That(restored.CheckpointState._seedCooldownTicks[(int)PlantKind.Wallnut], Is.EqualTo(123));
            Assert.That(Child(Child(Root(restored.CheckpointState), "BoardCells"), "Opacity").Children(8, 1)[0].Value, Is.EqualTo(123));
            Assert.That(restored.Presentation.PlantingEffects[0].Age, Is.EqualTo(.3f));
            Assert.That(restored.Presentation.MowerTimes[restored.Mowers[0]], Is.EqualTo(1.25f));
            Assert.That(restored.Suns[0].PresentationPickup.X, Is.EqualTo(222f));
            Assert.That(restored.Zombies[0].BungeeState!.TargetPlant, Is.SameAs(restored.Plants[0]));
            Assert.That(receipt, Is.EqualTo(_original.ModeSession!.RunId.ToString("N")));
            Assert.That(restored.ModeSession!.RunId, Is.EqualTo(_original.ModeSession.RunId));
            for (var draw = 0; draw < 20; draw++)
            {
                Assert.That(restored.DrawAdventureRandomInt(1000), Is.EqualTo(_original.DrawAdventureRandomInt(1000)));
            }
            Assert.That(File.ReadAllBytes(path + ".backup"), Is.EqualTo(before));
            Assert.That(File.ReadAllBytes(_store.SavePath(_second, 6)), Is.EqualTo(other));
            Assert.That(File.ReadAllBytes(_store.SavePath(_first, 0)), Is.EqualTo(adventure));
            Assert.That(File.ReadAllBytes(CatalogPath), Is.EqualTo(catalog));
            Assert.That(File.ReadAllBytes(Path.Combine(_directory, "player.account")), Is.EqualTo(new byte[] { 1, 2, 3 }));
        }

        /// <summary>Checks external runtime writes and a held checkpoint lease prevent stale editor commits.</summary>
        [Test]
        public void ExternalChangesAndHeldLeasesCannotBeOverwritten()
        {
            var session = Open();
            session.Game!.CheckpointState.SunAmount = 777;
            _original.CheckpointState.SunAmount = 888;
            _store.Save(_first, 1, _original.ModeSession!.RunId.ToString("N"), _original);
            var latest = File.ReadAllBytes(session.CheckpointPath);
            Assert.Throws<IOException>(() => session.Save());
            Assert.That(File.ReadAllBytes(session.CheckpointPath), Is.EqualTo(latest));
            Assert.That(File.Exists(session.CheckpointPath + ".backup"), Is.False);
            session.SelectMode(1);
            using (var lease = new FileStream(session.CheckpointPath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                Assert.Throws<IOException>(() => session.Save());
                Assert.Throws<IOException>(() => _store.Delete(_first, 1));
            }
            Assert.That(File.ReadAllBytes(session.CheckpointPath), Is.EqualTo(latest));
        }

        /// <summary>Checks invalid drafts cannot replace a valid saved board.</summary>
        [Test]
        public void InvalidCountersAndDamagedFilesAreRejectedWithoutReplacingDrafts()
        {
            var session = Open();
            var before = File.ReadAllBytes(session.CheckpointPath);
            session.Game!.CheckpointState.SunAmount = -1;
            Assert.Throws<InvalidDataException>(() => session.Save());
            Assert.That(File.ReadAllBytes(session.CheckpointPath), Is.EqualTo(before));
            var retained = session.Game;
            var bytes = File.ReadAllBytes(_store.SavePath(_first, 0));
            bytes[bytes.Length / 2] ^= 1;
            File.WriteAllBytes(_store.SavePath(_first, 0), bytes);
            Assert.Throws<InvalidDataException>(() => session.SelectMode(0));
            Assert.That(session.Game, Is.SameAs(retained));
            Assert.That(session.SelectedModeId, Is.EqualTo(1));
        }

        /// <summary>Checks successive editor commits replace the backup with the immediate previous revision.</summary>
        [Test]
        public void RepeatedSaveRetainsTheImmediatelyPreviousRevision()
        {
            var session = Open();
            session.Game!.CheckpointState.SunAmount = 700;
            session.Save();
            var prior = File.ReadAllBytes(session.CheckpointPath);
            session.Game!.CheckpointState.SunAmount = 800;
            session.Save();
            Assert.That(File.ReadAllBytes(session.CheckpointPath + ".backup"), Is.EqualTo(prior));
            Assert.That(_store.Load(_first, 1, out _).SunAmount, Is.EqualTo(800));
        }

        /// <summary>Checks complete pending edits and old baselines survive native window serialization twice.</summary>
        [Test]
        public void WindowRecoveryRetainsEditsAndStaleBaselinesAcrossReloads()
        {
            var session = Open();
            session.Game!.CheckpointState.SunAmount = 777;
            var source = Window(session);
            string json;
            try
            {
                source.MarkDirty();
                json = EditorJsonUtility.ToJson(source);
            }
            finally
            {
                Destroy(source);
            }
            _original.CheckpointState.SunAmount = 888;
            _store.Save(_first, 1, _original.ModeSession!.RunId.ToString("N"), _original);
            var latest = File.ReadAllBytes(_store.SavePath(_first, 1));
            for (var reload = 0; reload < 2; reload++)
            {
                var window = ScriptableObject.CreateInstance<MatchCheckpointEditorWindow>();
                try
                {
                    Invoke(window, "OnDisable");
                    EditorJsonUtility.FromJsonOverwrite(json, window);
                    Invoke(window, "OnEnable");
                    var restored = Read<MatchCheckpointEditorSession>(window, "_session");
                    Assert.That(restored.Game!.SunAmount, Is.EqualTo(777));
                    Assert.That(restored.HasStaleDraft, Is.True);
                    Assert.That(window.hasUnsavedChanges, Is.True);
                    Assert.That(Read<bool>(window, "_requiresReload"), Is.True);
                    Assert.Throws<InvalidOperationException>(() => window.SaveChanges());
                    window.MarkDirty();
                    json = EditorJsonUtility.ToJson(window);
                }
                finally
                {
                    Destroy(window);
                }
            }
            Assert.That(File.ReadAllBytes(_store.SavePath(_first, 1)), Is.EqualTo(latest));
        }

        /// <summary>Checks a fresh serialized draft can be recovered and committed using its unchanged original baseline.</summary>
        [Test]
        public void FreshDraftRecoveryCanSaveAndCatalogChangesBlockCommit()
        {
            var session = Open();
            session.Game!.CheckpointState.SunAmount = 777;
            var draft = JsonUtility.FromJson<MatchCheckpointEditorDraft>(JsonUtility.ToJson(MatchCheckpointEditorDraft.Capture(session)));
            var recovered = new MatchCheckpointEditorSession(_directory);
            recovered.RestoreDraft(draft);
            Assert.That(recovered.HasStaleDraft, Is.False);
            recovered.Save();
            Assert.That(_store.Load(_first, 1, out _).SunAmount, Is.EqualTo(777));
            var before = File.ReadAllBytes(recovered.CheckpointPath);
            new ProfileCatalog(CatalogPath).Rename(_first, "Changed externally");
            Assert.Throws<IOException>(() => recovered.Save());
            Assert.That(File.ReadAllBytes(recovered.CheckpointPath), Is.EqualTo(before));
        }

        /// <summary>Checks Play Mode invalidation preserves pending edits but refuses the native Save command.</summary>
        [Test]
        public void PlayModeInvalidatesTheWindowWithoutDroppingPendingEdits()
        {
            var session = Open();
            var before = File.ReadAllBytes(session.CheckpointPath);
            var window = Window(session);
            try
            {
                session.Game!.CheckpointState.SunAmount = 777;
                window.MarkDirty();
                typeof(MatchCheckpointEditorWindow).GetMethod("OnPlayModeChanged", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(window, new object[] { PlayModeStateChange.ExitingEditMode });
                Assert.That(Read<bool>(window, "_requiresReload"), Is.True);
                Assert.That(window.hasUnsavedChanges, Is.True);
                Assert.Throws<InvalidOperationException>(() => window.SaveChanges());
                Assert.That(File.ReadAllBytes(session.CheckpointPath), Is.EqualTo(before));
            }
            finally
            {
                Destroy(window);
            }
        }

        /// <summary>Checks all native GUI categories can repaint without writing files, then saves through the real window.</summary>
        /// <returns>Frames for actual EditorWindow repaint callbacks.</returns>
        [UnityTest]
        public IEnumerator NativeWindowRepaintsAllCategoriesAndSavesOneDraft()
        {
            var session = Open();
            var before = File.ReadAllBytes(session.CheckpointPath);
            var window = Window(session);
            try
            {
                window.Show();
                for (var tab = 0; tab < 7; tab++)
                {
                    Write(window, "_tab", tab);
                    window.Repaint();
                    yield return null;
                }
                Assert.That(window.hasUnsavedChanges, Is.False);
                Assert.That(File.ReadAllBytes(session.CheckpointPath), Is.EqualTo(before));
                session.Game!.CheckpointState.SunAmount = 1234;
                window.MarkDirty();
                using (var lease = new FileStream(session.CheckpointPath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {
                    Assert.Throws<InvalidOperationException>(() => window.SaveChanges());
                    Assert.That(window.hasUnsavedChanges, Is.True);
                }
                window.SaveChanges();
                Assert.That(window.hasUnsavedChanges, Is.False);
                Assert.That(_store.Load(_first, 1, out _).SunAmount, Is.EqualTo(1234));
            }
            finally
            {
                Destroy(window);
            }
        }

        /// <summary>Opens the fixture's first profile and survival checkpoint.</summary>
        /// <returns>The detached editor session.</returns>
        private MatchCheckpointEditorSession Open()
        {
            var session = new MatchCheckpointEditorSession(_directory);
            session.SelectMode(1);
            return session;
        }

        /// <summary>Wraps an actual saved model with the production field adapter.</summary>
        /// <param name="value">The fixture model.</param>
        /// <returns>The nonreplaceable editable root.</returns>
        private static MatchCheckpointFieldNode Root(object value)
        {
            return new MatchCheckpointFieldNode("Root", "Root", value.GetType(), () => value);
        }

        /// <summary>Finds an actual saved model field by its normalized display name.</summary>
        /// <param name="node">The parent model.</param>
        /// <param name="name">The native property or field name.</param>
        /// <returns>The matching field adapter.</returns>
        private static MatchCheckpointFieldNode Child(MatchCheckpointFieldNode node, string name)
        {
            return node.Children(0, node.ChildCount).Single(child => child.Name == name);
        }

        /// <summary>Creates an isolated native editor window with the fixture's detached state.</summary>
        /// <param name="session">The fixture session.</param>
        /// <returns>The temporary window.</returns>
        private static MatchCheckpointEditorWindow Window(MatchCheckpointEditorSession session)
        {
            var window = ScriptableObject.CreateInstance<MatchCheckpointEditorWindow>();
            Write(window, "_session", session);
            Write(window, "_directory", session.DirectoryPath);
            return window;
        }

        /// <summary>Closes a temporary window after discarding its synthetic edits.</summary>
        /// <param name="window">The fixture window.</param>
        private static void Destroy(MatchCheckpointEditorWindow window)
        {
            window.DiscardChanges();
            UnityEngine.Object.DestroyImmediate(window);
        }

        /// <summary>Reads private native window lifecycle state.</summary>
        /// <typeparam name="T">The expected value type.</typeparam>
        /// <param name="window">The fixture window.</param>
        /// <param name="name">The member name.</param>
        /// <returns>The current value.</returns>
        private static T Read<T>(MatchCheckpointEditorWindow window, string name)
        {
            return (T)typeof(MatchCheckpointEditorWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(window)!;
        }

        /// <summary>Assigns fixture-owned native window lifecycle state.</summary>
        /// <param name="window">The fixture window.</param>
        /// <param name="name">The member name.</param>
        /// <param name="value">The fixture value.</param>
        private static void Write(MatchCheckpointEditorWindow window, string name, object value)
        {
            typeof(MatchCheckpointEditorWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(window, value);
        }

        /// <summary>Balances one native enable/disable callback during serialized recovery.</summary>
        /// <param name="window">The fixture window.</param>
        /// <param name="name">The lifecycle callback.</param>
        private static void Invoke(MatchCheckpointEditorWindow window, string name)
        {
            typeof(MatchCheckpointEditorWindow).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(window, null);
        }
    }
}
