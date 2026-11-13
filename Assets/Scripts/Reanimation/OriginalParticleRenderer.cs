#nullable enable
using PlantsVsZombies.Randomness;
using PlantsVsZombies.Rendering;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEngine;

namespace PlantsVsZombies.Reanimation
{
    /// <summary>Evaluates imported original particle emitters. No effect is defined by this renderer.</summary>
    public sealed class OriginalParticleRenderer
    {
        /// <summary>Limits retained particles per immutable emitter definition after bursts have ended.</summary>
        private const int MaximumRetainedParticles = 256;
        /// <summary>Defines node.</summary>
        private sealed class Node
        {
            /// <summary>Stores time, minimum, maximum.</summary>
            public float Time, Minimum, Maximum;
            /// <summary>Stores shape.</summary>
            public string Shape = "Linear";
            /// <summary>Shapes the original random interpolation between this node's minimum and maximum.</summary>
            public string RandomShape = "Linear";
        }

        /// <summary>Defines curve.</summary>
        private sealed class Curve
        {
            /// <summary>Stores nodes.</summary>
            public readonly List<Node> Nodes = new List<Node>();
            /// <summary>Performs value.</summary>
            /// <param name = "time">The time.</param>
            /// <param name = "random">The random.</param>
            /// <returns>The resulting value.</returns>
            public float Value(float time, float random)
            {
                var left = Nodes[0];
                for (var i = 1; i < Nodes.Count; i++)
                {
                    var right = Nodes[i];
                    if (time <= right.Time)
                    {
                        var t = right.Time <= left.Time ? 0f : Mathf.Clamp01((time - left.Time) / (right.Time - left.Time));
                        return Mathf.Lerp(Mathf.Lerp(left.Minimum, left.Maximum, ApplyShape(random, left.RandomShape)), Mathf.Lerp(right.Minimum, right.Maximum, ApplyShape(random, right.RandomShape)), ApplyShape(t, left.Shape));
                    }

                    left = right;
                }

                return Mathf.Lerp(left.Minimum, left.Maximum, ApplyShape(random, left.RandomShape));
            }

            /// <summary>Performs apply shape.</summary>
            /// <param name = "t">The t.</param>
            /// <param name = "shape">The shape.</param>
            /// <returns>The resulting apply shape.</returns>
            private static float ApplyShape(float t, string shape)
            {
                switch (shape)
                {
                    case "Constant":
                        return 0;
                    case "EaseIn":
                        return t * t;
                    case "EaseOut":
                        return 2 * t - t * t;
                    case "FastInOutWeak":
                        // PC 0x526C90 curve 7 calls 0x526BD0, also inside random range brackets.
                        var half = t <= .5f ? t * 2f : (t - .5f) * 2f;
                        return t <= .5f ? (2f * half - half * half) * .5f : half * half * .5f + .5f;
                    case "EaseInOutWeak":
                        return 3 * t * t - 2 * t * t * t;
                    case "EaseInOut":
                        var s = 3 * t * t - 2 * t * t * t;
                        return 3 * s * s - 2 * s * s * s;
                    default:
                        return t;
                }
            }

            /// <summary>Performs parse.</summary>
            /// <param name = "source">The source.</param>
            /// <param name = "fallback">The fallback.</param>
            /// <returns>The resulting parse.</returns>
            /// <exception cref = "FormatException">The operation cannot satisfy its preconditions.</exception>
            public static Curve Parse(string? source, float fallback)
            {
                var curve = new Curve();
                if (string.IsNullOrWhiteSpace(source))
                {
                    curve.Nodes.Add(new Node { Minimum = fallback, Maximum = fallback });
                    return curve;
                }

                const string number = @"[-+]?(?:\d*\.\d+|\d+\.?\d*)(?:[eE][-+]?\d+)?";
                var values = Regex.Matches(source, @"(?:\[\s*(" + number + @")(?:\s+(?:(?<randomShape>[A-Za-z]+)\s+)?(" + number + @"))?\s*\]|(" + number + @"))(?:\s*,\s*(" + number + @"))?");
                foreach (Match value in values)
                {
                    var minimum = Number(value.Groups[1].Success ? value.Groups[1].Value : value.Groups[3].Value);
                    curve.Nodes.Add(new Node {
                        Minimum = minimum,
                        Maximum = value.Groups[2].Success ? Number(value.Groups[2].Value) : minimum,
                        RandomShape = value.Groups["randomShape"].Success ? value.Groups["randomShape"].Value : "Linear",
                        Time = value.Groups[4].Success ? Number(value.Groups[4].Value) * .01f : float.NaN
                    });
                }

                for (var i = 0; i + 1 < values.Count; i++)
                {
                    var gap = source.Substring(values[i].Index + values[i].Length, values[i + 1].Index - values[i].Index - values[i].Length).Trim();
                    if (gap.Length > 0)
                    {
                        curve.Nodes[i].Shape = gap;
                    }
                }

                if (curve.Nodes.Count == 0)
                {
                    throw new FormatException("Unsupported original particle curve: " + source);
                }

                if (float.IsNaN(curve.Nodes[0].Time))
                {
                    curve.Nodes[0].Time = 0f;
                }

                if (curve.Nodes.Count > 1 && float.IsNaN(curve.Nodes[curve.Nodes.Count - 1].Time))
                {
                    curve.Nodes[curve.Nodes.Count - 1].Time = 1f;
                }

                for (var i = 1; i < curve.Nodes.Count - 1; i++)
                {
                    if (!float.IsNaN(curve.Nodes[i].Time))
                    {
                        continue;
                    }

                    var end = i + 1;
                    while (end < curve.Nodes.Count && float.IsNaN(curve.Nodes[end].Time))
                    {
                        end++;
                    }

                    float beginTime = curve.Nodes[i - 1].Time, endTime = curve.Nodes[end].Time;
                    var begin = i - 1;
                    for (; i < end; i++)
                    {
                        curve.Nodes[i].Time = Mathf.Lerp(beginTime, endTime, (i - begin) / (float)(end - begin));
                    }

                    i--;
                }

                return curve;
            }
        }

