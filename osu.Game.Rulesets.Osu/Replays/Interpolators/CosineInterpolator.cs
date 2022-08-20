// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Interpolators
{
    // TODO: this can totally be replaced by OsuInterpolator with different easing types
    public class CosineInterpolator : ReplayInterpolator
    {
        public override void Update(OsuReplayFrame frame)
        {
            if (OutputFrames == null || OutputFrames.Count == 0)
                return;

            var lastFrame = (OsuReplayFrame)OutputFrames[^1];
            var pos1 = lastFrame.Position;
            var pos2 = frame.Position;

            for (double t = lastFrame.Time + FrameInterval; t < frame.Time; t += FrameInterval)
            {
                double p = (t - lastFrame.Time) / (frame.Time - lastFrame.Time);
                double p2 = (1 - Math.Cos(p * Math.PI)) / 2;
                var pos = new Vector2((float)(pos1.X * (1 - p2) + pos2.X * p2), (float)(pos1.Y * (1 - p2) + pos2.Y * p2));
                AddFrame(new OsuReplayFrame(t, pos, frame.Actions.ToArray()));
            }
        }
    }
}
