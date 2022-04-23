namespace Estreya.BlishHUD.EventTable.UI.Views.Settings.Controls
{
    using Blish_HUD.Controls;
    using Blish_HUD.Settings;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public abstract class ControlProvider<TValue> : ControlProvider
    {

        public Type Type { get; }

        internal ControlProvider()
        {
            this.Type = typeof(TValue);
        }

        internal abstract Control CreateControl(BoxedValue<TValue> value, Func<bool> isEnabled, Func<TValue, bool> validationFunction, (float Min, float Max)? range, int width, int heigth, int x, int y);
    }
}
