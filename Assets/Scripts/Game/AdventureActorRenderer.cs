#nullable enable
using System;
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Reanimation;
using PlantsVsZombies.Simulation;
using UnityEngine;

namespace PlantsVsZombies.Game
{
    /// <summary>Reuses synchronous animation callbacks instead of allocating closures for every actor draw.</summary>
    internal sealed class AdventureActorRenderer
    {
        /// <summary>The session animation renderer.</summary>
        private readonly ReanimationRenderer? _renderer;

        /// <summary>Gets the renderer, which must already exist wherever it is used.</summary>
        /// <exception cref="System.InvalidOperationException">The member is not available.</exception>
        private ReanimationRenderer RequiredRenderer => _renderer ?? throw new System.InvalidOperationException("The member '_renderer' is not available.");
        /// <summary>The retained callback for plant bodies and attached heads.</summary>
        private readonly Func<bool> _drawPlant;
        /// <summary>The retained callback for cursor imitation composition.</summary>
        private readonly Func<bool> _drawCursor;
        /// <summary>The retained cursor filter for the kernel catapult's idle pose.</summary>
        private static readonly Func<string, bool> s_cursorKernelVisible = track => !string.Equals(track, "Cornpult_butter", StringComparison.OrdinalIgnoreCase);
        /// <summary>Whether the current cursor uses the kernel catapult filter.</summary>
        private bool _cursorKernel;
        /// <summary>The retained callback for zombie bodies and attachments.</summary>
        private readonly Func<bool> _drawZombie;
        /// <summary>The retained track predicate for kernel and butter variants.</summary>
        private readonly Func<string, bool> _plantVisible;
        /// <summary>The retained track predicate for zombie variants and missing parts.</summary>
        private readonly Func<string, bool> _zombieVisible;
        /// <summary>The retained original pool attachment callback, shared by all synchronous zombie draws.</summary>
        private readonly Action<string, Matrix4x4, Matrix4x4, float> _poolTrackAttachment;
        /// <summary>The original particle systems supplying the current zombie's attached seaweed.</summary>
        private OriginalParticleRenderer? _poolParticles;
        /// <summary>The cached head, upper-arm, and tube particle identities for the current zombie.</summary>
        private string? _poolHeadKey, _poolInnerKey, _poolOuterKey;
        /// <summary>The plant whose pose is being drawn synchronously.</summary>
        private Plant _plant = null!;
        /// <summary>The zombie whose pose is being drawn synchronously.</summary>
        private Zombie _zombie = null!;
        /// <summary>The current asset, clip, and playback identity.</summary>
        private string? _name = "", _clip = "", _key = "";
        /// <summary>The stable optional composition surface identity.</summary>
        private string _surfaceKey = "";
        /// <summary>The current native origin.</summary>
        private Vector2 _origin;
        /// <summary>The current native scale, playback time, frame rate, and interpolation remainder.</summary>
        private float _scale, _seconds, _rate, _remainder;
        /// <summary>Whether the current clip loops or the actor faces backwards.</summary>
        private bool _loop, _backwards;
        /// <summary>Whether the current plant is the native shovel target.</summary>
        private bool _highlighted;
        /// <summary>The blend duration used by the current plant pose.</summary>
        private int _blendTicks;
        /// <summary>The reused attached head buffer, or null for a single body animation.</summary>
        private HeadPlayback[]? _heads;
        /// <summary>The optional original per-track shake offsets for the plant currently being drawn.</summary>
        private Func<string, Vector2>? _plantTrackOffset;
        /// <summary>The visible native zombie costume.</summary>
        private ZombieVariant _variant;
        /// <summary>Whether the zombie uses an attached plant head.</summary>
        private bool _plantHead;

        /// <summary>Creates reusable callbacks for one synchronous presenter.</summary>
        /// <param name="renderer">The animation renderer owned by the game session.</param>
        internal AdventureActorRenderer(ReanimationRenderer? renderer)
        {
            _renderer = renderer;
            _drawPlant = DrawPlantPose;
            _drawZombie = DrawZombiePose;
            _drawCursor = DrawCursorPose;
            _plantVisible = IsPlantTrackVisible;
            _zombieVisible = IsZombieTrackVisible;
            _poolTrackAttachment = DrawPoolTrackAttachment;
        }

