using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtoEntegre.Api.Entities
{
    [Table("siparis_urunleri")]

    public class SiparisUrunleri
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("siparis_id")]
        public Guid Siparis_Id { get; set; }
        [Column("urun_id")]
        public Guid Urun_Id { get; set; }
        [Column("adet")]
        public int Adet { get; set; }
        [Column("birim_fiyat")]
        public decimal Birim_Fiyat { get; set; }
        [Column("toplam_fiyat")]
        public decimal Toplam_Fiyat { get; set; }

        [Column("siparis_notu")]
        public string? SiparisNotu { get; set; }
        public Siparisler Siparis { get; set; } = null!;
        public Urunler Urun { get; set; } = null!;
    }
}
