#nullable enable
using PlantsVsZombies.Adventure;
using System;
using System.Text;
using UnityEngine;
using PlantsVsZombies.Game;

namespace PlantsVsZombies.Progression
{
    /// <summary>Owns Crazy Dave's animation, held item, voice and dialogue text shared by the opening, store, garden and tree.</summary>
    internal sealed class CrazyDaveActor
    {
        /// <summary>Provides the game session whose shared state and services this component uses.</summary>
        private readonly PvZGame _owner;

        /// <summary>Creates this component for one game session.</summary>
        /// <param name="owner">The game session whose audio and strings Dave uses.</param>
        internal CrazyDaveActor(PvZGame owner)
        {
            _owner = owner;
        }

        /// <summary>Adopts the source that plays Dave's voice and applies the persisted volume.</summary>
        /// <param name="voice">The source dedicated to Dave's original voice clips.</param>
        /// <param name="volume">The persisted effects volume in the range zero to one.</param>
        internal void Initialize(AudioSource voice, float volume)
        {
            _crazyDaveVoice = voice;
            voice.volume = volume;
        }

        /// <summary>Applies a new effects volume to Dave's voice.</summary>
        /// <param name="volume">The effects volume in the range zero to one.</param>
        internal void SetVoiceVolume(float volume)
        {
            _crazyDaveVoice.volume = volume;
        }

        /// <summary>Plays Dave's currently active original voice independently of other effects.</summary>
        internal AudioSource _crazyDaveVoice = null!;
        /// <summary>Whether the current voice delays a timed store bubble.</summary>
        internal bool _crazyDaveWaitForBubbleVoice;
        /// <summary>The currently displayed original entry, or -1 outside dialogue.</summary>
        internal int _crazyDaveMessageIndex = -1;
        /// <summary>The displayed text with original control markers removed.</summary>
        internal string _crazyDaveText = "";
        /// <summary>The currently displayed native animation.</summary>
        internal string? _crazyDaveAnimation;
        /// <summary>The elapsed time of Dave's current animation.</summary>
        internal float _crazyDaveAnimationAge;
        /// <summary>The playback rate of Dave's current animation.</summary>
        internal float _crazyDaveAnimationRate;
        /// <summary>Whether Dave's current animation repeats.</summary>
        internal bool _crazyDaveAnimationLoop;
        /// <summary>Whether the current animation is a handing pose.</summary>
        internal bool _crazyDaveHanding;
        /// <summary>The original item attached to Dave's hand, or null when he holds nothing.</summary>
        internal string? _crazyDaveHeldItem;
        /// <summary>Elapsed time of the item currently attached to Dave's hand.</summary>
        internal float _crazyDaveHeldItemAge;
        /// <summary>Stores the current message's mouth override for Dave's idle pose.</summary>
        internal string? _crazyDaveIdleMouth;
        /// <summary>Stores the original update ticks until the next blink.</summary>
        internal int _crazyDaveBlinkTicks;
        /// <summary>Stores the active blink age, or -1 when Dave is not blinking.</summary>
        internal float _crazyDaveBlinkAge = -1f;

        /// <summary>Starts one original Dave animation at its native playback rate.</summary>
        /// <param name="animation">The original reanimation track.</param>
        /// <param name="rate">The track playback rate in frames per second.</param>
        /// <param name="loop">Whether the track repeats.</param>
        internal void SetCrazyDaveAnimation(string animation, float rate, bool loop)
        {
            _crazyDaveAnimation = animation;
            _crazyDaveAnimationRate = rate;
            _crazyDaveAnimationLoop = loop;
            _crazyDaveAnimationAge = 0f;
        }

        /// <summary>Advances the Dave pose independently while the cutscene clock is paused.</summary>
        /// <param name="delta">The elapsed unpaused time in seconds.</param>
        internal void UpdateCrazyDaveAnimation(float delta)
        {
            if (_crazyDaveAnimation == null)
            {
                return;
            }

            _crazyDaveAnimationAge += delta;
            if (_crazyDaveHeldItem != null)
            {
                _crazyDaveHeldItemAge += delta;
            }

            if ((_owner.Opening._vaseDialogueActive || _owner.Opening._bowlingShovelLessonActive) && _crazyDaveAnimation == "anim_leave" && _crazyDaveAnimationAge >= .75f
                && !_owner._animations.TryGetNativeAnimationInfo("CrazyDave", "anim_leave", out _))
            {
                _crazyDaveAnimation = null;
                return;
            }
            if (_crazyDaveBlinkAge >= 0f)
            {
                _crazyDaveBlinkAge += delta;
                if (_owner._animations.TryGetNativeAnimationInfo("CrazyDave", "anim_blink", out var blink)
                    && _crazyDaveBlinkAge * 15f >= blink.Count)
                {
                    _crazyDaveBlinkAge = -1f;
                }
            }

            if (_crazyDaveAnimation == "anim_idle" || _crazyDaveAnimation == "anim_idle_handing"
                || _crazyDaveAnimation == "anim_smalltalk" || _crazyDaveAnimation == "anim_mediumtalk"
                || _crazyDaveAnimation == "anim_blahblah" || _crazyDaveAnimation == "anim_crazy"
                || _crazyDaveAnimation == "anim_talk_handing")
            {
                _crazyDaveBlinkTicks--;
                if (_crazyDaveBlinkTicks <= 0)
                {
                    _crazyDaveBlinkTicks = UnityEngine.Random.Range(400, 801);
                    _crazyDaveBlinkAge = 0f;
                }
            }

            if ((_crazyDaveAnimationLoop && _crazyDaveAnimation != "anim_talk_handing") || !_owner._animations.TryGetNativeAnimationInfo("CrazyDave", _crazyDaveAnimation, out var info)
                || _crazyDaveAnimationAge * _crazyDaveAnimationRate < Math.Max(1, info.Count))
            {
                return;
            }

            if (_crazyDaveAnimation == "anim_leave" || _crazyDaveAnimation == "anim_grab")
            {
                _crazyDaveAnimation = null;
                return;
            }

            SetCrazyDaveAnimation(_crazyDaveHanding ? "anim_idle_handing" : "anim_idle", 12f, true);
        }

