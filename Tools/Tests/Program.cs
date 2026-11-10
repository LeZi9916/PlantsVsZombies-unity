using System;
using PlantsVsZombies.Simulation;

internal static class Program
{
    private static int assertions;

    private static void Check(bool condition, string name)
    {
        assertions++;
        if (!condition) throw new Exception("Failed: " + name);
    }

    private static GameSimulation EmptyGame(int sun = 500, bool mowers = true)
    {
        return new GameSimulation(new GameRules {
            StartingSun = sun, ZombieCount = 0, SkySunInterval = 0f,
            ZombieSpeed = 0.18f, MowersEnabled = mowers
        });
    }

    private static void Main()
    {
        GameSimulation game = EmptyGame(150);
        Check((int)PlantKind.CherryBomb == 2 && (int)PlantKind.Repeater == 7, "plant IDs match executable definitions");
        Check((int)ZombieKind.Flag == 1 && (int)ZombieKind.Conehead == 2 && (int)ZombieKind.Buckethead == 4 &&
            (int)ZombieKind.ScreenDoor == 6,
            "zombie IDs match executable definitions");
        Check(!game.TryPlant((PlantKind)6, 0, 0), "unimplemented plant ID is rejected");
        Check(game.TryPlant(PlantKind.Peashooter, 2, 3), "plant in empty tile");
        Check(game.SunAmount == 50, "plant cost");
        Check(!game.TryPlant(PlantKind.Sunflower, 2, 3), "occupied tile rejected");
        Check(!game.TryPlant(PlantKind.Peashooter, 2, 4), "cooldown or insufficient sun rejected");
        Check(game.TryPlant(PlantKind.Sunflower, 1, 3), "sunflower can be planted");
        Check(game.SunAmount == 0, "sun spent");
        Check(!game.TryPlant(PlantKind.Wallnut, -1, 0), "invalid row rejected");
        game.Update(13f);
        Check(game.CooldownRemaining(PlantKind.Peashooter) == 0f, "seed recharges");
        Check(game.Suns.Count == 1, "sunflower produces sun");
        Sun sun = game.Suns[0];
        Check(game.CollectSun(sun) && game.SunAmount == 25, "collect sun");
        Check(!game.CollectSun(sun), "sun cannot be collected twice");
        game.TogglePause();
        float before = game.Elapsed;
        game.Update(1f);
        Check(game.Elapsed == before && !game.TryPlant(PlantKind.Wallnut, 0, 0), "pause freezes game");
        game.TogglePause();
        Check(game.RemovePlant(1, 3) && game.PlantAt(1, 3) == null, "shovel removes plant");

        game = EmptyGame();
        game.TryPlant(PlantKind.Peashooter, 0, 0);
        game.Update(7.5f);
        Check(game.CooldownRemaining(PlantKind.Peashooter) > 0f && !game.TryPlant(PlantKind.Peashooter, 0, 1),
            "seed threshold is not ready at exactly 750 ticks");
        game.Update(0.01f);
        Check(game.CooldownRemaining(PlantKind.Peashooter) == 0f && game.TryPlant(PlantKind.Peashooter, 0, 1),
            "seed becomes ready after the strict threshold");

        game = EmptyGame();
        game.TryPlant(PlantKind.Peashooter, 2, 0);
        game.Zombies.Add(new Zombie { Row = 2, X = 2.5f, Health = 20f });
        game.Update(2f);
        Check(game.Zombies.Count == 0, "pea hits and kills zombie");

        game = EmptyGame();
        game.TryPlant(PlantKind.Peashooter, 2, 0);
        game.Zombies.Add(new Zombie { Row = 2, X = 2.5f, Health = 20f });
        game.Zombies.Add(new Zombie { Row = 2, X = 3.5f, Health = 20f });
        game.Zombies.Add(new Zombie { Row = 3, X = 2.5f, Health = 20f });
        game.Update(1f);
        Check(game.Zombies.Count == 2 && game.Zombies.Exists(z => z.Row == 2 && z.X > 3f)
            && game.Zombies.Exists(z => z.Row == 3), "pea hits nearest zombie in the same lane");

        game = EmptyGame();
        game.TryPlant(PlantKind.Peashooter, 2, 0);
        Zombie armored = new Zombie { Row = 2, X = 2.5f, Health = 270f, ArmorHealth = 30f, Kind = ZombieKind.Conehead };
        game.Zombies.Add(armored);
        game.Update(1f);
        Check(armored.ArmorHealth == 10f && armored.Health == 270f, "armor absorbs projectile damage");
        game.Update(1.6f);
        Check(armored.ArmorHealth == 0f && armored.Health == 260f, "excess damage carries through armor");

        game = EmptyGame();
        game.TryPlant(PlantKind.Peashooter, 2, 0);
        Zombie shielded = new Zombie { Row = 2, X = 2.5f, Health = 270f, ShieldHealth = 30f, Kind = ZombieKind.ScreenDoor };
        game.Zombies.Add(shielded);
        game.Update(1f);
        Check(shielded.ShieldHealth == 10f && shielded.Health == 270f, "shield absorbs projectile damage");
        game.Update(1.6f);
        Check(shielded.ShieldHealth == 0f && shielded.Health == 260f, "excess shield damage reaches body");

        game = EmptyGame();
        Check(game.TryPlant(PlantKind.SnowPea, 2, 0) && game.SunAmount == 325, "snow pea cost");
        Zombie chilled = new Zombie { Row = 2, X = 2.5f, Health = 270f };
        game.Zombies.Add(chilled);
        game.Update(1f);
        Check(chilled.ChillRemaining > 9f && chilled.Health == 250f, "snow pea damages and chills");
        float chilledX = chilled.X;
        game.Update(1f);
        Check(chilledX - chilled.X < game.Rules.ZombieSpeed * 0.55f, "chill slows movement");

        game = EmptyGame();
        Check(game.TryPlant(PlantKind.Repeater, 2, 0) && game.SunAmount == 300, "repeater cost");
        Zombie repeatTarget = new Zombie { Row = 2, X = 2.5f, Health = 270f };
        game.Zombies.Add(repeatTarget);
        for (int tick = 0; tick < 300 && repeatTarget.Health > 230f; tick++) game.Update(0.01f);
        Check(repeatTarget.Health == 230f, "repeater fires two damaging peas");

        game = EmptyGame();
        Check(game.TryPlant(PlantKind.PotatoMine, 2, 2) && game.SunAmount == 475, "potato mine cost");
        Plant mine = game.PlantAt(2, 2);
        game.Update(14.9f);
        Check(mine.MinePhase == MinePhase.Buried, "potato mine starts buried");
        game.Update(0.2f);
        Check(mine.MinePhase == MinePhase.Rising, "potato mine rises after charging");
        game.Update(1.1f);
        Check(mine.MinePhase == MinePhase.Armed, "potato mine arms after rising");
        game.Zombies.Add(new Zombie { Row = 2, X = 2.5f, Health = 270f });
        game.Zombies.Add(new Zombie { Row = 3, X = 2.5f, Health = 270f });
        game.Update(0.1f);
        Check(game.PlantAt(2, 2) == null && game.Zombies.Count == 1 && game.Zombies[0].Row == 3,
            "armed mine detonates in its lane");

        game = EmptyGame();
        game.TryPlant(PlantKind.PotatoMine, 2, 2);
        game.Zombies.Add(new Zombie { Row = 2, X = 2.5f, Health = 270f });
        game.Update(1f);
        Check(game.PlantAt(2, 2).Health > 190f && game.PlantAt(2, 2).Health <= 200f,
            "zombie chewing applies incremental damage");
        game.Update(2.1f);
        Check(game.PlantAt(2, 2) == null && game.Zombies.Count == 1, "unarmed mine can be eaten");

        game = EmptyGame();
        game.TryPlant(PlantKind.Wallnut, 0, 0);
        game.Zombies.Add(new Zombie { Row = 0, X = 0.8f, Health = 200f });
        game.Update(1.1f);
        Check(game.Plants[0].Health < 4000f && game.Zombies[0].Eating, "zombie bites plant");

        game = EmptyGame();
        game.TryPlant(PlantKind.CherryBomb, 2, 4);
        game.Zombies.Add(new Zombie { Row = 1, X = 4.5f, Health = 200f });
        game.Zombies.Add(new Zombie { Row = 4, X = 4.5f, Health = 200f });
        game.Update(1.3f);
        Check(game.Zombies.Count == 1 && game.Zombies[0].Row == 4, "cherry blast area");
        Check(game.Plants.Count == 0, "cherry consumed");

        game = EmptyGame();
        game.TryPlant(PlantKind.CherryBomb, 2, 4);
        game.Zombies.Add(new Zombie { Row = 2, X = 4.5f, Health = 270f });
        game.Update(0.5f);
        Check(game.PlantAt(2, 4) != null && game.PlantAt(2, 4).Health == 300f,
            "armed explosive plant cannot be chewed before detonating");
        game.Update(0.5f);
        Check(game.Plants.Count == 0 && game.Zombies.Count == 0, "cherry detonates after its countdown");

        game = EmptyGame();
        game.Zombies.Add(new Zombie { Row = 3, X = 0.1f, Health = 200f });
        game.Update(0.1f);
        Check(game.Zombies.Count == 0 && !game.Mowers[3].Available, "mower clears lane");
        game.Update(3f);
        game.Zombies.Add(new Zombie { Row = 3, X = -0.64f, Health = 200f });
        game.Update(0.2f);
        Check(game.State == MatchState.Lost, "used mower cannot protect lane twice");

        game = new GameSimulation(new GameRules { ZombieCount = 0, SkySunInterval = 1f });
        game.Update(1.1f);
        Check(game.Suns.Count == 1, "sky sun spawns");
        Sun fallingSun = game.Suns[0];
        game.Update(1f);
        Check(fallingSun.Y > -1f, "sky sun falls");
        game.Update(14f);
        Check(!game.Suns.Contains(fallingSun), "uncollected sun expires");

        GameRules timingRules = new GameRules { ZombieCount = 3, FirstZombieDelay = 2f,
            ZombieInterval = 4f, SkySunInterval = 3f, ZombieSpeed = 0.18f };
        GameSimulation largeFrames = new GameSimulation(timingRules, 42);
        GameSimulation smallFrames = new GameSimulation(timingRules, 42);
        largeFrames.Update(12f);
        for (int i = 0; i < 1200; i++) smallFrames.Update(0.01f);
        Check(largeFrames.SpawnedZombies == smallFrames.SpawnedZombies &&
            largeFrames.Zombies.Count == smallFrames.Zombies.Count && largeFrames.Suns.Count == smallFrames.Suns.Count,
            "frame rate does not change scheduled entities");
        Check(Math.Abs(largeFrames.Zombies[0].X - smallFrames.Zombies[0].X) < 0.00001f &&
            Math.Abs(largeFrames.Elapsed - smallFrames.Elapsed) < 0.00001f,
            "frame rate does not change simulation positions or time");
        before = smallFrames.Elapsed;
        smallFrames.Update(float.NaN);
        smallFrames.Update(float.PositiveInfinity);
        Check(smallFrames.Elapsed == before, "non-finite time is rejected");

        game = EmptyGame(mowers: false);
        game.Zombies.Add(new Zombie { Row = 0, X = -0.64f, Health = 200f });
        game.Update(0.2f);
        Check(game.State == MatchState.Lost, "zombie reaches house");

        game = new GameSimulation(new GameRules { StartingSun = 100, ZombieCount = 1,
            FirstZombieDelay = 0f, SkySunInterval = 0f, ZombieSpeed = 0f, MowersEnabled = false });
        game.Update(0.01f);
        Zombie last = game.Zombies[0];
        last.X = 2.5f;
        last.Health = 20f;
        game.TryPlant(PlantKind.Peashooter, last.Row, 0);
        game.Update(2f);
        Check(game.State == MatchState.Won, "defeat final zombie wins");

        Console.WriteLine("PASS: " + assertions + " simulation assertions");
    }
}
