using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace PvZReplica.Waves
{
    /// <summary>PC 0x41FCD0, not a generic last-array-row announcement for continuous modes.</summary>
    public static class AdventureWaveNoticeRules
    {
        public const int HugeDuration = 750;
        public const int FinalSoundDelay = 60;
        public const int Baseline = 330; // local Chinese GOTY 0x467BB0 (reference mobile port uses290).
        public const string Font = "HouseofTerror28"; // 0x753240 <- FONT_HOUSEOFTERROR28 at0x48B4F8.
        public static bool AnnounceFinal(int nativeMode, bool finalSurvivalStage, bool adventureBoss)
        {
            if (nativeMode >= 1 && nativeMode <= 15 && !finalSurvivalStage) return false;
            if (nativeMode == 31 || adventureBoss) return false;
            return nativeMode != 36 && nativeMode != 40 && nativeMode != 22 && nativeMode != 18 &&
                nativeMode != 35 && nativeMode != 20 && nativeMode != 71 && nativeMode != 72 && nativeMode != 24;
        }
    }
    public struct AdventureWaveTextPose
    {
        public float X, Y, ScaleX, ScaleY, Alpha, Phase;
    }
    /// <summary>Original TextFadeOn locator track. Inherited values and native rate/count sampling are preserved.</summary>
    public sealed class AdventureWaveTextAnimation
    {
        private readonly AdventureWaveTextPose[] frames;
        private readonly int enterStart, enterCount, leaveStart, leaveCount;
        private AdventureWaveTextAnimation(AdventureWaveTextPose[] frames, int enterStart, int enterCount, int leaveStart, int leaveCount)
        { this.frames = frames; this.enterStart = enterStart; this.enterCount = enterCount; this.leaveStart = leaveStart; this.leaveCount = leaveCount; }
        public static AdventureWaveTextAnimation Load(string path)
        {
            var doc = new XmlDocument { XmlResolver = null };
            using (var input = new StringReader("<reanim>" + File.ReadAllText(path) + "</reanim>")) doc.Load(input);
            var poses = new List<AdventureWaveTextPose>();
            int enter = 0, enterLength = 0, leave = 0, leaveLength = 0;
            foreach (XmlNode track in doc.DocumentElement.SelectNodes("track"))
            {
                string name = track.SelectSingleNode("name").InnerText;
                int index = 0; float f = 0;
                var pose = new AdventureWaveTextPose { ScaleX = 1, ScaleY = 1, Alpha = 1 };
                foreach (XmlNode frame in track.SelectNodes("t"))
                {
                    foreach (XmlNode field in frame.ChildNodes)
                    {
                        float v; if (!float.TryParse(field.InnerText, NumberStyles.Float, CultureInfo.InvariantCulture, out v)) continue;
                        switch (field.Name) { case "x": pose.X = v; break; case "y": pose.Y = v; break;
                            case "sx": pose.ScaleX = v; break; case "sy": pose.ScaleY = v; break;
                            case "a": pose.Alpha = v; break; case "f": f = v; break; }
                    }
                    if (name == "locator_text") poses.Add(pose);
                    else if (name == "anim_enter" && f >= 0) { if (enterLength == 0) enter = index; enterLength++; }
                    else if (name == "anim_leave" && f >= 0) { if (leaveLength == 0) leave = index; leaveLength++; }
                    index++;
                }
            }
            if (poses.Count == 0 || enterLength == 0 || leaveLength == 0) throw new FormatException("Missing original TextFadeOn tracks.");
            return new AdventureWaveTextAnimation(poses.ToArray(), enter, enterLength, leave, leaveLength);
        }
        public AdventureWaveTextPose Sample(int ageTicks, int characterIndex, int textLength)
        {
            int remaining = AdventureWaveNoticeRules.HugeDuration - ageTicks;
            int slide = 100 + textLength;
            bool leaving = remaining <= slide;
            int start = leaving ? leaveStart : enterStart, count = leaving ? leaveCount : enterCount;
            float phase;
            if (!leaving) phase = Math.Min(1f, Math.Max(0, ageTicks) * .6f / count);
            else
            {
                // 0x467670: each glyph rate clamps((elapsed*100-index)/50)*40, advanced at100Hz.
                double sum = 0;
                for (int tick = 0; tick <= slide - remaining; tick++)
                    sum += Math.Max(0d, Math.Min(1d, (tick * 100d - characterIndex) / 50d)) * .4d / count;
                phase = (float)Math.Min(1d, sum);
            }
            float sample = start + (count - 1) * phase;
            int a = Math.Max(0, Math.Min(frames.Length - 1, (int)sample)), b = Math.Min(frames.Length - 1, a + 1);
            float t = sample - a;
            var p = frames[a]; var q = frames[b];
            return new AdventureWaveTextPose { X = p.X + (q.X - p.X) * t, Y = p.Y + (q.Y - p.Y) * t,
                ScaleX = p.ScaleX + (q.ScaleX - p.ScaleX) * t, ScaleY = p.ScaleY + (q.ScaleY - p.ScaleY) * t,
                Alpha = p.Alpha + (q.Alpha - p.Alpha) * t, Phase = phase };
        }
    }
}
