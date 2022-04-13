namespace Estreya.BlishHUD.EventTable.Models
{
    using Blish_HUD;
    using Blish_HUD._Extensions;
    using Blish_HUD.Controls;
    using Blish_HUD.Settings;
    using Estreya.BlishHUD.EventTable.Resources;
    using Estreya.BlishHUD.EventTable.Utils;
    using Microsoft.Xna.Framework;
    using Microsoft.Xna.Framework.Graphics;
    using MonoGame.Extended;
    using MonoGame.Extended.BitmapFonts;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    [Serializable]
    public class Event
    {
        private static readonly Logger Logger = Logger.GetLogger<Event>();

        private readonly TimeSpan updateInterval = TimeSpan.FromMinutes(15);
        private double timeSinceUpdate = 0;

        [JsonProperty("key")]
        public string Key { get; set; }

        /// <summary>
        /// The name of the event.
        /// <br/>
        /// Will get overridden with the localized event name if available.
        /// </summary>
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("offset"), JsonConverter(typeof(Json.TimeSpanJsonConverter), "dd\\.hh\\:mm", new string[] { "dd\\.hh\\:mm", "hh\\:mm" })]
        public TimeSpan Offset { get; set; }
        [JsonProperty("convertOffset")]
        public bool ConvertOffset { get; set; } = true;

        [JsonProperty("repeat"), JsonConverter(typeof(Json.TimeSpanJsonConverter), "dd\\.hh\\:mm", new string[] { "dd\\.hh\\:mm", "hh\\:mm" })]
        public TimeSpan Repeat { get; set; }

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
        public string BackgroundColorCode { get; set; }
        [JsonProperty("apiType")]
        public APICodeType APICodeType { get; set; }

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
                if (this._tooltip == null)
                {
                    this._tooltip = new Tooltip(new UI.Views.TooltipView(this.Name, $"{this.Location}", this.Icon));
                }

                return this._tooltip;
            }
        }

        [JsonIgnore]
        private string _settingKey;

        [JsonIgnore]
        public string SettingKey
        {
            get
            {
                if (this._settingKey == null)
                {
                    this._settingKey = $"{this.EventCategory.Key}-{this.Key ?? this.Name}";
                }

                return this._settingKey;
            }
        }

        [JsonIgnore]
        private ContextMenuStrip ContextMenuStrip
        {
            get
            {
                if (this._contextMenuStrip == null)
                {
                    this._contextMenuStrip = new ContextMenuStrip();

                    ContextMenuStripItem copyWaypoint = new ContextMenuStripItem
                    {
                        Text = Strings.Event_CopyWaypoint
                    };
                    copyWaypoint.Click += (s, e) => this.CopyWaypoint();
                    this._contextMenuStrip.AddMenuItem(copyWaypoint);

                    ContextMenuStripItem openWiki = new ContextMenuStripItem
                    {
                        Text = Strings.Event_OpenWiki
                    };
                    openWiki.Click += (s, e) => this.OpenWiki();
                    this._contextMenuStrip.AddMenuItem(openWiki);

                    ContextMenuStripItem hideCategory = new ContextMenuStripItem
                    {
                        Text = Strings.Event_HideCategory
                    };
                    hideCategory.Click += (s, e) => this.FinishCategory();
                    this._contextMenuStrip.AddMenuItem(hideCategory);

                    ContextMenuStripItem hideEvent = new ContextMenuStripItem
                    {
                        Text = Strings.Event_HideEvent
                    };
                    hideEvent.Click += (s, e) => this.Finish();
                    this._contextMenuStrip.AddMenuItem(hideEvent);

                    ContextMenuStripItem disable = new ContextMenuStripItem
                    {
                        Text = Strings.Event_Disable
                    };
                    disable.Click += (s, e) => this.Disable();
                    this._contextMenuStrip.AddMenuItem(disable);
                }

                return this._contextMenuStrip;
            }
        }

        [JsonIgnore]
        private Color? _backgroundColor;

        [JsonIgnore]
        public Color BackgroundColor
        {
            get
            {
                if (this._backgroundColor == null)
                {
                    if (!this.Filler)
                    {
                        System.Drawing.Color colorFromEvent = string.IsNullOrWhiteSpace(this.BackgroundColorCode) ? System.Drawing.Color.White : System.Drawing.ColorTranslator.FromHtml(this.BackgroundColorCode);
                        this._backgroundColor = new Color(colorFromEvent.R, colorFromEvent.G, colorFromEvent.B);
                    }
                }

                return this._backgroundColor ?? Color.Transparent;
            }
        }

        [JsonIgnore]
        private int _lastYPosition = 0;

        [JsonIgnore]
        public List<DateTime> Occurences { get; private set; } = new List<DateTime>();

        public Event()
        {
            this.timeSinceUpdate = this.updateInterval.TotalMilliseconds;
        }

        public bool Draw(SpriteBatch spriteBatch, Rectangle bounds, Control control, Texture2D baseTexture, int y, double pixelPerMinute, DateTime now, DateTime min, DateTime max, BitmapFont font)
        {
            List<DateTime> occurences = new List<DateTime>();
            lock (this.Occurences)
            {
                occurences.AddRange(this.Occurences.Where(oc => (oc >= min || oc.AddMinutes(this.Duration) >= min) && oc <= max));
            }

            this._lastYPosition = y;

            foreach (DateTime eventStart in occurences)
            {
                float width = (float)this.GetWidth(eventStart, min, bounds, pixelPerMinute);
                if (width <= 0)
                {
                    // Why would it be negativ anyway?
                    continue;
                }

                float x = (float)this.GetXPosition(eventStart, min, pixelPerMinute);
                x = Math.Max(x, 0);

                #region Draw Event Rectangle

                RectangleF eventTexturePosition = new RectangleF(x, y, width, EventTableModule.ModuleInstance.EventHeight);
                bool drawBorder = !this.Filler && EventTableModule.ModuleInstance.ModuleSettings.DrawEventBorder.Value;

                spriteBatch.DrawRectangle(control, baseTexture, eventTexturePosition, this.BackgroundColor * EventTableModule.ModuleInstance.ModuleSettings.Opacity.Value, drawBorder ? 1 : 0, Color.Black);

                #endregion

                Color textColor = Color.Black;
                if (this.Filler)
                {
                    if (EventTableModule.ModuleInstance.ModuleSettings.FillerTextColor.Value != null && EventTableModule.ModuleInstance.ModuleSettings.FillerTextColor.Value?.Id != 1)
                    {
                        textColor = EventTableModule.ModuleInstance.ModuleSettings.FillerTextColor.Value.Cloth.ToXnaColor();
                    }
                }
                else
                {
                    if (EventTableModule.ModuleInstance.ModuleSettings.TextColor.Value != null && EventTableModule.ModuleInstance.ModuleSettings.TextColor.Value?.Id != 1)
                    {
                        textColor = EventTableModule.ModuleInstance.ModuleSettings.TextColor.Value.Cloth.ToXnaColor();
                    }
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
                    string timeRemainingString = this.FormatTime(timeRemaining);// timeRemaining.Hours > 0 ? timeRemaining.ToString("hh\\:mm\\:ss") : timeRemaining.ToString("mm\\:ss");
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

                if (EventTableModule.ModuleInstance.ModuleSettings.EventCompletedAcion.Value == EventCompletedAction.Crossout && !this.Filler && !string.IsNullOrWhiteSpace(this.APICode))
                {
                    if (this.IsCompleted())
                    {
                        spriteBatch.DrawCrossOut( control, baseTexture, eventTexturePosition, Color.Red);
                    }
                }
                #endregion

            }

            return occurences.Any();
        }

        public bool IsCompleted()
        {
            bool completed = false;

            switch (this.APICodeType)
            {
                case APICodeType.Worldboss:
                    completed |= EventTableModule.ModuleInstance.WorldbossState.IsCompleted(this.APICode);
                    break;
                case APICodeType.Mapchest:
                    completed |= EventTableModule.ModuleInstance.MapchestState.IsCompleted(this.APICode);
                    break;
                default:
                    Logger.Warn($"Unsupported api code type: {this.APICodeType}");
                    break;
            }

            return completed;
        }

        private void UpdateTooltip(string description)
        {
            this._tooltip = new Tooltip(new UI.Views.TooltipView(this.Name, description, this.Icon));
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

        public void CopyWaypoint()
        {
            if (!string.IsNullOrWhiteSpace(this.Waypoint))
            {
                ClipboardUtil.WindowsClipboardService.SetTextAsync(this.Waypoint);
                Controls.ScreenNotification.ShowNotification(new[] { $"{this.Name}", Strings.Event_WaypointCopied });
                //ScreenNotification.ShowNotification($"Waypoint copied to clipboard!");
                //ScreenNotification.ShowNotification($"{this.Name}");
            }
            else
            {
                Controls.ScreenNotification.ShowNotification(new[] { $"{this.Name}", Strings.Event_NoWaypointFound });
                //ScreenNotification.ShowNotification($"No Waypoint found!");
                //ScreenNotification.ShowNotification($"{this.Name}");
            }
        }

        public void OpenWiki()
        {
            if (!string.IsNullOrWhiteSpace(this.Wiki))
            {
                Process.Start(this.Wiki);
            }
        }

        private List<DateTime> GetStartOccurences(DateTime now, DateTime max, DateTime min, bool addTimezoneOffset = true, bool limitsBetweenRanges = false)
        {
            List<DateTime> startOccurences = new List<DateTime>();

            DateTime zero = new DateTime(min.Year, min.Month, min.Day, 0, 0, 0).AddDays(this.Repeat.TotalMinutes == 0 ? 0 : -1);

            TimeSpan offset = this.Offset;
            if (this.ConvertOffset && addTimezoneOffset)
            {
                offset = offset.Add(TimeZone.CurrentTimeZone.GetUtcOffset(now));
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

                eventStart = this.Repeat.TotalMinutes == 0 ? eventStart.Add(TimeSpan.FromDays(1)) : eventStart.Add(this.Repeat);
            }

            return startOccurences;
        }

        public double GetXPosition(DateTime start, DateTime min, double pixelPerMinute)
        {
            double minutesSinceMin = start.Subtract(min).TotalMinutes;
            return minutesSinceMin * pixelPerMinute;
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
            if ((x > 0 ? x : 0) + eventWidth > bounds.Width)
            {
                eventWidth = bounds.Width - (x > 0 ? x : 0);
            }

            //eventWidth = Math.Min(eventWidth, bounds.Width/* - x*/);

            return eventWidth;
        }

        public bool IsHovered(DateTime min, Rectangle bounds, Point relativeMousePosition, double pixelPerMinute)
        {
            if (this.IsDisabled())
            {
                return false;
            }

            List<DateTime> occurences = this.Occurences;

            foreach (DateTime occurence in occurences)
            {
                double x = this.GetXPosition(occurence, min, pixelPerMinute);
                double width = this.GetWidth(occurence, min, bounds, pixelPerMinute);

                x = Math.Max(x, 0);

                bool hovered = (relativeMousePosition.X >= x && relativeMousePosition.X < x + width) && (relativeMousePosition.Y >= this._lastYPosition && relativeMousePosition.Y < this._lastYPosition + EventTableModule.ModuleInstance.EventHeight);

                if (hovered)
                {
                    return true;
                }
            }

            return false;
        }

        public void HandleClick(object sender, Blish_HUD.Input.MouseEventArgs e)
        {
            if (this.Filler)
            {
                return; // Currently don't do anything when filler
            }

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
            if (this.Filler)
            {
                return; // Currently don't do anything when filler
            }

            List<DateTime> occurences = this.Occurences;
            IEnumerable<DateTime> hoveredOccurences = occurences.Where(eo =>
            {
                double xStart = this.GetXPosition(eo, EventTableModule.ModuleInstance.EventTimeMin, pixelPerMinute);
                double xEnd = xStart + this.Duration * pixelPerMinute;
                return e.Position.X > xStart && e.Position.X < xEnd;
            });

            if (!this.Tooltip.Visible)
            {
                Debug.WriteLine($"Show Tooltip for Event: {this.Name} | {e.Position}");

                string description = $"{this.Location}";

                if (hoveredOccurences.Any())
                {
                    DateTime hoveredOccurence = hoveredOccurences.First();

                    if (EventTableModule.ModuleInstance.ModuleSettings.TooltipTimeMode.Value == TooltipTimeMode.Relative)
                    {
                        bool isPrev = hoveredOccurence.AddMinutes(this.Duration) < EventTableModule.ModuleInstance.DateTimeNow;
                        bool isNext = !isPrev && hoveredOccurence > EventTableModule.ModuleInstance.DateTimeNow;
                        bool isCurrent = !isPrev && !isNext;

                        description = $"{this.Location}{(!string.IsNullOrWhiteSpace(this.Location) ? "\n" : string.Empty)}\n";

                        if (isPrev)
                        {
                            description += $"{Strings.Event_Tooltip_FinishedSince}: {this.FormatTime(EventTableModule.ModuleInstance.DateTimeNow - hoveredOccurence.AddMinutes(this.Duration))}";
                        }
                        else if (isNext)
                        {
                            description += $"{Strings.Event_Tooltip_StartsIn}: {this.FormatTime(hoveredOccurence - EventTableModule.ModuleInstance.DateTimeNow)}";
                        }
                        else if (isCurrent)
                        {
                            description += $"{Strings.Event_Tooltip_Remaining}: {this.FormatTime(hoveredOccurence.AddMinutes(this.Duration) - EventTableModule.ModuleInstance.DateTimeNow)}";
                        }
                    }
                    else
                    {
                        // Absolute
                        description = $"{this.Location}{(!string.IsNullOrWhiteSpace(this.Location) ? "\n" : string.Empty)}\n{Strings.Event_Tooltip_StartsAt}: {this.FormatTime(hoveredOccurence)}";
                    }
                }
                else
                {
                    Logger.Warn($"Can't find hovered event: {this.Name} - {string.Join(", ", occurences.Select(o => o.ToString()))}");
                }

                this.UpdateTooltip(description);
                this.Tooltip.Show(0, 0);
            }
        }

        public void HandleNonHover(object sender, Input.MouseEventArgs e)
        {
            if (this.Tooltip.Visible)
            {
                Debug.WriteLine($"Hide Tooltip for Event: {this.Name} | {e.Position}");
                this.Tooltip.Hide();
            }
        }

        public void Finish()
        {
            DateTime now = EventTableModule.ModuleInstance.DateTimeNow.ToUniversalTime();
            DateTime until = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0).AddDays(1);
            EventTableModule.ModuleInstance.HiddenState.Add(this.SettingKey, until, true);
        }

        public void FinishCategory()
        {
            this.EventCategory?.Finish();
        }

        public void Disable()
        {
            // Check with .ToLower() because settings define is case insensitive
            IEnumerable<SettingEntry<bool>> eventSetting = EventTableModule.ModuleInstance.ModuleSettings.AllEvents.Where(e => e.EntryKey.ToLowerInvariant() == this.SettingKey.ToLowerInvariant());
            if (eventSetting.Any())
            {
                eventSetting.First().Value = false;
            }
        }

        public bool IsDisabled()
        {
            if (this.Filler)
            {
                return false;
            }

            // Check with .ToLower() because settings define is case insensitive
            IEnumerable<SettingEntry<bool>> eventSetting = EventTableModule.ModuleInstance.ModuleSettings.AllEvents.Where(e => e.EntryKey.ToLowerInvariant() == this.SettingKey.ToLowerInvariant());
            if (eventSetting.Any())
            {
                bool enabled = eventSetting.First().Value && !EventTableModule.ModuleInstance.HiddenState.IsHidden(this.SettingKey);

                return !enabled;
            }

            return false;
        }

        private void UpdateEventOccurences(GameTime gameTime)
        {
            if (this.Filler)
            {
                return;
            }

            lock (this.Occurences)
            {
                this.Occurences.Clear();
            }

            DateTime now = EventTableModule.ModuleInstance.DateTimeNow;
            DateTime min = now.AddDays(-4);
            DateTime max = now.AddDays(4);

            List<DateTime> occurences = this.GetStartOccurences(now, max, min);

            lock (this.Occurences)
            {
                this.Occurences.AddRange(occurences);
            }
        }

        public Task LoadAsync()
        {
            // Prevent crash on older events.json files
            if (string.IsNullOrWhiteSpace(this.Key))
            {
                this.Key = this.Name;
            }

            if (EventTableModule.ModuleInstance.ModuleSettings.UseEventTranslation.Value)
            {
                this.Name = Strings.ResourceManager.GetString($"event-{this.SettingKey}") ?? this.Name;
            }

            if (string.IsNullOrWhiteSpace(this.Icon))
            {
                this.Icon = this.EventCategory.Icon;
            }

            return Task.CompletedTask;
        }

        public void Unload()
        {
            Logger.Debug("Unload event: {0}", this.Key);

            this._tooltip?.Dispose();
            this._tooltip = null;

            this._contextMenuStrip?.Dispose();
            this._contextMenuStrip = null;

            Logger.Debug("Unloaded event: {0}", this.Key);
        }

        public void Update(GameTime gameTime)
        {
            UpdateCadenceUtil.UpdateWithCadence(this.UpdateEventOccurences, gameTime, this.updateInterval.TotalMilliseconds, ref this.timeSinceUpdate);
        }
    }
}
