using System;
using System.Drawing;
using System.Numerics;

namespace osu_rx.Helpers
{
    public static class Extensions
    {
        public static float NextFloat(this Random random, float min, float max) => (float)random.NextDouble() * (max - min) + min;

        public static bool AlmostEquals(this float f, float value, float allowance) => Math.Abs(f - value) <= allowance;

        public static Vector2 ToVector2(this Point point) => new Vector2(point.X, point.Y);
    }
}
