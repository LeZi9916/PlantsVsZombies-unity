using System;
using PvZReplica.Adventure;
using UnityEngine;

namespace PvZReplica.Reanimation
{
    public sealed partial class ReanimationRenderer
    {
        private Zombie nativeCombatZombie;
        private string nativeCombatBody;
        private Color nativeCombatTint = Color.white;
        private Color nativeTrackAdditive = Color.clear;
        private float nativeSurfaceAdditive;
        private bool nativeSpriteAdditive;
        private string nativeResourceImageOverride;

        // PC 0x548D40..0x54942E. A single original sprite pass plus its textured ADD pass.
        // The callback includes body and attached heads. Do not redraw the armor afterward.
        public bool DrawNativeZombieCombat(Zombie zombie, Rect viewport, Func<bool> draw)
        {
            if (zombie == null || draw == null) return false;
            Zombie previous = nativeCombatZombie;
            string previousBody = nativeCombatBody;
            Color previousTint = nativeCombatTint, previousAdd = nativeTrackAdditive, color = GUI.color;
            try
            {
                nativeCombatZombie = zombie;
                nativeCombatBody = GameSimulation.AdventureZombiePresentationAsset(zombie);
                nativeCombatTint = color;
                GUI.color = Color.white;
                return DrawNativeClipped("native-zombie-combat-" + zombie.Id, viewport, draw);
            }
            finally
            {
                nativeCombatZombie = previous; nativeCombatBody = previousBody;
                nativeCombatTint = previousTint; nativeTrackAdditive = previousAdd; GUI.color = color;
            }
        }
        private int NativeZombieGroup(Reanimation asset, string track)
        {
            return nativeCombatZombie != null && AdventureZombieFeedbackRules.Same(asset.Name, nativeCombatBody)
                ? AdventureZombieFeedbackRules.RenderGroup(AdventureZombieFeedbackRules.ShieldType(nativeCombatZombie), track) : 0;
        }
        private bool NativeZombieTrackVisible(Reanimation asset, string track)
        {
            if (nativeCombatZombie == null || !AdventureZombieFeedbackRules.Same(asset.Name, nativeCombatBody)) return true;
            int kind = nativeCombatZombie.ModeHead >= 26 ? nativeCombatZombie.ModeHead : (int)nativeCombatZombie.Kind;
            return AdventureZombieEquipmentRules.TrackVisible(nativeCombatZombie,track) && (!nativeCombatZombie.PresentationArmLost || AdventureZombieFeedbackRules.ArmTrackVisible(kind, track)) &&
                (!nativeCombatZombie.Headless || AdventureZombiePartsRules.HeadTrackVisible(kind, track));
        }
        private Texture2D NativeZombieImage(Reanimation asset, string track, Texture2D original)
        {
            if (nativeCombatZombie == null) return original;
            string equipment=AdventureZombieEquipmentRules.Image(nativeCombatZombie,asset.Name,nativeCombatBody,track);
            if(equipment!=null){EnsureNativeAtlas();return LoadTexture(equipment);}
            if(!nativeCombatZombie.PresentationArmLost)return original;
            int kind = nativeCombatZombie.ModeHead >= 26 ? nativeCombatZombie.ModeHead : (int)nativeCombatZombie.Kind;
            string replacement = AdventureZombieFeedbackRules.Same(asset.Name, nativeCombatBody)
                ? AdventureZombieFeedbackRules.ArmReplacement(kind, track) : null;
            if ((kind == 1 || kind == 33) && AdventureZombieFeedbackRules.Same(track, "Zombie_flag")) replacement = "IMAGE_REANIM_ZOMBIE_FLAG3";
            if (replacement == null) return original;
            EnsureNativeAtlas();
            return LoadTexture(replacement);
        }
        private Color NativeZombieTrackColor(int group, float alpha)
        {
            Zombie z = nativeCombatZombie;
            Color tint = Color.white, additive = Color.clear;
            if (group == 1)
            {
                float flash = AdventureZombieFeedbackRules.FlashChannel(z.PresentationArmorFlash) / 255f;
                additive = new Color(flash, flash, flash, 1f);
            }
            else
            {
                if (z.Hypnotized) tint = new Color(128f / 255f, 64f / 255f, 192f / 255f, 1f);
                else if (z.ChillRemaining > 0 || z.FrozenRemaining > 0 && z.PresentationButter <= 0)
                    tint = new Color(75f / 255f, 75f / 255f, 1f, 1f);
                else if (z.PresentationNativeHeight == 10 && z.Health < 100f)
                    tint = new Color(100f / 255f, 150f / 255f, 25f / 255f, 1f);
                if (z.Hypnotized || z.ChillRemaining > 0 || z.FrozenRemaining > 0 && z.PresentationButter <= 0 ||
                    z.PresentationNativeHeight == 10 && z.Health < 100f) additive = tint;
                bool sled = z.PresentationHasSled ?? (z.Kind == ZombieKind.Bobsled && !z.UsedAbility);
                additive.r = AdventureZombieFeedbackRules.AdditiveChannel((int)Math.Round(additive.r * 255f), z.PresentationFlash, sled) / 255f;
                additive.g = AdventureZombieFeedbackRules.AdditiveChannel((int)Math.Round(additive.g * 255f), z.PresentationFlash, sled) / 255f;
                additive.b = AdventureZombieFeedbackRules.AdditiveChannel((int)Math.Round(additive.b * 255f), z.PresentationFlash, sled) / 255f;
            }
            // A frozen-black burn corpse is supplied in the caller's native tint.
            if (nativeCombatTint.r == 0f && nativeCombatTint.g == 0f && nativeCombatTint.b == 0f) additive = Color.clear;
            nativeTrackAdditive = additive;
            return new Color(tint.r * nativeCombatTint.r, tint.g * nativeCombatTint.g,
                tint.b * nativeCombatTint.b, alpha * nativeCombatTint.a);
        }
        private Matrix4x4 NativeZombieTrackMatrix(Matrix4x4 matrix, int group)
        {
            if (nativeCombatZombie != null && group == 1)
                matrix.m03 += (int)(3f * Math.Min(.12f, nativeCombatZombie.PresentationShieldRecoil) / .12f);
            return matrix;
        }

