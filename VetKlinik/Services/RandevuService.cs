using System.Globalization;
using Microsoft.EntityFrameworkCore;
using VetKlinik.Data;
using VetKlinik.Dtos;
using VetKlinik.Models;

namespace VetKlinik.Services;

public interface IRandevuService
{
    IQueryable<Randevu> Sorgu(KullaniciBaglami k);
    Task<Randevu?> GetirAsync(int id, KullaniciBaglami k);
    Task<UygunSaatlerDto?> UygunSaatlerAsync(int hizmetId, DateOnly tarih, int? veterinerId = null);
    Task<IslemSonucu<Randevu>> OlusturAsync(RandevuOlusturDto talep, KullaniciBaglami k);
    Task<IslemSonucu> GuncelleAsync(int id, RandevuOlusturDto talep, KullaniciBaglami k);
    Task<IslemSonucu> OnaylaAsync(int id, KullaniciBaglami k);
    Task<IslemSonucu> ReddetAsync(int id, string? neden, KullaniciBaglami k);
    Task<IslemSonucu> TamamlaAsync(int id, string muayeneNotu, KullaniciBaglami k);
    Task<IslemSonucu> IptalEtAsync(int id, KullaniciBaglami k);
    Task<IslemSonucu> SilAsync(int id, KullaniciBaglami k);
}

/// <summary>
/// Randevu iş kurallarının TEK yeri. MVC controller'ları, REST API ve MCP araçları
/// randevu oluştururken / onaylarken hep bu sınıfı çağırır.
/// </summary>
public class RandevuService(AppDbContext db, TimeProvider saat) : IRandevuService
{
    /// <summary>Boş saat önerilerinde kullanılan adım (dakika).</summary>
    public const int SlotAdimDakika = 15;

    private DateTime Simdi => saat.GetLocalNow().DateTime;

    /// <summary>Kullanıcının görmeye yetkili olduğu randevular. Id değiştirerek başkasının randevusu görülemez.</summary>
    public IQueryable<Randevu> Sorgu(KullaniciBaglami k)
    {
        var q = db.Randevular
            .Include(r => r.EvcilHayvan).ThenInclude(e => e!.Sahip)
            .Include(r => r.Veteriner)
            .Include(r => r.Hizmet)
            .AsQueryable();

        if (k.IsAdmin) return q;
        if (k.IsVeteriner)
            return q.Where(r => r.VeterinerId == k.VeterinerId || r.EvcilHayvan!.SahipId == k.UserId);
        return q.Where(r => r.EvcilHayvan!.SahipId == k.UserId);
    }

    public Task<Randevu?> GetirAsync(int id, KullaniciBaglami k) =>
        Sorgu(k).FirstOrDefaultAsync(r => r.Id == id);

    public async Task<UygunSaatlerDto?> UygunSaatlerAsync(int hizmetId, DateOnly tarih, int? veterinerId = null)
    {
        var hizmet = await db.Hizmetler.Include(h => h.Klinik).FirstOrDefaultAsync(h => h.Id == hizmetId);
        if (hizmet is null) return null;

        var gun = tarih.DayOfWeek;
        var hekimler = await db.Veterinerler
            .Where(v => v.Hizmetler.Any(h => h.Id == hizmetId))
            .Where(v => veterinerId == null || v.Id == veterinerId)
            .Include(v => v.Klinik)
            .Include(v => v.Musaitlikler.Where(m => m.Gun == gun))
            .OrderBy(v => v.AdSoyad)
            .ToListAsync();

        var gunBasi = tarih.ToDateTime(TimeOnly.MinValue);
        var gunSonu = gunBasi.AddDays(1);
        var ids = hekimler.Select(h => h.Id).ToList();

        // O gün bu hekimlerin dolu (aktif) randevuları
        var dolu = await db.Randevular
            .Where(r => ids.Contains(r.VeterinerId)
                        && (r.Durum == RandevuDurumu.Bekliyor || r.Durum == RandevuDurumu.Onaylandi)
                        && r.Baslangic < gunSonu && r.Bitis > gunBasi)
            .Select(r => new { r.VeterinerId, r.Baslangic, r.Bitis })
            .ToListAsync();

        var sonuc = new List<UygunHekimDto>();
        foreach (var v in hekimler)
        {
            var bos = new List<SaatAraligiDto>();
            foreach (var m in v.Musaitlikler.OrderBy(m => m.BaslangicSaati))
            {
                // Hekimin müsaitliği ile kliniğin çalışma saatlerinin kesişimi
                var bas = tarih.ToDateTime(Max(m.BaslangicSaati, v.Klinik!.AcilisSaati));
                var bit = tarih.ToDateTime(Min(m.BitisSaati, v.Klinik!.KapanisSaati));

                for (var s = bas; s.AddMinutes(hizmet.SureDakika) <= bit; s = s.AddMinutes(SlotAdimDakika))
                {
                    var e = s.AddMinutes(hizmet.SureDakika);
                    if (s <= Simdi) continue;
                    // Aralık çakışması: [s,e) ile [r.Baslangic, r.Bitis) kesişiyor mu? (hizmet süresi dahil)
                    if (dolu.Any(r => r.VeterinerId == v.Id && r.Baslangic < e && r.Bitis > s)) continue;
                    bos.Add(new SaatAraligiDto(s.ToString("HH:mm"), e.ToString("HH:mm")));
                }
            }
            sonuc.Add(new UygunHekimDto(v.Id, v.AdSoyad, v.UzmanlikListesi.ToList(), bos));
        }

        return new UygunSaatlerDto(hizmet.Id, hizmet.Ad, hizmet.SureDakika, hizmet.Ucret,
            tarih.ToString(DtoDonusturucu.TarihBicimi, CultureInfo.InvariantCulture), sonuc);
    }

