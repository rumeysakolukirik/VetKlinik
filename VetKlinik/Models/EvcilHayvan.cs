using System.ComponentModel.DataAnnotations;

namespace VetKlinik.Models;

public class EvcilHayvan : IValidatableObject
{
    public int Id { get; set; }

    /// <summary>Sahibin Identity kullanıcı Id'si. Yetki kontrolleri bu alana göre yapılır.</summary>
    public string SahipId { get; set; } = string.Empty;
    public ApplicationUser? Sahip { get; set; }

    [Required(ErrorMessage = "Ad zorunludur.")]
    [StringLength(50)]
    public string Ad { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tür seçin.")]
    [StringLength(30)]
    [Display(Name = "Tür")]
    public string Tur { get; set; } = string.Empty;

    [StringLength(50)]
    [Display(Name = "Irk")]
    public string? Irk { get; set; }

    [Display(Name = "Doğum Tarihi")]
    [DataType(DataType.Date)]
    public DateOnly DogumTarihi { get; set; }

    [Range(0.01, 500, ErrorMessage = "Kilo 0,01 ile 500 kg arasında olmalıdır.")]
    [Display(Name = "Kilo (kg)")]
    public double Kilo { get; set; }

    [StringLength(200)]
    public string? FotografYolu { get; set; }

    public ICollection<AsiKaydi> AsiKayitlari { get; set; } = new List<AsiKaydi>();
    public ICollection<Randevu> Randevular { get; set; } = new List<Randevu>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DogumTarihi > DateOnly.FromDateTime(DateTime.Today))
            yield return new ValidationResult("Doğum tarihi gelecekte olamaz.", [nameof(DogumTarihi)]);
    }
}
