#nullable enable
using System;

namespace PlantsVsZombies.Adventure
{
    /// <summary>Describes the original Crazy Dave animation and voice selection for a message.</summary>
    public sealed class CrazyDaveSpeech
    {
        /// <summary>Stores the original sound name or variant prefix.</summary>
        private readonly string? _soundPrefix;

        /// <summary>Creates a selection of animation and voice behavior.</summary>
        /// <param name="animationName">The animation track, or null to keep the current animation.</param>
        /// <param name="loopAnimation">Whether the animation loops.</param>
        /// <param name="soundPrefix">The sound asset name or variant prefix.</param>
        /// <param name="soundVariantCount">The number of available sound variants.</param>
        /// <param name="stopPreviousSound">Whether the previous voice should stop.</param>
        /// <param name="handing">Whether the animation uses the handing state.</param>
        /// <param name="heldItem">The original attachment identifier, if any.</param>
        private CrazyDaveSpeech(string? animationName, bool loopAnimation, string? soundPrefix, int soundVariantCount, bool stopPreviousSound, bool handing, string? heldItem)
        {
            AnimationName = animationName;
            LoopAnimation = loopAnimation;
            _soundPrefix = soundPrefix;
            SoundVariantCount = soundVariantCount;
            StopPreviousSound = stopPreviousSound;
            Handing = handing;
            HeldItem = heldItem;
        }

        /// <summary>Gets the animation track, or null when the current animation should continue.</summary>
        public string? AnimationName { get; }

        /// <summary>Gets the original speaking animation rate in frames per second.</summary>
        public float AnimationFramesPerSecond => 12f;

        /// <summary>Gets whether the speaking animation loops.</summary>
        public bool LoopAnimation { get; }

        /// <summary>Gets the number of available voice variants.</summary>
        public int SoundVariantCount { get; }

        /// <summary>Gets whether the previous voice should be stopped before this message.</summary>
        public bool StopPreviousSound { get; }

        /// <summary>Gets whether the selected animation uses the handing state.</summary>
        public bool Handing { get; }

        /// <summary>Gets the original attachment identifier, or null when no object is shown.</summary>
        public string? HeldItem { get; }

        /// <summary>Gets the original sound asset name for a zero-based variant.</summary>
        /// <param name="variant">The selected zero-based variant.</param>
        /// <returns>The sound asset name without its file extension.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The selected variant is outside the available range.</exception>
        public string GetSoundName(int variant)
        {
            if (variant < 0 || variant >= SoundVariantCount)
            {
                throw new ArgumentOutOfRangeException(nameof(variant));
            }

            return SoundVariantCount == 1 ? _soundPrefix! : _soundPrefix + (variant + 1);
        }

        /// <summary>Selects the animation and voice using the original message control markers.</summary>
        /// <param name="message">The message, including any control markers.</param>
        /// <param name="currentlyTalking">Whether Dave is already in the normal talking state.</param>
        /// <returns>The selected animation and voice behavior.</returns>
        /// <exception cref="ArgumentNullException">The message is null.</exception>
        public static CrazyDaveSpeech Select(string message, bool currentlyTalking)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            var doSound = !HasMarker(message, "NO_SOUND");
            if (!doSound && currentlyTalking)
            {
                return new CrazyDaveSpeech(null, false, null, 0, false, false, null);
            }

            string animation;
            string sound;
            string? heldItem = null;
            var variants = 1;
            var handing = false;
            if (HasMarker(message, "HANDING"))
            {
                handing = true;
                animation = "anim_talk_handing";
                if (HasMarker(message, "SHORT_SOUND"))
                {
                    sound = "crazydaveshort";
                    variants = 3;
                }
                else if (HasMarker(message, "SCREAM"))
                {
                    sound = "crazydavescream";
                }
                else
                {
                    sound = "crazydavelong";
                    variants = 3;
                }
            }
            else if (HasMarker(message, "SHAKE"))
            {
                animation = "anim_crazy";
                sound = "crazydavecrazy";
            }
            else if (HasMarker(message, "SCREAM"))
            {
                animation = "anim_smalltalk";
                sound = "crazydavescream";
            }
            else if (HasMarker(message, "SCREAM2"))
            {
                animation = "anim_mediumtalk";
                sound = "crazydavescream2";
            }
            else if (HasMarker(message, "SHOW_WALLNUT") || HasMarker(message, "SHOW_HAMMER") || HasMarker(message, "SHOW_FERTILIZER") || HasMarker(message, "SHOW_TREE_FOOD") || HasMarker(message, "SHOW_MONEYBAG"))
            {
                handing = true;
                animation = "anim_talk_handing";
                if (HasMarker(message, "SHOW_WALLNUT"))
                {
                    heldItem = "WALLNUT";
                    sound = "crazydavescream2";
                }
                else
                {
                    heldItem = HasMarker(message, "SHOW_HAMMER") ? "HAMMER" : HasMarker(message, "SHOW_FERTILIZER") ? "FERTILIZER" : HasMarker(message, "SHOW_TREE_FOOD") ? "TREE_FOOD" : "MONEYBAG";
                    sound = "crazydavelong";
                    variants = 3;
                }
            }
            else
            {
                var count = CountVisibleCharacters(message);
                if (count < 23)
                {
                    animation = "anim_smalltalk";
                    sound = "crazydaveshort";
                    variants = 3;
                }
                else if (count < 52)
                {
                    animation = "anim_mediumtalk";
                    sound = "crazydavelong";
                    variants = 3;
                }
                else
                {
                    animation = "anim_blahblah";
                    sound = "crazydaveextralong";
                    variants = 3;
                }
            }

            return new CrazyDaveSpeech(animation, handing, doSound ? sound : null, doSound ? variants : 0, doSound, handing, heldItem);
        }

        /// <summary>Determines whether a message contains an original control marker.</summary>
        /// <param name="message">The message to inspect.</param>
        /// <param name="marker">The marker name without braces.</param>
        /// <returns>Whether the marker is present.</returns>
        private static bool HasMarker(string message, string marker)
        {
            return message.IndexOf("{" + marker + "}", StringComparison.Ordinal) >= 0;
        }

        /// <summary>Counts characters outside control markers using the original scanning rules.</summary>
        /// <param name="message">The message to inspect.</param>
        /// <returns>The number of characters outside braces.</returns>
        private static int CountVisibleCharacters(string message)
        {
            var count = 0;
            var controlWord = false;
            foreach (var character in message)
            {
                if (character == '{')
                {
                    controlWord = true;
                }
                else if (character == '}')
                {
                    controlWord = false;
                }
                else if (!controlWord)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
