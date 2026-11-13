#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;
using PlantsVsZombies.VerifiedData;

namespace PlantsVsZombies.Editor
{
    /// <summary>Identifies complete common battle dependencies without promoting unrelated images sharing a filename prefix.</summary>
    internal static class SpriteAtlasHotDependencies
    {
        /// <summary>The versioned dependency recipe included in the generated source fingerprint.</summary>
        internal static string RecipeIdentity => "CommonBattleDependencies2;" + string.Join(";", s_sharedAnimations)
            + ";" + string.Join(";", s_boardImages) + ";" + string.Join(";", s_sharedParticles);
        /// <summary>The shared battle animations supplementing the first eight verified plant definitions.</summary>
        private static readonly string[] s_sharedAnimations =
        {
            "PeaShooter", "PeaShooterSingle", "Zombie", "Zombie_FlagPole", "Zombie_charred", "Sun", "LawnMower", "PoolCleaner", "RoofCleaner",
            "Coin_silver", "Coin_gold", "Diamond"
        };
        /// <summary>The explicit board UI artwork that must share the common actor page.</summary>
        private static readonly string[] s_boardImages =
        {
            "images/SeedBank.png", "images/SunBank.png", "images/ShovelBank.png", "images/Shovel.png", "images/seeds.png",
            "images/button_left.png", "images/button_middle.png", "images/button_right.png",
            "images/button_down_left.png", "images/button_down_middle.png", "images/button_down_right.png",
            "images/Store_MainMenuButton.png", "images/Store_MainMenuButtonHighlight.png", "images/Store_MainMenuButtonDown.png",
            "images/SeedChooser_Button.png", "images/SeedChooser_Button_Glow.png",
            "images/plantshadow.png", "images/plantshadow2.png", "images/pea_shadows.png", "images/coinbank.png",
            "IMAGE_PROJECTILEPEA", "IMAGE_PROJECTILESNOWPEA", "IMAGE_WHITEPIXEL"
        };
        /// <summary>The effects belonging to common plants, normal zombie damage states, and their detached equipment.</summary>
        private static readonly string[] s_sharedParticles =
        {
            "ZombieArm", "MoweredZombieArm", "ZombieHead", "MoweredZombieHead", "ZombieHeadPool", "ZombieTrafficCone", "ZombiePail",
            "ZombieDoor", "ZombieFlag", "ZombieRise", "Zombie_seaweed", "Powie", "PotatoMine", "PotatoMineRise", "WallnutEatSmall",
            "WallnutEatLarge", "PeaSplat", "SnowPeaSplat", "SnowPeaPuff", "SnowPeaTrail", "Planting", "PlantingPool"
        };

