using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PvZReplica.Adventure;
using PvZReplica.Reanimation;
using PvZReplica.VerifiedData;
using PvZReplica.UI;

namespace PvZReplica
{
    public sealed partial class PvZGame
    {
        private const float AdventureGridX = 40f;
        private const float AdventureGridY = 80f;
        private readonly Dictionary<string, bool> adventureClips = new Dictionary<string, bool>();

        private float AdventurePitch { get { return game.Adventure.Level.HasPool || game.Adventure.Level.HasRoof ? 85f : 100f; } }
        private float AdventureSlope(float column)
        {
            return game.Adventure.Level.HasRoof ? Mathf.Max(0f, 5f - column) * 20f - 10f : 0f;
        }
        private float AdventureCellY(int row, float column) { return AdventureGridY + row * AdventurePitch + AdventureSlope(column); }
        private Rect AdventureCoinRect(AdventureCoin coin)
        {
            return new Rect(AdventureGridX + coin.X * CellWidth - 20,
                AdventureCellY((int)coin.Row, coin.X) + 36, 40, 40);
        }
        private Rect AdventureSunRect(Sun sun)
        {
            if (sun.PresentationNative)
            {
                var pickup = sun.PresentationPickup;
                if (pickup.Dead || pickup.Collecting) return new Rect(0, 0, 0, 0);
                float margin = pickup.Kind == PvZUnity.Sun.SunKind.Normal || sun.PresentationSpecialExpansion ? 15f : 0f;
                return new Rect(pickup.X - margin, pickup.Y - margin, 60 + margin * 2, 60 + margin * 2 + (sun.PresentationSpecialExpansion ? 30 : 0));
            }
            float fallbackMargin = sun.Value == 25 ? 15f : 0f;
            return new Rect(AdventureGridX + sun.X * CellWidth - 30f - fallbackMargin,
                AdventureGridY + sun.Y * AdventurePitch - 30f - fallbackMargin, 60 + fallbackMargin * 2, 60 + fallbackMargin * 2);
        }

        private bool AdventureClipExists(string name, string clip)
        {
            string key = name + ":" + clip;
            bool exists;
            if (!adventureClips.TryGetValue(key, out exists))
            {
                ReanimationRenderer.AnimationInfo info;
                exists = animations.TryGetAnimationInfo(name, clip, out info);
                adventureClips.Add(key, exists);
            }
            return exists;
        }
        private static string AdventurePlantAnimation(PlantKind kind)
        {
            int id = (int)kind;
            if (kind == PlantKind.ExplodeONut) return "Wallnut";
            if (id < 0 || id >= VerifiedDataCatalog.Plants.Count) return "PeaShooterSingle";
            ReanimationDefinition definition;
            return VerifiedDataCatalog.TryGetReanimation(VerifiedDataCatalog.Plants[id].ReanimationId, out definition)
                ? Path.GetFileNameWithoutExtension(definition.Path.Replace('\\', '/')) : "PeaShooterSingle";
        }
        private static string AdventureZombieAnimation(ZombieKind kind)
        {
            ReanimationDefinition definition;
            return VerifiedDataCatalog.TryGetReanimation(VerifiedDataCatalog.Zombies[(int)kind].ReanimationId, out definition)
                ? Path.GetFileNameWithoutExtension(definition.Path.Replace('\\', '/')) : "Zombie";
        }

