using Microsoft.EntityFrameworkCore;
using VetKlinik.Data;
using VetKlinik.Dtos;
using VetKlinik.Models;

namespace VetKlinik.Services;

public interface IKatalogService
{
    Task<List<HizmetDto>> HizmetlerAsync(int? klinikId = null);
    Task<List<VeterinerDto>> VeterinerlerAsync(int? hizmetId = null, string? uzmanlik = null, int? klinikId = null);
    Task<VeterinerDto?> VeterinerAsync(int id);
    Task<HizmetRaporuDto> HizmetRaporuAsync(DateOnly? baslangic, DateOnly? bitis);
}

/// <summary>Herkese açık listeler ve admin raporu (LINQ filtreleme / gruplama).</summary>
public class KatalogService(AppDbContext db) : IKatalogService
{
    public async Task<List<HizmetDto>> HizmetlerAsync(int? klinikId = null)
    {
        var q = db.Hizmetler.Include(h => h.Klinik).AsQueryable();
        if (klinikId is not null) q = q.Where(h => h.KlinikId == klinikId);
        var liste = await q.OrderBy(h => h.Ad).ToListAsync();
        return liste.Select(h => h.ToDto()).ToList();
    }

    public async Task<List<VeterinerDto>> VeterinerlerAsync(int? hizmetId = null, string? uzmanlik = null, int? klinikId = null)
    {
        var q = db.Veterinerler.Include(v => v.Klinik).Include(v => v.Hizmetler).AsQueryable();

        // LINQ ile filtreleme: hizmet, uzmanlık alanı, klinik
        if (hizmetId is not null) q = q.Where(v => v.Hizmetler.Any(h => h.Id == hizmetId));
        if (!string.IsNullOrWhiteSpace(uzmanlik)) q = q.Where(v => v.UzmanlikAlanlari.Contains(uzmanlik));
        if (klinikId is not null) q = q.Where(v => v.KlinikId == klinikId);

        var liste = await q.OrderBy(v => v.AdSoyad).ToListAsync();
        return liste.Select(v => v.ToDto()).ToList();
    }

    public async Task<VeterinerDto?> VeterinerAsync(int id)
    {
        var v = await db.Veterinerler.Include(x => x.Klinik).Include(x => x.Hizmetler).FirstOrDefaultAsync(x => x.Id == id);
        return v?.ToDto();
    }

    /// <summary>Hizmet türüne göre randevu sayısı ve ciro (yalnızca tamamlanan randevular ciroya girer).</summary>
    public async Task<HizmetRaporuDto> HizmetRaporuAsync(DateOnly? baslangic, DateOnly? bitis)
    {
        var q = db.Randevular.AsQueryable();
        if (baslangic is { } b) q = q.Where(r => r.Baslangic >= b.ToDateTime(TimeOnly.MinValue));
        if (bitis is { } e) q = q.Where(r => r.Baslangic < e.AddDays(1).ToDateTime(TimeOnly.MinValue));

        var satirlar = await q
            .Where(r => r.Durum != RandevuDurumu.Reddedildi && r.Durum != RandevuDurumu.IptalEdildi)
            .GroupBy(r => new { r.HizmetId, r.Hizmet!.Ad })
            .Select(g => new
            {
                g.Key.HizmetId,
                g.Key.Ad,
                Sayi = g.Count(),
                Tamamlanan = g.Count(r => r.Durum == RandevuDurumu.Tamamlandi),
                Ciro = g.Where(r => r.Durum == RandevuDurumu.Tamamlandi).Sum(r => r.Ucret)
            })
            .ToListAsync();

        var dto = satirlar
            .OrderByDescending(s => s.Ciro).ThenByDescending(s => s.Sayi)
            .Select(s => new HizmetRaporSatiriDto(s.HizmetId, s.Ad, s.Sayi, s.Tamamlanan, s.Ciro))
            .ToList();

        return new HizmetRaporuDto(
            baslangic?.ToString(DtoDonusturucu.TarihBicimi) ?? "-",
            bitis?.ToString(DtoDonusturucu.TarihBicimi) ?? "-",
            dto.Sum(s => s.RandevuSayisi), dto.Sum(s => s.Ciro), dto);
    }
}
