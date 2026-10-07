using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.ViewModels;

namespace PushDashboard.Services.BulkMessaging;

public interface IBulkMessagingService
{
    /// <summary>
    /// Şirketin toplu mesaj gönderimi modülüne sahip olup olmadığını kontrol eder
    /// </summary>
    Task<bool> HasBulkMessagingModuleAsync(Guid companyId);

    /// <summary>
    /// Şirketin toplu mesaj gönderimi modülü ayarlarını getirir
    /// </summary>
    Task<Dictionary<string, object>?> GetModuleSettingsAsync(Guid companyId);

    /// <summary>
    /// Modül ayarlarından aktif kanalları getirir
    /// </summary>
    Task<List<string>> GetEnabledChannelsAsync(Guid companyId);

    /// <summary>
    /// Modül ayarlarını günceller
    /// </summary>
    Task<(bool Success, string Message)> UpdateModuleSettingsAsync(Guid companyId, Dictionary<string, object> settings, string userId);

    /// <summary>
    /// Şirketin kullanabileceği iletişim kanallarını getirir
    /// </summary>
    Task<List<AvailableChannelViewModel>> GetAvailableChannelsAsync(Guid companyId);

    /// <summary>
    /// Müşteri filtrelerine göre hedef müşteri sayısını hesaplar
    /// </summary>
    Task<int> EstimateRecipientsAsync(Guid companyId, CustomerFilterViewModel filters);

    /// <summary>
    /// Toplu mesaj önizlemesi oluşturur
    /// </summary>
    Task<BulkMessagePreviewViewModel> CreatePreviewAsync(Guid companyId, CreateBulkMessageViewModel model);

    /// <summary>
    /// Toplu mesaj gönderimi oluşturur
    /// </summary>
    Task<(bool Success, string Message, int? BulkMessageId)> CreateBulkMessageAsync(Guid companyId, string userId, CreateBulkMessageViewModel model);

    /// <summary>
    /// Toplu mesaj gönderimini başlatır (background job)
    /// </summary>
    Task<(bool Success, string Message)> StartBulkMessageAsync(int bulkMessageId, string userId);

    /// <summary>
    /// Toplu mesaj gönderimini iptal eder
    /// </summary>
    Task<(bool Success, string Message)> CancelBulkMessageAsync(int bulkMessageId, string userId);

    /// <summary>
    /// Toplu mesaj gönderim durumunu getirir
    /// </summary>
    Task<BulkMessageProgressViewModel?> GetProgressAsync(int bulkMessageId);

    /// <summary>
    /// Toplu mesaj detaylarını getirir
    /// </summary>
    Task<BulkMessageDetailViewModel?> GetBulkMessageDetailsAsync(int bulkMessageId, Guid companyId);

    /// <summary>
    /// Şirketin toplu mesaj geçmişini getirir
    /// </summary>
    Task<List<BulkMessageViewModel>> GetBulkMessageHistoryAsync(Guid companyId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Toplu mesaj istatistiklerini getirir
    /// </summary>
    Task<BulkMessagingStatsViewModel> GetStatsAsync(Guid companyId);

    /// <summary>
    /// Toplu mesaj gönderimini işler (background job için)
    /// </summary>
    Task ProcessBulkMessageAsync(int bulkMessageId);

    /// <summary>
    /// Müşteri filtrelerine göre müşteri listesini getirir
    /// </summary>
    Task<List<Customer>> GetFilteredCustomersAsync(Guid companyId, CustomerFilterViewModel filters);

    /// <summary>
    /// Belirtilen müşteriler için mevcut iletişim kanallarını kontrol eder
    /// </summary>
    Task<Dictionary<int, List<string>>> GetCustomerAvailableChannelsAsync(Guid companyId, List<int> customerIds, List<string> requestedChannels);

    /// <summary>
    /// Toplu mesaj maliyetini hesaplar
    /// </summary>
    Task<decimal> CalculateCostAsync(Guid companyId, List<string> channels, int recipientCount);

    /// <summary>
    /// Şirketin bakiyesinin yeterli olup olmadığını kontrol eder
    /// </summary>
    Task<(bool CanAfford, decimal CurrentBalance, decimal RequiredAmount)> CheckBalanceAsync(Guid companyId, decimal requiredAmount);

    /// <summary>
    /// Tamamlanmış bir toplu mesajın belirli bir aralığındaki alıcılara tekrar gönderim yapar
    /// </summary>
    /// <param name="originalBulkMessageId">Orijinal toplu mesaj ID'si</param>
    /// <param name="startIndex">Başlangıç sırası (1-indexed)</param>
    /// <param name="count">Gönderilecek alıcı sayısı</param>
    /// <param name="companyId">Şirket ID'si</param>
    /// <param name="userId">Kullanıcı ID'si</param>
    Task<(bool Success, string Message, int? BulkMessageId)> ResendBulkMessageAsync(
        int originalBulkMessageId, int startIndex, int count, Guid companyId, string userId);

}
