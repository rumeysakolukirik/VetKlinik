using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VetKlinik.Models;

namespace VetKlinik.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Klinik> Klinikler => Set<Klinik>();
    public DbSet<Hizmet> Hizmetler => Set<Hizmet>();
    public DbSet<Veteriner> Veterinerler => Set<Veteriner>();
    public DbSet<Musaitlik> Musaitlikler => Set<Musaitlik>();
    public DbSet<EvcilHayvan> EvcilHayvanlar => Set<EvcilHayvan>();
    public DbSet<AsiKaydi> AsiKayitlari => Set<AsiKaydi>();
    public DbSet<Randevu> Randevular => Set<Randevu>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        // Klinik silinirse hizmet/hekim yetim kalmasın: önce onların silinmesi gerekir.
        b.Entity<Hizmet>().HasOne(h => h.Klinik).WithMany(k => k.Hizmetler)
            .HasForeignKey(h => h.KlinikId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Veteriner>().HasOne(v => v.Klinik).WithMany(k => k.Veterinerler)
            .HasForeignKey(v => v.KlinikId).OnDelete(DeleteBehavior.Restrict);

        // Hekim <-> Hizmet çoka-çok (hekimin verebildiği hizmetler)
        b.Entity<Veteriner>().HasMany(v => v.Hizmetler).WithMany(h => h.Veterinerler)
            .UsingEntity(j => j.ToTable("VeterinerHizmetleri"));

        b.Entity<Veteriner>().HasOne(v => v.User).WithMany()
            .HasForeignKey(v => v.UserId).OnDelete(DeleteBehavior.SetNull);
        b.Entity<Veteriner>().HasIndex(v => v.UserId).IsUnique();

        b.Entity<Musaitlik>().HasOne(m => m.Veteriner).WithMany(v => v.Musaitlikler)
            .HasForeignKey(m => m.VeterinerId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<EvcilHayvan>().HasOne(e => e.Sahip).WithMany(u => u.EvcilHayvanlar)
            .HasForeignKey(e => e.SahipId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<AsiKaydi>().HasOne(a => a.EvcilHayvan).WithMany(e => e.AsiKayitlari)
            .HasForeignKey(a => a.EvcilHayvanId).OnDelete(DeleteBehavior.Cascade);

        // Geçmiş randevusu olan hekim/hizmet silinemez (ciro raporu bozulmasın).
        b.Entity<Randevu>().HasOne(r => r.EvcilHayvan).WithMany(e => e.Randevular)
            .HasForeignKey(r => r.EvcilHayvanId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<Randevu>().HasOne(r => r.Veteriner).WithMany(v => v.Randevular)
            .HasForeignKey(r => r.VeterinerId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Randevu>().HasOne(r => r.Hizmet).WithMany()
            .HasForeignKey(r => r.HizmetId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Randevu>().HasIndex(r => new { r.VeterinerId, r.Baslangic });
    }
}
