#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

namespace PlantsVsZombies.Editor
{
    /// <summary>Checks imported runtime artwork, playable audio, font pages, and metadata without rewriting source assets.</summary>
    public static class RuntimeAssetVerification
    {
        /// <summary>The only Unity version allowed to import or verify this project's runtime assets.</summary>
        private const string RequiredEditorVersion = "6000.3.17f1";
        /// <summary>The ignored report location relative to the project root.</summary>
        private const string ReportPath = ".unity-runs/assets-import/unity-verification.json";
        /// <summary>The runtime source directory whose files are checked.</summary>
        private const string RuntimeRoot = "Assets/StreamingAssets";
        /// <summary>The maximum time allowed for one background audio decode request.</summary>
        private const double AudioTimeoutSeconds = 30d;
        /// <summary>The BMFont text page declaration, accepting both quoted and unquoted filenames.</summary>
        private static readonly Regex s_fontPage = new Regex("^\\s*page\\s+.*?\\bfile=(?:\"([^\"]+)\"|([^\\s]+))", RegexOptions.Compiled | RegexOptions.Multiline);

        /// <summary>Contains concise verification counts and every failure for automated inspection.</summary>
        [Serializable]
        private sealed class VerificationReport
        {
            /// <summary>The Editor version that actually performed verification.</summary>
            public string EditorVersion = "";
            /// <summary>The UTC time at which verification started.</summary>
            public string StartedUtc = "";
            /// <summary>The total elapsed verification time in seconds.</summary>
            public double ElapsedSeconds;
            /// <summary>Whether every requested check completed successfully.</summary>
            public bool Passed;
            /// <summary>The number of runtime images successfully decoded by Unity.</summary>
            public int ImagesDecoded;
            /// <summary>The largest decoded original image width.</summary>
            public int MaximumImageWidth;
            /// <summary>The largest decoded original image height.</summary>
            public int MaximumImageHeight;
            /// <summary>The total number of original pixels successfully decoded.</summary>
            public long DecodedImagePixels;
            /// <summary>The number of runtime OGG and WAV files successfully decoded.</summary>
            public int RuntimeAudioDecoded;
            /// <summary>The number of byte-identical imported audio assets verified by Unity.</summary>
            public int ImportedAudioDecoded;
            /// <summary>The number of raw StreamingAssets files verified through Unity's runtime audio decoder.</summary>
            public int RuntimeRequestAudioDecoded;
            /// <summary>The number of preserved AU and MO3 source files checked without claiming direct module playback.</summary>
            public int ArchivedAudioSources;
            /// <summary>The number of AU or MO3 sources with a verified Unity-compatible WAV companion.</summary>
            public int ConvertedAudioCompanions;
            /// <summary>The companion WAV mappings used for archived AU and MO3 source checks.</summary>
            public string[] ConvertedAudioMappings = Array.Empty<string>();
            /// <summary>The number of font descriptions whose page references were checked.</summary>
            public int FontsChecked;
            /// <summary>The number of font page references resolving to existing runtime files.</summary>
            public int FontPageReferences;
            /// <summary>The number of existing runtime asset and metadata pairs checked.</summary>
            public int AssetMetadataPairs;
            /// <summary>The number of project metadata GUIDs inspected for validity and duplication.</summary>
            public int MetadataGuidsChecked;
            /// <summary>The complete list of failed checks, using project-relative paths only.</summary>
            public string[] Failures = Array.Empty<string>();
        }

