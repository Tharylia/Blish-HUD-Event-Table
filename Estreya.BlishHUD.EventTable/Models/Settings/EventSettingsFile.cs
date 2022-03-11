namespace Estreya.BlishHUD.EventTable.Models.Settings
{
    using Newtonsoft.Json;
    using System.Collections.Generic;

    public class EventSettingsFile
    {
        [JsonProperty("version"), JsonConverter(typeof(Json.SemanticVersionConverter))]
        public SemanticVersioning.Version Version { get; set; } = new SemanticVersioning.Version(0, 0, 0);

        [JsonProperty("eventCategories")]
        public List<EventCategory> EventCategories { get; set; }
    }
}
