using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.ViewModels;
using System.Text.Json;
using PushDashboard.DTOs.Store;

namespace PushDashboard.Services;

public interface IStoreService
{
    Task<StoreIndexViewModel> GetStoreDataAsync(string userId);
    Task<List<StoreIndexViewModel.ModuleCategoryViewModel>> GetCategoriesAsync();
    Task<List<StoreIndexViewModel.ModuleViewModel>> GetModulesAsync(string userId, int? categoryId = null);
    Task<StoreIndexViewModel.ModuleDetailsViewModel?> GetModuleDetailsAsync(int moduleId, string userId);
    Task<(bool Success, string Message)> PurchaseModuleAsync(int moduleId, string userId, decimal paidAmount, string? transactionId = null);
    Task<(bool Success, string Message)> PurchaseModuleAsync(int moduleId, string userId, decimal paidAmount, SubscriptionType subscriptionType, bool autoRenewal, string? transactionId = null);
    Task<bool> IsModuleOwnedAsync(int moduleId, string userId);
    Task<List<StoreIndexViewModel.ModuleViewModel>> GetUserModulesAsync(string userId);
    Task<(bool Success, string Message)> AddCreditAsync(string userId, decimal amount, string? description = null);
    Task<decimal> GetUserCreditBalanceAsync(string userId);
    Task<StoreIndexViewModel.PagedTransactionHistoryViewModel> GetTransactionHistoryAsync(string userId, int page = 1, int pageSize = 10);
    Task<(Dictionary<string, object>? Settings, Module? ModuleInfo)> GetModuleSettingsAsync(int moduleId, string userId);
    Task<bool> SaveModuleSettingsAsync(int moduleId, string userId, Dictionary<string, object> settings);
    Task<bool> ResetModuleSettingsToDefaultAsync(int moduleId, string userId);
    Task<Module?> GetModuleInfoAsync(int moduleId);
    Task<(bool Success, string Message)> ToggleModuleStatusAsync(int moduleId, string userId);
    Task<List<ModuleStoreDto>> GetModulesWithPackagesAsync(CancellationToken cancellationToken = default);
    Task<(bool Success, string Message)> PurchaseModulePackageAsync(Guid companyId, int moduleId, int packageId, string subscriptionType = "monthly", string? userId = null, CancellationToken cancellationToken = default);
}

