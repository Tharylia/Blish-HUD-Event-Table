namespace Estreya.BlishHUD.EventTable.UI.Views.Settings
{
    using Blish_HUD.Controls;
    using System;
    using System.Threading.Tasks;

    public class GeneralSettingsView : BaseSettingsView
    {
        public GeneralSettingsView(ModuleSettings settings) : base(settings)
        {
        }

        protected override void InternalBuild(Panel parent)
        {
            this.RenderSetting(parent, this.ModuleSettings.GlobalEnabled);
            this.RenderSetting(parent, this.ModuleSettings.GlobalEnabledHotkey);
#if DEBUG
            this.RenderSetting(parent, this.ModuleSettings.DebugEnabled);
#endif
            this.RenderSetting(parent, this.ModuleSettings.RegisterCornerIcon);
            this.RenderEmptyLine(parent);
            this.RenderSetting(parent, this.ModuleSettings.HideOnOpenMap);
            this.RenderSetting(parent, this.ModuleSettings.HideOnMissingMumbleTicks);
            this.RenderSetting(parent, this.ModuleSettings.HideInCombat);
            this.RenderSetting(parent, this.ModuleSettings.ShowTooltips);
            this.RenderSetting(parent, this.ModuleSettings.TooltipTimeMode);
            this.RenderSetting(parent, this.ModuleSettings.CopyWaypointOnClick);
            this.RenderSetting(parent, this.ModuleSettings.ShowContextMenuOnClick);
            this.RenderSetting(parent, this.ModuleSettings.BuildDirection);
        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }
    }
}
