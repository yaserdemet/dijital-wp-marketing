using System.ComponentModel.DataAnnotations;

namespace PushDashboard.ViewModels;

public class BillingPageViewModel
{
    // Company Logo
    public string? LogoUrl { get; set; }

    // Contact Person Information
    [Required(ErrorMessage = "Ad alanı zorunludur.")]
    [StringLength(100, ErrorMessage = "Ad en fazla 100 karakter olabilir.")]
    [Display(Name = "Ad")]
    public string ContactFirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad alanı zorunludur.")]
    [StringLength(100, ErrorMessage = "Soyad en fazla 100 karakter olabilir.")]
    [Display(Name = "Soyad")]
    public string ContactLastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta alanı zorunludur.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [Display(Name = "E-posta")]
    public string ContactEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefon alanı zorunludur.")]
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz.")]
    [Display(Name = "Telefon")]
    public string ContactPhone { get; set; } = string.Empty;

    // Address Information
    [Required(ErrorMessage = "İl seçimi zorunludur.")]
    [Display(Name = "İl")]
    public string Province { get; set; } = string.Empty;

    [Required(ErrorMessage = "İlçe seçimi zorunludur.")]
    [Display(Name = "İlçe")]
    public string District { get; set; } = string.Empty;

    [Required(ErrorMessage = "Adres satırı 1 zorunludur.")]
    [StringLength(200, ErrorMessage = "Adres en fazla 200 karakter olabilir.")]
    [Display(Name = "Adres Satırı 1")]
    public string AddressLine1 { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Adres en fazla 200 karakter olabilir.")]
    [Display(Name = "Adres Satırı 2 (Opsiyonel)")]
    public string? AddressLine2 { get; set; }

    // Billing Information
    [Required(ErrorMessage = "Şirket adı zorunludur.")]
    [StringLength(100, ErrorMessage = "Şirket adı en fazla 100 karakter olabilir.")]
    [Display(Name = "Şirket Adı")]
    public string CompanyName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vergi dairesi zorunludur.")]
    [StringLength(100, ErrorMessage = "Vergi dairesi en fazla 100 karakter olabilir.")]
    [Display(Name = "Vergi Dairesi")]
    public string TaxOffice { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şirket kategorisi seçimi zorunludur.")]
    [Display(Name = "Şirket Kategorisi")]
    public string CompanyCategory { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vergi numarası zorunludur.")]
    [StringLength(11, MinimumLength = 10, ErrorMessage = "Vergi numarası 10-11 haneli olmalıdır.")]
    [RegularExpression(@"^\d{10,11}$", ErrorMessage = "Vergi numarası sadece rakamlardan oluşmalıdır.")]
    [Display(Name = "Vergi Numarası")]
    public string TaxNumber { get; set; } = string.Empty;

    // Permissions
    public bool CanEdit { get; set; } = true;
}

public class SaveBillingInfoRequest
{
    public string? ContactFirstName { get; set; }
    public string? ContactLastName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Province { get; set; }
    public string? District { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? CompanyName { get; set; }
    public string? TaxOffice { get; set; }
    public string? CompanyCategory { get; set; }
    public string? TaxNumber { get; set; }
}
