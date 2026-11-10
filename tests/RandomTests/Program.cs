using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using PvZReplica.Randomness;

internal static class Program
{
    private static int checks;

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception(message);
    }

    private static string Workspace()
    {
        DirectoryInfo directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "PlantsVsZombies.exe")) &&
                Directory.Exists(Path.Combine(directory.FullName, "PvZUnity"))) return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("Could not find the workspace and original executable.");
    }

    private static int Main()
    {
        string workspace = Workspace();
        var start = new ProcessStartInfo { RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = workspace };
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            start.FileName = "wsl.exe";
            start.ArgumentList.Add("-d");
            start.ArgumentList.Add("Ubuntu-24.04");
            start.ArgumentList.Add("--");
            start.ArgumentList.Add("sh");
            start.ArgumentList.Add("-c");
            start.ArgumentList.Add("PYTHONDONTWRITEBYTECODE=1 PYTHONPATH=analysis/ida/plant_timing/vendor python3 analysis/ida/random/machine_oracle.py");
        }
        else
        {
            start.FileName = "python3";
            start.ArgumentList.Add(Path.Combine(workspace, "analysis", "ida", "random", "machine_oracle.py"));
            start.Environment["PYTHONPATH"] = Path.Combine(workspace, "analysis", "ida", "plant_timing", "vendor");
            start.Environment["PYTHONDONTWRITEBYTECODE"] = "1";
        }
        using (Process process = Process.Start(start))
        {
            string json = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0) throw new Exception("Native oracle failed (" + process.ExitCode + "): " + error);
            using (JsonDocument document = JsonDocument.Parse(json))
            {
                foreach (JsonElement vector in document.RootElement.GetProperty("vectors").EnumerateArray())
                {
                    uint seed = vector.GetProperty("seed").GetUInt32();
                    var raw = new OriginalRandom(seed);
                    int index = 0;
                    foreach (JsonElement expected in vector.GetProperty("raw").EnumerateArray())
                    {
                        uint value = raw.Next31();
                        Check(value == expected.GetUInt32(), "Raw mismatch for seed " + seed + " at " + index);
                        Check(value <= 0x7fffffffu, "Output exceeded 31 bits");
                        index++;
                    }
                    var bounded = new OriginalRandom(seed);
                    index = 0;
                    foreach (JsonElement entry in vector.GetProperty("bounded").EnumerateArray())
                    {
                        uint bound = entry[0].GetUInt32(), expected = entry[1].GetUInt32();
                        uint value = bounded.NextBounded(bound);
                        Check(value == expected, "Bounded mismatch for seed " + seed + " at " + index);
                        Check(value < bound, "Bounded result outside range");
                        index++;
                    }
                }
            }
        }

        var zeroSeed = new OriginalRandom(0);
        var defaultSeed = new OriginalRandom(OriginalRandom.DefaultSeed);
        for (int i = 0; i < 1300; i++) Check(zeroSeed.Next31() == defaultSeed.Next31(), "Zero seed fallback");
        var invalid = new OriginalRandom(123);
        var control = new OriginalRandom(123);
        bool threw = false;
        try { invalid.NextBounded(0); } catch (ArgumentOutOfRangeException) { threw = true; }
        Check(threw, "Zero bound must be rejected");
        Check(invalid.Next31() == control.Next31(), "Rejected bound must not advance state");
        Console.WriteLine("PASS: " + checks + " checks against original x86 RNG and edge cases.");
        return 0;
    }
}
