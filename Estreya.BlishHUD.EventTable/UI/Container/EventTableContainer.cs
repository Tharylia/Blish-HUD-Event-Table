namespace Estreya.BlishHUD.EventTable.UI.Container
{
    using Blish_HUD;
    using Blish_HUD._Extensions;
    using Blish_HUD.Controls;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Extensions;
    using Estreya.BlishHUD.EventTable.Models;
    using Estreya.BlishHUD.EventTable.Utils;
    using Glide;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public class EventTableContainer : Blish_HUD.Controls.Container
    {
        private TimeSpan TimeSinceDraw { get; set; }
        private bool _currentVisibilityDirection = false;

        private static bool CursorVisible => GameService.Input.Mouse.CursorIsVisible;

        public new bool Visible
        {
            get
            {
                if (_currentVisibilityDirection && this.CurrentVisibilityAnimation != null)
                {
                    return true;
                }

                if (!_currentVisibilityDirection && this.CurrentVisibilityAnimation != null)
                {
                    return false;
                }

                return base.Visible;
            }
            set
            {
                base.Visible = value;
            }
        }

        private double PixelPerMinute
        {
            get
            {
                int pixels = this.Size.X;

                double pixelPerMinute = pixels / EventTableModule.ModuleInstance.EventTimeSpan.TotalMinutes;

                return pixelPerMinute;
            }
        }

        private Tween CurrentVisibilityAnimation { get; set; }

        private Texture2D Texture { get; set; }

        public EventTableContainer()
        {
            this.LeftMouseButtonPressed += this.EventTableContainer_Click;
            this.RightMouseButtonPressed += this.EventTableContainer_Click;
            this.MouseMoved += this.EventTableContainer_MouseMoved;
        }

        private void EventTableContainer_MouseMoved(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            if (!CursorVisible) return;

            var mouseEventArgs = new Input.MouseEventArgs(this.RelativeMousePosition, e.IsDoubleClick, e.EventType);
            foreach (EventCategory eventCategory in EventTableModule.ModuleInstance.EventCategories)
            {
                foreach (Event ev in eventCategory.Events)
                {
                    if (ev.IsHovered(EventTableModule.ModuleInstance.EventCategories, eventCategory, EventTableModule.ModuleInstance.DateTimeNow, EventTableModule.ModuleInstance.EventTimeMax, EventTableModule.ModuleInstance.EventTimeMin, this.ContentRegion, RelativeMousePosition, PixelPerMinute, EventTableModule.ModuleInstance.EventHeight, EventTableModule.ModuleInstance.Debug))
                    {
                        ev.HandleHover(sender, mouseEventArgs, this.PixelPerMinute);
                    }
                    else
                    {
                        ev.HandleNonHover(sender, mouseEventArgs);
                    }
                }
            }
        }

        private void EventTableContainer_Click(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            if (!CursorVisible) return;

            foreach (EventCategory eventCategory in EventTableModule.ModuleInstance.EventCategories)
            {
                foreach (Event ev in eventCategory.Events)
                {
                    if (ev.IsHovered(EventTableModule.ModuleInstance.EventCategories, eventCategory, EventTableModule.ModuleInstance.DateTimeNow, EventTableModule.ModuleInstance.EventTimeMax, EventTableModule.ModuleInstance.EventTimeMin, this.ContentRegion, RelativeMousePosition, PixelPerMinute, EventTableModule.ModuleInstance.EventHeight, EventTableModule.ModuleInstance.Debug))
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

            List<EventCategory> eventCategories = EventTableModule.ModuleInstance.EventCategories;

            Color backgroundColor = Color.Transparent;
            if (EventTableModule.ModuleInstance.ModuleSettings.BackgroundColor.Value != null && EventTableModule.ModuleInstance.ModuleSettings.BackgroundColor.Value.Id != 1)
            {
                backgroundColor = EventTableModule.ModuleInstance.ModuleSettings.BackgroundColor.Value.Cloth.ToXnaColor();
            }

            this.BackgroundColor = backgroundColor * EventTableModule.ModuleInstance.ModuleSettings.BackgroundColorOpacity.Value;

            int y = 0;

            bool anyCategoryDrawn = false;

            foreach (EventCategory eventCategory in eventCategories)
            {
                List<KeyValuePair<DateTime, Event>> eventStarts = eventCategory.GetEventOccurences(EventTableModule.ModuleInstance.DateTimeNow, EventTableModule.ModuleInstance.EventTimeMax, EventTableModule.ModuleInstance.EventTimeMin, EventTableModule.ModuleInstance.ModuleSettings.UseFiller.Value);

                var groups = eventStarts.GroupBy(ev => ev.Value);

                bool anyEventDrawn = false;

                foreach (var group in groups)
                {
                    var starts = group.Select(g => g.Key).ToList();
                    anyEventDrawn = starts.Count > 0;
                    group.Key.Draw(spriteBatch, bounds, this, this.Texture, eventCategories.ToList(), eventCategory, this.PixelPerMinute, EventTableModule.ModuleInstance.EventHeight, EventTableModule.ModuleInstance.DateTimeNow, EventTableModule.ModuleInstance.EventTimeMin, EventTableModule.ModuleInstance.EventTimeMax, EventTableModule.ModuleInstance.Font, starts);
                }

                if (anyEventDrawn)
                {
                    anyCategoryDrawn = true;
                    y = groups.ElementAt(0).Key.GetYPosition(eventCategories, eventCategory, EventTableModule.ModuleInstance.EventHeight, EventTableModule.ModuleInstance.Debug);
                }
            }

            this.Size = new Point(bounds.Width, y + (anyCategoryDrawn ? EventTableModule.ModuleInstance.EventHeight : 0));

            float middleLineX = this.Size.X * EventTableModule.ModuleInstance.EventTimeSpanRatio;
            this.DrawLine(spriteBatch, new RectangleF(middleLineX, 0, 2, this.Size.Y), Color.LightGray);

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

            this._currentVisibilityDirection = true;
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

            this._currentVisibilityDirection = false;
            this.CurrentVisibilityAnimation = Animation.Tweener.Tween(this, new { Opacity = 0f }, 0.2f);
            this.CurrentVisibilityAnimation.OnComplete(() =>
            {
                this.Visible = false;
                this.CurrentVisibilityAnimation = null;
            });
        }

        public void UpdatePosition(int x, int y)
        {
            bool buildFromBottom = EventTableModule.ModuleInstance.ModuleSettings.BuildDirection.Value == BuildDirection.Bottom;

            if (buildFromBottom)
            {
                this.Location = new Point(x, y - this.Height);
            }
            else
            {
                this.Location = new Point(x, y);
            }
        }

        public void UpdateSize(int width, int height, bool overrideHeight = false)
        {
            if (height == -1)
            {
                height = this.Size.Y;
            }

            this.Size = new Point(width, /*this.Settings.SnapHeight.Value && */!overrideHeight ? this.Size.Y : height);
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

        private void DrawLine(SpriteBatch spriteBatch, RectangleF coords, Color color)
        {
            this.InitializeBaseTexture(spriteBatch.GraphicsDevice);

            spriteBatch.DrawOnCtrl(this, this.Texture, coords, color);
        }

        protected override void DisposeControl()
        {
            this.Hide();

            if (this.Texture != null)
            {
                this.Texture.Dispose();
                this.Texture = null;
            }

            base.DisposeControl();
        }

    }
}
