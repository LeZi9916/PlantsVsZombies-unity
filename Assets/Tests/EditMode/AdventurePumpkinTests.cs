#nullable enable

using System.Reflection;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.BoardUI;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks original pumpkin collision width, shell protection, integer repair thresholds and damage artwork.</summary>
    public sealed class AdventurePumpkinTests
    {
        /// <summary>Checks PC PlantInit's 120-pixel width becomes a 100-by-80 GetPlantRect on each board type.</summary>
        /// <param name="level">The original adventure stage selecting terrain.</param>
        /// <param name="y">The native collision origin, including the roof slope.</param>
        [TestCase(11, 280)]
        [TestCase(31, 250)]
        [TestCase(41, 280)]
        public void OriginalCollisionRectangleAndTwentyPixelBiteBoundary(int level, int y)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = level }, 91);
            var plant = new Plant { Kind = PlantKind.Pumpkin, Row = 2, Column = 3, Health = 4000f, Layer = PlantLayer.Armor };
            var box = game.AdventurePlantEatingRectangle(plant);
            Assert.That(box.X, Is.EqualTo(280));
            Assert.That(box.Y, Is.EqualTo(y));
            Assert.That(box.Width, Is.EqualTo(100));
            Assert.That(box.Height, Is.EqualTo(80));
            Assert.That(AdventureZombieBiteRules.Reaches(new NativeRect(360, y, 50, 115), box), Is.True);
            Assert.That(AdventureZombieBiteRules.Reaches(new NativeRect(361, y, 50, 115), box), Is.False);
        }

        /// <summary>Checks the armor layer takes chewing damage before its contained normal plant.</summary>
        [Test]
        public void ShellProtectsContainedPlantUntilItIsRemoved()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 31 }, 91);
            game.ChooseDefaultAdventureSeeds();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var place = typeof(GameSimulation).GetMethod("PlaceAdventurePlant", flags)!;
            var main = (Plant)place.Invoke(game, new object[] { PlantKind.Peashooter, 1, 4 })!;
            var shell = (Plant)place.Invoke(game, new object[] { PlantKind.Pumpkin, 1, 4 })!;
            var zombie = (Zombie)typeof(GameSimulation).GetMethod("SpawnAdventureBoardZombie", flags)!
                .Invoke(game, new object[] { ZombieKind.Normal, 1, 5f, 0 })!;
            var find = typeof(GameSimulation).GetMethod("FindAdventureNativePlantTarget", flags)!;
            Assert.That(find.Invoke(game, new object[] { zombie, AdventureBiteAttack.Chew }), Is.SameAs(shell));
            var advance = typeof(GameSimulation).GetMethod("AdvanceAdventureZombies", flags)!;
            for (var tick = 0; tick < 200 && shell.Health == 4000f; tick++)
            {
                advance.Invoke(game, new object[] { .01f });
            }

            Assert.That(shell.Health, Is.EqualTo(3996f));
            Assert.That(main.Health, Is.EqualTo(300f));
            game.Plants.Remove(shell);
            Assert.That(find.Invoke(game, new object[] { zombie, AdventureBiteAttack.Chew }), Is.SameAs(main));
        }

        /// <summary>Checks original aerial attack priority regardless of shell/body creation order.</summary>
        /// <param name="kind">The original aerial attacker.</param>
        /// <param name="shellFirst">Whether the shell was planted before its protected body.</param>
        [TestCase(ZombieKind.Bungee, false)]
        [TestCase(ZombieKind.Bungee, true)]
        [TestCase(ZombieKind.Catapult, false)]
        [TestCase(ZombieKind.Catapult, true)]
        public void AerialAttacksSelectTheNormalPlantBeforeTheShell(ZombieKind kind, bool shellFirst)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 31 }, 91);
            var main = new Plant { Id = 1, Kind = PlantKind.Peashooter, Layer = PlantLayer.Main, Row = 1, Column = 4, Health = 300f };
            var shell = new Plant { Id = 2, Kind = PlantKind.Pumpkin, Layer = PlantLayer.Armor, Row = 1, Column = 4, Health = 4000f };
            game.Plants.Add(shellFirst ? shell : main);
            game.Plants.Add(shellFirst ? main : shell);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var zombie = (Zombie)typeof(GameSimulation).GetMethod("SpawnAdventureBoardZombie", flags)!
                .Invoke(game, new object[] { kind, 1, 7f, 0 })!;
            zombie.TargetColumn = 4;
            zombie.AbilityTimer = 0f;
            zombie.DropRemaining = 0f;
            if (kind == ZombieKind.Bungee)
            {
                zombie.BungeeState!.Enter(6, 1);
                zombie.PresentationNativePhase = 6;
                zombie.PresentationAltitude = 0f;
            }

            typeof(GameSimulation).GetMethod("AdvanceAdventureZombies", flags)!.Invoke(game, new object[] { .01f });
            Assert.That(shell.Health, Is.EqualTo(4000f));
            Assert.That(game.Plants.Contains(shell), Is.True);
            if (kind == ZombieKind.Bungee)
            {
                Assert.That(zombie.BungeeState!.TargetPlant, Is.SameAs(main));
                Assert.That(main.BungeeState, Is.EqualTo(1));
            }
            else
            {
                Assert.That(main.Health, Is.EqualTo(300f), "The catapult must launch a real ball before dealing damage.");
                for (var tick = 0; tick < 300 && main.Health == 300f; tick++)
                {
                    typeof(GameSimulation).GetMethod("AdventurePresentationTick", flags)!.Invoke(game, new object[] { .01f });
                    typeof(GameSimulation).GetMethod("AdvanceAdventureZombies", flags)!.Invoke(game, new object[] { .01f });
                    typeof(GameSimulation).GetMethod("AdvanceAdventureProjectiles", flags)!.Invoke(game, new object[] { .01f, true });
                }

                Assert.That(main.Health, Is.EqualTo(225f));
                Assert.That(shell.Health, Is.EqualTo(4000f));
            }
        }

        /// <summary>Checks the original lift replaces a stolen Cattail with a Lily Pad to retain its pumpkin.</summary>
        [Test]
        public void StolenCattailRestoresThePumpkinsLilyPadSupport()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 31 }, 91);
            var cattail = new Plant { Id = 1, Kind = PlantKind.Cattail, Layer = PlantLayer.Main, Row = 2, Column = 4, Health = 300f };
            var shell = new Plant { Id = 2, Kind = PlantKind.Pumpkin, Layer = PlantLayer.Armor, Row = 2, Column = 4, Health = 4000f };
            game.Plants.Add(cattail);
            game.Plants.Add(shell);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var zombie = (Zombie)typeof(GameSimulation).GetMethod("SpawnAdventureBoardZombie", flags)!
                .Invoke(game, new object[] { ZombieKind.Bungee, 2, 7f, 0 })!;
            zombie.TargetColumn = 4;
            zombie.AbilityTimer = 0f;
            zombie.DropRemaining = 0f;
            zombie.BungeeState!.Enter(7);
            zombie.BungeeState.TargetPlant = cattail;
            zombie.BungeeState.AnimationSeconds = 15f / 24f;
            cattail.BungeeState = 1;
            zombie.PresentationNativePhase = 7;
            zombie.PresentationAltitude = 0f;
            typeof(GameSimulation).GetMethod("AdvanceAdventureZombies", flags)!.Invoke(game, new object[] { .01f });
            Assert.That(cattail.BungeeState, Is.EqualTo(2));
            Assert.That(game.AdventurePlantAt(2, 4, PlantLayer.Support)?.Kind, Is.EqualTo(PlantKind.LilyPad));
            Assert.That(game.Plants.Contains(shell), Is.True);
        }

        /// <summary>Checks an empty ground pumpkin blocks planting a flower pot underneath it.</summary>
        [Test]
        public void EmptyShellRetainsOriginalSupportPlacementRestriction()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 31 }, 91);
            game.ChooseDefaultAdventureSeeds();
            var shell = new Plant { Kind = PlantKind.Pumpkin, Row = 1, Column = 4, Layer = PlantLayer.Armor, Health = 4000f };
            game.Plants.Add(shell);
            var method = typeof(GameSimulation).GetMethod("CheckAdventurePlanting", BindingFlags.Instance | BindingFlags.NonPublic)!;
            Assert.That(method.Invoke(game, new object[] { PlantKind.FlowerPot, 1, 4, true }), Is.EqualTo(AdventurePlantingFailure.Occupied));
        }

        /// <summary>Checks a pumpkin does not turn the original whole-cell crush into a shell-only hit.</summary>
        /// <param name="kind">The original crushing zombie.</param>
        [TestCase(ZombieKind.Gargantuar)]
        [TestCase(ZombieKind.Zomboni)]
        [TestCase(ZombieKind.Catapult)]
        public void CrushingDestroysTheShellAndItsContainedPlantTogether(ZombieKind kind)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 31 }, 91);
            var main = new Plant { Id = 1, Kind = PlantKind.Peashooter, Layer = PlantLayer.Main, Row = 1, Column = 4, Health = 300f };
            var shell = new Plant { Id = 2, Kind = PlantKind.Pumpkin, Layer = PlantLayer.Armor, Row = 1, Column = 4, Health = 4000f };
            game.Plants.Add(main);
            game.Plants.Add(shell);
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var zombie = (Zombie)typeof(GameSimulation).GetMethod("SpawnAdventureBoardZombie", flags)!
                .Invoke(game, new object[] { kind, 1, 4.6f, 0 })!;
            zombie.AbilityPhase = 12;
            zombie.BiteTimer = 0f;
            typeof(GameSimulation).GetMethod("AdvanceAdventureZombies", flags)!.Invoke(game, new object[] { .01f });
            Assert.That(game.Plants.Contains(shell), Is.False);
            Assert.That(game.Plants.Contains(main), Is.False);
        }

        /// <summary>Checks native integer thirds for repair eligibility and front-only damage images.</summary>
        /// <param name="health">The shell's current integer health.</param>
        /// <param name="repair">Whether Wall-nut First Aid permits replacement.</param>
        /// <param name="image">The original front image override, or null for intact artwork.</param>
        [TestCase(4000, false, null)]
        [TestCase(2666, false, null)]
        [TestCase(2665, true, "IMAGE_REANIM_PUMPKIN_DAMAGE1")]
        [TestCase(1333, true, "IMAGE_REANIM_PUMPKIN_DAMAGE1")]
        [TestCase(1332, true, "IMAGE_REANIM_PUMPKIN_DAMAGE3")]
        public void RepairAndDamageRespectOriginalIntegerThirds(int health, bool repair, string? image)
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 31, AdventureFirstAid = true }, 91);
            var shell = new Plant { Kind = PlantKind.Pumpkin, Row = 1, Column = 4, Layer = PlantLayer.Armor, Health = health };
            game.Plants.Add(shell);
            var method = typeof(GameSimulation).GetMethod("IsAdventureFirstAid", BindingFlags.Instance | BindingFlags.NonPublic)!;
            Assert.That(method.Invoke(game, new object[] { PlantKind.Pumpkin, 1, 4 }), Is.EqualTo(repair));
            Assert.That(AdventureDefensivePlantRules.DamageTrack(PlantKind.Pumpkin), Is.EqualTo("Pumpkin_front"));
            Assert.That(AdventureDefensivePlantRules.DamageImage(PlantKind.Pumpkin, health), Is.EqualTo(image));
        }
    }
}
