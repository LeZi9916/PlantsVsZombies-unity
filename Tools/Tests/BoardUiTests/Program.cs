using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PlantsVsZombies.BoardUI;

internal static class Program
{
    private static int assertions;

    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Equal(int expected, int actual, string message)
    {
        Check(expected == actual, message + ": expected " + expected + ", got " + actual);
    }

    private static void Rect(NativeRect actual, int x, int y, int width, int height, string message)
    {
        Equal(x, actual.X, message + " x");
        Equal(y, actual.Y, message + " y");
        Equal(width, actual.Width, message + " width");
        Equal(height, actual.Height, message + " height");
    }

    private static void Point(NativePoint actual, int x, int y, string message)
    {
        Equal(x, actual.X, message + " x");
        Equal(y, actual.Y, message + " y");
    }

    private static string FindWorkspace()
    {
        foreach (string initial in new[] { Environment.CurrentDirectory, AppContext.BaseDirectory })
        {
            for (DirectoryInfo dir = new DirectoryInfo(initial); dir != null; dir = dir.Parent)
            {
                if (File.Exists(Path.Combine(dir.FullName, "PlantsVsZombies.exe")) &&
                    Directory.Exists(Path.Combine(dir.FullName, "extracted_assets"))) return dir.FullName;
            }
        }
        throw new DirectoryNotFoundException("Original executable and resources not found.");
    }

    private sealed class Exe : IDisposable
    {
        private readonly byte[] data;
        private readonly MemoryStream stream;
        private readonly PEReader reader;

        public Exe(string path)
        {
            data = File.ReadAllBytes(path);
            stream = new MemoryStream(data, false);
            reader = new PEReader(stream);
            Check(reader.PEHeaders.PEHeader.Magic == PEMagic.PE32, "PE32");
            Check(reader.PEHeaders.PEHeader.ImageBase == 0x400000, "image base");
            using (SHA256 hash = SHA256.Create())
            {
                string actual = BitConverter.ToString(hash.ComputeHash(data)).Replace("-", "").ToLowerInvariant();
                Check(actual == "e1511201ec2ae9a7e30259f04bc2ec7272badb2574a2e9b405d0e916ae5c082c", "EXE SHA-256");
            }
        }

        private int Offset(int va)
        {
            int rva = checked(va - (int)reader.PEHeaders.PEHeader.ImageBase);
            foreach (SectionHeader section in reader.PEHeaders.SectionHeaders)
            {
                if (rva >= section.VirtualAddress && rva - section.VirtualAddress < section.SizeOfRawData)
                    return checked(section.PointerToRawData + rva - section.VirtualAddress);
            }
            throw new InvalidDataException("No raw data at VA " + va.ToString("X"));
        }

        public void Bytes(int va, string hex, string message)
        {
            int offset = Offset(va);
            string[] values = hex.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < values.Length; i++)
                Equal(Convert.ToInt32(values[i], 16), data[offset + i], message + " byte " + i);
        }

        public void Ascii(int va, string expected)
        {
            int offset = Offset(va);
            int length = Array.IndexOf(data, (byte)0, offset) - offset;
            Check(length >= 0, "zero-terminated resource name");
            Check(Encoding.ASCII.GetString(data, offset, length) == expected, "resource name " + expected);
        }

