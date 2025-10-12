using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtoEntegre.Api.Entities
{
        [Table("entegrasyonlar")]
        public class Entegrasyonlar
        {
                [Column("id")]
                public Guid Id { get; set; }

                
                [Column("kullanici_id")]
                [ForeignKey(nameof(Kullanici))]
                public Guid? Kullanici_Id { get; set; }
                [Column("platform_id")]
                [ForeignKey(nameof(Platform))]

                public Guid? Platform_Id { get; set; }
                [Column("api_key")]

                public string? Api_Key { get; set; }
                [Column("api_secret")]

                public string? Api_Secret { get; set; }
                [Column("kullanici_adi")]


                public string? Kullanici_Adi { get; set; }
                [Column("seller_id")]
                public int? Seller_Id { get; set; }

                [Column("sifre")]

                public string? Sifre { get; set; }
                [Column("extra_config", TypeName = "json")]
                public string? Extra_Config { get; set; }

                [Column("created_at")]
                public DateTime Created_At { get; set; } = DateTime.UtcNow;

                [Column("updated_at")]
                public DateTime Updated_At { get; set; } = DateTime.UtcNow;



                public KULLANICILAR? Kullanici { get; set; }

                public PLATFORMLAR? Platform { get; set; }

                public ICollection<Siparisler> Siparisler { get; set; } = new List<Siparisler>();
        }
}
