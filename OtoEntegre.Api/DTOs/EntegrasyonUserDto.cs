using System;

namespace OtoEntegre.Api.DTOs
{
    public class IntegrationDto
    {
        public Guid? Id { get; set; }
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
        public int? SellerId { get; set; }
        public Guid? PlatformId { get; set; }
        public string PlatformAdi { get; set; } = string.Empty;
    }

    public class EntegrasyonUserDto
    {
        public Guid KullaniciId { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Telefon { get; set; } = string.Empty;

        public IntegrationDto? Entegrasyon { get; set; }
    }
}
