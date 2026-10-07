namespace PushDashboard.Services.WhatsApp;

/// <summary>
/// WhatsApp mesaj takibi için servis interface
/// </summary>
public interface IWhatsAppMessageService
{
    /// <summary>
    /// Şirketin WhatsApp mesajlarını filtrelerle getirir (paginated)
    /// </summary>
    Task<WhatsAppMessageListResult> GetCompanyMessagesAsync(
        Guid companyId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? status = null,
        string? templateName = null,
        string? recipientPhone = null,
        int page = 1,
        int pageSize = 50);

    /// <summary>
    /// Gupshup mesaj ID'sine göre mesaj bulur
    /// </summary>
    Task<Models.WhatsAppMessage?> GetMessageByGupshupIdAsync(string gupshupMessageId);

    /// <summary>
    /// Mesaj durumunu günceller
    /// </summary>
    Task<bool> UpdateMessageStatusAsync(
        string gupshupMessageId,
        string newStatus,
        DateTime? timestamp = null,
        string? errorMessage = null,
        string? errorCode = null);

    /// <summary>
    /// Şirket için mesaj istatistiklerini getirir
    /// </summary>
    Task<WhatsAppMessageStatistics> GetMessageStatisticsAsync(
        Guid companyId,
        DateTime? startDate = null,
        DateTime? endDate = null);

    /// <summary>
    /// Yeni WhatsApp mesajı kaydı oluşturur
    /// </summary>
    Task<Models.WhatsAppMessage> CreateMessageAsync(
        Guid companyId,
        string gupshupMessageId,
        string recipientPhone,
        string? templateName,
        string? templateId,
        string messageType,
        string? messageContent,
        string? source,
        string? sourceReferenceId,
        int? companyIntegrationId = null,
        string? appId = null);
}

/// <summary>
/// WhatsApp mesaj listesi sonucu
/// </summary>
public class WhatsAppMessageListResult
{
    public List<Models.WhatsAppMessage> Messages { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>
/// WhatsApp mesaj istatistikleri
/// </summary>
public class WhatsAppMessageStatistics
{
    public int TotalSent { get; set; }
    public int TotalDelivered { get; set; }
    public int TotalRead { get; set; }
    public int TotalFailed { get; set; }
    public int TotalPending { get; set; }
    public double DeliveryRate => TotalSent > 0 ? (double)TotalDelivered / TotalSent * 100 : 0;
    public double ReadRate => TotalDelivered > 0 ? (double)TotalRead / TotalDelivered * 100 : 0;
    public double FailureRate => TotalSent > 0 ? (double)TotalFailed / TotalSent * 100 : 0;
}
