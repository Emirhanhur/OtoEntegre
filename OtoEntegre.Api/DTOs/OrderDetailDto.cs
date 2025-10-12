using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OtoEntegre.Api.DTOs
{
    public class OrderDetailDto
    {
        [JsonPropertyName("order")]
        public OrderDto Order { get; set; } = new OrderDto();

        [JsonPropertyName("customer")]
        public CustomerDto? Customer { get; set; }

        [JsonPropertyName("products")]
        public List<ProductDto> Products { get; set; } = new List<ProductDto>();

        [JsonPropertyName("summary")]
        public SummaryDto? Summary { get; set; }
    }


    public class CustomerDto
    {
        public string? Name { get; set; }
        public string Email { get; set; } = string.Empty;   // ✅ uyarı biter
        public string Phone { get; set; } = string.Empty;   // ✅ uyarı biter
        public AddressDto Delivery { get; set; } = new();   // ✅ uyarı biter
        public AddressDto Invoice { get; set; } = new();    // ✅ uyarı biter
    }

    public class AddressDto
    {
        public string Address { get; set; } = string.Empty;
        public string District { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }
    public class ProductDto
    {
        public string? Code { get; set; }
        public string? Name { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }

        [JsonConverter(typeof(IntFromStringConverter))]
        public int Quantity { get; set; }
    }

    public class IntFromStringConverter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var str = reader.GetString();
                if (int.TryParse(str, out var val))
                    return val;
                throw new JsonException($"Cannot convert '{str}' to int.");
            }
            else if (reader.TokenType == JsonTokenType.Number)
            {
                return reader.GetInt32();
            }

            throw new JsonException($"Unexpected token {reader.TokenType}");
        }

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value);
        }
    }
    public class SummaryDto
    {
        public decimal Total { get; set; }
        public decimal SubTotal { get; set; }
        public decimal Overall { get; set; }
    }
    public class TaxDto
    {
        public decimal Rate { get; set; }
        public decimal Amount { get; set; }
    }
}
