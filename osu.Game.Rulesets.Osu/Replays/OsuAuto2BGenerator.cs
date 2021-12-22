using System;
using System.Collections.Generic;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Replays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Replays.Interpolators;
using osu.Game.Rulesets.Osu.Replays.Postprocessors;
using osuTK;

namespace osu.Game.Rulesets.Osu.Replays
{
    public class OsuAuto2BGenerator : OsuAutoGeneratorBase
    {
        public new OsuBeatmap Beatmap => (OsuBeatmap)base.Beatmap;

        private List<OsuReplayFrameWithReason> framesWithReason = new List<OsuReplayFrameWithReason>();

        // TODO: make this configurable
        private readonly ReplayInterpolator interpolator = new AutoPlusCubicSplineInterpolator();

        // TODO: doesn't handle dynamic circle size...
        public double CircleSize => Beatmap.HitObjects[0].Radius * 2;

        public OsuAuto2BGenerator(IBeatmap beatmap, IReadOnlyList<Mod> mods)
            : base(beatmap, mods)
        {
            // empty...
        }

        public override Replay Generate()
        {
            var spinTimes = new List<SpinnerTime>();

            addFrameWithReason(new OsuKeyUpReplayFrame(Beatmap.HitObjects[0].StartTime - 3000, new Vector2(256, 192)), FrameReason.HitCircle);

            List<TickTime> tickTimes = new List<TickTime>();

            void addCircleClick(OsuHitObject h)
            {
                double startTime = h.StartTime;

                // this is a workaround for nested hit object handling being inconsistent based on framerate
                if (tickTimes.Count > 0)
                {
                    int index = tickTimes.BinarySearch(new TickTime(startTime));
                    if (index < 0) index = ~index;
                    index = Math.Min(index, tickTimes.Count - 1);

                    // TODO: this sucks so much
                    Vector2? tickPos = null;
                    if (Precision.AlmostEquals(startTime, tickTimes[index].Time, 1))
                        tickPos = tickTimes[index].Position;
                    else if (index != 0 && Precision.AlmostEquals(startTime, tickTimes[index - 1].Time, 1))
                        tickPos = tickTimes[index - 1].Position;

                    if (tickPos != null && Vector2.Distance(h.StackedPosition, tickPos.Value) > CircleSize)
                        startTime += 1;
                }

                addFrameWithReason(new OsuKeyUpReplayFrame(startTime, h.StackedPosition), FrameReason.HitCircle);
                addFrameWithReason(new OsuReplayFrame(startTime, h.StackedPosition, OsuAction.LeftButton), FrameReason.HitCircle);
            }

            foreach (var o in Beatmap.HitObjects)
            {
                // TODO: stop holding the key down if it really doesn't have to
                switch (o)
                {
                    case HitCircle h:
                    {
                        addCircleClick(h);
                        break;
                    }

                    case Slider s:
                    {
                        // hitting nested hit objects on sliders seems to be inconsistent on normal lazer
                        addCircleClick(s);
                        double sliderLength = s.EndTime - s.StartTime;

                        // it's impossible to hit nested slider objects if the slider ends instantly, so don't even try
                        if (sliderLength > 1)
                        {
                            foreach (OsuHitObject n in s.NestedHitObjects)
                            {
                                if (n is SliderHeadCircle)
                                    continue;

                                var reason = n is SliderTick || n is SliderRepeat ? FrameReason.SliderTick : FrameReason.SliderEnd;
                                double tickTime = n.StartTime;
                                var posAtTime = s.StackedPositionAt(Math.Floor(tickTime - s.StartTime) / sliderLength);

                                // HACK: super shitty hack to fc xnor xnor xnor, i have no idea why i need this
                                addFrameWithReason(new OsuReplayFrame(tickTime - 1, posAtTime, OsuAction.LeftButton), reason);
                                addFrameWithReason(new OsuReplayFrame(tickTime, posAtTime, OsuAction.LeftButton), reason);

                                if (reason == FrameReason.SliderTick)
                                {
                                    int index = tickTimes.BinarySearch(new TickTime(tickTime));
                                    if (index < 0) index = ~index;
                                    tickTimes.Insert(index, new TickTime(tickTime));
                                }
                                //AddFrameToReplay(new OsuKeyUpReplayFrame(n.StartTime, posAtTime));
                            }
                        }

                        //AddFrameToReplay(new OsuReplayFrame(s.EndTime, s.StackedEndPosition, OsuAction.LeftButton));
                        break;
                    }

                    case Spinner s:
                    {
                        if (s.SpinsRequired < 1)
                            break;

                        spinTimes.Add(new SpinnerTime
                        {
                            StartTime = s.StartTime,
                            EndTime = s.EndTime
                        });
                        break;
                    }
                }
            }

            mergeSpinnerTimes(spinTimes);

            foreach (var s in spinTimes)
            {
                for (double t = s.StartTime; t < s.EndTime; t += 1000.0 / 60.0)
                    addFrameWithReason(new OsuReplayFrame(t, CirclePosition(t / 20, SPIN_RADIUS) + SPINNER_CENTRE, OsuAction.LeftButton), FrameReason.Spinner);
            }

            double lastEndTime = Beatmap.HitObjects[^1] is IHasDuration obj ? obj.EndTime : Beatmap.HitObjects[^1].StartTime;
            addFrameWithReason(new OsuKeyUpReplayFrame(lastEndTime + 3000, new Vector2(256, 192)), FrameReason.HitCircle);

            return postprocessFrames();
        }

