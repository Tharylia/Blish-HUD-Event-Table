namespace Estreya.BlishHUD.EventTable.UI.Views.Settings
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Estreya.BlishHUD.EventTable.Helpers;
    using Estreya.BlishHUD.EventTable.Resources;
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

        protected override void InternalBuild(Panel parent)
        {
            RenderSetting(parent, ModuleSettings.EventHeight);
            RenderSetting(parent, ModuleSettings.EventFontSize);
            RenderSetting(parent, ModuleSettings.EventTimeSpan);
            RenderSetting(parent, ModuleSettings.EventHistorySplit);
            RenderSetting(parent, ModuleSettings.DrawEventBorder);
            RenderSetting(parent,ModuleSettings.UseEventTranslation);
            RenderEmptyLine(parent);
            RenderSetting(parent, ModuleSettings.WorldbossCompletedAcion);
            RenderEmptyLine(parent);
            RenderSetting(parent, ModuleSettings.AutomaticallyUpdateEventFile);
            RenderButton(parent, Strings.EventSettingsView_UpdateEventFile_Title, async () =>
            {
                await EventTableModule.ModuleInstance.EventFileState.ExportFile();
                Controls.ScreenNotification.ShowNotification(Strings.EventSettingsView_UpdateEventFile_Success);
            }/*, () => !AsyncHelper.RunSync( EventTableModule.ModuleInstance.EventSettingsFileManager.IsNewEventFileVersionAvaiable)*/);
            RenderEmptyLine(parent);
            RenderButton(parent, Strings.EventSettingsView_ResetHiddenStates_Title, () =>
            {
                EventTableModule.ModuleInstance.HiddenState.Clear();
                Controls.ScreenNotification.ShowNotification(Strings.EventSettingsView_ResetHiddenStates_Success);
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
