#nullable enable
using System;
using System.IO;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Profiles;
using PlantsVsZombies.Progression;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks that native fertilizer initialization survives an original-save round trip.</summary>
    public sealed class GardenFertilizerExportTests
    {
        /// <summary>Checks both spent fertilizer and an introduction that has not reached the store.</summary>
        /// <param name="initialized">Whether the native fertilizer counter was initialized.</param>
        [TestCase(true)]
        [TestCase(false)]
        public void PreservesEmptyFertilizerMarkerWithoutChangingWallet(bool initialized)
        {
            var directory = Path.Combine(Path.GetTempPath(), "garden-export-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var sourceFile = Path.Combine(directory, "source.dat");
                var exportedFile = Path.Combine(directory, "export.dat");
                var original = new byte[887];
                Array.Copy(BitConverter.GetBytes(12), 0, original, 0, 4);
                Array.Copy(BitConverter.GetBytes(45), 0, original, 4, 4);
                Array.Copy(BitConverter.GetBytes(123), 0, original, 8, 4);
                Array.Copy(BitConverter.GetBytes(initialized ? 1000 : 0), 0, original, 472, 4);
                File.WriteAllBytes(sourceFile, original);
                var source = OriginalSaveFormat.ReadUser(sourceFile);
                Assert.That(source.GardenFertilizerInitialized, Is.EqualTo(initialized));
                var id = Guid.NewGuid();
                var store = new PlayerAccountStore(Path.Combine(directory, "player.account"));
                store.Import(id, source.Coins, source.Purchases, source.ChallengeRecords, 0, source.Plants,
                    gardenGiftClaimed: true, gardenFertilizerInitialized: source.GardenFertilizerInitialized);
                if (!initialized)
                {
                    store.SetZenGardenTutorialStage(id, 3);
                }

                var account = new PlayerAccountStore(Path.Combine(directory, "player.account")).Get(id);
                var progress = new AdventureProgressStore(Path.Combine(directory, "player.adventure"));
                progress.Import(id, 45, 0);
                OriginalSaveFormat.ExportUser(sourceFile, exportedFile, account, progress.Get(id));
                var exported = OriginalSaveFormat.ReadUser(exportedFile);
                Assert.That(exported.Coins, Is.EqualTo(123));
                Assert.That(exported.Purchases[14], Is.Zero);
                Assert.That(exported.GardenFertilizerInitialized, Is.EqualTo(initialized));
                Assert.That(BitConverter.ToInt32(File.ReadAllBytes(exportedFile), 472), Is.EqualTo(initialized ? 1000 : 0));
                if (initialized)
                {
                    Assert.That(exported.GardenGiftClaimed, Is.True);
                }
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }

        /// <summary>Checks the grant records initialization even after all bags are used.</summary>
        [Test]
        public void TutorialGrantPersistsInitialization()
        {
            var directory = Path.Combine(Path.GetTempPath(), "garden-grant-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            try
            {
                var file = Path.Combine(directory, "player.account");
                var id = Guid.NewGuid();
                var store = new PlayerAccountStore(file);
                store.BeginZenGardenTutorial(id);
                store.CompleteZenGardenIntroduction(id);
                store.SetZenGardenTutorialStage(id, 5);
                Assert.That(store.Get(id).GardenFertilizerInitialized, Is.False);
                store.GrantZenGardenTutorialFertilizer(id);
                Assert.That(new PlayerAccountStore(file).Get(id).GardenFertilizerInitialized, Is.True);
            }
            finally
            {
                Directory.Delete(directory, true);
            }
        }
    }
}
