using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtoEntegre.Api.Entities
{
    [Table("kredi_islemleri")]
    public class KrediIslemleri
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("kullanici_id")]
        public Guid KullaniciId { get; set; }

        [Column("miktar")]
        public int Miktar { get; set; }

        [Column("tip")]
        public string Tip { get; set; } = string.Empty;

        [Column("balance_after")]
        public int BalanceAfter { get; set; }

        [Column("referans")]
        public string? Referans { get; set; }

        [Column("aciklama")]
        public string? Aciklama { get; set; }

        [Column("metadata")]
        public string? Metadata { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("created_by")]
        public Guid? CreatedBy { get; set; }
    }
}
