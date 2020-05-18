using osu_rx.Dependencies;
using osu_rx.Helpers;
using osu_rx.osu.Memory;
using osu_rx.osu.Memory.Objects;
using OsuParsers.Enums;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace osu_rx.osu
{
    public class OsuManager
    {
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point point);

        private object interProcessOsu;
        private MethodInfo bulkClientDataMethod;

        public OsuProcess OsuProcess { get; private set; }

        public OsuWindow OsuWindow { get; private set; }

        public bool UsingIPCFallback { get; set; }

        public int CurrentTime
        {
            get
            {
                if (!UsingIPCFallback)
                    return OsuProcess.ReadInt32(timeAddress);

                var data = bulkClientDataMethod.Invoke(interProcessOsu, null);
                return (int)data.GetType().GetField("MenuTime").GetValue(data);
            }
        }

        // Keep this function here to keep the program IPC-Safe
        public Mods CurrentMods
        {
            get
            {
                if (!UsingIPCFallback)
                    return Player.HitObjectManager.CurrentMods;

                return Mods.None;
            }
        }

        public bool IsPaused
        {
            get
            {
                if (!UsingIPCFallback)
                    return !OsuProcess.ReadBool(timeAddress + Signatures.IsAudioPlayingOffset);

                var data = bulkClientDataMethod.Invoke(interProcessOsu, null);
                return !(bool)data.GetType().GetField("AudioPlaying").GetValue(data);
            }
        }

        public bool IsPlayerLoaded
        {
            get
            {
                OsuProcess.Process.Refresh();
                return OsuProcess.Process.MainWindowTitle.Contains('-');
            }
        }

        public bool IsInReplayMode
        {
            get
            {
                if (!UsingIPCFallback)
                    return OsuProcess.ReadBool(replayModeAddress);

                var data = bulkClientDataMethod.Invoke(interProcessOsu, null);
                return (bool)data.GetType().GetField("LReplayMode").GetValue(data);
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

        public OsuStates CurrentState
        {
            get
            {
                if (!UsingIPCFallback)
                    return (OsuStates)OsuProcess.ReadInt32(stateAddress);

                var data = bulkClientDataMethod.Invoke(interProcessOsu, null);
                return (OsuStates)data.GetType().GetField("Mode").GetValue(data);
            }
        }

        public bool CanLoad
        {
            get => CurrentState == OsuStates.Play && !IsInReplayMode;
        }

        public bool CanPlay
        {
            get => CurrentState == OsuStates.Play && IsPlayerLoaded && !IsInReplayMode;
        }

        public Vector2 CursorPosition //relative to playfield
        {
            get
            {
                if (!UsingIPCFallback)
                    return Player.Ruleset.MousePosition - OsuWindow.PlayfieldPosition;

                GetCursorPos(out var pos);
                return pos.ToVector2() - (OsuWindow.WindowPosition + OsuWindow.PlayfieldPosition);
            }
        }

        public float HitObjectScalingFactor(float circleSize)
        {
            return 1f - 0.7f * (float)AdjustDifficulty(circleSize);
        }

        public float HitObjectRadius(float circleSize)
        {
            float size = (float)(OsuWindow.PlayfieldSize.X / 8f * HitObjectScalingFactor(circleSize));
            float radius = size / 2f / OsuWindow.PlayfieldRatio * 1.00041f;

            return radius;
        }

        public OsuPlayer Player { get; private set; }

        public string PathToOsu { get; private set; }

        public string SongsPath { get; private set; }

        public int HitWindow300(double od) => (int)DifficultyRange(od, 80, 50, 20);
        public int HitWindow100(double od) => (int)DifficultyRange(od, 140, 100, 60);
        public int HitWindow50(double od) => (int)DifficultyRange(od, 200, 150, 100);

        public double AdjustDifficulty(double difficulty) => (ApplyModsToDifficulty(difficulty, 1.3) - 5) / 5;

        public double ApplyModsToDifficulty(double difficulty, double hardrockFactor)
        {
            if (CurrentMods.HasFlag(Mods.Easy))
                difficulty = Math.Max(0, difficulty / 2);
            if (CurrentMods.HasFlag(Mods.HardRock))
                difficulty = Math.Min(10, difficulty * hardrockFactor);

            return difficulty;
        }

        public double DifficultyRange(double difficulty, double min, double mid, double max)
        {
            difficulty = ApplyModsToDifficulty(difficulty, 1.4);

            if (difficulty > 5)
                return mid + (max - mid) * (difficulty - 5) / 5;
            if (difficulty < 5)
                return mid - (mid - min) * (5 - difficulty) / 5;
            return mid;
        }

        public bool Initialize()
        {
            Console.WriteLine("Initializing...");

            var osuProcess = Process.GetProcessesByName("osu!").FirstOrDefault();

            if (osuProcess == default)
            {
                Console.WriteLine("\nosu! process not found! Please launch osu! first!");
                return false;
            }

            osuProcess.EnableRaisingEvents = true;
            osuProcess.Exited += (o, e) => Environment.Exit(0);
            OsuProcess = new OsuProcess(osuProcess);
            DependencyContainer.Cache(OsuProcess);

            OsuWindow = new OsuWindow(osuProcess.MainWindowHandle);

            scanMemory();
            connectToIPC();

            return true;
        }

        private UIntPtr timeAddress;
        private UIntPtr stateAddress;
        private UIntPtr replayModeAddress;
        private void scanMemory()
        {
            try
            {
                Console.WriteLine("\nScanning for memory addresses (this may take a while)...");

                //TODO: gooood this is dirty af
                if (OsuProcess.FindPattern(Signatures.Time.Pattern, out UIntPtr timeResult)
                    && OsuProcess.FindPattern(Signatures.State.Pattern, out UIntPtr stateResult)
                    && OsuProcess.FindPattern(Signatures.ReplayMode.Pattern, out UIntPtr replayModeResult)
                    && OsuProcess.FindPattern(Signatures.Player.Pattern, out UIntPtr playerResult))
                {
                    timeAddress = (UIntPtr)OsuProcess.ReadInt32(timeResult + Signatures.Time.Offset);
                    stateAddress = (UIntPtr)OsuProcess.ReadInt32(stateResult + Signatures.State.Offset);
                    replayModeAddress = (UIntPtr)OsuProcess.ReadInt32(replayModeResult + Signatures.ReplayMode.Offset);
                    Player = new OsuPlayer((UIntPtr)OsuProcess.ReadInt32(playerResult + Signatures.Player.Offset));
                }
            }
            catch { }
            finally
            {
                if (timeAddress == UIntPtr.Zero || stateAddress == UIntPtr.Zero || replayModeAddress == UIntPtr.Zero
                    || Player == null || Player.PointerToBaseAddress == UIntPtr.Zero)
                {
                    Console.WriteLine("\nScanning failed! Using IPC fallback...");
                    UsingIPCFallback = true;
                    Thread.Sleep(3000);
                }
            }
        }

        private void connectToIPC()
        {
            Console.WriteLine("\nConnecting to IPC...");

            string assemblyPath = OsuProcess.Process.MainModule.FileName;

            var assembly = Assembly.LoadFrom(assemblyPath);
            var interProcessOsuType = assembly.ExportedTypes.First(a => a.FullName == "osu.Helpers.InterProcessOsu");

            AppDomain.CurrentDomain.AssemblyResolve += (sender, eventArgs) => eventArgs.Name.Contains("osu!") ? Assembly.LoadFrom(assemblyPath) : null;

            interProcessOsu = Activator.GetObject(interProcessOsuType, "ipc://osu!/loader");
            bulkClientDataMethod = interProcessOsuType.GetMethod("GetBulkClientData");
        }
    }
}