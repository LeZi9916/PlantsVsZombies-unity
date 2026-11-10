using System;
using System.Collections.Generic;
using PvZReplica.Adventure;

namespace PvZReplica
{
    public sealed partial class GameSimulation
    {
        partial void AdvanceAdventureSunMotion(float dt,ref bool handled){handled=true;AdvanceAdventurePresentationSuns(dt);}
        partial void CollectAdventureSunMotion(Sun sun,ref bool handled,ref bool collected){handled=true;collected=TryCollectAdventurePresentationSun(sun); }
        private void InitializeReplaySeedChoices()
        {
            if(!Adventure.Level.IsReplay||!Adventure.Level.ChoosesSeeds)return;
            var level=Adventure.Level;var candidates=new List<PlantKind>();bool balloons=false,roofRaid=false;
            foreach(var wave in Adventure.Waves)foreach(var z in wave.Zombies){if(z==ZombieKind.Balloon)balloons=true;if(z==ZombieKind.Bungee||z==ZombieKind.Catapult)roofRaid=true;}
            foreach(var kind in level.UnlockedSeeds)
            {
                if(!level.IsNight&&Nocturnal(kind)||level.IsNight&&kind==PlantKind.CoffeeBean)continue;
                if(kind==PlantKind.GraveBuster&&level.Terrain!=AdventureTerrain.Night||kind==PlantKind.Plantern&&level.Terrain!=AdventureTerrain.Fog||kind==PlantKind.FlowerPot&&!level.HasRoof)continue;
                if(!level.HasPool&&(kind==PlantKind.LilyPad||kind==PlantKind.TangleKelp||kind==PlantKind.SeaShroom))continue;
                if(level.HasRoof&&(kind==PlantKind.Spikeweed||kind==PlantKind.Torchwood))continue;
                if(kind==PlantKind.UmbrellaLeaf&&!roofRaid||kind==PlantKind.Blover&&!balloons&&level.Terrain!=AdventureTerrain.Fog)continue;
                candidates.Add(kind);
            }
            var rng=new PvZReplica.Randomness.OriginalRandom(unchecked((uint)Rules.AdventureDaveSeed));var required=new List<PlantKind>();
            for(int i=0;i<3&&candidates.Count>0;i++){int pick=(int)rng.NextBounded((uint)candidates.Count);required.Add(candidates[pick]);candidates.RemoveAt(pick);}
            Adventure.RequiredSeeds=required.AsReadOnly();var defaults=new List<PlantKind>(required);
            foreach(var k in level.DefaultSeeds)if(defaults.Count<level.PacketSlots&&!defaults.Contains(k))defaults.Add(k);
            foreach(var k in Adventure.AvailableSeeds)if(defaults.Count<level.PacketSlots&&!defaults.Contains(k)&&k!=PlantKind.Imitater)defaults.Add(k);
            Adventure.SelectedSeeds=defaults.AsReadOnly();
        }
        public bool BankAdventureCoins()
        {
            if(!IsAdventure)return false;
            foreach(var coin in Adventure.coins.ToArray())if(!FinishAdventureCoin(coin))return false;
            return true;
        }
    }
    public sealed partial class GameRules
    {
        public int AdventureCycle=1;
        public int AdventureDaveSeed=12345;
        public int AdventurePacketSlots=6;
        public Func<int,bool> AdventureCoinCommit;
        public bool AdventureFirstAid;
        public bool AdventurePottedLoot,AdventureChocolateLoot,AdventureDisableLoot;
        public bool AdventureMinigamesUnlocked,AdventurePuzzleUnlocked;
        public Func<AdventureCoin,bool> AdventureLootCommit;
        public Func<bool> AdventurePlaceRake;
    }
}
namespace PvZReplica.Adventure
{
    public sealed partial class AdventureBoard
    {
        public IReadOnlyList<PlantKind> RequiredSeeds { get; internal set; } = Array.AsReadOnly(new PlantKind[0]);
    }
}
