﻿namespace osu_rx.Helpers
{
    public static class Constants
    {
        public const string AudioTimePattern = "D9 58 2C 8B 3D ?? ?? ?? ?? 8B 1D";
        public const int AudioTimeOffset = 11;
        public const int IsAudioPlayingOffset = 48;

        public const string GameStatePattern = "8D 45 BC 89 46 0C 83 3D";
        public const int GameStateOffset = 8;

        public const string ModsPattern = "53 8B F1 A1 ?? ?? ?? ?? 25 ?? ?? ?? ?? 85 C0";
        public const int ModsOffset = 4;

        public const string ReplayModePattern = "85 C0 75 0D 80 3D";
        public const int ReplayModeOffset = 6;

        public const string CursorPositionXPattern = "00 00 00 00 B4 78 A0 6F 00 00 00 00 00 00 00 00 56 05 00 00 00 03 00 00 00 00 00 00 ?? ?? ?? ??";
        public const int CursorPositionXOffset = 48;
        public const int CursorPositionYOffset = 4;
    }
}
