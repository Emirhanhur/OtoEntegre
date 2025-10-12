using System.ComponentModel.DataAnnotations;

public class LoginDto
{
    [Required]
    public string Email { get; set; } = null!;
    
    [Required]
    public string Password { get; set; } = null!;
}

public class LoginResponseDto
{
    public string Token { get; set; } = null!;
    public KullaniciDto User { get; set; } = null!;
}