using System.ComponentModel.DataAnnotations;

namespace PushDashboard.ViewModels;

public class SendInvitationRequest
{
    [Required(ErrorMessage = "E-posta adresi gereklidir.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    [StringLength(256, ErrorMessage = "E-posta adresi en fazla 256 karakter olabilir.")]
    public string Email { get; set; } = string.Empty;
}

public class InvitationListItemViewModel
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpirationDate { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = string.Empty;
    public bool IsExpired { get; set; }
    public bool IsValid { get; set; }
    public string TimeRemaining { get; set; } = string.Empty;
}

public class InvitationManagementViewModel
{
    public List<InvitationListItemViewModel> PendingInvitations { get; set; } = new();
    public int TotalInvitations { get; set; }
    public int PendingCount { get; set; }
    public int UsedCount { get; set; }
    public int ExpiredCount { get; set; }
    public int CurrentUserCount { get; set; }
    public int MaxUserLimit { get; set; } = 20;
    public bool CanSendMoreInvitations => (CurrentUserCount + PendingCount) < MaxUserLimit;
    public int RemainingSlots => MaxUserLimit - CurrentUserCount - PendingCount;
}

public class ValidateInvitationRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;
}

public class ValidateInvitationResponse
{
    public bool IsValid { get; set; }
    public string? CompanyName { get; set; }
    public string? InviterName { get; set; }
    public string? Message { get; set; }
    public DateTime? ExpirationDate { get; set; }
}

public class AcceptInvitationRequest
{
    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ad gereklidir.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Ad en az 2, en fazla 100 karakter olmalıdır.")]
    [RegularExpression(@"^[^0-9]*$", ErrorMessage = "{0} alanı sayı içeremez.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad gereklidir.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Soyad en az 2, en fazla 100 karakter olmalıdır.")]
    [RegularExpression(@"^[^0-9]*$", ErrorMessage = "{0} alanı sayı içeremez.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta adresi gereklidir.")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre gereklidir.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Şifre en az 6 karakter olmalıdır.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre onayı gereklidir.")]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Şifreler eşleşmiyor.")]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }
}

public class CancelInvitationRequest
{
    [Required]
    public int InvitationId { get; set; }
}
