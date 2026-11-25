using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace VetKlinik.Models;

/// <summary>Identity kullanıcısı. Hayvan sahipleri, veterinerler ve admin bu tabloda tutulur.</summary>
public class ApplicationUser : IdentityUser
{
    [Required, StringLength(100)]
    [Display(Name = "Ad Soyad")]
    public string AdSoyad { get; set; } = string.Empty;

    public ICollection<EvcilHayvan> EvcilHayvanlar { get; set; } = new List<EvcilHayvan>();
}
