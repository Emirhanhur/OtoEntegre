namespace OtoEntegre.Api.DTOs
{
    public class RolCreateDto
    {
        public string Ad { get; set; } = null!;
        public string? Aciklama { get; set; }
    }
    public class RolUpdateDto
{
    public string Ad { get; set; } = null!;
    public string? Aciklama { get; set; }
}

}
