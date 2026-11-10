using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using PvZReplica.Adventure;
using PvZReplica.Progression;
using PvZReplica.Profiles;
namespace PvZReplica
{
    // Explicit isolated Player verification only; never runs in normal play.
    internal sealed class ProgressionPlayerSmoke:MonoBehaviour
    {
        private PvZGame controller;private string loggedError;private int checks;
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private object Field(string name){return typeof(PvZGame).GetField(name,Private).GetValue(controller);}
        private void Set(string name,object value){typeof(PvZGame).GetField(name,Private).SetValue(controller,value);}
        private object Call(string name,params object[] args){return typeof(PvZGame).GetMethod(name,Private).Invoke(controller,args);}
        private void View(string view){var field=typeof(PvZGame).GetField("currentView",Private);field.SetValue(controller,Enum.Parse(field.FieldType,view));}
        private GameSimulation Sim{get{return (GameSimulation)Field("game");}}
        private void Check(bool passed,string message){checks++;if(!passed)throw new InvalidOperationException(message);}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap(){if(Array.IndexOf(Environment.GetCommandLineArgs(),"-pvz-progression-smoke")>=0)new GameObject("Progression Player smoke").AddComponent<ProgressionPlayerSmoke>();}
        private IEnumerator Start()
        {
            yield return null;controller=FindFirstObjectByType<PvZGame>();Application.logMessageReceived+=OnLog;
            var run=Run();while(true){bool next;try{next=run.MoveNext();}catch(Exception error){Debug.LogError("PVZ_PROGRESSION_SMOKE_FAIL: "+error);SmokeTestArtifacts.Quit(1);yield break;}if(!next)break;yield return run.Current;}
            if(loggedError!=null){Debug.LogError("PVZ_PROGRESSION_SMOKE_FAIL: "+loggedError);SmokeTestArtifacts.Quit(1);}
            else{Debug.Log("PVZ_PROGRESSION_SMOKE_PASS: "+checks+" integration checks");SmokeTestArtifacts.Quit(0);}
        }
        private void OnLog(string message,string trace,LogType type){if(type==LogType.Exception||type==LogType.Error||type==LogType.Assert)loggedError=message;}
        private void OnDestroy(){Application.logMessageReceived-=OnLog;}
        private IEnumerator Capture(string name){yield return new WaitForEndOfFrame();SmokeTestArtifacts.CaptureFrame(Path.Combine(SmokeTestArtifacts.DirectoryPath,name+".png"));}
        private IEnumerator Run()
        {
            Set("smokeMode",false);yield return new WaitForSecondsRealtime(.8f);yield return Capture("00-original-loading");
            float deadline=Time.unscaledTime+60f;while((!controller.OriginalLoadingReady||(float)Field("originalLoadWidth")<313.99f||(int)Field("originalTitleTicks")<500)&&Time.unscaledTime<deadline)yield return null;
            Check(controller.OriginalLoadingReady&&(float)Field("originalLoadWidth")>=313.99f,"Original loading finishes real progress and title animation");yield return Capture("00b-original-title-ready");Set("smokeMode",true);Call("CompleteOriginalLoading",false);Check(!controller.OriginalLoadingActive&&controller.DynamicAudioReady,"Original loading completes after real resources");
            Check((bool)Call("EnsureAdventureStore")&&(bool)Call("EnsurePlayerAccount"),"Isolated stores initialized");
            var catalog=(ProfileCatalog)Field("profiles");Guid id=catalog.SelectedId.Value;var accounts=(PlayerAccountStore)Field("accountStore");var campaign=(AdventureProgressStore)Field("adventureStore");
            var purchases=new int[80];purchases[13]=1;purchases[14]=5;purchases[15]=5;purchases[16]=1;purchases[17]=1;purchases[18]=1;purchases[19]=1;purchases[20]=1;purchases[25]=1;purchases[27]=1;purchases[28]=5;
            long utc=DateTimeOffset.UtcNow.ToUnixTimeSeconds();var plants=new List<GardenPlant>{new GardenPlant{Id=Guid.NewGuid(),Kind=PlantKind.Sunflower,Slot=0,Growth=3,FeedingsPerGrow=3},new GardenPlant{Id=Guid.NewGuid(),Kind=PlantKind.Marigold,Slot=10,Growth=0,FeedingsPerGrow=3},new GardenPlant{Id=Guid.NewGuid(),Kind=PlantKind.FumeShroom,Garden=GardenKind.Mushroom,Slot=2,Growth=3,FeedingsPerGrow=3},new GardenPlant{Id=Guid.NewGuid(),Kind=PlantKind.TangleKelp,Garden=GardenKind.Aquarium,Slot=3,Growth=3,FeedingsPerGrow=3}};
            accounts.Import(id,20000,purchases,new int[100],25,plants);campaign.Import(id,8,1);
            Call("OpenStore");yield return Capture("01-store-first-page");for(int i=1;i<4;i++){Set("shopPage",i);yield return Capture("02-store-page-"+i);}
            int before=accounts.Get(id).Coins;Set("shopPage",0);Call("HandleClick",new Vector2(407,350));yield return Capture("02b-store-purchase");Call("HandleClick",new Vector2(310,369));Check(accounts.Get(id).Coins==before-500&&accounts.Get(id).Owns(PlantKind.GatlingPea),"Actual purchased upgrade entitlement");
            Call("OpenZenGarden");yield return Capture("03-garden-greenhouse");Set("gardenKind",GardenKind.Mushroom);yield return Capture("04-garden-mushroom");Set("gardenKind",GardenKind.Aquarium);yield return Capture("05-garden-aquarium");
            Call("OpenWisdomTree");yield return Capture("06-wisdom-tree");accounts.FeedWisdomTree(id);Check(accounts.Get(id).TreeHeight==26,"Tree feed persisted");
            Call("OpenModes",1);yield return Capture("07-minigames-directory");Check((bool)Call("StartIndependentMode",16),"Independent mode launches");var mode=Sim.ModeSession;Check(mode.ChooseSeeds(Sim.Adventure.SelectedSeeds),"Mode accepts real loadout");Set("choosingSeeds",false);mode.Update(28f);yield return Capture("08-independent-mode");
            View("Selector");Check((bool)Call("StartAdventureCampaign"),"Replay launches from normal menu");Check(Sim.Adventure.Level.IsReplay&&Sim.Adventure.RequiredSeeds.Count==3&&Sim.Adventure.AvailableSeeds.Count==41,"Replay and owned seeds wired");yield return Capture("09-replay-chooser");
            controller.StartAdventureTestLevel(8,accounts.Get(id).SeedAccess());Check(Sim.ChooseDefaultAdventureSeeds(),"Test board seed chooser");Set("choosingSeeds",false);
            var plantMethod=typeof(GameSimulation).GetMethod("PlaceAdventurePlant",Private);var peashooter=(Plant)plantMethod.Invoke(Sim,new object[]{PlantKind.Peashooter,2,2});peashooter.Timing=PvZUnity.Timing.PlantTiming.FromRaw(PvZUnity.Timing.PlantKind.Peashooter,1,0);
            var sunflower=(Plant)plantMethod.Invoke(Sim,new object[]{PlantKind.Sunflower,1,1});sunflower.Timing=PvZUnity.Timing.PlantTiming.FromRaw(PvZUnity.Timing.PlantKind.Sunflower,1,0);plantMethod.Invoke(Sim,new object[]{PlantKind.CabbagePult,3,2});
            var spawn=typeof(GameSimulation).GetMethod("SpawnAdventureZombie",Private);var zombie=(Zombie)spawn.Invoke(Sim,new object[]{ZombieKind.Normal,2,6f,0});spawn.Invoke(Sim,new object[]{ZombieKind.Buckethead,3,7f,0});
            yield return new WaitForSecondsRealtime(.6f);yield return Capture("10-board-animations");typeof(GameSimulation).GetMethod("HitAdventureZombie",Private).Invoke(Sim,new object[]{zombie,2000f,false,0U});yield return new WaitForSecondsRealtime(.12f);yield return Capture("11-zombie-death");
            yield return new WaitForSecondsRealtime(.4f);yield return Capture("12-projectiles-and-sun");Check(loggedError==null,"Player rendered all inspected pages without errors");
        }
    }
}
