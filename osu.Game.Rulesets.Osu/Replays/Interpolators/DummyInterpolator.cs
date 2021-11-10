using System;
using System.Collections.Generic;
using System.Text;
using osu.Game.Rulesets.Replays;

namespace osu.Game.Rulesets.Osu.Replays.Interpolators
{
    public class DummyInterpolator : ReplayInterpolator
    {
        public override void Update(OsuReplayFrame frame)
        {
            // does nothing
        }
    }
}
