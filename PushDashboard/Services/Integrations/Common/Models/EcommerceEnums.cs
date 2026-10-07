namespace PushDashboard.Services.Integrations.Common.Models
{
    /// <summary>
    /// Standard e-commerce webhook event types
    /// </summary>
    public enum EcommerceWebhookEventType
    {
        // Order Events
        OrderCreated,
        OrderUpdated,
        OrderStatusChanged,
        OrderCancelled,
        OrderCompleted,
        OrderPaid,
        OrderShipped,
        OrderDelivered,
        OrderRefunded,

        // Customer Events
        CustomerCreated,
        CustomerUpdated,
        CustomerDeleted,
        CustomerFavoriteProductAdded,
        CustomerFavoriteProductRemoved,

        // Product Events
        ProductCreated,
        ProductUpdated,
        ProductDeleted,
        ProductStockChanged,

        // Cart Events
        CartCreated,
        CartUpdated,
        CartAbandoned,

        // Payment Events
        PaymentCompleted,
        PaymentFailed,
        PaymentRefunded,

        // Other Events
        CouponUsed,
        ReviewCreated,
        InventoryUpdated
    }

    /// <summary>
    /// Standard discount types for gift vouchers
    /// </summary>
    public enum EcommerceDiscountType
    {
        Amount,
        Percentage
    }

    /// <summary>
    /// Standard order statuses
    /// </summary>
    public enum EcommerceOrderStatus
    {
        Pending,
        Processing,
        Shipped,
        Delivered,
        Cancelled,
        Refunded,
        Completed
    }

    /// <summary>
    /// Standard customer genders
    /// </summary>
    public enum EcommerceGender
    {
        Male,
        Female,
        Other,
        NotSpecified
    }

    /// <summary>
    /// Standard sort directions
    /// </summary>
    public enum EcommerceSortDirection
    {
        Ascending,
        Descending
    }
}
