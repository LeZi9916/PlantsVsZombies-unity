#nullable enable
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PlantsVsZombies.Editor
{
    /// <summary>Defines original pv zasset verification.</summary>
    /// <exception cref = "Exception">The operation cannot satisfy its preconditions.</exception>
    public static class OriginalPvZAssetVerification
    {
        /// <summary>Stores root.</summary>
        private const string Root = "Assets/OriginalPvZ/";
        /// <summary>Performs verify.</summary>
        /// <exception cref = "Exception">The operation cannot satisfy its preconditions.</exception>
        [MenuItem("PvZ Replica/Verify Original Assets")]
        public static void Verify()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            int textures = 0, audio = 0, json = 0;
            foreach (string file in Directory.GetFiles(Root, "*", SearchOption.AllDirectories))
            {
                var path = file.Replace('\\', '/');
                var extension = Path.GetExtension(path).ToLowerInvariant();
                if (extension == ".png" || extension == ".jpg" || extension == ".jpeg")
                {
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (importer == null || importer.textureCompression != TextureImporterCompression.Uncompressed || importer.mipmapEnabled || importer.npotScale != TextureImporterNPOTScale.None || importer.maxTextureSize < 4096)
                    {
                        throw new Exception("Texture import settings missing: " + path);
                    }

                    textures++;
                }
                else if (extension == ".ogg" || extension == ".wav")
                {
                    var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                    if (importer == null || importer.defaultSampleSettings.compressionFormat != AudioCompressionFormat.PCM || importer.defaultSampleSettings.sampleRateSetting != AudioSampleRateSetting.PreserveSampleRate)
                    {
                        throw new Exception("Audio import settings missing: " + path);
                    }

                    audio++;
                }
                else if (extension == ".json" && path.StartsWith(Root + "Converted/", StringComparison.OrdinalIgnoreCase))
                {
                    if (AssetImporter.GetAtPath(path) == null)
                    {
                        throw new Exception("JSON was not imported: " + path);
                    }

                    json++;
                }
            }

            RequireTexture(Root + "Raw/images/SeedBank.png", 446, 87);
            RequireTexture(Root + "Raw/data/DwarvenTodcraft36GreenInset.png", 2440, 2318);
            RequireTexture(Root + "Converted/rgba/images/sod1row.png", 771, 127);
            RequireAudio(Root + "Raw/sounds/plant.ogg");
            RequireAudio(Root + "Converted/audio/mainmusic.wav");
            RequireAudio(Root + "Converted/audio/mainmusic_hihats.wav");
            RequireText(Root + "Converted/reanim/PeaShooter.reanim.json");
            RequireText(Root + "Converted/particles/Award.xml.json");
            if (!File.Exists(Root + "Raw/reanim/PeaShooter.reanim") || !File.Exists(Root + "Raw/sounds/mainmusic.mo3"))
            {
                throw new Exception("Original reanimation or module music is missing.");
            }

            Debug.Log("PVZ_ORIGINAL_ASSETS_PASS: textures=" + textures + ", audio=" + audio + ", converted JSON=" + json + "; source formats remain separate from Unity animation clips.");
        }

        /// <summary>Performs require texture.</summary>
        /// <param name = "path">The path.</param>
        /// <param name = "width">The width.</param>
        /// <param name = "height">The height.</param>
        /// <exception cref = "Exception">The operation cannot satisfy its preconditions.</exception>
        private static void RequireTexture(string path, int width, int height)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null || texture.width != width || texture.height != height)
            {
                throw new Exception("Texture missing or resized: " + path);
            }
        }

        /// <summary>Performs require audio.</summary>
        /// <param name = "path">The path.</param>
        /// <exception cref = "Exception">The operation cannot satisfy its preconditions.</exception>
        private static void RequireAudio(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null || clip.length <= 0f)
            {
                throw new Exception("Audio clip missing or empty: " + path);
            }
        }

        /// <summary>Performs require text.</summary>
        /// <param name = "path">The path.</param>
        /// <exception cref = "Exception">The operation cannot satisfy its preconditions.</exception>
        private static void RequireText(string path)
        {
            var text = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (text == null || text.bytes.Length == 0)
            {
                throw new Exception("Converted definition missing or empty: " + path);
            }
        }
    }
}
