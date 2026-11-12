#nullable enable
using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using NUnit.Framework;
using PlantsVsZombies.Progression;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks durable adventure packet offer choices and legacy account compatibility.</summary>
    public sealed class PacketUpgradeAccountTests
    {
        /// <summary>Checks two declines survive reload and stop subsequent offers.</summary>
        [Test]
        public void DeclinesArePersistentAndLimited()
        {
            var path = Path.Combine(Path.GetTempPath(), "packet-account-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            try
            {
                var file = Path.Combine(path, "player.account");
                var id = Guid.NewGuid();
                var store = new PlayerAccountStore(file);
                store.Import(id, 100, new int[80], new int[100], 0, Array.Empty<GardenPlant>());
                store.ResolveAdventurePacketOffer(id, false);
                store.ResolveAdventurePacketOffer(id, false);
                var reopened = new PlayerAccountStore(file);
                Assert.That(reopened.Get(id).PacketUpgradeDeclines, Is.EqualTo(2));
                Assert.Throws<InvalidOperationException>(() => reopened.ResolveAdventurePacketOffer(id, true));
                Assert.That(reopened.Get(id).Coins, Is.EqualTo(100));
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        /// <summary>Checks an upgrade immediately changes chooser capacity and the saved attempt.</summary>
        [Test]
        public void PurchasedSlotIsAvailableAndRestoredInCurrentAttempt()
        {
            var path = Path.Combine(Path.GetTempPath(), "packet-campaign-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            try
            {
                var id = Guid.NewGuid();
                var accountFile = Path.Combine(path, "player.account");
                var progressFile = Path.Combine(path, "player.adventure");
                var accounts = new PlayerAccountStore(accountFile);
                accounts.Import(id, 100, new int[80], new int[100], 0, Array.Empty<GardenPlant>());
                var progress = new AdventureProgressStore(progressFile);
                progress.Import(id, 13, 0);
                var campaign = new AdventureCampaign(progress, id, accounts);
                var game = campaign.BeginOrResume();
                Assert.That(game.Adventure.Level.PacketSlots, Is.EqualTo(6));
                accounts.ResolveAdventurePacketOffer(id, true);
                campaign.UpdatePacketSlots(accounts.Get(id).PacketSlots);
                var selected = new List<PlantKind>();
                foreach (var seed in game.Adventure.AvailableSeeds)
                {
                    if (selected.Count < 7)
                    {
                        selected.Add(seed);
                    }
                }

                Assert.That(selected.Count, Is.EqualTo(7));
                Assert.That(campaign.ChooseSeeds(selected), Is.True);
                var restored = new AdventureCampaign(new AdventureProgressStore(progressFile), id, new PlayerAccountStore(accountFile)).BeginOrResume();
                Assert.That(restored.Adventure.Level.PacketSlots, Is.EqualTo(7));
                Assert.That(restored.Adventure.SelectedSeeds.Count, Is.EqualTo(7));
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }

        /// <summary>Checks existing version-one accounts remain readable when a new field is added.</summary>
        [Test]
        public void ReadsLegacyVersionOneWithoutNewFields()
        {
            var path = Path.Combine(Path.GetTempPath(), "packet-legacy-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            try
            {
                var file = Path.Combine(path, "player.account");
                var id = Guid.NewGuid();
                var store = new PlayerAccountStore(file);
                store.Import(id, 123, new int[80], new int[100], 0, Array.Empty<GardenPlant>());
                var versionTwo = File.ReadAllBytes(file);
                var bodyLength = versionTwo.Length - 32;
                var versionOneBody = new byte[bodyLength - 5];
                Array.Copy(versionTwo, 0, versionOneBody, 0, 64);
                Array.Copy(versionTwo, 69, versionOneBody, 64, bodyLength - 69);
                Array.Copy(BitConverter.GetBytes(1), 0, versionOneBody, 8, 4);
                using var sha = SHA256.Create();
                using (var output = new FileStream(file, FileMode.Create, FileAccess.Write))
                {
                    output.Write(versionOneBody, 0, versionOneBody.Length);
                    var checksum = sha.ComputeHash(versionOneBody);
                    output.Write(checksum, 0, checksum.Length);
                }

                var account = new PlayerAccountStore(file).Get(id);
                Assert.That(account.Coins, Is.EqualTo(123));
                Assert.That(account.PacketUpgradeDeclines, Is.Zero);
                Assert.That(account.TreeFoodGiftClaimed, Is.True);
            }
            finally
            {
                Directory.Delete(path, true);
            }
        }
    }
}
