#nullable enable

using System;
using System.IO;
using NUnit.Framework;
using PlantsVsZombies.Game;
using UnityEngine;

namespace PlantsVsZombies.Tests.PlayMode
{
    /// <summary>Regresses original paper masks and handwriting alpha grids through Unity image decoding.</summary>
    public sealed class OriginalImageTextureTests
    {
        /// <summary>Checks original RGB handwriting masks become black ink with transparent unused pixels.</summary>
        [Test]
        public void AlphaGridKeepsColorArtworkAndUsesMaskAsOpacity()
        {
            CheckMaskedImage(false);
        }

        /// <summary>Checks loading a JPEG source cannot discard its separately loaded alpha channel.</summary>
        [Test]
        public void JpegPaperRetainsTransparentBorderAfterMaskComposition()
        {
            CheckMaskedImage(true);
        }

        /// <summary>Creates tiny original-format sources and verifies every merged output pixel.</summary>
        /// <param name="jpeg">Whether the color artwork should use JPEG decoding.</param>
        private static void CheckMaskedImage(bool jpeg)
        {
            var directory = Path.Combine(Application.temporaryCachePath, "PvZImageMask-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var color = new Texture2D(2, 2, TextureFormat.RGB24, false);
            var mask = new Texture2D(2, 2, TextureFormat.RGB24, false);
            Texture2D? result = null;
            try
            {
                var colorPath = Path.Combine(directory, jpeg ? "paper.jpg" : "ink.png");
                var maskPath = Path.Combine(directory, "alpha.png");
                var source = jpeg ? new Color32(240, 220, 100, 255) : new Color32(0, 0, 0, 255);
                color.SetPixels32(new[] { source, source, source, source });
                color.Apply();
                mask.SetPixels32(new[]
                {
                    new Color32(0, 0, 0, 255),
                    new Color32(64, 64, 64, 255),
                    new Color32(192, 192, 192, 255),
                    new Color32(255, 255, 255, 255)
                });
                mask.Apply();
                File.WriteAllBytes(colorPath, jpeg ? color.EncodeToJPG(100) : color.EncodeToPNG());
                File.WriteAllBytes(maskPath, mask.EncodeToPNG());
                result = PvZGame.LoadMaskedImage(colorPath, maskPath);
                Assert.That(result.format, Is.EqualTo(TextureFormat.RGBA32));
                var pixels = result.GetPixels32();
                Assert.That(pixels[0].a, Is.Zero);
                Assert.That(pixels[1].a, Is.EqualTo(64));
                Assert.That(pixels[2].a, Is.EqualTo(192));
                Assert.That(pixels[3].a, Is.EqualTo(255));
                foreach (var pixel in pixels)
                {
                    Assert.That(pixel.r, Is.EqualTo(source.r).Within(jpeg ? 3 : 0));
                    Assert.That(pixel.g, Is.EqualTo(source.g).Within(jpeg ? 3 : 0));
                    Assert.That(pixel.b, Is.EqualTo(source.b).Within(jpeg ? 3 : 0));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(result);
                UnityEngine.Object.DestroyImmediate(color);
                UnityEngine.Object.DestroyImmediate(mask);
                Directory.Delete(directory, true);
            }
        }
    }
}
