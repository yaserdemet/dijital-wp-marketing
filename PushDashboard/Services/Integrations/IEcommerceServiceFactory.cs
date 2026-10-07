using PushDashboard.Services.Integrations.Common;

namespace PushDashboard.Services.Integrations;

public interface IEcommerceServiceFactory
{
    Task<IEcommerceService?> GetEcommerceServiceAsync(Guid companyId);
    Task<Dictionary<string, object>?> GetActiveIntegrationSettingsAsync(Guid companyId);
    Task<string?> GetActiveIntegrationTypeAsync(Guid companyId);

    // Legacy methods - deprecated
    [Obsolete("Use GetEcommerceServiceAsync instead")]
    Task<IEcommerceService?> GetBasketServiceAsync(Guid companyId);
}
