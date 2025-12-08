using System;

namespace OtoEntegre.Api.DTOs
{
    public class KrediSummaryDto
    {
        public Guid KullaniciId { get; set; }
        public string Ad { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int KalanKredi { get; set; }
        public DateTime? SonSatinAlim { get; set; }
    }
}
