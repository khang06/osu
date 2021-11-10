using System;
using System.Collections.Generic;
using System.Text;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays.Postprocessors
{
    public class BounceOffEdgesPostprocessor : ReplayPostprocessor
    {
        public override void Update(OsuReplayFrame frame)
        {
            // port from danser
            var p = frame.Position;
            int minX = -170;
            int minY = -47;
            int maxX = 682;
            int maxY = 432;

            // algorithm from danser
            p.X -= minX;
            p.Y -= minY;
            p.X %= 2 * (maxX - minX);
            p.Y %= 2 * (maxY - minY);
            p.X += minX;
            p.Y += minY;

            while (true)
            {
                bool ok1 = false;
                bool ok2 = false;

                if (p.X < minX)
                    p.X = 2 * minX - p.X;
                else if (p.X > maxX)
                    p.X = maxX * 2 - p.X;
                else
                    ok1 = true;

                if (p.Y < minY)
                    p.Y = 2 * minY - p.Y;
                else if (p.Y > maxY)
                    p.Y = maxY * 2 - p.Y;
                else
                    ok2 = true;

                if (ok1 && ok2)
                    break;
            }

            frame.Position = p;
        }
    }
}
