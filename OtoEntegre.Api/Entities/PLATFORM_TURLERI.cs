using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtoEntegre.Api.Entities
{
        [Table("platform_turleri")]

    public class PLATFORM_TURLERI
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("ad")]
        public string Ad { get; set; } = null!;

        public ICollection<PLATFORMLAR> Platformlar { get; set; } = new List<PLATFORMLAR>();
    }
}
