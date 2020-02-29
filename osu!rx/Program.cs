using osu_rx.Configuration;
using osu_rx.osu;
using OsuParsers.Beatmaps;
using OsuParsers.Beatmaps.Objects;
using OsuParsers.Enums;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput;
using WindowsInput.Native;

namespace osu_rx
{
    class Program
    {
        private static OsuManager osuManager;
        private static ConfigManager configManager;
        private static InputSimulator input;
        private static string defaultConsoleTitle;

        static void Main(string[] args)
        {
            osuManager = new OsuManager();

            if (!osuManager.Initialize())
            {
                Console.WriteLine();
                Console.WriteLine("osu!rx will close in 5 seconds...");
                Thread.Sleep(5000);
                Environment.Exit(0);
            }

            configManager = new ConfigManager();

            defaultConsoleTitle = Console.Title;
            if (configManager.UseCustomWindowTitle)
                Console.Title = configManager.CustomWindowTitle;

            input = new InputSimulator();

            DrawMainMenu();
        }

        private static void DrawMainMenu()
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            version = version.Remove(version.LastIndexOf(".0"));

            Console.Clear();
            Console.WriteLine($"osu!rx v{version} (MPGH release){(osuManager.UsingIPCFallback ? "\n[IPC Fallback mode]" : string.Empty)}");
            Console.WriteLine("\n---Main Menu---");
            Console.WriteLine("\n1. Start relax");
            Console.WriteLine("2. Settings");

            switch (Console.ReadKey().Key)
            {
                case ConsoleKey.D1:
                    StartRelax();
                    break;
                case ConsoleKey.D2:
                    DrawSettings();
                    break;
                default:
                    DrawMainMenu();
                    break;
            }
        }

        private static void DrawSettings()
        {
            Console.Clear();
            Console.WriteLine("---Settings---\n");
            Console.WriteLine($"1. Playstyle            | [{configManager.PlayStyle}]");
            Console.WriteLine($"2. Primary key          | [{configManager.PrimaryKey}]");
            Console.WriteLine($"3. Secondary key        | [{configManager.SecondaryKey}]");
            Console.WriteLine($"4. Hit window 100 key   | [{configManager.HitWindow100Key}]");
            Console.WriteLine($"5. Max singletap BPM    | [{configManager.MaxSingletapBPM}]");
            Console.WriteLine($"6. Audio offset         | [{configManager.AudioOffset}]");
            Console.WriteLine($"7. Custom window title  | [{(configManager.UseCustomWindowTitle ? $"ON | {configManager.CustomWindowTitle}" : "OFF")}]");
            Console.WriteLine("\nESC. Back to main menu");

            switch (Console.ReadKey().Key)
            {
                case ConsoleKey.D1:
                    Console.Clear();
                    Console.WriteLine("Select new playstyle:\n");
                    PlayStyles[] playstyles = (PlayStyles[])Enum.GetValues(typeof(PlayStyles));
                    for (int i = 0; i < playstyles.Length; i++)
                        Console.WriteLine($"{i + 1}. {playstyles[i]}");
                    if (int.TryParse(Console.ReadKey().KeyChar.ToString(), out int selected) && selected > 0 && selected < 5)
                        configManager.PlayStyle = (PlayStyles)selected - 1;
                    else
                        goto case ConsoleKey.D1;
                    DrawSettings();
                    break;
                case ConsoleKey.D2:
                    Console.Clear();
                    Console.Write("Enter new primary key: ");
                    configManager.PrimaryKey = (VirtualKeyCode)Console.ReadKey().Key;
                    DrawSettings();
                    break;
                case ConsoleKey.D3:
                    Console.Clear();
                    Console.Write("Enter new secondary key: ");
                    configManager.SecondaryKey = (VirtualKeyCode)Console.ReadKey().Key;
                    DrawSettings();
                    break;
                case ConsoleKey.D4:
                    Console.Clear();
                    Console.Write("Enter new hit window 100 key: ");
                    configManager.HitWindow100Key = (VirtualKeyCode)Console.ReadKey().Key;
                    DrawSettings();
                    break;
                case ConsoleKey.D5:
                    Console.Clear();
                    Console.Write("Enter new max singletap BPM: ");
                    if (int.TryParse(Console.ReadLine(), out int bpm))
                        configManager.MaxSingletapBPM = bpm;
                    else
                        goto case ConsoleKey.D5;
                    DrawSettings();
                    break;
                case ConsoleKey.D6:
                    Console.Clear();
                    Console.Write("Enter new audio offset: ");
                    if (int.TryParse(Console.ReadLine(), out int offset))
                        configManager.AudioOffset = offset;
                    else
                        goto case ConsoleKey.D7;
                    DrawSettings();
                    break;
                case ConsoleKey.D7:
                    Console.Clear();
                    Console.WriteLine("Use custom window title?\n");
                    Console.WriteLine("1. Yes");
                    Console.WriteLine("2. No");
                    configManager.UseCustomWindowTitle = Console.ReadKey().Key == ConsoleKey.D1;
                    if (configManager.UseCustomWindowTitle)
                    {
                        Console.Clear();
                        Console.Write("Enter new custom window title: ");
                        configManager.CustomWindowTitle = Console.ReadLine();
                        Console.Title = configManager.CustomWindowTitle;
                    }
                    else
                        Console.Title = defaultConsoleTitle;
                    DrawSettings();
                    break;
                case ConsoleKey.Escape:
                    DrawMainMenu();
                    break;
                default:
                    DrawSettings();
                    break;
            }
        }

