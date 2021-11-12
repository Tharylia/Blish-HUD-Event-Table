namespace Estreya.BlishHUD.EventTable.UI.Views
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
                Size = bounds.Size,
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                ControlPadding = new Vector2(5, 2),
                OuterControlPadding = new Vector2(10, 15),
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.AutoSize,
                AutoSizePadding = new Point(0, 15),
                Parent = buildPanel
            };

            RenderSetting(parentPanel, ModuleSettings.GlobalEnabled);
            RenderSetting(parentPanel, ModuleSettings.GlobalEnabledHotkey);
#if DEBUG
            RenderSetting(parentPanel, ModuleSettings.DebugEnabled);
#endif
            RenderSetting(parentPanel, ModuleSettings.HideOnMissingMumbleTicks);
            RenderSetting(parentPanel, ModuleSettings.ShowTooltips);
            RenderSetting(parentPanel, ModuleSettings.CopyWaypointOnClick);
            RenderEmptyLine(parentPanel);
            RenderSetting(parentPanel, ModuleSettings.EventHeight);
            RenderSetting(parentPanel, ModuleSettings.EventFontSize);
            RenderSetting(parentPanel, ModuleSettings.EventTimeSpan);
            RenderSetting(parentPanel, ModuleSettings.Opacity);
            RenderEmptyLine(parentPanel);
            RenderSetting(parentPanel, ModuleSettings.LocationX);
            RenderSetting(parentPanel, ModuleSettings.LocationY);
            RenderSetting(parentPanel, ModuleSettings.Width);
            RenderSetting(parentPanel, ModuleSettings.Height);
            RenderSetting(parentPanel, ModuleSettings.SnapHeight);
            RenderEmptyLine(parentPanel);
            RenderSetting(parentPanel, ModuleSettings.BackgroundColorOpacity);
            RenderColorSetting(parentPanel, ModuleSettings.BackgroundColor);
        }

        private void RenderEmptyLine(Panel parent)
        {
            var lastSettingContainer = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.AutoSize,
                Parent = parent
            };

            lastSettingContainer.Show(new EmptySettingsLineView(25));
        }

        private void RenderSetting(Panel parent, SettingEntry setting)
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
        private void RenderColorSetting(Panel parent, SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> setting)
        {
            var settingContainer = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.AutoSize,
                Parent = parent
            };
            var label = new Label()
            {
                Location = new Point(5, 0),
                AutoSizeWidth = true,
                Parent = settingContainer,
                Text = setting.DisplayName
            };
            var colorBox = new ColorBox()
            {
                Location = new Point(Math.Max(185, label.Left + 10), 0),
                Parent = settingContainer,
                Color = setting.Value
            };


            var colorPicker = new ColorPicker()
            {
                Location = new Point(colorBox.Right + 30, 0),
                Size = new Point(parent.Width - colorBox.Right - 60, 850),
                Parent = settingContainer,
                CanScroll = true,
                Visible = false,
                AssociatedColorBox = colorBox
            };

            colorPicker.SelectedColorChanged += (s, e) =>
            {
                setting.Value = colorPicker.SelectedColor;
                colorPicker.Visible = false;
            };

            colorBox.LeftMouseButtonPressed += (s, e) =>
            {
                colorPicker.Visible = !colorPicker.Visible;
            };

            foreach (var color in Colors.OrderBy(color => color.Categories.FirstOrDefault()))
            {
                colorPicker.Colors.Add(color);
            }

        }
    }
}
