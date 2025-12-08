using System.Net.Http.Headers;
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
        private readonly KredilerService _kredilerService;
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
                KredilerService kredilerService,
            IWebHostEnvironment env,
            PdfLabelService pdfLabelService)
        {
            _entegrasyonService = entegrasyonService;
            _dbContext = dbContext;
            _dealerService = dealerService;
            _httpClientFactory = httpClientFactory;  // <-- ata
            _telegramService = telegramService;
            _kredilerService = kredilerService;
            _otostickerService = otostickerService;
            _trendyolService = trendyolService;
            _repo = repo;
            _urunlerRepo = urunlerRepo;
            _env = env;
            _pdfLabelService = pdfLabelService;
        }

        [HttpPost("send-iptal-telegram/{orderId}")]
        public async Task<IActionResult> SendIptalTelegram(Guid orderId)
        {
            Console.WriteLine($"SendIptalTelegram başladı {orderId}");
            var siparis = await _dbContext.Siparisler
                .FirstOrDefaultAsync(s => s.Id == orderId);

            if (siparis == null)
                return NotFound(new { sent = false, error = "Sipariş bulunamadı." });

            if (!siparis.KullaniciId.HasValue)
                return BadRequest(new { sent = false, error = "Siparişin kullanıcı bilgisi yok." });

            if (!siparis.TelegramMessageId.HasValue)
                return BadRequest(new { sent = false, error = "Önceki Telegram mesajı bulunamadı." });

            var message = $"⚠️⚠️⚠️ *Sipariş İptal Edildi*\n📦 Sipariş No: {siparis.SiparisNumarasi} \nMüşteri: {siparis.MusteriAdSoyad}";

            try
            {
                var sent = await _telegramService.SendReplyMessageAsync(siparis.KullaniciId, message, siparis.TelegramMessageId.Value);
                if (sent)
                {
                    siparis.CancelledNotified = true;
                    await _repo.SaveAsync();
                    return Ok(new { sent = true });
                }
                else
                {
                    return StatusCode(500, new { sent = false, error = "Telegram gönderimi başarısız." });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendIptalTelegram hata: " + ex.Message);
                return StatusCode(500, new { sent = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EntegrasyonCreateDto dto)
        {
            var created = await _entegrasyonService.CreateAsync(dto);
            return Ok(created);
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var list = await _entegrasyonService.GetAllAsync();
            return Ok(list);
        }

        [HttpGet("with-users")]
        public async Task<IActionResult> GetAllWithUsers()
        {
            var list = await _entegrasyonService.GetUsersWithIntegrationsAsync();
            return Ok(list);
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
                id = result.Id,
                kullaniciId = result.Kullanici_Id,
                apiKey = result.Api_Key,
                apiSecret = result.Api_Secret,
                sellerId = result.Seller_Id,
                platformId = result.Platform_Id,
                platformAdi = result.Platform != null ? result.Platform.Ad : string.Empty
            });

        }


        //         [HttpPost("trendyol-webhook")]
        //          public async Task<IActionResult> TrendyolWebhook([FromBody] TrendyolOrderDto webhookOrder, [FromQuery] Guid kullaniciId)
        //          {
        //              Console.WriteLine("trendyol-webhook başladı");
        //              if (webhookOrder == null) return BadRequest();

        //              foreach (var line in webhookOrder.Lines)
        //              {
        //                  Console.WriteLine($"Ürün: {line.ProductId}");
        //                  Console.WriteLine($"Adet: {line.Quantity}");
        //                  Console.WriteLine($"Fiyat: {line.SalePrice}");
        //                  Console.WriteLine($"resim: {line.Images}");
        //                  Console.WriteLine("----------------------");
        //              }
        //              var kullanici = await _dbContext.Kullanicilar.FirstOrDefaultAsync(u => u.Id == kullaniciId);
        //              if (kullanici == null) return BadRequest("Kullanıcı bulunamadı");

        //              var entegrasyon = (await _entegrasyonService.GetAllAsync())
        //                                  .FirstOrDefault(e => e.Kullanici_Id == kullaniciId);
        //              if (entegrasyon == null) return BadRequest("Entegrasyon bulunamadı");


        //              var siparis = new Siparisler
        //              {
        //                  Id = Guid.NewGuid(),
        //                  SiparisNumarasi = webhookOrder.OrderNumber,
        //                  ToplamTutar = webhookOrder.TotalPrice,
        //                  Durum = webhookOrder.StatusEnum?.ToString() ?? webhookOrder.StatusRaw,
        //                  CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(webhookOrder.OrderDate).UtcDateTime,
        //                  UpdatedAt = DateTimeOffset.FromUnixTimeMilliseconds(webhookOrder.LastModifiedDate).UtcDateTime,
        //                  TelegramSent = false,
        //                  TedarikSent = false,
        //                  TedarikKullaniciId = kullanici.Tedarik_Kullanici_Id,
        //                  DealerId = (int)(entegrasyon.Seller_Id ?? 0),
        //                  EntegrasyonId = entegrasyon.Id,
        //                  KullaniciId = kullaniciId,
        //                  // 🔽 EKLENMESİ GEREKENLER
        //                  MusteriAdSoyad = $"{webhookOrder.CustomerFirstName} {webhookOrder.CustomerLastName}",
        //                  MusteriAdres = webhookOrder.ShipmentAddress?.FullAddress ?? "",
        //                  Beden = string.Join(",", webhookOrder.Lines.Select(l => l.ProductSize)),
        //                  Renk = string.Join(",", webhookOrder.Lines.Select(l => l.MerchantSku)), // Renk bilgisi varsa burada
        //                  PlatformUrunKod = string.Join(",", webhookOrder.Lines.Select(l => l.Barcode)), // barcode
        //                  UrunTrendyolKod = string.Join(",", webhookOrder.Lines.Select(l => l.ProductCode)),
        //                  KargoTakipNumarasi = webhookOrder.KargoTakipNumarasi.ToString(),
        //                  PaketNumarasi = webhookOrder.PaketNumarasi.ToString()
        //              };


        //              await _repo.AddAsync(siparis);
        //              await _repo.SaveAsync();

        //              // ✅ SiparisUrunleri tablosuna ürünleri ekle
        //              foreach (var line in webhookOrder.Lines)
        //              {
        //                  // Önce ürünü Urunler tablosuna ekle veya güncelle
        //                  var urun = await _dbContext.Urunler.FirstOrDefaultAsync(u => u.ProductCode == long.Parse(line.ProductCode));
        //                  if (urun == null)
        //                  {
        //                      urun = new Urunler
        //                      {
        //                          Id = Guid.NewGuid(),
        //                          Ad = line.ProductName ?? "Bilinmeyen Ürün",
        //                          ProductCode = long.Parse(line.ProductCode),
        //                          Image = line.Images?.FirstOrDefault()?.ImagesUrl ?? "",
        //                          Created_At = DateTime.UtcNow,
        //                          Updated_At = DateTime.UtcNow
        //                      };
        //                      _dbContext.Urunler.Add(urun);
        //                  }

        //                  // SiparisUrunleri tablosuna ekle
        //                  var siparisUrunu = new SiparisUrunleri
        //                  {
        //                      Id = Guid.NewGuid(),
        //                      Siparis_Id = siparis.Id,
        //                      Urun_Id = urun.Id,
        //                      Adet = line.Quantity,
        //                      Toplam_Fiyat = line.SalePrice * line.Quantity,
        //                      Birim_Fiyat = line.SalePrice
        //                  };
        //                  _dbContext.SiparisUrunleri.Add(siparisUrunu);
        //              }
        //              await _dbContext.SaveChangesAsync();

        //              // Ürünleri tedarikçi sitesine gönder
        //              try
        //              {
        //                  var products = webhookOrder.Lines.Select(l => new OtostickerProductItemDto
        //                  {
        //                      Id = int.TryParse(l.ProductId, out var parsedId) ? parsedId : 0,  // fallback
        //                      Price = l.SalePrice,
        //                      Quantity = l.Quantity,
        //                      Variant1 = "" // Trendyol ürün varyantı varsa buraya koyabilirsin
        //                  }).ToList();



        //                  string message = $"Yeni Trendyol Siparişi:\n {webhookOrder.OrderNumber}\n" +
        //                                   $"Tutar: {webhookOrder.TotalPrice} {webhookOrder.CurrencyCode}\n---------------------------------------";

        //                  var firstImageUrl = webhookOrder.Lines?
        //        .FirstOrDefault()?.Images?
        //        .FirstOrDefault()?.ImagesUrl;

        //                  bool telegramSent;
        //                  if (!string.IsNullOrWhiteSpace(firstImageUrl))
        //                  {
        //                      telegramSent = await _telegramService.SendOrderMessageAsync(kullaniciId, message, firstImageUrl);
        //                  }
        //                  else
        //                  {
        //                      // Fotoğraf yok, sadece metin gönder
        //  #pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.

        //                      telegramSent = await _telegramService.SendOrderMessageAsync(kullaniciId, message, null);
        //  #pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.

        //                  }
        //                  siparis.TedarikSent = true;
        //                  siparis.TelegramSent = telegramSent;
        //                  await _repo.SaveAsync();

        //                  // ✅ PDF oluşturma ve gönderme
        //                  try
        //                  {
        //                      var result = await SendSiparisTelegram(siparis.Id);
        //                      Console.WriteLine($"Telegram PDF gönderim sonucu: {result}");
        //                  }
        //                  catch (Exception ex)
        //                  {
        //                      Console.WriteLine($"SendSiparisTelegram çağrısı hata verdi: {ex}");
        //                  }

        //              }
        //              catch (Exception ex)
        //              {
        //                  Console.WriteLine("Tedarikçi sipariş veya Telegram gönderim hatası: " + ex.Message);
        //              }


        //              return Ok(new { success = true });
        //          }




        [HttpPost("send-siparis-telegram/{orderId}")]
        public async Task<IActionResult> SendSiparisTelegram(Guid orderId)
        {
            Console.WriteLine($"SendSiparisTelegram başladı {orderId}");
            var siparis = await _dbContext.Siparisler
     .Include(s => s.Entegrasyonlar)
     .Include(s => s.SiparisUrunleri)                 // ürünleri dahil et
         .ThenInclude(su => su.Urun)                  // ürün bilgileriyle birlikte
     .FirstOrDefaultAsync(s => s.Id == orderId);

            if (siparis == null)
                return NotFound();

            if (siparis.Entegrasyonlar == null)
                return BadRequest(new { sent = false, error = "Entegrasyon bilgisi bulunamadı." });

            var entegrasyon = siparis.Entegrasyonlar;
            if (entegrasyon == null)
                return BadRequest("Entegrasyon bilgisi bulunamadı.");

            if (!entegrasyon.Seller_Id.HasValue ||
                string.IsNullOrWhiteSpace(entegrasyon.Api_Key) ||
                string.IsNullOrWhiteSpace(entegrasyon.Api_Secret))
            {
                return BadRequest("Trendyol entegrasyon bilgileri eksik veya hatalı.");
            }

            var sellerId = entegrasyon.Seller_Id.Value;
            var apiKey = entegrasyon.Api_Key.Trim();
            var apiSecret = entegrasyon.Api_Secret.Trim();

            var httpClient = _httpClientFactory.CreateClient();
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}"));
            var orderNumber = siparis.SiparisNumarasi;

            // 🔍 Debug log ekle (yetkilendirme ve URL bilgisi)
            Console.WriteLine("🔎 Trendyol istek bilgileri:");
            Console.WriteLine($"   📦 SellerId      : {sellerId}");
            Console.WriteLine($"   🔑 ApiKey        : {apiKey}");
            Console.WriteLine($"   🧩 ApiSecret     : {apiSecret.Substring(0, Math.Min(4, apiSecret.Length))}**** (maskelendi)");
            Console.WriteLine($"   🌐 Request URL   : https://apigw.trendyol.com/integration/order/sellers/{sellerId}/orders?orderNumber={orderNumber}");
            Console.WriteLine($"   🪶 Authorization : Basic {auth.Substring(0, 10)}**** (base64 maskelendi)");

            try
            {

                var getUrl = $"https://apigw.trendyol.com/integration/order/sellers/{sellerId}/orders?orderNumber={orderNumber}";
                var getRequest = new HttpRequestMessage(HttpMethod.Get, getUrl);
                getRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", auth);
                getRequest.Headers.Add("User-Agent", "MyAppIntegration/1.0");

                var getResponse = await httpClient.SendAsync(getRequest);
                var getBody = await getResponse.Content.ReadAsStringAsync();

                Console.WriteLine($"📦 Trendyol yanıtı: {(int)getResponse.StatusCode} - {getBody}");

                if (!getResponse.IsSuccessStatusCode)
                {
                    return Ok(new
                    {
                        success = false,
                        message = "Trendyol sipariş doğrulaması başarısız.",
                        trendyolResponse = getBody
                    });
                }

                if (string.IsNullOrWhiteSpace(getBody) || !getBody.Contains(orderNumber))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Trendyol'da bu sipariş bulunamadı.",
                        trendyolResponse = getBody
                    });
                }

                Console.WriteLine("✅ Trendyol siparişi doğrulandı, Telegram gönderimi başlatılıyor...");
                var kullaniciId = siparis.KullaniciId;
                if (!kullaniciId.HasValue)
                    return BadRequest(new { sent = false, error = "Siparişin kullanıcı bilgisi yok." });

                var guidKullanici = kullaniciId.Value;

                var krediHarcanabildiMi = await _kredilerService.ConsumeOneAsync(guidKullanici);

                if (!krediHarcanabildiMi)
                {
                    return BadRequest(new
                    {
                        sent = false,
                        error = "Krediniz tükendi. Yeni sipariş gönderemezsiniz."
                    });
                }

                // PDF için tüm ürünleri veritabanından çek
                var urunlerDb = await (from su in _dbContext.SiparisUrunleri
                                       join u in _dbContext.Urunler on su.Urun_Id equals u.Id
                                       join d in _dbContext.SiparisDosyalari
                                           on u.Image equals d.Dosya_Url into dosyalar
                                       from d in dosyalar.DefaultIfEmpty()
                                       where su.Siparis_Id == siparis.Id && (d == null || d.Dosya_Turu == "image")
                                       select new
                                       {
                                           UrunId = su.Urun_Id,
                                           u.Ad,
                                           u.ProductCode,
                                           Image = u.Image ?? string.Empty,
                                           su.Adet,
                                           siparis.Renk,
                                           siparis.Beden,
                                           su.MerchantSku,
                                           su.SiparisNotu,
                                           u.UrunTedarikBarcode,
                                           IsOtostickerProduct = _dbContext.Otosticker_Urunler
                                   .Any(o => o.ProductCode == u.ProductCode && o.KullaniciId == siparis.KullaniciId)
                                       }).Distinct().ToListAsync();
                Console.WriteLine($"sipariş stok kodu = {string.Join(", ", urunlerDb.Select(u => u.MerchantSku))}");

                // PDF tuple listesi
                var pdfUrunler = urunlerDb
        .GroupBy(u => u.UrunId)
        .Select(g => g.First())
        .Select(u => (
            Ad: u.Ad,
            Adet: u.Adet,
            Renk: string.IsNullOrWhiteSpace(u.Renk) ? "-" : u.Renk,
            Beden: string.IsNullOrWhiteSpace(u.Beden) ? "-" : u.Beden,
            Barkod: u.UrunTedarikBarcode ?? "-",
            StokKodu: u.ProductCode?.ToString() ?? "-",
            MerchantSku: u.MerchantSku ?? "-",
            SiparisNotu: _dbContext.SiparisUrunleri
                .Where(x => x.Siparis_Id == siparis.Id && x.Urun_Id == u.UrunId)
                .Select(x => x.SiparisNotu)
                .FirstOrDefault() ?? "-"
        )).ToList();





                // Telegram mesajı
                bool storedMessageId = false;
                foreach (var urun in urunlerDb)
                {
                    var messageBuilder = new StringBuilder();
                    if (urun.Adet > 1)
                        messageBuilder.AppendLine($"{urun.Adet} adet");

                    var imageUrl = !string.IsNullOrWhiteSpace(urun.Image)
                        ? urun.Image
                        : "https://dummyimage.com/600x400/cccccc/000000&text=Sipariş+Resmi";

                    if (!storedMessageId)
                    {
                        // İlk gönderimden dönen message_id ve chat_id'yi sakla
                        var result = await _telegramService.SendOrderMessageWithResultAsync(guidKullanici, messageBuilder.ToString(), imageUrl);
                        if (!result.success)
                            return StatusCode(500, new { sent = false, error = "Telegram gönderimi başarısız." });

                        if (result.messageId.HasValue)
                        {
                            siparis.TelegramMessageId = result.messageId.Value;
                        }
                        if (!string.IsNullOrWhiteSpace(result.chatId))
                        {
                            siparis.TelegramChatId = result.chatId;
                        }
                        storedMessageId = true;
                        await _repo.SaveAsync();
                    }
                    else
                    {
                        var sent = await _telegramService.SendOrderMessageAsync(guidKullanici, messageBuilder.ToString(), imageUrl);
                        if (!sent)
                            return StatusCode(500, new { sent = false, error = "Telegram gönderimi başarısız." });
                    }
                }




                // PDF şablon seçimi
                var cargoMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
    { "Yurtiçi Kargo Marketplace", "yurtici_ornekBarkod (2).pdf" },
    { "YKMP", "yurtici_ornekBarkod (2).pdf" },
    { "Trendyol Express Marketplace", "tyexpress_ornekBarkod (2).pdf" },
    { "TEXMP", "tyexpress_ornekBarkod (2).pdf" },
    { "Sürat Kargo Marketplace", "surat_ornekBarkod (2).pdf" },
    { "SURATMP", "surat_ornekBarkod (2).pdf" },
    { "PTT Kargo Marketplace", "ptt_ornekBarkod (2).pdf" },
    { "PTTMP", "ptt_ornekBarkod (2).pdf" },
    { "Kolay Gelsin Marketplace", "kolaygelsin_ornekBarkod (2).pdf" },
    { "KOLAYGELSINMP", "kolaygelsin_ornekBarkod (2).pdf" },
    { "Horoz Kargo Marketplace", "horoz_ornekBarkod (2).pdf" },
    { "HOROZMP", "horoz_ornekBarkod (2).pdf" },
    { "DHL eCommerce Marketplace", "dhl_ornekBarkod (2).pdf" },
    { "DHLECOMMP", "dhl_ornekBarkod (2).pdf" },
    { "CEVA Marketplace", "ceva_ornekBarkod (2).pdf" },
    { "CEVAMP", "ceva_ornekBarkod (2).pdf" },
    { "Aras Kargo Marketplace", "aras_ornekBarkod (2).pdf" },
    { "ARASMP", "aras_ornekBarkod (2).pdf" },
    { "Borusan Lojistik Marketplace", "borusan_ornekBarkod (2).pdf" }
};

                string cargoFileName = cargoMap.TryGetValue(siparis.CargoProviderName, out var fileName)
                    ? fileName
                    : "ornekBarkod (2).pdf";


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
                    throw new FileNotFoundException("PDF şablonu bulunamadı EntegrasyonController", string.Join(", ", filenames));

                var outputDir = Path.Combine(_env.ContentRootPath, "labels");


                // PDF oluştur
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
                    pdfUrunler, // burada tuple içinde MerchantSku da var
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

                // PDF gönderimi sonrası sipariş notu kontrolü
                var urunNotlari = pdfUrunler
                    .Where(u => !string.IsNullOrWhiteSpace(u.SiparisNotu) && u.SiparisNotu != "-")
                    .Select(u => $"{u.Ad}: {u.SiparisNotu}")
                    .ToList();

                if (urunNotlari.Any())
                {
                    var notMesaji = "📝 *Sipariş Notları:*\n" + string.Join("\n", urunNotlari);
                    await _telegramService.SendOrderMessageAsync(siparis.KullaniciId, notMesaji);
                }
                var pdfBytes = await System.IO.File.ReadAllBytesAsync(generatedPdf);

                var pdfSent = await _telegramService.SendDocumentAsync(
                    caption: $"{siparis.MusteriAdSoyad}",
                    fileBytes: pdfBytes,
                    userId: kullaniciId,
                    fileName: $"{siparis.MusteriAdSoyad}.pdf"
                );

                if (!pdfSent)
                {
                    return StatusCode(500, new { sent = false, error = "PDF Telegram'a gönderilemedi." });
                }



                var user = await _dbContext.Kullanicilar.FirstOrDefaultAsync(u => u.Id == siparis.KullaniciId);
                
                if (user == null)
                {
                    Console.WriteLine("Kullanıcı bulunamadı, OtoSticker gönderimi atlandı.");
                }
                else
                {
                    if (user.Tedarik_Musteri_No == 55)
                    {
                         // 🔹 Otosticker bayi listesini çek (kullanıcının e-postası ile eşleştir)
                    var dealerListResponse = await _otostickerService.GetDealerListAsync();
                    var dealer = dealerListResponse?.Result?.List?
                        .FirstOrDefault(d => d.Email.Trim().Equals(user.Email.Trim(), StringComparison.OrdinalIgnoreCase));

                    if (dealer == null)
                    {
                        // Console.WriteLine($"OtoSticker bayi bulunamadı: {user.Email}");
                        // await _telegramService.SendOrderMessageAsync(user.Id, $"⚠️ OtoSticker bayi bulunamadı: {user.Email}");
                    }
                    else
                    {
                        // 🔹 Sipariş ürünlerini getir ve urun_tedarik_barcode üzerinden barcode eşleştir
                        var urunler = await (from su in _dbContext.SiparisUrunleri
                                             join u in _dbContext.Otosticker_Urunler
                                                 on su.Urun.ProductCode equals u.ProductCode
                                             where su.Siparis_Id == siparis.Id
                                                   && u.KullaniciId == siparis.KullaniciId  // kullanıcıya ait ürünler
                                             select new
                                             {
                                                 u.Id,
                                                 u.UrunTedarikBarcode,
                                                 su.Adet,
                                                 su.Toplam_Fiyat,
                                             })
                      .Select(x => new
                      {
                          Id = x.Id,
                          Barcode = x.UrunTedarikBarcode,
                          x.Adet,
                          Fiyat = x.Toplam_Fiyat,
                      })
                      .ToListAsync();


                        if (urunler.Count == 0)
                        {
                            // await _telegramService.SendOrderMessageAsync(user.Id, "⚠️ Siparişe ait ürün bulunamadı.");
                        }
                        else
                        {
                            // 🔹 OtoSticker fastSale isteği
                            var productList = new List<object>();

                            foreach (var urun in urunler) // <-- burası siparis.SiparisUrunleri değil
                            {
                                var barcode = urun.Barcode;
                                if (string.IsNullOrWhiteSpace(barcode))
                                {
                                    Console.WriteLine($"⚠️ Ürünün barkodu yok: Id={urun.Id}");
                                    continue;
                                }

                                var otoProduct = await _otostickerService.GetProductByBarcodeAsync(barcode);

                                decimal? otoPrice = otoProduct?.SalePrice;
                                if (!otoPrice.HasValue)
                                {
                                    Console.WriteLine($"⚠️ OtoSticker fiyatı bulunamadı, varsayılan 0 kullanılıyor (barcode={barcode})");
                                    otoPrice = 0;
                                }

                                productList.Add(new
                                {
                                    id = otoProduct?.ProductId,
                                    price = otoPrice,
                                    quantity = urun.Adet,
                                    variant1 = siparis.Renk ?? ""
                                });
                            }



                            // FastSale JSON
                            var fastSaleRequest = new
                            {
                                customer = new
                                {
                                    ID = user.Tedarik_Kullanici_Id,
                                    email = dealer.Email,
                                    name = dealer.Name,
                                    lastname = dealer.Lastname,
                                    code = dealer.Code,
                                    title = dealer.Title,
                                    group = dealer.Group,
                                    status = dealer.Status,
                                    balance = dealer.Balance,
                                    discount = dealer.Discount,
                                    nationalId = dealer.NationalId,
                                    taxId = dealer.TaxId,
                                    taxBranch = dealer.TaxBranch,
                                    phone = user.Telefon,
                                    city = user.Sehir,
                                    district = user.Ilce,
                                    address = user.Adres
                                },
                                order = new
                                {
                                    date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                    paymentType = 10,
                                    status = 1,
                                    note = ""
                                },
                                products = productList
                            };





                            try
                            {
                                Console.WriteLine($"OtoSticker siparişi gönderiliyor... Kullanıcı: {user.Email}");
                                var result = await _otostickerService.CreateFastSaleAsync(fastSaleRequest, dealer, kullaniciId.Value);
                                Console.WriteLine("OtoSticker yanıtı: " + result);

                                // 🔹 OtoSticker sipariş listesini çek
                                var orderListJson = await _otostickerService.GetOrderListAsync(dealer.Id);

                                if (orderListJson != null)
                                {
                                    try
                                    {
                                        var list = orderListJson.RootElement
                                            .GetProperty("result")
                                            .GetProperty("list");

                                        if (list.GetArrayLength() > 0)
                                        {
                                            var lastOrder = list[0];
                                            var otoOrderId = lastOrder.TryGetProperty("id", out var idProp)
     ? idProp.GetInt32().ToString()
     : "(id bulunamadı)";
                                            var otoOrderNo = lastOrder.TryGetProperty("code", out var codeProp)
                                                ? codeProp.GetString()
                                                : "(code bulunamadı)";

                                            var message = $"📦 *OtoSticker Sipariş Numarası: {otoOrderNo}";
                                            await _telegramService.SendOrderMessageAsync(user.Id, message);

                                        }
                                        else
                                        {
                                            await _telegramService.SendOrderMessageAsync(user.Id, "⚠️ OtoSticker sipariş listesi boş döndü.");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine("Sipariş listesi parse hatası: " + ex.Message);
                                        await _telegramService.SendOrderMessageAsync(user.Id, $"⚠️ Sipariş listesi okunamadı: {ex.Message}");
                                    }
                                }

                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("OtoSticker siparişi başarısız: " + ex.Message);
                                await _telegramService.SendOrderMessageAsync(
                                    user.Id,
                                    $"⚠️ OtoSticker siparişi başarısız:\n{ex.Message}"
                                );
                            }
                        }
                    }
                    }
                }

                await _telegramService.SendOrderMessageAsync(siparis.KullaniciId, "-----------------");


                siparis.TelegramSent = true;
                await _repo.SaveAsync();
                return Ok(new { sent = true });
            }
            catch (Exception ex)
            {
                if (siparis != null)
                {
                    siparis.TelegramSent = false;
                    await _repo.SaveAsync();
                }
                Console.WriteLine("SendSiparisTelegram hata: " + ex);

                Console.WriteLine("SendSiparisTelegram hata: " + ex.Message);
                return StatusCode(500, new { sent = false, error = ex.Message });
            }
        }
        public class TopluPdfRequest
        {
            public List<Guid> OrderIds { get; set; }

            // kullanıcı isterse Telegram’a göndersin
            public bool SendToTelegram { get; set; } = false;
        }


        [HttpPost("toplu-pdf")]
        public async Task<IActionResult> TopluPdfOlustur([FromBody] TopluPdfRequest request)
        {
            if (request.OrderIds == null || !request.OrderIds.Any())
                return BadRequest("Sipariş seçilmedi.");

            // 1. Seçilen siparişleri veritabanından çek
            var siparisler = await _dbContext.Siparisler
                .Include(s => s.SiparisUrunleri)
                .ThenInclude(su => su.Urun)
                .Where(s => request.OrderIds.Contains(s.Id))
                .ToListAsync();

            if (!siparisler.Any())
                return NotFound("Sipariş bulunamadı.");

            // Dosya yollarını tutacak liste
            var generatedFiles = new List<string>();

            string tempOutputFolder = Path.Combine(Path.GetTempPath(), "OtoEntegrePdf");
            var tumSiparisUrunleri = siparisler.SelectMany(s => s.SiparisUrunleri).ToList();

            // Tüketilen kredileri takip et (hata durumunda geri almak için)
            var consumedCredits = new List<(Guid kullaniciId, int count)>();

            try
            {
                // 1. Önce tüm siparişler için kredi kontrolü yap ve tüket
                Console.WriteLine($"Siparişler = > {siparisler}");
                foreach (var siparis in siparisler)
                {
                    var kullaniciId = siparis.KullaniciId;
                    if (!kullaniciId.HasValue)
                    {
                        // Kullanıcı ID yoksa kredileri geri al ve hata döndür
                        foreach (var (kId, count) in consumedCredits)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                await _kredilerService.RefundOneAsync(kId, referans: $"Toplu PDF hatası - sipariş {siparis.Id}");
                            }
                        }
                        return BadRequest(new
                        {
                            sent = false,
                            error = "Siparişte kullanıcı bilgisi bulunamadı."
                        });
                    }

                    var krediHarcanabildiMi = await _kredilerService.ConsumeOneAsync(kullaniciId.Value, referans: $"Toplu PDF - sipariş {siparis.Id}");

                    if (!krediHarcanabildiMi)
                    {
                        // Kredi yetersizse, tüketilen kredileri geri al
                        foreach (var (kId, count) in consumedCredits)
                        {
                            for (int i = 0; i < count; i++)
                            {
                                await _kredilerService.RefundOneAsync(kId, referans: $"Toplu PDF hatası - kredi yetersiz");
                            }
                        }
                        return BadRequest(new
                        {
                            sent = false,
                            error = "Krediniz tükendi. Yeni sipariş gönderemezsiniz."
                        });
                    }

                    // Tüketilen krediyi kaydet
                    var existingIndex = consumedCredits.FindIndex(c => c.kullaniciId == kullaniciId.Value);
                    if (existingIndex == -1)
                    {
                        consumedCredits.Add((kullaniciId.Value, 1));
                    }
                    else
                    {
                        var existing = consumedCredits[existingIndex];
                        consumedCredits[existingIndex] = (kullaniciId.Value, existing.count + 1);
                    }
                }

                // 2. Tüm krediler tüketildi, şimdi PDF'leri oluştur
                foreach (var siparis in siparisler)
                {
                    // Ürün verilerini servisin istediği formata çevir
                    var urunVerileri = await (from su in _dbContext.SiparisUrunleri
                                              join u in _dbContext.Urunler on su.Urun_Id equals u.Id
                                              join d in _dbContext.SiparisDosyalari
                                                  on u.Image equals d.Dosya_Url into dosyalar
                                              from d in dosyalar.DefaultIfEmpty()
                                              where su.Siparis_Id == siparis.Id && (d == null || d.Dosya_Turu == "image")
                                              select new
                                              {
                                                  UrunId = su.Urun_Id,
                                                  u.Ad,
                                                  u.ProductCode,
                                                  Image = u.Image ?? string.Empty,
                                                  su.Adet,
                                                  siparis.Renk,
                                                  siparis.Beden,
                                                  su.MerchantSku,
                                                  su.SiparisNotu,
                                                  u.UrunTedarikBarcode,
                                                  IsOtostickerProduct = _dbContext.Otosticker_Urunler
                                                                              .Any(o => o.ProductCode == u.ProductCode && o.KullaniciId == siparis.KullaniciId)

                                              }).Distinct().ToListAsync();

                    var pdfUrunler = urunVerileri
                          .GroupBy(u => u.UrunId)
                          .Select(g => g.First())
                          .Select(u => (
                              Ad: u.Ad,
                              Adet: u.Adet,
                              Renk: string.IsNullOrWhiteSpace(u.Renk) ? "-" : u.Renk,
                              Beden: string.IsNullOrWhiteSpace(u.Beden) ? "-" : u.Beden,
                              Barkod: u.UrunTedarikBarcode ?? "-",
                              StokKodu: u.ProductCode?.ToString() ?? "-",
                              MerchantSku: u.MerchantSku ?? "-",
                              SiparisNotu: _dbContext.SiparisUrunleri
                                  .Where(x => x.Siparis_Id == siparis.Id && x.Urun_Id == u.UrunId)
                                  .Select(x => x.SiparisNotu)
                                  .FirstOrDefault() ?? "-"
                          )).ToList();
                    // 📌 Ürünlerden sadece ilkini alıyoruz
                    var ilkUrun = urunVerileri.FirstOrDefault();


                    var cargoMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "Yurtiçi Kargo Marketplace", "yurtici_ornekBarkod (2).pdf" },
        { "YKMP", "yurtici_ornekBarkod (2).pdf" },
        { "Trendyol Express Marketplace", "tyexpress_ornekBarkod (2).pdf" },
        { "TEXMP", "tyexpress_ornekBarkod (2).pdf" },
        { "Sürat Kargo Marketplace", "surat_ornekBarkod (2).pdf" },
        { "SURATMP", "surat_ornekBarkod (2).pdf" },
        { "PTT Kargo Marketplace", "ptt_ornekBarkod (2).pdf" },
        { "PTTMP", "ptt_ornekBarkod (2).pdf" },
        { "Kolay Gelsin Marketplace", "kolaygelsin_ornekBarkod (2).pdf" },
        { "KOLAYGELSINMP", "kolaygelsin_ornekBarkod (2).pdf" },
        { "Horoz Kargo Marketplace", "horoz_ornekBarkod (2).pdf" },
        { "HOROZMP", "horoz_ornekBarkod (2).pdf" },
        { "DHL eCommerce Marketplace", "dhl_ornekBarkod (2).pdf" },
        { "DHLECOMMP", "dhl_ornekBarkod (2).pdf" },
        { "CEVA Marketplace", "ceva_ornekBarkod (2).pdf" },
        { "CEVAMP", "ceva_ornekBarkod (2).pdf" },
        { "Aras Kargo Marketplace", "aras_ornekBarkod (2).pdf" },
        { "ARASMP", "aras_ornekBarkod (2).pdf" },
        { "Borusan Lojistik Marketplace", "borusan_ornekBarkod (2).pdf" }
    };

                    string cargoFileName = cargoMap.TryGetValue(siparis.CargoProviderName, out var fileName)
                                        ? fileName
                                        : "ornekBarkod (2).pdf";
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
                    // 2. Her sipariş için PDF oluştur (Senin servisini kullanıyoruz)
                    string filePath = await _pdfLabelService.GenerateFromTemplateAsync(
                    templatePath: template,
                    outputDirectory: tempOutputFolder,
                    siparisNo: siparis.SiparisNumarasi,
                    adSoyad: siparis.MusteriAdSoyad,
                    adres: siparis.MusteriAdres, // Veya uygun adres alanı
                    kargoBarkod: "", // Varsa doldurun
                    kargoBarkodNumarasi: "", // Varsa doldurun
                    renk: "",
                    beden: "",
                    kargoTakipNumarasi: siparis.KargoTakipNumarasi, // Örnek alan
                    urunTrendyolKod: "",
                    urunler: pdfUrunler,
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
                    var urunNotlari = pdfUrunler
                                        .Where(u => !string.IsNullOrWhiteSpace(u.SiparisNotu) && u.SiparisNotu != "-")
                                        .Select(u => $"{u.Ad}: {u.SiparisNotu}")
                                        .ToList();
                    Console.WriteLine("Referans: " + $"Toplu PDF - sipariş {siparis.Id}".Length);

                    if (urunNotlari.Any())
                    {
                        var notMesaji = "📝 *Sipariş Notları:*\n" + string.Join("\n", urunNotlari);
                        await _telegramService.SendOrderMessageAsync(siparis.KullaniciId, notMesaji);
                    }
                    var pdfBytes = await System.IO.File.ReadAllBytesAsync(filePath);



                    generatedFiles.Add(filePath);
                }

                // 3. Oluşan PDF'leri birleştir
                var mergedStream = _pdfLabelService.MergePdfs(generatedFiles);

                // Eğer kullanıcı PDF'i Telegram’a da göndermek istiyorsa
                if (request.SendToTelegram)
                {
                    Console.WriteLine("Toplu PDF telegrama gönderiliyor.");

                    var pdfBytes = mergedStream.ToArray();

                    // ❗ sadece bir kullanıcı id alıyoruz
                    var kullaniciId = siparisler.First().KullaniciId;

                    if (!kullaniciId.HasValue)
                    {
                        Console.WriteLine("Kullanıcı bilgisi yok, OtoSticker siparişi atlandı.");
                    }
                    else
                    {

                        int toplamAdet = tumSiparisUrunleri.Sum(su => su.Adet);
                        var messageBuilder = new StringBuilder();


                        // B. Bir siparişten herhangi bir ürün resmini bul (SiparisUrunleri'ni kullanabiliriz)
                        var ilkUrunResmi = tumSiparisUrunleri
                            .FirstOrDefault(su => su.Urun != null && !string.IsNullOrWhiteSpace(su.Urun.Image))?.Urun?.Image;

                        var imageUrl = !string.IsNullOrWhiteSpace(ilkUrunResmi)
                            ? ilkUrunResmi
                            : "https://dummyimage.com/600x400/cccccc/000000&text=Sipariş+Resmi";
                        Console.WriteLine("Image URL: " + imageUrl.Length);

                        // C. Ürün resmiyle birlikte ilk mesajı gönder (SADECE 1 KERE)
                        var result = await _telegramService.SendOrderMessageWithResultAsync(
                             kullaniciId.Value,
                             messageBuilder.ToString(),
                             imageUrl
                        );

                        // OTOSTİCKERDAN SATIN ALIM
                        var user = await _dbContext.Kullanicilar.FirstOrDefaultAsync(u => u.Id == kullaniciId.Value);
                        if (user == null)
                        {
                            Console.WriteLine("Kullanıcı bulunamadı.");
                        }
                        else
                        {
                            // E. PDF dosyasını gönder (SADECE 1 KERE)
                            await _telegramService.SendDocumentAsync("Toplu Sipariş PDF", pdfBytes, kullaniciId.Value, $"toplu_siparis_{siparisler.Count}_kisi.pdf");
                            var dealerListResponse = await _otostickerService.GetDealerListAsync();
                            var dealer = dealerListResponse?.Result?.List?
                                .FirstOrDefault(d => d.Email.Trim().Equals(user.Email.Trim(), StringComparison.OrdinalIgnoreCase));

                            Console.WriteLine($"OtoSticker bayi aranıyor: {user.Email}");
                            Console.WriteLine($"OtoSticker sipariş: {siparisler}");
                            if (dealer == null)
                            {
                                Console.WriteLine($"OtoSticker bayi bulunamadı: {user.Email}");
                            }
                            else
                            {
                                Console.WriteLine($"OtoSticker bayi bulundu: {user.Email}");

                                var groupedProducts = (from su in siparisler.SelectMany(s => s.SiparisUrunleri)
                                                       join u in _dbContext.Otosticker_Urunler
                                                           on su.Urun.ProductCode equals u.ProductCode
                                                       where su.Urun != null
                                                           && su.Siparis.KullaniciId == u.KullaniciId
                                                           && !string.IsNullOrWhiteSpace(u.UrunTedarikBarcode)
                                                       group new { su, u } by u.UrunTedarikBarcode into g
                                                       select new
                                                       {
                                                           Barcode = g.Key,
                                                           TotalQuantity = g.Sum(x => x.su.Adet)
                                                       })
                                    .ToList();

                                var otoProductsList = new List<object>();

                                foreach (var item in groupedProducts)
                                {
                                    var barcode = item.Barcode;

                                    if (string.IsNullOrWhiteSpace(barcode))
                                    {
                                        Console.WriteLine($"⚠️ Ürünün barkodu yok: Id={item.Barcode}");
                                        continue;
                                    }

                                    var otoProduct = await _otostickerService.GetProductByBarcodeAsync(barcode);


                                    if (otoProduct == null)
                                    {
                                        Console.WriteLine($"📌 OtoSticker ürün bulunamadı (barcode={barcode})");
                                        continue;
                                    }

                                    decimal otoPrice = otoProduct?.SalePrice ?? 0;

                                    otoProductsList.Add(new
                                    {
                                        id = otoProduct.ProductId,
                                        price = otoPrice,
                                        quantity = item.TotalQuantity,
                                        variant1 = ""
                                    });
                                }

                                if (otoProductsList.Any())
                                {
                                    var fastSaleRequest = new
                                    {
                                        customer = new
                                        {
                                            ID = user.Tedarik_Kullanici_Id,
                                            email = dealer.Email,
                                            name = dealer.Name,
                                            lastname = dealer.Lastname,
                                            code = dealer.Code,
                                            title = dealer.Title,
                                            group = dealer.Group,
                                            status = dealer.Status,
                                            balance = dealer.Balance,
                                            discount = dealer.Discount,
                                            nationalId = dealer.NationalId,
                                            taxId = dealer.TaxId,
                                            taxBranch = dealer.TaxBranch,
                                            phone = user.Telefon,
                                            city = user.Sehir,
                                            district = user.Ilce,
                                            address = user.Adres
                                        },
                                        order = new
                                        {
                                            date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                            paymentType = 10,
                                            status = 1,
                                            note = ""
                                        },
                                        products = otoProductsList
                                    };

                                    var orderListJson = await _otostickerService.GetOrderListAsync(dealer.Id);

                                    if (orderListJson != null)
                                    {
                                        try
                                        {
                                            var list = orderListJson.RootElement
                                                .GetProperty("result")
                                                .GetProperty("list");

                                            if (list.GetArrayLength() > 0)
                                            {
                                                var lastOrder = list[0];
                                                var otoOrderId = lastOrder.TryGetProperty("id", out var idProp)
         ? idProp.GetInt32().ToString()
         : "(id bulunamadı)";
                                                var otoOrderNo = lastOrder.TryGetProperty("code", out var codeProp)
                                                    ? codeProp.GetString()
                                                    : "(code bulunamadı)";

                                                var message = $"📦 *OtoSticker Sipariş Numarası: {otoOrderNo}";
                                                await _telegramService.SendOrderMessageAsync(user.Id, message);

                                            }
                                            else
                                            {
                                                await _telegramService.SendOrderMessageAsync(user.Id, "⚠️ OtoSticker sipariş listesi boş döndü.");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine("Sipariş listesi parse hatası: " + ex.Message);
                                            await _telegramService.SendOrderMessageAsync(user.Id, $"⚠️ Sipariş listesi okunamadı: {ex.Message}");
                                        }
                                    }
                                    var otoResult = await _otostickerService.CreateFastSaleAsync(fastSaleRequest, dealer, kullaniciId.Value);

                                    Console.WriteLine("📦 OtoSticker toplu sipariş oluşturuldu.");
                                }
                                else
                                {
                                    Console.WriteLine("OtoSticker’a gönderilecek ürün bulunamadı.");
                                }

                            }
                        }


                        await _telegramService.SendOrderMessageAsync(kullaniciId, "----------------------------------");

                    }

                    // TelegramSent işaretle
                    foreach (var siparis in siparisler)
                    {
                        siparis.TelegramSent = true;
                        _dbContext.Siparisler.Update(siparis);

                    }
                    var changes = _dbContext.ChangeTracker.Entries()
                        .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
                        .ToList();

                    foreach (var entry in changes)
                    {
                        foreach (var prop in entry.Properties)
                        {
                            if (prop.Metadata.ClrType == typeof(string))
                            {
                                var val = prop.CurrentValue?.ToString();
                                if (val != null && val.Length > 255)
                                {
                                    Console.WriteLine($"255'i aşan değer -> {entry.Entity.GetType().Name}.{prop.Metadata.Name} : {val.Length}");
                                }
                            }
                        }
                    }

                    await _dbContext.SaveChangesAsync();
                }


                // 4. Dosyayı döndür
                return File(mergedStream.ToArray(), "application/pdf", $"Toplu_Siparis_{DateTime.Now:yyyyMMddHHmm}.pdf");
            }
            catch (Exception ex)
            {
                // Hata durumunda tüketilen kredileri geri al
                foreach (var (kullaniciId, count) in consumedCredits)
                {
                    for (int i = 0; i < count; i++)
                    {
                        try
                        {
                            await _kredilerService.RefundOneAsync(kullaniciId, referans: $"Toplu PDF hatası - {ex.Message}");
                        }
                        catch (Exception refundEx)
                        {
                            Console.WriteLine($"Kredi geri alma hatası: {refundEx.Message}");
                        }
                    }
                }

                return BadRequest(new
                {
                    sent = false,
                    error = $"Hata oluştu: {ex.Message}"
                });
            }
            finally
            {
                // 5. Temizlik: Geçici dosyaları sil
                foreach (var file in generatedFiles)
                {
                    if (System.IO.File.Exists(file))
                        System.IO.File.Delete(file);
                }
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
