#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Editor;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.Progression;
using PlantsVsZombies.Simulation;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.EditorRendering
{
    /// <summary>Checks native save-window serialization, stale recovery, and passive rendering against isolated saves.</summary>
    public sealed class SaveEditorWindowTests
    {
        /// <summary>Owns every temporary window created during the current test.</summary>
        private readonly List<SaveEditorWindow> _windows = new List<SaveEditorWindow>();

        /// <summary>Stores the unique save directory owned by the current test.</summary>
        private string _directory = string.Empty;

        /// <summary>Identifies the user selected in the game catalog.</summary>
        private Guid _first;

        /// <summary>Identifies a different user selected only in the save editor.</summary>
        private Guid _second;

        /// <summary>Gets the synthetic catalog file path.</summary>
        private string CatalogPath => Path.Combine(_directory, "profiles.dat");

        /// <summary>Gets the synthetic account file path.</summary>
        private string AccountPath => Path.Combine(_directory, "player.account");

        /// <summary>Gets the synthetic adventure file path.</summary>
        private string AdventurePath => Path.Combine(_directory, "campaign.adventure");

        /// <summary>Creates a complete synthetic catalog and sidecars without changing the user's local saves.</summary>
        [SetUp]
        public void SetUp()
        {
            _directory = Path.Combine(Path.GetTempPath(), "pvz-save-window-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            var catalog = new ProfileCatalog(CatalogPath);
            _first = catalog.Create("First").Id;
            _second = catalog.Create("Second").Id;
            catalog.Select(_first);
            var accounts = new PlayerAccountStore(AccountPath);
            var plant = new GardenPlant
            {
                Id = Guid.NewGuid(),
                Kind = PlantKind.Marigold,
                Garden = GardenKind.Greenhouse,
                Slot = 0,
                Growth = 3,
                Waterings = 2,
                FeedingsPerGrow = 4,
                MatureNeed = GardenNeed.Music,
                NextCareUtc = 1800000000,
                PendingCoins = 7
            };
            accounts.Import(_first, 120, new int[80], new int[100], 25, new[] { plant });
            accounts.Import(_second, 240, new int[80], new int[100], 50, Array.Empty<GardenPlant>());
            accounts.SetZenGardenTutorialStage(_first, 3);
            var adventure = new AdventureProgressStore(AdventurePath);
            adventure.Import(_first, 15, 1);
            adventure.Import(_second, 29, 2);
        }

        /// <summary>Discards synthetic drafts before closing native windows and deleting this fixture's directory.</summary>
        [TearDown]
        public void TearDown()
        {
            foreach (var window in _windows)
            {
                DestroyWindow(window);
            }

            _windows.Clear();
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        /// <summary>Checks EditorWindow serialization restores the selected user, draft values, name, and native close warning.</summary>
        [Test]
        public void NativeWindowSerializationRestoresPendingEditsAndUnsavedState()
        {
            var session = new SaveEditorSession(_directory);
            session.SelectProfile(_second);
            session.Account.Coins = 65432;
            session.Account._purchases[21] = 3;
            session.Account._receipts["window:pending"] = 12;
            session.Adventure.AttemptSeed = -765432;
            var source = CreateInjectedWindow(session, "Pending editor name", true, true);
            var catalogBytes = File.ReadAllBytes(CatalogPath);
            var accountBytes = File.ReadAllBytes(AccountPath);
            var adventureBytes = File.ReadAllBytes(AdventurePath);
            var json = EditorJsonUtility.ToJson(source);
            DestroyWindow(source);

            var restored = RestoreWindow(json);

            var restoredSession = ReadField<SaveEditorSession>(restored, "_session");
            Assert.That(restoredSession.DirectoryPath, Is.EqualTo(_directory));
            Assert.That(restoredSession.SelectedProfileId, Is.EqualTo(_second));
            Assert.That(restoredSession.Account.Coins, Is.EqualTo(65432));
            Assert.That(restoredSession.Account.PurchaseCount(21), Is.EqualTo(3));
            Assert.That(restoredSession.Account._receipts["window:pending"], Is.EqualTo(12));
            Assert.That(restoredSession.Adventure.AttemptSeed, Is.EqualTo(-765432));
            Assert.That(ReadField<string>(restored, "_profileName"), Is.EqualTo("Pending editor name"));
            Assert.That(ReadField<bool>(restored, "_accountDirty"), Is.True);
            Assert.That(ReadField<bool>(restored, "_adventureDirty"), Is.True);
            Assert.That(ReadField<bool>(restored, "_requiresReload"), Is.False);
            Assert.That(restored.hasUnsavedChanges, Is.True);
            Assert.That(new ProfileCatalog(CatalogPath).SelectedId, Is.EqualTo(_first));
            Assert.That(File.ReadAllBytes(CatalogPath), Is.EqualTo(catalogBytes));
            Assert.That(File.ReadAllBytes(AccountPath), Is.EqualTo(accountBytes));
            Assert.That(File.ReadAllBytes(AdventurePath), Is.EqualTo(adventureBytes));
        }

        /// <summary>Checks repeated native serialization cannot accept a new baseline for a stale recovered draft.</summary>
        [Test]
        public void StaleRecoveryRemainsBlockedAcrossTwoWindowSerializationCycles()
        {
            var session = new SaveEditorSession(_directory);
            session.Account.Coins = 987;
            session.Account._receipts["window:stale"] = 6;
            session.Adventure.AttemptSeed = -876;
            var source = CreateInjectedWindow(session, "Pending stale name", true, true);
            var originalDraftJson = JsonUtility.ToJson(ReadField<SaveEditorDraft>(source, "_draft"));
            var serialized = EditorJsonUtility.ToJson(source);
            DestroyWindow(source);
            var external = new PlayerAccountStore(AccountPath);
            external.Import(_first, 777, new int[80], new int[100], 25, Array.Empty<GardenPlant>());
            var latestAccountBytes = File.ReadAllBytes(AccountPath);
            var adventureBytes = File.ReadAllBytes(AdventurePath);
            var catalogBytes = File.ReadAllBytes(CatalogPath);

            for (var cycle = 0; cycle < 2; cycle++)
            {
                var recovered = RestoreWindow(serialized);
                var recoveredSession = ReadField<SaveEditorSession>(recovered, "_session");
                Assert.That(ReadField<bool>(recovered, "_requiresReload"), Is.True);
                Assert.That(recovered.hasUnsavedChanges, Is.True);
                Assert.That(recoveredSession.Account.Coins, Is.EqualTo(987));
                Assert.That(recoveredSession.Account._receipts["window:stale"], Is.EqualTo(6));
                Assert.That(recoveredSession.Adventure.AttemptSeed, Is.EqualTo(-876));
                Assert.That(ReadField<string>(recovered, "_profileName"), Is.EqualTo("Pending stale name"));
                Invoke(recovered, "CaptureDraft");
                Assert.That(JsonUtility.ToJson(ReadField<SaveEditorDraft>(recovered, "_draft")), Is.EqualTo(originalDraftJson));
                Assert.Throws<InvalidOperationException>(() => recovered.SaveChanges());
                Assert.That(File.ReadAllBytes(AccountPath), Is.EqualTo(latestAccountBytes));
                Assert.That(File.ReadAllBytes(AdventurePath), Is.EqualTo(adventureBytes));
                Assert.That(File.ReadAllBytes(CatalogPath), Is.EqualTo(catalogBytes));
                serialized = EditorJsonUtility.ToJson(recovered);
                DestroyWindow(recovered);
            }
        }

        /// <summary>Checks a temporarily unreadable catalog retains native unsaved state and can recover its original draft later.</summary>
        /// <param name="missing">Whether the catalog disappears rather than containing invalid bytes during recovery.</param>
        [TestCase(true)]
        [TestCase(false)]
        public void MissingOrCorruptCatalogRetainsDraftUntilOriginalFileIsRestored(bool missing)
        {
            var session = new SaveEditorSession(_directory);
            session.Account.Coins = 963;
            session.Account._receipts["window:unreadable"] = 9;
            session.Adventure.AttemptSeed = -852;
            var source = CreateInjectedWindow(session, "Pending recovery name", true, true);
            var originalDraftJson = JsonUtility.ToJson(ReadField<SaveEditorDraft>(source, "_draft"));
            var serialized = EditorJsonUtility.ToJson(source);
            DestroyWindow(source);
            var catalogBytes = File.ReadAllBytes(CatalogPath);
            var accountBytes = File.ReadAllBytes(AccountPath);
            var adventureBytes = File.ReadAllBytes(AdventurePath);
            var corrupt = new byte[] { 1, 2, 3, 4 };
            if (missing)
            {
                File.Delete(CatalogPath);
            }
            else
            {
                File.WriteAllBytes(CatalogPath, corrupt);
            }

            var blocked = RestoreWindow(serialized);

            Assert.That(ReadField<SaveEditorSession?>(blocked, "_session"), Is.Null);
            Assert.That(ReadField<bool>(blocked, "_requiresReload"), Is.True);
            Assert.That(ReadField<bool>(blocked, "_accountDirty"), Is.True);
            Assert.That(ReadField<bool>(blocked, "_adventureDirty"), Is.True);
            Assert.That(ReadField<string>(blocked, "_profileName"), Is.EqualTo("Pending recovery name"));
            Assert.That(blocked.hasUnsavedChanges, Is.True);
            Assert.That(JsonUtility.ToJson(ReadField<SaveEditorDraft>(blocked, "_draft")), Is.EqualTo(originalDraftJson));
            Assert.Throws<InvalidOperationException>(() => blocked.SaveChanges());
            if (missing)
            {
                Assert.That(File.Exists(CatalogPath), Is.False);
            }
            else
            {
                Assert.That(File.ReadAllBytes(CatalogPath), Is.EqualTo(corrupt));
            }

            var retained = EditorJsonUtility.ToJson(blocked);
            DestroyWindow(blocked);
            File.WriteAllBytes(CatalogPath, catalogBytes);
            var recovered = RestoreWindow(retained);
            var recoveredSession = ReadField<SaveEditorSession>(recovered, "_session");
            Assert.That(ReadField<bool>(recovered, "_requiresReload"), Is.False);
            Assert.That(recovered.hasUnsavedChanges, Is.True);
            Assert.That(recoveredSession.SelectedProfileId, Is.EqualTo(_first));
            Assert.That(recoveredSession.Account.Coins, Is.EqualTo(963));
            Assert.That(recoveredSession.Account._receipts["window:unreadable"], Is.EqualTo(9));
            Assert.That(recoveredSession.Adventure.AttemptSeed, Is.EqualTo(-852));
            Assert.That(ReadField<string>(recovered, "_profileName"), Is.EqualTo("Pending recovery name"));
            Assert.That(File.ReadAllBytes(CatalogPath), Is.EqualTo(catalogBytes));
            Assert.That(File.ReadAllBytes(AccountPath), Is.EqualTo(accountBytes));
            Assert.That(File.ReadAllBytes(AdventurePath), Is.EqualTo(adventureBytes));
        }

        /// <summary>Checks rendering every native tab produces no errors, dirty state, or persistent data changes.</summary>
        /// <returns>The editor updates needed to lay out and repaint all six field categories.</returns>
        [UnityTest]
        public IEnumerator RepaintingEveryTabDoesNotModifyDraftsOrFiles()
        {
            var session = new SaveEditorSession(_directory);
            var window = CreateInjectedWindow(session, session.ProfileName, false, false);
            var initialDraft = JsonUtility.ToJson(SaveEditorDraft.Capture(session, session.ProfileName, false, false));
            var catalogBytes = File.ReadAllBytes(CatalogPath);
            var accountBytes = File.ReadAllBytes(AccountPath);
            var adventureBytes = File.ReadAllBytes(AdventurePath);
            window.position = new Rect(100, 100, 900, 700);
            window.ShowUtility();
            yield return null;

            for (var tab = 0; tab < 6; tab++)
            {
                WriteField(window, "_tab", tab);
                window.Repaint();
                yield return null;
                yield return null;
                Assert.That(ReadField<bool>(window, "_accountDirty"), Is.False, "Account dirtied while drawing tab " + tab);
                Assert.That(ReadField<bool>(window, "_adventureDirty"), Is.False, "Adventure dirtied while drawing tab " + tab);
                Assert.That(window.hasUnsavedChanges, Is.False, "Native close state dirtied while drawing tab " + tab);
                var drawnSession = ReadField<SaveEditorSession>(window, "_session");
                Assert.That(JsonUtility.ToJson(SaveEditorDraft.Capture(drawnSession, drawnSession.ProfileName, false, false)), Is.EqualTo(initialDraft));
            }

            Assert.That(File.ReadAllBytes(CatalogPath), Is.EqualTo(catalogBytes));
            Assert.That(File.ReadAllBytes(AccountPath), Is.EqualTo(accountBytes));
            Assert.That(File.ReadAllBytes(AdventurePath), Is.EqualTo(adventureBytes));
        }

        /// <summary>Creates a native window whose editable state belongs exclusively to synthetic test saves.</summary>
        /// <param name="session">The isolated session to inject after the window's initial enable callback.</param>
        /// <param name="profileName">The pending or unchanged profile name displayed by the window.</param>
        /// <param name="accountDirty">Whether the account draft should require an explicit save.</param>
        /// <param name="adventureDirty">Whether the adventure draft should require an explicit save.</param>
        /// <returns>A tracked native window with a captured recovery snapshot.</returns>
        private SaveEditorWindow CreateInjectedWindow(SaveEditorSession session, string profileName, bool accountDirty, bool adventureDirty)
        {
            var window = ScriptableObject.CreateInstance<SaveEditorWindow>();
            _windows.Add(window);
            Invoke(window, "OnDisable");
            WriteField(window, "_directory", session.DirectoryPath);
            WriteField(window, "_session", session);
            WriteField(window, "_profileName", profileName);
            WriteField(window, "_savedProfileName", session.ProfileName);
            WriteField(window, "_accountDirty", accountDirty);
            WriteField(window, "_adventureDirty", adventureDirty);
            WriteField(window, "_requiresReload", false);
            Invoke(window, "CaptureDraft");
            return window;
        }

        /// <summary>Recreates serialized native window state and invokes one balanced restore callback.</summary>
        /// <param name="json">The previous window's EditorJsonUtility representation.</param>
        /// <returns>A tracked restored window with fresh disk services and recovered detached drafts.</returns>
        private SaveEditorWindow RestoreWindow(string json)
        {
            var window = ScriptableObject.CreateInstance<SaveEditorWindow>();
            _windows.Add(window);
            Invoke(window, "OnDisable");
            WriteField(window, "_session", null);
            EditorJsonUtility.FromJsonOverwrite(json, window);
            Invoke(window, "OnEnable");
            return window;
        }

        /// <summary>Discards a synthetic draft before destroying its native window without a close dialog.</summary>
        /// <param name="window">The tracked window, which may already have been destroyed.</param>
        private static void DestroyWindow(SaveEditorWindow window)
        {
            if (window != null)
            {
                window.DiscardChanges();
                UnityEngine.Object.DestroyImmediate(window);
            }
        }

        /// <summary>Reads a private serialized or transient window field for lifecycle verification.</summary>
        /// <typeparam name="T">The expected field value type.</typeparam>
        /// <param name="window">The native save editor window under test.</param>
        /// <param name="name">The exact private field name.</param>
        /// <returns>The field's current value.</returns>
        /// <exception cref="MissingFieldException">The expected implementation field does not exist.</exception>
        private static T ReadField<T>(SaveEditorWindow window, string name)
        {
            var field = typeof(SaveEditorWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(typeof(SaveEditorWindow).FullName, name);
            return (T)field.GetValue(window)!;
        }

        /// <summary>Injects isolated test state without using native input or invoking user save actions.</summary>
        /// <param name="window">The native save editor window under test.</param>
        /// <param name="name">The exact private field name.</param>
        /// <param name="value">The synthetic field value to assign.</param>
        /// <exception cref="MissingFieldException">The expected implementation field does not exist.</exception>
        private static void WriteField(SaveEditorWindow window, string name, object? value)
        {
            var field = typeof(SaveEditorWindow).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingFieldException(typeof(SaveEditorWindow).FullName, name);
            field.SetValue(window, value);
        }

        /// <summary>Invokes one private lifecycle helper after balancing the automatic editor subscription.</summary>
        /// <param name="window">The native save editor window under test.</param>
        /// <param name="name">The exact zero-argument method name.</param>
        /// <exception cref="MissingMethodException">The expected implementation method does not exist.</exception>
        /// <exception cref="TargetInvocationException">The invoked helper fails during the test.</exception>
        private static void Invoke(SaveEditorWindow window, string name)
        {
            var method = typeof(SaveEditorWindow).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(typeof(SaveEditorWindow).FullName, name);
            method.Invoke(window, null);
        }
    }
}
