using Microsoft.AspNetCore.Mvc;
using PushDashboard.Data;
using PushDashboard.Helpers;
using PushDashboard.Models;
using PushDashboard.Services;
using PushDashboard.ViewModels;
using PushDashboard.Filters;

namespace PushDashboard.Controllers;

public class IntegrationController : BaseController
{
    private readonly IIntegrationService _integrationService;
    private readonly ILogger<IntegrationController> _logger;
    private readonly IActivityLogService _activityLogService;
    private readonly PushDashboard.Services.WhatsApp.IWhatsAppService _whatsAppService;

    public IntegrationController(
        IIntegrationService integrationService,
        ILogger<IntegrationController> logger,
        ApplicationDbContext context,
        IUserContextService userContextService,
        IActivityLogService activityLogService,
        PushDashboard.Services.WhatsApp.IWhatsAppService whatsAppService)
        : base(userContextService, context)
    {
        _integrationService = integrationService;
        _logger = logger;
        _activityLogService = activityLogService;
        _whatsAppService = whatsAppService;
    }

    public async Task<IActionResult> Index()
    {
        var user = GetCurrentUser();

        if (user?.Company == null)
        {
            return View(new IntegrationIndexViewModel());
        }

        var groupedIntegrations = await _integrationService.GetGroupedIntegrationsAsync(user.Company.Id);
        var companyIntegrations = await _integrationService.GetCompanyIntegrationsAsync(user.Company.Id);

        var viewModel = new IntegrationIndexViewModel
        {
            GroupedIntegrations = groupedIntegrations
                .GroupBy(g => g.Category)
                .ToDictionary(
                    g => g.Key,
                    g => g.SelectMany(gi => gi.Integrations)
                        .Select(i => new Integration
                        {
                            Id = i.Id,
                            Name = i.Name,
                            Description = i.Description,
                            IconClass = i.IconClass,
                            IconColor = i.IconColor,
                            Type = i.Type
                        }).ToList()),
            CompanyIntegrations = companyIntegrations.ToDictionary(ci => ci.IntegrationId, ci => ci)
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ValidateModelState]
    public async Task<IActionResult> AddIntegration([FromBody] AddIntegrationRequest request)
    {
        var user = GetCurrentUserWithCompany();
        if (user?.Company == null)
        {
            return Json(ApiResponse.UserCompanyNotFound);
        }

        var companyIntegration = await _integrationService.AddIntegrationAsync(user.Company.Id, request.IntegrationId);

        // Activity Log - Add Integration
        await _activityLogService.LogActivityAsync(
            user.Id,
            user.Company.Id,
            ActivityActionTypes.Create,
            ActivityModules.Integration,
            $"Yeni entegrasyon eklendi: {companyIntegration.Integration?.Name ?? "Entegrasyon"}",
            entityId: request.IntegrationId.ToString(),
            entityType: "Integration",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString());

        return Json(new { success = true, message = $"{companyIntegration.Integration?.Name ?? "Entegrasyon"} başarıyla eklendi." });
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetSettings(int integrationId)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var settings = await _integrationService.GetIntegrationSettingsAsync(companyId, integrationId);

        return Json(new { success = true, settings });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ValidateModelState]
    public async Task<IActionResult> SaveSettings([FromBody] SaveSettingsRequest request)
    {
        try
        {
            var user = GetCurrentUserWithCompany();
            if (user?.Company == null)
            {
                return Json(ApiResponse.UserCompanyNotFound);
            }

            var message = await _integrationService.SaveIntegrationSettingsAsync(
                user.Company.Id,
                request.IntegrationId,
                request.Settings,
                user.Id,
                Request.Scheme,
                Request.Host.ToString());

            // Activity Log - Save Integration Settings
            await _activityLogService.LogActivityAsync(
                user.Id,
                user.Company.Id,
                ActivityActionTypes.Configure,
                ActivityModules.Integration,
                "Entegrasyon ayarları güncellendi",
                entityId: request.IntegrationId.ToString(),
                entityType: "Integration",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            return Json(new { success = true, message });
        }
        catch (PushDashboard.Exceptions.ValidationException ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving integration settings");
            return Json(new { success = false, message = "Ayarlar kaydedilirken bir hata oluştu." });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ValidateModelState]
    public async Task<IActionResult> TestConnection([FromBody] TestConnectionRequest request)
    {
        var companyId = GetCurrentUserCompanyId();
        var testResult = await _integrationService.TestConnectionAsync(request.IntegrationId, request.Settings, companyId);
        
        return Json(new {
            success = testResult.Success,
            message = testResult.Message,
            errorCode = testResult.ErrorCode,
            responseTime = testResult.ResponseTime?.TotalMilliseconds ?? 0,
            additionalInfo = testResult.AdditionalInfo
        });
    }

    #region Order Status Module Actions

    [HttpGet]
    public async Task<IActionResult> CheckOrderStatusModule()
    {
        var companyId = GetCurrentUserCompanyId();
        if (companyId == null)
        {
            return Json(new { success = false, message = "Şirket bulunamadı." });
        }

        var hasModule = await _integrationService.HasOrderStatusModuleAsync(companyId.Value);

        return Json(new { success = true, hasModule });
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetStatusMappings(int integrationId)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var mappings = await _integrationService.GetStatusMappingsAsync(companyId, integrationId);

        return Json(new { success = true, data = mappings });
    }

    [HttpPost]
    public async Task<IActionResult> SaveStatusMappings([FromBody] SaveStatusMappingsRequest request)
    {
        var user = GetCurrentUserWithCompany();
        if (user?.Company == null)
        {
            return Json(ApiResponse.UserCompanyNotFound);
        }

        await _integrationService.SaveStatusMappingsAsync(
            user.Company.Id,
            request.IntegrationId,
            request.StatusMappings,
            user.Id);

        return Json(new { success = true, message = "Durum eşleştirmeleri başarıyla kaydedildi." });
    }

    #endregion

    #region Order Status Mapping Helper Methods

    [HttpGet]
    public IActionResult GetDefaultTicimaxMappings()
    {
        return Json(new { success = true, data = _integrationService.GetDefaultTicimaxMappings() });
    }

    #endregion

    #region Sync Integration

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ValidateModelState]
    public async Task<IActionResult> SyncIntegration([FromBody] SyncIntegrationRequest request)
    {
        var user = GetCurrentUserWithCompany();
        if (user?.Company == null)
        {
            _logger.LogWarning("SyncIntegration called but company not found");
            return Json(ApiResponse.UserCompanyNotFound);
        }

        var syncData = await _integrationService.SyncIntegrationAsync(user.Company.Id, request.IntegrationId);

        // Activity Log - Sync Integration
        await _activityLogService.LogActivityAsync(
            user.Id,
            user.Company.Id,
            ActivityActionTypes.Sync,
            ActivityModules.Integration,
            $"Entegrasyon senkronize edildi: {syncData.SyncedCustomers} müşteri, {syncData.SyncedCarts} sepet",
            entityId: request.IntegrationId.ToString(),
            entityType: "Integration",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString());

        return Json(new
        {
            success = true,
            message = syncData.Message,
            syncedCustomers = syncData.SyncedCustomers,
            syncedCarts = syncData.SyncedCarts,
            syncDuration = syncData.SyncDuration
        });
    }

    #endregion

    #region Get All Integrations

    /// <summary>
    /// Şirketin tüm aktif entegrasyonlarını getirir (Dashboard senkronizasyonu için)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllIntegrations()
    {
        var user = GetCurrentUserWithCompany();
        if (user?.Company == null)
        {
            return Json(ApiResponse.UserCompanyNotFound);
        }

        var integrations = await _integrationService.GetActiveCompanyIntegrationsAsync(user.Company.Id);

        var result = integrations.Select(i => new
        {
            id = i.Id,
            name = i.Name,
            type = i.Type
        }).ToList();

        return Json(new
        {
            success = true,
            integrations = result,
            count = result.Count
        });
    }

    #endregion

    #region Module Embed Code

    /// <summary>
    /// Tüm modüller için genel embed kod formatını döner
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetModuleEmbedCode()
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var embedCode = await _integrationService.GenerateModuleEmbedCodeAsync(companyId.Value);

        return Json(new
        {
            success = true,
            embedCode = embedCode,
            companyId = companyId
        });
    }

    #endregion

    #region Gupshup Integration

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGupshupApp([FromBody] CreateGupshupAppRequest request)
    {
        var user = GetCurrentUserWithCompany();
        if (user?.Company == null)
        {
            return Json(ApiResponse.UserCompanyNotFound);
        }

        try
        {
            string appName = $"{(user.Company.CompanyName ?? user.Company.Name).Replace(" ","")}{new Random().Next(0,10000)}";

            var result = await _whatsAppService.CreatePartnerAppAsync(appName);

            if (result.Success && !string.IsNullOrEmpty(result.Data))
            {
                var appId = result.Data;

                // Get the token for the app
                var tokenResult = await _whatsAppService.GetAppTokenAsync(appId);

                if (!tokenResult.Success || string.IsNullOrEmpty(tokenResult.Data))
                {
                    _logger.LogError("Failed to get token for app {AppId}: {Message}", appId, tokenResult.Message);
                    return Json(new { success = false, message = $"Uygulama oluşturuldu ancak token alınamadı: {tokenResult.Message}" });
                }

                var token = tokenResult.Data;

                // Save AppID and Token to integration settings
                var currentSettings = await _integrationService.GetIntegrationSettingsAsync(user.Company.Id, request.IntegrationId);
                
                // Update or add appId
                if (currentSettings.ContainsKey("appId"))
                    currentSettings["appId"] = appId;
                else
                    currentSettings.Add("appId", appId);

                if (currentSettings.ContainsKey("token"))
                    currentSettings["token"] = token;
                else
                    currentSettings.Add("token", token);
                
                // Enable Template Analytics (1 dakika gecikmeli, arka planda)
                var capturedAppId = appId;
                var capturedToken = token;
                _ = Task.Run(async () =>
                {
                    await Task.Delay(TimeSpan.FromMinutes(1));
                    var analyticsResult = await _whatsAppService.EnableTemplateAnalyticsAsync(capturedAppId, capturedToken);
                    if (!analyticsResult.Success)
                    {
                        _logger.LogWarning("Failed to enable template analytics for AppId {AppId}: {Message}", capturedAppId, analyticsResult.Message);
                    }
                    else
                    {
                        _logger.LogInformation("Template analytics enabled for AppId {AppId}", capturedAppId);
                    }
                });
                
                await _integrationService.SaveIntegrationSettingsAsync(
                    user.Company.Id,
                    request.IntegrationId,
                    currentSettings,
                    user.Id,
                    Request.Scheme,
                    Request.Host.ToString());

                return Json(new { success = true, appId = appId, message = "Gupshup uygulaması ve token başarıyla oluşturuldu." });
            }
            else
            {
                return Json(new { success = false, message = result.Message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error create gupshup app");
            return Json(new { success = false, message = "Uygulama oluşturulurken hata oluştu." });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetGupshupOnboardingUrl(string appId)
    {
        var user = GetCurrentUserWithCompany();
        if (user?.Company == null) return Json(ApiResponse.UserCompanyNotFound);

        var result = await _whatsAppService.GetOnboardingEmbedUrlAsync(appId);

        if (result.Success)
        {
            // Get the token for the app
            var tokenResult = await _whatsAppService.GetAppTokenAsync(appId);

            if (!tokenResult.Success || string.IsNullOrEmpty(tokenResult.Data))
            {
                _logger.LogError("Failed to get token for app {AppId}: {Message}", appId, tokenResult.Message);
                return Json(new { success = false, message = $"Uygulama oluşturuldu ancak token alınamadı: {tokenResult.Message}" });
            }

            var token = tokenResult.Data;

            // Save AppID and Token to integration settings
            var currentSettings = await _integrationService.GetIntegrationSettingsAsync(user.Company.Id, 5);

            // Update or add appId
            if (currentSettings.ContainsKey("appId"))
                currentSettings["appId"] = appId;
            else
                currentSettings.Add("appId", appId);

            // Update or add token
            if (currentSettings.ContainsKey("token"))
                currentSettings["token"] = token;
            else
                currentSettings.Add("token", token);

            // Enable Template Analytics (1 dakika gecikmeli, arka planda)
            var capturedAppId2 = appId;
            var capturedToken2 = token;
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromMinutes(1));
                var analyticsResult = await _whatsAppService.EnableTemplateAnalyticsAsync(capturedAppId2, capturedToken2);
                if (!analyticsResult.Success)
                {
                    _logger.LogWarning("Failed to enable template analytics for AppId {AppId}: {Message}", capturedAppId2, analyticsResult.Message);
                }
                else
                {
                    _logger.LogInformation("Template analytics enabled for AppId {AppId}", capturedAppId2);
                }
            });

            await _integrationService.SaveIntegrationSettingsAsync(
                user.Company.Id,
                5,
                currentSettings,
                user.Id,
                Request.Scheme,
                Request.Host.ToString());

            return Json(new { success = true, url = result.Data });
        }

        return Json(new { success = false, message = result.Message });
    }

    public class CreateGupshupAppRequest
    {
        public int IntegrationId { get; set; }
    }

    #endregion

    #region Helper Methods

    private ApplicationUser? GetCurrentUserWithCompany()
    {
        return GetCurrentUser();
    }

    #endregion
}