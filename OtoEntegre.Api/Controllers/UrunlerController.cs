using System.Net.Http.Headers;
using Newtonsoft.Json;
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
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly OtostickerService _otostickerService; // YENİ
        public UrunlerController(
            IHttpClientFactory httpClientFactory,
            OtostickerService otostickerService,
            IGenericRepository<Urunler> repo,
            EntegrasyonService entegrasyonService,
            TrendyolService trendyolService,
            AppDbContext dbContext)
        {
            _repo = repo;
            _entegrasyonService = entegrasyonService;
            _trendyolService = trendyolService;
            _httpClientFactory = httpClientFactory;
            _otostickerService = otostickerService;
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

        public class TrendyolUpdateProductItem
        {
            public string barcode { get; set; } = string.Empty;
            public string title { get; set; } = string.Empty;
            public string productMainId { get; set; } = string.Empty;
            public long brandId { get; set; }
            public long categoryId { get; set; }
            public string stockCode { get; set; } = string.Empty;
            public int dimensionalWeight { get; set; }
            public string description { get; set; } = string.Empty;
            public string currencyType { get; set; } = string.Empty;
            public int? deliveryDuration { get; set; }
            public int vatRate { get; set; }
            public string? locationBasedDelivery { get; set; }
            public string? lotNumber { get; set; }
            public DeliveryOption? deliveryOption { get; set; }
            public List<TrendyolImageDto> images { get; set; } = new();
            public List<TrendyolUpdateAttribute> attributes { get; set; } = new();
            public int cargoCompanyId { get; set; }
            public int? shipmentAddressId { get; set; }
            public int? returningAddressId { get; set; }

        }

        public class DeliveryOption
        {
            public int deliveryDuration { get; set; }
            public string fastDeliveryType { get; set; } = string.Empty; // SAME_DAY_SHIPPING | FAST_DELIVERY
        }

        public class TrendyolUpdateAttribute
        {
            public int attributeId { get; set; }
            public long? attributeValueId { get; set; }
            public string? customAttributeValue { get; set; }
        }


        [HttpPut("trendyol/{kullaniciId}/update-product")]
        public async Task<IActionResult> UpdateProductAsync(Guid kullaniciId, [FromBody] TrendyolUpdateProductItem productItem)
        {
            var entegrasyon = (await _entegrasyonService.GetAllAsync())
                .FirstOrDefault(e => e.Kullanici_Id == kullaniciId);

            if (entegrasyon == null)
                return NotFound(new { success = false, message = "Entegrasyon bulunamadı." });

            var supplierId = entegrasyon.Seller_Id.Value;
            var apiKey = entegrasyon.Api_Key;
            var apiSecret = entegrasyon.Api_Secret;

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "OtoEntegre");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic",
                    Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}")));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var url = $"https://apigw.trendyol.com/integration/product/sellers/{supplierId}/products";

            var payload = new { items = new[] { productItem } };
            var json = System.Text.Json.JsonSerializer.Serialize(payload, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = content };
            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            // LOG - Trendyol cevabını tam olarak yaz
            Console.WriteLine($"[TrendyolService] updateProduct StatusCode: {response.StatusCode}");
            Console.WriteLine($"[TrendyolService] updateProduct ResponseBody: {body}");

            // Eğer Trendyol boş body dönmüşse bunu de bildirelim, ama status / headers'i dönelim
            var headers = response.Headers.ToDictionary(h => h.Key, h => string.Join(", ", h.Value));

            // Eğer body JSON ise parse etmeye çalış, değilse raw string olarak dön
            object parsedBody = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(body))
                    parsedBody = System.Text.Json.JsonSerializer.Deserialize<object>(body);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return BadRequest(new { error = ex.Message });
            }

            return StatusCode((int)response.StatusCode, new
            {
                success = response.IsSuccessStatusCode,
                statusCode = (int)response.StatusCode,
                headers,
                body = parsedBody ?? body
            });
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

        [HttpGet("trendyol/{kullaniciId}/product-by-barcode")]
        public async Task<IActionResult> GetTrendyolProductByBarcode(Guid kullaniciId, string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return BadRequest("Barkod gönderilmedi.");

            // Entegrasyon bul
            var entegrasyon = (await _entegrasyonService.GetAllAsync())
                .FirstOrDefault(e => e.Kullanici_Id == kullaniciId);

            if (entegrasyon == null)
                return NotFound("Kullanıcının Trendyol entegrasyonu bulunamadı.");

            if (!entegrasyon.Seller_Id.HasValue)
                return BadRequest("Trendyol sellerId bulunamadı.");

            // Trendyol API çağır - filtre sadece barkod
            var resp = await _trendyolService.GetProductsAsync(
                entegrasyon.Seller_Id.Value,
                entegrasyon.Api_Key,
                entegrasyon.Api_Secret,
                page: 0,
                size: 1,
                search: null,
                barcode: barcode,
                approved: null,
                archived: null,
                onSale: null,
                rejected: null,
                blacklisted: null
            );

            if (resp == null || resp.content == null || resp.content.Count == 0)
                return NotFound("Bu barkod ile Trendyol’da ürün bulunamadı.");

            var p = resp.content.First();

            var mapped = new
            {
                barcode = p.barcode,
                productCode = p.productCode,
                title = p.title,
                description = p.description,
                brand = p.brand,
                brandId = p.brandId,
                category = p.categoryName,
                categoryId = p.pimCategoryId,
                stockCode = p.stockCode,
                deliveryDuration = p.deliveryDuration,
                dimensionalWeight = p.dimensionalWeight,
                cargoCompanyId = p.cargoCompanyId,
                shipmentAddressId = p.shipmentAddressId,
                returningAddressId = p.returningAddressId,
                vatRate = p.vatRate,
                images = p.images?.Select(x => new { url = x.url }).ToList(),
                attributes = p.attributes?.Select(a => new
                {
                    attributeId = a.attributeId,
                    attributeValueId = a.attributeValueId,
                    customAttributeValue = a.attributeValue
                }).ToList()
            };

            return Ok(mapped);
        }


        [HttpGet("trendyol/{kullaniciId}")]
        public async Task<IActionResult> GetTrendyolProducts(
    Guid kullaniciId,
    string? search = null,
    string? barcode = null,
    bool? approved = null,      // ✅ onaylı filtre
    bool? archived = null,      // ✅ arşivlenmiş filtre
    bool? onSale = null,        // ✅ satışta filtre
    bool? rejected = null,      // ✅ reddedilen filtre
    bool? blacklisted = null,   // ✅ blacklist filtre
    int page = 0,
    int size = 100)
        {
            var entegrasyon = (await _entegrasyonService.GetAllAsync())
                                        .FirstOrDefault(e => e.Kullanici_Id == kullaniciId);

            if (entegrasyon == null)
                return NotFound("Kullanıcının Trendyol entegrasyonu bulunamadı.");

            var supplierId = entegrasyon.Seller_Id.Value;

            try
            {
                // 1. Trendyol Ürünlerini Al
                var resp = await _trendyolService.GetProductsAsync(
                    supplierId,
                    entegrasyon.Api_Key,
                    entegrasyon.Api_Secret,
                    page,
                    size,
                    search,
                    barcode,
                    approved,
                    archived,
                    onSale,
                    rejected,
                    blacklisted
                );

                if (resp == null)
                    return StatusCode(502, "Trendyol API'den ürün yanıtı alınamadı.");

                
                var otostickerEslesmeleri = await _dbContext.Otosticker_Urunler
                    .Where(x => x.KullaniciId == kullaniciId)
                    .Select(x => x.ProductCode) // Sadece ProductCode'ları alıp performansı artırabiliriz.
                    .ToListAsync();

                
                var eslesenProductCodes = new HashSet<long?>(otostickerEslesmeleri);

                // 3. Ürünleri map'lerken Otosticker eşleşme durumunu ekle
                var mapped = resp.content.Select(p => new
                {
                    productCode = p.productCode,
                    barcode = p.barcode,
                    title = p.title,
                    description = p.description,
                    brand = p.brand,
                    brandId = p.brandId,
                    salePrice = p.salePrice,
                    listPrice = p.listPrice,
                    stock = p.quantity,
                    approved = p.approved,
                    archived = p.archived,
                    onSale = p.onSale,
                    rejected = p.rejected,
                    blacklisted = p.blacklisted,
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
                    images = (object[])(p.images?.Select(i => new { url = i.url }).ToArray() ?? Array.Empty<object>()),
                    // 👇 YENİ ALAN: Otosticker ile eşleşme kontrolü
                    otostickerEslesme = eslesenProductCodes.Contains(p.productCode)
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

            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(
                productPayload,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }
            ));

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


        [HttpGet("trendyol/batch-result")]
        public async Task<IActionResult> GetBatchResult(Guid kullaniciId, string batchId)
        {
            var entegrasyon = (await _entegrasyonService.GetAllAsync())
                .FirstOrDefault(e => e.Kullanici_Id == kullaniciId);

            if (entegrasyon == null)
                return NotFound("Trendyol entegrasyonu bulunamadı.");

            var result = await _trendyolService.GetBatchResultAsync(
                entegrasyon.Seller_Id!.Value,
                entegrasyon.Api_Key!,
                entegrasyon.Api_Secret!,
                batchId
            );

            if (result == null)
                return StatusCode(502, "Batch sonucu alınamadı.");

            return Ok(result);
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

        // UrunlerController.cs dosyasının içine, diğer DTO'ların yanına ekleyin
        public class OtostickerMatchDto
        {
            public string TrendyolBarcode { get; set; } = string.Empty;
            public string OtostickerBarcode { get; set; } = string.Empty;
            public Guid? KullaniciId { get; set; } // Gerekli değil ama güvenlik için tutulabilir
            public long? ProductCode { get; set; }
            public Guid? PlatformId { get; set; }
        }

        [HttpPost("match-otosticker-barcode")]
        public async Task<IActionResult> MatchOtostickerBarcode([FromBody] OtostickerMatchDto dto)
        {
            // 1. Zorunlu alan kontrolleri
            if (string.IsNullOrWhiteSpace(dto.TrendyolBarcode) || string.IsNullOrWhiteSpace(dto.OtostickerBarcode))
                return BadRequest(new { success = false, message = "Trendyol ve Otosticker barkodları gereklidir." });

            if (dto.KullaniciId == null)
                return BadRequest("KullaniciId gereklidir.");

            if (dto.PlatformId == null)
                return BadRequest("PlatformId gereklidir.");

            Guid merchantId = dto.KullaniciId.Value;
            Guid platformId = dto.PlatformId.Value;

            // 2. Kullanıcı entegrasyonunu kontrol et
            var entegrasyon = await _dbContext.Entegrasyonlar
                .FirstOrDefaultAsync(e => e.Kullanici_Id == merchantId);

            if (entegrasyon == null)
                return BadRequest("Entegrasyon bulunamadı.");

            if (entegrasyon.Seller_Id == null)
                return BadRequest("Entegrasyon.Seller_Id hatalı.");

            // 3. Trendyol ürün kontrolü
            var products = await _trendyolService.GetProductsByBarcodesAsync(
                entegrasyon.Seller_Id.Value,
                entegrasyon.Api_Key,
                entegrasyon.Api_Secret,
                new List<string> { dto.TrendyolBarcode }
            );

            if (products == null || !products.Any())
                return NotFound(new { success = false, message = $"Trendyol barkodu ({dto.TrendyolBarcode}) bulunamadı." });

            // 4. Otosticker ürün kontrolü
            var otostickerProduct = await _otostickerService.GetProductByBarcodeAsync(dto.OtostickerBarcode);

            if (otostickerProduct == null)
                return NotFound(new { success = false, message = $"Otosticker barkodu ({dto.OtostickerBarcode}) API'de bulunamadı." });

            // 5. Kayıt var mı kontrol et
            var matchEntry = await _dbContext.Otosticker_Urunler
                .FirstOrDefaultAsync(o =>
                    o.KullaniciId == merchantId &&
                    o.PlatformId == platformId &&
                    o.ProductCode == dto.ProductCode
                );

            if (matchEntry != null)
            {
                // Güncelle
                matchEntry.UrunTedarikBarcode = dto.OtostickerBarcode;
            }
            else
            {
                // Yeni kayıt
                var newMatch = new Otosticker_Urunler
                {
                    Id = Guid.NewGuid(),
                    KullaniciId = merchantId,
                    PlatformId = platformId,
                    ProductCode = dto.ProductCode,
                    UrunTedarikBarcode = dto.OtostickerBarcode,
                };

                _dbContext.Otosticker_Urunler.Add(newMatch);
            }

            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"Eşleme yapıldı: Trendyol ({dto.TrendyolBarcode}) → Otosticker ({dto.OtostickerBarcode})"
            });
        }
        [HttpGet("otosticker/eslesme-kontrol")]
        public async Task<IActionResult> GetEslesmeDurumu(Guid kullaniciId, string productCode)
        {
            if (kullaniciId == Guid.Empty || string.IsNullOrWhiteSpace(productCode))
                return BadRequest("kullaniciId ve productCode zorunludur.");

            var urun = await _dbContext.Otosticker_Urunler
                .FirstOrDefaultAsync(x => x.KullaniciId == kullaniciId && x.ProductCode == Convert.ToInt64(productCode));

            if (urun == null)
            {
                return Ok(new
                {
                    matched = false,
                    data = (object?)null
                });
            }

            return Ok(new
            {
                matched = true,
                data = urun
            });
        }

        // GET api/urunler/otosticker/eslesmeler/{kullaniciId}
        [HttpGet("otosticker/eslesmeler/{kullaniciId}")]
        public async Task<IActionResult> GetOtostickerEslesmeler(Guid kullaniciId)
        {
            if (kullaniciId == Guid.Empty)
                return BadRequest("kullaniciId zorunludur.");

            var list = await _dbContext.Otosticker_Urunler
                .Where(x => x.KullaniciId == kullaniciId)
                .ToListAsync();

            return Ok(list);
        }


        [HttpPost("update-price")]

        public async Task<string> UpdatePriceAndInventory(long sellerId, List<ProductUpdateDto> items, Guid kullaniciId)
        {
            var entegrasyon = (await _entegrasyonService.GetAllAsync())
                                   .FirstOrDefault(e => e.Kullanici_Id == kullaniciId);



            var supplierId = entegrasyon.Seller_Id.Value;
            var apiKey = entegrasyon.Api_Key;
            var apiSecret = entegrasyon.Api_Secret;
            var httpClient = _httpClientFactory.CreateClient();

            var url = $"https://apigw.trendyol.com/integration/inventory/sellers/{sellerId}/products/price-and-inventory";

            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}"));

            var request = new HttpRequestMessage(HttpMethod.Put, url);
            request.Headers.Add("Authorization", $"Basic {auth}");
            request.Content = new StringContent(JsonConvert.SerializeObject(items), Encoding.UTF8, "application/json");


            var body = new
            {
                items = items
            };

            var jsonBody = JsonConvert.SerializeObject(body);
            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            ;

            var response = await httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            return responseContent;
        }
        public class ProductUpdateDto
        {
            public string barcode { get; set; }
            public int quantity { get; set; }
            public decimal salePrice { get; set; }
            public decimal listPrice { get; set; }
        }


        // POST api/urunler/{productCode}/update-price
        // Body: { "kullaniciId": "guid", "price": 123.45 }
        [HttpPost("{barkod}/update-price")]
        public async Task<IActionResult> UpdatePrice(string barkod, [FromBody] UpdatePriceRequest req)
        {
            try
            {
                Console.WriteLine($"Price update requested: productCode={barkod}, price={req.Price}, kullaniciId={req.KullaniciId}");

                if (!req.KullaniciId.HasValue)
                    return BadRequest(new { success = false, message = "KullaniciId gereklidir." });

                var entegrasyon = (await _entegrasyonService.GetAllAsync())
                                    .FirstOrDefault(e => e.Kullanici_Id == req.KullaniciId.Value);

                if (entegrasyon == null)
                    return NotFound(new { success = false, message = "Kullanıcının Trendyol entegrasyonu bulunamadı." });

                if (!entegrasyon.Seller_Id.HasValue || string.IsNullOrEmpty(entegrasyon.Api_Key) || string.IsNullOrEmpty(entegrasyon.Api_Secret))
                    return BadRequest(new { success = false, message = "Entegrasyon bilgileri eksik." });

                var sellerId = entegrasyon.Seller_Id.Value;

                // Try to determine barcode from local product record (Sku may contain barcode or productCode)
                var allLocal = await _repo.GetAllAsync();
                var local = allLocal.FirstOrDefault(u => u.Sku == barkod.ToString() || u.Sku == barkod.ToString());
                string barcode = local?.Sku ?? barkod.ToString();

                // Build items payload
                var salePrice = req.Price;
                var listPrice = req.ListPrice ?? req.Price;
                var quantity = req.Quantity;

                var items = new List<(string barcode, int? quantity, decimal? salePrice, decimal? listPrice)>
                {
                    (barcode, quantity, salePrice, listPrice)
                };

                var result = await _trendyolService.UpdatePriceAndInventoryByBarcodeAsync(sellerId, entegrasyon.Api_Key, entegrasyon.Api_Secret, items);

                if (!result.Success)
                    return StatusCode(502, new { success = false, message = result.Message });

                return Ok(new { success = true, message = result.Message, batchRequestId = result.BatchRequestId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
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
            // Optional: list price and available quantity
            public decimal? ListPrice { get; set; }
            public int? Quantity { get; set; }
        }

        /// <summary>
        /// Eşleştirilmiş ürünler için kar/zarar hesaplama endpoint'i
        /// Hesaplama: Trendyol Satış Fiyatı - Trendyol Giderleri - Otosticker Maliyeti
        /// </summary>
        [HttpGet("kar-zarar/{kullaniciId}")]
        public async Task<IActionResult> GetKarZararHesaplama(Guid kullaniciId)
        {
            try
            {
                // 1. Kullanıcının eşleştirilmiş ürünlerini al
                var eslesmeler = await _dbContext.Otosticker_Urunler
                    .Where(x => x.KullaniciId == kullaniciId && x.ProductCode.HasValue)
                    .ToListAsync();

                if (!eslesmeler.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        data = new List<object>(),
                        message = "Eşleştirilmiş ürün bulunamadı."
                    });
                }

                // 2. Entegrasyon bilgilerini al
                var entegrasyon = (await _entegrasyonService.GetAllAsync())
                    .FirstOrDefault(e => e.Kullanici_Id == kullaniciId);

                if (entegrasyon == null || !entegrasyon.Seller_Id.HasValue)
                {
                    return NotFound("Kullanıcının Trendyol entegrasyonu bulunamadı.");
                }

                var supplierId = entegrasyon.Seller_Id.Value;

                // 3. Eşleştirilmiş productCode'ları topla
                var productCodes = eslesmeler
                    .Where(e => e.ProductCode.HasValue)
                    .Select(e => e.ProductCode!.Value)
                    .ToList();

                // 4. Trendyol'dan tüm ürünleri çek (sayfalama ile)
                var allTrendyolProducts = new List<TrendyolProduct>();
                int page = 0;
                int size = 100;
                bool hasMore = true;
                var foundProductCodes = new HashSet<long>();

                while (hasMore)
                {
                    var trendyolResponse = await _trendyolService.GetProductsAsync(
                        supplierId,
                        entegrasyon.Api_Key,
                        entegrasyon.Api_Secret,
                        page: page,
                        size: size,
                        search: null,
                        barcode: null,
                        approved: null,
                        archived: null,
                        onSale: null,
                        rejected: null,
                        blacklisted: null
                    );

                    if (trendyolResponse?.content == null || !trendyolResponse.content.Any())
                    {
                        hasMore = false;
                        break;
                    }

                    // Sadece eşleştirilmiş ürünleri filtrele
                    var matchedProducts = trendyolResponse.content
                        .Where(p => productCodes.Contains(p.productCode) && !foundProductCodes.Contains(p.productCode))
                        .ToList();

                    foreach (var product in matchedProducts)
                    {
                        allTrendyolProducts.Add(product);
                        foundProductCodes.Add(product.productCode);
                    }

                    // Eğer tüm eşleştirilmiş ürünler bulunduysa dur
                    if (foundProductCodes.Count >= productCodes.Count || 
                        trendyolResponse.content.Count < size)
                    {
                        hasMore = false;
                    }
                    else
                    {
                        page++;
                    }
                }

                // 5. Son 90 günlük siparişlerden commission bilgilerini al (bir kere)
                var startDate = DateTime.UtcNow.AddDays(-90);
                var orders = await _trendyolService.GetOrdersAsync(
                    supplierId,
                    entegrasyon.Api_Key,
                    entegrasyon.Api_Secret,
                    startDate: startDate
                );

                // 6. Her eşleştirme için kar/zarar hesapla
                var karZararListesi = new List<object>();

                foreach (var eslesme in eslesmeler)
                {
                    if (!eslesme.ProductCode.HasValue)
                        continue;

                    var productCode = eslesme.ProductCode.Value;

                    // 6.1. Trendyol ürün bilgilerini bul
                    var trendyolProduct = allTrendyolProducts
                        .FirstOrDefault(p => p.productCode == productCode);

                    if (trendyolProduct == null)
                        continue;

                    decimal trendyolSatisFiyati = trendyolProduct.salePrice;

                    // 6.2. Otosticker maliyet fiyatını al
                    decimal otostickerMaliyet = 0;
                    if (!string.IsNullOrWhiteSpace(eslesme.UrunTedarikBarcode))
                    {
                        var otostickerProduct = await _otostickerService.GetProductByBarcodeAsync(eslesme.UrunTedarikBarcode);
                        if (otostickerProduct != null)
                        {
                            otostickerMaliyet = otostickerProduct.SalePrice;
                        }
                    }

                    // 6.3. Trendyol giderlerini hesapla (commission + diğer giderler)
                    decimal commissionOrani = 0.12m; // Varsayılan %12

                    // Bu ürün için commission bilgisini bul
                    var productOrders = orders
                        .Where(o => o.Lines?.Any(l => l.ProductCode == productCode) == true)
                        .ToList();

                    if (productOrders.Any())
                    {
                        decimal toplamCommission = 0;
                        decimal toplamSatisFiyati = 0;

                        foreach (var order in productOrders)
                        {
                            var line = order.Lines?.FirstOrDefault(l => l.ProductCode == productCode);
                            if (line != null && line.Commission.HasValue && line.Price > 0)
                            {
                                toplamCommission += line.Commission.Value;
                                toplamSatisFiyati += line.Price;
                            }
                        }

                        if (toplamSatisFiyati > 0)
                        {
                            commissionOrani = toplamCommission / toplamSatisFiyati;
                        }
                    }

                    // Trendyol giderleri = Commission (şimdilik sadece commission)
                    decimal trendyolGiderleri = trendyolSatisFiyati * commissionOrani;

                    // 6.4. Kar/Zarar hesapla
                    decimal karZarar = trendyolSatisFiyati - trendyolGiderleri - otostickerMaliyet;
                    decimal karZararYuzdesi = otostickerMaliyet > 0 
                        ? ((karZarar / otostickerMaliyet) * 100) 
                        : 0;

                    karZararListesi.Add(new
                    {
                        ProductCode = productCode,
                        TrendyolBarcode = trendyolProduct.barcode ?? "",
                        OtostickerBarcode = eslesme.UrunTedarikBarcode ?? "",
                        UrunAdi = trendyolProduct.title ?? "",
                        TrendyolSatisFiyati = trendyolSatisFiyati,
                        TrendyolGiderleri = trendyolGiderleri,
                        CommissionOrani = Math.Round(commissionOrani * 100, 2), // Yüzde olarak
                        OtostickerMaliyet = otostickerMaliyet,
                        KarZarar = Math.Round(karZarar, 2),
                        KarZararYuzdesi = Math.Round(karZararYuzdesi, 2),
                        Durum = karZarar > 0 ? "Kar" : karZarar < 0 ? "Zarar" : "Başabaş",
                        Stock = trendyolProduct.quantity,
                        Category = trendyolProduct.categoryName ?? "",
                        Brand = trendyolProduct.brand ?? "",
                        ProductUrl = trendyolProduct.productUrl ?? "",
                        Images = trendyolProduct.images?.Select(i => i.url ?? "").Where(u => !string.IsNullOrEmpty(u)).ToList() ?? new List<string>()
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = karZararListesi,
                    toplamKarZarar = Math.Round(karZararListesi.Sum(x => (decimal)((dynamic)x).KarZarar), 2),
                    toplamUrunSayisi = karZararListesi.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Kar/zarar hesaplama sırasında hata oluştu: {ex.Message}",
                    error = ex.ToString()
                });
            }
        }

        /// <summary>
        /// Ürün bazlı satış istatistikleri endpoint'i
        /// Belirtilen gün sayısı içinde hangi ürünlerin ne kadar satıldığını gösterir
        /// </summary>
        [HttpGet("satis-istatistikleri/{kullaniciId}")]
        public async Task<IActionResult> GetSatisIstatistikleri(Guid kullaniciId, [FromQuery] int gunSayisi = 30)
        {
            try
            {
                if (gunSayisi <= 0)
                    return BadRequest("Gün sayısı 0'dan büyük olmalıdır.");

                var now = DateTime.UtcNow.AddHours(3); // Turkish timezone (UTC+3) - calculate once
                var baslangicTarihi = now.AddDays(-gunSayisi);
                Console.WriteLine($"Calculating sales stats for user {kullaniciId} from {baslangicTarihi} to {now}");
                // Sipariş ürünlerini çek (belirtilen tarih aralığında)
                var siparisUrunleri = await _dbContext.SiparisUrunleri
                    .Include(su => su.Urun)
                    .Include(su => su.Siparis)
                    .Where(su =>
                        su.Siparis.KullaniciId == kullaniciId &&
                        su.Siparis.CreatedAt >= baslangicTarihi)
                    .ToListAsync();

                // Ürün bazlı grupla ve istatistikleri hesapla
                var istatistikler = siparisUrunleri
                    .GroupBy(su => new
                    {
                        UrunId = su.Urun_Id,
                        UrunAdi = su.Urun.Ad,
                        ProductCode = su.Urun.ProductCode,
                        Image = su.Urun.Image
                    })
                    .Select(g => new
                    {
                        urunId = g.Key.UrunId,
                        urunAdi = g.Key.UrunAdi,
                        productCode = g.Key.ProductCode,
                        image = g.Key.Image ?? "",
                        toplamSatilanAdet = g.Sum(x => x.Adet),
                        toplamCiro = g.Sum(x => x.Toplam_Fiyat),
                        ortalamaFiyat = g.Average(x => x.Birim_Fiyat),
                        siparisSayisi = g.Select(x => x.Siparis_Id).Distinct().Count(),
                        sonSatisTarihi = g.Max(x => x.Siparis.CreatedAt)
                    })
                    .OrderByDescending(x => x.toplamSatilanAdet)
                    .ToList();

                // Toplam benzersiz siparişleri hesapla
                var toplamSiparisler = siparisUrunleri.Select(x => x.Siparis_Id).Distinct().Count();

                return Ok(new
                {
                    success = true,
                    period = gunSayisi == 1 ? "daily" : gunSayisi == 7 ? "weekly" : gunSayisi == 30 ? "monthly" : $"{gunSayisi}_days",
                    gunSayisi = gunSayisi,
                    baslangicTarihi = baslangicTarihi,
                    bitisTarihi = DateTime.UtcNow,
                    toplamSiparisler = toplamSiparisler,
                    toplamUrunSayisi = istatistikler.Count,
                    toplamSatilanAdet = istatistikler.Sum(x => x.toplamSatilanAdet),
                    toplamCiro = istatistikler.Sum(x => x.toplamCiro),
                    data = istatistikler
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = $"Satış istatistikleri alınırken hata oluştu: {ex.Message}",
                    error = ex.ToString()
                });
            }
        }

    }
}
