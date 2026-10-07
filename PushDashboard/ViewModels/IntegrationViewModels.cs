using System.ComponentModel.DataAnnotations;
using PushDashboard.Models;

namespace PushDashboard.ViewModels;

public class IntegrationIndexViewModel
{
    public Dictionary<string, List<Integration>> GroupedIntegrations { get; set; } = new();
    public Dictionary<int, CompanyIntegration> CompanyIntegrations { get; set; } = new();
}

public class AddIntegrationRequest
{
    [Required(ErrorMessage = "Entegrasyon seçimi gereklidir.")]
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir entegrasyon seçiniz.")]
    public int IntegrationId { get; set; }
}

public class SaveSettingsRequest
{
    [Required(ErrorMessage = "Entegrasyon seçimi gereklidir.")]
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir entegrasyon seçiniz.")]
    public int IntegrationId { get; set; }

    [Required(ErrorMessage = "Ayarlar gereklidir.")]
    public Dictionary<string, object> Settings { get; set; } = new();
}

public class TestConnectionRequest
{
    [Required(ErrorMessage = "Entegrasyon seçimi gereklidir.")]
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir entegrasyon seçiniz.")]
    public int IntegrationId { get; set; }

    [Required(ErrorMessage = "Ayarlar gereklidir.")]
    public Dictionary<string, object> Settings { get; set; } = new();
}

public class SyncIntegrationRequest
{
    [Required(ErrorMessage = "Entegrasyon seçimi gereklidir.")]
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir entegrasyon seçiniz.")]
    public int IntegrationId { get; set; }
}

// SettingsController DTOs
public class DeleteUserRequest
{
    [Required(ErrorMessage = "Kullanıcı ID'si gereklidir.")]
    public string UserId { get; set; } = string.Empty;
}

public class DeactivateUserRequest
{
    [Required(ErrorMessage = "Kullanıcı ID'si gereklidir.")]
    public string UserId { get; set; } = string.Empty;

    public bool Deactivate { get; set; }
}

public class TerminateSessionRequest
{
    [Required(ErrorMessage = "Oturum ID'si gereklidir.")]
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir oturum seçiniz.")]
    public int SessionId { get; set; }
}

public class TerminateDeviceRequest
{
    [Required(ErrorMessage = "Cihaz ID'si gereklidir.")]
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir cihaz seçiniz.")]
    public int DeviceId { get; set; }
}

