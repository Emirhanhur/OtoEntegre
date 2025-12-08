using System.Text;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using OtoEntegre.Api.DTOs;
using System;
using System.Text.Json;


namespace OtoEntegre.Api.Services
{
    public class OtostickerService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TelegramService _telegramService;

        public OtostickerService(IHttpClientFactory httpClientFactory, TelegramService telegramService)
        {
            _httpClientFactory = httpClientFactory;
            _telegramService = telegramService;
        }
        public async Task<OtostickerFastSaleOrderResponse> CreateOrderAsync(string email, string password, OtostickerFastSaleOrderDto orderDto)
        {
            var client = _httpClientFactory.CreateClient("Otosticker");
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var requestBody = new
            {
                email,
                password,
                order = orderDto
            };

            var json = System.Text.Json.JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.otosticker.com/api/orders/createFastSale", content);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return new OtostickerFastSaleOrderResponse
                {
                    Success = false,
                    Message = $"HTTP {response.StatusCode}: {responseString}"
                };
            }

            return System.Text.Json.JsonSerializer.Deserialize<OtostickerFastSaleOrderResponse>(responseString)
                   ?? new OtostickerFastSaleOrderResponse { Success = false, Message = "Yanıt deserialize edilemedi" };
        }

        public async Task<OtostickerFastSaleOrderResponse> CreateFastSaleOrderAsync(OtostickerFastSaleOrderDto order)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri("https://www.otosticker.com.tr/api/v2/");
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("apikey", "eba11f4b-2b1f-444e-a777-ec6872c95601");
                client.DefaultRequestHeaders.Add("apisecret", "e81498b3f3865c5bd2a5b2a6ab69028e5RbSwX32Q3CMlAwUDg==");
                client.DefaultRequestHeaders.Add("Cookie", "ecom_orcode=ff0dbfe2b0f475b52145d08407217ffftVm1PbuvhhZo");

                var response = await client.PostAsJsonAsync("order/fastSale", order);
                response.EnsureSuccessStatusCode();
                var json = JsonSerializer.Serialize(order, new JsonSerializerOptions { WriteIndented = true });

                var result = await response.Content.ReadFromJsonAsync<OtostickerFastSaleOrderResponse>();
                return result ?? new OtostickerFastSaleOrderResponse { Success = false, Code = "" };
            }
            catch (Exception ex)
            {
                return new OtostickerFastSaleOrderResponse
                {
                    Success = false,
                    Code = "",
                    Message = ex.Message
                };
            }
        }
        public class OtostickerLoginDto
        {
            public string Email { get; set; } = "";
            public string Password { get; set; } = "";
        }

        public class OtostickerProductSearchDto
        {
            public string ProductName { get; set; } = "";
        }


        // OtostickerService içine ekle
        public async Task<OtoStickerProduct?> GetProductByBarcodeAsync(string barcode)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://www.otosticker.com.tr");
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apikey", "eba11f4b-2b1f-444e-a777-ec6872c95601");
            client.DefaultRequestHeaders.Add("apisecret", "e81498b3f3865c5bd2a5b2a6ab69028e5RbSwX32Q3CMlAwUDg==");

            var response = await client.GetAsync($"/api/v2/product/lists?barcode={barcode}");
            var content = await response.Content.ReadAsStringAsync();


            if (!response.IsSuccessStatusCode) return null;

            try
            {
                using var doc = JsonDocument.Parse(content);
                var productEl = doc.RootElement.GetProperty("result").GetProperty("list")[0];

                return new OtoStickerProduct
                {
                    ProductId = productEl.GetProperty("productId").GetString()!,
                    SalePrice = productEl.GetProperty("salePrice").GetDecimal()
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine("OtoSticker product parse hatası: " + ex.Message);
                return null;
            }
        }

        public class OtoStickerProduct
        {
            public string ProductId { get; set; } = null!;
            public decimal SalePrice { get; set; }
        }

        public async Task<JsonDocument?> GetOrderListAsync(int dealerId)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://www.otosticker.com.tr");
            client.DefaultRequestHeaders.Clear();

            client.DefaultRequestHeaders.Add("apikey", "eba11f4b-2b1f-444e-a777-ec6872c95601");
            client.DefaultRequestHeaders.Add("apisecret", "e81498b3f3865c5bd2a5b2a6ab69028e5RbSwX32Q3CMlAwUDg==");

            var url = $"/api/v2/order/lists?pageStart=0&pageSize=10&orderBy=id&sort=desc&dealerId={dealerId}";

            var response = await client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            Console.WriteLine("=== OtoSticker Order List Response ===");
            Console.WriteLine(content);
            Console.WriteLine("=====================================");

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"⚠️ OtoSticker sipariş listesi alınamadı: {response.StatusCode}");
                return null;
            }

            try
            {
                return JsonDocument.Parse(content);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ OtoSticker sipariş listesi parse hatası: {ex.Message}");
                return null;
            }
        }


        public async Task<string> CreateFastSaleAsync(object fastSaleRequest, OtostickerDealerDto dealer, Guid kullaniciId)
        {

            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://www.otosticker.com.tr");
            client.DefaultRequestHeaders.Clear();

            client.DefaultRequestHeaders.Add("apikey", "eba11f4b-2b1f-444e-a777-ec6872c95601");
            client.DefaultRequestHeaders.Add("apisecret", "e81498b3f3865c5bd2a5b2a6ab69028e5RbSwX32Q3CMlAwUDg==");

            // 🔹 JSON oluşturma
            var json = JsonSerializer.Serialize(fastSaleRequest, new JsonSerializerOptions
            {
                WriteIndented = true // okunabilir hale getir
            });

            // 🔹 JSON’u konsola yaz
            Console.WriteLine("=== OtoSticker Gönderilen JSON ===");
            Console.WriteLine(json);
            Console.WriteLine("===================================");

            // 🔹 (isteğe bağlı) Telegram’a da log göndermek istersen:

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/api/v2/order/fastSale", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            // 🔹 API cevabını da yaz
            Console.WriteLine("=== OtoSticker Response ===");
            Console.WriteLine(responseContent);
            Console.WriteLine("===========================");

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Otosticker Hatası: {response.StatusCode} - {responseContent}");

            return responseContent;
        }



        public async Task<OtostickerDealerListResponse?> GetDealerListAsync()
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://www.otosticker.com.tr");
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apikey", "eba11f4b-2b1f-444e-a777-ec6872c95601");
            client.DefaultRequestHeaders.Add("apisecret", "e81498b3f3865c5bd2a5b2a6ab69028e5RbSwX32Q3CMlAwUDg==");

            int pageStart = 0;
            int pageSize = 100; // Otosticker API’nin limiti büyük ihtimalle 100
            var allDealers = new List<OtostickerDealerDto>();

            while (true)
            {
                var response = await client.GetAsync($"/api/v2/dealer/lists?");
                if (!response.IsSuccessStatusCode)
                    throw new Exception($"Dealer listesi çekilemedi (pageStart={pageStart})");

                var json = await response.Content.ReadAsStringAsync();
                var result = System.Text.Json.JsonSerializer.Deserialize<OtostickerDealerListResponse>(json);

                if (result?.Result?.List == null || result.Result.List.Count == 0)
                    break;

                allDealers.AddRange(result.Result.List);

                // Eğer dönen kayıt sayısı pageSize'dan azsa sayfa bitmiştir
                if (result.Result.List.Count < pageSize)
                    break;

                pageStart++;
            }

            // Tüm sayfalardaki verileri tek response altında döndür
            return new OtostickerDealerListResponse
            {
                Code = "200",
                Result = new OtostickerDealerResult
                {
                    Total = allDealers.Count,
                    PageSize = allDealers.Count,
                    List = allDealers
                }
            };
        }





        public class OtostickerDealerListResponse
        {
            [JsonPropertyName("code")]
            public string Code { get; set; } = string.Empty;

            [JsonPropertyName("result")]
            public OtostickerDealerResult Result { get; set; } = new();
        }

        public class OtostickerDealerResult
        {
            [JsonPropertyName("total")]
            public int Total { get; set; }

            [JsonPropertyName("pageSize")]
            public int PageSize { get; set; }

            [JsonPropertyName("list")]
            public List<OtostickerDealerDto> List { get; set; } = new();
        }

        public class OtostickerDealerDto
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("email")]
            public string Email { get; set; } = string.Empty;

            [JsonPropertyName("name")]
            public string Name { get; set; } = string.Empty;

            [JsonPropertyName("lastname")]
            public string Lastname { get; set; } = string.Empty;

            [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty; 

            [JsonPropertyName("title")]
            public string? Title { get; set; }

            [JsonPropertyName("group")]
            public string? Group { get; set; }

            [JsonPropertyName("status")]
            public int Status { get; set; }

            [JsonPropertyName("balance")]
            public decimal Balance { get; set; }

            [JsonPropertyName("discount")]
            public decimal Discount { get; set; }

            [JsonPropertyName("nationalId")]
            public string? NationalId { get; set; }

            [JsonPropertyName("taxId")]
            public string? TaxId { get; set; }

            [JsonPropertyName("taxBranch")]
            public string? TaxBranch { get; set; }

            [JsonPropertyName("phone")]
            public string? Phone { get; set; }

            [JsonPropertyName("nBalanceStatus")]
            public int? NBalanceStatus { get; set; }

            [JsonPropertyName("nBalanceLimit")]
            public int? NBalanceLimit { get; set; }

        }


        public async Task<OtostickerLoginResponse> LoginAsync(string email, string password)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                var payload = new { email, password };
                var response = await client.PostAsJsonAsync("https://www.otosticker.com.tr/api/v2/login", payload);
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadFromJsonAsync<OtostickerLoginResponse>();
                return result ?? new OtostickerLoginResponse { Success = false, Message = "Login failed" };
            }
            catch (Exception ex)
            {
                return new OtostickerLoginResponse { Success = false, Message = ex.Message };
            }
        }
        public class OtostickerLoginResponse
        {
            public bool Success { get; set; }
            public string Message { get; set; } = "";
            public string Token { get; set; } = ""; // Eğer API login sonrası token döndürüyorsa
        }
        public async Task<OtostickerSearchResult> SearchProductAsync(string email, string password, string productName)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("apikey", "eba11f4b-2b1f-444e-a777-ec6872c95601");
                client.DefaultRequestHeaders.Add("apisecret", "e81498b3f3865c5bd2a5b2a6ab69028e5RbSwX32Q3CMlAwUDg==");

                // 1️⃣ Auth kontrolü
                var authResponse = await client.GetAsync("https://www.otosticker.com.tr/api/v2/auth");
                if (!authResponse.IsSuccessStatusCode)
                {
                    return new OtostickerSearchResult
                    {
                        Success = false,
                        Message = $"Auth başarısız: {authResponse.StatusCode}"
                    };
                }

                // 2️⃣ Ürün arama isteği
                var response = await client.GetAsync(
                    $"https://www.otosticker.com.tr/api/v2/product/lists?search={Uri.EscapeDataString(productName)}&pageIndex=1&pageSize=50"
                );

                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("result", out var resultEl) &&
                    resultEl.TryGetProperty("list", out var listEl))
                {
                    var products = JsonSerializer.Deserialize<List<OtostickerProductDto>>(listEl.GetRawText(), options)
                                   ?? new List<OtostickerProductDto>();

                    return new OtostickerSearchResult
                    {
                        Success = true,
                        Products = products
                    };
                }

                return new OtostickerSearchResult { Success = false, Message = "Ürün listesi boş döndü." };
            }
            catch (Exception ex)
            {
                return new OtostickerSearchResult
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public class OtostickerSearchResult
        {
            public bool Success { get; set; }
            public string Message { get; set; } = "";
            public List<OtostickerProductDto> Products { get; set; } = new();
        }

        public async Task<List<OtostickerProductDto>> GetUserProductsAsync(string email, string password, string productName)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://www.otosticker.com.tr/api/v2/");
            client.DefaultRequestHeaders.Clear();

            // Header'lara API key ve secret ekle
            client.DefaultRequestHeaders.Add("apikey", "eba11f4b-2b1f-444e-a777-ec6872c95601");
            client.DefaultRequestHeaders.Add("apisecret", "e81498b3f3865c5bd2a5b2a6ab69028e5RbSwX32Q3CMlAwUDg==");

            // Kullanıcı login request body
            var loginBody = new
            {
                email = email,
                password = password
            };

            try
            {
                // 🔹 Login isteği
                var loginResponse = await client.PostAsJsonAsync("web_servis/login", loginBody);
                if (!loginResponse.IsSuccessStatusCode)
                    throw new Exception("Giriş başarısız.");


                // Cookie veya token al (otosticker genellikle session cookie döner)
                if (!loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies))
                    throw new Exception("Giriş sonrası oturum bilgisi alınamadı.");

                string cookieHeader = string.Join(";", cookies);
                client.DefaultRequestHeaders.Add("Cookie", cookieHeader);

                // 🔹 Ürün listesini çek (ürün adını filtreleyerek)
                var response = await client.GetAsync($"product/lists?search={Uri.EscapeDataString(productName)}&pageIndex=1&pageSize=50");
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("result", out var resultEl) &&
                    resultEl.TryGetProperty("list", out var listEl) &&
                    listEl.ValueKind == JsonValueKind.Array)
                {
                    return System.Text.Json.JsonSerializer.Deserialize<List<OtostickerProductDto>>(listEl.GetRawText(), options)
                           ?? new List<OtostickerProductDto>();
                }

                return new List<OtostickerProductDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Otosticker login/product hata: {ex.Message}");
                return new List<OtostickerProductDto>();
            }
        }

        public async Task<OtostickerFastSaleOrderResponse> CreateUserFastSaleOrderAsync(string email, string password, OtostickerFastSaleOrderDto order)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri("https://www.otosticker.com.tr/api/v2/");
            client.DefaultRequestHeaders.Clear();

            client.DefaultRequestHeaders.Add("apikey", "eba11f4b-2b1f-444e-a777-ec6872c95601");
            client.DefaultRequestHeaders.Add("apisecret", "e81498b3f3865c5bd2a5b2a6ab69028e5RbSwX32Q3CMlAwUDg==");

            try
            {
                // 🔹 Oturum aç
                var loginBody = new { email = email, password = password };
                var loginResponse = await client.PostAsJsonAsync("web_servis/login", loginBody);
                if (!loginResponse.IsSuccessStatusCode)
                    throw new Exception("Giriş başarısız.");

                // Cookie veya token al
                if (!loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies))
                    throw new Exception("Oturum bilgisi alınamadı.");

                string cookieHeader = string.Join(";", cookies);
                client.DefaultRequestHeaders.Add("Cookie", cookieHeader);

                // 🔹 Sipariş oluştur
                var response = await client.PostAsJsonAsync("order/fastSale", order);
                var json = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new OtostickerFastSaleOrderResponse
                    {
                        Success = false,
                        Message = $"Sipariş başarısız: {response.StatusCode}",
                        Code = ""
                    };
                }

                var result = JsonSerializer.Deserialize<OtostickerFastSaleOrderResponse>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return result ?? new OtostickerFastSaleOrderResponse { Success = false, Message = "Boş yanıt" };
            }
            catch (Exception ex)
            {
                return new OtostickerFastSaleOrderResponse
                {
                    Success = false,
                    Message = ex.Message,
                    Code = ""
                };
            }
        }


        // ✅ Otosticker ürünlerini çekmek için metod
        public async Task<List<OtostickerProductDto>> GetAllProductsAsync()
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            var apiKey = "eba11f4b-2b1f-444e-a777-ec6872c95601";
            var apiSecret = "e81498b3f3865c5bd2a5b2a6ab69028e5RbSwX32Q3CMlAwUDg==";
            client.DefaultRequestHeaders.Add("apiKey", apiKey);
            client.DefaultRequestHeaders.Add("apiSecret", apiSecret);

            var allProducts = new List<OtostickerProductDto>();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            int pageIndex = 1;
            int pageSize = 50;
            bool moreData = true;

            while (moreData)
            {
                var response = await client.GetAsync(
                    $"https://www.otosticker.com.tr/api/v2/product/lists?pageIndex={pageIndex}&pageSize={pageSize}"
                );

                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(content);
                var root = doc.RootElement;

                if (root.ValueKind == JsonValueKind.Object &&
                    root.TryGetProperty("result", out var resultEl) &&
                    resultEl.ValueKind == JsonValueKind.Object &&
                    resultEl.TryGetProperty("list", out var listEl) &&
                    listEl.ValueKind == JsonValueKind.Array)
                {
                    var products = JsonSerializer.Deserialize<List<OtostickerProductDto>>(listEl.GetRawText(), options)
                                   ?? new List<OtostickerProductDto>();

                    allProducts.AddRange(products);

                    // eğer gelen ürün sayısı pageSize'dan küçükse demek ki son sayfaya ulaştık
                    if (products.Count < 10)
                    {
                        moreData = false;
                    }
                    else
                    {
                        pageIndex++;
                    }
                }
                else
                {
                    moreData = false; // beklenmedik response gelirse çık
                }
            }

            return allProducts;
        }



        public async Task<decimal?> GetProductPriceAsync(string barcode)
        {
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri($"https://www.otosticker.com.tr/api/v2/product/lists?");
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apikey", "eba11f4b-2b1f-444e-a777-ec6872c95601");
            client.DefaultRequestHeaders.Add("apisecret", "e81498b3f3865c5bd2a5b2a6ab69028e5RbSwX32Q3CMlAwUDg==");
            client.DefaultRequestHeaders.Add("Cookie", "ecom_orcode=ff0dbfe2b0f475b52145d08407217ffftVm1PbuvhhZo");
            var response = await client.GetAsync($"barcode={barcode}");
            Console.WriteLine("=== OtoSticker Product Price Response ===");

            var content = await response.Content.ReadAsStringAsync();
            Console.WriteLine(content);
            Console.WriteLine("=========================================");
            if (!response.IsSuccessStatusCode)
                return null;

            try
            {
                using var doc = JsonDocument.Parse(content);
                var price = doc.RootElement
                    .GetProperty("result")
                    .GetProperty("price")
                    .GetDecimal();

                return price;
            }
            catch
            {
                return null;
            }
        }


    }
}
