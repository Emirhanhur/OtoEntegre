using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OtoEntegre.Api.DTOs
{
    public class TrendyolWebhookRequest
    {
        public ShipmentAddress ShipmentAddress { get; set; } = new ShipmentAddress();

        public string OrderNumber { get; set; } = string.Empty;
        public decimal GrossAmount { get; set; }
        public decimal TotalDiscount { get; set; }
        public decimal TotalTyDiscount { get; set; }
        public string? TaxNumber { get; set; }

        public InvoiceAddress InvoiceAddress { get; set; } = new InvoiceAddress();
        public string CustomerFirstName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public long CustomerId { get; set; }
        public string CustomerLastName { get; set; } = string.Empty;
        public long Id { get; set; }
        public long? CargoTrackingNumber { get; set; }
        public string CargoProviderName { get; set; } = string.Empty;
        public List<TrendyolOrderLine> Lines { get; set; } = new List<TrendyolOrderLine>();
        public long OrderDate { get; set; }
        public string IdentityNumber { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public List<PackageHistory> PackageHistories { get; set; } = new List<PackageHistory>();
        public string ShipmentPackageStatus { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string DeliveryType { get; set; } = string.Empty;
        public long TimeSlotId { get; set; }
        public long EstimatedDeliveryStartDate { get; set; }
        public long EstimatedDeliveryEndDate { get; set; }
        public decimal TotalPrice { get; set; }
        public string DeliveryAddressType { get; set; } = string.Empty;
        public long AgreedDeliveryDate { get; set; }
        public bool FastDelivery { get; set; }
        public long OriginShipmentDate { get; set; }
        public long LastModifiedDate { get; set; }
        public bool Commercial { get; set; }
        public string FastDeliveryType { get; set; } = string.Empty;
        public bool DeliveredByService { get; set; }
        public bool AgreedDeliveryDateExtendible { get; set; }
        public long ExtendedAgreedDeliveryDate { get; set; }
        public long AgreedDeliveryExtensionEndDate { get; set; }
        public long AgreedDeliveryExtensionStartDate { get; set; }
        public int WarehouseId { get; set; }
        public bool GroupDeal { get; set; }
        public bool Micro { get; set; }
        public bool GiftBoxRequested { get; set; }
        [JsonPropertyName("3pByTrendyol")]
        public bool ThreePByTrendyol { get; set; }
        public bool ContainsDangerousProduct { get; set; }
        public bool IsCod { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
   [JsonPropertyName("originPackageIds")]
public List<long>? OriginPackageIds { get; set; }


    }

    public class ShipmentAddress
    {
        public long Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public string Address1 { get; set; } = string.Empty;
        public string Address2 { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;
        public int CityCode { get; set; }
        public string District { get; set; } = string.Empty;
        public int DistrictId { get; set; }
        public int CountyId { get; set; }
        public string CountyName { get; set; } = string.Empty;
        public string ShortAddress { get; set; } = string.Empty;
        public string StateName { get; set; } = string.Empty;
        public AddressLines AddressLines { get; set; } = new AddressLines();
        public string PostalCode { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
        public int NeighborhoodId { get; set; }
        public string Neighborhood { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string FullAddress { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }

    public class InvoiceAddress : ShipmentAddress
    {
        public string TaxOffice { get; set; } = string.Empty;
        public string? TaxNumber { get; set; }
    }

    public class AddressLines
    {
        public string AddressLine1 { get; set; } = string.Empty;
        public string AddressLine2 { get; set; } = string.Empty;
    }

    public class TrendyolOrderLine
    {
        public int Quantity { get; set; }
        public long SalesCampaignId { get; set; }
        public string ProductSize { get; set; } = string.Empty;
        public string MerchantSku { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public long ProductCode { get; set; }
        public int MerchantId { get; set; }
        public decimal Amount { get; set; }
        public decimal Discount { get; set; }

        public decimal tyDiscount { get; set; }

        public List<DiscountDetail> DiscountDetails { get; set; } = new List<DiscountDetail>();
        public string CurrencyCode { get; set; } = string.Empty;
        public string ProductColor { get; set; } = string.Empty;
        public long Id { get; set; }
        public string Sku { get; set; } = string.Empty;
        public decimal VatBaseAmount { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string OrderLineItemStatusName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public List<FastDeliveryOption> FastDeliveryOptions { get; set; } = new List<FastDeliveryOption>();
        public long ProductCategoryId { get; set; }
        public decimal? Commission { get; set; }


    }

    public class DiscountDetail
    {
        public decimal LineItemPrice { get; set; }
        public decimal LineItemDiscount { get; set; }
        public decimal LineItemTyDiscount { get; set; }
    }

    public class FastDeliveryOption
    {
    }

    public class PackageHistory
    {
        public long CreatedDate { get; set; }
        public string Status { get; set; } = string.Empty;
    }
    public class TrendyolOrderPayload
    {
        [JsonPropertyName("shipmentAddress")]
        public ShipmentAddress ShipmentAddress { get; set; } = new ShipmentAddress();

        [JsonPropertyName("invoiceAddress")]
        public InvoiceAddress InvoiceAddress { get; set; } = new InvoiceAddress();

        [JsonPropertyName("orderNumber")]
        public string OrderNumber { get; set; } = string.Empty;

        [JsonPropertyName("grossAmount")]
        public decimal GrossAmount { get; set; }

        [JsonPropertyName("totalDiscount")]
        public decimal TotalDiscount { get; set; }

        [JsonPropertyName("totalTyDiscount")]
        public decimal TotalTyDiscount { get; set; }



        [JsonPropertyName("customerFirstName")]
        public string CustomerFirstName { get; set; } = string.Empty;

        [JsonPropertyName("customerLastName")]
        public string CustomerLastName { get; set; } = string.Empty;

        [JsonPropertyName("customerId")]
        public long CustomerId { get; set; }

        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("cargoTrackingNumber")]
        public long? CargoTrackingNumber { get; set; }

        [JsonPropertyName("productCode")]
        public long? ProductCode { get; set; }

        [JsonPropertyName("cargoTrackingLink")]
        public string CargoTrackingLink { get; set; } = string.Empty;

        [JsonPropertyName("cargoSenderNumber")]
        public string CargoSenderNumber { get; set; } = string.Empty;

        [JsonPropertyName("cargoProviderName")]
        public string CargoProviderName { get; set; } = string.Empty;

        [JsonPropertyName("lines")]
        public List<TrendyolOrderLine> Lines { get; set; } = new List<TrendyolOrderLine>();

        [JsonPropertyName("orderDate")]
        public long OrderDate { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("currencyCode")]
        public string CurrencyCode { get; set; } = string.Empty;

        [JsonPropertyName("packageHistories")]
        public List<PackageHistory> PackageHistories { get; set; } = new List<PackageHistory>();

        [JsonPropertyName("shipmentPackageStatus")]
        public string ShipmentPackageStatus { get; set; } = string.Empty;

        [JsonPropertyName("deliveryType")]
        public string DeliveryType { get; set; } = string.Empty;

        [JsonPropertyName("fastDelivery")]
        public bool FastDelivery { get; set; }

        [JsonPropertyName("fastDeliveryType")]
        public string FastDeliveryType { get; set; } = string.Empty;

        [JsonPropertyName("originShipmentDate")]
        public long OriginShipmentDate { get; set; }

        [JsonPropertyName("lastModifiedDate")]
        public long LastModifiedDate { get; set; }

        [JsonPropertyName("commercial")]
        public bool Commercial { get; set; }

        [JsonPropertyName("micro")]
        public bool Micro { get; set; }

        [JsonPropertyName("giftBoxRequested")]
        public bool GiftBoxRequested { get; set; }

        [JsonPropertyName("etgbNo")]
        public string EtgbNo { get; set; } = string.Empty;

        [JsonPropertyName("etgbDate")]
        public long EtgbDate { get; set; }

        [JsonPropertyName("containsDangerousProduct")]
        public bool ContainsDangerousProduct { get; set; }

        [JsonPropertyName("whoPays")]
        public int? WhoPays { get; set; }

        [JsonPropertyName("createdBy")]
        public string CreatedBy { get; set; } = string.Empty;

        [JsonPropertyName("productSize")]
        public string Size { get; set; } = string.Empty;

        [JsonPropertyName("productColor")]
        public string Color { get; set; } = string.Empty;

 [JsonPropertyName("originPackageIds")]
public List<long>? OriginPackageIds { get; set; }



    }

}