        /// <summary>Draws a plant and its optional hit, imitation, or producer feedback.</summary>
        /// <param name="plant">The plant supplying live presentation state.</param>
        /// <param name="name">The native animation asset.</param>
        /// <param name="origin">The native placement origin.</param>
        /// <param name="scale">The native scale.</param>
        /// <param name="seconds">The selected playback time.</param>
        /// <param name="clip">The selected clip.</param>
        /// <param name="loop">Whether playback loops.</param>
        /// <param name="rate">The native playback rate.</param>
        /// <param name="heads">The immediately consumed attached head poses.</param>
        /// <param name="key">The stable native playback identity.</param>
        /// <param name="viewport">The bounds of optional feedback composition.</param>
        /// <param name="blendTicks">The clip transition blend duration.</param>
        /// <param name="remainder">The render interpolation remainder.</param>
        /// <param name="surfaceKey">The stable feedback surface identity.</param>
        /// <param name="highlighted">Whether the held shovel highlights this plant.</param>
        /// <param name="trackOffset">Optional native per-track shake offsets for this plant's body animation.</param>
        internal void DrawPlant(Plant plant, string name, Vector2 origin, float scale, float seconds, string clip,
            bool loop, float rate, HeadPlayback[]? heads, string key, Rect viewport, int blendTicks, float remainder, string surfaceKey, bool highlighted = false, Func<string, Vector2>? trackOffset = null)
        {
            _plant = plant;
            _name = name;
            _origin = origin;
            _scale = scale;
            _seconds = seconds;
            _clip = clip;
            _loop = loop;
            _rate = rate;
            _heads = heads;
            _plantTrackOffset = trackOffset;
            _key = key;
            _blendTicks = blendTicks;
            _remainder = remainder;
            _surfaceKey = surfaceKey;
            _highlighted = highlighted;
            if (highlighted || AdventureProducerGlow.Visual(plant).Enabled || plant.IsImitated || plant.PresentationEatenFlashTicks > 0)
            {
                RequiredRenderer.DrawNativePlantHitFeedback(key, viewport, _drawPlant, plant, _surfaceKey, _highlighted);
            }
            else
            {
                DrawPlantPose();
            }
        }

        /// <summary>Draws a stationary cursor pose using a retained imitation callback.</summary>
        /// <param name="name">The native asset name.</param>
        /// <param name="origin">The native placement origin.</param>
        /// <param name="scale">The native scale.</param>
        /// <param name="clip">The optional selected animation clip.</param>
        /// <param name="kernel">Whether to hide the kernel catapult's butter layer.</param>
        /// <param name="imitated">Whether to compose the grayscale imitation artwork.</param>
        /// <param name="key">The cursor's persistent surface key.</param>
        /// <param name="viewport">The local composition viewport.</param>
        internal void DrawCursor(string name, Vector2 origin, float scale, string? clip, bool kernel, bool imitated, string key, Rect viewport)
        {
            _name = name;
            _origin = origin;
            _scale = scale;
            _clip = clip!;
            _cursorKernel = kernel;
            if (imitated)
            {
                RequiredRenderer.DrawNativeClipped(key, viewport, _drawCursor, false, true);
            }
            else
            {
                DrawCursorPose();
            }
        }

        /// <summary>Submits the configured stationary cursor pose.</summary>
        /// <returns>Whether native cursor artwork was submitted.</returns>
        private bool DrawCursorPose()
        {
            return RequiredRenderer.DrawNative(_name, _origin, _scale, 0f, _clip, _cursorKernel ? s_cursorKernelVisible : null);
        }

