using PushDashboard.Models;
using PushDashboard.ViewModels;

namespace PushDashboard.Services;

public interface IBasketReminderService
{
    #region Settings Management
    /// <summary>
    /// Şirketin sepet hatırlatma ayarlarını getirir
    /// </summary>
    Task<BasketReminderSettings?> GetSettingsAsync(Guid companyId);

    /// <summary>
    /// Şirketin sepet hatırlatma ayarlarını günceller
    /// </summary>
    Task<(bool Success, string Message)> UpdateSettingsAsync(Guid companyId, BasketReminderSettings settings, string userId);

    /// <summary>
    /// Şirket için varsayılan sepet hatırlatma ayarlarını oluşturur (modül satın alma sonrası)
    /// </summary>
    Task<(bool Success, string Message)> CreateDefaultSettingsAsync(Guid companyId, string userId);
    #endregion

    #region Schedule Management
    /// <summary>
    /// Şirketin aktif hatırlatma zamanlamalarını getirir
    /// </summary>
    Task<List<BasketReminderSchedule>> GetActiveSchedulesAsync(Guid companyId);

    /// <summary>
    /// Şirketin tüm hatırlatma zamanlamalarını getirir
    /// </summary>
    Task<List<BasketReminderSchedule>> GetAllSchedulesAsync(Guid companyId);

    /// <summary>
    /// Yeni hatırlatma zamanlaması oluşturur
    /// </summary>
    Task<int> CreateScheduleAsync(Guid companyId, string name, string message, int hours, bool isActive, bool allowResendForSentCustomers, string communicationChannels, string channelMessages, string userId);

    /// <summary>
    /// Hatırlatma zamanlamasını günceller
    /// </summary>
    Task UpdateScheduleAsync(Guid companyId, int scheduleId, string name, string message, int hours, bool isActive, bool allowResendForSentCustomers, string communicationChannels, string channelMessages, string userId);

    /// <summary>
    /// Hatırlatma zamanlamasını siler
    /// </summary>
    Task DeleteScheduleAsync(Guid companyId, int scheduleId, string userId);

    /// <summary>
    /// Hatırlatma zamanlamasının aktif/pasif durumunu değiştirir
    /// </summary>
    Task ToggleScheduleStatusAsync(Guid companyId, int scheduleId, string userId);
    #endregion

    #region Reminder Processing
    /// <summary>
    /// Belirtilen şirket için sepet hatırlatmalarını işler
    /// </summary>
    Task<(bool Success, string Message, int ProcessedCount)> ProcessRemindersForCompanyAsync(Guid companyId);

    /// <summary>
    /// Tüm aktif şirketler için sepet hatırlatmalarını işler
    /// </summary>
    Task<(bool Success, string Message, int ProcessedCompanies, int TotalReminders)> ProcessAllRemindersAsync();
    
    #endregion

    #region Validation
    /// <summary>
    /// Şirketin e-ticaret entegrasyonu ve sepet senkronizasyonu aktif mi kontrol eder
    /// </summary>
    Task<(bool IsValid, string Message)> ValidateEcommerceIntegrationAsync(Guid companyId);

    /// <summary>
    /// Sepeti e-ticaret platformunda doğrular
    /// </summary>
    Task<(bool IsValid, string Message)> ValidateBasketExistsAsync(Guid companyId, string basketExternalId);

    /// <summary>
    /// Müşterinin maksimum bildirim limitini kontrol eder (genel limit)
    /// </summary>
    Task<(bool CanSend, string Message, int SentCount)> CheckCustomerNotificationLimitAsync(Guid companyId, int customerId, int maxNotifications);

    /// <summary>
    /// Müşteri bildirim limitini schedule bazında kontrol eder
    /// </summary>
    Task<(bool CanSend, string Message, int SentCount)> CheckCustomerNotificationLimitForScheduleAsync(Guid companyId, int customerId, int scheduleId, int maxNotifications);
    #endregion

    #region Logs and Statistics
    /// <summary>
    /// Şirketin hatırlatma loglarını getirir
    /// </summary>
    Task<List<BasketReminderLog>> GetReminderLogsAsync(Guid companyId, int page = 1, int pageSize = 50);

    /// <summary>
    /// Belirli bir schedule için logları getirir (filtreleme ve sayfalama ile)
    /// </summary>
    Task<(List<BasketReminderLog> Logs, bool HasMore)> GetLogsForScheduleAsync(
        Guid companyId,
        int? scheduleId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool? isSuccessful = null,
        int page = 1,
        int pageSize = 20);

    /// <summary>
    /// Hatırlatma istatistiklerini getirir
    /// </summary>
    Task<BasketReminderStatsViewModel> GetReminderStatsAsync(Guid companyId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Hatırlatma logu oluşturur
    /// </summary>
    Task CreateReminderLogAsync(BasketReminderLog log);

    /// <summary>
    /// Hatırlatma log kaydı oluşturur (schedule ID ile)
    /// </summary>
    Task CreateReminderLogAsync(BasketReminderLog log, int? scheduleId);
    #endregion

    #region Helper Methods
    /// <summary>
    /// Bildirim içeriğindeki placeholder'ları değiştirir
    /// </summary>
    string ReplacePlaceholders(string content, string firstName, string lastName);

    /// <summary>
    /// Hatırlatma zamanı geçmiş sepetleri getirir
    /// </summary>
    Task<List<Basket>> GetEligibleBasketsForReminderAsync(Guid companyId, int reminderTimeForHours, int? scheduleId = null);

    /// <summary>
    /// Şirket için aktif iletişim kanallarını getirir
    /// </summary>
    Task<List<CommunicationChannel>> GetAvailableChannelsAsync(Guid companyId);

    /// <summary>
    /// Manuel hatırlatma gönderimi için kullanılır
    /// </summary>
    Task<(bool Success, string Message)> SendReminderWithChannelsAsync(Guid companyId, Basket basket, BasketReminderSchedule schedule);
    #endregion
}