// AccountController DTOs
public class ChangePasswordRequest
{
    [Required(ErrorMessage = "Mevcut şifre gereklidir.")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni şifre gereklidir.")]
    [StringLength(100, ErrorMessage = "Şifre en az {2} karakter olmalıdır.", MinimumLength = 6)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Şifre onayı gereklidir.")]
    [Compare("NewPassword", ErrorMessage = "Şifreler eşleşmiyor.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

// EmailTemplate ViewModels
public class EmailTemplateIndexViewModel
{
    public Dictionary<string, List<CompanyEmailTemplate>> UserTemplates { get; set; } = new();
    public Dictionary<string, List<EmailTemplate>> SystemTemplates { get; set; } = new();
    
    // Keeping for compatibility if needed, but we will use the above two
    public Dictionary<string, List<EmailTemplate>> GroupedTemplates { get; set; } = new();
    public Guid CompanyId { get; set; }
}

public class SaveTemplateRequest
{
    [Required(ErrorMessage = "Şablon seçimi gereklidir.")]
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir şablon seçiniz.")]
    public int TemplateId { get; set; }

    // For custom templates, this will be the CompanyEmailTemplate.Id
    public int? CompanyTemplateId { get; set; }

    [Required(ErrorMessage = "Konu gereklidir.")]
    [StringLength(200, ErrorMessage = "Konu en fazla 200 karakter olabilir.")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "İçerik gereklidir.")]
    public string Content { get; set; } = string.Empty;
}

public class PreviewTemplateRequest
{
    [Required(ErrorMessage = "Şablon seçimi gereklidir.")]
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir şablon seçiniz.")]
    public int TemplateId { get; set; }

    // For custom templates, this will be the CompanyEmailTemplate.Id
    public int? CompanyTemplateId { get; set; }

    // Current form data for preview
    public string? Subject { get; set; }
    public string? Content { get; set; }
}

public class ResetTemplateRequest
{
    [Required(ErrorMessage = "Şablon seçimi gereklidir.")]
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir şablon seçiniz.")]
    public int TemplateId { get; set; }
}

public class DeleteEmailTemplateRequest
{
    [Required(ErrorMessage = "Şablon seçimi gereklidir.")]
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir şablon seçiniz.")]
    public int CompanyTemplateId { get; set; }
}

public class CreateEmailTemplateRequest
{
    [Required(ErrorMessage = "Şablon adı gereklidir.")]
    [StringLength(100, ErrorMessage = "Şablon adı en fazla 100 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kategori seçimi gereklidir.")]
    [StringLength(50, ErrorMessage = "Kategori en fazla 50 karakter olabilir.")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Açıklama gereklidir.")]
    [StringLength(200, ErrorMessage = "Açıklama en fazla 200 karakter olabilir.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Konu gereklidir.")]
    [StringLength(100, ErrorMessage = "Konu en fazla 100 karakter olabilir.")]
    public string Subject { get; set; } = string.Empty;

    [Required(ErrorMessage = "İçerik gereklidir.")]
    public string Content { get; set; } = string.Empty;

    public string? ModuleType { get; set; }
}

// SMS Template ViewModels
public class SmsTemplateIndexViewModel
{
    public List<CompanySmsTemplate> CompanyTemplates { get; set; } = new();
    public List<Module> AvailableModules { get; set; } = new(); // Modules that can be selected for new templates
    public Guid CompanyId { get; set; }
}

public class SaveSmsTemplateRequest
{
    [Required(ErrorMessage = "Şablon seçimi gereklidir.")]
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir şablon seçiniz.")]
    public int TemplateId { get; set; }

    // For custom templates, this will be the CompanySmsTemplate.Id
    public int? CompanyTemplateId { get; set; }

    [Required(ErrorMessage = "İçerik gereklidir.")]
    public string Content { get; set; } = string.Empty;
}

public class PreviewSmsTemplateRequest
{
    [Required(ErrorMessage = "Şablon seçimi gereklidir.")]
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir şablon seçiniz.")]
    public int TemplateId { get; set; }

    // For custom templates, this will be the CompanySmsTemplate.Id
    public int? CompanyTemplateId { get; set; }

    // Current form data for preview
    public string? Content { get; set; }
}

public class ResetSmsTemplateRequest
{
    [Required(ErrorMessage = "Şablon seçimi gereklidir.")]
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir şablon seçiniz.")]
    public int TemplateId { get; set; }
}

public class DeleteSmsTemplateRequest
{
    [Required(ErrorMessage = "Şablon seçimi gereklidir.")]
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir şablon seçiniz.")]
    public int CompanyTemplateId { get; set; }
}

public class CreateSmsTemplateRequest
{
    [Required(ErrorMessage = "Şablon adı gereklidir.")]
    [StringLength(100, ErrorMessage = "Şablon adı en fazla 100 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kategori seçimi gereklidir.")]
    [StringLength(50, ErrorMessage = "Kategori en fazla 50 karakter olabilir.")]
    public string Category { get; set; } = string.Empty;

    [Required(ErrorMessage = "Açıklama gereklidir.")]
    [StringLength(200, ErrorMessage = "Açıklama en fazla 200 karakter olabilir.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "İçerik gereklidir.")]
    public string Content { get; set; } = string.Empty;

    public string? ModuleType { get; set; }
}


// Order Status Mapping DTOs
public class SaveStatusMappingsRequest
{
    [Required(ErrorMessage = "Entegrasyon seçimi gereklidir.")]
    [Range(1, int.MaxValue, ErrorMessage = "Geçerli bir entegrasyon seçiniz.")]
    public int IntegrationId { get; set; }

    [Required(ErrorMessage = "Durum eşleştirmeleri gereklidir.")]
    public Dictionary<string, string> StatusMappings { get; set; } = new();
}

// StoreController DTOs
public class StoreRequest
{
    [Required(ErrorMessage = "Mağaza adı gereklidir.")]
    [StringLength(100, ErrorMessage = "Mağaza adı en fazla 100 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    public string? Description { get; set; }
}
