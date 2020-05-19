using OsuParsers.Beatmaps;
using OsuParsers.Enums;
using System;
using System.Linq;
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

        public OsuHitObjectManager HitObjectManager
        {
            get => new OsuHitObjectManager((UIntPtr)OsuProcess.ReadInt32(BaseAddress + 0x40));
        }

        public Beatmap Beatmap
        {
            get
            {
                UIntPtr beatmapBase = (UIntPtr)OsuProcess.ReadInt32(BaseAddress + 0xD4);
                var beatmap = new Beatmap();

                int mode = OsuProcess.ReadInt32(beatmapBase + 0x114);
                beatmap.GeneralSection.Mode = (Ruleset)mode;
                beatmap.GeneralSection.ModeId = mode;
                beatmap.MetadataSection.Artist = OsuProcess.ReadString(beatmapBase + 0x18);
                beatmap.MetadataSection.Title = OsuProcess.ReadString(beatmapBase + 0x24);
                beatmap.MetadataSection.Creator = OsuProcess.ReadString(beatmapBase + 0x78);
                beatmap.MetadataSection.Version = OsuProcess.ReadString(beatmapBase + 0xA8);
                beatmap.DifficultySection.ApproachRate = OsuProcess.ReadFloat(beatmapBase + 0x2C);
                beatmap.DifficultySection.CircleSize = OsuProcess.ReadFloat(beatmapBase + 0x30);
                beatmap.DifficultySection.HPDrainRate = OsuProcess.ReadFloat(beatmapBase + 0x34);
                beatmap.DifficultySection.OverallDifficulty = OsuProcess.ReadFloat(beatmapBase + 0x38);
                beatmap.DifficultySection.SliderMultiplier = OsuProcess.ReadDouble(beatmapBase + 0x8);
                beatmap.DifficultySection.SliderTickRate = OsuProcess.ReadDouble(beatmapBase + 0x10);
                beatmap.HitObjects = HitObjectManager.HitObjects.ToList();

                return beatmap;
            }
        }

        public int AudioCheckTime
        {
            get => OsuProcess.ReadInt32(BaseAddress + 0x154);
            set
            {
                OsuProcess.WriteMemory(BaseAddress + 0x154, BitConverter.GetBytes(value), sizeof(int));
                OsuProcess.WriteMemory(BaseAddress + 0x158, BitConverter.GetBytes(value), sizeof(int));
            }
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
