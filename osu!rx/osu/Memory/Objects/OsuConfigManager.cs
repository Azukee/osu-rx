using System;
using System.Text;

namespace osu_rx.osu.Memory.Objects
{
    public class OsuConfigManager : OsuObject
    {
        public string BeatmapDirectory
        {
            get
            {
                IntPtr stringAddress = (IntPtr)OsuProcess.ReadInt32(BaseAddress + 0x27C);
                int stringLength = OsuProcess.ReadInt32(stringAddress + 0x4);

                return BitConverter.ToString(OsuProcess.ReadMemory(stringAddress + 0x8, stringLength));
            }
            set
            {
                IntPtr stringAddress = (IntPtr)OsuProcess.ReadInt32(BaseAddress + 0x27C);
                int stringLength = value.Length;

                //TODO: dunno if this is correct
                OsuProcess.WriteMemory(stringAddress + 0x4, BitConverter.GetBytes(stringLength), sizeof(int));
                OsuProcess.WriteMemory(stringAddress + 0x8, Encoding.Default.GetBytes(value.ToCharArray(), 0, stringLength), (uint)stringLength);
            }
        }

        public bool Fullscreen 
        {
            get
            {
                IntPtr bindableAddress = (IntPtr)OsuProcess.ReadInt32(BaseAddress + 0x1C8);
                return OsuProcess.ReadBool(bindableAddress + 0xC);
            }
            set
            {
                IntPtr bindableAddress = (IntPtr)OsuProcess.ReadInt32(BaseAddress + 0x1C8);
                OsuProcess.WriteMemory(bindableAddress + 0xC, BitConverter.GetBytes(value), sizeof(bool));
            }
        }

        public bool Letterboxing
        {
            get
            {
                IntPtr bindableAddress = (IntPtr)OsuProcess.ReadInt32(BaseAddress + 0x26C);
                return OsuProcess.ReadBool(bindableAddress + 0xC);
            }
            set
            {
                IntPtr bindableAddress = (IntPtr)OsuProcess.ReadInt32(BaseAddress + 0x26C);
                OsuProcess.WriteMemory(bindableAddress + 0xC, BitConverter.GetBytes(value), sizeof(bool));
            }
        }

        public int LetterboxingHorizontalPosition
        {
            get
            {
                IntPtr bindableAddress = (IntPtr)OsuProcess.ReadInt32(BaseAddress + 0x270);
                return OsuProcess.ReadInt32(bindableAddress + 0x4);
            }
            set
            {
                IntPtr bindableAddress = (IntPtr)OsuProcess.ReadInt32(BaseAddress + 0x270);
                OsuProcess.WriteMemory(bindableAddress + 0x4, BitConverter.GetBytes(value), sizeof(int));
            }
        }

        public int LetterboxingVerticalPosition
        {
            get
            {
                IntPtr bindableAddress = (IntPtr)OsuProcess.ReadInt32(BaseAddress + 0x274);
                return OsuProcess.ReadInt32(bindableAddress + 0x4);
            }
            set
            {
                IntPtr bindableAddress = (IntPtr)OsuProcess.ReadInt32(BaseAddress + 0x274);
                OsuProcess.WriteMemory(bindableAddress + 0x4, BitConverter.GetBytes(value), sizeof(int));
            }
        }

        public int Width
        {
            get
            {
                IntPtr bindableAddress = (IntPtr)OsuProcess.ReadInt32(BaseAddress + 0x1A0);
                return OsuProcess.ReadInt32(bindableAddress + 0x4);
            }
            set
            {
                IntPtr bindableAddress = (IntPtr)OsuProcess.ReadInt32(BaseAddress + 0x1A0);
                OsuProcess.WriteMemory(bindableAddress + 0x4, BitConverter.GetBytes(value), sizeof(int));
            }
        }

        public int Height
        {
            get
            {
                IntPtr bindableAddress = (IntPtr)OsuProcess.ReadInt32(BaseAddress + 0x17C);
                return OsuProcess.ReadInt32(bindableAddress + 0x4);
            }
            set
            {
                IntPtr bindableAddress = (IntPtr)OsuProcess.ReadInt32(BaseAddress + 0x17C);
                OsuProcess.WriteMemory(bindableAddress + 0x4, BitConverter.GetBytes(value), sizeof(int));
            }
        }

        public int WidthFullscreen
        {
            get
            {
                IntPtr bindableAddress = (IntPtr)OsuProcess.ReadInt32(BaseAddress + 0x1A4);
                return OsuProcess.ReadInt32(bindableAddress + 0x4);
            }
            set
            {
                IntPtr bindableAddress = (IntPtr)OsuProcess.ReadInt32(BaseAddress + 0x1A4);
                OsuProcess.WriteMemory(bindableAddress + 0x4, BitConverter.GetBytes(value), sizeof(int));
            }
        }

        public int HeightFullscreen
        {
            get
            {
                IntPtr bindableAddress = (IntPtr)OsuProcess.ReadInt32(BaseAddress + 0x180);
                return OsuProcess.ReadInt32(bindableAddress + 0x4);
            }
            set
            {
                IntPtr bindableAddress = (IntPtr)OsuProcess.ReadInt32(BaseAddress + 0x180);
                OsuProcess.WriteMemory(bindableAddress + 0x4, BitConverter.GetBytes(value), sizeof(int));
            }
        }

        public ScaleMode ScaleMode
        {
            get
            {
                IntPtr bindableAddress = (IntPtr)OsuProcess.ReadInt32(BaseAddress + 0x1C0);
                return (ScaleMode)OsuProcess.ReadInt32(bindableAddress + 0xC);
            }
            set
            {
                IntPtr bindableAddress = (IntPtr)OsuProcess.ReadInt32(BaseAddress + 0x1C0);
                OsuProcess.WriteMemory(bindableAddress + 0xC, BitConverter.GetBytes((int)value), sizeof(int));
            }
        }

        public OsuConfigManager(IntPtr baseAddress) => BaseAddress = baseAddress;
    }

    public enum ScaleMode
    {
        Letterbox = 0,
        WidescreenConservative = 1,
        WidescreenAlways = 2
    }
}
