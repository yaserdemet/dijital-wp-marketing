using System.ComponentModel.DataAnnotations;


namespace PushDashboard.ViewModels;

public class UserProfileViewModel
{
    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
    [RegularExpression(@"^[^0-9]*$", ErrorMessage = "{0} alanı sayı içeremez.")]
    [Display(Name = "Ad")]
    public string? FirstName { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
    [RegularExpression(@"^[^0-9]*$", ErrorMessage = "{0} alanı sayı içeremez.")]
    [Display(Name = "Soyad")]
    public string? LastName { get; set; }

    [Required]
    [EmailAddress]
    [Display(Name = "E-posta")]
    public string? Email { get; set; }

    [Phone]
    [Display(Name = "Telefon")]
    public string? PhoneNumber { get; set; }
}

public class CompanyProfileViewModel
{
    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 2)]
    [Display(Name = "Firma Adı")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    [Display(Name = "Adres")]
    public string? Address { get; set; }

    [StringLength(50)]
    [Display(Name = "Telefon")]
    public string? Phone { get; set; }

    [StringLength(100)]
    [Display(Name = "E-posta")]
    public string? Email { get; set; }

    [StringLength(200)]
    [Display(Name = "Website")]
    public string? Website { get; set; }

    // Custom validation method
    public bool IsValidEmail()
    {
        if (string.IsNullOrWhiteSpace(Email))
            return true; // Boş değer geçerli

        return new EmailAddressAttribute().IsValid(Email);
    }

    public bool IsValidWebsite()
    {
        if (string.IsNullOrWhiteSpace(Website))
            return true; // Boş değer geçerli

        return Uri.TryCreate(Website, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}

public class NotificationPreferencesViewModel
{
    public bool EmailInvoiceNotifications { get; set; } = true;
    public bool EmailCreditNotifications { get; set; } = true;
    public bool EmailMarketingNotifications { get; set; } = false;
    public bool SmsSecurityAlerts { get; set; } = true;
    public bool SmsPaymentNotifications { get; set; } = false;
}

public class UserListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string FullName => $"{FirstName} {LastName}".Trim();
    public string Status => IsActive ? "Aktif" : "Pasif";
    public string LastLoginDisplay
    {
        get
        {
            if (!LastLoginAt.HasValue)
                return "Hiç giriş yapmamış";
            return LastLoginAt.Value.ToString("dd.MM.yyyy HH:mm");
        }
    }
}

public class UserManagementViewModel
{
    public List<UserListItemViewModel> Users { get; set; } = new();
    public int TotalUsers { get; set; }
    public int PageSize { get; set; } = 10;
    public int CurrentPage { get; set; } = 1;
    public int TotalPages => (int)Math.Ceiling((double)TotalUsers / PageSize);
}

public class ActiveDeviceViewModel
{
    public int Id { get; set; }
    public string OperatingSystem { get; set; } = string.Empty;
    public string Browser { get; set; } = string.Empty;
    public string BrowserFamily { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty;
    public string LastIpAddress { get; set; } = string.Empty;
    public string LastLocation { get; set; } = string.Empty;
    public DateTime FirstSeenAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public bool IsCurrent { get; set; }
    public int SessionCount { get; set; }
    public string FormattedDeviceInfo => $"{Browser} - {OperatingSystem}";

    public string FormattedLocation => !string.IsNullOrEmpty(LastLocation) ? LastLocation : "Bilinmeyen Konum";

    public string FormattedLastActivity
    {
        get
        {
            var timeSpan = DateTime.Now - LastActivityAt;

            if (timeSpan.TotalMinutes < 1)
                return "Şu an aktif";
            else if (timeSpan.TotalMinutes < 60)
                return $"{(int)timeSpan.TotalMinutes} dakika önce";
            else if (timeSpan.TotalHours < 24)
                return $"{(int)timeSpan.TotalHours} saat önce";
            else if (timeSpan.TotalDays < 28)
                return $"{(int)timeSpan.TotalDays} gün önce";
            else
                return $"{(int)timeSpan.TotalDays} gün önce";
        }
    }

    public bool IsInactive
    {
        get
        {
            var timeSpan = DateTime.Now - LastActivityAt;
            return timeSpan.TotalDays > 28;
        }
    }

    public string StatusBadge
    {
        get
        {
            if (IsCurrent)
                return "Bu cihaz";

            var days = Math.Round((DateTime.Now - LastActivityAt).TotalDays);
            return IsInactive ? $"{days} gündür pasif" : "Aktif";
        }
    }

    public string StatusBadgeClass => IsCurrent ? "text-green-600" : (IsInactive ? "text-orange-600" : "text-blue-600");

    public string DisplayIpAddress
    {
        get
        {
            if (string.IsNullOrEmpty(LastIpAddress))
                return string.Empty;

            if (LastIpAddress == "127.0.0.1" ||
                LastIpAddress == "::1" ||
                LastIpAddress.StartsWith("192.168.") ||
                LastIpAddress.StartsWith("10.") ||
                LastIpAddress == "localhost")
            {
                return string.Empty;
            }

            return LastIpAddress;
        }
    }

    public string DeviceIcon
    {
        get
        {
            return DeviceType?.ToLower() switch
            {
                "mobile" => "📱",
                "tablet" => "📱",
                "desktop" => "💻",
                _ => "💻"
            };
        }
    }
}

public class ActiveDevicesViewModel
{
    public List<ActiveDeviceViewModel> Devices { get; set; } = new();
    public int TotalDevices { get; set; }
    public int ActiveDevices { get; set; }
}

public class SettingsViewModel
{
    public UserProfileViewModel UserProfile { get; set; } = new();
    public CompanyProfileViewModel CompanyProfile { get; set; } = new();
    public NotificationPreferencesViewModel NotificationPreferences { get; set; } = new();
    public bool CanEditCompany { get; set; }
    public bool CanManageUsers { get; set; }
    public string? CompanyName { get; set; }
    public TwoFactorStatusViewModel TwoFactorStatus { get; set; } = new();
    public ActiveDevicesViewModel ActiveDevices { get; set; } = new();
    public BillingInformationViewModel BillingInformation { get; set; } = new();
}


public class BillingInformationViewModel
{
    [Required(ErrorMessage = "Fatura türü seçimi zorunludur.")]
    [StringLength(20)]
    [Display(Name = "Fatura Türü")]
    public string BillingType { get; set; } = "Corporate";

    [StringLength(100, ErrorMessage = "Vergi dairesi en fazla 100 karakter olabilir.")]
    [Display(Name = "Vergi Dairesi")]
    public string? TaxOffice { get; set; }

    [StringLength(20, ErrorMessage = "Vergi numarası en fazla 20 karakter olabilir.")]
    [RegularExpression(@"^\d{10}$", ErrorMessage = "Vergi numarası 10 haneli sayı olmalıdır.")]
    [Display(Name = "Vergi Numarası")]
    public string? TaxNumber { get; set; }

    [StringLength(11, ErrorMessage = "TC Kimlik numarası 11 haneli olmalıdır.")]
    [RegularExpression(@"^\d{11}$", ErrorMessage = "TC Kimlik numarası 11 haneli sayı olmalıdır.")]
    [Display(Name = "TC Kimlik No")]
    public string? IdentityNumber { get; set; }

    [Required(ErrorMessage = "Fatura adresi zorunludur.")]
    [StringLength(500, ErrorMessage = "Fatura adresi en fazla 500 karakter olabilir.")]
    [MinLength(10, ErrorMessage = "Fatura adresi en az 10 karakter olmalıdır.")]
    [Display(Name = "Fatura Adresi")]
    public string BillingAddress { get; set; } = string.Empty;

    [StringLength(100, ErrorMessage = "Firma adı en fazla 100 karakter olabilir.")]
    [Display(Name = "Firma Adı")]
    public string? CompanyName { get; set; }

    [StringLength(100, ErrorMessage = "Ad soyad en fazla 100 karakter olabilir.")]
    [Display(Name = "Ad Soyad")]
    public string? FullName { get; set; }
    // Validation logic
    public bool IsIndividual => BillingType == "Individual";
    public bool IsCorporate => BillingType == "Corporate";
}
