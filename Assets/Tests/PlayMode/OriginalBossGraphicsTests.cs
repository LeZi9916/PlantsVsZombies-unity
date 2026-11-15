#nullable enable

using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using NUnit.Framework;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Checks original boss source ranges, live scene order, attachment placement and fire/ice rendering.</summary>
    public sealed class OriginalBossGraphicsTests
    {
        /// <summary>Checks every original body marker against simulation frame counts, including the missing entrance and death ranges.</summary>
        [Test]
        public void EveryBossActionKeepsItsOriginalSourceFrameCount()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(50);
            var root = XElement.Parse("<r>" + File.ReadAllText(Path.Combine(fixture.Root, "reanim", "Zombie_boss.reanim")) + "</r>");
            foreach (var track in root.Elements("track"))
            {
                var name = track.Element("name")!.Value;
                if (!name.StartsWith("anim_", StringComparison.Ordinal))
                {
                    continue;
                }
                var state = new AdventureBossState { Animation = name };
                if (name.StartsWith("anim_spawn_", StringComparison.Ordinal))
                {
                    state.TargetRow = name[name.Length - 1] - '1';
                }
                if (name.StartsWith("anim_stomp_", StringComparison.Ordinal))
                {
                    state.TargetRow = name[name.Length - 1] - '1';
                }
                if (name.StartsWith("anim_head_attack_", StringComparison.Ordinal))
                {
                    state.BallRow = name[name.Length - 1] - '1';
                }
                Assert.That(fixture.Animations.TryGetNativeAnimationInfo("Zombie_boss", name, out var info), Is.True);
                Assert.That(state.FrameCount(), Is.EqualTo(info.Count), name);
            }
        }

        /// <summary>Checks the actual adventure roof preview contains no boss and the entrance uses the PC body overlay.</summary>
        /// <returns>Original production board camera frames before and after PlayBossEnter.</returns>
        [UnityTest]
        public IEnumerator RoofPreviewIsEmptyAndEntranceUsesOriginalBodyOrigin()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(50);
            fixture.Owner._game = new GameSimulation(new GameRules { AdventureLevel = 50, DeferAdventureBossEntrance = true }, 7529);
            yield return fixture.Capture("DrawAdventureBoard", wholeBoard: true);
            Assert.That(Pool(fixture).SubmittedSprites.Any(sprite => sprite.SourceImage!.SourceIdentity.IndexOf("Zombie_boss", StringComparison.OrdinalIgnoreCase) >= 0), Is.False);
            fixture.Owner.RequiredGame.BeginAdventureBossEntrance();
            var boss = fixture.Owner.RequiredGame.Adventure.Boss!;
            boss.AnimationSeconds = 1.2f;
            boss.Time = .36f;
            yield return fixture.Capture("DrawAdventureBoard", wholeBoard: true);
            var sprites = Pool(fixture).SubmittedSprites;
            var part = sprites.First(sprite => sprite.GameObject != null && sprite.GameObject.name.StartsWith("Boss_outerleg", StringComparison.Ordinal));
            var xml = XElement.Parse("<r>" + File.ReadAllText(Path.Combine(fixture.Root, "reanim", "Zombie_boss.reanim")) + "</r>");
            var definition = xml.Elements("track").First(track => track.Element("name")!.Value == part.GameObject!.name);
            var x = 0f;
            var y = 0f;
            var from = Vector2.zero;
            var to = Vector2.zero;
            var frame = 39f * .36f;
            var index = 0;
            foreach (var transform in definition.Elements("t"))
            {
                if (transform.Element("x") != null)
                {
                    x = float.Parse(transform.Element("x")!.Value, System.Globalization.CultureInfo.InvariantCulture);
                }
                if (transform.Element("y") != null)
                {
                    y = float.Parse(transform.Element("y")!.Value, System.Globalization.CultureInfo.InvariantCulture);
                }
                if (index == (int)frame)
                {
                    from = new Vector2(x, y);
                }
                if (index++ == (int)frame + 1)
                {
                    to = new Vector2(x, y);
                    break;
                }
            }
            var position = Vector2.Lerp(from, to, frame - (int)frame) + new Vector2(15f, -8f);
            var scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
            var expected = new Vector2((Screen.width - 800f * scale) * .5f, (Screen.height - 600f * scale) * .5f) + position * scale;
            Assert.That(part.Vertices[0].x, Is.EqualTo(expected.x).Within(.001f));
            Assert.That(-part.Vertices[0].y, Is.EqualTo(expected.y).Within(.001f));
            Assert.That(sprites.Any(sprite => sprite.SourceImage!.SourceIdentity.IndexOf("bossdriver", StringComparison.OrdinalIgnoreCase) >= 0), Is.False,
                "PC mDontDrawIfParentHidden hides the driver throughout entry.");
        }

        /// <summary>Checks the original separated back/front layers surround a summoned zombie and the driver is drawn only once after Boss_head2.</summary>
        /// <returns>Production source-art board camera frames at summon and exposed-head poses.</returns>
        [UnityTest]
        public IEnumerator SummoningLayersAndDriverAttachmentFollowOriginalTrackOrder()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(50);
            var boss = fixture.Owner.RequiredGame.Adventure.Boss!;
            boss.TargetRow = 2;
            boss.Enter(80, "anim_spawn_3", 12f, 0);
            boss.AnimationSeconds = 1.15f;
            boss.Time = 1.15f * 12f / 21f;
            SimulationFixture.Zombies(fixture.Owner.RequiredGame).Add(new Zombie { Id = 99, Kind = ZombieKind.Normal, Row = 2,
                X = AdventureZombieNativeRules.GridX(600f), Health = 270f, PresentationAnimationAge = .3f });
            yield return fixture.Capture("DrawAdventureBoard", wholeBoard: true);
            var sprites = Pool(fixture).SubmittedSprites.ToArray();
            var back = Array.FindIndex(sprites, sprite => sprite.GameObject != null && sprite.GameObject.name.StartsWith("Boss_innerleg", StringComparison.Ordinal));
            var zombie = Array.FindIndex(sprites, sprite => sprite.GameObject != null && sprite.GameObject.name.StartsWith("Zombie_", StringComparison.Ordinal));
            var front = Array.FindIndex(sprites, sprite => sprite.GameObject != null && sprite.GameObject.name.StartsWith("Boss_outerleg", StringComparison.Ordinal));
            Assert.That(back, Is.GreaterThanOrEqualTo(0));
            Assert.That(zombie, Is.GreaterThan(back));
            Assert.That(front, Is.GreaterThan(zombie));
            SimulationFixture.Zombies(fixture.Owner.RequiredGame).Clear();
            boss.Enter(87, "anim_head_idle", 12f, 0);
            boss.AnimationSeconds = .3f;
            boss.DriverFrames = 5.4f;
            yield return fixture.Capture("DrawAdventureBoard", wholeBoard: true);
            sprites = Pool(fixture).SubmittedSprites.ToArray();
            var anchor = Array.FindIndex(sprites, sprite => sprite.GameObject != null && sprite.GameObject.name == "Boss_head2");
            var driver = Array.FindIndex(sprites, sprite => sprite.SourceImage!.SourceIdentity.IndexOf("bossdriver", StringComparison.OrdinalIgnoreCase) >= 0);
            var head = Array.FindIndex(sprites, sprite => sprite.GameObject != null && sprite.GameObject.name == "Boss_head");
            Assert.That(anchor, Is.GreaterThanOrEqualTo(0));
            Assert.That(driver, Is.GreaterThan(anchor));
            Assert.That(head, Is.GreaterThan(driver));
            Assert.That(sprites.Count(sprite => sprite.GameObject != null && sprite.GameObject.name == "Boss_head"), Is.EqualTo(1));
        }

        /// <summary>Checks original damage resources, blue spit glows and separate additive ball groups reach real mesh commands.</summary>
        /// <returns>Original source-art frames for damage and ice/fire ball glow passes.</returns>
        [UnityTest]
        public IEnumerator DamageAndIceFireGlowsUseOriginalResourcesAndBlendGroups()
        {
            using var fixture = new OriginalStormGraphicsTests.StormFixture(50);
            var boss = fixture.Owner.RequiredGame.Adventure.Boss!;
            boss.Enter(89, "anim_head_attack_1", 12f, 0);
            boss.DamageIndex = 2;
            boss.BallIsIce = true;
            boss.AnimationSeconds = 1.3f;
            yield return fixture.Capture(null, () => fixture.Animations.DrawBossPartNative(boss, AdventureBossPart.Main));
            var sources = Pool(fixture).SubmittedSprites.Select(sprite => sprite.SourceImage!.SourceIdentity.ToLowerInvariant()).ToArray();
            Assert.That(sources.Any(source => source.Contains("head_damage2")), Is.True);
            Assert.That(sources.Any(source => source.Contains("jaw_damage2")), Is.True);
            Assert.That(sources.Any(source => source.Contains("eyeglow_blue")), Is.True);
            foreach (var ice in new[] { false, true })
            {
                yield return fixture.Capture(null, () => fixture.Animations.DrawBossBallNative(new AdventureBossBall { Row = 1,
                    X = 280f / 80f, IsIce = ice, Rolling = true, AnimationSeconds = 3f }, 0f));
                Assert.That(Pool(fixture).SubmittedSprites.Any(sprite => sprite.Mode == 2), Is.True, ice ? "ice highlight" : "fire superglow");
                Assert.That(Pool(fixture).SubmittedSprites.Any(sprite => sprite.Mode == 0), Is.True);
            }
        }

        /// <summary>Checks the RV and bungee back-arm translations against the original target row/column formulas.</summary>
        /// <returns>Source-art camera frames at two native target positions for each original back-arm action.</returns>
        [UnityTest]
        public IEnumerator RvAndBungeeArmsKeepOriginalTargetTranslations()
        {
            foreach (var phase in new[] { 85, 82 })
            {
                using var fixture = new OriginalStormGraphicsTests.StormFixture(50);
                var boss = fixture.Owner.RequiredGame.Adventure.Boss!;
                boss.Enter(phase, phase == 85 ? "anim_RV_1" : "anim_bungee_1_enter", phase == 85 ? 16f : 12f, 0);
                boss.AnimationSeconds = 1.2f;
                boss.TargetRow = 1;
                yield return fixture.Capture(null, () =>
                {
                    using var actor = fixture.Animations.Hierarchy.BeginActor("Zombies", -1, "Boss");
                    fixture.Animations.DrawBossPartNative(boss, AdventureBossPart.BackArm);
                });
                var before = Pool(fixture).SubmittedSprites.Where(sprite => sprite.GameObject != null)
                    .ToDictionary(sprite => sprite.GameObject!.name, sprite => sprite.Vertices[0]);
                boss.TargetColumn = 2;
                boss.TargetRow = 3;
                yield return fixture.Capture(null, () =>
                {
                    using var actor = fixture.Animations.Hierarchy.BeginActor("Zombies", -1, "Boss");
                    fixture.Animations.DrawBossPartNative(boss, AdventureBossPart.BackArm);
                });
                var after = Pool(fixture).SubmittedSprites.First(sprite => sprite.GameObject != null && before.ContainsKey(sprite.GameObject.name));
                var original = before[after.GameObject!.name];
                var scale = Mathf.Min(Screen.width / 800f, Screen.height / 600f);
                Assert.That(after.Vertices[0].x - original.x, Is.EqualTo(160f * scale).Within(.001f));
                Assert.That(-after.Vertices[0].y + original.y, Is.EqualTo((phase == 85 ? 130f : 0f) * scale).Within(.001f));
            }
        }

        /// <summary>Reads the production renderer's submitted source commands after a completed camera frame.</summary>
        /// <param name="fixture">The production board fixture.</param>
        /// <returns>The scene sprite pool whose source commands are under test.</returns>
        private static ObjectSpriteRenderer Pool(OriginalStormGraphicsTests.StormFixture fixture)
        {
            return (ObjectSpriteRenderer)typeof(ReanimationRenderer).GetField("_objectRenderer", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(fixture.Animations)!;
        }
    }
}
