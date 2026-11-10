using System;
using PvZReplica.Adventure;
using UnityEngine;

namespace PvZReplica.Reanimation
{
    public sealed partial class ReanimationRenderer
    {
        private bool producerGlowShaderWarningReported;

        /// <summary>
        /// Draw the existing body/attached-track callback with the original source-colored additive brightness.
        /// The shared CombatSprite shader must expose _ProducerAdditive and apply
        /// saturate(pixel.rgb * (1 + _ProducerAdditive)) after its Imitater filter, before white-hit treatment.
        /// Production brightness is a per-track material operation; no glow sprite, blur or particle is added.
        /// The caller draws its original plant shadow outside this callback, as in the native renderer.
        /// </summary>
        public bool DrawProducerGlow(string instanceKey, Rect viewport, Func<bool> draw,
            AdventureProducerGlowVisual visual, bool imitated = false)
        {
            if (draw == null) return Fail("An existing producer reanimation draw callback is required.");
            if (!visual.Enabled && !imitated) return draw();
            return DrawNativeClipped("producer-" + instanceKey, viewport, () =>
            {
                // DrawNativeClipped creates its material before entering the callback. Use a separate property
                // from _Additive: the latter belongs to the shared white-hit effect and has a different color.
                if (!nativePortraitMaterial.HasProperty("_ProducerAdditive"))
                {
                    if (!producerGlowShaderWarningReported)
                    {
                        producerGlowShaderWarningReported = true;
                        Debug.LogWarning("Original producer hint awaits the CombatSprite _ProducerAdditive source-color hook.");
                    }
                    return draw();
                }
                float previous = nativePortraitMaterial.GetFloat("_ProducerAdditive");
                try
                {
                    nativePortraitMaterial.SetFloat("_ProducerAdditive", visual.Enabled ? visual.SourceAdditive : 0f);
                    return draw();
                }
                finally { nativePortraitMaterial.SetFloat("_ProducerAdditive", previous); }
            }, false, imitated, 0f);
        }
    }
}
