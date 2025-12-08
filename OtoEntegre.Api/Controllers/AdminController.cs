using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using OtoEntegre.Api.Data;
using OtoEntegre.Api.DTOs;

namespace OtoEntegre.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AdminController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet("sales-stats")]
        public async Task<IActionResult> GetSalesStats()
        {
            // allow only Admin role
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role")?.Value;
            if (role != "Admin") return Forbid();

            var now = DateTime.UtcNow.AddHours(3); // Turkish timezone (UTC+3)

            // Helper function to calculate sales for a given period
            async Task<(int count, decimal amount)> GetSalesForPeriod(DateTime startDate)
            {
                var count = await _db.SiparisUrunleri
                    .Where(su => su.Siparis.CreatedAt >= startDate && su.Siparis.CreatedAt <= now)
                    .Select(su => su.Siparis_Id)
                    .Distinct()
                    .CountAsync();

                var amount = await _db.SiparisUrunleri
                    .Where(su => su.Siparis.CreatedAt >= startDate && su.Siparis.CreatedAt <= now)
                    .SumAsync(su => (decimal?)su.Toplam_Fiyat) ?? 0m;

                return (count, amount);
            }

            // Daily (last 1 day) - Tüm sipariş detaylarını al ve konsola yazdır
            var dailyStart = now.AddDays(-1);
            
            var dailyOrders = await _db.SiparisUrunleri
                .Where(su => su.Siparis.CreatedAt >= dailyStart && su.Siparis.CreatedAt <= now && su.Siparis.KullaniciId.HasValue)
                .Select(su => new
                {
                    SiparisId = su.Siparis_Id,
                    SiparisNumarasi = su.Siparis.SiparisNumarasi,
                    UserId = su.Siparis.KullaniciId!.Value,
                    CreatedAt = su.Siparis.CreatedAt
                })
                .Distinct()
                .ToListAsync();

            // Kullanıcı adlarını al
            var userIds = dailyOrders.Select(x => x.UserId).Distinct().ToList();
            var users = await _db.Kullanicilar
                .Where(k => userIds.Contains(k.Id))
                .ToDictionaryAsync(k => k.Id, k => k.Ad);

            // Konsola her siparişi yazdır
            Console.WriteLine($"\n=== GÜNLÜK SİPARİŞ DETAYLARI ({dailyStart:yyyy-MM-dd HH:mm:ss} - {now:yyyy-MM-dd HH:mm:ss}) ===");
            foreach (var order in dailyOrders.OrderBy(x => x.CreatedAt))
            {
                var userName = users.ContainsKey(order.UserId) ? users[order.UserId] : "Bilinmeyen";
                Console.WriteLine($"Sipariş: {order.SiparisNumarasi} | Kullanıcı: {userName} | Saat: {order.CreatedAt:HH:mm:ss}");
            }

            // Kullanıcı başına özet
            var dailyOrdersByUser = dailyOrders
                .GroupBy(x => x.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    OrderCount = g.Count()
                })
                .ToList();

            Console.WriteLine($"\n=== KULLANICILAR BAZINDA ÖZET ===");
            var totalOrders = 0;
            foreach (var item in dailyOrdersByUser.OrderByDescending(x => x.OrderCount))
            {
                var userName = users.ContainsKey(item.UserId) ? users[item.UserId] : "Bilinmeyen";
                Console.WriteLine($"Kullanıcı: {userName} (ID: {item.UserId}) - Sipariş Sayısı: {item.OrderCount}");
                totalOrders += item.OrderCount;
            }
            Console.WriteLine($"=== TOPLAM GÜNLÜK SİPARİŞ SAYISI: {totalOrders} ===\n");

            // Daily, weekly, monthly hesaplamaları
            var daily = await GetSalesForPeriod(dailyStart);

            // Weekly (last 7 days)
            var weeklyStart = now.AddDays(-7);
            var weekly = await GetSalesForPeriod(weeklyStart);

            // Monthly (last 30 days)
            var monthlyStart = now.AddDays(-30);
            var monthly = await GetSalesForPeriod(monthlyStart);

            return Ok(new
            {
                calculatedAt = now,
                daily = new
                {
                    count = daily.count,
                    amount = daily.amount
                },
                weekly = new
                {
                    count = weekly.count,
                    amount = weekly.amount
                },
                monthly = new
                {
                    count = monthly.count,
                    amount = monthly.amount
                }
            });

        }

        [HttpGet("sales-by-user")]
        public async Task<IActionResult> GetSalesByUser([FromQuery] string period = "monthly")
        {
            // allow only Admin role
            var role = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role")?.Value;
            if (role != "Admin") return Forbid();

            var now = DateTime.UtcNow.AddHours(3); // Turkish timezone (UTC+3)
            DateTime baslangicTarihi;

            // Periyoda göre başlangıç tarihini belirle
            switch (period.ToLower())
            {
                case "daily":
                    baslangicTarihi = now.AddDays(-1);
                    break;
                case "weekly":
                    baslangicTarihi = now.AddDays(-7);
                    break;
                case "all":
                    baslangicTarihi = DateTime.MinValue.ToUniversalTime();
                    break;
                case "monthly":
                default:
                    baslangicTarihi = now.AddDays(-30);
                    break;
            }

            // Kullanıcı başına satışları grupla
            var salesByUser = await _db.SiparisUrunleri
                .Where(su => su.Siparis.CreatedAt >= baslangicTarihi && su.Siparis.KullaniciId.HasValue)
                .GroupBy(su => su.Siparis.KullaniciId!.Value)
                .Select(g => new
                {
                    UserId = g.Key,
                    Count = g.Count(),
                    Amount = g.Sum(su => su.Toplam_Fiyat)
                })
                .OrderByDescending(x => x.Amount)
                .ToListAsync();

            // Kullanıcı adlarını al
            var userIds = salesByUser.Select(x => x.UserId).ToList();
            var users = await _db.Kullanicilar
                .Where(k => userIds.Contains(k.Id))
                .ToDictionaryAsync(k => k.Id, k => k.Ad);

            // DTO'ya dönüştür
            var salesDtos = salesByUser.Select(s => new SalesByUserDto
            {
                UserId = s.UserId,
                Name = users.ContainsKey(s.UserId) ? users[s.UserId] : null,
                Count = s.Count,
                Amount = s.Amount
            }).ToList();

            // En çok adet ve en çok tutar yapanları bul
            var topByCount = salesDtos.OrderByDescending(x => x.Count).FirstOrDefault();
            var topByAmount = salesDtos.OrderByDescending(x => x.Amount).FirstOrDefault();

            return Ok(new SalesByUserResponseDto
            {
                Users = salesDtos,
                TopByCount = topByCount,
                TopByAmount = topByAmount
            });
        }
    }
}
