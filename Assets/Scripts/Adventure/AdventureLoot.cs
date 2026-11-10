using System;
using System.Collections.Generic;
using PvZReplica.Adventure;
using PvZReplica.VerifiedData;
namespace PvZReplica.Adventure
{
    public sealed partial class AdventureBoard
    {
        public int TotalWavesSpawned { get; internal set; }
        internal bool DroppedFirstCoin, DroppedModeGift;
    }
}
namespace PvZReplica
{
    public sealed partial class GameSimulation
    {
        // PC Board.DropLootPiece 0x4289C0: 30000 tickets; +14/+250/+2500 currency cutoffs.
        private void DropAdventureLoot(Zombie z)
        {
            var a=Adventure;if(Rules.AdventureDisableLoot||(ModeSession==null&&!a.Level.IsReplay&&a.Level.Number<11)||z.BungeeCarrier)return;
            if(ModeSession==null&&!a.Level.IsReplay&&a.CurrentWave>5&&!a.DroppedModeGift&&
                (a.Level.Number==22&&!Rules.AdventureMinigamesUnlocked||a.Level.Number==36&&!Rules.AdventurePuzzleUnlocked))
            {
                SpawnAdventureCoin(a.Level.Number==22?AdventureCoinKind.MinigameUnlock:AdventureCoinKind.PuzzleUnlock,Math.Max(.1f,Math.Min(8.8f,z.X)),z.Row);a.DroppedModeGift=true;return;
            }
            if(a.Level.Mode==AdventureMode.LittleTrouble&&random.Next(4)!=0)return;
            float x=Math.Max(.1f,Math.Min(8.8f,z.X));
            if(z.Kind==ZombieKind.Yeti){for(int i=0;i<4;i++)SpawnAdventureCoin(AdventureCoinKind.Diamond,Math.Max(.1f,x-i*.125f),z.Row);return;}
            int factor=Math.Max(0,VerifiedDataCatalog.Zombies[(int)z.Kind].WaveBudgetCostRaw);if(factor==0)return;
            int roll=random.Next(30000);
            if(ModeSession==null&&!a.Level.IsReplay&&a.Level.Number==11&&!a.DroppedFirstCoin&&a.CurrentWave>5)roll=1000;
            if(ModeSession!=null&&ModeSession.Definition.Mechanic==Modes.ModeMechanic.Columns)roll*=5;
            if(a.Level.Mode==AdventureMode.WhackAZombie)
            {
                int cutoff=SunAmount>500?2800:SunAmount>350?3100:SunAmount>200?3700:5000;
                if(roll>=2500*factor&&roll<cutoff*factor){for(int i=0;i<3;i++)Suns.Add(new Sun{X=x-i*.25f,Y=z.Row,TargetY=z.Row+.3f});return;}
            }
            if(a.TotalWavesSpawned>70)return;
            bool endless=ModeSession!=null&&ModeSession.Definition.Survival&&ModeSession.Definition.Endless;
            int plant=Rules.AdventurePottedLoot?(endless?3:a.Level.IsReplay?24:12):0;
            int chocolate=plant+(Rules.AdventureChocolateLoot?(endless?9:a.Level.IsReplay?72:36):0);
            AdventureCoinKind kind;
            if(roll<plant*factor)kind=AdventureCoinKind.PlantGift;
            else if(roll<chocolate*factor)kind=AdventureCoinKind.Chocolate;
            else if(roll<(chocolate+14)*factor)kind=Rules.AdventurePacketSlots>6?AdventureCoinKind.Diamond:AdventureCoinKind.Gold;
            else if(roll<(chocolate+250)*factor)kind=AdventureCoinKind.Gold;
            else if(roll<(chocolate+2500)*factor)kind=AdventureCoinKind.Silver;
            else return;
            // Original bowling coins come from ricochets rather than every zombie death.
            if(a.Level.Mode==AdventureMode.Bowling&&kind<=AdventureCoinKind.Diamond)return;
            var coin=SpawnAdventureCoin(kind,x,z.Row);if(coin==null)return;
            if(kind==AdventureCoinKind.PlantGift)coin.GiftPlant=PickAdventureGardenGift();a.DroppedFirstCoin=true;
        }
        private PlantKind PickAdventureGardenGift()
        {
            if(ModeSession!=null&&ModeSession.Definition.Endless){var all=new List<PlantKind>();for(int i=0;i<40;i++)if(i!=33&&i!=38)all.Add((PlantKind)i);return all[random.Next(all.Count)];}
            int[][] plants={new[]{0,1,2,3,7,4,5,6},new[]{8,9,10,11,12,13,14,15},new[]{16,17,18,19,20,21,22,23},new[]{24,25,26,27,28,29,30,31},new[]{32,34,35,36,37,39}};
            int index=(int)Adventure.Level.Terrain;if(index>=5)index=4;var list=plants[index];return (PlantKind)list[random.Next(list.Length)];
        }
    }
}
