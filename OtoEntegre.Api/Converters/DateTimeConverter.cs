using System.Text.Json;
using System.Text.Json.Serialization;

namespace OtoEntegre.Api.Converters
{
    public class DateTimeConverter : JsonConverter<DateTime>
    {
        private readonly string[] _dateFormats = {
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.fffZ",
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-dd"
        };

        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string? dateString = reader.GetString();
                
                if (string.IsNullOrEmpty(dateString))
                    return DateTime.MinValue;

                // Try parsing with different formats
                foreach (var format in _dateFormats)
                {
                    if (DateTime.TryParseExact(dateString, format, null, System.Globalization.DateTimeStyles.None, out DateTime result))
                    {
                        return result;
                    }
                }

                // If none of the formats work, try general parsing
                if (DateTime.TryParse(dateString, out DateTime generalResult))
                {
                    return generalResult;
                }

                return DateTime.MinValue;
            }

            if (reader.TokenType == JsonTokenType.Number)
            {
                // Handle Unix timestamp
                if (reader.TryGetInt64(out long timestamp))
                {
                    return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                }
            }

            throw new JsonException($"Unable to convert \"{reader.GetString()}\" to DateTime.");
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }
}

