using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Services.Integrations.Common.Models;
using PushDashboard.Services.Integrations.Ticimax;

namespace PushDashboard.Services.WhatsApp;

/// <summary>
/// WhatsApp template dönüşüm takibi servisi
/// Ticimax siparişlerinden ReklamKaynagi alanı ile template tracking GUID'lerini eşleştirerek
/// dönüşüm verilerini hesaplar
/// </summary>
public class WhatsAppConversionService : IWhatsAppConversionService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<WhatsAppConversionService> _logger;

    public WhatsAppConversionService(
        ApplicationDbContext dbContext,
        ILogger<WhatsAppConversionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TemplateConversionResult> GetConversionsAsync(
        Guid companyId,
        string templateName,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            // Template settings'den tracking GUID'i al
            var templateSettings = await _dbContext.WhatsAppTemplateSettings
                .FirstOrDefaultAsync(ts => ts.CompanyId == companyId && ts.TemplateName == templateName);

            if (templateSettings?.TrackingGuid == null)
            {
                return TemplateConversionResult.CreateFailure("Bu template için tracking GUID'i bulunamadı.");
            }

            var trackingGuidString = templateSettings.TrackingGuid.Value.ToString();

            // Tarih aralığını ayarla
            var effectiveEndDate = endDate ?? DateTime.Now;
            var effectiveStartDate = startDate ?? effectiveEndDate.AddDays(-30);

            _logger.LogInformation(
                "Fetching conversions for template {TemplateName} with TrackingGuid {TrackingGuid}, DateRange: {StartDate} - {EndDate}",
                templateName, trackingGuidString, effectiveStartDate, effectiveEndDate);

            // Fetch conversions from local database
            var conversionsQuery = _dbContext.OrderConversions
                .Where(oc => oc.CompanyId == companyId &&
                             oc.CreatedAt >= effectiveStartDate &&
                             oc.CreatedAt <= effectiveEndDate &&
                             oc.ConversionSource != null &&
                             oc.ConversionSource == trackingGuidString);

            var totalRevenue = await conversionsQuery.SumAsync(o => o.TotalAmount);
            var conversionsCount = await conversionsQuery.CountAsync();

            return TemplateConversionResult.CreateSuccess(
                conversionsCount,
                totalRevenue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversions for template {TemplateName}", templateName);
            return TemplateConversionResult.CreateFailure("Dönüşüm verileri alınırken hata oluştu: " + ex.Message);
        }
    }
}