public class StoreService : IStoreService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StoreService> _logger;
    private readonly IBasketReminderService _basketReminderService;
    private readonly IUserContextService _userContextService;

    public StoreService(
        ApplicationDbContext context,
        ILogger<StoreService> logger,
        IBasketReminderService basketReminderService,
        IUserContextService userContextService)
    {
        _context = context;
        _logger = logger;
        _basketReminderService = basketReminderService;
        _userContextService = userContextService;
    }

    private async Task<Guid?> GetEffectiveCompanyIdAsync(string userId)
    {
        if (!string.IsNullOrEmpty(userId) &&
            string.Equals(_userContextService.UserId, userId, StringComparison.Ordinal))
        {
            return _userContextService.CompanyId ?? _userContextService.User?.CompanyId;
        }

        return await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.CompanyId)
            .FirstOrDefaultAsync();
    }

    private async Task<Company?> GetEffectiveCompanyAsync(string userId)
    {
        var companyId = await GetEffectiveCompanyIdAsync(userId);
        if (!companyId.HasValue)
        {
            return null;
        }

        return await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId.Value);
    }

    public async Task<StoreIndexViewModel> GetStoreDataAsync(string userId)
    {
        var categories = await GetCategoriesAsync();
        var modules = await GetModulesAsync(userId);
        var ownedModules = modules.Where(m => m.IsOwned).ToList();
        var availableModules = modules.Where(m => !m.IsOwned).ToList();

        var company = await GetEffectiveCompanyAsync(userId);
        var companyCreditBalance = company?.CreditBalance ?? 0.00m;

        var lastUsage = ownedModules.Any() ? ownedModules.Max(m => m.LastUsedAt) : null;

        // If LastUsage is null, check if ownedModules has any item
        if (lastUsage == null && ownedModules.Any())
        {
            // If LastUsedAt is not set, fallback to PurchasedAt
            lastUsage = ownedModules.Max(m => m.PurchasedAt);
        }

        var activeModules = ownedModules.Where(m => !m.IsPassive).ToList();
        var passiveModules = ownedModules.Where(m => m.IsPassive).ToList();

        var stats = new StoreIndexViewModel.StoreStatsViewModel
        {
            TotalCredits = companyCreditBalance,
            LastUsage = lastUsage,
            ActiveModules = activeModules.Count,
            PassiveModules = passiveModules.Count,
            ActiveModuleNames = activeModules.Select(m => m.Name).ToList()
        };

        return new StoreIndexViewModel
        {
            Categories = categories,
            Modules = modules,
            OwnedModules = ownedModules,
            AvailableModules = availableModules,
            Stats = stats,
            RecentTransactions = new List<StoreIndexViewModel.TransactionHistoryViewModel>()
        };
    }

    public async Task<List<ModuleStoreDto>> GetModulesWithPackagesAsync(CancellationToken cancellationToken = default)
    {
        var modules = await _context.Modules
            .Include(m => m.Category)
            .Where(m => m.IsActive && (m.HasQuotaSystem || m.Name == "Video Hosting") && (m.HasPackages || m.Name == "Video Hosting"))
            .Select(m => new ModuleStoreDto
            {
                ModuleId = m.Id,
                Name = m.Name,
                Description = m.Description,
                DetailedDescription = m.DetailedDescription,
                IconClass = m.IconClass,
                IconColor = m.IconColor,
                Packages = _context.ModulePackages
                    .Where(p => p.ModuleId == m.Id && p.IsActive)
                    .Select(p => new ModulePackageDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        MonthlyPrice = p.MonthlyPrice,
                        YearlyPrice = p.YearlyPrice,
                        YearlyDiscountPercent = p.MonthlyPrice > 0 ? (int)(100 - (p.YearlyPrice / (p.MonthlyPrice * 12)) * 100) : 0,
                        QuotaAmount = p.QuotaAmount,
                        HtmlContent = p.HtmlContent
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return modules;
    }

    public async Task<(bool Success, string Message)> PurchaseModulePackageAsync(Guid companyId, int moduleId, int packageId, string subscriptionType = "monthly", string? userId = null, CancellationToken cancellationToken = default)
    {
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);

        if (company == null)
            return (false, "Şirket bulunamadı.");

        var package = await _context.ModulePackages
            .FirstOrDefaultAsync(p => p.Id == packageId && p.ModuleId == moduleId && p.IsActive, cancellationToken);

        if (package == null)
            return (false, "Geçersiz veya pasif paket seçimi.");

        bool isYearly = subscriptionType.Equals("yearly", StringComparison.OrdinalIgnoreCase);
        decimal priceToPay = isYearly ? package.YearlyPrice : package.MonthlyPrice;
        SubscriptionType subTypeEnum = isYearly ? SubscriptionType.Yearly : SubscriptionType.Monthly;
        int daysToAdd = isYearly ? 365 : 30;

        if (company.CreditBalance < priceToPay)
            return (false, "Bu paketi satın almak için yeterli krediniz bulunmamaktadır.");

        var strategy = _context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                company.CreditBalance -= priceToPay;

                var quota = await _context.CompanyModuleQuotas
                    .FirstOrDefaultAsync(q => q.CompanyId == companyId && q.ModuleId == moduleId, cancellationToken);

                if (quota == null)
                {
                    quota = new CompanyModuleQuota
                    {
                        CompanyId = companyId,
                        ModuleId = moduleId,
                        ModulePackageId = package.Id,
                        TotalQuota = package.QuotaAmount,
                        RemainingQuota = package.QuotaAmount
                    };
                    _context.CompanyModuleQuotas.Add(quota);
                }
                else
                {
                    quota.ModulePackageId = package.Id;
                    quota.TotalQuota = package.QuotaAmount;
                    quota.RemainingQuota = package.QuotaAmount;
                    _context.CompanyModuleQuotas.Update(quota);
                }

                // Upsert CompanyModule — paket satın alındığında modül de sahiplenilmiş sayılmalı
                var companyModule = await _context.CompanyModules
                    .FirstOrDefaultAsync(cm => cm.CompanyId == companyId && cm.ModuleId == moduleId, cancellationToken);

                if (companyModule == null)
                {
                    companyModule = new CompanyModule
                    {
                        CompanyId = companyId,
                        ModuleId = moduleId,
                        IsOwned = true,
                        IsPassive = false,
                        LastUsedAt = DateTime.UtcNow,
                        PurchasedByUserId = userId,
                        SubscriptionType = subTypeEnum,
                        ExpiresAt = DateTime.UtcNow.AddDays(daysToAdd),
                        PaidAmount = priceToPay,
                        AutoRenewal = true,
                        RenewalCount = 0
                    };
                    _context.CompanyModules.Add(companyModule);
                }
                else
                {
                    // Update existing
                    companyModule.IsOwned = true;
                    companyModule.IsPassive = false;
                    companyModule.LastUsedAt = DateTime.UtcNow;
                    companyModule.SubscriptionType = subTypeEnum;

                    // Extend expiration if it already has one, otherwise set from today
                    if (companyModule.ExpiresAt.HasValue && companyModule.ExpiresAt.Value > DateTime.UtcNow)
                    {
                        companyModule.ExpiresAt = companyModule.ExpiresAt.Value.AddDays(daysToAdd);
                    }
                    else
                    {
                        companyModule.ExpiresAt = DateTime.UtcNow.AddDays(daysToAdd);
                    }

                    companyModule.PaidAmount = priceToPay;
                    companyModule.RenewalCount++;
                    companyModule.LastRenewalDate = DateTime.UtcNow;

                    _context.CompanyModules.Update(companyModule);
                }

                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation("Company {CompanyId} successfully purchased ModulePackage {PackageId} ({SubType}) for Module {ModuleId}. Deducted {Price} credits.",
                    companyId, packageId, subscriptionType, moduleId, priceToPay);

                return (true, "Paket başarıyla satın alındı ve kotanız güncellendi.");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogWarning(ex, "Concurrency exception occurred while company {CompanyId} was purchasing package {PackageId}.", companyId, packageId);
                return (false, "Aynı anda başka bir işlem yapıldığı için satın alma tamamlanamadı. Lütfen tekrar deneyin.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error occurred while company {CompanyId} was purchasing package {PackageId}.", companyId, packageId);
                return (false, "İşlem sırasında beklenmedik bir hata oluştu.");
            }
        });
    }

    public async Task<(bool Success, string Message)> PurchaseModuleAsync(int moduleId, string userId, decimal paidAmount, string? transactionId = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Check if module exists and is active
            var module = await _context.Modules
                .FirstOrDefaultAsync(m => m.Id == moduleId && m.IsActive);

            if (module == null)
            {
                _logger.LogWarning("Attempted to purchase non-existent or inactive module {ModuleId}", moduleId);
                return (false, "Modül bulunamadı veya aktif değil.");
            }

            // Get user and company first
            var companyId = await GetEffectiveCompanyIdAsync(userId);
            var company = await GetEffectiveCompanyAsync(userId);
            var user = new { CompanyId = companyId, Company = company };
            if (!companyId.HasValue)
            {
                return (false, "Kullanıcı bulunamadı.");
            }

            if (user.Company == null)
            {
                return (false, "Şirket bilgisi bulunamadı.");
            }

            // Check if company already owns the module
            var existingCompanyModule = await _context.CompanyModules
                .FirstOrDefaultAsync(cm => cm.ModuleId == moduleId && cm.CompanyId == companyId.Value);

            if (existingCompanyModule != null)
            {
                _logger.LogWarning("Company {CompanyId} already owns module {ModuleId}", companyId.Value, moduleId);
                return (false, "Şirketiniz bu modüle zaten sahip.");
            }

            // Check credit balance

            // Check if company has enough credit
            if (company.CreditBalance < module.Price)
            {
                _logger.LogWarning("Company {CompanyId} has insufficient credit balance. Required: {Required}, Available: {Available}",
                    company.Id,
                    module.Price,
                    company.CreditBalance);

                return (false, $"Yetersiz kredi bakiyesi. Gerekli: ₺{module.Price:N2}, Mevcut: ₺{user.Company.CreditBalance:N2}");
            }

            // Deduct credit from company balance
            company.CreditBalance -= module.Price;
            _context.Companies.Update(company);

            // Create company module record
            var companyModule = new CompanyModule
            {
                CompanyId = companyId.Value,
                ModuleId = moduleId,
                PurchasedAt = DateTime.Now,
                IsOwned = true,
                IsActive = true,
                PaidAmount = module.Price, // Use actual module price
                TransactionId = transactionId,
                PurchasedByUserId = userId
            };

            _context.CompanyModules.Add(companyModule);
            await _context.SaveChangesAsync();

            // Create default settings for the purchased module
            if (!string.IsNullOrEmpty(module.DefaultSettings))
            {
                var defaultSettings = new CompanyModuleSettings
                {
                    CompanyModuleId = companyModule.Id,
                    SettingsJson = module.DefaultSettings,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    UpdatedByUserId = userId
                };

                _context.CompanyModuleSettings.Add(defaultSettings);
                await _context.SaveChangesAsync();
            }

            // Sepet Hatırlatma modülü satın alındıysa BasketReminderSettings tablosunda varsayılan kayıt oluştur
            if (module.Name == "Sepet Hatırlatma")
            {
                try
                {
                    var (settingsSuccess, settingsMessage) = await _basketReminderService.CreateDefaultSettingsAsync(companyId.Value, userId);
                    if (settingsSuccess)
                    {
                        _logger.LogInformation("Created default BasketReminderSettings for company {CompanyId} after purchasing module {ModuleId}",
                            companyId.Value, moduleId);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to create default BasketReminderSettings for company {CompanyId}: {Message}",
                            companyId.Value, settingsMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating default BasketReminderSettings for company {CompanyId} after purchasing module {ModuleId}",
                        companyId.Value, moduleId);
                    // Ana transaction'ı etkilememek için hata fırlatmıyoruz
                }
            }

            await transaction.CommitAsync();

            _logger.LogInformation("User {UserId} successfully purchased module {ModuleId} for {Amount}. Remaining company balance: {Balance}",
                userId,
                moduleId,
                module.Price,
                company.CreditBalance);

            return (true, $"Modül başarıyla satın alındı! Kalan bakiye: ₺{user.Company.CreditBalance:N2}");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error purchasing module {ModuleId} for user {UserId}", moduleId, userId);
            return (false, "Satın alma işlemi sırasında bir hata oluştu.");
        }
    }

    /// <summary>
    /// Abonelik tipi ve otomatik yenileme seçenekleriyle modül satın alma
    /// </summary>
    public async Task<(bool Success, string Message)> PurchaseModuleAsync(
        int moduleId,
        string userId,
        decimal paidAmount,
        SubscriptionType subscriptionType,
        bool autoRenewal,
        string? transactionId = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Check if module exists and is active
            var module = await _context.Modules
                .FirstOrDefaultAsync(m => m.Id == moduleId && m.IsActive);

            if (module == null)
            {
                _logger.LogWarning("Attempted to purchase non-existent or inactive module {ModuleId}", moduleId);
                return (false, "Modül bulunamadı veya aktif değil.");
            }

            var user = new
            {
                CompanyId = await GetEffectiveCompanyIdAsync(userId),
                Company = await GetEffectiveCompanyAsync(userId)
            };
            if (user.CompanyId == null)
            {
                return (false, "Kullanıcı bulunamadı.");
            }

            if (user.Company == null)
            {
                return (false, "Şirket bilgisi bulunamadı.");
            }

            // Check if company already owns the module
            var existingCompanyModule = await _context.CompanyModules
                .FirstOrDefaultAsync(cm => cm.ModuleId == moduleId && cm.CompanyId == user.CompanyId);

            if (existingCompanyModule != null)
            {
                _logger.LogWarning("Company {CompanyId} already owns module {ModuleId}", user.CompanyId, moduleId);
                return (false, "Şirketiniz bu modüle zaten sahip.");
            }

            // Determine price based on subscription type
            var price = subscriptionType == SubscriptionType.Yearly
                ? (module.YearlyPrice > 0 ? module.YearlyPrice : module.Price * 12)
                : (module.MonthlyPrice > 0 ? module.MonthlyPrice : module.Price);

            // Check if company has enough credit
            if (user.Company.CreditBalance < price)
            {
                _logger.LogWarning("Company {CompanyId} has insufficient credit balance. Required: {Required}, Available: {Available}",
                    user.Company.Id,
                    price,
                    user.Company.CreditBalance);

                return (false, $"Yetersiz kredi bakiyesi. Gerekli: ₺{price:N2}, Mevcut: ₺{user.Company.CreditBalance:N2}");
            }

            // Deduct credit from company balance
            user.Company.CreditBalance -= price;
            _context.Companies.Update(user.Company);

            // Calculate expiration date
            var expiresAt = subscriptionType == SubscriptionType.Yearly
                ? DateTime.Now.AddYears(1)
                : DateTime.Now.AddDays(30);

            // Create company module record with subscription info
            var companyModule = new CompanyModule
            {
                CompanyId = user.CompanyId.Value,
                ModuleId = moduleId,
                PurchasedAt = DateTime.Now,
                ExpiresAt = expiresAt,
                IsOwned = true,
                IsActive = true,
                PaidAmount = price,
                TransactionId = transactionId,
                PurchasedByUserId = userId,
                SubscriptionType = subscriptionType,
                AutoRenewal = autoRenewal,
                LastRenewalDate = null,
                RenewalCount = 0
            };

            _context.CompanyModules.Add(companyModule);

            // Record purchase in ModuleUsageLog for spending history
            var usageLog = new ModuleUsageLog
            {
                CompanyId = user.CompanyId.Value,
                ModuleId = moduleId,
                UsageType = "Modül Satın Alma",
                Description = $"{module.Name} modülü satın alındı ({(subscriptionType == SubscriptionType.Yearly ? "Yıllık" : "Aylık")} abonelik)",
                Cost = price,
                BalanceBefore = user.Company.CreditBalance + price, // Before deduction
                BalanceAfter = user.Company.CreditBalance,
                UserId = userId,
                ReferenceId = transactionId,
                Channel = subscriptionType == SubscriptionType.Yearly ? "yearly" : "monthly",
                IsSuccessful = true,
                CreatedAt = DateTime.Now
            };
            _context.ModuleUsageLogs.Add(usageLog);

            await _context.SaveChangesAsync();

            // Create default settings for the purchased module
            if (!string.IsNullOrEmpty(module.DefaultSettings))
            {
                var defaultSettings = new CompanyModuleSettings
                {
                    CompanyModuleId = companyModule.Id,
                    SettingsJson = module.DefaultSettings,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    UpdatedByUserId = userId
                };

                _context.CompanyModuleSettings.Add(defaultSettings);
                await _context.SaveChangesAsync();
            }

            // Sepet Hatırlatma modülü satın alındıysa BasketReminderSettings tablosunda varsayılan kayıt oluştur
            if (module.Name == "Sepet Hatırlatma")
            {
                try
                {
                    var (settingsSuccess, settingsMessage) = await _basketReminderService.CreateDefaultSettingsAsync(user.CompanyId.Value, userId);
                    if (!settingsSuccess)
                    {
                        _logger.LogWarning("Failed to create default BasketReminderSettings for company {CompanyId}: {Message}",
                            user.CompanyId.Value, settingsMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating default BasketReminderSettings for company {CompanyId}",
                        user.CompanyId.Value);
                }
            }

            await transaction.CommitAsync();

            var subscriptionLabel = subscriptionType == SubscriptionType.Yearly ? "yıllık" : "aylık";
            _logger.LogInformation(
                "User {UserId} successfully purchased module {ModuleId} ({SubscriptionType}) for {Amount}. Expires: {ExpiresAt}. Remaining balance: {Balance}",
                userId, moduleId, subscriptionLabel, price, expiresAt, user.Company.CreditBalance);

            return (true, $"Modül başarıyla satın alındı! ({subscriptionLabel} abonelik, bitiş: {expiresAt:dd.MM.yyyy}) Kalan bakiye: ₺{user.Company.CreditBalance:N2}");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error purchasing module {ModuleId} for user {UserId}", moduleId, userId);
            return (false, "Satın alma işlemi sırasında bir hata oluştu.");
        }
    }

    public async Task<(bool Success, string Message)> AddCreditAsync(string userId, decimal amount, string? description = null)
    {
        try
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return (false, "Kullanıcı bulunamadı.");
            }

            if (amount <= 0)
            {
                return (false, "Kredi miktarı pozitif olmalıdır.");
            }

            user.CreditBalance += amount;
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Added {Amount} credit to user {UserId}. New balance: {Balance}. Description: {Description}",
                amount,
                userId,
                user.CreditBalance,
                description ?? "Manual credit addition");

            return (true, $"₺{amount:N2} kredi başarıyla eklendi. Yeni bakiye: ₺{user.CreditBalance:N2}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding credit to user {UserId}", userId);
            return (false, "Kredi ekleme işlemi sırasında bir hata oluştu.");
        }
    }

    public async Task<decimal> GetUserCreditBalanceAsync(string userId)
    {
        var company = await GetEffectiveCompanyAsync(userId);
        return company?.CreditBalance ?? 0.00m;
    }

    public async Task<List<StoreIndexViewModel.ModuleCategoryViewModel>> GetCategoriesAsync()
    {
        return await _context.ModuleCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new StoreIndexViewModel.ModuleCategoryViewModel
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                IconClass = c.IconClass,
                ModuleCount = c.Modules.Count(m => m.IsActive)
            })
            .ToListAsync();
    }

    public async Task<List<StoreIndexViewModel.ModuleViewModel>> GetModulesAsync(string userId, int? categoryId = null)
    {
        var query = _context.Modules
            .Include(m => m.Category)
            .Where(m => m.IsActive);

        if (categoryId.HasValue)
        {
            query = query.Where(m => m.CategoryId == categoryId.Value);
        }

        var modules = await query
            .OrderByDescending(m => m.IsFeatured)
            .ThenByDescending(m => m.IsNew)
            .ThenBy(m => m.Name)
            .ToListAsync();

        // Get user's company modules instead of user modules
        var user = new { CompanyId = await GetEffectiveCompanyIdAsync(userId) };
        var companyModules = new List<CompanyModule>();
        var companyQuotas = new List<CompanyModuleQuota>();

        if (user?.CompanyId != null)
        {
            // IsOwned true olan tüm modülleri getir (aktif veya pasif fark etmez, sahiplik için)
            companyModules = await _context.CompanyModules
                .Where(cm => cm.CompanyId == user.CompanyId && cm.IsOwned)
                .ToListAsync();

            // Quota bilgisi olan modülleri getir
            companyQuotas = await _context.CompanyModuleQuotas
                .Include(q => q.ModulePackage)
                .Where(q => q.CompanyId == user.CompanyId)
                .ToListAsync();
        }

        return modules.Select(m =>
        {
            var companyModule = companyModules.FirstOrDefault(cm => cm.ModuleId == m.Id);
            var quota = companyQuotas.FirstOrDefault(q => q.ModuleId == m.Id);

            var features = new List<string>();
            if (!string.IsNullOrEmpty(m.Features))
            {
                try
                {
                    features = JsonSerializer.Deserialize<List<string>>(m.Features) ?? new List<string>();
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize features for module {ModuleId}", m.Id);
                }
            }

            return new StoreIndexViewModel.ModuleViewModel
            {
                Id = m.Id,
                Name = m.Name,
                Description = m.Description,
                DetailedDescription = m.DetailedDescription,
                Price = m.Price,
                IconClass = m.IconClass,
                IconColor = m.IconColor,
                BackgroundColor = m.BackgroundColor,
                IsActive = m.IsActive,
                IsNew = m.IsNew,
                IsFeatured = m.IsFeatured,
                IsOwned = companyModule?.IsOwned ?? false,
                IsPassive = companyModule?.IsPassive ?? false,
                CategoryId = m.CategoryId,
                CategoryName = m.Category.Name,
                Features = features,
                PurchasedAt = companyModule?.PurchasedAt,
                LastUsedAt = companyModule?.LastUsedAt,
                MonthlyPrice = m.MonthlyPrice,
                YearlyPrice = m.YearlyPrice,
                ExpiresAt = companyModule?.ExpiresAt,
                SubscriptionType = companyModule?.SubscriptionType.ToString(),
                AutoRenewal = companyModule?.AutoRenewal ?? false,
                HasPackages = m.HasPackages,
                PackageName = quota?.ModulePackage?.Name,
                TotalQuota = quota?.TotalQuota,
                RemainingQuota = quota?.RemainingQuota,
                QuotaUnit = m.Name == "Video Hosting" ? "MB" : "adet"
            };
        }).ToList();
    }

    public async Task<StoreIndexViewModel.ModuleDetailsViewModel?> GetModuleDetailsAsync(int moduleId, string userId)
    {
        var module = await _context.Modules
            .Include(m => m.Category)
            .FirstOrDefaultAsync(m => m.Id == moduleId && m.IsActive);

        if (module == null)
            return null;

        // Get user's company
        var user = new { CompanyId = await GetEffectiveCompanyIdAsync(userId) };
        CompanyModule? companyModule = null;

        if (user?.CompanyId != null)
        {
            // Sahiplik kontrolü için IsOwned kullan
            companyModule = await _context.CompanyModules
                .FirstOrDefaultAsync(cm => cm.ModuleId == moduleId && cm.CompanyId == user.CompanyId && cm.IsOwned);
        }

        var features = new List<string>();
        if (!string.IsNullOrEmpty(module.Features))
        {
            try
            {
                features = JsonSerializer.Deserialize<List<string>>(module.Features) ?? new List<string>();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize features for module {ModuleId}", moduleId);
            }
        }

        var moduleViewModel = new StoreIndexViewModel.ModuleViewModel
        {
            Id = module.Id,
            Name = module.Name,
            Description = module.Description,
            DetailedDescription = module.DetailedDescription,
            Price = module.Price,
            IconClass = module.IconClass,
            IconColor = module.IconColor,
            BackgroundColor = module.BackgroundColor,
            IsActive = module.IsActive,
            IsNew = module.IsNew,
            IsFeatured = module.IsFeatured,
            IsOwned = companyModule?.IsOwned ?? false,
            IsPassive = companyModule?.IsPassive ?? false,
            CategoryName = module.Category.Name,
            Features = features,
            PurchasedAt = companyModule?.PurchasedAt,
            LastUsedAt = companyModule?.LastUsedAt
        };

        // Get related modules from the same category
        var relatedModules = await GetModulesAsync(userId, module.CategoryId);
        relatedModules = relatedModules.Where(m => m.Id != moduleId).Take(3).ToList();

        // Sahiplik kontrolü IsOwned'a göre yapılır
        var isOwned = companyModule?.IsOwned ?? false;

        moduleViewModel.HasPackages = module.HasPackages || module.Name == "Video Hosting";

        return new StoreIndexViewModel.ModuleDetailsViewModel
        {
            Module = moduleViewModel,
            CanPurchase = !isOwned,
            PurchaseBlockReason = isOwned ? "Şirketiniz bu modüle zaten sahip." : null,
            RelatedModules = relatedModules
        };
    }

    public async Task<bool> IsModuleOwnedAsync(int moduleId, string userId)
    {
        // Get user's company
        var user = new { CompanyId = await GetEffectiveCompanyIdAsync(userId) };
        if (user?.CompanyId == null)
            return false;

        // Check if company owns the module
        return await _context.CompanyModules
            .AnyAsync(cm => cm.ModuleId == moduleId && cm.CompanyId == user.CompanyId && cm.IsOwned);
    }


    public async Task<List<StoreIndexViewModel.ModuleViewModel>> GetUserModulesAsync(string userId)
    {
        // Get user's company
        var user = new { CompanyId = await GetEffectiveCompanyIdAsync(userId) };
        if (user?.CompanyId == null)
            return new List<StoreIndexViewModel.ModuleViewModel>();

        // Sahip olunan, aktif olan ve pasif olmayan modülleri getir (IsOwned && IsActive && !IsPassive)
        var companyModules = await _context.CompanyModules
            .Include(cm => cm.Module)
            .ThenInclude(m => m.Category)
            .Where(cm => cm.CompanyId == user.CompanyId && cm.IsOwned && cm.IsActive && !cm.IsPassive && cm.Module.IsActive)
            .ToListAsync();

        return companyModules.Select(cm =>
        {
            var features = new List<string>();
            if (!string.IsNullOrEmpty(cm.Module.Features))
            {
                try
                {
                    features = JsonSerializer.Deserialize<List<string>>(cm.Module.Features) ?? new List<string>();
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize features for module {ModuleId}", cm.ModuleId);
                }
            }

            return new StoreIndexViewModel.ModuleViewModel
            {
                Id = cm.Module.Id,
                Name = cm.Module.Name,
                Description = cm.Module.Description,
                DetailedDescription = cm.Module.DetailedDescription,
                Price = cm.Module.Price,
                IconClass = cm.Module.IconClass,
                IconColor = cm.Module.IconColor,
                BackgroundColor = cm.Module.BackgroundColor,
                IsActive = cm.Module.IsActive,
                IsNew = cm.Module.IsNew,
                IsFeatured = cm.Module.IsFeatured,
                IsOwned = cm.IsOwned,
                IsPassive = cm.IsPassive,
                CategoryName = cm.Module.Category.Name,
                Features = features,
                PurchasedAt = cm.PurchasedAt,
                LastUsedAt = cm.LastUsedAt
            };
        }).ToList();
    }

    public async Task<StoreIndexViewModel.PagedTransactionHistoryViewModel> GetTransactionHistoryAsync(string userId, int page = 1, int pageSize = 10)
    {
        // Get user's company
        var user = new { CompanyId = await GetEffectiveCompanyIdAsync(userId) };
        if (user?.CompanyId == null)
        {
            return new StoreIndexViewModel.PagedTransactionHistoryViewModel
            {
                Transactions = new List<StoreIndexViewModel.TransactionHistoryViewModel>(),
                CurrentPage = page,
                TotalPages = 0,
                TotalItems = 0,
                PageSize = pageSize
            };
        }

        var query = _context.CompanyModules
            .Include(cm => cm.Module)
            .ThenInclude(m => m.Category)
            .Where(cm => cm.CompanyId == user.CompanyId && cm.IsOwned)
            .OrderByDescending(cm => cm.PurchasedAt);

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        var transactions = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(cm => new StoreIndexViewModel.TransactionHistoryViewModel
            {
                Id = cm.Id,
                ModuleName = cm.Module.Name,
                PaidAmount = cm.PaidAmount,
                PurchasedAt = cm.PurchasedAt,
                TransactionId = cm.TransactionId,
                Status = "Tamamlandı",
                CategoryName = cm.Module.Category.Name,
                ModuleIcon = cm.Module.IconClass,
                ModuleIconColor = cm.Module.IconColor,
                ModuleBackgroundColor = cm.Module.BackgroundColor
            })
            .ToListAsync();

        return new StoreIndexViewModel.PagedTransactionHistoryViewModel
        {
            Transactions = transactions,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            PageSize = pageSize
        };
    }

    public async Task<(Dictionary<string, object>? Settings, Module? ModuleInfo)> GetModuleSettingsAsync(int moduleId, string userId)
    {
        try
        {
            // Get user's company
            var user = new { CompanyId = await GetEffectiveCompanyIdAsync(userId) };
            if (user?.CompanyId == null)
            {
                return (null, null);
            }

            // Sahiplik kontrolü için IsOwned kullan (ayarları görmek için sahip olmak yeterli)
            var companyModule = await _context.CompanyModules
                .Include(cm => cm.Module)
                .Include(cm => cm.Settings)
                .FirstOrDefaultAsync(cm => cm.ModuleId == moduleId && cm.CompanyId == user.CompanyId && cm.IsOwned);

            if (companyModule == null)
            {
                return (null, null);
            }

            var settings = new Dictionary<string, object>();

            if (companyModule.Settings != null && !string.IsNullOrEmpty(companyModule.Settings.SettingsJson))
            {
                try
                {
                    settings = JsonSerializer.Deserialize<Dictionary<string, object>>(companyModule.Settings.SettingsJson)
                               ?? new Dictionary<string, object>();
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to deserialize settings for company module {CompanyModuleId}", companyModule.Id);
                }
            }

            return (settings, companyModule.Module);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting module settings for module {ModuleId} and user {UserId}", moduleId, userId);
            return (null, null);
        }
    }

    public async Task<bool> SaveModuleSettingsAsync(int moduleId, string userId, Dictionary<string, object> settings)
    {
        try
        {
            // Get user's company
            var user = new { CompanyId = await GetEffectiveCompanyIdAsync(userId) };
            if (user?.CompanyId == null)
            {
                return false;
            }

            // Sahiplik kontrolü için IsOwned kullan (ayarları kaydetmek için sahip olmak yeterli)
            var companyModule = await _context.CompanyModules
                .Include(cm => cm.Settings)
                .Include(cm => cm.Module)
                .FirstOrDefaultAsync(cm => cm.ModuleId == moduleId && cm.CompanyId == user.CompanyId && cm.IsOwned);

            if (companyModule == null)
            {
                return false;
            }

            // Settings içinden isPassive değerini al
            var settingsData = settings.Values.FirstOrDefault();
            bool? isPassiveValue = null;

            if (settingsData is JsonElement jsonElement)
            {
                if (jsonElement.TryGetProperty("isPassive", out var isPassiveElement))
                {
                    isPassiveValue = isPassiveElement.GetBoolean();
                }
            }

            // isPassive değeri varsa CompanyModule.IsPassive'i güncelle
            if (isPassiveValue.HasValue)
            {
                companyModule.IsPassive = isPassiveValue.Value;
                companyModule.UpdatedAt = DateTime.Now;
            }

            // Mevcut SettingsJson'ı al ve isPassive ekle
            Dictionary<string, object>? existingSettings = null;
            if (companyModule.Settings?.SettingsJson != null)
            {
                try
                {
                    existingSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(companyModule.Settings.SettingsJson);
                }
                catch
                {
                    existingSettings = new Dictionary<string, object>();
                }
            }
            existingSettings ??= new Dictionary<string, object>();

            // isPassive değerini SettingsJson'a ekle
            if (isPassiveValue.HasValue)
            {
                existingSettings["isPassive"] = isPassiveValue.Value;
            }

            if (settingsData is JsonElement settingsElement && settingsElement.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in settingsElement.EnumerateObject())
                {
                    if (prop.Name != "isPassive")
                    {
                        existingSettings[prop.Name] = prop.Value;
                    }
                }
            }

            var settingsJson = JsonSerializer.Serialize(existingSettings);

            if (companyModule.Settings == null)
            {
                // Create new settings record
                companyModule.Settings = new CompanyModuleSettings
                {
                    CompanyModuleId = companyModule.Id,
                    SettingsJson = settingsJson,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    UpdatedByUserId = userId
                };
                _context.CompanyModuleSettings.Add(companyModule.Settings);
            }
            else
            {
                // Update existing settings
                companyModule.Settings.SettingsJson = settingsJson;
                companyModule.Settings.UpdatedAt = DateTime.Now;
                companyModule.Settings.UpdatedByUserId = userId;
                _context.CompanyModuleSettings.Update(companyModule.Settings);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated settings for company module {CompanyModuleId} (Module: {ModuleId}, Company: {CompanyId}, User: {UserId}, IsPassive: {IsPassive})",
                companyModule.Id, moduleId, user.CompanyId, userId, companyModule.IsPassive);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving module settings for module {ModuleId} and user {UserId}", moduleId, userId);
            return false;
        }
    }
    public async Task<bool> ResetModuleSettingsToDefaultAsync(int moduleId, string userId)
    {
        try
        {
            // Get user's company
            var user = new { CompanyId = await GetEffectiveCompanyIdAsync(userId) };
            if (user?.CompanyId == null)
            {
                return false;
            }

            // Sahiplik kontrolü için IsOwned kullan
            var companyModule = await _context.CompanyModules
                .Include(cm => cm.Module)
                .Include(cm => cm.Settings)
                .FirstOrDefaultAsync(cm => cm.ModuleId == moduleId && cm.CompanyId == user.CompanyId && cm.IsOwned);

            if (companyModule == null || companyModule.Module == null)
            {
                return false;
            }

            // Get default settings from module
            if (string.IsNullOrEmpty(companyModule.Module.DefaultSettings))
            {
                return false;
            }

            if (companyModule.Settings == null)
            {
                // Create new settings record with default values
                companyModule.Settings = new CompanyModuleSettings
                {
                    CompanyModuleId = companyModule.Id,
                    SettingsJson = companyModule.Module.DefaultSettings,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    UpdatedByUserId = userId
                };

                _context.CompanyModuleSettings.Add(companyModule.Settings);
            }
            else
            {
                // Reset existing settings to default
                companyModule.Settings.SettingsJson = companyModule.Module.DefaultSettings;
                companyModule.Settings.UpdatedAt = DateTime.Now;
                companyModule.Settings.UpdatedByUserId = userId;
                _context.CompanyModuleSettings.Update(companyModule.Settings);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Reset settings to default for company module {CompanyModuleId} (Module: {ModuleId}, Company: {CompanyId}, User: {UserId})",
                companyModule.Id,
                moduleId,
                user.CompanyId,
                userId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting module settings to default for module {ModuleId} and user {UserId}", moduleId, userId);
            return false;
        }
    }

    public async Task<Module?> GetModuleInfoAsync(int moduleId)
    {
        try
        {
            return await _context.Modules
                .FirstOrDefaultAsync(m => m.Id == moduleId && m.IsActive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting module info for module {ModuleId}", moduleId);
            return null;
        }
    }

    public async Task<(bool Success, string Message)> ToggleModuleStatusAsync(int moduleId, string userId)
    {
        try
        {
            // Get user's company
            var user = new { CompanyId = await GetEffectiveCompanyIdAsync(userId) };
            if (user?.CompanyId == null)
            {
                return (false, "Kullanıcı veya şirket bulunamadı.");
            }

            // Sahiplik kontrolü için IsOwned kullan
            var companyModule = await _context.CompanyModules
                .Include(cm => cm.Module)
                .FirstOrDefaultAsync(cm => cm.ModuleId == moduleId && cm.CompanyId == user.CompanyId && cm.IsOwned);

            if (companyModule == null)
            {
                return (false, "Modül bulunamadı veya size ait değil.");
            }

            // Toggle sadece IsPassive değiştirir - IsActive ve IsOwned sabit kalır
            companyModule.IsPassive = !companyModule.IsPassive;
            companyModule.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            var statusText = companyModule.IsPassive ? "pasif" : "aktif";
            _logger.LogInformation("Module {ModuleId} status changed to {Status} for company {CompanyId} by user {UserId}",
                moduleId, statusText, user.CompanyId, userId);

            return (true, $"Modül başarıyla {statusText} yapıldı.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling module status for module {ModuleId} and user {UserId}", moduleId, userId);
            return (false, "Modül durumu güncellenirken bir hata oluştu.");
        }
    }
}
