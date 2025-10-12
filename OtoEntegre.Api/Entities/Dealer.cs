using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtoEntegre.Api.Entities
{
    [Table("dealers")]
    public class Dealer
    {
    public int Id { get; set; }

[Column("lastname")]
public string Lastname { get; set; } = string.Empty;

        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("phone")]
        public string Phone { get; set; } = string.Empty;

        // Siparisler ilişkisi
        public ICollection<Siparisler> Siparisler { get; set; } = new List<Siparisler>();
    }
}
