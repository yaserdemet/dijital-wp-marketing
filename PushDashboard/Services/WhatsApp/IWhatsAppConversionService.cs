namespace PushDashboard.Services.WhatsApp;

/// <summary>
/// WhatsApp template dönüşüm takibi servisi interface'i
/// </summary>
public interface IWhatsAppConversionService
{
    /// <summary>
    /// Template için dönüşüm verilerini getirir
    /// </summary>
    Task<TemplateConversionResult> GetConversionsAsync(
        Guid companyId,
        string templateName,
        DateTime? startDate = null,
        DateTime? endDate = null);
}

/// <summary>
/// Template dönüşüm sonucu
/// </summary>
public class TemplateConversionResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public int OrderCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public string Currency { get; set; } = "TRY";

    public static TemplateConversionResult CreateSuccess(int orderCount, decimal totalRevenue)
    {
        return new TemplateConversionResult
        {
            Success = true,
            OrderCount = orderCount,
            TotalRevenue = totalRevenue,
        };
    }

    public static TemplateConversionResult CreateFailure(string message)
    {
        return new TemplateConversionResult
        {
            Success = false,
            Message = message
        };
    }
}
