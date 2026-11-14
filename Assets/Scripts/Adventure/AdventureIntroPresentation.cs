#nullable enable
using System;
using System.Collections.Generic;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Defines adventure intro presentation.</summary>
    public static class AdventureIntroPresentation
    {
        /// <summary>Stores native update.</summary>
        public const int NativeUpdate = 10;
        /// <summary>Stores evidence.</summary>
        public const string Evidence = "PC 0x446520/0x4473C0/0x447E30; 0x444D20..0x4462C9; 0x4217DB..0x421A0F; local original reanim";
        /// <summary>Performs create plan.</summary>
        /// <param name = "context">The context.</param>
        /// <returns>The resulting create plan.</returns>
        /// <exception cref = "ArgumentNullException">The operation cannot satisfy its preconditions.</exception>
        public static AdventureIntroPlan CreatePlan(AdventureIntroContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var mode = context.NativeMode;
            var restrictedBank = mode == 19 || mode == 71 || mode == 72 || context.IsWhack || context.IsBowling || context.IsVase || mode == 43 || mode == 50;
            var nonScrolling = mode == 42 || mode == 71 || mode == 43 || mode == 50 || mode == 23 || context.IsVase || context.IsIZombie || context.IsWhack || context.IsShovel || context.IsBowling || context.HasBoard && mode == 46;
            var choose = !restrictedBank && !context.IsConveyor && mode != 42 && mode != 20 && mode != 24 && mode != 23 && !context.IsIZombie && !context.IsBowling && !(context.HasBoard && mode == 18) && !(mode == 0 && context.FirstAdventure && context.Level <= 7);
            var p = new AdventureIntroPlan
            {
                Context = context,
                ChooseSeeds = context.ChooseSeedsOverride ?? choose,
                NonScrolling = context.NonScrollingOverride ?? nonScrolling,
                DrawSeedBank = !restrictedBank
            };
            p.ScrolledLeftAtStart = !context.SurvivalRepick && !(context.HasBoard && (mode == 46 || mode == 49)) && !context.IsShovel && !p.NonScrolling;
            p.SodTime = mode == 0 && context.FirstAdventure && (context.Level == 1 || context.Level == 2 || context.Level == 4) ? 2000 : 0;
            p.GraveStoneTime = context.HasGraveStones && !context.IsWhack && !context.SurvivalRepick ? 1000 : 0;
            p.ReadyTime = mode == 0 && context.FirstAdventure && context.Level <= 2 || context.HasBoard && (mode == 46 || mode == 49) || context.IsShovel || mode == 23 || mode == 31 || mode == 50 || context.IsIZombie || context.IsWhack || context.IsVase ? 0 : 1830;
            p.LawnMowerTime = context.SurvivalRepick ? 0 : 550;
            p.DaveTime = context.CrazyDaveTimeOverride >= 0 ? context.CrazyDaveTimeOverride : context.HasCrazyDaveDialog ? 2500 + (context.IsFinalBoss && mode == 0 ? 4000 : 0) : 0;
            p.FogTime = context.NativeBackground == 3 && !context.IsStormyNight && mode != 21 ? 1950 - p.SodTime - p.LawnMowerTime : 0;
            p.BossTime = context.IsFinalBoss ? 4000 : 0;
            return p;
        }

        // Literal PC curve IDs 1(linear),3(invquad),4(S(S(t))) and native signed nearest rounding.
        /// <summary>Performs curve.</summary>
        /// <param name = "start">The start.</param>
        /// <param name = "end">The end.</param>
        /// <param name = "time">The time.</param>
        /// <param name = "from">The from.</param>
        /// <param name = "to">The to.</param>
        /// <param name = "curve">The curve.</param>
        /// <returns>The resulting curve.</returns>
        /// <exception cref = "ArgumentOutOfRangeException">The operation cannot satisfy its preconditions.</exception>
        public static int Curve(int start, int end, int time, int from, int to, int curve)
        {
            if (time <= start)
            {
                return from;
            }

            if (time >= end)
            {
                return to;
            }

            var t = (float)((double)(time - start) / (end - start));
            if (curve == 3)
            {
                t = t + t - t * t;
            }
            else if (curve == 4)
            {
                t = Smooth(t);
                t = Smooth(t);
            }
            else if (curve != 1)
            {
                throw new ArgumentOutOfRangeException(nameof(curve));
            }

            var value = (to - (float)from) * t + from;
            return (int)(value > 0 ? value + .5f : value - .5f);
        }

        /// <summary>Performs smooth.</summary>
        /// <param name = "t">The t.</param>
        /// <returns>The resulting smooth.</returns>
        private static float Smooth(float t)
        {
            return 3f * t * t - 2f * t * t * t;
        }

        /// <summary>Performs sample.</summary>
        /// <param name = "p">The p.</param>
        /// <param name = "time">The time.</param>
        /// <param name = "choosingSeeds">The choosing seeds.</param>
        /// <returns>The resulting sample.</returns>
        /// <exception cref = "ArgumentNullException">The operation cannot satisfy its preconditions.</exception>
        public static AdventureIntroFrame Sample(AdventureIntroPlan p, int time, bool choosingSeeds = false)
        {
            if (p == null)
            {
                throw new ArgumentNullException(nameof(p));
            }

            time = Math.Max(0, time);
            var f = new AdventureIntroFrame
            {
                NativeTime = time,
                ChooserY = 516,
                SeedBankX = p.RequiredContext.SeedBankStartX,
                SeedBankY = -p.RequiredContext.SeedBankImageHeight,
                SeedBankDarken = 255,
                SodPosition = p.SodTime > 0 ? Curve(p.SodStart, p.SodEnd, time, 0, 1000, 1) : 1000
            };
            int initialX = p.ScrolledLeftAtStart ? 220 : 0, streetX = p.RequiredContext.LogicalWidth - 1180;
            f.BoardX = time <= p.PanRightStart ? initialX : time <= p.PanRightEnd ? -Curve(p.PanRightStart, p.PanRightEnd, time, -initialX, -streetX, 4) : streetX;
            if (time > p.ChooserOffStart)
            {
                f.BoardX = -Curve(p.ChooserOffStart, p.PanLeftEnd, time, -streetX, 0, 4);
            }

            if (p.ChooseSeeds)
            {
                if (time > p.ChooserOnStart)
                {
                    f.ChooserY = Curve(p.ChooserOnStart, p.ChooserOnEnd, time, 516, 0, 4);
                }

                if (time > p.ChooserOffStart)
                {
                    f.ChooserY = Curve(p.ChooserOffStart, p.ChooserOffEnd, time, 0, 516, 4);
                }
            }

            if (p.DrawSeedBank && time > p.BankOnStart)
            {
                f.SeedBankY = Curve(p.BankOnStart, p.BankOnEnd, time, -p.RequiredContext.SeedBankImageHeight, 0, 4);
            }

            if (time > p.ChooserOffEnd)
            {
                f.SeedBankX = Curve(p.ChooserOffEnd, p.PanLeftEnd, time, p.RequiredContext.SeedBankStartX, 10, 4);
                f.SeedBankDarken = Curve(p.ChooserOffEnd, p.PanLeftEnd, time, 255, 128, 3);
            }

            for (var row = 0; row < 6; row++)
            {
                var start = 6000 + 300 - row * 50 + p.DaveTime + p.SodTime + p.GraveStoneTime;
                if (p.LawnMowerTime > 0 && time > start)
                {
                    f.MowerX[row] = Curve(start, start + 250, time, -80, -21, 4);
                }
            }

            AddSodBlits(p.RequiredContext, f);
            if (p.SodTime > 0 && time >= p.SodStart && time < p.SodEnd)
            {
                var age = (time - p.SodStart) * .001f;
                if (p.RequiredContext.Level == 1)
                {
                    AddRoll(f, 0, 0, 35, 348, age);
                }
                else if (p.RequiredContext.Level == 2)
                {
                    AddRoll(f, 0, -102, 35, 246, age);
                    AddRoll(f, 0, 111, 35, 459, age);
                }
                else if (p.RequiredContext.Level == 4)
                {
                    AddRoll(f, -3, -198, 32, 150, age);
                    AddRoll(f, -3, 203, 32, 551, age);
                }
            }

            var ready = p.ReadyTime > 0 && time >= p.ReadyStart && time < p.End;
            if (ready)
            {
                f.Animations.Add(new AdventureIntroAnimation("StartReadySetPlant", 400, 324, (time - p.ReadyStart) * .001f, 12, 22, 900000));
            }

            f.Stage = time >= p.End ? AdventureIntroStage.Complete : ready ? AdventureIntroStage.ReadySetPlant : choosingSeeds ? AdventureIntroStage.ChoosingSeeds : time > p.ChooserOffStart && time < p.PanLeftEnd ? AdventureIntroStage.ReturnToLawn : time >= p.PanLeftEnd ? AdventureIntroStage.LawnSetup : time > p.PanRightEnd ? AdventureIntroStage.Street : time > p.PanRightStart ? AdventureIntroStage.PanToStreet : AdventureIntroStage.Home;
            f.WorldInputEnabled = time >= p.End;
            f.MenuVisible = time >= p.End;
            f.ChooserVisible = p.ChooseSeeds && time > p.ChooserOnStart && time < p.ChooserOffEnd;
            f.PreviewVisible = time < p.End;
            return f;
        }

        /// <summary>Performs add roll.</summary>
        /// <param name = "f">The f.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <param name = "px">The px.</param>
        /// <param name = "py">The py.</param>
        /// <param name = "age">The age.</param>
        private static void AddRoll(AdventureIntroFrame f, int x, int y, int px, int py, float age)
        {
            f.Animations.Add(new AdventureIntroAnimation("SodRoll", x, y, age, 24, 48, 400000));
            f.Particles.Add(new AdventureIntroParticle("SodRoll", px, py, age, 400001));
        }

        /// <summary>Performs add sod blits.</summary>
        /// <param name = "c">The c.</param>
        /// <param name = "f">The f.</param>
        private static void AddSodBlits(AdventureIntroContext c, AdventureIntroFrame f)
        {
            if (c.NativeMode != 0 || !c.FirstAdventure || c.Level > 4)
            {
                return;
            }

            if (c.Level == 1)
            {
                f.SodBlits.Add(new AdventureIntroBlit("sod1row", 0, 0, Curve(0, 1000, f.SodPosition, 0, 771, 1), 127, 19, 265));
            }
            else if (c.Level <= 3)
            {
                f.SodBlits.Add(new AdventureIntroBlit("sod1row", 0, 0, 771, 127, 19, 265));
                f.SodBlits.Add(new AdventureIntroBlit("sod3row", 0, 0, Curve(0, 1000, f.SodPosition, 0, 771, 1), 355, 15, 149));
            }
            else
            {
                f.SodBlits.Add(new AdventureIntroBlit("sod3row", 0, 0, 771, 355, 15, 149));
                // PC level4 reveals the full day background's grass strip, not extra hand-drawn tiles.
                f.SodBlits.Add(new AdventureIntroBlit("background1", 232, 0, Curve(0, 1000, f.SodPosition, 0, 773, 1), 600, 12, 0));
            }
        }

        /// <summary>Performs events between.</summary>
        /// <param name = "p">The p.</param>
        /// <param name = "previous">The previous.</param>
        /// <param name = "current">The current.</param>
        /// <param name = "emit">The emit.</param>
        public static void EventsBetween(AdventureIntroPlan p, int previous, int current, Action<AdventureIntroEvent>? emit)
        {
            if (emit == null || current <= previous)
            {
                return;
            }

            if (p.ChooseSeeds && Crossed(previous, current, p.ChooserOnEnd))
            {
                emit(AdventureIntroEvent.SeedChooserOpened);
            }

            if (Crossed(previous, current, p.ChooserOffStart))
            {
                emit(AdventureIntroEvent.PlaceLawnItems);
            }

            if (p.SodTime > 0 && Crossed(previous, current, p.SodStart))
            {
                emit(AdventureIntroEvent.SodStarted);
            }

            if (p.SodTime > 0 && Crossed(previous, current, p.SodEnd))
            {
                emit(AdventureIntroEvent.SodFinished);
            }

            if (p.GraveStoneTime > 0 && Crossed(previous, current, p.GraveStoneStart))
            {
                emit(AdventureIntroEvent.GraveStonesEnabled);
            }

            if (p.RequiredContext.IsStormyNight)
            {
                if (Crossed(previous, current, p.PanRightEnd - 1000))
                {
                    emit(AdventureIntroEvent.StormFlashStarted);
                }

                if (Crossed(previous, current, p.PanLeftEnd))
                {
                    emit(AdventureIntroEvent.StormFlashStarted);
                }
            }

            if (p.ReadyTime > 0 && Crossed(previous, current, p.ReadyStart))
            {
                emit(AdventureIntroEvent.ReadySetPlantStarted);
            }

            if (Crossed(previous, current, p.End))
            {
                emit(AdventureIntroEvent.IntroFinished);
            }
        }

        /// <summary>Performs crossed.</summary>
        /// <param name = "previous">The previous.</param>
        /// <param name = "current">The current.</param>
        /// <param name = "point">The point.</param>
        /// <returns>The resulting crossed.</returns>
        private static bool Crossed(int previous, int current, int point)
        {
            return previous < point && current >= point;
        }

        /// <summary>Performs place street zombie.</summary>
        /// <param name = "c">The c.</param>
        /// <param name = "kind">The kind.</param>
        /// <param name = "gx">The gx.</param>
        /// <param name = "gy">The gy.</param>
        /// <param name = "next">The next.</param>
        /// <param name = "nextFloat">The next float.</param>
        /// <param name = "trackExists">The track exists.</param>
        /// <returns>The resulting place street zombie.</returns>
        /// <exception cref = "ArgumentNullException">The operation cannot satisfy its preconditions.</exception>
        public static AdventureIntroPreviewZombie PlaceStreetZombie(AdventureIntroContext c, int kind, int gx, int gy, Func<int, int> next, Func<float, float> nextFloat, Func<int, string, bool> trackExists)
        {
            if (next == null || nextFloat == null || trackExists == null)
            {
                throw new ArgumentNullException("Original random/track queries are required.");
            }

            var z = new AdventureIntroPreviewZombie
            {
                NativeKind = kind,
                GridX = gx,
                GridY = gy,
                WorldX = 830 + 56 * gx,
                WorldY = 70 + 90 * gy + (gx % 2 == 1 ? 30 : 0),
                RenderOrder = 300000 + 2 * (2 * gy + gx % 2),
                Scale = c.IsLittleTrouble ? .5f : 1f
            };
            if (kind == 10 && c.NativeMode == 32)
            {
                z.NativeKind = kind = 26;
                z.ShowDuckyTube = true;
            }

            // PC LoadReanim0x53FF50: preview/UI are -2/-3, select idle2 with R(4)>0.
            if (next(4) > 0 && trackExists(kind, "anim_idle2"))
            {
                z.Animation = "anim_idle2";
                z.AnimationRate = 12f + nextFloat(12f);
            }
            else if (trackExists(kind, "anim_idle"))
            {
                z.Animation = "anim_idle";
                z.AnimationRate = 12f + nextFloat(6f);
            }

            z.InitialPhase = nextFloat(.99f);
            if (kind == 8 || kind == 9)
            {
                // PC ZombieInitialize 0x53DD90 overrides the unavailable idle clip for both dancers.
                z.Scale = c.IsLittleTrouble ? .5f : .8f;
                z.Animation = "anim_armraise";
                z.AnimationRate = 12f;
                z.InitialPhase = 0f;
            }
            else if (kind == 12)
            {
                z.Animation = "anim_drive";
                z.AnimationRate = 12;
                z.InitialPhase = 0;
            }
            else if (kind == 22)
            {
                z.Animation = "anim_idle";
                z.AnimationRate = 8;
                z.InitialPhase = 0;
            }
            else if (kind == 13)
            {
                z.Animation = "anim_jump";
                z.AnimationRate = 20;
                z.InitialPhase = 1;
                z.Loop = false;
                z.Altitude = 18;
            }
            else if (kind == 18)
            {
                z.Animation = "anim_pogo";
                z.AnimationRate = 40;
                z.InitialPhase = 1;
                z.Loop = false;
                // PC 53DD90 initializes the same eighty-update counter for UI and on-board Pogo zombies.
                z.PogoState = new AdventurePogoState { PhaseTicks = next(AdventurePogoRules.BounceTicks) + 1 };
            }

            if (c.HasRoof)
            {
                z.WorldY -= 2 * gy + 30 - 7 * gx;
                z.WorldX -= 5;
            }

            if (kind == 12)
            {
                z.WorldY -= 10;
                z.WorldX -= 30;
            }
            else if (c.IsLittleTrouble)
            {
                z.WorldY += next(50) - 25;
                z.WorldX += next(50) - 25;
            }
            else if (kind == 23 || kind == 32)
            {
                z.WorldX += next(15) - 20;
            }
            else if (gy == 4 && c.CanShowAlmanacOrStore)
            {
                z.WorldX += next(15);
            }
            else
            {
                z.WorldY += next(15);
                z.WorldX += next(15);
            }

            if (kind == 20)
            {
                z.Animation = "anim_drop";
                z.AnimationRate = 24f;
                z.InitialPhase = 0f;
                z.BungeeTicks = next(201);
                z.RenderOrder = 200000;
                z.WorldX = 950 + gx * 50;
                z.WorldY = 50;
            }
            else if (kind == 13)
            {
                z.RenderOrder = 301000;
                z.WorldX = 1105;
                z.WorldY = 480;
            }

            return z;
        }

        /// <summary>Performs can use preview cell.</summary>
        /// <param name = "c">The c.</param>
        /// <param name = "kind">The kind.</param>
        /// <param name = "x">The x.</param>
        /// <param name = "y">The y.</param>
        /// <param name = "occupied">The occupied.</param>
        /// <returns>The resulting can use preview cell.</returns>
        public static bool CanUsePreviewCell(AdventureIntroContext c, int kind, int x, int y, bool[, ] occupied)
        {
            if (occupied[x, y])
            {
                return false;
            }

            if ((kind == 23 || kind == 32) && (x == 0 || y == 0 || occupied[x - 1, y] || occupied[x, y - 1] || occupied[x - 1, y - 1]))
            {
                return false;
            }

            if (x == 4)
            {
                if (y == 0)
                {
                    return false;
                }
            }
            else if (kind == 12 || x == 0 && c.HasPool)
            {
                return false;
            }

            if (c.HasRoof && x == 0 && y == 0 || x == 4 && c.NativeBackground == 3 && kind == 16)
            {
                return false;
            }

            return kind != 23 && kind != 32 && kind != 12 && kind != 13 && kind != 3 || x != 0 && (x != 1 || !c.HasPool && y != 0);
        }

        /// <summary>Performs create street preview.</summary>
        /// <param name = "c">The c.</param>
        /// <param name = "waves">The waves.</param>
        /// <param name = "next">The next.</param>
        /// <param name = "nextFloat">The next float.</param>
        /// <param name = "trackExists">The track exists.</param>
        /// <param name = "allowed">The allowed.</param>
        /// <returns>The resulting create street preview.</returns>
        public static List<AdventureIntroPreviewZombie> CreateStreetPreview(AdventureIntroContext c, IEnumerable<int[]> waves, Func<int, int> next, Func<float, float> nextFloat, Func<int, string, bool> trackExists, bool[]? allowed = null)
        {
            var result = new List<AdventureIntroPreviewZombie>();
            if (c.IsFinalBoss)
            {
                return result;
            }

            var counts = new int[34];
            var total = 0;
            foreach (int[] wave in waves)
            {
                foreach (int kind in wave)
                {
                    if (kind < 0)
                    {
                        break;
                    }

                    if (kind >= 34 || kind == 1 || kind == 33 || kind == 19 && !c.IsStormyNight || kind == 13 && c.NativeMode != 28)
                    {
                        continue;
                    }

                    counts[kind]++;
                    total++;
                    if (kind == 20 || kind == 13)
                    {
                        counts[kind] = 1;
                    }
                }
            }

            if (c.NativeMode == 31 && allowed != null)
            {
                for (var k = 0; k < Math.Min(34, allowed.Length); k++)
                {
                    if (k != 19 && allowed[k])
                    {
                        counts[k] = Math.Max(1, counts[k]);
                    }
                }
            }

            if (c.HasPool)
            {
                counts[10] = 1;
            }

            if (total == 0)
            {
                return result; // Host has not supplied the original populated wave list.
            }

            var budget = c.IsLittleTrouble ? 15 : c.NativeMode == 0 && (c.IsStormyNight || c.Level == 10 || c.Level == 20 || c.Level == 30) ? 18 : 10;
            var occupied = new bool[5, 5];
            for (var k = 0; k < 34; k++)
            {
                if (counts[k] > 0 && (k == 23 || k == 32 || k == 12))
                {
                    AddPreview(c, k, occupied, result, next, nextFloat, trackExists);
                }
            }

            for (var k = 0; k < 34; k++)
            {
                if (counts[k] > 0 && k != 23 && k != 32 && k != 12)
                {
                    var number = Math.Max(1, Math.Min(counts[k], budget * counts[k] / total));
                    for (var i = 0; i < number; i++)
                    {
                        AddPreview(c, k, occupied, result, next, nextFloat, trackExists);
                    }
                }
            }

            return result;
        }

        /// <summary>Performs add preview.</summary>
        /// <param name = "c">The c.</param>
        /// <param name = "kind">The kind.</param>
        /// <param name = "occupied">The occupied.</param>
        /// <param name = "result">The result.</param>
        /// <param name = "next">The next.</param>
        /// <param name = "nextFloat">The next float.</param>
        /// <param name = "trackExists">The track exists.</param>
        private static void AddPreview(AdventureIntroContext c, int kind, bool[, ] occupied, List<AdventureIntroPreviewZombie> result, Func<int, int> next, Func<float, float> nextFloat, Func<int, string, bool> trackExists)
        {
            int x = 0, y = 0;
            if (kind != 20)
            {
                var spots = new List<int>();
                for (var gx = 0; gx < 5; gx++)
                {
                    for (var gy = 0; gy < 5; gy++)
                    {
                        if (CanUsePreviewCell(c, kind, gx, gy, occupied))
                        {
                            spots.Add(gx * 5 + gy);
                        }
                    }
                }

                var spot = spots.Count == 0 ? 12 : spots[next(spots.Count)];
                x = spot / 5;
                y = spot % 5;
                occupied[x, y] = true;
                if (kind == 23 || kind == 32)
                {
                    occupied[x - 1, y] = true;
                    occupied[x, y - 1] = true;
                    occupied[x - 1, y - 1] = true;
                }
            }

            result.Add(PlaceStreetZombie(c, kind, x, y, next, nextFloat, trackExists));
            if (kind == 20 && (c.NativeMode == 48 || c.NativeMode == 0 && c.Level == 45))
            {
                result.Add(PlaceStreetZombie(c, kind, 1, y, next, nextFloat, trackExists));
                result.Add(PlaceStreetZombie(c, kind, 2, y, next, nextFloat, trackExists));
            }
        }
    }
}
