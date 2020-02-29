namespace osu_rx.Helpers
{
    public static class Constants
    {
        public const string AudioTimePattern = "39 09 FF 15 ?? ?? ?? ?? A1 ?? ?? ?? ?? 2B 05 ?? ?? ?? ?? A3";
        public const int AudioTimeOffset = 9;
        public const int IsAudioPlayingOffset = 30;

        public const string GameStatePattern = "8D 45 BC 89 46 0C 83 3D";
        public const int GameStateOffset = 8;

        public const string ModsPattern = "D9 1D ?? ?? ?? ?? 8B 7E 40 8B 0D";
        public const int ModsOffset = 0xB;

        public const string ReplayModePattern = "85 C0 75 0D 80 3D";
        public const int ReplayModeOffset = 6;
    }
}
