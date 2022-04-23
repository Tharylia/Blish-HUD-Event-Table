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
        internal override Control CreateControl(BoxedValue<T> value, Func<bool> enabledFunction, Func<T, bool> validationFunction, (float Min, float Max)? range, int width, int heigth, int x, int y)
        {
            Dropdown dropdown = new Dropdown
            {
                Width = width,
                Location = new Point(x, y),
                SelectedItem = value?.Value.ToString(),
                Enabled = enabledFunction?.Invoke() ?? true
            };

            foreach (string enumValue in Enum.GetNames(typeof(T)))
            {
                dropdown.Items.Add(enumValue);
            }

            if (value != null)
            {
                bool resetingValue = false;
                dropdown.ValueChanged += (s, e) =>
                {
                    if (resetingValue) return;

                    var newValue = (T)Enum.Parse(typeof(T), e.CurrentValue);
                    if (validationFunction?.Invoke(newValue) ?? true)
                    {
                        value.Value = newValue;
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
