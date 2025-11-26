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
                _logger.LogInformation($"{turkeyTime} OrderSyncBackgroundService started.");

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var trendyolService = scope.ServiceProvider.GetRequiredService<TrendyolService>();
                var telegramService = scope.ServiceProvider.GetRequiredService<TelegramService>();
                var kredilerService = scope.ServiceProvider.GetRequiredService<OtoEntegre.Api.Services.KredilerService>();
                var repo = scope.ServiceProvider.GetRequiredService<IGenericRepository<Siparisler>>();
                var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
                var pdfService = scope.ServiceProvider.GetRequiredService<PdfLabelService>();


                var users = await dbContext.Kullanicilar.ToListAsync(stoppingToken);

                var endDate = turkeyTime;
                var startDate = endDate.AddHours(-120);

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

                        var orders = await trendyolService.GetOrdersAsync(
                            supplierId,
                            apiKey,
                            apiSecret,
                            startDate,
                            endDate,
                            status: null // veya "all"
                        );
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


                            bool isSplitPackage = order.CreatedBy?.Equals("split", StringComparison.OrdinalIgnoreCase) == true;

                            // Her Trendyol paketi benzersiz ID'ye sahiptir -> kontrolü ID bazlı yapıyoruz
                            var existingOrder = await dbContext.Siparisler
                                .FirstOrDefaultAsync(s =>
                                    s.TrendyolSiparisId == order.Id &&
                                    s.KullaniciId == user.Id,
                                    stoppingToken);


                            if (existingOrder != null)
                            {

                                // ✅ Durum kontrolü ve güncelleme
                                // 📦 En son durum packageHistories’den alınır
                                // --- Durum kontrolü ve güncelleme (iyileştirilmiş) ---
                                var lastHistory = order.PackageHistories?
     .OrderByDescending(p => p.CreatedDate)
     .FirstOrDefault();


                                var yeniDurumRaw = lastHistory?.Status ?? order.Status ?? string.Empty;
                                var yeniDurumNormalized = yeniDurumRaw?.Trim();
                                var mevcutDurumRaw = existingOrder.Durum ?? string.Empty;
                                var mevcutDurumNormalized = mevcutDurumRaw?.Trim();

                                // Eğer durum değişmişse güncelle
                                if (!string.Equals(mevcutDurumNormalized, yeniDurumNormalized, StringComparison.OrdinalIgnoreCase))
                                {
                                    _logger.LogInformation($"[SYNC] Güncelle: {order.OrderNumber} | {mevcutDurumRaw} -> {yeniDurumNormalized}");
                                    existingOrder.Durum = yeniDurumNormalized ?? mevcutDurumRaw;
                                    existingOrder.UpdatedAt = DateTime.UtcNow;

                                    var entry = dbContext.Entry(existingOrder);
                                    entry.Property(e => e.Durum).IsModified = true;
                                    entry.Property(e => e.UpdatedAt).IsModified = true;

                                    await dbContext.SaveChangesAsync(stoppingToken);
                                    _logger.LogInformation($"[SYNC] Sipariş durumu güncellendi: {order.OrderNumber} | {mevcutDurumRaw} -> {yeniDurumNormalized}");
                                }
                                else
                                {
                                    _logger.LogInformation($"[SYNC] Durum değişmedi: {order.OrderNumber} mevcut ile yeni eşit.");
                                }



                                // Tekrar normalizasyon karşılaştırma (case-insensitive, trim)



                            }
                            // if (order.AgreedDeliveryDate > 0)
                            // {
                            //     var nonShippedStatuses = new[] { "CREATED", "PICKING", "AWAITING", "UNSUPPLIED", "UNPACKED" };
                            //     if (!nonShippedStatuses.Contains(order.Status?.ToUpperInvariant()))
                            //     {
                            //         var agreedDate = DateTimeOffset
                            //             .FromUnixTimeMilliseconds(order.AgreedDeliveryDate)
                            //             .ToLocalTime() // Trendyol timestamp Türkiye saatine çekilir
                            //             .DateTime;

                            //         var now = DateTime.Now;
                            //         var gecenGun = Math.Ceiling((now - agreedDate).TotalDays);

                            //         if (gecenGun >= 1 && existingOrder?.DelayNotified != true)
                            //         {
                            //             try
                            //             {
                            //                 string mesaj = $"⚠️ Sipariş numarası *{order.OrderNumber}* olan sipariş *{gecenGun} gün* gecikmiştir.";
                            //                 await telegramService.SendOrderMessageAsync(
                            //                     existingOrder.KullaniciId ?? Guid.Empty,
                            //                     mesaj,
                            //                     null
                            //                 );

                            //                 existingOrder.DelayNotified = true;
                            //                 await dbContext.SaveChangesAsync(stoppingToken);
                            //                 _logger.LogInformation($"[DELAY] Sipariş gecikme bildirimi gönderildi: {order.OrderNumber} ({gecenGun} gün)");
                            //             }
                            //             catch (Exception ex)
                            //             {
                            //                 _logger.LogError(ex, $"Telegram gecikme bildirimi gönderilirken hata: {order.OrderNumber}");
                            //             }
                            //         }
                            //     }
                            // }

                            // ❌ Eğer sipariş iptal olmuşsa ve daha önce telegramda gönderilmişse kullanıcıya iptal bilgisi gönder
                            if (order.Status == "Cancelled" && existingOrder != null)
                            {
                                // Eğer zaten iptal bildirimi yapılmışsa tekrar bildirim gönderme
                                if (existingOrder.CancelledNotified == true)
                                    continue;

                                // Eğer sipariş daha önce telegrama gönderildiyse bildir
                                if (existingOrder.TelegramSent == true && existingOrder.KullaniciId.HasValue)
                                {
                                    // await telegramService.SendOrderMessageAsync(
                                    //     existingOrder.KullaniciId.Value,
                                    //     $"⚠️⚠️⚠️⚠️⚠️⚠️ *Sipariş İptal Edildi*\n📦 Sipariş No: {order.OrderNumber} \n Müşteri: {existingOrder.MusteriAdSoyad}",
                                    //     null
                                    // );

                                    // Tekrar bildirmemek için flag'i işaretle
                                    existingOrder.CancelledNotified = true;
                                    await dbContext.SaveChangesAsync();
                                }

                                continue; // Bu siparişi tekrar işleme
                            }

                            // if (order.Status == "Cancelled")
                            // {
                            //     _logger.LogInformation("Cancelled sipariş atlandı: {OrderNumber}", order.OrderNumber);
                            //     continue; // sonraki siparişe geç
                            // }
                            bool exists = await dbContext.Siparisler
    .AnyAsync(s =>
        s.TrendyolSiparisId == order.Id && // ✅ yeni kontrol
        s.KullaniciId == user.Id,
        stoppingToken);

                            // 🔍 Veri tutarlılığı kontrolü
                            var kullanici = await dbContext.Kullanicilar.FirstOrDefaultAsync(k => k.Id == user.Id);
                            if (kullanici == null)
                            {
                                _logger.LogWarning("Kullanıcı bulunamadı: {UserId}", user.Id);
                                continue;
                            }

                            // entegrasyon ilişkilerini doğrula
                            if (entegrasyon.Kullanici_Id != kullanici.Id ||
                                entegrasyon.Kullanici_Adi != kullanici.Ad)
                            {
                                _logger.LogWarning(
                                    "⚠️ Entegrasyon - Kullanıcı uyumsuzluğu! EntegrasyonId={EntegrasyonId}, KullaniciId={KullaniciId}, EntegrasyonKullaniciAdi={EntegrasyonAdi}, GercekKullaniciAdi={KullaniciAdi}",
                                    entegrasyon.Id, kullanici.Id, entegrasyon.Kullanici_Adi, kullanici.Ad
                                );
                                continue;
                            }

                            if (!exists)
                            {
                                if (entegrasyon == null)
                                {
                                    _logger.LogWarning("Entegrasyon null geldi, atlanıyor for user {UserId}", user.Id);
                                    continue;
                                }


                                bool anaSiparisVar = await dbContext.Siparisler.AnyAsync(
                                s => s.SiparisNumarasi == order.OrderNumber && s.KullaniciId == user.Id,
                                stoppingToken
                            );

                                if (!isSplitPackage && anaSiparisVar)
                                {
                                    _logger.LogInformation($"Aynı orderNumber {order.OrderNumber} zaten var, split değil, atlandı.");
                                    continue;
                                }



                                var siparis = TrendyolMapping.MapToSiparis(order, entegrasyon);
                                siparis.KullaniciId = entegrasyon.Kullanici_Id; // garantiye al
                                siparis.EntegrasyonId = entegrasyon.Id;

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

                                        Toplam_Fiyat = line.Price * line.Quantity,
                                        MerchantSku = line.MerchantSku

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

                                // Decrement credit for this order immediately (allow negative balances)
                                if (entegrasyon.Kullanici_Id.HasValue)
                                {
                                    try
                                    {
                                        var consumed = await kredilerService.ConsumeOneAsync(entegrasyon.Kullanici_Id.Value);
                                        Console.WriteLine($"[Background] Kullanici {entegrasyon.Kullanici_Id.Value} için kredi tüketildi on order arrival: {consumed}");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"[Background] Kredi tüketimi sırasında hata: {ex.Message}");
                                    }
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
