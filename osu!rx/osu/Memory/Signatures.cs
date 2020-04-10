namespace osu_rx.osu.Memory
{
    public static class Signatures
    {
        public static readonly Signature GameBase = new Signature
        {
            Pattern = "C7 45 E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? A1 ?? ?? ?? ?? 89 45 CC 8B 45 D4",
            Offset = 0xD
        };

        public static readonly Signature ConfigManager = new Signature
        {
            Pattern = "8B 45 DC 8B 0D",
            Offset = 0x5
        };

        public static readonly Signature Mods = new Signature
        {
            Pattern = "53 8B F1 A1 ?? ?? ?? ?? 25 ?? ?? ?? ?? 85 C0",
            Offset = 0x4
        };

        public static readonly int[] CursorPositionXOffsetChain = new int[]{ -2452, 8, 28, 16, 432, 380, 8, 476, 20, 304, 124 };
        public const int CursorPositionXOffset = 4;
        public const int CursorPositionYOffset = 8;
    }

    public class Signature
    {
        public string Pattern;
        public int Offset;
    }
}
