﻿namespace Estreya.BlishHUD.EventTable.UI.Views.Settings.Controls
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
        internal override Control CreateControl(BoxedValue<int> value, Func<bool> isEnabled, Func<int, bool> validationFunction, (float Min, float Max)? range, int width, int heigth, int x, int y)
        {
            TrackBar trackBar = new TrackBar()
            {
                Width = width,
                Location = new Point(x, y),
                Enabled = isEnabled?.Invoke() ?? true,
                Value = value?.Value ?? 50
            };

            trackBar.MinValue = range.HasValue ? range.Value.Min : 0;
            trackBar.MaxValue = range.HasValue ? range.Value.Max : 100;

            if (value != null)
            {
                trackBar.ValueChanged += (s, e) =>
                {
                    if (validationFunction?.Invoke((int)e.Value) ?? true)
                    {
                        value.Value = (int)e.Value;
                    }
                    else
                    {
                        trackBar.Value = value.Value;
                    }
                };
            }

            return trackBar;
        }
    }
}