        // zombieOrigin is the SAME source/pivot origin used when the body was rendered at drop.X/Row.
        // All launch, acceleration, spin and bounce values remain in original particle XML.
        public bool DrawNativeZombieArmDrop(OriginalParticleRenderer particles, AdventureZombieArmDrop drop,
            Vector2 zombieOrigin, float renderRemainder = 0f)
        {
            if (particles == null || drop == null) return false;
            Vector2 point;
            if (!TryGetNativeTrackPosition(drop.Asset, drop.Animation, drop.Anchor, drop.AnimationAge, drop.Rate, true, out point))
                point = new Vector2(45f + (drop.Backwards ? 36f : 0f), 78f);
            else if (drop.Backwards) point.x = 90f - point.x;
            Vector2 origin = zombieOrigin + point * drop.Scale;
            string previousImage = nativeResourceImageOverride;
            Color previousColor = GUI.color;
            try
            {
                nativeResourceImageOverride = drop.Image;
                if (drop.Hypnotized) GUI.color = new Color(128f / 255f, 64f / 255f, 192f / 255f, previousColor.a);
                else if (drop.Chilled) GUI.color = new Color(75f / 255f, 75f / 255f, 1f, previousColor.a);
                return particles.Draw("native-arm-" + drop.Id, drop.Mowed ? "MoweredZombieArm" : "ZombieArm",
                    drop.Age + renderRemainder, origin, drop.Seed, drop.Scale);
            }
            finally { nativeResourceImageOverride = previousImage; GUI.color = previousColor; }
        }

