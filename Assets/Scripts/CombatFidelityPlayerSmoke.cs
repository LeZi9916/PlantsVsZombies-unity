using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using PvZReplica.Adventure;
using PvZReplica.Audio;
using PvZReplica.Reanimation;
using PvZReplica.UI;

namespace PvZReplica
{
    // This one explicitly requested QA switch creates disposable profiles and uses actual OnGUI/Player audio.
    internal sealed class CombatFidelityPlayerSmoke : MonoBehaviour
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private PvZGame controller;
        private string errorLog;
        private int checks;
        private readonly List<string> observations = new List<string>();
        private readonly List<string> deferredFailures = new List<string>();
        private GameSimulation Sim { get { return (GameSimulation)Field("game"); } }
        private object Field(string name) { return typeof(PvZGame).GetField(name, Private).GetValue(controller); }
        private void Set(string name, object value) { typeof(PvZGame).GetField(name, Private).SetValue(controller, value); }
        private object Call(string name, params object[] args) { return Invoke(typeof(PvZGame), controller, name, args); }
        private object SimCall(string name, params object[] args) { return Invoke(typeof(GameSimulation), Sim, name, args); }
        private static object Invoke(Type type, object target, string name, object[] args)
        {
            MethodInfo method = type.GetMethod(name, Private); ParameterInfo[] parameters = method.GetParameters();
            int supplied = args.Length; Array.Resize(ref args, parameters.Length);
            for (int i = supplied; i < parameters.Length; i++) args[i] = parameters[i].DefaultValue;
            return method.Invoke(target, args);
        }
        private void Check(bool value, string message)
        { checks++; if (!value) { deferredFailures.Add(message); Debug.LogWarning("PVZ_COMBAT_CHECK_FAIL: " + message); } }
        private void Require(bool value, string message)
        { Check(value, message); if (!value) throw new InvalidOperationException(message); }
        private void OnLog(string message, string stack, LogType type)
        { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errorLog = message; }
        private void OnDestroy() { Application.logMessageReceived -= OnLog; }
        private void MovePointer(Vector2 logical) { Set("smokePointer", (Vector2?)logical); }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] private static void Bootstrap()
        { if (Array.IndexOf(Environment.GetCommandLineArgs(), "-pvz-combat-fidelity-smoke") >= 0) new GameObject("Combat fidelity Player scenario").AddComponent<CombatFidelityPlayerSmoke>(); }
        private IEnumerator Start()
        {
            yield return null; controller = FindFirstObjectByType<PvZGame>(); Application.logMessageReceived += OnLog;
            IEnumerator run = Run();
            while (true)
            {
                bool next;
                try { next = run.MoveNext(); }
                catch (Exception error) { Debug.LogError("PVZ_COMBAT_FIDELITY_FAIL: " + error); SmokeTestArtifacts.Quit(1); yield break; }
                if (!next) break; yield return run.Current;
            }
            if (errorLog != null) { Debug.LogError("PVZ_COMBAT_FIDELITY_FAIL: " + errorLog); SmokeTestArtifacts.Quit(1); yield break; }
            File.WriteAllLines(Path.Combine(SmokeTestArtifacts.DirectoryPath, "observations.txt"), observations);
            if (deferredFailures.Count > 0) { Debug.LogError("PVZ_COMBAT_FIDELITY_FAIL: " + string.Join("; ", deferredFailures)); SmokeTestArtifacts.Quit(1); yield break; }
            Debug.Log("PVZ_COMBAT_FIDELITY_PASS: " + checks + " focused actual Player checks"); SmokeTestArtifacts.Quit(0);
        }
        private IEnumerator Capture(string name)
        { yield return new WaitForEndOfFrame(); SmokeTestArtifacts.CaptureFrame(Path.Combine(SmokeTestArtifacts.DirectoryPath, name + ".png")); }
        private IEnumerator Run()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-pvz-show-loading") >= 0)
            {
                MovePointer(new Vector2(780, 580));
                bool entry = false, partial = false, flowers = false, complete = false;
                float loadingDeadline = Time.unscaledTime + 30f;
                while (controller.OriginalLoadingActive && Time.unscaledTime < loadingDeadline)
                {
                    float width = (float)Field("originalLoadWidth"); int title = (int)Field("originalTitleTicks");
                    float[] sprouts = (float[])Field("originalLoadSproutAges");
                    if (!entry && title > 500) { entry = true; yield return Capture("startup-00-original-title"); }
                    if (!partial && width >= 55f) { partial = true; yield return Capture("startup-01-native-grass-and-cap"); }
                    if (!flowers && width >= 190f) { flowers = true; yield return Capture("startup-02-native-flowers"); }
                    if (controller.OriginalLoadingReady && width >= 313.9f && sprouts[4] >= 1.1f)
                    { complete = true; yield return Capture("startup-03-native-complete-bar"); Call("CompleteOriginalLoading", false); break; }
                    yield return null;
                }
                Require(entry && partial && flowers && complete, "Real original loading stages did not complete: active=" + controller.OriginalLoadingActive +
                    ", ready=" + controller.OriginalLoadingReady + ", progress=" + controller.OriginalLoadingProgress + ", width=" + Field("originalLoadWidth") +
                    ", ticks=" + Field("originalTitleTicks") + ", headAge=" + ((float[])Field("originalLoadSproutAges"))[4]);
                Check(((ReanimationRenderer.AnimationInfo)Field("originalSproutInfo")).FrameCount == 18 &&
                    ((ReanimationRenderer.AnimationInfo)Field("originalHeadInfo")).FrameCount == 18,
                    "Startup animation counters advanced without the actual original18-frame definitions");
                var titleTextures = (Dictionary<string, Texture2D>)Field("originalTitleTextures");
                Check(titleTextures["grass"].width == 314 && titleTextures["grass"].height == 33 &&
                    titleTextures["dirt"].width == 321 && titleTextures["dirt"].height == 53 &&
                    titleTextures["sodroll"].width == 73 && titleTextures["sodroll"].height == 71,
                    "Player imported loading artwork at rescaled NPOT dimensions");
                observations.Add("Actual startup rendered partial grass/cap, flowers and completed head; imported grass/dirt/cap retained original pixel dimensions.");
            }
            float deadline = Time.unscaledTime + 60;
            while (controller.OriginalLoadingActive && Time.unscaledTime < deadline) yield return null;
            Require(!controller.OriginalLoadingActive, "Original loading did not finish");
            controller.StartAdventureTestLevel(8); Require(Sim.ChooseAdventureSeeds(new[] { PlantKind.Sunflower, PlantKind.Peashooter, PlantKind.CherryBomb, PlantKind.Wallnut, PlantKind.PotatoMine, PlantKind.SnowPea }), "Could not prepare native unlocked seed selection");
            Call("AttachAdventureGame", Sim); Set("choosingSeeds", false);
            typeof(GameSimulation).GetProperty("SunAmount").SetValue(Sim, 500); Sim.ResetAdventureWaveSchedule(100000, false, false);
            Rect card = (Rect)Call("AdventureSeedRect", 0, false); Call("HandleClick", card.center);
            Check((int)Field("selected") == 0, "Real seed-bank click did not select the plant");
            Vector2 cell = new Vector2(355, 270); MovePointer(cell); yield return new WaitForSecondsRealtime(.12f);
            Check(((Vector2)Field("adventurePointer") - cell).sqrMagnitude < 4f, "Player render pointer did not reach the requested preview cell: " + Field("adventurePointer"));
            yield return Capture("00-held-plant-and-cell-preview");
            Call("HandleClick", cell); Check(Sim.Plants.Count == 1 && (int)Field("selected") == -1, "Preview cell did not place and release the selected plant");
            yield return Capture("01-native-cell-placement");
            var sun = (Sun)SimCall("CreateAdventurePresentationDroppedSun", 4f, 2f, 25); Sim.Suns.Add(sun);
            var events = new List<GameEvent>(); Action<GameEvent> previous = Sim.EventRaised;
            Sim.EventRaised = e => { events.Add(e); if (previous != null) previous(e); };
            AudioSource effects = (AudioSource)Field("audioSource"); effects.Stop(); int balance = Sim.SunAmount;
            Call("HandleClick", new Vector2(sun.PresentationPickup.X + 30, sun.PresentationPickup.Y + 30));
            Check(sun.PresentationPickup.Collecting && events.Count == 1 && events[0] == GameEvent.SunPickupStarted && Sim.SunAmount == balance,
                "Sun foley was not dispatched on the real click before credit");
            Check(effects.isPlaying, "Clicked sun did not produce actual AudioSource output");
            yield return Capture("02-sun-flight-after-click-sound");
            deadline = Time.unscaledTime + 3; while (Sim.Suns.Contains(sun) && Time.unscaledTime < deadline) yield return null;
            Check(Sim.SunAmount == balance + 25 && events.FindAll(e => e == GameEvent.SunPickupStarted).Count == 1 && events.Contains(GameEvent.SunCollected),
                "Sun arrival duplicated its pickup sound or failed to credit");
            observations.Add("Sun pickup event and audio started before credit; exactly one pickup event through arrival."); Sim.EventRaised = previous;
            // Native PC deliberately makes flying sunlight spendable before the displayed count catches up.
            typeof(GameSimulation).GetProperty("SunAmount").SetValue(Sim, 75); Sim.Adventure.SeedFlashes.Clear();
            var credit = (Sun)SimCall("CreateAdventurePresentationDroppedSun", 4f, 2f, 25); Sim.Suns.Add(credit);
            Check(Sim.CollectSun(credit) && Sim.SunAmount == 75 && Sim.AdventureSunAvailable == 100,
                "Flying sunlight did not use the native availability budget while retaining the displayed balance");
            Check(Sim.Adventure.SeedFlashes.Exists(f => f.Kind == PlantKind.Peashooter) && Sim.AdventurePacketContext(PlantKind.Peashooter).CanAfford,
                "Affordability flash and actual packet availability disagree");
            Call("HandleClick", ((Rect)Call("AdventureSeedRect", 1, false)).center); MovePointer(new Vector2(195, 155));
            yield return Capture("02b-native-picked-dark-card-and-flight-budget");
            Call("HandleClick", new Vector2(195, 155));
            Check(Sim.Plants.Exists(p => p.Kind == PlantKind.Peashooter && p.Row == 0 && p.Column == 1) && Sim.SunAmount == 0 && Sim.AdventureSunAvailable == 0,
                "Native pre-arrival planting failed or counted the incoming sunlight twice");
            deadline = Time.unscaledTime + 3f; while (Sim.Suns.Contains(credit) && Time.unscaledTime < deadline) yield return null;
            Check(!Sim.Suns.Contains(credit) && Sim.SunAmount == 0 && Sim.AdventureSunAvailable == 0, "Arrival did not settle the in-flight reservation once");
            observations.Add("Native in-flight affordability flash, pick-up, planting and arrival all used one consistent budget; no duplicated credit.");

            var normal = (Zombie)SimCall("SpawnAdventureZombie", ZombieKind.Normal, 2, 9.5f, 0);
            var flag = (Zombie)SimCall("SpawnAdventureZombie", ZombieKind.Flag, 3, 9.5f, 0);
            var renderer = (ReanimationRenderer)Field("animations"); ReanimationRenderer.NativeAnimationInfo info;
            Check(renderer.TryGetNativeAnimationInfo("Zombie", Sim.AdventurePresentationZombieAnimation(normal), out info), "Original walking clip missing");
            float firstPixel = AdventureZombieNativeRules.ReanimationSourceX(normal.X) + info.Bounds.xMin;
            float nativeBirth = AdventureZombieNativeRules.NativePositionX(normal.X);
            Check(nativeBirth >= 780f && nativeBirth < 820f && AdventureZombieNativeRules.NativePositionX(flag.X) == 800f,
                "Live right-edge spawns did not use the native PC body coordinates");
            // Clip.Bounds spans all frames and hidden attachment tracks; it is not the current opaque-pixel bound.
            observations.Add("Native ordinary birth=" + nativeBirth + "; clip-wide aggregate bound=" + firstPixel + ". Inspect the actual birth screenshot, not this aggregate as a visibility assertion.");
            yield return Capture("03-native-offscreen-spawn"); normal.X = 6.3f; flag.X = 6.8f;
            float minimum = float.MaxValue, maximum = float.MinValue;
            for (int i = 0; i < 120; i++) { float x = normal.X; Sim.Update(.01f); float distance = x - normal.X; minimum = Math.Min(minimum, distance); maximum = Math.Max(maximum, distance); }
            Check(maximum - minimum > .0001f, "Actual zombie walking displacement remained linear");
            Check(normal.PresentationNativeVelocity >= .23f && normal.PresentationNativeVelocity <= .37f && flag.PresentationNativeVelocity == .45f, "Native speed selection did not reach the live actors");
            yield return Capture("04-native-zombie-walk");
            SimCall("HitAdventureZombie", normal, 200f, false); SimCall("HitAdventureZombie", flag, 200f, false);
            Check(normal.Headless && flag.Headless && Sim.Presentation.ZombiePartDrops.Exists(p => p.ParticleId == 6) && Sim.Presentation.ZombiePartDrops.Exists(p => p.ParticleId == 11),
                "Head hit did not create native head and detached-flag effects");
            yield return new WaitForSecondsRealtime(.08f); yield return Capture("05-head-arm-and-flag-drops");
            Check((int)Field("zombiePartAudioSerial") == Sim.Presentation.ZombiePartSoundSerial && Sim.Presentation.ZombiePartSoundSerial > 0,
                "Native limb sound did not reach the live audio pump");
            yield return new WaitForSecondsRealtime(.35f); yield return Capture("06-detached-parts-in-motion");
            // Newly reported equipment feedback uses real colliding projectiles and original image tracks.
            Sim.Zombies.Clear(); Sim.Plants.Clear(); Sim.Peas.Clear(); Sim.Presentation.Corpses.Clear(); Sim.Presentation.ZombiePartDrops.Clear();
            var armored = new List<Zombie>();
            foreach (var pair in new[] { new { Kind = ZombieKind.Conehead, Row = 1 }, new { Kind = ZombieKind.Buckethead, Row = 2 }, new { Kind = ZombieKind.ScreenDoor, Row = 3 }, new { Kind = ZombieKind.Football, Row = 4 } })
                armored.Add((Zombie)SimCall("SpawnAdventureZombie", pair.Kind, pair.Row, 6f, 0));
            yield return Capture("14-native-equipment-intact");
            for (int stage = 1; stage <= 2; stage++)
            {
                foreach (Zombie target in armored)
                {
                    float equipmentMaximum = target.PresentationMaxArmor > 0 ? target.PresentationMaxArmor : target.PresentationMaxShield;
                    Sim.Peas.Add(new Pea { SourceKind = PlantKind.Peashooter, Row = target.Row, FlightRow = target.Row, X = target.X - .05f, StartX = target.X - .05f, Damage = equipmentMaximum * .4f });
                }
                Sim.Update(.02f); yield return new WaitForSecondsRealtime(.06f);
                foreach (Zombie target in armored)
                    Check(AdventureZombieEquipmentRules.DamageIndex(target.ArmorHealth > 0 ? target.ArmorHealth : target.ShieldHealth,
                        (int)(target.PresentationMaxArmor > 0 ? target.PresentationMaxArmor : target.PresentationMaxShield)) == stage,
                        "Actual projectile hit did not reach equipment damage stage " + stage);
                yield return Capture(stage == 1 ? "15-native-equipment-damage-one" : "16-native-equipment-damage-two");
            }
            Check(Sim.Presentation.ZombieSounds.Exists(s => s.Foley == 46) && Sim.Presentation.ZombieSounds.Exists(s => s.Foley == 58),
                "Actual bucket/cone impacts did not dispatch original metal/plastic foley");
            Check((int)Field("zombieSoundReadIndex") == Sim.Presentation.ZombieSounds.Count, "Equipment impact audio did not reach the Player audio pump");
            var dead = (Zombie)SimCall("SpawnAdventureZombie", ZombieKind.Normal, 0, 5.3f, 0); SimCall("HitAdventureZombie", dead, 2000f, false);
            AdventureZombieCorpse corpse = Sim.Presentation.Corpses.Find(c => ReferenceEquals(c.Zombie, dead));
            Require(corpse != null && corpse.AnimationDuration > 0 && corpse.FadeDuration == 1f, "Normal corpse did not receive native animation and land-fade durations");
            yield return Capture("17-native-corpse-death-animation");
            Sim.Update(Math.Max(0f, corpse.AnimationDuration + .5f - corpse.Age));
            Check(Sim.Presentation.Corpses.Contains(corpse) && corpse.Alpha() == 1f, "Corpse faded or disappeared before the native final ten fade ticks");
            yield return Capture("18-native-corpse-retention");
            Sim.Update(Math.Max(.01f, corpse.Duration - corpse.Age + .02f));
            Check(!Sim.Presentation.Corpses.Contains(corpse), "Native corpse deadline left a permanently retained body");
            observations.Add("Real projectile collisions rendered two native equipment-damage stages; metal/plastic foley reached audio. Land corpse remained through its native fade and was removed at its deadline.");
            Sim.Zombies.Clear(); Sim.Plants.Clear(); Sim.Peas.Clear(); Sim.Presentation.Corpses.Clear();
            typeof(GameSimulation).GetProperty("SunAmount").SetValue(Sim, 500);
            var cherryVictim = (Zombie)SimCall("SpawnAdventureZombie", ZombieKind.Normal, 1, 4.5f, 0);
            int cherryIndex = ((List<PlantKind>)Field("deck")).IndexOf(PlantKind.CherryBomb); Require(cherryIndex >= 0, "Cherry is absent from the prepared real deck");
            var musicMutes = new Dictionary<AudioSource, bool>();
            foreach (var stem in (OriginalMusicStem[])Field("dynamicMusicStems"))
                foreach (var voice in (AudioSource[])typeof(OriginalMusicStem).GetField("sources", Private).GetValue(stem))
                { musicMutes[voice] = voice.mute; voice.mute = true; }
            foreach (var voice in controller.GetComponentsInChildren<AudioSource>()) if (!musicMutes.ContainsKey(voice)) voice.Stop();
            effects.Stop(); Call("HandleClick", ((Rect)Call("AdventureSeedRect", cherryIndex, false)).center); Call("HandleClick", cell);
            Check(effects.isPlaying, "Cherry placement did not request an actual one-shot sound voice");
            Plant cherry = Sim.Plants.Find(p => p.Kind == PlantKind.CherryBomb);
            Require(cherry != null && cherry.CherryBombPresentation != null && cherry.CherryBombPresentation.Rate >= 10f && cherry.CherryBombPresentation.Rate < 15f,
                "Real card placement did not create the native Cherry fuse playback");
            float[] growingSamples = new float[1024]; float growingPeak = 0f, growingListenerPeak = 0f;
            var audioWindows = new List<string> { "time,dsp,playing,muted,virtual,sourcePeak,listenerPeak" };
            float growingStarted = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - growingStarted < .65f)
            {
                float sourceWindow = 0f, listenerWindow = 0f; effects.GetOutputData(growingSamples, 0);
                foreach (float value in growingSamples) sourceWindow = Math.Max(sourceWindow, Math.Abs(value));
                AudioListener.GetOutputData(growingSamples, 0);
                foreach (float value in growingSamples) listenerWindow = Math.Max(listenerWindow, Math.Abs(value));
                growingPeak = Math.Max(growingPeak, sourceWindow); growingListenerPeak = Math.Max(growingListenerPeak, listenerWindow);
                audioWindows.Add((Time.realtimeSinceStartup - growingStarted) + "," + AudioSettings.dspTime + "," + effects.isPlaying + "," +
                    effects.mute + "," + effects.isVirtual + "," + sourceWindow + "," + listenerWindow);
                yield return null;
            }
            File.WriteAllLines(Path.Combine(SmokeTestArtifacts.DirectoryPath, "cherry-sfx-windows.csv"), audioWindows);
            var reverseClip = ((Dictionary<string, AudioClip>)Field("presentationSounds"))["reverse_explosion"];
            float[] decodedReverse = new float[reverseClip.samples * reverseClip.channels]; bool decoded = reverseClip.GetData(decodedReverse, 0); float decodedPeak = 0f;
            foreach (float value in decodedReverse) decodedPeak = Math.Max(decodedPeak, Math.Abs(value));
            File.WriteAllText(Path.Combine(SmokeTestArtifacts.DirectoryPath, "actual-cherry-sfx-output.txt"),
                "clip=" + reverseClip.name + ", loaded=" + reverseClip.loadState + ", sourcePeak=" + growingPeak +
                ", isolatedListenerPeak=" + growingListenerPeak + ", effectVolume=" + effects.volume + ", pitch=" + effects.pitch +
                ", playing=" + effects.isPlaying + ", muted=" + effects.mute + ", listenerVolume=" + AudioListener.volume +
                ", listenerPaused=" + AudioListener.pause + ", decoded=" + decoded + ", decodedPeak=" + decodedPeak);
            checks++; if (growingListenerPeak <= .000001f) deferredFailures.Add("Cherry fuse had no isolated listener PCM; inspect cherry-sfx-windows.csv");
            foreach (var entryMute in musicMutes) entryMute.Key.mute = entryMute.Value;
            yield return Capture("19-original-cherry-pre-explosion");
            deadline = Time.unscaledTime + 2f; while (Sim.Plants.Contains(cherry) && Time.unscaledTime < deadline) yield return null;
            Check(cherry.CherryBombPresentation.Detonated && !Sim.Plants.Contains(cherry) && !Sim.Zombies.Contains(cherryVictim), "Native Cherry fuse did not detonate and apply its actual blast");
            yield return new WaitForSecondsRealtime(.08f); yield return Capture("20-original-cherry-explosion");
            var juicy = (List<AudioSource>)Field("adventureCherryJuicyVoices"); Check(juicy.Exists(v => v != null && v.isPlaying), "Original juicy voice was not played on actual detonation");
            observations.Add("Actual Cherry placement played anim_explode and reverse_explosion PCM; native fuse detonated once and started its separate juicy voice.");
            Check(Sim.Presentation.Explosions.Exists(e => e.Effect == AdventureExplosionEffect.Powie), "Cherry blast omitted its native Powie effect creation");
            Sim.Zombies.Clear(); Sim.Plants.Clear(); Sim.Peas.Clear(); Sim.ResetAdventureWaveSchedule(100000, false, false);
            var mine = (Plant)SimCall("PlaceAdventurePlant", PlantKind.PotatoMine, 3, 3);
            yield return Capture("21-native-mine-buried-size"); Sim.Update(15.01f);
            Check(mine.MinePhase == MinePhase.Rising && Sim.Presentation.Explosions.Exists(e => e.Effect == AdventureExplosionEffect.PotatoMineRise),
                "Native mine countdown did not create its original rise effect");
            yield return Capture("22-native-mine-rise-effect");
            deadline = Time.unscaledTime + 2; while (mine.MinePhase != MinePhase.Armed && Time.unscaledTime < deadline) yield return null;
            Check(mine.MinePhase == MinePhase.Armed, "Original mine rise animation did not arm");
            var mineTarget = (Zombie)SimCall("SpawnAdventureZombie", ZombieKind.Normal, 3, 7f, 0); mineTarget.FrozenRemaining = 100f;
            Sim.Update(.02f); int distantGlow = mine.PotatoMinePresentation.GlowFrames;
            yield return Capture("23-native-mine-armed-distant-light"); mineTarget.X = 5.075f; Sim.Update(.02f);
            Check(mine.PotatoMinePresentation.GlowFrames < distantGlow && mine.PotatoMinePresentation.GlowFrames >= 3,
                "Native mine glow did not speed up with the zombie rectangle gap");
            yield return Capture("24-native-mine-armed-close-light"); mineTarget.X = 3.85f; Sim.Update(.02f); // Native mine/body rectangles now overlap;4.05 left an8px gap.
            Check(!Sim.Plants.Contains(mine) && Sim.Presentation.Explosions.Exists(e => e.Effect == AdventureExplosionEffect.PotatoMine),
                "Mine contact omitted its native debris/SPUDOW explosion");
            yield return Capture("25-native-potato-explosion");
            Sim.Presentation.Explosions.Clear();
            var doom = (Plant)SimCall("PlaceAdventurePlant", PlantKind.DoomShroom, 1, 3); doom.Asleep = false; doom.ActionTimer = .01f; Sim.Update(.03f);
            Check(Sim.Presentation.Explosions.Exists(e => e.Effect == AdventureExplosionEffect.Doom), "Doom special omitted its native Doom effect");
            yield return Capture("26-native-doom-explosion");
            Sim.Presentation.Explosions.Clear();
            var jalapeno = (Plant)SimCall("PlaceAdventurePlant", PlantKind.Jalapeno, 2, 3); jalapeno.ActionTimer = .01f; Sim.Update(.03f);
            Check(Sim.Presentation.JalapenoFires.Exists(f => f.Row == 2 && f.Flames.Count == 12), "Native row burn did not create twelve original fire actors");
            yield return Capture("27-native-jalapeno-twelve-flames");
            observations.Add("Mine used original buried/rise/armed rendering, accelerated its light for nearer zombie bounds, and created its ordinary-death explosion; Cherry, Doom and twelve native Jalapeno fire actors reached actual drawing.");

            Sim.Zombies.Clear(); Sim.Plants.Clear(); Sim.Presentation.Explosions.Clear(); Sim.Presentation.JalapenoFires.Clear();
            int flagIndex = -1; for (int i = 0; i < Sim.Adventure.Waves.Count; i++) if (Sim.Adventure.Waves[i].IsFlag) flagIndex = i;
            Check(flagIndex >= 0, "Scenario has no flag wave"); Sim.Adventure.CurrentWave = flagIndex; Sim.ResetAdventureWaveSchedule(6, false, false); Sim.Update(.01f);
            yield return new WaitForSecondsRealtime(.5f); yield return Capture("07-native-huge-wave-red-text");
            Check((float)Field("hugeWaveNoticeStart") >= 0f, "Real flag-wave scheduler did not create the original message");
            // Advance the active native warning to completion, preserving its clear and final-wave events.
            Sim.Update(Sim.WaveSchedule.WarningTicks * .01f + .02f);
            yield return new WaitForSecondsRealtime(.25f); yield return Capture("08-original-final-wave-animation");
            Check((float)Field("finalWaveNoticeStart") >= 0f, "Real final-wave scheduler did not create its original reanimation");
            controller.StartAdventureTestLevel(5); Sim.Adventure.Tutorial = AdventureTutorial.Complete; Set("choosingSeeds", false);
            Sim.ResetAdventureWaveSchedule(100000, false, false);
            SimCall("AddAdventurePacket", PlantKind.Wallnut, null, null); var redPacket = (AdventurePacket)SimCall("AddAdventurePacket", PlantKind.ExplodeONut, null, null);
            Check(Sim.UsesNativeAdventureConveyor, "Bowling did not use the native conveyor");
            int offset = redPacket.ConveyorOffsetX; Sim.Update(.04f); Check(redPacket.ConveyorOffsetX == offset - 1, "Native conveyor did not move one pixel per four ticks");
            // Move elapsed presentation forward through the genuine update, with no wave replay.
            Sim.Update(17f); yield return Capture("28-native-conveyor-moving-packets");
            int redIndex = -1; for (int i = 0; i < Sim.Adventure.Packets.Count; i++) if (Sim.Adventure.Packets[i].Id == redPacket.Id) redIndex = i;
            Require(redIndex >= 0, "Original explosive packet disappeared before selection");
            Rect redCard = (Rect)Call("AdventureSeedRect", redIndex, true); Call("HandleClick", redCard.center); MovePointer(new Vector2(195, 370));
            Check((int)Field("selectedAdventurePacket") == redPacket.Id, "Moving red packet's real click rectangle was stale");
            yield return Capture("29-native-red-wallnut-held-and-ghost"); Call("HandleClick", new Vector2(195, 370));
            Check(Sim.Adventure.BowlingNuts.Count > 0 && Sim.Adventure.BowlingNuts[Sim.Adventure.BowlingNuts.Count - 1].Explosive,
                "Explosive packet preview placed an ordinary Wallnut");
            observations.Add("Bowling conveyor uses original textures and 4-tick movement; moving red packet retained its explosive type through real selection, held/ghost preview and placement.");
            Sim.Adventure.nuts.Clear(); Sim.Zombies.Clear(); Sim.Presentation.Corpses.Clear();
            var rolling = new AdventureBowlingNut { Row = 2, X = 2.5f };
            Sim.Adventure.nuts.Add(rolling);
            SimCall("SpawnAdventureZombie", ZombieKind.Normal, 2, AdventureZombieNativeRules.GridX(210f), 0);
            Sim.Update(.01f);
            Check(rolling.NativeMotionInitialized && rolling.NativeDirection != 0 && rolling.Row != 2 && Math.Abs(rolling.NativePixelY - 280) <= 2,
                "Bowling collision teleported the visible nut to the next row");
            int bounceY = rolling.NativePixelY; yield return Capture("30-native-bowling-rebound-start"); Sim.Update(.2f);
            Check(Math.Abs(rolling.NativePixelY - bounceY) >= 38 && Math.Abs(rolling.NativePixelY - (80 + rolling.Row * 100)) > 2,
                "Bowling rebound did not continue diagonally between rows");
            yield return Capture("31-native-bowling-continuous-diagonal");
            controller.StartAdventureTestLevel(8); Require(Sim.ChooseDefaultAdventureSeeds(), "Could not prepare the isolated bite scene");
            Call("AttachAdventureGame", Sim); Set("choosingSeeds", false); Sim.ResetAdventureWaveSchedule(100000, false, false);
            Plant bitten = (Plant)SimCall("PlaceAdventurePlant", PlantKind.Wallnut, 2, 3);
            Zombie chewing = (Zombie)SimCall("SpawnAdventureZombie", ZombieKind.Normal, 2, AdventureZombieNativeRules.GridX(330f), 0);
            Sim.Update(.04f); Check(!chewing.Eating, "Zombie chewed before the original attack/eating rectangles reached20px overlap");
            chewing.X = AdventureZombieNativeRules.GridX(309f); Sim.Update(.04f);
            Check(chewing.Eating && bitten.Health < 4000f, "Native overlap did not start actual eating damage");
            deadline = Time.unscaledTime + 2; while (bitten.PresentationEatenFlashTicks == 0 && Time.unscaledTime < deadline) yield return null;
            Check(bitten.PresentationEatenFlashTicks > 0 && Sim.Presentation.ZombieSounds.Exists(s => s.Foley == 6), "Original chew phase did not request plant hit feedback and soft chomp");
            yield return Capture("32-native-chew-and-plant-hit-additive");
            var foleyVoices = (Dictionary<int, List<AudioSource>>)Field("zombieFoleyInstances");
            Check(foleyVoices.ContainsKey(6) && foleyVoices[6].Exists(v => v != null && v.isPlaying), "Chew sound never reached an actual Player voice");
            SimCall("HitAdventureZombie", chewing, 200f, false); float heldX = chewing.X, heldHealth = bitten.Health;
            Sim.Update(.2f);
            Check(chewing.Headless && chewing.Eating && Math.Abs(chewing.X - heldX) < .00001f && bitten.Health == heldHealth,
                "Head loss changed native eating retention or continued forbidden headless bite damage");
            Plant shooter = (Plant)SimCall("PlaceAdventurePlant", PlantKind.Peashooter, 2, 0);
            shooter.Timing = PvZUnity.Timing.PlantTiming.FromRaw(PvZUnity.Timing.PlantKind.Peashooter, 1, 0); shooter.ActionTimer = .01f;
            Sim.Update(.5f);
            Check(Sim.Peas.Count > 0 && Sim.Zombies.Contains(chewing), "Original shooter stopped targeting a live headless zombie");
            yield return Capture("33-native-shooter-targets-headless-eating-body");
            observations.Add("Normal bowling hit changed target row without teleporting visible Y, then advanced diagonally. Bite used native rectangle overlap and chew phases; lost-head eater retained its position/pose, stopped new damage, and stayed targetable by the shooter.");

            controller.StartAdventureTestLevel(8); Require(Sim.ChooseDefaultAdventureSeeds(), "Could not prepare the isolated ice scene");
            Call("AttachAdventureGame", Sim); Set("choosingSeeds", false); Sim.ResetAdventureWaveSchedule(100000, false, false);
            Plant snowShooter = (Plant)SimCall("PlaceAdventurePlant", PlantKind.SnowPea, 2, 1);
            Zombie snowTarget = (Zombie)SimCall("SpawnAdventureZombie", ZombieKind.Normal, 2, 6.5f, 0);
            snowShooter.Timing = PvZUnity.Timing.PlantTiming.FromRaw(PvZUnity.Timing.PlantKind.SnowPea, 1, 0); snowShooter.ActionTimer = .01f;
            deadline = Time.unscaledTime + 2f; while (Sim.Presentation.IceLaunchSoundSerial == 0 && Time.unscaledTime < deadline) yield return null;
            Check(Sim.Presentation.IceLaunchSoundSerial == 1 && Sim.Presentation.SnowTrails.Count == 1,
                "Real Snow Pea release did not create its original launch event and attached trail");
            snowShooter.ActionTimer = 100000f; yield return null;
            Check(foleyVoices.ContainsKey(3) && foleyVoices[3].Exists(v => v != null && v.isPlaying) &&
                foleyVoices.ContainsKey(27) && foleyVoices[27].Exists(v => v != null && v.isPlaying),
                "Original Snow Pea launch Foley did not reach actual Player voices");
            yield return new WaitForSecondsRealtime(.2f); yield return Capture("34-native-snow-pea-trail");
            deadline = Time.unscaledTime + 3f; while (snowTarget.ChillRemaining <= 0f && Time.unscaledTime < deadline) yield return null;
            Check(snowTarget.ChillRemaining > 9f && Sim.Presentation.ChillOnsetSoundSerial == 1 &&
                Sim.Presentation.Impacts.Exists(i => i.Snow && i.Kind == PlantKind.SnowPea && i.NativeIcePoint),
                "Snow projectile hit omitted its native chill onset or distinct attached splat");
            yield return null;
            Check(foleyVoices.ContainsKey(18) && foleyVoices[18].Exists(v => v != null && v.isPlaying),
                "First cold onset did not play its original frozen Foley");
            yield return Capture("35-native-snow-pea-splat-and-chill");
            int coldSounds = Sim.Presentation.ChillOnsetSoundSerial; Sim.ApplyAdventureChill(snowTarget);
            Check(Sim.Presentation.ChillOnsetSoundSerial == coldSounds, "An already chilled zombie repeated its onset sound");
            Check(Sim.Presentation.SnowTrails.Exists(t => t.Stopped), "Projectile removal deleted the original trail before FadeOut");
            yield return new WaitForSecondsRealtime(.13f); yield return Capture("36-native-snow-trail-fadeout");
            Sim.Zombies.Clear(); Sim.Plants.Clear(); Sim.Peas.Clear(); Sim.Presentation.Impacts.Clear(); Sim.Presentation.SnowTrails.Clear();
            Plant winter = (Plant)SimCall("PlaceAdventurePlant", PlantKind.WinterMelon, 1, 1); winter.ActionTimer = .01f;
            Zombie winterTarget = (Zombie)SimCall("SpawnAdventureZombie", ZombieKind.Normal, 1, 6.5f, 0);
            Zombie winterSplash = (Zombie)SimCall("SpawnAdventureZombie", ZombieKind.Normal, 2, 6.5f, 0);
            int launchBeforeWinter = Sim.Presentation.IceLaunchSoundSerial;
            deadline = Time.unscaledTime + 2f; while (Sim.Presentation.IceLaunchSoundSerial == launchBeforeWinter && Time.unscaledTime < deadline) yield return null;
            Check(Sim.Peas.Exists(p => p.SourceKind == PlantKind.WinterMelon && p.IsSnow && p.Lobbed) && Sim.Presentation.SnowTrails.Count == 0,
                "Winter Melon release did not keep its original lobbed ice projectile classification");
            winter.ActionTimer = 100000f; yield return new WaitForSecondsRealtime(.2f); yield return Capture("37-native-winter-melon-lob");
            deadline = Time.unscaledTime + 4f; while (winterTarget.ChillRemaining <= 0f && Time.unscaledTime < deadline) yield return null;
            Check(winterTarget.ChillRemaining > 9f && winterSplash.ChillRemaining > 9f &&
                Sim.Presentation.Impacts.Exists(i => i.Kind == PlantKind.WinterMelon && i.NativeIcePoint),
                "Winter Melon hit omitted its primary/splash chill or original impact coordinates");
            yield return Capture("38-native-winter-melon-impact-and-splash");
            Zombie iceTrapTarget = (Zombie)SimCall("SpawnAdventureZombie", ZombieKind.Normal, 3, 6f, 0);
            Check(Sim.ApplyAdventureIceTrap(iceTrapTarget) && iceTrapTarget.FrozenRemaining >= 4f && iceTrapTarget.FrozenRemaining <= 6f &&
                iceTrapTarget.ChillRemaining == 20f, "First land ice trap did not use native freeze and chill counters");
            observations.Add("Actual Snow Pea release played both original launch Foley voices, retained a world-space trail through hit/FadeOut, and used SnowPeaSplat plus first-onset cold audio. Already cold actors did not replay onset. Winter Melon lob hit chilled the main and adjacent target through the real collision/splash path; land ice trap used the original cold counters.");

            Call("HandleSelector", SelectorAction.Adventure); Require(Sim.IsAdventure && Sim.Adventure.Level.Number == 1, "Isolated normal adventure did not start");
            var last = (Zombie)SimCall("SpawnAdventureZombie", ZombieKind.Normal, 2, 5.4f, 0); SimCall("HitAdventureZombie", last, 2000f, false);
            typeof(GameSimulation).GetProperty("State").SetValue(Sim, MatchState.Won); Sim.Adventure.Phase = AdventurePhase.Reward; SimCall("SpawnAdventureRewardPresentation");
            AdventureRewardPresentation reward = Sim.Adventure.RewardPresentation;
            deadline = Time.unscaledTime + 4; while (!reward.OnGround && Time.unscaledTime < deadline) yield return null;
            yield return Capture("09-seed-drop-and-original-arrow"); Call("HandleClick", new Vector2(reward.X + 25, reward.Y + 35));
            Check(reward.Stage == AdventureAwardStage.Collecting, "Real reward click did not start native collection");
            yield return new WaitForSecondsRealtime(.12f); yield return Capture("10-original-award-starburst");
            yield return new WaitForSecondsRealtime(1f); yield return Capture("11-original-award-rays");
            var particles = (OriginalParticleRenderer)Field("awardParticles"); Check(string.IsNullOrEmpty(particles.LastError), "Original award effect failed: " + particles.LastError);
            deadline = Time.unscaledTime + 8; while ((reward.Stage != AdventureAwardStage.Introduction || reward.IntroductionTicks < 65) && Time.unscaledTime < deadline) yield return null;
            Require(reward.Stage == AdventureAwardStage.Introduction, "Award never reached introduction");
            Check((MusicCue)Field("playingMusicCue") == MusicCue.ZenGarden, "Original introduction music did not start");
            string musicReport = Path.Combine(SmokeTestArtifacts.DirectoryPath, "actual-introduction-music.json");
            controller.BeginMusicOutputProbe(musicReport, 3f);
            yield return Capture("12-native-introduction-during-button-delay");
            Call("HandleClick", new Vector2(402, 566)); Check(Sim.Adventure.Level.Number == 1, "Original award button enabled before counter180");
            deadline = Time.unscaledTime + 5f; while (reward.IntroductionTicks < 185 && Time.unscaledTime < deadline) yield return null;
            Require(reward.IntroductionTicks >= 185, "Original introduction counter stalled"); MovePointer(new Vector2(402, 566)); yield return Capture("13-native-seed-introduction-and-button");
            deadline = Time.unscaledTime + 3f; while (!File.Exists(musicReport) && Time.unscaledTime < deadline) yield return null;
            Require(File.Exists(musicReport), "Introduction audio probe did not complete");
            var music = JsonUtility.FromJson<PvZGame.MusicOutputReport>(File.ReadAllText(musicReport));
            Check(music.status == "PASS" && music.sourcePcmNonzero && music.musicOnlyPcmNonzero, "Introduction music reached a cue but its actual output stayed silent");
            Call("HandleClick", new Vector2(402, 566)); Check(Sim.Adventure.Level.Number == 2, "Original introduction next button did not advance the saved campaign");
            observations.Add("Native Award.xml rendered without resource errors; original introduction music and counter180 controls reached actual Player.");
            Check(errorLog == null, "Player logged an error");
        }
    }
}
