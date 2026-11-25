using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VetKlinik.Models;

/// <summary>Klinikte verilen hizmet (muayene, aşı, kısırlaştırma...). Süre çakışma hesabında kullanılır.</summary>
public class Hizmet
{
    public int Id { get; set; }

    [Display(Name = "Klinik")]
    [Range(1, int.MaxValue, ErrorMessage = "Klinik seçin.")]
    public int KlinikId { get; set; }
    public Klinik? Klinik { get; set; }

    [Required(ErrorMessage = "Hizmet adı zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Hizmet Adı")]
    public string Ad { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Açıklama")]
    public string? Aciklama { get; set; }

    [Range(5, 480, ErrorMessage = "Süre 5 ile 480 dakika arasında olmalıdır.")]
    [Display(Name = "Süre (dk)")]
    public int SureDakika { get; set; } = 30;

    [Range(0, 100000, ErrorMessage = "Ücret 0 ile 100.000 arasında olmalıdır.")]
    [Column(TypeName = "decimal(10,2)")]
    [Display(Name = "Ücret (₺)")]
    public decimal Ucret { get; set; }

    public ICollection<Veteriner> Veterinerler { get; set; } = new List<Veteriner>();
}
