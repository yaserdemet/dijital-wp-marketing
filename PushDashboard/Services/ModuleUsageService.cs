using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage;

namespace PushDashboard.Services;

public class ModuleUsageService : IModuleUsageService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ModuleUsageService> _logger;

    public ModuleUsageService(ApplicationDbContext context, ILogger<ModuleUsageService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(bool Success, string Message)> DeductUsageCostAsync(
        Guid companyId,
        int moduleId,
        string usageType,
        decimal cost,
        string description,
        string userId,
        string? referenceId = null,
        string? channel = null,
        Dictionary<string, object>? metadata = null)
    {
        // moduleId kontrolü - geçersiz değer varsa hata dön
        if (moduleId <= 0)
        {
            _logger.LogWarning("Invalid moduleId ({ModuleId}) passed to DeductUsageCostAsync. CompanyId: {CompanyId}", moduleId, companyId);
            return (false, "Geçersiz modül ID.");
        }

        // Check if there's already an active transaction
        var hasExistingTransaction = _context.Database.CurrentTransaction != null;
        IDbContextTransaction? transaction = null;

        try
        {
            // ModuleId'nin veritabanında var olduğunu doğrula
            var moduleExists = await _context.Modules.AnyAsync(m => m.Id == moduleId);
            if (!moduleExists)
            {
                _logger.LogWarning("Module with Id {ModuleId} not found in database. CompanyId: {CompanyId}", moduleId, companyId);
                return (false, "Modül bulunamadı.");
            }

            // Only start a new transaction if there isn't one already
            if (!hasExistingTransaction)
            {
                transaction = await _context.Database.BeginTransactionAsync();
            }

            // Şirket bilgisini al ve bakiyeyi kontrol et
            var company = await _context.Companies.FindAsync(companyId);
            if (company == null)
            {
                return (false, "Şirket bulunamadı.");
            }

            if (company.CreditBalance < cost)
            {
                return (false, $"Yetersiz bakiye. Mevcut bakiye: {company.CreditBalance:C}, Gerekli tutar: {cost:C}");
            }

            var balanceBefore = company.CreditBalance;
            company.CreditBalance -= cost;
            var balanceAfter = company.CreditBalance;

            // UserId kontrolü - boş string veya "system" gibi geçersiz değerler için sistem kullanıcısını kullan
            var validUserId = GetValidUserId(userId);

            // ModuleUsageLog kaydı oluştur
            var usageLog = new ModuleUsageLog
            {
                CompanyId = companyId,
                ModuleId = moduleId,
                UsageType = usageType,
                Description = description,
                Cost = cost,
                BalanceBefore = balanceBefore,
                BalanceAfter = balanceAfter,
                UserId = validUserId,
                ReferenceId = referenceId,
                Channel = channel,
                IsSuccessful = true,
                Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null,
                CreatedAt = DateTime.Now
            };

            _context.ModuleUsageLogs.Add(usageLog);
            _context.Companies.Update(company);
            await _context.SaveChangesAsync();

            // Only commit if we started the transaction
            if (transaction != null)
            {
                await transaction.CommitAsync();
            }

            _logger.LogInformation("Module usage cost deducted successfully. Company: {CompanyId}, Module: {ModuleId}, Cost: {Cost}, New Balance: {Balance}",
                companyId, moduleId, cost, balanceAfter);

            return (true, $"İşlem başarılı. Düşülen tutar: {cost:C}, Kalan bakiye: {balanceAfter:C}");
        }
        catch (Exception ex)
        {
            // Only rollback if we started the transaction
            if (transaction != null)
            {
                await transaction.RollbackAsync();
            }

            _logger.LogError(ex, "Error deducting module usage cost for company {CompanyId}, module {ModuleId}", companyId, moduleId);

            // Hata durumunda da log kaydı oluştur (only if we don't have an existing transaction)
            if (!hasExistingTransaction)
            {
                try
                {
                    // UserId kontrolü - boş string veya "system" gibi geçersiz değerler için sistem kullanıcısını kullan
                    var validUserId = GetValidUserId(userId);

                    var errorLog = new ModuleUsageLog
                    {
                        CompanyId = companyId,
                        ModuleId = moduleId,
                        UsageType = usageType,
                        Description = description,
                        Cost = cost,
                        BalanceBefore = 0,
                        BalanceAfter = 0,
                        UserId = validUserId,
                        ReferenceId = referenceId,
                        Channel = channel,
                        IsSuccessful = false,
                        ErrorMessage = ex.Message,
                        Metadata = metadata != null ? JsonSerializer.Serialize(metadata) : null,
                        CreatedAt = DateTime.Now
                    };

                    _context.ModuleUsageLogs.Add(errorLog);
                    await _context.SaveChangesAsync();
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Error creating error log for module usage");
                }
            }

            return (false, "Modül kullanım maliyeti düşülürken hata oluştu: " + ex.Message);
        }
        finally
        {
            // Dispose the transaction if we created it
            transaction?.Dispose();
        }
    }

    public async Task<bool> HasActiveModuleAsync(string userId, int moduleId)
    {
        // Get user's company
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.CompanyId == null)
            return false;

        return await _context.CompanyModules
            .AnyAsync(cm => cm.CompanyId == user.CompanyId &&
                           cm.ModuleId == moduleId &&
                           cm.IsActive &&
                           (cm.ExpiresAt == null || cm.ExpiresAt > DateTime.UtcNow));
    }
    
    public async Task<bool> HasActiveModuleAsync(Guid companyId, int moduleId)
    {
        return await _context.CompanyModules
            .AnyAsync(cm => cm.CompanyId == companyId &&
                            cm.ModuleId == moduleId &&
                            cm.IsActive &&
                            (cm.ExpiresAt == null || cm.ExpiresAt > DateTime.UtcNow));
    }

    public async Task<bool> HasActiveModuleAsync(string userId, string moduleName)
    {
        // Get user's company
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user?.CompanyId == null)
            return false;

        return await _context.CompanyModules
            .Include(cm => cm.Module)
            .AnyAsync(cm => cm.CompanyId == user.CompanyId &&
                           cm.Module.Name == moduleName &&
                           cm.IsActive &&
                           (cm.ExpiresAt == null || cm.ExpiresAt > DateTime.UtcNow));
    }

    public async Task<decimal> GetModuleUsageCostAsync(Guid companyId, int moduleId, string usageType)
    {
        try
        {
            // Önce şirketin modül ayarlarından kontrol et
            var companyModule = await _context.CompanyModules
                .Include(cm => cm.Settings)
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId && cm.ModuleId == moduleId && cm.IsActive);

            if (companyModule?.Settings?.SettingsJson != null)
            {
                var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(companyModule.Settings.SettingsJson);
                var costKey = $"{usageType}Cost";

                if (settings != null && settings.ContainsKey(costKey))
                {
                    if (decimal.TryParse(settings[costKey].ToString(), out decimal customCost))
                    {
                        return customCost;
                    }
                }
            }

            // Kullanıcı ayarı yoksa modülün default ayarlarından al
            var module = await _context.Modules.FindAsync(moduleId);
            if (module?.DefaultSettings != null)
            {
                var defaultSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(module.DefaultSettings);
                var costKey = $"{usageType}Cost";

                if (defaultSettings != null && defaultSettings.ContainsKey(costKey))
                {
                    if (decimal.TryParse(defaultSettings[costKey].ToString(), out decimal defaultCost))
                    {
                        return defaultCost;
                    }
                }
            }

            // Hiçbir ayar bulunamazsa varsayılan değerler
            return usageType.ToLower() switch
            {
                "whatsapp" => 1.00m,
                _ => 1.00m
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting module usage cost for company {CompanyId}, module {ModuleId}, usage type {UsageType}",
                companyId, moduleId, usageType);
            return 1.00m; // Hata durumunda varsayılan değer
        }
    }

    public async Task<List<ModuleUsageLog>> GetUsageHistoryAsync(Guid companyId, int? moduleId = null, DateTime? startDate = null, DateTime? endDate = null, int limit = 100, List<string>? excludeUsageTypes = null)
    {
        var query = BuildUsageHistoryQuery(companyId, moduleId, startDate, endDate, excludeUsageTypes);

        return await query
            .OrderByDescending(mul => mul.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<ModuleUsageLog>> GetUsageHistoryPagedAsync(Guid companyId, int? moduleId = null, DateTime? startDate = null, DateTime? endDate = null, int page = 1, int pageSize = 20, List<string>? excludeUsageTypes = null)
    {
        var query = BuildUsageHistoryQuery(companyId, moduleId, startDate, endDate, excludeUsageTypes);

        var skip = (page - 1) * pageSize;

        return await query
            .OrderByDescending(mul => mul.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetUsageHistoryCountAsync(Guid companyId, int? moduleId = null, DateTime? startDate = null, DateTime? endDate = null, List<string>? excludeUsageTypes = null)
    {
        var query = _context.ModuleUsageLogs
            .Where(mul => mul.CompanyId == companyId);

        if (excludeUsageTypes != null && excludeUsageTypes.Any())
        {
            query = query.Where(mul => !excludeUsageTypes.Contains(mul.UsageType));
        }

        if (moduleId.HasValue)
        {
            query = query.Where(mul => mul.ModuleId == moduleId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(mul => mul.CreatedAt.Date >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            query = query.Where(mul => mul.CreatedAt.Date <= endDate.Value.Date);
        }

        return await query.CountAsync();
    }

    private IQueryable<ModuleUsageLog> BuildUsageHistoryQuery(Guid companyId, int? moduleId, DateTime? startDate, DateTime? endDate, List<string>? excludeUsageTypes = null)
    {
        var query = _context.ModuleUsageLogs
            .Include(mul => mul.Module)
            .Include(mul => mul.User)
            .Where(mul => mul.CompanyId == companyId);

        if (excludeUsageTypes != null && excludeUsageTypes.Any())
        {
            query = query.Where(mul => !excludeUsageTypes.Contains(mul.UsageType));
        }

        if (moduleId.HasValue)
        {
            query = query.Where(mul => mul.ModuleId == moduleId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(mul => mul.CreatedAt.Date >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            query = query.Where(mul => mul.CreatedAt.Date <= endDate.Value.Date);
        }

        return query;
    }

    public async Task<ModuleUsageStats> GetUsageStatsAsync(Guid companyId, DateTime? startDate = null, DateTime? endDate = null, List<string>? excludeUsageTypes = null)
    {
        var query = _context.ModuleUsageLogs
            .Include(mul => mul.Module)
            .Where(mul => mul.CompanyId == companyId && mul.IsSuccessful);

        if (excludeUsageTypes != null && excludeUsageTypes.Any())
        {
            query = query.Where(mul => !excludeUsageTypes.Contains(mul.UsageType));
        }

        if (startDate.HasValue)
        {
            query = query.Where(mul => mul.CreatedAt.Date >= startDate.Value.Date);
        }

        if (endDate.HasValue)
        {
            query = query.Where(mul => mul.CreatedAt.Date <= endDate.Value.Date);
        }

        var logs = await query.ToListAsync();

        var stats = new ModuleUsageStats
        {
            TotalSpent = logs.Sum(l => l.Cost),
            TotalUsages = logs.Count,
            SpentByModule = logs.GroupBy(l => l.Module.Name).ToDictionary(g => g.Key, g => g.Sum(l => l.Cost)),
            SpentByUsageType = logs.GroupBy(l => l.UsageType).ToDictionary(g => g.Key, g => g.Sum(l => l.Cost)),
            UsagesByModule = logs.GroupBy(l => l.Module.Name).ToDictionary(g => g.Key, g => g.Count()),
            UsagesByType = logs.GroupBy(l => l.UsageType).ToDictionary(g => g.Key, g => g.Count())
        };

        return stats;
    }

    public async Task<(bool Success, string Message)> LogUsageAndDeductCostAsync(
        Guid companyId,
        int moduleId,
        string usageType,
        decimal cost,
        string description,
        string userId,
        string? referenceId = null,
        string? channel = null)
    {
        return await DeductUsageCostAsync(companyId, moduleId, usageType, cost, description, userId, referenceId, channel);
    }

    /// <summary>
    /// Sistem işlemleri için kullanılacak varsayılan kullanıcı ID'si
    /// </summary>
    private const string SystemUserId = "785f0c69-61ae-4218-ae43-7ea7ce299e9b";

    /// <summary>
    /// UserId'yi doğrular ve geçersiz değerler için sistem kullanıcı ID'sini döndürür.
    /// Boş string, null veya "system" gibi yer tutucu değerler için varsayılan sistem kullanıcısı kullanılır.
    /// </summary>
    private static string GetValidUserId(string? userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return SystemUserId;

        // "system" gibi yer tutucu değerleri kontrol et
        var invalidPlaceholders = new[] { "system", "anonymous", "guest", "auto" };
        if (invalidPlaceholders.Contains(userId.ToLowerInvariant()))
            return SystemUserId;

        // GUID formatında olmalı (AspNetUsers ID formatı)
        if (!Guid.TryParse(userId, out _))
            return SystemUserId;

        return userId;
    }
}
