using System.Globalization;
using Microsoft.EntityFrameworkCore;
using VetKlinik.Data;
using VetKlinik.Dtos;
using VetKlinik.Models;

namespace VetKlinik.Services;

public interface IEvcilHayvanService
{
    IQueryable<EvcilHayvan> Sorgu(KullaniciBaglami k);
    Task<EvcilHayvan?> GetirAsync(int id, KullaniciBaglami k);
    bool DuzenleyebilirMi(EvcilHayvan e, KullaniciBaglami k);
    Task<EvcilHayvan?> AdaGoreBulAsync(string ad, KullaniciBaglami k);
    Task<AsiTakvimiDto?> AsiTakvimiAsync(int evcilHayvanId, KullaniciBaglami k);
    Task<AsiKaydi?> AsiGetirAsync(int asiId, KullaniciBaglami k);
}

/// <summary>Evcil hayvan ve aşı kayıtlarının erişim kuralları ile aşı takvimi hesabı.</summary>
public class EvcilHayvanService(AppDbContext db, TimeProvider saat) : IEvcilHayvanService
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>
    /// Görülebilen hayvanlar: Admin hepsini, üye yalnızca kendi hayvanlarını,
    /// veteriner kendi hayvanları + kendisinden randevu almış hayvanları görür.
    /// </summary>
    public IQueryable<EvcilHayvan> Sorgu(KullaniciBaglami k)
    {
        var q = db.EvcilHayvanlar.Include(e => e.Sahip).AsQueryable();
        if (k.IsAdmin) return q;
        if (k.IsVeteriner)
            return q.Where(e => e.SahipId == k.UserId || e.Randevular.Any(r => r.VeterinerId == k.VeterinerId));
        return q.Where(e => e.SahipId == k.UserId);
    }

    public Task<EvcilHayvan?> GetirAsync(int id, KullaniciBaglami k) =>
        Sorgu(k).FirstOrDefaultAsync(e => e.Id == id);

    /// <summary>Hayvan bilgilerini yalnızca sahibi veya admin değiştirebilir/silebilir.</summary>
    public bool DuzenleyebilirMi(EvcilHayvan e, KullaniciBaglami k) => k.IsAdmin || e.SahipId == k.UserId;

    public async Task<EvcilHayvan?> AdaGoreBulAsync(string ad, KullaniciBaglami k)
    {
        // Türkçe büyük/küçük harf duyarsız karşılaştırma (Pamuk = pamuk, İzmir = izmir) bellekte yapılır;
        // kullanıcının hayvan sayısı az olduğundan sorun değildir.
        var hayvanlar = await Sorgu(k).ToListAsync();
        ad = ad.Trim();
        return hayvanlar
            .OrderByDescending(e => e.SahipId == k.UserId) // önce kullanıcının kendi hayvanları
            .FirstOrDefault(e => string.Compare(e.Ad, ad, Tr, CompareOptions.IgnoreCase) == 0);
    }

    public async Task<AsiTakvimiDto?> AsiTakvimiAsync(int evcilHayvanId, KullaniciBaglami k)
    {
        var hayvan = await Sorgu(k).Include(e => e.AsiKayitlari).FirstOrDefaultAsync(e => e.Id == evcilHayvanId);
        if (hayvan is null) return null;

        var bugun = DateOnly.FromDateTime(saat.GetLocalNow().DateTime);
        var kayitlar = hayvan.AsiKayitlari.ToList();

        // Bir dozun "bekleyen" sayılması için aynı aşının daha sonra yapılmış bir kaydı olmamalı.
        bool SonrakiYapildiMi(AsiKaydi a) =>
            kayitlar.Any(x => x.Id != a.Id && x.AsiAdi == a.AsiAdi && x.YapildigiTarih > a.YapildigiTarih);

        var bekleyen = kayitlar.Where(a => a.SonrakiDozTarihi.HasValue && !SonrakiYapildiMi(a)).ToList();

        return new AsiTakvimiDto(
            hayvan.Id, hayvan.Ad,
            GecmisAsilar: kayitlar.OrderByDescending(a => a.YapildigiTarih).Select(a => a.ToDto()).ToList(),
            YaklasanDozlar: bekleyen.Where(a => a.SonrakiDozTarihi >= bugun)
                .OrderBy(a => a.SonrakiDozTarihi).Select(a => a.ToDto()).ToList(),
            GecikmisDozlar: bekleyen.Where(a => a.SonrakiDozTarihi < bugun)
                .OrderBy(a => a.SonrakiDozTarihi).Select(a => a.ToDto()).ToList());
    }

    /// <summary>Aşı kaydına, hayvanını görebilen kullanıcı (sahip, admin, ilgili hekim) erişebilir.</summary>
    public async Task<AsiKaydi?> AsiGetirAsync(int asiId, KullaniciBaglami k)
    {
        var asi = await db.AsiKayitlari.FirstOrDefaultAsync(a => a.Id == asiId);
        if (asi is null) return null;
        return await GetirAsync(asi.EvcilHayvanId, k) is null ? null : asi;
    }
}
