#nullable enable
using PlantsVsZombies.Adventure;
using PlantsVsZombies.Simulation;
using System;
using System.Collections.Generic;
using System.Reflection;

/// <summary>Checks contact plants and screen entry against the original PC collision rules.</summary>
internal static class CombatFidelityRegressionTests
{
    /// <summary>Runs the focused combat and defensive damage regressions.</summary>
    public static void Run()
    {
        MineContact();
        MineExplosionDamageAndDeath();
        TargetEntry();
        ShooterWindupMatchesLaunch();
        NativeShooterTiming();
        DefensiveDamage();
        DefensivePresentation();
        ChomperTransitions();
        NewspaperTransitions();
        HugeWavePreservesPlants();
        Console.WriteLine("PASS: native mine contact, zombie screen entry, defensive damage, and chomper regressions.");
    }

    /// <summary>Fails a focused regression when an original rule is not preserved.</summary>
    /// <param name="condition">Whether the tested original behavior was observed.</param>
    /// <param name="message">The behavior being verified.</param>
    /// <exception cref="InvalidOperationException">The assertion fails.</exception>
    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    /// <summary>Invokes an isolated simulation hook without changing production API visibility.</summary>
    /// <param name="game">The fixture simulation.</param>
    /// <param name="method">The private hook's method name.</param>
    /// <param name="arguments">Arguments required by the hook.</param>
    /// <returns>The hook's result.</returns>
    private static object? Invoke(GameSimulation game, string method, params object[] arguments)
    {
        return typeof(GameSimulation).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(game, arguments);
    }

    /// <summary>Creates an explicitly isolated legal adventure fixture with distant wave scheduling.</summary>
    /// <returns>The fixture simulation.</returns>
    private static GameSimulation Fixture(int level = 9)
    {
        var game = new GameSimulation(new GameRules { AdventureLevel = level }, 7529);
        Check(game.ChooseDefaultAdventureSeeds(), "Fixture chooses its earned seed bank.");
        game.ResetAdventureWaveSchedule(100000, false, false);
        return game;
    }