        /// <summary>Defines field.</summary>
        private sealed class Field
        {
            /// <summary>Stores type.</summary>
            public string Type = string.Empty;
            /// <summary>Stores x, y.</summary>
            public Curve? X, Y;

            /// <summary>Gets the x, which must already exist wherever it is used.</summary>
            /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
            public Curve RequiredX => X ?? throw new System.InvalidOperationException("The member 'X' is not available.");

            /// <summary>Gets the y, which must already exist wherever it is used.</summary>
            /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
            public Curve RequiredY => Y ?? throw new System.InvalidOperationException("The member 'Y' is not available.");
        }

        /// <summary>Defines emitter definition.</summary>
        private sealed class EmitterDefinition
        {
            /// <summary>Stores image, type, name.</summary>
            public string? Image = string.Empty, Type = string.Empty, Name = string.Empty;
            /// <summary>Stores image frames.</summary>
            public int ImageFrames = 1;
            /// <summary>Stores random spin, loops, particle loops, additive, dont follow, has alpha, and fullscreen drawing.</summary>
            public bool RandomSpin, Loops, ParticleLoops, Additive, DontFollow, HasAlpha, FullScreen;
            /// <summary>Stores tracks.</summary>
            public readonly Dictionary<string, Curve> Tracks = new Dictionary<string, Curve>();
            /// <summary>Stores fields.</summary>
            public readonly List<Field> Fields = new List<Field>();
            /// <summary>Stores system fields.</summary>
            public readonly List<Field> SystemFields = new List<Field>();
            /// <summary>Reuses particles and their random buffers after expiry or system retirement.</summary>
            public readonly Stack<Particle> InactiveParticles = new Stack<Particle>();
            /// <summary>Performs track.</summary>
            /// <param name = "name">The name.</param>
            /// <param name = "fallback">The fallback.</param>
            /// <returns>The resulting track.</returns>
            public Curve Track(string name, float fallback = 0)
            {
                Curve curve;
                if (!Tracks.TryGetValue(name, out curve))
                {
                    curve = Curve.Parse(null, fallback);
                    Tracks[name] = curve;
                }

                return curve;
            }
        }

        /// <summary>Defines particle.</summary>
        private sealed class Particle
        {
            /// <summary>Stores position, previous position, velocity.</summary>
            public Vector2 Position, PreviousPosition, Velocity;
            /// <summary>Stores age, duration, angle, previous angle, spin velocity.</summary>
            public float Age, Duration, Angle, PreviousAngle, SpinVelocity;
            /// <summary>Stores the native normalized time sampled before the most recent age increment.</summary>
            public float Time;
            /// <summary>Retains each field's previous shake displacement so jitter never accumulates into drift.</summary>
            public Vector2[] PreviousShake = System.Array.Empty<Vector2>();
            /// <summary>Stores frame.</summary>
            public int Frame;
            /// <summary>Stores the normalized animation phase and its fixed rate interpolation sample.</summary>
            public float AnimationTime, AnimationRandom;
            /// <summary>Stores random.</summary>
            public float[] Random = System.Array.Empty<float>();
            /// <summary>Stores color red, color green, color blue, brightness.</summary>
            public float ColorRed, ColorGreen, ColorBlue, Brightness;
            /// <summary>Stores field random.</summary>
            public float[, ]? FieldRandom;

