// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Utils;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Postprocessors
{
    // like pippi but more precise
    public class CircleEdgePostprocessor : ReplayPostprocessor
    {
        private Vector2? lastPos;
        private Vector2? lastPosOffset;
        private double lastTime = double.NegativeInfinity;
        private bool inverted = false;

        public override void Update(OsuReplayFrame frame)
        {
            if (AutoGenerator == null)
                return;

            if (Precision.AlmostEquals(frame.Time, lastTime, 1))
            {
                frame.Position = lastPosOffset ?? Vector2.Zero;
                return;
            }

            lastTime = frame.Time;
            inverted = !inverted;

            double angle = Math.Atan2((lastPos?.Y ?? 0) - frame.Position.Y, (lastPos?.X ?? 0) - frame.Position.X) + Math.PI / 2.0;
            //angle = inverted ? -Math.Abs(angle) : Math.Abs(angle);
            double addAngleAmount = AutoGenerator.CircleSize / 2.0 * 0.95;
            Vector2 angleOffset = new Vector2((float)(Math.Cos(angle) * addAngleAmount), (float)(Math.Sin(angle) * addAngleAmount));

            lastPos = frame.Position;
            frame.Position += angleOffset;
            lastPosOffset = frame.Position;
        }
    }
}
