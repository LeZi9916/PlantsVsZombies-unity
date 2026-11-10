using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PvZReplica.Levels;

internal static class Program
{
    private static int assertions;

    private static void Equal<T>(T expected, T actual, string description)
    {
        assertions++;
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(description + ": expected " + expected + ", got " + actual);
    }

    private static void Throws<T>(Action action, string description) where T : Exception
    {
        assertions++;
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException(description + ": expected " + typeof(T).Name);
    }

    private static string FindRoot()
    {
        foreach (string start in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
            for (DirectoryInfo d = new DirectoryInfo(start); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "PlantsVsZombies.exe")) &&
                    File.Exists(Path.Combine(d.FullName, "analysis", "ida", "level_1_1", "first-level-tables.json")))
                    return d.FullName;
        throw new DirectoryNotFoundException("Cannot find the local EXE and independent IDA export.");
    }

    // Translate a VA through the actual PE section table, without relying on IDA's export.
    private static int FileOffset(byte[] exe, int address)
    {
        int pe = BitConverter.ToInt32(exe, 0x3C);
        Equal(0x4550, BitConverter.ToInt32(exe, pe), "PE signature");
        int count = BitConverter.ToUInt16(exe, pe + 6);
        int optionalSize = BitConverter.ToUInt16(exe, pe + 20);
        int optional = pe + 24;
        Equal((ushort)0x10B, BitConverter.ToUInt16(exe, optional), "PE32 format");
        int rva = address - BitConverter.ToInt32(exe, optional + 28);
        int sections = optional + optionalSize;
        for (int i = 0; i < count; i++)
        {
            int section = sections + i * 40;
            int sectionRva = BitConverter.ToInt32(exe, section + 12);
            int rawSize = BitConverter.ToInt32(exe, section + 16);
            int rawOffset = BitConverter.ToInt32(exe, section + 20);
            if (rva >= sectionRva && rva - sectionRva < rawSize)
                return rawOffset + rva - sectionRva;
        }
        throw new InvalidDataException("No raw PE section for 0x" + address.ToString("X"));
    }

    private static int IntAt(byte[] exe, int address)
    {
        return BitConverter.ToInt32(exe, FileOffset(exe, address));
    }

    private static string WideAt(byte[] exe, int address)
    {
        int offset = FileOffset(exe, address);
        int end = offset;
        while (exe[end] != 0 || exe[end + 1] != 0) end += 2;
        return Encoding.Unicode.GetString(exe, offset, end - offset);
    }

    private static string AsciiAt(byte[] exe, int address)
    {
        int offset = FileOffset(exe, address);
        int end = offset;
        while (exe[end] != 0) end++;
        return Encoding.ASCII.GetString(exe, offset, end - offset);
    }

    private static string Sha256(byte[] bytes)
    {
        using (SHA256 sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }

    private static void TestExecutableEvidence(string root)
    {
        byte[] exe = File.ReadAllBytes(Path.Combine(root, "PlantsVsZombies.exe"));
        string evidence = Path.Combine(root, "analysis", "ida", "level_1_1");
        using (JsonDocument tables = JsonDocument.Parse(File.ReadAllText(Path.Combine(evidence, "first-level-tables.json"))))
        {
            JsonElement data = tables.RootElement;
            Equal("e1511201ec2ae9a7e30259f04bc2ec7272badb2574a2e9b405d0e916ae5c082c", Sha256(exe), "EXE identity");
            Equal(Sha256(exe), data.GetProperty("sha256").GetString(), "IDA export identity");
            Equal(LevelOneData.WaveCount, IntAt(exe, 0x74EDD8), "native first-level wave count");
            Equal(LevelOneData.WaveCount, data.GetProperty("level_wave_count").GetInt32(), "IDA wave count");
            Equal(LevelOneData.PeashooterCost, IntAt(exe, 0x74AA10), "seed 0 sun cost");
            Equal(LevelOneData.PeashooterRechargeNativeTicks, IntAt(exe, 0x74AA14), "seed 0 recharge field");
            Equal("PEASHOOTER", WideAt(exe, IntAt(exe, 0x74AA20)), "seed 0 label");
            Equal("ZOMBIE", AsciiAt(exe, IntAt(exe, 0x747B88)), "zombie 0 label");
            Equal(LevelOneData.StartingSun, IntAt(exe, 0x416DDF), "first-level sun instruction immediate");
            Equal(200, IntAt(exe, 0x41CBCC), "first planting sky-sun instruction immediate");
            Equal(100, IntAt(exe, 0x41CB6C), "second planting countdown instruction immediate");
            Equal(0x55555556, IntAt(exe, 0x415503), "signed divide-by-three multiplier");

            List<int> eligible = new List<int>();
            JsonElement records = data.GetProperty("zombie_records");
            Equal(34, records.GetArrayLength(), "zombie definition count");
            foreach (JsonElement row in records.EnumerateArray())
            {
                int id = row.GetProperty("id").GetInt32();
                int definition = 0x747B70 + 28 * id;
                Equal(id, IntAt(exe, definition), "native zombie ID " + id);
                Equal(row.GetProperty("cost").GetInt32(), IntAt(exe, definition + 8), "cost " + id);
                Equal(row.GetProperty("minimum_level").GetInt32(), IntAt(exe, definition + 12), "level gate " + id);
                Equal(row.GetProperty("minimum_wave_one_based").GetInt32(), IntAt(exe, definition + 16), "wave gate " + id);
                Equal(row.GetProperty("weight").GetInt32(), IntAt(exe, definition + 20), "weight " + id);
                Equal(row.GetProperty("first_level_flag").GetInt32(), IntAt(exe, 0x74EEA4 + 204 * id), "matrix " + id);
                if (id != 19 && row.GetProperty("minimum_level").GetInt32() <= 1 &&
                    row.GetProperty("weight").GetInt32() != 0 && row.GetProperty("first_level_flag").GetInt32() != 0)
                    eligible.Add(id);
            }
            // Native special case for ID 19 requires profile progress > 0; this is first play.
            Equal(1, eligible.Count, "first-play eligible type count");
            Equal(LevelOneData.NormalZombieId, eligible[0], "only eligible type");
            Equal(1, IntAt(exe, 0x747B78), "normal zombie budget cost");
            for (int wave = 0; wave < LevelOneData.WaveCount; wave++)
            {
                int nonFlagBudget = wave / 3 + 1;
                Equal(nonFlagBudget, LevelOneData.GetZombieCount(wave), "derived first-play wave " + wave);
                Equal(false, LevelOneData.IsFlagWave(wave), "no first-play flag wave " + wave);
                for (int i = 0; i < LevelOneData.GetZombieCount(wave); i++)
                    Equal(0, LevelOneData.GetZombieId(wave, i), "wave member");
            }
        }
        Equal("[ADVICE_CLICK_SEED_PACKET]", WideAt(exe, 0x70A59C), "selection message token");
        Equal("[ADVICE_CLICK_ON_GRASS]", WideAt(exe, 0x70A5FC), "planting message token");
        Equal("[ADVICE_PLANTED_PEASHOOTER]", WideAt(exe, 0x70A62C), "first planting token");
        Equal("[ADVICE_ZOMBIE_ONSLAUGHT]", WideAt(exe, 0x70A664), "onslaught token");
    }

    private static void TestLayoutAndBounds()
    {
        Equal(150, LevelOneData.StartingSun, "first-play sun");
        Equal(1, LevelOneData.SeedPacketCount, "seed count");
        Equal(2, LevelOneData.ZombieSpawnRow, "only eligible native spawn row");
        Equal(7, LevelOneData.NativeRewardItemType, "first-play reward item type");
        for (int row = 0; row < LevelOneData.NativeRowSlots; row++)
            for (int column = 0; column < LevelOneData.Columns; column++)
                Equal(row == 2, LevelOneData.IsGrassCell(column, row), "native grid cell");
        Throws<ArgumentOutOfRangeException>(() => LevelOneData.IsGrassCell(-1, 2), "negative column");
        Throws<ArgumentOutOfRangeException>(() => LevelOneData.IsGrassCell(9, 2), "last column boundary");
        Throws<ArgumentOutOfRangeException>(() => LevelOneData.IsGrassCell(0, 6), "row boundary");
        Throws<ArgumentOutOfRangeException>(() => LevelOneData.GetZombieCount(4), "wave boundary");
        Throws<ArgumentOutOfRangeException>(() => LevelOneData.GetZombieId(0, 1), "member boundary");
    }

    private static void TestTutorial()
    {
        LevelOneTutorial tutorial = new LevelOneTutorial();
        Equal(LevelOneTutorialStage.NotStarted, tutorial.Stage, "initial tutorial");
        Equal(false, tutorial.TutorialBlocksWaveCountdown, "not-started tutorial gate");
        Equal(false, tutorial.OnSeedPickedUp().Changed, "out-of-order pickup ignored");
        Equal(LevelOneTutorialStage.AwaitingSeedSelection, tutorial.OnFirstActiveGameplayUpdate().Stage, "start teaching");
        Equal(true, tutorial.TutorialBlocksWaveCountdown, "selection freezes wave countdown");
        Equal(LevelOneTutorialStage.AwaitingPlantPlacement, tutorial.OnSeedPickedUp().Stage, "pick seed");
        Equal(LevelOneTutorialStage.AwaitingSeedSelection, tutorial.OnPlantingCursorCancelled().Stage, "cancel returns to selection");
        tutorial.OnSeedPickedUp();
        Throws<ArgumentOutOfRangeException>(() => tutorial.OnPlantPlaced(0), "invalid accepted count");
        LevelOneTutorialEffect first = tutorial.OnPlantPlaced(1);
        Equal(LevelOneTutorialStage.AwaitingUsableSeed, first.Stage, "first plant");
        Equal((int?)200, first.SkySunCountdownNativeTicks, "first plant resets sky sun timer");
        Equal((int?)null, first.WaveCountdownAndInitialNativeTicks, "first plant leaves wave timer");
        Equal(true, tutorial.TutorialBlocksWaveCountdown, "waiting for seed freezes wave countdown");
        Equal(false, tutorial.OnSeedPickedUp().Changed, "cannot skip usability event");
        Equal(LevelOneTutorialStage.AwaitingSeedSelection, tutorial.OnSeedBecameUsable().Stage, "usable seed");
        tutorial.OnSeedPickedUp();
        Equal(LevelOneTutorialStage.AwaitingUsableSeed, tutorial.OnPlantPlaced(1).Stage, "native array count, not lifetime placements");
        tutorial.OnSeedBecameUsable();
        tutorial.OnSeedPickedUp();
        LevelOneTutorialEffect second = tutorial.OnPlantPlaced(2);
        Equal(LevelOneTutorialStage.ZombieOnslaughtPrompt, second.Stage, "two active plants");
        Equal((int?)100, second.WaveCountdownAndInitialNativeTicks, "onslaught resets both wave counters");
        Equal((int?)null, second.SkySunCountdownNativeTicks, "onslaught leaves sky sun timer");
        Equal(false, tutorial.TutorialBlocksWaveCountdown, "onslaught tutorial gate released");
        Equal(false, tutorial.OnFirstActiveGameplayUpdate().Changed, "no restart");
        Equal(false, tutorial.OnSeedBecameUsable().Changed, "no unverified later transition");
    }

    private static void TestVictoryFragments()
    {
        for (int ba = 0; ba < 2; ba++)
            for (int ec = 0; ec < 2; ec++)
                for (int b8 = 0; b8 < 2; b8++)
                    for (int phase = -1; phase <= 5; phase++)
                    {
                        NativeVictoryZombieFields z = new NativeVictoryZombieFields(ba != 0, ec != 0, phase, b8 != 0);
                        bool expected = ba != 0 && ec == 0 && b8 == 0 && (phase < 1 || phase > 3);
                        Equal(expected, LevelOneVictoryEvidence.IsNativeVictoryBlocker(z), "native zombie predicate");
                    }
        NativeVictoryZombieFields blocker = new NativeVictoryZombieFields(true, false, 0, false);
        NativeVictoryZombieFields ignored = new NativeVictoryZombieFields(true, false, 2, false);
        Equal(false, LevelOneVictoryEvidence.MeetsKnownRewardPrerequisites(3, new[] { ignored }), "unspawned waves");
        Equal(false, LevelOneVictoryEvidence.MeetsKnownRewardPrerequisites(4, new[] { blocker }), "blocking zombie");
        Equal(true, LevelOneVictoryEvidence.MeetsKnownRewardPrerequisites(4, new[] { ignored }), "necessary conditions only");
        Equal(true, LevelOneVictoryEvidence.MeetsKnownRewardPrerequisites(5, new NativeVictoryZombieFields[0]), "native greater-than case");
        Throws<ArgumentOutOfRangeException>(() => LevelOneVictoryEvidence.MeetsKnownRewardPrerequisites(-1, new NativeVictoryZombieFields[0]), "negative wave count");
        Throws<ArgumentNullException>(() => LevelOneVictoryEvidence.MeetsKnownRewardPrerequisites(4, null), "missing iterator");
    }

    private static void Main()
    {
        TestExecutableEvidence(FindRoot());
        TestLayoutAndBounds();
        TestTutorial();
        TestVictoryFragments();
        int experimentAssertions = ExperimentalModeTests.Run();
        Console.WriteLine("PASS: {0} assertions; IDA-exported data also checked directly against the local PE.", assertions);
        Console.WriteLine("PASS: {0} experimental-mode integration assertions.", experimentAssertions);
        Console.WriteLine("Static and unit checks only; no original-game runtime comparison was performed.");
    }
}
