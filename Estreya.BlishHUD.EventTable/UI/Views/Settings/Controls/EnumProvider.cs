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

    internal class EnumProvider<T> : ControlProvider<T> where T : Enum
    {
        internal override Control CreateControl(SettingEntry<T> settingEntry, Func<SettingEntry<T>, T, bool> validationFunction, int width, int heigth, int x, int y)
        {
            Dropdown dropdown = new Dropdown
            {
                Width = width,
                Location = new Point(x, y),
                SelectedItem = settingEntry?.Value.ToString(),
                Enabled = !settingEntry.IsDisabled()
            };

            foreach (string enumValue in Enum.GetNames(settingEntry.SettingType))
            {
                dropdown.Items.Add(enumValue);
            }

            if (settingEntry != null)
            {
                bool resetingValue = false;
                dropdown.ValueChanged += (s, e) =>
                {
                    if (resetingValue) return;

                    var newValue = (T)Enum.Parse(settingEntry.SettingType, e.CurrentValue);
                    if (validationFunction?.Invoke(settingEntry, newValue) ?? false)
                    {
                        settingEntry.Value = newValue;
                    }
                    else
                    {
                        resetingValue = true;
                        dropdown.SelectedItem = e.PreviousValue;
                        resetingValue = false;
                    }
                };
            }

            return dropdown;
        }
    }
}
