using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using PvZReplica.Adventure;
using PvZReplica.Audio;
using PvZReplica.Waves;
using UnityEngine;
using UnityEngine.Networking;

namespace PvZReplica
{
    public sealed partial class PvZGame
    {
        // Modes and garden pages may set this on entry, then clear it on return to the selector.
        public MusicCue? DynamicMusicOverride;
        private readonly Dictionary<string, AudioClip> dynamicMusicClips = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        private readonly Dictionary<string, NativeMusicClip> dynamicMusicInfo = new Dictionary<string, NativeMusicClip>(StringComparer.Ordinal);
        private readonly Dictionary<string, AudioClip> presentationSounds = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        private readonly HashSet<AudioClip> presentationResourceClips = new HashSet<AudioClip>();
        private OriginalMusicStem[] dynamicMusicStems;
        private readonly OriginalMusicBurst dynamicBurst = new OriginalMusicBurst();
        private MusicCue playingMusicCue = MusicCue.None;
        private GameSimulation subscribedAudioGame;
        private bool dynamicAudioReady;
        private readonly NativeMusicClip[] dynamicStemInfo = new NativeMusicClip[3];
        private double dynamicMusicNextTickDsp = double.PositiveInfinity;
        private bool dynamicMusicClockPaused;
        private double dynamicMusicPausedAt;
        private int dynamicAudioTasks, dynamicAudioCompleted;
        private AudioListener ownedMusicListener;
        private float nextAudioListenerCheck;
        private int dynamicAudioFailures;
        private bool musicOutputProbeRunning;
        public bool DynamicAudioReady { get { return dynamicAudioReady; } }
        private float DynamicAudioProgress { get { return dynamicAudioReady ? 1f : dynamicAudioCompleted / (float)Math.Max(1, dynamicAudioTasks); } }