            /// <summary>Gets the field random, which must already exist wherever it is used.</summary>
            /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
            public float[, ] RequiredFieldRandom => FieldRandom ?? throw new System.InvalidOperationException("The member 'FieldRandom' is not available.");
        }

        /// <summary>Defines emitter.</summary>
        private sealed class Emitter
        {
            /// <summary>Creates an emitter that spawns particles from the given definition.</summary>
            /// <param name="definition">The immutable emitter definition parsed from the effect file.</param>
            public Emitter(EmitterDefinition definition)
            {
                Definition = definition;
            }

            /// <summary>Stores definition.</summary>
            public readonly EmitterDefinition Definition;

            /// <summary>Stores particles.</summary>
            public readonly List<Particle> Particles = new List<Particle>();
            /// <summary>Stores random.</summary>
            public readonly Dictionary<string, float> Random = new Dictionary<string, float>();
            /// <summary>Stores age, duration, launched.</summary>
            public int Age, Duration, Launched;
            /// <summary>Stores accumulated.</summary>
            public float Accumulated;
        }

        /// <summary>Defines system instance.</summary>
        private sealed class SystemInstance
        {
            /// <summary>Stores tick, seen, stop tick, fade duration.</summary>
            public int Tick, Seen, StopTick = -1, FadeDuration;
            /// <summary>Stores fade out.</summary>
            public EmitterDefinition? FadeOut;
            /// <summary>Stores origin.</summary>
            public Vector2 Origin;
            /// <summary>Stores random.</summary>
            public OriginalRandomAdapter? Random;

            /// <summary>Gets the random, which must already exist wherever it is used.</summary>
            /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
            public OriginalRandomAdapter RequiredRandom => Random ?? throw new System.InvalidOperationException("The member 'Random' is not available.");
            /// <summary>Stores emitters.</summary>
            public readonly List<Emitter> Emitters = new List<Emitter>();
        }

        /// <summary>Stores root.</summary>
        private readonly string _root;
        /// <summary>Stores renderer.</summary>
        private readonly ReanimationRenderer? _renderer;

        /// <summary>Gets the renderer, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        private ReanimationRenderer RequiredRenderer => _renderer ?? throw new System.InvalidOperationException("The member '_renderer' is not available.");
        /// <summary>Stores definitions.</summary>
        private readonly Dictionary<string, List<EmitterDefinition>?> _definitions = new Dictionary<string, List<EmitterDefinition>?>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Stores systems.</summary>
        private readonly Dictionary<string, SystemInstance> _systems = new Dictionary<string, SystemInstance>();
        /// <summary>Reuses the keys collected while retiring systems after a presentation frame.</summary>
        private readonly List<string> _expiredSystems = new List<string>();
        /// <summary>Stores frame.</summary>
        private int _frame;
        /// <summary>Gets or sets last error.</summary>
        public string LastError { get; private set; } = string.Empty;
        /// <summary>Gets the number of particle objects created before reuse could satisfy a spawn.</summary>
        internal int CreatedParticleCount { get; private set; }

        /// <summary>Initializes original particle renderer.</summary>
        /// <param name = "assetRoot">The asset root.</param>
        /// <param name = "animationRenderer">The animation renderer.</param>
        public OriginalParticleRenderer(string assetRoot, ReanimationRenderer? animationRenderer)
        {
            _root = assetRoot;
            _renderer = animationRenderer;
        }

        /// <summary>Performs number.</summary>
        /// <param name = "text">The text.</param>
        /// <returns>The resulting number.</returns>
        private static float Number(string text)
        {
            return float.Parse(text, CultureInfo.InvariantCulture);
        }

        /// <summary>Performs random01.</summary>
        /// <param name = "system">The system.</param>
        /// <returns>The resulting random01.</returns>
        private static float Random01(SystemInstance system)
        {
            return (float)system.RequiredRandom.NextDouble();
        }

        /// <summary>Performs system value.</summary>
        /// <param name = "system">The system.</param>
        /// <param name = "emitter">The emitter.</param>
        /// <param name = "name">The name.</param>
        /// <param name = "fallback">The fallback.</param>
        /// <returns>The resulting system value.</returns>
        private static float SystemValue(SystemInstance system, Emitter emitter, string name, float fallback = 0)
        {
            float random;
            if (!emitter.Random.TryGetValue(name, out random))
            {
                random = Random01(system);
                emitter.Random[name] = random;
            }

            return emitter.Definition.Track(name, fallback).Value(emitter.Age / (float)Math.Max(1, emitter.Duration - 1), random);
        }

