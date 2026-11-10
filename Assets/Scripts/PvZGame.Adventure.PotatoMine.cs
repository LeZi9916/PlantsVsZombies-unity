using System;
using PvZReplica.Adventure;
using UnityEngine;
namespace PvZReplica
{
    public sealed partial class PvZGame
    {
        private GameSimulation potatoMineAudioGame;
        private int potatoMineRiseAudioSerial;
        private bool DrawAdventurePotatoMine(Plant plant, Vector2 origin, float scale)
        {
            if (plant.Kind != PlantKind.PotatoMine) return false;
            var state = plant.PotatoMinePresentation;
            string clip = plant.MinePhase == MinePhase.Buried ? "anim_idle" : plant.MinePhase == MinePhase.Rising ? "anim_rise" : "anim_armed";
            float rate = state == null ? 12f : state.BodyRate;
            float age = plant.MinePhase == MinePhase.Buried ? 0f : plant.PhaseAge + AdventureRenderRemainder;
            // PC UpdateReanim 471D60: translation (+12,+12), scale (.8,.8) for type 4.
            Vector2 nativeOrigin = origin + new Vector2(12f * scale, 12f * scale);
            float nativeScale = .8f * scale;
            Func<bool> draw = () => {
                bool drawn = animations.DrawNativePlayback("PotatoMine", nativeOrigin, nativeScale, age,
                    clip, plant.MinePhase != MinePhase.Rising, rate, track => !string.Equals(track,"anim_glow",StringComparison.OrdinalIgnoreCase),
                    null,null,plant.ModeFacesLeft,"plant-" + plant.Id,plant.MinePhase == MinePhase.Rising ? 20 : 0);
                if (state != null && state.LightCreated && plant.MinePhase == MinePhase.Armed)
                {
                    float phase = state.GlowTime + AdventureRenderRemainder * (state.BodyRate - 2f) / state.GlowFrames;
                    drawn |= animations.DrawNativePotatoMineLight(nativeOrigin,nativeScale,age,rate,phase,state.GlowFrames);
                }
                return drawn;
            };
            if (plant.IsImitated || plant.PresentationEatenFlashTicks > 0)
            {
                string key = "producer-plant-" + plant.Id;
                adventurePresentationSurfaces.Add(key); adventurePresentationLiveSurfaces.Add(key);
                animations.DrawNativePlantHitFeedback("plant-" + plant.Id,new Rect(origin.x-80,origin.y-160,300,340),draw,plant);
            }
            else draw();
            return true;
        }
        private void UpdateAdventurePotatoMineAudio()
        {
            if (potatoMineAudioGame != game) { potatoMineAudioGame = game; potatoMineRiseAudioSerial = 0; }
            if (game == null || !game.IsAdventure) return;
            int serial = game.Presentation.PotatoMineRiseSoundSerial;
            while (potatoMineRiseAudioSerial < serial) { potatoMineRiseAudioSerial++; PlayPresentationSound("dirt_rise"); }
        }
    }
}
