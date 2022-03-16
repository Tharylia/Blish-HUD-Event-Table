namespace Estreya.BlishHUD.EventTable.UI.Views.Settings.Controls
{
    using Blish_HUD.Controls;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Extensions;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class IntTrackBarProvider : ControlProvider<int>
    {
        internal override Control CreateControl(SettingEntry<int> settingEntry, Func<SettingEntry<int>, int, bool> validationFunction, int width, int heigth, int x, int y)
        {
            (int Min, int Max)? range = settingEntry?.GetRange() ?? null;
            TrackBar trackBar = new TrackBar()
            {
                Width = width,
                Location = new Point(x,y),
                Enabled = !settingEntry.IsDisabled(),
                MinValue = range.HasValue ? range.Value.Min : 0,
                MaxValue = range.HasValue ? range.Value.Max : 100,
                Value = settingEntry?.GetValue() ?? 50
            };

            if (settingEntry != null)
            {
                trackBar.ValueChanged += (s, e) => {
                    if (validationFunction?.Invoke(settingEntry, (int)e.Value) ?? false)
                    {
                        settingEntry.Value = (int)e.Value;
                    }
                    else
                    {
                        trackBar.Value = settingEntry.Value;
                    }
                };
            }

            return trackBar;
        }
    }
}
