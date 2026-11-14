#nullable enable

using System;
using PlantsVsZombies.BoardUI;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Preserves PC Squash contact targeting, damage overlap, and native flight interpolation.</summary>
    public static class AdventureSquashRules
    {
        /// <summary>Gets the PC Squash's narrow attack rectangle at its current native position.</summary>
        /// <param name="nativeX">The current plant body's native horizontal origin.</param>
        /// <returns>The original forty-five-pixel-wide attack rectangle.</returns>
        public static NativeRect AttackRectangle(int nativeX)
        {
            return new NativeRect(nativeX + 20, 0, 45, 80);
        }

        /// <summary>Gets the signed horizontal intersection used by both original Squash routines.</summary>
        /// <param name="attack">The Squash's native attack interval.</param>
        /// <param name="body">The candidate zombie's native body interval.</param>
        /// <returns>The overlapping pixels, or the negative gap between separated intervals.</returns>
        public static int Overlap(NativeRect attack, NativeRect body)
        {
            return Math.Min(attack.X + attack.Width, body.X + body.Width) - Math.Max(attack.X, body.X);
        }

        /// <summary>Checks PC FindSquashTarget's movement, proximity, and rear-edge restrictions after damage admission.</summary>
        /// <param name="plant">The planted Squash performing the original contact scan.</param>
        /// <param name="zombie">The candidate zombie whose head and movement state are checked.</param>
        /// <param name="body">The native body rectangle used by the local executable.</param>
        /// <param name="nativeX">The Squash's native horizontal origin.</param>
        /// <param name="phase">The resolved original zombie phase, including legacy simulation movement states.</param>
        /// <param name="vasebreaker">Whether the original Scary Potter rear-edge exception applies.</param>
        /// <returns>Whether the original actor can trigger or redirect this Squash.</returns>
        public static bool CanTarget(Plant plant, Zombie zombie, NativeRect body, int nativeX, int phase, bool vasebreaker)
        {
            if (zombie.Headless || zombie.TangleKelpGrab != null || zombie.PresentationNativeHeight == 3
                || zombie.Kind != ZombieKind.Boss && zombie.Row != plant.Row
                || phase == 11 && body.X >= nativeX + 20 || phase == 12 || phase == 58
                || phase == 52 || phase == 53 || phase == 54
                || zombie.Kind == ZombieKind.Bobsled && (zombie.PresentationHasSled ?? !zombie.UsedAbility))
            {
                return false;
            }

            var attack = AttackRectangle(nativeX);
            if (-Overlap(attack, body) > (zombie.Eating ? 110 : 70))
            {
                return false;
            }

            var rear = attack.X;
            if (phase == 11 || phase == 13 || phase == 55 || zombie.Kind == ZombieKind.Imp || zombie.Kind == ZombieKind.Football || vasebreaker)
            {
                rear -= 60;
            }

            var backwards = zombie.PresentationWalkingBackwards ?? (zombie.Hypnotized || zombie.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(zombie));
            return backwards || body.X + body.Width >= rear;
        }

        /// <summary>Samples PC TodAnimateCurve(50,20,counter,start,end,EaseInOut) for the rising body.</summary>
        /// <param name="remaining">The native rising countdown.</param>
        /// <param name="start">The body's original planting coordinate.</param>
        /// <param name="end">The locked landing or elevated destination coordinate.</param>
        /// <returns>The original integer position after two smooth-step applications.</returns>
        public static int RisingPosition(int remaining, int start, int end)
        {
            var time = Math.Max(0f, Math.Min(1f, (50f - remaining) / 30f));
            var first = time * time * (3f - 2f * time);
            var second = first * first * (3f - 2f * first);
            return (int)Math.Round(start + (end - start) * second, MidpointRounding.AwayFromZero);
        }
    }
}
