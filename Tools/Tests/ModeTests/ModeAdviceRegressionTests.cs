#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using PlantsVsZombies.Modes;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Tests.Modes
{
    /// <summary>Checks that independent mode advice uses original resources and native slot award messages.</summary>
    internal static class ModeAdviceRegressionTests
    {
        /// <summary>Runs focused mode introduction, survival stage, and slot result advice regressions.</summary>
        public static void Run()
        {
            var introductions = new Dictionary<int, string>
            {
                { 18, "[ADVICE_SLOT_MACHINE_PULL]" },
                { 20, "[ADVICE_BEGHOULED_DRAG_TO_MATCH_3]" },
                { 22, "[ADVICE_FILL_IN_STARFRUIT]" },
                { 23, "[ADVICE_ZOMBIQUARIUM_CLICK_TO_FEED]" },
                { 24, "[ADVICE_BEGHOULED_TWIST_TO_MATCH_3]" },
                { 31, "[ADVICE_SURVIVE_FLAGS]" }
            };
            foreach (var definition in ModeCatalog.All)
            {
                var session = new ModeSession(definition.Id);
                var expected = definition.Survival
                    ? definition.Endless ? "[ADVICE_SURVIVE_ENDLESS]" : "[ADVICE_SURVIVE_FLAGS]"
                    : definition.Mechanic == ModeMechanic.IZombie ? "[ADVICE_I_ZOMBIE_EAT_ALL_BRAINS]"
                    : introductions.TryGetValue(definition.Id, out var original) ? original : "";
                Require(session.Advice == expected && session.Game.ModeInstruction() == expected,
                    "Mode " + definition.Id + " must initialize with its original advice, without replacement tutorial prose.");
                if (definition.Survival || definition.Mechanic == ModeMechanic.LastStand)
                {
                    session.Stage = 2;
                    Require(session.Game.ModeInstruction() == "", "Original survival introductions must not repeat at later stages.");
                }
            }

            Require(SlotAdvice(0, 1, 2) == "[ADVICE_SLOT_MACHINE_SPIN_AGAIN]", "Unmatched reels use original spin-again advice.");
            Require(SlotAdvice(0, 1, 0) == "[ADVICE_SLOT_MACHINE_2_OF_A_KIND]", "Two plant symbols use the original two-of-a-kind advice.");
            Require(SlotAdvice(0, 0, 0) == "[ADVICE_SLOT_MACHINE_3_OF_A_KIND]", "Three plant symbols use the original three-of-a-kind advice.");
            Require(SlotAdvice(56, 1, 56) == "[ADVICE_SLOT_MACHINE_2_SUNS]", "Two sun symbols use the original bonus-sun advice.");
            Require(SlotAdvice(56, 56, 56) == "[ADVICE_SLOT_MACHINE_SUN_JACKPOT]", "Three sun symbols use the original sun-jackpot advice.");
            Require(SlotAdvice(57, 57, 1) == "[ADVICE_SLOT_MACHINE_2_DIAMONDS]", "Two diamond symbols use the original diamond advice.");
            Require(SlotAdvice(57, 57, 57) == "[ADVICE_SLOT_MACHINE_DIAMOND_JACKPOT]", "Three diamond symbols use the original diamond-jackpot advice.");
            Console.WriteLine("PASS: original mode introduction and slot-result advice regressions.");
        }

        /// <summary>Resolves a controlled isolated reel result through the production slot update.</summary>
        /// <param name="first">The first native slot symbol.</param>
        /// <param name="second">The second native slot symbol.</param>
        /// <param name="third">The third native slot symbol.</param>
        /// <returns>The production advice resource selected for the result.</returns>
        private static string SlotAdvice(int first, int second, int third)
        {
            var session = new ModeSession(18);
            session.SlotSymbols[0] = first;
            session.SlotSymbols[1] = second;
            session.SlotSymbols[2] = third;
            session.SlotRollingRemaining = .01f;
            typeof(GameSimulation).GetMethod("TickModeSlot", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(session.Game, new object[] { .02f });
            return session.Advice;
        }

        /// <summary>Fails the focused regression if an original advice contract is broken.</summary>
        /// <param name="condition">Whether the original advice behavior was observed.</param>
        /// <param name="message">The advice contract being verified.</param>
        /// <exception cref="InvalidOperationException">The advice contract is violated.</exception>
        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
