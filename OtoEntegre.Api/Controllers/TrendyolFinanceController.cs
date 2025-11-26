using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OtoEntegre.Api.Data;
using OtoEntegre.Api.Entities;
using System.Net.Http.Headers;
using System.Text;

namespace OtoEntegre.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrendyolFinanceController : ControllerBase
    {
        private readonly AppDbContext _dbContext;
        private readonly IHttpClientFactory _httpClientFactory;

        public TrendyolFinanceController(AppDbContext dbContext, IHttpClientFactory httpClientFactory)
        {
            _dbContext = dbContext;
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Trendyol cari ekstre verilerini döner.
        /// sellerId, apiKey ve apiSecret veritabanındaki Entegrasyonlar tablosundan çekilir.
        /// </summary>
        [HttpGet("get-cari-ekstre")]
        public async Task<IActionResult> GetCariEkstre(
    [FromQuery] long sellerId,
    [FromQuery] Guid kullaniciId,
    [FromQuery] string? transactionType = null,
    [FromQuery] DateTime? startDate = null,
    [FromQuery] DateTime? endDate = null)
        {
            try
            {
                // 1️⃣ Entegrasyon kaydını kullanıcıId ile çek
                var entegrasyon = await _dbContext.Entegrasyonlar
                    .FirstOrDefaultAsync(e => e.Kullanici_Id == kullaniciId); // 🔁 değişiklik burada

                if (entegrasyon == null)
                    return NotFound(new { error = "Bu kullanıcıya ait Trendyol entegrasyonu bulunamadı." });

                if (string.IsNullOrWhiteSpace(entegrasyon.Api_Key) ||
                    string.IsNullOrWhiteSpace(entegrasyon.Api_Secret) ||
                    !entegrasyon.Seller_Id.HasValue)
                    return BadRequest(new { error = "Trendyol entegrasyon bilgileri eksik." });

                // 2️⃣ Yetkilendirme
                var realSellerId = entegrasyon.Seller_Id.Value;
                var apiKey = entegrasyon.Api_Key.Trim();
                var apiSecret = entegrasyon.Api_Secret.Trim();

                var httpClient = _httpClientFactory.CreateClient();
                var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}"));

                // 3️⃣ Trendyol endpoint URL’si
                // 3️⃣ Trendyol endpoint URL’si
                var baseUrl = $"https://apigw.trendyol.com/integration/finance/che/sellers/{realSellerId}/settlements";
                var query = new List<string>();

                if (!string.IsNullOrWhiteSpace(transactionType))
                    query.Add($"transactionType={transactionType}");

                if (startDate.HasValue)
                {
                    var startTimestamp = new DateTimeOffset(startDate.Value).ToUnixTimeMilliseconds();
                    query.Add($"startDate={startTimestamp}");
                }

                if (endDate.HasValue)
                {
                    var endTimestamp = new DateTimeOffset(endDate.Value).ToUnixTimeMilliseconds();
                    query.Add($"endDate={endTimestamp}");
                }

                var url = baseUrl + (query.Count > 0 ? "?" + string.Join("&", query) : "");



                // 4️⃣ HTTP isteği
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
                request.Headers.Add("User-Agent", "OtoEntegre/1.0");

                Console.WriteLine($"🔹 Trendyol URL: {url}");
                var response = await httpClient.SendAsync(request);
                var body = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, new
                    {
                        success = false,
                        message = "Trendyol finans verisi alınamadı.",
                        responseBody = body
                    });
                }

                return Ok(System.Text.Json.JsonDocument.Parse(body));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ TrendyolFinance hata: {ex.Message}");
                return StatusCode(500, new { error = ex.Message });
            }
        }

    }
}
