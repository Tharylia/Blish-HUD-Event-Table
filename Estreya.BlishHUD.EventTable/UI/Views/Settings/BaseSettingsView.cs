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
    using Estreya.BlishHUD.EventTable.Extensions;
    using Estreya.BlishHUD.EventTable.Helpers;
    using static Blish_HUD.ContentService;

    public abstract class BaseSettingsView : View
    {
        private static readonly Logger Logger = Logger.GetLogger<BaseSettingsView>();
        protected ModuleSettings ModuleSettings { get; set; }

        private static IEnumerable<Gw2Sharp.WebApi.V2.Models.Color> Colors { get; set; }

        private static Panel ColorPickerPanel { get; set; }

        private static string SelectedColorSetting { get; set; }

        private static ColorPicker ColorPicker { get; set; }
        private const int LEFT_PADDING = 20;
        private const int CONTROL_X_SPACING = 20;
        private const int LABEL_WIDTH = 250;
        private const int BINDING_WIDTH = 170;


        private static readonly Dictionary<Type, Func<SettingEntry, int, int, Control>> _typeLookup = new Dictionary<Type, Func<SettingEntry, int, int, Control>>
        {
            {
                typeof(bool),
                (SettingEntry settingEntry, int definedWidth, int xPos) =>
                {
                    var setting =settingEntry as SettingEntry<bool>;
                    var checkbox = new Checkbox()
                    {
                        Width = definedWidth,
                        Location = new Point(xPos, 0),
                        Checked = setting?.Value ?? false,
                        Enabled = !settingEntry.IsDisabled()
                    };

                    if (setting != null){
                        checkbox.CheckedChanged += (s,e) => setting.Value = e.Checked;
                    }

                    return checkbox;
                }
            },
            {
                typeof(string),
                (SettingEntry settingEntry, int definedWidth, int xPos) =>
                {
                    var setting =settingEntry as SettingEntry<string>;
                    TextBox textBox =  new TextBox()
                    {
                        Width= definedWidth,
                        Location = new Point(xPos, 0),
                        Text = setting?.Value ?? string.Empty,
                        Enabled = !settingEntry.IsDisabled()
                    };

                    if (setting != null){
                        textBox.TextChanged += (s,e) => setting.Value = ((ValueChangedEventArgs<string>)e).NewValue;
                    }

                    return textBox;
                }
            },
            {
                typeof(float),
                (SettingEntry settingEntry, int definedWidth, int xPos) =>
                {
                    var setting =settingEntry as SettingEntry<float>;
                    var range = setting?.GetRange() ?? null;
                    TrackBar trackBar = new TrackBar()
                    {
                        Width= definedWidth,
                        Location = new Point(xPos, 0),
                        Enabled = !settingEntry.IsDisabled(),
                        Value = setting?.GetValue() ?? 50,
                        MinValue = range.HasValue ? range.Value.Min: 0,
                        MaxValue = range.HasValue ? range.Value.Max:100,
                        SmallStep = true
                    };

                    if (setting != null){
                        trackBar.ValueChanged += (s,e) => setting.Value = e.Value;
                    }

                    return trackBar;
                }
            },
            {
                typeof(int),
                (SettingEntry settingEntry, int definedWidth, int xPos) =>
                {
                    var setting =settingEntry as SettingEntry<int>;
                    var range = setting?.GetRange() ?? null;
                    TrackBar trackBar = new TrackBar()
                    {
                        Width= definedWidth,
                        Location = new Point(xPos, 0),
                        Value = setting?.GetValue() ?? 0,
                        Enabled = !settingEntry.IsDisabled(),
                        MinValue = range.HasValue ? range.Value.Min: 0,
                        MaxValue = range.HasValue ? range.Value.Max:100
                    };

                    if (setting != null){
                        trackBar.ValueChanged += (s,e) => setting.Value = (int)e.Value;
                    }

                    return trackBar;
                }
            },
            {
                typeof(KeyBinding),
                (SettingEntry settingEntry, int definedWidth, int xPos) =>
                {
                    var setting =settingEntry as SettingEntry<KeyBinding>;
                    Controls.KeybindingAssigner  keybindingAssigner= new Controls.KeybindingAssigner(setting.Value, false)
                    {
                        Width = definedWidth,
                        Location = new Point(xPos, 0),
                        Enabled = !settingEntry.IsDisabled()
                    };

                    if (setting != null){
                        keybindingAssigner.BindingChanged += (s,e) => setting.Value = keybindingAssigner.KeyBinding;
                    }

                    return keybindingAssigner;
                }
            },
            {
                typeof(Enum),
                (SettingEntry settingEntry, int definedWidth,int xPos) =>
                {

                    var setting = (dynamic)settingEntry;

                    var dropdown = new Dropdown
                    {
                            Width = definedWidth,
                            Location = new Point(xPos, 0),
                            SelectedItem =   setting?.Value.ToString(),
                            Enabled = !settingEntry.IsDisabled()
                    };

                    foreach(var enumValue in Enum.GetNames(settingEntry.SettingType))
                    {
                        dropdown.Items.Add(enumValue);
                    }

                    if (setting != null){
                        dropdown.ValueChanged += (s,e) => setting.Value = (dynamic)Enum.Parse(settingEntry.SettingType, e.CurrentValue);
                    }

                    return dropdown;
                }
            }
        };

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
                    if (this.ModuleSettings.DefaultGW2Color != null)
                    {
                        Logger.Debug($"Adding default color: {this.ModuleSettings.DefaultGW2Color.Name}");
                        Colors = new List<Gw2Sharp.WebApi.V2.Models.Color>() { this.ModuleSettings.DefaultGW2Color };
                    }
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
                OuterControlPadding = new Vector2(LEFT_PADDING, 15),
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.Fill,
                AutoSizePadding = new Point(0, 15),
                Parent = buildPanel
            };

            this.InternalBuild(parentPanel);
        }

        protected abstract Task<bool> InternalLoad(IProgress<string> progress);

        protected abstract void InternalBuild(Panel parent);

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

        protected Panel RenderSetting(Panel parent, SettingEntry setting)
        {
            /*var settingView = SettingView.FromType(setting, parent.Width);
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
            */

            Panel panel = GetPanel(parent);

            Label label = GetLabel(panel, setting.DisplayName);

            Type type = setting.SettingType;

            if (setting.SettingType.IsEnum)
            {
                type = typeof(Enum);
            }

            if (_typeLookup.TryGetValue(type, out var controlBuilder))
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

        private Panel GetPanel(Panel parent)
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
            Panel panel = GetPanel(parent);

            StandardButton button = new StandardButton()
            {
                Parent = panel,
                Text = text,
                Width = (int)EventTableModule.ModuleInstance.Font.MeasureString(text).Width,
                Enabled = !disabledCallback?.Invoke() ?? true,
            };

            button.Click += (s, e) => action.Invoke();
        }

        protected void RenderColorSetting(Panel parent, SettingEntry<Gw2Sharp.WebApi.V2.Models.Color> setting)
        {
            var panel = GetPanel(parent);
            Label label = GetLabel(panel, setting.DisplayName);

            var colorBox = new ColorBox()
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
                if (SelectedColorSetting != setting.EntryKey) return;

                setting.Value = ColorPicker.SelectedColor;
                ColorPickerPanel.Visible = false;
                colorBox.Color = ColorPicker.SelectedColor;
            };
        }
    }
}
