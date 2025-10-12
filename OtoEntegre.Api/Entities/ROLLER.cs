using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtoEntegre.Api.Entities
{
        [Table("roller")]

    public class ROLLER
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("ad")]
        public string Ad { get; set; } = null!;
        [Column("aciklama")]
        public string? Aciklama { get; set; }

        public ICollection<KullaniciRolleri> KullaniciRolleri { get; set; } = new List<KullaniciRolleri>();
    }
}