    /// <summary>Runs the native mine trigger at a controlled body coordinate.</summary>
    /// <param name="nativeBodyLeft">The zombie's left body edge in board pixels.</param>
    /// <param name="eating">Whether the zombie already has a latched meal.</param>
    /// <param name="phase">The mine phase being checked.</param>
    /// <param name="headless">Whether the zombie has lost its head.</param>
    /// <param name="small">Whether only the zombie's presentation is scaled down.</param>
    /// <param name="kind">The zombie species.</param>
    /// <param name="usedAbility">Whether the zombie's special movement is complete.</param>
    /// <returns>Whether the mine exploded.</returns>
    private static bool MineTriggered(int nativeBodyLeft, bool eating = false, MinePhase phase = MinePhase.Armed, bool headless = false, bool small = false, ZombieKind kind = ZombieKind.Normal, bool usedAbility = true)
    {
        var game = Fixture();
        var mine = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.PotatoMine, 2, 4)!;
        mine.MinePhase = phase;
        var bodyOffset = kind == ZombieKind.Football ? 50 : 36;
        var zombie = new Zombie
        {
            Kind = kind,
            Row = 2,
            X = AdventureZombieNativeRules.GridX(nativeBodyLeft - bodyOffset, small ? .5f : 1f),
            Health = 270,
            StartingHealth = 270,
            Eating = eating,
            Headless = headless,
            IsSmall = small,
            UsedAbility = usedAbility
        };
        game.Zombies.Add(zombie);
        Invoke(game, "AdvanceAdventurePotatoMine", mine);
        return !game.Plants.Contains(mine);
    }

    /// <summary>Checks edge contact, latched eating reach, arming, and native species exclusions.</summary>
    private static void MineContact()
    {
        // Column four's original mine attack interval is [360,415].
        Check(!MineTriggered(416), "Walking zombies do not trigger a mine across a one-pixel gap.");
        Check(MineTriggered(415), "Walking zombies trigger on exact body-edge contact.");
        Check(!MineTriggered(446, true), "Latched eating does not extend farther than thirty pixels.");
        Check(MineTriggered(445, true), "An eating zombie triggers at the original thirty-pixel tolerance.");
        Check(MineTriggered(445, true, small: true), "Small zombies keep the original unscaled collision body.");
        Check(!MineTriggered(445, false, MinePhase.Buried), "Buried mines do not explode.");
        Check(!MineTriggered(445, true, MinePhase.Rising), "Rising mines do not explode before arming.");
        Check(!MineTriggered(400, true, headless: true), "Headless zombies do not trigger a mine.");
        Check(MineTriggered(415, kind: ZombieKind.Football), "Football body contact uses its original wider rectangle.");
        Check(!MineTriggered(400, kind: ZombieKind.PoleVaulter, usedAbility: false), "Running pole vaulters do not trigger mines before vaulting.");
        Check(MineTriggered(400, kind: ZombieKind.PoleVaulter), "Pole vaulters can trigger mines after vaulting.");
        Check(!MineTriggered(400, kind: ZombieKind.Pogo, usedAbility: false), "Pogo zombies do not trigger mines while bouncing.");
    }

    /// <summary>Creates an isolated same-row zombie with a controlled original body edge.</summary>
    /// <param name="game">The simulation receiving the explicitly injected fixture zombie.</param>
    /// <param name="kind">The original zombie species.</param>
    /// <param name="bodyLeft">The original body rectangle's left edge in board pixels.</param>
    /// <param name="health">The fixture body health before the blast.</param>
    /// <param name="armor">The fixture helmet health before the blast.</param>
    /// <param name="shield">The fixture held shield health before the blast.</param>
    /// <returns>The injected zombie with its actual native body position.</returns>
    private static Zombie MineVictim(GameSimulation game, ZombieKind kind, int bodyLeft = 400, float health = 270, float armor = 0, float shield = 0)
    {
        var zombie = new Zombie
        {
            Id = game.Zombies.Count + 1000,
            Kind = kind,
            Row = 2,
            X = 5,
            Health = health,
            StartingHealth = health,
            ArmorHealth = armor,
            ShieldHealth = shield,
            UsedAbility = true
        };
        var offset = AdventureZombieTargetingRules.BodyRectangle(zombie).X - (int)AdventureZombieNativeRules.NativePositionX(zombie.X);
        zombie.X = AdventureZombieNativeRules.GridX(bodyLeft - offset);
        game.Zombies.Add(zombie);
        return zombie;
    }

    /// <summary>Checks the native mine blast circle, simultaneous shield/body damage, and no-animation death flag.</summary>
    private static void MineExplosionDamageAndDeath()
    {
        var game = Fixture();
        var mine = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.PotatoMine, 2, 4)!;
        mine.MinePhase = MinePhase.Armed;
        var trigger = MineVictim(game, ZombieKind.Normal, 440);
        trigger.Eating = true;
        var leftEdge = MineVictim(game, ZombieKind.Normal, 278);
        var outsideLeft = MineVictim(game, ZombieKind.Normal, 277);
        var outsideRight = MineVictim(game, ZombieKind.Normal, 441);
        var airborne = MineVictim(game, ZombieKind.Balloon);
        airborne.Airborne = true;
        var vaulting = MineVictim(game, ZombieKind.PoleVaulter);
        vaulting.PresentationNativePhase = 12;
        var otherRow = MineVictim(game, ZombieKind.Normal);
        otherRow.Row = 1;
        var raised = MineVictim(game, ZombieKind.Normal);
        raised.PresentationBiteBodyY = 180;
        var underground = MineVictim(game, ZombieKind.Digger);
        underground.AbilityPhase = 0;
        var headless = MineVictim(game, ZombieKind.Normal, health: 60);
        headless.Headless = true;
        var door = MineVictim(game, ZombieKind.ScreenDoor, shield: 1100);
        var paper = MineVictim(game, ZombieKind.Newspaper, shield: 150);
        var bucket = MineVictim(game, ZombieKind.Buckethead, armor: 1100);
        var football = MineVictim(game, ZombieKind.Football, armor: 1400);
        var giant = MineVictim(game, ZombieKind.Gargantuar, health: 3000);
        var armoredSurvivor = MineVictim(game, ZombieKind.Football, armor: 2000);
        var doorSurvivor = MineVictim(game, ZombieKind.ScreenDoor, health: 5000, shield: 1100);
        var deaths = 0;
        game.EventRaised += item =>
        {
            if (item == GameEvent.ZombieDied)
            {
                deaths++;
            }
        };
        Invoke(game, "AdvanceAdventurePotatoMine", mine);
        Check(!game.Plants.Contains(mine), "The eating target detonates the armed mine through the production path.");
        foreach (var victim in new[] { trigger, leftEdge, underground, headless, door, paper, bucket, football })
        {
            Check(!game.Zombies.Contains(victim) && victim.Health == 0, "Native blast victim retires with zero health: " + victim.Kind);
            Check(victim.PresentationDeath == AdventureZombieDeathPresentation.Normal, "Mine damage does not turn into ash, mowing, or swallowed death: " + victim.Kind);
        }

        Check(deaths == 8, "Each lethal mine hit, including an already headless victim, reports exactly one logical death.");
        foreach (var survivor in new[] { outsideLeft, outsideRight, airborne, vaulting, otherRow, raised })
        {
            Check(game.Zombies.Contains(survivor) && survivor.Health == 270, "The original blast radius and target mask exclude an ineligible body.");
        }

        Check(door.ShieldHealth == 0 && paper.ShieldHealth == 0, "Native bit two destroys held shields as well as applying full body damage.");
        Check(doorSurvivor.Health == 3200 && doorSurvivor.ShieldHealth == 0, "Shield damage does not absorb any of the original eighteen hundred body damage.");
        Check(armoredSurvivor.Health == 270 && armoredSurvivor.ArmorHealth == 200, "A helmet still absorbs damage before the body under native bit two.");
        Check(game.Zombies.Contains(giant) && giant.Health == 1200, "A full-health Gargantuar survives one original mine blast.");
        Check(game.Presentation.Corpses.Count == 0 && game.Presentation.ZombiePartDrops.Count == 0 && game.Presentation.ArmDrops.Count == 0,
            "Native bit sixteen suppresses ordinary falling corpses, ash corpses, and flying equipment or body parts.");
        Check(!game.Presentation.ZombieSounds.Exists(sound => sound.Foley == 25 || sound.Foley == 67), "Suppressed parts do not invent limb-pop or newspaper-gasp sounds.");

        Invoke(game, "HitAdventureZombie", giant, 1800f, true, 0x12U);
        Check(!game.Zombies.Contains(giant) && game.Presentation.Corpses.Count == 1 && ReferenceEquals(game.Presentation.Corpses[0].Zombie, giant)
            && game.Presentation.Corpses[0].Burn == null, "The native Gargantuar exception preserves its normal death animation.");

        var ordinary = Fixture();
        var regularVictim = MineVictim(ordinary, ZombieKind.Normal);
        Invoke(ordinary, "HitAdventureZombie", regularVictim, 300f, false, 0U);
        Check(ordinary.Presentation.Corpses.Count == 1 && ordinary.Presentation.ZombiePartDrops.Count > 0, "Ordinary lethal projectiles retain falling corpses and head parts.");
        var burning = Fixture();
        var burnedVictim = MineVictim(burning, ZombieKind.Normal);
        Invoke(burning, "HitAdventureExplosionZombie", burnedVictim, 1800f, true, 0U);
        Check(burning.Presentation.Corpses.Count == 1 && burning.Presentation.Corpses[0].Burn != null,
            "Cherry and other native burn-body explosions retain their distinct charred presentation.");
    }

    /// <summary>Checks the native body edge screen boundary, including its bobsled exception.</summary>
    private static void TargetEntry()
    {
        foreach (var kind in new[] { ZombieKind.Normal, ZombieKind.Football, ZombieKind.Gargantuar, ZombieKind.Catapult })
        {
            foreach (var small in new[] { false, true })
            {
                var zombie = new Zombie { Kind = kind, Health = 270, IsSmall = small, X = 12 };
                var original = AdventureZombieTargetingRules.BodyRectangle(zombie);
                zombie.X = AdventureZombieNativeRules.GridX(801 - (original.X - (int)AdventureZombieNativeRules.NativePositionX(zombie.X, small ? .5f : 1f)), small ? .5f : 1f);
                Check(!AdventureZombieTargetingRules.CanBeAutoTargeted(zombie), "Offscreen body cannot start plant attacks: " + kind);
                zombie.X -= 1f / 80f;
                Check(AdventureZombieTargetingRules.CanBeAutoTargeted(zombie), "Body touching native screen edge becomes targetable: " + kind);
            }
        }

        var sled = new Zombie { Kind = ZombieKind.Bobsled, Health = 270, X = 15 };
        Check(AdventureZombieTargetingRules.CanBeAutoTargeted(sled), "Original bobsled screen-boundary exception remains available.");
        var game = Fixture();
        var shooter = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.Peashooter, 2, 4)!;
        game.Zombies.Add(new Zombie { Kind = ZombieKind.Normal, Row = 2, X = 11, Health = 270, StartingHealth = 270 });
        shooter.ActionTimer = 0;
        Invoke(game, "AdvanceAdventurePlants", .01f);
        Check(game.Peas.Count == 0 && shooter.PresentationShotAge < 0, "An offscreen spawn cannot start a peashooter windup or projectile.");
    }

    /// <summary>Checks that attack windup follows the same target gates as the actual projectile branches.</summary>
    private static void ShooterWindupMatchesLaunch()
    {
        CheckShooterWindup(PlantKind.Peashooter, 9, 2, 4, 2, 4.2f, false, "A target behind the shooter does not start a forward attack.");
        CheckShooterWindup(PlantKind.Peashooter, 9, 2, 4, 2, 6f, true, "A forward ground target starts a shot.");
        CheckShooterWindup(PlantKind.Peashooter, 9, 2, 4, 2, 6f, false, "A submerged target cannot start a pea shot.", submerged: true);
        CheckShooterWindup(PlantKind.Peashooter, 9, 2, 4, 2, 6f, false, "A dropping target cannot start a pea shot.", dropRemaining: 1f);
        CheckShooterWindup(PlantKind.Peashooter, 9, 2, 4, 2, 3f, true, "A left-facing pea acquires a rear target.", modeFacesLeft: true);
        CheckShooterWindup(PlantKind.Peashooter, 9, 2, 4, 2, 6f, false, "A left-facing pea ignores a target in front.", modeFacesLeft: true);
        CheckShooterWindup(PlantKind.Cactus, 9, 2, 4, 2, 6f, true, "Cactus can acquire an airborne target.", airborne: true);
        CheckShooterWindup(PlantKind.ScaredyShroom, 11, 2, 4, 2, 5f, false, "Scaredy-shroom retracts when a zombie is near.");
        CheckShooterWindup(PlantKind.Threepeater, 9, 2, 4, 1, 6f, true, "Threepeater fires for an adjacent lane.");
        CheckShooterWindup(PlantKind.Threepeater, 9, 2, 4, 0, 6f, false, "Threepeater ignores a lane two rows away.");
        CheckShooterWindup(PlantKind.SplitPea, 9, 2, 4, 2, 3f, true, "Split pea fires backward within its acquisition interval.");
        CheckShooterWindup(PlantKind.SplitPea, 9, 2, 4, 2, -2f, false, "Split pea ignores targets beyond its rear acquisition interval.");
        CheckShooterWindup(PlantKind.Starfruit, 9, 2, 4, 2, 3f, true, "Starfruit fires for a target on its reverse path.");
        CheckShooterWindup(PlantKind.Starfruit, 9, 2, 4, 0, 10f, false, "Starfruit excludes targets at its far x limit.");
        CheckShooterWindup(PlantKind.Peashooter, 41, 2, 3, 2, 6f, true, "A forward pea begins its attack on the sloped roof.");
        CheckShooterWindup(PlantKind.CabbagePult, 41, 2, 3, 2, 6f, true, "A lobber clears the sloped roof.");
    }

    /// <summary>Compares the windup decision with the production launch branch for one controlled zombie.</summary>
    /// <param name="kind">The shooter kind.</param>
    /// <param name="level">The adventure level supplying the terrain.</param>
    /// <param name="plantRow">The shooter's lane.</param>
    /// <param name="column">The shooter's column.</param>
    /// <param name="zombieRow">The target's lane.</param>
    /// <param name="zombieX">The target's board x position.</param>
    /// <param name="expected">Whether the shooter should launch.</param>
    /// <param name="reason">The behavior being checked.</param>
    /// <param name="airborne">Whether the target is airborne.</param>
    /// <param name="submerged">Whether the target is underwater.</param>
    /// <param name="dropRemaining">The target's remaining drop time.</param>
    /// <param name="modeFacesLeft">Whether the plant is facing the rear lane direction.</param>
    private static void CheckShooterWindup(PlantKind kind, int level, int plantRow, int column, int zombieRow, float zombieX, bool expected, string reason,
        bool airborne = false, bool submerged = false, float dropRemaining = 0f, bool modeFacesLeft = false)
    {
        var game = Fixture(level);
        var plant = new Plant { Kind = kind, Row = plantRow, Column = column, ActionTimer = 0f, ModeFacesLeft = modeFacesLeft };
        var nativeTimed = kind == PlantKind.Peashooter || kind == PlantKind.SnowPea || kind == PlantKind.Repeater
            || kind == PlantKind.ScaredyShroom || kind == PlantKind.Cactus || kind == PlantKind.CabbagePult
            || kind == PlantKind.KernelPult || kind == PlantKind.MelonPult || kind == PlantKind.WinterMelon
            || kind == PlantKind.Threepeater || kind == PlantKind.SplitPea || kind == PlantKind.Starfruit;
        var multiHeadTimed = kind == PlantKind.Threepeater || kind == PlantKind.SplitPea;
        if (multiHeadTimed)
        {
            plant.MultiHeadTiming = AdventureMultiHeadTimingRules.FromRaw(kind, 0, 0, AdventureShootingHeads.None);
        }
        else if (kind == PlantKind.Starfruit)
        {
            plant.StarfruitLaunchCountdown = 0;
        }
        else if (nativeTimed)
        {
            plant.Timing = PlantsVsZombies.Timing.PlantTiming.FromRaw((PlantsVsZombies.Timing.PlantKind)(int)kind, 0, 0);
        }

        game.Plants.Add(plant);
        game.Zombies.Add(new Zombie
        {
            Kind = ZombieKind.Normal,
            Row = zombieRow,
            X = zombieX,
            Health = 270,
            StartingHealth = 270,
            Airborne = airborne,
            Submerged = submerged,
            DropRemaining = dropRemaining
        });

        var windup = nativeTimed ? (bool?)null : (bool)Invoke(game, "CanPresentAdventureWindup", plant)!;
        Invoke(game, "AdvanceAdventurePlants", .01f);
        var attackStarted = nativeTimed
            ? plant.PresentationShotAge >= 0f || Array.Exists(plant.PresentationHeadAge, age => age >= 0f)
            : game.Peas.Count > 0;
        Check((!windup.HasValue || windup.Value == expected) && attackStarted == expected, reason);
    }

    /// <summary>Checks the native initial periods, separate shooting delays, and shots after target loss.</summary>
    private static void NativeShooterTiming()
    {
        var cases = new[]
        {
            (PlantKind.ScaredyShroom, 150, 25),
            (PlantKind.Cactus, 150, 35),
            (PlantKind.CabbagePult, 300, 32),
            (PlantKind.KernelPult, 300, 30),
            (PlantKind.MelonPult, 300, 36),
            (PlantKind.WinterMelon, 300, 36)
        };

        foreach (var item in cases)
        {
            var kind = (PlantsVsZombies.Timing.PlantKind)(int)item.Item1;
            Check(PlantsVsZombies.Timing.PlantTiming.AttackPeriod(kind) == item.Item2
                && PlantsVsZombies.Timing.PlantTiming.InitialRandomBound(kind) == item.Item2 + 1,
                "Native attack period and first randomized countdown: " + item.Item1);
            var initial = PlantsVsZombies.Timing.PlantTiming.Initialize(kind, 0);
            var last = PlantsVsZombies.Timing.PlantTiming.Initialize(kind, item.Item2);
            Check(initial.Countdown == 0 && last.Countdown == item.Item2,
                "An original first attack may be immediate or use the full base period: " + item.Item1);

            var lateGame = Fixture(item.Item1 == PlantKind.ScaredyShroom ? 11 : 49);
            var latePlant = new Plant { Kind = item.Item1, Row = 2, Column = 4,
                Timing = PlantsVsZombies.Timing.PlantTiming.FromRaw(kind, 0, 0) };
            lateGame.Plants.Add(latePlant);
            Invoke(lateGame, "AdvanceAdventurePlants", .01f);
            Check(latePlant.Timing!.Value.Countdown >= item.Item2 - 14 && latePlant.Timing.Value.ShootingCountdown == 0,
                "An empty attack attempt resets the original full cycle: " + item.Item1);
            lateGame.Zombies.Add(new Zombie { Kind = ZombieKind.Normal, Row = 2, X = 6f, Health = 270, StartingHealth = 270 });
            for (var tick = 0; tick < item.Item3; tick++)
            {
                Invoke(lateGame, "AdvanceAdventurePlants", .01f);
            }

            Check(latePlant.Timing!.Value.ShootingCountdown == 0 && lateGame.Peas.Count == 0,
                "A newly arriving target cannot cause an immediate shot after a missed attack attempt: " + item.Item1);

            var game = Fixture(item.Item1 == PlantKind.ScaredyShroom ? 11 : 49);
            var plant = new Plant { Kind = item.Item1, Row = 2, Column = 4,
                Timing = PlantsVsZombies.Timing.PlantTiming.FromRaw(kind, 0, 0) };
            game.Plants.Add(plant);
            var target = new Zombie { Kind = ZombieKind.Normal, Row = 2, X = 6f, Health = 270, StartingHealth = 270 };
            game.Zombies.Add(target);
            Invoke(game, "AdvanceAdventurePlants", .01f);
            var delay = plant.Timing!.Value.ShootingCountdown;
            Check(delay == item.Item3,
                "The first valid target starts the species-specific shooting delay: " + item.Item1);
            Check(plant.PresentationShotAge == 0f && game.Peas.Count == 0,
                "An attack starts its complete animation before creating a projectile: " + item.Item1);

            game.Zombies.Remove(target);
            for (var tick = 0; tick < delay - 2; tick++)
            {
                Invoke(game, "AdvanceAdventurePlants", .01f);
                Check(game.Peas.Count == 0, "A pending projectile cannot launch early: " + item.Item1);
            }

            Invoke(game, "AdvanceAdventurePlants", .01f);
            Check(game.Peas.Count == 1 && game.Peas[0].SourceKind == item.Item1,
                "The scheduled projectile launches after its original target leaves: " + item.Item1);
            var shot = game.Peas[0];
            var lob = item.Item2 == 300;
            Check(shot.Lobbed == lob && shot.PiercesShield == lob, "The delayed projectile preserves its attack type: " + item.Item1);
            if (item.Item1 == PlantKind.KernelPult)
            {
                Check(shot.Butter == plant.PresentationButter, "Kernel-pult preserves the selected butter state through its fixed shooting delay.");
            }
            else if (item.Item1 == PlantKind.WinterMelon)
            {
                Check(shot.IsSnow && shot.ChillSplash && shot.Splash, "Winter Melon retains its chilling lob.");
            }

            if (lob)
            {
                Invoke(game, "AdvanceAdventureProjectilePresentation", shot, .01f);
                Check(shot.PresentationMotionInitialized, "A lob launched after target loss takes its original fallback path.");

                var movedGame = Fixture(49);
                var movedPlant = new Plant { Kind = item.Item1, Row = 2, Column = 4,
                    Timing = PlantsVsZombies.Timing.PlantTiming.FromRaw(kind, 0, 0) };
                movedGame.Plants.Add(movedPlant);
                var oldTarget = new Zombie { Kind = ZombieKind.Normal, Row = 2, X = 6f, Health = 270, StartingHealth = 270 };
                movedGame.Zombies.Add(oldTarget);
                Invoke(movedGame, "AdvanceAdventurePlants", .01f);
                movedGame.Zombies.Remove(oldTarget);
                movedGame.Zombies.Add(new Zombie { Kind = ZombieKind.Normal, Row = 2, X = 7f, Health = 270, StartingHealth = 270 });
                var movedDelay = movedPlant.Timing!.Value.ShootingCountdown;
                for (var tick = 0; tick < movedDelay - 1; tick++)
                {
                    Invoke(movedGame, "AdvanceAdventurePlants", .01f);
                }

                Check(movedGame.Peas.Count == 1, "A pending lob launches after its target changes: " + item.Item1);
                var movedShot = movedGame.Peas[0];
                Invoke(movedGame, "AdvanceAdventureProjectilePresentation", movedShot, .01f);
                Check(movedShot.PresentationMotionInitialized && movedShot.VelocityX < shot.VelocityX,
                    "A pending lob aims at the replacement target instead of the original or fallback position: " + item.Item1);
            }
        }

        var kernel = PlantsVsZombies.Timing.PlantTiming.FromRaw(PlantsVsZombies.Timing.PlantKind.KernelPult, 0, 0);
        var butter = PlantsVsZombies.Timing.PlantTiming.Tick(kernel,
            new PlantsVsZombies.Timing.PlantTimingInput(true, true, true, true, true, PlantsVsZombies.Timing.ShootingTrack.Body, true, 0, kernelButter: true));
        Check(butter.State.ShootingCountdown == 30, "The PC butter branch schedules thirty ticks.");
        var cactus = PlantsVsZombies.Timing.PlantTiming.FromRaw(PlantsVsZombies.Timing.PlantKind.Cactus, 0, 0);
        var high = PlantsVsZombies.Timing.PlantTiming.Tick(cactus,
            new PlantsVsZombies.Timing.PlantTimingInput(true, true, true, true, true, PlantsVsZombies.Timing.ShootingTrack.Body, true, 0, cactusHigh: true));
        Check(high.State.ShootingCountdown == 23, "An extended Cactus uses its twenty-three-tick high shot.");

    }

    /// <summary>Checks strict integer health boundaries and original per-track damage images.</summary>
    private static void DefensiveDamage()
    {
        foreach (var kind in new[] { PlantKind.Wallnut, PlantKind.TallNut, PlantKind.Pumpkin })
        {
            var maximum = kind == PlantKind.TallNut ? 8000 : 4000;
            var first = maximum * 2 / 3;
            var second = maximum / 3;
            Check(AdventureDefensivePlantRules.DamageStage(kind, first) == 0, "First damage threshold is strict: " + kind);
            Check(AdventureDefensivePlantRules.DamageStage(kind, first - 1) == 1, "First damage artwork appears below integer threshold: " + kind);
            Check(AdventureDefensivePlantRules.DamageStage(kind, second) == 1, "Second damage threshold is strict: " + kind);
            Check(AdventureDefensivePlantRules.DamageStage(kind, second - 1) == 2, "Heavy damage artwork appears below integer threshold: " + kind);
            Check(AdventureDefensivePlantRules.DamageImage(kind, maximum) == null, "Intact plants preserve their normal artwork: " + kind);
        }

        Check(AdventureDefensivePlantRules.DamageImage(PlantKind.Pumpkin, 1) == "IMAGE_REANIM_PUMPKIN_DAMAGE3", "Pumpkin uses the original DAMAGE3 artwork.");
        Check(AdventureDefensivePlantRules.DamageTrack(PlantKind.Pumpkin) == "Pumpkin_front", "Pumpkin damage changes only the front track.");
        Check(AdventureDefensivePlantRules.DamageTrack(PlantKind.Wallnut) == "anim_face", "Wallnut damage changes only its face track.");
        Check(AdventureDefensivePlantRules.DamageTrack(PlantKind.TallNut) == "anim_idle", "Tallnut damage changes only its idle track.");
    }

    /// <summary>Checks that damage particles occur once and eating changes speed without resetting phase.</summary>
    private static void DefensivePresentation()
    {
        var game = Fixture();
        var wallnut = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.Wallnut, 2, 4)!;
        wallnut.PresentationRate = 12f;
        Invoke(game, "AdvanceAdventureDefensivePlantPresentation", wallnut, 1f);
        Check(Math.Abs(wallnut.PresentationBodyFrames - 12f) < .0001f, "Defensive body accumulates animation frames.");
        wallnut.PresentationEatenFlashTicks = 25;
        Invoke(game, "AdvanceAdventureDefensivePlantPresentation", wallnut, 1f);
        Check(Math.Abs(wallnut.PresentationBodyFrames - 12.1f) < .0001f && wallnut.PresentationBodyRate == .1f, "Eating preserves phase at the original 0.1 fps.");
        wallnut.PresentationEatenFlashTicks = 0;
        Invoke(game, "AdvanceAdventureDefensivePlantPresentation", wallnut, .01f);
        Check(wallnut.PresentationBodyFrames > 12.1f && wallnut.PresentationBodyRate >= 10f && wallnut.PresentationBodyRate < 15f, "Recovery preserves phase and restores the original randomized idle speed.");
        wallnut.Health = 2665;
        Invoke(game, "AdvanceAdventureDefensivePlantPresentation", wallnut, .01f);
        Check(game.Presentation.PlantBiteEffects.Count == 1 && game.Presentation.PlantBiteEffects[0].ParticleId == 36, "First damage transition emits original large walnut debris.");
        Invoke(game, "AdvanceAdventureDefensivePlantPresentation", wallnut, .01f);
        Check(game.Presentation.PlantBiteEffects.Count == 1, "Unchanged damage stages do not repeat debris.");
        wallnut.Health = 1332;
        Invoke(game, "AdvanceAdventureDefensivePlantPresentation", wallnut, .01f);
        Check(game.Presentation.PlantBiteEffects.Count == 2, "Heavy damage transition emits original large walnut debris once.");
        var pumpkin = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.Pumpkin, 2, 4)!;
        pumpkin.Health = 1000;
        Invoke(game, "AdvanceAdventureDefensivePlantPresentation", pumpkin, .01f);
        Check(game.Presentation.PlantBiteEffects.Count == 2, "Pumpkin damage does not invent walnut debris.");
    }

    /// <summary>Checks original bite commit, digestion countdown, swallow duration, and miss recovery.</summary>
    private static void ChomperTransitions()
    {
        var game = Fixture();
        var chomper = (Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.Chomper, 2, 4)!;
        var zombie = new Zombie { Kind = ZombieKind.Normal, Row = 2, X = AdventureZombieNativeRules.GridX(470 - 36), Health = 270, StartingHealth = 270 };
        game.Zombies.Add(zombie);
        Invoke(game, "AdvanceAdventureChomper", chomper);
        var state = chomper.ChomperPresentation!;
        Check(state.NativePhase == 10 && state.Animation == "anim_bite" && state.Rate == 24 && !state.Loop && game.Zombies.Contains(zombie), "Chomper starts the original bite before consuming its target.");
        for (var tick = 0; tick < 69; tick++)
        {
            Invoke(game, "AdvanceAdventureChomper", chomper);
        }

        Check(game.Zombies.Contains(zombie), "Chomper does not consume before native bite tick seventy.");
        Invoke(game, "AdvanceAdventureChomper", chomper);
        Check(!game.Zombies.Contains(zombie) && state.NativePhase == 11, "Chomper consumes at original bite tick seventy.");
        Check(game.Presentation.ZombieSounds.Exists(sound => sound.Foley == 61), "Native bite commit requests the original bigchomp Foley.");
        Check(AdventureZombieFoleyData.Definitions[61].PitchRange == -2f && AdventureZombieFoleyData.Definitions[61].Samples[0] == "bigchomp", "Native Foley sixty-one preserves the original bigchomp sample and pitch.");
        for (var tick = 70; tick < 105; tick++)
        {
            Invoke(game, "AdvanceAdventureChomper", chomper);
        }

        Check(state.NativePhase == 13 && state.Animation == "anim_chew" && state.Rate == 15 && state.Loop && state.Countdown == 4000, "Successful bite changes to the original forty-second chew.");
        for (var tick = 0; tick < 3999; tick++)
        {
            Invoke(game, "AdvanceAdventureChomper", chomper);
        }

        Check(state.NativePhase == 13 && state.Countdown == 1, "Chewing lasts the full native four thousand ticks.");
        Invoke(game, "AdvanceAdventureChomper", chomper);
        Check(state.NativePhase == 14 && state.Animation == "anim_swallow" && state.Rate == 12 && !state.Loop, "Chewing finishes with the original swallowing animation.");
        for (var tick = 0; tick < 233; tick++)
        {
            Invoke(game, "AdvanceAdventureChomper", chomper);
        }

        Check(state.NativePhase == 14, "Swallowing does not end before its original twenty-eight frames.");
        Invoke(game, "AdvanceAdventureChomper", chomper);
        Check(state.NativePhase == 1 && state.Animation == "anim_idle", "Swallowing returns to original idle.");

        zombie = new Zombie { Kind = ZombieKind.Normal, Row = 2, X = AdventureZombieNativeRules.GridX(470 - 36), Health = 270, StartingHealth = 270 };
        game.Zombies.Add(zombie);
        Invoke(game, "AdvanceAdventureChomper", chomper);
        game.Zombies.Remove(zombie);
        for (var tick = 0; tick < 105; tick++)
        {
            Invoke(game, "AdvanceAdventureChomper", chomper);
        }

        Check(state.NativePhase == 1, "A target lost during windup finishes the bite and returns to idle without chewing.");
        var giant = new Zombie { Kind = ZombieKind.Gargantuar, Row = 2, X = AdventureZombieNativeRules.GridX(470 + 17), Health = 3000, StartingHealth = 3000 };
        game.Zombies.Add(giant);
        Invoke(game, "AdvanceAdventureChomper", chomper);
        for (var tick = 0; tick < 70; tick++)
        {
            Invoke(game, "AdvanceAdventureChomper", chomper);
        }

        Check(state.NativePhase == 12 && giant.Health == 2960 && game.Zombies.Contains(giant), "Gargantuar takes the original forty bite damage and is not swallowed.");
    }

    /// <summary>Creates a live newspaper zombie using the production spawn path and breaks its held paper.</summary>
    /// <param name="game">The isolated fixture simulation.</param>
    /// <param name="flags">The original damage flags.</param>
    /// <returns>The surviving newspaper zombie.</returns>
    private static Zombie BreakNewspaper(GameSimulation game, uint flags = 0)
    {
        var zombie = (Zombie)Invoke(game, "SpawnAdventureZombie", ZombieKind.Newspaper, 2, 6f, 0)!;
        zombie.Eating = true;
        Invoke(game, "HitAdventureZombie", zombie, 150f, false, flags);
        return zombie;
    }

    /// <summary>Checks original paper ripping, independent surprise playback, gasp completion, angry head, and roar admission.</summary>
    private static void NewspaperTransitions()
    {
        var game = Fixture();
        var zombie = BreakNewspaper(game);
        Check(zombie.Health == 270f && zombie.ShieldHealth == 0f && !zombie.Eating, "Breaking the newspaper stops eating without damaging the body.");
        Check(zombie.PresentationNativePhase == 30 && game.AdventurePresentationZombieAnimation(zombie) == "anim_gasp"
            && game.AdventurePresentationZombieRate(zombie) == 8f, "The original thirteen-frame eight-fps gasp plays before angry walking.");
        Check(zombie.PresentationNewspaperSurpriseAge == 0f && game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 67).Count == 1,
            "Unsuppressed paper loss creates one original surprise attachment and rip sound.");
        Check(AdventureZombieEquipmentRules.Image(zombie, "Zombie_paper", "Zombie_paper", "anim_head1") == null,
            "The angry head is not substituted during the gasp.");
        var x = zombie.X;
        Invoke(game, "AdvanceAdventureZombies", .1f);
        Check(zombie.X == x, "The surprised newspaper zombie stays still during the gasp.");
        Invoke(game, "AdvanceAdventureZombieFeedback", 1.24f);
        Check(zombie.PresentationNewspaperSurpriseAge >= 0f && zombie.PresentationNativePhase == 30,
            "The original fifteen-frame surprise attachment stays alive for its full source duration.");
        Invoke(game, "AdvanceAdventureZombieFeedback", .02f);
        Check(zombie.PresentationNewspaperSurpriseAge < 0f && zombie.PresentationNativePhase == 30,
            "The surprise attachment ends independently before the slower gasp.");
        Invoke(game, "AdvanceAdventureZombieFeedback", .38f);
        Check(zombie.PresentationNativePhase == 31 && game.AdventurePresentationZombieAnimation(zombie) == "anim_walk_nopaper",
            "Completing the gasp enters original angry walking.");
        Check(zombie.PresentationNativeVelocity >= .89f && zombie.PresentationNativeVelocity <= .91f,
            "Angry newspaper walking uses the original native speed interval.");
        Check(AdventureZombieEquipmentRules.Image(zombie, "Zombie_paper", "Zombie_paper", "anim_head1") == "IMAGE_REANIM_ZOMBIE_PAPER_MADHEAD",
            "Angry walking substitutes the original mad-head artwork.");
        Check(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 66).Count == 1, "The original roar plays once after gasp completion.");
        Invoke(game, "AdvanceAdventureZombieFeedback", 1f);
        Check(game.Presentation.ZombieSounds.FindAll(sound => sound.Foley == 66).Count == 1, "Angry walking does not repeat its transition roar.");
        var roar = AdventureZombieFoleyData.Definitions[66];
        Check(roar.PitchRange == -2f && roar.Samples.Length == 3 && roar.Samples[0] == "newspaper_rarrgh"
            && roar.Samples[1] == "newspaper_rarrgh2" && roar.Samples[2] == "newspaper_rarrgh2",
            "Native Foley sixty-six keeps the original one-to-two sample weighting and pitch range.");

        foreach (var flags in new[] { 1U, 16U })
        {
            var silent = Fixture();
            var paper = BreakNewspaper(silent, flags);
            Check(paper.PresentationNativePhase == 30 && paper.PresentationNewspaperSurpriseAge < 0f
                && !silent.Presentation.ZombieSounds.Exists(sound => sound.Foley == 67),
                "Original suppression flags keep the gasp while suppressing its question mark and rip sound.");
        }

        var frozen = Fixture();
        var frozenPaper = (Zombie)Invoke(frozen, "SpawnAdventureZombie", ZombieKind.Newspaper, 2, 6f, 0)!;
        frozenPaper.FrozenRemaining = 3f;
        frozenPaper.PresentationButter = 2f;
        Invoke(frozen, "HitAdventureZombie", frozenPaper, 150f, false, 0U);
        var gasp = frozenPaper.PresentationNewspaperGaspRemaining;
        Invoke(frozen, "AdvanceAdventureZombieFeedback", .5f);
        Check(frozenPaper.FrozenRemaining == 3f && frozenPaper.PresentationButter == 2f
            && frozenPaper.PresentationNewspaperGaspRemaining == gasp && frozenPaper.PresentationNewspaperSurpriseAge == .5f,
            "Paper loss preserves immobilization while the independent original question-mark attachment continues.");

        var crowded = Fixture();
        var crowdedPaper = BreakNewspaper(crowded);
        for (var index = 0; index < 10; index++)
        {
            Invoke(crowded, "SpawnAdventureZombie", ZombieKind.Normal, 0, 8f, 0);
        }

        Invoke(crowded, "AdvanceAdventureZombieFeedback", 2f);
        Check(crowdedPaper.PresentationNativePhase == 31 && !crowded.Presentation.ZombieSounds.Exists(sound => sound.Foley == 66),
            "More than ten live opponents suppress the original newspaper roar without blocking rage.");
        var headless = Fixture();
        var headlessPaper = BreakNewspaper(headless);
        headlessPaper.Headless = true;
        Invoke(headless, "AdvanceAdventureZombieFeedback", 2f);
        Check(headlessPaper.PresentationNativePhase == 31 && !headless.Presentation.ZombieSounds.Exists(sound => sound.Foley == 66),
            "A headless newspaper zombie completes its gasp without roaring.");
    }

    /// <summary>Checks live plant membership across the actual huge-wave warning and spawn path.</summary>
    private static void HugeWavePreservesPlants()
    {
        var game = Fixture();
        var expected = new List<Plant>();
        for (var row = 0; row < 5; row++)
        {
            for (var column = 0; column < 4; column++)
            {
                expected.Add((Plant)Invoke(game, "PlaceAdventurePlant", PlantKind.Wallnut, row, column)!);
            }
        }

        var kinds = new ZombieKind[24];
        for (var index = 0; index < kinds.Length; index++)
        {
            kinds[index] = index == 0 ? ZombieKind.Flag : index % 2 == 0 ? ZombieKind.Conehead : ZombieKind.Normal;
        }

        game.Adventure.Waves = Array.AsReadOnly(new[] { new AdventureWave { Index = 9, IsFlag = true, Zombies = Array.AsReadOnly(kinds) } });
        game.Adventure.CurrentWave = 0;
        game.ResetAdventureWaveSchedule(6, false, false);
        game.Update(8f);
        Check(game.Adventure.FlagsSpawned == 1 && game.SpawnedZombies == kinds.Length, "Huge-wave fixture executes the actual warning and full spawn path.");
        Check(game.Plants.Count == expected.Count, "Huge-wave spawning does not delete a plant column.");
        foreach (var plant in expected)
        {
            Check(game.Plants.Contains(plant) && plant.Health == 4000, "Huge-wave snapshots retain each original plant before enemies reach it.");
        }
    }
}
