using OsuParsers.Beatmaps.Objects;
using OsuParsers.Enums.Beatmaps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace osu_rx.Helpers
{
    //https://github.com/ppy/osu-framework/blob/master/osu.Framework/MathUtils/PathApproximator.cs
    public class CurveHelper
    {
        public static List<Vector2> ApproximateCircularArc(List<Vector2> points)
        {
            Vector2 a = points[0];
            Vector2 b = points[1];
            Vector2 c = points[2];

            float aSq = (b - c).LengthSquared();
            float bSq = (a - c).LengthSquared();
            float cSq = (a - b).LengthSquared();

            if (almostEquals(aSq, 0) || almostEquals(bSq, 0) || almostEquals(cSq, 0))
                return new List<Vector2>();

            float s = aSq * (bSq + cSq - aSq);
            float t = bSq * (aSq + cSq - bSq);
            float u = cSq * (aSq + bSq - cSq);

            float sum = s + t + u;

            if (almostEquals(sum, 0))
                return new List<Vector2>();

            Vector2 centre = (s * a + t * b + u * c) / sum;
            Vector2 dA = a - centre;
            Vector2 dC = c - centre;

            float r = dA.Length();

            double thetaStart = Math.Atan2(dA.Y, dA.X);
            double thetaEnd = Math.Atan2(dC.Y, dC.X);

            while (thetaEnd < thetaStart)
                thetaEnd += 2 * Math.PI;

            double dir = 1;
            double thetaRange = thetaEnd - thetaStart;

            Vector2 orthoAtoC = c - a;
            orthoAtoC = new Vector2(orthoAtoC.Y, -orthoAtoC.X);

            if (Vector2.Dot(orthoAtoC, b - a) < 0)
            {
                dir = -dir;
                thetaRange = 2 * Math.PI - thetaRange;
            }

            int amountPoints = 2 * r <= 0.1f ? 2 : Math.Max(2, (int)Math.Ceiling(thetaRange / (2 * Math.Acos(1 - 0.1f / r))));

            List<Vector2> output = new List<Vector2>(amountPoints);

            for (int i = 0; i < amountPoints; ++i)
            {
                double fract = (double)i / (amountPoints - 1);
                double theta = thetaStart + dir * fract * thetaRange;
                Vector2 o = new Vector2((float)Math.Cos(theta), (float)Math.Sin(theta)) * r;
                output.Add(centre + o);
            }

            return output;
        }

        public static List<Vector2> ApproximateCatmull(List<Vector2> points)
        {
            var result = new List<Vector2>();

            for (int i = 0; i < points.Count - 1; i++)
            {
                var v1 = i > 0 ? points[i - 1] : points[i];
                var v2 = points[i];
                var v3 = i < points.Count - 1 ? points[i + 1] : v2 + v2 - v1;
                var v4 = i < points.Count - 2 ? points[i + 2] : v3 + v3 - v2;

                for (int c = 0; c < 50; c++)
                {
                    result.Add(catmullFindPoint(ref v1, ref v2, ref v3, ref v4, (float)c / 50));
                    result.Add(catmullFindPoint(ref v1, ref v2, ref v3, ref v4, (float)(c + 1) / 50));
                }
            }

            return result;
        }

        public static List<Vector2> ApproximateBezier(List<Vector2> points)
        {
            List<Vector2> output = new List<Vector2>();

            if (points.Count == 0)
                return output;

            var subdivisionBuffer1 = new Vector2[points.Count];
            var subdivisionBuffer2 = new Vector2[points.Count * 2 - 1];

            Stack<Vector2[]> toFlatten = new Stack<Vector2[]>();
            Stack<Vector2[]> freeBuffers = new Stack<Vector2[]>();

            toFlatten.Push(points.ToArray());

            Vector2[] leftChild = subdivisionBuffer2;

            while (toFlatten.Count > 0)
            {
                Vector2[] parent = toFlatten.Pop();

                if (bezierIsFlatEnough(parent))
                {
                    bezierApproximate(parent, output, subdivisionBuffer1, subdivisionBuffer2, points.Count);

                    freeBuffers.Push(parent);
                    continue;
                }

                Vector2[] rightChild = freeBuffers.Count > 0 ? freeBuffers.Pop() : new Vector2[points.Count];
                bezierSubdivide(parent, leftChild, rightChild, subdivisionBuffer1, points.Count);

                for (int i = 0; i < points.Count; ++i)
                    parent[i] = leftChild[i];

                toFlatten.Push(rightChild);
                toFlatten.Push(parent);
            }

            output.Add(points[points.Count - 1]);
            return output;
        }

        private static Vector2 catmullFindPoint(ref Vector2 vec1, ref Vector2 vec2, ref Vector2 vec3, ref Vector2 vec4, float t)
        {
            float t2 = t * t;
            float t3 = t * t2;

            Vector2 result;
            result.X = 0.5f * (2f * vec2.X + (-vec1.X + vec3.X) * t + (2f * vec1.X - 5f * vec2.X + 4f * vec3.X - vec4.X) * t2 + (-vec1.X + 3f * vec2.X - 3f * vec3.X + vec4.X) * t3);
            result.Y = 0.5f * (2f * vec2.Y + (-vec1.Y + vec3.Y) * t + (2f * vec1.Y - 5f * vec2.Y + 4f * vec3.Y - vec4.Y) * t2 + (-vec1.Y + 3f * vec2.Y - 3f * vec3.Y + vec4.Y) * t3);

            return result;
        }

        private static void bezierApproximate(Vector2[] points, List<Vector2> output, Vector2[] subdivisionBuffer1, Vector2[] subdivisionBuffer2, int count)
        {
            Vector2[] l = subdivisionBuffer2;
            Vector2[] r = subdivisionBuffer1;

            bezierSubdivide(points, l, r, subdivisionBuffer1, count);

            for (int i = 0; i < count - 1; ++i)
                l[count + i] = r[i + 1];

            output.Add(points[0]);

            for (int i = 1; i < count - 1; ++i)
            {
                int index = 2 * i;
                Vector2 p = 0.25f * (l[index - 1] + 2 * l[index] + l[index + 1]);
                output.Add(p);
            }
        }

        private static void bezierSubdivide(Vector2[] points, Vector2[] l, Vector2[] r, Vector2[] subdivisionBuffer, int count)
        {
            Vector2[] midpoints = subdivisionBuffer;

            for (int i = 0; i < count; ++i)
                midpoints[i] = points[i];

            for (int i = 0; i < count; i++)
            {
                l[i] = midpoints[0];
                r[count - i - 1] = midpoints[count - i - 1];

                for (int j = 0; j < count - i - 1; j++)
                    midpoints[j] = (midpoints[j] + midpoints[j + 1]) / 2;
            }
        }

        private static bool bezierIsFlatEnough(Vector2[] points)
        {
            for (int i = 1; i < points.Length - 1; i++)
            {
                if ((points[i - 1] - 2 * points[i] + points[i + 1]).LengthSquared() > 0.25f * 0.25f * 4)
                    return false;
            }

            return true;
        }

        private static bool almostEquals(float a, float b) => Math.Abs(a - b) <= 1e-3f;
    }

    //https://github.com/ppy/osu/blob/master/osu.Game/Rulesets/Objects/SliderPath.cs
    public class SliderPath
    {
        public readonly double PixelLength;

        public readonly CurveType CurveType;

        public double Distance
        {
            get => cumulativeLength.Count == 0 ? 0 : cumulativeLength[cumulativeLength.Count - 1];
        }

        private Vector2[] sliderPoints;

        private List<Vector2> calculatedPath = new List<Vector2>();
        private List<double> cumulativeLength = new List<double>();

        public SliderPath(Slider slider)
        {
            sliderPoints = slider.SliderPoints.ToArray();
            CurveType = slider.CurveType;
            PixelLength = slider.PixelLength;

            calculatePath();
            calculateCumulativeLength();
        }

        public Vector2 PositionAt(double progress)
        {
            double d = progressToDistance(progress);
            return interpolateVertices(indexOfDistance(d), d);
        }

        private List<Vector2> calculateSubpath(List<Vector2> points)
        {
            switch (CurveType)
            {
                case CurveType.Linear:
                    return points;

                case CurveType.PerfectCurve:
                    if (sliderPoints.Length != 3 || points.ToArray().Length != 3)
                        break;

                    List<Vector2> subpath = CurveHelper.ApproximateCircularArc(points);

                    if (subpath.Count == 0)
                        break;

                    return subpath;

                case CurveType.Catmull:
                    return CurveHelper.ApproximateCatmull(points);
            }

            return CurveHelper.ApproximateBezier(points);
        }

        private void calculatePath()
        {
            int start = 0;
            int end = 0;

            for (int i = 0; i < sliderPoints.Length; ++i)
            {
                end++;

                if (i == sliderPoints.Length - 1 || sliderPoints[i] == sliderPoints[i + 1])
                {
                    var points = sliderPoints.Skip(start).Take(end - start).ToList();

                    foreach (Vector2 t in calculateSubpath(points))
                    {
                        if (calculatedPath.Count == 0 || calculatedPath.Last() != t)
                            calculatedPath.Add(t);
                    }

                    start = end;
                }
            }
        }

        private void calculateCumulativeLength()
        {
            double l = 0;

            cumulativeLength.Clear();
            cumulativeLength.Add(l);

            for (int i = 0; i < calculatedPath.Count - 1; ++i)
            {
                Vector2 diff = calculatedPath[i + 1] - calculatedPath[i];
                double d = diff.Length();

                if (PixelLength - l < d)
                {
                    calculatedPath[i + 1] = calculatedPath[i] + diff * (float)((PixelLength - l) / d);
                    calculatedPath.RemoveRange(i + 2, calculatedPath.Count - 2 - i);

                    l = PixelLength;
                    cumulativeLength.Add(l);
                    break;
                }

                l += d;
                cumulativeLength.Add(l);
            }

            if (l < PixelLength && calculatedPath.Count > 1)
            {
                Vector2 diff = calculatedPath[calculatedPath.Count - 1] - calculatedPath[calculatedPath.Count - 2];
                double d = diff.Length();

                if (d <= 0)
                    return;

                calculatedPath[calculatedPath.Count - 1] += diff * (float)((PixelLength - l) / d);
                cumulativeLength[calculatedPath.Count - 1] = PixelLength;
            }
        }

        private int indexOfDistance(double d)
        {
            int i = cumulativeLength.BinarySearch(d);
            if (i < 0) i = ~i;

            return i;
        }

        private double progressToDistance(double progress)
        {
            return progress * Distance;
        }

        private Vector2 interpolateVertices(int i, double d)
        {
            if (calculatedPath.Count == 0)
                return Vector2.Zero;

            if (i <= 0)
                return calculatedPath.First();
            if (i >= calculatedPath.Count)
                return calculatedPath.Last();

            Vector2 p0 = calculatedPath[i - 1];
            Vector2 p1 = calculatedPath[i];

            double d0 = cumulativeLength[i - 1];
            double d1 = cumulativeLength[i];

            if (d0.AlmostEquals(d1, 1e-7))
                return p0;

            double w = (d - d0) / (d1 - d0);
            return p0 + (p1 - p0) * (float)w;
        }
    }
}