        private void DrawAdventureBoard()
        {
            BeginAdventurePresentation();
            Matrix4x4 openingMatrix = GUI.matrix;
            if (AdventureIntroActive) GUI.matrix = openingMatrix * Matrix4x4.Translate(new Vector3(adventureIntroFrame.BoardX, adventureIntroFrame.BoardY, 0));
            try
            {
            var level = game.Adventure.Level;
            string background = level.Terrain == AdventureTerrain.Night ? "background2.jpg" :
                level.Terrain == AdventureTerrain.Pool ? "background3.jpg" :
                level.Terrain == AdventureTerrain.Fog ? "background4.jpg" :
                level.Terrain == AdventureTerrain.Roof ? "background5.jpg" :
                level.Terrain == AdventureTerrain.BossRoof ? "background6boss.jpg" : "background1.jpg";
            if (adventureIntroFrame != null && !level.IsReplay && level.Number <= 4)
            {
                DrawImage("background1unsodded.jpg", new Rect(-220, 0, 1400, 600));
                DrawAdventureOpeningWorld();
            }
            else if (level.Number <= 3 && !level.IsReplay)
            {
                DrawImage("background1unsodded.jpg", new Rect(-220, 0, 1400, 600));
                if (level.Number == 1) DrawImage("sod1row.png", new Rect(19, 265, 771, 127));
                else DrawImage("sod3row.png", new Rect(15, 149, 771, 355));
            }
            else DrawImage(background, new Rect(-220, 0, 1400, 600));
            if (level.HasPool) DrawImage(level.IsNight ? "pool_night.jpg" : "pool.jpg", new Rect(30, 279, 720, 159));
            foreach (var crater in game.Adventure.Craters)
                DrawImage(level.HasRoof ? "crater_roof_center.png" : level.HasPool && level.IsWaterRow(crater.Row) ?
                    level.IsNight ? "crater_water_night.png" : "crater_water_day.png" : "crater.png",
                    new Rect(AdventureGridX + crater.Column * CellWidth - 24, AdventureCellY(crater.Row, crater.Column) + 45, 128, 44));
            foreach (var ice in game.Adventure.IceTrails)
                DrawImage("ice.png", new Rect(AdventureGridX + ice.Column * CellWidth, AdventureCellY(ice.Row, ice.Column) + 44, 84, 42));
            foreach (var grave in game.Adventure.Graves)
            {
                Texture2D image = Texture("Tombstones.png");
                if (image != null) GUI.DrawTextureWithTexCoords(new Rect(AdventureGridX + grave.Column * CellWidth,
                    AdventureCellY(grave.Row, grave.Column), 80, 91), image, new Rect(0, 0.75f, 0.2f, 0.25f));
            }
            if (level.Mode == AdventureMode.Bowling)
                Fill(new Rect(278, 90, 4, 475), new Color(0.9f, 0.08f, 0.04f));
            if (level.Mode == AdventureMode.Boss)
                animations.DrawBossNative(new Vector2(0, 0), 1f, game.Elapsed, game.Adventure.BossHeadVulnerable);
            for (int row = 0; row < game.RowCount; row++)
            {
                if (game.Adventure.RakeRow == row)
                    animations.DrawNativePlayback("Rake", new Vector2(AdventureGridX + game.Adventure.RakeColumn * CellWidth + 20,
                        AdventureCellY(row, game.Adventure.RakeColumn)), 1f,
                        game.Adventure.RakeTriggered ? game.Adventure.RakeAge + AdventureRenderRemainder : 0f, null, false, 20f);
                foreach (Mower mower in game.Mowers)
                {
                    if (mower.Row != row || !mower.Available && !mower.Active) continue;
                    if (!AdventureIntroActive || adventureIntroFrame.MowerX[mower.Row] > -80) DrawAdventureMower(mower);
                }
                for (int layer = 0; layer <= (int)PlantLayer.Effect; layer++)
                    foreach (Plant plant in game.Plants)
                        if (plant.Row == row && (int)plant.Layer == layer) DrawAdventurePlant(plant);
                DrawAdventurePlantPreview(row);
                foreach (var vase in game.Adventure.Vases)
                {
                    if (vase.Row != row) continue;
                    Texture2D image = Texture("Scary_Pot.png");
                    if (image != null) GUI.DrawTextureWithTexCoords(new Rect(AdventureGridX + vase.Column * CellWidth,
                        AdventureCellY(row, vase.Column) - 6, 80, 101), image,
                        new Rect(vase.ShowsPlant ? 1f / 3f : 0, 0, 1f / 3f, 0.5f));
                }
                DrawAdventureDetachedZombieParts(row, true);
                DrawAdventureCorpses(row);
                foreach (Zombie zombie in game.Zombies) if (zombie.Row == row) DrawAdventureZombie(zombie);
                DrawAdventureDetachedArms(row);
                DrawAdventureDetachedZombieParts(row, false);
                DrawAdventurePlantBiteEffects(row);
                DrawAdventureImpacts(row);
                DrawAdventureExplosionRow(row);
                foreach (Pea pea in game.Peas)
                {
                    if (pea.Row != row) continue;
                    if (pea.FlightRow < 0 || pea.FlightRow >= game.RowCount) continue;
                    DrawAdventureProjectile(pea);
                }
                DrawAdventureSnowTrails(row);
                foreach (var nut in game.Adventure.BowlingNuts)
                {
                    if (nut.Row != row) continue;
                    Color previous = GUI.color;
                    if (nut.Explosive) GUI.color = new Color(1f, 64f / 255f, 64f / 255f);
                    bool giant = game.ModeSession != null && game.ModeSession.GiantNuts.Contains(nut);
                    AdventureBowlingPresentation pose; game.Presentation.Bowling.TryGetValue(nut, out pose);
                    animations.DrawNativePlayback("Wallnut", new Vector2((nut.NativeMotionInitialized?nut.NativePixelX:AdventureGridX+nut.X*CellWidth-40)+(giant?-76:0),
                        (nut.NativeMotionInitialized?nut.NativePixelY:AdventureCellY(row,nut.X))+(giant?-64:0)), giant ? 2f : 1f,
                        (pose == null ? game.Elapsed : pose.Age) + AdventureRenderRemainder, "_ground", true, pose == null ? 12 : pose.Rate);
                    GUI.color = previous;
                }
                foreach (var ball in game.Adventure.BossBalls)
                    if (ball.Row == row) animations.Draw(ball.IsIce ? "Zombie_boss_iceball" : "Zombie_boss_fireball",
                        new Rect(AdventureGridX + ball.X * CellWidth - 40, AdventureCellY(row, ball.X) - 2, 80, 80), game.Elapsed, "anim_role");
                foreach (LaneFlame flame in game.Flames)
                    if (flame.Row == row) for (int c = 0; c < 10; c++) animations.Draw("fire",
                        new Rect(AdventureGridX + c * 76, AdventureCellY(row, c) - 10, 84, 108), flame.Age + c * 0.025f, "anim_flame");
            }
            DrawAdventureExplosionOverlay();
            foreach (Sun sun in game.Suns) if (!sun.PresentationNative || !sun.PresentationPickup.Collecting) DrawAdventureSun(sun);
            foreach (var coin in game.Adventure.Coins)
            {
                Rect coinRect = AdventureCoinRect(coin);
                if (coin.Kind == AdventureCoinKind.PlantGift || coin.Kind == AdventureCoinKind.Chocolate ||
                    coin.Kind == AdventureCoinKind.MinigameUnlock || coin.Kind == AdventureCoinKind.PuzzleUnlock)
                {
                    string image = coin.Kind == AdventureCoinKind.Chocolate ? "chocolate.png" : "Present.png";
                    Texture2D gift = Texture(image);
                    if (gift != null) DrawImage(image, new Rect(coinRect.center.x - gift.width * .5f, coinRect.center.y - gift.height * .5f, gift.width, gift.height));
                    continue;
                }
                animations.DrawNative(coin.Kind == AdventureCoinKind.Diamond ? "Diamond" :
                    coin.Kind == AdventureCoinKind.Gold ? "Coin_gold" : "Coin_silver", new Vector2(coinRect.x, coinRect.y), 1f, coin.Age);
            }
            foreach (var shell in game.Adventure.CobShells)
            {
                float ticks = Math.Max(0, GameSimulation.AdventureCobFlightSeconds - shell.FlightRemaining + AdventureRenderRemainder) * 100f;
                bool rising = ticks < 88f;
                float x = rising ? shell.LaunchX : shell.TargetX;
                float row = rising ? shell.LaunchRow : shell.TargetRow;
                float z = rising ? -8f * ticks : -704f + 8f * (ticks - 88f);
                animations.DrawResourceSprite("IMAGE_REANIM_COBCANNON_COB", new Vector2(AdventureGridX + x * CellWidth,
                    AdventureGridY + row * AdventurePitch + AdventureSlope(x) + z), 1f, rising ? 90 : -90);
            }
            if (level.Terrain == AdventureTerrain.Fog && game.Adventure.FogBlownRemaining <= 0)
            {
                for (int row = 0; row < game.RowCount; row++) for (int col = 0; col < 9; col++)
                    if (!game.IsAdventureCellVisible(row, col))
                    {
                        Texture2D fog = Texture("fog.png");
                        if (fog != null) GUI.DrawTextureWithTexCoords(
                            new Rect(AdventureGridX + col * CellWidth - 55, AdventureCellY(row, col) - 35, 190, 170),
                            fog, new Rect((col % 8) / 8f, 0, 1f / 8f, 1));
                    }
            }
            if (level.Mode == AdventureMode.Storm && !game.Adventure.StormFlash)
                Fill(new Rect(0, 80, 800, 520), new Color(0.01f, 0.02f, 0.08f, 0.72f));
            DrawAdventureOpeningActors();
            }
            finally { GUI.matrix = openingMatrix; EndAdventurePresentation(); }
        }

