using System;
using System.Collections.Generic;
using PvZReplica.VerifiedData;
using OriginalTiming = PvZUnity.Timing;

namespace PvZReplica
{
    public enum PlantKind
    {
        Peashooter = 0, Sunflower = 1, CherryBomb = 2, Wallnut = 3,
        PotatoMine = 4, SnowPea = 5, Chomper = 6, Repeater = 7,
        PuffShroom = 8, SunShroom = 9, FumeShroom = 10, GraveBuster = 11, HypnoShroom = 12,
        ScaredyShroom = 13, IceShroom = 14, DoomShroom = 15, LilyPad = 16, Squash = 17,
        Threepeater = 18, TangleKelp = 19, Jalapeno = 20, Spikeweed = 21, Torchwood = 22, TallNut = 23,
        SeaShroom = 24, Plantern = 25, Cactus = 26, Blover = 27, SplitPea = 28, Starfruit = 29,
        Pumpkin = 30, MagnetShroom = 31, CabbagePult = 32, FlowerPot = 33, KernelPult = 34,
        CoffeeBean = 35, Garlic = 36, UmbrellaLeaf = 37, Marigold = 38, MelonPult = 39,
        GatlingPea = 40, TwinSunflower = 41, GloomShroom = 42, Cattail = 43, WinterMelon = 44,
        GoldMagnet = 45, Spikerock = 46, CobCannon = 47, Imitater = 48, ExplodeONut = 49
    }
    public enum MinePhase { Buried, Rising, Armed }
    public enum ZombieKind { Normal = 0, Flag = 1, Conehead = 2, PoleVaulter = 3, Buckethead = 4,
        Newspaper = 5, ScreenDoor = 6, Football = 7, Dancing = 8, BackupDancer = 9, DuckyTube = 10,
        Snorkel = 11, Zomboni = 12, Bobsled = 13, DolphinRider = 14, JackInTheBox = 15, Balloon = 16,
        Digger = 17, Pogo = 18, Yeti = 19, Bungee = 20, Ladder = 21, Catapult = 22, Gargantuar = 23,
        Imp = 24, Boss = 25 }
    public enum MatchState { Playing, Paused, Won, Lost }
    public enum GameEvent { Planted, Shoot, SunCollected, ZombieHit, ZombieDied, Mower, Explosion, MineExplosion, JalapenoExplosion, Won, Lost, SunPickupStarted }

    public sealed partial class GameRules
    {
        /// <summary>Opt in to the incomplete, explicitly approximate 1-1 experiment.</summary>
        public bool EnableExperimentalLevelOne = false;
        /// <summary>1..50 opts into the separate Adventure module; 0 retains the legacy demo.</summary>
        public int AdventureLevel = 0;
        public bool AdventureRoofCleaners = false;
        public bool AdventurePoolCleaners = false;
        public int StartingSun = 150;
        public int ZombieCount = 12;
        public float FirstZombieDelay = 15f;
        public float ZombieInterval = 9f;
        public float SkySunInterval = 9f;
        public float ZombieSpeed = 0.18f;
        public bool MowersEnabled = true;
    }

    public sealed partial class Plant
    {
        public int ColumnSpan = 1;
        public PlantKind Kind;
        public int Row;
        public int Column;
        public float Health;
        public float ActionTimer;
        public float Age;
        public float ShootAge = -1f;
        public MinePhase MinePhase;
        public float PhaseAge;
        public OriginalTiming.PlantTimingState? Timing;
    }

    public sealed partial class Zombie
    {
        public ZombieKind Kind;
        public int Row;
        public float X;
        public float Health;
        public float ArmorHealth;
        public float ShieldHealth;
        public float BiteTimer;
        public float Age;
        public float ChillRemaining;
        public bool Eating;
    }

    public sealed partial class Pea
    {
        /// <summary>Plant kind captured at creation; visual metadata, independent of current elemental flags.</summary>
        public PlantKind SourceKind { get; internal set; } = PlantKind.Peashooter;
        public bool SourceIsImitated { get; internal set; }
        public int Row;
        public float X;
        public bool IsSnow;
    }

    public sealed partial class Sun
    {
        public int Value = 25;
        public float X;
        public float Y;
        public float TargetY;
        public float Age;
    }

    public sealed class Mower
    {
        public int Row;
        public float X;
        public bool Available = true;
        public bool Active;
    }

