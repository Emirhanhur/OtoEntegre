using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtoEntegre.Api.Entities
{
    [Table("otosticker_urunler")]
    public class Otosticker_Urunler
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("platform_id")]
        public Guid PlatformId { get; set; }


        [Column("kullanici_id")]
        public Guid KullaniciId { get; set; }


        [Column("urun_tedarik_barcode")]
        public string? UrunTedarikBarcode { get; set; }

        [Column("product_code")]
        public long? ProductCode { get; set; }
  
    }
}
