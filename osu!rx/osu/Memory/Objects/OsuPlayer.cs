using System;
using System.Numerics;

namespace osu_rx.osu.Memory.Objects
{
    public class OsuPlayer : OsuObject
    {
        public IntPtr PointerToBaseAddress { get; private set; }

        public override IntPtr BaseAddress
        {
            get => (IntPtr)OsuProcess.ReadInt32(PointerToBaseAddress);
            protected set { }
        }

        public OsuRuleset Ruleset
        {
            get => new OsuRuleset((IntPtr)OsuProcess.ReadInt32(BaseAddress + 0x60));
        }

        public OsuPlayer(IntPtr pointerToBaseAddress) => PointerToBaseAddress = pointerToBaseAddress;
    }

    public class OsuRuleset : OsuObject
    {
        //TODO: works only for ctb ruleset
        public Vector2 MousePosition
        {
            get
            {
                IntPtr vectorAddress = (IntPtr)OsuProcess.ReadInt32(BaseAddress + 0x8C); //TODO: this one is most likely a catcherSprite, not vector2
                float x = OsuProcess.ReadFloat(vectorAddress + 0x4C);
                float y = OsuProcess.ReadFloat(vectorAddress + 0x50);

                return new Vector2(x, y);
            }
            set
            {
                IntPtr vectorAddress = (IntPtr)OsuProcess.ReadInt32(BaseAddress + 0x8C);

                OsuProcess.WriteMemory(vectorAddress + 0x4C, BitConverter.GetBytes(value.X), sizeof(float));
                OsuProcess.WriteMemory(vectorAddress + 0x50, BitConverter.GetBytes(value.Y), sizeof(float));
            }
        }

        public OsuRuleset(IntPtr baseAddress) => BaseAddress = baseAddress;
    }
}
