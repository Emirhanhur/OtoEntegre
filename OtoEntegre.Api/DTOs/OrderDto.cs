using OtoEntegre.Api.Converters;
using System.Text.Json.Serialization;

namespace OtoEntegre.Api.DTOs
{
   public class OrderDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("dealerId")]
        public int DealerId { get; set; }

        [JsonPropertyName("shipmentFirmId")]
        public int ShipmentFirmId { get; set; }

        [JsonPropertyName("status")]
        public OrderStatus Status { get; set; }   // ✅ ENUM kullanıyoruz

        [JsonPropertyName("createdAt")]
        public string CreatedAt { get; set; } = string.Empty;

        [JsonPropertyName("updatedAt")]
        public string UpdatedAt { get; set; } = string.Empty;

        // Eski alanlar (geriye dönük uyumluluk için)
        public string OrderNumber { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public decimal ShippingFee { get; set; }
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime? OrderDate { get; set; }
    }
public enum OrderStatus
    {
        Unknown = 0,
        Created = 1,      // Yeni sipariş
        Shipped = 2,      // Kargoya verildi
        Delivered = 3,    // Teslim edildi
        Cancelled = 4,    // İptal
        Returned = 5      // İade
        // İhtiyaca göre diğer status kodlarını ekleyebilirsin
    }

    public class OrderApiResponse
    {
        [JsonPropertyName("result")]
        public OrderResult? Result { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }
    }

    public class OrderResult
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        [JsonPropertyName("list")]
        public List<OrderDto>? List { get; set; }
    }


    public class OrderDetailApiResponse
    {
        public OrderDetailDto? Result { get; set; }
    }


    public class OtostickerQuickOrderDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public string PaymentType { get; set; } = "0"; // Örnek: 0 = Nakit


        public string Status { get; set; } = string.Empty; // Örnek: 1 = Yeni Sipariş
        public List<OtostickerProductDto> Products { get; set; } = new();
    }
    public class OtostickerFastSaleOrderDto
    {

        public OtostickerCustomerDto Customer { get; set; } = new();
        public OtostickerOrderDto Order { get; set; } = new();
        public List<OtostickerProductItemDto> Products { get; set; } = new();
    }
    public class OtostickerCustomerDto
    {
        public string Name { get; set; } = "";
        public string Lastname { get; set; } = "";
        public string Email { get; set; } = "";
        public string Phone { get; set; } = "";
        public string City { get; set; } = "";
        public string Distict { get; set; } = ""; // dikkat! api'de yanlış yazılmış "distict"
        public string Address { get; set; } = "";
        public string TaxId { get; set; } = "";
        public string TaxBranch { get; set; } = "";
        public string NationalId { get; set; } = "";
    }
    public class OtostickerOrderDto
    {
        public string Date { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        public int PaymentType { get; set; } = 2;
        [JsonConverter(typeof(JsonStringEnumConverter))] // veya custom converter

        public string Status { get; set; } = string.Empty;
        public string Note { get; set; } = "";
    }

    public class OtostickerProductItemDto
    {
        public int Id { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Variant1 { get; set; } = "";
    }

    public class OtostickerFastSaleOrderResponse
    {
        public bool Success { get; set; }
        public string Code { get; set; } = "";
        public string? Message { get; set; }
    }
    public class OtostickerProductsResponse
    {
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("result")]
        public OtostickerProductsResult Result { get; set; } = new();
    }


    public class OtostickerProductsResult
    {
        public int Total { get; set; }
        public int PageSize { get; set; }
        public List<OtostickerProductDto> List { get; set; } = new List<OtostickerProductDto>();

    }
    public class OtostickerProductDto
    {
        [JsonPropertyName("productId")]
        public object? ProductId { get; set; }  // JSON string olarak geliyor

        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;


        public string Category1 { get; set; } = string.Empty; // <-- Burayı eklemelisin
        public string Category2 { get; set; } = string.Empty;
        public string Category3 { get; set; } = string.Empty;
        public string Category4 { get; set; } = string.Empty;
        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("salePrice")]
        public decimal SalePrice { get; set; }

        [JsonPropertyName("images")]
        public List<OtostickerProductImageDto> Images { get; set; } = new();
    }

    public class OtostickerProductImageDto
    {
        [JsonPropertyName("imagesUrl")]
        public string ImagesUrl { get; set; } = string.Empty;
    }



}
