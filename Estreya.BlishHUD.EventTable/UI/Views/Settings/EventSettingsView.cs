namespace Estreya.BlishHUD.EventTable.UI.Views.Settings
{
    using Blish_HUD.Controls;
    using Estreya.BlishHUD.EventTable.Resources;
    using SemVer;
    using System;
    using System.Threading.Tasks;

    public class EventSettingsView : BaseSettingsView
    {
        private SemVer.Version CurrentVersion = null;
        private SemVer.Version NewestVersion = null;
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

            this.RenderSetting(parent, this.ModuleSettings.EventCompletedAcion);

            this.RenderEmptyLine(parent);

            this.RenderButton(parent, Strings.EventSettingsView_ReloadEvents_Title, async () =>
            {
                await EventTableModule.ModuleInstance.LoadEvents();
                EventTable.Controls.ScreenNotification.ShowNotification(Strings.EventSettingsView_ReloadEvents_Success);
            });

            this.RenderEmptyLine(parent);

            this.RenderSetting(parent, this.ModuleSettings.AutomaticallyUpdateEventFile);
            this.RenderButton(parent, Strings.EventSettingsView_UpdateEventFile_Title, async () =>
            {
                await EventTableModule.ModuleInstance.EventFileState.ExportFile();
                await EventTableModule.ModuleInstance.LoadEvents();
                EventTable.Controls.ScreenNotification.ShowNotification(Strings.EventSettingsView_UpdateEventFile_Success);
            });

            this.RenderLabel(parent, Strings.EventSettingsView_CurrentVersion_Title, this.CurrentVersion?.ToString() ?? Strings.EventSettingsView_CurrentVersion_Unknown);
            this.RenderLabel(parent, Strings.EventSettingsView_NewestVersion_Title, this.NewestVersion?.ToString() ?? Strings.EventSettingsView_NewestVersion_Unknown);

            this.RenderEmptyLine(parent);

            this.RenderButton(parent, Strings.EventSettingsView_ResetHiddenStates_Title, () =>
            {
                EventTableModule.ModuleInstance.HiddenState.Clear();
                EventTable.Controls.ScreenNotification.ShowNotification(Strings.EventSettingsView_ResetHiddenStates_Success);
            });

            this.RenderEmptyLine(parent);

            this.RenderSetting(parent, this.ModuleSettings.UseFiller);
            this.RenderSetting(parent, this.ModuleSettings.UseFillerEventNames);
            this.RenderColorSetting(parent, this.ModuleSettings.FillerTextColor);
        }

        protected override async Task<bool> InternalLoad(IProgress<string> progress)
        {
            this.CurrentVersion = (await EventTableModule.ModuleInstance.EventFileState.GetExternalFile())?.Version;
            this.NewestVersion = (await EventTableModule.ModuleInstance.EventFileState.GetInternalFile())?.Version;

            return true;
        }
    }
}
