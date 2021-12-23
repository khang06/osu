// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Replays;

namespace osu.Game.Rulesets.Osu.Mods
{
    public class OsuModAutoplay : ModAutoplay
    {
        public override Type[] IncompatibleMods => base.IncompatibleMods.Concat(new[] { typeof(OsuModMagnetised), typeof(OsuModRepel), typeof(OsuModAutopilot), typeof(OsuModSpunOut), typeof(OsuModAlternate), typeof(OsuModSingleTap) }).ToArray();

        public override ModReplayData CreateReplayData(IBeatmap beatmap, IReadOnlyList<Mod> mods)
            => new ModReplayData(new OsuAuto2BGenerator(beatmap, mods, Interpolator.Value, Pippi.Value).Generate(), new ModCreatedUser { Username = "Autoplay" });

        [SettingSource("Interpolator", "Change the movement style of the generated replay.", 1)]
        public Bindable<InterpolationStyle> Interpolator { get; } = new Bindable<InterpolationStyle>(InterpolationStyle.CubicSplineAutoPlus);

        [SettingSource("Pippi", "Hit circles on their edges. Not recommended for 2B/Aspire maps.", 2)]
        public Bindable<bool> Pippi { get; } = new Bindable<bool>();

        private void use2BChanged(ValueChangedEvent<bool> changedEvent)
        {
            bool enabled = changedEvent.NewValue;
            Interpolator.Disabled = !enabled;
        }

        public OsuModAutoplay()
        {
            Use2B?.BindValueChanged(use2BChanged);
        }

        public enum InterpolationStyle
        {
            CatmullRom,
            Cosine,
            CubicSplineAutoPlus,
            CubicSplineDanser,
            Dummy,
            FullCircle,
            HalfCircle,
            Osu,
        }
    }
}
