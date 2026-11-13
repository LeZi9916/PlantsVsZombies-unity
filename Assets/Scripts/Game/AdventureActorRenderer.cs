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
        /// <summary>Identifies the unclipped native pool overlay pass.</summary>
        private bool _waterOverlay;

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
        /// <param name="waterSurfaceKey">The separate surface for native unclipped pool overlays.</param>
        internal void DrawZombie(Zombie zombie, string name, string? clip, Vector2 origin, float scale, float seconds,
            float rate, bool loop, string key, string surfaceKey, string? waterSurfaceKey = null)
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
            _variant = zombie.Kind == ZombieKind.Flag && !zombie.Headless ? ZombieVariant.Flag
                : zombie.Kind == ZombieKind.Conehead && zombie.ArmorHealth > 0 ? ZombieVariant.Conehead
                : zombie.Kind == ZombieKind.Buckethead && zombie.ArmorHealth > 0 ? ZombieVariant.Buckethead
                : zombie.Kind == ZombieKind.ScreenDoor && zombie.ShieldHealth > 0 ? ZombieVariant.ScreenDoor : ZombieVariant.Normal;
            _plantHead = zombie.ModeHead >= 26 && zombie.ModeHead <= 31;
            _backwards = zombie.PresentationWalkingBackwards ?? (zombie.Hypnotized || zombie.Kind == ZombieKind.Digger && zombie.AbilityPhase == 2);
            if (AdventureDancerRules.IsDancer(zombie))
            {
                // PC UpdateReanim 0x547FF0 mirrors only the moonwalk, then applies mind control.
                _backwards = AdventureDancerRules.FacesRight(zombie);
            }

            var viewport = new Rect(origin.x - 160, origin.y - 160, 500, 460);
            if (zombie.GraveRiseTicks > 0)
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

            // PC GetDrawPos and DrawReanim use the waterline of the logical body, before the -8 overlay.
            var waterNativeY = origin.y + zombie.PresentationAltitude - 120f * (1f - scale) + 8f;
            viewport.yMax = Math.Min(viewport.yMax, AdventureZombieMotionRules.ClipBottom(zombie, waterNativeY, scale));
            _waterOverlay = false;
            RequiredRenderer.DrawNativeZombieCombat(zombie, viewport, _drawZombie, surfaceKey);
            if (zombie.PresentationInPool == true || zombie.PresentationPoolRow || zombie.Kind == ZombieKind.DuckyTube
                || zombie.Kind == ZombieKind.Snorkel && zombie.PresentationNativePhase == 58)
            {
                // Original duck tube, whitewater, and hands ignore the live pool clipping rectangle.
                _waterOverlay = true;
                RequiredRenderer.DrawNativeZombieCombat(zombie, new Rect(origin.x - 160, origin.y - 160, 500, 460), _drawZombie, waterSurfaceKey ?? key);
                _waterOverlay = false;
            }
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

            var water = _zombie.PresentationInPool == true || _zombie.PresentationPoolRow || _zombie.Kind == ZombieKind.DuckyTube;
            var ignoresWaterClip = IsWaterOverlayTrack(track, water);
            if (_waterOverlay != ignoresWaterClip)
            {
                return false;
            }

            return !string.Equals(_name, "Zombie", StringComparison.OrdinalIgnoreCase)
                || water && (string.Equals(track, "Zombie_duckytube", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(track, "Zombie_whitewater", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(track, "Zombie_whitewater2", StringComparison.OrdinalIgnoreCase))
                || ReanimationRenderer.IsZombieTrackVisible(track, _variant);
        }

        /// <summary>Checks the native SetupWaterTrack and ReanimIgnoreClipRect exemptions.</summary>
        /// <param name="track">The authored track name.</param>
        /// <param name="water">Whether a plain zombie wears its pool-row costume.</param>
        /// <returns>Whether this track belongs to the unclipped water overlay pass.</returns>
        private bool IsWaterOverlayTrack(string track, bool water)
        {
            if (_zombie.Kind == ZombieKind.Snorkel)
            {
                return track.IndexOf("whitewater", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            return water && string.Equals(_name, "Zombie", StringComparison.OrdinalIgnoreCase)
                && (string.Equals(track, "Zombie_duckytube", StringComparison.OrdinalIgnoreCase) && _zombie.PresentationNativePhase != 1
                    || string.Equals(track, "Zombie_whitewater", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(track, "Zombie_whitewater2", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(track, "Zombie_outerarm_hand", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(track, "Zombie_innerarm3", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Submits the configured zombie and any flag or plant head attachment.</summary>
        /// <returns>Whether native zombie artwork was submitted.</returns>
        private bool DrawZombiePose()
        {
            var painted = string.Equals(_name, "Zombie", StringComparison.OrdinalIgnoreCase) && !_zombie.Headless && !_plantHead
                && !_backwards && _zombie.PresentationInPool != true && !_zombie.PresentationPoolRow && _zombie.Kind != ZombieKind.Flag && _zombie.Kind != ZombieKind.DuckyTube
                ? RequiredRenderer.DrawZombiePlayback(_origin, _scale, _seconds, _variant, _clip, _loop, _rate, true, null, _key)
                : RequiredRenderer.DrawNativePlayback(_name, _origin, _scale, _seconds, _clip, _loop, _rate, _zombieVisible, null, null,
                    _backwards, _key, _clip == "anim_gasp" ? 10 : _zombie.Kind == ZombieKind.Snorkel && _zombie.PresentationNativePhase != 58
                        ? 0 : AdventureDancerRules.AnimationBlendTicks(_zombie), -1f, 90f,
                    fullLastFrame: _zombie.Kind == ZombieKind.Snorkel && _zombie.PresentationNativePhase == 59);
            if (_plantHead && !_zombie.Headless && !_waterOverlay)
            {
                RequiredRenderer.DrawNativePlantHeadAttachment(_origin, _scale, _clip, _seconds, _rate, _zombie.ModeHead, _zombie.PresentationPlantHeadAge, _backwards);
            }

            return painted;
        }
    }
}
