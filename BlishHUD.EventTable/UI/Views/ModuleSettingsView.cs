namespace BlishHUD.EventTable.UI.Views
{
    using Blish_HUD.Controls;
    using Microsoft.Xna.Framework;
    using Blish_HUD.Settings.UI.Views;
    using Blish_HUD.Graphics.UI;
    using Blish_HUD.Input;
    using Blish_HUD.Settings;

    public class ModuleSettingsView : View
    {
        private ModuleSettings ModuleSettings {  get; set; }

        public ModuleSettingsView(ModuleSettings settings)
        {
            this.ModuleSettings = settings;
        }

        protected override void Build(Container buildPanel)
        {
            Rectangle bounds = buildPanel.ContentRegion;

            var parentPanel = new FlowPanel()
            {
                Size = buildPanel.Size,
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                ControlPadding = new Vector2(5, 2),
                OuterControlPadding = new Vector2(10, 15),
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.AutoSize,
                AutoSizePadding = new Point(0, 15),
                Parent = buildPanel
            };

            RenderCheckBox(parentPanel, ModuleSettings.GlobalEnabled, true);
        }

        private void RenderCheckBox(Panel parent, SettingEntry setting, bool inline)
        {
            var settingView = SettingView.FromType(setting, parent.Width);
            if (settingView != null)
            {
                var lastSettingContainer = new ViewContainer()
                {
                    WidthSizingMode = SizingMode.Fill,
                    HeightSizingMode = SizingMode.AutoSize,
                    Parent = parent
                };


                lastSettingContainer.Show(settingView);

                if (settingView is SettingsView subSettingsView)
                {
                    subSettingsView.LockBounds = false;
                }
            }
        }
    }
}
