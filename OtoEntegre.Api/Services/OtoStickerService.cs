using System.Net.Http;
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

        public OtostickerService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
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

        // ✅ Otosticker ürünlerini çekmek için metod
        public async Task<List<OtostickerProductDto>> GetAllProductsAsync()
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apiKey", "eba11f4b-2b1f-444e-a777-ec6872c95601");
            client.DefaultRequestHeaders.Add("apiSecret", "e81498b3f3865c5bd2a5b2a6ab69028e5RbSwX32Q3CMlAwUDg==");

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





    }
}