        private void DrawAdventureReanimImage(string name, Rect destination)
        { DrawAdventureFileImage("reanim/" + name, destination); }
        private void DrawAdventureFileImage(string relative, Rect destination)
        {
            Texture2D texture;
            if (!textures.TryGetValue(relative, out texture))
            {
                string file = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(file)) { textures[relative] = null; return; }
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!texture.LoadImage(File.ReadAllBytes(file))) { Destroy(texture); texture = null; }
                else { texture.filterMode = FilterMode.Bilinear; texture.wrapMode = TextureWrapMode.Clamp; }
                textures[relative] = texture;
            }
            if (texture != null) GUI.DrawTexture(destination, texture, ScaleMode.ScaleToFit, true);
        }
        private OriginalParticleRenderer adventureParticles;
        private GameSimulation adventurePresentedGame;
        private readonly HashSet<string> adventurePresentationSurfaces = new HashSet<string>();
        private readonly HashSet<string> adventurePresentationLiveSurfaces = new HashSet<string>();

        private void BeginAdventurePresentation()
        {
            if (!ReferenceEquals(adventurePresentedGame, game))
            {
                if (adventureParticles != null) adventureParticles.Clear();
                foreach (string key in adventurePresentationSurfaces) animations.ReleaseNativeSurface(key);
                adventurePresentationSurfaces.Clear(); animations.ResetNativePlayback(); adventurePresentedGame = game;
                adventureParticles = new OriginalParticleRenderer(root, animations);
            }
            UpdateAdventureZombiePartAudio();
            adventurePresentationLiveSurfaces.Clear(); adventureParticles.BeginFrame();
        }
        private void EndAdventurePresentation()
        {
            adventureParticles.EndFrame();
            var expired = new List<string>();
            foreach (string key in adventurePresentationSurfaces)
                if (!adventurePresentationLiveSurfaces.Contains(key)) expired.Add(key);
            foreach (string key in expired) { animations.ReleaseNativeSurface(key); adventurePresentationSurfaces.Remove(key); }
        }
        private bool DrawAdventureSurface(string key, Rect viewport, Func<bool> draw, bool grayscale = false, float additive = 0f)
        {
            adventurePresentationSurfaces.Add(key); adventurePresentationLiveSurfaces.Add(key);
            return animations.DrawNativeClipped(key, viewport, draw, false, grayscale, additive);
        }
        private float AdventureRenderRemainder { get { return game.AdventurePresentationRemainder; } }
        private float AdventureRenderX(Zombie zombie)
        {
            return zombie.PresentationInitialized ? Mathf.Lerp(zombie.PresentationPreviousX, zombie.X,
                AdventureRenderRemainder * 100f) : zombie.X;
        }
        private float AdventureFloorYAtPixel(int row, float x)
        {
            return AdventureGridY + row * AdventurePitch + (game.Adventure.Level.HasRoof ? Mathf.Max(0, 440f - x) * .25f - 10f : 0f);
        }
        private Vector2 AdventureZombieSourceOrigin(Zombie zombie, float x)
        {
            float size = zombie.IsSmall ? .5f : 1f;
            float pixelX = AdventureZombieNativeRules.ReanimationSourceX(x, size);
            float y = AdventureFloorYAtPixel(zombie.Row, pixelX + 40f) - 30f;
            if (zombie.Kind == ZombieKind.Balloon) y -= 30f;
            if (zombie.Kind == ZombieKind.Pogo && !zombie.UsedAbility) y -= 16f;
            if (zombie.DropRemaining > 0) y -= Mathf.Min(260f, zombie.DropRemaining * 120f);
            return new Vector2(pixelX, y);
        }
        private Vector2 AdventureZombieOrigin(Zombie zombie, float x)
        {
            float scale = zombie.IsSmall ? .5f : 1f;
            return AdventureZombieSourceOrigin(zombie, x) + new Vector2(20f * (1f - scale), 120f * (1f - scale));
        }
        private void DrawAdventureMower(Mower mower)
        {
            bool pool = game.Adventure.Level.IsWaterRow(mower.Row) && game.Rules.AdventurePoolCleaners;
            bool roof = game.Adventure.Level.HasRoof;
            string name = roof ? "RoofCleaner" : pool ? "PoolCleaner" : "LawnMower";
            string clip = pool ? mower.X > .125f ? "anim_water" : "anim_land" : roof ? null : "anim_normal";
            float age; game.Presentation.MowerTimes.TryGetValue(mower, out age);
            if (mower.Active) age += AdventureRenderRemainder; else age = 0;
            float x = AdventureIntroActive ? adventureIntroFrame.MowerX[mower.Row] : AdventureGridX + mower.X * CellWidth - 21f;
            // PC 0x465F60: row floor+23, attached reanim y=18, scale .85 (.8 for pool).
            // Draw translates x+6; ready roof/pool offsets are -40/-33 and active -4/-7.
            Vector2 origin = new Vector2(x + 6f, AdventureFloorYAtPixel(mower.Row, x + 40f) + 23f + (mower.Active ? 18f : 19f));
            if (pool) { origin.y -= mower.Active ? 7f : 33f; if (mower.Active) origin.x -= 10f; }
            if (roof) { origin.y -= mower.Active ? 4f : 40f; if (mower.Active) origin.x -= 10f; }
            animations.DrawNativePlayback(name, origin, pool ? .8f : .85f, age, clip, true,
                mower.Active ? pool ? 35f : 70f : 0f);
        }
        private float AdventurePlantHeight(Plant plant)
        {
            int id = (int)plant.Kind;
            float offset = id < 49 ? AlmanacPortraitLayout.PlantHeightOffset(id) : 0;
            if (game.Adventure.Level.HasRoof) offset -= 12f;
            if (plant.Kind != PlantKind.FlowerPot && game.Plants.Exists(p => p.Kind == PlantKind.FlowerPot && p.Row == plant.Row && p.Column == plant.Column))
            {
                offset -= plant.Kind == PlantKind.Chomper || plant.Kind == PlantKind.Plantern ? 10f : 5f;
                if (plant.Kind == PlantKind.ScaredyShroom) offset -= 3f;
                if (plant.Kind == PlantKind.PuffShroom || plant.Kind == PlantKind.SunShroom) offset -= 4f;
                if (plant.Kind == PlantKind.HypnoShroom || plant.Kind == PlantKind.MagnetShroom) offset -= 8f;
            }
            return offset;
        }
        private ReanimationRenderer.HeadPlayback AdventurePlantHead(Plant plant, string name, string idle, string shoot, string anchor, int head)
        {
            float rate = AdventurePresentationRules.ShootingRate(plant.Kind);
            float age = plant.PresentationHeadAge[head];
            ReanimationRenderer.NativeAnimationInfo info;
            bool firing = !plant.Asleep && age >= 0 && animations.TryGetNativeAnimationInfo(name, shoot, out info) && age < info.Count / rate;
            return new ReanimationRenderer.HeadPlayback { Anchor = anchor, Animation = firing ? shoot : idle,
                Seconds = firing ? age + AdventureRenderRemainder : plant.Age + AdventureRenderRemainder,
                Rate = firing ? rate : plant.PresentationRate > 0 ? plant.PresentationRate : 15f, Loop = !firing };
        }
        private void DrawAdventurePlant(Plant plant)
        {
            float x = AdventureGridX + plant.Column * CellWidth, y = AdventureCellY(plant.Row, plant.Column);
            string name = AdventurePlantAnimation(plant.Kind);
            float size = plant.IsSmall ? .5f : 1f;
            DrawImage("plantshadow.png", new Rect(x + 1, y + 65, 76 * plant.ColumnSpan, 24));
            Vector2 origin = new Vector2(x + (1f - size) * 40f, y + AdventurePlantHeight(plant) + (1f - size) * 80f);
            if (DrawAdventureCherryBomb(plant, origin, size)) return;
            if (DrawAdventurePotatoMine(plant, origin, size)) return;
            string clip = "anim_idle"; bool loop = true;
            float rate = plant.PresentationRate > 0 ? plant.PresentationRate : 12f;
            float age = plant.Age + AdventureRenderRemainder;
            if (plant.Kind == PlantKind.PotatoMine)
            {
                clip = plant.MinePhase == MinePhase.Buried ? "anim_idle" : plant.MinePhase == MinePhase.Rising ? "anim_rise" : "anim_armed";
                if (plant.MinePhase == MinePhase.Rising) { age = plant.PhaseAge + AdventureRenderRemainder; rate = 18f; loop = false; }
            }
            if (plant.Kind == PlantKind.SunShroom)
            {
                float awakeAge = plant.PresentationAwakeAge;
                if (awakeAge >= 120f && awakeAge < 121f) { clip = "anim_grow"; age = awakeAge - 120f + AdventureRenderRemainder; rate = 12f; loop = false; }
                else if (awakeAge >= 121f) clip = plant.Asleep ? "anim_bigsleep" : "anim_bigidle";
            }
            if (plant.Asleep && plant.Kind != PlantKind.SunShroom && AdventureClipExists(name, "anim_sleep")) clip = "anim_sleep";
            else if (plant.Asleep && plant.Kind == PlantKind.SunShroom && plant.PresentationAwakeAge < 120f) clip = "anim_sleep";
            if (plant.Kind == PlantKind.CobCannon)
            {
                clip = plant.CannonPhase == CobCannonPhase.Firing ? "anim_shooting" :
                    plant.CannonPhase == CobCannonPhase.Arming || plant.CannonPhase == CobCannonPhase.Reloading ? "anim_unarmed_idle" :
                    plant.CannonPhase == CobCannonPhase.Loading ? "anim_charge" : "anim_idle";
                loop = plant.CannonPhase != CobCannonPhase.Firing && plant.CannonPhase != CobCannonPhase.Loading;
                rate = 12f;
                if (!loop) age = Math.Max(0, (plant.CannonPhase == CobCannonPhase.Firing ? GameSimulation.AdventureCobFiringSeconds : GameSimulation.AdventureCobLoadingSeconds) - plant.CannonReloadRemaining) + AdventureRenderRemainder;
            }
            if (plant.Kind == PlantKind.Imitater && AdventureClipExists(name, "anim_morph")) { clip = "anim_morph"; loop = false; }
            string shooting = plant.Kind == PlantKind.Starfruit ? "anim_shoot" : "anim_shooting";
            ReanimationRenderer.NativeAnimationInfo shot;
            if (!plant.Asleep && !AdventurePresentationRules.AttachedHead(plant.Kind) && plant.Kind != PlantKind.CobCannon &&
                plant.PresentationShotAge >= 0 && animations.TryGetNativeAnimationInfo(name, shooting, out shot) &&
                plant.PresentationShotAge < shot.Count / AdventurePresentationRules.ShootingRate(plant.Kind))
            {
                clip = shooting; age = plant.PresentationShotAge + AdventureRenderRemainder;
                rate = AdventurePresentationRules.ShootingRate(plant.Kind); loop = false;
            }
            Func<string, bool> visible = plant.Kind == PlantKind.KernelPult ?
                (Func<string, bool>)(track => !string.Equals(track, plant.PresentationButter ? "Cornpult_kernal" : "Cornpult_butter", StringComparison.OrdinalIgnoreCase)) : null;
            Func<bool> draw;
            if (AdventurePresentationRules.AttachedHead(plant.Kind) && !plant.Asleep)
            {
                ReanimationRenderer.HeadPlayback[] heads;
                if (plant.Kind == PlantKind.Threepeater)
                {
                    heads = new[] { AdventurePlantHead(plant, name, "anim_head_idle1", "anim_shooting1", "anim_head1", 0),
                        AdventurePlantHead(plant, name, "anim_head_idle2", "anim_shooting2", "anim_head2", 1),
                        AdventurePlantHead(plant, name, "anim_head_idle3", "anim_shooting3", "anim_head3", 2) };
                }
                else if (plant.Kind == PlantKind.SplitPea)
                    heads = new[] { AdventurePlantHead(plant, name, "anim_head_idle", "anim_shooting", "anim_idle", 0),
                        AdventurePlantHead(plant, name, "anim_splitpea_idle", "anim_splitpea_shooting", "anim_idle", 1) };
                else heads = new[] { AdventurePlantHead(plant, name, "anim_head_idle", "anim_shooting", AdventureClipExists(name, "anim_stem") ? "anim_stem" : "anim_idle", 0) };
                draw = () => animations.DrawNativeAttachedHeads(name, origin, size, plant.Age + AdventureRenderRemainder, rate, heads, visible, plant.ModeFacesLeft, "plant-" + plant.Id, plant.Kind == PlantKind.Threepeater ? 10 : 20);
            }
            else draw = () => animations.DrawNativePlayback(name, origin, size, age, clip, loop, rate, visible, null, null, plant.ModeFacesLeft, "plant-" + plant.Id, plant.Kind == PlantKind.Cattail ? 10 : 20);
            var producerGlow = AdventureProducerGlow.Visual(plant);
            if (producerGlow.Enabled || plant.IsImitated || plant.PresentationEatenFlashTicks>0)
            {
                string surfaceKey = "producer-plant-" + plant.Id;
                adventurePresentationSurfaces.Add(surfaceKey); adventurePresentationLiveSurfaces.Add(surfaceKey);
                animations.DrawNativePlantHitFeedback("plant-" + plant.Id,new Rect(x-80,y-160,300,340),draw,plant);
            }
            else draw();
            if (plant.Kind == PlantKind.FumeShroom && plant.PresentationShotAge >= .35f && plant.PresentationShotAge < 1.35f)
                adventureParticles.Draw("fume-" + plant.Id, "FumeCloud", plant.PresentationShotAge - .35f, new Vector2(x + 85, y + 31), plant.Id);
            if (plant.Kind == PlantKind.GloomShroom)
            {
                float[] nativeBursts = { .64f, .92f, 1.20f, 1.48f };
                for (int burst = 0; burst < nativeBursts.Length; burst++)
                    if (plant.PresentationShotAge >= nativeBursts[burst] && plant.PresentationShotAge < nativeBursts[burst] + .6f)
                        adventureParticles.Draw("gloom-" + plant.Id + "-" + burst, "GloomCloud", plant.PresentationShotAge - nativeBursts[burst], new Vector2(x + 40, y + 40), plant.Id + burst);
            }
        }
        private string AdventureZombieClip(Zombie zombie, string name)
        {
            string clip;
            if (zombie.Kind == ZombieKind.Newspaper && zombie.ShieldHealth <= 0) clip = zombie.Eating ? "anim_eat_nopaper" : "anim_walk_nopaper";
            else if (zombie.Kind == ZombieKind.Ladder && zombie.ShieldHealth > 0) clip = zombie.Eating ? "anim_laddereat" : "anim_ladderwalk";
            else if (zombie.Eating) clip = "anim_eat";
            else if (zombie.Kind == ZombieKind.PoleVaulter && !zombie.UsedAbility) clip = "anim_run";
            else if (zombie.Kind == ZombieKind.Pogo && !zombie.UsedAbility) clip = "anim_pogo";
            else if (zombie.Kind == ZombieKind.Digger && zombie.AbilityPhase == 0) clip = "anim_dig";
            else if (zombie.Kind == ZombieKind.Digger && zombie.AbilityPhase == 1) clip = "anim_dizzy";
            else if (zombie.Kind == ZombieKind.DolphinRider && !zombie.UsedAbility) clip = game.Adventure.Level.IsWaterRow(zombie.Row) ? "anim_ride" : "anim_walkdolphin";
            else if (zombie.Kind == ZombieKind.Snorkel && zombie.Submerged) clip = "anim_swim";
            else if (zombie.Kind == ZombieKind.DuckyTube && game.Adventure.Level.IsWaterRow(zombie.Row)) clip = "anim_swim";
            else if (zombie.Kind == ZombieKind.Zomboni || zombie.Kind == ZombieKind.Catapult) clip = "anim_drive";
            else clip = zombie.PresentationWalk2 && AdventureClipExists(name, "anim_walk2") ? "anim_walk2" : "anim_walk";
            if (AdventureClipExists(name, clip)) return clip;
            return AdventureClipExists(name, "anim_walk") ? "anim_walk" : AdventureClipExists(name, "anim_idle") ? "anim_idle" : null;
        }
        private float AdventureZombieRate(Zombie zombie, string name, string clip, float size)
        {
            if (zombie.Eating) return zombie.Kind == ZombieKind.PoleVaulter || zombie.Kind == ZombieKind.Balloon || zombie.Kind == ZombieKind.Imp ||
                zombie.Kind == ZombieKind.Digger || zombie.Kind == ZombieKind.JackInTheBox || zombie.Kind == ZombieKind.Snorkel || zombie.Kind == ZombieKind.Yeti ? 20f : 36f;
            ReanimationRenderer.NativeAnimationInfo info;
            if (animations.TryGetNativeAnimationInfo(name, clip, out info) && info.GroundDistance > .000001f)
                return zombie.Speed * CellWidth * .01f * 47f * info.Count / info.GroundDistance / size;
            return animations.TryGetNativeAnimationInfo(name, clip, out info) ? info.SourceRate : 12f;
        }
        private void DrawAdventureZombie(Zombie zombie)
        {
            if (!game.IsModeZombieVisible(zombie)) return;
            float size = zombie.IsSmall ? .5f : 1f, x = AdventureRenderX(zombie);
            Vector2 origin = AdventureZombieOrigin(zombie, x);
            DrawImage("plantshadow.png", new Rect(AdventureGridX + x * CellWidth - 38 * size, AdventureCellY(zombie.Row, x) + 68, 76 * size, 22 * size));
            string name = AdventureZombieAnimation(zombie.Kind), clip = game.AdventurePresentationZombieAnimation(zombie);
            if (!string.Equals(zombie.PresentationAnimation, clip, StringComparison.Ordinal)) { zombie.PresentationAnimation = clip; zombie.PresentationAnimationAge = 0; }
            float age = zombie.PresentationAnimationAge + (zombie.FrozenRemaining > 0 ? 0 : AdventureRenderRemainder * (zombie.ChillRemaining > 0 ? .5f : 1f));
            float rate = game.AdventurePresentationZombieRate(zombie);
            DrawAdventureZombiePose(zombie, name, clip, origin, size, age, rate, zombie.PresentationNewspaperGaspRemaining<=0f, "zombie-" + zombie.Id);
            if (zombie.FrozenRemaining > 0 && zombie.PresentationButter <= 0)
                animations.DrawResourceSprite("IMAGE_ICETRAP", new Vector2(AdventureGridX + x * CellWidth, AdventureCellY(zombie.Row, x) + 79), size);
            if (zombie.PresentationButter > 0)
            {
                Vector2 head;
                if (animations.TryGetNativeTrackPosition(name, clip, "anim_head1", age, rate, true, out head))
                    animations.DrawResourceSprite("IMAGE_REANIM_CORNPULT_BUTTER_SPLAT", origin + (head + new Vector2(11, 21)) * size, size);
            }
        }
        private void DrawAdventureZombiePose(Zombie zombie, string name, string clip, Vector2 origin, float size, float age, float rate, bool loop, string key)
        {
            ZombieVariant variant = zombie.Kind == ZombieKind.Flag && !zombie.Headless ? ZombieVariant.Flag :
                zombie.Kind == ZombieKind.Conehead && zombie.ArmorHealth > 0 ? ZombieVariant.Conehead :
                zombie.Kind == ZombieKind.Buckethead && zombie.ArmorHealth > 0 ? ZombieVariant.Buckethead :
                zombie.Kind == ZombieKind.ScreenDoor && zombie.ShieldHealth > 0 ? ZombieVariant.ScreenDoor : ZombieVariant.Normal;
            bool plantHead=zombie.ModeHead>=26&&zombie.ModeHead<=31;
            Func<string, bool> visible = track =>
            {
                if(plantHead&&(track.IndexOf("head",StringComparison.OrdinalIgnoreCase)>=0||track.IndexOf("hair",StringComparison.OrdinalIgnoreCase)>=0||
                    zombie.ModeHead!=26&&zombie.ModeHead!=29&&zombie.ModeHead!=30&&string.Equals(track,"Zombie_tie",StringComparison.OrdinalIgnoreCase)))return false;
                if (zombie.Headless && !AdventureZombiePartsRules.HeadTrackVisible(zombie.ModeHead >= 26 ? zombie.ModeHead : (int)zombie.Kind, track)) return false;
                return !string.Equals(name, "Zombie", StringComparison.OrdinalIgnoreCase) ||
                    zombie.Kind == ZombieKind.DuckyTube && string.Equals(track, "Zombie_duckytube", StringComparison.OrdinalIgnoreCase) ||
                    ReanimationRenderer.IsZombieTrackVisible(track, variant);
            };
            Color previous = GUI.color;
            bool backwards = zombie.PresentationWalkingBackwards ?? (zombie.Hypnotized || zombie.Kind == ZombieKind.Digger && zombie.AbilityPhase == 2);
            Func<bool> draw = () =>
            {
                bool painted=string.Equals(name,"Zombie",StringComparison.OrdinalIgnoreCase)&&!zombie.Headless&&!plantHead&&!backwards&&zombie.Kind!=ZombieKind.Flag&&zombie.Kind!=ZombieKind.DuckyTube?
                    animations.DrawZombiePlayback(origin,size,age,variant,clip,loop,rate,true,null,"actor-"+zombie.Id):
                    animations.DrawNativePlayback(name,origin,size,age,clip,loop,rate,visible,null,null,backwards,"actor-"+zombie.Id,clip=="anim_gasp"?10:20,-1f,90f);
                if(zombie.Kind==ZombieKind.Flag&&!zombie.Headless) animations.DrawNativeZombieFlagAttachment(origin,size,clip,age,rate,zombie.Age,backwards);
                if(plantHead&&!zombie.Headless)animations.DrawNativePlantHeadAttachment(origin,size,clip,age,rate,zombie.ModeHead,zombie.PresentationPlantHeadAge,backwards);return painted;
            };
            string surface = "native-zombie-combat-" + zombie.Id;
            adventurePresentationSurfaces.Add(surface); adventurePresentationLiveSurfaces.Add(surface);
            animations.DrawNativeZombieCombat(zombie, new Rect(origin.x - 160, origin.y - 160, 500, 460), draw);
            GUI.color = previous;
        }
        private void DrawAdventureDetachedArms(int row)
        {
            foreach (var arm in game.Presentation.ArmDrops)
            {
                if ((int)arm.Row != row) continue;
                var pose = new Zombie { Kind = (ZombieKind)arm.NativeKind, Row = row, X = arm.X, IsSmall = arm.Scale < 1f,
                    UsedAbility = arm.Animation != "anim_pogo" && arm.Animation != "anim_run" && arm.Animation != "anim_walkdolphin" };
                animations.DrawNativeZombieArmDrop(adventureParticles, arm, AdventureZombieOrigin(pose, arm.X), AdventureRenderRemainder);
            }
        }
        private void DrawAdventureDetachedZombieParts(int row, bool behindBody)
        {
            foreach (var part in game.Presentation.ZombiePartDrops)
            {
                if (part.Row != row || (part.NativeKind == 8 && part.ParticleId != 11) != behindBody) continue;
                var pose = new Zombie { Kind = (ZombieKind)part.NativeKind, Row = row, X = part.X, IsSmall = part.Scale < 1f,
                    UsedAbility = part.Animation != "anim_pogo" && part.Animation != "anim_run" && part.Animation != "anim_walkdolphin" };
                animations.DrawNativeZombiePartDrop(adventureParticles, part, AdventureZombieOrigin(pose, part.X), AdventureRenderRemainder);
            }
        }
        private void DrawAdventurePlantBiteEffects(int row)
        {
            foreach(var effect in game.Presentation.PlantBiteEffects)
            {
                if(effect.Row!=row)continue;
                adventureParticles.Draw("native-plant-bite-"+effect.Id,effect.ParticleId==35?"WallnutEatSmall":"MindControl",
                    effect.Age+AdventureRenderRemainder,new Vector2(effect.NativeX+effect.OffsetX,effect.NativeY+effect.OffsetY),effect.Seed);
            }
        }
        private void DrawAdventureCorpses(int row)
        {
            foreach (AdventureZombieCorpse corpse in game.Presentation.Corpses)
            {
                Zombie zombie = corpse.Zombie;
                if (zombie.Row != row || !game.IsModeZombieVisible(zombie)) continue;
                if (corpse.Burn != null) { DrawAdventureBurnedCorpse(corpse); continue; }
                string name = AdventureZombieAnimation(zombie.Kind), clip = corpse.Animation;
                Vector2 origin = AdventureZombieOrigin(zombie, zombie.X);
                float rate = corpse.Rate, size = zombie.IsSmall ? .5f : 1f;
                if (!AdventureClipExists(name, clip)) clip = "anim_death";
                ReanimationRenderer.NativeAnimationInfo info;
                if (!animations.TryGetNativeAnimationInfo(name, clip, out info)) { corpse.Duration = 0; continue; }
                if (corpse.Age < corpse.Duration)
                {
                    Color color=GUI.color;GUI.color=new Color(color.r,color.g,color.b,color.a*corpse.Alpha(AdventureRenderRemainder));
                    try { DrawAdventureZombiePose(zombie,name,clip,origin,size,Mathf.Min(corpse.AnimationDuration,corpse.Age+AdventureRenderRemainder),rate,false,"corpse-"+zombie.Id); }
                    finally { GUI.color=color; }
                }
            }
        }
        private void DrawAdventureBurnedCorpse(AdventureZombieCorpse corpse)
        {
            var burn = corpse.Burn; Zombie zombie = corpse.Zombie;
            if (burn.Visual == AdventureBurnVisual.FrozenOriginal)
            {
                if (corpse.Age >= 3f) return;
                string name = AdventureZombieAnimation(zombie.Kind);
                string clip = corpse.Animation ?? AdventureZombieClip(zombie, name);
                if (corpse.FrozenRate <= 0) corpse.FrozenRate = AdventureZombieRate(zombie, name, clip, burn.Scale);
                ZombieVariant variant = zombie.Kind == ZombieKind.Flag ? ZombieVariant.Flag :
                    zombie.Kind == ZombieKind.Conehead && zombie.ArmorHealth > 0 ? ZombieVariant.Conehead :
                    zombie.Kind == ZombieKind.Buckethead && zombie.ArmorHealth > 0 ? ZombieVariant.Buckethead :
                    zombie.Kind == ZombieKind.ScreenDoor && zombie.ShieldHealth > 0 ? ZombieVariant.ScreenDoor : ZombieVariant.Normal;
                bool plantHead = zombie.ModeHead >= 26 && zombie.ModeHead <= 31;
                Func<string, bool> visible = track =>
                    (!plantHead || !track.StartsWith("anim_head", StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(track, "anim_hair", StringComparison.OrdinalIgnoreCase) &&
                        (zombie.ModeHead == 26 || zombie.ModeHead == 29 || zombie.ModeHead == 30 || !string.Equals(track, "Zombie_tie", StringComparison.OrdinalIgnoreCase))) &&
                    (!zombie.Headless || track.IndexOf("head", StringComparison.OrdinalIgnoreCase) < 0 && track.IndexOf("hair", StringComparison.OrdinalIgnoreCase) < 0 &&
                        !string.Equals(track, "anim_cone", StringComparison.OrdinalIgnoreCase) && !string.Equals(track, "anim_bucket", StringComparison.OrdinalIgnoreCase)) &&
                    (!string.Equals(name, "Zombie", StringComparison.OrdinalIgnoreCase) ||
                        zombie.Kind == ZombieKind.DuckyTube && string.Equals(track, "Zombie_duckytube", StringComparison.OrdinalIgnoreCase) ||
                        ReanimationRenderer.IsZombieTrackVisible(track, variant));
                Color previous = GUI.color; GUI.color = new Color(0, 0, 0, previous.a);
                Vector2 origin = AdventureZombieOrigin(zombie, zombie.X);
                try
                {
                    if (string.Equals(name, "Zombie", StringComparison.OrdinalIgnoreCase) && !zombie.Headless && !plantHead && !burn.FacingLeft && zombie.Kind != ZombieKind.DuckyTube)
                        animations.DrawZombiePlayback(origin, burn.Scale, corpse.FrozenSeconds, variant, clip, true, corpse.FrozenRate);
                    else animations.DrawNativePlayback(name, origin, burn.Scale, corpse.FrozenSeconds, clip, true, corpse.FrozenRate, visible,
                        facingLeft: burn.FacingLeft, mirrorWidth: 60f);
                    if (plantHead && !zombie.Headless) animations.DrawNativePlantHeadAttachment(origin, burn.Scale, clip,
                        corpse.FrozenSeconds, corpse.FrozenRate, zombie.ModeHead, corpse.FrozenHeadSeconds, burn.FacingLeft);
                }
                finally { GUI.color = previous; }
                return;
            }
            if (burn.Visual != AdventureBurnVisual.CharredDefinition) return;
            ReanimationDefinition definition;
            if (!VerifiedDataCatalog.TryGetReanimation(burn.ReanimationId, out definition)) { corpse.Duration = 0; return; }
            string asset = Path.GetFileNameWithoutExtension(definition.Path.Replace('\\', '/'));
            ReanimationRenderer.NativeAnimationInfo info;
            bool full = burn.Animation == null;
            bool found = full ? animations.TryGetNativeDefinitionInfo(asset, out info) : animations.TryGetNativeAnimationInfo(asset, burn.Animation, out info);
            if (!found) { corpse.Duration = 0; return; }
            float rate = info.SourceRate * corpse.RateScale;
            corpse.Duration = info.Count / rate;
            if (corpse.Age >= corpse.Duration) return;
            Vector2 position = AdventureZombieSourceOrigin(zombie, zombie.X) + new Vector2(burn.X + burn.PivotX, burn.Y + burn.PivotY);
            Func<string, bool> trackVisible = burn.HideImp ? (Func<string, bool>)(track =>
                !string.Equals(track, "impblink", StringComparison.OrdinalIgnoreCase) && !string.Equals(track, "imphead", StringComparison.OrdinalIgnoreCase)) : null;
            if (full) animations.DrawNativeDefinition(asset, position, burn.Scale, corpse.Age + AdventureRenderRemainder,
                false, rate, trackVisible, burn.FacingLeft, 60f);
            else animations.DrawNativePlayback(asset, position, burn.Scale, corpse.Age + AdventureRenderRemainder, burn.Animation,
                false, rate, trackVisible, facingLeft: burn.FacingLeft, mirrorWidth: 60f);
        }

        private void DrawCollectedSunsAboveSeedBank()
        {
            if (game == null || !game.IsAdventure) return;
            foreach (Sun sun in game.Suns)
                if (sun.PresentationNative && sun.PresentationPickup.Collecting) DrawAdventureSun(sun);
        }
        private void DrawAdventureSun(Sun sun)
        {
            Vector2 center;
            float scale = sun.Value == 15 ? .5f : sun.Value == 50 ? 2f : 1f;
            float alpha = 1f;
            if (sun.PresentationNative)
            {
                var pickup = sun.PresentationPickup;
                if (pickup.Dead) return;
                center = new Vector2(Mathf.Lerp(sun.PresentationPreviousX, pickup.X, AdventureRenderRemainder * 100f) + 30,
                    Mathf.Lerp(sun.PresentationPreviousY, pickup.Y, AdventureRenderRemainder * 100f) + 30);
                scale = sun.PresentationScale;
                if (pickup.FadeCountdown > 0) alpha = pickup.FadeCountdown / 15f;
            }
            else center = new Vector2(AdventureGridX + sun.X * CellWidth, AdventureGridY + sun.Y * AdventurePitch);
            Color previous = GUI.color; GUI.color = new Color(previous.r, previous.g, previous.b, previous.a * alpha);
            // PC 0x43B610 creates the original Sun reanimation at the 60x60 pickup centre, rate 6.
            // The authored glow/rays extend beyond that logical pickup rectangle; never fit them into it.
            animations.DrawNativePlayback("Sun", center, scale, sun.Age + AdventureRenderRemainder, null, true, 6f);
            GUI.color = previous;
        }

        private void DrawAdventureProjectile(Pea pea)
        {
            if (pea.SourceKind == PlantKind.FumeShroom) return; // Original fume uses its authored cloud, not a flying pea.
            float x = pea.PresentationInitialized ? Mathf.Lerp(pea.PresentationPreviousX, pea.X, AdventureRenderRemainder * 100f) : pea.X;
            float row = pea.PresentationInitialized ? Mathf.Lerp(pea.PresentationPreviousRow, pea.FlightRow, AdventureRenderRemainder * 100f) : pea.FlightRow;
            float y = AdventureGridY + row * AdventurePitch + AdventureSlope(x) + 30f;
            float age = pea.PresentationAge + AdventureRenderRemainder;
            float rotation = 0;
            if (pea.Lobbed)
            {
                y = pea.PresentationLaunchY + Mathf.Lerp(pea.PresentationPreviousLobHeight, pea.PresentationLobHeight, AdventureRenderRemainder * 100f);
                rotation = (pea.PresentationRotation + pea.PresentationSpin * pea.PresentationNativeAge * 100f) * Mathf.Rad2Deg;
                animations.DrawResourceSprite("IMAGE_PEA_SHADOWS", new Vector2(AdventureGridX + x * CellWidth, AdventureCellY(pea.Row, x) + 80),
                    1.6f * 200f / (Mathf.Clamp(-pea.PresentationLobHeight, 0, 200) + 200), 0, game.Adventure.Level.IsNight ? 1 : 0);
            }
            Vector2 center = new Vector2(AdventureGridX + x * CellWidth, y);
            if (pea.Fire) { animations.DrawNativePlayback("FirePea", center, 1f, age, null, true); return; }
            string image;
            if (pea.Lobbed) image = pea.Butter ? "IMAGE_REANIM_CORNPULT_BUTTER" : pea.Splash ? pea.IsSnow ? "IMAGE_REANIM_WINTERMELON_PROJECTILE" : "IMAGE_REANIM_MELONPULT_MELON" :
                pea.SourceKind == PlantKind.KernelPult ? "IMAGE_REANIM_CORNPULT_KERNAL" : "IMAGE_REANIM_CABBAGEPULT_CABBAGE";
            else if (pea.SourceKind == PlantKind.Starfruit) image = "IMAGE_PROJECTILE_STAR";
            else if (pea.SourceKind == PlantKind.Cattail) { image = "IMAGE_REANIM_CATTAIL_SPIKE"; rotation = Mathf.Atan2(pea.VelocityY * AdventurePitch, pea.VelocityX * CellWidth) * Mathf.Rad2Deg; }
            else if (pea.SourceKind == PlantKind.Cactus) image = "IMAGE_PROJECTILECACTUS";
            else if (pea.SourceKind == PlantKind.PuffShroom || pea.SourceKind == PlantKind.ScaredyShroom || pea.SourceKind == PlantKind.SeaShroom) image = "IMAGE_PUFFSHROOM_PUFF1";
            else image = pea.IsSnow ? "IMAGE_PROJECTILESNOWPEA" : "IMAGE_PROJECTILEPEA";
            animations.DrawResourceSprite(image, center, 1f, rotation);
        }
        private void DrawAdventureImpacts(int row)
        {
            foreach (AdventureImpact impact in game.Presentation.Impacts)
            {
                if ((int)Math.Round(impact.Row) != row) continue;
                Vector2 point = new Vector2(AdventureGridX + impact.X * CellWidth, AdventureGridY + impact.Row * AdventurePitch + AdventureSlope(impact.X) + 42);
                if (impact.NativeIcePoint)
                {
                    float nativeX, nativeY;game.AdventureIceImpactPoint(impact, AdventureRenderRemainder*100f, out nativeX, out nativeY);
                    point=new Vector2(nativeX,nativeY);
                }
                string key = "impact-" + impact.Id;
                if (impact.Mowed)
                {
                    adventureParticles.Draw(key + "-head", "MoweredZombieHead", impact.Age, point, impact.Seed);
                    adventureParticles.Draw(key + "-arm", "MoweredZombieArm", impact.Age, point, impact.Seed + 1); continue;
                }
                if (impact.Cob)
                {
                    adventureParticles.Draw(key + "-blast", "BlastMark", impact.Age, point, impact.Seed);
                    adventureParticles.Draw(key + "-popcorn", "PopcornSplash", impact.Age, point, impact.Seed + 1); continue;
                }
                if (impact.Fire)
                {
                    ReanimationRenderer.NativeAnimationInfo fire;
                    if (animations.TryGetNativeAnimationInfo("fire", "anim_flame", out fire) && impact.Age < fire.Count / 24f * .75f)
                        animations.DrawNativePlayback("fire", point + new Vector2(38, -20), .7f, impact.Age + fire.Count / 24f * .25f,
                            "anim_flame", false, 24f, verticalScale: .4f);
                    continue;
                }
                string effect = impact.Butter ? "ButterSplat" : impact.Kind == PlantKind.MelonPult ? "melonimpact" :
                    impact.Kind == PlantKind.WinterMelon ? "WinterMelonImpact" : impact.Kind == PlantKind.CabbagePult ? "CabbageSplat" :
                    impact.Kind == PlantKind.Starfruit ? "StarSplat" : impact.Kind == PlantKind.PuffShroom || impact.Kind == PlantKind.ScaredyShroom || impact.Kind == PlantKind.SeaShroom ? "PuffSplat" :
                    impact.Kind == PlantKind.Cactus || impact.Kind == PlantKind.Cattail || impact.Kind == PlantKind.KernelPult || impact.Kind == PlantKind.FumeShroom ? null : impact.Snow ? "SnowPeaSplat" : "PeaSplat";
                if (effect != null) adventureParticles.Draw(key, effect, impact.Age, point, impact.Seed);
            }
        }

    }
}
