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
    using Estreya.BlishHUD.EventTable.Utils;
    using MonoGame.Extended;
    using Estreya.BlishHUD.EventTable.Extensions;

    [Serializable]
    public class Event
    {
        private static readonly Logger Logger = Logger.GetLogger<Event>();

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("offset"), JsonConverter(typeof(Json.TimeSpanJsonConverter), "dd\\.hh\\:mm", new string[] { "hh\\:mm" })]
        public TimeSpan Offset { get; set; }
        [JsonProperty("convertOffset")]
        public bool ConvertOffset { get; set; } = true;

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
        [JsonProperty("api")]
        public string APICode { get; set; }

        internal bool Filler { get; set; }
        internal EventCategory EventCategory { get; set; }

        [JsonIgnore]
        private Tooltip _tooltip;

        [JsonIgnore]
        private ContextMenuStrip _contextMenuStrip;

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

        [JsonIgnore]
        private ContextMenuStrip ContextMenuStrip
        {
            get
            {
                if (_contextMenuStrip == null)
                {
                    _contextMenuStrip = new ContextMenuStrip();

                    ContextMenuStripItem copyWaypoint = new ContextMenuStripItem();
                    copyWaypoint.Text = "Copy Waypoint";
                    copyWaypoint.Click += (s, e) => this.CopyWaypoint();
                    _contextMenuStrip.AddMenuItem(copyWaypoint);

                    ContextMenuStripItem openWiki = new ContextMenuStripItem();
                    openWiki.Text = "Open Wiki";
                    openWiki.Click += (s, e) => this.OpenWiki();
                    _contextMenuStrip.AddMenuItem(openWiki);

                    ContextMenuStripItem hideCategory = new ContextMenuStripItem();
                    hideCategory.Text = "Hide category until Reset";
                    hideCategory.Click += (s, e) => this.FinishCategory();
                    _contextMenuStrip.AddMenuItem(hideCategory);

                    ContextMenuStripItem hideEvent = new ContextMenuStripItem();
                    hideEvent.Text = "Hide event until Reset";
                    hideEvent.Click += (s, e) => this.Finish();
                    _contextMenuStrip.AddMenuItem(hideEvent);

                    ContextMenuStripItem disable = new ContextMenuStripItem();
                    disable.Text = "Disable";
                    disable.Click += (s, e) => this.Disable();
                    _contextMenuStrip.AddMenuItem(disable);
                }

                return _contextMenuStrip;
            }
        }

        public void Draw(SpriteBatch spriteBatch, Rectangle bounds, Control control, Texture2D baseTexture, List<EventCategory> allCategories, EventCategory currentCategory, double pixelPerMinute, int eventHeight, DateTime now, DateTime min, DateTime max, BitmapFont font, List<DateTime> startOccurences)
        {
            foreach (var eventStart in startOccurences)
            {
                float width = (float)this.GetWidth(eventStart, min, bounds, pixelPerMinute);
                if (width <= 0)
                {
                    // Why would it be negativ anyway?
                    continue;
                }

                int y = this.GetYPosition(allCategories, currentCategory, eventHeight, EventTableModule.ModuleInstance.Debug);
                float x = (float)this.GetXPosition(eventStart, min, pixelPerMinute);
                x = Math.Max(x, 0);

                #region Draw Event Rectangle

                Color color = Microsoft.Xna.Framework.Color.Transparent;

                if (!this.Filler)
                {
                    System.Drawing.Color colorFromEvent = string.IsNullOrWhiteSpace(this.Color) ? System.Drawing.Color.White : System.Drawing.ColorTranslator.FromHtml(this.Color);
                    color = new Color(colorFromEvent.R, colorFromEvent.G, colorFromEvent.B);
                }


                RectangleF eventTexturePosition = new RectangleF(x, y, width, eventHeight);
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

                RectangleF eventTextPosition = Rectangle.Empty;
                if (!string.IsNullOrWhiteSpace(this.Name) && (!this.Filler || (this.Filler && EventTableModule.ModuleInstance.ModuleSettings.UseFillerEventNames.Value)))
                {
                    string eventName = this.GetLongestEventName(eventTexturePosition.Width, font);
                    float eventTextWidth = this.MeasureStringWidth(eventName, font);
                    eventTextPosition = new RectangleF(eventTexturePosition.X + 5, eventTexturePosition.Y + 5, eventTextWidth, eventTexturePosition.Height - 10);

                    spriteBatch.DrawStringOnCtrl(control, eventName, font, eventTextPosition, textColor);
                }

                #endregion

                #region Draw Event Remaining Time

                bool running = eventStart <= now && eventStart.AddMinutes(this.Duration) > now;
                if (running)
                {
                    DateTime end = eventStart.AddMinutes(this.Duration);
                    TimeSpan timeRemaining = end.Subtract(now);
                    string timeRemainingString = FormatTime(timeRemaining);// timeRemaining.Hours > 0 ? timeRemaining.ToString("hh\\:mm\\:ss") : timeRemaining.ToString("mm\\:ss");
                    float timeRemainingWidth = this.MeasureStringWidth(timeRemainingString, font);
                    float timeRemainingX = eventTexturePosition.X + ((eventTexturePosition.Width / 2) - (timeRemainingWidth / 2));
                    if (timeRemainingX < eventTextPosition.X + eventTextPosition.Width)
                    {
                        timeRemainingX = eventTextPosition.X + eventTextPosition.Width + 10;
                    }

                    RectangleF eventTimeRemainingPosition = new RectangleF(timeRemainingX, eventTexturePosition.Y + 5, timeRemainingWidth, eventTexturePosition.Height - 10);

                    if (eventTimeRemainingPosition.X + eventTimeRemainingPosition.Width <= eventTexturePosition.X + eventTexturePosition.Width)
                    {
                        // Only draw if it fits in event bounds
                        spriteBatch.DrawStringOnCtrl(control, timeRemainingString, font, eventTimeRemainingPosition, textColor);
                    }
                }

                #endregion

                #region Draw Cross out

                if (EventTableModule.ModuleInstance.ModuleSettings.WorldbossCompletedAcion.Value == WorldbossCompletedAction.Crossout && !this.Filler && !string.IsNullOrWhiteSpace(this.APICode))
                {
                    if (EventTableModule.ModuleInstance.WorldbossState.IsCompleted(this.APICode))
                    {
                        this.DrawCrossOut(spriteBatch, control, baseTexture, eventTexturePosition, Microsoft.Xna.Framework.Color.Red);
                    }
                }
                #endregion

            }
        }

        private void UpdateTooltip(string description)
        {
            _tooltip = new Tooltip(new UI.Views.TooltipView(this.Name, description, this.Icon));
        }

        private string GetTimeRemaining(DateTime now, DateTime max, DateTime min)
        {
            var startOccurences = this.GetStartOccurences(now, max, min);
            var filteredStartOccurences = startOccurences.Where(so => so <= now && so.AddMinutes(this.Duration) > now);

            if (filteredStartOccurences.Any())
            {
                DateTime end = filteredStartOccurences.First().AddMinutes(this.Duration);
                TimeSpan timeRemaining = end.Subtract(now);
                string timeRemainingString = this.FormatTime(timeRemaining);
                return timeRemainingString;
            }

            return null;
        }

        private string FormatTime(TimeSpan ts)
        {
            return ts.Hours > 0 ? ts.ToString("hh\\:mm\\:ss") : ts.ToString("mm\\:ss");
        }

        private string FormatTime(DateTime dateTime)
        {
            return dateTime.Hour > 0 ? dateTime.ToString("HH:mm:ss") : dateTime.ToString("mm:ss");
        }

        private string GetLongestEventName(float maxSize, BitmapFont font)
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

        private void DrawRectangle(SpriteBatch spriteBatch, Control control, Texture2D baseTexture, RectangleF coords, Color color)
        {
            spriteBatch.DrawOnCtrl(control, baseTexture, coords, color);

            //spriteBatch.DrawOnCtrl(control, baseTexture, coords, color);
        }

        private void DrawLine(SpriteBatch spriteBatch, Control control, Texture2D baseTexture, Rectangle coords, Color color)
        {
            spriteBatch.DrawOnCtrl(control, baseTexture, coords, color);
        }

        private void DrawCrossOut(SpriteBatch spriteBatch, Control control, Texture2D baseTexture, RectangleF coords, Color color)
        {
            Point2 topLeft = new Point2(coords.Left, coords.Top);
            Point2 topRight = new Point2(coords.Right, coords.Top);
            Point2 bottomLeft = new Point2(coords.Left, coords.Bottom);
            Point2 bottomRight = new Point2(coords.Right, coords.Bottom);

            this.DrawAngledLine(spriteBatch, control, baseTexture, topLeft, bottomRight, color);
            this.DrawAngledLine(spriteBatch, control, baseTexture, bottomLeft, topRight, color);
        }

        private void DrawAngledLine(SpriteBatch spriteBatch, Control control, Texture2D baseTexture, Point2 start, Point2 end, Color color)
        {
            float length = Helpers.MathHelper.CalculeDistance(start, end);
            RectangleF lineRectangle = new RectangleF(start.X, start.Y, length, 1);
            float angle = Helpers.MathHelper.CalculeAngle(start, end);
            spriteBatch.DrawOnCtrl(control, baseTexture, lineRectangle, color, angle);
        }

        private void DrawRectangle(SpriteBatch spriteBatch, Control control, Texture2D baseTexture, RectangleF coords, Color color, int borderSize, Color borderColor)
        {
            this.DrawRectangle(spriteBatch, control, baseTexture, coords, color);

            if (borderSize > 0 && borderColor != Microsoft.Xna.Framework.Color.Transparent)
            {
                DrawRectangle(spriteBatch, control, baseTexture, new RectangleF(coords.Left, coords.Top, coords.Width - borderSize, borderSize), borderColor);
                DrawRectangle(spriteBatch, control, baseTexture, new RectangleF(coords.Right - borderSize, coords.Top, borderSize, coords.Height), borderColor);
                DrawRectangle(spriteBatch, control, baseTexture, new RectangleF(coords.Left, coords.Bottom - borderSize, coords.Width, borderSize), borderColor);
                DrawRectangle(spriteBatch, control, baseTexture, new RectangleF(coords.Left, coords.Top, borderSize, coords.Height), borderColor);
                //spriteBatch.DrawOnCtrl(control, baseTexture, new Rectangle(coords.Left, coords.Top, coords.Width - borderSize, borderSize), borderColor);
                //spriteBatch.DrawOnCtrl(control, baseTexture, new Rectangle(coords.Right - borderSize, coords.Top, borderSize, coords.Height), borderColor);
                //spriteBatch.DrawOnCtrl(control, baseTexture, new Rectangle(coords.Left, coords.Bottom - borderSize, coords.Width, borderSize), borderColor);
                //spriteBatch.DrawOnCtrl(control, baseTexture, new Rectangle(coords.Left, coords.Top, borderSize, coords.Height), borderColor);
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
            List<DateTime> startOccurences = new List<DateTime>();

            if (this.IsDisabled())
            {
                return startOccurences;
            }

            DateTime zero = new DateTime(min.Year, min.Month, min.Day, 0, 0, 0).AddDays(this.Repeat.TotalMinutes == 0 ? 0 : -1);

            TimeSpan offset = this.Offset;
            if (this.ConvertOffset && addTimezoneOffset)
            {
                offset = offset.Add(TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now));
            }

            DateTime eventStart = zero.Add(offset);

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

        public int GetYPosition(IEnumerable<EventCategory> eventCategories, EventCategory evc, int eventHeight, bool debugEnabled)
        {
            int y = this.GetMinYPosition(eventCategories, eventHeight, debugEnabled);
            foreach (EventCategory category in eventCategories)
            {
                bool anyFromCategoryRendered = false;
                foreach (Event e in category.Events)
                {
                    if (e.IsDisabled())
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
            if (x + eventWidth > bounds.Width)
            {
                eventWidth = bounds.Width - x;
            }

            //eventWidth = Math.Min(eventWidth, bounds.Width/* - x*/);

            return eventWidth;
        }

        public bool IsHovered(IEnumerable<EventCategory> eventCategories, EventCategory eventCategory, DateTime now, DateTime max, DateTime min, Rectangle bounds, Point relativeMousePosition, double pixelPerMinute, int eventHeight, bool debugEnabled)
        {
            var occurences = this.GetStartOccurences(now, max, min);

            foreach (var occurence in occurences)
            {
                double x = this.GetXPosition(occurence, min, pixelPerMinute);
                int eo_y = this.GetYPosition(eventCategories, eventCategory, eventHeight, debugEnabled);
                double width = this.GetWidth(occurence, min, bounds, pixelPerMinute);

                x = Math.Max(x, 0);

                bool hovered = (relativeMousePosition.X >= x && relativeMousePosition.X < x + width) && (relativeMousePosition.Y >= eo_y && relativeMousePosition.Y < eo_y + eventHeight);

                if (hovered) return true;
            }

            return false;
        }

        public void HandleClick(object sender, Blish_HUD.Input.MouseEventArgs e)
        {

            if (e.EventType == Blish_HUD.Input.MouseEventType.LeftMouseButtonPressed)
            {
                if (EventTableModule.ModuleInstance.ModuleSettings.CopyWaypointOnClick.Value)
                {
                    this.CopyWaypoint();
                }
            }
            else if (e.EventType == Blish_HUD.Input.MouseEventType.RightMouseButtonPressed)
            {
                if (EventTableModule.ModuleInstance.ModuleSettings.ShowContextMenuOnClick.Value)
                {
                    int topPos = e.MousePosition.Y + this.ContextMenuStrip.Height > GameService.Graphics.SpriteScreen.Height
                                    ? -this.ContextMenuStrip.Height
                                    : 0;

                    int leftPos = e.MousePosition.X + this.ContextMenuStrip.Width < GameService.Graphics.SpriteScreen.Width
                                      ? 0
                                      : -this.ContextMenuStrip.Width;

                    Point menuPosition = e.MousePosition + new Point(leftPos, topPos);
                    this.ContextMenuStrip.Show(menuPosition);
                }
            }
        }

        public void HandleHover(object sender, Input.MouseEventArgs e, double pixelPerMinute)
        {
            var occurences = this.GetStartOccurences(EventTableModule.ModuleInstance.DateTimeNow, EventTableModule.ModuleInstance.EventTimeMax, EventTableModule.ModuleInstance.EventTimeMin);
            var hoveredOccurences = occurences.Where(eo =>
            {
                double xStart = this.GetXPosition(eo, EventTableModule.ModuleInstance.EventTimeMin, pixelPerMinute);
                double xEnd = xStart + this.Duration * pixelPerMinute;
                return e.Position.X > xStart && e.Position.X < xEnd;
            });


            if (!this.Tooltip.Visible)
            {
                Debug.WriteLine($"Show Tooltip for Event: {this.Name}{e.Position}");

                string description = $"{this.Location}";

                if (hoveredOccurences.Any())
                {
                    var hoveredOccurence = hoveredOccurences.First();

                    if (EventTableModule.ModuleInstance.ModuleSettings.TooltipTimeMode.Value == TooltipTimeMode.Relative)
                    {
                        bool isPrev = hoveredOccurence.AddMinutes(this.Duration) < EventTableModule.ModuleInstance.DateTimeNow;
                        bool isNext = !isPrev && hoveredOccurence > EventTableModule.ModuleInstance.DateTimeNow;
                        bool isCurrent = !isPrev && !isNext;

                        description = $"{this.Location}{(!string.IsNullOrWhiteSpace(this.Location) ? "\n" : string.Empty)}\n";

                        if (isPrev)
                        {
                            description += $"Finished since: {FormatTime(EventTableModule.ModuleInstance.DateTimeNow - hoveredOccurence.AddMinutes(this.Duration))}";
                        }
                        else if (isNext)
                        {
                            description += $"Starts in: {FormatTime(hoveredOccurence - EventTableModule.ModuleInstance.DateTimeNow)}";
                        }
                        else if (isCurrent)
                        {
                            description += $"Remaining: {FormatTime(hoveredOccurence.AddMinutes(this.Duration) - EventTableModule.ModuleInstance.DateTimeNow)}";
                        }
                    }
                    else
                    {
                        // Absolute
                        description += $"{this.Location}{(!string.IsNullOrWhiteSpace(this.Location) ? "\n" : string.Empty)}\nStarts at: {FormatTime(hoveredOccurence)}";
                    }
                }
                else
                {
                    Logger.Error($"Can't find hovered event: {this.Name} - {string.Join(", ", occurences.Select(o => o.ToString()))}");
                }

                this.UpdateTooltip(description);
                this.Tooltip.Show(0, 0);
            }
        }

        public void HandleNonHover(object sender, Input.MouseEventArgs e)
        {
            if (this.Tooltip.Visible)
            {
                Debug.WriteLine($"Hide Tooltip for Event: {this.Name}{e.Position}");
                this.Tooltip.Hide();
            }
        }

        public void Finish()
        {
            var now = EventTableModule.ModuleInstance.DateTimeNow.ToUniversalTime();
            DateTime until = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).AddDays(1);
            EventTableModule.ModuleInstance.HiddenState.Add(this.Name, until, true);
        }

        public void FinishCategory()
        {
            var now = EventTableModule.ModuleInstance.DateTimeNow.ToUniversalTime();
            DateTime until = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).AddDays(1);
            EventTableModule.ModuleInstance.HiddenState.Add(this.EventCategory.Name, until, true);
        }

        public void Disable()
        {
            var eventSetting = EventTableModule.ModuleInstance.ModuleSettings.AllEvents.Where(e => e.EntryKey == this.Name);
            if (eventSetting.Any())
            {
                eventSetting.First().Value = false;
            }
        }

        public bool IsDisabled()
        {
            var eventSetting = EventTableModule.ModuleInstance.ModuleSettings.AllEvents.Where(e => e.EntryKey == this.Name);
            if (eventSetting.Any())
            {
                bool enabled = eventSetting.First().Value && !EventTableModule.ModuleInstance.HiddenState.IsHidden(this.Name);

                return !enabled;
            }

            return false;
        }
    }
}
