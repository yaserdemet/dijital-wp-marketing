using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace PushDashboard.Services;

public class NewsletterPopupService : INewsletterPopupService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NewsletterPopupService> _logger;
    private readonly IR2StorageService _r2StorageService;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public NewsletterPopupService(
        ApplicationDbContext context,
        ILogger<NewsletterPopupService> logger,
        IR2StorageService r2StorageService,
        IConfiguration configuration,
        IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _logger = logger;
        _r2StorageService = r2StorageService;
        _configuration = configuration;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<NewsletterPopupSettings?> GetNewsletterPopupAsync(Guid companyId)
    {
        try
        {
            return await _context.NewsletterPopupSettings
                .Include(n => n.Company)
                .Include(n => n.UpdatedByUser)
                .FirstOrDefaultAsync(n => n.CompanyId == companyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting newsletter popup for company {CompanyId}", companyId);
            return null;
        }
    }

    public async Task<bool> SaveNewsletterPopupAsync(NewsletterPopupSettings settings)
    {
        try
        {
            var existing = await _context.NewsletterPopupSettings
                .FirstOrDefaultAsync(n => n.CompanyId == settings.CompanyId);

            if (existing != null)
            {
                existing.Title = settings.Title;
                existing.Description = settings.Description;
                existing.SubmitButtonText = settings.SubmitButtonText;
                existing.PrivacyText = settings.PrivacyText;
                existing.PrivacyUrl = settings.PrivacyUrl;
                existing.NamePlaceholder = settings.NamePlaceholder;
                existing.SurnamePlaceholder = settings.SurnamePlaceholder;
                existing.PhonePlaceholder = settings.PhonePlaceholder;
                existing.BackgroundColor = settings.BackgroundColor;
                existing.TextColor = settings.TextColor;
                existing.ButtonColor = settings.ButtonColor;
                existing.ButtonTextColor = settings.ButtonTextColor;
                existing.BorderRadius = settings.BorderRadius;
                // ImageUrl is managed separately via UploadNewsletterImageAsync
                existing.ImagePosition = settings.ImagePosition;
                existing.HideImageOnMobile = settings.HideImageOnMobile;
                existing.MobileImageUrl = settings.MobileImageUrl;
                existing.SloganEnabled = settings.SloganEnabled;
                existing.SloganBadgeText = settings.SloganBadgeText;
                existing.SloganText = settings.SloganText;
                existing.SloganHighlightColor = settings.SloganHighlightColor;
                existing.SloganTextColor = settings.SloganTextColor;
                existing.ShowNameFields = settings.ShowNameFields;
                existing.ShowPhoneField = settings.ShowPhoneField;
                existing.ShowConsentCheckbox = settings.ShowConsentCheckbox;
                existing.DelaySeconds = settings.DelaySeconds;
                existing.ShowFrequencyDays = settings.ShowFrequencyDays;
                existing.ShowOnMobile = settings.ShowOnMobile;
                existing.ShowOnDesktop = settings.ShowOnDesktop;
                existing.EnableAnimation = settings.EnableAnimation;
                existing.IsActive = settings.IsActive;
                existing.UpdatedAt = DateTime.Now;
                existing.UpdatedByUserId = settings.UpdatedByUserId;

                _context.NewsletterPopupSettings.Update(existing);
            }
            else
            {
                settings.CreatedAt = DateTime.Now;
                settings.UpdatedAt = DateTime.Now;
                _context.NewsletterPopupSettings.Add(settings);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving newsletter popup for company {CompanyId}", settings.CompanyId);
            return false;
        }
    }

    public async Task<NewsletterPopupScriptConfig> GetScriptConfigAsync(Guid companyId)
    {
        try
        {
            var settings = await GetNewsletterPopupAsync(companyId);
            if (settings == null || !settings.IsActive)
            {
                return new NewsletterPopupScriptConfig { IsActive = false };
            }

            // Fetch legal text links from Integration Settings
            string? kvkkLink = null;
            string? etkLink = null;
            try
            {
                var ecommerceIntegration = await _context.CompanyIntegrations
                    .Include(ci => ci.Integration)
                    .Where(ci => ci.CompanyId == companyId
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
                _logger.LogWarning(ex, "Failed to fetch Integration Settings for Legal Texts in Newsletter Popup Config");
            }

            return new NewsletterPopupScriptConfig
            {
                IsActive = settings.IsActive,
                Title = settings.Title,
                Description = settings.Description,
                SubmitButtonText = settings.SubmitButtonText,
                PrivacyText = settings.PrivacyText,
                PrivacyUrl = settings.PrivacyUrl,
                KvkkLink = kvkkLink,
                EtkLink = etkLink,
                NamePlaceholder = settings.NamePlaceholder,
                SurnamePlaceholder = settings.SurnamePlaceholder,
                PhonePlaceholder = settings.PhonePlaceholder,
                BackgroundColor = settings.BackgroundColor,
                TextColor = settings.TextColor,
                ButtonColor = settings.ButtonColor,
                ButtonTextColor = settings.ButtonTextColor,
                BorderRadius = settings.BorderRadius,
                ImageUrl = settings.ImageUrl,
                ImagePosition = settings.ImagePosition,
                HideImageOnMobile = settings.HideImageOnMobile,
                MobileImageUrl = settings.MobileImageUrl,
                SloganEnabled = settings.SloganEnabled,
                SloganBadgeText = settings.SloganBadgeText,
                SloganText = settings.SloganText,
                SloganHighlightColor = settings.SloganHighlightColor,
                SloganTextColor = settings.SloganTextColor,
                ShowNameFields = settings.ShowNameFields,
                ShowPhoneField = settings.ShowPhoneField,
                ShowConsentCheckbox = settings.ShowConsentCheckbox,
                DelaySeconds = settings.DelaySeconds,
                ShowFrequencyDays = settings.ShowFrequencyDays,
                ShowOnMobile = settings.ShowOnMobile,
                ShowOnDesktop = settings.ShowOnDesktop,
                EnableAnimation = settings.EnableAnimation
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting script config for company {CompanyId}", companyId);
            return new NewsletterPopupScriptConfig { IsActive = false };
        }
    }



    public async Task<NewsletterPopupSettings> CreateDefaultSettingsAsync(Guid companyId, string userId)
    {
        var defaultSettings = new NewsletterPopupSettings
        {
            CompanyId = companyId,
            Title = "Bültenimize Abone Olun, Fırsatları İlk Siz Yakalayın!",
            Description = "İndirim ve fırsatlardan ilk sizin haberiniz olsun, kayıt olun ve avantajlardan yararlanın!",
            SubmitButtonText = "Kaydet",
            PrivacyText = "Kampanya, duyuru ve bilgilendirmelerden E-Posta, WhatsApp ve SMS yoluyla haberdar olmak istediğimi belirtiyorum.",
            PrivacyUrl = "#",
            NamePlaceholder = "Adınızı Giriniz",
            SurnamePlaceholder = "Soyadınızı Giriniz",
            PhonePlaceholder = "5XXXXXXXXX",
            BackgroundColor = "#ffffff",
            TextColor = "#333333",
            ButtonColor = "#e53935",
            ButtonTextColor = "#ffffff",
            BorderRadius = "12px",
            ShowNameFields = true,
            ShowPhoneField = true,
            ShowConsentCheckbox = true,
            DelaySeconds = 5,
            ShowFrequencyDays = 30,
            ShowOnMobile = true,
            ShowOnDesktop = true,
            EnableAnimation = true,
            IsActive = true,
            UpdatedByUserId = userId
        };

        return defaultSettings;
    }
    
   public async Task<string> ExportNewsletterPopupToR2Async(Guid companyId)
    {
        try
        {
            _logger.LogInformation("Starting R2 export for newsletter popup. CompanyId: {CompanyId}", companyId);

            var settings = await _context.NewsletterPopupSettings
                .FirstOrDefaultAsync(n => n.CompanyId == companyId && n.IsActive);

            if (settings == null)
            {
                await _r2StorageService.DeleteFileAsync($"{companyId}/newsletterPopup.js");

                _logger.LogInformation("Removed newsletter popup files from R2 for company {CompanyId} (not active)", companyId);
                return string.Empty;
            }

            var config = await GetScriptConfigAsync(companyId);
            if (!config.IsActive)
            {
                _logger.LogWarning("Newsletter popup is not active for company {CompanyId}", companyId);
                return "Newsletter popup is not active";
            }

            var allData = new
            {
                CompanyId = companyId,
                UpdatedAt = DateTime.Now,
                Config = config
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var jsonContent = JsonSerializer.Serialize(allData, jsonOptions);

            // JSON dosyasını R2'ye yükle
            var jsonKey = $"{companyId}/newsletterPopup.json";
            var jsonUrl = await _r2StorageService.UploadJsonAsync(jsonKey, jsonContent);

            // wwwroot/js/modules'dan JS dosyasını oku ve R2'ye yükle
            var jsFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "js", "modules", "newsletter-popup-module.js");
            if (File.Exists(jsFilePath))
            {
                var jsScript = await File.ReadAllTextAsync(jsFilePath);
                var jsKey = $"{companyId}/newsletterPopup.js";
                var jsUrl = await _r2StorageService.UploadJavaScriptAsync(jsKey, jsScript);

                _logger.LogInformation("Successfully exported newsletter popup to R2 for company {CompanyId}. JSON: {JsonUrl}, JS: {JsUrl}",
                    companyId, jsonUrl, jsUrl);
            }
            else
            {
                _logger.LogWarning("Newsletter popup module JS file not found at {Path}", jsFilePath);
            }

            return jsonUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting newsletter popup to R2 for company {CompanyId}", companyId);
            throw;
        }
    }

    public async Task<bool> RecordTrackingEventAsync(NewsletterPopupTrackingModel model, string? ipAddress, string? userAgent)
    {
        try
        {
            var settings = await _context.NewsletterPopupSettings
                .FirstOrDefaultAsync(n => n.CompanyId == model.CompanyId && n.IsActive);

            if (settings == null)
                return false;

            bool isMobile = !string.IsNullOrEmpty(userAgent) &&
                Regex.IsMatch(userAgent,
                    @"Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini",
                    RegexOptions.IgnoreCase);

            // Truncate IP address to fit varchar(45) column limit
            var safeIpAddress = ipAddress != null && ipAddress.Length > 45
                ? ipAddress[..45]
                : ipAddress;

            var trackingEvent = new NewsletterPopupEvent
            {
                NewsletterPopupSettingsId = settings.Id,
                EventType = model.EventType,
                PageUrl = model.PageUrl,
                SessionId = model.SessionId,
                IpAddress = safeIpAddress,
                UserAgent = userAgent,
                IsMobile = isMobile,
                CustomerName = model.CustomerName,
                CustomerSurname = model.CustomerSurname,
                CustomerPhone = model.CustomerPhone,
                CreatedAt = DateTime.Now
            };

            _context.NewsletterPopupEvents.Add(trackingEvent);

            switch (model.EventType)
            {
                case "impression":
                    settings.ImpressionCount++;
                    break;
                case "submit":
                    settings.SubmitCount++;
                    await CreateOrUpdateCustomerFromNewsletterAsync(model.CompanyId, model);
                    break;
                case "dismiss":
                    settings.DismissCount++;
                    break;
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording newsletter popup tracking event {EventType} for company {CompanyId}", model.EventType, model.CompanyId);
            return false;
        }
    }

    private async Task CreateOrUpdateCustomerFromNewsletterAsync(Guid companyId, NewsletterPopupTrackingModel model)
    {
        try
        {
            // Telefon numarasına göre mevcut müşteriyi ara
            Customer? existingCustomer = null;
            if (!string.IsNullOrEmpty(model.CustomerPhone))
            {
                existingCustomer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CompanyId == companyId &&
                        (c.Phone == model.CustomerPhone || c.MobilePhone == model.CustomerPhone));
            }

            if (existingCustomer != null)
            {
                // Mevcut müşteriyi güncelle - İzinleri otomatik ver
                existingCustomer.SmsPermission = true;
                existingCustomer.EmailPermission = true;
                existingCustomer.IsNewsletterSubscribed = true;
                existingCustomer.UpdatedAt = DateTime.Now;

                _logger.LogInformation("Customer permissions updated from newsletter. CustomerId: {CustomerId}", existingCustomer.Id);
            }
            else
            {
                // Yeni müşteri oluştur - E-Bülten kaynağı ile
                var nextExternalId = await GetNextNewsletterExternalIdAsync(companyId);
                var newCustomer = new Customer
                {
                    CompanyId = companyId,
                    ExternalId = nextExternalId,
                    FirstName = model.CustomerName ?? "",
                    LastName = model.CustomerSurname ?? "",
                    Email = "", // Email is not collected but permission is granted
                    MobilePhone = model.CustomerPhone,
                    Phone = model.CustomerPhone,
                    SmsPermission = true,
                    EmailPermission = true,
                    IsNewsletterSubscribed = true,
                    Source = CustomerSource.Newsletter,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    MembershipDate = DateTime.Now
                };

                _context.Customers.Add(newCustomer);

                _logger.LogInformation("New customer created from newsletter. Phone: {Phone}, Source: Newsletter", model.CustomerPhone);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating customer from newsletter for company {CompanyId}", companyId);
        }
    }

    private async Task<int> GetNextNewsletterExternalIdAsync(Guid companyId)
    {
        // Newsletter'dan eklenen müşteriler için negatif ExternalId kullan
        // Bu sayede e-ticaret entegrasyonlarından gelen pozitif ID'lerle çakışmaz
        var lastExternalId = await _context.Customers
            .Where(c => c.CompanyId == companyId && c.ExternalId < 0)
            .OrderBy(c => c.ExternalId)
            .Select(c => c.ExternalId)
            .FirstOrDefaultAsync();

        return lastExternalId == 0 ? -1 : lastExternalId - 1;
    }

    public async Task<string> UploadNewsletterImageAsync(Guid companyId, Stream imageStream, string contentType, string fileName, bool isMobile = false)
    {
        try
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant() ?? ".jpg";
            var prefix = isMobile ? "newsletter-popup-mobile-image" : "newsletter-popup-image";
            var imageKey = $"{companyId}/{prefix}{extension}";

            var imageUrl = await _r2StorageService.UploadImageAsync(imageKey, imageStream, contentType);

            // Update the settings with the new image URL
            var settings = await _context.NewsletterPopupSettings
                .FirstOrDefaultAsync(n => n.CompanyId == companyId);

            if (settings != null)
            {
                if (isMobile)
                    settings.MobileImageUrl = imageUrl;
                else
                    settings.ImageUrl = imageUrl;
                settings.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Newsletter popup {Type} image uploaded for company {CompanyId}. URL: {ImageUrl}", 
                isMobile ? "mobile" : "desktop", companyId, imageUrl);
            return imageUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading newsletter popup image for company {CompanyId}", companyId);
            throw;
        }
    }

    public async Task<bool> DeleteNewsletterImageAsync(Guid companyId)
    {
        try
        {
            var settings = await _context.NewsletterPopupSettings
                .FirstOrDefaultAsync(n => n.CompanyId == companyId);

            if (settings == null || string.IsNullOrEmpty(settings.ImageUrl))
                return true;

            // Try to delete from R2 (best effort - try common extensions)
            var extensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            foreach (var ext in extensions)
            {
                try
                {
                    await _r2StorageService.DeleteFileAsync($"{companyId}/newsletter-popup-image{ext}");
                }
                catch { /* ignore */ }
            }

            settings.ImageUrl = null;
            settings.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Newsletter popup image deleted for company {CompanyId}", companyId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting newsletter popup image for company {CompanyId}", companyId);
            return false;
        }
    }

}

