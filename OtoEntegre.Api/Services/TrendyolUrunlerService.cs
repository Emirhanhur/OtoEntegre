using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using OtoEntegre.Api.DTOs;
using System.Text;

public class TrendyolService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TrendyolService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<TrendyolProductResponse?> GetProductsAsync(long supplierId, string apiKey, string apiSecret, int page = 0, int size = 1000)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://apigw.trendyol.com/integration/product/sellers/{supplierId}/products?page={page}&size={size}");

        var auth = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{apiKey}:{apiSecret}"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
        request.Headers.Add("User-Agent", "MyAppIntegration/1.0");

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(10); // 10 saniye timeout

        Console.WriteLine($"[TrendyolService] Sayfa {page} için istek başlatılıyor...");

        HttpResponseMessage response;
        try
        {
            response = await httpClient.SendAsync(request);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TrendyolService] HttpRequest hatası: {ex.Message}");
            return null;
        }

        Console.WriteLine($"[TrendyolService] Response status: {response.StatusCode}");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[TrendyolService] Hatalı response: {await response.Content.ReadAsStringAsync()}");
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"[TrendyolService] Response alındı, uzunluk: {json.Length}");

        return JsonSerializer.Deserialize<TrendyolProductResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }


    public async Task<List<TrendyolProductDto>> GetAllProductsAsync(long supplierId, string apiKey, string apiSecret, int pageSize = 200)
    {
        var allProducts = new List<TrendyolProductDto>();
        int currentPage = 0;
        bool hasMore = true;

        while (hasMore)
        {
            var data = await GetProductsAsync(supplierId, apiKey, apiSecret, currentPage, pageSize);
            if (data?.Content != null && data.Content.Any())
            {
                allProducts.AddRange(data.Content);
                currentPage++;
                hasMore = data.Content.Count == pageSize; // son sayfa kontrolü
            }
            else
            {
                hasMore = false;
            }
        }

        return allProducts;
    }

    public async Task<List<TrendyolProductDto>> GetProductsByBarcodesAsync(
     long supplierId, string apiKey, string apiSecret, List<string> barcodes)
    {
        if (barcodes == null || barcodes.Count == 0)
            throw new ArgumentException("En az bir barkod girilmeli");

        var httpClient = _httpClientFactory.CreateClient();
        var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiKey}:{apiSecret}"));
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
        httpClient.DefaultRequestHeaders.Add("User-Agent", "OtoEntegre/1.0");

        // ✅ Barkodları virgül ile birleştir
        var joinedBarcodes = string.Join(",", barcodes);
        var url = $"https://apigw.trendyol.com/integration/product/sellers/{supplierId}/products?barcode={joinedBarcodes}";

        Console.WriteLine($"[TrendyolService] URL: {url}");

        var response = await httpClient.GetAsync(url);
        var responseBody = await response.Content.ReadAsStringAsync();
Console.WriteLine($"[TrendyolService] Barkodlar: {joinedBarcodes}");
Console.WriteLine($"[TrendyolService] Response: {responseBody}");

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[TrendyolService] Hata: {response.StatusCode}, Body: {responseBody}");
            return new List<TrendyolProductDto>();
        }

        var data = JsonSerializer.Deserialize<TrendyolProductResponse>(
            responseBody,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
        );

        return data?.Content?.ToList() ?? new List<TrendyolProductDto>();
    }


    public async Task<TrendyolOrderDto?> GetOrderByCodeAsync(string orderCode, long supplierId, string apiKey, string apiSecret)
    {
        var client = _httpClientFactory.CreateClient();

        var url = $"https://api.trendyol.com/sapigw/suppliers/{supplierId}/orders?orderNumber={orderCode}";

        client.DefaultRequestHeaders.Clear();

        // Trendyol Basic Auth zorunlu!
        var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiKey}:{apiSecret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
        client.DefaultRequestHeaders.Add("User-Agent", "OtoEntegre/1.0");

        var response = await client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"Trendyol API Response Status: {response.StatusCode}");
        Console.WriteLine($"Trendyol API Response Body: {body}");

        if (!response.IsSuccessStatusCode)
            return null;

        return JsonSerializer.Deserialize<TrendyolOrderDto>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
  public async Task<List<TrendyolOrderPayload>> GetOrdersAsync(
    long supplierId, string apiKey, string apiSecret,
    DateTime? startDate = null, DateTime? endDate = null,
    int page = 0, int size = 200)
{
    Console.WriteLine("getorderasync başladı");
    var client = _httpClientFactory.CreateClient();

    var auth = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiKey}:{apiSecret}"));
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
    client.DefaultRequestHeaders.Add("User-Agent", "OtoEntegre/1.0");

    var url = $"https://apigw.trendyol.com/integration/order/sellers/{supplierId}/orders?";
    var queryParams = new List<string>();

    if (startDate.HasValue)
        queryParams.Add($"startDate={new DateTimeOffset(startDate.Value.ToUniversalTime()).ToUnixTimeMilliseconds()}");
    if (endDate.HasValue)
        queryParams.Add($"endDate={new DateTimeOffset(endDate.Value.ToUniversalTime()).ToUnixTimeMilliseconds()}");

    url += string.Join("&", queryParams);

    var response = await client.GetAsync(url);
    var body = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"Orders API Hatası: {response.StatusCode} {body}");
        return new List<TrendyolOrderPayload>();
    }

    var data = JsonSerializer.Deserialize<TrendyolOrdersResponse>(body,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    return data?.Content ?? new List<TrendyolOrderPayload>();
}


}

public class TrendyolOrdersResponse
{
    public List<TrendyolOrderPayload> Content { get; set; } = new();
    public int TotalPages { get; set; }
    public int TotalElements { get; set; }
}

public class TrendyolOrderDto
{
    public List<TrendyolOrderLine> Lines { get; set; } = new();
}

public class TrendyolOrderLine
{
    public string Barcode { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class TrendyolProductResponse
{
    public List<TrendyolProductDto> Content { get; set; } = new List<TrendyolProductDto>();
}

public class TrendyolProductDto
{
    public long ProductCode { get; set; } // int değilse long kullan
    public string Barcode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal SalePrice { get; set; }
    public string ProductMainId { get; set; } = string.Empty;
    public string ProductUrl { get; set; } = string.Empty;
    public List<TrendyolImageDto> Images { get; set; } = new List<TrendyolImageDto>();
}

public class TrendyolImageDto
{
    public string Url { get; set; } = string.Empty;
}


public class TrendyolProductsResponse
{
    public int page { get; set; }
    public int size { get; set; }
    public int totalElements { get; set; }
    public int totalPages { get; set; }
    public List<TrendyolProduct> content { get; set; } = new List<TrendyolProduct>();
}

public class TrendyolProduct
{
    public string id { get; set; } = string.Empty;
    public long productCode { get; set; }
    public string title { get; set; } = string.Empty;
    public int pimCategoryId { get; set; }
    public int quantity { get; set; }
    public decimal salePrice { get; set; }
    public List<TrendyolProductImage> images { get; set; } = new List<TrendyolProductImage>();
}

public class TrendyolProductImage
{
    public string url { get; set; } = null!;
}
