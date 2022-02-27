namespace Estreya.BlishHUD.EventTable.Json
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    public class SemanticVersionConverter : Newtonsoft.Json.JsonConverter<SemanticVersioning.Version>
    {
        public override SemanticVersioning.Version ReadJson(JsonReader reader, Type objectType, SemanticVersioning.Version existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (objectType != typeof(SemanticVersioning.Version)) return new SemanticVersioning.Version(0,0,0);

            string value = (string)reader.Value;

            return SemanticVersioning.Version.Parse(value);
        }

        public override void WriteJson(JsonWriter writer, SemanticVersioning.Version value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }
}
