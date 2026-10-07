using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.ViewModels;
using PushDashboard.Services.Integrations;
using PushDashboard.Services.Integrations.Common.Models;
using PushDashboard.DTOs;

namespace PushDashboard.Services;

public class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CustomerService> _logger;
    private readonly IEcommerceServiceFactory _ecommerceServiceFactory;
    private readonly ICustomerImportService _customerImportService;
    private readonly IServiceProvider _serviceProvider;

    public CustomerService(
        ApplicationDbContext context,
        ILogger<CustomerService> logger,
        IEcommerceServiceFactory ecommerceServiceFactory,
        ICustomerImportService customerImportService,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _logger = logger;
        _ecommerceServiceFactory = ecommerceServiceFactory;
        _customerImportService = customerImportService;
        _serviceProvider = serviceProvider;
    }

    public async Task<CustomerIndexViewModel> GetCustomersAsync(Guid companyId, int page = 1, int pageSize = 50, string? searchTerm = null, string? status = null, string? source = null, string? dateFilter = null, string? sortBy = null, string? sortDirection = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _context.Customers
                .Where(c => c.CompanyId == companyId);

            // Arama filtresi - isim, soyisim, email, telefon, cep telefonu
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var searchLower = searchTerm.ToLower();
                query = query.Where(c =>
                    c.FirstName.ToLower().Contains(searchLower) ||
                    c.LastName.ToLower().Contains(searchLower) ||
                    c.Email.ToLower().Contains(searchLower) ||
                    (c.Phone != null && c.Phone.Contains(searchTerm)) ||
                    (c.MobilePhone != null && c.MobilePhone.Contains(searchTerm)));
            }

            // Durum filtresi
            if (!string.IsNullOrEmpty(status))
            {
                switch (status.ToLower())
                {
                    case "active":
                        query = query.Where(c => c.IsActive);
                        break;
                    case "inactive":
                        query = query.Where(c => !c.IsActive);
                        break;
                    case "new":
                        var oneMonthAgo = DateTime.Now.AddMonths(-1);
                        query = query.Where(c => c.MembershipDate >= oneMonthAgo);
                        break;
                }
            }

            // Tarih Filtresi (Yeni Müşteri)
            if (startDate.HasValue || endDate.HasValue)
            {
                if (startDate.HasValue)
                    query = query.Where(c => c.MembershipDate >= startDate.Value);
                if (endDate.HasValue)
                {
                    var end = endDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(c => c.MembershipDate <= end);
                }
            }
            else if (!string.IsNullOrEmpty(dateFilter))
            {
                var now = DateTime.Now;
                switch (dateFilter.ToLower())
                {
                    case "1day":
                        query = query.Where(c => c.MembershipDate >= now.AddDays(-1));
                        break;
                    case "1week":
                        query = query.Where(c => c.MembershipDate >= now.AddDays(-7));
                        break;
                    case "1month":
                        query = query.Where(c => c.MembershipDate >= now.AddMonths(-1));
                        break;
                }
            }

            if (!string.IsNullOrEmpty(source))
            {
                if (Enum.TryParse<CustomerSource>(source, true, out var customerSource))
                {
                    query = query.Where(c => c.Source == customerSource);
                }
            }

            var totalRecords = await query.CountAsync();
            var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            // Sıralama uygula
            query = ApplySorting(query, sortBy, sortDirection);

            var customers = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var customerViewModels = customers.Select(CustomerIndexViewModel.CustomerViewModel.FromCustomer).ToList();

            // İstatistikleri al
            var stats = await GetCustomerStatsAsync(companyId);

            // Son senkronizasyon loglarını al
            var syncLogs = await GetSyncLogsAsync(companyId, 5);

            return new CustomerIndexViewModel
            {
                Customers = customerViewModels,
                Stats = new CustomerIndexViewModel.CustomerStatsViewModel
                {
                    TotalCustomers = stats.TotalCustomers,
                    ActiveCustomers = stats.ActiveCustomers,
                    InactiveCustomers = stats.TotalCustomers - stats.ActiveCustomers,
                    NewCustomersThisMonth = stats.NewCustomers,
                    SmsPermittedCustomers = stats.SmsPermittedCustomers,
                    LastSyncDate = stats.LastSync
                },
                RecentSyncLogs = syncLogs.Select(CustomerIndexViewModel.CustomerSyncLogViewModel.FromSyncLog).ToList(),
                Pagination = new CustomerIndexViewModel.PaginationViewModel
                {
                    CurrentPage = page,
                    TotalPages = totalPages,
                    TotalRecords = totalRecords,
                    PageSize = pageSize
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customers for company {CompanyId}", companyId);
            return new CustomerIndexViewModel();
        }
    }

    public async Task SyncCustomersAsync(Guid companyId)
    {
        var syncStartTime = DateTime.Now;
        SyncLog? syncLog = null;

        try
        {
            _logger.LogInformation("Starting customer sync for company {CompanyId}", companyId);

            // SyncLog kaydı oluştur
            syncLog = new SyncLog
            {
                CompanyId = companyId,
                SyncType = "Customer",
                SyncStartTime = syncStartTime,
                IsSuccessful = false
            };
            _context.SyncLogs.Add(syncLog);
            await _context.SaveChangesAsync();

            // Aktif entegrasyon servisini al
            var ecommerceService = await _ecommerceServiceFactory.GetEcommerceServiceAsync(companyId);
            if (ecommerceService == null)
            {
                var errorMessage = "Bu şirket için aktif e-ticaret entegrasyonu bulunamadı.";
                _logger.LogWarning(errorMessage + " CompanyId: {CompanyId}", companyId);

                // SyncLog'u güncelle
                syncLog.SyncEndTime = DateTime.Now;
                syncLog.IsSuccessful = false;
                await _context.SaveChangesAsync();

                throw new InvalidOperationException(errorMessage);
            }

            // Entegrasyon ayarlarını al
            var settings = await _ecommerceServiceFactory.GetActiveIntegrationSettingsAsync(companyId);
            if (settings == null)
            {
                var errorMessage = "Entegrasyon ayarları bulunamadı.";
                _logger.LogWarning(errorMessage + " CompanyId: {CompanyId}", companyId);

                // SyncLog'u güncelle
                syncLog.SyncEndTime = DateTime.Now;
                syncLog.IsSuccessful = false;
                await _context.SaveChangesAsync();
                
                throw new InvalidOperationException(errorMessage);
            }

            // Entegrasyon tipini logla
            var integrationType = await _ecommerceServiceFactory.GetActiveIntegrationTypeAsync(companyId);
            _logger.LogInformation("Using {IntegrationType} integration for customer sync. CompanyId: {CompanyId}",
                integrationType, companyId);

            // CompanyIntegration'ı al (IsNew kontrolü için)
            var companyIntegration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                          ci.Integration.Type == "ecommerce" &&
                                          ci.IsActive);

            var isNewIntegration = companyIntegration?.IsNew ?? false;
            if (isNewIntegration)
            {
                _logger.LogInformation("IsNew flag is true for company {CompanyId}. Old customers will be deleted after successful sync.", companyId);
            }

            // İlgili entegrasyon servisini kullanarak senkronizasyonu gerçekleştir
            var filter = new EcommerceCustomerFilter
            {
                IsActive = null, // Tüm müşterileri getir
                EmailPermission = null,
                SmsPermission = null
            };
            var pagination = new EcommercePagination();

            var ecommerceCustomers = await ecommerceService.GetCustomersAsync(companyId, filter, pagination);
            if (ecommerceCustomers == null || ecommerceCustomers.Length == 0)
            {
                // SyncLog'u güncelle
                syncLog.SyncEndTime = DateTime.Now;
                syncLog.IsSuccessful = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("No customers found to sync for company {CompanyId}", companyId);
                return;
            }

            // Müşterileri ExternalId'ye göre distinct yap (aynı müşteri birden fazla sayfada gelebilir)
            var distinctCustomers = ecommerceCustomers
                .GroupBy(c => c.ExternalId)
                .Select(g => g.First())
                .ToList();

            _logger.LogInformation("Customer sync: {TotalCount} customers fetched, {DistinctCount} unique customers after deduplication",
                ecommerceCustomers.Length, distinctCustomers.Count);

            // Eğer IsNew true ise, önce mevcut müşterileri sil
            var deletedCount = 0;
            if (isNewIntegration)
            {
                var existingCustomers = await _context.Customers
                    .Where(c => c.CompanyId == companyId)
                    .ToListAsync();

                deletedCount = existingCustomers.Count;
                if (deletedCount > 0)
                {
                    _context.Customers.RemoveRange(existingCustomers);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Deleted {Count} existing customers for company {CompanyId} due to IsNew flag",
                        deletedCount, companyId);
                }
            }

            // Müşterileri veritabanına kaydet
            var newRecordsAdded = 0;
            var recordsUpdated = 0;
            var processedExternalIds = new HashSet<int>(); // Loop içinde eklenen müşterileri takip et

            foreach (var ecommerceCustomer in distinctCustomers)
            {
                var externalId = int.Parse(ecommerceCustomer.ExternalId);

                // Bu ExternalId zaten bu loop'ta işlendi mi?
                if (processedExternalIds.Contains(externalId))
                {
                    continue;
                }

                // Mevcut müşteriyi kontrol et (IsNew durumunda zaten silinmiş olacak)
                var existingCustomer = await _context.Customers
                    .FirstOrDefaultAsync(c => c.ExternalId == externalId && c.CompanyId == companyId);

                if (existingCustomer == null)
                {
                    // Yeni müşteri oluştur
                    var newCustomer = MapEcommerceCustomerToCustomer(ecommerceCustomer, companyId);
                    _context.Customers.Add(newCustomer);
                    newRecordsAdded++;
                    processedExternalIds.Add(externalId);
                }
                else
                {
                    // Mevcut müşteriyi güncelle
                    UpdateCustomerFromEcommerceCustomer(existingCustomer, ecommerceCustomer);
                    recordsUpdated++;
                    processedExternalIds.Add(externalId);
                }
            }

            await _context.SaveChangesAsync();

            // IsNew flag'ini false yap
            if (isNewIntegration && companyIntegration != null)
            {
                companyIntegration.IsNew = false;
                await _context.SaveChangesAsync();
                _logger.LogInformation("IsNew flag set to false for company {CompanyId} after successful sync", companyId);
            }

            // SyncLog'u güncelle
            syncLog.SyncEndTime = DateTime.Now;
            syncLog.IsSuccessful = true;
            syncLog.NewRecordsAdded = newRecordsAdded;
            syncLog.RecordsUpdated = recordsUpdated;
            await _context.SaveChangesAsync();

            var successMessage = isNewIntegration
                ? $"{distinctCustomers.Count} müşteri başarıyla senkronize edildi (Silinen: {deletedCount}, Yeni: {newRecordsAdded})"
                : $"{distinctCustomers.Count} müşteri başarıyla senkronize edildi (Yeni: {newRecordsAdded}, Güncellenen: {recordsUpdated})";
            
            _logger.LogInformation(successMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during customer sync for company {CompanyId}", companyId);

            // SyncLog'u güncelle
            if (syncLog != null)
            {
                syncLog.SyncEndTime = DateTime.Now;
                syncLog.IsSuccessful = false;
                await _context.SaveChangesAsync();
            }

            throw new InvalidOperationException($"Senkronizasyon sırasında hata oluştu: {ex.Message}", ex);
        }
    }

    public async Task<Customer?> GetCustomerByIdAsync(int id, Guid companyId)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == companyId);
    }

    public async Task<Customer?> GetCustomerByExternalIdAsync(Guid companyId, int externalId)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.ExternalId == externalId && c.CompanyId == companyId);
    }

    public async Task<List<SyncLog>> GetSyncLogsAsync(Guid companyId, int limit = 10)
    {
        return await _context.SyncLogs
            .Where(sl => sl.CompanyId == companyId && sl.SyncType == "Customer")
            .OrderByDescending(sl => sl.SyncStartTime)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<(int TotalCustomers, int ActiveCustomers, int NewCustomers, int SmsPermittedCustomers, DateTime? LastSync)> GetCustomerStatsAsync(Guid companyId)
    {
        var totalCustomers = await _context.Customers.CountAsync(c => c.CompanyId == companyId);
        var activeCustomers = await _context.Customers.CountAsync(c => c.CompanyId == companyId && c.IsActive);

        var oneMonthAgo = DateTime.Now.AddMonths(-1);
        var newCustomers = await _context.Customers.CountAsync(c => c.CompanyId == companyId && c.MembershipDate >= oneMonthAgo);

        var smsPermittedCustomers = await _context.Customers.CountAsync(c => c.CompanyId == companyId && c.SmsPermission);

        var lastSync = await _context.SyncLogs
            .Where(sl => sl.CompanyId == companyId && sl.SyncType == "Customer" && sl.IsSuccessful)
            .OrderByDescending(sl => sl.SyncEndTime)
            .Select(sl => sl.SyncEndTime)
            .FirstOrDefaultAsync();

        return (totalCustomers, activeCustomers, newCustomers, smsPermittedCustomers, lastSync);
    }

    private IQueryable<Customer> ApplySorting(IQueryable<Customer> query, string? sortBy, string? sortDirection)
    {
        var isDescending = sortDirection?.ToLower() == "desc";

        return sortBy?.ToLower() switch
        {
            "name" => isDescending
                ? query.OrderByDescending(c => c.FirstName).ThenByDescending(c => c.LastName)
                : query.OrderBy(c => c.FirstName).ThenBy(c => c.LastName),
            "email" => isDescending
                ? query.OrderByDescending(c => c.Email)
                : query.OrderBy(c => c.Email),
            "city" => isDescending
                ? query.OrderByDescending(c => c.City)
                : query.OrderBy(c => c.City),
            "membershipdate" => isDescending
                ? query.OrderByDescending(c => c.MembershipDate)
                : query.OrderBy(c => c.MembershipDate),
            "lastlogindate" => isDescending
                ? query.OrderByDescending(c => c.LastLoginDate)
                : query.OrderBy(c => c.LastLoginDate),
            "status" => isDescending
                ? query.OrderByDescending(c => c.IsActive)
                : query.OrderBy(c => c.IsActive),
            _ => isDescending
                ? query.OrderByDescending(c => c.UpdatedAt)
                : query.OrderBy(c => c.UpdatedAt)
        };
    }

    private Customer MapEcommerceCustomerToCustomer(EcommerceCustomer ecommerceCustomer, Guid companyId)
    {
        // CustomFields'dan ek bilgileri çek
        string? city = null;
        string? district = null;
        string? customerCode = null;
        DateTime? lastLoginDate = null;
        string? lastLoginIp = null;

        if (ecommerceCustomer.CustomFields != null)
        {
            if (ecommerceCustomer.CustomFields.TryGetValue("city", out var cityValue))
            {
                city = cityValue?.ToString();
            }

            if (ecommerceCustomer.CustomFields.TryGetValue("district", out var districtValue))
            {
                district = districtValue?.ToString();
            }

            if (ecommerceCustomer.CustomFields.TryGetValue("ticimaxCode", out var codeValue))
            {
                customerCode = codeValue?.ToString();
            }

            if (ecommerceCustomer.CustomFields.TryGetValue("lastLoginDate", out var lastLoginValue))
            {
                if (lastLoginValue is DateTime loginDate)
                {
                    lastLoginDate = loginDate;
                }
            }

            if (ecommerceCustomer.CustomFields.TryGetValue("lastLoginIp", out var lastLoginIpValue))
            {
                lastLoginIp = lastLoginIpValue?.ToString();
            }
        }

        return new Customer
        {
            ExternalId = int.TryParse(ecommerceCustomer.ExternalId, out var id) ? id : 0,
            Email = TruncateString(ecommerceCustomer.Email, 200),
            FirstName = TruncateString(ecommerceCustomer.FirstName, 100),
            LastName = TruncateString(ecommerceCustomer.LastName, 100),
            MobilePhone = TruncateString(ecommerceCustomer.Phone, 20),
            BirthDate = ecommerceCustomer.BirthDate,
            GenderId = ecommerceCustomer.Gender switch
            {
                "Male" => 1,
                "Female" => 2,
                _ => 0
            },
            City = TruncateString(city, 100),
            District = TruncateString(district, 100),
            CustomerCode = TruncateString(customerCode, 100),
            LastLoginDate = lastLoginDate,
            LastLoginIp = TruncateString(lastLoginIp, 50),
            IsActive = ecommerceCustomer.IsActive,
            EmailPermission = ecommerceCustomer.EmailPermission,
            SmsPermission = ecommerceCustomer.SmsPermission,
            MembershipDate = ecommerceCustomer.CreatedAt,
            CompanyId = companyId,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            LastSyncDate = DateTime.Now
        };
    }

    private void UpdateCustomerFromEcommerceCustomer(Customer existingCustomer, EcommerceCustomer ecommerceCustomer)
    {
        // CustomFields'dan ek bilgileri çek
        string? city = null;
        string? district = null;
        string? customerCode = null;
        DateTime? lastLoginDate = null;
        string? lastLoginIp = null;

        if (ecommerceCustomer.CustomFields != null)
        {
            if (ecommerceCustomer.CustomFields.TryGetValue("city", out var cityValue))
            {
                city = cityValue?.ToString();
            }

            if (ecommerceCustomer.CustomFields.TryGetValue("district", out var districtValue))
            {
                district = districtValue?.ToString();
            }

            if (ecommerceCustomer.CustomFields.TryGetValue("ticimaxCode", out var codeValue))
            {
                customerCode = codeValue?.ToString();
            }

            if (ecommerceCustomer.CustomFields.TryGetValue("lastLoginDate", out var lastLoginValue))
            {
                if (lastLoginValue is DateTime loginDate)
                {
                    lastLoginDate = loginDate;
                }
            }

            if (ecommerceCustomer.CustomFields.TryGetValue("lastLoginIp", out var lastLoginIpValue))
            {
                lastLoginIp = lastLoginIpValue?.ToString();
            }
        }

        existingCustomer.Email = TruncateString(ecommerceCustomer.Email, 200);
        existingCustomer.FirstName = TruncateString(ecommerceCustomer.FirstName, 100);
        existingCustomer.LastName = TruncateString(ecommerceCustomer.LastName, 100);
        existingCustomer.MobilePhone = TruncateString(ecommerceCustomer.Phone, 20);
        existingCustomer.BirthDate = ecommerceCustomer.BirthDate;
        existingCustomer.GenderId = ecommerceCustomer.Gender switch
        {
            "Male" => 1,
            "Female" => 2,
            _ => 0
        };
        existingCustomer.City = TruncateString(city, 100);
        existingCustomer.District = TruncateString(district, 100);
        existingCustomer.CustomerCode = TruncateString(customerCode, 100);
        existingCustomer.LastLoginDate = lastLoginDate;
        existingCustomer.LastLoginIp = TruncateString(lastLoginIp, 50);
        existingCustomer.IsActive = ecommerceCustomer.IsActive;
        existingCustomer.EmailPermission = ecommerceCustomer.EmailPermission;
        existingCustomer.SmsPermission = ecommerceCustomer.SmsPermission;
        existingCustomer.UpdatedAt = DateTime.Now;
        existingCustomer.LastSyncDate = DateTime.Now;
    }

    private static string TruncateString(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }

    // Yeni metodlar
    public async Task<int> CreateCustomerAsync(Guid companyId, CreateCustomerViewModel model)
    {
        return await _customerImportService.CreateCustomerAsync(companyId, model);
    }

    public async Task<int> StartBulkImportAsync(Guid companyId, string userId, IFormFile file)
    {
        // Import job'ı oluştur
        var jobId = await _customerImportService.CreateImportJobAsync(companyId, userId, file);

        // Task.Run ile arka planda çalıştır
        _ = Task.Run(async () =>
        {
            try
            {
                // Scoped service oluştur
                using var scope = _serviceProvider.CreateScope();
                var scopedImportService = scope.ServiceProvider.GetRequiredService<ICustomerImportService>();

                await scopedImportService.ProcessImportJobAsync(jobId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing import job {JobId}", jobId);
            }
        });

        return jobId;
    }

    public async Task<SyncResultDto> SyncCustomersAndBasketsAsync(Guid companyId)
    {
        var syncStartTime = DateTime.Now;

        // Önce müşterileri senkronize et
        await SyncCustomersAsync(companyId);

        // Sonra sepetleri senkronize et
        var basketService = _serviceProvider.GetRequiredService<IBasketService>();
        await basketService.SyncBasketsAsync(companyId);

        // Senkronizasyon süresini hesapla
        var syncDuration = DateTime.Now - syncStartTime;

        // SyncStatsJson'ı güncelle
        var companyIntegration = await _context.CompanyIntegrations
            .Include(ci => ci.Integration)
            .FirstOrDefaultAsync(ci => ci.CompanyId == companyId && ci.Integration.Type == "ecommerce" && ci.IsActive);

        if (companyIntegration != null)
        {
            var syncedCustomers = await _context.Customers.CountAsync(c => c.CompanyId == companyId);
            var syncedCarts = await _context.Baskets.CountAsync(b => b.CompanyId == companyId);

            var syncStats = new Dictionary<string, object>
            {
                ["syncedCustomers"] = syncedCustomers,
                ["syncedCarts"] = syncedCarts,
                ["lastSyncDuration"] = $"{syncDuration.TotalSeconds:F1}s",
                ["lastSyncAt"] = DateTime.Now.ToString("o")
            };

            companyIntegration.SyncStatsJson = System.Text.Json.JsonSerializer.Serialize(syncStats);
            companyIntegration.LastSyncAt = DateTime.Now;
            await _context.SaveChangesAsync();
        }

        var successMessage = $"✅ Müşteriler ve sepetler başarıyla senkronize edildi.";

        return new SyncResultDto
        {
            Message = successMessage,
            SyncedCustomers = await _context.Customers.CountAsync(c => c.CompanyId == companyId),
            SyncedCarts = await _context.Baskets.CountAsync(b => b.CompanyId == companyId),
            SyncDuration = $"{syncDuration.TotalSeconds:F1}s"
        };
    }


    public async Task<CustomerImportProgressViewModel?> GetImportProgressAsync(int jobId, Guid companyId)
    {
        return await _customerImportService.GetImportProgressAsync(jobId, companyId);
    }

    public async Task<List<CustomerImportJobViewModel>> GetImportHistoryAsync(Guid companyId, int limit = 10)
    {
        return await _customerImportService.GetImportHistoryAsync(companyId, limit);
    }

    public byte[] GenerateExcelTemplate()
    {
        return _customerImportService.GenerateExcelTemplate();
    }
}
