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

    }
}
