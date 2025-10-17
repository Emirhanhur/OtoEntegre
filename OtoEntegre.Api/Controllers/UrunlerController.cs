using Microsoft.AspNetCore.Mvc;
using OtoEntegre.Api.Entities;
using OtoEntegre.Api.Repositories;
using OtoEntegre.Api.Services;
using System.Text.Json;
using System.Net;
using OtoEntegre.Api.Data;
using Microsoft.EntityFrameworkCore;
namespace OtoEntegre.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UrunlerController : ControllerBase
    {
    private readonly IGenericRepository<Urunler> _repo;
    private readonly EntegrasyonService _entegrasyonService;
    private readonly TrendyolService _trendyolService;
    private readonly AppDbContext _dbContext;

        public UrunlerController(
            IGenericRepository<Urunler> repo,
            EntegrasyonService entegrasyonService,
            TrendyolService trendyolService,
            AppDbContext dbContext)
        {
            _repo = repo;
            _entegrasyonService = entegrasyonService;
            _trendyolService = trendyolService;
            _dbContext = dbContext;
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
        public async Task<IActionResult> GetTrendyolProducts(Guid kullaniciId,string? search = null, int page = 0, int size = 50)
        {
            // Kullanıcının entegrasyonunu al
            var entegrasyon = (await _entegrasyonService.GetAllAsync())
                                .FirstOrDefault(e => e.Kullanici_Id == kullaniciId);

            if (entegrasyon == null)
                return NotFound("Kullanıcının Trendyol entegrasyonu bulunamadı.");

            if (!entegrasyon.Seller_Id.HasValue || string.IsNullOrEmpty(entegrasyon.Api_Key) || string.IsNullOrEmpty(entegrasyon.Api_Secret))
                return BadRequest("Entegrasyon için gerekli bilgiler eksik.");

            var supplierId = entegrasyon.Seller_Id.Value;

            // Fetch only the requested page from Trendyol (Trendyol API supports page & size)
            try
            {
                var resp = await _trendyolService.GetProductsAsync(supplierId, entegrasyon.Api_Key, entegrasyon.Api_Secret, page, size,search);

                if (resp == null)
                    return StatusCode(502, "Trendyol API'den ürün yanıtı alınamadı.");

                // Map TrendyolProduct -> shape expected by frontend
                var mapped = resp.content.Select(p => new
                {
                    productCode = p.productCode,
                    barcode = p.id ?? string.Empty,
                    title = p.title,
                    salePrice = p.salePrice,
                    productUrl = string.Empty,
                    // prefer the human-readable categoryName from Trendyol, fallback to pimCategoryId
                    category = !string.IsNullOrEmpty(p.categoryName) ? p.categoryName : (p.pimCategoryId != 0 ? p.pimCategoryId.ToString() : string.Empty),
                    images = (object[])(p.images?.Select(i => new { url = i.url }).ToArray() ?? Array.Empty<object>())
                }).ToList();

                return Ok(new
                {
                    total = resp.totalElements,
                    page = resp.page,
                    size = resp.size,
                    data = mapped
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Trendyol ürünleri alınırken hata oluştu: {ex.Message}");
            }
        }

        // GET api/urunler/stats/{productCode}?kullaniciId={kullaniciId}
        [HttpGet("stats/{productCode}")]
        public async Task<IActionResult> GetProductStats(long productCode, [FromQuery] Guid kullaniciId)
        {
            try
            {
                var urun = await _dbContext.Urunler.FirstOrDefaultAsync(u => u.ProductCode == productCode);
                if (urun == null)
                {
                    return NotFound(new { message = "Ürün bulunamadı" });
                }

                // Toplam satılan adet ve kaç siparişte geçtiğini hesapla
                var query = from su in _dbContext.SiparisUrunleri
                            join u in _dbContext.Urunler on su.Urun_Id equals u.Id
                            join s in _dbContext.Siparisler on su.Siparis_Id equals s.Id
                            where u.ProductCode == productCode
                            select new { su.Adet, su.Toplam_Fiyat, s.KullaniciId, su.Siparis_Id };

                if (kullaniciId != Guid.Empty)
                {
                    query = query.Where(x => x.KullaniciId == kullaniciId);
                }

                var totalSold = await query.SumAsync(x => (int?)x.Adet) ?? 0;
                var orderCount = await query.Select(x => x.Siparis_Id).Distinct().CountAsync();

                return Ok(new { productCode, urunId = urun.Id, totalSold, orderCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // POST api/urunler/{productCode}/update-price
        // Body: { "kullaniciId": "guid", "price": 123.45 }
        [HttpPost("{productCode}/update-price")]
        public async Task<IActionResult> UpdatePrice(long productCode, [FromBody] UpdatePriceRequest req)
        {
            // NOTE: This currently only logs the requested update. Persisting or updating Trendyol
            // requires additional implementation. This endpoint acknowledges the request.
            Console.WriteLine($"Price update requested: productCode={productCode}, price={req.Price}, kullaniciId={req.KullaniciId}");
            // Optionally: find urun and update local record or call TrendyolService to update remote price.
            return Ok(new { success = true });
        }

        public class UpdatePriceRequest
        {
            public Guid? KullaniciId { get; set; }
            public decimal Price { get; set; }
        }

    }
}
