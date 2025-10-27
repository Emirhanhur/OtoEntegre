using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtoEntegre.Api.Entities
{
    [Table("krediler")]
    public class Krediler
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("kullanici_id")]
        public Guid KullaniciId { get; set; }

        [Column("kalan_kredi")]
        public int KalanKredi { get; set; }

        [Column("son_satin_alim")]
        public DateTime? SonSatinAlim { get; set; }
    }
}