        /// <summary>Runs verification from the menu or Unity's -executeMethod command-line entry point.</summary>
        /// <exception cref="InvalidOperationException">The Editor version, mode, or one or more runtime asset checks are invalid.</exception>
        /// <exception cref="IOException">The ignored verification report cannot be written.</exception>
        [MenuItem("PlantsVsZombies/Verify Imported Runtime Assets")]
        public static void Verify()
        {
            var clock = Stopwatch.StartNew();
            var report = new VerificationReport
            {
                EditorVersion = Application.unityVersion,
                StartedUtc = DateTime.UtcNow.ToString("O")
            };
            var failures = new List<string>();
            var companions = new List<string>();
            try
            {
                if (!string.Equals(Application.unityVersion, RequiredEditorVersion, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException("Use Unity " + RequiredEditorVersion + " for runtime asset verification.");
                }

                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    throw new InvalidOperationException("Run runtime asset verification in Edit Mode.");
                }

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                if (!Directory.Exists(RuntimeRoot))
                {
                    throw new DirectoryNotFoundException("The runtime resource directory is missing: " + RuntimeRoot);
                }

                var verifiedAudio = new HashSet<string>(StringComparer.Ordinal);
                foreach (var path in Directory.EnumerateFiles(RuntimeRoot, "*", SearchOption.AllDirectories))
                {
                    if (IsExcluded(path) || path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var assetPath = Normalize(path);
                    VerifyAssetPair(assetPath, report, failures);
                    var extension = Path.GetExtension(path).ToLowerInvariant();
                    try
                    {
                        if (extension == ".png" || extension == ".jpg" || extension == ".jpeg")
                        {
                            VerifyImage(assetPath, report);
                        }
                        else if (extension == ".ogg" || extension == ".wav")
                        {
                            VerifyAudio(assetPath, report, verifiedAudio);
                            report.RuntimeAudioDecoded++;
                        }
                        else if (extension == ".au" || extension == ".mo3")
                        {
                            VerifyAudioCompanion(assetPath, report, companions, verifiedAudio);
                        }
                        else if (extension == ".fnt")
                        {
                            VerifyFontPages(assetPath, report);
                        }
                    }
                    catch (Exception error)
                    {
                        failures.Add(assetPath + ": " + error.Message);
                    }
                }

                VerifyMetadata(report, failures);
            }
            catch (Exception error)
            {
                failures.Add(error.Message);
            }

            clock.Stop();
            report.ElapsedSeconds = clock.Elapsed.TotalSeconds;
            report.Passed = failures.Count == 0;
            report.ConvertedAudioMappings = companions.ToArray();
            report.Failures = failures.ToArray();
            var reportFile = Path.Combine(Path.GetDirectoryName(Application.dataPath)!, ReportPath);
            Directory.CreateDirectory(Path.GetDirectoryName(reportFile)!);
            File.WriteAllText(reportFile, JsonUtility.ToJson(report, true), new UTF8Encoding(false));
            var summary = "images=" + report.ImagesDecoded + ", runtime audio=" + report.RuntimeAudioDecoded
                + ", archived AU/MO3=" + report.ArchivedAudioSources + ", WAV companions=" + report.ConvertedAudioCompanions
                + ", fonts=" + report.FontsChecked + ", GUIDs=" + report.MetadataGuidsChecked + ", failures=" + failures.Count;
            if (!report.Passed)
            {
                Debug.LogError("PVZ_RUNTIME_ASSETS_FAIL: " + summary + "; report=" + ReportPath);
                throw new InvalidOperationException("Runtime asset verification failed. See " + ReportPath);
            }

            Debug.Log("PVZ_RUNTIME_ASSETS_PASS: " + summary + "; report=" + ReportPath);
        }

        /// <summary>Decodes original image bytes directly so importer scaling cannot hide a corrupt source.</summary>
        /// <param name="path">The project-relative runtime image path.</param>
        /// <param name="report">The successful decode counters.</param>
        /// <exception cref="InvalidDataException">Unity cannot decode the original image or its dimensions are empty.</exception>
        private static void VerifyImage(string path, VerificationReport report)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            try
            {
                if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), true) || texture.width <= 0 || texture.height <= 0)
                {
                    throw new InvalidDataException("Unity could not decode the original image with positive dimensions.");
                }

                report.ImagesDecoded++;
                report.MaximumImageWidth = Math.Max(report.MaximumImageWidth, texture.width);
                report.MaximumImageHeight = Math.Max(report.MaximumImageHeight, texture.height);
                report.DecodedImagePixels += (long)texture.width * texture.height;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        /// <summary>Verifies Unity audio using imported byte-identical originals or the runtime StreamingAssets decoder.</summary>
        /// <param name="path">The original OGG or WAV asset path.</param>
        /// <param name="report">The successful decoder counters.</param>
        /// <param name="verified">The audio paths already successfully checked in this run.</param>
        /// <exception cref="InvalidDataException">Unity rejects the clip or reports no sample data.</exception>
        private static void VerifyAudio(string path, VerificationReport report, HashSet<string> verified)
        {
            if (verified.Contains(path))
            {
                return;
            }

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null && path.StartsWith(RuntimeRoot + "/", StringComparison.Ordinal))
            {
                var relative = path.Substring((RuntimeRoot + "/").Length);
                var imported = "Assets/OriginalPvZ/Raw/" + relative;
                if (Path.GetExtension(path).Equals(".wav", StringComparison.OrdinalIgnoreCase))
                {
                    imported = "Assets/OriginalPvZ/Converted/audio/" + Path.GetFileName(path);
                }

                if (File.Exists(imported) && SameBytes(path, imported))
                {
                    clip = AssetDatabase.LoadAssetAtPath<AudioClip>(imported);
                }
            }

