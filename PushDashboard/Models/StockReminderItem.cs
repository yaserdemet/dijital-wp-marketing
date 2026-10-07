using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

/// <summary>
/// Stok hatırlatma alarm ürünü — Ticimax'tan çekilen stok alarm verileri
/// </summary>
public class StockReminderItem
{
    public int Id { get; set; }

    public Guid CompanyId { get; set; }
    public virtual Company Company { get; set; } = null!;

    /// <summary>
    /// Ticimax stok alarm ürün ID
    /// </summary>
    public int ExternalAlarmId { get; set; }

    /// <summary>
    /// Ticimax ürün kartı ID
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Ürün adı
    /// </summary>
    public string ProductName { get; set; } = string.Empty;

    /// <summary>
    /// Stok kodu
    /// </summary>
    public string? StockCode { get; set; }

    /// <summary>
    /// Toplam stok adedi
    /// </summary>
    public int TotalStock { get; set; }

    /// <summary>
    /// Ürün fiyatı (KDV hariç)
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Ürün fiyatı (KDV dahil)
    /// </summary>
    public decimal PriceWithTax { get; set; }

    /// <summary>
    /// Para birimi
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Ürün resim URL'si
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Ürün sayfa URL'si
    /// </summary>
    public string? ProductUrl { get; set; }

    /// <summary>
    /// Varyasyon sayısı
    /// </summary>
    public int VariationCount { get; set; }

    /// <summary>
    /// Eşleşen varyasyon ID (Ticimax)
    /// </summary>
    public int? VariationId { get; set; }

    /// <summary>
    /// Eşleşen varyasyon barkodu
    /// </summary>
    public string? VariationBarcode { get; set; }

    /// <summary>
    /// Eşleşen varyasyon stok kodu
    /// </summary>
    public string? VariationStockCode { get; set; }

    /// <summary>
    /// Eşleşen varyasyon stok adedi
    /// </summary>
    public int? VariationStockQuantity { get; set; }

    /// <summary>
    /// Ticimax üye ID (alarm ekleyen)
    /// </summary>
    public int ExternalMemberId { get; set; }

    /// <summary>
    /// Müşteri adı
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Müşteri email
    /// </summary>
    public string? CustomerEmail { get; set; }

    /// <summary>
    /// Ticimax'taki alarm ekleme tarihi
    /// </summary>
    public DateTime AlarmCreatedAt { get; set; }

    /// <summary>
    /// Son son senkronizasyon tarihi
    /// </summary>
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Kayıt oluşturulma tarihi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Bildirim gönderildi mi?
    /// </summary>
    public bool IsNotified { get; set; } = false;

    /// <summary>
    /// Bildirim gönderim tarihi
    /// </summary>
    public DateTime? NotifiedAt { get; set; }
}
