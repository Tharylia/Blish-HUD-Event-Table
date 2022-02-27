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
    using Blish_HUD;

    public abstract class BaseSettingsView : View
    {
        private static readonly Logger Logger = Logger.GetLogger<BaseSettingsView>();
        protected ModuleSettings ModuleSettings { get; set; }

        private static IEnumerable<Gw2Sharp.WebApi.V2.Models.Color> Colors { get; set; }

        private static Panel ColorPickerPanel { get; set; }

        private static string SelectedColorSetting {  get; set; }

        private static ColorPicker ColorPicker {  get; set; }

        public BaseSettingsView(ModuleSettings settings)
        {
            this.ModuleSettings = settings;
        }

        protected override async Task<bool> Load(IProgress<string> progress)
        {
            if (Colors == null)
            {
                progress.Report("Loading Colors...");

                try
                {
                    Colors = await EventTableModule.ModuleInstance.Gw2ApiManager.Gw2ApiClient.V2.Colors.AllAsync();
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Could not load gw2 colors: {ex.Message}");
                }
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
                if (Colors != null)
                {
                    foreach (var color in Colors.OrderBy(color => color.Categories.FirstOrDefault()))
                    {
                        ColorPicker.Colors.Add(color);
                    }
                }
            }

            progress.Report("");

            return await this.InternalLoad(progress);
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
                HeightSizingMode = SizingMode.Fill,
                AutoSizePadding = new Point(0, 15),
                Parent = buildPanel
            };

            this.InternalBuild(parentPanel);
        }

        protected abstract Task<bool> InternalLoad(IProgress<string> progress);

        protected abstract void InternalBuild(FlowPanel parent);

        protected void RenderEmptyLine(Panel parent)
        {
            var settingContainer = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.AutoSize,
                Parent = parent
            };

            settingContainer.Show(new EmptySettingsLineView(25));
        }

        protected void RenderSetting(Panel parent, SettingEntry setting)
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
#if DEBUG
                setting.PropertyChanged += (s, e) =>
                {
                    (settingView as View).Presenter.DoUpdateView();
                };
#endif
            }

        }

        protected void RenderButton(Panel parent,string text, Action action, Func<bool> disabledCallback = null)
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
                Text = text,
                Width = (int)EventTableModule.ModuleInstance.Font.MeasureString(text).Width,
                Enabled = !disabledCallback?.Invoke() ?? true,
            };

            button.Click += (s, e) => action.Invoke();
        }

        protected void RenderColorSetting(Panel parent, SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> setting)
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
