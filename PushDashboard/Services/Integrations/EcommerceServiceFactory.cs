using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Services.Integrations.Common;

namespace PushDashboard.Services.Integrations;

public class EcommerceServiceFactory : IEcommerceServiceFactory
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EcommerceServiceFactory> _logger;

    public EcommerceServiceFactory(
        ApplicationDbContext context,
        IServiceProvider serviceProvider,
        ILogger<EcommerceServiceFactory> logger)
    {
        _context = context;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<IEcommerceService?> GetEcommerceServiceAsync(Guid companyId)
    {
        var integrationType = await GetActiveIntegrationTypeAsync(companyId);

        if (string.IsNullOrEmpty(integrationType))
        {
            _logger.LogWarning("No active e-commerce integration found for company {CompanyId}", companyId);
            return null;
        }

        // Artık platform-agnostic IEcommerceService kullanıyoruz
        // Platform detection otomatik olarak TicimaxService içinde yapılıyor
        return _serviceProvider.GetService<IEcommerceService>();
    }

    // Bu metod artık gerekli değil - GetEcommerceServiceAsync kullanılacak
    [Obsolete("Use GetEcommerceServiceAsync instead")]
    public async Task<IEcommerceService?> GetBasketServiceAsync(Guid companyId)
    {
        return await GetEcommerceServiceAsync(companyId);
    }

    public async Task<Dictionary<string, object>?> GetActiveIntegrationSettingsAsync(Guid companyId)
    {
        var companyIntegration = await _context.CompanyIntegrations
            .Include(ci => ci.Integration)
            .Where(ci => ci.CompanyId == companyId &&
                        ci.IsActive &&
                        ci.IsConfigured &&
                        ci.Integration.Type == "ecommerce")
            .FirstOrDefaultAsync();

        if (companyIntegration == null)
        {
            _logger.LogWarning("No active e-commerce integration found for company {CompanyId}", companyId);
            return null;
        }

        return companyIntegration.Settings;
    }

    public async Task<string?> GetActiveIntegrationTypeAsync(Guid companyId)
    {
        var companyIntegration = await _context.CompanyIntegrations
            .Include(ci => ci.Integration)
            .Where(ci => ci.CompanyId == companyId &&
                        ci.IsActive &&
                        ci.IsConfigured &&
                        ci.Integration.Type == "ecommerce")
            .FirstOrDefaultAsync();

        if (companyIntegration == null)
        {
            _logger.LogWarning("No active e-commerce integration found for company {CompanyId}", companyId);
            return null;
        }

        return companyIntegration.Integration.Name;
    }
}
