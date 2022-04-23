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

    internal class CheckboxProvider : ControlProvider<bool>
    {
        internal override Control CreateControl(BoxedValue<bool> value, Func<bool> isEnabled, Func<bool, bool> validationFunction, (float Min, float Max)? range, int width, int heigth, int x, int y)
        {
            Checkbox checkbox = new Checkbox()
            {
                Width = width,
                Location = new Point(x, y),
                Checked = value?.Value ?? false,
                Enabled = isEnabled?.Invoke() ?? true
            };

            if (value != null)
            {
                checkbox.CheckedChanged += (s, e) =>
                {
                    if (validationFunction?.Invoke(e.Checked) ?? true)
                    {
                        value.Value = e.Checked;
                    }
                    else
                    {
                        checkbox.Checked = !e.Checked;
                    }
                };
            }

            return checkbox;
        }
    }
}
