#nullable enable
using System;
using System.Collections.Generic;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Progression;

namespace CrazyDaveSpeechTests
{
    /// <summary>Checks the original message-to-voice rules without Unity.</summary>
    internal static class Program
    {
        /// <summary>Runs the speech selection regression tests.</summary>
        private static void Main()
        {
            Check(new string('a', 22), "anim_smalltalk", "crazydaveshort3", 3);
            Check(new string('a', 23), "anim_mediumtalk", "crazydavelong3", 3);
            Check(new string('a', 51), "anim_mediumtalk", "crazydavelong3", 3);
            Check(new string('a', 52), "anim_blahblah", "crazydaveextralong3", 3);
            Check("{ignored}" + new string('a', 22), "anim_smalltalk", "crazydaveshort3", 3);
            Check("{HANDING}{SHORT_SOUND}{SCREAM}", "anim_talk_handing", "crazydaveshort3", 3);
            Check("{HANDING}{SCREAM}", "anim_talk_handing", "crazydavescream", 1);
            Check("{HANDING}", "anim_talk_handing", "crazydavelong3", 3);
            Check("{SHAKE}{SCREAM}", "anim_crazy", "crazydavecrazy", 1);
            Check("{SCREAM}", "anim_smalltalk", "crazydavescream", 1);
            Check("{SCREAM2}", "anim_mediumtalk", "crazydavescream2", 1);
            Check("{SHOW_WALLNUT}", "anim_talk_handing", "crazydavescream2", 1);
            Check("{SHOW_HAMMER}", "anim_talk_handing", "crazydavelong3", 3);
            Check("{SHOW_FERTILIZER}", "anim_talk_handing", "crazydavelong3", 3);
            Check("{SHOW_TREE_FOOD}", "anim_talk_handing", "crazydavelong3", 3);
            Check("{SHOW_MONEYBAG}", "anim_talk_handing", "crazydavelong3", 3);

            var silent = CrazyDaveSpeech.Select("{NO_SOUND}Hello", false);
            Assert(silent.AnimationName == "anim_smalltalk" && silent.SoundVariantCount == 0 && !silent.StopPreviousSound, "Silent message selection");
            var continued = CrazyDaveSpeech.Select("{NO_SOUND}{SHAKE}", true);
            Assert(continued.AnimationName == null && continued.SoundVariantCount == 0 && !continued.StopPreviousSound, "Silent message preserves animation");
            var item = CrazyDaveSpeech.Select("{SHOW_WALLNUT}{SHOW_HAMMER}", false);
            Assert(item.Handing && item.LoopAnimation && item.HeldItem == "WALLNUT", "Held item precedence");
            var expectedStoreMessages = new[] { 2000, 2001, 2002, 2003, 2004, 2005, 2006, 2007, 2008, 2009, 2010, 2010, 2010, 2019, 2020, 2022, 2021, 2023, 2032, 2024, 2025, 2011, 2026, 2027, 2028, 2029, -1, 2030, 2031, 2033 };
            for (var itemId = 0; itemId < expectedStoreMessages.Length; itemId++)
            {
                Assert(CrazyDaveStoreDialog.GetHoverMessageIndex(itemId, 0) == expectedStoreMessages[itemId], "Store item " + itemId);
            }

            Assert(CrazyDaveStoreDialog.GetHoverMessageIndex(21, 3) == 2014, "Packet upgrade progression");
            Assert(!CrazyDaveStoreDialog.CanHover(2, 34, false) && CrazyDaveStoreDialog.CanHover(2, 35, false), "Gloom-shroom availability");
            Assert(!CrazyDaveStoreDialog.CanHover(23, 41, false) && CrazyDaveStoreDialog.CanHover(23, 42, false), "Roof cleaner availability");
            Assert(!CrazyDaveStoreDialog.CanHover(29, 50, false) && CrazyDaveStoreDialog.CanHover(29, 50, true), "First aid availability");
            TestStoreTiming();
            Console.WriteLine("Crazy Dave speech selection tests passed.");
        }

        /// <summary>Verifies hover refresh, idle selection, and voice-dependent expiration.</summary>
        private static void TestStoreTiming()
        {
            var clock = new CrazyDaveStoreSpeechClock();
            var messages = new List<int>();
            var stops = 0;
            Func<int, int> random = limit => 0;
            Action<int> speak = messages.Add;
            Action stop = () => stops++;
            for (var tick = 0; tick < 119; tick++)
            {
                clock.Advance(true, false, false, false, false, false, false, false, 2000, false, random, speak, stop);
            }

            Assert(messages.Count == 0, "Store hover disabled before tick 120");
            clock.Advance(true, false, false, false, false, false, false, false, 2000, false, random, speak, stop);
            Assert(messages.Count == 1 && messages[0] == 2000 && clock.BubbleTicks == 100, "Store hover starts speech");
            clock.Advance(true, false, false, false, false, false, false, false, 2000, false, random, speak, stop);
            Assert(messages.Count == 1 && clock.BubbleTicks == 100, "Repeated hover refreshes without replay");
            for (var tick = 0; tick < 100; tick++)
            {
                clock.Advance(true, false, false, false, false, false, false, true, null, false, random, speak, stop);
            }

            Assert(clock.BubbleTicks == 1 && stops == 0, "Bubble waits for voice");
            clock.Advance(true, false, false, false, false, false, false, false, null, false, random, speak, stop);
            Assert(clock.MessageIndex == -1 && stops == 1, "Bubble closes after voice");
            var ambient = new CrazyDaveStoreSpeechClock();
            messages.Clear();
            for (var tick = 0; tick < 200; tick++)
            {
                ambient.Advance(true, false, false, false, false, false, false, false, null, false, random, speak, stop);
            }

            Assert(messages.Count == 1 && messages[0] == 2015 && ambient.BubbleTicks == 800 && ambient.AmbientTicks == 500, "First ambient speech");
            for (var tick = 0; tick < 1300; tick++)
            {
                ambient.Advance(true, false, false, false, false, false, false, false, null, false, random, speak, stop);
            }

            Assert(messages.Count == 2 && messages[1] == 2016, "Ambient speech excludes previous message");
            var paused = new CrazyDaveStoreSpeechClock();
            paused.Advance(true, false, true, false, false, false, false, false, 2000, false, random, speak, stop);
            Assert(paused.StoreTicks == 0 && paused.AmbientTicks == 200, "Modal pauses store clock");
        }

        /// <summary>Verifies an animation and its final sound variant.</summary>
        /// <param name="message">The message including control markers.</param>
        /// <param name="animation">The expected animation track.</param>
        /// <param name="sound">The expected final sound variant.</param>
        /// <param name="variants">The expected number of variants.</param>
        private static void Check(string message, string animation, string sound, int variants)
        {
            var result = CrazyDaveSpeech.Select(message, false);
            Assert(result.AnimationName == animation && result.SoundVariantCount == variants && result.GetSoundName(variants - 1) == sound && result.StopPreviousSound && result.AnimationFramesPerSecond == 12f, message);
        }

        /// <summary>Fails when a test condition is false.</summary>
        /// <param name="condition">The condition to check.</param>
        /// <param name="name">The test case name.</param>
        /// <exception cref="InvalidOperationException">The condition is false.</exception>
        private static void Assert(bool condition, string name)
        {
            if (!condition)
            {
                throw new InvalidOperationException(name);
            }
        }
    }
}
