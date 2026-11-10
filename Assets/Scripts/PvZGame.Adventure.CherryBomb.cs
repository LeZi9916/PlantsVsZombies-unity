using System;
using System.Collections.Generic;
using PvZReplica.Adventure;
using UnityEngine;

namespace PvZReplica
{
    public sealed partial class PvZGame
    {
        private readonly List<AudioSource> adventureCherryJuicyVoices = new List<AudioSource>();

        // Hook after the existing plantshadow and origin/size calculation, before generic
        // plant playback. No interpolated scale/pulse: every pose is the original reanim.
        private bool DrawAdventureCherryBomb(Plant plant, Vector2 origin, float scale)
        {
            if (plant.Kind != PlantKind.CherryBomb) return false;
            var state = plant.CherryBombPresentation;
            float age = state == null ? 0f : state.SampleSeconds(AdventureRenderRemainder);
            float rate = state == null ? 10f : state.Rate;
            if (state != null) origin += new Vector2(state.ShakeX, state.ShakeY);
            Func<bool> draw = () => animations.DrawNativePlayback("CherryBomb", origin, scale,
                age, AdventureCherryBombState.Animation, false, rate, null, null, null,
                plant.ModeFacesLeft, "plant-" + plant.Id, 0);
            if (plant.IsImitated || plant.PresentationEatenFlashTicks > 0)
            {
                string key = "producer-plant-" + plant.Id;
                adventurePresentationSurfaces.Add(key); adventurePresentationLiveSurfaces.Add(key);
                animations.DrawNativePlantHitFeedback("plant-" + plant.Id,
                    new Rect(origin.x - 80, origin.y - 160, 300, 340), draw,
                    plant);
            }
            else draw();
            return true;
        }

        // Called synchronously by PlaySound for the existing simulation event. Returning
        // true suppresses the old Explosion->cherrybomb mapping for this Cherry only.
        private bool PlayAdventureCherryBombSounds(GameEvent trigger)
        {
            if (game == null || !game.IsAdventure) return false;
            AdventureCherryBombSound sound;
            bool detonation = false;
            while (game.TryTakeAdventureCherryBombSound(trigger, out sound))
            {
                if (!sound.Detonation) { PlayPresentationSound("reverse_explosion"); continue; }
                detonation = true;
                PlayPresentationSound("cherrybomb");
                AudioClip clip;
                if (audioSource == null || !presentationSounds.TryGetValue("juicy", out clip) || clip == null) continue;
                AudioSource voice = null;
                foreach (var candidate in adventureCherryJuicyVoices)
                    if (candidate != null && !candidate.isPlaying) { voice = candidate; break; }
                if (voice == null)
                {
                    voice = gameObject.AddComponent<AudioSource>();
                    voice.playOnAwake = false; voice.loop = false; voice.spatialBlend = 0f;
                    voice.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
                    voice.ignoreListenerPause = audioSource.ignoreListenerPause;
                    adventureCherryJuicyVoices.Add(voice);
                }
                voice.volume = audioSource.volume * .7f;
                // PC 63E850 applies integer DirectSound frequency after semitone scaling.
                double frequency = Math.Max(100.0, Math.Min(100000.0, clip.frequency * Math.Pow(1.0594630943592953, sound.JuicyPitchSteps)));
                voice.pitch = (float)((int)frequency / (double)clip.frequency);
                voice.clip = clip; voice.Play();
            }
            return trigger == GameEvent.Explosion && detonation;
        }

        private void UpdateAdventureCherryBombAudio()
        {
            // Also delivers fuse notifications from generated plants whose creation did
            // not go through the player's Planted event. This reads explicit events only.
            PlayAdventureCherryBombSounds(GameEvent.Planted);
            if (audioSource != null)
                foreach (var voice in adventureCherryJuicyVoices)
                    if (voice != null) voice.volume = audioSource.volume * .7f;
        }

        private void DisposeAdventureCherryBombAudio()
        {
            foreach (var voice in adventureCherryJuicyVoices) if (voice != null) Destroy(voice);
            adventureCherryJuicyVoices.Clear();
        }
    }
}
