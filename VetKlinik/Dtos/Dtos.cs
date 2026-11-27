using System.ComponentModel.DataAnnotations;

namespace VetKlinik.Dtos;

// REST API ve MCP araçları entity yerine bu DTO'ları döndürür.
// Böylece navigation property döngüleri ve gizli alanlar (SahipId vb.) dışarı sızmaz.

public record HizmetDto(int Id, string Ad, string? Aciklama, int SureDakika, decimal Ucret, string Klinik);

public record VeterinerDto(int Id, string AdSoyad, string Klinik, IReadOnlyList<string> UzmanlikAlanlari,
    IReadOnlyList<string> Hizmetler);

public record SaatAraligiDto(string Baslangic, string Bitis);

public record UygunHekimDto(int VeterinerId, string AdSoyad, IReadOnlyList<string> UzmanlikAlanlari,
    IReadOnlyList<SaatAraligiDto> BosSaatler);

public record UygunSaatlerDto(int HizmetId, string Hizmet, int SureDakika, decimal Ucret, string Tarih,
    IReadOnlyList<UygunHekimDto> Hekimler);

public record EvcilHayvanDto(int Id, string Ad, string Tur, string? Irk, string DogumTarihi, double Kilo);

public record AsiDto(int Id, string AsiAdi, string YapildigiTarih, string? SonrakiDozTarihi, string? Notlar);

public record AsiTakvimiDto(int EvcilHayvanId, string HayvanAdi, IReadOnlyList<AsiDto> GecmisAsilar,
    IReadOnlyList<AsiDto> YaklasanDozlar, IReadOnlyList<AsiDto> GecikmisDozlar);

public record RandevuDto(int Id, string EvcilHayvan, string Hizmet, string Veteriner, string Baslangic,
    string Bitis, int SureDakika, decimal Ucret, string Durum, string? SahipNotu, string? MuayeneNotu,
    string? RedNedeni);

public record HizmetRaporSatiriDto(int HizmetId, string Hizmet, int RandevuSayisi, int TamamlananSayisi,
    decimal Ciro);

public record HizmetRaporuDto(string Baslangic, string Bitis, int ToplamRandevu, decimal ToplamCiro,
    IReadOnlyList<HizmetRaporSatiriDto> Satirlar);

public record HataDto(string Mesaj, IReadOnlyList<string>? Detaylar = null);

public class RandevuOlusturDto
{
    [Range(1, int.MaxValue)] public int EvcilHayvanId { get; set; }
    [Range(1, int.MaxValue)] public int HizmetId { get; set; }
    [Range(1, int.MaxValue)] public int VeterinerId { get; set; }
    [Required] public DateTime Baslangic { get; set; }
    [StringLength(500)] public string? Not { get; set; }
}

public class TokenIstegiDto
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Sifre { get; set; } = string.Empty;
}

public record TokenDto(string Token, DateTime GecerlilikSonu);