        /// <summary>Performs begin frame.</summary>
        public void BeginFrame()
        {
            _frame++;
        }

        /// <summary>Performs end frame.</summary>
        public void EndFrame()
        {
            _expiredSystems.Clear();
            foreach (var pair in _systems)
            {
                if (pair.Value.Seen != _frame)
                {
                    _expiredSystems.Add(pair.Key);
                }
            }

            foreach (var key in _expiredSystems)
            {
                ReleaseSystemParticles(_systems[key]);
                _systems.Remove(key);
            }

            _expiredSystems.Clear();
        }

        /// <summary>Performs clear.</summary>
        public void Clear()
        {
            foreach (var system in _systems.Values)
            {
                ReleaseSystemParticles(system);
            }

            _systems.Clear();
        }

        /// <summary>Performs draw.</summary>
        /// <param name = "key">The key.</param>
        /// <param name = "name">The name.</param>
        /// <param name = "age">The age.</param>
        /// <param name = "origin">The origin.</param>
        /// <param name = "seed">The seed.</param>
        /// <param name = "scale">The scale.</param>
        /// <param name = "stopAge">The stop age.</param>
        /// <param name="fullScreenMatrix">The unshifted viewport transform for original fullscreen particle fills.</param>
        /// <returns>The resulting draw.</returns>
        public bool Draw(string key, string name, float age, Vector2 origin, int seed, float scale = 1f, float stopAge = -1f, Matrix4x4? fullScreenMatrix = null)
        {
            SystemInstance system;
            if (!_systems.TryGetValue(key, out system))
            {
                var list = Load(name);
                if (list == null)
                {
                    return false;
                }

                system = new SystemInstance
                {
                    Random = new OriginalRandomAdapter(unchecked((uint)seed)),
                    Origin = origin
                };
                foreach (var definition in list)
                {
                    if (string.IsNullOrEmpty(definition.Image))
                    {
                        if (definition.Name == "FadeOut")
                        {
                            system.FadeOut = definition;
                            system.FadeDuration = Math.Max(1, (int)definition.Track("CrossFadeDuration", 1).Value(0, .5f));
                        }

                        continue;
                    }

                    var emitter = new Emitter(definition);
                    emitter.Duration = Math.Max(1, (int)definition.Track("SystemDuration", definition.Track("ParticleDuration", 100).Value(0, 1)).Value(0, Random01(system)));
                    system.Emitters.Add(emitter);
                }

                _systems.Add(key, system);
                if (stopAge >= 0f)
                {
                    system.StopTick = Math.Max(0, (int)Math.Round(stopAge * 100f));
                }

                Step(system);
            }

            system.Seen = _frame;
            if (stopAge >= 0f)
            {
                system.StopTick = Math.Max(0, (int)Math.Round(stopAge * 100f));
            }

            var target = Math.Max(0, (int)Math.Floor(age * 100f));
            int firstTick = system.Tick, span = target - firstTick;
            var firstOrigin = system.Origin;
            while (system.Tick < target)
            {
                // The original trail flag leaves existing particles in world space.
                // Newly emitted flakes use the emitter position at their 100Hz birth update.
                MoveSystemOrigin(system, Vector2.Lerp(firstOrigin, origin, (system.Tick - firstTick + 1f) / span), scale);
                Step(system);
                system.Tick++;
            }

            MoveSystemOrigin(system, origin, scale);
            var fraction = age * 100f - target;
            var previous = SpriteDrawState.Color;
            foreach (Emitter emitter in system.Emitters)
            {
                var systemAlpha = SystemValue(system, emitter, "SystemAlpha", 1);
                foreach (Particle particle in emitter.Particles)
                {
                    var time = Mathf.Clamp01(particle.Time);
                    var alpha = emitter.Definition.Track("ParticleAlpha", 1).Value(time, particle.Random[0]) * systemAlpha;
                    if (system.StopTick >= 0 && target >= system.StopTick && system.FadeOut != null)
                    {
                        // PC 52C940/52C460 keep the source particle alive; 52CBF0 inherits
                        // undefined target attributes and blends the explicitly defined alpha.
                        var fade = Mathf.Clamp01((target - system.StopTick) / (float)Math.Max(1, system.FadeDuration - 1));
                        var targetAlpha = system.FadeOut.Track("ParticleAlpha", 1).Value(fade, particle.Random[0]) * system.FadeOut.Track("SystemAlpha", 1).Value(fade, particle.Random[0]);
                        alpha = emitter.Definition.HasAlpha ? Mathf.Lerp(alpha, targetAlpha, fade) : targetAlpha;
                    }

                    if (alpha <= 0)
                    {
                        continue;
                    }

                    var size = emitter.Definition.Track("ParticleScale", 1).Value(time, particle.Random[1]);
                    var stretch = emitter.Definition.Track("ParticleStretch", 1).Value(time, particle.Random[2]);
                    var brightness = emitter.Definition.Track("ParticleBrightness", 1).Value(time, particle.Brightness);
                    SpriteDrawState.Color = new Color(previous.r * emitter.Definition.Track("ParticleRed", 1).Value(time, particle.ColorRed) * brightness, previous.g * emitter.Definition.Track("ParticleGreen", 1).Value(time, particle.ColorGreen) * brightness, previous.b * emitter.Definition.Track("ParticleBlue", 1).Value(time, particle.ColorBlue) * brightness, previous.a * Mathf.Clamp01(alpha));
                    if (emitter.Definition.FullScreen)
                    {
                        DrawFullScreen(fullScreenMatrix ?? SpriteDrawState.Matrix);
                        continue;
                    }

                    var position = Vector2.Lerp(particle.PreviousPosition, particle.Position, fraction);
                    var clipTop = emitter.Definition.Track("ClipTop").Value(time, .5f);
                    var clipBottom = emitter.Definition.Track("ClipBottom").Value(time, .5f);
                    var imageRow = emitter.Definition.Tracks.ContainsKey("ImageRow") ? (int)SystemValue(system, emitter, "ImageRow") : 0;
                    var imageFrame = emitter.Definition.Tracks.ContainsKey("AnimationRate")
                        ? Math.Max(0, Math.Min(emitter.Definition.ImageFrames - 1, (int)(particle.AnimationTime * emitter.Definition.ImageFrames)))
                        : particle.Frame;
                    RequiredRenderer.DrawResourceSprite(emitter.Definition.Image, origin + position * scale, size * scale, Mathf.Lerp(particle.PreviousAngle, particle.Angle, fraction),
                        imageFrame, 0, 0, stretch, emitter.Definition.Additive, clipTop, clipBottom, imageRow);
                }
            }

            SpriteDrawState.Color = previous;
            return true;
        }

