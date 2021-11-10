using System;
using System.Collections.Generic;
using System.Text;
using osu.Game.Rulesets.Replays;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Postprocessors
{
    public class PippiPostprocessor : ReplayPostprocessor
    {
        public void Init(OsuAuto2BGenerator autoGenerator)
        {
            Init(null, autoGenerator);
        }

        public override void Init(List<OsuAuto2BGenerator.OsuReplayFrameWithReason> frames, OsuAuto2BGenerator autoGenerator)
        {
            base.Init(frames, autoGenerator);
        }

        public override void Update(OsuReplayFrame frame)
        {
            double angle = frame.Time / 100.0;
            // originally, this was 0.98
            // but it needs to be more lenient because of slider stuff
            double addAngleAmount = AutoGenerator.CircleSize * 0.95;
            Vector2 pippiOffset = new Vector2((float)(Math.Cos(angle) * addAngleAmount), (float)(Math.Sin(angle) * addAngleAmount));

            frame.Position += pippiOffset;
        }
    }
}
