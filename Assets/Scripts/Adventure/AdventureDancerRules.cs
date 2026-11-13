#nullable enable

using System;
using System.Collections.Generic;
using PlantsVsZombies.Simulation;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Applies the local PC dancer routines at 0x5441E0 through 0x54496F and 0x549CF0.</summary>
    public static class AdventureDancerRules
    {
        /// <summary>The original backup dancer's linear emergence duration.</summary>
        public const int RiseTicks = 150;

        /// <summary>Checks whether a zombie uses the original shared dance behavior.</summary>
        /// <param name="zombie">The zombie to inspect.</param>
        /// <returns>True for a dancing zombie or backup dancer.</returns>
        public static bool IsDancer(Zombie zombie)
        {
            return zombie.Kind == ZombieKind.Dancing || zombie.Kind == ZombieKind.BackupDancer;
        }

        /// <summary>Gets the original visual scale, including the later Little Trouble override.</summary>
        /// <param name="zombie">The zombie whose native scale is needed.</param>
        /// <returns>One half for small zombies, four fifths for dancers, and one for other zombies.</returns>
        public static float Scale(Zombie zombie)
        {
            return zombie.IsSmall ? .5f : IsDancer(zombie) ? .8f : 1f;
        }

        /// <summary>Initializes the original entrance or independent backup dance phase.</summary>
        /// <param name="zombie">The newly created dancer.</param>
        /// <param name="entranceRandom">The original random integer in the range zero through eleven.</param>
        public static void Initialize(Zombie zombie, int entranceRandom)
        {
            zombie.DancerState = new AdventureDancerState();
            if (zombie.Kind == ZombieKind.Dancing)
            {
                SetPhase(zombie, 40);
                zombie.DancerState.PhaseTicks = 300 + entranceRandom;
                zombie.PresentationNativeVelocity = .5f;
                zombie.Speed = AdventureZombieNativeRules.GridSpeed(.5f);
            }
            else
            {
                SetPhase(zombie, 44);
            }
        }

        /// <summary>Gets the shared PC dance frame without restarting the clock when a zombie spawns.</summary>
        /// <param name="zombie">The dancer whose entrance and immobilization state select the frame.</param>
        /// <param name="clockTicks">The shared native board clock.</param>
        /// <returns>The original entrance frame zero through ten, or dance frame zero through twenty-two.</returns>
        public static int Frame(Zombie zombie, int clockTicks)
        {
            if (zombie.Wave == -3 || zombie.FrozenRemaining > 0f || zombie.PresentationButter > 0f)
            {
                return 0;
            }

            return zombie.PresentationNativePhase == 40 ? clockTicks % 110 / 10 : clockTicks % 460 / 20;
        }

        /// <summary>Gets the phases actually returned by the local PC executable at 0x549D40.</summary>
        /// <param name="frame">The shared frame zero through twenty-two.</param>
        /// <returns>The original walking, walk-to-raise, or raised-arm phase.</returns>
        public static int Phase(int frame)
        {
            // The local PC merges frames 13..18 into phase 46 and 19..22 into phase 48.
            return frame <= 11 ? 44 : frame == 12 ? 45 : frame <= 18 ? 46 : 48;
        }

        /// <summary>Gets the original clip for a dancer's current native phase.</summary>
        /// <param name="zombie">The live dancer.</param>
        /// <returns>The original moonwalk, pointing, raised-arm, eating, or walking track.</returns>
        public static string Animation(Zombie zombie)
        {
            if (zombie.Eating)
            {
                return "anim_eat";
            }

            var phase = zombie.PresentationNativePhase;
            return phase == 40 ? "anim_moonwalk" : phase >= 41 && phase <= 43 ? "anim_point"
                : phase >= 45 && phase <= 49 ? "anim_armraise" : "anim_walk";
        }

        /// <summary>Gets a fixed source rate for clips whose playback does not follow walking velocity.</summary>
        /// <param name="zombie">The dancer whose phase and meal state select the source rate.</param>
        /// <returns>The fixed rate, or minus one when native ground-track walking determines it.</returns>
        public static float FixedRate(Zombie zombie)
        {
            if (zombie.Eating)
            {
                return 36f;
            }

            var phase = zombie.PresentationNativePhase;
            return phase == 50 ? 0f : phase >= 40 && phase <= 43 ? 24f
                : phase >= 45 && phase <= 49 ? 18f : -1f;
        }

        /// <summary>Checks whether the original pointing clip plays once and holds its final pose.</summary>
        /// <param name="zombie">The dancer whose current phase is inspected.</param>
        /// <returns>True during the pointing gesture and its two-hundred-update hold.</returns>
        public static bool HoldsPoint(Zombie zombie)
        {
            return IsDancer(zombie) && !zombie.Eating
                && zombie.PresentationNativePhase >= 41 && zombie.PresentationNativePhase <= 43;
        }

        /// <summary>Gets the original blend selected by dancer initialization, dance updates, and meal transitions.</summary>
        /// <param name="zombie">The actor whose current native body playback is being drawn.</param>
        /// <returns>Zero for entrance or emergence, ten for ordinary dance steps, and twenty for pointing or meals.</returns>
        public static int AnimationBlendTicks(Zombie zombie)
        {
            if (!IsDancer(zombie) || zombie.Eating || zombie.PresentationNativePhase < 40 || zombie.PresentationNativePhase > 50)
            {
                return 20;
            }

            return zombie.PresentationNativePhase == 50 ? 0 : zombie.DancerState?.AnimationBlendTicks ?? 20;
        }

        /// <summary>Checks the original PC spotlight phase exclusions at 0x548B00.</summary>
        /// <param name="zombie">The actor whose native phase selects the spotlight.</param>
        /// <returns>True for a leader after its first pointing gesture and outside normal death.</returns>
        public static bool HasSpotlight(Zombie zombie)
        {
            var phase = zombie.PresentationNativePhase;
            return zombie.Kind == ZombieKind.Dancing && phase != 40 && phase != 41 && phase != 0 && phase != 1;
        }

        /// <summary>Gets the original PC spotlight color cycle at 0x548B4E.</summary>
        /// <param name="ageTicks">The leader's age in native one-hundred-Hertz updates.</param>
        /// <returns>The packed twenty-four-bit RGB color used for both original spotlight images.</returns>
        public static int SpotlightColor(int ageTicks)
        {
            var index = ageTicks >= 700 ? ageTicks / 100 * 7 % 5 : 0;
            switch (index)
            {
                case 1:
                    return 0x72EAAA;
                case 2:
                    return 0xD87ECA;
                case 3:
                    return 0x5A6E8C;
                case 4:
                    return 0xF05A82;
                default:
                    return 0xFAFAA0;
            }
        }

        /// <summary>Gets whether the original dancer body is mirrored for the current dance step.</summary>
        /// <param name="zombie">The live dancer.</param>
        /// <returns>The body-facing flag after the original mind-control inversion.</returns>
        public static bool FacesRight(Zombie zombie)
        {
            var phase = zombie.PresentationNativePhase;
            var right = !zombie.Eating && phase == 40;
            return right != zombie.Hypnotized;
        }

        /// <summary>Gets the original linear altitude of a backup emerging from underground.</summary>
        /// <param name="remainingTicks">The remaining native emergence updates.</param>
        /// <returns>The integer altitude from minus two hundred to zero pixels.</returns>
        public static int RiseAltitude(int remainingTicks)
        {
            return -200 * Math.Max(0, Math.Min(RiseTicks, remainingTicks)) / RiseTicks;
        }

        /// <summary>Finds a live dancer by its original stable board identifier.</summary>
        /// <param name="zombies">The current live board actors.</param>
        /// <param name="id">The requested identifier.</param>
        /// <returns>The matching live zombie, or null after removal.</returns>
        public static Zombie? Find(List<Zombie> zombies, int id)
        {
            if (id != 0)
            {
                foreach (var zombie in zombies)
                {
                    if (zombie.Id == id)
                    {
                        return zombie;
                    }
                }
            }

            return null;
        }

        /// <summary>Checks whether an original slot still owns a living or dying backup.</summary>
        /// <param name="zombies">The board's live zombies.</param>
        /// <param name="corpses">The original death presentations still awaiting removal.</param>
        /// <param name="id">The slot's stable zombie identifier.</param>
        /// <returns>True until the actor and its original death animation have both left the board.</returns>
        public static bool Occupied(List<Zombie> zombies, List<AdventureZombieCorpse> corpses, int id)
        {
            if (Find(zombies, id) != null)
            {
                return true;
            }

            if (id != 0)
            {
                foreach (var corpse in corpses)
                {
                    if (corpse.Zombie != null && corpse.Zombie.Id == id)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Checks the original row restrictions for a summoned backup.</summary>
        /// <param name="level">The active board terrain.</param>
        /// <param name="row">The requested summon lane.</param>
        /// <returns>True for an existing land lane.</returns>
        public static bool CanSummonInRow(AdventureLevelSpec level, int row)
        {
            return level.IsLawnRow(row) && !level.IsWaterRow(row);
        }

        /// <summary>Checks the leader's four original slots without adopting another leader's nearby backups.</summary>
        /// <param name="zombie">The summoning leader.</param>
        /// <param name="zombies">The board's live zombies.</param>
        /// <param name="corpses">The board's retained death presentations.</param>
        /// <param name="level">The current row terrain.</param>
        /// <returns>True when at least one permitted slot has no living or dying backup.</returns>
        public static bool NeedsBackup(Zombie zombie, List<Zombie> zombies, List<AdventureZombieCorpse> corpses, AdventureLevelSpec level)
        {
            var state = zombie.DancerState;
            if (state == null)
            {
                return false;
            }

            for (var slot = 0; slot < 4; slot++)
            {
                var row = zombie.Row + (slot == 0 ? -1 : slot == 1 ? 1 : 0);
                if (CanSummonInRow(level, row) && !Occupied(zombies, corpses, state.Followers[slot]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Advances one original unpaused dancer update and reports a completed pointing gesture.</summary>
        /// <param name="zombie">The dancer being advanced.</param>
        /// <param name="clockTicks">The shared native dance clock.</param>
        /// <param name="needsBackup">Whether a permitted original follower slot is vacant.</param>
        /// <param name="chilled">Whether this dancer or its linked group is chilled.</param>
        /// <returns>True when the original point animation finishes and requests missing backups.</returns>
        public static bool Tick(Zombie zombie, int clockTicks, bool needsBackup, bool chilled)
        {
            var state = zombie.DancerState;
            if (state == null || zombie.FrozenRemaining > 0f || zombie.PresentationButter > 0f)
            {
                return false;
            }

            state.PhaseTicks = Math.Max(0, state.PhaseTicks - 1);
            if (zombie.Eating)
            {
                return false;
            }

            state.PhaseAge += chilled ? .005d : .01d;
            if (zombie.Kind == ZombieKind.BackupDancer)
            {
                if (zombie.PresentationNativePhase != 50 || state.PhaseTicks == 0)
                {
                    SetPhase(zombie, Phase(Frame(zombie, clockTicks)));
                }

                return false;
            }

            if (state.SummonTicks > 0 && --state.SummonTicks == 0)
            {
                if (Frame(zombie, clockTicks) == 12 && !zombie.Headless
                    && AdventureZombieNativeRules.NativePositionX(zombie.X, Scale(zombie)) < 700f)
                {
                    SetPhase(zombie, 42);
                }
                else
                {
                    state.SummonTicks = 1;
                }
            }

            switch (zombie.PresentationNativePhase)
            {
                case 40:
                    if (!zombie.Headless && state.PhaseTicks == 0)
                    {
                        SetPhase(zombie, 41);
                        zombie.PresentationNativeVelocity = .45f;
                        zombie.Speed = AdventureZombieNativeRules.GridSpeed(.45f);
                    }

                    return false;
                case 41:
                case 42:
                    // Original Zombie_disco anim_point is nine frames at 24 fps.
                    if (state.PhaseAge + .0000001d < 9d / 24d)
                    {
                        return false;
                    }

                    SetPhase(zombie, 43, false);
                    state.PhaseTicks = 200;
                    return true;
                case 43:
                    if (state.PhaseTicks > 0)
                    {
                        return false;
                    }

                    break;
            }

            SetPhase(zombie, Phase(Frame(zombie, clockTicks)));
            if (!zombie.Headless && state.SummonTicks == 0 && needsBackup)
            {
                state.SummonTicks = 100;
            }

            return false;
        }

        /// <summary>Checks whether any linked dancer makes the group's movement stop or slow.</summary>
        /// <param name="zombie">The dancer whose original linked group is inspected.</param>
        /// <param name="zombies">The board's live actors.</param>
        /// <param name="chillOnly">Whether to inspect chill instead of eating, ice, and butter.</param>
        /// <returns>True when the requested original condition holds in the linked group.</returns>
        public static bool GroupAffected(Zombie zombie, List<Zombie> zombies, bool chillOnly)
        {
            if (Affected(zombie, chillOnly))
            {
                return true;
            }

            var leader = zombie.Kind == ZombieKind.Dancing ? zombie : Find(zombies, zombie.DancerState?.LeaderId ?? 0);
            if (leader == null || leader.DancerState == null)
            {
                return false;
            }

            if (Affected(leader, chillOnly))
            {
                return true;
            }

            foreach (var id in leader.DancerState.Followers)
            {
                var follower = Find(zombies, id);
                if (follower != null && Affected(follower, chillOnly))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Checks the original fixed-phase and group movement gates.</summary>
        /// <param name="zombie">The dancer attempting to walk.</param>
        /// <param name="zombies">The board's live actors.</param>
        /// <returns>True when the dancer can advance horizontally.</returns>
        public static bool CanWalk(Zombie zombie, List<Zombie> zombies)
        {
            var phase = zombie.PresentationNativePhase;
            return (phase == 40 || phase == 44) && !GroupAffected(zombie, zombies, false);
        }

        /// <summary>Disconnects original dancer relationships when this actor is mind-controlled.</summary>
        /// <param name="zombie">The newly hypnotized dancer.</param>
        /// <param name="zombies">The board's live actors.</param>
        public static void DetachForMindControl(Zombie zombie, List<Zombie> zombies)
        {
            var state = zombie.DancerState;
            if (state == null)
            {
                return;
            }

            if (zombie.Kind == ZombieKind.Dancing)
            {
                Array.Clear(state.Followers, 0, state.Followers.Length);
                return;
            }

            var leader = Find(zombies, state.LeaderId);
            if (leader?.DancerState != null)
            {
                for (var slot = 0; slot < 4; slot++)
                {
                    if (leader.DancerState.Followers[slot] == zombie.Id)
                    {
                        leader.DancerState.Followers[slot] = 0;
                    }
                }
            }

            state.LeaderId = 0;
        }

        /// <summary>Checks a single dancer's original slow or stop condition.</summary>
        /// <param name="zombie">The group member to inspect.</param>
        /// <param name="chillOnly">Whether to test chill rather than stopping conditions.</param>
        /// <returns>Whether this member has the requested movement condition.</returns>
        private static bool Affected(Zombie zombie, bool chillOnly)
        {
            return chillOnly ? zombie.ChillRemaining > 0f : zombie.Eating || zombie.FrozenRemaining > 0f || zombie.PresentationButter > 0f;
        }

        /// <summary>Starts the exact original phase and its clip, preserving held pointing frames when requested.</summary>
        /// <param name="zombie">The dancer changing phase.</param>
        /// <param name="phase">The new original PC phase.</param>
        /// <param name="restartAnimation">Whether the phase change starts its authored clip again.</param>
        private static void SetPhase(Zombie zombie, int phase, bool restartAnimation = true)
        {
            if (zombie.PresentationNativePhase == phase)
            {
                return;
            }

            var previousPhase = zombie.PresentationNativePhase;
            zombie.PresentationNativePhase = phase;
            if (zombie.DancerState != null)
            {
                zombie.DancerState.PhaseAge = 0d;
            }

            if (restartAnimation)
            {
                if (zombie.DancerState != null)
                {
                    // PC Init, 0x544600 and 0x544750 preserve the first walk's twenty-tick blend after the point hold.
                    zombie.DancerState.AnimationBlendTicks = previousPhase < 0 || phase == 40 ? 0
                        : phase == 44 && previousPhase == 43 ? 20 : phase >= 44 && phase <= 49 ? 10 : 20;
                }

                zombie.PresentationAnimation = Animation(zombie);
                zombie.PresentationAnimationAge = phase == 45 ? .6f * 22f / 18f : 0f;
                zombie._presentationWalkPhase = phase == 45 ? .6f : 0f;
                zombie._presentationWalkLoops = 0;
                zombie._presentationMovementAnimation = null;
                zombie.PresentationMovementRate = -1f;
            }
        }
    }
}