    public sealed class LaneFlame
    {
        public int Row;
        public float Age;
    }

    public sealed partial class GameSimulation
    {
        public const int Rows = 5;
        public const int Columns = 9;

        public readonly List<Plant> Plants = new List<Plant>();
        public readonly List<Zombie> Zombies = new List<Zombie>();
        public readonly List<Pea> Peas = new List<Pea>();
        public readonly List<Sun> Suns = new List<Sun>();
        public readonly List<Mower> Mowers = new List<Mower>();
        public readonly List<LaneFlame> Flames = new List<LaneFlame>();

        public readonly GameRules Rules;
        public MatchState State { get; private set; }
        public int SunAmount { get; private set; }
        public int SpawnedZombies { get; private set; }
        public float Elapsed { get; private set; }
        /// <summary>The mode selected at construction; changing the rules later does not switch modes.</summary>
        public bool IsExperimentalLevelOne { get; private set; }
        public bool IsAdventure { get; private set; }
        public int RowCount { get; private set; } = Rows;
        public Action<GameEvent> EventRaised;

        private readonly Random random;
        private readonly int[] seedCooldownTicks = new int[VerifiedDataCatalog.Plants.Count];
        private float nextZombie;
        private float nextSkySun;
        private double accumulatedTime;
        private const double SimulationStep = 0.01;

        partial void InitializeExperimentalLevelOne(ref bool initialized);
        partial void CanPlantExperimentalLevelOne(PlantKind kind, int row, int column, ref bool allowed);
        partial void CanRemoveExperimentalLevelOnePlant(ref bool allowed);
        partial void OnExperimentalLevelOnePlantPlaced();
        partial void OnExperimentalLevelOneSunCollected();
        partial void BeforeExperimentalLevelOneTick();
        partial void AdvanceExperimentalLevelOneNativeSkySun(ref bool handled);
        partial void CanDropExperimentalLevelOneSkySun(ref bool allowed);
        partial void SpawnExperimentalLevelOneSkySun(ref bool handled);
        partial void AdvanceExperimentalLevelOneWaves();
        partial void CheckExperimentalLevelOneCompletion();

        partial void InitializeAdventure(ref bool initialized);
        partial void AdvanceAdventure(float dt, ref bool handled);
        partial void PlantAdventure(PlantKind kind, int row, int column, ref bool handled, ref bool planted);
        partial void RemoveAdventurePlant(int row, int column, ref bool handled, ref bool removed);
        partial void CollectAdventureSun();
        partial void AdvanceAdventureSunMotion(float dt,ref bool handled);
        partial void CollectAdventureSunMotion(Sun sun,ref bool handled,ref bool collected);

        public GameSimulation(GameRules rules = null, int randomSeed = 12345)
        {
            Rules = rules ?? new GameRules();
            IsExperimentalLevelOne = Rules.EnableExperimentalLevelOne;
            IsAdventure = Rules.AdventureLevel != 0;
            if (IsAdventure && IsExperimentalLevelOne)
                throw new ArgumentException("Adventure and the legacy 1-1 experiment are separate modes.");
            random = new PvZReplica.Randomness.OriginalRandomAdapter(unchecked((uint)randomSeed));
            State = MatchState.Playing;
            SunAmount = Rules.StartingSun;
            nextZombie = Rules.FirstZombieDelay;
            nextSkySun = Rules.SkySunInterval;
            for (int row = 0; row < Rows; row++)
                Mowers.Add(new Mower { Row = row, X = -0.5f, Available = Rules.MowersEnabled });
            if (IsAdventure)
            {
                bool initialized = false;
                InitializeAdventure(ref initialized);
                if (!initialized) throw new InvalidOperationException("Adventure requires the Adventure module.");
            }
            if (IsExperimentalLevelOne)
            {
                bool initialized = false;
                InitializeExperimentalLevelOne(ref initialized);
                if (!initialized)
                    throw new InvalidOperationException("Experimental level 1-1 requires the Levels module.");
            }
        }

        public static int Cost(PlantKind kind)
        {
            return DefinitionFor(kind).SunCost;
        }

        public static float RechargeTime(PlantKind kind)
        {
            return (float)((DefinitionFor(kind).RechargeThresholdRaw + 1) * SimulationStep);
        }