        private void combineOverlappingObjects()
        {
            var newFrames = new List<OsuReplayFrameWithReason>();
            var queuedFrames = new List<OsuReplayFrameWithReason>();

            void processQueuedFrames(FrameReason reason)
            {
                if (queuedFrames.Count != 0)
                {
                    // time or reason changed, time to process the queue!
                    // TODO: can i do this better than O(n^2) worst case???
                    bool overlapping = true;

                    foreach (var x in queuedFrames)
                    {
                        foreach (var y in queuedFrames)
                        {
                            if (Vector2.Distance(x.Position, y.Position) > CircleSize)
                            {
                                overlapping = false;
                                break;
                            }
                        }

                        if (!overlapping)
                            break;
                    }

                    if (overlapping)
                    {
                        // find the middle between all points
                        // TODO: is this actually where they all overlap???
                        double midX = 0;
                        double midY = 0;

                        foreach (var x in queuedFrames)
                        {
                            midX += x.Position.X;
                            midY += x.Position.Y;
                        }

                        Vector2 middle = new Vector2((float)(midX / queuedFrames.Count), (float)(midY / queuedFrames.Count));

                        // append to new frames
                        foreach (var x in queuedFrames)
                        {
                            newFrames.Add(new OsuReplayFrameWithReason(new OsuReplayFrame(x.Time, middle, x.Actions.ToArray()), x.Reason));
                        }
                    }
                    else
                    {
                        // just do it as-is if it's for hitcircles
                        // otherwise, just take the position of the first one to avoid unnecessary effort
                        // TODO: try and find the point where the most circles overlap
                        if (reason == FrameReason.HitCircle)
                        {
                            foreach (var x in queuedFrames)
                            {
                                newFrames.Add(x);
                            }
                        }
                        else
                        {
                            newFrames.Add(queuedFrames[0]);
                        }
                    }

                    queuedFrames.Clear();
                }
            }

            double lastTime = double.NegativeInfinity;
            FrameReason? lastReason = null;

            foreach (var f in framesWithReason)
            {
                if (!Precision.AlmostEquals(lastTime, f.Time) || lastReason != f.Reason)
                {
                    if (lastReason.HasValue)
                        processQueuedFrames(lastReason.Value);
                    lastTime = f.Time;
                    lastReason = f.Reason;
                }

                queuedFrames.Add(f);
            }

            if (lastReason.HasValue)
                processQueuedFrames(lastReason.Value);
            framesWithReason = newFrames;
        }