    public async Task<IslemSonucu<Randevu>> OlusturAsync(RandevuOlusturDto t, KullaniciBaglami k)
    {
        // Üye yalnızca kendi hayvanı için randevu alabilir (admin herkes için).
        var hayvan = await db.EvcilHayvanlar
            .FirstOrDefaultAsync(e => e.Id == t.EvcilHayvanId && (k.IsAdmin || e.SahipId == k.UserId));
        if (hayvan is null) return IslemSonucu<Randevu>.BulunamadiHatasi("Evcil hayvan bulunamadı.");

        var hizmet = await db.Hizmetler.FindAsync(t.HizmetId);
        if (hizmet is null) return IslemSonucu<Randevu>.BulunamadiHatasi("Hizmet bulunamadı.");

        var vet = await HekimGetirAsync(t.VeterinerId);
        if (vet is null) return IslemSonucu<Randevu>.BulunamadiHatasi("Veteriner hekim bulunamadı.");

        var bas = SaniyesizYap(t.Baslangic);
        var hatalar = await UygunlukHatalariAsync(vet, hizmet, hayvan.Id, bas, haricRandevuId: null);
        if (hatalar.Count > 0) return IslemSonucu<Randevu>.Hata(hatalar);

        var randevu = new Randevu
        {
            EvcilHayvanId = hayvan.Id,
            VeterinerId = vet.Id,
            HizmetId = hizmet.Id,
            Baslangic = bas,
            Bitis = bas.AddMinutes(hizmet.SureDakika),
            SureDakika = hizmet.SureDakika,
            Ucret = hizmet.Ucret,
            Durum = RandevuDurumu.Bekliyor, // Onay mekanizması: her randevu "Bekliyor" olarak başlar
            SahipNotu = t.Not,
            OlusturmaTarihi = Simdi
        };
        db.Randevular.Add(randevu);
        await db.SaveChangesAsync();

        randevu.EvcilHayvan = hayvan;
        randevu.Hizmet = hizmet;
        randevu.Veteriner = vet;
        return IslemSonucu<Randevu>.Tamam(randevu);
    }

    public async Task<IslemSonucu> GuncelleAsync(int id, RandevuOlusturDto t, KullaniciBaglami k)
    {
        var r = await db.Randevular.Include(x => x.EvcilHayvan).FirstOrDefaultAsync(x => x.Id == id);
        if (r is null || !(k.IsAdmin || r.EvcilHayvan!.SahipId == k.UserId))
            return IslemSonucu.BulunamadiHatasi("Randevu bulunamadı.");
        if (r.Durum != RandevuDurumu.Bekliyor)
            return IslemSonucu.Hata("Yalnızca onay bekleyen randevular değiştirilebilir.");

        var hayvan = await db.EvcilHayvanlar
            .FirstOrDefaultAsync(e => e.Id == t.EvcilHayvanId && (k.IsAdmin || e.SahipId == k.UserId));
        if (hayvan is null) return IslemSonucu.BulunamadiHatasi("Evcil hayvan bulunamadı.");
        var hizmet = await db.Hizmetler.FindAsync(t.HizmetId);
        if (hizmet is null) return IslemSonucu.BulunamadiHatasi("Hizmet bulunamadı.");
        var vet = await HekimGetirAsync(t.VeterinerId);
        if (vet is null) return IslemSonucu.BulunamadiHatasi("Veteriner hekim bulunamadı.");

        var bas = SaniyesizYap(t.Baslangic);
        var hatalar = await UygunlukHatalariAsync(vet, hizmet, hayvan.Id, bas, haricRandevuId: r.Id);
        if (hatalar.Count > 0) return new IslemSonucu { Hatalar = hatalar };

        r.EvcilHayvanId = hayvan.Id;
        r.HizmetId = hizmet.Id;
        r.VeterinerId = vet.Id;
        r.Baslangic = bas;
        r.Bitis = bas.AddMinutes(hizmet.SureDakika);
        r.SureDakika = hizmet.SureDakika;
        r.Ucret = hizmet.Ucret;
        r.SahipNotu = t.Not;
        await db.SaveChangesAsync();
        return IslemSonucu.Tamam();
    }

