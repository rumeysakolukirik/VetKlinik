using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VetKlinik.Models;

namespace VetKlinik.Data;

/// <summary>Uygulama açılışında migration'ları uygular, rolleri/admini ve örnek verileri oluşturur.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        await db.Database.MigrateAsync();

        foreach (var rol in new[] { Roller.Admin, Roller.Veteriner, Roller.Uye })
            if (!await roleManager.RoleExistsAsync(rol))
                await roleManager.CreateAsync(new IdentityRole(rol));

        // Ödevde istenen admin hesabı: ogrencinumarasi@sakarya.edu.tr / sau (appsettings.json'dan)
        var adminEmail = config["Seed:AdminEmail"] ?? "admin@sakarya.edu.tr";
        var adminSifre = config["Seed:AdminSifre"] ?? "sau";
        await KullaniciOlusturAsync(userManager, adminEmail, adminSifre, "Sistem Yöneticisi", Roller.Admin);

        if (await db.Klinikler.AnyAsync()) return; // örnek veriler zaten var

        var klinik = new Klinik
        {
            Ad = "Pati Veteriner Kliniği", Adres = "Serdivan, Sakarya", Telefon = "0264 000 00 00",
            AcilisSaati = new(9, 0), KapanisSaati = new(18, 0)
        };
        var muayene = new Hizmet { Klinik = klinik, Ad = "Genel Muayene", SureDakika = 30, Ucret = 750, Aciklama = "Genel sağlık kontrolü" };
        var asi = new Hizmet { Klinik = klinik, Ad = "Aşı Uygulaması", SureDakika = 15, Ucret = 600, Aciklama = "Karma, kuduz, iç-dış parazit" };
        var kisir = new Hizmet { Klinik = klinik, Ad = "Kısırlaştırma", SureDakika = 120, Ucret = 6500, Aciklama = "Genel anestezi altında operasyon" };
        var dis = new Hizmet { Klinik = klinik, Ad = "Diş Temizliği", SureDakika = 60, Ucret = 2500 };
        var tiras = new Hizmet { Klinik = klinik, Ad = "Tıraş ve Bakım", SureDakika = 45, Ucret = 900 };
        var deri = new Hizmet { Klinik = klinik, Ad = "Dermatoloji Muayenesi", SureDakika = 30, Ucret = 1000 };
        var egzotik = new Hizmet { Klinik = klinik, Ad = "Egzotik Hayvan Muayenesi", SureDakika = 40, Ucret = 1100 };

        var v1User = await KullaniciOlusturAsync(userManager, "ayse.vet@vetklinik.com", "vet123", "Vet. Hek. Ayşe Yılmaz", Roller.Veteriner);
        var v2User = await KullaniciOlusturAsync(userManager, "mehmet.vet@vetklinik.com", "vet123", "Vet. Hek. Mehmet Demir", Roller.Veteriner);
        var v3User = await KullaniciOlusturAsync(userManager, "zeynep.vet@vetklinik.com", "vet123", "Vet. Hek. Zeynep Kaya", Roller.Veteriner);

        var v1 = new Veteriner { Klinik = klinik, AdSoyad = "Vet. Hek. Ayşe Yılmaz", UzmanlikAlanlari = "Küçük Hayvan, Cerrahi", UserId = v1User.Id, Hizmetler = { muayene, asi, kisir } };
        var v2 = new Veteriner { Klinik = klinik, AdSoyad = "Vet. Hek. Mehmet Demir", UzmanlikAlanlari = "Dermatoloji, Bakım, Diş Sağlığı", UserId = v2User.Id, Hizmetler = { muayene, deri, tiras, dis } };
        var v3 = new Veteriner { Klinik = klinik, AdSoyad = "Vet. Hek. Zeynep Kaya", UzmanlikAlanlari = "Egzotik Hayvan, Küçük Hayvan", UserId = v3User.Id, Hizmetler = { muayene, asi, egzotik } };

        // Hafta içi 09:00-12:00 ve 13:00-17:00; Zeynep Hanım cumartesi de çalışıyor
        foreach (var v in new[] { v1, v2, v3 })
            for (var g = DayOfWeek.Monday; g <= DayOfWeek.Friday; g++)
            {
                v.Musaitlikler.Add(new Musaitlik { Gun = g, BaslangicSaati = new(9, 0), BitisSaati = new(12, 0) });
                v.Musaitlikler.Add(new Musaitlik { Gun = g, BaslangicSaati = new(13, 0), BitisSaati = new(17, 0) });
            }
        v3.Musaitlikler.Add(new Musaitlik { Gun = DayOfWeek.Saturday, BaslangicSaati = new(10, 0), BitisSaati = new(14, 0) });

        db.AddRange(klinik, v1, v2, v3);

        var uye = await KullaniciOlusturAsync(userManager, "uye@ornek.com", "uye123", "Elif Şahin", Roller.Uye);
        var uye2 = await KullaniciOlusturAsync(userManager, "uye2@ornek.com", "uye123", "Can Arslan", Roller.Uye);

        var bugun = DateOnly.FromDateTime(DateTime.Today);
        var pamuk = new EvcilHayvan { SahipId = uye.Id, Ad = "Pamuk", Tur = "Kedi", Irk = "Van Kedisi", DogumTarihi = bugun.AddYears(-3).AddDays(-40), Kilo = 4.2 };
        var karabas = new EvcilHayvan { SahipId = uye.Id, Ad = "Karabaş", Tur = "Köpek", Irk = "Golden Retriever", DogumTarihi = bugun.AddYears(-5), Kilo = 31.5 };
        var boncuk = new EvcilHayvan { SahipId = uye2.Id, Ad = "Boncuk", Tur = "Tavşan", Irk = "Hollanda Lop", DogumTarihi = bugun.AddYears(-1), Kilo = 1.8 };

        pamuk.AsiKayitlari.Add(new AsiKaydi { AsiAdi = "Karma Aşı (FVRCP)", YapildigiTarih = bugun.AddMonths(-11), SonrakiDozTarihi = bugun.AddMonths(1) });
        pamuk.AsiKayitlari.Add(new AsiKaydi { AsiAdi = "Kuduz", YapildigiTarih = bugun.AddMonths(-12).AddDays(-10), SonrakiDozTarihi = bugun.AddDays(-10) });
        pamuk.AsiKayitlari.Add(new AsiKaydi { AsiAdi = "İç Parazit", YapildigiTarih = bugun.AddMonths(-2), SonrakiDozTarihi = bugun.AddDays(20) });
        karabas.AsiKayitlari.Add(new AsiKaydi { AsiAdi = "Karma Aşı (DHPPi+L)", YapildigiTarih = bugun.AddMonths(-6), SonrakiDozTarihi = bugun.AddMonths(6) });
        karabas.AsiKayitlari.Add(new AsiKaydi { AsiAdi = "Kuduz", YapildigiTarih = bugun.AddMonths(-3), SonrakiDozTarihi = bugun.AddMonths(9) });
        boncuk.AsiKayitlari.Add(new AsiKaydi { AsiAdi = "RHD", YapildigiTarih = bugun.AddMonths(-4), SonrakiDozTarihi = bugun.AddMonths(8) });

        db.AddRange(pamuk, karabas, boncuk);

        // Geçmiş (tamamlanmış) randevular: rapor ve muayene notu örneği için
        Randevu Gecmis(EvcilHayvan e, Veteriner v, Hizmet h, int gunOnce, int saat, string not) => new()
        {
            EvcilHayvan = e, Veteriner = v, Hizmet = h,
            Baslangic = DateTime.Today.AddDays(-gunOnce).AddHours(saat),
            Bitis = DateTime.Today.AddDays(-gunOnce).AddHours(saat).AddMinutes(h.SureDakika),
            SureDakika = h.SureDakika, Ucret = h.Ucret, Durum = RandevuDurumu.Tamamlandi, MuayeneNotu = not,
            OlusturmaTarihi = DateTime.Today.AddDays(-gunOnce - 3)
        };
        db.Randevular.AddRange(
            Gecmis(pamuk, v1, muayene, 60, 10, "Genel durum iyi. Tüy kalitesi normal, kilo ideal."),
            Gecmis(pamuk, v3, asi, 60, 14, "İç parazit uygulaması yapıldı."),
            Gecmis(karabas, v2, deri, 30, 11, "Hafif alerjik dermatit. Şampuan önerildi, 2 hafta sonra kontrol."),
            Gecmis(karabas, v2, tiras, 20, 15, "Yaz tıraşı yapıldı."),
            Gecmis(boncuk, v3, egzotik, 15, 10, "Diş uzunlukları normal, saman ağırlıklı beslenme önerildi."));

        // Gelecekteki bekleyen randevu (onay mekanizması demosu)
        var ileri = SonrakiIsGunu(DateTime.Today.AddDays(2)).AddHours(10);
        db.Randevular.Add(new Randevu
        {
            EvcilHayvan = pamuk, Veteriner = v1, Hizmet = muayene, Baslangic = ileri, Bitis = ileri.AddMinutes(30),
            SureDakika = 30, Ucret = muayene.Ucret, Durum = RandevuDurumu.Bekliyor, SahipNotu = "Son günlerde iştahsız."
        });

        await db.SaveChangesAsync();
    }

    private static DateTime SonrakiIsGunu(DateTime d)
    {
        while (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) d = d.AddDays(1);
        return d.Date;
    }

    private static async Task<ApplicationUser> KullaniciOlusturAsync(UserManager<ApplicationUser> um,
        string email, string sifre, string adSoyad, string rol)
    {
        var user = await um.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, AdSoyad = adSoyad };
            var sonuc = await um.CreateAsync(user, sifre);
            if (!sonuc.Succeeded)
                throw new InvalidOperationException("Seed kullanıcısı oluşturulamadı: " +
                                                    string.Join(", ", sonuc.Errors.Select(e => e.Description)));
        }
        if (!await um.IsInRoleAsync(user, rol)) await um.AddToRoleAsync(user, rol);
        return user;
    }
}
