using osu_rx.Configuration;
using osu_rx.Core;
using osu_rx.Dependencies;
using osu_rx.osu;
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

        private static void StartRelax()
        {
            bool shouldExit = false;
            Task.Run(() =>
            {
                while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)) ;

                shouldExit = true;
                relax.Stop();
            });

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
                    Console.WriteLine("Beatmap not found! Please select another beatmap, reimport this one or restart osu! to fix this issue.\n\nReturn to song select to continue or press ESC to return to main menu.");

                    while (osuManager.CanPlay && !shouldExit) ;
                    if (shouldExit)
                        break;
                    continue;
                }

                Console.Clear();
                Console.WriteLine($"Playing {beatmap.MetadataSection.Artist} - {beatmap.MetadataSection.Title} ({beatmap.MetadataSection.Creator}) [{beatmap.MetadataSection.Version}]");
                Console.WriteLine("\nPress ESC to return to the main menu.");

                relax.Start();
            }

            DrawMainMenu();
        }
    }
}
