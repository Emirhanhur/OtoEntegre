using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtoEntegre.Api.Entities
{
    [Table("kullanicilar")]
    public class KULLANICILAR
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("ad")]
        public string Ad { get; set; } = null!;

        [Column("adres")]
        public string? Adres { get; set; } 

         [Column("sehir")]
        public string? Sehir { get; set; } 

        [Column("ilce")]
        public string? Ilce { get; set; } 

        [Column("email")]
        public string Email { get; set; } = null!;

        [Column("telefon")]
        public string Telefon { get; set; } = null!;

        [Column("entegrasyon_telefon")]
        public string? Entegrasyon_Telefon { get; set; }

        [Column("sifre_hash")]
        // Hashlenmiş şifre (BCrypt ile)
        public string Sifre_Hash { get; set; } = null!;

        [Column("rol")]
        public string? Rol { get; set; } // artık null değer alabilir


        [Column("created_at")]
        public DateTime Created_At { get; set; }

        [Column("updated_at")]
        public DateTime Updated_At { get; set; }

        [Column("tedarik_kullanici_id")]
        public int? Tedarik_Kullanici_Id { get; set; }

        [Column("telegram_chat")]
        public string? Telegram_Chat { get; set; }
        
        [Column("telegram_token")]
        public string? Telegram_Token { get; set; }

        [Column("tedarik_musteri_no")]
        public int? Tedarik_Musteri_No { get; set; }

        [Column("deleted")]

        public bool Deleted { get ; set;} 

        [Column("son_giris")]
        public DateTime? Son_Giris { get; set; }

        // Navigation
        public ICollection<KullaniciRolleri> Roller { get; set; } = new List<KullaniciRolleri>();
    }
}
