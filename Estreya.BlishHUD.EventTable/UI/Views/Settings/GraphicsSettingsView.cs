namespace Estreya.BlishHUD.EventTable.UI.Views.Settings
{
    using Blish_HUD.Controls;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class GraphicsSettingsView : BaseSettingsView
    {
        public GraphicsSettingsView(ModuleSettings settings) : base(settings)
        {
        }

        protected override void InternalBuild(Panel parent)
        {
            RenderSetting(parent, ModuleSettings.LocationX);
            RenderSetting(parent, ModuleSettings.LocationY);
            RenderSetting(parent, ModuleSettings.Width);
            RenderEmptyLine(parent);
            RenderSetting(parent, ModuleSettings.Opacity);
            RenderEmptyLine(parent);
            RenderSetting(parent, ModuleSettings.BackgroundColorOpacity);
            RenderColorSetting(parent, ModuleSettings.BackgroundColor);
        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }
    }
}
