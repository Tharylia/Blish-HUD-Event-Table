namespace Estreya.BlishHUD.EventTable.UI.Container
{
    using Blish_HUD;
    using Blish_HUD._Extensions;
    using Blish_HUD.Controls;
    using Blish_HUD.Settings;
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
        private int EVENT_HEIGHT
        {
            get
            {
                return this.Settings.EventHeight.Value;
            }
        }

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

        private DateTime DateTimeNow
        {
            get
            {
                return DateTime.Now;
            }
        }

        private DateTime EventTimeMin
        {
            get
            {
                DateTime min = this.DateTimeNow.Subtract(this.EventTimeSpan.Subtract(TimeSpan.FromMilliseconds(this.EventTimeSpan.TotalMilliseconds / 2)));
                return min;
            }
        }

        private DateTime EventTimeMax
        {
            get
            {
                DateTime max = this.DateTimeNow.Add(this.EventTimeSpan.Subtract(TimeSpan.FromMilliseconds(this.EventTimeSpan.TotalMilliseconds / 2)));
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
        private bool DebugEnabled { get => this.Settings.DebugEnabled.Value; }
        private IEnumerable<EventCategory> EventCategories { get; set; }
        private ModuleSettings Settings { get; set; }

        private Texture2D Texture { get; set; }

        private Dictionary<string, Tooltip> EventTooltips { get; set; } = new Dictionary<string, Tooltip>();

        public EventTableContainer(IEnumerable<EventCategory> eventCategories, ModuleSettings settings)
        {
            this.EventCategories = eventCategories;
            this.Settings = settings;
            this.Settings.ModuleSettingsChanged += this.Settings_ModuleSettingsChanged;
            this.Click += this.EventTableContainer_Click;

            this.BuildTooltips();
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

        private void BuildTooltips()
        {
            foreach (EventCategory category in this.EventCategories)
            {
                foreach (Event e in category.Events)
                {
                    if (this.EventTooltips.ContainsKey(e.Name))
                    {
                        continue;
                    }

                    Tooltip tooltip = new Tooltip(new UI.Views.TooltipView(e.Name, $"{e.Location}", e.Icon));

                    this.EventTooltips.Add(e.Name, tooltip);
                }
            }
        }

        private void EventTableContainer_Click(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            if (!this.Settings.CopyWaypointOnClick.Value)
            {
                return;
            }

            foreach (EventCategory eventCategory in this.EventCategories)
            {
                foreach (Event ev in eventCategory.Events)
                {
                    List<DateTime> eventOccurences = this.GetEventStartOccurences(ev);

                    if (eventOccurences.Any(eo => this.IsEventOccurenceHovered(ev, eo, this.GetMinY(ev), this.ContentRegion)))
                    {
                        ev.CopyWaypoint();
                    }
                }
            }
        }

        protected override CaptureType CapturesInput()
        {
            return CaptureType.DoNotBlock;
        }

        private int GetMinY(Event ev)
        {
            int minY = 0;

            if (this.DebugEnabled)
            {
                minY += this.EVENT_HEIGHT; // Pixel per Minute
                foreach (EventCategory eventCategory in this.EventCategories)
                {
                    foreach (Event e in eventCategory.Events)
                    {
                        minY += this.EVENT_HEIGHT;
                        if (ev == e)
                        {
                            return minY;
                        }
                    }
                }
            }

            return minY;
        }

        public override void PaintBeforeChildren(SpriteBatch spriteBatch, Rectangle bounds)
        {
            Color backgroundColor = this.Settings.BackgroundColor.Value.Id == 1 ? Color.Transparent : this.Settings.BackgroundColor.Value.Cloth.ToXnaColor();// new Color(this.Settings.BackgroundColor.Value.BaseRgb[2], this.Settings.BackgroundColor.Value.BaseRgb[1], this.Settings.BackgroundColor.Value.BaseRgb[0]);

            this.BackgroundColor = backgroundColor * this.Settings.BackgroundColorOpacity.Value;

            if (this.DebugEnabled)
            {
                spriteBatch.DrawStringOnCtrl(this, $"Pixels per Minute: {this.PixelPerMinute}", this.Font, new Rectangle(0, 0, bounds.Width, this.EVENT_HEIGHT), Color.Aqua);
            }

            int y = 0;

            foreach (EventCategory eventCategory in this.EventCategories)
            {
                foreach (Event e in eventCategory.Events)
                {
                    int minY = this.GetMinY(e);
                    SettingEntry<bool> setting = this.Settings.AllEvents.Find(eventSetting => eventSetting.EntryKey == e.Name);
                    if (!setting.Value)
                    {
                        continue;
                    }

                    List<DateTime> eventOccurences = this.GetEventStartOccurences(e);

                    foreach (DateTime eventOccurence in eventOccurences)
                    {
                        double eventWidth = this.GetEventWidth(e, eventOccurence, bounds);

                        #region Prepare Y

                        y = this.GetYFromEvent(minY, e);

                        #endregion

                        #region Prepare X

                        double x = this.GetXFromEventOccurence(eventOccurence);

                        // Start event at min 0
                        x = Math.Max(x, 0);

                        #endregion

                        #region Draw Event Rectangle

                        System.Drawing.Color colorFromEvent = string.IsNullOrWhiteSpace(e.Color) ? System.Drawing.Color.White : System.Drawing.ColorTranslator.FromHtml(e.Color);

                        Color color = new Color(colorFromEvent.R, colorFromEvent.G, colorFromEvent.B);

                        Rectangle eventTexturePosition = new Rectangle((int)Math.Floor(x), y, (int)Math.Floor(eventWidth), this.EVENT_HEIGHT);

                        this.DrawRectangle(spriteBatch, eventTexturePosition, color * this.Settings.Opacity.Value, this.Settings.DrawEventBorder.Value ? 1 : 0, Color.Black);

                        #endregion

                        #region Draw Event Name

                        Rectangle eventTextPosition = Rectangle.Empty;
                        if (!string.IsNullOrWhiteSpace(e.Name))
                        {
                            string eventName = this.GetLongestEventName(e, eventTexturePosition.Width);
                            eventTextPosition = new Rectangle(eventTexturePosition.X + 5, eventTexturePosition.Y + 5, (int)Math.Floor(this.MeasureStringWidth(eventName)), eventTexturePosition.Height - 10);
                            spriteBatch.DrawStringOnCtrl(this, eventName, this.Font, eventTextPosition, Color.Black);
                        }

                        #endregion

                        #region Draw Event Remaining Time

                        bool running = eventOccurence <= this.DateTimeNow && eventOccurence.AddMinutes(e.Duration) > this.DateTimeNow;
                        if (running)
                        {
                            DateTime end = eventOccurence.AddMinutes(e.Duration);
                            TimeSpan timeRemaining = end.Subtract(this.DateTimeNow);
                            string timeRemainingString = timeRemaining.Hours > 0 ? timeRemaining.ToString("hh\\:mm\\:ss") : timeRemaining.ToString("mm\\:ss");
                            int timeRemainingWidth = (int)Math.Ceiling(this.MeasureStringWidth(timeRemainingString));
                            int timeRemainingX = eventTexturePosition.X + ((eventTexturePosition.Width / 2) - (timeRemainingWidth / 2));
                            if (timeRemainingX < eventTextPosition.X + eventTextPosition.Width)
                            {
                                timeRemainingX = eventTextPosition.X + eventTextPosition.Width + 10;
                            }

                            Rectangle eventTimeRemainingPosition = new Rectangle(timeRemainingX, eventTexturePosition.Y + 5, timeRemainingWidth, eventTexturePosition.Height - 10);

                            if (eventTimeRemainingPosition.X + eventTimeRemainingPosition.Width <= eventTexturePosition.X + eventTexturePosition.Width)
                            {
                                // Only draw if it fits in event bounds
                                spriteBatch.DrawStringOnCtrl(this, timeRemainingString, this.Font, eventTimeRemainingPosition, Color.Black);
                            }
                        }

                        #endregion
                    }

                    #region Draw Tooltip

                    if (this.Settings.ShowTooltips.Value && this.MouseOver)
                    {
                        IEnumerable<IGrouping<string, Event>> groups = eventCategory.Events.GroupBy(ev => ev.Name);
                        IEnumerable<Event> eventFilter = groups.SelectMany(g => g.Select(innerG => innerG)).Where(ev => this.GetEventStartOccurences(ev).Count > 0);
                        IEnumerable<Event> events = eventCategory.ShowCombined ? eventFilter : eventCategory.Events;

                        if (!eventCategory.ShowCombined || events.Contains(e))
                        {
                            if (this.EventTooltips.TryGetValue(e.Name, out Tooltip tooltip))
                            {
                                bool isMouseOver = eventOccurences.Any(eo =>
                                {
                                    return this.IsEventOccurenceHovered(e, eo, minY, bounds);
                                });

                                if (isMouseOver && !tooltip.Visible)
                                {
                                    Debug.WriteLine($"Show Tooltip for Event: {e.Name}");
                                    tooltip.Show(0, 0);
                                }
                                else if (!isMouseOver && tooltip.Visible)
                                {
                                    Debug.WriteLine($"Hide Tooltip for Event: {e.Name}");
                                    tooltip.Hide();
                                }
                            }
                        }
                    }

                    #endregion
                }
            }

            if (this.Settings.SnapHeight.Value)
            {
                this.Size = new Point(bounds.Width, y + this.EVENT_HEIGHT);
            }

            this.DrawLine(spriteBatch, new Rectangle(this.Size.X / 2, 0, 2, this.Size.Y), Color.LightGray);
        }

        private bool IsEventOccurenceHovered(Event e, DateTime eo, int minY, Rectangle bounds)
        {
            double x = this.GetXFromEventOccurence(eo);
            int eo_y = this.GetYFromEvent(minY, e);
            double width = this.GetEventWidth(e, eo, bounds);

            x = Math.Max(x, 0);

            return (this.RelativeMousePosition.X >= x && this.RelativeMousePosition.X < x + width) && (this.RelativeMousePosition.Y >= eo_y && this.RelativeMousePosition.Y < eo_y + this.EVENT_HEIGHT);
        }

        private string GetLongestEventName(Event e, int maxSize)
        {
            float size = this.MeasureStringWidth(e.Name);

            if (size <= maxSize)
            {
                return e.Name;
            }

            for (int i = 0; i < e.Name.Length; i++)
            {
                string name = e.Name.Substring(0, e.Name.Length - i);
                size = this.MeasureStringWidth(name);

                if (size <= maxSize)
                {
                    return name;
                }
            }

            return "...";
        }

        private float MeasureStringWidth(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            return this.Font.MeasureString(text).Width + 10; // TODO: Why is +10 needed?
        }

        private double GetEventWidth(Event e, DateTime eventOccurence, Rectangle bounds)
        {
            double eventWidth = e.Duration * this.PixelPerMinute;

            double x = this.GetXFromEventOccurence(eventOccurence);

            if (x < 0)
            {
                eventWidth -= Math.Abs(x);
            }

            // Only draw event until end of form
            eventWidth = Math.Min(eventWidth, bounds.Width);

            return eventWidth;
        }

        private List<DateTime> GetEventStartOccurences(Event e)
        {
            DateTime zero = new DateTime(this.DateTimeNow.Year, this.DateTimeNow.Month, this.DateTimeNow.Day - (e.Repeat.TotalMinutes == 0 ? 0 : 1), 0, 0, 0);

            TimeSpan offset = e.Offset;
            offset = offset.Add(TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now));

            DateTime eventStart = zero.Add(offset);

            List<DateTime> startOccurences = new List<DateTime>();

            while (eventStart < this.EventTimeMax)
            {

                bool startAfterMin = eventStart > this.EventTimeMin;
                bool endAfterMin = eventStart.AddMinutes(e.Duration) > this.EventTimeMin;

                if ((startAfterMin || endAfterMin) && eventStart < this.EventTimeMax)
                {
                    startOccurences.Add(eventStart);
                }

                if (e.Repeat.TotalMinutes == 0)
                {
                    eventStart = eventStart.Add(TimeSpan.FromDays(1));
                }
                else
                {
                    eventStart = eventStart.Add(e.Repeat);
                }
            }

            return startOccurences;
        }

        private int GetYFromEvent(int minY, Event ev)
        {
            int y = minY;
            foreach (EventCategory categories in this.EventCategories)
            {
                bool anyFromCategoryRendered = false;
                foreach (Event e in categories.Events)
                {
                    SettingEntry<bool> setting = this.Settings.AllEvents.Find(eventSetting => eventSetting.EntryKey == e.Name);
                    if (!setting.Value)
                    {
                        continue;
                    }

                    anyFromCategoryRendered = true;

                    if (e.Name != ev.Name)
                    {
                        continue;
                    }

                    return y;
                }

                if (anyFromCategoryRendered)
                {
                    y += this.EVENT_HEIGHT;
                }
            }

            return y;
        }

        private double GetXFromEventOccurence(DateTime start)
        {
            double minutesSinceMin = start.Subtract(this.EventTimeMin).TotalMinutes;
            return minutesSinceMin * this.PixelPerMinute;
        }

        public new void Show()
        {
            if (this.Visible) return;

            this.Visible = true;
            Animation.Tweener.Tween(this, new { Opacity = 1f }, 0.2f);
        }

        public new void Hide()
        {
            if (!this.Visible) return;

            Tween tween = Animation.Tweener.Tween(this, new { Opacity = 0f }, 0.2f);
            tween.OnComplete(() => this.Visible = false);
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
        }

        private void InitializeBaseTexture()
        {
            if (this.Texture == null)
            {
                this.Texture = new Texture2D(GameService.Graphics.GraphicsDevice, 1, 1);
                this.Texture.SetData(new[] { Color.White });
            }
        }

        private void DrawRectangle(SpriteBatch spriteBatch, Rectangle coords, Color color)
        {
            this.InitializeBaseTexture();

            spriteBatch.DrawOnCtrl(this, this.Texture, coords, color);
        }

        private void DrawLine(SpriteBatch spriteBatch, Rectangle coords, Color color)
        {
            this.InitializeBaseTexture();

            spriteBatch.DrawOnCtrl(this, this.Texture, coords, color);
        }

        private void DrawRectangle(SpriteBatch spriteBatch, Rectangle coords, Color color, int borderSize, Color borderColor)
        {
            this.InitializeBaseTexture();

            this.DrawRectangle(spriteBatch, coords, color);

            spriteBatch.DrawOnCtrl(this, this.Texture, new Rectangle(coords.Left, coords.Top, coords.Width - borderSize, borderSize), borderColor);
            spriteBatch.DrawOnCtrl(this, this.Texture, new Rectangle(coords.Right - borderSize, coords.Top, borderSize, coords.Height), borderColor);
            spriteBatch.DrawOnCtrl(this, this.Texture, new Rectangle(coords.Left, coords.Bottom - borderSize, coords.Width, borderSize), borderColor);
            spriteBatch.DrawOnCtrl(this, this.Texture, new Rectangle(coords.Left, coords.Top, borderSize, coords.Height), borderColor);
        }

        protected override void DisposeControl()
        {
            this.Visible = true;

            if (this.Texture != null)
            {
                this.Texture.Dispose();
                this.Texture = null;
            }

            if (this.EventTooltips != null)
            {
                foreach (KeyValuePair<string, Tooltip> tooltip in this.EventTooltips)
                {
                    tooltip.Value.Dispose();
                }

                this.EventTooltips.Clear();
                this.EventTooltips = null;
            }

            base.DisposeControl();
        }

    }
}
