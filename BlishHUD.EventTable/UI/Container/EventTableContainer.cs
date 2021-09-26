namespace BlishHUD.EventTable.UI.Container
{
    using Blish_HUD;
    using Blish_HUD.Common.UI.Views;
    using Blish_HUD.Controls;
    using Blish_HUD.Settings;
    using BlishHUD.EventTable.Models;
    using Glide;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended.BitmapFonts;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class EventTableContainer : Blish_HUD.Controls.Container
    {
        private const int EVENT_HEIGHT = 30;
        private TimeSpan EventTimeSpan = TimeSpan.FromHours(2);

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
                DateTime min = DateTimeNow.Subtract(EventTimeSpan.Subtract(TimeSpan.FromMinutes(EventTimeSpan.TotalMinutes / 2)));
                return min;
            }
        }

        private DateTime EventTimeMax
        {
            get
            {
                DateTime max = DateTimeNow.Add(EventTimeSpan.Subtract(TimeSpan.FromMinutes(EventTimeSpan.TotalMinutes / 2)));
                return max;
            }
        }

        private BitmapFont Font { get; } = Content.DefaultFont18;

        private int PixelPerMinute
        {
            get
            {
                int pixels = this.Size.X;

                int pixelPerMinute = (int)Math.Round(pixels / EventTimeSpan.TotalMinutes);

                return pixelPerMinute;
            }
        }

        private double ElapsedSeconds { get; set; }
        private bool DebugEnabled { get; set; }
        private IEnumerable<EventCategory> EventCategories { get; set; }
        private ModuleSettings Settings { get; set; }

        private Texture2D Texture { get; set; }

        private Dictionary<string, Tooltip> Tooltips { get; set; } = new Dictionary<string, Tooltip>();

        public EventTableContainer(IEnumerable<EventCategory> eventCategories, ModuleSettings settings)
        {
            this.EventCategories = eventCategories;
            this.Settings = settings;
            this.MouseMoved += EventTableContainer_MouseMoved;

            foreach (EventCategory category in eventCategories)
            {
                foreach (Event e in category.Events)
                {
                    if (Tooltips.ContainsKey(e.Name)) continue;

                    Tooltip tooltip = new Tooltip(new UI.Views.TooltipView(e.Name, "Test Description", e.Icon));

                    this.Tooltips.Add(e.Name, tooltip);
                }
            }
        }

        private void EventTableContainer_MouseMoved(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            this.HandleTooltip(e);
        }

        private void HandleTooltip(Blish_HUD.Input.MouseEventArgs me)
        {
            if (!MouseOver) return;


            Debug.WriteLine("Mouse Inside");

            foreach (EventCategory category in EventCategories)
            {
                foreach (Event e in category.Events)
                {
                    Debug.WriteLine($"Check Event: {e.Name}");
                    if (this.Tooltips.TryGetValue(e.Name, out var tooltip))
                    {
                        bool shown = false;
                        foreach (DateTime eventOccurence in GetEventStartOccurences(e))
                        {
                            int x = GetXFromEventOccurence(eventOccurence);
                            int y = GetYFromEvent(0, e);
                            int eventWidth = GetEventWidth(e, eventOccurence, this.ContentRegion);
                            bool mouseOver = this.MouseOver;
                            /*Debug.WriteLine("X: " + x + " | AX: " + me.MousePosition.X);
                            Debug.WriteLine("Y: " + y + " | AY: " + me.MousePosition.Y);*/
                            if ((RelativeMousePosition.X >= x && RelativeMousePosition.X < x + eventWidth) && (RelativeMousePosition.Y >= y && RelativeMousePosition.Y < y + EVENT_HEIGHT))
                            {
                                Debug.WriteLine($"Event matched!");
                                shown = true;
                                if (!tooltip.Visible)
                                    tooltip.Show(50, 50);

                                return;
                            }
                        }

                        if (!shown)
                        {
                            tooltip.Hide();
                        }
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
            Debug.WriteLine($"Paint: {ElapsedSeconds}");
            base.PaintBeforeChildren(spriteBatch, bounds);
            int minY = 0;

            if (DebugEnabled)
            {
                spriteBatch.DrawStringOnCtrl(this, $"Elapsed Seconds: {Math.Round(ElapsedSeconds, 2)}", Content.DefaultFont32, new Rectangle(0, minY, bounds.Width, EVENT_HEIGHT), Color.Aqua);
                minY += EVENT_HEIGHT;
            }

            int y = minY;

            foreach (EventCategory eventCategory in EventCategories)
            {
                string group = eventCategory.Key;

                bool anyEventFromGroupRendered = false;

                foreach (Event e in eventCategory.Events)
                {
                    SettingEntry<bool> setting = this.Settings.AllEvents.Find(eventSetting => eventSetting.EntryKey == e.Name);
                    if (!setting.Value)
                    {
                        continue;
                    }
                    anyEventFromGroupRendered = true;

                    List<DateTime> eventOccurences = GetEventStartOccurences(e);

                    foreach (DateTime eventOccurence in eventOccurences)
                    {
                        int eventWidth = e.Duration * PixelPerMinute;

                        if (DebugEnabled && eventOccurences.IndexOf(eventOccurence) == 0)
                        {
                            minY += EVENT_HEIGHT;
                            y = minY;
                        }

                        #region Prepare X

                        int x = GetXFromEventOccurence(eventOccurence);

                        if (x < 0)
                        {
                            eventWidth -= Math.Abs(x);
                        }

                        // Only draw event until end of form
                        eventWidth = Math.Min(eventWidth, bounds.Width - x);

                        // Start event at min 0
                        x = Math.Max(x, 0);

                        #endregion

                        #region Draw Event Rectangle

                        System.Drawing.Color colorFromEvent = string.IsNullOrWhiteSpace(e.Color) ? System.Drawing.Color.White : System.Drawing.ColorTranslator.FromHtml(e.Color);

                        Color color = new Color(colorFromEvent.R, colorFromEvent.G, colorFromEvent.B);

                        Rectangle eventTexturePosition = new Rectangle(x, y, eventWidth, EVENT_HEIGHT);

                        DrawRectangle(spriteBatch, eventTexturePosition, color, 2, Color.Black);

                        #endregion

                        #region Draw Event Name

                        string eventName = GetLongestEventName(e, eventWidth);
                        Rectangle eventTextPosition = new Rectangle(eventTexturePosition.X + 5, eventTexturePosition.Y + 5, (int)Math.Floor(MeasureStringWidth(eventName)), eventTexturePosition.Height - 10);
                        spriteBatch.DrawStringOnCtrl(this, eventName, Font, eventTextPosition, Color.Black);

                        #endregion

                        #region Draw Event Remaining Time

                        bool running = eventOccurence <= DateTimeNow && eventOccurence.AddMinutes(e.Duration) > DateTimeNow;
                        if (running)
                        {
                            DateTime end = eventOccurence.AddMinutes(e.Duration);
                            TimeSpan timeRemaining = end.Subtract(DateTimeNow);
                            string timeRemainingString = timeRemaining.Hours > 0 ? timeRemaining.ToString("hh\\:mm\\:ss") : timeRemaining.ToString("mm\\:ss");
                            int timeRemainingWidth = (int)Math.Floor(MeasureStringWidth(timeRemainingString));
                            int timeRemainingX = eventTexturePosition.X + ((eventTexturePosition.Width / 2) - (timeRemainingWidth / 2));
                            if (timeRemainingX < eventTextPosition.X + eventTextPosition.Width)
                            {
                                timeRemainingX = eventTextPosition.X + eventTextPosition.Width + 10;
                            }

                            Rectangle eventTimeRemainingPosition = new Rectangle(timeRemainingX, eventTexturePosition.Y + 5, timeRemainingWidth, eventTexturePosition.Height - 10);
                            spriteBatch.DrawStringOnCtrl(this, timeRemainingString, Font, eventTimeRemainingPosition, Color.Black);
                        }

                        #endregion
                    }
                }

                if (anyEventFromGroupRendered)
                    y += EVENT_HEIGHT;
            }

            if (this.Settings.SnapHeight.Value)
            {
                this.UpdateSize(bounds.Width, y, true);
            }

            DrawLine(spriteBatch, new Rectangle((this.Size.X / 2) + PixelPerMinute - 1, 0, 2, this.Size.Y), Color.LightGray);
        }

        private string GetLongestEventName(Event e, int maxSize)
        {
            float size = MeasureStringWidth(e.Name);

            if (size <= maxSize) return e.Name;

            if (e.Name.Length <= 3) return "...";

            for (int i = 0; i < e.Name.Length - 3; i++)
            {
                size = MeasureStringWidth(e.Name.Substring(0, e.Name.Length - 3 - i));
                if (size <= maxSize) return e.Name.Substring(0, e.Name.Length - 3 - i);
            }

            return "...";
        }

        private float MeasureStringWidth(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            var lastGlyph = Font.GetGlyphs(text).Last();

            return lastGlyph.Position.X + (lastGlyph.FontRegion?.Width ?? 0);
        }

        private int GetEventWidth(Event e, DateTime eventOccurence, Rectangle bounds)
        {
            int eventWidth = e.Duration * PixelPerMinute;

            int x = GetXFromEventOccurence(eventOccurence);

            if (x < 0)
            {
                eventWidth -= Math.Abs(x);
            }

            // Only draw event until end of form
            eventWidth = Math.Min(eventWidth, bounds.Width - x);

            return eventWidth;
        }

        private List<DateTime> GetEventStartOccurences(Event e)
        {
            DateTime zero = new DateTime(DateTimeNow.Year, DateTimeNow.Month, DateTimeNow.Day - 1, 0, 0, 0);

            DateTime eventStart = zero.Add(e.Offset);

            List<DateTime> startOccurences = new List<DateTime>();

            while (eventStart < EventTimeMax)
            {

                bool startAfterMin = eventStart > EventTimeMin;
                bool endAfterMin = eventStart.AddMinutes(e.Duration) > EventTimeMin;

                if ((startAfterMin || endAfterMin) && eventStart < EventTimeMax)
                {
                    startOccurences.Add(eventStart);
                }

                if (e.Repeat.TotalMinutes == 0)
                {
                    eventStart = eventStart.Add(TimeSpan.FromDays(1));
                }else

                eventStart = eventStart.Add(e.Repeat);
            }

            return startOccurences;
        }

        private int GetYFromEvent(int minY, Event ev)
        {
            int y = minY;
            foreach (var categories in EventCategories)
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

                    if (e.Name != ev.Name) continue;

                    return y;
                }

                if (anyFromCategoryRendered)
                    y += EVENT_HEIGHT;
            }

            return y;
        }

        private int GetXFromEventOccurence(DateTime start)
        {
            int minutesSinceMin = (int)Math.Floor(start.Subtract(EventTimeMin).TotalMinutes);
            return minutesSinceMin * PixelPerMinute;
        }

        public void SetDebugMode(bool active)
        {
            this.DebugEnabled = active;
        }

        public new Tween Show()
        {
            this.Visible = true;
            return Animation.Tweener.Tween(this, new { Opacity = 1f }, 0.2f);
        }

        public new Tween Hide()
        {
            Tween tween = Animation.Tweener.Tween(this, new { Opacity = 0f }, 0.2f);
            return tween.OnComplete(() => this.Visible = false);
        }

        public void UpdatePosition(int x, int y)
        {
            this.Location = new Point(x, y);
        }

        public void UpdateSize(int width, int height, bool overrideHeight = false)
        {
            this.Size = new Point(width, this.Settings.SnapHeight.Value && !overrideHeight ? this.Size.Y : height);
        }

        public void UpdateEventTimeSpan(TimeSpan timespan)
        {
            if (timespan == null) return;

            this.EventTimeSpan = timespan;
        }

        public override void UpdateContainer(GameTime gameTime)
        {
            base.UpdateContainer(gameTime);

            ElapsedSeconds += gameTime.ElapsedGameTime.TotalSeconds;
        }

        private void DrawRectangle(SpriteBatch spriteBatch, Rectangle coords, Color color)
        {
            if (Texture == null)
            {
                Texture = new Texture2D(GameService.Graphics.GraphicsDevice, 1, 1);
                Texture.SetData(new[] { Color.White });
            }

            spriteBatch.DrawOnCtrl(this, Texture, coords, color);
        }

        private void DrawLine(SpriteBatch spriteBatch, Rectangle coords, Color color)
        {
            if (Texture == null)
            {
                Texture = new Texture2D(GameService.Graphics.GraphicsDevice, 1, 1);
                Texture.SetData(new[] { Color.White });
            }

            spriteBatch.DrawOnCtrl(this, Texture, coords, color);
        }

        private void DrawRectangle(SpriteBatch spriteBatch, Rectangle coords, Color color, int borderSize, Color borderColor)
        {
            if (Texture == null)
            {
                Texture = new Texture2D(GameService.Graphics.GraphicsDevice, 1, 1);
                Texture.SetData(new[] { Color.White });
            }

            DrawRectangle(spriteBatch, coords, color);

            spriteBatch.DrawOnCtrl(this, Texture, new Rectangle(coords.Left, coords.Top, coords.Width - borderSize, borderSize), borderColor);
            spriteBatch.DrawOnCtrl(this, Texture, new Rectangle(coords.Right - borderSize, coords.Top, borderSize, coords.Height), borderColor);
            spriteBatch.DrawOnCtrl(this, Texture, new Rectangle(coords.Left, coords.Bottom - borderSize, coords.Width, borderSize), borderColor);
            spriteBatch.DrawOnCtrl(this, Texture, new Rectangle(coords.Left, coords.Top, borderSize, coords.Height), borderColor);
        }

    }
}