        // ATTENTION!
        // Please value your life and don't look, touch or do anything with the code below, it's bad. I warned you.
        // ATTENTION!
        #region Relax stuff
        //TODO: move to its own class (and rewrite)
        private static void StartRelax()
        {
            bool shouldExit = false;
            Task.Run(() =>
            {
                while (Console.ReadKey().Key != ConsoleKey.Escape && !shouldExit) { }
                shouldExit = true;
            });

            var playStyle = configManager.PlayStyle;
            var primaryKey = configManager.PrimaryKey;
            var hit100Key = configManager.HitWindow100Key;
            var secondaryKey = configManager.SecondaryKey;
            int maxBPM = configManager.MaxSingletapBPM;
            int audioOffset = configManager.AudioOffset;
            float audioRate = osuManager.CurrentMods.HasFlag(Mods.DoubleTime) ? 1.5f : osuManager.CurrentMods.HasFlag(Mods.HalfTime) ? 0.75f : 1f;

            while (!shouldExit)
            {
                Console.Clear();
                Console.WriteLine("Idling");
                Console.WriteLine("\nPress ESC to return to the main menu.");

                while (!osuManager.CanPlay && !shouldExit)
                    Thread.Sleep(5);

                if (shouldExit)
                    break;

                var beatmap = osuManager.CurrentBeatmap;
                if (beatmap == null)
                {
                    Console.Clear();
                    Console.WriteLine("Beatmap not found! Please select another beatmap, reimport this one or restart osu! to fix this issue.\n\nPress ESC to return to main menu...");
                    while (!shouldExit)
                        Thread.Sleep(5);
                    break;
                }

                Console.Clear();
                Console.WriteLine($"Playing {beatmap.MetadataSection.Artist} - {beatmap.MetadataSection.Title} ({beatmap.MetadataSection.Creator}) [{beatmap.MetadataSection.Version}]");
                Console.WriteLine("\nPress ESC to return to the main menu.");

                int index = 0;
                bool isHit = false;
                var currentKey = primaryKey;
                var currentHitObject = randomizeHitObjectTimes(beatmap.HitObjects[index], beatmap, false);
                while (!shouldExit && osuManager.CanPlay && index < beatmap.HitObjects.Count)
                {
                    Thread.Sleep(1);

                    int currentTime = osuManager.CurrentTime + audioOffset;

                    if (osuManager.IsPaused && isHit)
                    {
                        isHit = false;
                        releaseAllKeys();
                    }

                    //TODO: hitscan
                    if (currentTime < (isHit ? currentHitObject.EndTime : currentHitObject.StartTime) || osuManager.IsPaused)
                        continue;

                    if (isHit)
                    {
                        releaseAllKeys();
                        isHit = false;
                        index++;
                        if (index < beatmap.HitObjects.Count)
                            currentHitObject = randomizeHitObjectTimes(beatmap.HitObjects[index], beatmap, input.InputDeviceState.IsKeyDown(hit100Key));
                    }
                    else
                    {
                        bool shouldStartAlternating = index + 1 < beatmap.HitObjects.Count ? 60000 / (beatmap.HitObjects[index + 1].StartTime - beatmap.HitObjects[index].EndTime) >= maxBPM / (audioRate / 2) : false;
                        bool shouldAlternate = index > 0 ? 60000 / (beatmap.HitObjects[index].StartTime - beatmap.HitObjects[index - 1].EndTime) >= maxBPM / (audioRate / 2) : false;

                        if (shouldAlternate || playStyle == PlayStyles.Alternate)
                            currentKey = (currentKey == primaryKey) ? secondaryKey : primaryKey;
                        else
                            currentKey = primaryKey;

                        if (playStyle == PlayStyles.MouseOnly)
                        {
                            if (currentKey == primaryKey)
                                input.Mouse.LeftButtonDown();
                            else
                                input.Mouse.RightButtonDown();
                        }
                        else if (playStyle == PlayStyles.TapX && !shouldAlternate && !shouldStartAlternating)
                        {
                            input.Mouse.LeftButtonDown();
                            currentKey = primaryKey;
                        }
                        else
                            input.Keyboard.KeyDown(currentKey);

                        isHit = true;
                    }
                }

                lastEndTime = 0;
                elapsed = 0;
                kps = 0;
                releaseAllKeys();

                while (osuManager.CanPlay && !shouldExit) //waiting just in case user still hasn't exited player
                    Thread.Sleep(5);
            }

            releaseAllKeys();
            DrawMainMenu();
        }