        private static PlantDefinition DefinitionFor(PlantKind kind)
        {
            PlantDefinition definition;
            if (!Enum.IsDefined(typeof(PlantKind), kind) || !VerifiedDataCatalog.TryGetPlant((int)kind, out definition))
                throw new ArgumentOutOfRangeException(nameof(kind));
            return definition;
        }

        public float CooldownRemaining(PlantKind kind)
        {
            DefinitionFor(kind);
            return (float)(seedCooldownTicks[(int)kind] * SimulationStep);
        }

        public Plant PlantAt(int row, int column)
        {
            return Plants.FindLast(p => p.Row == row && p.Column <= column && column < p.Column + p.ColumnSpan);
        }

        public bool TryPlant(PlantKind kind, int row, int column)
        {
            if (IsAdventure)
            {
                bool handled = false, planted = false;
                PlantAdventure(kind, row, column, ref handled, ref planted);
                return handled && planted;
            }
            // Expanded IDs belong to the opt-in Adventure module. The legacy demo still implements its original nine plants.
            if (kind != PlantKind.Peashooter && kind != PlantKind.Sunflower && kind != PlantKind.CherryBomb && kind != PlantKind.Wallnut &&
                kind != PlantKind.PotatoMine && kind != PlantKind.SnowPea && kind != PlantKind.Repeater && kind != PlantKind.Jalapeno && kind != PlantKind.TallNut) return false;
            int index = (int)kind;
            if (index < 0 || index >= seedCooldownTicks.Length || !Enum.IsDefined(typeof(PlantKind), kind) || State != MatchState.Playing ||
                row < 0 || row >= Rows || column < 0 || column >= Columns ||
                SunAmount < Cost(kind) || seedCooldownTicks[index] > 0 || PlantAt(row, column) != null)
                return false;

            if (IsExperimentalLevelOne)
            {
                bool allowed = false;
                CanPlantExperimentalLevelOne(kind, row, column, ref allowed);
                if (!allowed) return false;
            }

            SunAmount -= Cost(kind);
            seedCooldownTicks[index] = DefinitionFor(kind).RechargeThresholdRaw + 1;
            Plant plant = new Plant
            {
                Kind = kind,
                Row = row,
                Column = column,
                Health = kind == PlantKind.Wallnut ? 4000f : kind == PlantKind.TallNut ? 8000f : 300f,
                ActionTimer = kind == PlantKind.CherryBomb ? 1f :
                    kind == PlantKind.PotatoMine ? 15f : kind == PlantKind.Jalapeno ? 1f : 0.5f
            };
            if (HasOriginalTiming(kind))
            {
                var timingKind = (OriginalTiming.PlantKind)(int)kind;
                plant.Timing = OriginalTiming.PlantTiming.Initialize(timingKind,
                    random.Next(OriginalTiming.PlantTiming.InitialRandomBound(timingKind)));
            }
            Plants.Add(plant);
            if (IsExperimentalLevelOne) OnExperimentalLevelOnePlantPlaced();
            Raise(GameEvent.Planted);
            return true;
        }

        public bool RemovePlant(int row, int column)
        {
            if (State != MatchState.Playing) return false;
            if (IsAdventure)
            {
                bool handled = false, removed = false;
                RemoveAdventurePlant(row, column, ref handled, ref removed);
                return handled && removed;
            }
            if (IsExperimentalLevelOne)
            {
                bool allowed = false;
                CanRemoveExperimentalLevelOnePlant(ref allowed);
                if (!allowed) return false;
            }
            Plant plant = PlantAt(row, column);
            return plant != null && Plants.Remove(plant);
        }

        public bool CollectSun(Sun sun)
        {
            bool handled=false,collected=false;if(IsAdventure)CollectAdventureSunMotion(sun,ref handled,ref collected);if(handled)return collected;
            if (State != MatchState.Playing || !Suns.Remove(sun)) return false;
            Raise(GameEvent.SunPickupStarted);
            SunAmount = IsAdventure ? Math.Min(9990, SunAmount + sun.Value) : SunAmount + sun.Value;
            if (IsAdventure) CollectAdventureSun();
            if (IsExperimentalLevelOne) OnExperimentalLevelOneSunCollected();
            Raise(GameEvent.SunCollected);
            return true;
        }

