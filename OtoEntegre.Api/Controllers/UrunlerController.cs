using Microsoft.AspNetCore.Mvc;
using OtoEntegre.Api.Entities;
using OtoEntegre.Api.Repositories;
using OtoEntegre.Api.Services;
using System.Text.Json;
using System.Net;
namespace OtoEntegre.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UrunlerController : ControllerBase
    {
        private readonly IGenericRepository<Urunler> _repo;
        private readonly EntegrasyonService _entegrasyonService;
        private readonly TrendyolService _trendyolService;

        public UrunlerController(
            IGenericRepository<Urunler> repo,
            EntegrasyonService entegrasyonService,
            TrendyolService trendyolService) // ekle)
        {
            _repo = repo;
            _entegrasyonService = entegrasyonService;
            _trendyolService = trendyolService;
        }

        [HttpGet]
        public async Task<IEnumerable<Urunler>> GetAll()
            => await _repo.GetAllAsync();

        [HttpGet("{id}")]
        public async Task<ActionResult<Urunler>> GetById(Guid id)
        {
            var item = await _repo.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        public async Task<ActionResult<Urunler>> Create(UrunCreateDto dto)
        {
            var item = new Urunler
            {
                Id = Guid.NewGuid(),
                Sku = dto.Sku,
                Ad = dto.Ad,
                Kategori = dto.Kategori,
                Created_At = DateTime.UtcNow,
                Updated_At = DateTime.UtcNow
            };

            await _repo.AddAsync(item);
            await _repo.SaveAsync();

            return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, UrunUpdateDto dto)
        {
            var existing = await _repo.GetByIdAsync(id);
            if (existing == null) return NotFound();

            existing.Sku = dto.Sku;
            existing.Ad = dto.Ad;
            existing.Kategori = dto.Kategori;
            existing.Updated_At = DateTime.UtcNow;
            existing.Created_At = DateTime.UtcNow;

            _repo.Update(existing);
            await _repo.SaveAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var item = await _repo.GetByIdAsync(id);
            if (item == null) return NotFound();

            _repo.Delete(item);
            await _repo.SaveAsync();
            return NoContent();
        }


        [HttpPost("trendyol-import")]
        public async Task<IActionResult> ImportFromTrendyol(long supplierId, string apiKey, string apiSecret)
        {
            // 1️⃣ Trendyol’dan tüm ürünleri çek
            var products = await _trendyolService.GetAllProductsAsync(supplierId, apiKey, apiSecret);

            if (products == null || !products.Any())
                return BadRequest("Trendyol’dan ürün alınamadı veya ürün yok.");

            // 2️⃣ DB’de kontrol ederek kaydet
            var existingProducts = await _repo.GetAllAsync();

            foreach (var item in products)
            {
                var sku = !string.IsNullOrEmpty(item.Barcode) ? item.Barcode : item.ProductCode.ToString();
                var existing = existingProducts.FirstOrDefault(u => u.Sku == sku);
                if (existing != null) continue;

                var newUrun = new Urunler
                {
                    Id = Guid.NewGuid(),
                    Sku = sku,
                    Ad = item.Title,
                    Kategori = item.CategoryName ?? "Trendyol Ürün",
                    Created_At = DateTime.UtcNow,
                    Updated_At = DateTime.UtcNow
                };

                await _repo.AddAsync(newUrun);
            }

            await _repo.SaveAsync();

            return Ok(new { imported = products.Count });
        }

        [HttpPost("trendyol-import/{kullaniciId}")]
        public async Task<IActionResult> ImportUserTrendyolProducts(Guid kullaniciId)
        {
            // Kullanıcının entegrasyonunu al
            var entegrasyon = (await _entegrasyonService.GetAllAsync())
                                .FirstOrDefault(e => e.Kullanici_Id == kullaniciId);

            if (entegrasyon == null) return NotFound("Kullanıcının Trendyol entegrasyonu bulunamadı.");
            if (!entegrasyon.Seller_Id.HasValue || string.IsNullOrEmpty(entegrasyon.Api_Key) || string.IsNullOrEmpty(entegrasyon.Api_Secret))
                return BadRequest("Entegrasyon için gerekli bilgiler eksik.");

            var supplierId = entegrasyon.Seller_Id.Value;


            var products = await _trendyolService.GetAllProductsAsync(supplierId, entegrasyon.Api_Key, entegrasyon.Api_Secret);

            foreach (var item in products)
            {
                var existing = (await _repo.GetAllAsync())
                    .FirstOrDefault(u => u.Sku == item.ProductCode.ToString() || u.Sku == item.Barcode);

                if (existing == null)
                {
                    var newUrun = new Urunler
                    {
                        Id = Guid.NewGuid(),
                        Sku = !string.IsNullOrEmpty(item.Barcode)
                                ? item.Barcode
                                : item.ProductCode.ToString(),
                        Ad = item.Title,
                        Kategori = "Trendyol Ürün",
                        Created_At = DateTime.UtcNow,
                        Updated_At = DateTime.UtcNow
                    };

                    await _repo.AddAsync(newUrun);
                }
            }

            await _repo.SaveAsync();

            return Ok(new { imported = products.Count });
        }

        [HttpGet("trendyol/{kullaniciId}")]
        public async Task<IActionResult> GetTrendyolProducts(Guid kullaniciId, int page = 0, int size = 50)
        {
            // Kullanıcının entegrasyonunu al
            var entegrasyon = (await _entegrasyonService.GetAllAsync())
                                .FirstOrDefault(e => e.Kullanici_Id == kullaniciId);

            if (entegrasyon == null)
                return NotFound("Kullanıcının Trendyol entegrasyonu bulunamadı.");

            if (!entegrasyon.Seller_Id.HasValue || string.IsNullOrEmpty(entegrasyon.Api_Key) || string.IsNullOrEmpty(entegrasyon.Api_Secret))
                return BadRequest("Entegrasyon için gerekli bilgiler eksik.");

            var supplierId = entegrasyon.Seller_Id.Value;

            try
            {
                using var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Get,
                    $"https://apigw.trendyol.com/integration/product/sellers/{supplierId}/products");

                // Basic Auth header
                var auth = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{entegrasyon.Api_Key}:{entegrasyon.Api_Secret}"));
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", auth);

                // User-Agent header (zorunlu olabilir)
                request.Headers.Add("User-Agent", "MyAppIntegration/1.0");

                var response = await client.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, "Trendyol ürünleri çekilemedi.");

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<TrendyolProductResponse>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                var products = data?.Content ?? new List<TrendyolProductDto>();
                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Trendyol ürünleri alınırken hata oluştu: {ex.Message}");
            }
        }

    }
}