        /// <summary>Fills the original PC viewport without inheriting a particle's position or scale.</summary>
        /// <param name="viewport">The unshifted logical-to-screen transform.</param>
        private void DrawFullScreen(Matrix4x4 viewport)
        {
            // PC TodParticleDraw 0x52D370: flag 0x200 fills (-gTransX, -gTransY, 800, 600).
            var previous = SpriteDrawState.Matrix;
            try
            {
                SpriteDrawState.Matrix = viewport;
                RequiredRenderer.DrawNativeImage(Texture2D.whiteTexture, new Rect(0f, 0f, 800f, 600f), new Rect(0f, 0f, 1f, 1f));
            }
            finally
            {
                SpriteDrawState.Matrix = previous;
            }
        }

        /// <summary>Performs move system origin.</summary>
        /// <param name = "system">The system.</param>
        /// <param name = "origin">The origin.</param>
        /// <param name = "scale">The scale.</param>
        private static void MoveSystemOrigin(SystemInstance system, Vector2 origin, float scale)
        {
            var delta = (origin - system.Origin) / Math.Max(.000001f, scale);
            if (delta.sqrMagnitude > 0f)
            {
                foreach (Emitter emitter in system.Emitters)
                {
                    if (emitter.Definition.DontFollow)
                    {
                        foreach (Particle particle in emitter.Particles)
                        {
                            particle.Position -= delta;
                            particle.PreviousPosition -= delta;
                        }
                    }
                }
            }

            system.Origin = origin;
        }

