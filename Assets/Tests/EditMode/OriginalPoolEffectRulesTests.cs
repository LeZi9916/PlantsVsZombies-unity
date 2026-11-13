#nullable enable

using System;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using PlantsVsZombies.Rendering;
using PlantsVsZombies.Simulation;
using UnityEngine;

namespace PlantsVsZombies.Tests.EditMode
{
    /// <summary>Checks native PoolEffect machine-code samples and the Ice-shroom water-clock boundary.</summary>
    public sealed class OriginalPoolEffectRulesTests
    {
        /// <summary>Compares actual PC 0x478ED0 vertex UV output captured before its draw calls.</summary>
        /// <param name="layer">The original pool layer.</param>
        /// <param name="column">The original grid vertex column.</param>
        /// <param name="row">The original grid vertex row.</param>
        /// <param name="ticks">The original independent PoolEffect counter.</param>
        /// <param name="u">The U value produced by the original x86 instructions.</param>
        /// <param name="v">The V value produced by the original x86 instructions.</param>
        [TestCase(0, 1, 1, 0, 0.0625521690f, 0.2285317034f)]
        [TestCase(0, 7, 3, 0, 0.4600093067f, 0.6176335812f)]
        [TestCase(0, 14, 4, 0, 0.9374479055f, 0.7714683414f)]
        [TestCase(1, 1, 1, 0, 0.0851494446f, 0.2099476010f)]
        [TestCase(1, 7, 3, 0, 0.4874705672f, 0.6392915249f)]
        [TestCase(1, 14, 4, 0, 0.9367173910f, 0.7852750421f)]
        [TestCase(2, 1, 1, 0, 0.1280432642f, 0.4070004225f)]
        [TestCase(2, 7, 3, 0, 0.9247739315f, 1.1780400276f)]
        [TestCase(2, 14, 4, 0, 1.8719568253f, 1.5929994583f)]
        [TestCase(0, 1, 1, 117, 0.0630371496f, 0.2272211164f)]
        [TestCase(0, 7, 3, 117, 0.4640465081f, 0.5980216265f)]
        [TestCase(0, 14, 4, 117, 0.9337104559f, 0.7843991518f)]
        [TestCase(1, 1, 1, 117, 0.0697873980f, 0.2345920801f)]
        [TestCase(1, 7, 3, 117, 0.4777532816f, 0.6211363077f)]
        [TestCase(1, 14, 4, 117, 0.9397521615f, 0.7632991672f)]
        [TestCase(2, 1, 1, 117, 0.1247467995f, 0.3574773669f)]
        [TestCase(2, 7, 3, 117, 0.9328822494f, 1.2252612114f)]
        [TestCase(2, 14, 4, 117, 1.8618111610f, 1.5580661297f)]
        [TestCase(0, 1, 1, 800, 0.0629142746f, 0.2144093066f)]
        [TestCase(0, 7, 3, 800, 0.4633977115f, 0.6086490750f)]
        [TestCase(0, 14, 4, 800, 0.9342832565f, 0.7857491374f)]
        [TestCase(1, 1, 1, 800, 0.0792166963f, 0.2031091899f)]
        [TestCase(1, 7, 3, 800, 0.4618536234f, 0.6311132908f)]
        [TestCase(1, 14, 4, 800, 0.9210094810f, 0.7930326462f)]
        [TestCase(2, 1, 1, 800, 0.1327455342f, 0.3833993673f)]
        [TestCase(2, 7, 3, 800, 0.9323823452f, 1.1816455126f)]
        [TestCase(2, 14, 4, 800, 1.8672544956f, 1.5556735992f)]
        public void UvMatchesOriginalMachineCode(int layer, int column, int row, int ticks, float u, float v)
        {
            var actual = OriginalPoolEffectRules.Uv(layer, column, row, ticks);
            Assert.That(actual.x, Is.EqualTo(u).Within(.000001f));
            Assert.That(actual.y, Is.EqualTo(v).Within(.000001f));
        }

