using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations; // ForeignKey attribute burada var

namespace OtoEntegre.Api.Entities
{
    [Table("siparisler")]
    public class Siparisler
    {
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Column("kullanici_id")]
        public Guid? KullaniciId { get; set; }      // GUID bekleniyorsa

        [Column("entegrasyon_id")]
        public Guid? EntegrasyonId { get; set; }   // DB'de Guid

        [Column("siparis_numarasi")]
        public string SiparisNumarasi { get; set; } = string.Empty;

        [Column("toplam_tutar")]
        public decimal ToplamTutar { get; set; }

        [Column("kargo_ucreti")]
        public decimal KargoUcreti { get; set; }

        [Column("kargo_takip_numarasi")]
        public string KargoTakipNumarasi { get; set; } = string.Empty;

        
         [Column("kargo_firma_adi")]
        public string CargoProviderName { get; set; } = string.Empty;

        [Column("paket_numarasi")]
        public string PaketNumarasi { get; set; } = string.Empty;

        [Column("odeme_durumu")]
        public string OdemeDurumu { get; set; } = string.Empty;

        [Column("durum")]
        public string Durum { get; set; }= string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Column("dealer_id")]
        public int DealerId { get; set; }

        [Column("kod")]
        public string Kod { get; set; } = string.Empty;

        [Column("tedarik_kullanici_id")]
        public int? TedarikKullaniciId { get; set; }


        [NotMapped]
        public int ApiOrderId { get; set; }

        [Column("telegram_sent")]
        public bool TelegramSent { get; set; } = false;

        [Column("okundu")]
        public bool Okundu { get; set; } = false;

        [Column("geldigi_yer")]
        public int GeldigiYer { get; set; } 

        [Column("tedarik_sent")]
        public bool TedarikSent { get; set; } = false;

        [Column("tedarik_siparis_id")]
        public long TedarikSiparisId { get; set; }

        [Column("platform_urun_code")]
        public string PlatformUrunKod { get; set; } = string.Empty;

        [Column("musteri_ad_soyad")]
        public string MusteriAdSoyad { get; set; } = string.Empty;

        [Column("renk")]
        public string Renk { get; set; } = string.Empty;

        

        [Column("beden")]
        public string Beden { get; set; } = string.Empty;

        [Column("musteri_adres")]
        public string MusteriAdres { get; set; } = string.Empty;

         [Column("urun_trendyol_kod")]
        public string UrunTrendyolKod { get; set; } = string.Empty;



    public ICollection<SiparisUrunleri> SiparisUrunleri { get; set; } = new List<SiparisUrunleri>();


        [ForeignKey(nameof(EntegrasyonId))]
        public Entegrasyonlar? Entegrasyonlar { get; set; }


        public Dealer? Dealer { get; set; }
    }


    public class TrendyolProductResponse
    {
        public List<TrendyolProductDto> Content { get; set; } = new List<TrendyolProductDto>();
    }

    public class TrendyolProductDto
    {
        public long ProductCode { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
    }

    public class TrendyolOrderDto
    {
        public List<TrendyolOrderLineDto> Lines { get; set; } = new List<TrendyolOrderLineDto>();
    }

    public class TrendyolOrderLineDto
    {
        public string Barcode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

}
