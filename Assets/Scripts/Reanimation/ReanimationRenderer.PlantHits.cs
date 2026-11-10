using System;
using PvZReplica.Adventure;
using UnityEngine;
namespace PvZReplica.Reanimation
{
    public sealed partial class ReanimationRenderer
    {
        // PC 0x547810 raises the SAME +0xB8 counter that production uses.
        // Reuse the existing source-texture ADD path, preserving original attachments and Imitater filtering.
        public bool DrawNativePlantHitFeedback(string key,Rect viewport,Func<bool> draw,Plant plant)
        {return DrawProducerGlow(key,viewport,draw,AdventurePlantHitRules.Combined(plant),plant.IsImitated);}
    }
}
