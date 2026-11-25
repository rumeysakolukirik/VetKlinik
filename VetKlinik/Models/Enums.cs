using System.ComponentModel.DataAnnotations;

namespace VetKlinik.Models;

/// <summary>Randevunun yaşam döngüsü. Randevu her zaman "Bekliyor" durumunda oluşur.</summary>
public enum RandevuDurumu
{
    [Display(Name = "Bekliyor")] Bekliyor = 0,
    [Display(Name = "Onaylandı")] Onaylandi = 1,
    [Display(Name = "Reddedildi")] Reddedildi = 2,
    [Display(Name = "Tamamlandı")] Tamamlandi = 3,
    [Display(Name = "İptal Edildi")] IptalEdildi = 4
}

/// <summary>Sistemdeki roller (ödev en az üç rol istiyor).</summary>
public static class Roller
{
    public const string Admin = "Admin";
    public const string Veteriner = "Veteriner";
    public const string Uye = "Uye";
}

/// <summary>Formlarda kullanılan sabit seçenek listeleri.</summary>
public static class Secenekler
{
    public static readonly string[] Turler = ["Kedi", "Köpek", "Kuş", "Tavşan", "Hamster", "Sürüngen", "Diğer"];

    public static readonly string[] UzmanlikAlanlari =
        ["Küçük Hayvan", "Egzotik Hayvan", "Cerrahi", "Dermatoloji", "Diş Sağlığı", "Dahiliye", "Bakım"];
}
