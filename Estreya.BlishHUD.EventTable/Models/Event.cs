namespace Estreya.BlishHUD.EventTable.Models
{
    using Blish_HUD;
    using Blish_HUD.Controls;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class Event
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("offset"), JsonConverter(typeof(Json.TimeSpanJsonConverter), "dd\\.hh\\:mm", new string[] { "hh\\:mm" })]
        public TimeSpan Offset { get; set; }

        [JsonProperty("repeat"), JsonConverter(typeof(Json.TimeSpanJsonConverter), "dd\\.hh\\:mm", new string[] { "dd\\.hh\\:mm", "hh\\:mm"  })]
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

        public enum EventDifficulty
        { 
            None,
            Standard,
            Hardcore
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
    }
}
