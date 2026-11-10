using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using PvZReplica.Adventure;
using UnityEngine;
using UnityEngine.Networking;
namespace PvZReplica
{
    public sealed partial class PvZGame
    {
        private GameSimulation zombiePartAudioGame;
        private int zombiePartAudioSerial,zombieSoundReadIndex;
        private bool zombiePartAudioLoading,zombieFoleyReady;
        private readonly System.Random zombieFoleyRandom=new Randomness.OriginalRandomAdapter(12345U);
        private readonly Dictionary<int,int> zombieFoleyLastTick=new Dictionary<int,int>();
        private readonly Dictionary<int,List<AudioSource>> zombieFoleyInstances=new Dictionary<int,List<AudioSource>>();
        // Called at the top of the host PlaySound; keeps the old classic-game event behavior.
        private bool SuppressLegacyZombieHitSound(GameEvent evt)
        {
            if(game==null||!game.IsAdventure)return false;
            if(evt==GameEvent.Shoot&&game.Presentation.IceShootSuppressions>0)
            {game.Presentation.IceShootSuppressions--;return true;}
            return evt==GameEvent.ZombieHit;
        }
        private void UpdateAdventureZombiePartAudio()
        {
            if(!ReferenceEquals(zombiePartAudioGame,game))
            {zombiePartAudioGame=game;zombiePartAudioSerial=0;zombieSoundReadIndex=0;zombieFoleyLastTick.Clear();}
            if(!zombiePartAudioLoading){zombiePartAudioLoading=true;StartCoroutine(LoadAdventureZombiePartAudio());}
            if(!zombieFoleyReady||game==null||audioSource==null)return;
            while(zombieSoundReadIndex<game.Presentation.ZombieSounds.Count)
            {
                var evt=game.Presentation.ZombieSounds[zombieSoundReadIndex++];
                if(evt.Foley==25)zombiePartAudioSerial++;
                PlayAdventureZombieFoley(evt);
            }
        }
        private void PlayAdventureZombieFoley(AdventureZombieSoundEvent evt)
        {
            if(evt.Foley==-1){AudioClip gulp;if(presentationSounds.TryGetValue("gulp",out gulp)&&gulp!=null)audioSource.PlayOneShot(gulp);return;}
            AdventureZombieFoleyDefinition definition;
            if(!AdventureZombieFoleyData.Definitions.TryGetValue(evt.Foley,out definition))return;
            // PC 0x52A560: 8 instances per type, a request within 10 native updates is suppressed.
            int previous;if(zombieFoleyLastTick.TryGetValue(evt.Foley,out previous)&&evt.Tick-previous<10)return;
            List<AudioSource> sources;
            if(!zombieFoleyInstances.TryGetValue(evt.Foley,out sources))
                zombieFoleyInstances[evt.Foley]=sources=new List<AudioSource>();
            AudioSource source=sources.Find(s=>s!=null&&!s.isPlaying);
            if(source==null)
            {
                if(sources.Count>=8)return;
                var child=new GameObject("Original Zombie Foley "+evt.Foley);child.transform.SetParent(transform,false);
                source=child.AddComponent<AudioSource>();source.playOnAwake=false;source.loop=false;source.spatialBlend=0f;
                source.outputAudioMixerGroup=audioSource.outputAudioMixerGroup;sources.Add(source);
            }
            // PC table 0x74B3B8: preserve duplicate entries as variation weights.
            float semitones=(float)zombieFoleyRandom.NextDouble()*definition.PitchRange;
            string sample=definition.Samples[zombieFoleyRandom.Next(definition.Samples.Length)];AudioClip clip;
            if(!presentationSounds.TryGetValue(sample,out clip)||clip==null)return;
            // PC DSoundInstance::AdjustPitch 0x63E850, including native frequency limits.
            double frequency=Math.Max(100d,Math.Min(100000d,clip.frequency*Math.Pow(1.059463094359295d,semitones)));
            source.clip=clip;source.pitch=(float)(frequency/clip.frequency);source.volume=soundVolume;
            zombieFoleyLastTick[evt.Foley]=evt.Tick;source.Play();
        }
        private IEnumerator LoadAdventureZombiePartAudio()
        {
            var names=new HashSet<string>{"gulp"};
            foreach(var definition in AdventureZombieFoleyData.Definitions.Values)foreach(string name in definition.Samples)names.Add(name);
            foreach(string name in names)
            {
                if(presentationSounds.ContainsKey(name))continue;
                string path=Path.Combine(root,"sounds",name+".ogg");
                if(!File.Exists(path)){Debug.LogError("Missing original zombie Foley: "+path);continue;}
                using(var request=UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri,AudioType.OGGVORBIS))
                {
                    yield return request.SendWebRequest();
                    if(request.result==UnityWebRequest.Result.Success)presentationSounds[name]=DownloadHandlerAudioClip.GetContent(request);
                    else Debug.LogError("Original zombie Foley failed: "+name+": "+request.error);
                }
            }
            // Existing DisposeDynamicAudio owns the clips; child audio sources share this component's lifetime.
            zombieFoleyReady=true;
        }
    }
}
