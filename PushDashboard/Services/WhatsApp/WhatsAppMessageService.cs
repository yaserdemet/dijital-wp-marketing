using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;

namespace PushDashboard.Services.WhatsApp;

public class WhatsAppMessageService : IWhatsAppMessageService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WhatsAppMessageService> _logger;

    public WhatsAppMessageService(
        ApplicationDbContext context,
        ILogger<WhatsAppMessageService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WhatsAppMessageListResult> GetCompanyMessagesAsync(
        Guid companyId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? status = null,
        string? templateName = null,
        string? recipientPhone = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _context.WhatsAppMessages
            .Where(m => m.CompanyId == companyId);

        if (startDate.HasValue)
        {
            query = query.Where(m => m.SentAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(m => m.SentAt <= endDate.Value);
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(m => m.Status == status);
        }

        if (!string.IsNullOrEmpty(templateName))
        {
            query = query.Where(m => m.TemplateName != null && m.TemplateName.Contains(templateName));
        }

        if (!string.IsNullOrEmpty(recipientPhone))
        {
            query = query.Where(m => m.RecipientPhone.Contains(recipientPhone));
        }

        var totalCount = await query.CountAsync();

        var messages = await query
            .OrderByDescending(m => m.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new WhatsAppMessageListResult
        {
            Messages = messages,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<WhatsAppMessage?> GetMessageByGupshupIdAsync(string gupshupMessageId)
    {
        return await _context.WhatsAppMessages
            .FirstOrDefaultAsync(m => m.GupshupMessageId == gupshupMessageId);
    }

    public async Task<bool> UpdateMessageStatusAsync(
        string gupshupMessageId,
        string newStatus,
        DateTime? timestamp = null,
        string? errorMessage = null,
        string? errorCode = null)
    {
        var message = await GetMessageByGupshupIdAsync(gupshupMessageId);
        if (message == null)
        {
            _logger.LogWarning("Message not found for Gupshup ID {GupshupMessageId}", gupshupMessageId);
            return false;
        }

        message.Status = newStatus;
        message.UpdatedAt = DateTime.Now;

        switch (newStatus.ToLower())
        {
            case "delivered":
                message.DeliveredAt = timestamp ?? DateTime.Now;
                break;
            case "read":
                message.ReadAt = timestamp ?? DateTime.Now;
                // Eğer delivered yoksa da set et
                if (!message.DeliveredAt.HasValue)
                {
                    message.DeliveredAt = timestamp ?? DateTime.Now;
                }
                break;
            case "failed":
                message.FailedAt = timestamp ?? DateTime.Now;
                message.ErrorMessage = errorMessage;
                message.ErrorCode = errorCode;
                break;
        }

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation(
                "Updated message {GupshupMessageId} status to {Status}",
                gupshupMessageId,
                newStatus);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Failed to update message {GupshupMessageId} status to {Status}",
                gupshupMessageId,
                newStatus);
            return false;
        }
    }

    public async Task<WhatsAppMessageStatistics> GetMessageStatisticsAsync(
        Guid companyId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var query = _context.WhatsAppMessages
            .Where(m => m.CompanyId == companyId);

        if (startDate.HasValue)
        {
            query = query.Where(m => m.SentAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(m => m.SentAt <= endDate.Value);
        }

        var stats = new WhatsAppMessageStatistics
        {
            TotalSent = await query.CountAsync(),
            TotalDelivered = await query.CountAsync(m => m.Status == "delivered" || m.Status == "read"),
            TotalRead = await query.CountAsync(m => m.Status == "read"),
            TotalFailed = await query.CountAsync(m => m.Status == "failed"),
            TotalPending = await query.CountAsync(m => m.Status == "pending" || m.Status == "sent")
        };

        return stats;
    }

    public async Task<WhatsAppMessage> CreateMessageAsync(
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
        string? appId = null)
    {
        var message = new WhatsAppMessage
        {
            CompanyId = companyId,
            GupshupMessageId = gupshupMessageId,
            RecipientPhone = recipientPhone,
            TemplateName = templateName,
            TemplateId = templateId,
            MessageType = messageType,
            MessageContent = messageContent,
            Source = source,
            SourceReferenceId = sourceReferenceId,
            CompanyIntegrationId = companyIntegrationId,
            AppId = appId,
            Status = "sent", // Initial status
            SentAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        _context.WhatsAppMessages.Add(message);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Created WhatsApp message record: {GupshupMessageId} for company {CompanyId}",
            gupshupMessageId,
            companyId);

        return message;
    }
}
