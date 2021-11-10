using System;
using System.Collections.Generic;
using System.Text;
using osu.Framework.Utils;
using osu.Game.Rulesets.Osu.Replays.Postprocessors;
using osu.Game.Rulesets.Replays;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Interpolators
{
    // port of https://github.com/Ushio/ConstrainedCubicSpline/blob/master/cs_ver/ConstrainedCubicSpline.cs
    public class ConstrainedCubicSplineInterpolator : ReplayInterpolator
    {
        // i don't actually understand the math behind this, so this uses two separate splines for two axises
        // SplineX -> (time, pos.X)
        // SplineY -> (time, pos.Y)
        public ConstrainedCubicSpline SplineX;
        public ConstrainedCubicSpline SplineY;

        private double lastTime = double.NegativeInfinity;

        public override void Init(List<OsuAuto2BGenerator.OsuReplayFrameWithReason> frames, List<ReplayFrame> outputFrames, OsuAuto2BGenerator autoGenerator, params ReplayPostprocessor[] postprocessors)
        {
            base.Init(frames, outputFrames, autoGenerator, postprocessors);

            List<Point> pointsX = new List<Point>(frames.Count);
            List<Point> pointsY = new List<Point>(frames.Count);

            double last = double.NegativeInfinity;
            for (int i = 0; i < frames.Count; i++)
            {
                var frame = (OsuReplayFrame)frames[i];
                if (Precision.AlmostEquals(frame.Time, last))
                    continue;
                last = frame.Time;
                pointsX.Add(new Point(frame.Time, frame.Position.X));
                pointsY.Add(new Point(frame.Time, frame.Position.Y));
            }

            SplineX = new ConstrainedCubicSpline(pointsX.ToArray());
            SplineY = new ConstrainedCubicSpline(pointsY.ToArray());
        }

        public override void Update(OsuReplayFrame frame)
        {
            if (OutputFrames.Count == 0)
                return;

            if (Precision.AlmostEquals(frame.Time, lastTime))
                return;
            lastTime = frame.Time;

            var lastFrame = (OsuReplayFrame)OutputFrames[^1];
            for (double t = lastFrame.Time + FrameInterval; t < frame.Time; t += FrameInterval)
            {
                var pos = new Vector2((float)SplineX.Evaluate(t), (float)SplineY.Evaluate(t));
                addFrame(new OsuReplayFrame(t, pos, frame.Actions.ToArray()));
            }
        }

        public class Point
        {
            public double X;
            public double Y;

            public Point(double x, double y)
            {
                X = x;
                Y = y;
            }
        }

        public class ConstrainedCubicSpline
        {
            public class Cubic
            {
                public double A;
                public double B;
                public double C;
                public double D;

                public Cubic(double a, double b, double c, double d)
                {
                    A = a;
                    B = b;
                    C = c;
                    D = d;
                }

                public double Evaluate(double x)
                {
                    // .net core 3.0 has System.Math.FusedMultiplyAdd, but not .net standard 2.0!
                    static double fma(double a, double b, double c) => a * b + c;
                    return fma(fma(fma(D, x, C), x, B), x, A);
                }
            }

            public Point[] Points;
            public Cubic[] Cubics;

            public ConstrainedCubicSpline(Point[] points)
            {
                Points = points;
                Cubics = new Cubic[points.Length - 1];
                // TODO: this is totally parallelizable
                for (int i = 0; i < Cubics.Length; i++)
                    Cubics[i] = calcCubic(i + 1);
            }

            private static double square(double x) => x * x;
            private static double cube(double x) => x * x * x;

            public double Evaluate(double x)
            {
                if (Points.Length == 0)
                    return 0;
                else if (Points.Length == 1)
                    return Points[0].Y;

                if (x <= Points[0].X)
                    return Points[0].Y;
                else if (x >= Points[^1].X)
                    return Points[^1].Y;

                int upper = Points.Length - 1;
                int lower = 0;

                while (lower + 1 != upper)
                {
                    int mid = (lower + upper) / 2;
                    if (x < Points[mid].X)
                        upper = mid;
                    else
                        lower = mid;
                }

                return Cubics[upper - 1].Evaluate(x);
            }

            private double fDot(int i)
            {
                if (i == 0)
                    return 3.0 * (Points[1].Y - Points[0].Y) / (2.0 * (Points[1].X - Points[0].X)) - fDot(1) / 2.0;

                int n = Points.Length - 1;
                if (i == n)
                    return 3.0 * (Points[n].Y - Points[n - 1].Y) / (2.0 * (Points[n].X - Points[n - 1].X)) - fDot(n - 1) / 2.0;

                double slope = 2.0 / (
                    (Points[i + 1].X - Points[i].X) / (Points[i + 1].Y - Points[i].Y)
                    +
                    (Points[i].X - Points[i - 1].X) / (Points[i].Y - Points[i - 1].Y)
                    );

                double sLHS = (Points[i + 1].Y - Points[i].Y) / (Points[i + 1].X - Points[i].X);
                double sRHS = (Points[i].Y - Points[i - 1].Y) / (Points[i].X - Points[i - 1].X);
                if (Math.Sign(sLHS) == Math.Sign(sRHS))
                    return slope;
                return 0;
                //return slope;
            }

            private Cubic calcCubic(int i)
            {
                double fDoti = fDot(i);
                double fDotiMinus1 = fDot(i - 1);

                double fDotDoti =
                    2.0 * (2.0 * fDoti + fDotiMinus1) / (Points[i].X - Points[i - 1].X)
                    -
                    6.0 * (Points[i].Y - Points[i - 1].Y) / square(Points[i].X - Points[i - 1].X);
                double fDotDotiMinus1 =
                    -2.0 * (fDoti + 2.0 * fDotiMinus1) / (Points[i].X - Points[i - 1].X)
                    +
                    6.0 * (Points[i].Y - Points[i - 1].Y) / square(Points[i].X - Points[i - 1].X);

                double d = (fDotDoti - fDotDotiMinus1) / (6.0 * (Points[i].X - Points[i - 1].X));
                double c = (Points[i].X * fDotDotiMinus1 - Points[i - 1].X * fDotDoti) / (2.0 * (Points[i].X - Points[i - 1].X));
                double b = (Points[i].Y - Points[i - 1].Y - c * (square(Points[i].X) - square(Points[i - 1].X)) - d * (cube(Points[i].X) - cube(Points[i - 1].X))) / (Points[i].X - Points[i - 1].X);
                double a = Points[i - 1].Y - b * Points[i - 1].X - c * square(Points[i - 1].X) - d * cube(Points[i - 1].X);
                return new Cubic(a, b, c, d);
            }
        }
    }
}
