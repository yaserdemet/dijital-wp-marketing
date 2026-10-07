using PushDashboard.ViewModels;

namespace PushDashboard.Services;

/// <summary>
/// Tüm modüller için ortak şablon işlemlerini yöneten servis
/// </summary>
public interface ITemplateModuleService
{
    /// <summary>
    /// Belirtilen kanal ve modül tipi için mevcut şablonları getirir
    /// </summary>
    /// <param name="companyId">Şirket ID</param>
    /// <param name="channelType">Kanal tipi (email, sms, whatsapp)</param>
    /// <param name="moduleType">Modül tipi (BulkMessaging, FirstOrder, etc.)</param>
    /// <returns>Şablon listesi</returns>
    Task<List<TemplateOptionViewModel>> GetChannelTemplatesAsync(Guid companyId, string channelType, string? moduleType);
}
