using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OtoEntegre.Api.Data;
using OtoEntegre.Api.Entities;

namespace OtoEntegre.Api.Services
{
    public class KredilerService
    {
        private readonly AppDbContext _db;

        public KredilerService(AppDbContext db)
        {
            _db = db;
        }
        public async Task<Krediler?> GetByKullaniciAsync(Guid kullaniciId)
        {
            var kred = await _db.Krediler.FirstOrDefaultAsync(k => k.KullaniciId == kullaniciId);
            if (kred == null)
            {
                kred = new Krediler
                {
                    Id = Guid.NewGuid(),
                    KullaniciId = kullaniciId,
                    KalanKredi = 0,
                    SonSatinAlim = null
                };
                _db.Krediler.Add(kred);
                await _db.SaveChangesAsync();
            }
            return kred;
        }

        // Atomik tüketim: bakiye >= 1 ise decrement et ve işlem kaydı ekle
        public async Task<bool> ConsumeOneAsync(Guid kullaniciId, Guid? performedBy = null, string? referans = null)
        {
            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                // Fetch existing krediler row (if any) and decrement in a tracked, transactional way
                var kred = await _db.Krediler.FirstOrDefaultAsync(k => k.KullaniciId == kullaniciId);
                if (kred == null)
                {
                    // No existing record: create one with -1 balance
                    kred = new Krediler
                    {
                        Id = Guid.NewGuid(),
                        KullaniciId = kullaniciId,
                        KalanKredi = -1,
                        SonSatinAlim = DateTime.UtcNow
                    };
                    _db.Krediler.Add(kred);
                }
                else
                {
                    // Decrement the tracked entity so EF will persist the correct new balance
                    kred.KalanKredi -= 1;
                    kred.SonSatinAlim = DateTime.UtcNow;
                    _db.Krediler.Update(kred);
                }
                await _db.SaveChangesAsync();

                var islemi = new KrediIslemleri
                {
                    Id = Guid.NewGuid(),
                    KullaniciId = kullaniciId,
                    Miktar = -1,
                    Tip = "Harcandı",
                    BalanceAfter = kred.KalanKredi,
                    Referans = referans,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = performedBy
                };
                _db.KrediIslemleri.Add(islemi);
                await _db.SaveChangesAsync();

                await tx.CommitAsync();
                return true;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        // Kredi ekleme (satın alma / admin)
        public async Task<Krediler> AddCreditsAsync(Guid kullaniciId, int amount, Guid? performedBy = null, string? referans = null, string tip = "Yüklendi")
        {
            if (amount <= 0) throw new ArgumentException("amount must be > 0", nameof(amount));

            await using var tx = await _db.Database.BeginTransactionAsync();
            try
            {
                var kred = await _db.Krediler.FirstOrDefaultAsync(k => k.KullaniciId == kullaniciId);
                if (kred == null)
                {
                    kred = new Krediler
                    {
                        Id = Guid.NewGuid(),
                        KullaniciId = kullaniciId,
                        KalanKredi = amount,
                        SonSatinAlim = DateTime.UtcNow
                    };
                    _db.Krediler.Add(kred);
                }
                else
                {
                    kred.KalanKredi += amount;
                    kred.SonSatinAlim = DateTime.UtcNow;
                    _db.Krediler.Update(kred);
                }

                var islemi = new KrediIslemleri
                {
                    Id = Guid.NewGuid(),
                    KullaniciId = kullaniciId,
                    Miktar = amount,
                    Tip = tip,
                    BalanceAfter = kred.KalanKredi,
                    Referans = referans,
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = performedBy
                };
                _db.KrediIslemleri.Add(islemi);

                await _db.SaveChangesAsync();
                await tx.CommitAsync();

                return kred;
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }
    }
}
