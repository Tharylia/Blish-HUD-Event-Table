﻿namespace BlishHUD.EventTable.UI.Views
{
    using Blish_HUD;
    using Blish_HUD.Common.UI.Views;
    using Blish_HUD.Controls;
    using Blish_HUD.Graphics.UI;
    using Microsoft.Xna.Framework;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class TooltipView : View, ITooltipView, IView
    {
        private string Title { get; set; }
        private string Description { get; set; }
        private string Icon { get; set; }
        public TooltipView(string title, string description)
        {
            this.Title = title;
            this.Description = description;
        }
        public TooltipView(string title, string description, string icon) : this(title, description)
        {
            this.Icon = icon;
        }

        protected override void Build(Container buildPanel)
        {
            //buildPanel.Size = new Point(300, 256);
            buildPanel.HeightSizingMode = SizingMode.AutoSize;
            buildPanel.WidthSizingMode = SizingMode.AutoSize;

            var image = new Image()
            {
                Size = new Point(48, 48),
                Location = new Point(8, 8),
                Parent = buildPanel,
                Texture = string.IsNullOrWhiteSpace(this.Icon) ? null : GameService.Content.GetRenderServiceTexture(this.Icon)
            };

            var nameLabel = new Label()
            {
                AutoSizeHeight = false,
                AutoSizeWidth = true,
                Location = new Point(image.Right + 8, image.Top),
                Height = image.Height / 2,
                Padding = new Thickness(0, 8, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Middle,
                Font = GameService.Content.DefaultFont16,
                Text = this.Title,
                Parent = buildPanel
            };

            var descriptionLabel = new Label()
            {
                AutoSizeHeight = true,
                AutoSizeWidth = false,
                Location = new Point(nameLabel.Left, image.Top + image.Height / 2),
                Width = Math.Max(nameLabel.Width, 200),
                Padding = new Thickness(0, 8, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Middle,
                TextColor = Control.StandardColors.DisabledText,
                WrapText = true,
                Text = this.Description,
                Parent = buildPanel
            };

            /*_achievementRequirementLabel = new Label()
            {
                AutoSizeHeight = true,
                AutoSizeWidth = false,
                Location = new Point(_categoryIconImage.Left, _achievementDescriptionLabel.Bottom + 8),
                WrapText = true,
                Parent = buildPanel
            };*/
        }
    }
}
