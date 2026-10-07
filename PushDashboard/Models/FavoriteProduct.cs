using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

/// <summary>
/// Müşterilerin favorilerine eklediği ürünleri takip eder
/// </summary>
public class FavoriteProduct
{
    public int Id { get; set; }

    /// <summary>
    /// Hangi şirkete ait
    /// </summary>
    public Guid CompanyId { get; set; }
    public virtual Company Company { get; set; } = null!;

    /// <summary>
    /// E-ticaret platformundaki Favori Ürün ID'si
    /// </summary>
    public int ExternalFavoriteId { get; set; }

    /// <summary>
    /// E-ticaret platformundaki Ürün Kartı ID'si
    /// </summary>
    public int ExternalProductId { get; set; }

    /// <summary>
    /// E-ticaret platformundaki Üye ID'si
    /// </summary>
    public int ExternalMemberId { get; set; }

    /// <summary>
    /// Ürün Adı
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Stok Kodu
    /// </summary>
    public string? StockCode { get; set; }

    /// <summary>
    /// Ürün Fiyatı (KDV Hariç)
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Ürün Fiyatı (KDV Dahil)
    /// </summary>
    public decimal PriceWithVat { get; set; }

    /// <summary>
    /// Para Birimi
    /// </summary>
    public string Currency { get; set; } = "TRY";

    /// <summary>
    /// Toplam Stok Adedi
    /// </summary>
    public int StockQuantity { get; set; }

    /// <summary>
    /// Ürün Resim URL'si
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Ürün Sayfa URL'si
    /// </summary>
    public string? ProductUrl { get; set; }

    /// <summary>
    /// Varyasyon Sayısı
    /// </summary>
    public int VariantCount { get; set; }

    /// <summary>
    /// E-ticaret platformunda favoriye eklenme tarihi
    /// </summary>
    public DateTime AddedToFavoritesAt { get; set; }

    /// <summary>
    /// Sistemimize kaydedilme tarihi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Son güncelleme tarihi
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Son senkronizasyon tarihi
    /// </summary>
    public DateTime? LastSyncAt { get; set; }

    /// <summary>
    /// Aktif mi? (Platform'dan silinmişse false)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Bu favori için bildirim gönderildi mi?
    /// </summary>
    public bool NotificationSent { get; set; } = false;

    /// <summary>
    /// Bildirim gönderilme tarihi
    /// </summary>
    public DateTime? NotificationSentAt { get; set; }

    /// <summary>
    /// Müşteri bilgisi (varsa)
    /// </summary>
    public int? CustomerId { get; set; }
    public virtual Customer? Customer { get; set; }
}

/// <summary>
/// Favori ürün senkronizasyon log'u
/// </summary>
public class FavoriteProductSyncLog
{
    public int Id { get; set; }

    public Guid CompanyId { get; set; }
    public virtual Company Company { get; set; } = null!;

    /// <summary>
    /// Senkronizasyon başlangıç zamanı
    /// </summary>
    public DateTime StartedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Senkronizasyon bitiş zamanı
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Başarılı mı?
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Toplam çekilen kayıt sayısı
    /// </summary>
    public int TotalFetched { get; set; }

    /// <summary>
    /// Yeni eklenen kayıt sayısı
    /// </summary>
    public int NewRecords { get; set; }

    /// <summary>
    /// Güncellenen kayıt sayısı
    /// </summary>
    public int UpdatedRecords { get; set; }

    /// <summary>
    /// Silinen (pasife alınan) kayıt sayısı
    /// </summary>
    public int DeletedRecords { get; set; }

    /// <summary>
    /// Hata mesajı (varsa)
    /// </summary>
    public string? ErrorMessage { get; set; }
}