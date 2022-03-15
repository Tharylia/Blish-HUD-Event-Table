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


        private Dictionary<Type, Func<SettingEntry, int, int, Control>> _typeLookup;

        public BaseSettingsView(ModuleSettings settings)
        {
            this.ModuleSettings = settings;
            this.LoadTypeLookup();
        }

        private void LoadTypeLookup()
        {
            this._typeLookup = new Dictionary<Type, Func<SettingEntry, int, int, Control>>
            {
                {
                    typeof(bool),
                    (SettingEntry settingEntry, int definedWidth, int xPos) =>
                    {
                        SettingEntry<bool> setting =settingEntry as SettingEntry<bool>;
                        Checkbox checkbox = new Checkbox()
                        {
                            Width = definedWidth,
                            Location = new Point(xPos, 0),
                            Checked = setting?.Value ?? false,
                            Enabled = !settingEntry.IsDisabled()
                        };

                        if (setting != null){
                            checkbox.CheckedChanged += (s,e) => {
                                if (this.HandleValidation(setting, e.Checked))
                                {
                                    setting.Value = e.Checked;
                                }
                                else
                                {
                                    checkbox.Checked = !e.Checked;
                                }
                            };
                        }

                        return checkbox;
                    }
                },
                {
                    typeof(string),
                    (SettingEntry settingEntry, int definedWidth, int xPos) =>
                    {
                        SettingEntry<string> setting =settingEntry as SettingEntry<string>;
                        TextBox textBox =  new TextBox()
                        {
                            Width= definedWidth,
                            Location = new Point(xPos, 0),
                            Text = setting?.Value ?? string.Empty,
                            Enabled = !settingEntry.IsDisabled()
                        };

                        if (setting != null){
                            textBox.TextChanged += (s,e) => {
                                ValueChangedEventArgs<string> eventArgs = (ValueChangedEventArgs<string>)e;
                                if (this.HandleValidation(setting, eventArgs.NewValue))
                                {
                                    setting.Value = eventArgs.NewValue;
                                }
                                else
                                {
                                    textBox.Text = eventArgs.PreviousValue;
                                }
                            };
                        }

                        return textBox;
                    }
                },
                {
                    typeof(float),
                    (SettingEntry settingEntry, int definedWidth, int xPos) =>
                    {
                        SettingEntry<float> setting =settingEntry as SettingEntry<float>;
                        (float Min, float Max)? range = setting?.GetRange() ?? null;
                        TrackBar trackBar = new TrackBar()
                        {
                            Width= definedWidth,
                            Location = new Point(xPos, 0),
                            Enabled = !settingEntry.IsDisabled(),
                            MinValue = range.HasValue ? range.Value.Min: 0,
                            MaxValue = range.HasValue ? range.Value.Max:100,
                            SmallStep = true,
                            Value = setting?.GetValue() ?? 50
                        };

                        if (setting != null){
                            trackBar.ValueChanged += (s,e) => {
                                if (this.HandleValidation(setting, e.Value))
                                {
                                    setting.Value = e.Value;
                                }
                                else
                                {
                                    trackBar.Value = setting.Value;
                                }
                            };
                        }

                        return trackBar;
                    }
                },
                {
                    typeof(int),
                    (SettingEntry settingEntry, int definedWidth, int xPos) =>
                    {
                        SettingEntry<int> setting =settingEntry as SettingEntry<int>;
                        (int Min, int Max)? range = setting?.GetRange() ?? null;
                        TrackBar trackBar = new TrackBar()
                        {
                            Width= definedWidth,
                            Location = new Point(xPos, 0),
                            Enabled = !settingEntry.IsDisabled(),
                            MinValue = range.HasValue ? range.Value.Min: 0,
                            MaxValue = range.HasValue ? range.Value.Max:100,
                            Value = setting?.GetValue() ?? 50
                        };

                        if (setting != null){
                            trackBar.ValueChanged += (s,e) => {
                                if (this.HandleValidation(setting, (int)e.Value))
                                {
                                    setting.Value = (int)e.Value;
                                }
                                else
                                {
                                    trackBar.Value = setting.Value;
                                }
                            };
                        }

                        return trackBar;
                    }
                },
                {
                    typeof(KeyBinding),
                    (SettingEntry settingEntry, int definedWidth, int xPos) =>
                    {
                        SettingEntry<KeyBinding> setting =settingEntry as SettingEntry<KeyBinding>;
                        Controls.KeybindingAssigner  keybindingAssigner= new Controls.KeybindingAssigner(setting.Value, false)
                        {
                            Width = definedWidth,
                            Location = new Point(xPos, 0),
                            Enabled = !settingEntry.IsDisabled()
                        };

                        if (setting != null){
                            keybindingAssigner.BindingChanged += (s,e) => {
                                if (this.HandleValidation(setting, keybindingAssigner.KeyBinding))
                                {
                                    setting.Value = keybindingAssigner.KeyBinding;
                                }
                                else
                                {
                                    keybindingAssigner.KeyBinding = setting.Value;
                                }
                            };
                        }

                        return keybindingAssigner;
                    }
                },
                {
                    typeof(Enum),
                    (SettingEntry settingEntry, int definedWidth,int xPos) =>
                    {
                        dynamic setting = (dynamic)settingEntry;

                        Dropdown dropdown = new Dropdown
                        {
                            Width = definedWidth,
                            Location = new Point(xPos, 0),
                            SelectedItem =   setting?.Value.ToString(),
                            Enabled = !settingEntry.IsDisabled()
                        };

                        foreach(string enumValue in Enum.GetNames(settingEntry.SettingType))
                        {
                            dropdown.Items.Add(enumValue);
                        }

                        if (setting != null){
                            dropdown.ValueChanged += (s,e) => {
                                if (HandleValidation(setting, e.CurrentValue))
                                {
                                    setting.Value = (dynamic)Enum.Parse(settingEntry.SettingType, e.CurrentValue);
                                }
                                else
                                {
                                    dropdown.SelectedItem = e.PreviousValue;
                                }
                            };
                        }

                        return dropdown;
                    }
                }
            };
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

        protected Panel RenderSetting(Panel parent, SettingEntry setting)
        {
            Panel panel = this.GetPanel(parent);

            Label label = this.GetLabel(panel, setting.DisplayName);

            Type type = setting.SettingType;

            if (setting.SettingType.IsEnum)
            {
                type = typeof(Enum);
            }

            if (this._typeLookup.TryGetValue(type, out Func<SettingEntry, int, int, Control> controlBuilder))
            {
                Control ctrl = controlBuilder.Invoke(setting, BINDING_WIDTH, label.Right + CONTROL_X_SPACING);
                ctrl.Parent = panel;
                ctrl.BasicTooltipText = setting.Description;
            }
            else
            {
                Logger.Warn($"Type \"{setting.SettingType.FullName}\" could not be found in internal type lookup.");
            }

            return panel;
        }

        private Panel GetPanel(Container parent)
        {
            return new Panel
            {
                HeightSizingMode = SizingMode.AutoSize,
                WidthSizingMode = SizingMode.AutoSize,
                Parent = parent
            };
        }

        private Label GetLabel(Panel parent, string text)
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

            button.Click += (s, e) => AsyncHelper.RunSync(action.Invoke);
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
