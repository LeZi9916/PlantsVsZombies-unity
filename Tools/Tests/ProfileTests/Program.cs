using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using PvZReplica.Profiles;

internal static class Program
{
    private static int checks;
    private static string root;

    private static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--probe-locked")
        {
            try { new ProfileCatalog(args[1]).Create("Other process"); return 1; }
            catch (IOException) { return 0; }
        }
        string testRuns = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "profile-test-runs"));
        root = Path.GetFullPath(Path.Combine(testRuns, Guid.NewGuid().ToString("N")));
        if (!root.StartsWith(testRuns + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The test directory escaped its designated root.");
        Directory.CreateDirectory(root);
        try
        {
            BasicOperations();
            NameValidation();
            ProfileLimit();
            RejectInvalidFiles();
            SaveFailures();
            CommitRecovery();
            ExternalChanges();
            LockContention();
            ConcurrentCalls();
            Console.WriteLine("PASS: " + checks + " profile checks.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            // Only the random directory created by this test run can be removed.
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static string Scenario(string name)
    {
        string directory = Path.Combine(root, name);
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, "profiles.dat");
    }

    private static void Check(bool condition, string message)
    {
        checks++;
        if (!condition) throw new Exception("Check failed: " + message);
    }

    private static T Throws<T>(Action action, string message) where T : Exception
    {
        checks++;
        try { action(); }
        catch (T exception) { return exception; }
        catch (Exception ex) { throw new Exception(message + ": unexpected " + ex.GetType().Name, ex); }
        throw new Exception(message + ": no exception");
    }

    private static bool SameBytes(byte[] left, byte[] right)
    {
        if (left.Length != right.Length) return false;
        for (int i = 0; i < left.Length; i++) if (left[i] != right[i]) return false;
        return true;
    }

    private static void NoTemporaryFiles(string path)
    {
        Check(Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileName(path) + ".tmp-*").Length == 0,
            "Owned temporary files should be cleaned up");
    }

    private static void BasicOperations()
    {
        string path = Scenario("basic");
        ProfileCatalog catalog = new ProfileCatalog(path);
        Check(!File.Exists(path), "Construction does not create a catalog");
        Check(Directory.GetFiles(Path.GetDirectoryName(path)).Length == 0, "Construction has no side effects");
        Check(catalog.Profiles.Count == 0 && catalog.SelectedId == null, "New catalog is empty");

        UserProfile alice = catalog.Create("  Alice  ");
        Check(alice.Name == "Alice" && alice.Id != Guid.Empty && alice.AdventureLevel == 1,
            "Create trims the name, generates an ID, and initializes level one");
        Check(catalog.SelectedId == alice.Id && File.Exists(path), "Create persists and selects");
        int filesBeforeNoOps = Directory.GetFiles(Path.GetDirectoryName(path)).Length;
        catalog.Select(alice.Id);
        catalog.Rename(alice.Id, "Alice");
        Check(Directory.GetFiles(Path.GetDirectoryName(path)).Length == filesBeforeNoOps,
            "Unchanged operations verify the file without creating backups");
        IReadOnlyList<UserProfile> oldSnapshot = catalog.Profiles;
        UserProfile bob = catalog.Create("Bob");
        Check(bob.Id != alice.Id && catalog.Profiles.Count == 2 && catalog.SelectedId == bob.Id,
            "Second create is distinct and selected");
        Check(oldSnapshot.Count == 1 && oldSnapshot[0].Name == "Alice", "Snapshots do not change");
        Throws<NotSupportedException>(() => ((IList<UserProfile>)oldSnapshot).Add(bob), "List is read-only");
        Check(!typeof(UserProfile).GetProperty("Id").CanWrite &&
            !typeof(UserProfile).GetProperty("Name").CanWrite &&
            !typeof(UserProfile).GetProperty("AdventureLevel").CanWrite, "Profile properties are read-only");

        UserProfile renamed = catalog.Rename(bob.Id, "  Bobby ");
        Check(renamed.Id == bob.Id && renamed.Name == "Bobby" && bob.Name == "Bob",
            "Rename retains ID and does not mutate old snapshots");
        catalog.Select(alice.Id);
        ProfileCatalog loaded = new ProfileCatalog(path);
        Check(loaded.Profiles.Count == 2 && loaded.Profiles[0].Id == alice.Id &&
            loaded.Profiles[1].Id == bob.Id && loaded.Profiles[1].Name == "Bobby" &&
            loaded.SelectedId == alice.Id, "All records and selection survive reload");
        Check(loaded.Profiles[0].AdventureLevel == 1 && loaded.Profiles[1].AdventureLevel == 1,
            "Reloaded profiles have only the initial level");

        loaded.Delete(alice.Id);
        Check(loaded.SelectedId == bob.Id && loaded.Profiles.Count == 1,
            "Deleting selected profile selects first remaining profile");
        loaded.Delete(bob.Id);
        Check(loaded.SelectedId == null && loaded.Profiles.Count == 0, "Deleting last profile clears selection");
        ProfileCatalog emptyAgain = new ProfileCatalog(path);
        Check(emptyAgain.Profiles.Count == 0 && emptyAgain.SelectedId == null, "Empty catalog survives reload");
        Throws<KeyNotFoundException>(() => emptyAgain.Rename(alice.Id, "Alice"), "Unknown rename ID");
        Throws<KeyNotFoundException>(() => emptyAgain.Select(alice.Id), "Unknown select ID");
        Throws<KeyNotFoundException>(() => emptyAgain.Delete(alice.Id), "Unknown delete ID");
        NoTemporaryFiles(path);
    }

    private static void NameValidation()
    {
        string path = Scenario("names");
        ProfileCatalog catalog = new ProfileCatalog(path);
        UserProfile first = catalog.Create("Alice");
        UserProfile second = catalog.Create("Bob");
        byte[] original = File.ReadAllBytes(path);
        Throws<ArgumentException>(() => catalog.Create(" aLiCe "), "Duplicate names ignore case and trim");
        Throws<ArgumentException>(() => catalog.Rename(second.Id, "ALICE"), "Rename cannot duplicate a name");
        Throws<ArgumentNullException>(() => catalog.Create(null), "Null name");
        Throws<ArgumentException>(() => catalog.Create(" \t "), "Blank name");
        Throws<ArgumentException>(() => catalog.Create(new string('x', ProfileCatalog.MaxNameLength + 1)), "Long name");
        Throws<ArgumentException>(() => catalog.Create("a\nb"), "Control character in name");
        Throws<ArgumentException>(() => catalog.Create("broken\ud800"), "Unpaired surrogate in name");
        Check(SameBytes(original, File.ReadAllBytes(path)), "Rejected names do not change disk");
        UserProfile uppercase = catalog.Rename(first.Id, "ALICE");
        Check(uppercase.Id == first.Id && uppercase.Name == "ALICE", "Case-only rename is allowed");
        string longest = new string('\u4e2d', ProfileCatalog.MaxNameLength);
        UserProfile chinese = catalog.Create(longest);
        UserProfile emoji = catalog.Create(string.Concat(new string[] {
            "\ud83c\udf31", "\ud83c\udf31", "\ud83c\udf31", "\ud83c\udf31", "\ud83c\udf31",
            "\ud83c\udf31", "\ud83c\udf31", "\ud83c\udf31", "\ud83c\udf31", "\ud83c\udf31",
            "\ud83c\udf31", "\ud83c\udf31", "\ud83c\udf31", "\ud83c\udf31", "\ud83c\udf31",
            "\ud83c\udf31" }));
        Throws<ArgumentException>(() => catalog.Create(emoji.Name + "x"), "Length counts UTF-16 units");
        ProfileCatalog loaded = new ProfileCatalog(path);
        Check(loaded.Profiles[2].Id == chinese.Id && loaded.Profiles[2].Name == longest &&
            loaded.Profiles[3].Id == emoji.Id && loaded.Profiles[3].Name == emoji.Name,
            "Multibyte names round-trip");
    }

    private static void ProfileLimit()
    {
        string path = Scenario("limit");
        Guid selected = Guid.NewGuid();
        byte[] bytes = BuildFile(w => {
            w.Write(ProfileCatalog.MaxProfiles);
            w.Write(selected.ToByteArray());
            Entry(w, selected, "Profile 0", 1);
            for (int i = 1; i < ProfileCatalog.MaxProfiles; i++)
                Entry(w, Guid.NewGuid(), "Profile " + i, 1);
        }, 1);
        File.WriteAllBytes(path, bytes);
        ProfileCatalog catalog = new ProfileCatalog(path);
        Check(catalog.Profiles.Count == ProfileCatalog.MaxProfiles && catalog.SelectedId == selected,
            "Maximum-sized catalog can be loaded");
        Throws<InvalidOperationException>(() => catalog.Create("One too many"), "Profile count limit is enforced");
        Check(SameBytes(bytes, File.ReadAllBytes(path)), "Profile limit failure does not write");
    }

    private static byte[] BuildFile(Action<BinaryWriter> writePayload, int version)
    {
        byte[] payload;
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
        {
            writePayload(writer);
            writer.Flush();
            payload = stream.ToArray();
        }
        byte[] body;
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8))
        {
            writer.Write(Encoding.ASCII.GetBytes("PVZRPROF"));
            writer.Write(version);
            writer.Write(payload.Length);
            writer.Write(payload);
            writer.Flush();
            body = stream.ToArray();
        }
        byte[] hash;
        using (SHA256 sha = SHA256.Create()) hash = sha.ComputeHash(body);
        byte[] result = new byte[body.Length + hash.Length];
        Buffer.BlockCopy(body, 0, result, 0, body.Length);
        Buffer.BlockCopy(hash, 0, result, body.Length, hash.Length);
        return result;
    }

    private static void Entry(BinaryWriter writer, Guid id, string name, int level)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(name);
        writer.Write(id.ToByteArray());
        writer.Write(bytes.Length);
        writer.Write(bytes);
        writer.Write(level);
    }

    private static void RejectFile(string name, byte[] bytes)
    {
        string path = Scenario("invalid-" + name);
        File.WriteAllBytes(path, bytes);
        Throws<InvalidDataException>(() => new ProfileCatalog(path), "Reject " + name);
        Check(SameBytes(bytes, File.ReadAllBytes(path)), "Preserve " + name);
        Check(Directory.GetFiles(Path.GetDirectoryName(path)).Length == 1, "Do not create sidecars for " + name);
    }

    private static void RejectInvalidFiles()
    {
        Guid a = Guid.NewGuid();
        Guid b = Guid.NewGuid();
        byte[] valid = BuildFile(w => { w.Write(1); w.Write(a.ToByteArray()); Entry(w, a, "Alice", 1); }, 1);
        byte[] badMagic = (byte[])valid.Clone();
        badMagic[0] ^= 1;
        byte[] badChecksum = (byte[])valid.Clone();
        badChecksum[badChecksum.Length - 1] ^= 1;
        byte[] truncated = new byte[valid.Length - 1];
        Buffer.BlockCopy(valid, 0, truncated, 0, truncated.Length);
        byte[] trailing = new byte[valid.Length + 1];
        Buffer.BlockCopy(valid, 0, trailing, 0, valid.Length);
        RejectFile("empty", new byte[0]);
        RejectFile("magic", badMagic);
        RejectFile("checksum", badChecksum);
        RejectFile("truncated", truncated);
        RejectFile("trailing", trailing);
        RejectFile("oversized", new byte[256 * 1024 + 1]);
        RejectFile("version", BuildFile(w => { w.Write(0); w.Write(Guid.Empty.ToByteArray()); }, 2));
        RejectFile("duplicate-id", BuildFile(w => {
            w.Write(2); w.Write(a.ToByteArray()); Entry(w, a, "Alice", 1); Entry(w, a, "Bob", 1); }, 1));
        RejectFile("duplicate-name", BuildFile(w => {
            w.Write(2); w.Write(a.ToByteArray()); Entry(w, a, "Alice", 1); Entry(w, b, "ALICE", 1); }, 1));
        RejectFile("invalid-selected", BuildFile(w => {
            w.Write(1); w.Write(b.ToByteArray()); Entry(w, a, "Alice", 1); }, 1));
        RejectFile("missing-selected", BuildFile(w => {
            w.Write(1); w.Write(Guid.Empty.ToByteArray()); Entry(w, a, "Alice", 1); }, 1));
        RejectFile("selected-empty", BuildFile(w => { w.Write(0); w.Write(a.ToByteArray()); }, 1));
        RejectFile("progress", BuildFile(w => {
            w.Write(1); w.Write(a.ToByteArray()); Entry(w, a, "Alice", 2); }, 1));
        RejectFile("too-many", BuildFile(w => {
            w.Write(ProfileCatalog.MaxProfiles + 1); w.Write(Guid.Empty.ToByteArray()); }, 1));
        RejectFile("invalid-utf8", BuildFile(w => {
            w.Write(1); w.Write(a.ToByteArray()); w.Write(a.ToByteArray()); w.Write(1); w.Write((byte)0xff); w.Write(1); }, 1));
        RejectFile("untrimmed", BuildFile(w => {
            w.Write(1); w.Write(a.ToByteArray()); Entry(w, a, " Alice ", 1); }, 1));
        RejectFile("payload-extra", BuildFile(w => {
            w.Write(0); w.Write(Guid.Empty.ToByteArray()); w.Write((byte)0); }, 1));
    }

    private static void SaveFailures()
    {
        string newPath = Scenario("fail-new");
        ProfileCatalog empty = new ProfileCatalog(newPath, stage => {
            if (stage == ProfileSaveStage.BeforeCommit) throw new IOException("injected before commit");
        });
        Throws<IOException>(() => empty.Create("Alice"), "Failed first write");
        Check(!File.Exists(newPath) && empty.Profiles.Count == 0 && empty.SelectedId == null,
            "Failed first write leaves both memory and disk empty");
        NoTemporaryFiles(newPath);

        string path = Scenario("fail-replace");
        ProfileCatalog initial = new ProfileCatalog(path);
        UserProfile alice = initial.Create("Alice");
        byte[] original = File.ReadAllBytes(path);
        ProfileCatalog failing = new ProfileCatalog(path, stage => {
            if (stage == ProfileSaveStage.BeforeCommit) throw new IOException("injected before replace");
        });
        Throws<IOException>(() => failing.Rename(alice.Id, "Renamed"), "Failed replacement");
        Check(SameBytes(original, File.ReadAllBytes(path)) && failing.Profiles[0].Name == "Alice" &&
            new ProfileCatalog(path).Profiles[0].Name == "Alice", "Failed replacement preserves disk and memory");
        NoTemporaryFiles(path);

        string missing = Path.Combine(root, "missing-parent", "profiles.dat");
        ProfileCatalog missingParent = new ProfileCatalog(missing);
        Throws<IOException>(() => missingParent.Create("Alice"), "Missing parent prevents writing");
        Check(!Directory.Exists(Path.GetDirectoryName(missing)), "A missing directory is not created implicitly");
        Throws<InvalidOperationException>(() => { var ignored = missingParent.Profiles; },
            "Failure to acquire the lock invalidates the instance");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            string lockedPath = Scenario("locked-target");
            ProfileCatalog target = new ProfileCatalog(lockedPath);
            UserProfile profile = target.Create("Alice");
            byte[] before = File.ReadAllBytes(lockedPath);
            using (FileStream held = new FileStream(lockedPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                Throws<IOException>(() => target.Rename(profile.Id, "Bob"), "OS denies replacement of a locked file");
                Check(target.Profiles[0].Name == "Alice" && SameBytes(before, File.ReadAllBytes(lockedPath)),
                    "Real replacement failure leaves state unchanged");
            }
            NoTemporaryFiles(lockedPath);
        }
    }

    private static bool HasBackup(string path, byte[] expected)
    {
        string[] backups = Directory.GetFiles(Path.GetDirectoryName(path), Path.GetFileName(path) + ".backup-*");
        foreach (string backup in backups)
            if (SameBytes(expected, File.ReadAllBytes(backup))) return true;
        return false;
    }

    private static void CommitRecovery()
    {
        string newPath = Scenario("committed-create");
        ProfileCatalog create = new ProfileCatalog(newPath, stage => {
            if (stage == ProfileSaveStage.AfterCommit) throw new IOException("injected after create");
        });
        UserProfile created = create.Create("Alice");
        Check(create.SelectedId == created.Id && new ProfileCatalog(newPath).SelectedId == created.Id,
            "Verified completed create is published despite a post-commit error");

        string path = Scenario("committed-replace");
        ProfileCatalog initial = new ProfileCatalog(path);
        UserProfile alice = initial.Create("Alice");
        byte[] original = File.ReadAllBytes(path);
        ProfileCatalog committed = new ProfileCatalog(path, stage => {
            if (stage == ProfileSaveStage.AfterCommit) throw new IOException("injected after replace");
        });
        UserProfile renamed = committed.Rename(alice.Id, "Bob");
        Check(renamed.Name == "Bob" && committed.Profiles[0].Name == "Bob" &&
            new ProfileCatalog(path).Profiles[0].Name == "Bob", "Verified completed replace is published");
        Check(HasBackup(path, original), "The previous file is retained as a backup");
        NoTemporaryFiles(path);

        string restorePath = Scenario("restore-missing");
        ProfileCatalog beforeRestore = new ProfileCatalog(restorePath);
        UserProfile restoreAlice = beforeRestore.Create("Alice");
        byte[] before = File.ReadAllBytes(restorePath);
        ProfileCatalog restoring = new ProfileCatalog(restorePath, stage => {
            if (stage == ProfileSaveStage.AfterCommit)
            {
                // Simulates a missing target after replacement in an isolated test directory.
                File.Delete(restorePath);
                throw new IOException("injected missing target");
            }
        });
        Throws<IOException>(() => restoring.Rename(restoreAlice.Id, "Bob"), "Restored replacement reports failure");
        Check(SameBytes(before, File.ReadAllBytes(restorePath)) && restoring.Profiles[0].Name == "Alice" &&
            new ProfileCatalog(restorePath).Profiles[0].Name == "Alice", "Missing target restored from exact backup");
        Check(HasBackup(restorePath, before), "Recovery retains the backup");
        NoTemporaryFiles(restorePath);

        string foreignPath = Scenario("foreign-content");
        new ProfileCatalog(foreignPath).Create("Foreign");
        byte[] foreign = File.ReadAllBytes(foreignPath);
        string uncertainPath = Scenario("uncertain");
        UserProfile uncertainAlice = new ProfileCatalog(uncertainPath).Create("Alice");
        ProfileCatalog uncertain = new ProfileCatalog(uncertainPath, stage => {
            if (stage == ProfileSaveStage.AfterCommit)
            {
                File.WriteAllBytes(uncertainPath, foreign);
                throw new IOException("injected unexpected file");
            }
        });
        IOException error = Throws<IOException>(() => uncertain.Rename(uncertainAlice.Id, "Bob"),
            "An unrecognizable post-commit state is reported");
        Check(error.InnerException != null && SameBytes(foreign, File.ReadAllBytes(uncertainPath)),
            "Unexpected file is preserved and failure retains its cause");
        Throws<InvalidOperationException>(() => { var ignored = uncertain.Profiles; }, "Uncertain profiles are inaccessible");
        Throws<InvalidOperationException>(() => { var ignored = uncertain.SelectedId; }, "Uncertain selection is inaccessible");
        Throws<InvalidOperationException>(() => uncertain.Create("Another"), "Uncertain catalog cannot be modified");
        Check(new ProfileCatalog(uncertainPath).Profiles[0].Name == "Foreign", "A new catalog observes disk reality");
    }

    private static void ExternalChanges()
    {
        string path = Scenario("conflict");
        ProfileCatalog initial = new ProfileCatalog(path);
        UserProfile alice = initial.Create("Alice");
        ProfileCatalog first = new ProfileCatalog(path);
        ProfileCatalog stale = new ProfileCatalog(path);
        ProfileCatalog staleSelect = new ProfileCatalog(path);
        ProfileCatalog staleRename = new ProfileCatalog(path);
        first.Create("Bob");
        byte[] latest = File.ReadAllBytes(path);
        Throws<IOException>(() => stale.Create("Carol"), "Stale catalog cannot overwrite another writer");
        Throws<IOException>(() => staleSelect.Select(alice.Id), "A stale no-op selection reports conflict");
        Throws<IOException>(() => staleRename.Rename(alice.Id, "Alice"), "A stale no-op rename reports conflict");
        Check(SameBytes(latest, File.ReadAllBytes(path)) && new ProfileCatalog(path).Profiles.Count == 2,
            "Conflict preserves the other writer's catalog");
        Throws<InvalidOperationException>(() => { var ignored = stale.Profiles; }, "Conflict invalidates stale state");

        string unknownPath = Scenario("external-version");
        ProfileCatalog versioned = new ProfileCatalog(unknownPath);
        versioned.Create("Alice");
        byte[] unknown = BuildFile(w => { w.Write(0); w.Write(Guid.Empty.ToByteArray()); }, 42);
        File.WriteAllBytes(unknownPath, unknown);
        Throws<IOException>(() => versioned.Create("Bob"), "External unknown version prevents writing");
        Check(SameBytes(unknown, File.ReadAllBytes(unknownPath)), "Unknown external version remains untouched");
        Throws<InvalidDataException>(() => new ProfileCatalog(unknownPath), "Unknown external version remains unreadable");
    }

    private static void LockContention()
    {
        string path = Scenario("lock");
        byte[] existingSidecar = Encoding.UTF8.GetBytes("existing data owned by someone else");
        File.WriteAllBytes(path + ".lock", existingSidecar);
        ProfileCatalog catalog = new ProfileCatalog(path);
        catalog.Create("Alice");
        Check(SameBytes(existingSidecar, File.ReadAllBytes(path + ".lock")),
            "An existing lock sidecar is never truncated");
        byte[] original = File.ReadAllBytes(path);
        using (FileStream held = new FileStream(path + ".lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Throws<IOException>(() => catalog.Create("Bob"), "A held catalog lock prevents another save");
            Throws<InvalidOperationException>(() => { var ignored = catalog.Profiles; },
                "Lock acquisition failure invalidates the instance");
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";
            info.UseShellExecute = false;
            info.RedirectStandardError = true;
            info.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
            info.ArgumentList.Add("--probe-locked");
            info.ArgumentList.Add(path);
            using (Process process = Process.Start(info))
            {
                if (!process.WaitForExit(15000))
                {
                    process.Kill();
                    throw new Exception("Lock probe process timed out.");
                }
                Check(process.ExitCode == 0, "A separate process respects the catalog lock: " +
                    process.StandardError.ReadToEnd());
            }
        }
        Check(SameBytes(original, File.ReadAllBytes(path)) &&
            SameBytes(existingSidecar, File.ReadAllBytes(path + ".lock")), "Lock failure does not delete user files");
        Check(new ProfileCatalog(path).Profiles.Count == 1, "Catalog can be reopened after lock contention");
    }

    private static void ConcurrentCalls()
    {
        string path = Scenario("threads");
        ProfileCatalog catalog = new ProfileCatalog(path);
        Task[] tasks = new Task[16];
        for (int i = 0; i < tasks.Length; i++)
        {
            int number = i;
            tasks[i] = Task.Run(() => catalog.Create("Thread " + number));
        }
        Task.WaitAll(tasks);
        ProfileCatalog loaded = new ProfileCatalog(path);
        HashSet<Guid> ids = new HashSet<Guid>();
        HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (UserProfile profile in loaded.Profiles)
        {
            ids.Add(profile.Id);
            names.Add(profile.Name);
        }
        Check(catalog.Profiles.Count == tasks.Length && loaded.Profiles.Count == tasks.Length &&
            ids.Count == tasks.Length && names.Count == tasks.Length && catalog.SelectedId == loaded.SelectedId,
            "Concurrent calls on one instance serialize without lost profiles");
        NoTemporaryFiles(path);
    }
}