        public void TogglePause()
        {
            if (State == MatchState.Playing) State = MatchState.Paused;
            else if (State == MatchState.Paused) State = MatchState.Playing;
        }

        public void Update(float deltaTime)
        {
            if (IsAdventure && State == MatchState.Won) { AdvanceAdventureRewardPresentation(deltaTime); return; }
            if (State != MatchState.Playing || deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
            accumulatedTime += deltaTime;
            while (accumulatedTime + 0.0000001 >= SimulationStep && State == MatchState.Playing)
            {
                Advance((float)SimulationStep);
                accumulatedTime -= SimulationStep;
                if (Math.Abs(accumulatedTime) < 0.0000001) accumulatedTime = 0d;
            }
        }

        private void Advance(float dt)
        {
            if (IsAdventure)
            {
                bool handled = false;
                AdvanceAdventure(dt, ref handled);
                if (!handled) throw new InvalidOperationException("Adventure tick handler is missing.");
                return;
            }
            Elapsed += dt;
            for (int i = 0; i < seedCooldownTicks.Length; i++)
                if (seedCooldownTicks[i] > 0) seedCooldownTicks[i]--;

            if (IsExperimentalLevelOne) BeforeExperimentalLevelOneTick();
            bool nativeSkySunHandled = false;
            if (IsExperimentalLevelOne) AdvanceExperimentalLevelOneNativeSkySun(ref nativeSkySunHandled);
            if (nativeSkySunHandled && State != MatchState.Playing) return;
            if (!nativeSkySunHandled)
            {
                bool skySunAllowed = true;
                if (IsExperimentalLevelOne) CanDropExperimentalLevelOneSkySun(ref skySunAllowed);
                if (skySunAllowed)
                {
                    nextSkySun -= dt;
                    if (nextSkySun <= 0f && Rules.SkySunInterval > 0f)
                    {
                        bool handled = false;
                        if (IsExperimentalLevelOne) SpawnExperimentalLevelOneSkySun(ref handled);
                        if (!handled)
                            Suns.Add(new Sun { X = 0.4f + (float)random.NextDouble() * 8.2f, Y = -1f,
                                TargetY = 0.5f + (float)random.NextDouble() * 3.8f });
                        nextSkySun += Rules.SkySunInterval;
                    }
                }
            }

            if (IsExperimentalLevelOne)
            {
                AdvanceExperimentalLevelOneWaves();
            }
            else
            {
                nextZombie -= dt;
                if (nextZombie <= 0f && SpawnedZombies < Rules.ZombieCount)
                {
                    SpawnZombie();
                    nextZombie += Math.Max(3.5f, Rules.ZombieInterval - SpawnedZombies * 0.18f);
                }
            }

            UpdateSuns(dt);
            UpdatePlants(dt);
            UpdatePeas(dt);
            UpdateZombies(dt);
            UpdateMowers(dt);
            for (int i = Flames.Count - 1; i >= 0; i--)
            {
                Flames[i].Age += dt;
                if (Flames[i].Age >= 1f) Flames.RemoveAt(i);
            }

            if (State == MatchState.Playing && IsExperimentalLevelOne)
                CheckExperimentalLevelOneCompletion();
            else if (State == MatchState.Playing && Rules.ZombieCount > 0 &&
                     SpawnedZombies >= Rules.ZombieCount && Zombies.Count == 0)
            {
                State = MatchState.Won;
                Raise(GameEvent.Won);
            }
        }

        private void SpawnZombie()
        {
            int number = ++SpawnedZombies;
            ZombieKind kind = number == Rules.ZombieCount ? ZombieKind.Flag :
                number >= 10 && number % 5 == 0 ? ZombieKind.ScreenDoor :
                number >= 8 && number % 4 == 0 ? ZombieKind.Buckethead :
                number >= 4 && number % 3 == 0 ? ZombieKind.Conehead : ZombieKind.Normal;
            float armor = kind == ZombieKind.Buckethead ? 1100f : kind == ZombieKind.Conehead ? 370f : 0f;
            float shield = kind == ZombieKind.ScreenDoor ? 1100f : 0f;
            Zombies.Add(new Zombie { Kind = kind, Row = random.Next(Rows), X = 9.5f, Health = 270f,
                ArmorHealth = armor, ShieldHealth = shield });
        }

        private void UpdateSuns(float dt)
        {
            bool handled=false;if(IsAdventure)AdvanceAdventureSunMotion(dt,ref handled);if(handled)return;
            for (int i = Suns.Count - 1; i >= 0; i--)
            {
                Sun sun = Suns[i];
                sun.Age += dt;
                sun.Y = Math.Min(sun.TargetY, sun.Y + dt * 0.75f);
                if (sun.Age > 14f) Suns.RemoveAt(i);
            }
        }

        private void UpdatePlants(float dt)
        {
            for (int i = Plants.Count - 1; i >= 0; i--)
            {
                Plant plant = Plants[i];
                plant.Age += dt;
                plant.PhaseAge += dt;
                if (plant.ShootAge >= 0f) plant.ShootAge += dt;
                if (plant.Timing.HasValue)
                {
                    UpdateTimedPlant(plant);
                    continue;
                }
                plant.ActionTimer -= dt;
                if (plant.Kind == PlantKind.PotatoMine)
                {
                    UpdateMine(i, plant);
                    continue;
                }
                if (UpdateExtendedPlant(i, plant)) continue;
                if (plant.ActionTimer > 0.000001f) continue;

                if (plant.Kind == PlantKind.CherryBomb)
                {
                    for (int z = Zombies.Count - 1; z >= 0; z--)
                    {
                        Zombie zombie = Zombies[z];
                        if (Math.Abs(zombie.Row - plant.Row) <= 1 && Math.Abs(zombie.X - (plant.Column + 0.5f)) <= 1.5f)
                            DamageZombie(z, 1800f);
                    }
                    Plants.RemoveAt(i);
                    Raise(GameEvent.Explosion);
                }
            }
        }

        private static bool HasOriginalTiming(PlantKind kind)
        {
            return kind == PlantKind.Peashooter || kind == PlantKind.SnowPea ||
                kind == PlantKind.Repeater || kind == PlantKind.Sunflower;
        }

        private void UpdateTimedPlant(Plant plant)
        {
            OriginalTiming.PlantTimingState state = plant.Timing.Value;
            bool target = Zombies.Exists(z => z.Row == plant.Row && z.X > plant.Column + 0.4f);
            var input = new OriginalTiming.PlantTimingInput(true, true, true, true, target,
                OriginalTiming.ShootingTrack.Head, true, 0);
            int bound = OriginalTiming.PlantTiming.RequiredRandomBound(state, input);
            if (bound > 0)
                input = new OriginalTiming.PlantTimingInput(true, true, true, true, target,
                    OriginalTiming.ShootingTrack.Head, true, random.Next(bound));
            OriginalTiming.PlantTimingResult result = OriginalTiming.PlantTiming.Tick(state, input);
            plant.Timing = result.State;
            if (result.AnimationStarted) plant.ShootAge = 0f;
            if (result.SunCreated)
                Suns.Add(new Sun { X = plant.Column + 0.5f, Y = plant.Row + 0.45f, TargetY = plant.Row + 0.7f });
            for (int i = 0; i < result.ProjectilesCreated; i++)
            {
                Peas.Add(new Pea { SourceKind = plant.Kind, Row = plant.Row, X = plant.Column + 0.85f, IsSnow = plant.Kind == PlantKind.SnowPea });
                Raise(GameEvent.Shoot);
            }
        }

        private void UpdateMine(int plantIndex, Plant mine)
        {
            if (mine.MinePhase == MinePhase.Buried && mine.ActionTimer <= 0f)
            {
                mine.MinePhase = MinePhase.Rising;
                mine.PhaseAge = 0f;
                mine.ActionTimer = 19f / 18f;
            }
            else if (mine.MinePhase == MinePhase.Rising && mine.ActionTimer <= 0f)
            {
                mine.MinePhase = MinePhase.Armed;
                mine.PhaseAge = 0f;
            }
            else if (mine.MinePhase == MinePhase.Armed &&
                Zombies.Exists(z => z.Row == mine.Row && Math.Abs(z.X - (mine.Column + 0.5f)) <= 0.75f))
            {
                for (int z = Zombies.Count - 1; z >= 0; z--)
                    if (Zombies[z].Row == mine.Row && Math.Abs(Zombies[z].X - (mine.Column + 0.5f)) <= 0.75f)
                        DamageZombie(z, 1800f);
                Plants.RemoveAt(plantIndex);
                Raise(GameEvent.MineExplosion);
            }
        }

        private void UpdatePeas(float dt)
        {
            for (int i = Peas.Count - 1; i >= 0; i--)
            {
                Pea pea = Peas[i];
                float oldX = pea.X;
                pea.X += dt * 4.5f;
                int hit = -1;
                float closest = float.MaxValue;
                for (int j = 0; j < Zombies.Count; j++)
                {
                    Zombie zombie = Zombies[j];
                    if (zombie.Row == pea.Row && zombie.X >= oldX - 0.15f && zombie.X <= pea.X + 0.35f && zombie.X < closest)
                    {
                        hit = j;
                        closest = zombie.X;
                    }
                }
                if (hit >= 0)
                {
                    DamageZombie(hit, 20f, pea.IsSnow);
                    Peas.RemoveAt(i);
                }
                else if (pea.X > 10f) Peas.RemoveAt(i);
            }
        }

        private void UpdateZombies(float dt)
        {
            for (int i = Zombies.Count - 1; i >= 0; i--)
            {
                Zombie zombie = Zombies[i];
                float speedMultiplier = zombie.ChillRemaining > 0f ? 0.5f : 1f;
                zombie.ChillRemaining = Math.Max(0f, zombie.ChillRemaining - dt);
                zombie.Age += dt * speedMultiplier;
                Plant target = null;
                for (int j = 0; j < Plants.Count; j++)
                {
                    Plant candidate = Plants[j];
                    if (candidate.Row == zombie.Row && Math.Abs(zombie.X - (candidate.Column + 0.5f)) < 0.38f)
                    {
                        target = candidate;
                        break;
                    }
                }

                zombie.Eating = target != null;
                if (target != null)
                {
                    zombie.BiteTimer -= dt * speedMultiplier;
                    if (zombie.BiteTimer <= 0f &&
                        !(target.Kind == PlantKind.PotatoMine && target.MinePhase != MinePhase.Buried) &&
                        target.Kind != PlantKind.CherryBomb && target.Kind != PlantKind.Jalapeno)
                    {
                        target.Health -= 4f;
                        zombie.BiteTimer += 0.04f;
                        if (target.Health <= 0f) Plants.Remove(target);
                    }
                }
                else
                {
                    zombie.BiteTimer = 0f;
                    zombie.X -= Rules.ZombieSpeed * dt * speedMultiplier;
                }

                if (zombie.X < 0.15f && Mowers[zombie.Row].Available)
                {
                    Mowers[zombie.Row].Available = false;
                    Mowers[zombie.Row].Active = true;
                    Raise(GameEvent.Mower);
                }
                else if (zombie.X < -0.65f && !Mowers[zombie.Row].Active)
                {
                    State = MatchState.Lost;
                    Raise(GameEvent.Lost);
                    return;
                }
            }
        }

        private void UpdateMowers(float dt)
        {
            foreach (Mower mower in Mowers)
            {
                if (!mower.Active) continue;
                mower.X += dt * 5f;
                for (int i = Zombies.Count - 1; i >= 0; i--)
                    if (Zombies[i].Row == mower.Row && Math.Abs(Zombies[i].X - mower.X) < 0.7f)
                        DamageZombie(i, 99999f);
                if (mower.X > 10f) mower.Active = false;
            }
        }

        private void DamageZombie(int index, float damage, bool chill = false)
        {
            Zombie zombie = Zombies[index];
            if (chill) zombie.ChillRemaining = Math.Max(zombie.ChillRemaining, 10f);
            float shieldDamage = Math.Min(zombie.ShieldHealth, damage);
            zombie.ShieldHealth -= shieldDamage;
            damage -= shieldDamage;
            float armorDamage = Math.Min(zombie.ArmorHealth, damage);
            zombie.ArmorHealth -= armorDamage;
            zombie.Health -= damage - armorDamage;
            Raise(GameEvent.ZombieHit);
            if (zombie.Health <= 0f)
            {
                Zombies.RemoveAt(index);
                Raise(GameEvent.ZombieDied);
            }
        }

        private void EmitLaneFlame(int row)
        {
            Flames.Add(new LaneFlame { Row = row });
        }

        private void Raise(GameEvent gameEvent)
        {
            if (EventRaised != null) EventRaised(gameEvent);
        }
    }
}
