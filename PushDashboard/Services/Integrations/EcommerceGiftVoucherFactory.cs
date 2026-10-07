using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.Services.Integrations.Common;
using PushDashboard.Services.Integrations.Common.Models;
namespace PushDashboard.Services.Integrations
{
    public class EcommerceGiftVoucherFactory
    {
        private readonly ApplicationDbContext _context;
        private readonly IEcommerceServiceFactory _ecommerceServiceFactory;
        private readonly ILogger<EcommerceGiftVoucherFactory> _logger;

        public EcommerceGiftVoucherFactory(
            ApplicationDbContext context,
            IEcommerceServiceFactory ecommerceServiceFactory,
            ILogger<EcommerceGiftVoucherFactory> logger)
        {
            _context = context;
            _ecommerceServiceFactory = ecommerceServiceFactory;
            _logger = logger;
        }

        /// <summary>
        /// Şirketin aktif e-ticaret entegrasyonunu döner
        /// </summary>
        public async Task<CompanyIntegration?> GetActiveEcommerceIntegrationAsync(Guid companyId)
        {
            try
            {
                // E-ticaret kategorisindeki aktif entegrasyonları al
                var ecommerceIntegration = await _context.CompanyIntegrations
                    .Include(ci => ci.Integration)
                    .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                              ci.IsActive &&
                                              ci.IsConfigured &&
                                              ci.Integration.Type == "ecommerce");

                return ecommerceIntegration;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active e-commerce integration for company {CompanyId}", companyId);
                return null;
            }
        }

