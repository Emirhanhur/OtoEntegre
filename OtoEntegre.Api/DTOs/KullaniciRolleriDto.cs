public class KullaniciRolDto
{
    public Guid Id { get; set; }
    public Guid KullaniciId { get; set; }
    public Guid RolId { get; set; }
    public RolDto Rol { get; set; } = null!;
}

public class RolDto
{
    public Guid Id { get; set; }
    public string Ad { get; set; } = null!;
    public string? Aciklama { get; set; }
}
