using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using PvZReplica.Adventure;

namespace PvZReplica.Adventure
{
    /// <summary>Explicit host-supplied access, not a shop purchase or persistent entitlement.</summary>
    public sealed class AdventureSeedAccess
    {
        public IReadOnlyList<PlantKind> AdditionalSeeds { get; private set; }
        public PlantKind? ImitaterTarget { get; private set; }
        public AdventureSeedAccess(IEnumerable<PlantKind> additionalSeeds,PlantKind? imitaterTarget=null)
        {
            if(additionalSeeds==null)throw new ArgumentNullException("additionalSeeds");
            var values=new List<PlantKind>();
            foreach(var kind in additionalSeeds)
            {
                if((int)kind<40 || (int)kind>48 || values.Contains(kind))throw new ArgumentException("Explicit access accepts unique IDs 40..48 only.");
                values.Add(kind);
            }
            if(imitaterTarget.HasValue && ((int)imitaterTarget.Value<0 || (int)imitaterTarget.Value>39 || !values.Contains(PlantKind.Imitater)))
                throw new ArgumentException("Imitater requires access and a basic plant target ID 0..39.");
            AdditionalSeeds=Array.AsReadOnly(values.ToArray());ImitaterTarget=imitaterTarget;
        }
        public static AdventureSeedAccess DevelopmentAll(PlantKind? imitaterTarget=null)
        {
            var values=new List<PlantKind>();for(int id=40;id<=48;id++)values.Add((PlantKind)id);
            return new AdventureSeedAccess(values,imitaterTarget);
        }
    }
    public enum CobCannonPhase { Arming, Loading, Ready, Aiming, Firing, Reloading }
    public enum AdventureCoinKind { Silver, Gold, Diamond, PlantGift, Chocolate, MinigameUnlock, PuzzleUnlock }
    public sealed class AdventureCoin
    {
        public int Id { get; internal set; }
        public AdventureCoinKind Kind { get; internal set; }
        public PlantKind? GiftPlant { get; internal set; }
        public int ValueUnits { get { return Kind==AdventureCoinKind.Silver?1:Kind==AdventureCoinKind.Gold?5:Kind==AdventureCoinKind.Diamond?100:0; } }
        public float X { get; internal set; }
        public float Row { get; internal set; }
        public float Age { get; internal set; }
        public bool IsBeingCollected { get; internal set; }
        public bool PersistencePending { get; internal set; }
        public int CollectorPlantId { get; internal set; }
        internal float CollectionRemaining;
    }
    public sealed class AdventureCobShell
    {
        public int SourcePlantId { get; internal set; }
        public float TargetX { get; internal set; }
        public float TargetRow { get; internal set; }
        public float FlightRemaining { get; internal set; }
        public float LaunchX { get; internal set; }
        public float LaunchRow { get; internal set; }
    }
    public sealed partial class AdventureBoard
    {
        public IReadOnlyList<PlantKind> AvailableSeeds { get; internal set; }
        public PlantKind? ImitaterTarget { get; internal set; }
        public int? SelectedCobCannonId { get; internal set; }
        public int MoneyCollectedUnits { get; internal set; }
        public long MoneyDisplayValue { get { return (long)MoneyCollectedUnits*10; } }
        internal int PlantId;
        internal int CoinId;
        internal readonly List<AdventureCoin> coins=new List<AdventureCoin>();
        internal readonly List<AdventureCobShell> cobShells=new List<AdventureCobShell>();
        public IReadOnlyList<AdventureCoin> Coins { get { return coins.AsReadOnly(); } }
        public IReadOnlyList<AdventureCobShell> CobShells { get { return cobShells.AsReadOnly(); } }
    }
}
namespace PvZReplica
{
    public sealed partial class GameRules
    {
        /// <summary>Null leaves the first-adventure unlocks and loadouts unchanged. Captured at construction.</summary>
        public AdventureSeedAccess AdventureSeedAccess;
    }
    public sealed partial class Plant
    {
        public int Id;
        public PlantKind? ImitaterTarget;
        public bool IsImitated;
        public float ImitaterTransformRemaining;
        public int UpgradeBurstTicks;
        public CobCannonPhase CannonPhase;
        public float CannonReloadRemaining;
        public float CannonTargetX;
        public float CannonTargetRow;
        public int VehicleHitsTaken;
    }
    public sealed partial class Pea
    {
        public bool Homing;
        public int TargetZombieId;
        public bool ChillSplash;
    }
    public sealed partial class GameSimulation
    {
        private void InitializeAdventureSeedAccess()
        {
            var values=new List<PlantKind>();foreach(var k in Adventure.Level.UnlockedSeeds)values.Add(k);
            var access=Rules.AdventureSeedAccess;
            if(access!=null)
            {
                foreach(var k in access.AdditionalSeeds)if(!values.Contains(k))values.Add(k);
                if(access.ImitaterTarget.HasValue)
                {
                    if(!ContainsSeed(Adventure.Level.UnlockedSeeds,access.ImitaterTarget.Value))throw new ArgumentException("Imitater target is not yet earned on this level.");
                    Adventure.ImitaterTarget=access.ImitaterTarget;
                }
            }
            Adventure.AvailableSeeds=Array.AsReadOnly(values.ToArray());
        }
        public bool SetAdventureImitaterTarget(PlantKind target)
        {
            if(!IsAdventure || State!=MatchState.Playing || Adventure.Phase!=AdventurePhase.ChoosingSeeds ||
                !ContainsSeed(Adventure.AvailableSeeds,PlantKind.Imitater) || (int)target<0 || (int)target>39 || !ContainsSeed(Adventure.Level.UnlockedSeeds,target))return false;
            Adventure.ImitaterTarget=target;return true;
        }
        public int AdventureSeedCost(PlantKind kind)
        {return Cost(ResolveAdventureSeed(kind));}
        public float AdventureSeedRechargeTime(PlantKind kind)
        {return RechargeTime(ResolveAdventureSeed(kind));}
        private PlantKind ResolveAdventureSeed(PlantKind kind)
        {
            if(kind!=PlantKind.Imitater)return kind;
            if(!IsAdventure || !Adventure.ImitaterTarget.HasValue)throw new InvalidOperationException("Select an earned basic plant as the Imitater target first.");
            return Adventure.ImitaterTarget.Value;
        }
        private static PlantKind? UpgradePrerequisite(PlantKind kind)
        {
            switch(kind)
            {
                case PlantKind.GatlingPea:return PlantKind.Repeater;
                case PlantKind.TwinSunflower:return PlantKind.Sunflower;
                case PlantKind.GloomShroom:return PlantKind.FumeShroom;
                case PlantKind.Cattail:return PlantKind.LilyPad;
                case PlantKind.WinterMelon:return PlantKind.MelonPult;
                case PlantKind.GoldMagnet:return PlantKind.MagnetShroom;
                case PlantKind.Spikerock:return PlantKind.Spikeweed;
                case PlantKind.CobCannon:return PlantKind.KernelPult;
                default:return null;
            }
        }
        private AdventurePlantingFailure CheckAdventureUpgrade(PlantKind kind,int row,int column)
        {
            PlantKind? prerequisite=UpgradePrerequisite(kind);
            if(!prerequisite.HasValue)return AdventurePlantingFailure.NeedsUpgrade;
            var l=Adventure.Level;
            if(kind==PlantKind.Cattail)
            {
                Plant lily=AdventurePlantAt(row,column,PlantLayer.Support);
                return l.IsWaterRow(row) && lily!=null && lily.Kind==PlantKind.LilyPad && AdventurePlantAt(row,column,PlantLayer.Main)==null ? AdventurePlantingFailure.None:AdventurePlantingFailure.NeedsUpgrade;
            }
            Plant source=AdventurePlantAt(row,column,PlantLayer.Main);
            if(source==null || source.Column!=column || source.Kind!=prerequisite.Value)return AdventurePlantingFailure.NeedsUpgrade;
            if(kind==PlantKind.Spikerock && (l.IsWaterRow(row) || l.HasRoof || AdventurePlantAt(row,column,PlantLayer.Support)!=null))return AdventurePlantingFailure.RoofRestriction;
            if(kind==PlantKind.CobCannon)
            {
                if(column>=Columns-1)return AdventurePlantingFailure.CannonFootprint;
                Plant right=AdventurePlantAt(row,column+1,PlantLayer.Main);
                if(right==null || right.Kind!=PlantKind.KernelPult || right.Column!=column+1 ||
                    AdventurePlantAt(row,column,PlantLayer.Armor)!=null || AdventurePlantAt(row,column+1,PlantLayer.Armor)!=null)return AdventurePlantingFailure.NeedsUpgrade;
                Plant leftPot=AdventurePlantAt(row,column,PlantLayer.Support),rightPot=AdventurePlantAt(row,column+1,PlantLayer.Support);
                bool a=leftPot!=null && leftPot.Kind==PlantKind.FlowerPot,b=rightPot!=null && rightPot.Kind==PlantKind.FlowerPot;
                if(a!=b)return AdventurePlantingFailure.CannonFootprint;
            }
            return AdventurePlantingFailure.None;
        }
        private Plant PlaceAdventureUpgrade(PlantKind kind,int row,int column)
        {
            if(kind==PlantKind.Imitater)
            {
                PlantKind target=ResolveAdventureSeed(kind);
                var mimic=new Plant {Id=++Adventure.PlantId,Kind=kind,Row=row,Column=column,Health=300f,
                    Layer=target==PlantKind.CoffeeBean?PlantLayer.Effect:LayerFor(target),ImitaterTarget=target,ImitaterTransformRemaining=3.2f,ActionTimer=float.MaxValue};
                Plants.Add(mimic);Raise(GameEvent.Planted);return mimic;
            }
            Plant source=AdventurePlantAt(row,column,kind==PlantKind.Cattail?PlantLayer.Support:PlantLayer.Main);
            bool asleep=source!=null && source.Asleep,coffee=source!=null && source.AwakeByCoffee;
            float wake=source==null?0:source.WakeTimer;
            if(source!=null)Plants.Remove(source);
            if(kind==PlantKind.CobCannon){Plant right=AdventurePlantAt(row,column+1,PlantLayer.Main);if(right!=null)Plants.Remove(right);}
            var result=new Plant{Id=++Adventure.PlantId,Kind=kind,Row=row,Column=column,Layer=PlantLayer.Main,
                Health=kind==PlantKind.Spikerock?450f:300f,ColumnSpan=kind==PlantKind.CobCannon?2:1,
                Asleep=kind==PlantKind.GloomShroom && asleep,AwakeByCoffee=kind==PlantKind.GloomShroom && coffee,WakeTimer=kind==PlantKind.GloomShroom?wake:0,
                ActionTimer=kind==PlantKind.TwinSunflower?Math.Max(0.01f,source==null?5f:source.ActionTimer):0.5f,
                CannonPhase=CobCannonPhase.Arming,CannonReloadRemaining=kind==PlantKind.CobCannon?5f:0};
            Plants.Add(result);Raise(GameEvent.Planted);return result;
        }
    }
}
