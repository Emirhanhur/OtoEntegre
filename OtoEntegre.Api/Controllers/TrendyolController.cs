using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace OtoEntegre.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrendyolController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        // Trendyol API bilgilerini sabit tut
        private const string BASE_URL = "https://apigw.trendyol.com/integration/product";

        public TrendyolController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        private HttpClient CreateClient()
        {
            var client = _httpClientFactory.CreateClient();
            return client;
        }

        // 🔹 1️⃣ Marka Listesi
        [HttpGet("brands")]
        public async Task<IActionResult> GetBrands([FromQuery] int page = 0, [FromQuery] int size = 1000)
        {
            var client = CreateClient();
            var url = $"{BASE_URL}/brands?page={page}&size={size}";
            var response = await client.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, json);

            return Content(json, "application/json");
        }

        // 🔹 2️⃣ Kategori Ağacı
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var client = CreateClient();
            var url = $"{BASE_URL}/product-categories";
            var response = await client.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, json);

            return Content(json, "application/json");
        }

        // 🔹 3️⃣ Kategori Özellikleri
        [HttpGet("category-attributes/{categoryId}")]
        public async Task<IActionResult> GetCategoryAttributes(long categoryId)
        {
            var client = CreateClient();
            var url = $"{BASE_URL}/product-categories/{categoryId}/attributes";
            var response = await client.GetAsync(url);
            var json = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, json);

            return Content(json, "application/json");
        }
    }
}
