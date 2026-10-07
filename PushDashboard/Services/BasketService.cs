using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.ViewModels;
using PushDashboard.Services.Integrations;
using PushDashboard.Services.Integrations.Common.Models;
using PushDashboard.Exceptions;

namespace PushDashboard.Services;

public class BasketService : IBasketService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BasketService> _logger;
    private readonly IEcommerceServiceFactory _ecommerceServiceFactory;

    public BasketService(
        ApplicationDbContext context,
        ILogger<BasketService> logger,
        IEcommerceServiceFactory ecommerceServiceFactory)
    {
        _context = context;
        _logger = logger;
        _ecommerceServiceFactory = ecommerceServiceFactory;
    }

    public async Task<BasketIndexViewModel> GetBasketsAsync(Guid companyId, int page, int pageSize, BasketAdvancedFilter filter)
    {
        var query = _context.Baskets
            .Where(b => b.CompanyId == companyId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm;
            query = query.Where(b =>
                b.CustomerName.Contains(searchTerm) ||
                b.CustomerEmail.Contains(searchTerm) ||
                b.GuidBasketId.Contains(searchTerm));
        }

        // Müşteri adı filtresi
        if (!string.IsNullOrEmpty(filter.CustomerName))
        {
            query = query.Where(b => b.CustomerName.Contains(filter.CustomerName));
        }

        // E-posta filtresi
        if (!string.IsNullOrEmpty(filter.CustomerEmail))
        {
            query = query.Where(b => b.CustomerEmail.Contains(filter.CustomerEmail));
        }

        // Tarih aralığı filtresi
        if (filter.DateFrom.HasValue)
        {
            var dateFrom = DateTime.SpecifyKind(filter.DateFrom.Value.Date, DateTimeKind.Utc);
            query = query.Where(b => b.BasketDate >= dateFrom);
        }

        if (filter.DateTo.HasValue)
        {
            var dateTo = DateTime.SpecifyKind(filter.DateTo.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(b => b.BasketDate <= dateTo);
        }

        // Ürün sayısı filtresi
        if (filter.MinProductCount.HasValue)
        {
            query = query.Where(b => b.ProductCount >= filter.MinProductCount.Value);
        }

        if (filter.MaxProductCount.HasValue)
        {
            query = query.Where(b => b.ProductCount <= filter.MaxProductCount.Value);
        }

        // Tutar filtresi
        if (filter.MinAmount.HasValue)
        {
            query = query.Where(b => b.TotalAmount >= filter.MinAmount.Value);
        }

        if (filter.MaxAmount.HasValue)
        {
            query = query.Where(b => b.TotalAmount <= filter.MaxAmount.Value);
        }

        // Durum filtresi
        if (!string.IsNullOrEmpty(filter.Status))
        {
            switch (filter.Status.ToLower())
            {
                case "active":
                    query = query.Where(b => b.IsActive && !b.IsAbandoned);
                    break;
                case "abandoned":
                    query = query.Where(b => b.IsAbandoned);
                    break;
                case "inactive":
                    query = query.Where(b => !b.IsActive);
                    break;
                case "member":
                    query = query.Where(b => b.CustomerId > 0 && !string.IsNullOrEmpty(b.CustomerName) && !string.IsNullOrEmpty(b.CustomerEmail));
                    break;
                case "guest":
                    query = query.Where(b => b.CustomerId <= 0 || string.IsNullOrEmpty(b.CustomerName) || string.IsNullOrEmpty(b.CustomerEmail));
                    break;
            }
        }

        // Sıralama
        query = (filter.SortBy?.ToLower(), filter.SortDirection?.ToLower()) switch
        {
            ("customername", "desc") => query.OrderByDescending(b => b.CustomerName),
            ("customername", _) => query.OrderBy(b => b.CustomerName),
            ("basketdate", "desc") => query.OrderByDescending(b => b.BasketDate),
            ("basketdate", _) => query.OrderBy(b => b.BasketDate),
            ("totalamount", "desc") => query.OrderByDescending(b => b.TotalAmount),
            ("totalamount", _) => query.OrderBy(b => b.TotalAmount),
            ("productcount", "desc") => query.OrderByDescending(b => b.ProductCount),
            ("productcount", _) => query.OrderBy(b => b.ProductCount),
            ("email", "desc") => query.OrderByDescending(b => b.CustomerEmail),
            ("email", _) => query.OrderBy(b => b.CustomerEmail),
            _ => query.OrderByDescending(b => b.BasketDate)
        };

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var baskets = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BasketIndexViewModel.BasketViewModel
            {
                Id = b.Id,
                ExternalId = b.ExternalId,
                GuidBasketId = b.GuidBasketId,
                CustomerName = b.CustomerName,
                CustomerEmail = b.CustomerEmail,
                BasketDate = b.BasketDate,
                ProductCount = b.ProductCount,
                TotalAmount = b.TotalAmount,
                Currency = b.Currency,
                IsActive = b.IsActive,
                IsAbandoned = b.IsAbandoned,
                FormattedBasketDate = b.FormattedBasketDate,
                FormattedTotalAmount = b.FormattedTotalAmount,
                StatusText = b.StatusText,
                StatusBadgeClass = b.StatusBadgeClass,
                AbandonedStatusText = b.AbandonedStatusText,
                AbandonedStatusBadgeClass = b.AbandonedStatusBadgeClass,
                // Üyelik durumu properties
                HasMembership = b.HasMembership,
                MembershipStatusText = b.MembershipStatusText,
                MembershipStatusBadgeClass = b.MembershipStatusBadgeClass,
                DisplayCustomerName = b.DisplayCustomerName,
                DisplayCustomerEmail = b.DisplayCustomerEmail,
                ProfileIconClass = b.ProfileIconClass
            })
            .ToListAsync();

        // Para birimi sayısını kontrol et
        var currencyCount = await _context.Baskets
            .Where(b => b.CompanyId == companyId)
            .Select(b => b.Currency)
            .Distinct()
            .CountAsync();

        // Son senkronizasyon zamanını al
        var lastSync = await _context.SyncLogs
            .Where(sl => sl.CompanyId == companyId && sl.SyncType == "Basket" && sl.IsSuccessful)
            .OrderByDescending(sl => sl.SyncEndTime)
            .Select(sl => sl.SyncEndTime)
            .FirstOrDefaultAsync();

        // İstatistikler
        var oneDayAgo = DateTime.SpecifyKind(DateTime.Now.AddDays(-1), DateTimeKind.Utc);
        var stats = new BasketIndexViewModel.BasketStatsViewModel
        {
            TotalBaskets = await _context.Baskets.Where(b => b.CompanyId == companyId).CountAsync(),
            ActiveBaskets = await _context.Baskets.Where(b => b.CompanyId == companyId && b.IsActive && !b.IsAbandoned).CountAsync(),
            AbandonedBaskets = await _context.Baskets
                .Where(b => b.CompanyId == companyId && b.IsAbandoned).CountAsync(),
            // Birden fazla para birimi varsa toplam tutarı 0 yap (gizlenecek)
            TotalValue = currencyCount > 1 ? 0 : await _context.Baskets.Where(b => b.CompanyId == companyId && b.IsActive).SumAsync(b => b.TotalAmount),
            CurrencyTotals = await GetCurrencyTotalsAsync(companyId),
            LastSyncDate = lastSync
        };

        // Sayfalama bilgileri
        var pagination = new BasketIndexViewModel.PaginationViewModel
        {
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize,
            TotalCount = totalCount,
            HasPrevious = page > 1,
            HasNext = page < totalPages
        };

        // Son sync logları
        var recentSyncLogs = await GetRecentSyncLogsAsync(companyId, 5);

        return new BasketIndexViewModel
        {
            Baskets = baskets,
            Stats = stats,
            Pagination = pagination,
            RecentSyncLogs = recentSyncLogs
        };
    }

    public async Task<BasketIndexViewModel.BasketViewModel?> GetBasketDetailsAsync(string guidBasketId, Guid companyId)
    {
        // Timeout ayarı ile sorgu - Sadece sepet bilgilerini çek, ürünleri ayrı endpoint'ten al
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        var basket = await _context.Baskets
            .AsNoTracking() // Performance için tracking'i kapat
            .Where(b => b.ExternalId == guidBasketId && b.CompanyId == companyId)
            .Select(b => new BasketIndexViewModel.BasketViewModel
            {
                Id = b.Id,
                ExternalId = b.ExternalId,
                GuidBasketId = b.GuidBasketId,
                CustomerName = b.CustomerName,
                CustomerEmail = b.CustomerEmail,
                BasketDate = b.BasketDate,
                ProductCount = b.ProductCount,
                TotalAmount = b.TotalAmount,
                TotalTax = b.TotalTax,
                ShippingCost = b.ShippingCost,
                Currency = b.Currency,
                IsActive = b.IsActive,
                IsAbandoned = b.IsAbandoned,
                LastUpdateDate = b.LastUpdateDate,
                FormattedBasketDate = b.FormattedBasketDate,
                FormattedLastUpdateDate = b.FormattedLastUpdateDate,
                FormattedTotalAmount = b.FormattedTotalAmount,
                StatusText = b.StatusText,
                StatusBadgeClass = b.StatusBadgeClass,
                AbandonedStatusText = b.AbandonedStatusText,
                AbandonedStatusBadgeClass = b.AbandonedStatusBadgeClass,
                Items = new List<BasketIndexViewModel.BasketItemViewModel>() // Boş liste, ürünler ayrı endpoint'ten gelecek
            })
            .FirstOrDefaultAsync(cancellationTokenSource.Token);

        return basket;
    }

    // Alternatif metod: Sepet detaylarını ayrı ayrı çek (performance için)
    public async Task<BasketIndexViewModel.BasketViewModel?> GetBasketDetailsOptimizedAsync(string guidBasketId, Guid companyId)
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(5));

        // Önce sepet bilgilerini çek
        var basket = await _context.Baskets
            .AsNoTracking()
            .Where(b => b.ExternalId == guidBasketId && b.CompanyId == companyId)
            .Select(b => new BasketIndexViewModel.BasketViewModel
            {
                Id = b.Id,
                ExternalId = b.ExternalId,
                GuidBasketId = b.GuidBasketId,
                CustomerName = b.CustomerName,
                CustomerEmail = b.CustomerEmail,
                BasketDate = b.BasketDate,
                ProductCount = b.ProductCount,
                TotalAmount = b.TotalAmount,
                TotalTax = b.TotalTax,
                ShippingCost = b.ShippingCost,
                Currency = b.Currency,
                IsActive = b.IsActive,
                IsAbandoned = b.IsAbandoned,
                LastUpdateDate = b.LastUpdateDate,
                FormattedBasketDate = b.FormattedBasketDate,
                FormattedLastUpdateDate = b.FormattedLastUpdateDate,
                FormattedTotalAmount = b.FormattedTotalAmount,
                StatusText = b.StatusText,
                StatusBadgeClass = b.StatusBadgeClass,
                AbandonedStatusText = b.AbandonedStatusText,
                AbandonedStatusBadgeClass = b.AbandonedStatusBadgeClass,
                Items = new List<BasketIndexViewModel.BasketItemViewModel>()
            })
            .FirstOrDefaultAsync(cancellationTokenSource.Token);

        if (basket == null)
            return null;

        // Sonra sepet ürünlerini ayrı sorgu ile çek
        var basketItems = await _context.BasketItems
            .AsNoTracking()
            .Where(bi => bi.BasketId == basket.Id)
            .Select(item => new BasketIndexViewModel.BasketItemViewModel
            {
                Id = item.Id,
                ProductCode = item.ProductCode,
                ProductName = item.ProductName,
                ProductImage = item.ProductImage,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice,
                TaxRate = item.TaxRate,
                TaxAmount = item.TaxAmount,
                ShippingCost = item.ShippingCost,
                FreeShipping = item.FreeShipping,
                Currency = item.Currency,
                FormattedUnitPrice = item.FormattedUnitPrice,
                FormattedTotalPrice = item.FormattedTotalPrice,
                FormattedQuantity = item.FormattedQuantity
            })
            .ToListAsync(cancellationTokenSource.Token);

        basket.Items = basketItems;
        return basket;
    }

    public async Task<BasketIndexViewModel.BasketItemsPagedViewModel> GetBasketItemsPagedAsync(string guidBasketId, Guid companyId, int page = 1, int pageSize = 10)
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        // Önce sepet var mı kontrol et
        var basketExists = await _context.Baskets
            .AsNoTracking()
            .AnyAsync(b => b.ExternalId == guidBasketId && b.CompanyId == companyId, cancellationTokenSource.Token);

        if (!basketExists)
        {
            return new BasketIndexViewModel.BasketItemsPagedViewModel
            {
                Items = new List<BasketIndexViewModel.BasketItemViewModel>(),
                TotalItems = 0,
                CurrentPage = page,
                PageSize = pageSize
            };
        }

        // Sepet ID'sini al
        var basketId = await _context.Baskets
            .AsNoTracking()
            .Where(b => b.ExternalId == guidBasketId && b.CompanyId == companyId)
            .Select(b => b.Id)
            .FirstOrDefaultAsync(cancellationTokenSource.Token);

        // Toplam ürün sayısını al
        var totalItems = await _context.BasketItems
            .AsNoTracking()
            .CountAsync(bi => bi.BasketId == basketId, cancellationTokenSource.Token);

        // Sayfalama ile ürünleri al
        var basketItems = await _context.BasketItems
            .AsNoTracking()
            .Where(bi => bi.BasketId == basketId)
            .OrderBy(bi => bi.ProductName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new BasketIndexViewModel.BasketItemViewModel
            {
                Id = item.Id,
                ProductCode = item.ProductCode,
                ProductName = item.ProductName,
                ProductImage = item.ProductImage,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice,
                TaxRate = item.TaxRate,
                TaxAmount = item.TaxAmount,
                ShippingCost = item.ShippingCost,
                FreeShipping = item.FreeShipping,
                Currency = item.Currency,
                FormattedUnitPrice = item.FormattedUnitPrice,
                FormattedTotalPrice = item.FormattedTotalPrice,
                FormattedQuantity = item.FormattedQuantity
            })
            .ToListAsync(cancellationTokenSource.Token);

        return new BasketIndexViewModel.BasketItemsPagedViewModel
        {
            Items = basketItems,
            TotalItems = totalItems,
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    public async Task<List<BasketIndexViewModel.BasketSyncLogViewModel>> GetRecentSyncLogsAsync(Guid companyId, int count = 10)
    {
        return await _context.SyncLogs
            .Where(sl => sl.CompanyId == companyId && sl.SyncType == "Basket")
            .OrderByDescending(sl => sl.SyncStartTime)
            .Take(count)
            .Select(sl => new BasketIndexViewModel.BasketSyncLogViewModel
            {
                Id = sl.Id,
                SyncStartTime = sl.SyncStartTime,
                SyncEndTime = sl.SyncEndTime,
                TotalRecordsProcessed = sl.TotalRecordsProcessed,
                NewRecordsAdded = sl.NewRecordsAdded,
                RecordsUpdated = sl.RecordsUpdated,
                IsSuccessful = sl.IsSuccessful,
                ErrorMessage = sl.ErrorMessage,
                Notes = sl.Notes,
                FormattedSyncStartTime = sl.FormattedSyncStartTime,
                FormattedSyncEndTime = sl.FormattedSyncEndTime,
                StatusText = sl.StatusText,
                StatusBadgeClass = sl.StatusBadgeClass,
                Duration = sl.Duration
            })
            .ToListAsync();
    }

    public async Task<(bool Success, string Message, int SyncedCount)> SyncBasketsFromApiAsync(Guid companyId)
    {
        var syncStartTime = DateTime.Now;
        var syncLogId = 0;

        try
        {
            _logger.LogInformation("Starting basket sync for company {CompanyId}", companyId);

            var company = await _context.Companies.FindAsync(companyId);
            var abandonedTimeHours = company?.SelectedAbandonedTime ?? 24; // Varsayılan 24 saat

            // Sync log oluştur
            var syncLog = new SyncLog
            {
                CompanyId = companyId,
                SyncType = "Basket",
                SyncStartTime = syncStartTime,
                IsSuccessful = false,
                TotalRecordsProcessed = 0,
                NewRecordsAdded = 0,
                RecordsUpdated = 0
            };

            _context.SyncLogs.Add(syncLog);
            await _context.SaveChangesAsync();
            syncLogId = syncLog.Id;

            // Aktif entegrasyon servisini al
            var ecommerceService = await _ecommerceServiceFactory.GetEcommerceServiceAsync(companyId);
            if (ecommerceService == null)
            {
                var errorMessage = "Bu şirket için aktif e-ticaret entegrasyonu bulunamadı.";
                _logger.LogWarning(errorMessage + " CompanyId: {CompanyId}", companyId);

                // Sync log güncelle
                syncLog.IsSuccessful = false;
                syncLog.ErrorMessage = errorMessage;
                syncLog.SyncEndTime = DateTime.Now;
                await _context.SaveChangesAsync();

                return (false, errorMessage, 0);
            }

            // Önce tüm sepetleri sil
            _logger.LogInformation("Deleting all baskets for company {CompanyId}", companyId);

            int pageNumber = 1;
            int totalProcessed = 0;
            int newRecords = 0;
            
            var ecommerceAllCarts = new List<EcommerceCart>();
            while (true)
            {
                _logger.LogInformation("Fetching baskets page {PageNumber} with records", pageNumber);

                var filter = new EcommerceCartFilter();
                var pagination = new EcommercePagination
                {
                    PageNumber = pageNumber,
                };

                var ecommerceCarts = await ecommerceService.GetCartsAsync(companyId, filter, pagination);
                if (ecommerceCarts == null || ecommerceCarts.Length == 0)
                {
                    _logger.LogInformation("No more baskets found, stopping sync");
                    break;
                }
                
                ecommerceAllCarts.AddRange(ecommerceCarts);

                if (ecommerceCarts.Length < pagination.PageSize)
                {
                    _logger.LogInformation("No more pages available, stopping sync");
                    break;
                }

                pageNumber++;
                await Task.Delay(1000);
            }
            
            await _context.Baskets.Where(b => b.CompanyId == companyId).ExecuteDeleteAsync();
            
            foreach (var cart in ecommerceAllCarts)
            {
                // Direkt yeni sepet ekle (tüm sepetler önceden silindiği için)
                var newBasket = MapEcommerceCartToBasket(cart, companyId, abandonedTimeHours);
                if (newBasket.CustomerId == 0)
                    continue;
                    
                _context.Baskets.Add(newBasket);
                newRecords++;
                totalProcessed++;
            }
            
            await _context.SaveChangesAsync();

            syncLog.SyncEndTime = DateTime.Now;
            syncLog.TotalRecordsProcessed = totalProcessed;
            syncLog.NewRecordsAdded = newRecords;
            syncLog.RecordsUpdated = 0; // Artık güncelleme yok, sadece insert
            syncLog.IsSuccessful = true;
            syncLog.Notes = $"Başarıyla tamamlandı. Tüm sepetler yenilendi: {newRecords} sepet.";
            await _context.SaveChangesAsync();

            var message = $"{totalProcessed} sepet başarıyla senkronize edildi.";
            _logger.LogInformation(message + " CompanyId: {CompanyId}", companyId);

            return (true, message, totalProcessed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during basket sync for company {CompanyId}", companyId);

            // Sync log güncelle
            if (syncLogId > 0)
            {
                var syncLog = await _context.SyncLogs.FindAsync(syncLogId);
                if (syncLog != null)
                {
                    syncLog.IsSuccessful = false;
                    syncLog.SyncEndTime = DateTime.Now;
                    syncLog.ErrorMessage = ex.Message;
                    await _context.SaveChangesAsync();
                }
            }

            return (false, $"Senkronizasyon hatası: {ex.Message}", 0);
        }
    }

    public async Task SyncBasketsAsync(Guid companyId)
    {
        var result = await SyncBasketsFromApiAsync(companyId);
        
        if (!result.Success)
        {
            throw new InvalidOperationException(result.Message);
        }
    }


    public async Task<List<BasketIndexViewModel.CurrencyTotalViewModel>> GetCurrencyTotalsAsync(Guid companyId)
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        // Önce database'den raw data'yı çek
        var rawCurrencyData = await _context.Baskets
            .AsNoTracking()
            .Where(b => b.CompanyId == companyId)
            .GroupBy(b => b.Currency)
            .Select(g => new
            {
                Currency = g.Key ?? "TRY",
                TotalAmount = g.Sum(b => b.TotalAmount),
                BasketCount = g.Count()
            })
            .OrderByDescending(c => c.TotalAmount)
            .ToListAsync(cancellationTokenSource.Token);

        // Sonra client-side'da formatting yap
        var currencyTotals = rawCurrencyData.Select(data => new BasketIndexViewModel.CurrencyTotalViewModel
        {
            Currency = data.Currency,
            TotalAmount = data.TotalAmount,
            BasketCount = data.BasketCount,
            FormattedTotalAmount = FormatCurrency(data.TotalAmount, data.Currency),
            CurrencySymbol = GetCurrencySymbol(data.Currency)
        }).ToList();

        return currencyTotals;
    }

    public async Task<List<BasketIndexViewModel.BasketViewModel>> GetBasketsByCustomerAsync(int customerId, Guid companyId)
    {
        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));

        // Önce Customer'ın ExternalId'sini bulalım
        var customer = await _context.Customers
            .AsNoTracking()
            .Where(c => c.Id == customerId && c.CompanyId == companyId)
            .FirstOrDefaultAsync(cancellationTokenSource.Token);

        if (customer == null)
        {
            _logger.LogWarning("Customer not found: CustomerId={CustomerId}, CompanyId={CompanyId}", customerId, companyId);
            return new List<BasketIndexViewModel.BasketViewModel>();
        }

        _logger.LogInformation("Found customer: Id={CustomerId}, ExternalId={ExternalId}, Name={CustomerName}",
            customer.Id, customer.ExternalId, customer.FullName);

        // Basket'taki CustomerId aslında ExternalCustomerId, ama aynı zamanda CustomerName ve Email ile de arama yapalım
        var baskets = await _context.Baskets
            .AsNoTracking()
            .Where(b => b.CompanyId == companyId &&
                       (b.CustomerId == customer.ExternalId ||
                        (b.CustomerName == customer.FullName && b.CustomerEmail == customer.Email)))
            .OrderByDescending(b => b.BasketDate)
            .Select(b => new BasketIndexViewModel.BasketViewModel
            {
                Id = b.Id,
                ExternalId = b.ExternalId,
                GuidBasketId = b.GuidBasketId,
                CustomerName = b.CustomerName,
                CustomerEmail = b.CustomerEmail,
                BasketDate = b.BasketDate,
                ProductCount = b.ProductCount,
                TotalAmount = b.TotalAmount,
                Currency = b.Currency,
                IsActive = b.IsActive,
                IsAbandoned = b.IsAbandoned,
                FormattedBasketDate = b.FormattedBasketDate,
                FormattedTotalAmount = b.FormattedTotalAmount,
                StatusText = b.StatusText,
                StatusBadgeClass = b.StatusBadgeClass,
                AbandonedStatusText = b.AbandonedStatusText,
                AbandonedStatusBadgeClass = b.AbandonedStatusBadgeClass,
                // Üyelik durumu properties
                HasMembership = b.HasMembership,
                MembershipStatusText = b.MembershipStatusText,
                MembershipStatusBadgeClass = b.MembershipStatusBadgeClass,
                DisplayCustomerName = b.DisplayCustomerName,
                DisplayCustomerEmail = b.DisplayCustomerEmail,
                ProfileIconClass = b.ProfileIconClass
            })
            .ToListAsync(cancellationTokenSource.Token);

        _logger.LogInformation("Found {BasketCount} baskets for customer ExternalId={ExternalId}",
            baskets.Count, customer.ExternalId);

        return baskets;
    }

    private static string FormatCurrency(decimal amount, string currency)
    {
        return currency.ToUpper() switch
        {
            "TRY" => $"{amount:N2} ₺",
            "USD" => $"${amount:N2}",
            "EUR" => $"€{amount:N2}",
            "GBP" => $"£{amount:N2}",
            _ => $"{amount:N2} {currency}"
        };
    }

    private static string GetCurrencySymbol(string currency)
    {
        return currency.ToUpper() switch
        {
            "TRY" => "₺",
            "USD" => "$",
            "EUR" => "€",
            "GBP" => "£",
            _ => currency
        };
    }

    private Basket MapEcommerceCartToBasket(EcommerceCart ecommerceCart, Guid companyId, int abandonedTimeHours)
    {
        // Sepet tarihini hesapla
        var basketDate = ecommerceCart.CreatedAt;
        
        var cutoffTime = DateTime.Now.AddHours(-abandonedTimeHours);
        var isAbandoned = basketDate < cutoffTime;

        // Sepet oluştur
        var basket = new Basket
        {
            ExternalId = ecommerceCart.ExternalId,
            GuidBasketId = ecommerceCart.ExternalId, // Guid olarak kullan
            CustomerId = int.TryParse(ecommerceCart.CustomerId, out var customerId) ? customerId : 0,
            CustomerName = TruncateString(ecommerceCart.CustomerName, 200),
            CustomerEmail = TruncateString(ecommerceCart.CustomerEmail, 200),
            BasketDate = basketDate,
            LastUpdateDate = ecommerceCart.UpdatedAt ?? DateTime.Now,
            ProductCount = ecommerceCart.ItemCount,
            TotalAmount = ecommerceCart.TotalAmount,
            TotalTax = ecommerceCart.TotalTax,
            ShippingCost = ecommerceCart.ShippingCost,
            Currency = ecommerceCart.Currency ?? "TRY",
            IsActive = true, 
            IsAbandoned = isAbandoned, 
            CompanyId = companyId,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            LastSyncDate = DateTime.Now,
            BasketItems = new List<BasketItem>()
        };

        // BasketItems'ları ekle
        if (ecommerceCart.Items != null && ecommerceCart.Items.Any())
        {
            foreach (var ecommerceItem in ecommerceCart.Items)
            {
                var basketItem = MapEcommerceCartItemToBasketItem(ecommerceItem);
                basket.BasketItems.Add(basketItem);
            }
        }

        return basket;
    }

    private async Task UpdateBasketFromEcommerceCartAsync(Basket existingBasket, EcommerceCart ecommerceCart, Guid companyId, int abandonedTimeHours)
    {
        // Mevcut basket'ı database'den fresh olarak çek ve attach et
        var freshBasket = await _context.Baskets
            .Include(b => b.BasketItems)
            .FirstOrDefaultAsync(b => b.Id == existingBasket.Id && b.CompanyId == companyId);

        if (freshBasket == null)
        {
            _logger.LogWarning("Basket {BasketId} not found during update", existingBasket.Id);
            return;
        }

        // Basket bilgilerini güncelle
        freshBasket.CustomerId = int.TryParse(ecommerceCart.CustomerId, out var customerId) ? customerId : 0;
        freshBasket.CustomerName = TruncateString(ecommerceCart.CustomerName, 200);
        freshBasket.CustomerEmail = TruncateString(ecommerceCart.CustomerEmail, 200);
        freshBasket.LastUpdateDate = ecommerceCart.UpdatedAt ?? DateTime.Now;
        freshBasket.ProductCount = ecommerceCart.ItemCount;
        freshBasket.TotalAmount = ecommerceCart.TotalAmount;
        freshBasket.TotalTax = ecommerceCart.TotalTax;
        freshBasket.ShippingCost = ecommerceCart.ShippingCost;
        freshBasket.Currency = ecommerceCart.Currency ?? "TRY";
        freshBasket.IsAbandoned = freshBasket.BasketDate < DateTime.Now.AddHours(-abandonedTimeHours);
        freshBasket.UpdatedAt = DateTime.Now;
        freshBasket.LastSyncDate = DateTime.Now;

        // BasketItems'ları güncelle
        await UpdateBasketItemsAsync(freshBasket, ecommerceCart.Items);
    }

    private BasketItem MapEcommerceCartItemToBasketItem(EcommerceCartItem ecommerceItem)
    {
        return new BasketItem
        {
            ExternalId = int.TryParse(ecommerceItem.ExternalId, out var externalId) ? externalId : 0,
            ProductCode = TruncateString(ecommerceItem.ProductCode, 100),
            ProductName = TruncateString(ecommerceItem.ProductName, 200),
            ProductImage = TruncateString(ecommerceItem.ProductImage, 500),
            Quantity = (double)ecommerceItem.Quantity, // int to double conversion
            UnitPrice = ecommerceItem.UnitPrice,
            TotalPrice = ecommerceItem.TotalPrice,
            TaxRate = (double)ecommerceItem.TaxRate, // decimal to double conversion
            TaxAmount = ecommerceItem.TaxAmount,
            ShippingCost = ecommerceItem.ShippingCost,
            FreeShipping = ecommerceItem.FreeShipping,
            Currency = ecommerceItem.Currency ?? "TRY",
            CreatedAt = DateTime.Now
        };
    }

    private async Task UpdateBasketItemsAsync(Basket existingBasket, List<EcommerceCartItem>? ecommerceItems)
    {
        // Mevcut BasketItems'ları temizle
        if (existingBasket.BasketItems != null && existingBasket.BasketItems.Any())
        {
            _context.BasketItems.RemoveRange(existingBasket.BasketItems);
            existingBasket.BasketItems.Clear();
        }
        else
        {
            existingBasket.BasketItems = new List<BasketItem>();
        }

        // Yeni BasketItems'ları ekle
        if (ecommerceItems != null && ecommerceItems.Any())
        {
            foreach (var ecommerceItem in ecommerceItems)
            {
                var basketItem = MapEcommerceCartItemToBasketItem(ecommerceItem);
                basketItem.BasketId = existingBasket.Id;
                existingBasket.BasketItems.Add(basketItem);
            }
        }

        await Task.CompletedTask; // Async pattern için
    }

    private static string TruncateString(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }

    // Abandoned time management
    public async Task<int> GetAbandonedTimeAsync(Guid companyId)
    {
        var company = await _context.Companies.FindAsync(companyId);
        if (company == null)
        {
            throw new NotFoundException("Company", companyId);
        }

        return company.SelectedAbandonedTime;
    }

    public async Task UpdateAbandonedTimeAsync(Guid companyId, int hours)
    {
        // Validate hours
        var validHours = new[] { 1, 2, 4, 6, 12, 24 };
        if (!validHours.Contains(hours))
        {
            throw new ValidationException("Geçersiz saat değeri. Geçerli değerler: 1, 2, 4, 6, 12, 24");
        }

        var company = await _context.Companies.FindAsync(companyId);
        if (company == null)
        {
            throw new NotFoundException("Company", companyId);
        }

        company.SelectedAbandonedTime = hours;
        company.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        // Update basket abandoned statuses
        await UpdateBasketsAbandonedStatusAsync(companyId, hours);

        _logger.LogInformation("Abandoned time updated to {Hours} hours for company {CompanyId}", hours, companyId);
    }

    private async Task UpdateBasketsAbandonedStatusAsync(Guid companyId, int abandonedTimeHours)
    {
        var cutoffTime = DateTime.Now.AddHours(-abandonedTimeHours);

        // Get all active baskets and update IsAbandoned status
        var baskets = await _context.Baskets
            .Where(b => b.CompanyId == companyId && b.IsActive)
            .ToListAsync();

        foreach (var basket in baskets)
        {
            basket.IsAbandoned = basket.BasketDate < cutoffTime;
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Updated IsAbandoned status for {Count} baskets with {Hours}h threshold", baskets.Count, abandonedTimeHours);
    }
}