using System.Globalization;

namespace PushDashboard.ViewModels;

public class BasketIndexViewModel
{
    public List<BasketViewModel> Baskets { get; set; } = new();
    public BasketStatsViewModel Stats { get; set; } = new();
    public PaginationViewModel Pagination { get; set; } = new();
    public List<BasketSyncLogViewModel> RecentSyncLogs { get; set; } = new();

    public class BasketViewModel
    {
        public Guid Id { get; set; }
        public string ExternalId { get; set; } = string.Empty;
        public string? GuidBasketId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime BasketDate { get; set; }
        public int ProductCount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalTax { get; set; }
        public decimal ShippingCost { get; set; }
        public string Currency { get; set; } = "TRY";
        public bool IsActive { get; set; }
        public bool IsAbandoned { get; set; }
        public DateTime? LastUpdateDate { get; set; }

        // Formatted properties
        public string FormattedBasketDate { get; set; } = string.Empty;
        public string FormattedLastUpdateDate { get; set; } = string.Empty;
        public string FormattedTotalAmount { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public string StatusBadgeClass { get; set; } = string.Empty;
        public string AbandonedStatusText { get; set; } = string.Empty;
        public string AbandonedStatusBadgeClass { get; set; } = string.Empty;

        // Üyelik durumu properties
        public bool HasMembership { get; set; }
        public string MembershipStatusText { get; set; } = string.Empty;
        public string MembershipStatusBadgeClass { get; set; } = string.Empty;
        public string DisplayCustomerName { get; set; } = string.Empty;
        public string DisplayCustomerEmail { get; set; } = string.Empty;
        public string ProfileIconClass { get; set; } = string.Empty;

        // Navigation properties
        public List<BasketItemViewModel> Items { get; set; } = new();
    }

    public class BasketItemViewModel
    {
        public Guid Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImage { get; set; }
        public double Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public double TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ShippingCost { get; set; }
        public bool FreeShipping { get; set; }
        public string Currency { get; set; } = "TRY";

        // Formatted properties
        public string FormattedUnitPrice { get; set; } = string.Empty;
        public string FormattedTotalPrice { get; set; } = string.Empty;
        public string FormattedQuantity { get; set; } = string.Empty;
    }

    public class BasketItemsPagedViewModel
    {
        public List<BasketItemViewModel> Items { get; set; } = new();
        public int TotalItems { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class BasketStatsViewModel
    {
        public int TotalBaskets { get; set; }
        public int ActiveBaskets { get; set; }
        public int AbandonedBaskets { get; set; }
        public decimal TotalValue { get; set; }
        public List<CurrencyTotalViewModel> CurrencyTotals { get; set; } = new();
        public DateTime? LastSyncDate { get; set; }

        public string FormattedTotalValue => 
            TotalValue.ToString("C2", new CultureInfo("tr-TR"));
        public double AbandonmentRate => TotalBaskets > 0 ? (double)AbandonedBaskets / TotalBaskets * 100 : 0;
        public string FormattedAbandonmentRate => $"{AbandonmentRate:F1}%";
    }

    public class CurrencyTotalViewModel
    {
        public string Currency { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int BasketCount { get; set; }
        public string FormattedTotalAmount { get; set; } = string.Empty;
        public string CurrencySymbol { get; set; } = string.Empty;
    }

    public class PaginationViewModel
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPrevious { get; set; }
        public bool HasNext { get; set; }

        public int StartRecord => (CurrentPage - 1) * PageSize + 1;
        public int EndRecord => Math.Min(CurrentPage * PageSize, TotalCount);
    }

    public class BasketSyncLogViewModel
    {
        public int Id { get; set; }
        public DateTime SyncStartTime { get; set; }
        public DateTime? SyncEndTime { get; set; }
        public int TotalRecordsProcessed { get; set; }
        public int NewRecordsAdded { get; set; }
        public int RecordsUpdated { get; set; }
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Notes { get; set; }

        // Formatted properties
        public string FormattedSyncStartTime { get; set; } = string.Empty;
        public string FormattedSyncEndTime { get; set; } = string.Empty;
        public string StatusText { get; set; } = string.Empty;
        public string StatusBadgeClass { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
    }
}
