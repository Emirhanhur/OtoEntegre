using System;
using OtoEntegre.Api.Converters;
namespace OtoEntegre.Api.DTOs
{
    public class EntegrasyonCreateDto
    {
        public Guid? Kullanici_Id { get; set; }
        public Guid? Platform_Id { get; set; }
        public string? Api_Key { get; set; }

        public int? Seller_Id { get; set; }

        public string? Api_Secret { get; set; }
        public string? Kullanici_Adi { get; set; }
        public string? Sifre { get; set; }
        public object? Extra_Config { get; set; } // object tipinde JSON al
    }
}