    public async Task<IslemSonucu> OnaylaAsync(int id, KullaniciBaglami k)
    {
        var (r, hata) = await HekimIslemiIcinGetirAsync(id, k);
        if (r is null) return hata!;
        if (r.Durum != RandevuDurumu.Bekliyor) return IslemSonucu.Hata("Yalnızca bekleyen randevular onaylanabilir.");
        r.Durum = RandevuDurumu.Onaylandi;
        await db.SaveChangesAsync();
        return IslemSonucu.Tamam();
    }

    public async Task<IslemSonucu> ReddetAsync(int id, string? neden, KullaniciBaglami k)
    {
        var (r, hata) = await HekimIslemiIcinGetirAsync(id, k);
        if (r is null) return hata!;
        if (r.Durum != RandevuDurumu.Bekliyor) return IslemSonucu.Hata("Yalnızca bekleyen randevular reddedilebilir.");
        r.Durum = RandevuDurumu.Reddedildi;
        r.RedNedeni = string.IsNullOrWhiteSpace(neden) ? null : neden.Trim();
        await db.SaveChangesAsync();
        return IslemSonucu.Tamam();
    }

    public async Task<IslemSonucu> TamamlaAsync(int id, string muayeneNotu, KullaniciBaglami k)
    {
        var (r, hata) = await HekimIslemiIcinGetirAsync(id, k);
        if (r is null) return hata!;
        if (r.Durum is not (RandevuDurumu.Onaylandi or RandevuDurumu.Tamamlandi))
            return IslemSonucu.Hata("Yalnızca onaylanmış randevular tamamlanabilir.");
        if (string.IsNullOrWhiteSpace(muayeneNotu))
            return IslemSonucu.Hata("Muayene notu boş olamaz.");
        r.Durum = RandevuDurumu.Tamamlandi;
        r.MuayeneNotu = muayeneNotu.Trim();
        await db.SaveChangesAsync();
        return IslemSonucu.Tamam();
    }

    public async Task<IslemSonucu> IptalEtAsync(int id, KullaniciBaglami k)
    {
        var r = await db.Randevular.Include(x => x.EvcilHayvan).FirstOrDefaultAsync(x => x.Id == id);
        if (r is null || !(k.IsAdmin || r.EvcilHayvan!.SahipId == k.UserId))
            return IslemSonucu.BulunamadiHatasi("Randevu bulunamadı.");
        if (r.Durum is not (RandevuDurumu.Bekliyor or RandevuDurumu.Onaylandi))
            return IslemSonucu.Hata("Bu randevu iptal edilemez.");
        if (r.Baslangic <= Simdi) return IslemSonucu.Hata("Başlamış/geçmiş randevu iptal edilemez.");
        r.Durum = RandevuDurumu.IptalEdildi;
        await db.SaveChangesAsync();
        return IslemSonucu.Tamam();
    }

    public async Task<IslemSonucu> SilAsync(int id, KullaniciBaglami k)
    {
        var r = await db.Randevular.Include(x => x.EvcilHayvan).FirstOrDefaultAsync(x => x.Id == id);
        if (r is null || !(k.IsAdmin || r.EvcilHayvan!.SahipId == k.UserId))
            return IslemSonucu.BulunamadiHatasi("Randevu bulunamadı.");
        // Üye, tamamlanmış ya da onaylanmış randevuyu silemez (kayıt bütünlüğü); admin silebilir.
        if (!k.IsAdmin && r.Durum is RandevuDurumu.Tamamlandi or RandevuDurumu.Onaylandi)
            return IslemSonucu.Hata("Onaylanmış veya tamamlanmış randevular silinemez; onaylı randevuyu iptal edebilirsiniz.");
        db.Randevular.Remove(r);
        await db.SaveChangesAsync();
        return IslemSonucu.Tamam();
    }

    // ---------------- yardımcılar ----------------

    private Task<Veteriner?> HekimGetirAsync(int id) => db.Veterinerler
        .Include(v => v.Klinik).Include(v => v.Hizmetler).Include(v => v.Musaitlikler)
        .FirstOrDefaultAsync(v => v.Id == id);

