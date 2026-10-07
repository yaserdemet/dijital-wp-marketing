using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Helpers;
using PushDashboard.Models;
using PushDashboard.Services;
using PushDashboard.ViewModels;
using PushDashboard.Services.Integrations;
using PushDashboard.Filters;
using PushDashboard.DTOs.Store;

namespace PushDashboard.Controllers;

[Authorize]
public class StoreController : BaseController
{
    private readonly IStoreService _storeService;
    private readonly IModuleUsageService _moduleUsageService;
    private readonly IOrderStatusMappingService _orderStatusMappingService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StoreController> _logger;
    private readonly EcommerceGiftVoucherFactory _giftVoucherFactory;
    private readonly IActivityLogService _activityLogService;

    public StoreController(
        IStoreService storeService,
        IModuleUsageService moduleUsageService,
        IOrderStatusMappingService orderStatusMappingService,
        ApplicationDbContext context,
        ILogger<StoreController> logger,
        EcommerceGiftVoucherFactory giftVoucherFactory,
        IUserContextService userContextService,
        IActivityLogService activityLogService)
        : base(userContextService, context)
    {
        _storeService = storeService;
        _moduleUsageService = moduleUsageService;
        _orderStatusMappingService = orderStatusMappingService;
        _context = context;
        _logger = logger;
        _giftVoucherFactory = giftVoucherFactory;
        _activityLogService = activityLogService;
    }

    public async Task<IActionResult> Index(int? categoryId = null)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var storeData = await _storeService.GetStoreDataAsync(userId);

        // Filter by category if specified
        if (categoryId.HasValue)
        {
            storeData.Modules = storeData.Modules.Where(m =>
                storeData.Categories.Any(c => c.Id == categoryId.Value && c.Name == m.CategoryName)).ToList();

            storeData.AvailableModules = storeData.AvailableModules.Where(m =>
                storeData.Categories.Any(c => c.Id == categoryId.Value && c.Name == m.CategoryName)).ToList();
        }