        /// <summary>Performs load.</summary>
        /// <param name = "name">The name.</param>
        /// <returns>The resulting load.</returns>
        private List<EmitterDefinition>? Load(string name)
        {
            List<EmitterDefinition>? result;
            if (_definitions.TryGetValue(name, out result))
            {
                return result;
            }

            var path = Path.Combine(_root, "particles", name + ".xml");
            if (!File.Exists(path))
            {
                _definitions[name] = null;
                LastError = "Original particle definition missing: " + name;
                return null;
            }

            var xml = new XmlDocument
            {
                XmlResolver = null
            };
            xml.LoadXml("<Particles>" + File.ReadAllText(path) + "</Particles>");
            result = new List<EmitterDefinition>();
            foreach (XmlNode node in xml.SelectNodes("/Particles/Emitter"))
            {
                var image = node.SelectSingleNode("Image") == null ? null : node.SelectSingleNode("Image").InnerText;
                var emitterName = node.SelectSingleNode("Name") == null ? null : node.SelectSingleNode("Name").InnerText;
                if (string.IsNullOrEmpty(image) && emitterName != "FadeOut")
                {
                    continue;
                }

                var definition = new EmitterDefinition
                {
                    Image = image,
                    Type = "Box",
                    Name = emitterName
                };
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (child.NodeType != XmlNodeType.Element)
                    {
                        continue;
                    }

                    switch (child.Name)
                    {
                        case "Image":
                        case "Name":
                            break;
                        case "ParticleAlpha":
                        case "SystemAlpha":
                            definition.HasAlpha = true;
                            definition.Tracks[child.Name] = Curve.Parse(child.InnerText, 1);
                            break;
                        case "ParticlesDontFollow":
                            definition.DontFollow = child.InnerText.Trim() == "1";
                            break;
                        case "EmitterType":
                            definition.Type = child.InnerText;
                            break;
                        case "ImageFrames":
                            definition.ImageFrames = int.Parse(child.InnerText, CultureInfo.InvariantCulture);
                            break;
                        case "RandomLaunchSpin":
                            definition.RandomSpin = child.InnerText == "1";
                            break;
                        case "SystemLoops":
                            definition.Loops = child.InnerText == "1";
                            break;
                        case "ParticleLoops":
                            definition.ParticleLoops = child.InnerText == "1";
                            break;
                        case "Additive":
                            definition.Additive = child.InnerText == "1";
                            break;
                        case "FullScreen":
                            definition.FullScreen = child.InnerText.Trim() == "1";
                            break;
                        case "SystemField":
                            definition.SystemFields.Add(new Field {
                                Type = child.SelectSingleNode("FieldType").InnerText,
                                X = Curve.Parse(child.SelectSingleNode("X") == null ? null : child.SelectSingleNode("X").InnerText, 0),
                                Y = Curve.Parse(child.SelectSingleNode("Y") == null ? null : child.SelectSingleNode("Y").InnerText, 0)
                            });
                            break;
                        case "Field":
                            definition.Fields.Add(new Field {
                                Type = child.SelectSingleNode("FieldType").InnerText,
                                X = Curve.Parse(child.SelectSingleNode("X") == null ? null : child.SelectSingleNode("X").InnerText, 0),
                                Y = Curve.Parse(child.SelectSingleNode("Y") == null ? null : child.SelectSingleNode("Y").InnerText, 0)
                            });
                            break;
                        default:
                            definition.Tracks[child.Name] = Curve.Parse(child.InnerText, 0);
                            break;
                    }
                }

                result.Add(definition);
            }

            _definitions[name] = result;
            return result;
        }

        /// <summary>Performs spawn.</summary>
        /// <param name = "system">The system.</param>
        /// <param name = "emitter">The emitter.</param>
        private void Spawn(SystemInstance system, Emitter emitter)
        {
            var definition = emitter.Definition;
            Particle particle;
            if (definition.InactiveParticles.Count > 0)
            {
                particle = definition.InactiveParticles.Pop();
            }
            else
            {
                particle = new Particle
                {
                    Random = new float[8],
                    FieldRandom = new float[definition.Fields.Count, 2],
                    PreviousShake = new Vector2[definition.Fields.Count]
                };
                CreatedParticleCount++;
            }

            particle.Age = 0f;
            particle.Time = -1f;
            System.Array.Clear(particle.PreviousShake, 0, particle.PreviousShake.Length);
            particle.SpinVelocity = 0f;
            particle.AnimationTime = 0f;
            for (var i = 0; i < particle.Random.Length; i++)
            {
                particle.Random[i] = Random01(system);
            }

            for (var i = 0; i < definition.Fields.Count; i++)
            {
                for (var j = 0; j < 2; j++)
                {
                    particle.RequiredFieldRandom[i, j] = Random01(system);
                }
            }

            particle.Duration = Math.Max(1, definition.Track("ParticleDuration", 100).Value(emitter.Age / (float)Math.Max(1, emitter.Duration - 1), Random01(system)));
            var systemTime = emitter.Age / (float)Math.Max(1, emitter.Duration - 1);
            var angle = definition.Tracks.ContainsKey("LaunchAngle") ? definition.Track("LaunchAngle").Value(systemTime, Random01(system)) * Mathf.Deg2Rad : Random01(system) * 2 * Mathf.PI;
            var radius = definition.Track("EmitterRadius").Value(systemTime, Random01(system));
            if (definition.Type == "Circle")
            {
                particle.Position = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius;
            }
            else
            {
                particle.Position = new Vector2(definition.Track("EmitterBoxX").Value(systemTime, Random01(system)), definition.Track("EmitterBoxY").Value(systemTime, Random01(system)));
            }

            if (definition.Tracks.ContainsKey("EmitterSkewX") || definition.Tracks.ContainsKey("EmitterSkewY"))
            {
                // PC SpawnParticle 0x52BBF6..0x52BC30 applies both skews to the original unskewed box position.
                var box = particle.Position;
                var skewX = definition.Track("EmitterSkewX").Value(systemTime, Random01(system));
                var skewY = definition.Track("EmitterSkewY").Value(systemTime, Random01(system));
                particle.Position = new Vector2(box.x + box.y * skewX, box.y + box.x * skewY);
            }

            // Original SodRoll.xml moves the emitter's system centre; already spawned dirt stays behind.
            foreach (Field systemField in definition.SystemFields)
            {
                if (systemField.Type == "SystemPosition")
                {
                    particle.Position += new Vector2(systemField.RequiredX.Value(systemTime, .5f), systemField.RequiredY.Value(systemTime, .5f));
                }
            }

            particle.Position += new Vector2(SystemValue(system, emitter, "EmitterOffsetX"), SystemValue(system, emitter, "EmitterOffsetY"));
            var speed = definition.Track("LaunchSpeed").Value(0, Random01(system)) * .01f;
            particle.Velocity = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * speed;
            particle.ColorRed = Random01(system);
            particle.ColorGreen = Random01(system);
            particle.ColorBlue = Random01(system);
            particle.Brightness = Random01(system);
            particle.Angle = definition.Track("ParticleSpinAngle").Value(0, Random01(system)) + (definition.RandomSpin ? Random01(system) * 360f : 0);
            particle.PreviousPosition = particle.Position;
            particle.PreviousAngle = particle.Angle;
            if (definition.Tracks.ContainsKey("AnimationRate"))
            {
                // Original animated particles always start at the first atlas frame.
                particle.AnimationRandom = Random01(system);
                particle.Frame = 0;
            }
            else
            {
                particle.Frame = system.RequiredRandom.Next(Math.Max(1, definition.ImageFrames));
            }

            // PC SpawnParticle 0x52BD35 updates once before the particle can first be drawn.
            AdvanceParticle(system, definition, particle);
            particle.PreviousPosition = particle.Position;
            particle.PreviousAngle = particle.Angle;
            emitter.Particles.Add(particle);
            emitter.Launched++;
        }

