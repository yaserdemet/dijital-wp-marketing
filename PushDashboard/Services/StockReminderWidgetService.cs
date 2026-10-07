using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.Services.Integrations.Ticimax;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PushDashboard.Services;

public class StockReminderWidgetService : IStockReminderWidgetService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StockReminderWidgetService> _logger;
    private readonly IR2StorageService _r2StorageService;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly TicimaxService _ticimaxService;
    private readonly IActivityLogService _activityLogService;

    public StockReminderWidgetService(
        ApplicationDbContext context,
        ILogger<StockReminderWidgetService> logger,
        IR2StorageService r2StorageService,
        IConfiguration configuration,
        IWebHostEnvironment webHostEnvironment,
        TicimaxService ticimaxService,
        IActivityLogService activityLogService)
    {
        _context = context;
        _logger = logger;
        _r2StorageService = r2StorageService;
        _configuration = configuration;
        _webHostEnvironment = webHostEnvironment;
        _ticimaxService = ticimaxService;
        _activityLogService = activityLogService;
    }

    public async Task<StockReminderWidgetSettings?> GetWidgetSettingsAsync(Guid companyId)
    {
        try
        {
            return await _context.StockReminderWidgetSettings
                .Include(s => s.Company)
                .Include(s => s.UpdatedByUser)
                .FirstOrDefaultAsync(s => s.CompanyId == companyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock reminder widget settings for company {CompanyId}", companyId);
            return null;
        }
    }

    public async Task<bool> SaveWidgetSettingsAsync(StockReminderWidgetSettings settings)
    {
        try
        {
            var existing = await _context.StockReminderWidgetSettings
                .FirstOrDefaultAsync(s => s.CompanyId == settings.CompanyId);

            if (existing != null)
            {
                existing.Title = settings.Title;
                existing.Description = settings.Description;
                existing.SubmitButtonText = settings.SubmitButtonText;
                existing.PrivacyText = settings.PrivacyText;
                existing.PhonePlaceholder = settings.PhonePlaceholder;
                existing.EmailPlaceholder = settings.EmailPlaceholder;
                existing.VariantOptions = settings.VariantOptions;
                existing.VariantLabel = settings.VariantLabel;
                existing.ShowVariantSelector = settings.ShowVariantSelector;
                existing.ShowPhoneField = settings.ShowPhoneField;
                existing.ShowEmailField = settings.ShowEmailField;
                existing.ShowNameFields = settings.ShowNameFields;
                existing.RequirePhone = settings.RequirePhone;
                existing.ShowConsentCheckbox = settings.ShowConsentCheckbox;
                existing.BackgroundColor = settings.BackgroundColor;
                existing.TextColor = settings.TextColor;
                existing.TitleColor = settings.TitleColor;
                existing.ButtonColor = settings.ButtonColor;
                existing.ButtonTextColor = settings.ButtonTextColor;
                existing.VariantBorderColor = settings.VariantBorderColor;
                existing.VariantSelectedColor = settings.VariantSelectedColor;
                existing.BorderRadius = settings.BorderRadius;
                existing.IsActive = settings.IsActive;
                existing.ShowOnMobile = settings.ShowOnMobile;
                existing.ShowOnDesktop = settings.ShowOnDesktop;
                existing.EnableAnimation = settings.EnableAnimation;
                existing.UpdatedAt = DateTime.Now;
                existing.UpdatedByUserId = settings.UpdatedByUserId;

                _context.StockReminderWidgetSettings.Update(existing);
            }
            else
            {
                settings.CreatedAt = DateTime.Now;
                settings.UpdatedAt = DateTime.Now;
                _context.StockReminderWidgetSettings.Add(settings);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving stock reminder widget settings for company {CompanyId}", settings.CompanyId);
            return false;
        }
    }

    public async Task<StockReminderWidgetSettings> CreateDefaultSettingsAsync(Guid companyId, string userId)
    {
        return new StockReminderWidgetSettings
        {
            CompanyId = companyId,
            Title = "Bedenini Seç, Stokta Olunca Sana Bildirelim!",
            Description = "Ürün stoğa girdiğinde size WhatsApp veya SMS ile hemen bilgi vereceğiz. Telefon numaranızın doğru olduğundan emin olun!",
            SubmitButtonText = "Kaydet",
            PrivacyText = "Kampanya, duyuru ve bilgilendirmelerden WhatsApp ve SMS yoluyla haberdar olmak istediğimi belirtiyorum.",
            PhonePlaceholder = "5XXXXXXXXX",
            EmailPlaceholder = "E-posta adresiniz (isteğe bağlı)",
            VariantOptions = "S,M,L,XL",
            VariantLabel = "Beden Seçin",
            ShowVariantSelector = true,
            ShowPhoneField = true,
            ShowEmailField = false,
            ShowNameFields = false,
            RequirePhone = true,
            ShowConsentCheckbox = true,
            BackgroundColor = "#ffffff",
            TextColor = "#333333",
            TitleColor = "#e53935",
            ButtonColor = "#e53935",
            ButtonTextColor = "#ffffff",
            VariantBorderColor = "#e5e7eb",
            VariantSelectedColor = "#111111",
            BorderRadius = "16px",
            IsActive = true,
            ShowOnMobile = true,
            ShowOnDesktop = true,
            EnableAnimation = true,
            UpdatedByUserId = userId
        };
    }

    public async Task<StockReminderWidgetScriptConfig> GetScriptConfigAsync(Guid companyId)
    {
        try
        {
            var settings = await GetWidgetSettingsAsync(companyId);
            if (settings == null || !settings.IsActive)
            {
                return new StockReminderWidgetScriptConfig { IsActive = false };
            }

            var apiBaseUrl = _configuration["AppSettings:ApiBaseUrl"] ?? "https://pushadmin-production.up.railway.app";

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
                    kvkkLink = ecommerceIntegration.KvkkLink;
                    etkLink = ecommerceIntegration.TicariElektronikIletiLink;

                    if (string.IsNullOrEmpty(kvkkLink) && !string.IsNullOrEmpty(ecommerceIntegration.SettingsJson))
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(ecommerceIntegration.SettingsJson);
                            var root = doc.RootElement;
                            if ((root.TryGetProperty("KvkkLink", out var kProp) || root.TryGetProperty("kvkkLink", out kProp)) &&
                                kProp.ValueKind == JsonValueKind.String)
                                kvkkLink = kProp.GetString();
                        }
                        catch { }
                    }

                    if (string.IsNullOrEmpty(etkLink) && !string.IsNullOrEmpty(ecommerceIntegration.SettingsJson))
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(ecommerceIntegration.SettingsJson);
                            var root = doc.RootElement;
                            if ((root.TryGetProperty("EtkLink", out var eProp) || root.TryGetProperty("ticariElektronikIletiLink", out eProp)) &&
                                eProp.ValueKind == JsonValueKind.String)
                                etkLink = eProp.GetString();
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch legal links for stock reminder widget config. CompanyId: {CompanyId}", companyId);
            }

            return new StockReminderWidgetScriptConfig
            {
                IsActive = settings.IsActive,
                CompanyId = companyId,
                Title = settings.Title,
                Description = settings.Description,
                SubmitButtonText = settings.SubmitButtonText,
                PrivacyText = settings.PrivacyText,
                KvkkLink = kvkkLink,
                EtkLink = etkLink,
                PhonePlaceholder = settings.PhonePlaceholder,
                EmailPlaceholder = settings.EmailPlaceholder,
                VariantOptions = settings.VariantOptions,
                VariantLabel = settings.VariantLabel,
                ShowVariantSelector = settings.ShowVariantSelector,
                ShowPhoneField = settings.ShowPhoneField,
                ShowEmailField = settings.ShowEmailField,
                ShowNameFields = settings.ShowNameFields,
                RequirePhone = settings.RequirePhone,
                ShowConsentCheckbox = settings.ShowConsentCheckbox,
                BackgroundColor = settings.BackgroundColor,
                TextColor = settings.TextColor,
                TitleColor = settings.TitleColor,
                ButtonColor = settings.ButtonColor,
                ButtonTextColor = settings.ButtonTextColor,
                VariantBorderColor = settings.VariantBorderColor,
                VariantSelectedColor = settings.VariantSelectedColor,
                BorderRadius = settings.BorderRadius,
                ShowOnMobile = settings.ShowOnMobile,
                ShowOnDesktop = settings.ShowOnDesktop,
                EnableAnimation = settings.EnableAnimation,
                SubmitUrl = $"{apiBaseUrl}/api/StockReminderWidgetApi/submit",
                TrackUrl = $"{apiBaseUrl}/api/StockReminderWidgetApi/track"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stock reminder widget script config for company {CompanyId}", companyId);
            return new StockReminderWidgetScriptConfig { IsActive = false };
        }
    }

    public async Task<string> ExportWidgetToR2Async(Guid companyId)
    {
        try
        {
            _logger.LogInformation("Starting R2 export for stock reminder widget. CompanyId: {CompanyId}", companyId);

            var settings = await _context.StockReminderWidgetSettings
                .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.IsActive);

            if (settings == null)
            {
                await _r2StorageService.DeleteFileAsync($"{companyId}/stockReminderWidget.js");
                _logger.LogInformation("Removed stock reminder widget files from R2 for company {CompanyId} (not active)", companyId);
                return string.Empty;
            }

            var config = await GetScriptConfigAsync(companyId);
            if (!config.IsActive)
            {
                _logger.LogWarning("Stock reminder widget is not active for company {CompanyId}", companyId);
                return "Stock reminder widget is not active";
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

            var jsonKey = $"{companyId}/stockReminderWidget.json";
            var jsonUrl = await _r2StorageService.UploadJsonAsync(jsonKey, jsonContent);

            // JS modülünü R2'ye yükle
            var jsFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "js", "modules", "stock-reminder-module.js");
            if (File.Exists(jsFilePath))
            {
                var jsScript = await File.ReadAllTextAsync(jsFilePath);
                var jsKey = $"{companyId}/stockReminderWidget.js";
                await _r2StorageService.UploadJavaScriptAsync(jsKey, jsScript);
                _logger.LogInformation("Successfully exported stock reminder widget to R2 for company {CompanyId}. JSON: {JsonUrl}", companyId, jsonUrl);
            }
            else
            {
                _logger.LogWarning("Stock reminder widget module JS file not found at {Path}", jsFilePath);
            }

            return jsonUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting stock reminder widget to R2 for company {CompanyId}", companyId);
            throw;
        }
    }

    public async Task<bool> RecordSubmissionAsync(StockReminderWidgetSubmissionModel model, string? ipAddress, string? userAgent)
    {
        try
        {
            var settings = await _context.StockReminderWidgetSettings
                .FirstOrDefaultAsync(s => s.CompanyId == model.CompanyId && s.IsActive);

            if (settings == null)
                return false;

            var safeIpAddress = ipAddress != null && ipAddress.Length > 50 ? ipAddress[..50] : ipAddress;

            var submission = new StockReminderWidgetSubmission
            {
                StockReminderWidgetSettingsId = settings.Id,
                CompanyId = model.CompanyId,
                Source = "widget",
                ProductId = model.VariantId,
                SelectedVariant = model.SelectedVariant,
                Phone = model.Phone,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PageUrl = model.PageUrl,
                SessionId = model.SessionId,
                ConsentGiven = model.ConsentGiven,
                IpAddress = safeIpAddress,
                UserAgent = userAgent,
                CreatedAt = DateTime.Now
            };

            // Ürün detaylarını Ticimax'tan çek
            if (!string.IsNullOrEmpty(model.ProductId) && !string.IsNullOrEmpty(model.VariantId))
            {
                try
                {
                    var product = await _ticimaxService.GetProductAsync(model.CompanyId, model.ProductId);
                    var variant = await _ticimaxService.GetVariationAsync(model.CompanyId, model.VariantId);
                    if (product != null && variant != null)
                    {
                        submission.ProductName = product.Name + " - " + variant.Name;
                        submission.StockCode = variant.VariantCode;
                        submission.TotalStock = variant.StockQuantity;
                        submission.Price = product.Price;
                        submission.PriceWithTax = product.KDVAmount.HasValue
                            ? product.Price + product.KDVAmount.Value
                            : product.Price;
                        submission.Currency = product.Currency;
                        submission.ImageUrl = product.MainImage;
                        submission.ProductUrl = product.ProductUrl;
                        submission.LastSyncedAt = DateTime.UtcNow;
                    }
                    else
                    {
                        _logger.LogWarning("Product {ProductId} not found in Ticimax for company {CompanyId}",
                            model.ProductId, model.CompanyId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching product details for widget submission. ProductId: {ProductId}, CompanyId: {CompanyId}",
                        model.ProductId, model.CompanyId);
                    // Ürün bilgileri alınamazsa yine de kaydı kaydet
                }
            }

            _context.StockReminderWidgetSubmissions.Add(submission);
            settings.SubmitCount++;
            settings.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording stock reminder widget submission for company {CompanyId}", model.CompanyId);
            return false;
        }
    }

    public async Task TrackEventAsync(Guid companyId, string eventType)
    {
        try
        {
            var settings = await _context.StockReminderWidgetSettings
                .FirstOrDefaultAsync(s => s.CompanyId == companyId);

            if (settings == null)
            {
                _logger.LogWarning("TrackEventAsync: no widget settings for company {CompanyId}", companyId);
                return;
            }

            switch (eventType?.ToLowerInvariant())
            {
                case "impression":
                case "view":
                    settings.ImpressionCount++;
                    break;
                case "dismiss":
                case "close":
                    settings.DismissCount++;
                    break;
                default:
                    _logger.LogDebug("TrackEventAsync: unrecognised event type '{EventType}' for company {CompanyId}", eventType, companyId);
                    return;
            }

            settings.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error tracking widget event '{EventType}' for company {CompanyId}", eventType, companyId);
        }
    }
}
