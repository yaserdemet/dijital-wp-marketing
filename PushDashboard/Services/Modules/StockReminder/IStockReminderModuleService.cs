using PushDashboard.DTOs;

namespace PushDashboard.Services.Modules.StockReminder;

public interface IStockReminderModuleService
{
    /// <summary>
    /// Stok hatırlatma bildirimi gönderir
    /// </summary>
    Task<(bool Success, string Message, int NotificationsSent, decimal TotalCost)> SendStockReminderNotificationAsync(
        Guid companyId, int productId, int currentStock, string userId);

    /// <summary>
    /// Şirketin aktif iletişim kanallarını getirir
    /// </summary>
    Task<List<ActiveChannelDto>> GetActiveChannelsAsync(Guid companyId);

    /// <summary>
    /// Stok hatırlatma modülü ayarlarını getirir
    /// </summary>
    Task<StockReminderSettingsDto> GetModuleSettingsAsync(Guid companyId);

    /// <summary>
    /// Stok hatırlatma modülü ayarlarını günceller
    /// </summary>
    Task<(bool Success, string Message)> UpdateModuleSettingsAsync(Guid companyId, UpdateStockReminderSettingsRequest request, string userId);

    /// <summary>
    /// Stok hatırlatma modülü istatistiklerini getirir
    /// </summary>
    Task<StockReminderStatsDto> GetModuleStatsAsync(Guid companyId);

    /// <summary>
    /// Ürün stoğunun eşiğin altına düşüp düşmediğini kontrol eder
    /// </summary>
    Task<bool> IsStockBelowThresholdAsync(Guid companyId, int productId, int currentStock);

    /// <summary>
    /// Stok güncellendiğinde webhook ile tetiklenir - abone müşterilere bildirim gönderir
    /// </summary>
    Task<(bool Success, string Message, int NotificationsSent, decimal TotalCost)> ProcessStockUpdatedWebhookAsync(
        Guid companyId, 
        int productId, 
        string productName,
        string? productUrl,
        int oldStock, 
        int newStock);

    /// <summary>
    /// Belirli bir ürün için stok alarm abonelerine bildirim gönderir
    /// </summary>
    Task<(bool Success, string Message, int NotificationsSent, decimal TotalCost)> SendStockArrivedNotificationsAsync(
        Guid companyId,
        int productId,
        string productName,
        string? productUrl,
        int currentStock,
        string userId);

    /// <summary>
    /// Ticimax'tan stok alarm abonelerini getirir
    /// </summary>
    Task<List<StockAlarmSubscriberDto>> GetStockAlarmSubscribersAsync(Guid companyId, int productId);

    /// <summary>
    /// E-ticaret sisteminden stok alarm ürünlerini çeker ve stok durumlarını döndürür
    /// </summary>
    Task<StockReminderPullResult> PullStockRemindersAsync(Guid companyId, int productCardId = 0);

    /// <summary>
    /// Ürün servisinden belirli bir ürünün güncel bilgilerini alarak, o ürüne ait stok hatırlatma kayıtlarını günceller
    /// </summary>
    Task<StockReminderPullResult> RefreshProductStockDataAsync(Guid companyId, int productId);

    /// <summary>
    /// Stoğu gelen ürünlerin abonelerine kanallar üzerinden mesaj gönderir
    /// </summary>
    Task<StockReminderSendResult> SendStockReminderMessagesAsync(Guid companyId, List<int>? productIds = null);

    /// <summary>
    /// Tüm şirketler için stok alarm ürünlerini çeker
    /// </summary>
    Task<AllCompaniesStockReminderPullResult> PullAllStockRemindersAsync();

    /// <summary>
    /// Tüm şirketler için stoğu gelen ürünlerin abonelerine mesaj gönderir
    /// </summary>
    Task<AllCompaniesStockReminderSendResult> SendAllStockReminderMessagesAsync();
}

/// <summary>
/// Stok alarm abonesi bilgisi
/// </summary>
public class StockAlarmSubscriberDto
{
    public int ExternalMemberId { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime SubscribedAt { get; set; }

    /// <summary>
    /// Abone kaynağı: "ticimax" veya "widget"
    /// </summary>
    public string Source { get; set; } = "ticimax";

    /// <summary>
    /// Widget abonelerinde seçilen varyasyon/beden
    /// </summary>
    public string? SelectedVariant { get; set; }

    /// <summary>
    /// Widget abonelerinde sayfa URL'si
    /// </summary>
    public string? PageUrl { get; set; }

    public bool IsNotified { get; set; }
}

/// <summary>
/// Stok alarm ürünlerini çekme sonucu
/// </summary>
public class StockReminderPullResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int TotalProductsWithAlarm { get; set; }
    public int ProductsInStock { get; set; }
    public int ProductsOutOfStock { get; set; }
    public List<StockAlarmProductDto> Products { get; set; } = new();
}

/// <summary>
/// Stok alarm ürün bilgisi
/// </summary>
public class StockAlarmProductDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductUrl { get; set; }
    public int CurrentStock { get; set; }
    public bool InStock { get; set; }
    public int SubscriberCount { get; set; }
    public DateTime? LastNotifiedAt { get; set; }
}

/// <summary>
/// Stok hatırlatma mesaj gönderim sonucu
/// </summary>
public class StockReminderSendResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int TotalProducts { get; set; }
    public int TotalNotificationsSent { get; set; }
    public decimal TotalCost { get; set; }
    public List<ProductNotificationResult> ProductResults { get; set; } = new();
}

/// <summary>
/// Ürün bazında bildirim gönderim sonucu
/// </summary>
public class ProductNotificationResult
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int NotificationsSent { get; set; }
    public decimal Cost { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Tüm şirketler için stok alarm çekme sonucu
/// </summary>
public class AllCompaniesStockReminderPullResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int TotalCompaniesProcessed { get; set; }
    public int TotalCompaniesSucceeded { get; set; }
    public int TotalCompaniesFailed { get; set; }
    public List<CompanyStockReminderPullResult> CompanyResults { get; set; } = new();
}

public class CompanyStockReminderPullResult
{
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public StockReminderPullResult? Result { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Tüm şirketler için stok hatırlatma mesaj gönderim sonucu
/// </summary>
public class AllCompaniesStockReminderSendResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int TotalCompaniesProcessed { get; set; }
    public int TotalNotificationsSent { get; set; }
    public decimal TotalCost { get; set; }
    public List<CompanyStockReminderSendResult> CompanyResults { get; set; } = new();
}

public class CompanyStockReminderSendResult
{
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public StockReminderSendResult? Result { get; set; }
    public string? Error { get; set; }
}
