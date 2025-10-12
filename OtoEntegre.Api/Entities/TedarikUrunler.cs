using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtoEntegre.Api.Entities
{
    [Table("tedarik_urunler")]
    public class TedarikUrunler
    {
        [Column("id")]
        public Guid Id { get; set; }
        [Column("name")]
        public string Name { get; set; } = null!;
        [Column("barcode")]
        public string Barcode { get; set; } = null!;
        [Column("brand")]
        public string Brand { get; set; } = null!;
    }
}
