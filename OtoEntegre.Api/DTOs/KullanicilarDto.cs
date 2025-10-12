public class KullaniciDto
{
    public Guid Id { get; set; }
    public string Ad { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Telefon { get; set; } = null!;
    public string? Entegrasyon_Telefon { get; set; }
    public string? Telegram_Chat { get; set; }
    public string? Telegram_Token { get; set; }
    public int? Tedarik_Kullanici_Id { get; set; }
    public DateTime Created_At { get; set; }
    public DateTime Updated_At { get; set; }
    public List<KullaniciRolDto> Roller { get; set; } = new List<KullaniciRolDto>();
    // Şifre asla response Dto'da yer almaz!
}


public class CreateKullaniciDto
{
    public string Ad { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Telefon { get; set; } = null!;
    public string? Entegrasyon_Telefon { get; set; }
    public bool? TelegramUseSamePhone { get; set; }

    public string? Telegram_Chat { get; set; }
    public string? Telegram_Token { get; set; }
    public string Sifre { get; set; } = null!;
    public Guid RolId { get; set; } // Artık string yerine Guid
    public int? Tedarik_Kullanici_Id { get; set; }
}


public class UpdateKullaniciDto
{
    public string Ad { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Telefon { get; set; } = null!;
    public string? Entegrasyon_Telefon { get; set; }
    public bool? TelegramUseSamePhone { get; set; }

    public string? Telegram_Chat { get; set; }
    public string? Telegram_Token { get; set; }
    public Guid RolId { get; set; } 
}
