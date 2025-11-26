using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using OtoEntegre.Api.DTOs;
using System.Text;
using BarcodeStandard;
using SixLabors.ImageSharp;

public class TrendyolService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public TrendyolService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

   public async Task<TrendyolProductsResponse?> GetProductsAsync(
    long supplierId,
    string apiKey,
    string apiSecret,
    int page = 0,
    int size = 1000,
    string? search = null,
    string? barcode = null,
    bool? approved = null,
    bool? archived = null,
    bool? onSale = null,
    bool? rejected = null,
    bool? blacklisted = null
)
{
    var url = $"https://apigw.trendyol.com/integration/product/sellers/{supplierId}/products?page={page}&size={size}";

    if (!string.IsNullOrEmpty(search))
        url += $"&searchText={Uri.EscapeDataString(search)}";

    if (!string.IsNullOrEmpty(barcode))
        url += $"&barcode={Uri.EscapeDataString(barcode)}";

    if (approved.HasValue)
        url += $"&approved={approved.Value.ToString().ToLower()}";
    if (archived.HasValue)
        url += $"&archived={archived.Value.ToString().ToLower()}";
    if (onSale.HasValue)
        url += $"&onSale={onSale.Value.ToString().ToLower()}";
    if (rejected.HasValue)
        url += $"&rejected={rejected.Value.ToString().ToLower()}";
    if (blacklisted.HasValue)
        url += $"&blacklisted={blacklisted.Value.ToString().ToLower()}";

    var request = new HttpRequestMessage(HttpMethod.Get, url);

    var auth = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{apiKey}:{apiSecret}"));
    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
    request.Headers.Add("User-Agent", "MyAppIntegration/1.0");

    var httpClient = _httpClientFactory.CreateClient();
    httpClient.Timeout = TimeSpan.FromSeconds(10);

    HttpResponseMessage response;
    try
    {
        response = await httpClient.SendAsync(request);
    }
    catch
    {
        return null;
    }

    if (!response.IsSuccessStatusCode)
        return null;

    var json = await response.Content.ReadAsStringAsync();

    return JsonSerializer.Deserialize<TrendyolProductsResponse>(json, new JsonSerializerOptions
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
            if (data?.content != null && data.content.Any())
            {
                // map TrendyolProduct -> TrendyolProductDto
                var mapped = data.content.Select(p => new TrendyolProductDto
                {
                    ProductCode = p.productCode,
                    Barcode = p.id ?? string.Empty,
                    Title = p.title ?? string.Empty,
                    Quantity = p.quantity,
                    SalePrice = p.salePrice,
                    ProductMainId = p.id ?? string.Empty,
                    Images = p.images?.Select(i => new TrendyolImageDto { Url = i.url }).ToList() ?? new List<TrendyolImageDto>()
                }).ToList();

                allProducts.AddRange(mapped);
                currentPage++;
                hasMore = data.content.Count == pageSize; // son sayfa kontrolü
            }
            else
            {
                hasMore = false;
            }
        }

        return allProducts;
    }

    public async Task<bool> CreateProductAsync(string apiKey, string apiSecret, int sellerId, object productData)
    {
        using var client = new HttpClient();
        client.BaseAddress = new Uri("https://apigw.trendyol.com/integration/product/sellers/");
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{apiKey}:{apiSecret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var json = System.Text.Json.JsonSerializer.Serialize(productData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"{sellerId}/products", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"[TrendyolService] Response: {responseBody}");

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Trendyol API Hatası ({response.StatusCode}): {responseBody}");
        }

        return true;
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

    public async Task<(bool Success, string Message)> AddProductAsync(
        long supplierId, string apiKey, string apiSecret, object productPayload)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "OtoEntegre");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}")));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var url = $"https://apigw.trendyol.com/integration/product/sellers/{supplierId}/products";
        var json = JsonSerializer.Serialize(productPayload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, content);
        var body = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"[TrendyolService] CreateProduct Response: {body}");

        if (!response.IsSuccessStatusCode)
            return (false, $"API Hatası: {response.StatusCode} - {body}");

        // ✅ Trendyol batchRequestId döner
        // ✅ Trendyol batchRequestId döner
        var jsonDoc = JsonDocument.Parse(body);
        if (!jsonDoc.RootElement.TryGetProperty("batchRequestId", out var batchIdElement))
            return (false, "batchRequestId alınamadı, ürün eklenmemiş olabilir.");

        var batchId = batchIdElement.ValueKind == JsonValueKind.String
            ? batchIdElement.GetString()
            : batchIdElement.GetInt64().ToString();

        // ✅ Batch sonucunu sorgula
        await Task.Delay(3000); // 3 sn bekleme (isteğe bağlı)
        var checkUrl = $"https://apigw.trendyol.com/integration/product/sellers/{supplierId}/products/batch-requests/{batchId}";
        var checkResponse = await client.GetAsync(checkUrl);

        var checkBody = await checkResponse.Content.ReadAsStringAsync();

        Console.WriteLine($"[TrendyolService] Batch kontrol cevabı: {checkBody}");

        if (!checkResponse.IsSuccessStatusCode)
            return (false, $"Batch sorgusu başarısız: {checkBody}");

        if (checkBody.Contains("APPROVED"))
            return (true, "Ürün başarıyla Trendyol hesabına eklendi.");
        else if (checkBody.Contains("REJECTED"))
            return (false, $"Ürün reddedildi: {checkBody}");

        return (true, "Ürün Trendyol’a gönderildi, işlem sonucu bekleniyor.");
    }

    /// <summary>
    /// Update price and inventory for one or more items by barcode using Trendyol's
    /// POST /integration/inventory/sellers/{sellerId}/products/price-and-inventory
    /// Expects body: { items: [ { barcode, quantity, salePrice, listPrice } ] }
    /// Returns batchRequestId on success.
    /// </summary>
    public async Task<(bool Success, string Message, string? BatchRequestId)> UpdatePriceAndInventoryByBarcodeAsync(
        long sellerId, string apiKey, string apiSecret,
        List<(string barcode, int? quantity, decimal? salePrice, decimal? listPrice)> items)
    {
        if (items == null || items.Count == 0)
            return (false, "No items provided.", null);

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "OtoEntegre");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}")));
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var url = $"https://apigw.trendyol.com/integration/inventory/sellers/{sellerId}/products/price-and-inventory";

        var payload = new
        {
            items = items.Select(i => new
            {
                barcode = i.barcode,
                quantity = i.quantity ?? 0,
                salePrice = i.salePrice ?? 0m,
                listPrice = i.listPrice ?? 0m
            }).ToArray()
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, content);
        var body = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"[TrendyolService] updatePriceAndInventory Response: {body}");

        if (!response.IsSuccessStatusCode)
            return (false, $"API Hatası: {response.StatusCode} - {body}", null);

        try
        {
            var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("batchRequestId", out var idEl))
            {
                var id = idEl.GetString();
                return (true, "Güncelleme isteği alındı.", id);
            }

            return (true, "Güncelleme isteği alındı, ancak batchRequestId bulunamadı.", null);
        }
        catch (Exception ex)
        {
            return (true, $"Güncelleme isteği alındı, parse hatası: {ex.Message}", null);
        }
    }

    public async Task<string?> GetBatchResultAsync(
    long sellerId, string apiKey, string apiSecret, string batchRequestId)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "OtoEntegre");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}")));

        var url =
            $"https://apigw.trendyol.com/integration/product/sellers/{sellerId}/products/batch-requests/{batchRequestId}";

        var response = await client.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();

        Console.WriteLine($"[TrendyolService] GetBatchResult Response: {body}");

        if (!response.IsSuccessStatusCode)
            return null;

        return body;
    }


    public async Task<List<TrendyolOrderPayload>> GetOrdersByProductCodeAsync(
        long supplierId, string apiKey, string apiSecret, long productCode,
        DateTime? startDate = null, DateTime? endDate = null)
    {
        var allOrders = await GetOrdersAsync(supplierId, apiKey, apiSecret, startDate, endDate);
        Console.WriteLine($"[{nameof(TrendyolService)}] Toplam {allOrders.Count} sipariş alındı.");
        // Trendyol tüm siparişleri döner, biz productCode’a göre filtreliyoruz.

        Console.WriteLine($"[{nameof(TrendyolService)}] {allOrders.Count} sipariş bulundu (productCode: {productCode}).");

        return allOrders;
    }

    public async Task<List<TrendyolOrderPayload>> GetOrdersAsync(
    long supplierId, string apiKey, string apiSecret,
    DateTime? startDate = null, DateTime? endDate = null,
    string? status = null, // Yeni parametre
    int page = 0, int size = 200)
    {
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

        // Tüm sipariş durumlarını almak için status parametresini ekle
        if (!string.IsNullOrEmpty(status))
            queryParams.Add($"status={status}");

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

    
    public int vatRate { get; set; }
    public int returningAddressId { get; set; }
    public int shipmentAddressId { get; set; }
    public int cargoCompanyId { get; set; }
public double? dimensionalWeight { get; set; }   // ✅ Doğru
    public int deliveryDuration { get; set; }
    public string stockCode {get;set;} = string.Empty;
    public string id { get; set; } = string.Empty;
    public long productCode { get; set; }
    public string title { get; set; } = string.Empty;
    public string categoryName { get; set; } = string.Empty;
    public int pimCategoryId { get; set; }
    public int quantity { get; set; }
    public decimal salePrice { get; set; }
    public decimal listPrice { get; set; }
    public bool onSale { get; set; }
    public string brand { get; set; } = string.Empty;
    public long brandId { get; set; }
    public string description { get; set; } = string.Empty;
    public bool approved { get; set; }
    public bool archived { get; set; }
    public long createDateTime { get; set; }
    public long lastUpdateDate { get; set; }
    public string productMainId { get; set; } = string.Empty;
    public string productUrl { get; set; } = string.Empty;
    public bool rejected { get; set; } 
    public bool blacklisted { get; set; }
    public List<TrendyolProductImage> images { get; set; } = new List<TrendyolProductImage>();
    public List<TrendyolProductAttribute> attributes { get; set; } = new List<TrendyolProductAttribute>();
    public string barcode { get; set; } = string.Empty;
}
public class TrendyolProductAttribute
{
    public int attributeId { get; set; }
    public string attributeName { get; set; } = string.Empty;
    public string attributeValue { get; set; } = string.Empty;
    public long? attributeValueId { get; set; }
}
public class TrendyolProductImage
{
    public string url { get; set; } = null!;
}