    /// <summary>Onay/red/tamamlama yalnızca Admin veya randevunun kendi hekimi tarafından yapılabilir.</summary>
    private async Task<(Randevu?, IslemSonucu?)> HekimIslemiIcinGetirAsync(int id, KullaniciBaglami k)
    {
        var r = await db.Randevular.FirstOrDefaultAsync(x => x.Id == id);
        if (r is null || !(k.IsAdmin || (k.IsVeteriner && r.VeterinerId == k.VeterinerId)))
            return (null, IslemSonucu.BulunamadiHatasi("Randevu bulunamadı."));
        return (r, null);
    }

    /// <summary>
    /// Randevu saatinin uygunluğunu denetler ve uygun değilse NEDENİNİ döndürür.
    /// Hizmet süresi çakışma hesabına dahildir: [başlangıç, başlangıç+süre) aralığı kontrol edilir.
    /// </summary>
    private async Task<List<string>> UygunlukHatalariAsync(Veteriner vet, Hizmet hizmet, int hayvanId,
        DateTime bas, int? haricRandevuId)
    {
        var hatalar = new List<string>();
        var bitis = bas.AddMinutes(hizmet.SureDakika);

        if (bas <= Simdi) hatalar.Add("Geçmiş bir zamana randevu alınamaz.");

        if (!vet.Hizmetler.Any(h => h.Id == hizmet.Id))
            hatalar.Add($"{vet.AdSoyad} \"{hizmet.Ad}\" hizmetini vermiyor.");

        if (bitis.Date != bas.Date)
            hatalar.Add("Randevu aynı gün içinde bitmelidir.");

        var t1 = TimeOnly.FromDateTime(bas);
        var t2 = TimeOnly.FromDateTime(bitis);
        var klinik = vet.Klinik!;
        if (t1 < klinik.AcilisSaati || t2 > klinik.KapanisSaati || bitis.Date != bas.Date)
            hatalar.Add($"Klinik çalışma saatleri {klinik.AcilisSaati:HH\\:mm}-{klinik.KapanisSaati:HH\\:mm} arasındadır " +
                        $"(hizmet {hizmet.SureDakika} dk sürer, {bitis:HH:mm}'de biter).");

        var gunAraliklari = vet.Musaitlikler.Where(m => m.Gun == bas.DayOfWeek).OrderBy(m => m.BaslangicSaati).ToList();
        if (gunAraliklari.Count == 0)
            hatalar.Add($"{vet.AdSoyad} {bas.DayOfWeek.GunAdi()} günleri çalışmıyor.");
        else if (!gunAraliklari.Any(m => m.BaslangicSaati <= t1 && m.BitisSaati >= t2 && bitis.Date == bas.Date))
            hatalar.Add($"{vet.AdSoyad} bu saatte müsait değil. {bas.DayOfWeek.GunAdi()} müsaitlik: " +
                        string.Join(", ", gunAraliklari.Select(m => $"{m.BaslangicSaati:HH\\:mm}-{m.BitisSaati:HH\\:mm}")) +
                        $" (hizmet {hizmet.SureDakika} dk).");

        // Hekimin aktif randevularıyla çakışma
        var cakisan = await db.Randevular
            .Where(r => r.VeterinerId == vet.Id && r.Id != (haricRandevuId ?? 0)
                        && (r.Durum == RandevuDurumu.Bekliyor || r.Durum == RandevuDurumu.Onaylandi)
                        && r.Baslangic < bitis && r.Bitis > bas)
            .OrderBy(r => r.Baslangic)
            .FirstOrDefaultAsync();
        if (cakisan is not null)
            hatalar.Add($"{vet.AdSoyad} {cakisan.Baslangic:HH:mm}-{cakisan.Bitis:HH:mm} arasında başka bir randevuda " +
                        $"(istenen aralık {bas:HH:mm}-{bitis:HH:mm}, hizmet süresi dahil).");

        // Aynı hayvanın aynı anda iki randevusu olamaz
        var hayvanCakisan = await db.Randevular.AnyAsync(r => r.EvcilHayvanId == hayvanId
            && r.Id != (haricRandevuId ?? 0)
            && (r.Durum == RandevuDurumu.Bekliyor || r.Durum == RandevuDurumu.Onaylandi)
            && r.Baslangic < bitis && r.Bitis > bas);
        if (hayvanCakisan) hatalar.Add("Evcil hayvanın bu saatle çakışan başka bir randevusu var.");

        return hatalar;
    }

    private static DateTime SaniyesizYap(DateTime d) => new(d.Year, d.Month, d.Day, d.Hour, d.Minute, 0, DateTimeKind.Unspecified);
    private static TimeOnly Max(TimeOnly a, TimeOnly b) => a > b ? a : b;
    private static TimeOnly Min(TimeOnly a, TimeOnly b) => a < b ? a : b;
}
