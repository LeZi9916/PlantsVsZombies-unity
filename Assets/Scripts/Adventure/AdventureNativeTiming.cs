using System;
using Native=PvZUnity.Timing;
namespace PvZReplica
{
    public sealed partial class Plant { internal bool KernelWeaponPrepared; }
    public sealed partial class GameSimulation
    {
        private void InitializeAdventureNativePlantTiming(Plant plant)
        {
            if (plant.Kind == PlantKind.CherryBomb) { InitializeAdventureCherryBomb(plant); return; }
            if (plant.Kind == PlantKind.PotatoMine) { InitializeAdventurePotatoMine(plant); return; }
            if(plant.Kind!=PlantKind.Peashooter&&plant.Kind!=PlantKind.SnowPea&&plant.Kind!=PlantKind.Repeater&&plant.Kind!=PlantKind.Sunflower)return;
            var kind=(Native.PlantKind)(int)plant.Kind;plant.Timing=Native.PlantTiming.Initialize(kind,random.Next(Native.PlantTiming.InitialRandomBound(kind)));
            plant.ActionTimer=plant.Timing.Value.Countdown*.01f;
        }
        private bool AdvanceAdventureNativeTimedPlant(Plant plant)
        {
            if (plant.Kind == PlantKind.CherryBomb) return AdvanceAdventureCherryBomb(plant);
            if (plant.Kind == PlantKind.PotatoMine) return AdvanceAdventurePotatoMine(plant);
            if(plant.Kind!=PlantKind.Peashooter&&plant.Kind!=PlantKind.SnowPea&&plant.Kind!=PlantKind.Repeater&&plant.Kind!=PlantKind.Sunflower)return false;
            if(plant.ActionTimer>=100000f)return false;
            if(!plant.Timing.HasValue)InitializeAdventureNativePlantTiming(plant);
            bool sunflower=plant.Kind==PlantKind.Sunflower;
            bool target=!sunflower&&(plant.ModeFacesLeft?FindAdventureTarget(plant.Row,-1f,plant.Column+.5f,false)!=null:FindAdventureTarget(plant.Row,plant.Column+.4f,20f,false)!=null||CanShootAdventureBoss(plant));
            var state=plant.Timing.Value;var input=new Native.PlantTimingInput(true,true,true,true,target,Native.ShootingTrack.Head,true,0);
            int bound=Native.PlantTiming.RequiredRandomBound(state,input);int draw=bound==0?0:random.Next(bound);
            var result=Native.PlantTiming.Tick(state,new Native.PlantTimingInput(true,true,true,true,target,Native.ShootingTrack.Head,true,draw));
            plant.Timing=result.State;plant.ActionTimer=result.State.Countdown*.01f;
            if(result.AnimationStarted){plant.PresentationShotAge=0;plant.PresentationHeadAge[0]=0;plant.PresentationPrefiring=true;plant.ShootAge=0;}
            if(result.SunCreated)Suns.Add(CreateAdventurePresentationSun(plant,25));
            for(int i=0;i<result.ProjectilesCreated;i++)
            {
                var shot=FireAdventurePea(plant,plant.Row,plant.ModeFacesLeft,20f);shot.IsSnow=plant.Kind==PlantKind.SnowPea;
                // Source 0x472C70 queues the shot independently of later target loss.
                if(!plant.ModeFacesLeft&&Adventure.Level.HasRoof&&plant.Column<5)shot.Range=Math.Max(.1f,5-plant.Column-.5f);
            }
            return true;
        }
    }
}