        /// <summary>Samples one native particle update, including the update performed at birth.</summary>
        /// <param name="system">The effect owning the replayable decorative random stream.</param>
        /// <param name="definition">The original emitter's authored fields and tracks.</param>
        /// <param name="particle">The live particle whose current sample and next age are advanced.</param>
        private void AdvanceParticle(SystemInstance system, EmitterDefinition definition, Particle particle)
        {
            particle.PreviousPosition = particle.Position;
            particle.PreviousAngle = particle.Angle;
            var time = particle.Age / Math.Max(1f, particle.Duration - 1f);
            var last = particle.Time;
            for (var i = 0; i < definition.Fields.Count; i++)
            {
                var field = definition.Fields[i];
                var x = field.RequiredX.Value(time, particle.RequiredFieldRandom[i, 0]);
                var y = field.RequiredY.Value(time, particle.RequiredFieldRandom[i, 1]);
                switch (field.Type)
                {
                    case "Friction":
                        particle.Velocity = new Vector2(particle.Velocity.x * (1 - x), particle.Velocity.y * (1 - y));
                        break;
                    case "Acceleration":
                        particle.Velocity += new Vector2(x, y) * .01f;
                        break;
                    case "Velocity":
                        particle.Position += new Vector2(x, y) * .01f;
                        break;
                    case "Position":
                        // PC FloatTrackEvaluateFromLastTime 0x52BD50 returns zero before the first sample.
                        var previousX = last < 0f ? 0f : field.RequiredX.Value(last, particle.RequiredFieldRandom[i, 0]);
                        var previousY = last < 0f ? 0f : field.RequiredY.Value(last, particle.RequiredFieldRandom[i, 1]);
                        particle.Position += new Vector2(x - previousX, y - previousY);
                        break;
                    case "Shake":
                        // PC UpdateParticleField 0x52C04A removes the previous jitter before adding this tick's sample.
                        var shake = new Vector2((Random01(system) * 2 - 1) * x, (Random01(system) * 2 - 1) * y);
                        particle.Position += shake - particle.PreviousShake[i];
                        particle.PreviousShake[i] = shake;
                        break;
                    case "Circle":
                        var tangent = new Vector2(-particle.Position.y, particle.Position.x).normalized;
                        particle.Position += tangent * ((x + particle.Position.magnitude * y) * .01f);
                        break;
                    case "Away":
                        particle.Position += particle.Position.normalized * ((x + particle.Position.magnitude * y) * .01f);
                        break;
                    case "GroundConstraint":
                        if (particle.Position.y >= y)
                        {
                            particle.Position = new Vector2(particle.Position.x, y);
                            var reflect = definition.Track("CollisionReflect").Value(time, particle.Random[6]);
                            particle.SpinVelocity = particle.Velocity.y * definition.Track("CollisionSpin").Value(time, particle.Random[7]) / 1000f * Mathf.Rad2Deg;
                            particle.Velocity = new Vector2(particle.Velocity.x * reflect, -particle.Velocity.y * reflect);
                        }

                        break;
                }
            }

            particle.Position += particle.Velocity;
            particle.Angle += definition.Track("ParticleSpinSpeed").Value(time, particle.Random[3]) * .01f + particle.SpinVelocity;
            if (definition.Tracks.TryGetValue("AnimationRate", out var animationRate))
            {
                // PC 0x52C460 advances a normalized cycle, not a frame count, each 100 Hz update.
                particle.AnimationTime += animationRate.Value(time, particle.AnimationRandom) * .01f;
                particle.AnimationTime -= Mathf.Floor(particle.AnimationTime);
            }

            particle.Time = time;
            particle.Age++;
        }

