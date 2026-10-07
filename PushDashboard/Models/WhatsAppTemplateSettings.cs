namespace PushDashboard.Models;

/// <summary>
/// WhatsApp template'leri için local ayarları saklar (Facebook'tan gelen template'lere ek bilgiler)
/// </summary>
public class WhatsAppTemplateSettings
{
    public int Id { get; set; }

    /// <summary>
    /// Şirket ID'si
    /// </summary>
    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    /// <summary>
    /// Template adı - Unique identifier (CompanyId ile birlikte)
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Hangi modül için kullanılacak (OrderStatus, BulkMessaging, BasketReminder, Generic)
    /// </summary>
    public string? ModuleType { get; set; }

    /// <summary>
    /// Oluşturulma tarihi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// Güncellenme tarihi
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Dönüşüm takibi için benzersiz GUID
    /// Bu GUID, template butonlarındaki URL'lere push parametresi olarak eklenir
    /// Ticimax'tan gelen siparişlerde ReklamKaynagi alanında bu GUID ile eşleşme yapılır
    /// </summary>
    public Guid? TrackingGuid { get; set; }
}
