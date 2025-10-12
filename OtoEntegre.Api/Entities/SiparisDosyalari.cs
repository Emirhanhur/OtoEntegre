using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace OtoEntegre.Api.Entities
{
        [Table("siparis_dosyalari")]
    
    public class SiparisDosyalari
    {
        [Column("id")]
        public Guid Id { get; set; }

[Column("telegram_image_sent")]
        public bool TelegramImageSent { get; set; }
        
        [Column("siparis_id")]
        public Guid Siparis_Id { get; set; }
        [Column("dosya_turu")]
        public string Dosya_Turu { get; set; } = null!;
        [Column("dosya_url")]
        public string Dosya_Url { get; set; } = null!;
        [Column("created_at")]
        public DateTime Created_At { get; set; }
        
    }
}
