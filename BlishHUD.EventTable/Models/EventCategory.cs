namespace BlishHUD.EventTable.Models
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class EventCategory
    {
        [JsonProperty("key")]
        public string Key { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("showCombined")]
        public bool ShowCombined { get; set; }
        [JsonProperty("events")]
        public List<Event> Events { get; set; }
    }
}
