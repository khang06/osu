
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using osu.Framework.Utils;
using osu.Game.Rulesets.Osu.Replays.Postprocessors;
using osu.Game.Rulesets.Replays;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Interpolators
{
    public class AutoPlusCubicSplineInterpolator : ReplayInterpolator
    {
        // port of Auto+'s interpolator

        protected struct SplinePoints
        {
            public unsafe double* ts;

            public unsafe double* xs;

            public unsafe double* ys;

            public uint n;

            private IntPtr private_data;
        }

        protected SplinePoints Spline;

        protected int CurrentSplinePointIndex;

        public double StartTime { get; set; }

        public double EndTime { get; set; }

        [DllImport("Spline.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void spline_interp(double step, double d1_x, double d1_y, double dn_x, double dn_y, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 8)] double[] ts, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 8)] double[] xs, [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 8)] double[] ys, uint n, out SplinePoints result);

        [DllImport("Spline.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void spline_free(ref SplinePoints splinePoints);

        private double lastTime = double.NegativeInfinity;

        private void dumpToFile(string name, double[] values)
        {
            byte[] result = new byte[values.Length * sizeof(double)];
            Buffer.BlockCopy(values, 0, result, 0, result.Length);
            File.WriteAllBytes(name, result);
        }

        public override void Init(List<OsuAuto2BGenerator.OsuReplayFrameWithReason> inputFrames, List<ReplayFrame> outputFrames, OsuAuto2BGenerator autoGenerator,
                                  params ReplayPostprocessor[] postprocessors)
        {
            base.Init(inputFrames, outputFrames, autoGenerator, postprocessors);

            var prunedFrames = new List<OsuAuto2BGenerator.OsuReplayFrameWithReason>(inputFrames.Count);
            double last = double.NegativeInfinity;

            for (int i = 0; i < inputFrames.Count; i++)
            {
                if (i != 0 && Precision.AlmostEquals(inputFrames[i].Time, last, 1))
                    continue;
                //    prunedFrames.RemoveAt(prunedFrames.Count - 1);

                prunedFrames.Add(inputFrames[i]);
                last = inputFrames[i].Time;
            }

            StartTime = prunedFrames[0].Time;
            EndTime = prunedFrames[^1].Time;

            List<double> list2 = new List<double>();
            List<double> list3 = new List<double>();
            List<double> list4 = new List<double>();

            foreach (var item in prunedFrames)
            {
                list2.Add(item.Time);
                list3.Add(item.Position.X);
                list4.Add(item.Position.Y);
            }

            double[] ts = list2.ToArray();
            double[] xs = list3.ToArray();
            double[] ys = list4.ToArray();

            //dumpToFile("ts.bin", ts);
            //dumpToFile("xs.bin", ts);
            //dumpToFile("ys.bin", ts);

            // derivative at start/end not implemented
            spline_interp(FrameInterval, 0, 0, 0, 0, ts, xs, ys, (uint)list2.Count, out Spline);
            CurrentSplinePointIndex = 0;
        }

        ~AutoPlusCubicSplineInterpolator()
        {
            spline_free(ref Spline);
        }

        private unsafe Vector2 positionAtTime(double time)
        {
            while (CurrentSplinePointIndex != Spline.n - 1 && Spline.ts[CurrentSplinePointIndex + 1] <= time)
                CurrentSplinePointIndex++;
            while (Spline.ts[CurrentSplinePointIndex] > time)
                CurrentSplinePointIndex--;

            return new Vector2((float)Spline.xs[CurrentSplinePointIndex], (float)Spline.ys[CurrentSplinePointIndex]);
        }

        public override void Update(OsuReplayFrame frame)
        {
            if (OutputFrames.Count == 0)
                return;

            if (Precision.AlmostEquals(frame.Time, lastTime, 1))
                return;

            lastTime = frame.Time;

            var lastFrame = (OsuReplayFrame)OutputFrames[^1];

            for (double t = lastFrame.Time + FrameInterval; t < frame.Time; t += FrameInterval)
            {
                var pos = positionAtTime(t);
                addFrame(new OsuReplayFrame(t, pos, frame.Actions.ToArray()));
            }
        }
    }
}
