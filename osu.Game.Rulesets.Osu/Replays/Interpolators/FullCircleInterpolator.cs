// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Text;
using osu.Framework.Utils;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Interpolators
{
    public class FullCircleInterpolator : ReplayInterpolator
    {
        private double lastTime = double.NegativeInfinity;

        private Vector2 lastPos = Vector2.Zero;
        private Vector2 lastMidpoint = Vector2.Zero;
        private double lastRadius = 100;
        private double lastInitialAngle = 0;

        public override void Update(OsuReplayFrame frame)
        {
            if (OutputFrames == null || OutputFrames.Count == 0)
                return;

            if (Precision.AlmostEquals(frame.Time, lastTime))
                return;
            lastTime = frame.Time;

            // TODO: this is totally broken lol
            //bool samePos = frame.Position == lastPos;
            bool samePos = false;

            var lastFrame = (OsuReplayFrame)OutputFrames[^1];
            var midpoint = samePos ? lastMidpoint : (frame.Position + lastFrame.Position) / 2;
            double initialAngle = samePos ? lastInitialAngle : Math.Atan2(lastFrame.Position.Y - frame.Position.Y, lastFrame.Position.X - frame.Position.X);
            double radius = samePos ? lastRadius : Vector2.Distance(frame.Position, lastFrame.Position) / 2;
            for (double t = lastFrame.Time + FrameInterval; t < frame.Time; t += FrameInterval)
            {
                double p = (t - lastFrame.Time) / (frame.Time - lastFrame.Time);
                double offsetAngle = samePos ? p * Math.PI * 2 : p * Math.PI * 3;
                offsetAngle = Math.Abs(offsetAngle);

                double newAngle = initialAngle + offsetAngle;
                var offset = new Vector2((float)(Math.Cos(newAngle) * radius), (float)(Math.Sin(newAngle) * radius));
                var output = midpoint + offset;
                AddFrame(new OsuReplayFrame(t, output, frame.Actions.ToArray()));
            }
            lastPos = frame.Position;
            if (!samePos)
            {
                lastInitialAngle = initialAngle + Math.PI;
                lastMidpoint = midpoint;
            }
        }
    }
}
