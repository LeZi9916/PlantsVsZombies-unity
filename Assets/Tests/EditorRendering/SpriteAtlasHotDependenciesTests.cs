#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PlantsVsZombies.Editor;
using UnityEngine;

namespace PlantsVsZombies.Tests.EditorRendering
{
    /// <summary>Checks full original clip closures include shared and replacement images without filename-prefix overreach.</summary>
    public sealed class SpriteAtlasHotDependenciesTests
    {
        /// <summary>Checks every frame is collected while unrelated similarly named sources remain cold.</summary>
        [Test]
        public void FullClipReferencesAndOpaqueAlphaPairsAreCollectedWithoutPromotingColdPrefixes()
        {
            var root = Path.Combine(Application.temporaryCachePath, "PvZHotClosure-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.Combine(root, "reanim"));
            File.WriteAllText(Path.Combine(root, "reanim", "Zombie.reanim"),
                "<fps>12</fps><track><name>all</name><t><f>-1</f><i>IMAGE_HEAD</i></t><t><i>IMAGE_DAMAGE</i></t></track>");
            File.WriteAllText(Path.Combine(root, "reanim", "SunFlower.reanim"),
                "<fps>12</fps><track><name>shared</name><t><i>IMAGE_SHARED_STEM</i></t></track>");
            File.WriteAllText(Path.Combine(root, "reanim", "Coin_silver.reanim"),
                "<fps>12</fps><track><name>spin</name><t><i>IMAGE_COIN</i></t></track>");
            File.WriteAllText(Path.Combine(root, "reanim", "Diamond.reanim"),
                "<fps>12</fps><track><name>shine</name><t><i>IMAGE_DIAMOND_SHINE</i></t></track>");
            var identities = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["IMAGE_HEAD"] = "reanim/Zombie_head.png",
                ["IMAGE_DAMAGE"] = "images/color.jpg|images/mask.png",
                ["IMAGE_SHARED_STEM"] = "reanim/PEASHOOTER_STEM.png",
                ["images/ShovelBank.png"] = "images/ShovelBank.png",
                ["images/SeedChooser_Button_Glow.png"] = "images/SeedChooser_Button_Glow.png",
                ["IMAGE_COIN"] = "reanim/SILVERCOIN.png",
                ["IMAGE_DIAMOND_SHINE"] = "reanim/DIAMOND_SHINE6.png",
                ["IMAGE_REANIM_ZOMBIE_CONE2"] = "reanim/Zombie_cone2.png",
                ["IMAGE_REANIM_ZOMBIE_HAND_ROCK"] = "reanim/Zombie_hand_rock.png",
                ["images/Sunflower_trophy.png"] = "images/Sunflower_trophy.png",
                ["images/SelectorScreen_Shadow_Adventure.png"] = "images/SelectorScreen_Shadow_Adventure.png"
            };
            try
            {
                var common = SpriteAtlasHotDependencies.Collect(root, identities);
                Assert.That(common, Does.Contain("reanim/Zombie_head.png"));
                Assert.That(common, Does.Contain("images/color.jpg|images/mask.png"));
                Assert.That(common, Does.Contain("reanim/PEASHOOTER_STEM.png"));
                Assert.That(common, Does.Contain("images/ShovelBank.png"));
                Assert.That(common, Does.Contain("images/SeedChooser_Button_Glow.png"));
                Assert.That(common, Does.Contain("reanim/SILVERCOIN.png"));
                Assert.That(common, Does.Contain("reanim/DIAMOND_SHINE6.png"));
                Assert.That(common, Does.Contain("reanim/Zombie_cone2.png"));
                Assert.That(common, Does.Not.Contain("reanim/Zombie_hand_rock.png"));
                Assert.That(common, Does.Not.Contain("images/Sunflower_trophy.png"));
                Assert.That(common, Does.Not.Contain("images/SelectorScreen_Shadow_Adventure.png"));
            }
            finally
            {
                Directory.Delete(root, true);
            }
        }
    }
}
