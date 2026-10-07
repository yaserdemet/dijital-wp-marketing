using PushDashboard.DTOs;
using PushDashboard.Models;

namespace PushDashboard.Services.Modules.FavoriteProduct;

public interface IFavoriteProductService
{
    /// <summary>
    /// Modül ayarlarını getirir
    /// </summary>
    Task<FavoriteProductSettingsDto> GetModuleSettingsAsync(Guid companyId);

    /// <summary>
    /// Modül ayarlarını günceller
    /// </summary>
    Task<bool> UpdateModuleSettingsAsync(Guid companyId, UpdateFavoriteProductSettingsRequest request);

    /// <summary>
    /// Favori ürünleri API'den çekip veritabanına senkronize eder
    /// </summary>
    Task<FavoriteProductSyncResult> SyncFavoriteProductsAsync(Guid companyId, string userId, bool sendNewFavoriteNotifications = false);

    /// <summary>
    /// Veritabanından favori ürünleri getirir (gruplu - ürün bazında)
    /// </summary>
    Task<FavoriteProductListResult> GetFavoriteProductsAsync(
        Guid companyId,
        int page = 1,
        int pageSize = 20,
        string? searchTerm = null,
        DateTime? startDate = null,
        DateTime? endDate = null);

    /// <summary>
    /// Belirli bir ürünün favori detaylarını getirir
    /// </summary>
    Task<List<FavoriteProductMemberDto>> GetProductFavoriteMembersAsync(
        Guid companyId,
        int productId,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Favori ürün istatistiklerini getirir
    /// </summary>
    Task<FavoriteProductStatsResult> GetStatsAsync(Guid companyId);

    /// <summary>
    /// Son senkronizasyon bilgisini getirir
    /// </summary>
    Task<FavoriteProductSyncLog?> GetLastSyncAsync(Guid companyId);

    /// <summary>
    /// Favori ürün sahiplerine bildirim gönderir (stok geldi, indirim vb.)
    /// </summary>
    Task<FavoriteProductNotificationResult> SendNotificationAsync(
        Guid companyId,
        int productId,
        string notificationType,
        string message,
        string userId);

    /// <summary>
    /// Yeni eklenen favoriler için otomatik bildirim gönderir
    /// </summary>
    Task<FavoriteProductNotificationResult> SendNewFavoriteNotificationsAsync(
        Guid companyId,
        List<Models.FavoriteProduct> newFavorites,
        string userId);

    /// <summary>
    /// Webhook'tan gelen favori ekleme eventini işler
    /// </summary>
    Task<bool> ProcessFavoriteAddedWebhookAsync(
        Guid companyId,
        int externalProductId,
        int externalMemberId,
        string productName,
        string? productUrl);
}

// DTOs
public class FavoriteProductSyncResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int TotalFetched { get; set; }
    public int NewRecords { get; set; }
    public int UpdatedRecords { get; set; }
    public int DeletedRecords { get; set; }
    public DateTime SyncedAt { get; set; }
}

public class FavoriteProductListResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<FavoriteProductGroupDto> Products { get; set; } = new();
    public int TotalCount { get; set; }
    public int TotalFavoriteCount { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
}

public class FavoriteProductGroupDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string? StockCode { get; set; }
    public int StockQuantity { get; set; }
    public decimal Price { get; set; }
    public decimal PriceWithVat { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? ImageUrl { get; set; }
    public string? ProductUrl { get; set; }
    public int VariantCount { get; set; }
    public int FavoriteCount { get; set; }
    public DateTime FirstAddedAt { get; set; }
    public DateTime LastAddedAt { get; set; }
}

public class FavoriteProductMemberDto
{
    public int MemberId { get; set; }
    public string MemberName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime AddedAt { get; set; }
    public bool NotificationSent { get; set; }
    public DateTime? NotificationSentAt { get; set; }
    public string? CustomerPageUrl { get; set; }
}

public class FavoriteProductStatsResult
{
    public int TotalFavoriteProducts { get; set; }
    public int TotalFavoriteCount { get; set; }
    public int TotalUniqueMembers { get; set; }
    public int FavoritesThisMonth { get; set; }
    public int FavoritesThisWeek { get; set; }
    public int FavoritesToday { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public bool HasData { get; set; }
}

public class FavoriteProductNotificationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public int NotificationsSent { get; set; }
    public decimal TotalCost { get; set; }
}