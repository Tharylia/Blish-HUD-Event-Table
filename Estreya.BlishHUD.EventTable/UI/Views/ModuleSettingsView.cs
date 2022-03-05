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
    using Estreya.BlishHUD.EventTable.Resources;

    public class ModuleSettingsView : View
    {
        protected override Task<bool> Load(IProgress<string> progress)
        {
            return Task.FromResult(true);
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

            var settingContainer = new ViewContainer()
            {
                WidthSizingMode = SizingMode.Fill,
                HeightSizingMode = SizingMode.AutoSize,
                Parent = parentPanel
            };

            var buttonText = Strings.SettingsView_OpenSettings;

            StandardButton button = new StandardButton()
            {
                Parent = settingContainer,
                Text = buttonText,
                Width = (int)EventTableModule.ModuleInstance.Font.MeasureString(buttonText).Width,
            };

            button.Location = new Point(Math.Max(buildPanel.Width / 2 - button.Width / 2, 20), Math.Max(buildPanel.Height / 2 - button.Height, 20));

            button.Click += (s, e) => EventTableModule.ModuleInstance.SettingsWindow.ToggleWindow();
        }
    }
}
