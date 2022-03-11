namespace Estreya.BlishHUD.EventTable.UI.Views.Settings
{
    using Blish_HUD.Controls;
    using Estreya.BlishHUD.EventTable.Resources;
    using System;
    using System.Threading.Tasks;

    public class EventSettingsView : BaseSettingsView
    {
        public EventSettingsView(ModuleSettings settings) : base(settings)
        {
        }

        protected override void InternalBuild(Panel parent)
        {
            this.RenderSetting(parent, this.ModuleSettings.EventHeight);
            this.RenderSetting(parent, this.ModuleSettings.EventFontSize);
            this.RenderSetting(parent, this.ModuleSettings.EventTimeSpan);
            this.RenderSetting(parent, this.ModuleSettings.EventHistorySplit);
            this.RenderSetting(parent, this.ModuleSettings.DrawEventBorder);
            this.RenderSetting(parent, this.ModuleSettings.UseEventTranslation);
            this.RenderEmptyLine(parent);
            this.RenderSetting(parent, this.ModuleSettings.WorldbossCompletedAcion);
            this.RenderEmptyLine(parent);
            this.RenderSetting(parent, this.ModuleSettings.AutomaticallyUpdateEventFile);
            this.RenderButton(parent, Strings.EventSettingsView_UpdateEventFile_Title, async () =>
            {
                await EventTableModule.ModuleInstance.EventFileState.ExportFile();
                Controls.ScreenNotification.ShowNotification(Strings.EventSettingsView_UpdateEventFile_Success);
            }/*, () => !AsyncHelper.RunSync( EventTableModule.ModuleInstance.EventSettingsFileManager.IsNewEventFileVersionAvaiable)*/);
            this.RenderEmptyLine(parent);
            this.RenderButton(parent, Strings.EventSettingsView_ResetHiddenStates_Title, () =>
            {
                EventTableModule.ModuleInstance.HiddenState.Clear();
                Controls.ScreenNotification.ShowNotification(Strings.EventSettingsView_ResetHiddenStates_Success);
            });
            this.RenderEmptyLine(parent);
            this.RenderSetting(parent, this.ModuleSettings.UseFiller);
            this.RenderSetting(parent, this.ModuleSettings.UseFillerEventNames);
            this.RenderColorSetting(parent, this.ModuleSettings.FillerTextColor);
        }

        protected override Task<bool> InternalLoad(IProgress<string> progress)
        {
            return Task.FromResult(true);
        }
    }
}
