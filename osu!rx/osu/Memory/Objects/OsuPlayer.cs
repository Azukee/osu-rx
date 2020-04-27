using System;
using System.Numerics;

namespace osu_rx.osu.Memory.Objects
{
    public class OsuPlayer : OsuObject
    {
        public UIntPtr PointerToBaseAddress { get; private set; }

        public override UIntPtr BaseAddress
        {
            get => (UIntPtr)OsuProcess.ReadInt32(PointerToBaseAddress);
            protected set { }
        }

        public OsuRuleset Ruleset
        {
            get => new OsuRuleset((UIntPtr)OsuProcess.ReadInt32(BaseAddress + 0x60));
        }

        public OsuPlayer(UIntPtr pointerToBaseAddress) => PointerToBaseAddress = pointerToBaseAddress;
    }

    public class OsuRuleset : OsuObject
    {
        public Vector2 MousePosition
        {
            get
            {
                //TODO: find a way to get position relative to playfield
                float x = OsuProcess.ReadFloat(BaseAddress + 0x7C);
                float y = OsuProcess.ReadFloat(BaseAddress + 0x80);

                return new Vector2(x, y);
            }
        }

        //ctb catcher position below
        /*public Vector2 MousePosition
        {
            get
            {
                IntPtr catcherAddress = (IntPtr)OsuProcess.ReadInt32(BaseAddress + 0x8C);
                float x = OsuProcess.ReadFloat(catcherAddress + 0x4C);
                float y = OsuProcess.ReadFloat(catcherAddress + 0x50);

                return new Vector2(x, y);
            }
            set
            {
                IntPtr wank = (IntPtr)OsuProcess.ReadInt32(BaseAddress + 0xA4);
                IntPtr vectorAddress = (IntPtr)OsuProcess.ReadInt32(BaseAddress + 0x8C);

                OsuProcess.WriteMemory(wank + 0x8, BitConverter.GetBytes(value.X), sizeof(float));
                OsuProcess.WriteMemory(wank + 0xC, BitConverter.GetBytes(1), sizeof(int));
                OsuProcess.WriteMemory(vectorAddress + 0x4C, BitConverter.GetBytes(value.X), sizeof(float));
                OsuProcess.WriteMemory(vectorAddress + 0x50, BitConverter.GetBytes(value.Y), sizeof(float));
            }
        }*/

        public OsuRuleset(UIntPtr baseAddress) => BaseAddress = baseAddress;
    }
}