        private void InitializeDynamicAudio()
        {
            if (dynamicMusicStems != null) return;
            EnsureOriginalAudioListener();
            dynamicMusicStems = new[] { new OriginalMusicStem(gameObject, musicSource), new OriginalMusicStem(gameObject), new OriginalMusicStem(gameObject) };
            StartCoroutine(LoadDynamicOriginalAudio());
            StartMusicProbeFromArguments();
        }
        private IEnumerator LoadDynamicOriginalAudio()
        {
            var json = Resources.Load<TextAsset>("PvZMusic/native_music");
            var manifest = json == null ? null : JsonUtility.FromJson<NativeMusicManifest>(json.text);
            string[] cues = { "hugewave", "finalwave", "siren", "awooga", "loadingbar_flower", "loadingbar_zombie", "buttonclick", "reverse_explosion", "cherrybomb", "juicy", "dirt_rise" };
            dynamicAudioTasks = (manifest == null || manifest.clips == null ? 0 : manifest.clips.Length) + cues.Length;
            if (manifest != null && manifest.clips != null)
            {
                foreach (var entry in manifest.clips)
                {
                    var request = Resources.LoadAsync<AudioClip>(entry.resource);
                    yield return request;
                    var clip = request.asset as AudioClip;
                    if (clip != null)
                    {
                        clip.LoadAudioData();
                        while (clip.loadState == AudioDataLoadState.Loading) yield return null;
                        if (clip.loadState == AudioDataLoadState.Loaded && clip.samples > 0)
                        { dynamicMusicClips[entry.key] = clip; dynamicMusicInfo[entry.key] = entry; }
                        else { dynamicAudioFailures++; Debug.LogWarning("Original tracker failed to decode: " + entry.resource + " (" + clip.loadState + ")"); }
                    }
                    else { dynamicAudioFailures++; Debug.LogWarning("Original tracker stem is missing: " + entry.resource); }
                    dynamicAudioCompleted++;
                }
            }
            else { dynamicAudioFailures++; Debug.LogWarning("Original music manifest is missing. Run Audio/Tools/prepare_original_music.py before building."); }
            foreach (string cue in cues)
            {
                string path = Path.Combine(root, "sounds", cue + ".ogg");
                if (File.Exists(path))
                {
                    using (var request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, AudioType.OGGVORBIS))
                    {
                        yield return request.SendWebRequest();
                        if (request.result == UnityWebRequest.Result.Success) presentationSounds[cue] = DownloadHandlerAudioClip.GetContent(request);
                    }
                }
                if (!presentationSounds.ContainsKey(cue))
                {
                    // The shipped StreamingAssets set is intentionally small. Wave cues must also travel
                    // as original Resources audio, otherwise an event can fire successfully into silence.
                    bool cherry = cue == "reverse_explosion" || cue == "cherrybomb" || cue == "juicy";
                    var bundled = Resources.LoadAsync<AudioClip>((cherry ? "PvZCherryBombSounds/" : cue == "dirt_rise" ? "PvZMineSounds/" : "PvZWaveNotices/") + cue);
                    yield return bundled;
                    var clip = bundled.asset as AudioClip;
                    if (clip != null)
                    {
                        clip.LoadAudioData();
                        while (clip.loadState == AudioDataLoadState.Loading) yield return null;
                        if (clip.loadState == AudioDataLoadState.Loaded)
                        { presentationSounds[cue] = clip; presentationResourceClips.Add(clip); }
                    }
                }
                dynamicAudioCompleted++;
            }
            dynamicAudioReady = true;
            // A context may start while only its base clip is loaded. Restart that same original cue once
            // the complete stem set is resident so the drums/hihats are not permanently left unassigned.
            if (playingMusicCue != MusicCue.None) PlayOriginalMusicCue(playingMusicCue);
        }
        private void UpdateDynamicAudio()
        {
            if (dynamicMusicStems == null) return;
            UpdateAdventureCherryBombAudio();
            UpdateAdventurePotatoMineAudio();
            if (Time.unscaledTime >= nextAudioListenerCheck)
            { EnsureOriginalAudioListener(); nextAudioListenerCheck = Time.unscaledTime + 2f; }
            if (subscribedAudioGame != game)
            {
                if (subscribedAudioGame != null) subscribedAudioGame.WaveScheduleEventRaised -= HandleScheduledWaveAudio;
                subscribedAudioGame = game;
                if (game != null) game.WaveScheduleEventRaised += HandleScheduledWaveAudio;
            }
            MusicCue desired = ResolveOriginalMusicCue();
            if (desired != playingMusicCue || desired != MusicCue.None && !dynamicMusicStems[0].IsPlaying)
                PlayOriginalMusicCue(desired);
            bool paused = optionsView != null && optionsView.IsOpen || currentView == ScreenView.Board && game != null && game.State == MatchState.Paused;
            double now = AudioSettings.dspTime;
            float volume = optionsView != null && optionsView.IsOpen ? optionsView.MusicVolume : musicVolume;
            if (paused)
            {
                if (!dynamicMusicClockPaused) { dynamicMusicClockPaused = true; dynamicMusicPausedAt = now; }
                foreach (var stem in dynamicMusicStems) stem.Update(volume, true);
                return;
            }
            if (dynamicMusicClockPaused)
            {
                dynamicMusicNextTickDsp += Math.Max(0, now - dynamicMusicPausedAt) + OriginalMusicStem.SchedulingLeadSeconds;
                foreach (var stem in dynamicMusicStems) stem.Update(volume, false);
                dynamicMusicClockPaused = false;
            }
            int living = 0;
            if (game != null) foreach (Zombie z in game.Zombies) if (z.Health > 0f && !z.Headless && !z.Hypnotized && z.Kind != ZombieKind.Bungee) living++;
            if (IsTerrainCue(desired) && dynamicStemInfo[0] != null && dynamicMusicStems[0].IsPlaying)
            {
                while (dynamicMusicNextTickDsp <= now + 0.00000001)
                {
                    var info = dynamicStemInfo[0];
                    int sample = dynamicMusicStems[0].NativeSampleAtDsp(dynamicMusicNextTickDsp, info.sampleRate);
                    dynamicBurst.Tick(living, desired == MusicCue.Night, info.PositionAtSample(sample));
                    if (dynamicBurst.RequestedNightOrder != -1)
                    {
                        // Apply the native order jump with its original time origin. The seek includes the
                        // elapsed part of the row, so a main-thread frame cannot restart a late drum attack.
                        double playbackDsp = now + OriginalMusicStem.SchedulingLeadSeconds;
                        string key = dynamicBurst.RequestedNightOrder == 76 ? "night_burst" : "night_burst77";
                        NativeMusicClip jump;
                        if (dynamicMusicInfo.TryGetValue(key, out jump))
                        {
                            int phase = (int)Math.Floor(Math.Max(0, playbackDsp - dynamicMusicNextTickDsp) * jump.sampleRate);
                            PlayOriginalStem(1, key, playbackDsp, phase);
                        }
                    }
                    dynamicMusicNextTickDsp += 0.01;
                }
            }
            dynamicMusicStems[0].Gain = desired == MusicCue.Night ? dynamicBurst.NightBase : 1f;
            dynamicMusicStems[1].Gain = dynamicBurst.Drums;
            dynamicMusicStems[2].Gain = dynamicBurst.HiHats;
            foreach (var stem in dynamicMusicStems) stem.Update(volume, false);
        }
        private MusicCue ResolveOriginalMusicCue()
        {
            if (OriginalLoadingActive) return MusicCue.Title;
            // PC CutScene::StartLevelIntro 0x446F64 selects tune 6 (ChooseYourSeeds).
            if (AdventureIntroActive && currentView == ScreenView.Board && game.Adventure.Level.Mode != AdventureMode.Boss) return MusicCue.ChooseSeeds;
            if (currentView == ScreenView.Board && game != null)
            {
                MusicCue awardCue;
                if (TryResolveAdventureRewardMusic(out awardCue)) return awardCue;
                if (game.State == MatchState.Won || game.State == MatchState.Lost) return MusicCue.None;
                if (choosingSeeds || game.IsAdventure && game.Adventure.Phase == AdventurePhase.ChoosingSeeds) return MusicCue.ChooseSeeds;
            }
            if (DynamicMusicOverride.HasValue) return DynamicMusicOverride.Value;
            if (currentView != ScreenView.Board || game == null) return MusicCue.Title;
            if (!game.IsAdventure) return MusicCue.Day;
            var level = game.Adventure.Level;
            if (level.Mode == AdventureMode.Boss) return MusicCue.Boss;
            if (level.Mode == AdventureMode.Storm) return MusicCue.None;
            if (level.Mode == AdventureMode.Bowling || level.Mode == AdventureMode.WhackAZombie || level.Mode == AdventureMode.LittleTrouble || level.Mode == AdventureMode.Bungee) return MusicCue.Minigame;
            if (level.Mode == AdventureMode.Vasebreaker) return MusicCue.Puzzle;
            if (level.Number == 10 || level.Number == 20 || level.Number == 30) return MusicCue.Conveyor;
            if (level.Terrain == AdventureTerrain.Fog) return MusicCue.Fog;
            if (level.IsNight) return MusicCue.Night;
            if (level.HasPool) return MusicCue.Pool;
            if (level.HasRoof) return MusicCue.Roof;
            return MusicCue.Day;
        }
        private static bool IsTerrainCue(MusicCue cue)
        { return cue == MusicCue.Day || cue == MusicCue.Night || cue == MusicCue.Pool || cue == MusicCue.Fog || cue == MusicCue.Roof; }
        private void PlayOriginalMusicCue(MusicCue cue)
        {
            foreach (var stem in dynamicMusicStems) stem.Stop();
            playingMusicCue = cue; dynamicBurst.Reset();
            dynamicMusicClockPaused = false; dynamicMusicNextTickDsp = double.PositiveInfinity;
            Array.Clear(dynamicStemInfo, 0, dynamicStemInfo.Length);
            string key;
            switch (cue)
            {
                case MusicCue.Title: key = "title"; break;
                case MusicCue.ChooseSeeds: key = "choose"; break;
                case MusicCue.Day: key = "day_base"; break;
                case MusicCue.Night: key = "night_base"; break;
                case MusicCue.Pool: key = "pool_base"; break;
                case MusicCue.Fog: key = "fog_base"; break;
                case MusicCue.Roof: key = "roof_base"; break;
                case MusicCue.Boss: key = "boss"; break;
                case MusicCue.Minigame: key = "minigame"; break;
                case MusicCue.Puzzle: key = "puzzle"; break;
                case MusicCue.Conveyor: key = "conveyor"; break;
                case MusicCue.ZenGarden: key = "garden"; break;
                default: return;
            }
            double start = AudioSettings.dspTime + OriginalMusicStem.SchedulingLeadSeconds;
            dynamicMusicNextTickDsp = start;
            dynamicMusicStems[0].Gain = 1f;
            dynamicMusicStems[1].Gain = dynamicMusicStems[2].Gain = 0f;
            PlayOriginalStem(0, key, start);
            if (IsTerrainCue(cue))
            {
                string prefix = key.Substring(0, key.Length - 5);
                PlayOriginalStem(1, cue == MusicCue.Night ? "night_standby" : prefix + "_drums", start);
                if (cue != MusicCue.Night) PlayOriginalStem(2, prefix + "_hihats", start);
            }
        }
        private void PlayOriginalStem(int index, string key, double dsp, int nativeStartSample = 0)
        {
            AudioClip clip; NativeMusicClip info;
            if (dynamicMusicClips.TryGetValue(key, out clip) && dynamicMusicInfo.TryGetValue(key, out info))
            {
                dynamicStemInfo[index] = info;
                dynamicMusicStems[index].Play(clip, info.loopStartSample, info.sampleRate, dsp, nativeStartSample);
            }
        }
        private void HandleScheduledWaveAudio(WaveScheduleEvent e)
        {
            if (currentView != ScreenView.Board || OriginalLoadingActive) return;
            if (e.Kind == WaveScheduleEventKind.HugeWaveSound) PlayPresentationSound("hugewave");
            else if (e.Kind == WaveScheduleEventKind.FinalWaveSound) PlayPresentationSound("finalwave");
            else if (e.Kind == WaveScheduleEventKind.SirenSound) PlayPresentationSound("siren");
            else if (e.Kind == WaveScheduleEventKind.FirstWaveApproaching) PlayPresentationSound("awooga");
            else if (e.Kind == WaveScheduleEventKind.MusicBurst) dynamicBurst.ForceBurst();
        }
        private void PlayPresentationSound(string name)
        {
            AudioClip clip;
            if (audioSource != null && presentationSounds.TryGetValue(name, out clip) && clip != null)
                audioSource.PlayOneShot(clip, 0.7f);
        }
        private void EnsureOriginalAudioListener()
        {
            var listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var listener in listeners)
            {
                if (listener != ownedMusicListener && listener.isActiveAndEnabled)
                {
                    if (ownedMusicListener != null) ownedMusicListener.enabled = false;
                    return;
                }
            }
            if (ownedMusicListener == null)
            {
                // The IMGUI-only Lawn scene is built empty: it has neither a Camera nor its usual listener.
                // A 2D source still needs one listener to reach Unity's output mixer.
                var listenerObject = new GameObject("PvZ Audio Listener");
                listenerObject.transform.SetParent(transform, false);
                ownedMusicListener = listenerObject.AddComponent<AudioListener>();
                Debug.Log("Original audio installed the missing listener for the IMGUI-only scene.");
            }
            ownedMusicListener.enabled = true;
        }

        [Serializable] public sealed class MusicOutputReport
        {
            public string status, cue, outputPath, explanation;
            public bool loadingReady, listenerPaused, gamePaused, sourcePcmNonzero, listenerPcmNonzero, musicOnlyPcmNonzero;
            public int listeners, decodedClips, decodeFailures, outputSampleRate, windows, musicOnlyWindows;
            public float savedMusicVolume, effectiveMusicVolume, listenerVolume, seconds;
            public double dspStart, dspEnd, sourceEnergy, listenerEnergy, musicOnlyEnergy;
            public long sourceSamples, listenerSamples, musicOnlySamples;
            public float sourcePeak, listenerPeak, musicOnlyPeak;
            public OriginalMusicStemState[] stems;
        }
        public MusicOutputReport GetMusicOutputReport()
        {
            int listeners = 0;
            foreach (var listener in FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (listener.isActiveAndEnabled) listeners++;
            OriginalMusicStemState[] states = dynamicMusicStems == null ? new OriginalMusicStemState[0] : new OriginalMusicStemState[dynamicMusicStems.Length];
            for (int i = 0; i < states.Length; i++) states[i] = dynamicMusicStems[i].Describe();
            return new MusicOutputReport { cue = playingMusicCue.ToString(), loadingReady = dynamicAudioReady,
                listeners = listeners, decodedClips = dynamicMusicClips.Count, decodeFailures = dynamicAudioFailures,
                savedMusicVolume = musicVolume, effectiveMusicVolume = optionsView != null && optionsView.IsOpen ? optionsView.MusicVolume : musicVolume,
                listenerVolume = AudioListener.volume, listenerPaused = AudioListener.pause,
                gamePaused = dynamicMusicClockPaused, outputSampleRate = AudioSettings.outputSampleRate, stems = states };
        }
        public void BeginMusicOutputProbe(string reportPath, float seconds = 6f, bool quitWhenFinished = false)
        {
            if (string.IsNullOrEmpty(reportPath)) throw new ArgumentException("A report path is required.", nameof(reportPath));
            if (musicOutputProbeRunning) throw new InvalidOperationException("A music output probe is already running.");
            musicOutputProbeRunning = true;
            StartCoroutine(ProbeMusicOutput(Path.GetFullPath(reportPath), Mathf.Clamp(seconds, 1f, 30f), quitWhenFinished));
        }
        private void StartMusicProbeFromArguments()
        {
            string[] args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-pvz-audio-probe");
            if (index < 0) return;
            string path = index + 1 < args.Length && !args[index + 1].StartsWith("-", StringComparison.Ordinal) ? args[index + 1] : Path.Combine(Application.persistentDataPath, "music-output.json");
            float seconds = 6f; int duration = Array.IndexOf(args, "-pvz-audio-probe-seconds");
            if (duration >= 0 && duration + 1 < args.Length)
                float.TryParse(args[duration + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out seconds);
            BeginMusicOutputProbe(path, seconds <= 0 ? 6f : seconds, Array.IndexOf(args, "-pvz-audio-probe-quit") >= 0);
        }
        private IEnumerator ProbeMusicOutput(string reportPath, float duration, bool quitWhenFinished)
        {
            float deadline = Time.realtimeSinceStartup + 45f;
            while (!dynamicAudioReady && Time.realtimeSinceStartup < deadline) yield return null;
            // Allow the actual scheduled start, decoder and at least one DSP output block to finish.
            yield return new WaitForSecondsRealtime(0.4f);
            float start = Time.realtimeSinceStartup;
            var report = GetMusicOutputReport(); report.dspStart = AudioSettings.dspTime;
            float[] pcm = new float[1024];
            var probeInterval = new WaitForSecondsRealtime(0.02f);
            while (Time.realtimeSinceStartup - start < duration)
            {
                bool musicVoice = false;
                if (dynamicMusicStems != null) foreach (var stem in dynamicMusicStems)
                {
                    float peak; int samples;
                    report.sourceEnergy += stem.ReadOutputEnergy(pcm, out peak, out samples);
                    report.sourceSamples += samples; report.sourcePeak = Math.Max(report.sourcePeak, peak);
                    if (samples > 0 && stem.Gain > 0f) musicVoice = true;
                }
                AudioListener.GetOutputData(pcm, 0);
                bool musicOnly = musicVoice && audioSource != null && !audioSource.isPlaying && !dynamicMusicClockPaused;
                foreach (float sample in pcm)
                {
                    if (float.IsNaN(sample) || float.IsInfinity(sample)) continue;
                    float peak = Math.Abs(sample); double energy = sample * (double)sample;
                    report.listenerEnergy += energy; report.listenerSamples++; report.listenerPeak = Math.Max(report.listenerPeak, peak);
                    if (musicOnly) { report.musicOnlyEnergy += energy; report.musicOnlySamples++; report.musicOnlyPeak = Math.Max(report.musicOnlyPeak, peak); }
                }
                report.windows++; if (musicOnly) report.musicOnlyWindows++;
                yield return probeInterval;
            }
            var end = GetMusicOutputReport();
            report.cue = end.cue; report.loadingReady = end.loadingReady; report.listeners = end.listeners;
            report.decodedClips = end.decodedClips; report.decodeFailures = end.decodeFailures;
            report.listenerPaused = end.listenerPaused; report.gamePaused = end.gamePaused;
            report.savedMusicVolume = end.savedMusicVolume; report.effectiveMusicVolume = end.effectiveMusicVolume;
            report.listenerVolume = end.listenerVolume; report.outputSampleRate = end.outputSampleRate; report.stems = end.stems;
            report.dspEnd = AudioSettings.dspTime; report.seconds = Time.realtimeSinceStartup - start; report.outputPath = reportPath;
            report.sourcePcmNonzero = report.sourcePeak > 0.000001f;
            report.listenerPcmNonzero = report.listenerPeak > 0.000001f;
            report.musicOnlyPcmNonzero = report.musicOnlyPeak > 0.000001f;
            report.status = report.sourcePcmNonzero && report.musicOnlyPcmNonzero ? "PASS" :
                report.effectiveMusicVolume <= 0f || report.listenerVolume <= 0f ? "MUTED_BY_VOLUME" :
                report.listenerPaused || report.gamePaused ? "PAUSED" : "NO_MUSIC_OUTPUT";
            report.explanation = "PCM sampled from native AudioSource outputs and final AudioListener mix; music-only windows exclude presentation/SFX playback. User volumes were not changed. Physical OS/device output is outside this probe.";
            string parent = Path.GetDirectoryName(reportPath); if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
            File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
            Debug.Log("Original music PCM probe " + report.status + ": " + reportPath);
            musicOutputProbeRunning = false;
            if (quitWhenFinished) Application.Quit(report.status == "PASS" ? 0 : 2);
        }
        private void DisposeDynamicAudio()
        {
            if (subscribedAudioGame != null) subscribedAudioGame.WaveScheduleEventRaised -= HandleScheduledWaveAudio;
            if (dynamicMusicStems != null) foreach (var stem in dynamicMusicStems) stem.Dispose();
            foreach (var clip in presentationSounds.Values) if (clip != null && !presentationResourceClips.Contains(clip)) Destroy(clip);
            presentationSounds.Clear(); presentationResourceClips.Clear();
            DisposeAdventureWaveNotices();
            DisposeAdventureCherryBombAudio();
            // Resources owns the imported music clips. They are not the runtime clips in LoadSoundsCore.
            dynamicMusicClips.Clear(); dynamicMusicInfo.Clear(); dynamicMusicStems = null;
            if (ownedMusicListener != null) { Destroy(ownedMusicListener.gameObject); ownedMusicListener = null; }
        }
    }
}
