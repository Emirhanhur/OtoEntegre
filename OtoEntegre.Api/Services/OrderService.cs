using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using OtoEntegre.Api.DTOs;

namespace OtoEntegre.Api.Services
{
    public class OrderService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public OrderService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        /// <summary>
        /// DealerId’ye ait tüm siparişlerin detaylı listesini döner.
        /// </summary>

        private HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apiKey", "eba11f4b-2b1f-444e-a777-ec6872c95601");
            client.DefaultRequestHeaders.Add("apiSecret", "e81498b3f3865c5bd2a5b2a6ab69028e5RbSwX32Q3CMlAwUDg==");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            return client;
        }
        public async Task<List<OrderDto>> GetOrdersByDealerIdAsync(int dealerId, int pageStart, int pageSize)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apiKey", "eba11f4b-2b1f-444e-a777-ec6872c95601");
            client.DefaultRequestHeaders.Add("apiSecret", "e81498b3f3865c5bd2a5b2a6ab69028e5RbSwX32Q3CMlAwUDg==");
            Console.WriteLine("sürekli çalışan yer burası = OrderService 29");

            var response = await client.GetAsync(
                $"https://www.otosticker.com.tr/api/v2/order/lists?"
            );

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var apiResponse = JsonSerializer.Deserialize<OrderApiResponse>(content, options);

            var currentOrders = apiResponse?.Result?.List ?? new List<OrderDto>();

            // DealerId filtreleme
            return currentOrders.Where(o => o.DealerId == dealerId).ToList();
        }

        public async Task<List<OrderDto>> GetOrdersAsync(int pageStart, int pageSize)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apikey", "eba11f4b-2b1f-444e-a777-ec6872c95601");
            client.DefaultRequestHeaders.Add("apisecret", "e81498b3f3865c5bd2a5b2a6ab69028e5RbSwX32Q3CMlAwUDg==");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            Console.WriteLine("sürekli çalışan yer burası = OrderService 59");

            var response = await client.GetAsync(
                $"https://www.otosticker.com.tr/api/v2/order/lists?pageStart={pageStart}&pageSize={pageSize}"
            );

            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var apiResponse = JsonSerializer.Deserialize<OrderApiResponse>(content, options);

            return apiResponse?.Result?.List ?? new List<OrderDto>();
        }

        public async Task<OrderDetailApiResponse?> GetOrderByIdAsync(int orderId)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apiKey", "eba11f4b-2b1f-444e-a777-ec6872c95601");
            client.DefaultRequestHeaders.Add("apiSecret", "e81498b3f3865c5bd2a5b2a6ab69028e5RbSwX32Q3CMlAwUDg==");

            var response = await client.GetAsync($"https://www.otosticker.com.tr/api/v2/order/show/{orderId}");
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<OrderDetailApiResponse>(content, options);
        }

        public async Task<int> GetTotalOrderCountByDealerIdAsync(int dealerId)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apiKey", "...");
            client.DefaultRequestHeaders.Add("apiSecret", "...");
            Console.WriteLine("sürekli çalışan yer burası = OrderService 92");

            var response = await client.GetAsync(
       $"https://www.otosticker.com.tr/api/v2/order/lists?dealerId={dealerId}&pageStart=0&pageSize=1"
   );
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var apiResponse = JsonSerializer.Deserialize<OrderApiResponse>(content, options);
            return apiResponse?.Result?.Total ?? 0;
        }

       public async Task<List<OrderDetailDto>> GetOrdersWithDetailsByDealerIdAsync(int dealerId, int pageStart = 0, int pageSize = 100)
    {
        var detailedOrders = new List<OrderDetailDto>();
        var client = CreateClient();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        try
        {
            var response = await client.GetAsync(
                $"https://www.otosticker.com.tr/api/v2/order/lists?dealerId={dealerId}&pageStart={pageStart}&pageSize={pageSize}"
            );

            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<OrderApiResponse>(content, options);
            var orders = apiResponse?.Result?.List ?? new List<OrderDto>();

            // Detayları paralel çek
            var detailTasks = orders.Select(async order =>
            {
                try
                {
                    var detailResp = await client.GetAsync($"https://www.otosticker.com.tr/api/v2/order/show/{order.Id}");
                    if (!detailResp.IsSuccessStatusCode) return null;

                    var detailContent = await detailResp.Content.ReadAsStringAsync();
                    var detail = JsonSerializer.Deserialize<OrderDetailApiResponse>(detailContent, options);
                    if (detail?.Result == null) return null;

                    return new OrderDetailDto
                    {
                        Summary = detail.Result.Summary,
                        Customer = detail.Result.Customer,
                        Products = detail.Result.Products,
                        Order = detail.Result.Order
                    };
                }
                catch
                {
                    return null;
                }
            });

            var results = await Task.WhenAll(detailTasks);
            detailedOrders = results.Where(d => d != null).ToList()!;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"🔥 API çağrısı sırasında hata: {ex.Message}");
        }

        return detailedOrders;
    }

        public async Task<bool> SendQuickOrderToOtostickerAsync(OtostickerQuickOrderDto order)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("apiKey", "eba11f4b-2b1f-444e-a777-ec6872c95601");
                client.DefaultRequestHeaders.Add("apiSecret", "e81498b3f3865c5bd2a5b2a6ab69028e5RbSwX32Q3CMlAwUDg==");

                var json = JsonSerializer.Serialize(order);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://www.otosticker.com.tr/api/v2/order/quick", content);
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsStringAsync();
                // Dilersen responseBody ile başarılı olup olmadığını kontrol edebilirsin
                return true;
            }
            catch
            {
                return false;
            }
        }
         public async Task<List<OrderDetailDto>> GetAllOrdersWithDetailsByDealerIdAsync(int dealerId)
    {
        int pageStart = 0;
        int pageSize = 100;
        var allOrders = new List<OrderDetailDto>();

        while (true)
        {
            var batch = await GetOrdersWithDetailsByDealerIdAsync(dealerId, pageStart, pageSize);
            if (batch == null || batch.Count == 0)
                break;

            allOrders.AddRange(batch);
            pageStart += pageSize;

            // API rate limit için kısa gecikme
            await Task.Delay(300);
        }

        return allOrders;
    }
    }
}
