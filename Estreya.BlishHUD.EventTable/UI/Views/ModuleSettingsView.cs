namespace Estreya.BlishHUD.EventTable.UI.Views
{
    using Blish_HUD.Controls;
    using Microsoft.Xna.Framework;
    using Blish_HUD.Settings.UI.Views;
    using Blish_HUD.Graphics.UI;
    using Blish_HUD.Input;
    using Blish_HUD.Settings;
    using System.Threading.Tasks;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ModuleSettingsView : View
    {
        private ModuleSettings ModuleSettings { get; set; }

        private static IEnumerable<Gw2Sharp.WebApi.V2.Models.Color> Colors { get; set; }

        private static Panel ColorPickerPanel { get; set; }

        private static string SelectedColorSetting {  get; set; }

        private static ColorPicker ColorPicker {  get; set; }

        public ModuleSettingsView(ModuleSettings settings)
        {
            this.ModuleSettings = settings;
        }

        protected override async Task<bool> Load(IProgress<string> progress)
        {
            if (Colors == null)
            {
                progress.Report("Loading Colors...");
                Colors = await EventTableModule.ModuleInstance.Gw2ApiManager.Gw2ApiClient.V2.Colors.AllAsync();
            }

            if (ColorPicker == null)
            {
                progress.Report("Loading ColorPicker...");
                // build initial colorpicker

                ColorPickerPanel = new Panel()
                {
                    Location = new Point(10, 10),
                    WidthSizingMode = SizingMode.AutoSize,
                    HeightSizingMode = SizingMode.AutoSize,
                    Visible = false,
                    ZIndex = int.MaxValue,
                    BackgroundColor = Color.Black,
                    ShowBorder = false,
                };

                ColorPicker = new ColorPicker()
                {
                    Location = new Point(10, 10),
                    Parent = ColorPickerPanel,
                    Visible = true
                };

                progress.Report($"Adding Colors to ColorPicker...");
                foreach (var color in Colors.OrderBy(color => color.Categories.FirstOrDefault()))
                {
                    ColorPicker.Colors.Add(color);
                }
            }

            progress.Report("");
            return true;
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
            RenderButton(parentPanel, "Open Settings", () =>
            {
                if (EventTableModule.ModuleInstance.SettingsWindow.Visible)
                {
                    EventTableModule.ModuleInstance.SettingsWindow.Hide();
                }
                else
                {
                    EventTableModule.ModuleInstance.SettingsWindow.Show();
                }
            });
#endif
            RenderSetting(parentPanel, ModuleSettings.HideOnMissingMumbleTicks);
            RenderSetting(parentPanel, ModuleSettings.ShowTooltips);
            RenderSetting(parentPanel, ModuleSettings.CopyWaypointOnClick);
            RenderSetting(parentPanel, ModuleSettings.ShowContextMenuOnClick);
            RenderSetting(parentPanel, ModuleSettings.BuildDirection);
            RenderEmptyLine(parentPanel);
            RenderSetting(parentPanel, ModuleSettings.EventHeight);
            RenderSetting(parentPanel, ModuleSettings.EventFontSize);
            RenderSetting(parentPanel, ModuleSettings.EventTimeSpan);
            RenderSetting(parentPanel, ModuleSettings.DrawEventBorder);
            RenderSetting(parentPanel, ModuleSettings.Opacity);
            RenderColorSetting(parentPanel, ModuleSettings.TextColor);
            RenderEmptyLine(parentPanel);
            RenderSetting(parentPanel, ModuleSettings.UseFiller);
            RenderSetting(parentPanel, ModuleSettings.UseFillerEventNames);
            RenderColorSetting(parentPanel, ModuleSettings.FillerTextColor);
            RenderEmptyLine(parentPanel);
            RenderSetting(parentPanel, ModuleSettings.BackgroundColorOpacity);
            RenderColorSetting(parentPanel, ModuleSettings.BackgroundColor);
            RenderEmptyLine(parentPanel);
            RenderSetting(parentPanel, ModuleSettings.LocationX);
            RenderSetting(parentPanel, ModuleSettings.LocationY);
            RenderSetting(parentPanel, ModuleSettings.Width);
            //RenderSetting(parentPanel, ModuleSettings.Height);
            //RenderSetting(parentPanel, ModuleSettings.SnapHeight);
        }

        private void RenderEmptyLine(Panel parent)
        {
            var settingContainer = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.AutoSize,
                Parent = parent
            };

            settingContainer.Show(new EmptySettingsLineView(25));
        }

        private void RenderSetting(Panel parent, SettingEntry setting)
        {
            var settingView = SettingView.FromType(setting, parent.Width);
            if (settingView != null)
            {
                var settingContainer = new ViewContainer()
                {
                    WidthSizingMode = SizingMode.Fill,
                    HeightSizingMode = SizingMode.AutoSize,
                    Parent = parent
                };


                settingContainer.Show(settingView);

                if (settingView is SettingsView subSettingsView)
                {
                    subSettingsView.LockBounds = false;
                }
            }
        }

        private void RenderButton(Panel parent,string text, Action action)
        {
                var settingContainer = new ViewContainer()
                {
                    WidthSizingMode = SizingMode.Fill,
                    HeightSizingMode = SizingMode.AutoSize,
                    Parent = parent
                };

            StandardButton button = new StandardButton()
            {
                Parent = settingContainer,
                Text = text
            };

            button.Click += (s, e) => action.Invoke();
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

            colorBox.LeftMouseButtonPressed += (s, e) =>
            {
                ColorPickerPanel.Parent = parent.Parent;
                ColorPickerPanel.Size = new Point(parent.Width -  30, 850);
                ColorPicker.Size = new Point(ColorPickerPanel.Size.X - 20, ColorPickerPanel.Size.Y - 20);

                // Hack to get lineup right
                Gw2Sharp.WebApi.V2.Models.Color tempColor = new Gw2Sharp.WebApi.V2.Models.Color()
                {
                    Id = int.MaxValue,
                    Name = "temp"
                };

                ColorPicker.RecalculateLayout();
                ColorPicker.Colors.Add(tempColor);
                ColorPicker.Colors.Remove(tempColor);


                ColorPickerPanel.Visible = !ColorPickerPanel.Visible;
                SelectedColorSetting = setting.EntryKey;
            };

            ColorPicker.SelectedColorChanged += (sender, eArgs) =>
            {
                if (SelectedColorSetting != setting.EntryKey) return;

                setting.Value = ColorPicker.SelectedColor;
                ColorPickerPanel.Visible = false;
                colorBox.Color = ColorPicker.SelectedColor;
            };
        }
    }
}
