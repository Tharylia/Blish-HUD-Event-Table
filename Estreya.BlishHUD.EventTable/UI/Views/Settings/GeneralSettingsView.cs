namespace Estreya.BlishHUD.EventTable.UI.Views.Settings
{
    using Blish_HUD.Controls;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class GeneralSettingsView : BaseSettingsView
    {
        public GeneralSettingsView(ModuleSettings settings) : base(settings)
        {
        }

        protected override void InternalBuild(FlowPanel parent)
        {
            RenderSetting(parent, ModuleSettings.GlobalEnabled);
            RenderSetting(parent, ModuleSettings.GlobalEnabledHotkey);
#if DEBUG
            RenderSetting(parent, ModuleSettings.DebugEnabled);

#endif
            RenderSetting(parent, ModuleSettings.RegisterCornerIcon);
            RenderEmptyLine(parent);
            RenderSetting(parent, ModuleSettings.HideOnMissingMumbleTicks);
            RenderSetting(parent, ModuleSettings.HideInCombat);
            RenderSetting(parent, ModuleSettings.ShowTooltips);
            RenderSetting(parent, ModuleSettings.TooltipTimeMode);
            RenderSetting(parent, ModuleSettings.CopyWaypointOnClick);
            RenderSetting(parent, ModuleSettings.ShowContextMenuOnClick);
            RenderSetting(parent, ModuleSettings.BuildDirection);
        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }
    }
}
