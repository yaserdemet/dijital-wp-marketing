using System.ComponentModel.DataAnnotations;

namespace PushDashboard.ViewModels;

public class BulkMessagingIndexViewModel
{
    public bool HasBulkMessagingModule { get; set; }
    public List<string> EnabledChannels { get; set; } = new();
    public List<BulkMessageViewModel> RecentMessages { get; set; } = new();
    public BulkMessagingStatsViewModel Stats { get; set; } = new();
}

public class BulkMessageViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalRecipients { get; set; }
    public int ProcessedRecipients { get; set; }
    public int SuccessfulSends { get; set; }
    public int FailedSends { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal ActualCost { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public int CurrentBatch { get; set; }
    public int TotalBatches { get; set; }

    // Computed properties
    public string StatusBadgeClass { get; set; } = string.Empty;
    public double ProgressPercentage { get; set; }
    public double SuccessRate { get; set; }
    public string FormattedEstimatedCost { get; set; } = string.Empty;
    public string FormattedActualCost { get; set; } = string.Empty;
    public string FormattedCreatedAt { get; set; } = string.Empty;
    public string FormattedDuration { get; set; } = string.Empty;
}

public class BulkMessagingStatsViewModel
{
    public int TotalMessages { get; set; }
    public int ActiveMessages { get; set; }
    public int CompletedMessages { get; set; }
    public int TotalRecipients { get; set; }
    public decimal TotalCost { get; set; }
    public double AverageSuccessRate { get; set; }

    public string FormattedTotalCost => $"₺{TotalCost:N2}";
    public string FormattedAverageSuccessRate => $"{AverageSuccessRate:F1}%";
}

public class CreateBulkMessageViewModel
{
    [Required(ErrorMessage = "Başlık gereklidir")]
    [StringLength(200, ErrorMessage = "Başlık en fazla 200 karakter olabilir")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Açıklama en fazla 1000 karakter olabilir")]
    public string? Description { get; set; }

    // Müşteri filtreleri
    public CustomerFilterViewModel CustomerFilters { get; set; } = new();

    // Kanal ayarları
    public List<ChannelSettingViewModel> ChannelSettings { get; set; } = new();

    // Önizleme
    public bool EnablePreview { get; set; } = true;
}

public class CustomerFilterViewModel
{
    public bool? IsActive { get; set; } // null = hepsi, true = aktif, false = pasif
    public bool? EmailPermission { get; set; }
    public bool? SmsPermission { get; set; }
    public List<string> Cities { get; set; } = new();
    public List<string> Sources { get; set; } = new(); // Müşteri kaynağı filtresi
    public DateTime? MembershipDateFrom { get; set; }
    public DateTime? MembershipDateTo { get; set; }
    public DateTime? LastLoginDateFrom { get; set; }
    public DateTime? LastLoginDateTo { get; set; }
    public string? SearchTerm { get; set; } // İsim, email, telefon araması
}

public class ChannelSettingViewModel
{
    [Required]
    public string ChannelType { get; set; } = string.Empty; // "email", "whatsapp"

    [Required]
    public bool IsEnabled { get; set; }

    [Required]
    public string TemplateName { get; set; } = string.Empty;

    public string? Subject { get; set; } // Email için
    public string? Content { get; set; } // Önizleme için
}

public class BulkMessagePreviewViewModel
{
    public int EstimatedRecipients { get; set; }
    public decimal EstimatedCost { get; set; }
    public List<ChannelPreviewViewModel> ChannelPreviews { get; set; } = new();
    public List<CustomerPreviewViewModel> SampleCustomers { get; set; } = new();
    public bool CanAfford { get; set; }
    public decimal CurrentBalance { get; set; }
    public string? WarningMessage { get; set; }
}

public class ChannelPreviewViewModel
{
    public string ChannelType { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string Content { get; set; } = string.Empty;
    public int EstimatedRecipients { get; set; }
    public decimal CostPerMessage { get; set; }
    public decimal TotalCost { get; set; }

    public string FormattedCostPerMessage => ChannelType.Equals("whatsapp", StringComparison.OrdinalIgnoreCase) ? "1 TL" : "Ücretsiz";
    public string FormattedTotalCost => ChannelType.Equals("whatsapp", StringComparison.OrdinalIgnoreCase) && TotalCost > 0 ? $"{TotalCost:N0} TL" : "Ücretsiz";
}

public class CustomerPreviewViewModel
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? MobilePhone { get; set; }
    public bool EmailPermission { get; set; }
    public bool SmsPermission { get; set; }
    public string? City { get; set; }
    public DateTime MembershipDate { get; set; }
    public List<string> AvailableChannels { get; set; } = new();

    public string FormattedMembershipDate => MembershipDate.ToString("dd.MM.yyyy");
}

public class BulkMessageProgressViewModel
{
    public int Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TotalRecipients { get; set; }
    public int ProcessedRecipients { get; set; }
    public int SuccessfulSends { get; set; }
    public int FailedSends { get; set; }
    public int CurrentBatch { get; set; }
    public int TotalBatches { get; set; }
    public decimal ActualCost { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EstimatedCompletionTime { get; set; }

    public double ProgressPercentage => TotalRecipients > 0 ? Math.Round((double)ProcessedRecipients / TotalRecipients * 100, 1) : 0;
    public double SuccessRate => ProcessedRecipients > 0 ? Math.Round((double)SuccessfulSends / ProcessedRecipients * 100, 1) : 0;
    public string FormattedActualCost => $"₺{ActualCost:N2}";
    public string FormattedEstimatedCompletion => EstimatedCompletionTime?.ToString("HH:mm") ?? "Hesaplanıyor...";
}

public class BulkMessageDetailViewModel
{
    public BulkMessageViewModel BulkMessage { get; set; } = new();
    public List<BulkMessageRecipientViewModel> Recipients { get; set; } = new();
    public BulkMessageProgressViewModel Progress { get; set; } = new();
    public List<ChannelPreviewViewModel> ChannelDetails { get; set; } = new();
}

public class BulkMessageRecipientViewModel
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public List<string> SentChannels { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ErrorMessage { get; set; }

    public string StatusBadgeClass { get; set; } = string.Empty;
    public string FormattedCost { get; set; } = string.Empty;
    public string FormattedProcessedAt { get; set; } = string.Empty;
}

public class AvailableChannelViewModel
{
    public string ChannelType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsConfigured { get; set; }
    public List<TemplateOptionViewModel> AvailableTemplates { get; set; } = new();
    public decimal CostPerMessage { get; set; }

    public string FormattedCostPerMessage => $"₺{CostPerMessage:N2}";
}

public class TemplateOptionViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string PreviewContent { get; set; } = string.Empty;
    public string? Category { get; set; }
}
