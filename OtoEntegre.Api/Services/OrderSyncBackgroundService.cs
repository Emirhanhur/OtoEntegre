using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OtoEntegre.Api.Data;
using OtoEntegre.Api.Services;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Text;              // Encoding.UTF8 için
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OtoEntegre.Api.Entities;
using OtoEntegre.Api.Repositories;
using OtoEntegre.Api.DTOs;
using System.IO;
public class OrderSyncBackgroundService : BackgroundService
{

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderSyncBackgroundService> _logger;

    public OrderSyncBackgroundService(IServiceProvider serviceProvider, ILogger<OrderSyncBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }


    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var turkeyTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"));
                Console.WriteLine($"OrderSync çalıştı: {turkeyTime}");

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var trendyolService = scope.ServiceProvider.GetRequiredService<TrendyolService>();
                var telegramService = scope.ServiceProvider.GetRequiredService<TelegramService>();
                var repo = scope.ServiceProvider.GetRequiredService<IGenericRepository<Siparisler>>();
                var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
                var pdfService = scope.ServiceProvider.GetRequiredService<PdfLabelService>();


                var users = await dbContext.Kullanicilar.ToListAsync(stoppingToken);

                var endDate = turkeyTime;
                var startDate = endDate.AddHours(-5); // Son 1 saat
                Console.WriteLine($"endDate = {endDate}, startDate = {startDate}");

