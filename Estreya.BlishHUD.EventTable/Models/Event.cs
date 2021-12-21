namespace Estreya.BlishHUD.EventTable.Models
{
    using Blish_HUD;
    using Blish_HUD.Contexts;
    using Blish_HUD.Controls;
    using Blish_HUD.Settings;
    using Microsoft.Xna.Framework;
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
                minY += eventHight; // Pixel per Minute
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
                    if (setting != null && !setting.Value)
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
                int eo_y = this.GetYPosition(eventCategories, eventSettings, eventCategory, eventHeight,debugEnabled);
                double width = this.GetWidth(occurence, min, bounds, pixelPerMinute);

                x = Math.Max(x, 0);

                bool hovered = (relativeMousePosition.X >= x && relativeMousePosition.X < x + width) && (relativeMousePosition.Y >= eo_y && relativeMousePosition.Y < eo_y + eventHeight);

                if (hovered) return true;
            }

            return false;
        }
    }
}
