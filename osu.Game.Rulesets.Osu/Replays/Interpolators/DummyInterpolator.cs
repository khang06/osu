// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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