        /// <summary>Compares every caustic alpha byte with PC 0x478D90 running against a fixed grayscale input.</summary>
        /// <param name="ticks">The original independent PoolEffect counter.</param>
        /// <param name="expected">The SHA-256 of all 8192 top-to-bottom alpha bytes produced by the PC instructions.</param>
        [TestCase(0, "64ba7b0186a907ce2c166a79970698f2f8879a2fc1d235bdcead60e38a57768f")]
        [TestCase(1, "793e91cb75b83d51cec2ea560b39c10220de7f1496ef471aac01b7f8018ea34c")]
        [TestCase(150, "1e37f87b665c8f502b09130eacca585f3ef7125b664eb4db38a5ca6917bbb0a4")]
        [TestCase(800, "ea43e29f9af7f7ad456107bbf7c9cd1dc7297a3e7a3eca314b772cc20d76ac85")]
        public void CausticTextureMatchesOriginalMachineCode(int ticks, string expected)
        {
            var source = Source();
            var pixels = new Color32[128 * 64];
            OriginalPoolEffectRules.FillCaustics(source, pixels, ticks);
            var alpha = new byte[pixels.Length];
            for (var row = 0; row < 64; row++)
            {
                for (var column = 0; column < 128; column++)
                {
                    alpha[row * 128 + column] = pixels[(63 - row) * 128 + column].a;
                }
            }

            using var sha = SHA256.Create();
            var actual = BitConverter.ToString(sha.ComputeHash(alpha)).Replace("-", "").ToLowerInvariant();
            Assert.That(actual, Is.EqualTo(expected));
        }

        /// <summary>Checks wrapping source samples and PC 0x478CB0's separate truncation of all four bilinear terms.</summary>
        /// <param name="x">The signed wrapping 16.16 X coordinate.</param>
        /// <param name="y">The signed wrapping 16.16 Y coordinate.</param>
        /// <param name="expected">The grayscale output produced by the original x86 instructions.</param>
        [TestCase(0, 0, 0)]
        [TestCase(65536, 131072, 12)]
        [TestCase(-1234567, 3456789, 206)]
        [TestCase(16777215, 16777215, 0)]
        public void BilinearSamplesMatchOriginalMachineCode(int x, int y, int expected)
        {
            Assert.That(OriginalPoolEffectRules.Sample(Source(), x, y), Is.EqualTo(expected));
        }

        /// <summary>Checks the actual Ice-shroom activation tick, 299 held updates, and the first resumed water update.</summary>
        [Test]
        public void IceShroomStopsWaterForTheOriginalCounterWindow()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 22 }, 7529);
            Assert.That(game.ChooseDefaultAdventureSeeds(), Is.True);
            game.ResetAdventureWaveSchedule(100000, false, false);
            var plant = (Plant)typeof(GameSimulation).GetMethod("PlaceAdventurePlant", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(game, new object[] { PlantKind.IceShroom, 0, 0 })!;
            plant.Asleep = false;
            plant.ActionTimer = .01f;
            game.Update(.01f);
            Assert.That(game.Plants.Contains(plant), Is.False);
            Assert.That(game.Adventure.PoolEffectTicks, Is.EqualTo(1));
            Assert.That(game.Adventure.IceTrapTicks, Is.EqualTo(299));
            for (var tick = 0; tick < 299; tick++)
            {
                game.Update(.01f);
            }

            Assert.That(game.Adventure.PoolEffectTicks, Is.EqualTo(1));
            Assert.That(game.Adventure.IceTrapTicks, Is.Zero);
            Assert.That(game.Adventure._ticks, Is.EqualTo(300), "Plant bobbing keeps the board main clock while the water is frozen.");
            game.Update(.01f);
            Assert.That(game.Adventure.PoolEffectTicks, Is.EqualTo(2));
        }

        /// <summary>Checks that previews animate independently, pause holds water, and a survival repick keeps its native gate.</summary>
        [Test]
        public void PoolPreviewAndPauseUseTheSeparateNativeClock()
        {
            var game = new GameSimulation(new GameRules { AdventureLevel = 22 }, 7529);
            game.AdvanceAdventurePoolPresentation(.03f);
            Assert.That(game.Adventure.PoolEffectTicks, Is.EqualTo(3));
            Assert.That(game.Adventure._ticks, Is.Zero);
            game.AdvanceAdventurePoolPresentation(.02f, survivalRepick: true);
            Assert.That(game.Adventure.PoolEffectTicks, Is.EqualTo(3));
            game.TogglePause();
            game.AdvanceAdventurePoolPresentation(.02f);
            Assert.That(game.Adventure.PoolEffectTicks, Is.EqualTo(3));
            game.TogglePause();
            game.AdvanceAdventurePoolPresentation(.02f);
            Assert.That(game.Adventure.PoolEffectTicks, Is.EqualTo(5));
        }

        /// <summary>Creates the fixed input supplied to the native machine-code oracle.</summary>
        /// <returns>The top-to-bottom 256-by-256 grayscale source used by all fixed-point comparisons.</returns>
        private static byte[] Source()
        {
            var source = new byte[256 * 256];
            for (var row = 0; row < 256; row++)
            {
                for (var column = 0; column < 256; column++)
                {
                    source[row * 256 + column] = (byte)((column * 3 + row * 5) & 255);
                }
            }

            return source;
        }
    }
}
