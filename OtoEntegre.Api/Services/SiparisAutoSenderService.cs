using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OtoEntegre.Api.Data;
using OtoEntegre.Api.DTOs;
using OtoEntegre.Api.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OtoEntegre.Api.Services
{
    public class SiparisAutoSenderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public SiparisAutoSenderService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var otosticker = scope.ServiceProvider.GetRequiredService<OtostickerService>();

                    // Tedarik gönderilmemiş siparişleri al
                    var yeniSiparisler = await db.Siparisler
                        .Where(s => !s.TedarikSent)
                        .ToListAsync(stoppingToken);

                    foreach (var siparis in yeniSiparisler)
                    {
                        try
                        {
                            // Otosticker DTO oluştur
                            var orderDto = new OtostickerFastSaleOrderDto
                            {
                                Customer = new OtostickerCustomerDto
                                {
                                    Name = "Büşra",
                                    Lastname = "Çap",
                                    Email = "busracapp@gmail.com",
                                    Phone = "05342146124",
                                    City = "İstanbul",
                                    Distict = "Sarıyer",
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
                                Products = new System.Collections.Generic.List<OtostickerProductItemDto>
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

                            // Gönder
                            await otosticker.CreateFastSaleOrderAsync(orderDto);

                            // Başarılı ise DB güncelle
                            siparis.TedarikSent = true;
                            db.Siparisler.Update(siparis);
                        }
                        catch (Exception ex)
                        {
                            // Logla ama döngüye devam et
                            Console.WriteLine($"Tedarik gönderimi hatası: {ex.Message}");
                        }
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Otomatik gönderim döngü hatası: {ex.Message}");
                }

                // 1 dakika bekle, sonra tekrar kontrol et
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}
