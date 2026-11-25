using System.ComponentModel.DataAnnotations;

namespace VetKlinik.Models;

public class AsiKaydi : IValidatableObject
{
    public int Id { get; set; }

    public int EvcilHayvanId { get; set; }
    public EvcilHayvan? EvcilHayvan { get; set; }

    [Required(ErrorMessage = "Aşı adı zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Aşı Adı")]
    public string AsiAdi { get; set; } = string.Empty;

    [Display(Name = "Yapıldığı Tarih")]
    [DataType(DataType.Date)]
    public DateOnly YapildigiTarih { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Sonraki Doz")]
    [DataType(DataType.Date)]
    public DateOnly? SonrakiDozTarihi { get; set; }

    [StringLength(300)]
    public string? Notlar { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (YapildigiTarih > DateOnly.FromDateTime(DateTime.Today))
            yield return new ValidationResult("Yapıldığı tarih gelecekte olamaz.", [nameof(YapildigiTarih)]);
        if (SonrakiDozTarihi is { } d && d <= YapildigiTarih)
            yield return new ValidationResult("Sonraki doz tarihi, yapıldığı tarihten sonra olmalıdır.", [nameof(SonrakiDozTarihi)]);
    }
}
