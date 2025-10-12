using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

public class StringOrArrayJsonConverter : JsonConverter<List<string>>
{
    public override List<string> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var result = new List<string>();

        switch (reader.TokenType)
        {
            case JsonTokenType.StartArray:
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                        break;

                    switch (reader.TokenType)
                    {
                        case JsonTokenType.String:
                            result.Add(reader.GetString() ?? "");
                            break;
                        case JsonTokenType.Number:
                            result.Add(reader.GetInt64().ToString());
                            break;
                        case JsonTokenType.Null:
                            result.Add("");
                            break;
                        case JsonTokenType.StartObject:
                            // Eğer array içinde object gelirse tamamen skip
                            reader.Skip();
                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
                break;

            case JsonTokenType.String:
                result.Add(reader.GetString() ?? "");
                break;

            case JsonTokenType.Number:
                result.Add(reader.GetInt64().ToString());
                break;

            case JsonTokenType.Null:
                // null geldiğinde boş liste döndür
                break;

            case JsonTokenType.StartObject:
                // Tek obje geldiğinde atla, boş liste dön
                reader.Skip();
                break;

            default:
                // Beklenmeyen diğer tipler için de boş liste
                reader.Skip();
                break;
        }

        return result;
    }

    public override void Write(Utf8JsonWriter writer, List<string> value, JsonSerializerOptions options)
    {
        if (value == null || value.Count == 0)
        {
            writer.WriteStartArray();
            writer.WriteEndArray();
        }
        else if (value.Count == 1)
        {
            writer.WriteStringValue(value[0]);
        }
        else
        {
            writer.WriteStartArray();
            foreach (var item in value)
                writer.WriteStringValue(item);
            writer.WriteEndArray();
        }
    }
}
