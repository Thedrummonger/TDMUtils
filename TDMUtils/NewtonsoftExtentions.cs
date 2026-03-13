using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TDMUtils
{
    public static class NewtonsoftExtensions
    {

        public readonly static Newtonsoft.Json.JsonSerializerSettings DefaultSerializerSettings = new()
        {
            Formatting = Newtonsoft.Json.Formatting.Indented,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
            Converters = { new Newtonsoft.Json.Converters.StringEnumConverter(), new IPConverter() }
        };

        public sealed class IPConverter : JsonConverter<IPAddress>
        {
            public override void WriteJson(JsonWriter writer, IPAddress? value, JsonSerializer serializer)
            {
                writer.WriteValue(value?.ToString());
            }

            public override IPAddress? ReadJson(JsonReader reader, Type objectType, IPAddress? existingValue, bool hasExistingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.Null) return null;
                if (reader.TokenType != JsonToken.String) throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing IPAddress.");
                var s = (string?)reader.Value;
                return string.IsNullOrWhiteSpace(s) ? null : IPAddress.Parse(s);
            }
        }
    }
}