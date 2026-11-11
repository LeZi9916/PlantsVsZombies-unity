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
        DefensiveDamage();
        DefensivePresentation();
        ChomperTransitions();
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
    private static GameSimulation Fixture()
    {
        var game = new GameSimulation(new GameRules { AdventureLevel = 9 }, 7529);
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
