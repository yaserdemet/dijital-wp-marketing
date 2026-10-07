using System.ComponentModel.DataAnnotations;

namespace PushDashboard.Models;

/// <summary>
/// E-ticaret platformu sipariş durumlarının sistem durumlarına eşleştirmesi
/// </summary>
public class OrderStatusMapping
{
    public int Id { get; set; }

    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    /// <summary>
    /// E-ticaret platform türü (Ticimax, Shopify, vb.)
    /// </summary>
    public string IntegrationType { get; set; } = string.Empty;

    /// <summary>
    /// E-ticaret platformundaki durum kodu/adı
    /// </summary>
    public string ExternalStatus { get; set; } = string.Empty;

    /// <summary>
    /// E-ticaret platformundaki durumun görüntülenen adı
    /// </summary>
    public string ExternalStatusDisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Sistemdeki karşılık gelen durum (1-7)
    /// </summary>
    public string InternalStatus { get; set; } = string.Empty;

    /// <summary>
    /// Bu eşleştirme aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Oluşturulma tarihi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Güncellenme tarihi
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
