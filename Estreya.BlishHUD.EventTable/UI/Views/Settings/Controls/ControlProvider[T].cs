namespace Estreya.BlishHUD.EventTable.UI.Views.Settings.Controls
{
    using Blish_HUD.Controls;
    using Blish_HUD.Settings;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public abstract class ControlProvider<T> : ControlProvider
    {

        public Type Type { get; }

        internal ControlProvider()
        {
            this.Type = typeof(T);
        }

        internal abstract Control CreateControl(SettingEntry<T> settingEntry, Func<SettingEntry<T>, T, bool> validationFunction, int width, int heigth, int x, int y);
    }
}