        /// <summary>Draws a zombie using cached body and visibility callbacks.</summary>
        /// <param name="zombie">The zombie supplying live costume and combat state.</param>
        /// <param name="name">The native animation asset.</param>
        /// <param name="clip">The selected clip.</param>
        /// <param name="origin">The native placement origin.</param>
        /// <param name="scale">The native scale.</param>
        /// <param name="seconds">The selected playback time.</param>
        /// <param name="rate">The native playback rate.</param>
        /// <param name="loop">Whether playback loops.</param>
        /// <param name="key">The stable native playback identity.</param>
        /// <param name="surfaceKey">The stable combat surface identity.</param>
        /// <param name="poolParticles">The original particle systems owning the pool-emergence attachments.</param>
        /// <param name="poolHeadKey">The cached identity of the original head or helmet seaweed.</param>
        /// <param name="poolInnerKey">The cached identity of the original upper-arm seaweed.</param>
        /// <param name="poolOuterKey">The cached identity of the original tube seaweed.</param>
        /// <param name="remainder">The nonnegative render remainder added to attachment ages.</param>
        internal void DrawZombie(Zombie zombie, string name, string? clip, Vector2 origin, float scale, float seconds,
            float rate, bool loop, string key, string surfaceKey, OriginalParticleRenderer? poolParticles = null,
            string? poolHeadKey = null, string? poolInnerKey = null, string? poolOuterKey = null, float remainder = 0f)
        {
            _zombie = zombie;
            _name = name;
            _clip = clip;
            _origin = origin;
            _scale = scale;
            _seconds = seconds;
            _rate = rate;
            _loop = loop;
            _key = key;
            _poolParticles = poolParticles;
            _poolHeadKey = poolHeadKey;
            _poolInnerKey = poolInnerKey;
            _poolOuterKey = poolOuterKey;
            _remainder = remainder;
            _variant = zombie.Kind == ZombieKind.Flag && !zombie.Headless ? ZombieVariant.Flag
                : zombie.Kind == ZombieKind.Conehead && zombie.ArmorHealth > 0 ? ZombieVariant.Conehead
                : zombie.Kind == ZombieKind.Buckethead && zombie.ArmorHealth > 0 ? ZombieVariant.Buckethead
                : zombie.Kind == ZombieKind.ScreenDoor && zombie.ShieldHealth > 0 ? ZombieVariant.ScreenDoor : ZombieVariant.Normal;
            _plantHead = zombie.ModeHead >= 26 && zombie.ModeHead <= 31;
            _backwards = zombie.PresentationWalkingBackwards ?? (zombie.Hypnotized || zombie.Kind == ZombieKind.Digger && AdventureDiggerState.WalksBackwards(zombie));
            if (AdventureDancerRules.IsDancer(zombie))
            {
                // PC UpdateReanim 0x547FF0 mirrors only the moonwalk, then applies mind control.
                _backwards = AdventureDancerRules.FacesRight(zombie);
            }

            var viewport = new Rect(origin.x - 160, origin.y - 160, 500, 460);
            if (zombie.Kind == ZombieKind.DolphinRider && RequiredRenderer.TryGetNativeDefinitionInfo(name, out var dolphin))
            {
                // The authored jump extends left of the generic composition bounds. Bounds are a capture
                // allocation limit, not PC DrawReanim's independent water rectangle or its ignore-clip tracks.
                var left = _backwards ? 90f - dolphin.Bounds.xMax : dolphin.Bounds.xMin;
                var right = _backwards ? 90f - dolphin.Bounds.xMin : dolphin.Bounds.xMax;
                viewport.xMin = Math.Min(viewport.xMin, origin.x + left * scale);
                viewport.xMax = Math.Max(viewport.xMax, origin.x + right * scale);
                viewport.yMin = Math.Min(viewport.yMin, origin.y + dolphin.Bounds.yMin * scale);
                viewport.yMax = Math.Max(viewport.yMax, origin.y + dolphin.Bounds.yMax * scale);
            }
            float? bodyClipBottom = null;
            Rect? bodyClipBounds = null;
            var pool = zombie.PoolState;
            if (pool != null && AdventureZombiePoolRules.UsesPlainBody(zombie.Kind)
                && (zombie.PresentationNativePhase == 14 || zombie.PresentationInPool == true))
            {
                // Keep the complete viewport: PC applies the water line per track, preserving ignore-clip exceptions.
                var nativeY = origin.y + pool.Altitude - 120f * (1f - scale) + 8f;
                bodyClipBottom = nativeY + (zombie.PresentationNativePhase == 14
                    ? 113f : 120f - 10f * (1f - scale) - (zombie.Eating ? 7f : 0f));
            }
            else if (zombie.GraveRiseTicks > 0)
            {
                var nativeY = origin.y + AdventureZombieRiseRules.Altitude(zombie.GraveRiseTicks)
                    - 120f * (1f - scale) + 8f;
                viewport.yMax = Math.Min(viewport.yMax, AdventureZombieRiseRules.ClipBottom(nativeY, zombie.GraveRiseTicks));
            }
            else if (zombie.PresentationNativePhase == 50 && zombie.DancerState != null)
            {
                // PC 0x549C33/0x548D40 cancels rise altitude from the fixed ground clipping edge.
                var nativeY = origin.y + AdventureDancerRules.RiseAltitude(zombie.DancerState.PhaseTicks)
                    - 120f * (1f - scale) + 8f;
                viewport.yMax = Math.Min(viewport.yMax, nativeY + 113f);
            }

            if (pool == null)
            {
                var nativeY = origin.y + zombie.PresentationAltitude - 120f * (1f - scale) + 8f
                    - AdventureZombieMotionRules.DolphinImageOffsetY(zombie);
                var waterline = AdventureZombieMotionRules.ClipBottom(zombie, nativeY, scale);
                if (!float.IsPositiveInfinity(waterline))
                {
                    bodyClipBottom = waterline;
                    if (zombie.Kind == ZombieKind.DolphinRider)
                    {
                        // PC DrawReanim 548D40 clips ordinary tracks at imageOffsetX-200, width520,
                        // and imageOffsetY+bodyY-78. Water tracks retain their independent ignore-clip flag.
                        var left = origin.x - 15f - 30f * (1f - scale) - 200f;
                        var top = nativeY + AdventureZombieMotionRules.DolphinImageOffsetY(zombie)
                            - zombie.PresentationAltitude - 78f;
                        bodyClipBounds = Rect.MinMaxRect(left, top, left + 520f, waterline);
                    }
                }
            }

            RequiredRenderer.DrawNativeZombieCombat(zombie, viewport, _drawZombie, surfaceKey, bodyClipBottom, bodyClipBounds);
        }