        /// <summary>Resolves and presents one numbered original conversation entry.</summary>
        /// <param name="index">The original conversation entry number.</param>
        internal void ShowCrazyDaveMessage(int index)
        {
            ShowCrazyDaveResolvedMessage(index, _owner._presenter.OriginalProgressionText("CRAZY_DAVE_" + index));
        }

        /// <summary>Presents an original message after replacing its context-specific values.</summary>
        /// <param name="index">The original conversation entry number.</param>
        /// <param name="message">The original message with context-specific values resolved.</param>
        internal void ShowCrazyDaveResolvedMessage(int index, string message)
        {
            if (_owner._profiles != null && _owner._profiles.SelectedId.HasValue)
            {
                foreach (var profile in _owner._profiles.Profiles)
                {
                    if (profile.Id == _owner._profiles.SelectedId.Value)
                    {
                        message = message.Replace("{PLAYER_NAME}", profile.Name);
                        break;
                    }
                }
            }

            var account = _owner.Accounts.GetSelectedAccount();
            if (account != null)
            {
                message = message.Replace("{MONEY}", CrazyDaveDialogue.FormatMoney(account.DisplayCoins));
                message = message.Replace("{UPGRADE_COST}", CrazyDaveDialogue.FormatMoney((long)ShopCatalog.Price(ShopCatalog.Get(21), account) * 10));
            }

            _crazyDaveIdleMouth = CrazyDaveDialogue.IdleMouthImage(message);
            var talking = _crazyDaveAnimation == "anim_smalltalk" || _crazyDaveAnimation == "anim_mediumtalk" || _crazyDaveAnimation == "anim_blahblah" || _crazyDaveAnimation == "anim_crazy";
            var speech = CrazyDaveSpeech.Select(message, talking);
            if (message.IndexOf("{HANDING}", StringComparison.Ordinal) < 0)
            {
                _crazyDaveHeldItem = speech.HeldItem;
                _crazyDaveHeldItemAge = 0f;
            }

            if (speech.StopPreviousSound)
            {
                _crazyDaveVoice.Stop();
            }

            if (speech.AnimationName != null)
            {
                _crazyDaveHanding = speech.Handing;
                SetCrazyDaveAnimation(speech.AnimationName, speech.AnimationFramesPerSecond, speech.LoopAnimation);
            }

            if (speech.SoundVariantCount > 0)
            {
                var sound = speech.GetSoundName(UnityEngine.Random.Range(0, speech.SoundVariantCount));
                if (_owner.Sounds._presentationSounds.TryGetValue(sound, out var clip) && clip != null)
                {
                    _crazyDaveVoice.volume = _owner.Sounds._soundVolume;
                    _crazyDaveVoice.clip = clip;
                    _crazyDaveWaitForBubbleVoice = sound.StartsWith("crazydaveshort", StringComparison.Ordinal)
                        || sound.StartsWith("crazydavelong", StringComparison.Ordinal)
                        || sound.StartsWith("crazydaveextralong", StringComparison.Ordinal);
                    _crazyDaveVoice.Play();
                }
                else
                {
                    Debug.LogWarning($"{nameof(PvZGame)}: Original Crazy Dave sound {sound} is missing.");
                }
            }

            var visible = new StringBuilder(message.Length);
            var control = false;
            foreach (var character in message)
            {
                if (character == '{')
                {
                    control = true;
                }
                else if (character == '}')
                {
                    control = false;
                }
                else if (!control)
                {
                    visible.Append(character);
                }
            }

            _crazyDaveMessageIndex = index;
            _crazyDaveText = visible.ToString();
        }

        /// <summary>Clears the current message and starts Dave's departure animation.</summary>
        internal void LeaveCrazyDaveDialogue()
        {
            _crazyDaveMessageIndex = -1;
            _crazyDaveText = "";
            _crazyDaveHanding = false;
            _crazyDaveHeldItem = null;
            _crazyDaveIdleMouth = null;
            _crazyDaveVoice.Stop();
            SetCrazyDaveAnimation("anim_leave", 24f, false);
        }
    }
}
