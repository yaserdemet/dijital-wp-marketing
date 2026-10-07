using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Models;
using PushDashboard.Services;
using PushDashboard.Data;
using PushDashboard.Filters;
using System.Text.Json;

namespace PushDashboard.Controllers;

[Authorize]
public class NewsletterPopupController : BaseController
{
    private readonly INewsletterPopupService _newsletterPopupService;
    private readonly ILogger<NewsletterPopupController> _logger;
    private readonly IActivityLogService _activityLogService;

    public NewsletterPopupController(
        INewsletterPopupService newsletterPopupService,
        ILogger<NewsletterPopupController> logger,
        IActivityLogService activityLogService,
        IUserContextService userContextService,
        ApplicationDbContext context)
        : base(userContextService, context)
    {
        _newsletterPopupService = newsletterPopupService;
        _logger = logger;
        _activityLogService = activityLogService;
    }

    public async Task<IActionResult> Index()
    {
        var companyId = GetCurrentUserCompanyId();
        if (companyId == null)
        {
            return RedirectToAction("Index", "Home");
        }

        var settings = await _newsletterPopupService.GetNewsletterPopupAsync(companyId.Value);
        if (settings == null)
        {
            var userId = GetCurrentUserId();
            settings = await _newsletterPopupService.CreateDefaultSettingsAsync(companyId.Value, userId!);
            await _newsletterPopupService.SaveNewsletterPopupAsync(settings);
            settings = await _newsletterPopupService.GetNewsletterPopupAsync(companyId.Value);
        }

        // Fetch legal text links from Integration Settings (same pattern as GiftWheel)
        string? kvkkLink = null;
        string? etkLink = null;
        try
        {
            var ecommerceIntegration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .Where(ci => ci.CompanyId == companyId.Value
                             && ci.IsActive
                             && ci.IsConfigured
                             && ci.Integration.Type == "ecommerce")
                .FirstOrDefaultAsync();

            if (ecommerceIntegration != null) 
            {
                if (!string.IsNullOrEmpty(ecommerceIntegration.KvkkLink))
                {
                    kvkkLink = ecommerceIntegration.KvkkLink;
                }
                else if (!string.IsNullOrEmpty(ecommerceIntegration.SettingsJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(ecommerceIntegration.SettingsJson);
                        var root = doc.RootElement;
                        if ((root.TryGetProperty("KvkkLink", out var kvkkProp) || root.TryGetProperty("kvkkLink", out kvkkProp)) &&
                            kvkkProp.ValueKind == JsonValueKind.String)
                        {
                            kvkkLink = kvkkProp.GetString();
                        }
                    }
                    catch { /* Ignore JSON parse errors */ }
                }

                if (!string.IsNullOrEmpty(ecommerceIntegration.TicariElektronikIletiLink))
                {
                    etkLink = ecommerceIntegration.TicariElektronikIletiLink;
                }
                else if (!string.IsNullOrEmpty(ecommerceIntegration.SettingsJson))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(ecommerceIntegration.SettingsJson);
                        var root = doc.RootElement;
                        if ((root.TryGetProperty("EtkLink", out var etkProp) ||
                             root.TryGetProperty("ticariElektronikIletiLink", out etkProp)) &&
                            etkProp.ValueKind == JsonValueKind.String)
                        {
                            etkLink = etkProp.GetString();
                        }
                    }
                    catch { /* Ignore JSON parse errors */ }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch Integration Settings for Legal Texts in Newsletter Popup");
        }

        ViewBag.KvkkLink = kvkkLink;
        ViewBag.EtkLink = etkLink;

        if (settings == null)
        {
            _logger.LogError("Newsletter popup settings could not be loaded or created for company {CompanyId}", companyId);
            return View(new NewsletterPopupSettings
            {
                CompanyId = companyId.Value,
                Title = "Bültenimize Abone Olun",
                Description = "Kampanya ve fırsatlardan haberdar olun!",
                SubmitButtonText = "Abone Ol",
                IsActive = false
            });
        }

        return View(settings);
    }

    [HttpPost]
    [RequireCompanyId]
    public async Task<IActionResult> SaveSettings([FromBody] NewsletterPopupSaveModel model)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var userId = GetCurrentUserId();

