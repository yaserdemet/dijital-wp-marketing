namespace PushDashboard.Models;

public class SmsTemplate
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty; // Müşteri, Sepet, Sipariş, Genel

    public string Description { get; set; } = string.Empty;

    public string DefaultContent { get; set; } = string.Empty; // SMS content

    public string? Variables { get; set; } // JSON array of available variables

    public int? ModuleId { get; set; } // Hangi modüle bağlı (null = herkese açık)

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public virtual ICollection<CompanySmsTemplate> CompanyTemplates { get; set; } = new List<CompanySmsTemplate>();
}

public class CompanySmsTemplate
{
    public int Id { get; set; }

    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    public int? SmsTemplateId { get; set; } // Nullable for custom templates (null = custom template)

    public virtual SmsTemplate? SmsTemplate { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string CustomContent { get; set; } = string.Empty; // Şirketin özelleştirdiği içerik

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public string? LastModifiedBy { get; set; } // User ID

    public virtual ApplicationUser? LastModifiedByUser { get; set; }

    public string? ModuleType { get; set; } // Hangi modül için: FirstOrder, BulkMessaging, etc.
}
