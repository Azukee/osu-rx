using osu_rx.Helpers;
using OsuParsers.Beatmaps;
using OsuParsers.Database;
using OsuParsers.Decoders;
using OsuParsers.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace osu_rx.osu
{
    public class OsuManager
    {
        private object interProcessOsu;
        private MethodInfo bulkClientDataMethod;

        private FileSystemWatcher fileSystemWatcher;
        private OsuDatabase osuDatabase;
        private string databaseHash;

        public int CurrentTime
        {
            get
            {
                var data = bulkClientDataMethod.Invoke(interProcessOsu, null);
                return (int)data.GetType().GetField("MenuTime").GetValue(data);
            }
        }

        public Mods CurrentMods //TODO: implement
        {
            get => Mods.None;
        }

        public bool IsPaused
        {
            get
            {
                var data = bulkClientDataMethod.Invoke(interProcessOsu, null);
                return !(bool)data.GetType().GetField("AudioPlaying").GetValue(data);
            }
        }

        public bool IsPlayerLoaded
        {
            get
            {
                var data = bulkClientDataMethod.Invoke(interProcessOsu, null);
                return (bool)data.GetType().GetField("LPlayerLoaded").GetValue(data);
            }
        }

        public string BeatmapChecksum
        {
            get
            {
                var data = bulkClientDataMethod.Invoke(interProcessOsu, null);
                return (string)data.GetType().GetField("BeatmapChecksum").GetValue(data);
            }
        }

        private List<(string MD5, Beatmap Beatmap)> beatmapCache = new List<(string, Beatmap)>();
        private List<(string MD5, string PathToBeatmap)> newlyImported = new List<(string, string)>();
        public Beatmap CurrentBeatmap
        {
            get
            {
                updateDatabase();

                (string MD5, Beatmap Beatmap) beatmap = (string.Empty, null);

                if (beatmapCache.Find(b => b.MD5 == BeatmapChecksum) is var cachedBeatmap && cachedBeatmap != default)
                    beatmap = cachedBeatmap;
                else if (osuDatabase.Beatmaps.Find(b => b.MD5Hash == BeatmapChecksum) is var dbBeatmap && dbBeatmap != default)
                    beatmap = (dbBeatmap.MD5Hash, BeatmapDecoder.Decode($@"{SongsPath}\{dbBeatmap.FolderName}\{dbBeatmap.FileName}"));
                else if (newlyImported.Find(b => b.MD5 == BeatmapChecksum) is var importedBeatmap && importedBeatmap != default)
                    beatmap = (importedBeatmap.MD5, BeatmapDecoder.Decode(importedBeatmap.PathToBeatmap));

                if (beatmap != (string.Empty, null) && !beatmapCache.Contains(beatmap))
                    beatmapCache.Add(beatmap);

                return beatmap.Beatmap;
            }
        }

        public OsuStates CurrentState
        {
            get
            {
                var data = bulkClientDataMethod.Invoke(interProcessOsu, null);
                return (OsuStates)data.GetType().GetField("Mode").GetValue(data);
            }
        }

        public bool CanPlay
        {
            get => CurrentState == OsuStates.Play && IsPlayerLoaded;
        }

        public string PathToOsu { get; private set; }

        public string SongsPath { get; private set; }

        public int HitWindow300(double od) => (int)difficultyRange(od, 80, 50, 20, CurrentMods);
        public int HitWindow100(double od) => (int)difficultyRange(od, 140, 100, 60, CurrentMods);
        public int HitWindow50(double od) => (int)difficultyRange(od, 200, 150, 100, CurrentMods);

        public bool Initialize()
        {
            Console.WriteLine("Initializing...");

            Process osuProcess = Process.GetProcessesByName("osu!").FirstOrDefault();

            if (osuProcess == default)
            {
                Console.WriteLine("osu! process not found! Please launch osu! first!");
                return false;
            }

            PathToOsu = Path.GetDirectoryName(osuProcess.MainModule.FileName);
            parseConfig();

            string assemblyPath = osuProcess.MainModule.FileName;

            var assembly = Assembly.LoadFrom(assemblyPath);
            var interProcessOsuType = assembly.ExportedTypes.First(a => a.FullName == "osu.Helpers.InterProcessOsu");

            AppDomain.CurrentDomain.AssemblyResolve += (sender, eventArgs) => eventArgs.Name.Contains("osu!") ? Assembly.LoadFrom(assemblyPath) : null;

            interProcessOsu = Activator.GetObject(interProcessOsuType, "ipc://osu!/loader");
            bulkClientDataMethod = interProcessOsuType.GetMethod("GetBulkClientData");

            initializeBeatmapWatcher();

            return true;
        }

        private double applyModsToDifficulty(double difficulty, double hardrockFactor, Mods mods)
        {
            if (mods.HasFlag(Mods.Easy))
                difficulty = Math.Max(0, difficulty / 2);
            if (mods.HasFlag(Mods.HardRock))
                difficulty = Math.Min(10, difficulty * hardrockFactor);

            return difficulty;
        }

        private double difficultyRange(double difficulty, double min, double mid, double max, Mods mods)
        {
            difficulty = applyModsToDifficulty(difficulty, 1.4, mods);

            if (difficulty > 5)
                return mid + (max - mid) * (difficulty - 5) / 5;
            if (difficulty < 5)
                return mid - (mid - min) * (5 - difficulty) / 5;
            return mid;
        }

        private void parseConfig()
        {
            string pathToConfig = $@"{PathToOsu}\osu!.{Environment.UserName}.cfg";

            if (File.Exists(pathToConfig))
            {
                foreach (string line in File.ReadAllLines(pathToConfig))
                {
                    if (line.StartsWith("BeatmapDirectory"))
                    {
                        string path = line.Split('=')[1].Trim();
                        if (!path.Contains(":\\"))
                            path = Path.Combine(PathToOsu, path);

                        SongsPath = Path.GetFullPath(path);
                    }
                }
            }
            else
                SongsPath = $@"{PathToOsu}\Songs";
        }

        private void initializeBeatmapWatcher()
        {
            updateDatabase();

            fileSystemWatcher = new FileSystemWatcher(SongsPath);
            fileSystemWatcher.Created += (object sender, FileSystemEventArgs e) => onNewBeatmapImport(e.FullPath);
            fileSystemWatcher.Changed += (object sender, FileSystemEventArgs e) => onNewBeatmapImport(e.FullPath);
            fileSystemWatcher.EnableRaisingEvents = true;
            fileSystemWatcher.IncludeSubdirectories = true;

            var lastModified = osuDatabase.Beatmaps.Max(b => b.LastModifiedTime);
            foreach (var dir in new DirectoryInfo(SongsPath).EnumerateDirectories().OrderByDescending(d => d.LastWriteTime))
                if (dir.LastWriteTime >= lastModified)
                    dir.EnumerateFiles(".osu").ToList().ForEach(f => onNewBeatmapImport(f.FullName));
        }

        private void updateDatabase()
        {
            string currentDatabaseHash = CryptoHelper.GetMD5String(File.ReadAllBytes($@"{PathToOsu}\osu!.db"));
            if (currentDatabaseHash != databaseHash)
            {
                databaseHash = currentDatabaseHash;
                osuDatabase = DatabaseDecoder.DecodeOsu($@"{PathToOsu}\osu!.db");
            }
        }

        private void onNewBeatmapImport(string path)
        {
            if (path.EndsWith(".osu"))
            {
                try
                {
                    (string MD5, string PathToBeatmap) beatmap = (CryptoHelper.GetMD5String(File.ReadAllBytes(path)), path);
                    if (newlyImported.Exists(b => b.MD5 == beatmap.MD5))
                        newlyImported.RemoveAll(b => b.MD5 == beatmap.MD5);

                    newlyImported.Add(beatmap);
                }
                catch (IOException) //try again if file is already being used
                {
                    Thread.Sleep(500);
                    onNewBeatmapImport(path);
                }
            }
        }
    }
}