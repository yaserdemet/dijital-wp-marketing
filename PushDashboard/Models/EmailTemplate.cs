namespace PushDashboard.Models;

public class EmailTemplate
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty; // Müşteri, Sepet, Sipariş

    public string Description { get; set; } = string.Empty;

    public string DefaultContent { get; set; } = string.Empty; // HTML content

    public string? DefaultSubject { get; set; }

    public string? Variables { get; set; } // JSON array of available variables

    public int? ModuleId { get; set; } // Hangi modüle bağlı (null = herkese açık)

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public virtual ICollection<CompanyEmailTemplate> CompanyTemplates { get; set; } = new List<CompanyEmailTemplate>();
}

public class CompanyEmailTemplate
{
    public int Id { get; set; }

    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    public int? EmailTemplateId { get; set; }

    // No foreign key constraint for custom templates (negative IDs)
    public virtual EmailTemplate? EmailTemplate { get; set; }

    public string? Name { get; set; } // Şablon adı

    public string? Category { get; set; } // Şablon kategorisi

    public string? Description { get; set; } // Şablon açıklaması

    public string CustomContent { get; set; } = string.Empty; // Şirketin özelleştirdiği içerik

    public string? CustomSubject { get; set; }

    public bool IsEnabled { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public string? LastModifiedBy { get; set; } // User ID

    public virtual ApplicationUser? LastModifiedByUser { get; set; }

    public string? ModuleType { get; set; } // Hangi modül için: OrderStatus, BulkMessaging, etc.
}
