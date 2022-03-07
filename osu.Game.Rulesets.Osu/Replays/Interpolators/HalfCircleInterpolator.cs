using System;
using System.Collections.Generic;
using System.Text;
using osu.Framework.Utils;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Interpolators
{
    public class HalfCircleInterpolator : ReplayInterpolator
    {
        private readonly bool brokenMode = true; // it looked cool
        private bool inverted = false;
        private double lastTime = double.NegativeInfinity;

        public override void Update(OsuReplayFrame frame)
        {
            if (OutputFrames.Count == 0)
                return;

            if (Precision.AlmostEquals(frame.Time, lastTime))
                return;
            lastTime = frame.Time;

            var lastFrame = (OsuReplayFrame)OutputFrames[^1];
            var midpoint = (frame.Position + lastFrame.Position) / 2;
            double initialAngle = Math.Atan2(lastFrame.Position.Y - frame.Position.Y, lastFrame.Position.X - frame.Position.X);
            double radius = Vector2.Distance(frame.Position, lastFrame.Position) / 2;
            if (!brokenMode)
                inverted = !inverted;
            for (double t = lastFrame.Time + FrameInterval; t < frame.Time; t += FrameInterval)
            {
                double p = (t - lastFrame.Time) / (frame.Time - lastFrame.Time);
                double offsetAngle = p * Math.PI;
                if (inverted)
                    offsetAngle = -Math.Abs(offsetAngle);
                else
                    offsetAngle = Math.Abs(offsetAngle);
                if (brokenMode)
                    inverted = !inverted;

                double newAngle = initialAngle + offsetAngle;
                var offset = new Vector2((float)(Math.Cos(newAngle) * radius), (float)(Math.Sin(newAngle) * radius));
                var output = midpoint + offset;
                addFrame(new OsuReplayFrame(t, output, frame.Actions.ToArray()));
            }
        }
    }
}
