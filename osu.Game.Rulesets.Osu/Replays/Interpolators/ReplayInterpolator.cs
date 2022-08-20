// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Rulesets.Osu.Replays.Postprocessors;
using osu.Game.Rulesets.Replays;

namespace osu.Game.Rulesets.Osu.Replays.Interpolators
{
    public abstract class ReplayInterpolator
    {
        public OsuAuto2BGenerator? AutoGenerator;
        public List<OsuAuto2BGenerator.OsuReplayFrameWithReason>? InputFrames;
        public List<ReplayFrame>? OutputFrames;

        public List<ReplayPostprocessor>? Postprocessors;

        public readonly double FrameInterval = 1000.0 / 144.0;

        public virtual void Init(List<OsuAuto2BGenerator.OsuReplayFrameWithReason> inputFrames, List<ReplayFrame> outputFrames, OsuAuto2BGenerator autoGenerator, params ReplayPostprocessor[] postprocessors)
        {
            AutoGenerator = autoGenerator;
            InputFrames = inputFrames;
            OutputFrames = outputFrames;
            Postprocessors = new List<ReplayPostprocessor>(postprocessors);

            foreach (var postprocessor in Postprocessors)
                postprocessor.Init(inputFrames, autoGenerator);
        }

        public abstract void Update(OsuReplayFrame frame);

        protected void AddFrame(OsuReplayFrame frame)
        {
            // TODO: should this add the frame in guaranteed sorted way?
            if (Postprocessors != null)
            {
                foreach (var postprocessor in Postprocessors)
                    postprocessor.Update(frame);
            }
            OutputFrames?.Add(frame);
        }
    }
}
