using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using PlantsVsZombies.Timing;

internal static class Program
{
    private sealed class Request
    {
        public List<Scenario> cases = new List<Scenario>();
    }

    private sealed class Scenario
    {
        public string name;
        public int kind;
        public int initialDraw;
        public List<TickRequest> ticks = new List<TickRequest>();
    }

    private sealed class TickRequest
    {
        public bool plant = true;
        public bool shooting = true;
        public bool main = true;
        public bool sun = true;
        public bool target = true;
        public bool reset;
        public bool bodyInstance = true;
        public bool headInstance = true;
        public string track = "head";
        public int draw;
        public int? setCountdown;
        public int? setShooting;
    }

    private static int assertions;

    private static void Check(bool condition, string context)
    {
        assertions++;
        if (!condition)
            throw new Exception(context);
    }

    private static Scenario Add(Request request, string name, PlantKind kind, int draw)
    {
        var scenario = new Scenario { name = name, kind = (int)kind, initialDraw = draw };
        request.cases.Add(scenario);
        return scenario;
    }

    private static void AddTicks(Scenario scenario, int count, int draw)
    {
        for (int i = 0; i < count; i++)
            scenario.ticks.Add(new TickRequest { draw = draw });
    }

    private static Request MakeRequest()
    {
        var request = new Request();
        AddTicks(Add(request, "pea", PlantKind.Peashooter, 0), 36, 0);
        AddTicks(Add(request, "snow", PlantKind.SnowPea, 0), 36, 14);
        AddTicks(Add(request, "repeater", PlantKind.Repeater, 26), 52, 0);
        var bodyRepeater = Add(request, "repeater_body", PlantKind.Repeater, 26);
        for (int i = 0; i < 54; i++)
            bodyRepeater.ticks.Add(new TickRequest { track = "body", draw = 0 });
        AddTicks(Add(request, "sun_min", PlantKind.Sunflower, 0), 2651, 0);
        AddTicks(Add(request, "sun_max", PlantKind.Sunflower, 950), 3751, 150);

        foreach (PlantKind kind in new[] { PlantKind.Peashooter, PlantKind.SnowPea, PlantKind.Repeater, PlantKind.Sunflower })
        {
            Add(request, "init_min_" + kind, kind, 0);
            Add(request, "init_max_" + kind, kind, PlantTiming.InitialRandomBound(kind) - 1);
        }

        var overlap = Add(request, "two_projectiles", PlantKind.Peashooter, 0);
        overlap.ticks.Add(new TickRequest { setCountdown = 1, setShooting = 2, track = "none", draw = 0 });
        var targetLost = Add(request, "target_lost", PlantKind.Repeater, 100);
        for (int i = 1; i <= 101; i++)
            targetLost.ticks.Add(new TickRequest { target = i == 75, draw = 0 });

        var random = new Random(0x134C0);
        int[] attackCountdowns = { 0, 1, 2, 24, 25, 26, 27, 35, 136, 150 };
        int[] sunCountdowns = { 0, 1, 2, 99, 100, 101, 300, 1250, 2350, 2500 };
        int[] shootingCountdowns = { 0, 1, 2, 3, 25, 26, 29, 35 };
        string[] tracks = { "head", "body", "none" };
        foreach (PlantKind kind in new[] { PlantKind.Peashooter, PlantKind.SnowPea, PlantKind.Repeater, PlantKind.Sunflower })
        {
            var scenario = Add(request, "matrix_" + kind, kind, 0);
            for (int i = 0; i < 800; i++)
            {
                bool isSun = kind == PlantKind.Sunflower;
                int[] countdowns = isSun ? sunCountdowns : attackCountdowns;
                int instances = random.Next(3);
                scenario.ticks.Add(new TickRequest {
                    setCountdown = countdowns[random.Next(countdowns.Length)],
                    setShooting = isSun ? 0 : shootingCountdowns[random.Next(shootingCountdowns.Length)],
                    plant = random.Next(5) != 0,
                    shooting = random.Next(4) != 0,
                    main = random.Next(4) != 0,
                    sun = random.Next(4) != 0,
                    target = random.Next(3) != 0,
                    reset = random.Next(2) != 0,
                    headInstance = instances == 0,
                    bodyInstance = instances != 2,
                    track = tracks[random.Next(tracks.Length)],
                    draw = random.Next(isSun ? 151 : 15)
                });
            }
        }
        return request;
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "PlantsVsZombies.exe")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new Exception("Could not locate the original PlantsVsZombies.exe.");
    }

    private static JsonDocument RunOracle(Request request)
    {
        string root = FindRoot();
        string script = Path.Combine(root, "analysis", "ida", "plant_timing", "machine_oracle.py");
        string vendor = Path.Combine(root, "analysis", "ida", "plant_timing", "vendor");
        var process = new Process();
        process.StartInfo.WorkingDirectory = root;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            process.StartInfo.FileName = "wsl.exe";
            process.StartInfo.ArgumentList.Add("-d");
            process.StartInfo.ArgumentList.Add(Environment.GetEnvironmentVariable("PLANT_TIMING_WSL_DISTRO") ?? "Ubuntu-24.04");
            process.StartInfo.ArgumentList.Add("--");
            process.StartInfo.ArgumentList.Add("sh");
            process.StartInfo.ArgumentList.Add("-c");
            process.StartInfo.ArgumentList.Add("PYTHONPATH=analysis/ida/plant_timing/vendor python3 analysis/ida/plant_timing/machine_oracle.py");
        }
        else
        {
            process.StartInfo.FileName = "python3";
            process.StartInfo.ArgumentList.Add(script);
            process.StartInfo.Environment["PYTHONPATH"] = vendor;
        }
        process.Start();
        var options = new JsonSerializerOptions {
            IncludeFields = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        process.StandardInput.Write(JsonSerializer.Serialize(request, options));
        process.StandardInput.Close();
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
            throw new Exception("Machine-code oracle failed (" + process.ExitCode + "): " + error);
        return JsonDocument.Parse(output);
    }

    private static ShootingTrack ParseTrack(string value)
    {
        if (value == "head") return ShootingTrack.Head;
        if (value == "body") return ShootingTrack.Body;
        if (value == "none") return ShootingTrack.None;
        throw new Exception("Unknown track " + value);
    }

    private static int[] EventTicks(JsonElement ticks, string eventName)
    {
        var result = new List<int>();
        int i = 0;
        foreach (JsonElement tick in ticks.EnumerateArray())
        {
            i++;
            if (tick.GetProperty(eventName).GetArrayLength() != 0)
                result.Add(i);
        }
        return result.ToArray();
    }

    private static void ExpectTicks(JsonElement ticks, string eventName, params int[] expected)
    {
        int[] actual = EventTicks(ticks, eventName);
        Check(actual.Length == expected.Length, eventName + " event count");
        for (int i = 0; i < expected.Length; i++)
            Check(actual[i] == expected[i], eventName + " event tick " + i);
    }

    private static void Verify(Request request, JsonDocument doc)
    {
        JsonElement cases = doc.RootElement.GetProperty("cases");
        Check(cases.GetArrayLength() == request.cases.Count, "scenario count");
        int totalTicks = 0;
        for (int c = 0; c < request.cases.Count; c++)
        {
            Scenario scenario = request.cases[c];
            JsonElement actual = cases[c];
            PlantKind kind = (PlantKind)scenario.kind;
            PlantTimingState state = PlantTiming.Initialize(kind, scenario.initialDraw);
            string label = scenario.name;
            Check(actual.GetProperty("initialBounds")[0].GetInt32() == PlantTiming.InitialRandomBound(kind), label + " init bound");
            Check(actual.GetProperty("initial").GetProperty("countdown").GetInt32() == state.Countdown, label + " init countdown");
            Check(actual.GetProperty("initial").GetProperty("shooting").GetInt32() == state.ShootingCountdown, label + " init shooting");
            JsonElement ticks = actual.GetProperty("ticks");
            Check(ticks.GetArrayLength() == scenario.ticks.Count, label + " tick count");
            if (label == "pea" || label == "snow") ExpectTicks(ticks, "projectiles", 35);
            if (label == "repeater") ExpectTicks(ticks, "projectiles", 26, 51);
            if (label == "repeater_body") ExpectTicks(ticks, "projectiles", 54);
            if (label == "sun_min") ExpectTicks(ticks, "suns", 300, 2650);
            if (label == "sun_max") ExpectTicks(ticks, "suns", 1250, 3750);
            if (label == "two_projectiles") Check(ticks[0].GetProperty("projectiles").GetArrayLength() == 2, label);
            if (label == "target_lost") ExpectTicks(ticks, "projectiles", 100);

            for (int i = 0; i < scenario.ticks.Count; i++)
            {
                TickRequest t = scenario.ticks[i];
                JsonElement expected = ticks[i];
                if (t.setCountdown.HasValue)
                    state = PlantTiming.FromRaw(kind, t.setCountdown.Value, t.setShooting.Value);
                ShootingTrack track = ParseTrack(t.track);
                if ((track == ShootingTrack.Head && !t.headInstance) ||
                    (track == ShootingTrack.Body && !t.bodyInstance))
                    track = ShootingTrack.None;
                var input = new PlantTimingInput(t.plant, t.shooting, t.main, t.sun,
                    t.target, track, t.reset && (t.headInstance || t.bodyInstance), t.draw);
                int bound = PlantTiming.RequiredRandomBound(state, input);
                PlantTimingResult result = PlantTiming.Tick(state, input);
                string at = label + " tick " + (i + 1);
                JsonElement nativeState = expected.GetProperty("state");
                Check(result.State.Countdown == nativeState.GetProperty("countdown").GetInt32(), at + " countdown");
                Check(result.State.ShootingCountdown == nativeState.GetProperty("shooting").GetInt32(), at + " shooting countdown");
                JsonElement bounds = expected.GetProperty("drawBounds");
                Check(bounds.GetArrayLength() == (bound == 0 ? 0 : 1), at + " draw count");
                if (bound != 0) Check(bounds[0].GetInt32() == bound, at + " draw bound");
                JsonElement attempts = expected.GetProperty("attempts");
                Check(attempts.GetArrayLength() == (result.Attempt == AttackAttempt.None ? 0 : 1), at + " attempts");
                if (result.Attempt != AttackAttempt.None)
                {
                    string nativeAttempt = attempts[0].GetString();
                    Check(nativeAttempt == (result.Attempt == AttackAttempt.RepeaterAt25 ? "at25" : "expired"), at + " attempt kind");
                }
                Check((expected.GetProperty("animations").GetInt32() != 0) == result.AnimationStarted, at + " animation");
                JsonElement projectiles = expected.GetProperty("projectiles");
                Check(projectiles.GetArrayLength() == result.ProjectilesCreated, at + " projectile count");
                foreach (JsonElement projectile in projectiles.EnumerateArray())
                    Check(projectile.GetInt32() == (kind == PlantKind.SnowPea ? 1 : 0), at + " projectile type");
                JsonElement suns = expected.GetProperty("suns");
                Check(suns.GetArrayLength() == (result.SunCreated ? 1 : 0), at + " sun count");
                foreach (JsonElement sun in suns.EnumerateArray())
                    Check(sun[0].GetInt32() == 4 && sun[1].GetInt32() == 2, at + " sun type/motion");
                state = result.State;
                totalTicks++;
            }
        }
        Console.WriteLine("PASS: " + request.cases.Count + " machine-code scenarios, " + totalTicks +
            " ticks, " + assertions + " assertions; original x86 execution matched C#.");
    }

    private static int Main()
    {
        try
        {
            CheckThrows<ArgumentOutOfRangeException>(() => PlantTiming.Initialize(PlantKind.Peashooter, 151), "shooter initial draw");
            CheckThrows<ArgumentOutOfRangeException>(() => PlantTiming.Initialize(PlantKind.Sunflower, -1), "sun initial draw");
            var due = PlantTiming.FromRaw(PlantKind.Peashooter, 1, 0);
            var missingDraw = new PlantTimingInput(true, true, true, true, true, ShootingTrack.Head, false, -1);
            CheckThrows<ArgumentOutOfRangeException>(() => PlantTiming.Tick(due, missingDraw), "required draw");
            var notDue = PlantTiming.FromRaw(PlantKind.Peashooter, 2, 0);
            Check(PlantTiming.RequiredRandomBound(notDue, missingDraw) == 0, "unneeded draw bound");
            Check(PlantTiming.Tick(notDue, missingDraw).State.Countdown == 1, "unneeded draw is ignored");
            Request request = MakeRequest();
            using (JsonDocument oracle = RunOracle(request))
                Verify(request, oracle);
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }

    private static void CheckThrows<T>(Action action, string context) where T : Exception
    {
        try { action(); }
        catch (T) { Check(true, context); return; }
        throw new Exception("Expected " + typeof(T).Name + ": " + context);
    }
}
