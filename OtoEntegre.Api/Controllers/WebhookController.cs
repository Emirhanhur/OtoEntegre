using Microsoft.EntityFrameworkCore; // ✅ Bunu ekle
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using OtoEntegre.Api.Entities;
using OtoEntegre.Api.Repositories;
using OtoEntegre.Api.DTOs;
using OtoEntegre.Api.Services;
using OtoEntegre.Api.Data;
using PuppeteerSharp;
namespace OtoEntegre.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public static class TrendyolMapping
    {
        public static Siparisler MapToSiparis(TrendyolWebhookRequest request, Entegrasyonlar entegrasyon)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            var payload = new TrendyolOrderPayload
            {
                ShipmentAddress = request.ShipmentAddress,
                InvoiceAddress = request.InvoiceAddress,
                OrderNumber = request.OrderNumber,
                GrossAmount = request.GrossAmount,
                TotalDiscount = request.TotalDiscount,
                TotalTyDiscount = request.TotalTyDiscount,
                CustomerFirstName = request.CustomerFirstName,
                CustomerLastName = request.CustomerLastName,
                CustomerId = request.CustomerId,
                Id = request.Id,
                CargoTrackingNumber = request.CargoTrackingNumber,
                CargoProviderName = request.CargoProviderName,
                Lines = request.Lines,
                OrderDate = request.OrderDate,
                Status = request.Status,
                CurrencyCode = request.CurrencyCode,
                PackageHistories = request.PackageHistories,
                ShipmentPackageStatus = request.ShipmentPackageStatus,
                DeliveryType = request.DeliveryType,
                FastDelivery = request.FastDelivery,
                OriginShipmentDate = request.OriginShipmentDate,
                LastModifiedDate = request.LastModifiedDate,
                Commercial = request.Commercial,
                Micro = request.Micro,
                GiftBoxRequested = request.GiftBoxRequested,
                ContainsDangerousProduct = request.ContainsDangerousProduct,
                CreatedBy = request.CreatedBy,
                OriginPackageIds = request.OriginPackageIds
            };

            // Burada doğru overload çağrılmalı
            return MapToSiparis(payload, entegrasyon);
        }
        public static Siparisler MapToSiparis(TrendyolOrderPayload payload, Entegrasyonlar entegrasyon)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            var firstLine = payload.Lines?.FirstOrDefault();

            var siparis = new Siparisler
            {
                KullaniciId = entegrasyon?.Kullanici_Id,
                EntegrasyonId = entegrasyon?.Id,
                SiparisNumarasi = payload.OrderNumber,
                MusteriAdSoyad = $"{payload.CustomerFirstName} {payload.CustomerLastName}",
                MusteriAdres = $"{payload.ShipmentAddress.FullAddress} ",
                ToplamTutar = payload.GrossAmount,
                CargoProviderName = payload.CargoProviderName ?? string.Empty,
                KargoTakipNumarasi = payload.CargoTrackingNumber?.ToString() ?? string.Empty,
                PaketNumarasi = payload.Id.ToString(),
                Durum = payload.Status,
                CreatedAt = DateTimeOffset.FromUnixTimeMilliseconds(payload.OrderDate).UtcDateTime,
                UpdatedAt = DateTimeOffset.FromUnixTimeMilliseconds(payload.LastModifiedDate).UtcDateTime,


                UrunTrendyolKod = firstLine?.ProductCode.ToString()??"",
                Kod = firstLine?.ProductCode.ToString()??"",
                Beden = firstLine?.ProductSize??"",
                Renk = firstLine?.ProductColor??"",
                PlatformUrunKod = firstLine?.ProductCode.ToString()??""
            };

            return siparis;
        }


    }
}