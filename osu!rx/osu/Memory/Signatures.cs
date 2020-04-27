namespace osu_rx.osu.Memory
{
    public static class Signatures
    {
        public static readonly Signature Time = new Signature
        {
            Pattern = "7E 55 8B 76 10 DB 05",
            Offset = 0x7
        };

        public const int IsAudioPlayingOffset = 0x30;

        public static readonly Signature Mods = new Signature
        {
            Pattern = "85 DB 75 0A 81 25",
            Offset = 0x6
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

        public static readonly Signature RetryCount = new Signature
        {
            Pattern = "74 08 FF 05 ?? ?? ?? ?? EB 08 33 D2",
            Offset = 0x4
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
    }

    public class Signature
    {
        public string Pattern;
        public int Offset;
    }
}
