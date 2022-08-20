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
    // mostly just ripped from osu-framework implementation
    public class CatmullRomInterpolator : ReplayInterpolator
    {
        private List<CatmullParams>? catmullParams = null;

        private double lastTime = double.NegativeInfinity;

        private class CatmullParams : IComparable<CatmullParams>
        {
            public CatmullParams(double time)
                : this(time, Vector2.Zero, Vector2.Zero, Vector2.Zero, Vector2.Zero)
            { }

            public CatmullParams(double time, Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
            {
                Time = time;
                P1 = p1;
                P2 = p2;
                P3 = p3;
                P4 = p4;
            }

            public Vector2 Evaluate(float t)
            {
                // PathApproximator.catmullFindPoint
                float t2 = t * t;
                float t3 = t * t2;

                Vector2 result;
                result.X = 0.5f * (2f * P2.X + (-P1.X + P3.X) * t + (2f * P1.X - 5f * P2.X + 4f * P3.X - P4.X) * t2 + (-P1.X + 3f * P2.X - 3f * P3.X + P4.X) * t3);
                result.Y = 0.5f * (2f * P2.Y + (-P1.Y + P3.Y) * t + (2f * P1.Y - 5f * P2.Y + 4f * P3.Y - P4.Y) * t2 + (-P1.Y + 3f * P2.Y - 3f * P3.Y + P4.Y) * t3);

                return result;
            }

            public int CompareTo(CatmullParams other)
            {
                return Time.CompareTo(other.Time);
            }

            public double Time;
            public Vector2 P1;
            public Vector2 P2;
            public Vector2 P3;
            public Vector2 P4;
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

            catmullParams = new List<CatmullParams>(prunedFrames.Count);
            for (int i = 0; i < prunedFrames.Count; i++)
            {
                var p1 = i > 0 ? prunedFrames[i - 1].Position : prunedFrames[i].Position;
                var p2 = prunedFrames[i].Position;
                var p3 = i < prunedFrames.Count - 1 ? prunedFrames[i + 1].Position : p2 + p2 - p1;
                var p4 = i < prunedFrames.Count - 2 ? prunedFrames[i + 2].Position : p3 + p3 - p2;

                catmullParams.Add(new CatmullParams(prunedFrames[i].Time, p1, p2, p3, p4));
            }
        }

        public override void Update(OsuReplayFrame frame)
        {
            if (catmullParams == null || OutputFrames == null || OutputFrames.Count == 0)
                return;

            if (Precision.AlmostEquals(frame.Time, lastTime, 1))
                return;
            lastTime = frame.Time;

            // get the closest set of catmull parameters
            var lastFrame = (OsuReplayFrame)OutputFrames[^1];
            int index = catmullParams.BinarySearch(new CatmullParams(lastFrame.Time));
            if (index < 0)
                index = ~index;
            index = Math.Max(0, Math.Min(index, catmullParams.Count - 1));
            var catParams = catmullParams[index];
            double nextParamTime = index == catmullParams.Count - 1 ? catParams.Time + 3000 : catmullParams[index + 1].Time;
            //double lastParamTime = index == 0 ? catParams.Time - 3000 : catmullParams[index - 1].Time;

            for (double t = lastFrame.Time + FrameInterval; t < frame.Time; t += FrameInterval)
            {
                //float p = (float)((t - lastParamTime) / (catParams.Time - lastParamTime));
                float p = (float)((t - catParams.Time) / (nextParamTime - catParams.Time));
                var pos = catParams.Evaluate(p);
                AddFrame(new OsuReplayFrame(t, pos, frame.Actions.ToArray()));
            }
        }
    }
}
