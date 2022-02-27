namespace Estreya.BlishHUD.EventTable.Models.Settings
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class EventSettingsFile
    {
        [JsonProperty("version"), JsonConverter(typeof(Json.SemanticVersionConverter))]
        public SemanticVersioning.Version Version { get; set; } = new SemanticVersioning.Version(0, 0, 0);

        [JsonProperty("eventCategories")]
        public List<EventCategory> EventCategories {  get; set; }
    }
}
