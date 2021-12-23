using System;
using System.Collections.Generic;
using System.Text;
using osu.Game.Rulesets.Replays;

namespace osu.Game.Rulesets.Osu.Replays.Postprocessors
{
    public abstract class ReplayPostprocessor
    {
        public OsuAuto2BGenerator AutoGenerator;
        public List<OsuAuto2BGenerator.OsuReplayFrameWithReason> Frames;

        public void Init(OsuAuto2BGenerator autoGenerator)
        {
            Init(null, autoGenerator);
        }

        public virtual void Init(List<OsuAuto2BGenerator.OsuReplayFrameWithReason> frames, OsuAuto2BGenerator autoGenerator)
        {
            AutoGenerator = autoGenerator;
            Frames = frames;
        }

        public abstract void Update(OsuReplayFrame frame);
    }
}
