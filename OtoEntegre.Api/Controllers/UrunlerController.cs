using Microsoft.AspNetCore.Mvc;
using OtoEntegre.Api.Entities;
using OtoEntegre.Api.Repositories;
using System.Text;
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
        public async Task<IActionResult> GetTrendyolProducts(Guid kullaniciId, string? search = null, int page = 0, int size = 50)
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
                var resp = await _trendyolService.GetProductsAsync(supplierId, entegrasyon.Api_Key, entegrasyon.Api_Secret, page, size, search);

                if (resp == null)
                    return StatusCode(502, "Trendyol API'den ürün yanıtı alınamadı.");

                // Map TrendyolProduct -> shape expected by frontend
                var mapped = resp.content.Select(p => new
                {
                    productCode = p.productCode,
                    barcode = p.id ?? string.Empty,
                    title = p.title,
                    description = p.description,
                    brand = p.brand,
                    brandId = p.brandId,
                    salePrice = p.salePrice,
                    listPrice = p.listPrice,
                    stock = p.quantity,
                    onSale = p.onSale,
                    approved = p.approved,
                    archived = p.archived,
                    category = !string.IsNullOrEmpty(p.categoryName) ? p.categoryName : (p.pimCategoryId != 0 ? p.pimCategoryId.ToString() : string.Empty),
                    productUrl = p.productUrl,
                    createDateTime = p.createDateTime,
                    lastUpdateDate = p.lastUpdateDate,
                    attributes = p.attributes.Select(a => new
                    {
                        a.attributeId,
                        a.attributeName,
                        a.attributeValue,
                        a.attributeValueId
                    }),
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

        [HttpPost("trendyol-add")]
        public async Task<IActionResult> AddProductToTrendyol([FromBody] TrendyolAddProductDto dto)
        {
            var entegrasyon = (await _entegrasyonService.GetAllAsync())
                                .FirstOrDefault(e => e.Kullanici_Id.ToString() == dto.KullaniciId.ToString());
            Console.WriteLine(entegrasyon?.Kullanici_Id + " - " + dto.KullaniciId);
            if (entegrasyon == null)
                return NotFound("Kullanıcının Trendyol entegrasyonu bulunamadı.");

            if (!entegrasyon.Seller_Id.HasValue || string.IsNullOrEmpty(entegrasyon.Api_Key) || string.IsNullOrEmpty(entegrasyon.Api_Secret))
                return BadRequest("Entegrasyon bilgileri eksik.");

            if (dto.Variants == null || dto.Variants.Count == 0)
                return BadRequest("En az bir varyant bilgisi gönderilmelidir.");

            // Tek ürün - çok varyant (örnek: XL ve L)
            var productPayload = new
            {
                items = dto.Variants.Select(v => new
                {
                    barcode = v.Barcode ?? Guid.NewGuid().ToString(),
                    title = dto.Title,
                    productMainId = dto.ProductMainId, // tüm varyantlarda aynı olmalı
                    brandId = dto.BrandId,
                    categoryId = dto.CategoryId,
                    quantity = v.Stock > 0 ? v.Stock : 1,
                    stockCode = v.StockCode ?? v.Barcode ?? Guid.NewGuid().ToString(),
                    dimensionalWeight = 1.0m,
                    description = v.Description ?? dto.Description ?? "",
                    currencyType = "TRY",
                    listPrice = Convert.ToDecimal(v.SalePrice),
                    salePrice = Convert.ToDecimal(v.SalePrice),
                    vatRate = 20,
                    cargoCompanyId = 10,
                    images = v.ImageUrls.Take(5).Select(url => new { url }).ToArray(),
                    attributes = v.Attributes?.Select(a => new
                    {
                        attributeId = a.AttributeId,
                        attributeValueId = a.AttributeValueId,
                        customAttributeValue = a.CustomAttributeValue
                    }).ToArray() ?? Array.Empty<object>()
                }).ToArray()
            };

            Console.WriteLine(JsonSerializer.Serialize(productPayload, new JsonSerializerOptions { WriteIndented = true }));

            var result = await _trendyolService.AddProductAsync(
                entegrasyon.Seller_Id.Value,
                entegrasyon.Api_Key,
                entegrasyon.Api_Secret,
                productPayload
            );

            if (!result.Success)
                return StatusCode(500, $"Trendyol'a ürün eklenemedi: {result.Message}");

            return Ok(new { message = "Ürün varyantları Trendyol hesabına başarıyla eklendi." });
        }


        public class TrendyolAddProductDto
        {
            public Guid KullaniciId { get; set; }
            public string Title { get; set; } = "";
            public string CategoryName { get; set; } = "";
            public long CategoryId { get; set; }
            public long BrandId { get; set; }
            public string ProductMainId { get; set; } = "";
            public string? Description { get; set; }

            public List<VariantDto> Variants { get; set; } = new();

            public class VariantDto
            {
                public string? Barcode { get; set; }
                public string? StockCode { get; set; }
                public decimal SalePrice { get; set; }
                public int Stock { get; set; }
                public string? Description { get; set; }
                public List<string> ImageUrls { get; set; } = new();
                public List<ProductAttributeDto> Attributes { get; set; } = new();
            }

            public class ProductAttributeDto
            {
                public int AttributeId { get; set; }
                public int? AttributeValueId { get; set; }
                public string? CustomAttributeValue { get; set; }
            }
        }



        // GET api/urunler/stats/{productCode}?kullaniciId={kullaniciId}
        [HttpGet("stats/{productCode}")]
        public async Task<IActionResult> GetProductStats(long productCode, [FromQuery] Guid kullaniciId)
        {
            try
            {
                // Kullanıcının Trendyol entegrasyonu
                var entegrasyon = (await _entegrasyonService.GetAllAsync())
                                    .FirstOrDefault(e => e.Kullanici_Id == kullaniciId);

                if (entegrasyon == null)
                    return NotFound("Kullanıcının Trendyol entegrasyonu bulunamadı.");

                var supplierId = entegrasyon.Seller_Id.Value;
                var apiKey = entegrasyon.Api_Key;
                var apiSecret = entegrasyon.Api_Secret;

                // Trendyol siparişlerini çek (son 90 gün gibi bir tarih aralığı koyabilirsin)
                var orders = await _trendyolService.GetOrdersByProductCodeAsync(supplierId, apiKey, apiSecret, productCode);
                // Sadece ilgili productCode'lu ürünleri filtrele


                return Ok(new { orders });
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


    [HttpPost("trendyol/download-template/{categoryId}")]
public IActionResult DownloadTrendyolTemplate(long categoryId, [FromBody] DownloadTemplateRequest request)
{
    var csv = new StringBuilder();

    // Trendyol API'ye göre zorunlu + isteğe bağlı alanlar
    var headers = new List<string>
    {
        "barcode (*)",
        "title (*)",
        "productMainId (*)",
        "brandId (*)",
        "categoryId (*)",
        "quantity (*)",
        "stockCode (*)",
        "dimensionalWeight (*)",
        "description (*)",
        "currencyType (*)",
        "listPrice (*)",
        "salePrice (*)",
        "cargoCompanyId (*)",
        "vatRate (*)",
        "deliveryDuration",
        "deliveryOption",
        "lotNumber",
        "shipmentAddressId",
        "returningAddressId",
        "images (URL1, URL2, ...)",
        "attributes (attributeId:attributeValueId veya customAttributeValue)"
    };

    // Eğer kategoriye özel attribute başlıkları varsa ekle
    if (request?.Columns?.Any() == true)
    {
        headers.AddRange(request.Columns.Select(c => c.Header));
    }

    // Başlık satırını oluştur
    csv.AppendLine(string.Join(",", headers));

    var bytes = Encoding.UTF8.GetBytes(csv.ToString());

    return File(bytes, "text/csv", $"trendyol-urun-sablonu-{categoryId}.csv");
}

public class DownloadTemplateRequest
{
    public long CategoryId { get; set; }
    public List<ColumnInfo> Columns { get; set; } = new();
    public class ColumnInfo
    {
        public string Header { get; set; } = "";
    }
}


        public class UpdatePriceRequest
        {
            public Guid? KullaniciId { get; set; }
            public decimal Price { get; set; }
        }

    }
}
