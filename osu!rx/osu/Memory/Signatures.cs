namespace osu_rx.osu.Memory
{
    public static class Signatures
    {
        public static readonly Signature Time = new Signature
        {
            Pattern = "D9 58 2C 8B 3D ?? ?? ?? ?? 8B 1D",
            Offset = 0xB
        };

        public const int IsAudioPlayingOffset = 0x30;

        public static readonly Signature Mods = new Signature
        {
            Pattern = "53 8B F1 A1 ?? ?? ?? ?? 25 ?? ?? ?? ?? 85 C0",
            Offset = 0x4
        };

        public static readonly Signature State = new Signature
        {
            Pattern = "8D 45 BC 89 46 0C 83 3D",
            Offset = 0x8
        };

        public static readonly Signature ReplayMode = new Signature
        {
            Pattern = "85 C0 75 0D 80 3D",
            Offset = 0x6
        };

        public static readonly Signature ConfigManager = new Signature
        {
            Pattern = "8B 45 DC 8B 0D",
            Offset = 0x5
        };

        public static readonly Signature Player = new Signature
        {
            Pattern = "FF 50 0C 8B D8 8B 15",
            Offset = 0x7
        };

        //TODO: i couldn't create signature for this one :(
        public static readonly int[] AudioRateOffsets = new int[]
        {
            0x00034268,
            0x8,
            0x10,
            0xC,
            0x40
        };
    }

    public class Signature
    {
        public string Pattern;
        public int Offset;
    }
}