        public void Dispose()
        {
            reader.Dispose();
            stream.Dispose();
        }
    }

    private static void MachineEvidence(string root)
    {
        using (Exe exe = new Exe(Path.Combine(root, "PlantsVsZombies.exe")))
        {
            exe.Ascii(0x714378, "IMAGE_BACKGROUND1UNSODDED");
            exe.Ascii(0x714394, "IMAGE_SOD1ROW");
            exe.Ascii(0x716AB8, "IMAGE_SEEDBANK");
            exe.Ascii(0x714B18, "FONT_CONTINUUMBOLD14");
            exe.Bytes(0x4890CB, "a3 d8 2c 75 00", "background resource binding");
            exe.Bytes(0x489121, "a3 38 35 75 00", "sod resource binding");
            exe.Bytes(0x490C4D, "a3 74 35 75 00", "seed bank resource binding");
            exe.Bytes(0x48B16A, "a3 30 2d 75 00", "sun font resource binding");
            exe.Bytes(0x421809, "a1 d8 2c 75 00 33 ff 57 68 24 ff ff ff", "background x");
            exe.Bytes(0x421824, "6a 01 e8 b5 38 17 00 50 8b 83 6c 55 00 00 57 b9 e8 03 00 00 33 d2", "sod interpolation inputs");
            exe.Bytes(0x42185A, "68 09 01 00 00 6a 13", "sod destination");
            exe.Bytes(0x421A01, "89 7c 24 24 89 7c 24 28", "sod source x and y");
            exe.Bytes(0x421A0B, "89 74 24 2c e8 bc 16 17 00", "sod source width and draw");
            exe.Bytes(0x421A77, "83 bb 9c 55 00 00 02", "tutorial overlay state");
            exe.Bytes(0x421AC4, "8b 15 38 35 75 00 68 09 01 00 00 6a 13", "tutorial overlay resource and destination");
            exe.Bytes(0x5950EA, "8b 40 24", "image width accessor");
            exe.Bytes(0x59510A, "8b 40 28", "image height accessor");
            exe.Bytes(0x526CAA, "d9 44 24 0c e9 fa 00 00 00", "linear interpolation case");
            exe.Bytes(0x526F73, "dc 05 a8 71 72 00", "positive round adjustment");
            exe.Bytes(0x446559, "8b 15 f8 5d 75 00 89 51 08 f7 d8 89 41 0c", "entrance position");
            exe.Bytes(0x425CC4, "db 40 08 d8 46 08 d9 5e 08 db 40 0c d8 46 0c d9 5e 0c", "seed bank translation");
            exe.Bytes(0x49F153, "8b 45 04 d9 46 08 da 60 40 d9 5e 08", "cancel board x");
            exe.Bytes(0x49F15F, "8b 4d 04 d9 46 0c da 61 44 d9 5e 0c", "cancel board y");
            exe.Bytes(0x427EAE, "33 c0 c3", "one-slot bank extension");
            exe.Bytes(0x49F207, "2b f8 53 83 c0 0c 51 83 ef 0c", "bank end-cap source width and x");
            exe.Bytes(0x49F232, "8b 48 24 53 83 e9 0c", "bank end-cap destination x");
            exe.Bytes(0x427E66, "8b c7 6b c0 3b 83 c0 55", "first seed x");
            exe.Bytes(0x416F61, "89 46 08 c7 46 0c 08 00 00 00", "seed x and y");
            exe.Bytes(0x49EB44, "c7 46 38 32 00 00 00 c7 46 3c 46 00 00 00", "slot object size");
            exe.Bytes(0x49F383, "6a 02 89 50 0c a1 30 2d 75 00 50 6a 4e 6a 26", "sun font and anchor");
            exe.Bytes(0x49F32C, "99 b9 14 00 00 00 f7 f9 83 fa 0a", "sun flash period and phase");
            exe.Bytes(0x5271C8, "ff d2 99 2b c2 d1 f8", "centered text integer division");
        }
    }

    private static void ImageSize(string path, int width, int height)
    {
        byte[] bytes = File.ReadAllBytes(path);
        int actualWidth = 0;
        int actualHeight = 0;
        if (bytes.Length >= 24 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4e && bytes[3] == 0x47)
        {
            actualWidth = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4));
            actualHeight = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4));
        }
        else if (bytes.Length > 2 && bytes[0] == 0xff && bytes[1] == 0xd8)
        {
            int i = 2;
            while (i + 3 < bytes.Length)
            {
                if (bytes[i++] != 0xff) throw new InvalidDataException("Invalid JPEG marker");
                while (i < bytes.Length && bytes[i] == 0xff) i++;
                int marker = bytes[i++];
                if (marker == 0xd9 || marker == 0xda) break;
                if (marker == 1 || (marker >= 0xd0 && marker <= 0xd7)) continue;
                int length = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(i, 2));
                if (length < 2 || i + length > bytes.Length) throw new InvalidDataException("Invalid JPEG segment");
                if ((marker >= 0xc0 && marker <= 0xc3) || (marker >= 0xc5 && marker <= 0xc7) ||
                    (marker >= 0xc9 && marker <= 0xcb) || (marker >= 0xcd && marker <= 0xcf))
                {
                    actualHeight = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(i + 3, 2));
                    actualWidth = BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(i + 5, 2));
                    break;
                }
                i += length;
            }
        }
        Equal(width, actualWidth, Path.GetFileName(path) + " width");
        Equal(height, actualHeight, Path.GetFileName(path) + " height");
    }

    private static void ResourceEvidence(string root)
    {
        string raw = Path.Combine(root, "extracted_assets", "raw", "images");
        ImageSize(Path.Combine(raw, "background1unsodded.jpg"), 1400, 600);
        ImageSize(Path.Combine(raw, "sod1row.jpg"), 771, 127);
        ImageSize(Path.Combine(raw, "sod1row_.png"), 771, 127);
        ImageSize(Path.Combine(raw, "SeedBank.png"), 446, 87);
        ImageSize(Path.Combine(root, "extracted_assets", "converted", "rgba", "images", "sod1row.png"), 771, 127);
    }

    private static void Layout()
    {
        NativeBlit visible;
        NativeBlit background = FirstAdventureBoardUi.UnsoddedBackground(0, 0);
        Rect(background.Source, 0, 0, 1400, 600, "full background source");
        Rect(background.Destination, -220, 0, 1400, 600, "background destination");
        Check(background.TryClip(FirstAdventureBoardUi.Canvas, out visible), "visible background");
        Rect(visible.Source, 220, 0, 800, 600, "visible background source");
        Rect(visible.Destination, 0, 0, 800, 600, "visible background destination");
        Check(FirstAdventureBoardUi.UnsoddedBackground(-10, 5).TryClip(FirstAdventureBoardUi.Canvas, out visible), "moved board");
        Rect(visible.Source, 230, 0, 800, 595, "moved board source");
        Rect(visible.Destination, 0, 5, 800, 595, "moved board destination");

        int[] inputs = { int.MinValue, -1, 0, 1, 250, 500, 750, 999, 1000, 1001, int.MaxValue };
        int[] widths = { 0, 0, 0, 1, 193, 386, 578, 770, 771, 771, 771 };
        for (int i = 0; i < inputs.Length; i++)
            Equal(widths[i], FirstAdventureBoardUi.RevealedSodWidth(inputs[i]), "sod width " + inputs[i]);
        NativeBlit grass = FirstAdventureBoardUi.SingleRowSod(0, 0, 1000);
        Rect(grass.Source, 0, 0, 771, 127, "grass source");
        Rect(grass.Destination, 19, 265, 771, 127, "grass destination");
        Rect(FirstAdventureBoardUi.TutorialSodOverlay(-10, 5).Source, 0, 0, 771, 127, "tutorial overlay source");
        Rect(FirstAdventureBoardUi.TutorialSodOverlay(-10, 5).Destination, 9, 270, 771, 127, "tutorial overlay destination");
        Check(FirstAdventureBoardUi.SingleRowSod(-50, 0, 1000).TryClip(FirstAdventureBoardUi.Canvas, out visible), "partially clipped grass");
        Rect(visible.Source, 31, 0, 740, 127, "clipped grass source");
        Rect(visible.Destination, 0, 265, 740, 127, "clipped grass destination");
        Check(!FirstAdventureBoardUi.SingleRowSod(0, 0, 0).TryClip(FirstAdventureBoardUi.Canvas, out visible), "zero-width grass");

        Rect(FirstAdventureBoardUi.SeedBank(10, -20).Destination, 10, -20, 446, 87, "bank origin");
        Check(FirstAdventureBoardUi.SeedBank(10, -20).TryClip(FirstAdventureBoardUi.Canvas, out visible), "partially visible bank");
        Rect(visible.Source, 0, 20, 446, 67, "clipped bank source");
        Rect(visible.Destination, 10, 0, 446, 67, "clipped bank destination");
        Check(!FirstAdventureBoardUi.SeedBank(0, FirstAdventureBoardUi.SeedBankEntranceY).TryClip(FirstAdventureBoardUi.Canvas, out visible), "entrance starts above canvas");
        Rect(FirstAdventureBoardUi.SeedBankEndCap(10, 2).Source, 434, 0, 12, 87, "bank end-cap source");
        Rect(FirstAdventureBoardUi.SeedBankEndCap(10, 2).Destination, 444, 2, 12, 87, "bank end-cap destination");
        Rect(FirstAdventureBoardUi.FirstSeedSlot(10, 2), 95, 10, 50, 70, "first slot object");
        Point(FirstAdventureBoardUi.SunTextOrigin(10, 2, 33), 32, 80, "odd-width sun text origin");
        Point(FirstAdventureBoardUi.SunTextOrigin(0, 0, 20), 28, 78, "even-width sun text origin");
        Equal(0, FirstAdventureBoardUi.DisplayedSun(-1), "negative sun display");
        Equal(150, FirstAdventureBoardUi.DisplayedSun(150), "starting sun display");
        Check(!FirstAdventureBoardUi.IsSunTextRed(0), "flash disabled");
        Check(FirstAdventureBoardUi.IsSunTextRed(1), "red flash");
        Check(!FirstAdventureBoardUi.IsSunTextRed(10), "black flash phase");
        Check(FirstAdventureBoardUi.IsSunTextRed(20), "next red flash phase");
        bool rejected = false;
        try { FirstAdventureBoardUi.SunTextOrigin(0, 0, -1); }
        catch (ArgumentOutOfRangeException) { rejected = true; }
        Check(rejected, "negative measured width rejected");
    }

    private static void NativeOracle(string root)
    {
        var start = new ProcessStartInfo
        {
            FileName = Environment.OSVersion.Platform == PlatformID.Win32NT ? "wsl.exe" : "sh",
            WorkingDirectory = root,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            start.ArgumentList.Add("-d");
            start.ArgumentList.Add("Ubuntu-24.04");
            start.ArgumentList.Add("--");
            start.ArgumentList.Add("sh");
        }
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add("PYTHONDONTWRITEBYTECODE=1 PYTHONPATH=analysis/ida/plant_timing/vendor python3 analysis/ida/board_ui/sod_oracle.py");
        using (Process process = Process.Start(start))
        {
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            Check(process.ExitCode == 0, "native oracle exit " + process.ExitCode + ": " + error);
            using (JsonDocument vectors = JsonDocument.Parse(output))
            {
                int count = 0;
                foreach (JsonElement pair in vectors.RootElement.EnumerateArray())
                {
                    int input = pair[0].GetInt32();
                    Equal(pair[1].GetInt32(), FirstAdventureBoardUi.RevealedSodWidth(input), "native x86 sod progress " + input);
                    count++;
                }
                Equal(1005, count, "native oracle vector count");
            }
        }
    }

    private static int Main(string[] args)
    {
        string root = FindWorkspace();
        MachineEvidence(root);
        ResourceEvidence(root);
        Layout();
        if (args.Length == 1 && args[0] == "--native-oracle") NativeOracle(root);
        else if (args.Length != 0) throw new ArgumentException("Use --native-oracle or no arguments.");
        Console.WriteLine("Board UI checks passed: " + assertions + (args.Length == 0 ? " (static and geometry)" : " (including native x86 oracle)"));
        return 0;
    }
}