        ViewData["SelectedCategoryId"] = categoryId;
        return View(storeData);
    }

    [HttpGet]
    public async Task<IActionResult> GetModulesWithPackages()
    {
        var result = await _storeService.GetModulesWithPackagesAsync();
        return Json(new { success = true, data = result });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ValidateModelState]
    public async Task<IActionResult> PurchaseModule([FromBody] StoreIndexViewModel.PurchaseModuleViewModel model)
    {
        var userId = GetCurrentUserId();
        var companyId = GetCurrentUserCompanyId();
        if (string.IsNullOrEmpty(userId) || !companyId.HasValue)
        {
            return Json(ApiResponse.UserNotFound);
        }

        // Check if user already owns the module
        var isOwned = await _storeService.IsModuleOwnedAsync(model.ModuleId, userId);
        if (isOwned)
        {
            return Json(ApiResponse.Error("Bu modüle zaten sahipsiniz."));
        }

        // Get current user credit balance for pre-check
        var currentBalance = await _storeService.GetUserCreditBalanceAsync(userId);
        if (currentBalance < model.Price)
        {
            return Json(new
            {
                success = false,
                message = $"Yetersiz kredi bakiyesi. Gerekli: ₺{model.Price:N2}, Mevcut: ₺{currentBalance:N2}",
                errorType = "insufficient_balance",
                requiredAmount = model.Price,
                currentBalance = currentBalance,
                missingAmount = model.Price - currentBalance
            });
        }

        // Generate transaction ID
        var transactionId = $"TXN_{DateTime.Now:yyyyMMddHHmmss}_{userId}_{model.ModuleId}";

        // Parse subscription type
        var subscriptionType = model.SubscriptionType?.ToLower() == "yearly"
            ? PushDashboard.Models.SubscriptionType.Yearly
            : PushDashboard.Models.SubscriptionType.Monthly;

        // Attempt purchase with subscription info
        var (success, message) = await _storeService.PurchaseModuleAsync(
            model.ModuleId,
            userId,
            model.Price,
            subscriptionType,
            model.AutoRenewal,
            transactionId);

        if (success)
        {
            _logger.LogInformation("User {UserId} successfully purchased module {ModuleId} for {Price}",
                userId,
                model.ModuleId,
                model.Price);

            // Get updated balance for response
            var newBalance = await _storeService.GetUserCreditBalanceAsync(userId);

            // Activity Log - Module Purchase
            await _activityLogService.LogActivityAsync(
                userId,
                companyId.Value,
                ActivityActionTypes.Purchase,
                ActivityModules.Store,
                $"Modül satın alındı: ₺{model.Price:N2}",
                entityId: model.ModuleId.ToString(),
                entityType: "Module",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            return Json(new
            {
                success = true,
                message = message,
                transactionId = transactionId,
                newBalance = newBalance,
                purchasedAmount = model.Price
            });
        }
        else
        {
            return Json(new { success = false, message = message });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PurchasePackage([FromBody] PushPackageDtoRequest request)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue) return Json(ApiResponse.UserNotFound);

        try
        {
            var userId = GetCurrentUserId();
            var result = await _storeService.PurchaseModulePackageAsync(
                companyId.Value, request.ModuleId, request.PackageId,
                request.SubscriptionType, userId);

            if (result.Success)
            {
                return Json(ApiResponse.SuccessMessage(result.Message));
            }
            else
            {
                return Json(ApiResponse.Error(result.Message));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while purchasing package");
            return Json(ApiResponse.Error("İşlem sırasında hata oluştu."));
        }
    }

    // A helper class mapped to [FromBody] payload
    public class PushPackageDtoRequest
    {
        public int ModuleId { get; set; }
        public int PackageId { get; set; }
        /// <summary>"monthly" veya "yearly"</summary>
        public string SubscriptionType { get; set; } = "monthly";
    }

    [HttpGet]
    public async Task<IActionResult> GetModuleSettings(int moduleId)
    {
        var userId = GetCurrentUserId();
        var companyId = GetCurrentUserCompanyId();
        if (string.IsNullOrEmpty(userId) || !companyId.HasValue)
        {
            return Json(ApiResponse.UserNotFound);
        }

        var (settings, moduleInfo) = await _storeService.GetModuleSettingsAsync(moduleId, userId);

        if (moduleInfo == null)
        {
            return Json(ApiResponse.Error("Modül bulunamadı veya size ait değil."));
        }

        // Doğum günü modülü için maliyet bilgilerini ve e-ticaret platform bilgisini ekle
        if (moduleInfo.Name.Contains("Doğum Günü") || moduleInfo.Name.Contains("Birthday"))
        {
            var emailCost = await _moduleUsageService.GetModuleUsageCostAsync(companyId.Value, moduleInfo.Id, "email");
            var smsCost = await _moduleUsageService.GetModuleUsageCostAsync(companyId.Value, moduleInfo.Id, "sms");
            var whatsappCost = await _moduleUsageService.GetModuleUsageCostAsync(companyId.Value, moduleInfo.Id, "whatsapp");

            // Maliyet bilgilerini settings'e ekle
            if (settings == null)
                settings = new Dictionary<string, object>();

            settings["emailCost"] = emailCost;
            settings["smsCost"] = smsCost;
            settings["whatsappCost"] = whatsappCost;

            // E-ticaret platform bilgisini ekle
            var activePlatformName = await _giftVoucherFactory.GetActivePlatformNameAsync(companyId.Value);
            var hasEcommerceIntegration = await _giftVoucherFactory.HasActiveEcommerceIntegrationAsync(companyId.Value);

            settings["activePlatformName"] = activePlatformName ?? "";
            settings["hasEcommerceIntegration"] = hasEcommerceIntegration;
        }

        return Json(new
        {
            success = true,
            settings = settings,
            moduleInfo = new
            {
                id = moduleInfo.Id,
                name = moduleInfo.Name,
                description = moduleInfo.Description
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveModuleSettings(int moduleId, [FromBody] Dictionary<string, object> settings)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Json(ApiResponse.UserNotFound);
        }

        var success = await _storeService.SaveModuleSettingsAsync(moduleId, userId, settings);

        if (success)
        {
            _logger.LogInformation("User {UserId} updated settings for module {ModuleId}", userId, moduleId);

            // Activity Log - Save Module Settings
            var companyId = GetCurrentUserCompanyId();
            if (companyId.HasValue)
            {
                var moduleInfo = await _storeService.GetModuleInfoAsync(moduleId);
                var moduleName = moduleInfo?.Name ?? $"Modül #{moduleId}";

                var (activityModule, description) = ModuleActivityLogHelper.GenerateActivityLogForModule(
                    moduleId,
                    moduleName,
                    settings);

                await _activityLogService.LogActivityAsync(
                    userId,
                    companyId.Value,
                    ActivityActionTypes.Settings,
                    activityModule,
                    description,
                    entityId: moduleId.ToString(),
                    entityType: "Module",
                   details: new { ModuleName = moduleName, Settings = settings ?? new Dictionary<string, object>() },
                   ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString());
            }

            return Json(ApiResponse.SuccessMessage("Ayarlar başarıyla kaydedildi."));
        }
        else
        {
            return Json(ApiResponse.Error("Modül bulunamadı veya size ait değil."));
        }
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetModuleSettings(int moduleId)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Json(ApiResponse.UserNotFound);
        }

        var success = await _storeService.ResetModuleSettingsToDefaultAsync(moduleId, userId);

        if (success)
        {
            _logger.LogInformation("User {UserId} reset settings to default for module {ModuleId}", userId, moduleId);

            // Activity Log - Reset Module Settings
            var companyId = GetCurrentUserCompanyId();
            if (companyId.HasValue)
            {
                var moduleInfo = await _storeService.GetModuleInfoAsync(moduleId);
                var moduleName = moduleInfo?.Name ?? $"Modül #{moduleId}";

                await _activityLogService.LogActivityAsync(
                    userId,
                    companyId.Value,
                    ActivityActionTypes.Settings,
                    ActivityModules.Store,
                    $"{moduleName} modülü ayarları varsayılana sıfırlandı",
                    entityId: moduleId.ToString(),
                    entityType: "Module",
                    details: new { ModuleName = moduleName },
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString());
            }

            return Json(ApiResponse.SuccessMessage("Ayarlar varsayılan değerlere sıfırlandı."));
        }
        else
        {
            return Json(ApiResponse.Error("Modül bulunamadı veya size ait değil."));
        }
    }

    public async Task<IActionResult> GetTransactionHistory(int page = 1, int pageSize = 10)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Json(ApiResponse.UserNotFound);
        }

        var result = await _storeService.GetTransactionHistoryAsync(userId, page, pageSize);
        return Json(new { success = true, data = result });
    }

    [HttpGet]
    public async Task<IActionResult> GetModuleDetails(int moduleId)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Json(ApiResponse.UserNotFound);
        }

        var moduleDetails = await _storeService.GetModuleDetailsAsync(moduleId, userId);

        if (moduleDetails == null)
        {
            return Json(ApiResponse.Error("Modül bulunamadı."));
        }

        return Json(new
        {
            success = true,
            moduleDetails = moduleDetails
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleModuleStatus(int moduleId)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Json(ApiResponse.UserNotFound);
        }

        var (success, message) = await _storeService.ToggleModuleStatusAsync(moduleId, userId);

        if (success)
        {
            _logger.LogInformation("User {UserId} toggled status for module {ModuleId}", userId, moduleId);

            // Activity Log - Toggle Module Status
            var companyId = GetCurrentUserCompanyId();
            if (companyId.HasValue)
            {
                var moduleInfo = await _storeService.GetModuleInfoAsync(moduleId);
                var moduleName = moduleInfo?.Name ?? $"Modül #{moduleId}";

                await _activityLogService.LogActivityAsync(
                    userId,
                    companyId.Value,
                    message.Contains("aktif") ? ActivityActionTypes.Activate : ActivityActionTypes.Deactivate,
                    ActivityModules.Store,
                    $"{moduleName}: {message}",
                    entityId: moduleId.ToString(),
                    entityType: "Module",
                    details: new { ModuleName = moduleName },
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString());
            }

            return Json(new { success = true, message = message });
        }
        else
        {
            return Json(new { success = false, message = message });
        }
    }

    /// <summary>
    /// Modülün otomatik yenileme durumunu değiştirir
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleAutoRenewal(int moduleId)
    {
        var userId = GetCurrentUserId();
        var companyId = GetCurrentUserCompanyId();

        if (string.IsNullOrEmpty(userId) || !companyId.HasValue)
        {
            return Json(ApiResponse.UserNotFound);
        }

        var companyModule = await _context.CompanyModules
            .Include(cm => cm.Module)
            .FirstOrDefaultAsync(cm => cm.ModuleId == moduleId && cm.CompanyId == companyId.Value);

        if (companyModule == null)
        {
            return Json(ApiResponse.Error("Bu modüle sahip değilsiniz."));
        }

        // Toggle auto-renewal
        companyModule.AutoRenewal = !companyModule.AutoRenewal;
        companyModule.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        var status = companyModule.AutoRenewal ? "açıldı" : "kapatıldı";
        var message = $"{companyModule.Module?.Name ?? "Modül"} için otomatik yenileme {status}.";

        _logger.LogInformation("User {UserId} toggled auto-renewal for module {ModuleId} to {AutoRenewal}",
            userId, moduleId, companyModule.AutoRenewal);

        // Activity Log
        await _activityLogService.LogActivityAsync(
            userId,
            companyId.Value,
            companyModule.AutoRenewal ? ActivityActionTypes.Activate : ActivityActionTypes.Deactivate,
            ActivityModules.Store,
            message,
            entityId: moduleId.ToString(),
            entityType: "Module",
            details: new { AutoRenewal = companyModule.AutoRenewal },
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString());

        return Json(new
        {
            success = true,
            message = message,
            autoRenewal = companyModule.AutoRenewal
        });
    }

    #region Order Status Mapping Endpoints

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetOrderStatusMappingSettings(int moduleId)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        // Modülün "Sipariş Durumu Bildirimleri" olduğunu kontrol et
        var moduleInfo = await _storeService.GetModuleInfoAsync(moduleId);
        if (moduleInfo == null || moduleInfo.Name != "Sipariş Durumu Bildirimleri")
        {
            return Json(ApiResponse.Error("Bu özellik sadece Sipariş Durumu Bildirimleri modülü için kullanılabilir."));
        }

        // Şirketin aktif e-ticaret entegrasyonunu bul
        var integrationType = await GetActiveIntegrationTypeAsync(companyId);
        _logger.LogInformation("Integration type for company {CompanyId}: {IntegrationType}", companyId, integrationType);

        if (string.IsNullOrEmpty(integrationType))
        {
            return Json(ApiResponse.Error("Aktif e-ticaret entegrasyonu bulunamadı."));
        }

        // Mevcut mapping'leri al
        var currentMappings = await _orderStatusMappingService.GetMappingsAsync(companyId, integrationType);
        _logger.LogInformation("Current mappings count: {Count}", currentMappings.Count);

        // E-ticaret platform durumlarını al
        var externalStatuses = await _orderStatusMappingService.GetExternalStatusesAsync(companyId, integrationType);
        _logger.LogInformation("External statuses count: {Count}", externalStatuses.Count);

        // Sistem durumları
        var internalStatuses = GetInternalOrderStatuses();
        _logger.LogInformation("Internal statuses count: {Count}", internalStatuses.Count);

        var settings = new OrderStatusMappingSettingsViewModel
        {
            IntegrationType = integrationType,
            ExternalStatuses = externalStatuses,
            InternalStatuses = internalStatuses,
            CurrentMappings = currentMappings
        };

        return Json(new { success = true, settings = settings });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveOrderStatusMappings(int moduleId, [FromBody] List<OrderStatusMappingViewModel> mappings)
    {
        if (!TryGetUserContext(out var userId, out var companyId))
        {
            return Json(ApiResponse.Error("Kullanıcı veya şirket bulunamadı."));
        }

        // Modülün "Sipariş Durumu Bildirimleri" olduğunu kontrol et
        var moduleInfo = await _storeService.GetModuleInfoAsync(moduleId);
        if (moduleInfo == null || moduleInfo.Name != "Sipariş Durumu Bildirimleri")
        {
            return Json(ApiResponse.Error("Bu özellik sadece Sipariş Durumu Bildirimleri modülü için kullanılabilir."));
        }

        // Şirketin aktif e-ticaret entegrasyonunu bul
        var integrationType = await GetActiveIntegrationTypeAsync(companyId);
        if (string.IsNullOrEmpty(integrationType))
        {
            return Json(ApiResponse.Error("Aktif e-ticaret entegrasyonu bulunamadı."));
        }

        var success = await _orderStatusMappingService.SaveMappingsAsync(companyId, integrationType, mappings);

        if (success)
        {
            _logger.LogInformation("User {UserId} updated order status mappings for module {ModuleId}", userId, moduleId);
            return Json(ApiResponse.SuccessMessage("Sipariş durumu eşleştirmeleri başarıyla kaydedildi."));
        }
        else
        {
            return Json(ApiResponse.Error("Eşleştirmeler kaydedilemedi."));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateDefaultOrderStatusMappings(int moduleId)
    {
        if (!TryGetUserContext(out var userId, out var companyId))
        {
            return Json(ApiResponse.Error("Kullanıcı veya şirket bulunamadı."));
        }

        // Şirketin aktif e-ticaret entegrasyonunu bul
        var integrationType = await GetActiveIntegrationTypeAsync(companyId);
        if (string.IsNullOrEmpty(integrationType))
        {
            return Json(ApiResponse.Error("Aktif e-ticaret entegrasyonu bulunamadı."));
        }

        var success = await _orderStatusMappingService.CreateDefaultMappingsAsync(companyId, integrationType);

        if (success)
        {
            _logger.LogInformation("User {UserId} created default order status mappings for module {ModuleId}", userId, moduleId);
            return Json(ApiResponse.SuccessMessage("Varsayılan eşleştirmeler başarıyla oluşturuldu."));
        }
        else
        {
            return Json(ApiResponse.Error("Varsayılan eşleştirmeler oluşturulamadı."));
        }
    }

    #endregion

    #region Helper Methods

    private async Task<string?> GetActiveIntegrationTypeAsync(Guid companyId)
    {
        _logger.LogInformation("Looking for active ecommerce integration for company {CompanyId}", companyId);

        // Şirketin aktif e-ticaret entegrasyonunu bul
        var integration = await _context.CompanyIntegrations
            .Include(ci => ci.Integration)
            .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                       ci.Integration.Type == "ecommerce" &&
                                       ci.IsActive);

        if (integration?.Integration != null)
        {
            _logger.LogInformation("Found active integration: {IntegrationName} for company {CompanyId}",
                integration.Integration.Name,
                companyId);

            // Integration adına göre platform türünü belirle
            return integration.Integration.Name switch
            {
                "Ticimax" => "Ticimax",
                "Shopify" => "Shopify",
                "WooCommerce" => "WooCommerce",
                _ => integration.Integration.Name
            };
        }

        _logger.LogWarning("No active ecommerce integration found for company {CompanyId}", companyId);
        return null;
    }

    private List<OrderStatusSelectItem> GetInternalOrderStatuses()
    {
        return new List<OrderStatusSelectItem>
        {
            new() { Value = "1", Text = "Beklemede" },
            new() { Value = "2", Text = "Onaylandı" },
            new() { Value = "3", Text = "Hazırlanıyor" },
            new() { Value = "4", Text = "Kargoya Verildi" },
            new() { Value = "5", Text = "Teslim Edildi" },
            new() { Value = "6", Text = "İptal Edildi" },
            new() { Value = "7", Text = "İade Edildi" }
        };
    }

    #endregion
}
