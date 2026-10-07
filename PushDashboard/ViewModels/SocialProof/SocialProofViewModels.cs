using System.ComponentModel.DataAnnotations;
using PushDashboard.Models;

namespace PushDashboard.ViewModels.SocialProof;

public class SocialProofIndexViewModel
{
    public SocialProofSettings Settings { get; set; } = new();
    public string? ScriptUrl { get; set; }
}

public class UpdateSocialProofSettingsViewModel
{
    [Required]
    public bool IsActive { get; set; } = true;

    public bool IsViewersActive { get; set; } = true;

    [Required]
    [Range(1, 1000, ErrorMessage = "İnceleyenler minimum değeri 1-1000 arasında olmalıdır.")]
    public int ViewersMin { get; set; } = 15;

    [Required]
    [Range(1, 1000, ErrorMessage = "İnceleyenler maksimum değeri 1-1000 arasında olmalıdır.")]
    public int ViewersMax { get; set; } = 45;

    public bool IsFollowersActive { get; set; } = true;

    [Required]
    [Range(1, 1000, ErrorMessage = "Takip edenler minimum değeri 1-1000 arasında olmalıdır.")]
    public int FollowersMin { get; set; } = 5;

    [Required]
    [Range(1, 1000, ErrorMessage = "Takip edenler maksimum değeri 1-1000 arasında olmalıdır.")]
    public int FollowersMax { get; set; } = 20;

    public bool IsBuyersActive { get; set; } = true;

    [Required]
    [Range(1, 1000, ErrorMessage = "Satın alanlar minimum değeri 1-1000 arasında olmalıdır.")]
    public int BuyersMin { get; set; } = 2;

    [Required]
    [Range(1, 1000, ErrorMessage = "Satın alanlar maksimum değeri 1-1000 arasında olmalıdır.")]
    public int BuyersMax { get; set; } = 8;

    [Required]
    [Range(30, 300, ErrorMessage = "Güncelleme sıklığı 30-300 saniye arasında olmalıdır.")]
    public int UpdateInterval { get; set; } = 60;

    [Required]
    [Range(3, 15, ErrorMessage = "Gösterim süresi 3-15 saniye arasında olmalıdır.")]
    public int DisplayDuration { get; set; } = 5;

    // Metin şablonları
    [Required]
    [StringLength(200, ErrorMessage = "İnceleyenler metni en fazla 200 karakter olabilir.")]
    public string ViewersTemplate { get; set; } = "{count} kişi şu anda bu ürünü inceliyor";

    [Required]
    [StringLength(200, ErrorMessage = "Takip edenler metni en fazla 200 karakter olabilir.")]
    public string FollowersTemplate { get; set; } = "{count} kişi bu ürünü takip ediyor";

    [Required]
    [StringLength(200, ErrorMessage = "Satın alanlar metni en fazla 200 karakter olabilir.")]
    public string BuyersTemplate { get; set; } = "{count} kişi satın almaya hazırlanıyor";

    // Görsel ayarlar
    [Required]
    public string Position { get; set; } = "bottom-left";

    [Required]
    [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Ana renk geçerli bir hex renk kodu olmalıdır.")]
    public string PrimaryColor { get; set; } = "#007bff";

    [Required]
    [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Metin rengi geçerli bir hex renk kodu olmalıdır.")]
    public string TextColor { get; set; } = "#ffffff";

    [Required]
    [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "Arka plan rengi geçerli bir hex renk kodu olmalıdır.")]
    public string BackgroundColor { get; set; } = "#333333";

    [Required]
    [Range(0, 50, ErrorMessage = "Border radius 0-50 px arasında olmalıdır.")]
    public int BorderRadius { get; set; } = 8;

    [Required]
    [Range(10, 24, ErrorMessage = "Font boyutu 10-24 px arasında olmalıdır.")]
    public int FontSize { get; set; } = 14;

    public bool EnableAnimation { get; set; } = true;
    public bool EnableShadow { get; set; } = true;

    // Validation method
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ViewersMin >= ViewersMax)
        {
            yield return new ValidationResult("İnceleyenler minimum değeri maksimum değerden küçük olmalıdır.", 
                new[] { nameof(ViewersMin), nameof(ViewersMax) });
        }

        if (FollowersMin >= FollowersMax)
        {
            yield return new ValidationResult("Takip edenler minimum değeri maksimum değerden küçük olmalıdır.", 
                new[] { nameof(FollowersMin), nameof(FollowersMax) });
        }

        if (BuyersMin >= BuyersMax)
        {
            yield return new ValidationResult("Satın alanlar minimum değeri maksimum değerden küçük olmalıdır.", 
                new[] { nameof(BuyersMin), nameof(BuyersMax) });
        }

        if (!ViewersTemplate.Contains("{count}"))
        {
            yield return new ValidationResult("İnceleyenler metni {count} placeholder'ını içermelidir.", 
                new[] { nameof(ViewersTemplate) });
        }

        if (!FollowersTemplate.Contains("{count}"))
        {
            yield return new ValidationResult("Takip edenler metni {count} placeholder'ını içermelidir.", 
                new[] { nameof(FollowersTemplate) });
        }

        if (!BuyersTemplate.Contains("{count}"))
        {
            yield return new ValidationResult("Satın alanlar metni {count} placeholder'ını içermelidir.", 
                new[] { nameof(BuyersTemplate) });
        }

        var validPositions = new[] { "bottom-left", "bottom-right", "top-left", "top-right" };
        if (!validPositions.Contains(Position))
        {
            yield return new ValidationResult("Geçersiz pozisyon değeri.", new[] { nameof(Position) });
        }
    }
}
