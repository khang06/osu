using System;
using System.Collections.Generic;
using System.Text;
using osu.Framework.Utils;
using osu.Game.Rulesets.Replays;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Interpolators
{
    internal class OsuInterpolator : ReplayInterpolator
    {
        public override void Update(OsuReplayFrame frame)
        {
            if (OutputFrames.Count == 0)
                return;

            var lastFrame = (OsuReplayFrame)OutputFrames[^1];
            for (double t = lastFrame.Time + FrameInterval; t < frame.Time; t += FrameInterval)
            {
                Vector2 output = Interpolation.ValueAt(t, lastFrame.Position, frame.Position, lastFrame.Time, frame.Time, Framework.Graphics.Easing.Out);
                addFrame(new OsuReplayFrame(t, output, frame.Actions.ToArray()));
            }
        }
    }
}
