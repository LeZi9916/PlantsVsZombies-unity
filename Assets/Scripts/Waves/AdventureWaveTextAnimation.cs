#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace PlantsVsZombies.Waves
{
    /// <summary>Original TextFadeOn locator track. Inherited values and native rate/count sampling are preserved.</summary>
    public sealed class AdventureWaveTextAnimation
    {
        /// <summary>Stores frames.</summary>
        private readonly AdventureWaveTextPose[] _frames;
        /// <summary>Stores enter start,  enter count,  leave start,  leave count.</summary>
        private readonly int _enterStart, _enterCount, _leaveStart, _leaveCount;
        /// <summary>Initializes adventure wave text animation.</summary>
        /// <param name = "frames">The frames.</param>
        /// <param name = "enterStart">The enter start.</param>
        /// <param name = "enterCount">The enter count.</param>
        /// <param name = "leaveStart">The leave start.</param>
        /// <param name = "leaveCount">The leave count.</param>
        private AdventureWaveTextAnimation(AdventureWaveTextPose[] frames, int enterStart, int enterCount, int leaveStart, int leaveCount)
        {
            this._frames = frames;
            this._enterStart = enterStart;
            this._enterCount = enterCount;
            this._leaveStart = leaveStart;
            this._leaveCount = leaveCount;
        }

        /// <summary>Performs load.</summary>
        /// <param name = "path">The path.</param>
        /// <returns>The resulting load.</returns>
        /// <exception cref = "FormatException">The operation cannot satisfy its preconditions.</exception>
        public static AdventureWaveTextAnimation Load(string path)
        {
            var doc = new XmlDocument
            {
                XmlResolver = null
            };
            using (var input = new StringReader("<reanim>" + File.ReadAllText(path) + "</reanim>"))
            {
                doc.Load(input);
            }

            var poses = new List<AdventureWaveTextPose>();
            int enter = 0, enterLength = 0, leave = 0, leaveLength = 0;
            var root = doc.DocumentElement ?? throw new FormatException("The wave text animation has no root element: " + path);
            foreach (XmlNode track in root.SelectNodes("track") ?? throw new FormatException("The wave text animation has no track list: " + path))
            {
                var name = (track.SelectSingleNode("name") ?? throw new FormatException("A wave text track has no name: " + path)).InnerText;
                var index = 0;
                float f = 0;
                var pose = new AdventureWaveTextPose
                {
                    ScaleX = 1,
                    ScaleY = 1,
                    Alpha = 1
                };
                foreach (XmlNode frame in track.SelectNodes("t") ?? throw new FormatException("A wave text track has no frame list: " + path))
                {
                    foreach (XmlNode field in frame.ChildNodes)
                    {
                        float v;
                        if (!float.TryParse(field.InnerText, NumberStyles.Float, CultureInfo.InvariantCulture, out v))
                        {
                            continue;
                        }

                        switch (field.Name)
                        {
                            case "x":
                                pose.X = v;
                                break;
                            case "y":
                                pose.Y = v;
                                break;
                            case "sx":
                                pose.ScaleX = v;
                                break;
                            case "sy":
                                pose.ScaleY = v;
                                break;
                            case "a":
                                pose.Alpha = v;
                                break;
                            case "f":
                                f = v;
                                break;
                        }
                    }

                    if (name == "locator_text")
                    {
                        poses.Add(pose);
                    }
                    else if (name == "anim_enter" && f >= 0)
                    {
                        if (enterLength == 0)
                        {
                            enter = index;
                        }

                        enterLength++;
                    }
                    else if (name == "anim_leave" && f >= 0)
                    {
                        if (leaveLength == 0)
                        {
                            leave = index;
                        }

                        leaveLength++;
                    }

                    index++;
                }
            }

            if (poses.Count == 0 || enterLength == 0 || leaveLength == 0)
            {
                throw new FormatException("Missing original TextFadeOn tracks.");
            }

            return new AdventureWaveTextAnimation(poses.ToArray(), enter, enterLength, leave, leaveLength);
        }

        /// <summary>Performs sample.</summary>
        /// <param name = "ageTicks">The age ticks.</param>
        /// <param name = "characterIndex">The character index.</param>
        /// <param name = "textLength">The text length.</param>
        /// <returns>The resulting sample.</returns>
        public AdventureWaveTextPose Sample(int ageTicks, int characterIndex, int textLength)
        {
            var remaining = AdventureWaveNoticeRules.HugeDuration - ageTicks;
            var slide = 100 + textLength;
            var leaving = remaining <= slide;
            int start = leaving ? _leaveStart : _enterStart, count = leaving ? _leaveCount : _enterCount;
            float phase;
            if (!leaving)
            {
                phase = Math.Min(1f, Math.Max(0, ageTicks) * .6f / count);
            }
            else
            {
                // 0x467670: each glyph rate clamps((elapsed*100-index)/50)*40, advanced at100Hz.
                double sum = 0;
                for (var tick = 0; tick <= slide - remaining; tick++)
                {
                    sum += Math.Max(0d, Math.Min(1d, (tick * 100d - characterIndex) / 50d)) * .4d / count;
                }

                phase = (float)Math.Min(1d, sum);
            }

            var sample = start + (count - 1) * phase;
            int a = Math.Max(0, Math.Min(_frames.Length - 1, (int)sample)), b = Math.Min(_frames.Length - 1, a + 1);
            var t = sample - a;
            var p = _frames[a];
            var q = _frames[b];
            return new AdventureWaveTextPose
            {
                X = p.X + (q.X - p.X) * t,
                Y = p.Y + (q.Y - p.Y) * t,
                ScaleX = p.ScaleX + (q.ScaleX - p.ScaleX) * t,
                ScaleY = p.ScaleY + (q.ScaleY - p.ScaleY) * t,
                Alpha = p.Alpha + (q.Alpha - p.Alpha) * t,
                Phase = phase
            };
        }
    }
}