                foreach (var user in users)
                {
                    var entegrasyonlar = await dbContext.Entegrasyonlar
                        .Where(e => e.Kullanici_Id == user.Id)
                        .ToListAsync(stoppingToken);

                    foreach (var entegrasyon in entegrasyonlar)
                    {
                        long supplierId = entegrasyon.Seller_Id ?? 0;
                        string apiKey = entegrasyon.Api_Key ?? string.Empty;
                        string apiSecret = entegrasyon.Api_Secret ?? string.Empty;
                        _logger.LogInformation("UserId {UserId} SupplierId {SupplierId} için sipariş yok.", user.Id, supplierId);

                        var orders = await trendyolService.GetOrdersAsync(supplierId, apiKey, apiSecret, startDate, endDate);
                        if (orders == null || !orders.Any())
                        {
                            continue;
                        }

                        var filteredOrders = orders
                            .Where(o =>
                            {
                                var orderUtc = DateTimeOffset.FromUnixTimeMilliseconds(o.OrderDate).UtcDateTime;
                                return orderUtc >= startDate && orderUtc <= endDate;
                            })
                            .ToList();

                        foreach (var order in filteredOrders)
                        {
                            bool exists = await dbContext.Siparisler.AnyAsync(s => s.SiparisNumarasi == order.OrderNumber, stoppingToken);
                            if (!exists)
                            {
                                var siparis = TrendyolMapping.MapToSiparis(order, entegrasyon);
                                siparis.GeldigiYer = 1;
                                if (siparis == null)
                                {
                                    _logger.LogWarning("Sipariş Maplenemedi: {OrderNumber}", order.OrderNumber);
                                    continue;
                                }

                                dbContext.Siparisler.Add(siparis);
                                await dbContext.SaveChangesAsync(stoppingToken); // Önce DB kaydet
                                var productImages = new List<string>();

                                foreach (var line in order.Lines)
                                {
                                    // Ürün tablosundan mevcut ürünü bul
                                    var urun = await dbContext.Urunler
                                        .FirstOrDefaultAsync(u => u.ProductCode == line.ProductCode);

                                    if (urun == null)
                                    {
                                        // Ürün yoksa önce Urunler tablosuna ekle

                                        urun = new Urunler
                                        {
                                            Id = Guid.NewGuid(),
                                            UrunTedarikBarcode = line.Barcode,
                                            Ad = line.ProductName ?? "-",
                                            ProductCode = line.ProductCode,
                                        };
                                        dbContext.Urunler.Add(urun);
                                        await dbContext.SaveChangesAsync(stoppingToken);
                                    }

                                    // SiparisUrunleri kaydı oluştur
                                    var siparisUrun = new SiparisUrunleri
                                    {
                                        Id = Guid.NewGuid(),
                                        Siparis_Id = siparis.Id,
                                        Urun_Id = urun.Id,
                                        Adet = line.Quantity,
                                        Birim_Fiyat = line.Price,

                                        Toplam_Fiyat = line.Price * line.Quantity

                                    };
                                    dbContext.SiparisUrunleri.Add(siparisUrun);


                                    var products = await trendyolService.GetProductsByBarcodesAsync(supplierId, apiKey, apiSecret, new List<string> { line.Barcode });
                                    var productData = products.FirstOrDefault(p => p.ProductCode == line.ProductCode);
                                    var imageUrl = productData?.Images.FirstOrDefault()?.Url;

                                    // SiparisDosyalari ekleme (image)
                                    if (!string.IsNullOrWhiteSpace(imageUrl))
                                    {
                                        var dosya = new SiparisDosyalari
                                        {
                                            Id = Guid.NewGuid(),
                                            Siparis_Id = siparis.Id,
                                            Dosya_Turu = "image",
                                            Dosya_Url = urun!.Image,
                                            Created_At = DateTime.UtcNow
                                        };
                                        dbContext.SiparisDosyalari.Add(dosya);
                                        urun.Image = imageUrl;
                                        productImages.Add(imageUrl);
                                    }
                                }

                                // Son olarak DB'ye kaydet
                                await dbContext.SaveChangesAsync(stoppingToken);
                                // Telegram gönderimi
                                string message = $"{order.OrderNumber}\n";
                                string? firstImageUrl = productImages.FirstOrDefault();

                                try
                                {
                                    var barcodes = order.Lines
                                        .Where(l => !string.IsNullOrWhiteSpace(l.Barcode))
                                        .Select(l => l.Barcode)
                                        .Distinct()
                                        .ToList();

                                    if (barcodes.Any())
                                    {

                                        Console.WriteLine($"[{DateTime.Now}] Trendyol ürünleri getiriliyor, barkod sayısı: {barcodes.Count}");

                                        const int MaxBarcodeCount = 250;
                                        var allProducts = new List<TrendyolProductDto>();

                                        foreach (var batch in barcodes.Chunk(MaxBarcodeCount))
                                        {
                                            var products = await trendyolService.GetProductsByBarcodesAsync(
                                                supplierId, apiKey, apiSecret, batch.ToList()
                                            );
                                            allProducts.AddRange(products);
                                        }

                                        foreach (var product in allProducts)
                                        {
                                            var imageUrl = product.Images.FirstOrDefault()?.Url;

                                            // DB kaydı ekleme veya güncelleme isteğe bağlı kalabilir
                                            var existingProduct = await dbContext.Urunler
                                                .FirstOrDefaultAsync(p => p.ProductCode == product.ProductCode);

                                            if (!string.IsNullOrWhiteSpace(imageUrl))
                                            {
                                                productImages.Add(imageUrl);
#pragma warning disable CS8601 // Possible null reference assignment.
                                                existingProduct = new Urunler
                                                {
                                                    Id = Guid.NewGuid(),
                                                    UrunTedarikBarcode = product.Barcode,
                                                    Ad = product.Title,
                                                    Kategori = product.CategoryName ?? "",
                                                    ProductCode = product.ProductCode,
                                                    Image = imageUrl
                                                };
#pragma warning restore CS8601 // Possible null reference assignment.
                                                dbContext.Urunler.Add(existingProduct);
                                            }
                                            else
                                            {
#pragma warning disable CS8601 // Possible null reference assignment.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                                                existingProduct.Image = imageUrl; // her seferinde güncelle
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8601 // Possible null reference assignment.
                                            }

                                            // SiparisDosyalari ekleme, her zaman ekle (DB’de varsa da tekrar ekle)
                                            if (!string.IsNullOrWhiteSpace(imageUrl))
                                            {
                                                var dosya = new SiparisDosyalari
                                                {
                                                    Id = Guid.NewGuid(),
                                                    Siparis_Id = siparis.Id,
                                                    Dosya_Turu = "image",
                                                    Dosya_Url = imageUrl,
                                                    Created_At = DateTime.UtcNow
                                                };
                                                dbContext.Set<SiparisDosyalari>().Add(dosya);

                                                // Telegram için firstImageUrl her zaman güncellenir
                                                firstImageUrl ??= imageUrl;

                                            }
                                        }

                                        await dbContext.SaveChangesAsync();
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Trendyol ürün bilgisi çekilirken hata: {ex}");
                                }


                                // Telegram + PDF gönderimi
                                try
                                {
                                    bool telegramSent = false;
                                    int retries = 3;
                                    for (int attempt = 1; attempt <= retries; attempt++)
                                    {
                                        try
                                        {
                                            Guid? userId = siparis.KullaniciId;
                                            telegramSent = await telegramService.SendOrderMessageAsync(userId, message, firstImageUrl);
                                            break;
                                        }
                                        catch (HttpRequestException ex)
                                        {
                                            Console.WriteLine($"Telegram gönderim hatası, deneme {attempt}/{retries}: {ex.Message}");
                                            if (attempt == retries)
                                                throw;
                                            await Task.Delay(2000);
                                        }
                                    }

                                    // DB güncelle
                                    var existingSiparis = await repo.GetByIdAsync(siparis.Id);
                                    if (existingSiparis != null)
                                    {
                                        existingSiparis.TelegramSent = telegramSent;
                                        await repo.SaveAsync();
                                    }

                                    // PDF oluştur ve gönder
                                    Console.WriteLine("pdf oluşturma başladı");
                                    var pdfPath = Path.Combine(Directory.GetCurrentDirectory(), "labels", "ornekBarkod (2).pdf");
                                    if (System.IO.File.Exists(pdfPath))
                                    {
                                        int pdfRetries = 2;
                                        for (int attempt = 1; attempt <= pdfRetries; attempt++)
                                        {
                                            try
                                            {
                                                var filenames = new[] { "ornekBarkod (2).pdf" };
                                                var basePaths = new[]
                                                {
                        env.ContentRootPath,
                        Directory.GetCurrentDirectory(),
                        Path.GetFullPath(Path.Combine(env.ContentRootPath, "..")),
                        AppContext.BaseDirectory ?? env.ContentRootPath
                    };
                                                var candidates = basePaths
                                                    .SelectMany(p => filenames.Select(f => Path.Combine(p, "labels", f)))
                                                    .ToList();
                                                var template = candidates.FirstOrDefault(System.IO.File.Exists) ?? string.Empty;
                                                if (string.IsNullOrEmpty(template))
                                                    throw new FileNotFoundException("PDF şablonu bulunamadı", string.Join(", ", filenames));

                                                var outputDir = Path.Combine(env.ContentRootPath, "labels");
                                                string siparisNo = siparis.SiparisNumarasi ?? "-";
                                                string adSoyad = siparis.MusteriAdSoyad;
                                                string adres = siparis.MusteriAdres;
                                                string kargoBarkod = siparis.PlatformUrunKod ?? "-";
                                                string kargoBarkodNumarasi = siparis.Kod ?? siparis.SiparisNumarasi ?? "-";
                                                string kargoTakipNumarasi = siparis.KargoTakipNumarasi ?? "-";
                                                string paketNumarasi = siparis.PaketNumarasi ?? "-";
                                                string urunTrendyolKod = siparis.UrunTrendyolKod ?? "-";

                                                var urunDetaylari = order.Lines.Select(line => (
                                                    Title: line.ProductName ?? "-",
                                                    ImageUrl: firstImageUrl,
                                                    Price: line.Price,
                                                    Barkod: line.Barcode ?? "-",
                                                    StokKodu: line.ProductCode.ToString()
                                                )).ToList();
                                                Console.WriteLine($"firstImageUrl ====={firstImageUrl}");

                                                // PDF için ürün detaylarını oluştur
                                                var urunler = order.Lines.Select(line => (
                                                    Ad: line.ProductName ?? "-",
                                                    Adet: line.Quantity,   // <-- artık SiparisUrunleri tablosundaki gerçek adet kullanılıyor
                                                    Renk: string.IsNullOrWhiteSpace(siparis.Renk) ? "-" : siparis.Renk,
                                                    Beden: string.IsNullOrWhiteSpace(siparis.Beden) ? "-" : siparis.Beden,
                                                    Barkod: line.Barcode ?? "-",
                                                    StokKodu: line.ProductCode.ToString() ?? "-"
                                                )).ToList();

                                                // PDF oluşturma çağrısı
                                                var generatedPdf = await pdfService.GenerateFromTemplateAsync(
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
                                                    urunler,  // <-- artık doğru adetle gönderiliyor
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


                                                var caption = $"{siparis.MusteriAdSoyad}";
                                                await telegramService.SendDocumentAsync(caption, generatedPdf, siparis.KullaniciId);
                                                await telegramService.SendOrderMessageAsync(siparis.KullaniciId, "-----------------");
                                                break;
                                            }
                                            catch (HttpRequestException ex)
                                            {
                                                Console.WriteLine($"PDF Telegram gönderim hatası, deneme {attempt}/{pdfRetries}: {ex.Message}");
                                                if (attempt == pdfRetries)
                                                    throw;
                                                await Task.Delay(1000);
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Telegram işlemlerinde hata: {ex}");
                                }

                            }
                        }

                    }
                }

                await dbContext.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Order Sync Hatası: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Her 5 dakikada bir çalışacak
        }
    }
}
