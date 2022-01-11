namespace Estreya.BlishHUD.EventTable.UI.Container
{
    using Blish_HUD;
    using Blish_HUD._Extensions;
    using Blish_HUD.Controls;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Extensions;
    using Estreya.BlishHUD.EventTable.Models;
    using Glide;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public class EventTableContainer : Blish_HUD.Controls.Container
    {
        private int EventHeight
        {
            get
            {
                return this.Settings.EventHeight.Value;
            }
        }

        private TimeSpan TimeSinceDraw { get; set; }

        private TimeSpan _eventTimeSpan = TimeSpan.Zero;

        private TimeSpan EventTimeSpan
        {
            get
            {
                if (this._eventTimeSpan == TimeSpan.Zero)
                {
                    this._eventTimeSpan = TimeSpan.FromMinutes(this.Settings.EventTimeSpan.Value);
                }

                return this._eventTimeSpan;
            }
        }

        

        private DateTime EventTimeMin
        {
            get
            {
                DateTime min = EventTableModule.ModuleInstance.DateTimeNow.Subtract(this.EventTimeSpan.Subtract(TimeSpan.FromMilliseconds(this.EventTimeSpan.TotalMilliseconds / 2)));
                return min;
            }
        }

        private DateTime EventTimeMax
        {
            get
            {
                DateTime max = EventTableModule.ModuleInstance.DateTimeNow.Add(this.EventTimeSpan.Subtract(TimeSpan.FromMilliseconds(this.EventTimeSpan.TotalMilliseconds / 2)));
                return max;
            }
        }

        private BitmapFont _font;

        private BitmapFont Font
        {
            get
            {
                if (this._font == null)
                {
                    //TODO: When fixed in core
                    string name = Enum.GetName(typeof(EventTableContainer.FontSize), this.Settings.EventFontSize.Value);

                    if (!Enum.TryParse(name, out ContentService.FontSize size))
                    {
                        size = ContentService.FontSize.Size16;
                    }

                    this._font = GameService.Content.GetFont(ContentService.FontFace.Menomonia, size /* this.Settings.EventFontSize.Value*/, ContentService.FontStyle.Regular);
                }

                return this._font;
            }
        }

        public enum FontSize
        {
            Size8,
            Size11,
            Size12,
            Size14,
            Size16,
            Size18,
            Size20,
            Size22,
            Size24,
            Size32,
            Size34,
            Size36
        }

        private double PixelPerMinute
        {
            get
            {
                int pixels = this.Size.X;

                double pixelPerMinute = pixels / this.EventTimeSpan.TotalMinutes;

                return pixelPerMinute;
            }
        }

        private IEnumerable<EventCategory> _eventCategories;
        private IEnumerable<EventCategory> EventCategories
        {
            get
            {
                var categories = _eventCategories;

                return categories;
            }
            set
            {
                this._eventCategories = value;
            }
        }

        private Tween CurrentVisibilityAnimation { get; set; }

        private ModuleSettings Settings { get; set; }

        private Texture2D Texture { get; set; }

        public EventTableContainer(IEnumerable<EventCategory> eventCategories, ModuleSettings settings)
        {
            this.EventCategories = eventCategories;
            this.Settings = settings;
            this.Settings.ModuleSettingsChanged += this.Settings_ModuleSettingsChanged;
            this.LeftMouseButtonPressed += this.EventTableContainer_Click;
            this.RightMouseButtonPressed += this.EventTableContainer_Click;
        }

        private void Settings_ModuleSettingsChanged(object sender, ModuleSettings.ModuleSettingsChangedEventArgs e)
        {
            switch (e.Name)
            {
                case nameof(ModuleSettings.EventFontSize):
                    this._font = null;
                    break;
                case nameof(ModuleSettings.EventTimeSpan):
                    this._eventTimeSpan = TimeSpan.Zero;
                    break;
            }
        }

        private void EventTableContainer_Click(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            foreach (EventCategory eventCategory in this.EventCategories)
            {
                foreach (Event ev in eventCategory.Events)
                {
                    if (ev.IsHovered(EventCategories, this.Settings.AllEvents, eventCategory, EventTableModule.ModuleInstance.DateTimeNow, EventTimeMax, EventTimeMin, this.ContentRegion, RelativeMousePosition, PixelPerMinute, EventHeight, EventTableModule.ModuleInstance.Debug))
                    {
                        ev.HandleClick(sender, e);
                        return;
                    }
                }
            }
        }

        protected override CaptureType CapturesInput()
        {
            return CaptureType.DoNotBlock;
        }

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            spriteBatch.End();
            spriteBatch.Begin(this.SpriteBatchParameters);

            InitializeBaseTexture(spriteBatch.GraphicsDevice);

            IEnumerable<EventCategory> eventCategories = this.EventCategories;

            Color backgroundColor = this.Settings.BackgroundColor.Value.Id == 1 ? Color.Transparent : this.Settings.BackgroundColor.Value.Cloth.ToXnaColor();

            this.BackgroundColor = backgroundColor * this.Settings.BackgroundColorOpacity.Value;

            int y = 0;

            foreach (EventCategory eventCategory in eventCategories)
            {
                List<KeyValuePair<DateTime, Event>> eventStarts = eventCategory.GetEventOccurences(this.Settings.AllEvents, EventTableModule.ModuleInstance.DateTimeNow, EventTimeMax, EventTimeMin, this.Settings.UseFiller.Value);

                var groups = eventStarts.GroupBy(ev => ev.Value);

                bool anyEventDrawn = false;

                foreach (var group in groups)
                {
                    var starts = group.Select(g => g.Key).ToList();
                    anyEventDrawn = starts.Count > 0;
                    group.Key.Draw(spriteBatch, bounds, this, this.Texture, eventCategories.ToList(), eventCategory, this.PixelPerMinute, this.EventHeight, EventTableModule.ModuleInstance.DateTimeNow, EventTimeMin, EventTimeMax, this.Font, starts);
                }

                if (anyEventDrawn)
                    y = groups.ElementAt(0).Key.GetYPosition(eventCategories, Settings.AllEvents, eventCategory, EventHeight, EventTableModule.ModuleInstance.Debug);
            }

            if (this.Settings.SnapHeight.Value)
            {
                this.Size = new Point(bounds.Width, y + this.EventHeight);
            }

            this.DrawLine(spriteBatch, new Rectangle(this.Size.X / 2, 0, 2, this.Size.Y), Color.LightGray);

            spriteBatch.End();
            spriteBatch.Begin(this.SpriteBatchParameters);

        }

        public new void Show()
        {
            if (this.Visible && this.CurrentVisibilityAnimation == null) return;

            if (this.CurrentVisibilityAnimation != null)
            {
                this.CurrentVisibilityAnimation.Cancel();
            }

            this.Visible = true;
            this.CurrentVisibilityAnimation = Animation.Tweener.Tween(this, new { Opacity = 1f }, 0.2f);
            this.CurrentVisibilityAnimation.OnComplete(() =>
            {
                this.CurrentVisibilityAnimation = null;
            });
        }

        public new void Hide()
        {
            if (!this.Visible && this.CurrentVisibilityAnimation == null) return;

            if (this.CurrentVisibilityAnimation != null)
            {
                this.CurrentVisibilityAnimation.Cancel();
            }

            this.CurrentVisibilityAnimation = Animation.Tweener.Tween(this, new { Opacity = 0f }, 0.2f);
            this.CurrentVisibilityAnimation.OnComplete(() =>
            {
                this.Visible = false;
                this.CurrentVisibilityAnimation = null;
            });
        }

        public void UpdatePosition(int x, int y)
        {
            x = (int)Math.Ceiling(x * GameService.Graphics.UIScaleMultiplier);
            y = (int)Math.Ceiling(y * GameService.Graphics.UIScaleMultiplier);
            this.Location = new Point(x, y);
        }

        public void UpdateSize(int width, int height, bool overrideHeight = false)
        {
            width = (int)Math.Ceiling(width * GameService.Graphics.UIScaleMultiplier);
            height = (int)Math.Ceiling(height * GameService.Graphics.UIScaleMultiplier);
            this.Size = new Point(width, this.Settings.SnapHeight.Value && !overrideHeight ? this.Size.Y : height);
        }

        public override void UpdateContainer(GameTime gameTime)
        {
            base.UpdateContainer(gameTime);

            TimeSinceDraw += gameTime.ElapsedGameTime;
        }

        private void InitializeBaseTexture(GraphicsDevice graphicsDevice)
        {
            if (this.Texture == null)
            {
                this.Texture = new Texture2D(graphicsDevice, 1, 1);
                this.Texture.SetData(new[] { Color.White });
            }
        }

        private void DrawLine(SpriteBatch spriteBatch, Rectangle coords, Color color)
        {
            this.InitializeBaseTexture(spriteBatch.GraphicsDevice);

            spriteBatch.DrawOnCtrl(this, this.Texture, coords, color);
        }

        protected override void DisposeControl()
        {
            this.Visible = false;

            if (this.Texture != null)
            {
                this.Texture.Dispose();
                this.Texture = null;
            }

            base.DisposeControl();
        }

    }
}
