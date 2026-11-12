#nullable enable

using System.Linq;
using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks the original zombie equipment durability and the Grave Buster eating sequence.</summary>
    public sealed class AdventureEquipmentAndGraveBusterTests
    {
        /// <summary>Checks the starting equipment health matches Zombie::ZombieInitialize for every equipped kind.</summary>
        /// <param name="kind">The zombie kind.</param>
        /// <param name="armor">The expected helm health.</param>
        /// <param name="shield">The expected shield health.</param>
        [TestCase(ZombieKind.Normal, 0f, 0f)]
        [TestCase(ZombieKind.Conehead, 370f, 0f)]
        [TestCase(ZombieKind.Buckethead, 1100f, 0f)]
        [TestCase(ZombieKind.Football, 1400f, 0f)]
        [TestCase(ZombieKind.Digger, 100f, 0f)]
        [TestCase(ZombieKind.Bobsled, 300f, 0f)]
        [TestCase(ZombieKind.ScreenDoor, 0f, 1100f)]
        [TestCase(ZombieKind.Newspaper, 0f, 150f)]
        [TestCase(ZombieKind.Ladder, 0f, 500f)]
        public void StartingEquipmentMatchesOriginalInitialization(ZombieKind kind, float armor, float shield)
        {
            Assert.That(AdventureZombieEquipmentRules.StartingArmor(kind), Is.EqualTo(armor));
            Assert.That(AdventureZombieEquipmentRules.StartingShield(kind), Is.EqualTo(shield));
        }

        /// <summary>Checks a football zombie shows its helmet only while the helm health remains.</summary>
        [Test]
        public void FootballHelmetTrackFollowsHelmHealth()
        {
            var zombie = new Zombie { Kind = ZombieKind.Football, ArmorHealth = AdventureZombieEquipmentRules.StartingArmor(ZombieKind.Football) };
            Assert.That(AdventureZombieEquipmentRules.TrackVisible(zombie, "zombie_football_helmet"), Is.True);
            Assert.That(AdventureZombieEquipmentRules.TrackVisible(zombie, "anim_hair"), Is.False);
            zombie.ArmorHealth = 0f;
            Assert.That(AdventureZombieEquipmentRules.TrackVisible(zombie, "zombie_football_helmet"), Is.False);
        }

        /// <summary>Checks the production spawn path gives a football zombie the original 1400 helm and 270 body health.</summary>
        [Test]
        public void SpawnedFootballZombieHasOriginalDurability()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 11 }, 7529);
            var zombie = (Zombie)typeof(GameSimulation).GetMethod("SpawnAdventureZombie", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { ZombieKind.Football, 2, 6.5f, 0 })!;
            Assert.That(zombie.ArmorHealth, Is.EqualTo(1400f));
            Assert.That(zombie.Health, Is.EqualTo(270f));
        }

        /// <summary>Checks the eating state, countdown and sink offset derived from the plant timer.</summary>
        [Test]
        public void GraveBusterTimerMapsToOriginalCountdownAndSink()
        {
            Assert.That(AdventureGraveBusterRules.IsEating(AdventureGraveBusterRules.TotalSeconds), Is.False);
            Assert.That(AdventureGraveBusterRules.SinkOffset(AdventureGraveBusterRules.TotalSeconds), Is.Zero);
            Assert.That(AdventureGraveBusterRules.IsEating(AdventureGraveBusterRules.EatSeconds), Is.True);
            Assert.That(AdventureGraveBusterRules.EatingTicksRemaining(AdventureGraveBusterRules.EatSeconds), Is.EqualTo(400));
            Assert.That(AdventureGraveBusterRules.SinkOffset(2f), Is.EqualTo(15f).Within(.001f));
            Assert.That(AdventureGraveBusterRules.SinkOffset(0f), Is.EqualTo(30f).Within(.001f));
        }

        /// <summary>Checks planting, eating and finishing create the original sound serial and both particle systems.</summary>
        [Test]
        public void GraveBusterPlaysChompThenRocksThenDirtBurst()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 11 }, 7529);
            game.Adventure.GravesEnabled = true;
            typeof(AdventureBoard).GetProperty("Phase")!.GetSetMethod(true)!.Invoke(game.Adventure, new object[] { AdventurePhase.Playing });
            var grave = game.Adventure.Graves[0];
            var plant = (Plant)typeof(GameSimulation).GetMethod("PlaceAdventurePlant", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { PlantKind.GraveBuster, grave.Row, grave.Column })!;
            Assert.That(game.Presentation.GraveBusterChompSoundSerial, Is.EqualTo(1));
            Assert.That(game.Presentation.Explosions.Exists(e => e.Effect == AdventureExplosionEffect.GraveBuster), Is.False);

            for (var tick = 0; tick < 70; tick++)
            {
                game.Update(.01f);
            }

            Assert.That(game.Presentation.Explosions.FindAll(e => e.Effect == AdventureExplosionEffect.GraveBuster), Has.Count.EqualTo(1));
            Assert.That(plant.GraveBusterEatingStarted, Is.True);
            Assert.That(game.Plants.Contains(plant), Is.True);

            for (var tick = 0; tick < 450; tick++)
            {
                game.Update(.01f);
            }

            Assert.That(game.Plants.Contains(plant), Is.False);
            Assert.That(game.Adventure.Graves.Any(c => c.Row == grave.Row && c.Column == grave.Column), Is.False);
            Assert.That(game.Presentation.Explosions.FindAll(e => e.Effect == AdventureExplosionEffect.GraveBusterDie), Has.Count.EqualTo(1));
            // The attached rock system lasts exactly the 400 eating ticks, so it has expired with the plant.
            Assert.That(game.Presentation.Explosions.Exists(e => e.Effect == AdventureExplosionEffect.GraveBuster), Is.False);
        }
    }
}
