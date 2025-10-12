using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtoEntegre.Api.Entities
{
        [Table("sistem_log")]

    public class SistemLog
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("kullanici_id")]
        public Guid? Kullanici_Id { get; set; }
        [Column("tablo")]
        public string Tablo { get; set; } = null!;
        [Column("tablo_id")]
        public Guid? Tablo_Id { get; set; }
        [Column("islem")]
        public string Islem { get; set; } = null!;
        [Column("mesaj")]
        public string Mesaj { get; set; } = null!;
        [Column("created_at")]
        public DateTime Created_At { get; set; }

        public KULLANICILAR? Kullanici { get; set; }
    }
}
