using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OtoEntegre.Api.Data;
using OtoEntegre.Api.DTOs;
using OtoEntegre.Api.Entities;
using OtoEntegre.Api.Repositories;

namespace OtoEntegre.Api.Services
{
    public class TedarikService
    {
        private readonly TrendyolService _trendyolService;
        private readonly IGenericRepository<Siparisler> _siparisRepo;
        private readonly IGenericRepository<Urunler> _urunRepo;
        private readonly IGenericRepository<SiparisUrunleri> _siparisUrunRepo;
        private readonly IGenericRepository<Entegrasyonlar> _entegrasyonRepo;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppDbContext _dbContext;

        public TedarikService(
            TrendyolService trendyolService,
            IGenericRepository<Siparisler> siparisRepo,
            IGenericRepository<Urunler> urunRepo,
            IGenericRepository<SiparisUrunleri> siparisUrunRepo,
            IGenericRepository<Entegrasyonlar> entegrasyonRepo,
            IHttpClientFactory httpClientFactory,
            AppDbContext dbContext)
        {
            _trendyolService = trendyolService;
            _siparisRepo = siparisRepo;
            _urunRepo = urunRepo;
            _siparisUrunRepo = siparisUrunRepo;
            _entegrasyonRepo = entegrasyonRepo;
            _httpClientFactory = httpClientFactory;
            _dbContext = dbContext;
        }

        public async Task<bool> SiparisiTedarikSitesineGonder(string orderCode)
        {
            long supplierId = 1120553;

            var siparis = (await _siparisRepo.GetAllAsync()).FirstOrDefault(x => x.Kod == orderCode);
            if (siparis == null)
                throw new Exception("Sipariş bulunamadı.");

            var entegrasyon = (await _entegrasyonRepo.GetAllAsync())
                .FirstOrDefault(e => e.Kullanici_Id == siparis.KullaniciId);

            if (entegrasyon == null)
                throw new Exception("Kullanıcının entegrasyon bilgisi yok.");

            string apiKey = entegrasyon.Api_Key ?? "";
            string apiSecret = entegrasyon.Api_Secret ?? "";

            var trendyolOrder = await _trendyolService.GetOrderByCodeAsync(orderCode, supplierId, apiKey, apiSecret);
            if (trendyolOrder == null)
                throw new Exception("Trendyol siparişi bulunamadı.");

            using var transaction = await _dbContext.Database.BeginTransactionAsync();

            var siparisUrunleriList = new List<SiparisUrunleri>();
            var productsPayload = new List<object>();

            foreach (var line in trendyolOrder.Lines)
            {
                var urun = (await _urunRepo.GetAllAsync())
                    .FirstOrDefault(u => u.UrunTedarikBarcode == line.Barcode);

                if (urun == null)
                    continue;

                var siparisUrun = new SiparisUrunleri
                {
                    Id = Guid.NewGuid(),
                    Siparis_Id = siparis.Id,
                    Urun_Id = urun.Id,
                    Adet = line.Quantity,
                    Birim_Fiyat = line.Price,
                    Toplam_Fiyat = line.Price * line.Quantity
                };

                siparisUrunleriList.Add(siparisUrun);

                productsPayload.Add(new
                {
                    id = urun.OtostickerProductId,
                    price = 99.99,
                    quantity = line.Quantity
                });
            }

            await _siparisUrunRepo.AddRangeAsync(siparisUrunleriList);
            await _siparisUrunRepo.SaveAsync();

            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("apiKey", "eba11f4b-2b1f-444e-a777-ec6872c95601");
            httpClient.DefaultRequestHeaders.Add("apiSecret", "e81498b3f3865c5bd2a5b2a6ab69028e5RbSwX32Q3CMlAwUDg==");

            var payload = new
            {
                customer = new
                {
                    name = "Emirhan11111",
                    lastname = "Hür",
                    email = "emirhanhur53@gmail.com",
                    phone = "+905342146124",
                    city = "istanbul",
                    district = "sarıyer",
                    address = "test adres",
                    taxId = "111",
                    taxBranch = "sss",
                    nationalId = "222"
                },
                order = new
                {
                    date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    paymentType = 2,
                    status = 1,
                    note = $"Sipariş #{siparis.SiparisNumarasi}",
                    amount = siparisUrunleriList.Sum(x => x.Toplam_Fiyat) // veya siparis.ToplamTutar
                },

                products = productsPayload
            };

            var response = await httpClient.PostAsJsonAsync("https://www.otosticker.com.tr/api/v2/order/fastSale", payload);
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                await transaction.RollbackAsync();
                throw new Exception($"Otosticker API hatası: {content}");
            }

            siparis.TedarikSent = true;
            _siparisRepo.Update(siparis);
            await _siparisRepo.SaveAsync();

            await transaction.CommitAsync();

            return true;
        }
    }


}