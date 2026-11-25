using System.ComponentModel.DataAnnotations;

namespace VetKlinik.Models;

public class Veteriner
{
    public int Id { get; set; }

    [Display(Name = "Klinik")]
    [Range(1, int.MaxValue, ErrorMessage = "Klinik seçin.")]
    public int KlinikId { get; set; }
    public Klinik? Klinik { get; set; }

    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Ad Soyad")]
    public string AdSoyad { get; set; } = string.Empty;

    /// <summary>Virgülle ayrılmış uzmanlık alanları, ör. "Küçük Hayvan, Cerrahi".</summary>
    [StringLength(300)]
    [Display(Name = "Uzmanlık Alanları")]
    public string UzmanlikAlanlari { get; set; } = string.Empty;

    [Phone, StringLength(20)]
    public string? Telefon { get; set; }

    /// <summary>Hekimin giriş yapabileceği Identity hesabı (Veteriner rolü).</summary>
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    /// <summary>Hekimin verebildiği hizmetler (çoka-çok).</summary>
    public ICollection<Hizmet> Hizmetler { get; set; } = new List<Hizmet>();
    public ICollection<Musaitlik> Musaitlikler { get; set; } = new List<Musaitlik>();
    public ICollection<Randevu> Randevular { get; set; } = new List<Randevu>();

    public IEnumerable<string> UzmanlikListesi =>
        UzmanlikAlanlari.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
