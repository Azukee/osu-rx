using osu_rx.Dependencies;
using osu_rx.osu;
using osu_rx.osu.Memory;
using System;
using System.Diagnostics;

namespace osu_rx.Core
{
    public class Timewarp
    {
        private const double defaultRate = 1147;

        private OsuManager osuManager;
        private UIntPtr audioRateAddress = UIntPtr.Zero;

        public Timewarp() => osuManager = DependencyContainer.Get<OsuManager>();

        public void Refresh()
        {
            foreach (ProcessModule module in osuManager.OsuProcess.Process.Modules)
            {
                if (module.ModuleName == "bass.dll")
                {
                    audioRateAddress = (UIntPtr)module.BaseAddress.ToInt32();
                    break;
                }
            }

            for (int i = 0; i < Signatures.AudioRateOffsets.Length; i++)
            {
                audioRateAddress += Signatures.AudioRateOffsets[i];

                if (i != Signatures.AudioRateOffsets.Length - 1)
                    audioRateAddress = (UIntPtr)osuManager.OsuProcess.ReadInt32(audioRateAddress);
            }
        }

        public void Update(double rate, double initialRate)
        {
            if (osuManager.OsuProcess.ReadDouble(audioRateAddress) != rate)
            {
                osuManager.OsuProcess.WriteMemory(audioRateAddress, BitConverter.GetBytes(rate), sizeof(double));
                osuManager.OsuProcess.WriteMemory(audioRateAddress + 0x8, BitConverter.GetBytes(rate * defaultRate), sizeof(double));
            }

            //bypassing audio checks
            osuManager.Player.AudioCheckTime = (int)(osuManager.CurrentTime * initialRate);
        }

        public void Reset()
        {
            osuManager.OsuProcess.WriteMemory(audioRateAddress, BitConverter.GetBytes(1), sizeof(double));
            osuManager.OsuProcess.WriteMemory(audioRateAddress + 0x8, BitConverter.GetBytes(defaultRate), sizeof(double));
        }
    }
}