using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.DTOs;
using PushDashboard.Models;
using PushDashboard.Services.Integrations;
using PushDashboard.Services.Integrations.Common.Models;
using PushDashboard.Services.Notifications;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PushDashboard.Services.Modules.GiftWheel;

/// <summary>
/// Main service for Gift Wheel functionality
/// </summary>
public class GiftWheelService : IGiftWheelService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<GiftWheelService> _logger;
    private readonly EcommerceGiftVoucherFactory _giftVoucherFactory;
    private readonly INotificationChannelFactory _notificationChannelFactory;
    private readonly IModuleUsageService _moduleUsageService;
    private readonly IGiftWheelModuleService _giftWheelModuleService;
    private readonly IR2StorageService _r2StorageService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IEcommerceServiceFactory _ecommerceServiceFactory;

    public GiftWheelService(
        ApplicationDbContext context,
        ILogger<GiftWheelService> logger,
        EcommerceGiftVoucherFactory giftVoucherFactory,
        INotificationChannelFactory notificationChannelFactory,
        IModuleUsageService moduleUsageService,
        IGiftWheelModuleService giftWheelModuleService,
        IR2StorageService r2StorageService,
        IWebHostEnvironment webHostEnvironment,
        IEcommerceServiceFactory ecommerceServiceFactory)
    {
        _context = context;
        _logger = logger;
        _giftVoucherFactory = giftVoucherFactory;
        _notificationChannelFactory = notificationChannelFactory;
        _moduleUsageService = moduleUsageService;
        _giftWheelModuleService = giftWheelModuleService;
        _r2StorageService = r2StorageService;
        _webHostEnvironment = webHostEnvironment;
        _ecommerceServiceFactory = ecommerceServiceFactory;
    }

    #region Wheel Management

    /// <summary>
    /// Fetches KVKK and ETK links from Integration Settings for a company
    /// </summary>
    private async Task<(string? PrivacyPolicyUrl, string? TermsUrl)> GetLegalLinksFromIntegrationAsync(Guid companyId)
    {
        string? privacyPolicyUrl = null;
        string? termsUrl = null;

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
                // 1. Try to get from explicit columns (Standard way for Ticimax, etc.)
                if (!string.IsNullOrEmpty(ecommerceIntegration.KvkkLink))
                {
                    privacyPolicyUrl = ecommerceIntegration.KvkkLink;
                }
                else if (!string.IsNullOrEmpty(ecommerceIntegration.SettingsJson))
                {
                    // 2. Fallback: Try to get from JSON (Legacy or other integrations)
                    try
                    {
                        using var doc = JsonDocument.Parse(ecommerceIntegration.SettingsJson);
                        var root = doc.RootElement;

                        if ((root.TryGetProperty("KvkkLink", out var kvkkLinkProp) || root.TryGetProperty("kvkkLink", out kvkkLinkProp)) &&
                            kvkkLinkProp.ValueKind == JsonValueKind.String)
                        {
                            var link = kvkkLinkProp.GetString();
                            if (!string.IsNullOrEmpty(link)) privacyPolicyUrl = link;
                        }
                    }
                    catch { /* Ignore JSON parse errors */ }
                }

                if (!string.IsNullOrEmpty(ecommerceIntegration.TicariElektronikIletiLink))
                {
                    termsUrl = ecommerceIntegration.TicariElektronikIletiLink;
                }
                else if (!string.IsNullOrEmpty(ecommerceIntegration.SettingsJson))
                {
                    // 2. Fallback
                    try
                    {
                        using var doc = JsonDocument.Parse(ecommerceIntegration.SettingsJson);
                        var root = doc.RootElement;

                        if ((root.TryGetProperty("EtkLink", out var etkLinkProp) ||
                             root.TryGetProperty("ticariElektronikIletiLink", out etkLinkProp)) &&
                            etkLinkProp.ValueKind == JsonValueKind.String)
                        {
                            var link = etkLinkProp.GetString();
                            if (!string.IsNullOrEmpty(link)) termsUrl = link;
                        }
                    }
                    catch { /* Ignore JSON parse errors */ }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch Integration Settings for Legal Texts for company {CompanyId}", companyId);
        }

        return (privacyPolicyUrl, termsUrl);
    }

    public async Task<Models.GiftWheel?> GetCompanyWheelAsync(Guid companyId)
    {
        try
        {
            return await _context.GiftWheels
                .Include(w => w.Settings)
                .FirstOrDefaultAsync(w => w.CompanyId == companyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company wheel for {CompanyId}", companyId);
            return null;
        }
    }

    public async Task<(bool Success, string Message)> CreateOrUpdateWheelAsync(Guid companyId, string wheelName, string userId)
    {
        try
        {
            var existingWheel = await _context.GiftWheels
                .FirstOrDefaultAsync(w => w.CompanyId == companyId);

            if (existingWheel != null)
            {
                existingWheel.Name = wheelName;
                existingWheel.UpdatedAt = DateTime.Now;
                _context.GiftWheels.Update(existingWheel);
            }
            else
            {
                var newWheel = new Models.GiftWheel
                {
                    CompanyId = companyId,
                    Name = wheelName,
                    IsActive = true,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                _context.GiftWheels.Add(newWheel);
                await _context.SaveChangesAsync();

                // Create default settings
                var defaultSettings = new GiftWheelSettings
                {
                    GiftWheelId = newWheel.Id,
                    UpdatedByUserId = userId
                };

                _context.GiftWheelSettings.Add(defaultSettings);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Gift wheel created/updated for company {CompanyId}", companyId);

            return (true, "Hediye çarkı başarıyla kaydedildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating wheel for company {CompanyId}", companyId);
            return (false, "Hediye çarkı kaydedilirken hata oluştu.");
        }
    }

    public async Task<(bool Success, string Message)> ToggleWheelStatusAsync(Guid companyId, bool isActive, string userId)
    {
        try
        {
            var wheel = await _context.GiftWheels
                .FirstOrDefaultAsync(w => w.CompanyId == companyId);

            if (wheel == null)
            {
                return (false, "Hediye çarkı bulunamadı.");
            }

            wheel.IsActive = isActive;
            wheel.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            var status = isActive ? "aktif" : "pasif";
            _logger.LogInformation("Gift wheel status changed to {Status} for company {CompanyId}", status, companyId);

            return (true, $"Hediye çarkı {status} duruma getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling wheel status for company {CompanyId}", companyId);
            return (false, "Durum değiştirilirken hata oluştu.");
        }
    }

    #endregion

    #region Prize Management

    public async Task<List<GiftWheelPrizeDto>> GetWheelPrizesAsync(int wheelId)
    {
        try
        {
            var prizes = await _context.GiftWheelPrizes
                .Where(p => p.GiftWheelId == wheelId)
                .OrderBy(p => p.SortOrder)
                .Select(p => new GiftWheelPrizeDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    PrizeType = p.PrizeType,
                    DiscountAmount = p.DiscountAmount,
                    DiscountType = p.DiscountType,
                    ValidityDays = p.ValidityDays,
                    VoucherMode = p.VoucherMode,
                    VoucherCodeFormat = p.VoucherCodeFormat,
                    VoucherPrefix = p.VoucherPrefix,
                    AllowDiscountedProducts = p.AllowDiscountedProducts,
                    CombineWithOtherCampaigns = p.CombineWithOtherCampaigns,
                    MinimumOrderAmount = p.MinimumOrderAmount,
                    Probability = p.Probability,
                    Color = p.Color,
                    TextColor = p.TextColor,
                    IsActive = p.IsActive,
                    SortOrder = p.SortOrder
                })
                .ToListAsync();

            return prizes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting prizes for wheel {WheelId}", wheelId);
            return new List<GiftWheelPrizeDto>();
        }
    }

    public async Task<(bool Success, string Message, GiftWheelPrizeDto? Prize)> AddPrizeAsync(int wheelId, GiftWheelPrizeRequest request)
    {
        try
        {
            // Validate wheel exists
            var wheel = await _context.GiftWheels.FindAsync(wheelId);
            if (wheel == null)
            {
                return (false, "Hediye çarkı bulunamadı.", null);
            }

            // Validate probability doesn't exceed 100% total
            var totalProbability = await _context.GiftWheelPrizes
                .Where(p => p.GiftWheelId == wheelId && p.IsActive)
                .SumAsync(p => p.Probability);

            if (totalProbability + request.Probability > 100)
            {
                return (false, $"Toplam olasılık %100'ü geçemez. Mevcut: %{totalProbability}, Eklenmek istenen: %{request.Probability}", null);
            }

            var validityDays = request.VoucherMode == "usage" ? 365 : request.ValidityDays;

            var prize = new GiftWheelPrize
            {
                GiftWheelId = wheelId,
                Name = request.Name,
                PrizeType = request.PrizeType,
                DiscountAmount = request.DiscountAmount,
                DiscountType = request.DiscountType,
                ValidityDays = validityDays,
                VoucherMode = request.VoucherMode,
                VoucherCodeFormat = request.VoucherCodeFormat,
                VoucherPrefix = request.VoucherPrefix,
                AllowDiscountedProducts = request.AllowDiscountedProducts,
                CombineWithOtherCampaigns = request.CombineWithOtherCampaigns,
                MinimumOrderAmount = request.PrizeType == "message" ? null : NormalizeMinimumOrderAmount(request.MinimumOrderAmount),
                Probability = request.Probability,
                Color = request.Color,
                TextColor = request.TextColor,
                IsActive = request.IsActive,
                SortOrder = request.SortOrder,
                CreatedAt = DateTime.Now
            };

            _context.GiftWheelPrizes.Add(prize);
            await _context.SaveChangesAsync();

            var prizeDto = new GiftWheelPrizeDto
            {
                Id = prize.Id,
                Name = prize.Name,
                PrizeType = prize.PrizeType,
                DiscountAmount = prize.DiscountAmount,
                DiscountType = prize.DiscountType,
                ValidityDays = prize.ValidityDays,
                VoucherMode = prize.VoucherMode,
                VoucherCodeFormat = prize.VoucherCodeFormat,
                VoucherPrefix = prize.VoucherPrefix,
                AllowDiscountedProducts = prize.AllowDiscountedProducts,
                CombineWithOtherCampaigns = prize.CombineWithOtherCampaigns,
                MinimumOrderAmount = prize.MinimumOrderAmount,
                Probability = prize.Probability,
                Color = prize.Color,
                TextColor = prize.TextColor,
                IsActive = prize.IsActive,
                SortOrder = prize.SortOrder
            };

            _logger.LogInformation("Prize added to wheel {WheelId}: {PrizeName}", wheelId, request.Name);
            return (true, "Ödül başarıyla eklendi.", prizeDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding prize to wheel {WheelId}", wheelId);
            return (false, "Ödül eklenirken hata oluştu.", null);
        }
    }

    public async Task<(bool Success, string Message)> UpdatePrizeAsync(int prizeId, GiftWheelPrizeRequest request)
    {
        try
        {
            var prize = await _context.GiftWheelPrizes.FindAsync(prizeId);
            if (prize == null)
            {
                return (false, "Ödül bulunamadı.");
            }

            // Validate probability doesn't exceed 100% total (excluding current prize)
            var totalProbability = await _context.GiftWheelPrizes
                .Where(p => p.GiftWheelId == prize.GiftWheelId && p.IsActive && p.Id != prizeId)
                .SumAsync(p => p.Probability);

            if (totalProbability + request.Probability > 100)
            {
                return (false, $"Toplam olasılık %100'ü geçemez. Diğer ödüllerin toplamı: %{totalProbability}, Yeni değer: %{request.Probability}");
            }

            prize.Name = request.Name;
            prize.PrizeType = request.PrizeType;
            prize.DiscountAmount = request.DiscountAmount;
            prize.DiscountType = request.DiscountType;
            prize.ValidityDays = request.VoucherMode == "usage" ? 365 : request.ValidityDays;
            prize.VoucherMode = request.VoucherMode;
            prize.VoucherCodeFormat = request.VoucherCodeFormat;
            prize.VoucherPrefix = request.VoucherPrefix;
            prize.AllowDiscountedProducts = request.AllowDiscountedProducts;
            prize.CombineWithOtherCampaigns = request.CombineWithOtherCampaigns;
            prize.MinimumOrderAmount = request.PrizeType == "message" ? null : NormalizeMinimumOrderAmount(request.MinimumOrderAmount);
            prize.Probability = request.Probability;
            prize.Color = request.Color;
            prize.TextColor = request.TextColor;
            prize.IsActive = request.IsActive;
            prize.SortOrder = request.SortOrder;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Prize updated: {PrizeId} - {PrizeName}", prizeId, request.Name);
            return (true, "Ödül başarıyla güncellendi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating prize {PrizeId}", prizeId);
            return (false, "Ödül güncellenirken hata oluştu.");
        }
    }

    public async Task<(bool Success, string Message)> DeletePrizeAsync(int prizeId)
    {
        try
        {
            var prize = await _context.GiftWheelPrizes.FindAsync(prizeId);
            if (prize == null)
            {
                return (false, "Ödül bulunamadı.");
            }

            _context.GiftWheelPrizes.Remove(prize);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Prize deleted: {PrizeId} - {PrizeName}", prizeId, prize.Name);
            return (true, "Ödül başarıyla silindi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting prize {PrizeId}", prizeId);
            return (false, "Ödül silinirken hata oluştu.");
        }
    }

    public async Task<(bool Success, string Message)> DeleteAllPrizesAsync(int wheelId)
    {
        try
        {
            var prizes = await _context.GiftWheelPrizes
                .Where(p => p.GiftWheelId == wheelId)
                .ToListAsync();

            if (!prizes.Any())
            {
                return (false, "Silinecek ödül bulunamadı.");
            }

            _context.GiftWheelPrizes.RemoveRange(prizes);
            await _context.SaveChangesAsync();

            _logger.LogInformation("All prizes deleted for wheel: {WheelId}", wheelId);
            return (true, "Tüm ödüller başarıyla silindi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting all prizes for wheel {WheelId}", wheelId);
            return (false, "Ödüller silinirken hata oluştu.");
        }
    }

    public async Task<(bool Success, string Message)> ReorderPrizesAsync(int wheelId, Dictionary<int, int> prizeOrders)
    {
        try
        {
            foreach (var kvp in prizeOrders)
            {
                var prize = await _context.GiftWheelPrizes.FindAsync(kvp.Key);
                if (prize != null && prize.GiftWheelId == wheelId)
                {
                    prize.SortOrder = kvp.Value;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Prizes reordered for wheel {WheelId}", wheelId);
            return (true, "Ödül sıralaması güncellendi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering prizes for wheel {WheelId}", wheelId);
            return (false, "Sıralama güncellenirken hata oluştu.");
        }
    }

    /// <summary>
    /// Toggles the status of a specific prize
    /// </summary>
    public async Task<(bool Success, string Message)> TogglePrizeStatusAsync(int prizeId, bool isActive)
    {
        try
        {
            var prize = await _context.GiftWheelPrizes.FindAsync(prizeId);
            if (prize == null)
            {
                return (false, "Ödül bulunamadı.");
            }

            prize.IsActive = isActive;
            await _context.SaveChangesAsync();

            var statusText = isActive ? "aktif" : "pasif";
            _logger.LogInformation("Prize {PrizeId} status changed to {Status}", prizeId, statusText);
            return (true, $"Ödül {statusText} duruma getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling prize status for prize {PrizeId}", prizeId);
            return (false, "Ödül durumu değiştirilirken hata oluştu.");
        }
    }

    /// <summary>
    /// Toggles the status of all prizes in a wheel
    /// </summary>
    public async Task<(bool Success, string Message)> ToggleAllPrizesAsync(int wheelId, bool isActive)
    {
        try
        {
            var wheel = await _context.GiftWheels
                .Include(w => w.Prizes)
                .FirstOrDefaultAsync(w => w.Id == wheelId);

            if (wheel == null)
            {
                return (false, "Çark bulunamadı.");
            }

            foreach (var prize in wheel.Prizes)
            {
                prize.IsActive = isActive;
            }

            await _context.SaveChangesAsync();

            var statusText = isActive ? "aktif" : "pasif";
            _logger.LogInformation("All prizes in wheel {WheelId} status changed to {Status}", wheelId, statusText);
            return (true, $"Tüm ödüller {statusText} duruma getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling all prizes status for wheel {WheelId}", wheelId);
            return (false, "Ödül durumları değiştirilirken hata oluştu.");
        }
    }

    #endregion

    #region Settings Management

    public async Task<GiftWheelSettingsDto?> GetWheelSettingsAsync(int wheelId)
    {
        try
        {
            var settings = await _context.GiftWheelSettings
                .Include(s => s.GiftWheel)
                .FirstOrDefaultAsync(s => s.GiftWheelId == wheelId);

            if (settings == null)
            {
                return null;
            }

            // Fetch legal text links from Integration Settings
            var (privacyPolicyUrl, termsUrl) = await GetLegalLinksFromIntegrationAsync(settings.GiftWheel.CompanyId);

            // Fallback to database values if integration doesn't have links
            privacyPolicyUrl ??= settings.PrivacyPolicyUrl;
            termsUrl ??= settings.TermsUrl;

            return new GiftWheelSettingsDto
            {
                WheelId = wheelId,
                WheelTitle = settings.WheelTitle,
                WheelSubtitle = settings.WheelSubtitle,
                ButtonText = settings.ButtonText,
                WinMessage = settings.WinMessage,
                LoseMessage = settings.LoseMessage,
                MaxSpinsPerDay = settings.MaxSpinsPerDay,
                RequirePhone = settings.RequirePhone,
                RequireEmail = settings.RequireEmail,
                NotificationTemplate = settings.NotificationTemplate,
                PrimaryColor = settings.PrimaryColor,
                SecondaryColor = settings.SecondaryColor,
                WheelSize = settings.WheelSize,
                Layout = settings.Layout,
                LogoUrl = settings.LogoUrl,
                WinFrequencyDays = settings.WinFrequencyDays,
                LogoAnimation = settings.LogoAnimation,
                LogoSizePercentage = settings.LogoSizePercentage > 0 ? settings.LogoSizePercentage : 15,
                BackgroundImageUrl = settings.BackgroundImageUrl,
                ModalTextColor = string.IsNullOrEmpty(settings.ModalTextColor) ? "#1f2937" : settings.ModalTextColor,
                PrivacyPolicyUrl = privacyPolicyUrl,
                TermsUrl = termsUrl,
                ShowConfetti = settings.ShowConfetti,
                ButtonPosition = string.IsNullOrEmpty(settings.ButtonPosition) ? "right" : settings.ButtonPosition,
                ButtonBottomPx = settings.ButtonBottomPx,
                ButtonMobileBottomPx = settings.ButtonMobileBottomPx,
                WhatsappTemplateId = settings.WhatsappTemplateId,
                WhatsappTemplateName = settings.WhatsappTemplateName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting settings for wheel {WheelId}", wheelId);
            return null;
        }
    }

    public async Task<(bool Success, string Message)> UpdateWheelSettingsAsync(int wheelId, UpdateGiftWheelSettingsRequest request, string userId)
    {
        try
        {
            var settings = await _context.GiftWheelSettings
                .FirstOrDefaultAsync(s => s.GiftWheelId == wheelId);

            if (settings == null)
            {
                settings = new GiftWheelSettings
                {
                    GiftWheelId = wheelId,
                    UpdatedByUserId = userId
                };
                _context.GiftWheelSettings.Add(settings);
            }

            settings.WheelTitle = request.WheelTitle;
            settings.WheelSubtitle = request.WheelSubtitle;
            settings.ButtonText = request.ButtonText;
            settings.WinMessage = request.WinMessage;
            settings.LoseMessage = request.LoseMessage;
            settings.MaxSpinsPerDay = request.MaxSpinsPerDay;
            settings.WinFrequencyDays = request.WinFrequencyDays;
            settings.RequirePhone = request.RequirePhone;
            settings.RequireEmail = request.RequireEmail;
            settings.NotificationTemplate = request.NotificationTemplate;
            settings.PrimaryColor = request.PrimaryColor;
            settings.SecondaryColor = request.SecondaryColor;
            settings.WheelSize = request.WheelSize;
            settings.Layout = request.Layout;
            settings.LogoUrl = request.LogoUrl;
            settings.LogoAnimation = request.LogoAnimation;
            settings.LogoSizePercentage = request.LogoSizePercentage > 0 ? request.LogoSizePercentage : 15;
            settings.BackgroundImageUrl = request.BackgroundImageUrl;
            settings.ModalTextColor = string.IsNullOrEmpty(request.ModalTextColor) ? "#1f2937" : request.ModalTextColor;
            // settings.PrivacyPolicyUrl = request.PrivacyPolicyUrl; // Managed by Integration Settings
            // settings.TermsUrl = request.TermsUrl; // Managed by Integration Settings
            settings.ShowConfetti = request.ShowConfetti;
            settings.ButtonPosition = string.IsNullOrEmpty(request.ButtonPosition) ? "right" : request.ButtonPosition;
            settings.ButtonBottomPx = request.ButtonBottomPx;
            settings.ButtonMobileBottomPx = request.ButtonMobileBottomPx;
            settings.WhatsappTemplateId = request.WhatsappTemplateId;
            settings.WhatsappTemplateName = request.WhatsappTemplateName;
            settings.UpdatedAt = DateTime.Now;
            settings.UpdatedByUserId = userId;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Wheel settings updated for wheel {WheelId}", wheelId);
            return (true, "Çark ayarları başarıyla güncellendi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings for wheel {WheelId}", wheelId);
            return (false, "Ayarlar güncellenirken hata oluştu.");
        }
    }

    #endregion

    #region Spin Processing

    public async Task<GiftWheelSpinResult> ProcessSpinAsync(Guid companyId, GiftWheelSpinRequest request, string ipAddress)
    {
        try
        {
            // Validate spin eligibility
            var (canSpin, message) = await ValidateSpinEligibilityAsync(companyId, request.CustomerPhone, ipAddress);
            if (!canSpin)
            {
                return new GiftWheelSpinResult
                {
                    Success = false,
                    Message = message,
                    ErrorCode = "SPIN_NOT_ALLOWED"
                };
            }

            // Get company wheel
            var wheel = await GetCompanyWheelAsync(companyId);
            if (wheel == null || !wheel.IsActive)
            {
                return new GiftWheelSpinResult
                {
                    Success = false,
                    Message = "Hediye çarkı bulunamadı veya aktif değil.",
                    ErrorCode = "WHEEL_NOT_FOUND"
                };
            }

            // Determine winning prize
            var winningPrize = await DetermineWinningPrizeAsync(wheel.Id);
            if (winningPrize == null)
            {
                return new GiftWheelSpinResult
                {
                    Success = false,
                    Message = "Ödül belirlenirken hata oluştu.",
                    ErrorCode = "PRIZE_DETERMINATION_ERROR"
                };
            }

            // Create spin record
            var spin = new Models.GiftWheelSpin
            {
                GiftWheelId = wheel.Id,
                CustomerName = request.CustomerName,
                CustomerPhone = request.CustomerPhone,
                CustomerEmail = request.CustomerEmail,
                SmsPermissionGranted = request.CommunicationConsent,
                EmailPermissionGranted = request.CommunicationConsent,
                PrizeId = winningPrize.Id,
                IpAddress = ipAddress,
                SpinDate = DateTime.Now
            };

            var result = new GiftWheelSpinResult
            {
                Success = true,
                Message = "Çark başarıyla çevrildi!",
                Prize = new GiftWheelPrizeDto
                {
                    Id = winningPrize.Id,
                    Name = winningPrize.Name,
                    PrizeType = winningPrize.PrizeType,
                    DiscountAmount = winningPrize.DiscountAmount,
                    DiscountType = winningPrize.DiscountType,
                    ValidityDays = winningPrize.ValidityDays,
                    VoucherMode = winningPrize.VoucherMode,
                    VoucherCodeFormat = winningPrize.VoucherCodeFormat,
                    VoucherPrefix = winningPrize.VoucherPrefix,
                    AllowDiscountedProducts = winningPrize.AllowDiscountedProducts,
                    CombineWithOtherCampaigns = winningPrize.CombineWithOtherCampaigns,
                    MinimumOrderAmount = winningPrize.MinimumOrderAmount,
                    Probability = winningPrize.Probability,
                    Color = winningPrize.Color,
                    TextColor = winningPrize.TextColor,
                    IsActive = winningPrize.IsActive,
                    SortOrder = winningPrize.SortOrder
                }
            };

            // ADIM 1: Telefon numarası validasyonu
            if (!IsValidPhoneNumber(request.CustomerPhone))
            {
                spin.IsSuccessful = false;
                spin.ErrorMessage = "Geçersiz telefon numarası formatı.";

                _context.GiftWheelSpins.Add(spin);
                await _context.SaveChangesAsync();

                _logger.LogWarning(
                    "ProcessSpinAsync: Invalid phone number. SpinId={SpinId}, Phone={Phone}",
                    spin.Id, request.CustomerPhone);

                return new GiftWheelSpinResult
                {
                    Success = false,
                    Message = "Geçersiz telefon numarası girdiniz. Hediye çekinizin tanımlanması için geçerli bir telefon numarası girerek tekrar deneyin.",
                    ErrorCode = "INVALID_PHONE_NUMBER",
                    Prize = result.Prize
                };
            }

            await CreateOrUpdateCustomerFromSpinAsync(companyId, request);
            var whatsAppChannel = await _notificationChannelFactory.GetChannelAsync(companyId, "whatsapp");
            var hasWhatsAppIntegration = whatsAppChannel != null;

                // Hediye çarkı modül ayarlarından bildirim maliyetini al (mümkünse)
                decimal? notificationCostPerMessage = null;
                try
                {
                    var moduleSettings = await _giftWheelModuleService.GetModuleSettingsAsync(companyId);
                    notificationCostPerMessage = moduleSettings.NotificationCost;
                }
                catch
                {
                    notificationCostPerMessage = null;
                }

            if ((winningPrize.PrizeType == "voucher" || winningPrize.PrizeType == "cargo") && winningPrize.DiscountAmount.HasValue)
            {
                try
                {
                    var prizeTypeLabel = winningPrize.PrizeType == "cargo" ? "kargo indirimi" : "hediye çeki";
                    _logger.LogInformation(
                        "ProcessSpinAsync: Starting {PrizeTypeLabel} creation. Phone={Phone}, Prize={Prize}, Amount={Amount}",
                        prizeTypeLabel, request.CustomerPhone, winningPrize.Name, winningPrize.DiscountAmount);

                    var existingCustomer = await _context.Customers
                        .FirstOrDefaultAsync(c => c.CompanyId == companyId &&
                            (c.Phone == request.CustomerPhone || c.MobilePhone == request.CustomerPhone));

                    var customer = new Customer
                    {
                        FirstName = request.CustomerName.Split(' ').FirstOrDefault() ?? request.CustomerName,
                        LastName = request.CustomerName.Split(' ').Skip(1).FirstOrDefault() ?? "",
                        Phone = request.CustomerPhone,
                        MobilePhone = request.CustomerPhone,
                        Email = request.CustomerEmail ?? "",
                        ExternalId = existingCustomer?.ExternalId ?? 0
                    };

                    _logger.LogInformation(
                        "ProcessSpinAsync: Customer info for voucher. ExternalId={ExternalId}, Name={Name}, Phone={Phone}",
                        customer.ExternalId, customer.FullName, customer.Phone);

                    var (voucherSuccess, voucherCode, voucherMessage) = await CreateGiftVoucherAsync(companyId, customer, winningPrize);

                    if (voucherSuccess && !string.IsNullOrEmpty(voucherCode))
                    {
                        spin.VoucherCode = voucherCode;
                        spin.VoucherCreated = true;
                        spin.IsSuccessful = true;
                        result.VoucherCode = voucherCode;
                        result.VoucherCreated = true;

                        _context.GiftWheelSpins.Add(spin);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation(
                            "ProcessSpinAsync: Voucher created successfully. SpinId={SpinId}, VoucherCode={VoucherCode}",
                            spin.Id, voucherCode);
                    }
                    else
                    {
                        spin.IsSuccessful = false;
                        spin.ErrorMessage = voucherMessage;
                        result.Success = false;
                        result.Message = voucherMessage;
                        result.ErrorCode = "VOUCHER_CREATION_FAILED";

                        _context.GiftWheelSpins.Add(spin);
                        await _context.SaveChangesAsync();

                        _logger.LogWarning(
                            "ProcessSpinAsync: Voucher creation failed. SpinId={SpinId}, Error={Error}",
                            spin.Id, voucherMessage);

                        return result;
                    }
                }
                catch (Exception voucherEx)
                {
                    spin.IsSuccessful = false;
                    spin.ErrorMessage = $"Hediye çeki oluşturma hatası: {voucherEx.Message}";
                    result.Success = false;
                    result.Message = "Hediye çeki oluşturulurken bir hata oluştu.";
                    result.ErrorCode = "VOUCHER_EXCEPTION";

                    _context.GiftWheelSpins.Add(spin);
                    await _context.SaveChangesAsync();

                    _logger.LogError(voucherEx,
                        "ProcessSpinAsync: Voucher creation exception. SpinId={SpinId}, Phone={Phone}",
                        spin.Id, request.CustomerPhone);

                    return result;
                }
            }
            else
            {
                spin.IsSuccessful = true;

                _context.GiftWheelSpins.Add(spin);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "ProcessSpinAsync: Non-voucher prize spin recorded. SpinId={SpinId}, Prize={Prize}",
                    spin.Id, winningPrize.Name);
            }

            // ADIM 5: WhatsApp bildirimi gönderme
            var hasSelectedWhatsAppTemplate = !string.IsNullOrWhiteSpace(wheel.Settings?.WhatsappTemplateId);

            if (hasWhatsAppIntegration && hasSelectedWhatsAppTemplate && !string.IsNullOrEmpty(spin.VoucherCode))
            {
                try
                {
                    spin.NotificationSent = true; // denendi

                    var (success, msg) = await SendPrizeNotificationAsync(companyId, spin);

                    spin.IsNotificationSuccessful = success;
                    result.NotificationSent = success;

                    if (success && notificationCostPerMessage.HasValue)
        spin.Cost = notificationCostPerMessage.Value;
    else
        spin.Cost = null;

                    if (!success)
                    {
                        var err = $"WhatsApp bildirimi gönderilemedi: {msg}";
                        spin.ErrorMessage = string.IsNullOrEmpty(spin.ErrorMessage) ? err : $"{spin.ErrorMessage}; {err}";

                        _logger.LogWarning("ProcessSpinAsync: WhatsApp notification failed. SpinId={SpinId}, Error={Error}",
                            spin.Id, msg);
                    }
                    else
                    {
                        _logger.LogInformation("ProcessSpinAsync: WhatsApp notification sent. SpinId={SpinId}", spin.Id);
                    }
                }
                catch (Exception ex)
                {
                    spin.NotificationSent = true;          // denendi
                    spin.IsNotificationSuccessful = false;
                    result.NotificationSent = false;

                    spin.Cost = null;

                    var err = $"WhatsApp bildirimi hatası: {ex.Message}";
                    spin.ErrorMessage = string.IsNullOrEmpty(spin.ErrorMessage) ? err : $"{spin.ErrorMessage}; {err}";

                    _logger.LogError(ex, "ProcessSpinAsync: WhatsApp notification exception. SpinId={SpinId}", spin.Id);
                }

                await _context.SaveChangesAsync();
            }
            else if (!hasSelectedWhatsAppTemplate && !string.IsNullOrEmpty(spin.VoucherCode))
            {
                _logger.LogInformation(
                    "ProcessSpinAsync: WhatsApp template not selected, skipping notification. SpinId={SpinId}",
                    spin.Id);
            }
            else if (!hasWhatsAppIntegration)
            {
                _logger.LogInformation(
                    "ProcessSpinAsync: WhatsApp integration not found for company {CompanyId}, skipping notification. SpinId={SpinId}",
                    companyId, spin.Id);
            }

            _logger.LogInformation(
                "ProcessSpinAsync: Spin completed. CompanyId={CompanyId}, SpinId={SpinId}, " +
                "CustomerName={CustomerName}, CustomerPhone={CustomerPhone}, Prize={PrizeName}, " +
                "VoucherCode={VoucherCode}, VoucherCreated={VoucherCreated}, NotificationSent={NotificationSent}, " +
                "ErrorMessage={ErrorMessage}",
                companyId, spin.Id, request.CustomerName, request.CustomerPhone, winningPrize.Name,
                result.VoucherCode ?? "N/A", result.VoucherCreated, result.NotificationSent,
                spin.ErrorMessage ?? "None");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing spin for company {CompanyId}", companyId);
            return new GiftWheelSpinResult
            {
                Success = false,
                Message = "Çark çevrilirken hata oluştu.",
                ErrorCode = "PROCESSING_ERROR"
            };
        }
    }

    #endregion

    #region Helper Methods

    private bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        var digitsOnly = new string(phoneNumber.Where(char.IsDigit).ToArray());
        return digitsOnly.Length >= 10 && digitsOnly.Length <= 15;
    }

    private async Task CreateOrUpdateCustomerFromSpinAsync(Guid companyId, GiftWheelSpinRequest request)
    {
        try
        {
            // Telefon numarasına göre mevcut müşteriyi ara
            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CompanyId == companyId &&
                    (c.Phone == request.CustomerPhone || c.MobilePhone == request.CustomerPhone));

            // Email ile de ara (telefon bulunamazsa)
            if (existingCustomer == null && !string.IsNullOrEmpty(request.CustomerEmail))
            {
                existingCustomer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.Email == request.CustomerEmail);
            }

            if (existingCustomer != null)
            {
                // Mevcut müşteriyi güncelle - sadece izin verilmişse true yap (false yapma)
                if (request.CommunicationConsent)
                {
                    existingCustomer.SmsPermission = true;
                    existingCustomer.EmailPermission = true;
                }

                // Email bilgisi yoksa ve request'te varsa ekle
                if (string.IsNullOrEmpty(existingCustomer.Email) && !string.IsNullOrEmpty(request.CustomerEmail))
                    existingCustomer.Email = request.CustomerEmail;

                existingCustomer.UpdatedAt = DateTime.Now;

                _logger.LogInformation("Customer permissions updated from gift wheel spin. CustomerId: {CustomerId}, CommunicationConsent: {CommunicationConsent}",
                    existingCustomer.Id, request.CommunicationConsent);
            }
            else
            {
                // Yeni müşteri oluştur - Hediye Çarkı kaynağı ile
                var nameParts = request.CustomerName.Split(' ', 2);
                var newCustomer = new Customer
                {
                    CompanyId = companyId,
                    ExternalId = await GetNextExternalIdAsync(companyId),
                    FirstName = nameParts.Length > 0 ? nameParts[0] : request.CustomerName,
                    LastName = nameParts.Length > 1 ? nameParts[1] : "",
                    Email = request.CustomerEmail ?? "",
                    MobilePhone = request.CustomerPhone,
                    SmsPermission = request.CommunicationConsent,
                    EmailPermission = request.CommunicationConsent,
                    Source = CustomerSource.GiftWheel,
                    IsActive = true,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    MembershipDate = DateTime.Now
                };

                _context.Customers.Add(newCustomer);

                _logger.LogInformation("New potential customer created from gift wheel. Phone: {Phone}, Source: GiftWheel, CommunicationConsent: {CommunicationConsent}",
                    request.CustomerPhone, request.CommunicationConsent);
            }

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating/updating customer from gift wheel spin. Phone: {Phone}", request.CustomerPhone);
        }
    }

    private async Task<int> GetNextExternalIdAsync(Guid companyId)
    {
        var maxExternalId = await _context.Customers
            .Where(c => c.CompanyId == companyId)
            .MaxAsync(c => (int?)c.ExternalId) ?? 0;

        return maxExternalId + 1;
    }

    public async Task<(bool CanSpin, string Message)> ValidateSpinEligibilityAsync(Guid companyId, string customerPhone, string ipAddress)
    {
        try
        {
            // Check if company has active module
            if (!await HasActiveModuleAsync(companyId))
            {
                return (false, "Hediye çarkı modülü aktif değil.");
            }

            var wheel = await GetCompanyWheelAsync(companyId);
            if (wheel?.Settings == null)
            {
                return (false, "Çark ayarları bulunamadı.");
            }

            // Check daily spin limit per phone number.
            // "message" tipi ödüller günlük hak tüketmez; kullanıcı aynı gün tekrar çevirebilir.
            var today = DateTime.Today;
            var todaySpins = await _context.GiftWheelSpins
                .CountAsync(s => s.GiftWheelId == wheel.Id &&
                               s.CustomerPhone == customerPhone &&
                               s.SpinDate >= today &&
                               s.IsSuccessful &&
                               s.Prize.PrizeType != "message");

            if (todaySpins >= wheel.Settings.MaxSpinsPerDay)
            {
                return (false, $"Günlük maksimum çevirme sayısına ulaştınız. ({wheel.Settings.MaxSpinsPerDay})");
            }

            // Check win frequency limit (aynı telefon numarası ne sıklıkla kazanabilir)
            if (wheel.Settings.WinFrequencyDays > 0)
            {
                var frequencyStartDate = DateTime.Now.AddDays(-wheel.Settings.WinFrequencyDays);
                var recentWin = await _context.GiftWheelSpins
                    .AnyAsync(s => s.GiftWheelId == wheel.Id &&
                                   s.CustomerPhone == customerPhone &&
                                   s.SpinDate >= frequencyStartDate &&
                                   s.IsSuccessful &&
                                   s.VoucherCreated);

                if (recentWin)
                {
                    var frequencyText = wheel.Settings.WinFrequencyDays switch
                    {
                        7 => "haftada",
                        30 => "ayda",
                        90 => "3 ayda",
                        180 => "6 ayda",
                        365 => "yılda",
                        _ => $"{wheel.Settings.WinFrequencyDays} günde"
                    };
                    return (false, $"Bu telefon numarası ile {frequencyText} bir kez ödül kazanabilirsiniz.");
                }
            }

            // IP bazlı rate limiting Program.cs'deki GiftWheelSpinPolicy ile merkezi olarak yönetiliyor
            // Burada ekstra IP kontrolü yapılmıyor - tek kontrol noktası Program.cs'deki rate limiting middleware'i

            return (true, "Çevirme izni verildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating spin eligibility for company {CompanyId}", companyId);
            return (false, "Doğrulama sırasında hata oluştu.");
        }
    }

    public async Task<GiftWheelPrize?> DetermineWinningPrizeAsync(int wheelId)
    {
        try
        {
            var prizes = await _context.GiftWheelPrizes
                .Where(p => p.GiftWheelId == wheelId && p.IsActive)
                .OrderBy(p => p.SortOrder)
                .ToListAsync();

            if (!prizes.Any())
            {
                return null;
            }

            // Calculate total probability
            var totalProbability = prizes.Sum(p => p.Probability);
            if (totalProbability <= 0)
            {
                return null;
            }

            // Generate random number
            var random = new Random();
            var randomValue = random.Next(1, totalProbability + 1);

            // Determine winning prize based on probability
            var currentSum = 0;
            foreach (var prize in prizes)
            {
                currentSum += prize.Probability;
                if (randomValue <= currentSum)
                {
                    return prize;
                }
            }

            // Fallback to first prize if something goes wrong
            return prizes.First();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining winning prize for wheel {WheelId}", wheelId);
            return null;
        }
    }

    public async Task<(bool Success, string? VoucherCode, string Message)> CreateGiftVoucherAsync(Guid companyId, Customer customer, GiftWheelPrize prize)
    {
        try
        {
            if (!(prize.PrizeType == "voucher" || prize.PrizeType == "cargo") || !prize.DiscountAmount.HasValue)
            {
                _logger.LogWarning("CreateGiftVoucherAsync: Invalid prize type or no discount amount. PrizeType={PrizeType}, DiscountAmount={DiscountAmount}",
                    prize.PrizeType, prize.DiscountAmount);
                return (false, null, "Bu ödül için hediye çeki oluşturulamaz.");
            }

            if (prize.VoucherMode == "usage" && prize.VoucherCodeFormat == "fixed_prefix")
            {
                _logger.LogWarning("CreateGiftVoucherAsync: Kullanım bazlı modda sabit ön ek kullanılamaz. CompanyId={CompanyId}, PrizeId={PrizeId}",
                    companyId, prize.Id);
                return (false, null, "Kullanım bazlı modda sabit ön ek kullanılamaz.");
            }

            var validityDays = prize.VoucherMode == "usage" ? 365 : (prize.ValidityDays ?? 30);
            var usageLimit = prize.VoucherMode == "usage" ? 1 : 1;

            _logger.LogInformation(
                "CreateGiftVoucherAsync: Creating gift voucher. CompanyId={CompanyId}, CustomerName={CustomerName}, " +
                "CustomerPhone={CustomerPhone}, CustomerExternalId={CustomerExternalId}, " +
                "PrizeName={PrizeName}, DiscountAmount={DiscountAmount}, DiscountType={DiscountType}, " +
                "VoucherMode={VoucherMode}, ValidityDays={ValidityDays}, CodeFormat={CodeFormat}",
                companyId, customer.FullName, customer.Phone, customer.ExternalId,
                prize.Name, prize.DiscountAmount, prize.DiscountType,
                prize.VoucherMode, validityDays, prize.VoucherCodeFormat);

            var giftVoucherSettings = new GiftVoucherSettings
            {
                Amount = (double)prize.DiscountAmount.Value,
                DiscountType = prize.DiscountType ?? 1,
                ValidityDays = validityDays,
                Description = "Hediye Çarkı - Dijitaldesin",
                UsageLimit = usageLimit,
                IsFreeShipping = prize.PrizeType == "cargo",
                VoucherCodeFormat = prize.VoucherCodeFormat,
                VoucherPrefix = prize.VoucherPrefix,
                VoucherMode = prize.VoucherMode,
                AllowDiscountedProducts = prize.AllowDiscountedProducts,
                CombineWithOtherCampaigns = prize.CombineWithOtherCampaigns,
                MinOrderAmount = prize.MinimumOrderAmount
            };

            var result = await _giftVoucherFactory.CreateGiftVoucherAsync(companyId, customer, giftVoucherSettings);

            _logger.LogInformation(
                "CreateGiftVoucherAsync: Factory result. CompanyId={CompanyId}, Success={Success}, " +
                "VoucherCode={VoucherCode}, ErrorMessage={ErrorMessage}",
                companyId, result.Success, result.VoucherCode, result.ErrorMessage);

            if (result.Success && !string.IsNullOrEmpty(result.VoucherCode))
            {
                await LogUsageAndDeductCostAsync(companyId, "Hediye Çeki Oluşturma", 0m,
                    $"Kupon kodu {result.VoucherCode}", "system", result.VoucherCode);

                return (true, result.VoucherCode, "Hediye çeki başarıyla oluşturuldu.");
            }

            _logger.LogWarning("CreateGiftVoucherAsync: Voucher creation failed. CompanyId={CompanyId}, Error={Error}",
                companyId, result.ErrorMessage);
            return (false, null, result.ErrorMessage ?? "Hediye çeki oluşturulamadı.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateGiftVoucherAsync: Exception creating gift voucher. CompanyId={CompanyId}", companyId);
            return (false, null, "Hediye çeki oluşturulurken hata oluştu.");
        }
    }

    private static decimal? NormalizeMinimumOrderAmount(decimal? amount)
    {
        if (!amount.HasValue || amount.Value <= 0)
        {
            return null;
        }

        return Math.Round(amount.Value, 2);
    }

    public async Task<(bool Success, string Message)> SendPrizeNotificationAsync(Guid companyId, Models.GiftWheelSpin spin)
    {
        try
        {
            var wheel = await _context.GiftWheels
                .Include(w => w.Settings)
                .Include(w => w.Company)
                .FirstOrDefaultAsync(w => w.Id == spin.GiftWheelId);

            if (wheel?.Settings == null)
            {
                return (false, "Çark ayarları bulunamadı.");
            }

            var prize = await _context.GiftWheelPrizes.FindAsync(spin.PrizeId);
            if (prize == null)
            {
                return (false, "Ödül bilgisi bulunamadı.");
            }

            // Get WhatsApp notification channel
            var whatsAppChannel = await _notificationChannelFactory.GetChannelAsync(companyId, "whatsapp");
            if (whatsAppChannel == null)
            {
                return (false, "WhatsApp entegrasyonu bulunamadı.");
            }

            // GiftWheel modül ID'sini al
            var giftWheelModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Hediye Çarkı" && m.IsActive);
            var moduleId = giftWheelModule?.Id;

            // Prepare notification variables
            var variables = new Dictionary<string, string>
            {
                ["customer_name"] = spin.CustomerName,
                ["customer_email"] = spin.CustomerEmail ?? "",
                ["customer_phone"] = spin.CustomerPhone ?? "",
                ["prize"] = prize.Name,
            };

            if (!string.IsNullOrEmpty(spin.VoucherCode))
            {
                variables["gift_voucher_code"] = spin.VoucherCode;
                variables["gift_voucher_amount"] = prize.DiscountAmount?.ToString() ?? "";
                variables["has_gift_voucher"] = true.ToString();
                variables["validity_period"] = prize.ValidityDays?.ToString() ?? "";
                variables["discount_type"] = prize.DiscountType.ToString() ?? "";
            }

            // Create customer object for notification
            var customer = new Customer
            {
                FirstName = spin.CustomerName.Split(' ').FirstOrDefault() ?? spin.CustomerName,
                LastName = spin.CustomerName.Split(' ').Skip(1).FirstOrDefault() ?? "",
                Phone = spin.CustomerPhone,
                Email = spin.CustomerEmail ?? ""
            };

            // Send notification using template with correct moduleId
            var templateId = wheel.Settings.WhatsappTemplateId;
            if (string.IsNullOrWhiteSpace(templateId))
            {
                return (false, "WhatsApp şablonu seçilmedi.");
            }
            var success = await whatsAppChannel.SendNotificationWithCostTrackingAsync(
                companyId, customer, templateId, variables, "system", moduleId, spin.Id.ToString(), spin.SmsPermissionGranted);

            if (success)
            {
                _logger.LogInformation("Prize notification sent for spin {SpinId}", spin.Id);
                return (true, "Bildirim başarıyla gönderildi.");
            }

            return (false, "Bildirim gönderilemedi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending prize notification for spin {SpinId}", spin.Id);
            return (false, "Bildirim gönderilirken hata oluştu.");
        }
    }

    public async Task<bool> HasActiveModuleAsync(Guid companyId)
    {
        try
        {
            var giftWheelModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Hediye Çarkı" && m.IsActive);

            if (giftWheelModule == null)
            {
                return false;
            }

            // Sahiplik (IsOwned), aktiflik (IsActive) ve pasif olmama (!IsPassive) kontrolü
            return await _context.CompanyModules
                .AnyAsync(cm => cm.CompanyId == companyId &&
                               cm.ModuleId == giftWheelModule.Id &&
                               cm.IsOwned &&
                               cm.IsActive &&
                               !cm.IsPassive &&
                               (cm.ExpiresAt == null || cm.ExpiresAt > DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking active module for company {CompanyId}", companyId);
            return false;
        }
    }

    public async Task<(bool HasCredits, decimal RequiredAmount, decimal AvailableBalance)> CheckCreditsAsync(Guid companyId, decimal requiredAmount)
    {
        try
        {
            var company = await _context.Companies.FindAsync(companyId);
            if (company == null)
            {
                return (false, requiredAmount, 0);
            }

            return (company.CreditBalance >= requiredAmount, requiredAmount, company.CreditBalance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking credits for company {CompanyId}", companyId);
            return (false, requiredAmount, 0);
        }
    }

    public async Task<(bool Success, string Message)> LogUsageAndDeductCostAsync(Guid companyId, string usageType, decimal cost, string description, string userId, string? referenceId = null)
    {
        try
        {
            return await _moduleUsageService.LogUsageAndDeductCostAsync(companyId, 9, usageType, cost, description, userId, referenceId, "gift_wheel");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging usage for company {CompanyId}", companyId);
            return (false, "Kullanım kaydedilirken hata oluştu.");
        }
    }

    #endregion

    #region Statistics and Reporting

    public async Task<GiftWheelStatsDto> GetWheelStatsAsync(Guid companyId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var wheel = await GetCompanyWheelAsync(companyId);
            if (wheel == null)
            {
                return new GiftWheelStatsDto();
            }

            var query = _context.GiftWheelSpins.Where(s => s.GiftWheelId == wheel.Id);

            if (startDate.HasValue)
                query = query.Where(s => s.SpinDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(s => s.SpinDate <= endDate.Value);

            var spins = await query.ToListAsync();

            var today = DateTime.Today;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var stats = new GiftWheelStatsDto
            {
                TotalSpins = spins.Count,
                TotalVouchersCreated = spins.Count(s => s.VoucherCreated),
                TotalNotificationsSent = spins.Count(s => s.NotificationSent),
                TotalCost = spins.Sum(s => s.Cost ?? 0),
                SpinsToday = spins.Count(s => s.SpinDate >= today),
                SpinsThisWeek = spins.Count(s => s.SpinDate >= weekStart),
                SpinsThisMonth = spins.Count(s => s.SpinDate >= monthStart)
            };

            // Prize statistics
            var prizes = await _context.GiftWheelPrizes
                .Where(p => p.GiftWheelId == wheel.Id)
                .ToListAsync();

            var prizeStats = prizes.Select(p => new PrizeStatsDto
            {
                PrizeId = p.Id,
                PrizeName = p.Name,
                TimesWon = spins.Count(s => s.PrizeId == p.Id),
                WinRate = spins.Count > 0 ? (decimal)spins.Count(s => s.PrizeId == p.Id) / spins.Count * 100 : 0
            }).ToList();

            stats.PrizeStats = prizeStats;

            // Daily statistics for last 30 days
            var dailyStats = spins
                .Where(s => s.SpinDate >= DateTime.Today.AddDays(-30))
                .GroupBy(s => s.SpinDate.Date)
                .Select(g => new DailySpinStatsDto
                {
                    Date = g.Key,
                    SpinCount = g.Count(),
                    VoucherCount = g.Count(s => s.VoucherCreated)
                })
                .OrderBy(d => d.Date)
                .ToList();

            stats.DailyStats = dailyStats;

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wheel stats for company {CompanyId}", companyId);
            return new GiftWheelStatsDto();
        }
    }

    public async Task<List<RecentSpinDto>> GetRecentSpinsAsync(Guid companyId, int limit = 50)
    {
        try
        {
            var wheel = await GetCompanyWheelAsync(companyId);
            if (wheel == null)
            {
                return new List<RecentSpinDto>();
            }

            var recentSpins = await _context.GiftWheelSpins
                .Where(s => s.GiftWheelId == wheel.Id)
                .Include(s => s.Prize)
                .OrderByDescending(s => s.SpinDate)
                .Take(limit)
                .Select(s => new RecentSpinDto
                {
                    Id = s.Id,
                    CustomerName = s.CustomerName,
                    CustomerPhone = s.CustomerPhone,
                    CustomerEmail = s.CustomerEmail,
                    PrizeName = s.Prize.Name,
                    PrizeType = s.Prize.PrizeType,
                    DiscountAmount = s.Prize.DiscountAmount,
                    DiscountType = s.Prize.DiscountType,
                    VoucherCode = s.VoucherCode,
                    VoucherCreated = s.VoucherCreated,
                    VoucherUsed = s.VoucherUsed,
                    NotificationSent = s.NotificationSent,
                    IsNotificationSuccessful = s.IsNotificationSuccessful,
                    CommunicationConsent = s.SmsPermissionGranted,
                    SpinDate = s.SpinDate,
                    ErrorMessage = s.ErrorMessage,
                    IsSuccessful = s.IsSuccessful
                })
                .ToListAsync();

            return recentSpins;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent spins for company {CompanyId}", companyId);
            return new List<RecentSpinDto>();
        }
    }

    public async Task<List<DailySpinStatsDto>> GetDailyStatsAsync(Guid companyId, int days = 30)
    {
        try
        {
            var wheel = await GetCompanyWheelAsync(companyId);
            if (wheel == null)
            {
                return new List<DailySpinStatsDto>();
            }

            var startDate = DateTime.Today.AddDays(-days);
            var spins = await _context.GiftWheelSpins
                .Where(s => s.GiftWheelId == wheel.Id && s.SpinDate >= startDate)
                .ToListAsync();

            var dailyStats = spins
                .GroupBy(s => s.SpinDate.Date)
                .Select(g => new DailySpinStatsDto
                {
                    Date = g.Key,
                    SpinCount = g.Count(),
                    VoucherCount = g.Count(s => s.VoucherCreated)
                })
                .OrderBy(d => d.Date)
                .ToList();

            return dailyStats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting daily stats for company {CompanyId}", companyId);
            return new List<DailySpinStatsDto>();
        }
    }

    public async Task<GiftWheelDashboardKpiDto> GetDashboardKpisAsync(Guid companyId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var wheel = await GetCompanyWheelAsync(companyId);
            if (wheel == null)
            {
                return new GiftWheelDashboardKpiDto();
            }

            var query = _context.GiftWheelSpins
                .Include(s => s.Prize)
                .Where(s => s.GiftWheelId == wheel.Id);

            if (startDate.HasValue)
                query = query.Where(s => s.SpinDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(s => s.SpinDate <= endDate.Value);

            var spins = await query.ToListAsync();

            var today = DateTime.Now.Date;
            var weekStart = today.AddDays(-(int)today.DayOfWeek);
            var monthStart = new DateTime(today.Year, today.Month, 1);

            var totalSpins = spins.Count;
            var totalVouchersCreated = spins.Count(s => s.VoucherCreated);
            var totalVouchersUsed = spins.Count(s => s.VoucherUsed);
           var totalNotificationsSent = spins.Count(s => s.NotificationSent);
            var totalNotificationsSuccessful = spins.Count(s => s.NotificationSent && s.IsNotificationSuccessful);
            var notificationsSuccessfulThisWeek = spins.Count(s => s.SpinDate.Date >= weekStart && s.NotificationSent && s.IsNotificationSuccessful);

            var usedSpins = spins.Where(s => s.VoucherUsed && s.OrderAmount.HasValue).ToList();
            var brutSepetTutari = usedSpins.Sum(s => s.OrderAmount ?? 0);
            var totalRevenue = brutSepetTutari;
            var totalDiscountAmount = usedSpins.Where(s => s.OrderDiscountAmount.HasValue).Sum(s => s.OrderDiscountAmount ?? 0);
            var totalCost = spins.Sum(s => s.Cost ?? 0);

            return new GiftWheelDashboardKpiDto
            {
                TotalSpins = totalSpins,
                TotalVouchersCreated = totalVouchersCreated,
                TotalVouchersUsed = totalVouchersUsed,
                VoucherUsageRate = totalVouchersCreated > 0 ? Math.Round((decimal)totalVouchersUsed / totalVouchersCreated * 100, 2) : 0,
                TotalRevenue = totalRevenue,
                AverageVoucherAmount = 0,
                AverageOrderAmount = totalVouchersUsed > 0 ? Math.Round(brutSepetTutari / totalVouchersUsed, 2) : 0,
                SpinsToday = spins.Count(s => s.SpinDate.Date == today),
                SpinsThisWeek = spins.Count(s => s.SpinDate.Date >= weekStart),
                SpinsThisMonth = spins.Count(s => s.SpinDate.Date >= monthStart),
                RevenueToday = spins.Where(s => s.SpinDate.Date == today && s.OrderAmount.HasValue).Sum(s => s.OrderAmount ?? 0),
                RevenueThisWeek = spins.Where(s => s.SpinDate.Date >= weekStart && s.OrderAmount.HasValue).Sum(s => s.OrderAmount ?? 0),
                RevenueThisMonth = spins.Where(s => s.SpinDate.Date >= monthStart && s.OrderAmount.HasValue).Sum(s => s.OrderAmount ?? 0),
                TotalNotificationsSent = totalNotificationsSent,
                TotalNotificationsSuccessful = totalNotificationsSuccessful,
                NotificationsSuccessfulThisWeek = notificationsSuccessfulThisWeek,
                OrderAmountWithKDV = brutSepetTutari,
                TotalDiscountAmount = Math.Round(totalDiscountAmount, 2),
                TotalCost = Math.Round(totalCost, 2)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard KPIs for company {CompanyId}", companyId);
            return new GiftWheelDashboardKpiDto();
        }
    }

    public async Task<GiftWheelFunnelDto> GetFunnelStatsAsync(Guid companyId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var wheel = await GetCompanyWheelAsync(companyId);
            if (wheel == null)
            {
                return new GiftWheelFunnelDto();
            }

            var query = _context.GiftWheelSpins
                .Where(s => s.GiftWheelId == wheel.Id);

            if (startDate.HasValue)
                query = query.Where(s => s.SpinDate >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(s => s.SpinDate <= endDate.Value);

            var spins = await query.ToListAsync();

            var spinCount = spins.Count;
            var voucherCreatedCount = spins.Count(s => s.VoucherCreated);
            var voucherUsedCount = spins.Count(s => s.VoucherUsed);
            var usedSpins = spins.Where(s => s.VoucherUsed && s.OrderAmount.HasValue).ToList();
            var totalRevenue = usedSpins.Sum(s => s.OrderAmount ?? 0);

            return new GiftWheelFunnelDto
            {
                SpinCount = spinCount,
                VoucherCreatedCount = voucherCreatedCount,
                VoucherUsedCount = voucherUsedCount,
                TotalRevenue = totalRevenue,
                SpinToVoucherRate = spinCount > 0 ? Math.Round((decimal)voucherCreatedCount / spinCount * 100, 2) : 0,
                VoucherToUsageRate = voucherCreatedCount > 0 ? Math.Round((decimal)voucherUsedCount / voucherCreatedCount * 100, 2) : 0,
                UsageToRevenueRate = voucherUsedCount > 0 ? Math.Round(totalRevenue / voucherUsedCount, 2) : 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting funnel stats for company {CompanyId}", companyId);
            return new GiftWheelFunnelDto();
        }
    }

    public async Task<GiftWheelTrendDto> GetTrendStatsAsync(Guid companyId, int days = 30)
    {
        try
        {
            var wheel = await GetCompanyWheelAsync(companyId);
            if (wheel == null)
            {
                return new GiftWheelTrendDto();
            }

            var startDate = DateTime.Now.Date.AddDays(-days);
            var spins = await _context.GiftWheelSpins
                .Where(s => s.GiftWheelId == wheel.Id && s.SpinDate >= startDate)
                .ToListAsync();

            var result = new GiftWheelTrendDto();

            for (int i = 0; i <= days; i++)
            {
                var date = startDate.AddDays(i);
                var daySpins = spins.Where(s => s.SpinDate.Date == date).ToList();

                result.DailySpins.Add(new TrendDataPointDto
                {
                    Date = date,
                    Value = daySpins.Count,
                    Label = date.ToString("dd MMM")
                });

                result.DailyVouchers.Add(new TrendDataPointDto
                {
                    Date = date,
                    Value = daySpins.Count(s => s.VoucherCreated),
                    Label = date.ToString("dd MMM")
                });

                result.DailyUsage.Add(new TrendDataPointDto
                {
                    Date = date,
                    Value = daySpins.Count(s => s.VoucherUsed),
                    Label = date.ToString("dd MMM")
                });

                result.DailyRevenue.Add(new TrendDataPointDto
                {
                    Date = date,
                    Value = daySpins.Where(s => s.OrderAmount.HasValue).Sum(s => s.OrderAmount ?? 0),
                    Label = date.ToString("dd MMM")
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trend stats for company {CompanyId}", companyId);
            return new GiftWheelTrendDto();
        }
    }

    public async Task<PaginatedSpinLogResultDto> GetDetailedLogsAsync(Guid companyId, SpinLogFilterDto filter)
    {
        try
        {
            var wheel = await GetCompanyWheelAsync(companyId);
            if (wheel == null)
            {
                return new PaginatedSpinLogResultDto();
            }

            var query = _context.GiftWheelSpins
                .Where(s => s.GiftWheelId == wheel.Id)
                .Include(s => s.Prize)
                .AsQueryable();

            if (filter.StartDate.HasValue)
                query = query.Where(s => s.SpinDate >= filter.StartDate.Value);
            if (filter.EndDate.HasValue)
                query = query.Where(s => s.SpinDate <= filter.EndDate.Value);
            if (filter.VoucherUsed.HasValue)
                query = query.Where(s => s.VoucherUsed == filter.VoucherUsed.Value);
            if (filter.VoucherCreated.HasValue)
                query = query.Where(s => s.VoucherCreated == filter.VoucherCreated.Value);
            if (filter.MinAmount.HasValue)
                query = query.Where(s => s.OrderAmount >= filter.MinAmount.Value);
            if (filter.MaxAmount.HasValue)
                query = query.Where(s => s.OrderAmount <= filter.MaxAmount.Value);
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(s =>
                    s.CustomerName.ToLower().Contains(searchTerm) ||
                    s.CustomerPhone.Contains(searchTerm) ||
                    (s.CustomerEmail != null && s.CustomerEmail.ToLower().Contains(searchTerm)) ||
                    (s.VoucherCode != null && s.VoucherCode.ToLower().Contains(searchTerm)));
            }

            var totalCount = await query.CountAsync();

            query = filter.SortDescending
                ? query.OrderByDescending(s => s.SpinDate)
                : query.OrderBy(s => s.SpinDate);

            var items = await query
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(s => new DetailedSpinLogDto
                {
                    Id = s.Id,
                    CustomerName = s.CustomerName,
                    CustomerPhone = s.CustomerPhone,
                    CustomerEmail = s.CustomerEmail,
                    PrizeName = s.Prize.Name,
                    PrizeType = s.Prize.PrizeType,
                    DiscountAmount = s.Prize.DiscountAmount,
                    DiscountType = s.Prize.DiscountType,
                    VoucherCode = s.VoucherCode,
                    VoucherCreated = s.VoucherCreated,
                    VoucherUsed = s.VoucherUsed,
                    VoucherUsedAt = s.VoucherUsedAt,
                    OrderId = s.OrderId,
                    OrderAmount = s.OrderAmount,
                    OrderDate = s.OrderDate,
                    NotificationSent = s.NotificationSent,
                    IsNotificationSuccessful = s.IsNotificationSuccessful,
                    CommunicationConsent = s.SmsPermissionGranted,
                    SpinDate = s.SpinDate,
                    ErrorMessage = s.ErrorMessage,
                    IsSuccessful = s.IsSuccessful,
                    ValidityDays = s.Prize.ValidityDays,
                    VoucherStartDate = s.SpinDate,
                    VoucherEndDate = s.Prize.ValidityDays.HasValue ? s.SpinDate.AddDays(s.Prize.ValidityDays.Value) : null
                })
                .ToListAsync();

            var totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);

            return new PaginatedSpinLogResultDto
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize,
                TotalPages = totalPages,
                HasNextPage = filter.Page < totalPages,
                HasPreviousPage = filter.Page > 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting detailed logs for company {CompanyId}", companyId);
            return new PaginatedSpinLogResultDto();
        }
    }

    public async Task<(bool Success, string Message)> ProcessVoucherUsedWebhookAsync(Guid companyId, TicimaxVoucherWebhookDto webhookData)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(webhookData.VoucherCode))
            {
                return (false, "Kupon kodu boş olamaz.");
            }

            var wheel = await GetCompanyWheelAsync(companyId);
            if (wheel == null)
            {
                _logger.LogWarning("Gift wheel not found for company {CompanyId}", companyId);
                return (false, "Hediye çarkı bulunamadı.");
            }

            var spin = await _context.GiftWheelSpins
                .FirstOrDefaultAsync(s => s.GiftWheelId == wheel.Id &&
                                          s.VoucherCode != null &&
                                          s.VoucherCode.ToLower() == webhookData.VoucherCode.ToLower() &&
                                          s.VoucherCreated);

            if (spin == null)
            {
                _logger.LogWarning("Voucher not found for webhook. VoucherCode={VoucherCode}", webhookData.VoucherCode);
                return (false, "Kupon bulunamadı.");
            }

            if (spin.VoucherUsed)
            {
                _logger.LogInformation("Voucher already marked as used. VoucherCode={VoucherCode}", webhookData.VoucherCode);
                return (true, "Kupon zaten kullanılmış olarak işaretli.");
            }

            spin.VoucherUsed = true;
            spin.VoucherUsedAt = webhookData.OrderDate;
            spin.OrderId = webhookData.OrderId;
            spin.OrderAmount = webhookData.OrderAmount;
            spin.OrderDate = webhookData.OrderDate;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Voucher marked as used via webhook. VoucherCode={VoucherCode}, OrderId={OrderId}, OrderAmount={OrderAmount}",
                webhookData.VoucherCode, webhookData.OrderId, webhookData.OrderAmount);

            return (true, "Kupon kullanıldı olarak işaretlendi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing voucher used webhook. VoucherCode={VoucherCode}", webhookData.VoucherCode);
            return (false, "Webhook işlenirken hata oluştu: " + ex.Message);
        }
    }

    public async Task<(bool Success, string Message, int UpdatedCount)> SyncOrdersFromTicimaxAsync(Guid companyId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var wheel = await GetCompanyWheelAsync(companyId);
            if (wheel == null)
            {
                return (false, "Hediye çarkı bulunamadı.", 0);
            }

            var ecommerceService = await _ecommerceServiceFactory.GetEcommerceServiceAsync(companyId);
            if (ecommerceService == null)
            {
                return (false, "E-ticaret entegrasyonu bulunamadı.", 0);
            }
            
            startDate ??= DateTime.Now.AddDays(-30);
            endDate ??= DateTime.Now.AddDays(1);

            var filter = new EcommerceOrderFilter
            {
                CreatedAfter = startDate,
                CreatedBefore = endDate,
                PaymentCompleted = true
            };

            var pagination = new EcommercePagination
            {
                PageNumber = 1,
                PageSize = 1000,
                SortField = "OrderDate",
                SortDirection = "DESC"
            };

            var orders = await ecommerceService.GetOrdersAsync(companyId, filter, pagination);
            if (orders == null || orders.Length == 0)
            {
                return (true, "Sipariş bulunamadı.", 0);
            }

            var updatedCount = 0;
            var allSpins = await _context.GiftWheelSpins
                .Where(s => s.GiftWheelId == wheel.Id && s.VoucherCreated && !string.IsNullOrEmpty(s.VoucherCode))
                .ToListAsync();

            _logger.LogInformation("SyncOrdersFromTicimaxAsync: Found {SpinCount} spins with vouchers, {OrderCount} orders to check",
                allSpins.Count, orders.Length);

            foreach (var order in orders)
            {
                if (order.CustomFields == null || !order.CustomFields.ContainsKey("voucherCode"))
                    continue;

                var voucherCode = order.CustomFields["voucherCode"]?.ToString();
                if (string.IsNullOrWhiteSpace(voucherCode))
                    continue;

                voucherCode = voucherCode.Trim();

                var spin = allSpins.FirstOrDefault(s =>
                {
                    if (string.IsNullOrEmpty(s.VoucherCode))
                        return false;

                    var spinCode = s.VoucherCode.Trim();
                    return spinCode.Equals(voucherCode, StringComparison.OrdinalIgnoreCase);
                });

                if (spin == null)
                {
                    _logger.LogDebug("SyncOrdersFromTicimaxAsync: No matching spin found for voucher code '{VoucherCode}' in order {OrderId}",
                        voucherCode, order.ExternalId);
                    continue;
                }

                var discountAmount = order.CustomFields.ContainsKey("discountAmount") &&
                                     order.CustomFields["discountAmount"] != null
                    ? Convert.ToDecimal(order.CustomFields["discountAmount"])
                    : 0m;

                spin.VoucherUsed = true;
                spin.VoucherUsedAt = order.OrderDate ?? order.CreatedAt;
                spin.OrderId = order.ExternalId;
                spin.OrderAmount = order.TotalAmount;
                spin.KDVAmount= order.KDVAmount;
                spin.OrderDiscountAmount = discountAmount;
                spin.OrderDate = order.OrderDate ?? order.CreatedAt;

                _logger.LogInformation("SyncOrdersFromTicimaxAsync: Updated spin {SpinId} with voucher '{VoucherCode}' for order {OrderId}, amount: {Amount}",
                    spin.Id, voucherCode, order.ExternalId, order.TotalAmount);

                updatedCount++;
            }

            if (updatedCount > 0)
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Synced {Count} gift wheel voucher usages from Ticimax for company {CompanyId}", updatedCount, companyId);
            }

            return (true, $"{updatedCount} kupon kullanımı güncellendi.", updatedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing orders from Ticimax for company {CompanyId}", companyId);
            return (false, "Siparişler senkronize edilirken hata oluştu: " + ex.Message, 0);
        }
    }

    public async Task<(bool Success, string Message)> ProcessOrderForVoucherUsageAsync(Guid companyId, string orderId)
    {
        try
        {
            var wheel = await GetCompanyWheelAsync(companyId);
            if (wheel == null)
            {
                return (false, "Hediye çarkı bulunamadı.");
            }

            var ecommerceService = await _ecommerceServiceFactory.GetEcommerceServiceAsync(companyId);
            if (ecommerceService == null)
            {
                return (false, "E-ticaret entegrasyonu bulunamadı.");
            }

            var order = await ecommerceService.GetOrderAsync(companyId, orderId);
            if (order == null)
            {
                _logger.LogWarning("ProcessOrderForVoucherUsageAsync: Order {OrderId} not found for company {CompanyId}",
                    orderId, companyId);
                return (false, "Sipariş bulunamadı.");
            }

            if (order.CustomFields == null || !order.CustomFields.ContainsKey("voucherCode"))
            {
                _logger.LogDebug("ProcessOrderForVoucherUsageAsync: Order {OrderId} has no voucherCode, skipping",
                    orderId);
                return (true, "Siparişte kupon kodu bulunamadı.");
            }

            var voucherCode = order.CustomFields["voucherCode"]?.ToString();
            if (string.IsNullOrWhiteSpace(voucherCode))
            {
                return (true, "Siparişte kupon kodu boş.");
            }

            voucherCode = voucherCode.Trim();

            var spin = await _context.GiftWheelSpins
                .FirstOrDefaultAsync(s => s.GiftWheelId == wheel.Id &&
                                          s.VoucherCreated &&
                                          !string.IsNullOrEmpty(s.VoucherCode) &&
                                          s.VoucherCode.ToLower() == voucherCode.ToLower());

            if (spin == null)
            {
                _logger.LogDebug("ProcessOrderForVoucherUsageAsync: No matching spin found for voucher '{VoucherCode}' in order {OrderId}",
                    voucherCode, orderId);
                return (true, "Eşleşen kupon bulunamadı.");
            }

            if (spin.VoucherUsed)
            {
                _logger.LogInformation("ProcessOrderForVoucherUsageAsync: Voucher '{VoucherCode}' already used for spin {SpinId}",
                    voucherCode, spin.Id);
                return (true, "Kupon zaten kullanılmış.");
            }

            var discountAmount = order.CustomFields.ContainsKey("discountAmount") &&
                                 order.CustomFields["discountAmount"] != null
                ? Convert.ToDecimal(order.CustomFields["discountAmount"])
                : 0m;

            spin.VoucherUsed = true;
            spin.VoucherUsedAt = order.OrderDate ?? order.CreatedAt;
            spin.OrderId = order.ExternalId;
            spin.OrderAmount = order.TotalAmount;
            spin.KDVAmount = order.KDVAmount;
            spin.OrderDiscountAmount = discountAmount;
            spin.OrderDate = order.OrderDate ?? order.CreatedAt;

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "ProcessOrderForVoucherUsageAsync: Updated spin {SpinId} with voucher '{VoucherCode}' for order {OrderId}, amount: {Amount}",
                spin.Id, voucherCode, order.ExternalId, order.TotalAmount);

            return (true, "Kupon kullanımı güncellendi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order {OrderId} for voucher usage in company {CompanyId}",
                orderId, companyId);
            return (false, "Sipariş işlenirken hata oluştu: " + ex.Message);
        }
    }

    #endregion

    #region Script Generation

    public async Task<string> GenerateEmbedScriptAsync(Guid companyId)
    {
        try
        {
            var config = await GetWheelConfigAsync(companyId);
            if (config == null)
            {
                return "// Hediye çarkı yapılandırması bulunamadı";
            }

            var scriptTemplate = GetEmbedScriptTemplate();

            var configJson = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var script = scriptTemplate
                .Replace("{config}", configJson)
                .Replace("{apiBaseUrl}", "https://admin.pushonica.com") // This should come from configuration
                .Replace("{companyId}", companyId.ToString());

            return script;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embed script for company {CompanyId}", companyId);
            return "// Script oluşturulurken hata oluştu";
        }
    }

    private string GetEmbedScriptTemplate()
    {
        return @"
(function() {
    'use strict';

    // Gift Wheel Configuration
    const WHEEL_CONFIG = {config};
    const API_BASE_URL = '{apiBaseUrl}';
    const COMPANY_ID = '{companyId}';

    // Gift Wheel Implementation
    class GiftWheel {
        constructor(containerId) {
            this.container = document.getElementById(containerId);
            if (!this.container) {
                console.error('Gift Wheel: Container element not found:', containerId);
                return;
            }

            this.canvas = null;
            this.ctx = null;
            this.isSpinning = false;
            this.currentRotation = 0;
            this.prizes = WHEEL_CONFIG.prizes || [];
            this.activePrizes = this.prizes.filter(p => p.isActive);

            this.init();
        }

        init() {
            this.createWheelHTML();
            this.setupCanvas();
            this.attachEventListeners();
            this.drawWheel();
            this.checkEligibility();
        }

        createWheelHTML() {
            const emailField = WHEEL_CONFIG.requireEmail ?
                `<input type='email' id='gw-customerEmail' placeholder='E-posta Adresiniz' class='gw-input' required>` : '';

            this.container.innerHTML = `
                <div class='gw-container'>
                    <div class='gw-header'>
                        <h2 class='gw-title'>${WHEEL_CONFIG.title}</h2>
                        <p class='gw-subtitle'>${WHEEL_CONFIG.subtitle}</p>
                    </div>

                    <div class='gw-wheel-section'>
                        <div class='gw-wheel-wrapper'>
                            <canvas id='gw-wheelCanvas' class='gw-canvas'></canvas>
                            <div class='gw-wheel-pointer'></div>
                            <div class='gw-wheel-center'></div>
                        </div>
                    </div>

                    <div class='gw-form-section'>
                        <div id='gw-customerForm' class='gw-form'>
                            <input type='text' id='gw-customerName' placeholder='Adınız' class='gw-input' required>
                            <input type='tel' id='gw-customerPhone' placeholder='Telefon Numaranız' class='gw-input' required>
                            ${emailField}
                        </div>

                        <button id='gw-spinButton' class='gw-spin-button' disabled>
                            <span class='gw-button-text'>${WHEEL_CONFIG.buttonText}</span>
                            <div class='gw-spinner hidden'>
                                <div class='gw-spinner-icon'></div>
                            </div>
                        </button>
                    </div>

                    <div id='gw-result' class='gw-result hidden'></div>
                    <div id='gw-error' class='gw-error hidden'></div>
                </div>
            `;

            this.injectStyles();
        }

        injectStyles() {
            if (document.getElementById('gw-styles')) return;

            const styles = document.createElement('style');
            styles.id = 'gw-styles';
            styles.textContent = `
                .gw-container {
                    max-width: 400px;
                    margin: 0 auto;
                    padding: 20px;
                    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
                    text-align: center;
                    background: #ffffff;
                    border-radius: 16px;
                    box-shadow: 0 4px 20px rgba(0,0,0,0.1);
                }

                .gw-header {
                    margin-bottom: 30px;
                }

                .gw-title {
                    font-size: 24px;
                    font-weight: bold;
                    color: #1f2937;
                    margin: 0 0 8px 0;
                }

                .gw-subtitle {
                    font-size: 16px;
                    color: #6b7280;
                    margin: 0;
                }

                .gw-wheel-section {
                    margin-bottom: 30px;
                }

                .gw-wheel-wrapper {
                    position: relative;
                    display: inline-block;
                }

                .gw-canvas {
                    border-radius: 50%;
                    box-shadow: 0 8px 32px rgba(0,0,0,0.2);
                    transition: transform 0.1s ease;
                }

                .gw-wheel-pointer {
                    position: absolute;
                    top: -8px;
                    left: 50%;
                    transform: translateX(-50%);
                    width: 0;
                    height: 0;
                    border-left: 12px solid transparent;
                    border-right: 12px solid transparent;
                    border-bottom: 20px solid #1f2937;
                    z-index: 10;
                }

                .gw-wheel-center {
                    position: absolute;
                    top: 50%;
                    left: 50%;
                    transform: translate(-50%, -50%);
                    width: 40px;
                    height: 40px;
                    background: #ffffff;
                    border: 3px solid #1f2937;
                    border-radius: 50%;
                    z-index: 10;
                    box-shadow: 0 2px 8px rgba(0,0,0,0.2);
                }

                .gw-form-section {
                    margin-bottom: 20px;
                }

                .gw-form {
                    display: flex;
                    flex-direction: column;
                    gap: 12px;
                    margin-bottom: 20px;
                }

                .gw-input {
                    padding: 12px 16px;
                    border: 2px solid #e5e7eb;
                    border-radius: 8px;
                    font-size: 16px;
                    transition: border-color 0.2s ease;
                    outline: none;
                }

                .gw-input:focus {
                    border-color: ${WHEEL_CONFIG.primaryColor};
                    box-shadow: 0 0 0 3px ${WHEEL_CONFIG.primaryColor}20;
                }

                .gw-input.error {
                    border-color: #ef4444;
                    box-shadow: 0 0 0 3px #ef444420;
                }

                .gw-spin-button {
                    background: linear-gradient(135deg, ${WHEEL_CONFIG.primaryColor}, ${WHEEL_CONFIG.primaryColor}dd);
                    color: white;
                    border: none;
                    padding: 16px 32px;
                    font-size: 18px;
                    font-weight: bold;
                    border-radius: 50px;
                    cursor: pointer;
                    transition: all 0.3s ease;
                    position: relative;
                    overflow: hidden;
                    min-width: 200px;
                    min-height: 56px;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                }

                .gw-spin-button:hover:not(:disabled) {
                    transform: translateY(-2px);
                    box-shadow: 0 8px 25px rgba(0,0,0,0.2);
                }

                .gw-spin-button:disabled {
                    opacity: 0.6;
                    cursor: not-allowed;
                    transform: none;
                }

                .gw-spinner {
                    display: flex;
                    align-items: center;
                    justify-content: center;
                }

                .gw-spinner-icon {
                    width: 20px;
                    height: 20px;
                    border: 2px solid #ffffff40;
                    border-top: 2px solid #ffffff;
                    border-radius: 50%;
                    animation: gw-spin 1s linear infinite;
                }

                @keyframes gw-spin {
                    0% { transform: rotate(0deg); }
                    100% { transform: rotate(360deg); }
                }

                .gw-result, .gw-error {
                    padding: 16px;
                    border-radius: 8px;
                    margin-top: 20px;
                    font-weight: 500;
                }

                .gw-result {
                    background: #dcfce7;
                    color: #166534;
                    border: 1px solid #bbf7d0;
                }

                .gw-error {
                    background: #fef2f2;
                    color: #dc2626;
                    border: 1px solid #fecaca;
                }

                .hidden {
                    display: none !important;
                }

                .gw-confetti {
                    position: fixed;
                    width: 10px;
                    height: 10px;
                    background: #ff6b6b;
                    z-index: 1000;
                    pointer-events: none;
                }

                @media (max-width: 480px) {
                    .gw-container {
                        padding: 16px;
                        margin: 10px;
                    }

                    .gw-title {
                        font-size: 20px;
                    }

                    .gw-subtitle {
                        font-size: 14px;
                    }

                    .gw-canvas {
                        width: 250px !important;
                        height: 250px !important;
                    }

                    .gw-spin-button {
                        font-size: 16px;
                        padding: 14px 28px;
                        min-width: 180px;
                    }
                }
            `;

            document.head.appendChild(styles);
        }

        setupCanvas() {
            this.canvas = document.getElementById('gw-wheelCanvas');
            if (!this.canvas) return;

            this.ctx = this.canvas.getContext('2d');

            // Set canvas size
            const size = Math.min(WHEEL_CONFIG.wheelSize || 300, 300);
            this.canvas.width = size;
            this.canvas.height = size;
            this.canvas.style.width = size + 'px';
            this.canvas.style.height = size + 'px';
        }

        drawWheel() {
            if (!this.ctx || this.activePrizes.length === 0) return;

            const centerX = this.canvas.width / 2;
            const centerY = this.canvas.height / 2;
            const radius = Math.min(centerX, centerY) - 5;

            // Clear canvas
            this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);

            // Calculate total probability
            const totalProbability = this.activePrizes.reduce((sum, prize) => sum + prize.probability, 0);
            if (totalProbability === 0) return;

            let currentAngle = this.currentRotation;

            // Draw prize segments
            this.activePrizes.forEach((prize, index) => {
                const segmentAngle = (prize.probability / totalProbability) * 2 * Math.PI;

                // Draw segment
                this.ctx.beginPath();
                this.ctx.moveTo(centerX, centerY);
                this.ctx.arc(centerX, centerY, radius, currentAngle, currentAngle + segmentAngle);
                this.ctx.closePath();

                // Fill with prize color
                this.ctx.fillStyle = prize.color || '#3B82F6';
                this.ctx.fill();

                // Add border
                this.ctx.strokeStyle = '#ffffff';
                this.ctx.lineWidth = 2;
                this.ctx.stroke();

                // Draw text
                this.drawPrizeText(centerX, centerY, radius, currentAngle, segmentAngle, prize.name);

                currentAngle += segmentAngle;
            });
        }

        drawPrizeText(centerX, centerY, radius, startAngle, segmentAngle, text) {
            const textAngle = startAngle + segmentAngle / 2;
            const textRadius = radius * 0.7;
            const textX = centerX + Math.cos(textAngle) * textRadius;
            const textY = centerY + Math.sin(textAngle) * textRadius;

            this.ctx.save();
            this.ctx.translate(textX, textY);
            this.ctx.rotate(textAngle + Math.PI / 2);

            // Text styling
            this.ctx.fillStyle = '#ffffff';
            this.ctx.font = 'bold 12px Arial';
            this.ctx.textAlign = 'center';
            this.ctx.textBaseline = 'middle';

            // Add text shadow for better readability
            this.ctx.shadowColor = 'rgba(0,0,0,0.5)';
            this.ctx.shadowBlur = 2;
            this.ctx.shadowOffsetX = 1;
            this.ctx.shadowOffsetY = 1;

            // Wrap text if too long
            const maxWidth = radius * 0.4;
            this.wrapText(text, 0, 0, maxWidth, 14);

            this.ctx.restore();
        }

        wrapText(text, x, y, maxWidth, lineHeight) {
            const words = text.split(' ');
            let line = '';
            let lines = [];

            for (let n = 0; n < words.length; n++) {
                const testLine = line + words[n] + ' ';
                const metrics = this.ctx.measureText(testLine);
                const testWidth = metrics.width;

                if (testWidth > maxWidth && n > 0) {
                    lines.push(line);
                    line = words[n] + ' ';
                } else {
                    line = testLine;
                }
            }
            lines.push(line);

            // Draw lines centered
            const startY = y - (lines.length - 1) * lineHeight / 2;
            lines.forEach((line, index) => {
                this.ctx.fillText(line, x, startY + index * lineHeight);
            });
        }

        attachEventListeners() {
            const spinButton = document.getElementById('gw-spinButton');
            const nameInput = document.getElementById('gw-customerName');
            const phoneInput = document.getElementById('gw-customerPhone');
            const emailInput = document.getElementById('gw-customerEmail');

            // Form validation
            const validateForm = () => {
                const name = nameInput.value.trim();
                const phone = phoneInput.value.trim();
                const email = emailInput ? emailInput.value.trim() : '';

                let isValid = true;

                // Clear previous errors
                [nameInput, phoneInput, emailInput].forEach(input => {
                    if (input) input.classList.remove('error');
                });

                if (!name) {
                    nameInput.classList.add('error');
                    isValid = false;
                }

                if (!phone) {
                    phoneInput.classList.add('error');
                    isValid = false;
                }

                if (emailInput && WHEEL_CONFIG.requireEmail && !email) {
                    emailInput.classList.add('error');
                    isValid = false;
                }

                spinButton.disabled = !isValid || this.isSpinning;
                return isValid;
            };

            // Add input listeners
            [nameInput, phoneInput, emailInput].forEach(input => {
                if (input) {
                    input.addEventListener('input', validateForm);
                    input.addEventListener('blur', validateForm);
                }
            });

            // Spin button click
            spinButton.addEventListener('click', () => this.handleSpin());

            // Initial validation
            validateForm();
        }

        async checkEligibility() {
            // Check if user can spin (optional pre-validation)
            const phoneInput = document.getElementById('gw-customerPhone');
            if (phoneInput && phoneInput.value.trim()) {
                try {
                    const response = await fetch(`${API_BASE_URL}/api/gift-wheel/${COMPANY_ID}/validate-spin`, {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ customerPhone: phoneInput.value.trim() })
                    });

                    const result = await response.json();
                    if (!result.canSpin) {
                        this.showError(result.message);
                        document.getElementById('gw-spinButton').disabled = true;
                    }
                } catch (error) {
                    // Ignore validation errors, allow spin attempt
                }
            }
        }

        async handleSpin() {
            if (this.isSpinning) return;

            const customerData = this.getCustomerData();
            if (!this.validateCustomerData(customerData)) return;

            this.setSpinningState(true);
            this.hideMessages();

            try {
                // Start wheel animation
                this.animateWheelSpin();

                // Make API call
                const response = await fetch(`${API_BASE_URL}/api/gift-wheel/${COMPANY_ID}/spin`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(customerData)
                });

                const result = await response.json();

                // Wait for animation to complete
                await this.waitForSpinAnimation();

                if (result.success) {
                    this.showWinResult(result);
                } else {
                    this.showError(result.message);
                }

            } catch (error) {
                console.error('Spin error:', error);
                this.showError('Bağlantı hatası. Lütfen tekrar deneyin.');
            } finally {
                this.setSpinningState(false);
            }
        }

        getCustomerData() {
            return {
                customerName: document.getElementById('gw-customerName').value.trim(),
                customerPhone: document.getElementById('gw-customerPhone').value.trim(),
                customerEmail: document.getElementById('gw-customerEmail')?.value.trim() || null
            };
        }

        validateCustomerData(data) {
            if (!data.customerName) {
                this.showError('Lütfen adınızı giriniz.');
                return false;
            }

            if (!data.customerPhone) {
                this.showError('Lütfen telefon numaranızı giriniz.');
                return false;
            }

            if (WHEEL_CONFIG.requireEmail && !data.customerEmail) {
                this.showError('Lütfen e-posta adresinizi giriniz.');
                return false;
            }

            return true;
        }

        setSpinningState(spinning) {
            this.isSpinning = spinning;
            const button = document.getElementById('gw-spinButton');
            const buttonText = button.querySelector('.gw-button-text');
            const spinner = button.querySelector('.gw-spinner');

            button.disabled = spinning;

            if (spinning) {
                buttonText.classList.add('hidden');
                spinner.classList.remove('hidden');
            } else {
                buttonText.classList.remove('hidden');
                spinner.classList.add('hidden');
            }
        }

        animateWheelSpin() {
            return new Promise((resolve) => {
                const duration = 3000; // 3 seconds
                const rotations = 5 + Math.random() * 3; // 5-8 full rotations
                const finalRotation = rotations * 2 * Math.PI;

                const startTime = Date.now();
                const startRotation = this.currentRotation;

                const animate = () => {
                    const elapsed = Date.now() - startTime;
                    const progress = Math.min(elapsed / duration, 1);

                    // Easing function (ease-out cubic)
                    const easeOut = 1 - Math.pow(1 - progress, 3);

                    this.currentRotation = startRotation + (finalRotation * easeOut);
                    this.drawWheel();

                    if (progress < 1) {
                        requestAnimationFrame(animate);
                    } else {
                        this.currentRotation = this.currentRotation % (2 * Math.PI);
                        resolve();
                    }
                };

                animate();
            });
        }

        waitForSpinAnimation() {
            return new Promise(resolve => setTimeout(resolve, 500)); // Small delay for effect
        }

        showWinResult(result) {
            const resultDiv = document.getElementById('gw-result');
            const prize = result.prize;

            let message = WHEEL_CONFIG.winMessage.replace('{prize}', prize.name);

            if (result.voucherCode) {
                message += `<br><strong>Hediye Çeki Kodu:</strong> ${result.voucherCode}`;
            }

            if (result.notificationSent) {
                message += '<br><small>WhatsApp bildirimi gönderildi.</small>';
            }

            resultDiv.innerHTML = `
                <div class='gw-result-icon'>🎉</div>
                <div class='gw-result-message'>${message}</div>
            `;

            resultDiv.classList.remove('hidden');

            // Show confetti if enabled and it's a voucher
            if (WHEEL_CONFIG.showConfetti && prize.prizeType === 'voucher') {
                this.showConfetti();
            }

            // Scroll to result
            resultDiv.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        }

        showError(message) {
            const errorDiv = document.getElementById('gw-error');
            errorDiv.innerHTML = `
                <div class='gw-error-icon'>⚠️</div>
                <div class='gw-error-message'>${message}</div>
            `;
            errorDiv.classList.remove('hidden');

            // Scroll to error
            errorDiv.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
        }

        hideMessages() {
            document.getElementById('gw-result').classList.add('hidden');
            document.getElementById('gw-error').classList.add('hidden');
        }

        showConfetti() {
            const colors = ['#ff6b6b', '#4ecdc4', '#45b7d1', '#96ceb4', '#feca57', '#ff9ff3', '#a8e6cf', '#ffd93d'];
            const confettiCount = 50;
            const container = this.container;

            for (let i = 0; i < confettiCount; i++) {
                setTimeout(() => {
                    this.createConfettiPiece(colors[Math.floor(Math.random() * colors.length)], container);
                }, i * 50); // Stagger the confetti
            }
        }

        createConfettiPiece(color, container) {
            const confetti = document.createElement('div');
            confetti.className = 'gw-confetti';
            confetti.style.backgroundColor = color;
            confetti.style.left = Math.random() * 100 + '%';
            confetti.style.top = '-10px';
            confetti.style.borderRadius = Math.random() > 0.5 ? '50%' : '0';

            // Random size
            const size = Math.random() * 8 + 4;
            confetti.style.width = size + 'px';
            confetti.style.height = size + 'px';

            container.appendChild(confetti);

            // Animate falling
            const containerRect = container.getBoundingClientRect();
            const fallDistance = containerRect.height + 100;
            const fallDuration = Math.random() * 2000 + 2000; // 2-4 seconds
            const horizontalDrift = (Math.random() - 0.5) * 100; // -50 to 50px drift

            const animation = confetti.animate([
                {
                    transform: 'translateY(0px) translateX(0px) rotate(0deg)',
                    opacity: 1
                },
                {
                    transform: `translateY(${fallDistance}px) translateX(${horizontalDrift}px) rotate(${Math.random() * 360}deg)`,
                    opacity: 0
                }
            ], {
                duration: fallDuration,
                easing: 'cubic-bezier(0.25, 0.46, 0.45, 0.94)'
            });

            animation.onfinish = () => {
                if (confetti.parentNode) {
                    confetti.parentNode.removeChild(confetti);
                }
            };
        }

        // Utility method to handle responsive canvas
        handleResize() {
            if (window.innerWidth <= 480) {
                const size = 250;
                this.canvas.width = size;
                this.canvas.height = size;
                this.canvas.style.width = size + 'px';
                this.canvas.style.height = size + 'px';
            } else {
                const size = Math.min(WHEEL_CONFIG.wheelSize || 300, 300);
                this.canvas.width = size;
                this.canvas.height = size;
                this.canvas.style.width = size + 'px';
                this.canvas.style.height = size + 'px';
            }
            this.drawWheel();
        }
    }

    // Auto-initialize when DOM is ready
    function initGiftWheel() {
        if (document.getElementById('gift-wheel-container')) {
            const wheel = new GiftWheel('gift-wheel-container');

            // Handle window resize
            let resizeTimeout;
            window.addEventListener('resize', () => {
                clearTimeout(resizeTimeout);
                resizeTimeout = setTimeout(() => {
                    if (wheel.handleResize) {
                        wheel.handleResize();
                    }
                }, 250);
            });

            return wheel;
        } else {
            console.warn('Gift Wheel: Container element #gift-wheel-container not found');
        }
    }

    // Initialize
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initGiftWheel);
    } else {
        initGiftWheel();
    }

    // Expose for debugging (optional)
    window.GiftWheel = GiftWheel;

})();";
    }

    public async Task<GiftWheelConfigDto?> GetWheelConfigAsync(Guid companyId)
    {
        try
        {
            var wheel = await GetCompanyWheelAsync(companyId);
            if (wheel?.Settings == null)
            {
                return null;
            }

            var prizes = await GetWheelPrizesAsync(wheel.Id);

            // Get legal URLs from Integration Settings
            var (privacyPolicyUrl, termsUrl) = await GetLegalLinksFromIntegrationAsync(companyId);

            // Fallback to database values if integration doesn't have links
            privacyPolicyUrl ??= wheel.Settings.PrivacyPolicyUrl;
            termsUrl ??= wheel.Settings.TermsUrl;

            return new GiftWheelConfigDto
            {
                WheelId = wheel.Id,
                Title = wheel.Settings.WheelTitle,
                Subtitle = wheel.Settings.WheelSubtitle,
                ButtonText = wheel.Settings.ButtonText,
                WinMessage = wheel.Settings.WinMessage,
                LoseMessage = wheel.Settings.LoseMessage,
                RequirePhone = wheel.Settings.RequirePhone,
                RequireEmail = wheel.Settings.RequireEmail,
                PrimaryColor = wheel.Settings.PrimaryColor,
                SecondaryColor = wheel.Settings.SecondaryColor,
                WheelSize = wheel.Settings.WheelSize,
                Layout = wheel.Settings.Layout,
                LogoUrl = wheel.Settings.LogoUrl,
                LogoAnimation = wheel.Settings.LogoAnimation,
                LogoSizePercentage = wheel.Settings.LogoSizePercentage > 0 ? wheel.Settings.LogoSizePercentage : 15,
                BackgroundImageUrl = wheel.Settings.BackgroundImageUrl,
                ModalTextColor = string.IsNullOrEmpty(wheel.Settings.ModalTextColor) ? "#1f2937" : wheel.Settings.ModalTextColor,
                PrivacyPolicyUrl = privacyPolicyUrl,
                TermsUrl = termsUrl,
                ShowConfetti = wheel.Settings.ShowConfetti,
                ButtonPosition = string.IsNullOrEmpty(wheel.Settings.ButtonPosition) ? "right" : wheel.Settings.ButtonPosition,
                ButtonBottomPx = wheel.Settings.ButtonBottomPx,
                ButtonMobileBottomPx = wheel.Settings.ButtonMobileBottomPx,
                Prizes = prizes
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting wheel config for company {CompanyId}", companyId);
            return null;
        }
    }

    #endregion

    #region R2 Export

    public async Task<string> ExportGiftWheelToR2Async(Guid companyId)
    {
        try
        {
            _logger.LogInformation("Starting R2 export for gift wheel. CompanyId: {CompanyId}", companyId);

            // Get wheel configuration
            var wheelConfig = await GetWheelConfigAsync(companyId);
            if (wheelConfig == null)
            {
                _logger.LogWarning("No wheel configuration found for company {CompanyId}", companyId);
                return "No wheel configuration found";
            }

            // Serialize configuration to JSON
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var jsonContent = JsonSerializer.Serialize(wheelConfig, jsonOptions);

            // Upload JSON configuration
            var jsonFileName = $"{companyId}/giftWheel.json";
            var jsonUrl = await _r2StorageService.UploadJsonAsync(jsonFileName, jsonContent);

            // Upload standalone script
            var jsFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "js", "modules", "gift-wheel-module.js");
            if (File.Exists(jsFilePath))
            {
                var jsScript = await File.ReadAllTextAsync(jsFilePath);
                var jsKey = $"{companyId}/giftWheel.js";
                var jsUrl = await _r2StorageService.UploadJavaScriptAsync(jsKey, jsScript);

                _logger.LogInformation("Successfully exported gift wheel to R2. CompanyId: {CompanyId}. JSON: {JsonUrl}, JS: {JsUrl}", companyId, jsonUrl, jsUrl);
            }
            else
            {
                _logger.LogWarning("Gift wheel module JS file not found at {Path}", jsFilePath);
            }

            return "Gift wheel exported successfully to R2";

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting gift wheel to R2. CompanyId: {CompanyId}", companyId);
            return $"Export failed: {ex.Message}";
        }
    }

    #endregion
}
