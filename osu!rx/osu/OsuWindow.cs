using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;

namespace osu_rx.osu
{
    public class OsuWindow
    {
        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hwnd, out Rect rectangle);

        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hwnd, out Point point);

        private struct Rect
        {
            public int Left { get; set; }
            public int Top { get; set; }
            public int Right { get; set; }
            public int Bottom { get; set; }
        }

        private IntPtr windowHandle;

        public Vector2 WindowSize
        {
            get
            {
                GetClientRect(windowHandle, out var osuRectangle);
                return new Vector2(osuRectangle.Right, osuRectangle.Bottom);
            }
        }

        public Vector2 WindowPosition
        {
            get
            {
                ClientToScreen(windowHandle, out var osuPosition);
                return new Vector2(osuPosition.X, osuPosition.Y);
            }
        }

        public float WindowRatio
        {
            get => WindowSize.Y / 480;
        }

        public Vector2 PlayfieldSize
        {
            get
            {
                float width = 512 * WindowRatio;
                float height = 384 * WindowRatio;
                return new Vector2(width, height);
            }
        }

        public Vector2 PlayfieldPosition //topleft origin
        {
            get
            {
                var windowCentre = WindowSize / 2;
                float x = windowCentre.X - PlayfieldSize.X / 2;
                float y = windowCentre.Y - PlayfieldSize.Y / 2;
                return new Vector2(x, y);
            }
        }

        public float PlayfieldRatio
        {
            get => PlayfieldSize.Y / 384;
        }

        public OsuWindow(IntPtr handle) => windowHandle = handle;
    }
}
