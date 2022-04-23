namespace Estreya.BlishHUD.EventTable.UI.Views.Settings.Controls
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Extensions;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class TextBoxProvider : ControlProvider<string>
    {
        internal override Control CreateControl(BoxedValue<string> value, Func<bool> isEnabled, Func<string, bool> validationFunction, (float Min, float Max)? range, int width, int heigth, int x, int y)
        {
            TextBox textBox = new TextBox()
            {
                Width = width,
                Location = new Point(x, y),
                Text = value?.Value ?? string.Empty,
                Enabled = isEnabled?.Invoke() ?? true
            };

            if (value != null)
            {
                textBox.TextChanged += (s, e) =>
                {
                    ValueChangedEventArgs<string> eventArgs = (ValueChangedEventArgs<string>)e;

                    bool rangeValid = true;

                    if (range != null)
                    {
                        if (eventArgs.NewValue.Length < range.Value.Min || eventArgs.NewValue.Length > range.Value.Max)
                        {
                            rangeValid = false;
                        }
                    }

                    if (rangeValid && (validationFunction?.Invoke(eventArgs.NewValue) ?? true))
                    {
                        value.Value = eventArgs.NewValue;
                    }
                    else
                    {
                        textBox.Text = eventArgs.PreviousValue;
                    }
                };
            }

            return textBox;
        }
    }
}
