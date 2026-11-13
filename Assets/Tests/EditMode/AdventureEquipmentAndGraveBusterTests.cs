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

        /// <summary>Regresses PC DieWithLoot's DropShield(1) when a piercing hit kills an intact shield carrier.</summary>
        /// <param name="kind">One of the original three shield carriers.</param>
        /// <param name="particle">The PC detached shield particle identifier.</param>
        [TestCase(ZombieKind.ScreenDoor, 12)]
        [TestCase(ZombieKind.Newspaper, 13)]
        [TestCase(ZombieKind.Ladder, 54)]
        public void PiercingBodyDeathDetachesHeldShieldOnce(ZombieKind kind, int particle)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 11 }, 7529);
            var zombie = SpawnShieldCarrier(game, kind);
            var clip = game.AdventurePresentationZombieAnimation(zombie);
            zombie.PresentationAnimationAge = .35f;
            Hit(game, zombie, 1000f, true);
            Assert.That(game.Zombies.Contains(zombie), Is.False);
            Assert.That(zombie.ShieldHealth, Is.Zero);
            var drop = game.Presentation.ZombiePartDrops.Single(part => part.ParticleId == particle);
            Assert.That(drop.Anchor, Is.EqualTo(AdventureZombieEquipmentRules.ShieldTrack(zombie)));
            Assert.That(drop.Animation, Is.EqualTo(clip), "The detached object retains the held pose before the death clip starts.");
            Assert.That(drop.AnimationAge, Is.EqualTo(.35f));
            Assert.That(drop.Duration, Is.EqualTo(.4f));
            Assert.That(game.Presentation.Corpses, Has.Count.EqualTo(1));
            Assert.That(AdventureZombieEquipmentRules.TrackVisible(zombie, drop.Anchor!), Is.False);
            Assert.That(game.Presentation.ZombieSounds.Any(sound => sound.Foley == 67), Is.False,
                "PC death passes bit 1 and does not play the newspaper rip or surprise.");
            Assert.That(zombie.PresentationNewspaperGaspRemaining, Is.Zero);
            Hit(game, zombie, 1000f, true);
            Assert.That(game.Presentation.ZombiePartDrops.Count(part => part.ParticleId == particle), Is.EqualTo(1));
        }

        /// <summary>Preserves ordinary breakage while preventing a later body death from emitting the same shield twice.</summary>
        /// <param name="kind">One of the original three shield carriers.</param>
        [TestCase(ZombieKind.ScreenDoor)]
        [TestCase(ZombieKind.Newspaper)]
        [TestCase(ZombieKind.Ladder)]
        public void BrokenShieldDoesNotDetachAgainAtBodyDeath(ZombieKind kind)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 11 }, 7529);
            var zombie = SpawnShieldCarrier(game, kind);
            var particle = AdventureZombieEquipmentRules.ShieldParticle(zombie);
            var bodyHealth = zombie.Health;
            Hit(game, zombie, zombie.ShieldHealth, false);
            Assert.That(zombie.Health, Is.EqualTo(bodyHealth));
            Assert.That(game.Presentation.ZombiePartDrops.Count(part => part.ParticleId == particle), Is.EqualTo(1));
            if (kind == ZombieKind.Newspaper)
            {
                Assert.That(zombie.PresentationNewspaperGaspRemaining, Is.GreaterThan(0f));
                Assert.That(game.Presentation.ZombieSounds.Any(sound => sound.Foley == 67), Is.True);
            }

            Hit(game, zombie, 1000f, true);
            Assert.That(game.Presentation.ZombiePartDrops.Count(part => part.ParticleId == particle), Is.EqualTo(1));
        }

        /// <summary>Preserves the PC mower's DropShield(0) path, including the newspaper rip.</summary>
        /// <param name="kind">One of the original three shield carriers.</param>
        [TestCase(ZombieKind.ScreenDoor)]
        [TestCase(ZombieKind.Newspaper)]
        [TestCase(ZombieKind.Ladder)]
        public void MowerDetachesRemainingShield(ZombieKind kind)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 11 }, 7529);
            var zombie = SpawnShieldCarrier(game, kind);
            zombie.PresentationDeath = AdventureZombieDeathPresentation.Mowed;
            typeof(GameSimulation).GetMethod("KillAdventureZombie", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { zombie, 0U });
            Assert.That(zombie.ShieldHealth, Is.Zero);
            Assert.That(game.Presentation.ZombiePartDrops.Count(part => part.ParticleId == AdventureZombieEquipmentRules.ShieldParticle(zombie)), Is.EqualTo(1));
            Assert.That(game.Presentation.ZombieSounds.Any(sound => sound.Foley == 67), Is.EqualTo(kind == ZombieKind.Newspaper));
        }

        /// <summary>Keeps ash, swallowed, removed, and no-body-part deaths outside the normal held-shield detachment path.</summary>
        /// <param name="death">The original special death presentation.</param>
        /// <param name="flags">The native damage flags passed to death.</param>
        [TestCase(AdventureZombieDeathPresentation.Burned, 0U)]
        [TestCase(AdventureZombieDeathPresentation.Eaten, 0U)]
        [TestCase(AdventureZombieDeathPresentation.Removed, 0U)]
        [TestCase(AdventureZombieDeathPresentation.Normal, 16U)]
        public void SpecialDeathsDoNotAddNormalShieldDrops(AdventureZombieDeathPresentation death, uint flags)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 11 }, 7529);
            var zombie = SpawnShieldCarrier(game, ZombieKind.ScreenDoor);
            zombie.PresentationDeath = death;
            zombie.Health = 0f;
            typeof(GameSimulation).GetMethod("KillAdventureZombie", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { zombie, flags });
            Assert.That(game.Presentation.ZombiePartDrops, Is.Empty);
        }

        /// <summary>Creates one production shield carrier at an explicit combat location.</summary>
        /// <param name="game">The isolated adventure simulation receiving the zombie.</param>
        /// <param name="kind">The original shield carrier to initialize.</param>
        /// <returns>The spawned live zombie with its original body and shield health.</returns>
        private static Zombie SpawnShieldCarrier(GameSimulation game, ZombieKind kind)
        {
            return (Zombie)typeof(GameSimulation).GetMethod("SpawnAdventureZombie", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { kind, 2, 4f, 0 })!;
        }

        /// <summary>Applies the actual production hit path without bypassing its death and part transitions.</summary>
        /// <param name="game">The simulation containing the live zombie.</param>
        /// <param name="zombie">The shield carrier receiving the hit.</param>
        /// <param name="damage">The native damage before shield absorption.</param>
        /// <param name="piercing">Whether the hit bypasses held shields.</param>
        private static void Hit(GameSimulation game, Zombie zombie, float damage, bool piercing)
        {
            typeof(GameSimulation).GetMethod("HitAdventureZombie", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { zombie, damage, piercing, 0U });
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
