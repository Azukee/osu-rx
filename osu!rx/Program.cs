using osu_rx.Configuration;
using osu_rx.Core;
using osu_rx.Dependencies;
using osu_rx.osu;
using OsuParsers.Enums;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using WindowsInput.Native;

namespace osu_rx
{
    class Program
    {
        private static OsuManager osuManager;
        private static ConfigManager configManager;
        private static Relax relax;
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

            DependencyContainer.Cache(osuManager);
            DependencyContainer.Cache(configManager);

            relax = new Relax();

            defaultConsoleTitle = Console.Title;
            if (configManager.UseCustomWindowTitle)
                Console.Title = configManager.CustomWindowTitle;

            DrawMainMenu();
        }

        private static void DrawMainMenu()
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            version = version.Remove(version.LastIndexOf(".0"));

            Console.Clear();
            Console.WriteLine($"osu!rx v{version} (MPGH release){(osuManager.UsingIPCFallback ? " | [IPC Fallback mode]" : string.Empty)}");
            Console.WriteLine("\n---Main Menu---");
            Console.WriteLine("\n1. Start relax");
            Console.WriteLine("2. Settings");

            if (osuManager.UsingIPCFallback)
                Console.WriteLine("\n3. What is IPC Fallback mode?");

            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.D1:
                    StartRelax();
                    break;
                case ConsoleKey.D2:
                    DrawSettings();
                    break;
                case ConsoleKey.D3:
                    if (osuManager.UsingIPCFallback)
                        DrawIPCFallbackInfo();
                    else
                        DrawMainMenu();
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
            Console.WriteLine($"8. Hitscan              | [{(configManager.EnableHitScan ? "ENABLED" : "DISABLED")}]");
            Console.WriteLine($"9. Hold before Spinner  | [{(configManager.HoldBeforeSpinner ? "ENABLED" : "DISABLED")}]");;

            if (!osuManager.UsingIPCFallback)
                Console.WriteLine("\n0. Turn on IPC Fallback mode");

            Console.WriteLine("\nESC. Back to main menu");