        /// <summary>Submits the configured plant without allocating callback captures.</summary>
        /// <returns>Whether native plant artwork was submitted.</returns>
        private bool DrawPlantPose()
        {
            var visible = _plant.Kind == PlantKind.KernelPult || _plant.Kind == PlantKind.PotatoMine ? _plantVisible : null;
            var drawn = _heads != null
                ? RequiredRenderer.DrawNativeAttachedHeads(_name, _origin, _scale, _seconds, _rate, _heads, visible, _plant.ModeFacesLeft, _key, _blendTicks)
                : RequiredRenderer.DrawNativePlayback(_name, _origin, _scale, _seconds, _clip, _loop, _rate, visible,
                    AdventureDefensivePlantRules.DamageTrack(_plant.Kind), AdventureDefensivePlantRules.DamageImage(_plant.Kind, _plant.Health),
                    _plant.ModeFacesLeft, _key, _blendTicks, trackOffset: _plantTrackOffset);
            var mine = _plant.PotatoMinePresentation;
            if (_plant.Kind == PlantKind.PotatoMine && mine != null && mine.LightCreated && _plant.MinePhase == MinePhase.Armed)
            {
                var phase = mine.GlowTime + _remainder * (mine.BodyRate - 2f) / mine.GlowFrames;
                drawn |= RequiredRenderer.DrawNativePotatoMineLight(_origin, _scale, _seconds, _rate, phase, mine.GlowFrames);
            }

            return drawn;
        }