        private Replay postprocessFrames()
        {
            // will not work with pippi mode on!
            combineOverlappingObjects();

            // TODO: make this configurable
            interpolator.Init(framesWithReason, Frames, this);

            //double lastSliderEndTime = double.NegativeInfinity;

            foreach (var f in framesWithReason)
            {
                /*
                if (f.Reason == FrameReason.SliderEnd)
                {
                    if (Precision.AlmostEquals(lastSliderEndTime, f.Time))
                    {
                        lastSliderEndTime = f.Time;
                        continue; // it's not possible to hit multiple slider ends at once if their radii don't overlap
                        // TODO: that really needs to be handled better in the future
                    }

                    lastSliderEndTime = f.Time;
                }
                */

                interpolator.Update(f);

                Frames.Add(f);
            }

            // TODO: abstract this better
            /*
            var pippi = new PippiPostprocessor();
            pippi.Init(this);
            foreach (var f in Frames)
            {
                pippi.Update((OsuReplayFrame)f);
            }
            */

            return Replay;
        }

        private void mergeSpinnerTimes(List<SpinnerTime> spinnerTimes)
        {
            if (spinnerTimes.Count == 0)
                return;

            int max = 0;

            for (int i = 0; i < spinnerTimes.Count; i++)
            {
                if (spinnerTimes[max].EndTime >= spinnerTimes[i].StartTime)
                {
                    var time = new SpinnerTime
                    {
                        StartTime = Math.Min(spinnerTimes[max].StartTime, spinnerTimes[i].StartTime),
                        EndTime = Math.Max(spinnerTimes[max].EndTime, spinnerTimes[i].EndTime)
                    };
                    spinnerTimes[max] = time;
                }
                else
                {
                    max++;
                    spinnerTimes[max] = spinnerTimes[i];
                }
            }

            max++;

            spinnerTimes.RemoveRange(max, spinnerTimes.Count - max);
        }

        private void addFrameWithReason(OsuReplayFrame frame, FrameReason reason)
        {
            // TODO: does this actually help?
            frame.Time = Math.Floor(frame.Time);
            var frameWithReason = new OsuReplayFrameWithReason(frame, reason);
            framesWithReason.Insert(findInsertionIndexWithReason(frameWithReason), frameWithReason);
        }

        private static readonly IComparer<OsuReplayFrameWithReason> replay_frame_comparer = new OsuReplayFrameWithReasonComparer();

        private int findInsertionIndexWithReason(OsuReplayFrameWithReason frame)
        {
            int index = framesWithReason.BinarySearch(frame, replay_frame_comparer);

            if (index < 0)
            {
                index = ~index;
            }
            else
            {
                // Go to the first index which is actually bigger
                while (index < framesWithReason.Count && frame.Time == framesWithReason[index].Time)
                {
                    ++index;
                }
            }

            return index;
        }

        private class OsuReplayFrameWithReasonComparer : IComparer<OsuReplayFrameWithReason>
        {
            public int Compare(OsuReplayFrameWithReason f1, OsuReplayFrameWithReason f2)
            {
                int cmp = f1.Time.CompareTo(f2.Time);

                if (cmp != 0 && !Precision.AlmostEquals(f1.Time, f2.Time))
                {
                    return cmp;
                }

                return f1.Reason.CompareTo(f2.Reason);
            }
        }

        // Ordered by how they should be in the replay
        // It seems like for slider judgements, the first frame of a group of frames that happen at the same time is the only one that matters
        // Basically, this is ordered by priority
        public enum FrameReason
        {
            SliderTick, // Combo break if not handled
            SliderEnd, // One less combo if not handled
            HitCircle, // Can be handled regardless of frame order
            Spinner, // Who cares lol
        }

        private struct SpinnerTime
        {
            public double StartTime;
            public double EndTime;
        }

        private class TickTime : IComparable<TickTime>
        {
            public TickTime(double time)
                : this(time, Vector2.Zero)
            {
            }

            public TickTime(double time, Vector2 pos)
            {
                Time = time;
                Position = pos;
            }

            public int CompareTo(TickTime other)
            {
                return Time.CompareTo(other.Time);
            }

            public readonly double Time;
            public readonly Vector2 Position;
        }

        public class OsuReplayFrameWithReason : OsuReplayFrame
        {
            public OsuReplayFrameWithReason(OsuReplayFrame frame, FrameReason reason)
                : base(frame.Time, frame.Position, frame.Actions.ToArray())
            {
                Reason = reason;
            }

            public FrameReason Reason;
        }

        private class OsuKeyUpReplayFrame : OsuReplayFrame
        {
            public OsuKeyUpReplayFrame(double time, Vector2 position)
                : base(time, position)
            {
            }
        }
    }
}
