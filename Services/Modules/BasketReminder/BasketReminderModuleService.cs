using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.DTOs;
using PushDashboard.Models;
using System.Text.Json;

namespace PushDashboard.Services.Modules.BasketReminder;

/// <summary>
/// Basket Reminder module access and management service implementation
/// </summary>
public class BasketReminderModuleService : IBasketReminderModuleService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BasketReminderModuleService> _logger;

    public BasketReminderModuleService(
        ApplicationDbContext context,
        ILogger<BasketReminderModuleService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> HasActiveModuleAsync(Guid companyId)
    {
        try
        {
            return await _context.CompanyModules
                .Include(cm => cm.Module)
                .AnyAsync(cm => cm.CompanyId == companyId && 
                               cm.Module.Name == "Sepet Hatırlatma" && 
                               cm.IsActive &&
                               (cm.ExpiresAt == null || cm.ExpiresAt > DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Basket Reminder module access for company {CompanyId}", companyId);
            return false;
        }
    }

    public async Task<ModuleStatusDto> GetModuleStatusAsync(Guid companyId)
    {
        try
        {
            var companyModule = await _context.CompanyModules
                .Include(cm => cm.Module)
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId && cm.Module.Name == "Sepet Hatırlatma");

            if (companyModule == null)
            {
                return new ModuleStatusDto
                {
                    IsOwned = false,
                    IsActive = false,
                    PurchasedAt = null,
                    ExpiresAt = null,
                    PaidAmount = null,
                    TransactionId = null,
                    LastUsedAt = null
                };
            }

            return new ModuleStatusDto
            {
                IsOwned = true,
                IsActive = companyModule.IsActive && (companyModule.ExpiresAt == null || companyModule.ExpiresAt > DateTime.UtcNow),
                PurchasedAt = companyModule.PurchasedAt,
                ExpiresAt = companyModule.ExpiresAt,
                PaidAmount = companyModule.PaidAmount,
                TransactionId = companyModule.TransactionId,
                LastUsedAt = companyModule.LastUsedAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Basket Reminder module status for company {CompanyId}", companyId);
            return new ModuleStatusDto { IsOwned = false, IsActive = false };
        }
    }

    public async Task<Dictionary<string, object>?> GetModuleSettingsAsync(Guid companyId)
    {
        try
        {
            var companyModule = await _context.CompanyModules
                .Include(cm => cm.Settings)
                .Include(cm => cm.Module)
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId && cm.Module.Name == "Sepet Hatırlatma");

            if (companyModule?.Settings != null)
            {
                return companyModule.Settings.Settings;
            }

            if (companyModule?.Module?.DefaultSettings != null)
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(companyModule.Module.DefaultSettings);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Basket Reminder module settings for company {CompanyId}", companyId);
            return null;
        }
    }

    public async Task<(bool Success, string Message)> UpdateModuleSettingsAsync(Guid companyId, string userId, Dictionary<string, object> settings)
    {
        try
        {
            var companyModule = await _context.CompanyModules
                .Include(cm => cm.Settings)
                .Include(cm => cm.Module)
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId && cm.Module.Name == "Sepet Hatırlatma");

            if (companyModule == null)
            {
                return (false, "Sepet Hatırlatma modülü bulunamadı.");
            }

            if (companyModule.Settings == null)
            {
                companyModule.Settings = new CompanyModuleSettings
                {
                    CompanyModuleId = companyModule.Id,
                    Settings = settings,
                    UpdatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.CompanyModuleSettings.Add(companyModule.Settings);
            }
            else
            {
                companyModule.Settings.Settings = settings;
                companyModule.Settings.UpdatedByUserId = userId;
                companyModule.Settings.UpdatedAt = DateTime.UtcNow;
                _context.CompanyModuleSettings.Update(companyModule.Settings);
            }

            await _context.SaveChangesAsync();
            return (true, "Ayarlar başarıyla güncellendi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating Basket Reminder module settings for company {CompanyId}", companyId);
            return (false, "Ayarlar güncellenirken bir hata oluştu.");
        }
    }
}
