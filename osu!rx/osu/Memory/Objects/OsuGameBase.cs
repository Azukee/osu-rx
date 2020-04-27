using OsuParsers.Enums;
using System;

namespace osu_rx.osu.Memory.Objects
{
    public class OsuGameBase : OsuObject
    {
        public int Time
        {
            get => OsuProcess.ReadInt32(BaseAddress + 0x394);
            set => OsuProcess.WriteMemory(BaseAddress + 0x394, BitConverter.GetBytes(value), sizeof(int));
        }

        public bool IsAudioPlaying
        {
            get => OsuProcess.ReadBool(BaseAddress + 0x3C4);
            set => OsuProcess.WriteMemory(BaseAddress + 0x3C4, BitConverter.GetBytes(value), sizeof(bool));
        }

        public OsuStates State
        {
            get => (OsuStates)OsuProcess.ReadInt32(BaseAddress + 0x130);
            set => OsuProcess.WriteMemory(BaseAddress + 0x130, BitConverter.GetBytes((int)value), sizeof(int));
        }

        public Mods Mods
        {
            get => (Mods)OsuProcess.ReadInt32(BaseAddress + 0x28C);
            set => OsuProcess.WriteMemory(BaseAddress + 0x28C, BitConverter.GetBytes((int)value), sizeof(int));
        }

        public bool ReplayMode
        {
            get => OsuProcess.ReadBool(BaseAddress);
            set => OsuProcess.WriteMemory(BaseAddress, BitConverter.GetBytes(value), sizeof(bool));
        }

        public int RetryCount
        {
            get => OsuProcess.ReadInt32(BaseAddress + 0x450);
            set => OsuProcess.WriteMemory(BaseAddress + 0x450, BitConverter.GetBytes(value), sizeof(int));
        }

        public OsuGameBase(UIntPtr baseAddress) => BaseAddress = baseAddress;
    }
}
