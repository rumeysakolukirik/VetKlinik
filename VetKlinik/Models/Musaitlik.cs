using System.ComponentModel.DataAnnotations;

namespace VetKlinik.Models;

/// <summary>Hekimin haftalık çalışma aralığı. Randevular yalnızca bu aralıklara alınabilir.</summary>
public class Musaitlik : IValidatableObject
{
    public int Id { get; set; }

    public int VeterinerId { get; set; }
    public Veteriner? Veteriner { get; set; }

    [Display(Name = "Gün")]
    public DayOfWeek Gun { get; set; }

    [Display(Name = "Başlangıç")]
    public TimeOnly BaslangicSaati { get; set; }

    [Display(Name = "Bitiş")]
    public TimeOnly BitisSaati { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (BitisSaati <= BaslangicSaati)
            yield return new ValidationResult("Bitiş saati başlangıçtan sonra olmalıdır.", [nameof(BitisSaati)]);
    }
}