            switch (Console.ReadKey(true).Key)
            {
                case ConsoleKey.D1:
                    Console.Clear();
                    Console.WriteLine("Select new playstyle:\n");
                    PlayStyles[] playstyles = (PlayStyles[])Enum.GetValues(typeof(PlayStyles));
                    for (int i = 0; i < playstyles.Length; i++)
                        Console.WriteLine($"{i + 1}. {playstyles[i]}");
                    if (int.TryParse(Console.ReadKey(true).KeyChar.ToString(), out int selected) && selected > 0 && selected < 5)
                        configManager.PlayStyle = (PlayStyles)selected - 1;
                    else
                        goto case ConsoleKey.D1;
                    DrawSettings();
                    break;
                case ConsoleKey.D2:
                    Console.Clear();
                    Console.Write("Enter new primary key: ");
                    configManager.PrimaryKey = (VirtualKeyCode)Console.ReadKey(true).Key;
                    DrawSettings();
                    break;
                case ConsoleKey.D3:
                    Console.Clear();
                    Console.Write("Enter new secondary key: ");
                    configManager.SecondaryKey = (VirtualKeyCode)Console.ReadKey(true).Key;
                    DrawSettings();
                    break;
                case ConsoleKey.D4:
                    Console.Clear();
                    Console.Write("Enter new hit window 100 key: ");
                    configManager.HitWindow100Key = (VirtualKeyCode)Console.ReadKey(true).Key;
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
                    configManager.UseCustomWindowTitle = Console.ReadKey(true).Key == ConsoleKey.D1;
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
                case ConsoleKey.D8:
                    configManager.EnableHitScan = !configManager.EnableHitScan;
                    DrawSettings();
                    break;
                case ConsoleKey.D9:
                    configManager.HoldBeforeSpinner = !configManager.HoldBeforeSpinner;
                    DrawSettings();
                    break;
                case ConsoleKey.D0:
                    if (!osuManager.UsingIPCFallback)
                    {
                        Console.Clear();
                        Console.WriteLine("Turn this on manually only if osu!rx works incorrectly for you.");
                        Console.WriteLine("You won't be able to turn off IPC Fallback mode without restarting osu!rx.");
                        Console.WriteLine("\nTurn on IPC Fallback mode anyway?");
                        Console.WriteLine("\n1. Yes");
                        Console.WriteLine("2. No");

                        osuManager.UsingIPCFallback = Console.ReadKey(true).Key == ConsoleKey.D1;
                        DrawSettings();
                    }
                    else
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

        private static void StartRelax()
        {
            bool shouldExit = false;
            Task.Run(() =>
            {
                while (Console.ReadKey(true).Key != ConsoleKey.Escape) ;

                shouldExit = true;
                relax.Stop();
            });

            while (!shouldExit)
            {
                Console.Clear();
                Console.WriteLine("Idling");
                Console.WriteLine("\nPress ESC to return to the main menu.");

                while (!osuManager.CanLoad && !shouldExit)
                    Thread.Sleep(5);

                if (shouldExit)
                    break;

                var beatmap = osuManager.CurrentBeatmap;
                if (beatmap == null || beatmap.GeneralSection.Mode != Ruleset.Standard)
                {
                    Console.Clear();
                    if (beatmap == null)
                        Console.WriteLine("Beatmap not found! Please select another beatmap, reimport this one or restart osu! to fix this issue.\n\nReturn to song select to continue or press ESC to return to main menu.");
                    else
                        Console.WriteLine("Only osu!standard beatmaps are supported!\n\nReturn to song select to continue or press ESC to return to main menu.");

                    while (osuManager.CanLoad && !shouldExit)
                        Thread.Sleep(1);

                    if (shouldExit)
                        break;

                    continue;
                }

                Console.Clear();
                Console.WriteLine($"Playing {beatmap.MetadataSection.Artist} - {beatmap.MetadataSection.Title} ({beatmap.MetadataSection.Creator}) [{beatmap.MetadataSection.Version}]");
                Console.WriteLine("\nPress ESC to return to the main menu.");

                while (!osuManager.CanPlay)
                    Thread.Sleep(1);

                relax.Start();
            }

            DrawMainMenu();
        }

        private static void DrawIPCFallbackInfo()
        {
            Console.Clear();
            Console.WriteLine("---What is IPC Fallback mode?---");
            Console.WriteLine("\nIPC Fallback mode automatically turns on when osu!rx fails to find important addresses in game's memory.");
            Console.WriteLine("While IPC Fallback mode is on, osu!rx will communicate with osu! through IPC to get needed variables.");
            Console.WriteLine("That means you can use osu!rx even if it's outdated.");
            Console.WriteLine("\nHowever, IPC Fallback mode has quite a few cons:");
            Console.WriteLine("\n1. You will experience timing issues if your game is running below 200 fps.");
            Console.WriteLine("2. You will probably be missing a lot if your game lags/stutters.");
            Console.WriteLine("3. Mods support will no longer work.\n   That means you won't be able to play hd/ez mods with hitscan and your max singletap bpm won't scale with dt/ht mods.");
            Console.WriteLine("4. Hitscan may not work if you have raw input turned on.");
            Console.WriteLine("5. And you'll probably experience a bunch of other unknown issues.");
            Console.WriteLine("\nBut hey, you'll still be able to use osu!rx (in some way) even if i die and won't be able to update this piece of junk ;)");
            Console.WriteLine("\n---How to get rid of this?---");
            Console.WriteLine("\n- Try restarting osu! and osu!rx multiple times.");
            Console.WriteLine("- If the advice above didn't helped, then report this on github/mpgh and i'll try to fix this ASAP!");
            Console.WriteLine("\nPress ESC to return to the main menu.");

            if (Console.ReadKey(true).Key == ConsoleKey.Escape)
                DrawMainMenu();
            else
                DrawIPCFallbackInfo();
        }
    }
}