            if (clip != null)
            {
                var previouslyLoaded = clip.loadState == AudioDataLoadState.Loaded;
                try
                {
                    RequireAudioData(clip);
                    report.ImportedAudioDecoded++;
                }
                finally
                {
                    if (!previouslyLoaded)
                    {
                        clip.UnloadAudioData();
                    }
                }
            }
            else
            {
                DecodeRuntimeAudio(path);
                report.RuntimeRequestAudioDecoded++;
            }

            verified.Add(path);
        }

        /// <summary>Checks imported audio metadata and waits only for a bounded native audio-data request.</summary>
        /// <param name="clip">The audio clip imported from the original matching bytes.</param>
        /// <exception cref="InvalidDataException">The clip is empty, fails loading, or exceeds the bounded decode wait.</exception>
        private static void RequireAudioData(AudioClip clip)
        {
            if (clip.length <= 0f || clip.channels <= 0 || clip.samples <= 0 || clip.frequency <= 0 || !clip.LoadAudioData())
            {
                throw new InvalidDataException("Unity audio metadata or LoadAudioData is invalid.");
            }

            var clock = Stopwatch.StartNew();
            while (clip.loadState == AudioDataLoadState.Loading && clock.Elapsed.TotalSeconds < AudioTimeoutSeconds)
            {
                Thread.Sleep(1);
            }

            if (clip.loadState != AudioDataLoadState.Loaded)
            {
                throw new InvalidDataException("Unity audio did not reach Loaded: " + clip.loadState);
            }
        }

        /// <summary>Decodes byte-preserved StreamingAssets audio that Unity intentionally imports as a DefaultAsset.</summary>
        /// <param name="path">The runtime OGG or WAV file path.</param>
        /// <exception cref="InvalidDataException">Unity's audio request fails, times out, or contains no samples.</exception>
        private static void DecodeRuntimeAudio(string path)
        {
            var type = Path.GetExtension(path).Equals(".wav", StringComparison.OrdinalIgnoreCase) ? AudioType.WAV : AudioType.OGGVORBIS;
            using var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(Path.GetFullPath(path)).AbsoluteUri, type);
            var operation = request.SendWebRequest();
            var clock = Stopwatch.StartNew();
            while (!operation.isDone && clock.Elapsed.TotalSeconds < AudioTimeoutSeconds)
            {
                Thread.Sleep(1);
            }

            if (!operation.isDone || request.result != UnityWebRequest.Result.Success)
            {
                request.Abort();
                throw new InvalidDataException("Unity runtime audio decode failed or timed out: " + request.error);
            }

            var clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip == null)
            {
                throw new InvalidDataException("Unity's audio download handler returned no clip.");
            }

            try
            {
                RequireAudioData(clip);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clip);
            }
        }

        /// <summary>Checks an archived AU or MO3 file and its existing converted WAV without decoding the module as a Unity clip.</summary>
        /// <param name="path">The original archived audio source.</param>
        /// <param name="report">The successful source and companion counters.</param>
        /// <param name="mappings">The companion paths included in the report.</param>
        /// <param name="verified">The companion audio paths already checked.</param>
        /// <exception cref="InvalidDataException">The original file is empty or its converted WAV is absent or invalid.</exception>
        private static void VerifyAudioCompanion(string path, VerificationReport report, List<string> mappings, HashSet<string> verified)
        {
            report.ArchivedAudioSources++;
            if (new FileInfo(path).Length == 0)
            {
                throw new InvalidDataException("The preserved original audio source is empty.");
            }

            var name = Path.GetFileNameWithoutExtension(path) + ".wav";
            var companion = Normalize(Path.Combine(Path.GetDirectoryName(path)!, name));
            if (!File.Exists(companion))
            {
                companion = "Assets/OriginalPvZ/Converted/audio/" + name;
            }

            if (!File.Exists(companion))
            {
                throw new InvalidDataException("The converted WAV companion is missing: " + companion);
            }

            VerifyAudio(companion, report, verified);
            mappings.Add(path + " -> " + companion);
            report.ConvertedAudioCompanions++;
        }

        /// <summary>Checks every original BMFont page reference resolves beside its font description.</summary>
        /// <param name="path">The runtime BMFont text description.</param>
        /// <param name="report">The successful font and page counters.</param>
        /// <exception cref="InvalidDataException">The font is missing a page declaration or references absent artwork.</exception>
        private static void VerifyFontPages(string path, VerificationReport report)
        {
            var matches = s_fontPage.Matches(File.ReadAllText(path));
            if (matches.Count == 0)
            {
                throw new InvalidDataException("The font contains no BMFont page declaration.");
            }

            foreach (Match match in matches)
            {
                var name = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                var page = Normalize(Path.Combine(Path.GetDirectoryName(path)!, name));
                if (!File.Exists(page))
                {
                    throw new InvalidDataException("The font page is missing: " + page);
                }

                report.FontPageReferences++;
            }

            report.FontsChecked++;
        }

        /// <summary>Checks the original runtime asset retains a metadata partner without creating or repairing GUIDs.</summary>
        /// <param name="path">The runtime file or directory.</param>
        /// <param name="report">The successful pair counter.</param>
        /// <param name="failures">The complete list receiving missing metadata checks.</param>
        private static void VerifyAssetPair(string path, VerificationReport report, List<string> failures)
        {
            if (!File.Exists(path + ".meta"))
            {
                failures.Add("Missing metadata: " + path + ".meta");
            }
            else
            {
                report.AssetMetadataPairs++;
            }
        }

        /// <summary>Checks runtime directory pairing and globally unique, valid project GUIDs without changing metadata.</summary>
        /// <param name="report">The successful metadata counters.</param>
        /// <param name="failures">The complete list receiving metadata failures.</param>
        private static void VerifyMetadata(VerificationReport report, List<string> failures)
        {
            VerifyAssetPair(RuntimeRoot, report, failures);
            foreach (var directory in Directory.EnumerateDirectories(RuntimeRoot, "*", SearchOption.AllDirectories))
            {
                if (!IsExcluded(directory))
                {
                    VerifyAssetPair(Normalize(directory), report, failures);
                }
            }

            var guids = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var file in Directory.EnumerateFiles("Assets", "*.meta", SearchOption.AllDirectories))
            {
                if (IsExcluded(file))
                {
                    continue;
                }

                var path = Normalize(file);
                var asset = path.Substring(0, path.Length - ".meta".Length);
                if (IsExcluded(asset))
                {
                    continue;
                }

                if (!File.Exists(asset) && !Directory.Exists(asset))
                {
                    failures.Add("Orphan metadata: " + path);
                }

                string? value = null;
                foreach (var line in File.ReadLines(file))
                {
                    if (line.StartsWith("guid:", StringComparison.Ordinal))
                    {
                        value = line.Substring("guid:".Length).Trim();
                        break;
                    }
                }

                report.MetadataGuidsChecked++;
                if (value == null || !Guid.TryParseExact(value, "N", out var guid) || guid == Guid.Empty)
                {
                    failures.Add("Invalid GUID: " + path);
                }
                else if (guids.TryGetValue(value, out var previous))
                {
                    failures.Add("Duplicate GUID: " + path + " and " + previous);
                }
                else
                {
                    guids.Add(value, path);
                }
            }
        }

        /// <summary>Compares runtime and already imported originals before using their decoded clip as byte-identical evidence.</summary>
        /// <param name="first">The runtime source path.</param>
        /// <param name="second">The imported original or converted source path.</param>
        /// <returns>True when every source byte is identical.</returns>
        private static bool SameBytes(string first, string second)
        {
            if (new FileInfo(first).Length != new FileInfo(second).Length)
            {
                return false;
            }

            using var left = File.OpenRead(first);
            using var right = File.OpenRead(second);
            var leftBytes = new byte[8192];
            var rightBytes = new byte[8192];
            int count;
            while ((count = left.Read(leftBytes, 0, leftBytes.Length)) > 0)
            {
                if (right.Read(rightBytes, 0, count) != count)
                {
                    return false;
                }

                for (var index = 0; index < count; index++)
                {
                    if (leftBytes[index] != rightBytes[index])
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>Excludes local caches, operating-system metadata, and hidden files from runtime asset checks.</summary>
        /// <param name="path">The project-relative file or directory path.</param>
        /// <returns>True when the path is outside the requested original runtime resource checks.</returns>
        private static bool IsExcluded(string path)
        {
            var normalized = Normalize(path);
            foreach (var part in normalized.Split('/'))
            {
                if (part.StartsWith(".", StringComparison.Ordinal) || part.Equals("Cache", StringComparison.OrdinalIgnoreCase)
                    || part.Equals("Temp", StringComparison.OrdinalIgnoreCase) || part.Equals("__pycache__", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            var name = Path.GetFileName(path);
            return name.Equals("Thumbs.db", StringComparison.OrdinalIgnoreCase) || name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Uses Unity's project-relative path separators in reports and asset database lookups.</summary>
        /// <param name="path">The original local path.</param>
        /// <returns>The same path with forward-slash separators.</returns>
        private static string Normalize(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
