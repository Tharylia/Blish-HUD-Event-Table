namespace Estreya.BlishHUD.EventTable.UI.Views.Settings
{
    using Blish_HUD.Controls;
    using Estreya.BlishHUD.EventTable.Helpers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class EventSettingsView : BaseSettingsView
    {
        public EventSettingsView(ModuleSettings settings) : base(settings)
        {
        }

        protected override void InternalBuild(FlowPanel parent)
        {
            RenderSetting(parent, ModuleSettings.EventHeight);
            RenderSetting(parent, ModuleSettings.EventFontSize);
            RenderSetting(parent, ModuleSettings.EventTimeSpan);
            RenderSetting(parent, ModuleSettings.EventHistorySplit);
            RenderSetting(parent, ModuleSettings.DrawEventBorder);
            RenderEmptyLine(parent);
            RenderSetting(parent, ModuleSettings.WorldbossCompletedAcion);
            RenderEmptyLine(parent);
            RenderSetting(parent, ModuleSettings.AutomaticallyUpdateEventFile);
            RenderButton(parent, "Update Event File", () =>
            {
                AsyncHelper.RunSync(EventTableModule.ModuleInstance.EventFileState.ExportFile);
                ScreenNotification.ShowNotification("Successfully updated!");
            }/*, () => !AsyncHelper.RunSync( EventTableModule.ModuleInstance.EventSettingsFileManager.IsNewEventFileVersionAvaiable)*/);
            RenderEmptyLine(parent);
            RenderButton(parent, "Reset hidden states", () =>
            {
                EventTableModule.ModuleInstance.HiddenState.Clear();
            });
            RenderEmptyLine(parent);
            RenderSetting(parent, ModuleSettings.UseFiller);
            RenderSetting(parent, ModuleSettings.UseFillerEventNames);
            RenderColorSetting(parent, ModuleSettings.FillerTextColor);
        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }
    }
}
