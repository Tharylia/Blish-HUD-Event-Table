namespace Estreya.BlishHUD.EventTable.UI.Views
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using Blish_HUD.Input;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Extensions;
    using Estreya.BlishHUD.EventTable.Helpers;
    using Estreya.BlishHUD.EventTable.Resources;
    using Estreya.BlishHUD.EventTable.UI.Views.Settings.Controls;
    using Microsoft.Xna.Framework;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class BaseSettingsView : View
    {
        private const int LEFT_PADDING = 20;
        private const int CONTROL_X_SPACING = 20;
        private const int LABEL_WIDTH = 250;
        private const int BINDING_WIDTH = 170;

        private static readonly Logger Logger = Logger.GetLogger<BaseSettingsView>();
        protected ModuleSettings ModuleSettings { get; set; }

        private static IEnumerable<Gw2Sharp.WebApi.V2.Models.Color> Colors { get; set; }

        private static Panel ColorPickerPanel { get; set; }

        private static string SelectedColorSetting { get; set; }

        private static ColorPicker ColorPicker { get; set; }
        private Container BuildPanel { get; set; }
        private Panel ErrorPanel { get; set; }
        private CancellationTokenSource ErrorCancellationTokenSource = new CancellationTokenSource();

        public BaseSettingsView(ModuleSettings settings)
        {
            this.ModuleSettings = settings;
        }

        protected override async Task<bool> Load(IProgress<string> progress)
        {
            if (Colors == null)
            {
                progress.Report(Strings.BaseSettingsView_LoadingColors);

                try
                {
                    Colors = await EventTableModule.ModuleInstance.Gw2ApiManager.Gw2ApiClient.V2.Colors.AllAsync();
                }
                catch (Exception ex)
                {
                    Logger.Warn($"Could not load gw2 colors: {ex.Message}");
                    if (this.ModuleSettings.DefaultGW2Color != null)
                    {
                        Logger.Debug($"Adding default color: {this.ModuleSettings.DefaultGW2Color.Name}");
                        Colors = new List<Gw2Sharp.WebApi.V2.Models.Color>() { this.ModuleSettings.DefaultGW2Color };
                    }
                }
            }

            if (ColorPicker == null)
            {
                progress.Report(Strings.BaseSettingsView_LoadingColorPicker);
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

                progress.Report(Strings.BaseSettingsView_AddingColorsToColorPicker);
                if (Colors != null)
                {
                    foreach (Gw2Sharp.WebApi.V2.Models.Color color in Colors.OrderBy(color => color.Categories.FirstOrDefault()))
                    {
                        ColorPicker.Colors.Add(color);
                    }
                }
            }

            progress.Report(string.Empty);

            return await this.InternalLoad(progress);
        }

        protected override void Build(Container buildPanel)
        {
            Rectangle bounds = buildPanel.ContentRegion;

            FlowPanel parentPanel = new FlowPanel()
            {
                Size = bounds.Size,
                FlowDirection = ControlFlowDirection.SingleTopToBottom,
                ControlPadding = new Vector2(5, 2),
                OuterControlPadding = new Vector2(LEFT_PADDING, 15),
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.Fill,
                AutoSizePadding = new Point(0, 15),
                Parent = buildPanel
            };

            this.BuildPanel = parentPanel;
            this.RegisterErrorPanel(buildPanel);

            this.InternalBuild(parentPanel);
        }

        protected abstract Task<bool> InternalLoad(IProgress<string> progress);

        protected abstract void InternalBuild(Panel parent);

        protected void RenderEmptyLine(Panel parent)
        {
            ViewContainer settingContainer = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.AutoSize,
                Parent = parent
            };

            settingContainer.Show(new EmptySettingsLineView(25));
        }

        protected Panel RenderSetting<T>(Panel parent, SettingEntry<T> setting)
        {
            Panel panel = this.GetPanel(parent);

            Label label = this.GetLabel(panel, setting.DisplayName);

            Type type = setting.SettingType;

            try
            {
                Control ctrl = ControlProvider<T>.Create(setting, this.HandleValidation, BINDING_WIDTH, -1, label.Right + CONTROL_X_SPACING, 0);
                ctrl.Parent = panel;
                ctrl.BasicTooltipText = setting.Description;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, $"Type \"{setting.SettingType.FullName}\" could not be found in internal type lookup:");
            }

            return panel;
        }

        protected Panel GetPanel(Container parent)
        {
            return new Panel
            {
                HeightSizingMode = SizingMode.AutoSize,
                WidthSizingMode = SizingMode.AutoSize,
                Parent = parent
            };
        }

        protected Label GetLabel(Panel parent, string text)
        {
            return new Label()
            {
                Parent = parent,
                Text = text,
                AutoSizeHeight = true,
                Width = LABEL_WIDTH
            };
        }


        protected void RenderButton(Panel parent, string text, Action action, Func<bool> disabledCallback = null)
        {
            this.RenderButton(parent, text, () =>
            {
                action.Invoke();
                return Task.CompletedTask;
            }, disabledCallback);
        }

        protected void RenderButton(Panel parent, string text, Func<Task> action, Func<bool> disabledCallback = null)
        {
            Panel panel = this.GetPanel(parent);

            StandardButton button = new StandardButton()
            {
                Parent = panel,
                Text = text,
                Width = (int)EventTableModule.ModuleInstance.Font.MeasureString(text).Width,
                Enabled = !disabledCallback?.Invoke() ?? true,
            };

            button.Click += (s, e) => Task.Run(action.Invoke);
        }

        protected void RenderLabel(Panel parent, string title, string value)
        {
            Panel panel = this.GetPanel(parent);

            Label titleLabel = this.GetLabel(panel, title);
            Label valueLabel = this.GetLabel(panel, value);
            valueLabel.Left = titleLabel.Right + CONTROL_X_SPACING;
        }

        protected void RenderColorSetting(Panel parent, SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> setting)
        {
            Panel panel = this.GetPanel(parent);
            Label label = this.GetLabel(panel, setting.DisplayName);

            ColorBox colorBox = new ColorBox()
            {
                Location = new Point(label.Right + CONTROL_X_SPACING, 0),
                Parent = panel,
                Color = setting.Value
            };

            colorBox.LeftMouseButtonPressed += (s, e) =>
            {
                ColorPickerPanel.Parent = parent.Parent;
                ColorPickerPanel.Size = new Point(parent.Width - 30, 850);
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
                if (SelectedColorSetting != setting.EntryKey)
                {
                    return;
                }

                Gw2Sharp.WebApi.V2.Models.Color selectedColor = ColorPicker.SelectedColor;

                if (!this.HandleValidation(setting, selectedColor))
                {
                    selectedColor = setting.Value;
                }

                setting.Value = selectedColor;
                ColorPickerPanel.Visible = false;
                colorBox.Color = selectedColor;
            };
        }

        private void RegisterErrorPanel(Container parent)
        {
            Panel panel = this.GetPanel(parent);
            panel.ZIndex = 1000;
            panel.WidthSizingMode = SizingMode.Fill;
            panel.Visible = false;

            this.ErrorPanel = panel;
        }

        public async void ShowError(string message)
        {
            lock (this.ErrorPanel)
            {
                if (this.ErrorPanel.Visible)
                {
                    this.ErrorCancellationTokenSource.Cancel();
                    this.ErrorCancellationTokenSource = new CancellationTokenSource();
                }
            }

            this.ErrorPanel.ClearChildren();
            BitmapFont font = GameService.Content.DefaultFont32;
            message = DrawUtil.WrapText(font, message, this.ErrorPanel.Width * (3f / 4f));

            Label label = this.GetLabel(this.ErrorPanel, message);
            label.Width = this.ErrorPanel.Width;

            label.Font = font;
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.TextColor = Color.Red;

            this.ErrorPanel.Height = label.Height;
            this.ErrorPanel.Bottom = this.BuildPanel.ContentRegion.Bottom;

            lock (this.ErrorPanel)
            {
                this.ErrorPanel.Show();
            }

            try
            {
                await Task.Delay(5000, this.ErrorCancellationTokenSource.Token);
            }
            catch (TaskCanceledException)
            {
                Logger.Debug("Task was canceled to show new error:");
            }

            lock (this.ErrorPanel)
            {
                this.ErrorPanel.Hide();
            }
        }

        private bool HandleValidation<T>(SettingEntry<T> settingEntry, T value)
        {
            SettingValidationResult result = settingEntry.CheckValidation(value);

            if (!result.Valid)
            {
                this.ShowError(result.InvalidMessage);
                return false;
            }

            return true;
        }

        protected override void Unload()
        {
            base.Unload();
            this.ErrorPanel.Dispose();
        }
    }
}
