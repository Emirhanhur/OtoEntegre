using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OtoEntegre.Api.DTOs;
using OtoEntegre.Api.Services;
using OtoEntegre.Api.Entities;
using OtoEntegre.Api.Data;
using OtoEntegre.Api.Converters;
using System.Text.Json;
using OtoEntegre.Api.Repositories; // Siparisler ve TrendyolOrderDto burada olmalı
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace OtoEntegre.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EntegrasyonlarController : ControllerBase
    {

        private readonly IGenericRepository<Siparisler> _repo;
        private readonly IGenericRepository<Urunler> _urunlerRepo;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly EntegrasyonService _entegrasyonService;
        private readonly AppDbContext _dbContext;
        private readonly DealerService _dealerService;
        private readonly TelegramService _telegramService;
        private readonly OtostickerService _otostickerService;
        private readonly TrendyolService _trendyolService;
        private readonly IWebHostEnvironment _env;
        private readonly PdfLabelService _pdfLabelService;


        public EntegrasyonlarController(
            IGenericRepository<Urunler> urunlerRepo,
            EntegrasyonService entegrasyonService,
            TrendyolService trendyolService,
            AppDbContext dbContext,
            DealerService dealerService,
            IHttpClientFactory httpClientFactory,
            IGenericRepository<Siparisler> repo,
            OtostickerService otostickerService,
            TelegramService telegramService,  // <-- ekledik
            IWebHostEnvironment env,
            PdfLabelService pdfLabelService)
        {
            _entegrasyonService = entegrasyonService;
            _dbContext = dbContext;
            _dealerService = dealerService;
            _httpClientFactory = httpClientFactory;  // <-- ata
            _telegramService = telegramService;
            _otostickerService = otostickerService;
            _trendyolService = trendyolService;
            _repo = repo;
            _urunlerRepo = urunlerRepo;
            _env = env;
            _pdfLabelService = pdfLabelService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EntegrasyonCreateDto dto)
        {
            var created = await _entegrasyonService.CreateAsync(dto);
            return Ok(created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] EntegrasyonCreateDto dto)
        {
            var updated = await _entegrasyonService.UpdateAsync(id, dto);
            return Ok(updated);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var entity = await _entegrasyonService.GetByIdAsync(id);
            if (entity == null) return NotFound();
            return Ok(entity);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _entegrasyonService.DeleteAsync(id);
            return NoContent();
        }




        [HttpGet("trendyol-orders/{entegrasyonId}")]
        public async Task<IActionResult> GetTrendyolOrders(Guid entegrasyonId, int page = 0, int size = 200, string? sortField = null, string? sortDir = "desc")
        {
            Console.WriteLine("trendyol-orders başladı");

            // Entegrasyonu kontrol et (opsiyonel)
            var entegrasyon = await _entegrasyonService.GetByIdAsync(entegrasyonId);
            if (entegrasyon == null)
                return NotFound(new { error = "Trendyol entegrasyonu bulunamadı." });

            // Siparişleri veritabanından çek
            var query = _dbContext.Siparisler
                .Where(s => s.EntegrasyonId == entegrasyonId);

            // Sıralama
            var sf = (sortField ?? "").ToLowerInvariant();
            var sd = (sortDir ?? "desc").ToLowerInvariant();

            query = sf switch
            {
                "siparis_numarasi" => sd == "asc" ? query.OrderBy(s => s.SiparisNumarasi) : query.OrderByDescending(s => s.SiparisNumarasi),
                "toplam_tutar" => sd == "asc" ? query.OrderBy(s => s.ToplamTutar) : query.OrderByDescending(s => s.ToplamTutar),
                "created_at" => sd == "asc" ? query.OrderBy(s => s.CreatedAt) : query.OrderByDescending(s => s.CreatedAt),
                "updated_at" => sd == "asc" ? query.OrderBy(s => s.UpdatedAt) : query.OrderByDescending(s => s.UpdatedAt),
                _ => sd == "asc" ? query.OrderBy(s => s.CreatedAt) : query.OrderByDescending(s => s.CreatedAt)
            };

            // Sayfalama
            var totalElements = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalElements / (double)size);

            var orders = await query
                .Skip(page * size)
                .Take(size)
                .ToListAsync();

            return Ok(new
            {
                totalElements,
                totalPages,
                page,
                size,
                orders
            });
        }




        [HttpGet("by-user/{kullaniciId}")]
        public async Task<IActionResult> GetByKullanici(Guid kullaniciId)
        {
            Console.WriteLine("by-user başladı");

            var result = await _dbContext.Entegrasyonlar
     .Include(e => e.Platform)
     .Where(e => e.Kullanici_Id == kullaniciId)
     .FirstOrDefaultAsync();

            if (result == null)
                return NotFound();

            return Ok(new
            {
                result.Id,
                result.Kullanici_Id,
                result.Api_Key,
                result.Api_Secret,
                result.Seller_Id,
                result.Platform_Id,
                PlatformAdi = result.Platform != null ? result.Platform.Ad : string.Empty
            });

        }


       /*[HttpPost("trendyol-webhook")]
        public async Task<IActionResult> TrendyolWebhook([FromBody] TrendyolOrderDto webhookOrder, [FromQuery] Guid kullaniciId)
        {
            Console.WriteLine("trendyol-webhook başladı");
            if (webhookOrder == null) return BadRequest();

            foreach (var line in webhookOrder.Lines)
            {
                Console.WriteLine($"Ürün: {line.ProductId}");
                Console.WriteLine($"Adet: {line.Quantity}");
                Console.WriteLine($"Fiyat: {line.SalePrice}");
                Console.WriteLine($"resim: {line.Images}");
                Console.WriteLine("----------------------");
            }
            var kullanici = await _dbContext.Kullanicilar.FirstOrDefaultAsync(u => u.Id == kullaniciId);
            if (kullanici == null) return BadRequest("Kullanıcı bulunamadı");

            var entegrasyon = (await _entegrasyonService.GetAllAsync())
                                .FirstOrDefault(e => e.Kullanici_Id == kullaniciId);
            if (entegrasyon == null) return BadRequest("Entegrasyon bulunamadı");


            var siparis = new Siparisler
            {
                Id = Guid.NewGuid(),
                SiparisNumarasi = webhookOrder.OrderNumber,
                ToplamTutar = webhookOrder.TotalPrice,
                Durum = webhookOrder.StatusEnum?.ToString() ?? webhookOrder.StatusRaw,
                CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(webhookOrder.OrderDate).UtcDateTime,
                UpdatedAt = DateTimeOffset.FromUnixTimeMilliseconds(webhookOrder.LastModifiedDate).UtcDateTime,
                TelegramSent = false,
                TedarikSent = false,
                TedarikKullaniciId = kullanici.Tedarik_Kullanici_Id,
                DealerId = (int)(entegrasyon.Seller_Id ?? 0),
                EntegrasyonId = entegrasyon.Id,
                KullaniciId = kullaniciId,
                // 🔽 EKLENMESİ GEREKENLER
                MusteriAdSoyad = $"{webhookOrder.CustomerFirstName} {webhookOrder.CustomerLastName}",
                MusteriAdres = webhookOrder.ShipmentAddress?.FullAddress ?? "",
                Beden = string.Join(",", webhookOrder.Lines.Select(l => l.ProductSize)),
                Renk = string.Join(",", webhookOrder.Lines.Select(l => l.MerchantSku)), // Renk bilgisi varsa burada
                PlatformUrunKod = string.Join(",", webhookOrder.Lines.Select(l => l.Barcode)), // barcode
                UrunTrendyolKod = string.Join(",", webhookOrder.Lines.Select(l => l.ProductCode)),
                KargoTakipNumarasi = webhookOrder.KargoTakipNumarasi.ToString(),
                PaketNumarasi = webhookOrder.PaketNumarasi.ToString()
            };


            await _repo.AddAsync(siparis);
            await _repo.SaveAsync();



            // Ürünleri tedarikçi sitesine gönder
            try
            {
                var products = webhookOrder.Lines.Select(l => new OtostickerProductItemDto
                {
                    Id = int.TryParse(l.ProductId, out var parsedId) ? parsedId : 0,  // fallback
                    Price = l.SalePrice,
                    Quantity = l.Quantity,
                    Variant1 = "" // Trendyol ürün varyantı varsa buraya koyabilirsin
                }).ToList();



                string message = $"Yeni Trendyol Siparişi:\n {webhookOrder.OrderNumber}\n" +
                                 $"Tutar: {webhookOrder.TotalPrice} {webhookOrder.CurrencyCode}\n---------------------------------------";

                var firstImageUrl = webhookOrder.Lines?
      .FirstOrDefault()?.Images?
      .FirstOrDefault()?.ImagesUrl;

                bool telegramSent;
                if (!string.IsNullOrWhiteSpace(firstImageUrl))
                {
                    telegramSent = await _telegramService.SendOrderMessageAsync(kullaniciId, message, firstImageUrl);
                }
                else
                {
                    // Fotoğraf yok, sadece metin gönder
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

                    telegramSent = await _telegramService.SendOrderMessageAsync(kullaniciId, message, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

                }
                siparis.TedarikSent = true;
                siparis.TelegramSent = telegramSent;
                await _repo.SaveAsync();

                // ✅ PDF oluşturma ve gönderme
                try
                {
                    var result = await SendSiparisTelegram(siparis.Id);
                    Console.WriteLine($"Telegram PDF gönderim sonucu: {result}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SendSiparisTelegram çağrısı hata verdi: {ex}");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("Tedarikçi sipariş veya Telegram gönderim hatası: " + ex.Message);
            }


            return Ok(new { success = true });
        }
*/


        [HttpPost("send-siparis-telegram/{orderId}")]
        public async Task<IActionResult> SendSiparisTelegram(Guid orderId)
        {
            Console.WriteLine($"SendSiparisTelegram başladı {orderId}");
            var siparis = await _dbContext.Siparisler
                .Include(s => s.Entegrasyonlar)
                .FirstOrDefaultAsync(s => s.Id == orderId);

            if (siparis == null)
                return NotFound();

            if (siparis.Entegrasyonlar == null)
                return BadRequest(new { sent = false, error = "Entegrasyon bilgisi bulunamadı." });

            try
            {
                var kullaniciId = siparis.KullaniciId;
                var urunKodlari = siparis.PlatformUrunKod?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

                var client = _httpClientFactory.CreateClient();
                var urunDetaylari = new List<(string Title, string ImageUrl, decimal Price, string Barkod, string StokKodu)>();

                foreach (var kod in urunKodlari)
                {
                    string url = $"https://apigw.trendyol.com/integration/product/sellers/{siparis.Entegrasyonlar.Seller_Id}/products?barcode={kod}";
                    client.DefaultRequestHeaders.Clear();
                    var byteArray = System.Text.Encoding.ASCII.GetBytes($"{siparis.Entegrasyonlar.Api_Key}:{siparis.Entegrasyonlar.Api_Secret}");
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                    try
                    {
                        var json = await client.GetStringAsync(url);
                        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var productResponse = JsonSerializer.Deserialize<TrendyolBatchProductResponse>(json, options);
                        var product = productResponse?.Content.FirstOrDefault();
                        if (product != null)
                        {
                            urunDetaylari.Add((
                                product.Title,
                                product.Images?.FirstOrDefault()?.Url ?? "",
                                product.SalePrice,
                                product.Barcode ?? kod,
                                product.StockCode ?? "-"
                            ));
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ürün kodu {kod} alınamadı: {ex.Message}");
                    }
                }

                // Telegram mesajı oluştur
                var messageBuilder = new StringBuilder();
                messageBuilder.AppendLine($"{siparis.SiparisNumarasi}");
                foreach (var urun in urunDetaylari)
                    messageBuilder.AppendLine($"🛒 {urun.Title} - {urun.Price}₺");

                string imageUrlToSend = urunDetaylari.FirstOrDefault().ImageUrl
                    ?? "https://dummyimage.com/600x400/cccccc/000000&text=Sipariş+Resmi";

                await _telegramService.SendOrderMessageAsync(kullaniciId, messageBuilder.ToString(), imageUrlToSend);

                // ✅ BURADA KARGO FİRMASINA GÖRE ŞABLON SEÇİYORUZ
                string cargoFileName = siparis.CargoProviderName switch
                {
                    "Yurtiçi Kargo Marketplace" => "yurtici_ornekBarkod (2).pdf",
                    "Trendyol Express Marketplace" => "tyexpress_ornekBarkod (2).pdf",
                    "Sürat Kargo Marketplace" => "surat_ornekBarkod (2).pdf",
                    "PTT Kargo Marketplace" => "ptt_ornekBarkod (2).pdf",
                    "Kolay Gelsin Marketplace" => "kolaygelsin_ornekBarkod (2).pdf",
                    "Horoz Kargo Marketplace" => "horoz_ornekBarkod (2).pdf",
                    "DHL eCommerce Marketplace" => "dhl_ornekBarkod (2).pdf",
                    "CEVA Marketplace" => "ceva_ornekBarkod (2).pdf",
                    "Borusan Lojistik Marketplace" => "borusan_ornekBarkod (2).pdf",
                    "Aras Kargo Marketplace" => "aras_ornekBarkod (2).pdf",
                    _ => "ornekBarkod (2).pdf"
                };

                var filenames = new[] { cargoFileName };
                var basePaths = new[]
                {
            _env.ContentRootPath,
            Directory.GetCurrentDirectory(),
            Path.GetFullPath(Path.Combine(_env.ContentRootPath, "..")),
            AppContext.BaseDirectory ?? _env.ContentRootPath
        };
                var candidates = basePaths
                    .SelectMany(p => filenames.Select(f => Path.Combine(p, "labels", f)))
                    .ToList();

                var template = candidates.FirstOrDefault(System.IO.File.Exists) ?? string.Empty;
                if (string.IsNullOrEmpty(template))
                    throw new FileNotFoundException("PDF şablonu bulunamadı", string.Join(", ", filenames));

                // PDF üretme
                var outputDir = Path.Combine(_env.ContentRootPath, "labels");

                var urunler = urunDetaylari.Select(u => (
                    Ad: u.Title,
                    Adet: 1,
                    Renk: string.IsNullOrWhiteSpace(siparis.Renk) ? "-" : siparis.Renk,
                    Beden: string.IsNullOrWhiteSpace(siparis.Beden) ? "-" : siparis.Beden,
                    Barkod: u.Barkod,
                    StokKodu: u.StokKodu
                )).ToList();

                var generatedPdf = await _pdfLabelService.GenerateFromTemplateAsync(
                    template,
                    outputDir,
                    siparis.SiparisNumarasi ?? "-",
                    siparis.MusteriAdSoyad,
                    siparis.MusteriAdres,
                    siparis.PlatformUrunKod ?? "-",
                    siparis.Kod ?? siparis.SiparisNumarasi ?? "-",
                    string.IsNullOrWhiteSpace(siparis.Renk) ? "-" : siparis.Renk,
                    string.IsNullOrWhiteSpace(siparis.Beden) ? "-" : siparis.Beden,
                    siparis.KargoTakipNumarasi ?? "-",
                    siparis.UrunTrendyolKod ?? "-",
                    urunler,
                    new PdfLabelService.PdfLabelPositions
                    {
                        SiparisNoX = 98,
                        SiparisNoY = 276,
                        AdSoyadX = 98,
                        AdSoyadY = 308,
                        AdresX = 96,
                        AdresY = 330,
                        UrunBaslikX = 35,
                        UrunBaslikY = 400,
                        UrunSatirX = 35,
                        UrunSatirStartY = 420,
                        UrunSatirHeight = 14,
                        MaxUrunSatir = 10,
                        FontFamily = "Arial",
                        FontSize = 10,
                        FontBoldFamily = "Arial",
                        FontBoldSize = 11
                    }
                );

                await _telegramService.SendDocumentAsync($"{siparis.MusteriAdSoyad}", generatedPdf, kullaniciId);
                                await _telegramService.SendOrderMessageAsync(siparis.KullaniciId, "-----------------");

                siparis.TelegramSent = true;
                await _repo.SaveAsync();
                return Ok(new { sent = true });
            }
            catch (Exception ex)
            {
                siparis.TelegramSent = false;
                await _repo.SaveAsync();
                return StatusCode(500, new { sent = false, error = ex.Message });
            }
        }


        [HttpGet("siparisler/{kullaniciId}")]
        public async Task<IActionResult> GetSiparisler(Guid kullaniciId)
        {
            Console.WriteLine($"GetSiparisler başladı ");

            var siparisler = await _repo.GetAllAsync();

            var filtered = siparisler.Where(s => s.KullaniciId == kullaniciId).ToList();

            var sent = filtered.Where(s => s.TelegramSent).ToList();
            var unsent = filtered.Where(s => !s.TelegramSent).ToList();

            return Ok(new
            {
                sent,
                unsent
            });
        }


        public class TrendyolProductDto
        {
            [JsonPropertyName("barcode")]
            public string Barcode { get; set; } = string.Empty;

            [JsonPropertyName("title")]
            public string Title { get; set; } = string.Empty;

            [JsonPropertyName("salePrice")]
            public decimal SalePrice { get; set; }

            [JsonPropertyName("productCode")]
            [JsonConverter(typeof(IntToStringConverter))]  // <<< burası
            public string StockCode { get; set; } = string.Empty;



            [JsonPropertyName("images")]
            public List<TrendyolProductImage> Images { get; set; } = new();
        }

        public class TrendyolProductImage
        {
            [JsonPropertyName("url")]
            public string Url { get; set; } = string.Empty;
        }
        public class TrendyolBatchProductResponse
        {
            [JsonPropertyName("content")]
            public List<TrendyolProductDto> Content { get; set; } = new();
        }
        public class TrendyolOrderResponse
        {

            public int Page { get; set; }
            public int Size { get; set; }
            public int TotalPages { get; set; }
            public int TotalElements { get; set; }
            public List<TrendyolOrderDto> Content { get; set; } = new List<TrendyolOrderDto>();
            public int TotalCount { get; set; }
        }


        public class TrendyolWebhookOrderDto
        {
            public string OrderNumber { get; set; } = string.Empty;
            public decimal TotalPrice { get; set; }
            public string CurrencyCode { get; set; } = string.Empty;
            [JsonConverter(typeof(JsonStringEnumConverter))] // veya custom converter
            public string Status { get; set; } = string.Empty;
            public List<OrderLineDto> Lines { get; set; } = new List<OrderLineDto>();
        }

        public class OrderLineDto
        {
            public int Id { get; set; }
            public int Quantity { get; set; }
            public ProductDto? Product { get; set; }
        }

        public class ProductDto
        {
            public string Name { get; set; } = string.Empty;
            public string ImageUrl { get; set; } = string.Empty;
        }
        public enum TrendyolOrderStatus
        {
            CREATED,
            PICKING,
            INVOICED,
            SHIPPED,
            CANCELLED,
            DELIVERED,
            UNDELIVERED,
            RETURNED,
            UNSUPPLIED,
            AWAITING,
            UNPACKED,
            AT_COLLECTION_POINT,
            VERIFIED,
            ReadyToShip,    // JSON'daki özel durumlar
            UnDelivered,
            AwaitingFulfillment // eksikse ekle

        }


        public class TrendyolOrderDto
        {
            [JsonPropertyName("id")]

            public long Id { get; set; }

            [JsonPropertyName("orderNumber")]
            public string OrderNumber { get; set; } = string.Empty;

            [JsonPropertyName("status")]
            public string StatusRaw { get; set; } = string.Empty;

            [JsonIgnore]

            public TrendyolOrderStatus? StatusEnum =>
                Enum.TryParse<TrendyolOrderStatus>(StatusRaw, true, out var result) ? result : null;

            public long OrderDate { get; set; }
            public long LastModifiedDate { get; set; }
            public decimal TotalPrice { get; set; }
            public string CurrencyCode { get; set; } = string.Empty;
            public string PlatformUrunKod { get; set; } = string.Empty;
            public string TrendyolUrunKod { get; set; } = string.Empty;
            public decimal CargoPrice { get; set; }

            // Burada JSON alanlarıyla eşleştirme yap
            [JsonPropertyName("customerFirstName")]
            public string CustomerFirstName { get; set; } = string.Empty;

            [JsonPropertyName("customerLastName")]
            public string CustomerLastName { get; set; } = string.Empty;

            [JsonPropertyName("shipmentAddress")]
            public ShipmentAddressDto ShipmentAddress { get; set; } = new();

            public string Renk { get; set; } = string.Empty;

            public string Beden { get; set; } = string.Empty;
            [JsonPropertyName("cargoTrackingNumber")]
            public long KargoTakipNumarasi { get; set; }

            [JsonIgnore]
            public long PaketNumarasi { get; set; }

            [JsonPropertyName("productCode")]
            public long? UrunTrendyolKod { get; set; }

            // Ürünler
            public List<TrendyolOrderLineDto> Lines { get; set; } = new();

            public void SetPaketNumarasi()
            {
                PaketNumarasi = Id;
            }
        }
        public class ShipmentAddressDto
        {
            [JsonPropertyName("fullAddress")]
            public string FullAddress { get; set; } = string.Empty;

            [JsonPropertyName("id")]
            public long Id { get; set; }

            [JsonPropertyName("firstName")]
            public string FirstName { get; set; } = string.Empty;

            [JsonPropertyName("lastName")]
            public string LastName { get; set; } = string.Empty;

            [JsonPropertyName("city")]
            public string City { get; set; } = string.Empty;

            [JsonPropertyName("district")]
            public string District { get; set; } = string.Empty;

            [JsonPropertyName("postalCode")]
            public string PostalCode { get; set; } = string.Empty;

            [JsonPropertyName("countryCode")]
            public string CountryCode { get; set; } = string.Empty;

        }
        public class TrendyolOrderLineDto
        {
            public string ProductId { get; set; } = string.Empty;
            [JsonPropertyName("productCode")]
            [JsonConverter(typeof(IntToStringConverter))]
            public string ProductCode { get; set; } = string.Empty; public string Barcode { get; set; } = string.Empty;
            public string Sku { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            [JsonPropertyName("productName")]
            public string ProductName { get; set; } = string.Empty;
            [JsonPropertyName("productSize")]
            public string ProductSize { get; set; } = string.Empty;
            [JsonPropertyName("merchantSku")]
            public string MerchantSku { get; set; } = string.Empty;
            public decimal SalePrice { get; set; }
            public int Quantity { get; set; }
            public List<TrendyolOrderImageDto> Images { get; set; } = new List<TrendyolOrderImageDto>();
        }

        public class TrendyolOrderImageDto
        {
            public string ImagesUrl { get; set; } = string.Empty;
        }

        // Ürün adından basit renk tespiti
        private static string GuessColorFromName(string productName)
        {
            if (string.IsNullOrWhiteSpace(productName)) return string.Empty;

            var name = productName.ToUpperInvariant();
            var candidates = new[]
            {
                "SİYAH","SIYAH","BLACK",
                "BEYAZ","WHITE",
                "KIRMIZI","RED",
                "MAVİ","MAVI","BLUE",
                "YEŞİL","YESIL","GREEN",
                "GRİ","GRI","GRAY","GREY",
                "PEMBE","PINK",
                "LACİVERT","LACIVERT","NAVY",
                "MOR","PURPLE",
                "TURUNCU","ORANGE",
                "KAHVERENGİ","KAHVERENGI","BROWN",
                "ALTIN","GOLD",
                "GÜMÜŞ","GUMUS","SILVER"
            };

            foreach (var c in candidates)
            {
                if (name.Contains(c)) return c.Substring(0, 1) + c.Substring(1).ToLower();
            }
            return string.Empty;
        }
    }
    public class IntToStringConverter : JsonConverter<string>
    {
        public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Number)
                return reader.GetInt32().ToString();
            if (reader.TokenType == JsonTokenType.String)
                return reader.GetString() ?? string.Empty;
            return string.Empty;
        }

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value);
        }
    }

}
