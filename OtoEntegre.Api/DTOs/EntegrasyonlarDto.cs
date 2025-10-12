using System;

namespace OtoEntegre.Api.DTOs
{
    public class EntegrasyonDto
    {
        public Guid Id { get; set; }
        public Guid? Kullanici_Id { get; set; }
        public Guid? Platform_Id { get; set; }
        public int? Seller_Id { get; set; }
        public string? Api_Key { get; set; }
        public string? Api_Secret { get; set; }
        public string? Kullanici_Adi { get; set; }
        public string? Sifre { get; set; }
        public string? Extra_Config { get; set; }
        public DateTime Created_At { get; set; }
        public DateTime Updated_At { get; set; }
    }
}
