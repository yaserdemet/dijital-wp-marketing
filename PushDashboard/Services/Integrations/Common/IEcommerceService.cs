using PushDashboard.Services.Integrations.Common.Models;

namespace PushDashboard.Services.Integrations.Common
{
    /// <summary>
    /// Global e-commerce platform service interface
    /// Platform-agnostic methods for all e-commerce integrations
    /// </summary>
    public interface IEcommerceService
    {
        /// <summary>
        /// Platform identifier (e.g., "Ticimax", "Shopify", "IKAS")
        /// </summary>
        string PlatformName { get; }

        #region Webhook Operations

        /// <summary>
        /// Gets existing webhooks for a company
        /// </summary>
        Task<EcommerceWebhook[]> GetWebhooksAsync(Guid companyId);

        /// <summary>
        /// Creates or updates a webhook
        /// </summary>
        Task<EcommerceWebhookResult> SaveWebhookAsync(Guid companyId, EcommerceWebhookRequest request);

        /// <summary>
        /// Deletes a webhook
        /// </summary>
        Task<EcommerceResult> DeleteWebhookAsync(Guid companyId, string webhookId);

        #endregion

        #region Customer Operations

        /// <summary>
        /// Gets customers with filtering and pagination
        /// </summary>
        Task<EcommerceCustomer[]> GetCustomersAsync(Guid companyId, EcommerceCustomerFilter filter, EcommercePagination pagination);

        /// <summary>
        /// Gets a specific customer by external ID
        /// </summary>
        Task<EcommerceCustomer?> GetCustomerAsync(Guid companyId, string externalCustomerId);

        #endregion

        #region Order Operations

        /// <summary>
        /// Gets orders with filtering and pagination
        /// </summary>
        Task<EcommerceOrder[]> GetOrdersAsync(Guid companyId, EcommerceOrderFilter filter, EcommercePagination pagination);

        /// <summary>
        /// Gets a specific order by external ID
        /// </summary>
        Task<EcommerceOrder?> GetOrderAsync(Guid companyId, string externalOrderId);

        #endregion

        #region Cart/Basket Operations

        /// <summary>
        /// Gets shopping carts/baskets with filtering and pagination
        /// </summary>
        Task<EcommerceCart[]> GetCartsAsync(Guid companyId, EcommerceCartFilter filter, EcommercePagination pagination);

        /// <summary>
        /// Validates if a basket/cart exists on the platform
        /// </summary>
        Task<bool> ValidateBasketExistsAsync(Guid companyId, string basketExternalId);

        #endregion

        #region Product Operations

        /// <summary>
        /// Gets products with filtering and pagination
        /// </summary>
        Task<EcommerceProduct[]> GetProductsAsync(Guid companyId, EcommerceProductFilter filter, EcommercePagination pagination);

        /// <summary>
        /// Gets a specific product by external ID
        /// </summary>
        Task<EcommerceProduct?> GetProductAsync(Guid companyId, string externalProductId);

        Task<EcommerceProductVariant?> GetVariationAsync(Guid companyId, string externalProductId);

        #endregion

        #region Gift Voucher Operations

        /// <summary>
        /// Creates a gift voucher/coupon
        /// </summary>
        Task<EcommerceGiftVoucherResult> CreateGiftVoucherAsync(Guid companyId, EcommerceGiftVoucherRequest request);

        #endregion

        #region Connection Test

        /// <summary>
        /// Tests the connection to the e-commerce platform
        /// </summary>
        Task<EcommerceConnectionResult> TestConnectionAsync(Guid companyId);

        #endregion

        #region Cache Management

        /// <summary>
        /// Invalidates product cache for a specific company
        /// Should be called when products are added/updated/deleted
        /// </summary>
        void InvalidateProductCache(Guid companyId);

        /// <summary>
        /// Invalidates all cache (products, suppliers, tags, etc.) for a specific company
        /// </summary>
        void InvalidateAllCache(Guid companyId, string? apiKey = null);

        #endregion
    }
}
