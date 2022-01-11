namespace Estreya.BlishHUD.EventTable.Models
{
    using Blish_HUD;
    using Blish_HUD._Extensions;
    using Blish_HUD.Contexts;
    using Blish_HUD.Controls;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Helpers;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended.BitmapFonts;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [Serializable]
    public class Event
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("offset"), JsonConverter(typeof(Json.TimeSpanJsonConverter), "dd\\.hh\\:mm", new string[] { "hh\\:mm" })]
        public TimeSpan Offset { get; set; }

        [JsonProperty("repeat"), JsonConverter(typeof(Json.TimeSpanJsonConverter), "dd\\.hh\\:mm", new string[] { "dd\\.hh\\:mm", "hh\\:mm" })]
        public TimeSpan Repeat { get; set; }

        [JsonProperty("diffculty")]
        public EventDifficulty Difficulty { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("waypoint")]
        public string Waypoint { get; set; }

        [JsonProperty("wiki")]
        public string Wiki { get; set; }

        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }
        [JsonProperty("color")]
        public string Color { get; set; }

        [JsonProperty("filler")]
        internal bool Filler { get; set; }
        [JsonProperty("api")]
        internal string APICode { get; set; }

        [JsonIgnore]
        private Tooltip _tooltip;

        [JsonIgnore]
        private Tooltip Tooltip
        {
            get
            {
                if (_tooltip == null)
                {
                    _tooltip = new Tooltip(new UI.Views.TooltipView(this.Name, $"{this.Location}", this.Icon));
                }

                return _tooltip;
            }
        }

        public void Draw(SpriteBatch spriteBatch, Rectangle bounds, Control control, Texture2D baseTexture, List<EventCategory> allCategories, EventCategory currentCategory, double pixelPerMinute, int eventHeight, DateTime now, DateTime min, DateTime max, BitmapFont font, List<DateTime> startOccurences)
        {
            foreach (var eventStart in startOccurences)
            {
                double width = this.GetWidth(eventStart, min, bounds, pixelPerMinute);
                int y = this.GetYPosition(allCategories, EventTableModule.ModuleInstance.ModuleSettings.AllEvents, currentCategory, eventHeight, EventTableModule.ModuleInstance.Debug);
                double x = this.GetXPosition(eventStart, min, pixelPerMinute);
                x = Math.Max(x, 0);

                #region Draw Event Rectangle

                Color color = Microsoft.Xna.Framework.Color.Transparent;

                if (!this.Filler)
                {
                    System.Drawing.Color colorFromEvent = string.IsNullOrWhiteSpace(this.Color) ? System.Drawing.Color.White : System.Drawing.ColorTranslator.FromHtml(this.Color);
                    color = new Color(colorFromEvent.R, colorFromEvent.G, colorFromEvent.B);
                }


                Rectangle eventTexturePosition = new Rectangle((int)Math.Floor(x), y, (int)Math.Ceiling(width), eventHeight);
                bool drawBorder = !this.Filler && EventTableModule.ModuleInstance.ModuleSettings.DrawEventBorder.Value;

                this.DrawRectangle(spriteBatch, control, baseTexture, eventTexturePosition, color * EventTableModule.ModuleInstance.ModuleSettings.Opacity.Value, drawBorder ? 1 : 0, Microsoft.Xna.Framework.Color.Black);

                #endregion

                Color textColor = Microsoft.Xna.Framework.Color.Black;
                if (this.Filler)
                {
                    textColor = EventTableModule.ModuleInstance.ModuleSettings.FillerTextColor.Value.Id == 1 ? textColor : EventTableModule.ModuleInstance.ModuleSettings.FillerTextColor.Value.Cloth.ToXnaColor();
                }
                else
                {
                    textColor = EventTableModule.ModuleInstance.ModuleSettings.TextColor.Value.Id == 1 ? textColor : EventTableModule.ModuleInstance.ModuleSettings.TextColor.Value.Cloth.ToXnaColor();
                }

                #region Draw Event Name

                Rectangle eventTextPosition = Rectangle.Empty;
                if (!string.IsNullOrWhiteSpace(this.Name) && (!this.Filler || (this.Filler && EventTableModule.ModuleInstance.ModuleSettings.UseFillerEventNames.Value)))
                {
                    string eventName = this.GetLongestEventName(eventTexturePosition.Width, font);
                    eventTextPosition = new Rectangle(eventTexturePosition.X + 5, eventTexturePosition.Y + 5, (int)Math.Floor(this.MeasureStringWidth(eventName, font)), eventTexturePosition.Height - 10);


                    spriteBatch.DrawStringOnCtrl(control, eventName, font, eventTextPosition, textColor);
                }

                #endregion

                #region Draw Event Remaining Time

                bool running = eventStart <= now && eventStart.AddMinutes(this.Duration) > now;
                if (running)
                {
                    DateTime end = eventStart.AddMinutes(this.Duration);
                    TimeSpan timeRemaining = end.Subtract(now);
                    string timeRemainingString = timeRemaining.Hours > 0 ? timeRemaining.ToString("hh\\:mm\\:ss") : timeRemaining.ToString("mm\\:ss");
                    int timeRemainingWidth = (int)Math.Ceiling(this.MeasureStringWidth(timeRemainingString, font));
                    int timeRemainingX = eventTexturePosition.X + ((eventTexturePosition.Width / 2) - (timeRemainingWidth / 2));
                    if (timeRemainingX < eventTextPosition.X + eventTextPosition.Width)
                    {
                        timeRemainingX = eventTextPosition.X + eventTextPosition.Width + 10;
                    }

                    Rectangle eventTimeRemainingPosition = new Rectangle(timeRemainingX, eventTexturePosition.Y + 5, timeRemainingWidth, eventTexturePosition.Height - 10);

                    if (eventTimeRemainingPosition.X + eventTimeRemainingPosition.Width <= eventTexturePosition.X + eventTexturePosition.Width)
                    {
                        // Only draw if it fits in event bounds
                        spriteBatch.DrawStringOnCtrl(control, timeRemainingString, font, eventTimeRemainingPosition, textColor);
                    }
                }

                #endregion

                #region Draw Cross out

                if (!this.Filler && !string.IsNullOrWhiteSpace(this.APICode))
                {
                    if (EventTableModule.ModuleInstance.CompletedWorldbosses.Contains(this.APICode))
                    {
                        this.DrawCrossOut(spriteBatch, control, baseTexture, eventTexturePosition, Microsoft.Xna.Framework.Color.Red);
                    }
                }
                #endregion

            }
        }

        public void DrawTooltip(Control control, Rectangle bounds, List<EventCategory> allCategories, EventCategory currentCategory, double pixelPerMinute, int eventHeight, DateTime now, DateTime min, DateTime max)
        {
            #region Draw Tooltip

            if (EventTableModule.ModuleInstance.ModuleSettings.ShowTooltips.Value && !this.Filler && control.MouseOver)
            {
                IEnumerable<IGrouping<string, Event>> groups = currentCategory.Events.GroupBy(ev => ev.Name);
                IEnumerable<Event> eventFilter = groups.SelectMany(g => g.Select(innerG => innerG)).Where(ev => ev.GetStartOccurences(now, max, min).Count > 0);
                IEnumerable<Event> events = currentCategory.ShowCombined ? eventFilter : currentCategory.Events;

                if (!currentCategory.ShowCombined || events.Contains(this))
                {
                    bool isMouseOver = this.IsHovered(allCategories, EventTableModule.ModuleInstance.ModuleSettings.AllEvents, currentCategory, now, max, min, bounds, control.RelativeMousePosition, pixelPerMinute, eventHeight, EventTableModule.ModuleInstance.Debug);

                    if (isMouseOver && !this.Tooltip.Visible)
                    {
                        Debug.WriteLine($"Show Tooltip for Event: {this.Name}");
                        this.Tooltip.Show(0, 0);
                    }
                    else if (!isMouseOver && this.Tooltip.Visible)
                    {
                        Debug.WriteLine($"Hide Tooltip for Event: {this.Name}");
                        this.Tooltip.Hide();
                    }
                }
            }

            #endregion
        }

        private string GetLongestEventName(int maxSize, BitmapFont font)
        {
            float size = this.MeasureStringWidth(this.Name, font);

            if (size <= maxSize)
            {
                return this.Name;
            }

            for (int i = 0; i < this.Name.Length; i++)
            {
                string name = this.Name.Substring(0, this.Name.Length - i);
                size = this.MeasureStringWidth(name, font);

                if (size <= maxSize)
                {
                    return name;
                }
            }

            return "...";
        }

        private float MeasureStringWidth(string text, BitmapFont font)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            return font.MeasureString(text).Width + 10; // TODO: Why is +10 needed?
        }

        private void DrawRectangle(SpriteBatch spriteBatch, Control control, Texture2D baseTexture, Rectangle coords, Color color)
        {
            spriteBatch.DrawOnCtrl(control, baseTexture, coords, color);
        }

        private void DrawLine(SpriteBatch spriteBatch, Control control, Texture2D baseTexture, Rectangle coords, Color color)
        {
            spriteBatch.DrawOnCtrl(control, baseTexture, coords, color);
        }

        private void DrawCrossOut(SpriteBatch spriteBatch, Control control, Texture2D baseTexture, Rectangle coords, Color color)
        {
            Point topLeft = new Point(coords.Left, coords.Top);
            Point topRight = new Point(coords.Right, coords.Top);
            Point bottomLeft = new Point(coords.Left, coords.Bottom);
            Point bottomRight = new Point(coords.Right, coords.Bottom);

            this.DrawAngledLine(spriteBatch, control, baseTexture, topLeft, bottomRight, color);
            this.DrawAngledLine(spriteBatch, control, baseTexture, bottomLeft, topRight, color);
        }

        private void DrawAngledLine(SpriteBatch spriteBatch, Control control, Texture2D baseTexture, Point start, Point end, Color color)
        {
            int length = (int)Math.Floor(Helpers.MathHelper.CalculeDistance(start, end));
            Rectangle lineRectangle = new Rectangle(start.X, start.Y, length, 1);
            float angle = (float)Helpers.MathHelper.CalculeAngle(start, end);
            spriteBatch.DrawOnCtrl(control, baseTexture, lineRectangle, null, color, angle, new Vector2(0f, 0f));
        }

        private void DrawRectangle(SpriteBatch spriteBatch, Control control, Texture2D baseTexture, Rectangle coords, Color color, int borderSize, Color borderColor)
        {
            this.DrawRectangle(spriteBatch, control, baseTexture, coords, color);

            if (borderSize > 0 && borderColor != Microsoft.Xna.Framework.Color.Transparent)
            {
                spriteBatch.DrawOnCtrl(control, baseTexture, new Rectangle(coords.Left, coords.Top, coords.Width - borderSize, borderSize), borderColor);
                spriteBatch.DrawOnCtrl(control, baseTexture, new Rectangle(coords.Right - borderSize, coords.Top, borderSize, coords.Height), borderColor);
                spriteBatch.DrawOnCtrl(control, baseTexture, new Rectangle(coords.Left, coords.Bottom - borderSize, coords.Width, borderSize), borderColor);
                spriteBatch.DrawOnCtrl(control, baseTexture, new Rectangle(coords.Left, coords.Top, borderSize, coords.Height), borderColor);
            }
        }

        public void CopyWaypoint()
        {
            if (!string.IsNullOrWhiteSpace(this.Waypoint))
            {
                ClipboardUtil.WindowsClipboardService.SetTextAsync(this.Waypoint);
                ScreenNotification.ShowNotification($"Waypoint copied to clipboard!");
                ScreenNotification.ShowNotification($"{this.Name}");
            }
            else
            {
                ScreenNotification.ShowNotification($"No Waypoint found!");
                ScreenNotification.ShowNotification($"{this.Name}");
            }
        }

        public void OpenWiki()
        {
            if (!string.IsNullOrWhiteSpace(this.Wiki))
            {
                Process.Start(this.Wiki);
            }
        }

        public List<DateTime> GetStartOccurences(DateTime now, DateTime max, DateTime min, bool addTimezoneOffset = true, bool limitsBetweenRanges = false)
        {
            DateTime zero = new DateTime(min.Year, min.Month, min.Day, 0, 0, 0).AddDays(this.Repeat.TotalMinutes == 0 ? 0 : -1);

            TimeSpan offset = this.Offset;
            if (addTimezoneOffset)
            {
                offset = offset.Add(TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now));
            }

            DateTime eventStart = zero.Add(offset);

            List<DateTime> startOccurences = new List<DateTime>();

            while (eventStart < max)
            {

                bool startAfterMin = eventStart > min;
                bool endAfterMin = eventStart.AddMinutes(this.Duration) > min;
                bool endBeforeMax = eventStart.AddMinutes(this.Duration) < max;

                bool inRange = limitsBetweenRanges ? (startAfterMin && endBeforeMax) : (startAfterMin || endAfterMin);

                if (inRange && eventStart < max)
                {
                    startOccurences.Add(eventStart);
                }

                if (this.Repeat.TotalMinutes == 0)
                {
                    eventStart = eventStart.Add(TimeSpan.FromDays(1));
                }
                else
                {
                    eventStart = eventStart.Add(this.Repeat);
                }
            }

            return startOccurences;
        }

        public double GetXPosition(DateTime start, DateTime min, double pixelPerMinute)
        {
            double minutesSinceMin = start.Subtract(min).TotalMinutes;
            return minutesSinceMin * pixelPerMinute;
        }

        private int GetMinYPosition(IEnumerable<EventCategory> eventCategories, int eventHight, bool debugEnabled)
        {
            int minY = 0;

            if (debugEnabled)
            {
                foreach (EventCategory eventCategory in eventCategories)
                {
                    foreach (Event e in eventCategory.Events)
                    {
                        minY += eventHight;
                        if (this == e)
                        {
                            return minY;
                        }
                    }
                }
            }

            return minY;
        }

        public int GetYPosition(IEnumerable<EventCategory> eventCategories, List<SettingEntry<bool>> eventSettings, EventCategory evc, int eventHeight, bool debugEnabled)
        {
            int y = this.GetMinYPosition(eventCategories, eventHeight, debugEnabled);
            foreach (EventCategory category in eventCategories)
            {
                bool anyFromCategoryRendered = false;
                foreach (Event e in category.Events)
                {
                    SettingEntry<bool> setting = eventSettings.Find(eventSetting => eventSetting.EntryKey == e.Name);
                    if (setting == null || !setting.Value)
                    {
                        continue;
                    }

                    anyFromCategoryRendered = true;

                    if ((e.Filler && category.Key == evc.Key) || category.Key != evc.Key)
                    {
                        if (e.Filler || (e.Name != this.Name))
                        {
                            continue;
                        }
                    }

                    return y;
                }

                if (anyFromCategoryRendered)
                {
                    y += eventHeight;
                }
            }

            return y;
        }

        public double GetWidth(DateTime eventOccurence, DateTime min, Rectangle bounds, double pixelPerMinute)
        {
            double eventWidth = this.Duration * pixelPerMinute;

            double x = this.GetXPosition(eventOccurence, min, pixelPerMinute);

            if (x < 0)
            {
                eventWidth -= Math.Abs(x);
            }

            // Only draw event until end of form
            eventWidth = Math.Min(eventWidth, bounds.Width - x);

            return eventWidth;
        }

        public bool IsHovered(IEnumerable<EventCategory> eventCategories, List<SettingEntry<bool>> eventSettings, EventCategory eventCategory, DateTime now, DateTime max, DateTime min, Rectangle bounds, Point relativeMousePosition, double pixelPerMinute, int eventHeight, bool debugEnabled)
        {
            var occurences = this.GetStartOccurences(now, max, min);

            foreach (var occurence in occurences)
            {
                double x = this.GetXPosition(occurence, min, pixelPerMinute);
                int eo_y = this.GetYPosition(eventCategories, eventSettings, eventCategory, eventHeight, debugEnabled);
                double width = this.GetWidth(occurence, min, bounds, pixelPerMinute);

                x = Math.Max(x, 0);

                bool hovered = (relativeMousePosition.X >= x && relativeMousePosition.X < x + width) && (relativeMousePosition.Y >= eo_y && relativeMousePosition.Y < eo_y + eventHeight);

                if (hovered) return true;
            }

            return false;
        }
    }
}
