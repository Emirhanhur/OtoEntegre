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

namespace OtoEntegre.Api.Services
{
    public class DealerService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppDbContext _dbContext;

        public DealerService(IHttpClientFactory httpClientFactory, AppDbContext dbContext)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        private static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return string.Empty;
            phone = phone.Replace("+", "").Replace(" ", "").Replace("-", "");
            if (phone.StartsWith("90") && phone.Length > 10) phone = phone.Substring(2);
            if (phone.StartsWith("0")) phone = phone.Substring(1);
            return phone;
        }

        public async Task<int> FetchAndSaveDealers()
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apiKey", "eba11f4b-2b1f-444e-a777-ec6872c95601");
            client.DefaultRequestHeaders.Add("apiSecret", "e81498b3f3865c5bd2a5b2a6ab69028e5RbSwX32Q3CMlAwUDg==");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            int pageStart = 0;
            int pageSize = 10; // API en fazla 100 veriyor
            List<DealerDto> allDealers = new();

            while (true)
            {
                string url = $"https://www.otosticker.com.tr/api/v2/dealer/lists?pageStart={pageStart}&pageSize={pageSize}&orderBy=id&sort=desc";

                string jsonString = await client.GetStringAsync(url);
                var apiResponse = JsonSerializer.Deserialize<DealerApiResponse>(jsonString, options);

                var dealers = apiResponse?.Result?.List;
                if (dealers == null || dealers.Count == 0)
                    break;

                allDealers.AddRange(dealers);

                // Gelen kayıt sayısı pageSize'dan küçükse son sayfadayız
                if (dealers.Count < pageSize)
                    break;

                pageStart += pageSize;
            }

            // DB’ye kaydetme kısmı (senin mevcut kodunla aynı)
            var existingDealers = await _dbContext.Dealers.AsTracking().ToListAsync();
            var toAdd = new List<Dealer>();
            int added = 0;

            foreach (var d in allDealers)
            {
                var match = existingDealers.FirstOrDefault(e => e.Id == d.Id);
                if (match == null)
                {
                    toAdd.Add(new Dealer
                    {
                        Id = d.Id,
                        Name = d.Name ?? string.Empty,
                        Email = d.Email ?? string.Empty,
                        Phone = NormalizePhone(d.Phone)
                    });
                    added++;
                }
            }

            if (toAdd.Count > 0)
                _dbContext.Dealers.AddRange(toAdd);

            await _dbContext.SaveChangesAsync();
            return added;
        }

        // JSON wrapper modeli
        public class DealerApiWrapper
        {
            public int Total { get; set; }
            public DealerResult? Result { get; set; }
        }
        public async Task<int> FetchAndSaveOrders()
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("apiKey", "eba11f4b-2b1f-444e-a777-ec6872c95601");
            client.DefaultRequestHeaders.Add("apiSecret", "e81498b3f3865c5bd2a5b2a6ab69028e5RbSwX32Q3CMlAwUDg==");
            Console.WriteLine("sürekli çalışan yer burası = DealerService 107");
            string url = "https://www.otosticker.com.tr/api/v2/order/lists?pageStart=0&pageSize=100000&orderBy=id&sort=desc";

            string jsonString;
            try
            {
                jsonString = await client.GetStringAsync(url);
            }
            catch
            {
                return 0;
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var apiResponse = JsonSerializer.Deserialize<OrderApiResponse>(jsonString, options);
            var orders = apiResponse?.Result?.List;
            if (orders == null || orders.Count == 0) return 0;

            var existingOrders = await _dbContext.Siparisler.AsTracking().ToListAsync();
            var toAdd = new List<Siparisler>();
            int added = 0;

            foreach (var o in orders)
            {
                var match = existingOrders.FirstOrDefault(e => e.ApiOrderId == o.Id);
                if (match == null)
                {
                    toAdd.Add(new Siparisler
                    {
                        Id = Guid.NewGuid(),
                        DealerId = o.DealerId, // DealerId mapping API’den gelirse ekle
                        ApiOrderId = o.Id,
                        SiparisNumarasi = o.OrderNumber,
                        ToplamTutar = o.Total,
                        KargoUcreti = o.ShippingFee,
                        OdemeDurumu = o.PaymentStatus,
                        Kod = o.Code,
                        Durum = o.Status.ToString(),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                    added++;
                }
            }

            if (toAdd.Count > 0)
                _dbContext.Siparisler.AddRange(toAdd);

            await _dbContext.SaveChangesAsync();
            return added;
        }
    }
}
