using System;
using PvZReplica.Adventure;

namespace PvZReplica
{
    public enum AdventurePlantingFailure { None, NotPlaying, InvalidCell, SeedUnavailable, Cooldown, Sun,
        Occupied, NeedsLilyPad, NeedsFlowerPot, NeedsWater, RoofRestriction, Grave, Crater, Ice, NeedsSleepingMushroom, Tutorial, BowlingLine, NeedsUpgrade, CannonFootprint, ImitaterTarget }
    public sealed partial class GameSimulation
    {
        public AdventurePlantingFailure LastAdventurePlantingFailure { get; private set; }
        public Plant AdventurePlantAt(int row, int column, PlantLayer layer)
        { return Plants.Find(p => p.Row == row && p.Column <= column && column < p.Column + p.ColumnSpan && p.Layer == layer); }
        partial void PlantAdventure(PlantKind kind, int row, int column, ref bool handled, ref bool planted)
        {
            handled = true;
            if (Adventure.Level.HasConveyor || Adventure.Level.Mode == AdventureMode.Vasebreaker)
            {
                var packet = Adventure.packets.Find(p => p.Kind == kind);
                planted = packet != null && TryUseAdventurePacket(packet.Id, row, column);
                if (packet == null) LastAdventurePlantingFailure = AdventurePlantingFailure.SeedUnavailable;
                return;
            }
            LastAdventurePlantingFailure = CheckAdventurePlanting(kind, row, column, false);
            if (LastAdventurePlantingFailure != AdventurePlantingFailure.None) return;
            if (!SpendAdventureSun(AdventureSeedCost(kind))) return;
            seedCooldownTicks[(int)kind] = DefinitionFor(ResolveAdventureSeed(kind)).RechargeThresholdRaw + 1;
            PlaceAdventurePlant(kind, row, column);
            planted = true;
        }
        public AdventurePlantingFailure CanPlantAdventure(PlantKind kind, int row, int column)
        { return !IsAdventure ? AdventurePlantingFailure.NotPlaying : CheckAdventurePlanting(kind,row,column,Adventure.Level.HasConveyor || Adventure.Level.Mode == AdventureMode.Vasebreaker); }
        public bool TryUseAdventurePacket(int packetId, int row, int column)
        {
            if (!IsAdventure) return false;
            var p = Adventure.packets.Find(x => x.Id == packetId);
            if (p == null) return false;
            LastAdventurePlantingFailure = CheckAdventurePlanting(p.Kind,row,column,true);
            if (LastAdventurePlantingFailure != AdventurePlantingFailure.None) return false;
            if (Adventure.Level.Mode == AdventureMode.Bowling)
                Adventure.nuts.Add(new AdventureBowlingNut { Row = row, X = column + 0.5f, Explosive = p.Kind == PlantKind.ExplodeONut });
            else PlaceAdventurePlant(p.Kind,row,column);
            RemoveAdventurePacket(p);
            return true;
        }
        private AdventurePlantingFailure CheckAdventurePlanting(PlantKind kind, int row, int column, bool packet)
        {
            var a = Adventure; var l = a.Level;
            if (State != MatchState.Playing || a.Phase != AdventurePhase.Playing) return AdventurePlantingFailure.NotPlaying;
            if (!l.IsLawnRow(row) || column < 0 || column >= Columns || !Enum.IsDefined(typeof(PlantKind),kind)) return AdventurePlantingFailure.InvalidCell;
            if (l.Number == 5 && a.Tutorial == AdventureTutorial.Shovel) return AdventurePlantingFailure.Tutorial;
            if (l.Mode == AdventureMode.Bowling)
                return packet && (kind == PlantKind.Wallnut || kind == PlantKind.ExplodeONut) ? column < 3 ? AdventurePlantingFailure.None : AdventurePlantingFailure.BowlingLine : AdventurePlantingFailure.SeedUnavailable;
            if(kind==PlantKind.Imitater && !a.ImitaterTarget.HasValue)return AdventurePlantingFailure.ImitaterTarget;
            bool whackSeed = l.Mode == AdventureMode.WhackAZombie && (kind == PlantKind.CherryBomb || kind == PlantKind.GraveBuster || kind == PlantKind.PotatoMine);
            if (!packet && !whackSeed && !ContainsSeed(a.SelectedSeeds,kind)) return AdventurePlantingFailure.SeedUnavailable;
            if (!packet && seedCooldownTicks[(int)kind] > 0) return AdventurePlantingFailure.Cooldown;
            if (!packet && AdventureSunAvailable < AdventureSeedCost(kind)) return AdventurePlantingFailure.Sun;
            kind=ResolveAdventureSeed(kind);
            if (!l.IsReplay && (l.Number == 1 && a.Tutorial == AdventureTutorial.ChooseSeed ||
                l.Number == 2 && a.PlantedSunflowers < 2 && kind != PlantKind.Sunflower ||
                l.Number == 5 && a.Tutorial == AdventureTutorial.Shovel)) return AdventurePlantingFailure.Tutorial;
            if (a.vases.Exists(v => v.Row == row && v.Column == column)) return AdventurePlantingFailure.Occupied;
            if (a.craters.Exists(v => v.Row == row && v.Column == column)) return AdventurePlantingFailure.Crater;
            if (a.ice.Exists(v => v.Row == row && v.Column == column)) return AdventurePlantingFailure.Ice;
            bool grave = a.graves.Exists(g => g.Row == row && g.Column == column);
            if (kind == PlantKind.GraveBuster) return grave && AdventurePlantAt(row,column,PlantLayer.Main) == null ? AdventurePlantingFailure.None : AdventurePlantingFailure.Grave;
            if (grave) return AdventurePlantingFailure.Grave;
            if(IsAdventureFirstAid(kind,row,column))return AdventurePlantingFailure.None;
            Plant main = AdventurePlantAt(row,column,PlantLayer.Main);
            if(UpgradePrerequisite(kind).HasValue)return CheckAdventureUpgrade(kind,row,column);
            if(kind==PlantKind.Pumpkin && main!=null && main.Kind==PlantKind.CobCannon)return AdventurePlantingFailure.CannonFootprint;
            if (kind == PlantKind.CoffeeBean) return main != null && main.Asleep && main.WakeTimer <= 0 && AdventurePlantAt(row,column,PlantLayer.Effect)==null ? AdventurePlantingFailure.None : AdventurePlantingFailure.NeedsSleepingMushroom;
            bool aquatic = kind == PlantKind.LilyPad || kind == PlantKind.TangleKelp || kind == PlantKind.SeaShroom;
            if (aquatic && !l.IsWaterRow(row)) return AdventurePlantingFailure.NeedsWater;
            if (l.HasRoof && kind == PlantKind.Spikeweed) return AdventurePlantingFailure.RoofRestriction;
            PlantLayer layer = LayerFor(kind);
            if (AdventurePlantAt(row,column,layer) != null) return AdventurePlantingFailure.Occupied;
            if (l.IsWaterRow(row) && kind == PlantKind.PotatoMine) return AdventurePlantingFailure.NeedsWater;
            Plant support=AdventurePlantAt(row,column,PlantLayer.Support);
            if (l.IsWaterRow(row) && !aquatic && !(kind==PlantKind.Pumpkin && main!=null && main.Kind==PlantKind.Cattail) && (support==null || support.Kind!=PlantKind.LilyPad)) return AdventurePlantingFailure.NeedsLilyPad;
            if (kind == PlantKind.LilyPad && main != null) return AdventurePlantingFailure.Occupied;
            if (kind == PlantKind.FlowerPot && (l.IsWaterRow(row) || main != null)) return AdventurePlantingFailure.Occupied;
            if (l.HasRoof && kind != PlantKind.FlowerPot && (support==null || support.Kind!=PlantKind.FlowerPot)) return AdventurePlantingFailure.NeedsFlowerPot;
            return AdventurePlantingFailure.None;
        }
        private static PlantLayer LayerFor(PlantKind kind)
        { return kind == PlantKind.LilyPad || kind == PlantKind.FlowerPot ? PlantLayer.Support : kind == PlantKind.Pumpkin ? PlantLayer.Armor : PlantLayer.Main; }
        private static bool Nocturnal(PlantKind k)
        { return (int)k >= 8 && (int)k <= 15 && k != PlantKind.GraveBuster || k == PlantKind.SeaShroom || k == PlantKind.MagnetShroom || k == PlantKind.GloomShroom; }
        private Plant PlaceAdventurePlant(PlantKind kind, int row, int column)
        {
            if((int)kind>=40 && (int)kind<=48)
            {var upgraded=PlaceAdventureUpgrade(kind,row,column);Adventure.Notice(AdventureNoticeKind.UpgradePlanted,(int)kind,row,column);return upgraded;}
            if (kind == PlantKind.CoffeeBean)
            {
                var target = AdventurePlantAt(row,column,PlantLayer.Main);
                if (target != null) target.WakeTimer = 2f;
                Raise(GameEvent.Planted); return target;
            }
            if(IsAdventureFirstAid(kind,row,column)){var old=AdventurePlantAt(row,column,kind==PlantKind.Pumpkin?PlantLayer.Armor:PlantLayer.Main);Plants.Remove(old);}
            var plant = new Plant { Id = ++Adventure.PlantId, Kind = kind, Row = row, Column = column, Layer = LayerFor(kind),
                Health = kind == PlantKind.Wallnut || kind == PlantKind.Pumpkin ? 4000f : kind == PlantKind.TallNut ? 8000f : kind == PlantKind.Garlic ? 400f : 300f,
                Asleep = Nocturnal(kind) && !Adventure.Level.IsNight,
                ActionTimer = kind == PlantKind.PotatoMine ? 15f : kind == PlantKind.CherryBomb || kind == PlantKind.Jalapeno || kind == PlantKind.DoomShroom || kind == PlantKind.IceShroom ? 1f :
                    kind == PlantKind.GraveBuster ? 4.5f : kind == PlantKind.Sunflower || kind == PlantKind.SunShroom ? 5f + (float)random.NextDouble() * 2.5f : 0.5f,
                IsSmall = false };
            InitializeAdventureNativePlantTiming(plant);
            Plants.Add(plant);
            if (kind == PlantKind.Sunflower)
            {
                Adventure.PlantedSunflowers++;
                if (!Adventure.Level.IsReplay && Adventure.Level.Number == 2) Adventure.Tutorial = Adventure.PlantedSunflowers >= 2 ? AdventureTutorial.Complete : AdventureTutorial.RechargeSunflower;
            }
            if (!Adventure.Level.IsReplay && Adventure.Level.Number == 1) Adventure.Tutorial = Adventure.CollectedSun ? AdventureTutorial.Complete : AdventureTutorial.CollectSun;
            if (Adventure.Tutorial == AdventureTutorial.PoolSupport && kind == PlantKind.LilyPad ||
                Adventure.Tutorial == AdventureTutorial.RoofPots && kind == PlantKind.CabbagePult ||
                Adventure.Tutorial == AdventureTutorial.NightMushrooms && Nocturnal(kind)) Adventure.Tutorial = AdventureTutorial.Complete;
            Raise(GameEvent.Planted);
            return plant;
        }
        partial void RemoveAdventurePlant(int row, int column, ref bool handled, ref bool removed)
        {
            handled = true;
            if (Adventure.Phase != AdventurePhase.Playing || !Adventure.Level.IsReplay && Adventure.Level.Number < 5 || Adventure.Level.Mode == AdventureMode.WhackAZombie) return;
            Plant p = AdventurePlantAt(row,column,PlantLayer.Effect) ?? AdventurePlantAt(row,column,PlantLayer.Armor) ?? AdventurePlantAt(row,column,PlantLayer.Main) ?? AdventurePlantAt(row,column,PlantLayer.Support);
            if (p == null) return;
            RemoveAdventurePlantAndStack(p);
            removed = true;
            if (Adventure.Tutorial == AdventureTutorial.Shovel && !Plants.Exists(x => x.Row == 2 && x.Column >= 3 && x.Column <= 5 && x.Kind == PlantKind.Peashooter))
            { Adventure.RemovalTutorialDone = true; Adventure.Tutorial = AdventureTutorial.Complete; }
        }
        private void RemoveAdventurePlantAndStack(Plant p)
        {
            Plants.Remove(p);
            if (p.Layer == PlantLayer.Support) Plants.RemoveAll(x => x.Row == p.Row && x.Column <= p.Column && p.Column < x.Column+x.ColumnSpan);
            if(Adventure.SelectedCobCannonId.HasValue && !Plants.Exists(x=>x.Id==Adventure.SelectedCobCannonId.Value))Adventure.SelectedCobCannonId=null;
        }
        public bool IsAdventureCellVisible(int row, int column)
        {
            if (!IsAdventure || !Adventure.Level.IsLawnRow(row) || column < 0 || column >= 9) return false;
            if (Adventure.Level.Mode == AdventureMode.Storm && !Adventure.StormFlash) return column < 2;
            if (Adventure.Level.Terrain != AdventureTerrain.Fog || Adventure.FogBlownRemaining > 0f || column < 4) return true;
            return Plants.Exists(p => p.Kind == PlantKind.Plantern && Math.Abs(p.Row-row) <= 1 && Math.Abs(p.Column-column) <= 2);
        }
    }
}
