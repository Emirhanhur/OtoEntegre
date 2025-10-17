using System.Net.Http.Headers;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore; // ✅ Bunu ekle
using System.Text;              // Encoding.UTF8 için
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OtoEntegre.Api.Entities;
using OtoEntegre.Api.Repositories;
using OtoEntegre.Api.DTOs;
using OtoEntegre.Api.Services;
using OtoEntegre.Api.Data;
using System.IO;

namespace OtoEntegre.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SiparislerController : ControllerBase
    {
        private readonly OrderService _orderService;
        private readonly EntegrasyonService _entegrasyonService;
        private readonly TedarikService _tedarikService;
        private readonly TelegramService _telegramService;
        private readonly TrendyolService _trendyolService;

        private readonly IGenericRepository<Siparisler> _repo;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppDbContext _appDbContext;
        private readonly PdfLabelService _pdfLabelService;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OrderSyncBackgroundService> _logger;
        private readonly IHubContext<OrderHub> _orderHub;

        public SiparislerController(IHubContext<OrderHub> orderHub, IConfiguration configuration, EntegrasyonService entegrasyonService, ILogger<OrderSyncBackgroundService> logger,
            IWebHostEnvironment env,
            PdfLabelService pdfLabelService,
            AppDbContext appDbContext, TelegramService telegramService, TedarikService tedarikService, TrendyolService trendyolService, IHttpClientFactory httpClientFactory, OrderService orderService, IGenericRepository<Siparisler> repo)
        {
            _repo = repo;
            _logger = logger;
            _orderHub = orderHub;

            _appDbContext = appDbContext;
            _trendyolService = trendyolService;
            _tedarikService = tedarikService;
            _telegramService = telegramService;
            _orderService = orderService;
            _entegrasyonService = entegrasyonService;
            _httpClientFactory = httpClientFactory;
            _pdfLabelService = pdfLabelService;
            _env = env;
            _configuration = configuration;
        }


        [HttpPost("webhook")]
        public async Task<IActionResult> TrendyolWebhook([FromBody] TrendyolWebhookRequest request)
        {
            if (request == null)
                return BadRequest("Payload deserialize edilemedi.");
            if (request.Status == "Cancelled")
            {
                Console.WriteLine("Cancelled sipariş alındı, işlem yapılmadı.");
                return Ok(new { success = false, message = "Cancelled sipariş, işlem yapılmadı." });
            }
            Console.WriteLine("TrendyolWebhook başladı");

            if (request.Lines == null || !request.Lines.Any())
                return BadRequest("Sipariş satırı bulunamadı.");

            // Log dizini
            var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            // Payload log
            var rawJson = System.Text.Json.JsonSerializer.Serialize(request, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            var payloadFilePath = Path.Combine(logDir, $"trendyol_webhook_payload_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
            await System.IO.File.WriteAllTextAsync(payloadFilePath, rawJson);

            // İlk sipariş
            var firstOrder = request;
            var firstLine = firstOrder.Lines.FirstOrDefault();
            if (firstLine == null)
                return BadRequest("Sipariş satırı bulunamadı.");

            int merchantId = request.Lines.First().MerchantId;
            var entegrasyon = await _appDbContext.Entegrasyonlar.FirstOrDefaultAsync(e => e.Seller_Id == merchantId);
            if (entegrasyon == null)
                return BadRequest("Bu sipariş için entegrasyon bulunamadı.");

            // DB entity map
            var siparis = TrendyolMapping.MapToSiparis(request, entegrasyon);
            // DB entity map
            siparis.GeldigiYer = 0;

            // Sipariş zaten var mı kontrol et
            bool siparisVarMi = await _appDbContext.Siparisler
                .AnyAsync(s => s.SiparisNumarasi == siparis.SiparisNumarasi);

            if (siparisVarMi)
            {
                Console.WriteLine($"Sipariş zaten var: {siparis.SiparisNumarasi}, DB'ye eklenmedi.");
                return Ok(new { success = false, message = "Sipariş zaten mevcut", siparisNo = siparis.SiparisNumarasi });
            }
            else
            {
                await _orderHub.Clients.All.SendAsync("ReceiveOrderNotification", new
                {
                    Id = siparis.Id,
                    Siparis = siparis,
                    Message = "Yeni sipariş!"
                });

                // Sipariş dosyaya kaydet
                var siparisJson = System.Text.Json.JsonSerializer.Serialize(siparis, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                var siparisFilePath = Path.Combine(logDir, $"trendyol_webhook_siparis_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt");
                await System.IO.File.WriteAllTextAsync(siparisFilePath, siparisJson);
                Console.WriteLine($"MapToSiparis sonucu dosyaya kaydedildi: {siparisFilePath}");

                if (siparis.Id == Guid.Empty)
                    siparis.Id = Guid.NewGuid();

                _appDbContext.Siparisler.Add(siparis);
                try
                {
                    await _appDbContext.SaveChangesAsync();

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SaveChanges hatası: {ex.Message}");
                }


                // SiparisUrunleri eklenmeli
                foreach (var line in request.Lines)
                {
                    var urun = await _appDbContext.Urunler
                        .FirstOrDefaultAsync(u => u.ProductCode == line.ProductCode);

                    if (urun == null)
                    {
                        urun = new Urunler
                        {
                            Id = Guid.NewGuid(),
                            UrunTedarikBarcode = line.Barcode,
                            Ad = line.ProductName ?? "-",
                            ProductCode = line.ProductCode
                        };
                        _appDbContext.Urunler.Add(urun);
                        try
                        {
                            await _appDbContext.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"SaveChanges hatası: {ex.Message}");
                        }
                    }

                    var siparisUrun = new SiparisUrunleri
                    {
                        Id = Guid.NewGuid(),
                        Siparis_Id = siparis.Id,
                        Urun_Id = urun.Id,
                        Adet = line.Quantity,
                        Birim_Fiyat = line.Price,
                        Toplam_Fiyat = line.Price * line.Quantity
                    };
                    _appDbContext.SiparisUrunleri.Add(siparisUrun);
                }

                try
                {
                    await _appDbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"SaveChanges hatası: {ex.Message}");
                }


                // // Telegram mesajı
                // string message = $"{firstOrder.OrderNumber}\n";
                // string? firstImageUrl = null;
                // Console.WriteLine($"firstImageUrl ====={firstImageUrl}");
                // try
                // {
                //     var barcodes = request.Lines
                //         .Where(l => !string.IsNullOrWhiteSpace(l.Barcode))
                //         .Select(l => l.Barcode)
                //         .Distinct()
                //         .ToList();

                //     if (barcodes.Any())
                //     {
                //         long supplierId = entegrasyon.Seller_Id ?? 0;
                //         string apiKey = entegrasyon.Api_Key ?? string.Empty;
                //         string apiSecret = entegrasyon.Api_Secret ?? string.Empty;

                //         Console.WriteLine($"[{DateTime.Now}] Trendyol ürünleri getiriliyor, barkod sayısı: {barcodes.Count}");

                //         var allProducts = new List<TrendyolProductDto>();

                //         foreach (var barcode in barcodes)
                //         {
                //             if (string.IsNullOrWhiteSpace(barcode))
                //                 continue;

                //             var productList = await _trendyolService.GetProductsByBarcodesAsync(
                //                 supplierId, apiKey, apiSecret, new List<string> { barcode }
                //             );

                //             if (productList != null && productList.Any())
                //             {
                //                 allProducts.AddRange(productList);
                //                 _logger.LogInformation($"[{DateTime.Now}] Trendyol'dan ürün bulundu: {barcode}");
                //             }
                //             else
                //             {
                //                 _logger.LogWarning($"[{DateTime.Now}] Trendyol'dan ürün bulunamadı: {barcode}");
                //             }
                //         }


                //         foreach (var product in allProducts)
                //         {
                //             var imageUrl = product.Images.FirstOrDefault()?.Url ?? "";

                //             // DB kaydı ekleme veya güncelleme isteğe bağlı kalabilir
                //             var existingProduct = await _appDbContext.Urunler
                //                 .FirstOrDefaultAsync(p => p.ProductCode == product.ProductCode);

                //             if (existingProduct == null)
                //             {
                //                 existingProduct = new Urunler
                //                 {
                //                     Id = Guid.NewGuid(),
                //                     UrunTedarikBarcode = product.Barcode,
                //                     Ad = product.Title,
                //                     Kategori = product.CategoryName,
                //                     ProductCode = product.ProductCode,
                //                     Image = imageUrl
                //                 };
                //                 _appDbContext.Urunler.Add(existingProduct);
                //             }
                //             else
                //             {
                //                 existingProduct.Image = imageUrl; // her seferinde güncelle
                //             }

                //             // SiparisDosyalari ekleme, her zaman ekle (DB’de varsa da tekrar ekle)
                //             if (!string.IsNullOrWhiteSpace(imageUrl))
                //             {
                //                 var dosya = new SiparisDosyalari
                //                 {
                //                     Id = Guid.NewGuid(),
                //                     Siparis_Id = siparis.Id,
                //                     Dosya_Turu = "image",
                //                     Dosya_Url = imageUrl,
                //                     Created_At = DateTime.UtcNow
                //                 };
                //                 _appDbContext.Set<SiparisDosyalari>().Add(dosya);

                //                 // Telegram için firstImageUrl her zaman güncellenir
                //                 firstImageUrl ??= imageUrl;
                //             }
                //         }

                //         try
                //         {
                //             await _appDbContext.SaveChangesAsync();
                //         }
                //         catch (Exception ex)
                //         {
                //             Console.WriteLine($"SaveChanges hatası: {ex.Message}");
                //         }
                //     }
                // }
                // catch (Exception ex)
                // {
                //     Console.WriteLine($"Trendyol ürün bilgisi çekilirken hata: {ex}");
                // }


                // // Telegram + PDF gönderimi
                // try
                // {
                //     bool telegramSent = false;
                //     int retries = 3;
                //     for (int attempt = 1; attempt <= retries; attempt++)
                //     {
                //         try
                //         {
                //             Guid? userId = siparis.KullaniciId;
                //             telegramSent = await _telegramService.SendOrderMessageAsync(userId, message, firstImageUrl);
                //             break;
                //         }
                //         catch (HttpRequestException ex)
                //         {
                //             Console.WriteLine($"Telegram gönderim hatası, deneme {attempt}/{retries}: {ex.Message}");
                //             if (attempt == retries)
                //                 throw;
                //             await Task.Delay(2000);
                //         }
                //     }

                //     // DB güncelle
                //     var existingSiparis = await _repo.GetByIdAsync(siparis.Id);
                //     if (existingSiparis != null)
                //     {
                //         existingSiparis.TelegramSent = telegramSent;
                //         await _repo.SaveAsync();
                //     }

                //     // PDF oluştur ve gönder
                //     Console.WriteLine("pdf oluşturma başladı");
                //     var pdfPath = Path.Combine(Directory.GetCurrentDirectory(), "labels", "ornekBarkod (2).pdf");
                //     if (System.IO.File.Exists(pdfPath))
                //     {
                //         int pdfRetries = 2;
                //         for (int attempt = 1; attempt <= pdfRetries; attempt++)
                //         {
                //             try
                //             {
                //                 var filenames = new[] { "ornekBarkod (2).pdf" };
                //                 var basePaths = new[]
                //                 {
                //         _env.ContentRootPath,
                //         Directory.GetCurrentDirectory(),
                //         Path.GetFullPath(Path.Combine(_env.ContentRootPath, "..")),
                //         AppContext.BaseDirectory ?? _env.ContentRootPath
                //     };
                //                 var candidates = basePaths
                //                     .SelectMany(p => filenames.Select(f => Path.Combine(p, "labels", f)))
                //                     .ToList();
                //                 var template = candidates.FirstOrDefault(System.IO.File.Exists) ?? string.Empty;
                //                 if (string.IsNullOrEmpty(template))
                //                     throw new FileNotFoundException("PDF şablonu bulunamadı", string.Join(", ", filenames));

                //                 var outputDir = Path.Combine(_env.ContentRootPath, "labels");
                //                 string siparisNo = siparis.SiparisNumarasi ?? "-";
                //                 string adSoyad = siparis.MusteriAdSoyad;
                //                 string adres = siparis.MusteriAdres;
                //                 string kargoBarkod = siparis.PlatformUrunKod ?? "-";
                //                 string kargoBarkodNumarasi = siparis.Kod ?? siparis.SiparisNumarasi ?? "-";
                //                 string kargoTakipNumarasi = siparis.KargoTakipNumarasi ?? "-";
                //                 string paketNumarasi = siparis.PaketNumarasi ?? "-";
                //                 string urunTrendyolKod = siparis.UrunTrendyolKod ?? "-";

                //                 var urunDetaylari = firstOrder.Lines.Select(line => (
                //                     Title: line.ProductName ?? "-",
                //                     ImageUrl: firstImageUrl,
                //                     Price: line.Price,
                //                     Barkod: line.Barcode ?? "-",
                //                     StokKodu: line.ProductCode.ToString()
                //                 )).ToList();

                //                 var urunler = urunDetaylari.Select(u => (
                //                     Ad: u.Title,
                //                     Adet: 1,
                //                     Renk: string.IsNullOrWhiteSpace(siparis.Renk) ? "-" : siparis.Renk,
                //                     Beden: string.IsNullOrWhiteSpace(siparis.Beden) ? "-" : siparis.Beden,
                //                     Barkod: u.Barkod,
                //                     StokKodu: u.StokKodu
                //                 )).ToList();

                //                 var generatedPdf = await _pdfLabelService.GenerateFromTemplateAsync(
                //                     template,
                //                     outputDir,
                //                     siparisNo,
                //                     adSoyad,
                //                     adres,
                //                     kargoBarkod,
                //                     kargoBarkodNumarasi,
                //                     string.IsNullOrWhiteSpace(siparis.Renk) ? "-" : siparis.Renk,
                //                     string.IsNullOrWhiteSpace(siparis.Beden) ? "-" : siparis.Beden,
                //                     kargoTakipNumarasi,
                //                     urunTrendyolKod,
                //                     urunler,
                //                     new PdfLabelService.PdfLabelPositions
                //                     {
                //                         SiparisNoX = 98,
                //                         SiparisNoY = 276,
                //                         AdSoyadX = 98,
                //                         AdSoyadY = 308,
                //                         AdresX = 96,
                //                         AdresY = 330,
                //                         UrunBaslikX = 35,
                //                         UrunBaslikY = 400,
                //                         UrunSatirX = 35,
                //                         UrunSatirStartY = 420,
                //                         UrunSatirHeight = 14,
                //                         MaxUrunSatir = 10,
                //                         FontFamily = "Arial",
                //                         FontSize = 10,
                //                         FontBoldFamily = "Arial",
                //                         FontBoldSize = 11
                //                     }
                //                 );

                //                 var caption = $"{siparis.MusteriAdSoyad}";
                //                 await _telegramService.SendDocumentAsync(caption, generatedPdf, siparis.KullaniciId);
                //                 await _telegramService.SendOrderMessageAsync(siparis.KullaniciId, "-----------------");

                //                 break;
                //             }
                //             catch (HttpRequestException ex)
                //             {
                //                 Console.WriteLine($"PDF Telegram gönderim hatası, deneme {attempt}/{pdfRetries}: {ex.Message}");
                //                 if (attempt == pdfRetries)
                //                     throw;
                //                 await Task.Delay(1000);
                //             }
                //         }
                //     }
                // }
                // catch (Exception ex)
                // {
                //     Console.WriteLine($"Telegram işlemlerinde hata: {ex}");
                // }
            }
            return Ok(new { success = true, id = siparis.Id });
        }

        //BAYİNİN otostickerdaki SİPARİŞLERİNİ ÇEKER
        [HttpGet("by-user/{dealerId}/all")]
        public async Task<IActionResult> GetAllOrdersByUser(int dealerId)
        {
            Console.WriteLine($"GetAllOrdersByUser başladı: dealerId={dealerId}");
            var orders = await _orderService.GetAllOrdersWithDetailsByDealerIdAsync(dealerId);
            return Ok(orders);
        }

        // Sadece sayfa sayısı ile çek
        [HttpGet("by-user/{dealerId}")]
        public async Task<IActionResult> GetOrdersByUser(int dealerId, int page = 0, int pageSize = 10)
        {
            var orders = await _orderService.GetOrdersWithDetailsByDealerIdAsync(dealerId, page * pageSize, pageSize);
            return Ok(orders);
        }

        // Toplam sipariş sayısı
        [HttpGet("by-user/{dealerId}/count")]
        public async Task<IActionResult> GetOrderCountByUser(int dealerId)
        {
            var orders = await _orderService.GetAllOrdersWithDetailsByDealerIdAsync(dealerId);
            return Ok(orders.Count);
        }


        [HttpGet]
        public async Task<IEnumerable<Siparisler>> GetAll([FromQuery] string? durum = null, [FromQuery] string? sort = "desc")
        {
            Console.WriteLine("GetAll servisi başladı");
            var siparisler = await _repo.GetAllAsync();

            // Durum filtresi
            if (!string.IsNullOrEmpty(durum))
                siparisler = siparisler.Where(s => s.Durum == durum);

            // Sıralama (desc = varsayılan, asc = artan)
            siparisler = sort?.ToLower() switch
            {
                "asc" => siparisler.OrderBy(s => s.CreatedAt),
                "desc" => siparisler.OrderByDescending(s => s.CreatedAt),
                _ => siparisler.OrderByDescending(s => s.CreatedAt)
            };

            return siparisler;
        }

        [HttpPost("update-product-note")]
        public async Task<IActionResult> UpdateProductNote([FromBody] ProductNoteDto dto)
        {
            Console.WriteLine($"UpdateProductNote çağrıldı: OrderId={dto.OrderId}, ProductId={dto.ProductId}, Note={dto.Note}");

            // Hem sipariş hem ürün eşleşmeli!
            var siparisUrun = await _appDbContext.SiparisUrunleri
                .FirstOrDefaultAsync(su => su.Siparis_Id == dto.OrderId && su.Urun_Id == dto.ProductId);

            if (siparisUrun == null)
                return NotFound(new { success = false, message = "Ürün bulunamadı veya siparişe ait değil" });

            // Notu güncelle
            siparisUrun.SiparisNotu = dto.Note;
            await _appDbContext.SaveChangesAsync();

            return Ok(new { success = true, message = "Not güncellendi" });
        }

        public class ProductNoteDto
        {
            public Guid OrderId { get; set; }
            public Guid ProductId { get; set; }
            public string Note { get; set; } = "";
        }

        //VERİTABANINDAN KULLANICI BAZLI SİPARİŞLERİ ÇEKER
        [HttpGet("kullanici/{kullaniciId}")]
        public async Task<IActionResult> GetOrdersByUserFromDatabase(Guid kullaniciId,
    [FromQuery] int page = 0,
[FromQuery] int pageSize = 0,
    [FromQuery] string? durum = null,
    [FromQuery] string? sort = "desc")
        {
            Console.WriteLine($"GetOrdersByUserFromDatabase başladı - KullaniciId: {kullaniciId}");

            try
            {
                // Kullanıcıya ait siparişleri getir
                var query = _appDbContext.Siparisler
                    .Where(s => s.KullaniciId == kullaniciId);

                // Durum filtresi
                if (!string.IsNullOrEmpty(durum))
                    query = query.Where(s => s.Durum == durum);

                // Sıralama
                query = sort?.ToLower() switch
                {
                    "asc" => query.OrderBy(s => s.CreatedAt),
                    "desc" => query.OrderByDescending(s => s.CreatedAt),
                    _ => query.OrderByDescending(s => s.CreatedAt)
                };

                // Toplam kayıt sayısı
                var totalCount = await query.CountAsync();

                // Sayfalama: pageSize 0 ise tüm kayıtları getir
                IQueryable<Siparisler> siparislerQuery = query;
                if (pageSize > 0)
                {
                    siparislerQuery = siparislerQuery
                        .Skip(page * pageSize)
                        .Take(pageSize);
                }

                var siparisler = await siparislerQuery
    .Include(s => s.SiparisUrunleri)
        .ThenInclude(su => su.Urun)
    .ToListAsync();

                // DTO ile map et
                var siparisDto = siparisler.Select(s => new
                {
                    s.SiparisNumarasi,
                    s.MusteriAdSoyad,
                    Urunler = s.SiparisUrunleri
                                 .GroupBy(u => u.Urun_Id) // Aynı ürünü grupla
                                 .Select(g => new
                                 {
                                     g.First().Urun.Ad,
                                     Adet = g.Sum(x => x.Adet),
                                     Fiyat = g.First().Birim_Fiyat
                                 }).ToList()
                }).ToList();

                foreach (var siparis in siparisler)
                {
                    // Müşteri adını sadece sipariş başında yazdır
                    Console.WriteLine($"Müşteri: {siparis.MusteriAdSoyad} - Sipariş No: {siparis.SiparisNumarasi}");

                    // Ürünleri yazdır
                    foreach (var urun in siparis.SiparisUrunleri)
                    {
                        Console.WriteLine($"  Ürün: {urun.Urun.Ad} | Adet: {urun.Adet} | Fiyat: {urun.Birim_Fiyat}");
                    }
                }
                var result = new
                {
                    Data = siparisler,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = pageSize > 0 ? (int)Math.Ceiling((double)totalCount / pageSize) : 1
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetOrdersByUserFromDatabase hatası: {ex.Message}");
                return StatusCode(500, new { message = "Siparişler getirilirken bir hata oluştu.", error = ex.Message });
            }
        }

        [HttpGet("{siparisId}/urunler")]
        public async Task<IActionResult> GetSiparisUrunleri(Guid siparisId)
        {
            var siparis = await _appDbContext.Siparisler
                .FirstOrDefaultAsync(s => s.Id == siparisId);

            if (siparis == null)
                return NotFound(new { message = "Sipariş bulunamadı." });

            var urunler = await (from su in _appDbContext.SiparisUrunleri
                                 join u in _appDbContext.Urunler on su.Urun_Id equals u.Id
                                 join d in _appDbContext.SiparisDosyalari
                                     on u.Image equals d.Dosya_Url into dosyalar
                                 from d in dosyalar.DefaultIfEmpty()
                                 where su.Siparis_Id == siparisId && (d == null || d.Dosya_Turu == "image")
                                 select new
                                 {
                                     u.Id,
                                     u.Ad,
                                     u.ProductCode,
                                     Image = u.Image ?? string.Empty,
                                     su.Adet,
                                     su.Toplam_Fiyat
                                 }).ToListAsync();

            // Aynı ürünü tekilleştir (adet ve fiyat aynı kalacak)
            var tekUrunler = urunler
                .GroupBy(x => x.Id)
                .Select(g => g.First())
                .ToList();

            return Ok(new
            {
                siparisId = siparis.Id,
                SiparisNo = siparis.SiparisNumarasi,
                Urunler = tekUrunler
            });
        }



        //VERİTABANINDAN KULLANICI BAZLI SİPARİŞ SAYISINI ÇEKER
        [HttpGet("kullanici/{kullaniciId}/count")]
        public async Task<IActionResult> GetOrderCountByUserFromDatabase(Guid kullaniciId, [FromQuery] string? durum = null)
        {
            Console.WriteLine($"GetOrderCountByUserFromDatabase başladı - KullaniciId: {kullaniciId}");

            try
            {
                var query = _appDbContext.Siparisler
                    .Where(s => s.KullaniciId == kullaniciId);

                // Durum filtresi
                if (!string.IsNullOrEmpty(durum))
                    query = query.Where(s => s.Durum == durum);

                var count = await query.CountAsync();

                return Ok(new { Count = count, KullaniciId = kullaniciId, Durum = durum });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetOrderCountByUserFromDatabase hatası: {ex.Message}");
                return StatusCode(500, new { message = "Sipariş sayısı getirilirken bir hata oluştu.", error = ex.Message });
            }
        }


        //BAYİNİN ORDER ID'YE GÖRE TRENDYOLDAKİ SİPARİŞİNİ  ÇEKER

        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderDetail(int orderId)
        {
            Console.WriteLine("GetOrderDetail servisi başladı");

            var order = await _orderService.GetOrderByIdAsync(orderId);
            if (order == null) return NotFound();
            return Ok(order.Result); // order.Result içinde detay var
        }


        //BAYİNİN ID'YE GÖRE VERİTABANINDAKİ SİPARİŞİNİ  ÇEKER

        [HttpGet("by-guid/{id}")]
        public async Task<ActionResult<Siparisler>> GetById(Guid id)
        {
            Console.WriteLine("GetById servisi başladı");

            var item = await _repo.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }



        [HttpPost]
        public async Task<ActionResult<Siparisler>> Create(
     SiparisCreateDto dto,
     [FromServices] TedarikService tedarikService,
     [FromServices] TelegramService telegramService,
     [FromServices] IServiceScopeFactory scopeFactory)
        {
            if (dto == null)
                return BadRequest(new { message = "Sipariş verisi boş olamaz." });

            // 1️⃣ Siparişi oluştur
            var siparis = new Siparisler
            {
                Id = Guid.NewGuid(),
                SiparisNumarasi = dto.Siparis_Numarasi,
                ToplamTutar = dto.Toplam_Tutar,
                KargoUcreti = dto.Kargo_Ucreti ?? 0,
                OdemeDurumu = dto.Odeme_Durumu ?? string.Empty,
                Kod = dto.Kod ?? string.Empty,
                Durum = dto.Durum ?? string.Empty,
                KullaniciId = dto.Kullanici_Id,
                EntegrasyonId = dto.Entegrasyon_Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TelegramSent = false
            };

            // 2️⃣ DB'ye kaydet
            await _repo.AddAsync(siparis);
            await _repo.SaveAsync();
            Console.WriteLine($"Webhook -> Sipariş KullaniciId: {siparis.KullaniciId}");

            // 3️⃣ Arka planda tedarik ve telegram gönderimini başlat
            _ = Task.Run(async () =>
         {
             try
             {
                 using var scope = scopeFactory.CreateScope();

                 var scopedRepo = scope.ServiceProvider.GetRequiredService<IGenericRepository<Siparisler>>();
                 //var scopedTedarikService = scope.ServiceProvider.GetRequiredService<TedarikService>();
                 var scopedTelegramService = scope.ServiceProvider.GetRequiredService<TelegramService>();

                 // Tedarik gönder
                 //bool tedarikSent = await scopedTedarikService.SiparisiTedarikSitesineGonder(siparis.Kod);

                 // Telegram gönder
                 string imageUrl = "https://dummyimage.com/600x400/cccccc/000000&text=Sipariş+Resmi";
                 bool telegramSent = await scopedTelegramService.SendOrderMessageAsync(siparis.KullaniciId, siparis.SiparisNumarasi, imageUrl);

                 // DB güncelle
                 var siparisToUpdate = await scopedRepo.GetByIdAsync(siparis.Id);
                 //siparisToUpdate.TedarikSent = tedarikSent;
                 if (siparisToUpdate != null)
                 {
                     siparisToUpdate.TelegramSent = telegramSent;
                     siparisToUpdate.UpdatedAt = DateTime.UtcNow;
                 }

                 await scopedRepo.SaveAsync();
             }
             catch (Exception ex)
             {
                 Console.WriteLine($"[Sipariş Otomatik Gönderim Hatası] {ex.Message}");
             }
         });

            return CreatedAtAction(nameof(GetById), new
            {
                id = siparis.Id
            }, siparis);
        }






        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, SiparisUpdateDto dto)
        {
            var siparis = await _repo.GetByIdAsync(id);
            if (siparis == null) return NotFound();

            siparis.SiparisNumarasi = dto.Siparis_Numarasi;
            siparis.ToplamTutar = dto.Toplam_Tutar;
            siparis.KargoUcreti = dto.Kargo_Ucreti;
            siparis.OdemeDurumu = dto.Odeme_Durumu ?? string.Empty;
            siparis.Kod = dto.Kod ?? string.Empty;
            siparis.Durum = dto.Durum;
            siparis.KullaniciId = dto.Kullanici_Id;
            siparis.EntegrasyonId = dto.Entegrasyon_Id;
            siparis.UpdatedAt = DateTime.UtcNow;

            _repo.Update(siparis);
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


        [HttpPost("send-siparis-tedarik/{orderCode}")]
        public async Task<IActionResult> SendSiparisTedarik(string orderCode, [FromServices] TedarikService tedarikService)
        {

            Console.WriteLine("SendSiparisTedarik servisi başladı");

            try
            {
                await tedarikService.SiparisiTedarikSitesineGonder(orderCode);
                return Ok(new { success = true, message = "Sipariş Otosticker'a gönderildi." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }






        [HttpPost("create-and-send")]
        public async Task<IActionResult> CreateAndSendToOtosticker(SiparisCreateDto dto, [FromServices] OtostickerService otostickerService)
        {
            Console.WriteLine("CretateAndSendToOtosticker servisi başladı");

            // 1️⃣ Siparişi önce DB'ye kaydet
            var siparis = new Siparisler
            {
                Id = Guid.NewGuid(),
                SiparisNumarasi = dto.Siparis_Numarasi,
                ToplamTutar = dto.Toplam_Tutar,
                KargoUcreti = dto.Kargo_Ucreti ?? 0,
                OdemeDurumu = dto.Odeme_Durumu ?? string.Empty,
                Kod = dto.Kod ?? string.Empty,
                Durum = dto.Durum ?? string.Empty,
                KullaniciId = dto.Kullanici_Id,
                EntegrasyonId = dto.Entegrasyon_Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _repo.AddAsync(siparis);
            await _repo.SaveAsync();

            // 2️⃣ Otosticker DTO oluştur
            var orderDto = new OtostickerFastSaleOrderDto
            {
                Customer = new OtostickerCustomerDto
                {
                    Name = "Büşra",
                    Lastname = "Çap",
                    Email = "busracapp@gmail.com",
                    Phone = "05342146124",
                    City = "istanbul",
                    Distict = "sarıyer",
                    Address = "test adres",
                    TaxId = "111",
                    TaxBranch = "sss",
                    NationalId = "222"
                },
                Order = new OtostickerOrderDto
                {
                    Date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    PaymentType = 2,
                    Status = "Created",
                    Note = "Sipariş #" + siparis.SiparisNumarasi
                },
                Products = new List<OtostickerProductItemDto>
        {
            new OtostickerProductItemDto
            {
                Id = 12635,
                Price = 15.30m,
                Quantity = 1,
                Variant1 = "sarı"
            }
        }
            };

            // 3️⃣ Otosticker'a gönder
            var response = await otostickerService.CreateFastSaleOrderAsync(orderDto);

            return Ok(new
            {
                SavedOrder = siparis,
                OtostickerResult = response
            });
        }


        [HttpPut("siparisler/{packageId}/kargo-firmasi")]
        public async Task<IActionResult> ChangeCargoProvider(long packageId, [FromBody] ChangeCargoProviderRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CargoProvider))
                return BadRequest("Kargo firması boş olamaz.");

            // Entegrasyon bilgilerini DB'den al
            var entegrasyon = await _appDbContext.Entegrasyonlar
                .FirstOrDefaultAsync(e => e.Id == request.EntegrasyonId);

            if (entegrasyon == null)
                return NotFound("Entegrasyon bulunamadı.");

            if (!entegrasyon.Seller_Id.HasValue ||
                string.IsNullOrWhiteSpace(entegrasyon.Api_Key) ||
                string.IsNullOrWhiteSpace(entegrasyon.Api_Secret))
            {
                return BadRequest("Entegrasyon bilgileri eksik veya hatalı.");
            }

            var sellerId = entegrasyon.Seller_Id.Value;
            var apiKey = entegrasyon.Api_Key.Trim();
            var apiSecret = entegrasyon.Api_Secret.Trim();

            var httpClient = _httpClientFactory.CreateClient();

            // Trendyol API PUT request
            var url = $"https://apigw.trendyol.com/integration/order/sellers/{sellerId}/shipment-packages/{packageId}/cargo-providers";
            var payload = new { cargoProvider = request.CargoProvider };
            var jsonContent = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}"));

            var requestMessage = new HttpRequestMessage(HttpMethod.Put, url);
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
            requestMessage.Headers.Add("User-Agent", "MyAppIntegration/1.0");
            requestMessage.Content = jsonContent;

            Console.WriteLine($"PUT {url}");
            Console.WriteLine($"Payload: {JsonConvert.SerializeObject(payload)}");
            Console.WriteLine($"Auth: {auth.Substring(0, 5)}***");

            try
            {
                var response = await httpClient.SendAsync(requestMessage);
                var responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"Trendyol response status: {(int)response.StatusCode}, body: {responseBody}");

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, responseBody);
                }

                var siparis = await _appDbContext.Siparisler
                    .FirstOrDefaultAsync(s => s.PaketNumarasi == packageId.ToString() && s.EntegrasyonId == request.EntegrasyonId);

                if (siparis != null)
                {
                    siparis.CargoProviderName = request.CargoProvider;
                    siparis.UpdatedAt = DateTime.UtcNow;
                    await _appDbContext.SaveChangesAsync();
                }

                return Ok(new { message = "Kargo firması başarıyla değiştirildi." });
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Network hatası: {ex.Message}");
                return StatusCode(500, $"Trendyol API ile iletişim kurulamadı: {ex.Message}");
            }
        }


        [HttpPost("toplu-kargo-degistir")]
        public async Task<IActionResult> TopluKargoDegistir([FromBody] TopluKargoDto dto)
        {
            if (dto == null || dto.OrderIds == null || dto.OrderIds.Count == 0)
                return BadRequest("Sipariş listesi boş olamaz.");

            if (string.IsNullOrWhiteSpace(dto.ShippingCompany))
                return BadRequest("Kargo firması boş olamaz.");

            // Entegrasyon bilgilerini al
            var firstOrder = await _appDbContext.Siparisler
                .FirstOrDefaultAsync(s => dto.OrderIds.Contains(s.Id.ToString()));

            if (firstOrder == null)
                return NotFound("Sipariş bulunamadı.");

            var entegrasyon = await _appDbContext.Entegrasyonlar
                .FirstOrDefaultAsync(e => e.Id == firstOrder.EntegrasyonId);

            if (entegrasyon == null)
                return NotFound("Entegrasyon bulunamadı.");

            if (!entegrasyon.Seller_Id.HasValue ||
                string.IsNullOrWhiteSpace(entegrasyon.Api_Key) ||
                string.IsNullOrWhiteSpace(entegrasyon.Api_Secret))
            {
                return BadRequest("Entegrasyon bilgileri eksik veya hatalı.");
            }

            var sellerId = entegrasyon.Seller_Id.Value;
            var apiKey = entegrasyon.Api_Key.Trim();
            var apiSecret = entegrasyon.Api_Secret.Trim();

            var httpClient = _httpClientFactory.CreateClient();
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}"));

            var orders = await _appDbContext.Siparisler
                .Where(s => dto.OrderIds.Contains(s.Id.ToString()))
                .ToListAsync();

            var results = new List<dynamic>();

            foreach (var order in orders)
            {
                if (string.IsNullOrEmpty(order.PaketNumarasi))
                {
                    results.Add(new { order.Id, success = false, message = "Paket numarası boş, Trendyol güncellemesi atlanıyor." });
                    continue;
                }

                var url = $"https://apigw.trendyol.com/integration/order/sellers/{sellerId}/shipment-packages/{order.PaketNumarasi}/cargo-providers";
                var payload = new { cargoProvider = dto.ShippingCompany };
                var jsonContent = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

                var requestMessage = new HttpRequestMessage(HttpMethod.Put, url);
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
                requestMessage.Headers.Add("User-Agent", "MyAppIntegration/1.0");
                requestMessage.Content = jsonContent;

                try
                {
                    var response = await httpClient.SendAsync(requestMessage);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        order.CargoProviderName = dto.ShippingCompany;
                        order.UpdatedAt = DateTime.UtcNow;
                        results.Add(new { order.Id, success = true });
                    }
                    else
                    {
                        results.Add(new { order.Id, success = false, message = responseBody });
                    }
                }
                catch (HttpRequestException ex)
                {
                    results.Add(new { order.Id, success = false, message = $"HTTP hatası: {ex.Message}" });
                }
            }

            await _appDbContext.SaveChangesAsync();

            // dynamic ile çok basit ve null-safe
            return Ok(new
            {
                success = true,
                updated = results.Count(r => r.success == true),
                results
            });
        }


        public class TopluKargoDto
        {
            public List<string>? OrderIds { get; set; }
            public string? ShippingCompany { get; set; }
        }


        public class ChangeCargoProviderRequest
        {
            public string CargoProvider { get; set; } = string.Empty;
            public Guid EntegrasyonId { get; set; }

        }

    }
}