        try
        {
            var settings = new NewsletterPopupSettings
            {
                CompanyId = companyId,
                Title = model.Title,
                Description = model.Description,
                SubmitButtonText = model.SubmitButtonText,
                PrivacyText = model.PrivacyText,
                PrivacyUrl = model.PrivacyUrl,
                NamePlaceholder = model.NamePlaceholder,
                SurnamePlaceholder = model.SurnamePlaceholder,
                PhonePlaceholder = model.PhonePlaceholder,
                BackgroundColor = model.BackgroundColor,
                TextColor = model.TextColor,
                ButtonColor = model.ButtonColor,
                ButtonTextColor = model.ButtonTextColor,
                BorderRadius = model.BorderRadius,
                ImagePosition = model.ImagePosition,
                HideImageOnMobile = model.HideImageOnMobile,
                MobileImageUrl = model.MobileImageUrl,
                SloganEnabled = model.SloganEnabled,
                SloganBadgeText = model.SloganBadgeText,
                SloganText = model.SloganText,
                SloganHighlightColor = model.SloganHighlightColor,
                SloganTextColor = model.SloganTextColor,
                ShowNameFields = model.ShowNameFields,
                ShowPhoneField = model.ShowPhoneField,
                ShowConsentCheckbox = model.ShowConsentCheckbox,
                DelaySeconds = model.DelaySeconds,
                ShowFrequencyDays = model.ShowFrequencyDays,
                ShowOnMobile = model.ShowOnMobile,
                ShowOnDesktop = model.ShowOnDesktop,
                EnableAnimation = model.EnableAnimation,
                IsActive = model.IsActive,
                UpdatedByUserId = userId!
            };

            var result = await _newsletterPopupService.SaveNewsletterPopupAsync(settings);

            if (result)
            {
                await _activityLogService.LogActivityAsync(
                    userId!,
                    companyId,
                    ActivityActionTypes.Update,
                    "NewsletterPopup",
                    "E-Bülten ayarları güncellendi",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString());

                return Json(new { success = true, message = "Ayarlar başarıyla kaydedildi." });
            }

            return Json(new { success = false, message = "Ayarlar kaydedilirken bir hata oluştu." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving newsletter popup settings");
            return Json(new { success = false, message = "Bir hata oluştu: " + ex.Message });
        }
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetPreviewData()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var config = await _newsletterPopupService.GetScriptConfigAsync(companyId);
        return Json(config);
    }

    [HttpPost]
    [RequireCompanyId]
    public async Task<IActionResult> ExportToR2()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var userId = GetCurrentUserId();

        try
        {
            var url = await _newsletterPopupService.ExportNewsletterPopupToR2Async(companyId);

            await _activityLogService.LogActivityAsync(
                userId!,
                companyId,
                ActivityActionTypes.Update,
                "NewsletterPopup",
                "E-Bülten R2'ye yayınlandı",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            return Json(new { success = true, message = "E-Bülten başarıyla yayınlandı.", url });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting newsletter popup to R2");
            return Json(new { success = false, message = "Yayınlama sırasında bir hata oluştu: " + ex.Message });
        }
    }

    [HttpPost]
    [RequireCompanyId]
    public async Task<IActionResult> UploadImage(IFormFile file, [FromForm] bool isMobile = false)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        try
        {
            if (file == null || file.Length == 0)
                return Json(new { success = false, message = "Dosya seçilmedi." });

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp", "image/gif" };
            if (!allowedTypes.Contains(file.ContentType.ToLowerInvariant()))
                return Json(new { success = false, message = "Sadece JPEG, PNG, WebP ve GIF dosyaları yüklenebilir." });

            // Validate file size (max 5MB)
            if (file.Length > 5 * 1024 * 1024)
                return Json(new { success = false, message = "Dosya boyutu 5MB'dan büyük olamaz." });

            using var stream = file.OpenReadStream();
            var imageUrl = await _newsletterPopupService.UploadNewsletterImageAsync(
                companyId, stream, file.ContentType, file.FileName, isMobile);

            return Json(new { success = true, message = "Fotoğraf başarıyla yüklendi.", imageUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading newsletter popup image");
            return Json(new { success = false, message = "Fotoğraf yüklenirken bir hata oluştu: " + ex.Message });
        }
    }

    [HttpPost]
    [RequireCompanyId]
    public async Task<IActionResult> DeleteImage()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        try
        {
            var result = await _newsletterPopupService.DeleteNewsletterImageAsync(companyId);
            return result
                ? Json(new { success = true, message = "Fotoğraf başarıyla silindi." })
                : Json(new { success = false, message = "Fotoğraf silinirken bir hata oluştu." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting newsletter popup image");
            return Json(new { success = false, message = "Fotoğraf silinirken bir hata oluştu: " + ex.Message });
        }
    }
}

/// <summary>
/// E-bülten popup ayarlarını kaydetmek için kullanılan model
/// </summary>
public class NewsletterPopupSaveModel
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SubmitButtonText { get; set; } = string.Empty;
    public string PrivacyText { get; set; } = string.Empty;
    public string PrivacyUrl { get; set; } = string.Empty;
    public string NamePlaceholder { get; set; } = string.Empty;
    public string SurnamePlaceholder { get; set; } = string.Empty;
    public string PhonePlaceholder { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = string.Empty;
    public string TextColor { get; set; } = string.Empty;
    public string ButtonColor { get; set; } = string.Empty;
    public string ButtonTextColor { get; set; } = string.Empty;
    public string BorderRadius { get; set; } = string.Empty;
    public string ImagePosition { get; set; } = "left";
    public bool HideImageOnMobile { get; set; }
    public string? MobileImageUrl { get; set; }
    public bool SloganEnabled { get; set; }
    public string? SloganBadgeText { get; set; }
    public string? SloganText { get; set; }
    public string SloganHighlightColor { get; set; } = "#c8a96e";
    public string SloganTextColor { get; set; } = "#ffffff";
    public bool ShowNameFields { get; set; }
    public bool ShowPhoneField { get; set; }
    public bool ShowConsentCheckbox { get; set; }
    public int DelaySeconds { get; set; }
    public int ShowFrequencyDays { get; set; }
    public bool ShowOnMobile { get; set; }
    public bool ShowOnDesktop { get; set; }
    public bool EnableAnimation { get; set; }
    public bool IsActive { get; set; }
}