        private static void releaseAllKeys()
        {
            input.Keyboard.KeyUp(configManager.PrimaryKey);
            input.Keyboard.KeyUp(configManager.SecondaryKey);
            input.Mouse.LeftButtonUp();
            input.Mouse.RightButtonUp();
        }

        //TODO: better (and not shocking) humanization implementation
        private static int lastEndTime = 0;
        private static int elapsed = 0;
        private static int kps = 0;
        private static HitObject randomizeHitObjectTimes(HitObject hitObject, Beatmap beatmap, bool allowHit100)
        {
            var result = new HitObject(hitObject.Position, hitObject.StartTime, hitObject.EndTime, hitObject.HitSound, null, false, 0);

            int hitWindow300 = osuManager.HitWindow300(beatmap.DifficultySection.OverallDifficulty);
            int hitWindow100 = osuManager.HitWindow100(beatmap.DifficultySection.OverallDifficulty);

            var random = new Random();

            //TODO: everything below should depend on audio rate
            float acc = kps >= 10 ? 1f : kps >= 5 ? 1.5f : 2f;

            if (allowHit100)
                result.StartTime += random.Next(-hitWindow100 / 2, hitWindow100 / 2);
            else
                result.StartTime += random.Next((int)(-hitWindow300 / acc), (int)(hitWindow300 / acc));

            int circleHoldTime = random.Next(hitWindow300, hitWindow300 * 2);
            int sliderHoldTime = random.Next(-hitWindow300 / 2, hitWindow300 * 2);

            if (hitObject is HitCircle)
                result.EndTime = result.StartTime + circleHoldTime;
            else if (hitObject is Slider)
                result.EndTime += sliderHoldTime;

            elapsed += result.EndTime - lastEndTime;
            if (elapsed >= 1000)
            {
                kps = 1;
                elapsed = 0;
            }
            else
                kps++;

            lastEndTime = result.EndTime;

            return result;
        }
        #endregion
    }
}
