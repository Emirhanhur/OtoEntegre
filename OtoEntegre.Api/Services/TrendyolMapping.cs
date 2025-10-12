using System;
using System.Linq;
using OtoEntegre.Api.DTOs;
using OtoEntegre.Api.Entities;

namespace OtoEntegre.Api.Services
{
    public static class TrendyolMapping
    {
        public static Siparisler MapToSiparis(TrendyolOrderPayload payload, Entegrasyonlar entegrasyon)
        {
            var firstLine = payload.Lines?.FirstOrDefault();
            var musteriAdSoyad = (payload.ShipmentAddress?.FirstName + " " + payload.ShipmentAddress?.LastName)?.Trim() ?? string.Empty;

            return new Siparisler
            {
                Id = Guid.NewGuid(),
                EntegrasyonId = entegrasyon?.Id,
                KullaniciId = entegrasyon?.Kullanici_Id,
                SiparisNumarasi = payload.OrderNumber ?? string.Empty,
                ToplamTutar = payload.GrossAmount-payload.TotalTyDiscount,
                KargoUcreti = 0,
                OdemeDurumu = string.Empty,
                Durum = payload.Status ?? string.Empty,
                Kod = firstLine?.Barcode ?? firstLine?.Sku ?? string.Empty,
                PlatformUrunKod = firstLine?.Sku ?? string.Empty,
                KargoTakipNumarasi = payload.CargoTrackingNumber?.ToString() ?? string.Empty,
                PaketNumarasi = payload.Id.ToString() ?? string.Empty,
                MusteriAdSoyad = musteriAdSoyad,
                Renk = firstLine?.ProductColor ?? string.Empty,
                Beden = firstLine?.ProductSize ?? string.Empty,
                MusteriAdres = payload.ShipmentAddress?.FullAddress ?? string.Empty,
                UrunTrendyolKod = firstLine?.ProductCode.ToString() ?? string.Empty,
                CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(payload.OrderDate).UtcDateTime,
                UpdatedAt = DateTime.UtcNow,
                TelegramSent = false,
                TedarikSent = false,
                CargoProviderName = payload.CargoProviderName,
            };
        }
    }
}


