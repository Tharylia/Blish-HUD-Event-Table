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
        internal override Control CreateControl(SettingEntry<bool> settingEntry, Func<SettingEntry<bool>, bool, bool> validationFunction, int width, int heigth, int x, int y)
        {
            Checkbox checkbox = new Checkbox()
            {
                Width = width,
                Location = new Point(x, y),
                Checked = settingEntry?.Value ?? false,
                Enabled = !settingEntry.IsDisabled()
            };

            if (settingEntry != null)
            {
                checkbox.CheckedChanged += (s, e) => {
                    if (validationFunction?.Invoke(settingEntry, e.Checked) ?? false)
                    {
                        settingEntry.Value = e.Checked;
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
