// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;

namespace osu.Game.Rulesets.Osu.Replays.Postprocessors
{
    public abstract class ReplayPostprocessor
    {
        public OsuAuto2BGenerator? AutoGenerator;
        public List<OsuAuto2BGenerator.OsuReplayFrameWithReason>? Frames;

        public void Init(OsuAuto2BGenerator autoGenerator)
        {
            Init(null, autoGenerator);
        }

        public virtual void Init(List<OsuAuto2BGenerator.OsuReplayFrameWithReason>? frames, OsuAuto2BGenerator autoGenerator)
        {
            AutoGenerator = autoGenerator;
            Frames = frames;
        }

        public abstract void Update(OsuReplayFrame frame);
    }
}