        /// <summary>Tests the current kernel or butter animation layer.</summary>
        /// <param name="track">The native track name.</param>
        /// <returns>Whether the current plant should display the track.</returns>
        private bool IsPlantTrackVisible(string track)
        {
            if (_plant.Kind == PlantKind.PotatoMine)
            {
                return !string.Equals(track, "anim_glow", StringComparison.OrdinalIgnoreCase);
            }

            return !string.Equals(track, _plant.PresentationButter ? "Cornpult_kernal" : "Cornpult_butter", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Tests costume, plant head, and missing head layers of the configured zombie.</summary>
        /// <param name="track">The native track name.</param>
        /// <returns>Whether the current zombie should display the track.</returns>
        private bool IsZombieTrackVisible(string track)
        {
            if (_plantHead && (track.IndexOf("head", StringComparison.OrdinalIgnoreCase) >= 0
                || track.IndexOf("hair", StringComparison.OrdinalIgnoreCase) >= 0
                || _zombie.ModeHead != 26 && _zombie.ModeHead != 29 && _zombie.ModeHead != 30 && string.Equals(track, "Zombie_tie", StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            if (_zombie.Headless && !AdventureZombiePartsRules.HeadTrackVisible(_zombie.ModeHead >= 26 ? _zombie.ModeHead : (int)_zombie.Kind, track))
            {
                return false;
            }

            if (string.Equals(_name, "Zombie", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(track, "Zombie_duckytube", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(track, "Zombie_whitewater", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(track, "Zombie_whitewater2", StringComparison.OrdinalIgnoreCase)))
            {
                // Phase fourteen reveals these enabled source tracks through the rising clip, never a visibility toggle.
                return _zombie.PoolState?.HasDuckyTube
                    ?? (_zombie.PresentationPoolRow || _zombie.PresentationInPool == true || _zombie.Kind == ZombieKind.DuckyTube);
            }

            return !string.Equals(_name, "Zombie", StringComparison.OrdinalIgnoreCase)
                || ReanimationRenderer.IsZombieTrackVisible(track, _variant);
        }

        /// <summary>Submits the configured zombie and any flag or plant head attachment.</summary>
        /// <returns>Whether native zombie artwork was submitted.</returns>
        private bool DrawZombiePose()
        {
            if (_zombie.Kind == ZombieKind.DolphinRider && _zombie.DolphinState != null
                && (_zombie.PresentationNativePhase == 55 || _zombie.PresentationNativePhase == 56))
            {
                // Native StartWalkAnim(0) runs before StartEating(20), even if no render falls between them.
                RequiredRenderer.EnsureNativePlaybackPose(_name, _key,
                    _zombie.PresentationNativePhase == 55 ? "anim_swim" : "anim_walk", _zombie.DolphinState.WalkingPoseVersion);
            }

            var painted = string.Equals(_name, "Zombie", StringComparison.OrdinalIgnoreCase) && !_zombie.Headless && !_plantHead
                && !_backwards && _zombie.Kind != ZombieKind.Flag && _zombie.Kind != ZombieKind.DuckyTube
                && _zombie.PoolState == null && _zombie.PresentationInPool != true && !_zombie.PresentationPoolRow
                ? RequiredRenderer.DrawZombiePlayback(_origin, _scale, _seconds, _variant, _clip, _loop, _rate, true, null, _key)
                : RequiredRenderer.DrawNativePlayback(_name, _origin, _scale, _seconds, _clip, _loop, _rate, _zombieVisible, null, null,
                    _backwards, _key, _zombie.PresentationBodyBlendTicks >= 0 ? _zombie.PresentationBodyBlendTicks
                        : _zombie.Kind == ZombieKind.Balloon ? _zombie.PresentationBalloonBlendTicks
                        : _zombie.Kind == ZombieKind.Bobsled && _zombie.BobsledState != null ? 0
                        : _zombie.Kind == ZombieKind.Pogo ? _zombie.PogoState?.AnimationBlendTicks ?? 20
                        : _zombie.Kind == ZombieKind.Digger ? _zombie.DiggerState?.AnimationBlendTicks ?? 20
                        : _zombie.Kind == ZombieKind.DolphinRider ? AdventureZombieMotionRules.DolphinBlendTicks(_zombie)
                        : _clip == "anim_gasp" || _zombie.ZomboniState?.FlatTires == true ? 10
                            : _zombie.Kind == ZombieKind.Snorkel && _zombie.PresentationNativePhase != 58
                                ? 0 : _zombie.Kind == ZombieKind.PoleVaulter ? _zombie.PresentationPoleVaultBlendTicks
                                    : AdventureDancerRules.AnimationBlendTicks(_zombie), -1f, 90f,
                    trackAttachment: _poolParticles != null && (_zombie.PoolState?.SeaweedAge >= 0f
                        || _zombie.ZomboniState?.FlatTires == true) ? _poolTrackAttachment : null,
                    fullLastFrame: _zombie.Kind == ZombieKind.Snorkel && _zombie.PresentationNativePhase == 59
                        || _zombie.Kind == ZombieKind.DolphinRider && _zombie.PresentationNativePhase == 53);
            if (_plantHead && !_zombie.Headless)
            {
                RequiredRenderer.DrawNativePlantHeadAttachment(_origin, _scale, _clip, _seconds, _rate, _zombie.ModeHead, _zombie.PresentationPlantHeadAge, _backwards);
            }

            return painted;
        }

        /// <summary>Submits PC pool-emergence seaweed immediately after its original visible body track.</summary>
        /// <param name="track">The sampled original reanimation track name.</param>
        /// <param name="placement">The native body placement multiplied by this track's blended transform.</param>
        /// <param name="boardTransform">The outer board-to-target transform before native placement.</param>
        /// <param name="alpha">The sampled attachment track opacity.</param>
        private void DrawPoolTrackAttachment(string track, Matrix4x4 placement, Matrix4x4 boardTransform, float alpha)
        {
            var particles = _poolParticles;
            if (particles != null && _zombie.ZomboniState?.FlatTires == true && !_zombie.ZomboniState.Wheelie2
                && _poolHeadKey != null && string.Equals(track, "Zombie_zamboni_1", StringComparison.OrdinalIgnoreCase))
            {
                // PC 0x54A899 attaches the original smoke 35,85 pixels into the animated hood track.
                var smokePoint = placement.MultiplyPoint3x4(new Vector3(35f, 85f, 0f));
                RequiredRenderer.DrawNativePoolParticleAttachment(particles, _poolHeadKey,
                    (280 - _zombie.ZomboniState.DeathTicks) * .01f + _remainder,
                    new Vector2(smokePoint.x, smokePoint.y), _zombie.Id, 1f, boardTransform, alpha, "ZamboniSmoke");
                return;
            }

            var pool = _zombie.PoolState;
            if (particles == null || pool == null || pool.SeaweedAge < 0f)
            {
                return;
            }

            string? key;
            Vector3 offset;
            var seedOffset = 0;
            var head = _zombie.Kind == ZombieKind.Conehead ? "anim_cone"
                : _zombie.Kind == ZombieKind.Buckethead ? "anim_bucket" : "anim_head1";
            if (string.Equals(track, head, StringComparison.OrdinalIgnoreCase))
            {
                key = _poolHeadKey;
                offset = new Vector3(_zombie.Kind == ZombieKind.Conehead || _zombie.Kind == ZombieKind.Buckethead ? 37f : 30f, 20f, 0f);
            }
            else if (string.Equals(track, "Zombie_outerarm_upper", StringComparison.OrdinalIgnoreCase))
            {
                key = _poolInnerKey;
                offset = new Vector3(5f, 5f, 0f);
                seedOffset = 1;
            }
            else if (string.Equals(track, "Zombie_duckytube", StringComparison.OrdinalIgnoreCase))
            {
                key = _poolOuterKey;
                offset = new Vector3(77f, 20f, 0f);
                seedOffset = 2;
            }
            else
            {
                return;
            }

            if (key == null)
            {
                return;
            }

            // PC Attachment::SetMatrix moves particles to the transformed point without inheriting track rotation.
            var point = placement.MultiplyPoint3x4(offset);
            RequiredRenderer.DrawNativePoolParticleAttachment(particles, key, pool.SeaweedAge + _remainder,
                new Vector2(point.x, point.y), unchecked(_zombie.Id * 397 + seedOffset), _scale, boardTransform, alpha);
        }
    }
}
