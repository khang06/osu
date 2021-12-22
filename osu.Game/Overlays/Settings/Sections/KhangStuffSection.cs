// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Game.Configuration;

namespace osu.Game.Overlays.Settings.Sections
{
    public class KhangStuffSection : SettingsSection
    {
        public override Drawable CreateIcon() => new SpriteIcon { Icon = FontAwesome.Solid.Wheelchair };
        public override LocalisableString Header => new LocalisableString("Khang Stuff");

        [BackgroundDependencyLoader(true)]
        private void load(Storage storage, OsuConfigManager config, OsuGame game)
        {
            Add(new SettingsCheckbox
            {
                LabelText = new LocalisableString("Rainbow cursor trail"),
                Current = config.GetBindable<bool>(OsuSetting.RainbowCursorTrail)
            });
        }
    }
}