        public bool DrawNativeZombiePartDrop(OriginalParticleRenderer particles, AdventureZombiePartDrop drop,
            Vector2 zombieOrigin, float renderRemainder = 0f)
        {
            if (particles == null || drop == null) return false;
            Vector2 point,origin;
            if(drop.ParticleId==54)
            {
                // PC DropShield ladder: (mPosX+31,mPosY+80), independent of the ladder track.
                origin=zombieOrigin+new Vector2(16f-30f*(1f-drop.Scale),80f-120f*(1f-drop.Scale));
            }
            else
            {
                if (!TryGetNativeTrackPosition(drop.Asset, drop.Animation, drop.Anchor, drop.AnimationAge, drop.Rate, true, out point))
                    return false;
                if (drop.Backwards) point.x = 90f - point.x;
                origin=zombieOrigin+point*drop.Scale;
            }
            if (drop.ParticleId == 11) origin += new Vector2(6f, -45f); // PC 0x5453B0, unscaled world offset.
            string previousImage = nativeResourceImageOverride;
            Color previousColor = GUI.color;
            try
            {
                nativeResourceImageOverride = drop.Image;
                if (drop.Hypnotized) GUI.color = new Color(128f / 255f, 64f / 255f, 192f / 255f, previousColor.a);
                else if (drop.Chilled) GUI.color = new Color(75f / 255f, 75f / 255f, 1f, previousColor.a);
                // Backup dancer's 1.05 is an absolute particle scale in PC, after ApplyZombieScale.
                float scale = drop.NativeKind == 9 && drop.ParticleId != 11 ? 1.05f : drop.Scale;
                return particles.Draw("native-zombie-part-" + drop.Id, AdventureZombiePartsRules.ParticleName(drop.ParticleId),
                    drop.Age + renderRemainder, origin, drop.Seed, scale);
            }
            finally { nativeResourceImageOverride = previousImage; GUI.color = previousColor; }
        }
        public bool DrawNativeZombieFlagAttachment(Vector2 origin, float scale, string bodyClip, float seconds,
            float bodyRate, float flagSeconds, bool backwards)
        {
            Reanimation parent, child; Clip body, flag;
            if (!TryGetClip("Zombie", bodyClip, out parent, out body) ||
                !TryGetNativeDefinitionClip("Zombie_FlagPole", out child, out flag)) return false;
            Track anchor = parent.Tracks.Find(t => TrackIs(t.Name, "Zombie_flaghand"));
            Matrix4x4 inverse;
            if (anchor == null || !TryInverseAffine(PartMatrix(GetFrame(anchor, 0)), out inverse)) return false;
            FrameSample sample = SampleFrame(body, NativeSeconds(body, seconds, bodyRate, parent.Fps, true), parent.Fps, false);
            Frame pose = Interpolate(GetFrame(anchor, sample.Current), GetFrame(anchor, sample.Next), sample.Fraction);
            return DrawClipPose(child, flag, NativeSeconds(flag, flagSeconds, 15f, child.Fps, true), child.Fps, false,
                SpriteMatrix * CombatPlacement(origin, scale, backwards, 90f) * PartMatrix(pose) * inverse, GUI.color);
        }

        private void DrawNativeSpritePass(Texture texture, float width, float height, Rect uv, int pass, Matrix4x4 matrix)
        {
            nativePortraitMaterial.SetTexture("_MainTex", texture);
            nativePortraitMaterial.SetPass(pass);
            GL.Begin(GL.QUADS);
            GL.Color(GUI.color);
            GL.TexCoord2(uv.xMin, uv.yMax); GL.Vertex(matrix.MultiplyPoint3x4(new Vector3(0, 0, 0)));
            GL.TexCoord2(uv.xMax, uv.yMax); GL.Vertex(matrix.MultiplyPoint3x4(new Vector3(width, 0, 0)));
            GL.TexCoord2(uv.xMax, uv.yMin); GL.Vertex(matrix.MultiplyPoint3x4(new Vector3(width, height, 0)));
            GL.TexCoord2(uv.xMin, uv.yMin); GL.Vertex(matrix.MultiplyPoint3x4(new Vector3(0, height, 0)));
            GL.End();
        }
        private void DrawNativeSurfaceComposite(Texture texture, Rect viewport, Rect? source = null)
        {
            if (!EnsureNativeSpriteMaterial()) return;
            GL.PushMatrix();
            try
            {
                GL.LoadPixelMatrix(0, Screen.width, Screen.height, 0);
                GL.modelview = Matrix4x4.identity;
                DrawNativeSpritePass(texture, viewport.width, viewport.height, source ?? new Rect(0, 0, 1, 1), 3,
                    GUI.matrix * NativeMatrix(new Vector2(viewport.x, viewport.y), 1f));
            }
            finally { GL.PopMatrix(); }
        }
        private bool EnsureNativeSpriteMaterial()
        {
            if (nativePortraitMaterial != null) return true;
            Shader shader = Resources.Load<Shader>("Shaders/PvZCombatSprite");
            if (shader == null) return Fail("Native combat sprite shader resource is missing.");
            nativePortraitMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            return true;
        }
    }
}
