using System.ComponentModel.DataAnnotations;

namespace PushDashboard.ViewModels;

public class TwoFactorSetupViewModel
{
    public string QrCodeUri { get; set; } = string.Empty;
    public string ManualEntryKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    
    [Required(ErrorMessage = "Doğrulama kodu gereklidir.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Doğrulama kodu 6 haneli olmalıdır.")]
    [Display(Name = "Doğrulama Kodu")]
    public string VerificationCode { get; set; } = string.Empty;
}

public class TwoFactorVerifyViewModel
{
    [Required(ErrorMessage = "Doğrulama kodu gereklidir.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Doğrulama kodu 6 haneli olmalıdır.")]
    [Display(Name = "Doğrulama Kodu")]
    public string Code { get; set; } = string.Empty;
    
    [Display(Name = "Bu cihazı hatırla")]
    public bool RememberMachine { get; set; }
    
    public string? ReturnUrl { get; set; }
}

public class TwoFactorLoginViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me?")]
    public bool RememberMe { get; set; }
    
    [Required(ErrorMessage = "Doğrulama kodu gereklidir.")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Doğrulama kodu 6 haneli olmalıdır.")]
    [Display(Name = "Doğrulama Kodu")]
    public string TwoFactorCode { get; set; } = string.Empty;
    
    [Display(Name = "Bu cihazı hatırla")]
    public bool RememberMachine { get; set; }
}

public class TwoFactorStatusViewModel
{
    public bool IsEnabled { get; set; }
    public bool HasRecoveryCodes { get; set; }
    public int RecoveryCodesLeft { get; set; }
    public bool IsMachineRemembered { get; set; }
}
