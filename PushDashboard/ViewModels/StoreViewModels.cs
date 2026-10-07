using PushDashboard.Models;

namespace PushDashboard.ViewModels;

public class StoreIndexViewModel
{
    public List<ModuleCategoryViewModel> Categories { get; set; } = new();

    public List<ModuleViewModel> Modules { get; set; } = new();

    public List<ModuleViewModel> OwnedModules { get; set; } = new();
    public List<TransactionHistoryViewModel> RecentTransactions { get; set; } = new();
    public List<ModuleViewModel> AvailableModules { get; set; } = new();

    public StoreStatsViewModel Stats { get; set; } = new();

    public class ModuleCategoryViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? IconClass { get; set; }

        public int ModuleCount { get; set; }
    }

    public class ModuleViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string? DetailedDescription { get; set; }

        public decimal Price { get; set; }

        public string? IconClass { get; set; }

        public string? IconColor { get; set; }

        public string? BackgroundColor { get; set; }

        /// <summary>
        /// Modülün genel aktiflik durumu (Module tablosundan)
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Kullanıcının modülü pasife alıp almadığı (CompanyModule.IsPassive)
        /// Toggle için kullanılır - true ise modül pasif, false ise aktif
        /// </summary>
        public bool IsPassive { get; set; }

        public bool IsNew { get; set; }

        public bool IsFeatured { get; set; }

        /// <summary>
        /// Şirketin bu modüle sahip olup olmadığı (CompanyModule.IsOwned)
        /// </summary>
        public bool IsOwned { get; set; }

        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public List<string> Features { get; set; } = new();

        public DateTime? PurchasedAt { get; set; }

        public DateTime? LastUsedAt { get; set; }

        public string FormattedPrice => $"₺{Price:N0}";

        /// <summary>
        /// Sahiplik ve pasiflik durumuna göre badge
        /// </summary>
        public string StatusBadge => IsOwned ? (!IsPassive ? "Aktif" : "Pasif") : (IsNew ? "Yeni" : "");

        public string StatusBadgeClass => IsOwned
            ? (!IsPassive ? "bg-green-100 text-green-800" : "bg-yellow-100 text-yellow-800")
            : (IsNew ? "bg-blue-100 text-blue-800" : "");

        /// <summary>
        /// Aylık fiyat
        /// </summary>
        public decimal MonthlyPrice { get; set; }

        /// <summary>
        /// Yıllık fiyat (indirimli)
        /// </summary>
        public decimal YearlyPrice { get; set; }

        /// <summary>
        /// Abonelik tipi (Monthly/Yearly)
        /// </summary>
        public string? SubscriptionType { get; set; }

        /// <summary>
        /// Otomatik yenileme
        /// </summary>
        public bool AutoRenewal { get; set; } = true;

        /// <summary>
        /// Abonelik bitiş tarihi
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Formatlanmış abonelik bitiş tarihi
        /// </summary>
        public string? FormattedExpiresAt => ExpiresAt?.ToString("dd MMMM yyyy");

        /// <summary>
        /// Modülün sayfa URL'si (varsa) - Modülü Kullan butonu için
        /// </summary>
        public string? PageUrl => Name switch
        {
            "Favori Ürünler" => "/FavoriteProduct",
            "Stok Hatırlatma" => "/StockReminder",
            "Sepet Hatırlatma" => "/BasketReminder",
            "Yorum Taşıma" => "/Comment",
            "Sipariş Durumu Bildirimleri" => "/OrderStatus",
            "Toplu Mesaj Gönderimi" => "/Customer?openBulkMessaging=true",
            "Hediye Çarkı" => "/GiftWheel",
            "Video Hosting" => "/VideoHosting",
            "Ürün Slider" => "/ProductSlider",
            "Çıkış Anketi" => "/ExitSurvey",
            // "Doğum Günü Hatırlatma" => "/Customer", // Müşteriler sayfasından doğum günü takibi yapılabilir
            "Social Proof" => "/SocialProof",
            "Çerez Yönetimi" => "/CookieManagement",
            _ => null
        };

        /// <summary>
        /// Modülün bir sayfa URL'si var mı?
        /// </summary>
        public bool HasPageUrl => !string.IsNullOrEmpty(PageUrl);

        /// <summary>
        /// Modülün paket sistemi kullanıp kullanmadığı
        /// </summary>
        public bool HasPackages { get; set; }

        // Quota info (only populated for modules with packages when owned)
        public string? PackageName { get; set; }
        public decimal? TotalQuota { get; set; }
        public decimal? RemainingQuota { get; set; }
        /// <summary>
        /// Unit label for quota ("adet", "MB", etc.) Default: "adet"
        /// </summary>
        public string QuotaUnit { get; set; } = "adet";

        public bool HasQuotaInfo => HasPackages && IsOwned && TotalQuota.HasValue;
    }

    public class StoreStatsViewModel
    {
        public decimal TotalCredits { get; set; }

        public DateTime? LastUsage { get; set; }

        public int ActiveModules { get; set; }

        /// <summary>
        /// Kullanıcının pasife aldığı modül sayısı (IsOwned && IsPassive)
        /// </summary>
        public int PassiveModules { get; set; }

        public List<string> ActiveModuleNames { get; set; } = new();

        public string FormattedCredits => $"₺{TotalCredits:N2}";

        public string FormattedLastUsage => LastUsage?.ToString("dd MMMM yyyy") ?? "Henüz kullanılmadı";
    }

    public class PurchaseModuleViewModel
    {
        public int ModuleId { get; set; }

        public string ModuleName { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string PaymentMethod { get; set; } = "credit"; // credit, card

        public bool AcceptTerms { get; set; }

        /// <summary>
        /// Abonelik tipi - Monthly veya Yearly
        /// </summary>
        public string SubscriptionType { get; set; } = "Monthly";

        /// <summary>
        /// Otomatik yenileme aktif mi?
        /// </summary>
        public bool AutoRenewal { get; set; } = true;
    }

    public class ModuleDetailsViewModel
    {
        public ModuleViewModel Module { get; set; } = new();

        public bool CanPurchase { get; set; }

        public string? PurchaseBlockReason { get; set; }

        public List<ModuleViewModel> RelatedModules { get; set; } = new();
    }

    public class PagedTransactionHistoryViewModel
    {
        public List<TransactionHistoryViewModel> Transactions { get; set; } = new();

        public int CurrentPage { get; set; } = 1;

        public int TotalPages { get; set; } = 1;

        public int TotalItems { get; set; } = 0;

        public int PageSize { get; set; } = 10;

        public bool HasPreviousPage => CurrentPage > 1;

        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class TransactionHistoryViewModel
    {
        public int Id { get; set; }

        public string ModuleName { get; set; } = string.Empty;

        public decimal PaidAmount { get; set; }

        public DateTime PurchasedAt { get; set; }

        public string? TransactionId { get; set; }

        public string Status { get; set; } = "Tamamlandı";

        public int CategoryId { get; set; }

        public string CategoryName { get; set; } = string.Empty;

        public string? ModuleIcon { get; set; }

        public string? ModuleIconColor { get; set; }

        public string? ModuleBackgroundColor { get; set; }

        public string FormattedAmount => $"₺{PaidAmount:N2}";

        public string FormattedDate => PurchasedAt.ToString("dd MMM yyyy");

        public string FormattedTime => PurchasedAt.ToString("HH:mm");

        public string StatusBadgeClass => Status switch
        {
            "Tamamlandı" => "bg-green-100 text-green-800",
            "Beklemede" => "bg-yellow-100 text-yellow-800",
            "İptal" => "bg-red-100 text-red-800",
            _ => "bg-gray-100 text-gray-800"
        };
    }
}

public class TransactionHistoryViewModel
{
    public int Id { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public decimal PaidAmount { get; set; }
    public DateTime PurchasedAt { get; set; }
    public string? TransactionId { get; set; }
    public string Status { get; set; } = "Tamamlandı";
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;
    public string? ModuleIcon { get; set; }
    public string? ModuleIconColor { get; set; }
    public string? ModuleBackgroundColor { get; set; }

    public string FormattedAmount => $"₺{PaidAmount:N2}";
    public string FormattedDate => PurchasedAt.ToString("dd MMM yyyy");
    public string FormattedTime => PurchasedAt.ToString("HH:mm");
    public string StatusBadgeClass => Status switch
    {
        "Tamamlandı" => "bg-green-100 text-green-800",
        "Beklemede" => "bg-yellow-100 text-yellow-800",
        "İptal" => "bg-red-100 text-red-800",
        _ => "bg-gray-100 text-gray-800"
    };
}
