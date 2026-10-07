using PushDashboard.DTOs;

namespace PushDashboard.Services.Modules.FirstOrder;

public interface IFirstOrderModuleService
{
    /// <summary>
    /// İlk alışveriş yapan müşteri için bildirim gönderir
    /// </summary>
    Task<(bool Success, string Message, int NotificationsSent, decimal TotalCost)> SendFirstOrderNotificationAsync(
        Guid companyId, int customerId, string userId);

    /// <summary>
    /// Müşterinin daha önce sipariş verip vermediğini kontrol eder
    /// </summary>
    Task<bool> IsFirstOrderAsync(Guid companyId, int customerId);

    /// <summary>
    /// Şirketin aktif iletişim kanallarını getirir
    /// </summary>
    Task<List<ActiveChannelDto>> GetActiveChannelsAsync(Guid companyId);

    /// <summary>
    /// İlk alışveriş modülü ayarlarını getirir
    /// </summary>
    Task<FirstOrderSettingsDto> GetModuleSettingsAsync(Guid companyId);

    /// <summary>
    /// İlk alışveriş modülü ayarlarını günceller
    /// </summary>
    Task<(bool Success, string Message)> UpdateModuleSettingsAsync(Guid companyId, UpdateFirstOrderSettingsRequest request, string userId);

    /// <summary>
    /// Hediye çeki oluşturmayı test eder
    /// </summary>
    Task<GiftVoucherTestResult> TestGiftVoucherCreationAsync(Guid companyId);

    /// <summary>
    /// İlk alışveriş modülü istatistiklerini getirir
    /// </summary>
    Task<FirstOrderStatsDto> GetModuleStatsAsync(Guid companyId);
}
