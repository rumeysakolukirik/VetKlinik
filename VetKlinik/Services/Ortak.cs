using System.Globalization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using VetKlinik.Data;
using VetKlinik.Dtos;
using VetKlinik.Models;

namespace VetKlinik.Services;

/// <summary>
/// İsteği yapan kullanıcının servis katmanında ihtiyaç duyulan bilgileri.
/// MVC (cookie), REST API (cookie veya JWT) ve MCP (JWT) aynı nesneyi üretir;
/// yetki kuralları servislerde bu nesneye göre uygulanır.
/// </summary>
public record KullaniciBaglami(string UserId, bool IsAdmin, int? VeterinerId)
{
    public bool IsVeteriner => VeterinerId.HasValue;
}

public interface IKullaniciBaglamiSaglayici
{
    Task<KullaniciBaglami> GetirAsync(ClaimsPrincipal user);
}

public class KullaniciBaglamiSaglayici(AppDbContext db) : IKullaniciBaglamiSaglayici
{
    public async Task<KullaniciBaglami> GetirAsync(ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? throw new UnauthorizedAccessException("Kullanıcı kimliği bulunamadı.");
        int? vetId = null;
        if (user.IsInRole(Roller.Veteriner))
            vetId = await db.Veterinerler.Where(v => v.UserId == userId).Select(v => (int?)v.Id).FirstOrDefaultAsync();
        return new KullaniciBaglami(userId, user.IsInRole(Roller.Admin), vetId);
    }
}

/// <summary>Servis metotlarının sonucu. Hata nedenleri kullanıcıya gösterilecek Türkçe mesajlardır.</summary>
public class IslemSonucu
{
    public bool Basarili => Hatalar.Count == 0;
    public bool Bulunamadi { get; init; }
    public List<string> Hatalar { get; init; } = [];

    public static IslemSonucu Tamam() => new();
    public static IslemSonucu Hata(params string[] hatalar) => new() { Hatalar = [.. hatalar] };
    public static IslemSonucu BulunamadiHatasi(string mesaj) => new() { Bulunamadi = true, Hatalar = [mesaj] };
}

public class IslemSonucu<T> : IslemSonucu
{
    public T? Veri { get; init; }
    public static IslemSonucu<T> Tamam(T veri) => new() { Veri = veri };
    public new static IslemSonucu<T> Hata(params string[] hatalar) => new() { Hatalar = [.. hatalar] };
    public static IslemSonucu<T> Hata(List<string> hatalar) => new() { Hatalar = hatalar };
    public new static IslemSonucu<T> BulunamadiHatasi(string mesaj) => new() { Bulunamadi = true, Hatalar = [mesaj] };
}

/// <summary>Entity → DTO dönüşümleri.</summary>
public static class DtoDonusturucu
{
    public const string TarihBicimi = "yyyy-MM-dd";
    public const string TarihSaatBicimi = "yyyy-MM-dd HH:mm";
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Metin(this RandevuDurumu d) => d switch
    {
        RandevuDurumu.Bekliyor => "Bekliyor",
        RandevuDurumu.Onaylandi => "Onaylandı",
        RandevuDurumu.Reddedildi => "Reddedildi",
        RandevuDurumu.Tamamlandi => "Tamamlandı",
        RandevuDurumu.IptalEdildi => "İptal Edildi",
        _ => d.ToString()
    };

    public static string GunAdi(this DayOfWeek g) => CultureInfo.GetCultureInfo("tr-TR").DateTimeFormat.GetDayName(g);

    public static HizmetDto ToDto(this Hizmet h) =>
        new(h.Id, h.Ad, h.Aciklama, h.SureDakika, h.Ucret, h.Klinik?.Ad ?? "");

    public static VeterinerDto ToDto(this Veteriner v) =>
        new(v.Id, v.AdSoyad, v.Klinik?.Ad ?? "", v.UzmanlikListesi.ToList(),
            v.Hizmetler.Select(h => h.Ad).OrderBy(a => a).ToList());

    public static EvcilHayvanDto ToDto(this EvcilHayvan e) =>
        new(e.Id, e.Ad, e.Tur, e.Irk, e.DogumTarihi.ToString(TarihBicimi, Inv), e.Kilo);

    public static AsiDto ToDto(this AsiKaydi a) =>
        new(a.Id, a.AsiAdi, a.YapildigiTarih.ToString(TarihBicimi, Inv),
            a.SonrakiDozTarihi?.ToString(TarihBicimi, Inv), a.Notlar);

    public static RandevuDto ToDto(this Randevu r) =>
        new(r.Id, r.EvcilHayvan?.Ad ?? "", r.Hizmet?.Ad ?? "", r.Veteriner?.AdSoyad ?? "",
            r.Baslangic.ToString(TarihSaatBicimi, Inv), r.Bitis.ToString(TarihSaatBicimi, Inv),
            r.SureDakika, r.Ucret, r.Durum.Metin(), r.SahipNotu, r.MuayeneNotu, r.RedNedeni);
}
