// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Postprocessors
{
    public class PippiPostprocessor : ReplayPostprocessor
    {
        public override void Init(List<OsuAuto2BGenerator.OsuReplayFrameWithReason>? frames, OsuAuto2BGenerator autoGenerator)
        {
            base.Init(frames, autoGenerator);
        }

        public override void Update(OsuReplayFrame frame)
        {
            if (AutoGenerator == null)
                return;

            double angle = frame.Time / 100.0;
            // originally, this was 0.98
            // but it needs to be more lenient because of slider stuff
            double addAngleAmount = AutoGenerator.CircleSize / 2.0 * 0.95;
            Vector2 pippiOffset = new Vector2((float)(Math.Cos(angle) * addAngleAmount), (float)(Math.Sin(angle) * addAngleAmount));

            frame.Position += pippiOffset;
        }
    }
}