        /// <summary>
        /// Şirketin aktif e-ticaret servisi döner
        /// </summary>
        public async Task<IEcommerceService?> GetActiveEcommerceServiceAsync(Guid companyId)
        {
            try
            {
                return await _ecommerceServiceFactory.GetEcommerceServiceAsync(companyId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active e-commerce service for company {CompanyId}", companyId);
                return null;
            }
        }

        /// <summary>
        /// Şirketin hediye çeki oluşturabileceği platform adını döner
        /// </summary>
        public async Task<string?> GetActivePlatformNameAsync(Guid companyId)
        {
            var service = await GetActiveEcommerceServiceAsync(companyId);
            return service?.PlatformName;
        }

        /// <summary>
        /// Şirketin herhangi bir e-ticaret entegrasyonu var mı kontrol eder
        /// </summary>
        public async Task<bool> HasActiveEcommerceIntegrationAsync(Guid companyId)
        {
            var integration = await GetActiveEcommerceIntegrationAsync(companyId);
            return integration != null;
        }

        /// <summary>
        /// Müşteri için hediye çeki oluşturur (aktif e-ticaret platformu üzerinden)
        /// </summary>
        public async Task<GiftVoucherResult> CreateGiftVoucherAsync(Guid companyId, Customer customer, GiftVoucherSettings settings)
        {
            try
            {
                _logger.LogInformation(
                    "CreateGiftVoucherAsync: Starting. CompanyId={CompanyId}, CustomerName={CustomerName}, " +
                    "CustomerExternalId={ExternalId}, Amount={Amount}, DiscountType={DiscountType}",
                    companyId, customer.FullName, customer.ExternalId, settings.Amount, settings.DiscountType);

                var service = await GetActiveEcommerceServiceAsync(companyId);
                if (service == null)
                {
                    _logger.LogWarning("CreateGiftVoucherAsync: No active e-commerce service found for company {CompanyId}", companyId);
                    return GiftVoucherResult.CreateFailure("No active e-commerce integration found that supports gift vouchers");
                }

                _logger.LogInformation("CreateGiftVoucherAsync: Using platform {Platform}", service.PlatformName);

                if (settings.VoucherMode == "usage" && settings.VoucherCodeFormat == "fixed_prefix")
                {
                    _logger.LogWarning("CreateGiftVoucherAsync: Kullanım bazlı modda sabit ön ek kullanılamaz. CompanyId={CompanyId}",
                        companyId);
                    return GiftVoucherResult.CreateFailure("Kullanım bazlı modda sabit ön ek kullanılamaz.");
                }

                var voucherCode = GenerateVoucherCode(settings.VoucherCodeFormat, settings.VoucherPrefix);

                var isPersonalized = settings.VoucherMode == "usage";
                var customerName = isPersonalized ? customer.FullName : null;

                var ecommerceRequest = new EcommerceGiftVoucherRequest
                {
                    Name = !string.IsNullOrEmpty(settings.Description)
                        ? $"{settings.Description}"
                        : $"Hediye Çeki - {customer.FullName}",
                    DiscountType = settings.DiscountType == 1 ? "AMOUNT" : "PERCENTAGE",
                    DiscountValue = (decimal)settings.Amount,
                    CustomerName = customerName,
                    ExpiryDate = DateTime.Now.AddDays(settings.ValidityDays),
                    UsageLimit = settings.UsageLimit,
                    FreeShipping = settings.IsFreeShipping,
                    VoucherCode = voucherCode,
                    VoucherCodeFormat = settings.VoucherCodeFormat,
                    VoucherPrefix = settings.VoucherPrefix,
                    AllowDiscountedProducts = settings.AllowDiscountedProducts,
                    CombineWithOtherCampaigns = settings.CombineWithOtherCampaigns,
                    MinOrderAmount = settings.MinOrderAmount
                };

                _logger.LogInformation(
                    "CreateGiftVoucherAsync: Request built. Name={Name}, SettingsDiscountType={SettingsDiscountType}, DiscountType={DiscountType}, " +
                    "DiscountValue={DiscountValue}, CustomerId={CustomerId}, ExpiryDate={ExpiryDate}",
                    ecommerceRequest.Name, settings.DiscountType, ecommerceRequest.DiscountType, ecommerceRequest.DiscountValue,
                    ecommerceRequest.CustomerId ?? "null", ecommerceRequest.ExpiryDate);

                var response = await service.CreateGiftVoucherAsync(companyId, ecommerceRequest);

                _logger.LogInformation(
                    "CreateGiftVoucherAsync: API Response. Success={Success}, VoucherCode={VoucherCode}, Message={Message}",
                    response?.Success, response?.VoucherCode ?? "null", response?.Message ?? "null");

                if (response != null && response.Success && !string.IsNullOrEmpty(response.VoucherCode))
                {
                    _logger.LogInformation(
                        "CreateGiftVoucherAsync: Gift voucher created successfully. CustomerName={CustomerName}, " +
                        "ExternalId={ExternalId}, VoucherCode={VoucherCode}",
                        customer.FullName, customer.ExternalId, response.VoucherCode);

                    var amountText = settings.DiscountType == 1 ? $"{settings.Amount:N0} TL" : $"%{settings.Amount:N0}";
                    return GiftVoucherResult.CreateSuccess(response.VoucherCode, amountText);
                }
                else
                {
                    var errorMessage = response?.Message ?? "No response from API";
                    _logger.LogWarning(
                        "CreateGiftVoucherAsync: Failed to create gift voucher. CustomerName={CustomerName}, Error={Error}",
                        customer.FullName, errorMessage);

                    return GiftVoucherResult.CreateFailure(errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "CreateGiftVoucherAsync: Exception for company {CompanyId}", companyId);
                return GiftVoucherResult.CreateFailure($"Exception: {ex.Message}");
            }
        }

        private string? GenerateVoucherCode(string codeFormat, string? prefix)
        {
            switch (codeFormat)
            {
                case "auto_prefix":
                    var autoPrefix = string.IsNullOrEmpty(prefix)
                        ? $"{"PUSH"}-{GenerateRandomCode(6)}"
                        : prefix.ToUpperInvariant();
                    return $"{autoPrefix}-{GenerateRandomCode(6)}";

                case "fixed_prefix":
                    var fixedPrefix = string.IsNullOrEmpty(prefix) ? "PUSH" : prefix.ToUpperInvariant();
                    return $"{fixedPrefix}";

                case "full_auto":
                default:
                    return null;
            }
        }



        private static string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
