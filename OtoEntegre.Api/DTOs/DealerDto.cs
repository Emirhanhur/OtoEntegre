using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OtoEntegre.Api.DTOs
{
    public class DealerApiResponse
    {
        public bool Success { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DealerResult Result { get; set; } = new DealerResult();
    }

  public class DealerResult
{
    public int Total { get; set; }
    public int PageSize { get; set; }
    public int PageStart { get; set; }
    public List<DealerDto> List { get; set; } = [];
}

   public class DealerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    [JsonConverter(typeof(JsonStringEnumConverter))] // veya custom converter

    public string Status { get; set; } = string.Empty;  // object yerine int? yap
    public decimal Balance { get; set; }
    public decimal Discount { get; set; }
}

}