        /// <summary>Performs step.</summary>
        /// <param name = "system">The system.</param>
        private void Step(SystemInstance system)
        {
            foreach (Emitter emitter in system.Emitters)
            {
                var definition = emitter.Definition;
                var stopped = system.StopTick >= 0 && system.Tick >= system.StopTick;
                var fading = stopped && system.FadeOut != null && system.Tick - system.StopTick < system.FadeDuration;
                if (stopped && !fading)
                {
                    ReleaseEmitterParticles(emitter);
                    continue;
                }

                for (var index = emitter.Particles.Count - 1; index >= 0; index--)
                {
                    var particle = emitter.Particles[index];
                    if (particle.Age >= particle.Duration)
                    {
                        if (definition.ParticleLoops)
                        {
                            particle.Age = 0;
                        }
                        else if (fading)
                        {
                            particle.Age = particle.Duration - 1;
                        }
                        else
                        {
                            emitter.Particles.RemoveAt(index);
                            ReturnParticle(definition, particle);
                            continue;
                        }
                    }

                    AdvanceParticle(system, definition, particle);
                }

                if (!stopped && (emitter.Age < emitter.Duration || definition.Loops))
                {
                    emitter.Accumulated += SystemValue(system, emitter, "SpawnRate") * .01f;
                    var spawn = Math.Max((int)emitter.Accumulated, (int)SystemValue(system, emitter, "SpawnMinActive", -1) - emitter.Particles.Count);
                    emitter.Accumulated -= (int)emitter.Accumulated;
                    if (definition.Tracks.ContainsKey("SpawnMaxLaunched"))
                    {
                        spawn = Math.Min(spawn, (int)SystemValue(system, emitter, "SpawnMaxLaunched") - emitter.Launched);
                    }

                    if (definition.Tracks.ContainsKey("SpawnMaxActive"))
                    {
                        spawn = Math.Min(spawn, (int)SystemValue(system, emitter, "SpawnMaxActive") - emitter.Particles.Count);
                    }

                    for (var i = 0; i < spawn; i++)
                    {
                        Spawn(system, emitter);
                    }
                }

                emitter.Age++;
                if (definition.Loops && emitter.Age >= emitter.Duration)
                {
                    emitter.Age = 0;
                }
            }
        }

        /// <summary>Returns the particles owned by every emitter of a retired effect system.</summary>
        /// <param name="system">The system omitted by presentation or explicitly cleared.</param>
        private static void ReleaseSystemParticles(SystemInstance system)
        {
            foreach (var emitter in system.Emitters)
            {
                ReleaseEmitterParticles(emitter);
            }
        }

        /// <summary>Retires active particles while retaining the emitter list's reusable capacity.</summary>
        /// <param name="emitter">The emitter whose particles are no longer displayed.</param>
        private static void ReleaseEmitterParticles(Emitter emitter)
        {
            foreach (var particle in emitter.Particles)
            {
                ReturnParticle(emitter.Definition, particle);
            }

            emitter.Particles.Clear();
        }

        /// <summary>Retains a bounded particle buffer for a later spawn using the same immutable field layout.</summary>
        /// <param name="definition">The definition owning the particle's fixed random-buffer dimensions.</param>
        /// <param name="particle">The expired particle to make available for reuse.</param>
        private static void ReturnParticle(EmitterDefinition definition, Particle particle)
        {
            if (definition.InactiveParticles.Count < MaximumRetainedParticles)
            {
                definition.InactiveParticles.Push(particle);
            }
        }
    }
}
