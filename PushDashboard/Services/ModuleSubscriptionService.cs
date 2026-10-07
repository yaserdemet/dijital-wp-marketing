using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;

namespace PushDashboard.Services;

/// <summary>
/// Modül abonelik yenileme ve yönetimi servisi
/// </summary>
public class ModuleSubscriptionService : IModuleSubscriptionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ModuleSubscriptionService> _logger;

    public ModuleSubscriptionService(ApplicationDbContext context, ILogger<ModuleSubscriptionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<RenewalResult>> ProcessExpiredSubscriptionsAsync()
    {
        var results = new List<RenewalResult>();
        var now = DateTime.UtcNow;

        try
        {
            // Süresi dolan ve otomatik yenileme aktif olan abonelikleri bul
            var expiredSubscriptions = await _context.CompanyModules
                .Include(cm => cm.Company)
                .Include(cm => cm.Module)
                .Where(cm => cm.IsOwned &&
                             cm.IsActive &&
                             !cm.IsPassive &&
                             cm.AutoRenewal &&
                             cm.ExpiresAt.HasValue &&
                             cm.ExpiresAt.Value <= now)
                .ToListAsync();

            _logger.LogInformation("Found {Count} expired subscriptions to process", expiredSubscriptions.Count);

            foreach (var subscription in expiredSubscriptions)
            {
                var result = await ProcessSingleRenewalAsync(subscription);
                results.Add(result);
            }

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing expired subscriptions");
            return results;
        }
    }

    /// <inheritdoc />
    public async Task<RenewalResult> RenewSubscriptionAsync(Guid companyId, int moduleId)
    {
        try
        {
            var subscription = await _context.CompanyModules
                .Include(cm => cm.Company)
                .Include(cm => cm.Module)
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId &&
                                           cm.ModuleId == moduleId &&
                                           cm.IsOwned);

            if (subscription == null)
            {
                return new RenewalResult
                {
                    CompanyId = companyId,
                    ModuleId = moduleId,
                    Success = false,
                    Message = "Abonelik bulunamadı"
                };
            }

            return await ProcessSingleRenewalAsync(subscription);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renewing subscription for company {CompanyId}, module {ModuleId}", companyId, moduleId);
            return new RenewalResult
            {
                CompanyId = companyId,
                ModuleId = moduleId,
                Success = false,
                Message = "Yenileme sırasında bir hata oluştu"
            };
        }
    }

    /// <inheritdoc />
    public async Task<List<ExpiringSubscriptionDto>> GetExpiringSubscriptionsAsync(int daysBeforeExpiry = 3)
    {
        var now = DateTime.UtcNow;
        var expiryThreshold = now.AddDays(daysBeforeExpiry);

        try
        {
            var expiringSubscriptions = await _context.CompanyModules
                .Include(cm => cm.Company)
                .Include(cm => cm.Module)
                .Where(cm => cm.IsOwned &&
                             cm.IsActive &&
                             !cm.IsPassive &&
                             cm.ExpiresAt.HasValue &&
                             cm.ExpiresAt.Value > now &&
                             cm.ExpiresAt.Value <= expiryThreshold)
                .Select(cm => new ExpiringSubscriptionDto
                {
                    CompanyId = cm.CompanyId,
                    CompanyName = cm.Company.CompanyName ?? "Bilinmiyor",
                    ModuleId = cm.ModuleId,
                    ModuleName = cm.Module.Name ?? "Bilinmiyor",
                    ExpiresAt = cm.ExpiresAt!.Value,
                    DaysUntilExpiry = (int)(cm.ExpiresAt.Value - now).TotalDays,
                    AutoRenewal = cm.AutoRenewal,
                    RenewalPrice = cm.SubscriptionType == SubscriptionType.Yearly
                        ? cm.Module.YearlyPrice
                        : cm.Module.MonthlyPrice
                })
                .ToListAsync();

            return expiringSubscriptions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expiring subscriptions");
            return new List<ExpiringSubscriptionDto>();
        }
    }

    private async Task<RenewalResult> ProcessSingleRenewalAsync(CompanyModule subscription)
    {
        var result = new RenewalResult
        {
            CompanyId = subscription.CompanyId,
            ModuleId = subscription.ModuleId,
            ModuleName = subscription.Module?.Name ?? "Bilinmiyor"
        };

        try
        {
            // Yenileme fiyatını hesapla
            var renewalPrice = subscription.SubscriptionType == SubscriptionType.Yearly
                ? (subscription.Module?.YearlyPrice ?? 0)
                : (subscription.Module?.MonthlyPrice ?? 0);

            if (renewalPrice <= 0)
            {
                result.Success = false;
                result.Message = "Modül ücreti bulunamadı";
                return result;
            }

            // Şirket bakiyesini kontrol et
            if (subscription.Company == null)
            {
                result.Success = false;
                result.Message = "Şirket bilgisi bulunamadı";
                return result;
            }

            if (subscription.Company.CreditBalance < renewalPrice)
            {
                // Yetersiz bakiye - modülü pasife al
                subscription.IsPassive = true;
                subscription.UpdatedAt = DateTime.Now;

                _logger.LogWarning(
                    "Insufficient balance for subscription renewal. Company: {CompanyId}, Module: {ModuleId}, Required: {Required}, Available: {Available}",
                    subscription.CompanyId,
                    subscription.ModuleId,
                    renewalPrice,
                    subscription.Company.CreditBalance);

                await _context.SaveChangesAsync();

                result.Success = false;
                result.Message = $"Yetersiz bakiye. Gerekli: ₺{renewalPrice:N2}, Mevcut: ₺{subscription.Company.CreditBalance:N2}. Modül pasife alındı.";
                return result;
            }

            // Bakiyeden düş
            subscription.Company.CreditBalance -= renewalPrice;

            // Yeni bitiş tarihini hesapla
            var currentExpiryDate = subscription.ExpiresAt ?? DateTime.Now;
            var newExpiryDate = subscription.SubscriptionType == SubscriptionType.Yearly
                ? currentExpiryDate.AddYears(1)
                : currentExpiryDate.AddMonths(1);

            // Abonelik bilgilerini güncelle
            subscription.ExpiresAt = newExpiryDate;
            subscription.LastRenewalDate = DateTime.Now;
            subscription.RenewalCount++;
            subscription.PaidAmount = renewalPrice;
            subscription.UpdatedAt = DateTime.Now;

            // Record renewal in ModuleUsageLog for spending history
            var usageLog = new ModuleUsageLog
            {
                CompanyId = subscription.CompanyId,
                ModuleId = subscription.ModuleId,
                UsageType = "Abonelik Yenilendi",
                Description = $"{subscription.Module?.Name ?? "Modül"} aboneliği yenilendi ({(subscription.SubscriptionType == SubscriptionType.Yearly ? "Yıllık" : "Aylık")})",
                Cost = renewalPrice,
                BalanceBefore = subscription.Company.CreditBalance + renewalPrice, // Before deduction
                BalanceAfter = subscription.Company.CreditBalance,
                UserId = subscription.PurchasedByUserId ?? "system",
                ReferenceId = $"RENEWAL_{subscription.Id}_{DateTime.Now:yyyyMMddHHmmss}",
                Channel = subscription.SubscriptionType == SubscriptionType.Yearly ? "yearly" : "monthly",
                IsSuccessful = true,
                CreatedAt = DateTime.Now
            };
            _context.ModuleUsageLogs.Add(usageLog);

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Successfully renewed subscription. Company: {CompanyId}, Module: {ModuleId}, Amount: {Amount}, NewExpiry: {NewExpiry}",
                subscription.CompanyId,
                subscription.ModuleId,
                renewalPrice,
                newExpiryDate);

            result.Success = true;
            result.Message = $"Abonelik başarıyla yenilendi. Yeni bitiş tarihi: {newExpiryDate:dd.MM.yyyy}";
            result.AmountCharged = renewalPrice;
            result.NewExpiryDate = newExpiryDate;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing renewal for company {CompanyId}, module {ModuleId}",
                subscription.CompanyId, subscription.ModuleId);

            result.Success = false;
            result.Message = "Yenileme işlemi sırasında bir hata oluştu";
            return result;
        }
    }
}
