using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VetKlinik.Models;

/// <summary>
/// Randevu. Süre ve ücret, randevu alındığı andaki hizmetten kopyalanır;
/// böylece hizmet fiyatı sonradan değişse de geçmiş randevu/ciro bilgisi bozulmaz.
/// </summary>
public class Randevu
{
    public int Id { get; set; }

    public int EvcilHayvanId { get; set; }
    public EvcilHayvan? EvcilHayvan { get; set; }

    public int VeterinerId { get; set; }
    public Veteriner? Veteriner { get; set; }

    public int HizmetId { get; set; }
    public Hizmet? Hizmet { get; set; }

    [Display(Name = "Başlangıç")]
    public DateTime Baslangic { get; set; }

    /// <summary>Başlangıç + hizmet süresi. Çakışma sorgusu bu sütun üzerinden yapılır.</summary>
    [Display(Name = "Bitiş")]
    public DateTime Bitis { get; set; }

    [Display(Name = "Süre (dk)")]
    public int SureDakika { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    [Display(Name = "Ücret (₺)")]
    public decimal Ucret { get; set; }

    public RandevuDurumu Durum { get; set; } = RandevuDurumu.Bekliyor;

    [StringLength(500)]
    [Display(Name = "Sahip Notu")]
    public string? SahipNotu { get; set; }

    [StringLength(2000)]
    [Display(Name = "Muayene Notu")]
    public string? MuayeneNotu { get; set; }

    [StringLength(300)]
    [Display(Name = "Red Nedeni")]
    public string? RedNedeni { get; set; }

    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
}
