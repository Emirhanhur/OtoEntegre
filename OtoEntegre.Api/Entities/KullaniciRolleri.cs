using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OtoEntegre.Api.Entities
{
[Table("kullanici_rolleri")]
public class KullaniciRolleri
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("kullanici_id")]
    public Guid KullaniciId { get; set; }

    [Column("rol_id")]
    public Guid RolId { get; set; }

[ForeignKey(nameof(RolId))]
public ROLLER? Rol { get; set; }

[ForeignKey(nameof(KullaniciId))]
public KULLANICILAR? Kullanici { get; set; }

}


}
