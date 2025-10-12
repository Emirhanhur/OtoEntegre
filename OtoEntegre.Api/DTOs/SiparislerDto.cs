public class SiparisDto
{
    public Guid Id { get; set; }
    public string? Detay { get; set; }
    public Guid? Entegrasyon_Id { get; set; }
}

public class SiparisCreateDto
{
    public string Siparis_Numarasi { get; set; } = string.Empty;
    public decimal Toplam_Tutar { get; set; }
    public decimal? Kargo_Ucreti { get; set; }
    public string? Odeme_Durumu { get; set; }
    public string? Kod { get; set; }
    public string? Durum { get; set; }
    public Guid? Kullanici_Id { get; set; }
    public Guid? Entegrasyon_Id { get; set; }
    public Guid? Tedarik_Kullanici_Id { get; set; }
    public string? Platform_Urun_Code { get; set; }
    public string? Musteri_Ad_Soyad { get; set; }
    public string? Musteri_Adres { get; set; }
    public string? Beden { get; set; }
    public string? Renk { get; set; }
    public string? Kargo_Takip_Numarasi { get; set; }
    public string? Urun_Trendyol_Kod { get; set; }
}


public class SiparisUpdateDto
{
    public string Siparis_Numarasi { get; set; } = string.Empty;
    public decimal Toplam_Tutar { get; set; }
    public decimal Kargo_Ucreti { get; set; }
    public string Odeme_Durumu { get; set; } = string.Empty;
    public string? Kod { get; set; } = null;
    public string Durum { get; set; }= string.Empty;
    public Guid? Kullanici_Id { get; set; }       // GUID yapıldı
    public Guid? Entegrasyon_Id { get; set; }     // GUID yapıldı
}
