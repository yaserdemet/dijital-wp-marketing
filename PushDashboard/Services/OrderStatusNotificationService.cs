using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.Services.Notifications;
using PushDashboard.Services.Integrations;
using PushDashboard.Services.Integrations.Common;
using PushDashboard.Services.Integrations.Common.Models;
using System.Text.Json;

namespace PushDashboard.Services;

public interface IOrderStatusNotificationService
{
    Task ProcessOrderStatusNotificationAsync(Guid companyId, OrderStatusChangeLog orderStatusChange);
    Task<bool> SendTestNotificationAsync(Guid companyId, string orderStatus, string testEmail, string? testPhone, string testOrderId, string? testCustomerName, List<string> testChannels);
}

public class OrderStatusNotificationService : IOrderStatusNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationChannelFactory _notificationChannelFactory;
    private readonly IEcommerceServiceFactory _ecommerceServiceFactory;
    private readonly ILogger<OrderStatusNotificationService> _logger;

    public OrderStatusNotificationService(
        ApplicationDbContext context,
        INotificationChannelFactory notificationChannelFactory,
        IEcommerceServiceFactory ecommerceServiceFactory,
        ILogger<OrderStatusNotificationService> logger)
    {
        _context = context;
        _notificationChannelFactory = notificationChannelFactory;
        _ecommerceServiceFactory = ecommerceServiceFactory;
        _logger = logger;
    }

    public async Task ProcessOrderStatusNotificationAsync(Guid companyId, OrderStatusChangeLog orderStatusChange)
    {
        try
        {
            _logger.LogInformation("Processing order status notification for company {CompanyId}, order {OrderId}, status {Status}",
                companyId, orderStatusChange.OrderId, orderStatusChange.NewStatus);

            // Müşteri bilgilerini al (izin kontrolü için)
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.ExternalId == orderStatusChange.CustomerId);

            if (customer == null)
            {
                _logger.LogWarning("Customer not found for email {Email}. Cannot send notification without customer record.", 
                    orderStatusChange.CustomerEmail);
                return;
            }

            // Bu sipariş durumu için aktif bildirim ayarları var mı kontrol et
            var notification = await _context.OrderStatusNotifications
                .FirstOrDefaultAsync(osn => osn.CompanyId == companyId &&
                                          osn.OrderStatus == orderStatusChange.NewStatus &&
                                          osn.IsActive);

            if (notification == null)
            {
                _logger.LogInformation("No active notification settings found for order status {Status} in company {CompanyId}",
                    orderStatusChange.NewStatus, companyId);
                return;
            }

            // Gecikme varsa bekle
            if (notification.DelayMinutes > 0)
            {
                _logger.LogInformation("Delaying notification for {DelayMinutes} minutes", notification.DelayMinutes);
                await Task.Delay(TimeSpan.FromMinutes(notification.DelayMinutes));
            }

            // Order Status modülünü getir
            var orderStatusModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Sipariş Durumu Bildirimleri");
            var moduleId = orderStatusModule?.Id;

            // E-ticaret servisinden detaylı sipariş bilgilerini al
            EcommerceOrder? ecommerceOrder = null;
            try
            {
                var ecommerceService = await _ecommerceServiceFactory.GetEcommerceServiceAsync(companyId);
                ecommerceOrder = await ecommerceService.GetOrderAsync(companyId, orderStatusChange.OrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching order details for order {orderStatusChange.OrderId}");
            }

            // Template değişkenlerini hazırla
            var variables = CreateOrderVariables(orderStatusChange, ecommerceOrder);

            var sentChannels = new List<string>();
            var errors = new List<string>();
            var referenceId = $"order_status_{orderStatusChange.OrderId}";

            // Email bildirimi
            if (notification.EmailNotificationEnabled && notification.EmailTemplateId.HasValue)
            {
                var result = await SendNotificationViaChannelAsync(
                    companyId, customer, "email", 
                    await GetTemplateNameByIdAsync(notification.EmailTemplateId.Value, "email", companyId),
                    variables, moduleId, referenceId);
                
                if (result.Success) sentChannels.Add("email");
                else if (!string.IsNullOrEmpty(result.ErrorMessage)) errors.Add($"Email: {result.ErrorMessage}");
            }

            // SMS bildirimi
            if (notification.SmsNotificationEnabled && notification.SmsTemplateId.HasValue && !string.IsNullOrEmpty(orderStatusChange.CustomerPhone))
            {
                var result = await SendNotificationViaChannelAsync(
                    companyId, customer, "sms",
                    await GetTemplateNameByIdAsync(notification.SmsTemplateId.Value, "sms", companyId),
                    variables, moduleId, referenceId);
                
                if (result.Success) sentChannels.Add("sms");
                else if (!string.IsNullOrEmpty(result.ErrorMessage)) errors.Add($"SMS: {result.ErrorMessage}");
            }

            // WhatsApp bildirimi
            if (notification.WhatsAppNotificationEnabled && !string.IsNullOrWhiteSpace(notification.WhatsAppTemplateId) && !string.IsNullOrEmpty(orderStatusChange.CustomerPhone))
            {
                var result = await SendNotificationViaChannelAsync(
                    companyId, customer, "whatsapp",
                    notification.WhatsAppTemplateId, // WhatsApp uses template name directly
                    variables, moduleId, referenceId);
                
                if (result.Success) sentChannels.Add("whatsapp");
                else if (!string.IsNullOrEmpty(result.ErrorMessage)) errors.Add($"WhatsApp: {result.ErrorMessage}");
            }

            // Sonuçları kaydet
            await UpdateNotificationResultAsync(orderStatusChange, notification, sentChannels, errors);

            _logger.LogInformation("Order status notification processing completed for order {OrderId}. Sent channels: {Channels}",
                orderStatusChange.OrderId, string.Join(", ", sentChannels));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order status notification for company {CompanyId}, order {OrderId}",
                companyId, orderStatusChange.OrderId);
        }
    }

    public async Task<bool> SendTestNotificationAsync(Guid companyId, string orderStatus, string testEmail, string? testPhone, string testOrderId, string? testCustomerName, List<string> testChannels)
    {
        try
        {
            _logger.LogInformation("Sending test notification for company {CompanyId}, order status {OrderStatus}", companyId, orderStatus);

            // Test müşterisi oluştur veya bul
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.Email == testEmail);

            if (customer == null)
            {
                _logger.LogWarning("Test customer not found for email {Email}. Cannot send test notification.", testEmail);
                return false;
            }

            // Test için sahte OrderStatusChangeLog oluştur
            var testOrderStatusChange = new OrderStatusChangeLog
            {
                CompanyId = companyId,
                OrderId = testOrderId,
                OrderNumber = $"TEST-{testOrderId}",
                CustomerEmail = testEmail,
                CustomerName = testCustomerName ?? "Test Müşteri",
                CustomerPhone = testPhone,
                NewStatus = orderStatus,
                NewStatusDisplayName = GetOrderStatusDisplayName(orderStatus),
                OrderAmount = 299.99m,
                OrderCurrency = "TRY",
                StatusChangedAt = DateTime.Now
            };

            var notification = await _context.OrderStatusNotifications
                .FirstOrDefaultAsync(osn => osn.CompanyId == companyId && osn.OrderStatus == orderStatus);

            if (notification == null)
            {
                _logger.LogWarning("No notification settings found for test - order status {OrderStatus} in company {CompanyId}", orderStatus, companyId);
                return false;
            }

            // Order Status modülünü getir
            var orderStatusModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Sipariş Durumu Bildirimleri");
            var moduleId = orderStatusModule?.Id;

            // E-ticaret servisinden detaylı sipariş bilgilerini al (test için boş olabilir)
            EcommerceOrder? ecommerceOrder = null;
            try
            {
                var ecommerceService = await _ecommerceServiceFactory.GetEcommerceServiceAsync(companyId);
                ecommerceOrder = await ecommerceService.GetOrderAsync(companyId, testOrderId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error fetching order details for test order {testOrderId}. Proceeding without detailed order info.");
            }

            var variables = CreateOrderVariables(testOrderStatusChange, ecommerceOrder);
            var referenceId = $"test_order_status_{testOrderId}";
            var success = true;

            // Test kanallarına göre bildirim gönder
            if (testChannels.Contains("email") && notification.EmailNotificationEnabled && notification.EmailTemplateId.HasValue)
            {
                var templateName = await GetTemplateNameByIdAsync(notification.EmailTemplateId.Value, "email", companyId);
                var result = await SendNotificationViaChannelAsync(companyId, customer, "email", templateName, variables, moduleId, referenceId);
                if (!result.Success)
                {
                    success = false;
                    _logger.LogWarning("Test email notification failed: {Error}", result.ErrorMessage);
                }
            }

            if (testChannels.Contains("sms") && notification.SmsNotificationEnabled && notification.SmsTemplateId.HasValue && !string.IsNullOrEmpty(testPhone))
            {
                var templateName = await GetTemplateNameByIdAsync(notification.SmsTemplateId.Value, "sms", companyId);
                var result = await SendNotificationViaChannelAsync(companyId, customer, "sms", templateName, variables, moduleId, referenceId);
                if (!result.Success)
                {
                    success = false;
                    _logger.LogWarning("Test SMS notification failed: {Error}", result.ErrorMessage);
                }
            }

            if (testChannels.Contains("whatsapp") && notification.WhatsAppNotificationEnabled && !string.IsNullOrWhiteSpace(notification.WhatsAppTemplateId) && !string.IsNullOrEmpty(testPhone))
            {
                var result = await SendNotificationViaChannelAsync(companyId, customer, "whatsapp", notification.WhatsAppTemplateId, variables, moduleId, referenceId);
                if (!result.Success)
                {
                    success = false;
                    _logger.LogWarning("Test WhatsApp notification failed: {Error}", result.ErrorMessage);
                }
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending test notification for company {CompanyId}, order status {OrderStatus}", companyId, orderStatus);
            return false;
        }
    }

    private async Task<NotificationResult> SendNotificationViaChannelAsync(
        Guid companyId, 
        Customer customer, 
        string channelType, 
        string? templateName,
        Dictionary<string, string> variables,
        int? moduleId,
        string referenceId)
    {
        try
        {
            if (string.IsNullOrEmpty(templateName))
            {
                return new NotificationResult { Success = false, ErrorMessage = $"{channelType} şablonu bulunamadı." };
            }

            var channel = await _notificationChannelFactory.GetChannelAsync(companyId, channelType);
            if (channel == null)
            {
                return new NotificationResult { Success = false, ErrorMessage = $"{channelType} kanalı yapılandırılmamış." };
            }

            var sent = await channel.SendNotificationWithCostTrackingAsync(
                companyId,
                customer,
                templateName,
                variables,
                "system",
                moduleId,
                referenceId
            );

            if (sent)
            {
                _logger.LogInformation("{ChannelType} notification sent successfully via channel factory for {ReferenceId}", 
                    channelType, referenceId);
            }

            return new NotificationResult
            {
                Success = sent,
                ErrorMessage = sent ? null : $"{channelType} bildirimi gönderilemedi."
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending {ChannelType} notification via channel factory", channelType);
            return new NotificationResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<string?> GetTemplateNameByIdAsync(int templateId, string channelType, Guid companyId)
    {
        try
        {
            if (channelType.Equals("email", StringComparison.OrdinalIgnoreCase))
            {
                var template = await _context.CompanyEmailTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId && t.CompanyId == companyId);
                return template?.Name;
            }
            else if (channelType.Equals("sms", StringComparison.OrdinalIgnoreCase))
            {
                var template = await _context.CompanySmsTemplates
                    .FirstOrDefaultAsync(t => t.Id == templateId && t.CompanyId == companyId);
                return template?.Name;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template name for ID {TemplateId}, channel {ChannelType}", templateId, channelType);
            return null;
        }
    }

    private Dictionary<string, string> CreateOrderVariables(OrderStatusChangeLog orderStatusChange, EcommerceOrder? ecommerceOrder)
    {
        var variables = new Dictionary<string, string>
        {
            ["customer_name"] = orderStatusChange.CustomerName ?? "",
            ["customer_email"] = orderStatusChange.CustomerEmail ?? "",
            ["customer_phone"] = orderStatusChange.CustomerPhone ?? "",
            ["order_number"] = orderStatusChange.OrderNumber ?? orderStatusChange.OrderId,
            ["order_status"] = orderStatusChange.NewStatusDisplayName ?? orderStatusChange.NewStatus ?? "",
            ["order_total"] = orderStatusChange.OrderAmount?.ToString("N2") ?? "",
            ["order_currency"] = orderStatusChange.OrderCurrency ?? "TRY",
        };

        if (ecommerceOrder != null)
        {
            variables["order_payment_status"] = ecommerceOrder.PaymentStatus ?? "";
            variables["order_date"] = ecommerceOrder.OrderDate?.ToString("dd.MM.yyyy HH:mm") ?? "";
            variables["payment_date"] = ecommerceOrder.PaymentDate?.ToString("dd.MM.yyyy HH:mm") ?? "";
            variables["advertising_source"] = ecommerceOrder.AdvertisingSource ?? "";
            variables["cargo_tracking_link"] = ecommerceOrder.CargoTrackingLink ?? "";
            variables["cargo_tracking_number"] = ecommerceOrder.CargoTrackingNumber ?? "";

            variables["shipping_cost"] = ecommerceOrder.ShippingCost.ToString("N2");
            variables["total_tax"] = ecommerceOrder.TotalTax.ToString("N2");
        }
        else
        {
            // Fallback default values for new variables
            variables["order_payment_status"] = "";
            variables["order_date"] = "";
            variables["payment_date"] = "";
            variables["shipping_cost"] = "";
            variables["total_tax"] = "";
            variables["advertising_source"] = "";
            variables["cargo_tracking_link"] = "";
            variables["cargo_tracking_number"] = "";
        }

        return variables;
    }

    private async Task UpdateNotificationResultAsync(OrderStatusChangeLog orderStatusChange, OrderStatusNotification notification, List<string> sentChannels, List<string> errors)
    {
        try
        {
            // OrderStatusChangeLog'u güncelle
            orderStatusChange.NotificationSent = sentChannels.Any();
            orderStatusChange.NotificationSentAt = sentChannels.Any() ? DateTime.Now : null;
            orderStatusChange.NotificationChannels = sentChannels.Any() ? JsonSerializer.Serialize(sentChannels) : null;
            orderStatusChange.NotificationError = errors.Any() ? string.Join("; ", errors) : null;

            // OrderStatusNotification istatistiklerini güncelle
            if (sentChannels.Any())
            {
                notification.TotalNotificationsSent++;
                notification.LastNotificationAt = DateTime.Now;
            }
            notification.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification result for order {OrderId}", orderStatusChange.OrderId);
        }
    }

    private string? GetOrderStatusDisplayName(string? status)
    {
        if (string.IsNullOrEmpty(status))
            return null;

        return status switch
        {
            "1" => "Beklemede",
            "2" => "Onaylandı",
            "3" => "Hazırlanıyor",
            "4" => "Kargoya Verildi",
            "5" => "Teslim Edildi",
            "6" => "İptal Edildi",
            "7" => "İade Edildi",
            _ => status
        };
    }
}

public class NotificationResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
