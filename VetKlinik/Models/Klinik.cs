using System.ComponentModel.DataAnnotations;

namespace VetKlinik.Models;

public class Klinik : IValidatableObject
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Klinik adı zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Klinik Adı")]
    public string Ad { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Adres { get; set; }

    [Phone(ErrorMessage = "Geçerli bir telefon girin.")]
    [StringLength(20)]
    public string? Telefon { get; set; }

    [Display(Name = "Açılış Saati")]
    public TimeOnly AcilisSaati { get; set; } = new(9, 0);

    [Display(Name = "Kapanış Saati")]
    public TimeOnly KapanisSaati { get; set; } = new(18, 0);

    public ICollection<Hizmet> Hizmetler { get; set; } = new List<Hizmet>();
    public ICollection<Veteriner> Veterinerler { get; set; } = new List<Veteriner>();

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (KapanisSaati <= AcilisSaati)
            yield return new ValidationResult("Kapanış saati açılış saatinden sonra olmalıdır.", [nameof(KapanisSaati)]);
    }
}
