using PushDashboard.Services.Integrations.Common.Models;

namespace PushDashboard.Services.Integrations.Common.Models
{
    #region Base Models

    /// <summary>
    /// Base result model for all e-commerce operations
    /// </summary>
    public class EcommerceResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? ErrorCode { get; set; }
        public Dictionary<string, object>? AdditionalInfo { get; set; }
    }

    /// <summary>
    /// Base pagination model for e-commerce queries
    /// </summary>
    public class EcommercePagination
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public string? SortField { get; set; }
        public string? SortDirection { get; set; } = "ASC";
    }

    #endregion

    #region Customer Models

    /// <summary>
    /// Filter model for customer queries
    /// </summary>
    public class EcommerceCustomerFilter
    {
        public string? MemberId { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Name { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public bool? IsActive { get; set; }
        public string? Gender { get; set; }
        public bool? HasMadePurchase { get; set; }
        public bool? EmailPermission { get; set; }
        public bool? SmsPermission { get; set; }
        public Dictionary<string, object>? CustomFilters { get; set; }
    }

    /// <summary>
    /// Customer model for e-commerce platforms
    /// </summary>
    public class EcommerceCustomer
    {
        public string ExternalId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public bool EmailPermission { get; set; } = false;
        public bool SmsPermission { get; set; } = false;
        public Dictionary<string, object>? CustomFields { get; set; }
    }

    #endregion

    #region Order Models

    /// <summary>
    /// Filter model for order queries
    /// </summary>
    public class EcommerceOrderFilter
    {
        public string? CustomerId { get; set; }
        public string? OrderNumber { get; set; }

        public int? OrderId { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public bool? PaymentCompleted { get; set; }
        public string? Status { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public Dictionary<string, object>? CustomFilters { get; set; }
    }

    /// <summary>
    /// Order model for e-commerce platforms
    /// </summary>
    public class EcommerceOrder
    {
        public string ExternalId { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerExternalId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal KDVAmount { get; set; }
        public string Currency { get; set; } = "TRY";
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public bool PaymentCompleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal TotalTax { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? PaymentDate { get; set; }
        /// <summary>
        /// Reklam kaynağı - Ticimax'tan gelen ReklamKaynagi alanı
        /// WhatsApp template tracking GUID'i bu alanda bulunur
        /// </summary>
        public string? AdvertisingSource { get; set; }

        public string? CargoTrackingLink { get; set; }
        public string? CargoTrackingNumber { get; set; }
        public List<EcommerceOrderItem>? Items { get; set; }
        public Dictionary<string, object>? CustomFields { get; set; }
    }

    /// <summary>
    /// Order item model
    /// </summary>
    public class EcommerceOrderItem
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    #endregion

    #region Cart Models

    /// <summary>
    /// Filter model for cart queries
    /// </summary>
    public class EcommerceCartFilter
    {
        public string? CustomerId { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public bool? IsAbandoned { get; set; }
        public decimal? MinAmount { get; set; }
        public decimal? MaxAmount { get; set; }
        public Dictionary<string, object>? CustomFilters { get; set; }
    }

    /// <summary>
    /// Cart model for e-commerce platforms
    /// </summary>
    public class EcommerceCart
    {
        public string ExternalId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string? SessionId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal TotalTax { get; set; }
        public decimal ShippingCost { get; set; }
        public int ItemCount { get; set; }
        public bool IsAbandoned { get; set; }
        public int SelectedAbandonedTime { get; set; }
        public string Currency { get; set; } = "TRY";
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<EcommerceCartItem>? Items { get; set; }
        public Dictionary<string, object>? CustomFields { get; set; }
    }

    /// <summary>
    /// Cart item model
    /// </summary>
    public class EcommerceCartItem
    {
        public string ExternalId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImage { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ShippingCost { get; set; }
        public bool FreeShipping { get; set; }
        public string Currency { get; set; } = "TRY";
        public Dictionary<string, object>? CustomFields { get; set; }
    }

    #endregion

    #region Webhook Models

    /// <summary>
    /// Webhook model for e-commerce platforms
    /// </summary>
    public class EcommerceWebhook
    {
        public string Id { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, object>? Settings { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Request model for creating/updating webhooks
    /// </summary>
    public class EcommerceWebhookRequest
    {
        public string EventType { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? Password { get; set; }
        public Dictionary<string, object>? Settings { get; set; }
    }

    /// <summary>
    /// Result model for webhook operations
    /// </summary>
    public class EcommerceWebhookResult : EcommerceResult
    {
        public string? WebhookId { get; set; }
        public string? WebhookUrl { get; set; }
    }

    #endregion

    #region Product Models

    /// <summary>
    /// Filter model for product queries
    /// </summary>
    public class EcommerceProductFilter
    {
        public string? SearchTerm { get; set; }
        public string? CategoryId { get; set; }
        public string? BrandId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? IsActive { get; set; }
        public bool? InStock { get; set; }
        public DateTime? CreatedAfter { get; set; }
        public DateTime? CreatedBefore { get; set; }
        public Dictionary<string, object>? CustomFilters { get; set; }
    }

    /// <summary>
    /// Product model for e-commerce platforms
    /// </summary>
    public class EcommerceProduct
    {
        public string ExternalId { get; set; } = string.Empty;
        public string ProductCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ShortDescription { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal? KDVAmount { get; set; }
        public double TaxRate { get; set; } = 18;
        public bool KdvIncluded { get; set; } = true;
        public string Currency { get; set; } = "TRY";
        public string? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? BrandId { get; set; }
        public string? BrandName { get; set; }
        public string? SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public List<string>? Tags { get; set; }
        public string? MainImage { get; set; }
        public List<string>? Images { get; set; }
        public List<EcommerceProductVariant>? Variants { get; set; }
        public int StockQuantity { get; set; }
        public bool InStock { get; set; }
        public bool IsActive { get; set; }
        public string? ProductUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Dictionary<string, object>? CustomFields { get; set; }
    }

    /// <summary>
    /// Product variant model for e-commerce platforms
    /// </summary>
    public class EcommerceProductVariant
    {
        public string ExternalId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string VariantCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
        public string? Barcode { get; set; }
        public string? Currency { get; set; }
        public Dictionary<string, object>? CustomFields { get; set; }
    }

    #endregion

    #region Gift Voucher Models

    /// <summary>
    /// Request model for creating gift vouchers
    /// </summary>
    public class EcommerceGiftVoucherRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string DiscountType { get; set; } = "AMOUNT"; // AMOUNT, PERCENTAGE
        public decimal DiscountValue { get; set; }
        public bool FreeShipping { get; set; }
        public string? Currency { get; set; } = "TRY";
        public string? CustomerId { get; set; }
        /// <summary>
        /// Müşteri adı - Ticimax'ta UyeAdi alanına yazılır
        /// </summary>
        public string? CustomerName { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public decimal? MinOrderAmount { get; set; }
        public int? UsageLimit { get; set; }
        /// <summary>
        /// Önceden belirlenmiş hediye çeki kodu - Ticimax'ta bu kod kullanılır
        /// </summary>
        public string? VoucherCode { get; set; }
        /// <summary>
        /// Kupon kodu formatı: auto_prefix, fixed_prefix, full_auto
        /// </summary>
        public string VoucherCodeFormat { get; set; } = "full_auto";
        /// <summary>
        /// Kupon kodu ön eki
        /// </summary>
        public string? VoucherPrefix { get; set; }
        /// <summary>
        /// İndirimli ürünlerde geçerli mi
        /// </summary>
        public bool AllowDiscountedProducts { get; set; } = true;
        /// <summary>
        /// Başka kampanyalarla birleştirilebilir mi
        /// </summary>
        public bool CombineWithOtherCampaigns { get; set; } = false;
        public Dictionary<string, object>? CustomFields { get; set; }
    }

    /// <summary>
    /// Result model for gift voucher creation
    /// </summary>
    public class EcommerceGiftVoucherResult : EcommerceResult
    {
        public string? VoucherCode { get; set; }
        public string? VoucherId { get; set; }
    }

    #endregion

    #region Gift Voucher Factory Models

    /// <summary>
    /// Settings for creating gift vouchers
    /// </summary>
    public class GiftVoucherSettings
    {
        public double Amount { get; set; }
        public int DiscountType { get; set; } // 1: Sabit tutar, 2: Yüzde
        public int ValidityDays { get; set; }
        public string Description { get; set; } = string.Empty;
        public int UsageLimit { get; set; } = 1;
        public bool IsFreeShipping { get; set; }
        public string VoucherCodeFormat { get; set; } = "full_auto"; // auto_prefix, fixed_prefix, full_auto
        public string? VoucherPrefix { get; set; }
        public string VoucherMode { get; set; } = "duration"; // duration: Süre bazlı (herkese açık), usage: Kullanım bazlı (kişiye özel)
        public bool AllowDiscountedProducts { get; set; } = true;
        public bool CombineWithOtherCampaigns { get; set; } = false;
        public decimal? MinOrderAmount { get; set; }
    }

    /// <summary>
    /// Result for gift voucher creation
    /// </summary>
    public class GiftVoucherResult
    {
        public bool Success { get; set; }
        public string? VoucherCode { get; set; }
        public string? Amount { get; set; }
        public string? ErrorMessage { get; set; }

        public static GiftVoucherResult CreateSuccess(string voucherCode, string amount)
        {
            return new GiftVoucherResult
            {
                Success = true,
                VoucherCode = voucherCode,
                Amount = amount
            };
        }

        public static GiftVoucherResult CreateFailure(string errorMessage)
        {
            return new GiftVoucherResult
            {
                Success = false,
                ErrorMessage = errorMessage
            };
        }
    }

    #endregion

    #region Connection Test Models

    /// <summary>
    /// Result model for connection testing
    /// </summary>
    public class EcommerceConnectionResult : EcommerceResult
    {
        public string? PlatformName { get; set; }
        public string? PlatformVersion { get; set; }
        public Dictionary<string, object>? ConnectionInfo { get; set; }
    }

    #endregion
}
