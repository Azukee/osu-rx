namespace osu_rx.osu.Memory
{
    public static class Signatures
    {
        public static readonly Signature GameBase = new Signature
        {
            //TODO: this might not actually be a gamebase because replaymode shouldn't be a part of it
            //TODO: that's actually ReplayMode's pattern but who cares LUL, ^ also yeah it's not a gamebase
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
    }

    public class Signature
    {
        public string Pattern;
        public int Offset;
    }
}