        /// <summary>Collects exact logical image identities referenced by every original clip and known common runtime substitution.</summary>
        /// <param name="root">The original resource directory containing real reanim and particle files.</param>
        /// <param name="imageIdentities">A case-insensitive map from original IDs and paths to opaque logical source identities.</param>
        /// <returns>The complete common image identity set, including existing optional board artwork.</returns>
        /// <exception cref="IOException">An original dependency file cannot be read.</exception>
        /// <exception cref="XmlException">An original dependency file contains malformed or prohibited XML.</exception>
        /// <exception cref="InvalidDataException">A common animation or particle references an absent original image.</exception>
        internal static HashSet<string> Collect(string root, IReadOnlyDictionary<string, string> imageIdentities)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var animations = IndexFiles(Path.Combine(root, "reanim"), ".reanim");
            var wanted = new HashSet<string>(s_sharedAnimations, StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < Math.Min(8, VerifiedDataCatalog.Plants.Count); index++)
            {
                if (VerifiedDataCatalog.TryGetReanimation(VerifiedDataCatalog.Plants[index].ReanimationId, out var definition))
                {
                    wanted.Add(Path.GetFileNameWithoutExtension(definition.Path.Replace('\\', '/')));
                }
            }
            foreach (var animation in wanted)
            {
                if (animations.TryGetValue(animation, out var path))
                {
                    CollectReferences(path, "i", imageIdentities, result);
                }
            }
            foreach (var image in s_boardImages)
            {
                AddOptional(image, imageIdentities, result);
            }
            for (var stage = 1; stage <= 2; stage++)
            {
                AddOptional(AdventureZombieEquipmentRules.HelmImage(1, stage), imageIdentities, result);
                AddOptional(AdventureZombieEquipmentRules.HelmImage(2, stage), imageIdentities, result);
                AddOptional(AdventureZombieEquipmentRules.HelmImage(8, stage), imageIdentities, result);
                AddOptional(AdventureZombieEquipmentRules.ShieldImage(new Zombie { Kind = ZombieKind.ScreenDoor }, stage), imageIdentities, result);
            }
            AddOptional(AdventureZombieFeedbackRules.ArmImage((int)ZombieKind.Normal), imageIdentities, result);
            AddOptional(AdventureZombieFeedbackRules.ArmReplacement((int)ZombieKind.Normal, "Zombie_outerarm_upper"), imageIdentities, result);
            AddOptional(AdventureZombieFeedbackRules.ArmReplacement((int)ZombieKind.Flag, "Zombie_flag"), imageIdentities, result);
            var particles = IndexFiles(Path.Combine(root, "particles"), ".xml");
            foreach (var particle in s_sharedParticles)
            {
                if (particles.TryGetValue(particle, out var path))
                {
                    CollectReferences(path, "Image", imageIdentities, result);
                }
            }
            return result;
        }

        /// <summary>Indexes real dependency filenames while keeping opaque color/mask image identities away from filesystem APIs.</summary>
        /// <param name="directory">The original animation or particle directory.</param>
        /// <param name="extension">The required original file extension.</param>
        /// <returns>The case-insensitive original stem-to-real-file lookup.</returns>
        private static Dictionary<string, string> IndexFiles(string directory, string extension)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!Directory.Exists(directory))
            {
                return result;
            }
            foreach (var path in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                if (Path.GetExtension(path).Equals(extension, StringComparison.OrdinalIgnoreCase))
                {
                    result[Path.GetFileNameWithoutExtension(path)] = path;
                }
            }
            return result;
        }

        /// <summary>Collects all image references, including hidden tracks and frames outside the default idle clip.</summary>
        /// <param name="path">A real original dependency file path.</param>
        /// <param name="element">The original image-reference element name.</param>
        /// <param name="identities">The original ID and path lookup.</param>
        /// <param name="result">The exact common logical image identities to extend.</param>
        /// <exception cref="InvalidDataException">A referenced image has no decoded source.</exception>
        private static void CollectReferences(string path, string element, IReadOnlyDictionary<string, string> identities, HashSet<string> result)
        {
            var document = new XmlDocument { XmlResolver = null };
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using var text = new StringReader("<Root>" + File.ReadAllText(path) + "</Root>");
            using var reader = XmlReader.Create(text, settings);
            document.Load(reader);
            foreach (XmlNode node in document.GetElementsByTagName(element))
            {
                var identifier = node.InnerText.Trim();
                if (identifier.Length == 0)
                {
                    continue;
                }
                if (!identities.TryGetValue(identifier, out var identity))
                {
                    throw new InvalidDataException("Common rendering dependency references a missing image: " + identifier + " in " + path);
                }
                result.Add(identity);
            }
        }

        /// <summary>Includes an existing optional board asset or explicitly selected runtime image substitution.</summary>
        /// <param name="identifier">The original image identifier or relative path, if the rule supplies one.</param>
        /// <param name="identities">The original ID and path lookup.</param>
        /// <param name="result">The common logical identities to extend.</param>
        private static void AddOptional(string? identifier, IReadOnlyDictionary<string, string> identities, HashSet<string> result)
        {
            if (identifier != null && identities.TryGetValue(identifier, out var identity))
            {
                result.Add(identity);
            }
        }
    }
}
