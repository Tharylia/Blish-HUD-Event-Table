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
        internal override Control CreateControl(SettingEntry<string> settingEntry, Func<SettingEntry<string>, string, bool> validationFunction, int width, int heigth, int x, int y)
        {
            TextBox textBox = new TextBox()
            {
                Width = width,
                Location = new Point(x,y),
                Text = settingEntry?.Value ?? string.Empty,
                Enabled = !settingEntry.IsDisabled()
            };

            if (settingEntry != null)
            {
                textBox.TextChanged += (s, e) => {
                    ValueChangedEventArgs<string> eventArgs = (ValueChangedEventArgs<string>)e;
                    if (validationFunction?.Invoke(settingEntry, eventArgs.NewValue) ?? false)
                    {
                        settingEntry.Value = eventArgs.NewValue;
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
