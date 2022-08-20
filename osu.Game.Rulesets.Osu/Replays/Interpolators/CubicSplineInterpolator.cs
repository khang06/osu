// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Utils;
using osu.Game.Rulesets.Osu.Replays.Postprocessors;
using osu.Game.Rulesets.Replays;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Interpolators
{
    // port from danser
    public class CubicSplineInterpolator : ReplayInterpolator
    {
        private Spline? path = null;

        private double lastTime = double.NegativeInfinity;

        private class Bezier
        {
            // no length needed for this
            public readonly Vector2[] Points;

            public Bezier(Vector2[] points)
            {
                Points = points;
            }

            public Vector2 PointAt(float t)
            {
                // fun time
                //t = Math.Clamp(t, 0.0f, 1.0f);
                var p = new Vector2();
                int n = Points.Length - 1;

                for (int i = 0; i <= n; i++)
                {
                    float b = bernstein(i, n, t);
                    p.X += Points[i].X * b;
                    p.Y += Points[i].Y * b;
                }

                return p;
            }

            private long binomialCoefficient(long n, long k)
            {
                long r = 1;
                if (k > n)
                    return 0;

                for (long d = 1; d <= k; d++)
                {
                    r *= n--;
                    r /= d;
                }

                return r;
            }

            private float bernstein(long i, long n, float t)
            {
                return (float)(binomialCoefficient(n, i) * Math.Pow(t, i) * Math.Pow(1.0 - t, n - i));
            }
        }

        private class Spline
        {
            public readonly float[] Sections;
            public readonly Bezier[] Path;
            public readonly float Length;

            public Spline(Bezier[] curves, float[] weights)
            {
                Path = curves;
                Sections = new float[curves.Length + 1];
                Length = 0;

                for (int i = 0; i < curves.Length; i++)
                {
                    Length = weights[i];
                    Sections[i + 1] = Length;
                }
            }

            public Vector2 PointAt(float t)
            {
                // no clamping is more fun
                double desiredWidth = Length * Math.Clamp(t, 0.0f, 1.0f);

                float[] withoutFirst = Sections[1..];
                int index = Array.BinarySearch(withoutFirst, (float)desiredWidth);
                if (index < 0)
                    index = ~index;
                index = Math.Max(0, Math.Min(index, Path.Length - 1));

                if (Sections[index + 1] - Sections[index] == 0)
                    return Path[index].PointAt(0);

                float bt = (float)(desiredWidth - Sections[index]) / (Sections[index + 1] - Sections[index]);
                //float bt = (float)Interpolation.ValueAt(desiredWidth, 0.0, 1.0, Sections[index], Sections[index + 1], Framework.Graphics.Easing.InOutBack);
                // TODO: this would be way nicer if it scaled based on the length of the next and previous curves
                return Path[index].PointAt(bt);
            }
        }

        // TODO: this is black magic!!! what the hell is this doing???
        // i can't find anything online that looks like this
        private Bezier[] solveBSpline(Vector2[] points1)
        {
            int pointsLen = points1.Length;

            // visual studio would not shut the fuck up if i didn't do this
            var points = new List<Vector2>(pointsLen)
            {
                points1[0]
            };
            points.AddRange(points1[2..(pointsLen - 2)]);
            points.Add(points1[pointsLen - 1]);
            points.Add(points1[1]);
            points.Add(points1[pointsLen - 2]);

            int n = points.Count - 2;

            var d = new Vector2[n];
            d[0] = points[n] - points[0];
            d[n - 1] = (points[n + 1] - points[n - 1]) * -1;

            var a = new Vector2[points.Count];
            float[] bi = new float[points.Count];

            bi[1] = -0.25f;
            a[1] = (points[2] - points[0] - d[0]) / 4;

            for (int i = 2; i < n - 1; i++)
            {
                bi[i] = -1 / (4 + bi[i - 1]);
                a[i] = (points[i + 1] - points[i - 1] - a[i - 1]) * (-1 * bi[i]);
            }

            for (int i = n - 2; i > 0; i--)
                d[i] = a[i] + d[i + 1] * bi[i];

            var bezierPoints = new List<Vector2>
            {
                points[0],
                points[0] + d[0]
            };

            for (int i = 1; i < n - 1; i++)
            {
                bezierPoints.Add(points[i] - d[i]);
                bezierPoints.Add(points[i]);
                bezierPoints.Add(points[i] + d[i]);
            }

            bezierPoints.Add(points[n - 1] - d[n - 1]);
            bezierPoints.Add(points[n - 1]);

            var beziers = new List<Bezier>(bezierPoints.Count);
            var bezierPointsArray = bezierPoints.ToArray();
            for (int i = 0; i < bezierPoints.Count - 3; i += 3)
                beziers.Add(new Bezier(bezierPointsArray[i..(i + 4)]));

            return beziers.ToArray();
        }

        public override void Init(List<OsuAuto2BGenerator.OsuReplayFrameWithReason> inputFrames, List<ReplayFrame> outputFrames, OsuAuto2BGenerator autoGenerator, params ReplayPostprocessor[] postprocessors)
        {
            base.Init(inputFrames, outputFrames, autoGenerator, postprocessors);

            var prunedFrames = new List<OsuAuto2BGenerator.OsuReplayFrameWithReason>(inputFrames.Count);
            double last = inputFrames[0].Time - 3000;

            for (int i = 0; i < inputFrames.Count; i++)
            {
                if (Precision.AlmostEquals(inputFrames[i].Time, last, 1))
                    continue;

                prunedFrames.Add(inputFrames[i]);
                last = inputFrames[i].Time;
            }

            // TODO: fix this properly later
            //var beziers = solveBSpline(prunedFrames.Select(x => x.Position).Prepend(Vector2.Zero).Prepend(Vector2.Zero).ToArray());
            //path = new Spline(beziers, prunedFrames.Select(x => (float)x.Time).Take(prunedFrames.Count - 1).ToArray());

            var points = new List<Vector2>();
            var timing = new List<float>();
            float offset = 0;

            for (int i = 0; i < prunedFrames.Count; i++)
            {
                var frame = prunedFrames[i];

                if (i == 0)
                {
                    // TODO: totally wrong
                    points.Add(frame.Position);
                    points.Add(frame.Position);
                    points.Add(frame.Position);
                    offset = (float)frame.Time;
                    timing.Add(0);
                    continue;
                }

                if (i == prunedFrames.Count - 1)
                {
                    points.Add(Vector2.Zero); // TODO: totally wrong
                    points.Add(frame.Position);
                    timing.Add((float)frame.Time - offset);
                    break;
                }
                else if (i > 1 && i < prunedFrames.Count - 1)
                {
                    // ...
                }

                points.Add(frame.Position);
                timing.Add((float)frame.Time - offset);
            }

            var beziers = solveBSpline(points.ToArray());

            for (int i = 0; i < timing.Count - 1; i++)
            {
                // TODO: lol
                if (i > beziers.Length - 2)
                    break;

                float diff = timing[i + 1] - timing[i];

                if (diff > 600)
                {
                    float scale = diff / 2;
                    var b = beziers[i + 1];
                    b.Points[1] = b.Points[0] + ((b.Points[1] - b.Points[0]).Normalized() * scale);
                    b.Points[2] = b.Points[3] + ((b.Points[2] - b.Points[3]).Normalized() * scale);
                }
            }

            path = new Spline(beziers, timing.ToArray());
        }

        public override void Update(OsuReplayFrame frame)
        {
            if (path == null || InputFrames == null || OutputFrames == null || OutputFrames.Count == 0)
                return;

            if (Precision.AlmostEquals(frame.Time, lastTime, 1))
                return;

            lastTime = frame.Time;

            // get the closest set of catmull parameters
            var lastFrame = (OsuReplayFrame)OutputFrames[^1];

            for (double t = lastFrame.Time + FrameInterval; t < frame.Time; t += FrameInterval)
            {
                float p = (float)((t - InputFrames[0].Time) / (InputFrames[^1].Time - InputFrames[0].Time));
                var pos = path.PointAt(p);
                AddFrame(new OsuReplayFrame(t, pos, frame.Actions.ToArray()));
            }
        }
    }
}
