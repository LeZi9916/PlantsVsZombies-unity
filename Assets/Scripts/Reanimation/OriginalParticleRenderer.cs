using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEngine;
using PvZReplica.Randomness;

namespace PvZReplica.Reanimation
{
    /// <summary>Evaluates imported original particle emitters. No effect is defined by this renderer.</summary>
    public sealed class OriginalParticleRenderer
    {
        private sealed class Node { public float Time, Minimum, Maximum; public string Shape = "Linear"; }
        private sealed class Curve
        {
            public readonly List<Node> Nodes = new List<Node>();
            public float Value(float time, float random)
            {
                Node left = Nodes[0];
                for (int i = 1; i < Nodes.Count; i++)
                {
                    Node right = Nodes[i];
                    if (time <= right.Time)
                    {
                        float t = right.Time <= left.Time ? 0f : Mathf.Clamp01((time - left.Time) / (right.Time - left.Time));
                        return Mathf.Lerp(Mathf.Lerp(left.Minimum, left.Maximum, random), Mathf.Lerp(right.Minimum, right.Maximum, random), ApplyShape(t, left.Shape));
                    }
                    left = right;
                }
                return Mathf.Lerp(left.Minimum, left.Maximum, random);
            }
            private static float ApplyShape(float t, string shape)
            {
                switch (shape)
                {
                    case "Constant": return 0;
                    case "EaseIn": return t * t;
                    case "EaseOut": return 2 * t - t * t;
                    case "EaseInOutWeak": return 3 * t * t - 2 * t * t * t;
                    case "EaseInOut": float s = 3 * t * t - 2 * t * t * t; return 3 * s * s - 2 * s * s * s;
                    default: return t;
                }
            }
            public static Curve Parse(string source, float fallback)
            {
                Curve curve = new Curve();
                if (string.IsNullOrWhiteSpace(source)) { curve.Nodes.Add(new Node { Minimum = fallback, Maximum = fallback }); return curve; }
                const string number = @"[-+]?(?:\d*\.\d+|\d+\.?\d*)(?:[eE][-+]?\d+)?";
                MatchCollection values = Regex.Matches(source, @"(?:\[\s*(" + number + @")(?:\s+(" + number + @"))?\s*\]|(" + number + @"))(?:\s*,\s*(" + number + @"))?");
                foreach (Match value in values)
                {
                    float minimum = Number(value.Groups[1].Success ? value.Groups[1].Value : value.Groups[3].Value);
                    curve.Nodes.Add(new Node { Minimum = minimum, Maximum = value.Groups[2].Success ? Number(value.Groups[2].Value) : minimum,
                        Time = value.Groups[4].Success ? Number(value.Groups[4].Value) * .01f : float.NaN });
                }
                for (int i = 0; i + 1 < values.Count; i++)
                {
                    string gap = source.Substring(values[i].Index + values[i].Length, values[i + 1].Index - values[i].Index - values[i].Length).Trim();
                    if (gap.Length > 0) curve.Nodes[i].Shape = gap;
                }
                if (curve.Nodes.Count == 0) throw new FormatException("Unsupported original particle curve: " + source);
                if (float.IsNaN(curve.Nodes[0].Time)) curve.Nodes[0].Time = 0f;
                if (curve.Nodes.Count > 1 && float.IsNaN(curve.Nodes[curve.Nodes.Count - 1].Time)) curve.Nodes[curve.Nodes.Count - 1].Time = 1f;
                for (int i = 1; i < curve.Nodes.Count - 1; i++)
                {
                    if (!float.IsNaN(curve.Nodes[i].Time)) continue;
                    int end = i + 1; while (end < curve.Nodes.Count && float.IsNaN(curve.Nodes[end].Time)) end++;
                    float beginTime = curve.Nodes[i - 1].Time, endTime = curve.Nodes[end].Time;
                    int begin = i - 1;
                    for (; i < end; i++) curve.Nodes[i].Time = Mathf.Lerp(beginTime, endTime, (i - begin) / (float)(end - begin));
                    i--;
                }
                return curve;
            }
        }
        private sealed class Field
        {
            public string Type; public Curve X, Y;
        }
        private sealed class EmitterDefinition
        {
            public string Image, Type, Name;
            public int ImageFrames = 1;
            public bool RandomSpin, Loops, ParticleLoops, Additive, DontFollow, HasAlpha;
            public readonly Dictionary<string, Curve> Tracks = new Dictionary<string, Curve>();
            public readonly List<Field> Fields = new List<Field>();
            public readonly List<Field> SystemFields = new List<Field>();
            public Curve Track(string name, float fallback = 0)
            { Curve curve; if (!Tracks.TryGetValue(name, out curve)) { curve = Curve.Parse(null, fallback); Tracks[name] = curve; } return curve; }
        }
        private sealed class Particle
        {
            public Vector2 Position, PreviousPosition, Velocity;
            public float Age, Duration, Angle, PreviousAngle, SpinVelocity;
            public int Frame;
            public float[] Random;
            public float ColorRed, ColorGreen, ColorBlue, Brightness;
            public float[,] FieldRandom;
        }
        private sealed class Emitter
        {
            public EmitterDefinition Definition;
            public readonly List<Particle> Particles = new List<Particle>();
            public readonly Dictionary<string, float> Random = new Dictionary<string, float>();
            public int Age, Duration, Launched;
            public float Accumulated;
        }
        private sealed class SystemInstance
        {
            public int Tick, Seen, StopTick = -1, FadeDuration;
            public EmitterDefinition FadeOut;
            public Vector2 Origin;
            public OriginalRandomAdapter Random;
            public readonly List<Emitter> Emitters = new List<Emitter>();
        }
        private readonly string root;
        private readonly ReanimationRenderer renderer;
        private readonly Dictionary<string, List<EmitterDefinition>> definitions = new Dictionary<string, List<EmitterDefinition>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, SystemInstance> systems = new Dictionary<string, SystemInstance>();
        private int frame;
        public string LastError { get; private set; }
        public OriginalParticleRenderer(string assetRoot, ReanimationRenderer animationRenderer) { root = assetRoot; renderer = animationRenderer; }
        private static float Number(string text) { return float.Parse(text, CultureInfo.InvariantCulture); }
        private static float Random01(SystemInstance system) { return (float)system.Random.NextDouble(); }
        private static float SystemValue(SystemInstance system, Emitter emitter, string name, float fallback = 0)
        {
            float random;
            if (!emitter.Random.TryGetValue(name, out random)) { random = Random01(system); emitter.Random[name] = random; }
            return emitter.Definition.Track(name, fallback).Value(emitter.Age / (float)Math.Max(1, emitter.Duration - 1), random);
        }
        private static bool CanRenderFrame { get { return Event.current == null || Event.current.type == EventType.Repaint; } }
        public void BeginFrame() { if (CanRenderFrame) frame++; }
        public void EndFrame()
        {
            if (!CanRenderFrame) return;
            var expired = new List<string>();
            foreach (var pair in systems) if (pair.Value.Seen != frame) expired.Add(pair.Key);
            foreach (string key in expired) systems.Remove(key);
        }
        public void Clear() { systems.Clear(); }
        public bool Draw(string key, string name, float age, Vector2 origin, int seed, float scale = 1f, float stopAge = -1f)
        {
            if (Event.current != null && Event.current.type != EventType.Repaint) return true;
            SystemInstance system;
            if (!systems.TryGetValue(key, out system))
            {
                List<EmitterDefinition> list = Load(name);
                if (list == null) return false;
                system = new SystemInstance { Random = new OriginalRandomAdapter(unchecked((uint)seed)), Origin = origin };
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
                    var emitter = new Emitter { Definition = definition };
                    emitter.Duration = Math.Max(1, (int)definition.Track("SystemDuration", definition.Track("ParticleDuration", 100).Value(0, 1)).Value(0, Random01(system)));
                    system.Emitters.Add(emitter);
                }
                systems.Add(key, system);
                if (stopAge >= 0f) system.StopTick = Math.Max(0, (int)Math.Round(stopAge * 100f));
                Step(system);
            }
            system.Seen = frame;
            if (stopAge >= 0f) system.StopTick = Math.Max(0, (int)Math.Round(stopAge * 100f));
            int target = Math.Max(0, (int)Math.Floor(age * 100f));
            int firstTick = system.Tick, span = target - firstTick;
            Vector2 firstOrigin = system.Origin;
            while (system.Tick < target)
            {
                // The original trail flag leaves existing particles in world space.
                // Newly emitted flakes use the emitter position at their 100Hz birth update.
                MoveSystemOrigin(system, Vector2.Lerp(firstOrigin, origin, (system.Tick - firstTick + 1f) / span), scale);
                Step(system); system.Tick++;
            }
            MoveSystemOrigin(system, origin, scale);
            float fraction = age * 100f - target;
            Color previous = GUI.color;
            foreach (Emitter emitter in system.Emitters)
            {
                float systemAlpha = SystemValue(system, emitter, "SystemAlpha", 1);
                foreach (Particle particle in emitter.Particles)
                {
                    float time = Mathf.Clamp01(particle.Age / Math.Max(1f, particle.Duration - 1f));
                    float alpha = emitter.Definition.Track("ParticleAlpha", 1).Value(time, particle.Random[0]) * systemAlpha;
                    if (system.StopTick >= 0 && target >= system.StopTick && system.FadeOut != null)
                    {
                        // PC 52C940/52C460 keep the source particle alive; 52CBF0 inherits
                        // undefined target attributes and blends the explicitly defined alpha.
                        float fade = Mathf.Clamp01((target - system.StopTick) / (float)Math.Max(1, system.FadeDuration - 1));
                        float targetAlpha = system.FadeOut.Track("ParticleAlpha", 1).Value(fade, particle.Random[0]) *
                            system.FadeOut.Track("SystemAlpha", 1).Value(fade, particle.Random[0]);
                        alpha = emitter.Definition.HasAlpha ? Mathf.Lerp(alpha, targetAlpha, fade) : targetAlpha;
                    }
                    if (alpha <= 0) continue;
                    float size = emitter.Definition.Track("ParticleScale", 1).Value(time, particle.Random[1]);
                    float stretch = emitter.Definition.Track("ParticleStretch", 1).Value(time, particle.Random[2]);
                    float brightness = emitter.Definition.Track("ParticleBrightness", 1).Value(time, particle.Brightness);
                    GUI.color = new Color(previous.r * emitter.Definition.Track("ParticleRed", 1).Value(time, particle.ColorRed) * brightness,
                        previous.g * emitter.Definition.Track("ParticleGreen", 1).Value(time, particle.ColorGreen) * brightness,
                        previous.b * emitter.Definition.Track("ParticleBlue", 1).Value(time, particle.ColorBlue) * brightness, previous.a * Mathf.Clamp01(alpha));
                    Vector2 position = Vector2.Lerp(particle.PreviousPosition, particle.Position, fraction);
                    renderer.DrawResourceSprite(emitter.Definition.Image, origin + position * scale, size * scale,
                        Mathf.Lerp(particle.PreviousAngle, particle.Angle, fraction), particle.Frame, 0, 0, stretch, emitter.Definition.Additive);
                }
            }
            GUI.color = previous; return true;
        }
        private static void MoveSystemOrigin(SystemInstance system, Vector2 origin, float scale)
        {
            Vector2 delta = (origin - system.Origin) / Math.Max(.000001f, scale);
            if (delta.sqrMagnitude > 0f)
                foreach (Emitter emitter in system.Emitters)
                    if (emitter.Definition.DontFollow)
                        foreach (Particle particle in emitter.Particles)
                        { particle.Position -= delta; particle.PreviousPosition -= delta; }
            system.Origin = origin;
        }
        private List<EmitterDefinition> Load(string name)
        {
            List<EmitterDefinition> result;
            if (definitions.TryGetValue(name, out result)) return result;
            string path = Path.Combine(root, "particles", name + ".xml");
            if (!File.Exists(path)) { definitions[name] = null; LastError = "Original particle definition missing: " + name; return null; }
            XmlDocument xml = new XmlDocument { XmlResolver = null };
            xml.LoadXml("<Particles>" + File.ReadAllText(path) + "</Particles>");
            result = new List<EmitterDefinition>();
            foreach (XmlNode node in xml.SelectNodes("/Particles/Emitter"))
            {
                string image = node.SelectSingleNode("Image") == null ? null : node.SelectSingleNode("Image").InnerText;
                string emitterName = node.SelectSingleNode("Name") == null ? null : node.SelectSingleNode("Name").InnerText;
                if (string.IsNullOrEmpty(image) && emitterName != "FadeOut") continue;
                var definition = new EmitterDefinition { Image = image, Type = "Box", Name = emitterName };
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (child.NodeType != XmlNodeType.Element) continue;
                    switch (child.Name)
                    {
                        case "Image": case "Name": break;
                        case "ParticleAlpha": case "SystemAlpha": definition.HasAlpha = true; definition.Tracks[child.Name] = Curve.Parse(child.InnerText, 1); break;
                        case "ParticlesDontFollow": definition.DontFollow = child.InnerText.Trim() == "1"; break;
                        case "EmitterType": definition.Type = child.InnerText; break;
                        case "ImageFrames": definition.ImageFrames = int.Parse(child.InnerText, CultureInfo.InvariantCulture); break;
                        case "RandomLaunchSpin": definition.RandomSpin = child.InnerText == "1"; break;
                        case "SystemLoops": definition.Loops = child.InnerText == "1"; break;
                        case "ParticleLoops": definition.ParticleLoops = child.InnerText == "1"; break;
                        case "Additive": definition.Additive = child.InnerText == "1"; break;
                        case "SystemField":
                            definition.SystemFields.Add(new Field { Type = child.SelectSingleNode("FieldType").InnerText,
                                X = Curve.Parse(child.SelectSingleNode("X") == null ? null : child.SelectSingleNode("X").InnerText, 0),
                                Y = Curve.Parse(child.SelectSingleNode("Y") == null ? null : child.SelectSingleNode("Y").InnerText, 0) }); break;
                        case "Field":
                            definition.Fields.Add(new Field { Type = child.SelectSingleNode("FieldType").InnerText,
                                X = Curve.Parse(child.SelectSingleNode("X") == null ? null : child.SelectSingleNode("X").InnerText, 0),
                                Y = Curve.Parse(child.SelectSingleNode("Y") == null ? null : child.SelectSingleNode("Y").InnerText, 0) }); break;
                        default: definition.Tracks[child.Name] = Curve.Parse(child.InnerText, 0); break;
                    }
                }
                result.Add(definition);
            }
            definitions[name] = result; return result;
        }
        private static void Spawn(SystemInstance system, Emitter emitter)
        {
            EmitterDefinition definition = emitter.Definition;
            var particle = new Particle { Random = new float[8], FieldRandom = new float[definition.Fields.Count, 2] };
            for (int i = 0; i < particle.Random.Length; i++) particle.Random[i] = Random01(system);
            for (int i = 0; i < definition.Fields.Count; i++) for (int j = 0; j < 2; j++) particle.FieldRandom[i, j] = Random01(system);
            particle.Duration = Math.Max(1, definition.Track("ParticleDuration", 100).Value(emitter.Age / (float)Math.Max(1, emitter.Duration - 1), Random01(system)));
            float systemTime = emitter.Age / (float)Math.Max(1, emitter.Duration - 1);
            float angle = definition.Tracks.ContainsKey("LaunchAngle") ? definition.Track("LaunchAngle").Value(systemTime, Random01(system)) * Mathf.Deg2Rad : Random01(system) * 2 * Mathf.PI;
            float radius = definition.Track("EmitterRadius").Value(systemTime, Random01(system));
            if (definition.Type == "Circle") particle.Position = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * radius;
            else particle.Position = new Vector2(definition.Track("EmitterBoxX").Value(systemTime, Random01(system)), definition.Track("EmitterBoxY").Value(systemTime, Random01(system)));
            // Original SodRoll.xml moves the emitter's system centre; already spawned dirt stays behind.
            foreach (Field systemField in definition.SystemFields)
                if (systemField.Type == "SystemPosition") particle.Position += new Vector2(systemField.X.Value(systemTime, .5f), systemField.Y.Value(systemTime, .5f));
            particle.Position += new Vector2(SystemValue(system, emitter, "EmitterOffsetX"), SystemValue(system, emitter, "EmitterOffsetY"));
            float speed = definition.Track("LaunchSpeed").Value(0, Random01(system)) * .01f;
            particle.Velocity = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * speed;
            particle.ColorRed = Random01(system); particle.ColorGreen = Random01(system); particle.ColorBlue = Random01(system); particle.Brightness = Random01(system);
            particle.Angle = definition.Track("ParticleSpinAngle").Value(0, Random01(system)) + (definition.RandomSpin ? Random01(system) * 360f : 0);
            particle.PreviousPosition = particle.Position; particle.PreviousAngle = particle.Angle;
            particle.Frame = system.Random.Next(Math.Max(1, definition.ImageFrames));
            emitter.Particles.Add(particle); emitter.Launched++;
        }
        private static void Step(SystemInstance system)
        {
            foreach (Emitter emitter in system.Emitters)
            {
                EmitterDefinition definition = emitter.Definition;
                bool stopped = system.StopTick >= 0 && system.Tick >= system.StopTick;
                bool fading = stopped && system.FadeOut != null && system.Tick - system.StopTick < system.FadeDuration;
                if (stopped && !fading) { emitter.Particles.Clear(); continue; }
                for (int index = emitter.Particles.Count - 1; index >= 0; index--)
                {
                    Particle particle = emitter.Particles[index];
                    if (particle.Age >= particle.Duration)
                    {
                        if (definition.ParticleLoops) particle.Age = 0;
                        else if (fading) particle.Age = particle.Duration - 1;
                        else { emitter.Particles.RemoveAt(index); continue; }
                    }
                    particle.PreviousPosition = particle.Position; particle.PreviousAngle = particle.Angle;
                    float time = particle.Age / Math.Max(1f, particle.Duration - 1), last = Math.Max(0, particle.Age - 1) / Math.Max(1f, particle.Duration - 1);
                    for (int i = 0; i < definition.Fields.Count; i++)
                    {
                        Field field = definition.Fields[i];
                        float x = field.X.Value(time, particle.FieldRandom[i, 0]), y = field.Y.Value(time, particle.FieldRandom[i, 1]);
                        switch (field.Type)
                        {
                            case "Friction": particle.Velocity = new Vector2(particle.Velocity.x * (1 - x), particle.Velocity.y * (1 - y)); break;
                            case "Acceleration": particle.Velocity += new Vector2(x, y) * .01f; break;
                            case "Velocity": particle.Position += new Vector2(x, y) * .01f; break;
                            case "Position": particle.Position += new Vector2(x - field.X.Value(last, particle.FieldRandom[i, 0]), y - field.Y.Value(last, particle.FieldRandom[i, 1])); break;
                            case "Shake": particle.Position += new Vector2((Random01(system) * 2 - 1) * x, (Random01(system) * 2 - 1) * y); break;
                            case "Circle":
                                Vector2 tangent = new Vector2(-particle.Position.y, particle.Position.x).normalized;
                                particle.Position += tangent * ((x + particle.Position.magnitude * y) * .01f); break;
                            case "Away": particle.Position += particle.Position.normalized * ((x + particle.Position.magnitude * y) * .01f); break;
                            case "GroundConstraint":
                                if (particle.Position.y >= y)
                                {
                                    particle.Position = new Vector2(particle.Position.x, y);
                                    float reflect = definition.Track("CollisionReflect").Value(time, particle.Random[6]);
                                    particle.SpinVelocity = particle.Velocity.y * definition.Track("CollisionSpin").Value(time, particle.Random[7]) / 1000f * Mathf.Rad2Deg;
                                    particle.Velocity = new Vector2(particle.Velocity.x * reflect, -particle.Velocity.y * reflect);
                                }
                                break;
                        }
                    }
                    particle.Position += particle.Velocity;
                    particle.Angle += definition.Track("ParticleSpinSpeed").Value(time, particle.Random[3]) * .01f + particle.SpinVelocity;
                    particle.Age++;
                }
                if (!stopped && (emitter.Age < emitter.Duration || definition.Loops))
                {
                    emitter.Accumulated += SystemValue(system, emitter, "SpawnRate") * .01f;
                    int spawn = Math.Max((int)emitter.Accumulated, (int)SystemValue(system, emitter, "SpawnMinActive", -1) - emitter.Particles.Count);
                    emitter.Accumulated -= (int)emitter.Accumulated;
                    if (definition.Tracks.ContainsKey("SpawnMaxLaunched")) spawn = Math.Min(spawn, (int)SystemValue(system, emitter, "SpawnMaxLaunched") - emitter.Launched);
                    if (definition.Tracks.ContainsKey("SpawnMaxActive")) spawn = Math.Min(spawn, (int)SystemValue(system, emitter, "SpawnMaxActive") - emitter.Particles.Count);
                    for (int i = 0; i < spawn; i++) Spawn(system, emitter);
                }
                emitter.Age++;
                if (definition.Loops && emitter.Age >= emitter.Duration) emitter.Age = 0;
            }
        }
    }
}
