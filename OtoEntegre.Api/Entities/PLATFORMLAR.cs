using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtoEntegre.Api.Entities
{
    [Table("platformlar")]
    public class PLATFORMLAR
    {
        [Column("id")]
        public Guid Id { get; set; }

        [Column("ad")]
        public string Ad { get; set; } = null!;

        [Column("tip_id")]
        public Guid? Tip_Id { get; set; }  // nullable yaptık

        [Column("api_url")]
public string? Api_Url { get; set; }  // nullable yaptık


        [Column("created_at")]
        public DateTime? Created_At { get; set; }

        [Column("updated_at")]
        public DateTime? Updated_At { get; set; }

        // Foreign key ilişkisi
        [ForeignKey("Tip_Id")]
        public PLATFORM_TURLERI? Tip { get; set; }

        public ICollection<Entegrasyonlar> Entegrasyonlar { get; set; } = new List<Entegrasyonlar>();
    }
}
