using OsuParsers.Enums;
using System;

namespace osu_rx.osu.Memory.Objects
{
    public class OsuGameBase : OsuObject
    {
        public int Time
        {
            get => OsuProcess.ReadInt32(BaseAddress + 0x220);
            set => OsuProcess.WriteMemory(BaseAddress + 0x220, BitConverter.GetBytes(value), sizeof(int));
        }

        public bool IsAudioPlaying
        {
            get => OsuProcess.ReadBool(BaseAddress + 0x250);
            set => OsuProcess.WriteMemory(BaseAddress + 0x250, BitConverter.GetBytes(value), sizeof(bool));
        }

        public OsuStates State
        {
            get => (OsuStates)OsuProcess.ReadInt32(BaseAddress + 0x33C);
            set => OsuProcess.WriteMemory(BaseAddress + 0x33C, BitConverter.GetBytes((int)value), sizeof(int));
        }

        public Mods Mods
        {
            get => (Mods)OsuProcess.ReadInt32(BaseAddress + 0x42C);
            set => OsuProcess.WriteMemory(BaseAddress + 0x42C, BitConverter.GetBytes((int)value), sizeof(int));
        }

        public bool ReplayMode
        {
            get => OsuProcess.ReadBool(BaseAddress + 0x67C);
            set => OsuProcess.WriteMemory(BaseAddress + 0x67C, BitConverter.GetBytes(value), sizeof(bool));
        }

        public int RetryCount
        {
            get => OsuProcess.ReadInt32(BaseAddress + 0x44);
            set => OsuProcess.WriteMemory(BaseAddress + 0x44, BitConverter.GetBytes(value), sizeof(int));
        }

        public OsuGameBase(IntPtr baseAddress) => BaseAddress = baseAddress;
    }
}
