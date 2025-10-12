using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtoEntegre.Api.Entities
{
    [Table("urunler")]

    public class Urunler
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("urun_tedarik_barcode")]
        public string? UrunTedarikBarcode { get; set; }  // nullable bırakabiliriz


        [Column("otosticker_id")]

        public int? OtostickerProductId { get; set; }

        [Column("sku")]
        public string? Sku { get; set; } = null!;

        [Column("image")]
        public string Image { get; set; } = string.Empty;


        [Column("product_code")]
        public long? ProductCode { get; set; }
        [Column("ad")]
        public string Ad { get; set; } = null!;
        [Column("kategori")]
        public string? Kategori { get; set; } = null!;
        [Column("created_at")]
        public DateTime Created_At { get; set; }
        [Column("updated_at")]
        public DateTime Updated_At { get; set; }

        public ICollection<SiparisUrunleri> SiparisUrunleri { get; set; } = new List<SiparisUrunleri>();
    }
}
