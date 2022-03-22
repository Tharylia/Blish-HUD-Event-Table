namespace Estreya.BlishHUD.EventTable.UI.Views.Settings.Controls
{
    using Blish_HUD.Controls;
    using Blish_HUD.Input;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Extensions;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    internal class KeybindingProvider : ControlProvider<KeyBinding>
    {
        internal override Control CreateControl(SettingEntry<KeyBinding> settingEntry, Func<SettingEntry<KeyBinding>, KeyBinding, bool> validationFunction, int width, int heigth, int x, int y)
        {
            EventTable.Controls.KeybindingAssigner keybindingAssigner = new EventTable.Controls.KeybindingAssigner(settingEntry.Value, false)
            {
                Width = width,
                Location = new Point(x, y),
                Enabled = !settingEntry.IsDisabled()
            };

            if (settingEntry != null)
            {
                keybindingAssigner.BindingChanged += (s, e) => {
                    if (validationFunction?.Invoke(settingEntry, keybindingAssigner.KeyBinding) ?? false)
                    {
                        settingEntry.Value = keybindingAssigner.KeyBinding;
                    }
                    else
                    {
                        keybindingAssigner.KeyBinding = settingEntry.Value;
                    }
                };
            }

            return keybindingAssigner;
        }
    }
}
