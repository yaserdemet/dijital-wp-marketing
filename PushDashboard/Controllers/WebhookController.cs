using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.Services.Integrations;
using PushDashboard.Services;
using System.Text.Json;
using PushDashboard.Services.Modules.FirstOrder;
using PushDashboard.Services.Modules.GiftWheel;
using PushDashboard.Services.Modules.StockReminder;
using PushDashboard.DTOs;
using PushDashboard.Services.Integrations.Common.Models;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace PushDashboard.Controllers;

[ApiController]
[Route("webhook")]
public class WebhookController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<WebhookController> _logger;
    private readonly IEcommerceWebhookFactory _webhookFactory;
    private readonly IFirstOrderModuleService _firstOrderModuleService;
    private readonly IOrderStatusNotificationService _orderStatusNotificationService;
    private readonly IOrderStatusMappingService _orderStatusMappingService;
    private readonly ICommentService _commentService;
    private readonly ITrendyolScraperService _trendyolScraperService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IProductSliderService _productSliderService;
    private readonly IEcommerceServiceFactory _ecommerceServiceFactory;
    private readonly IGiftWheelService _giftWheelService;
    private readonly IStockReminderModuleService _stockReminderModuleService;

    public WebhookController(
        ApplicationDbContext context,
        ILogger<WebhookController> logger,
        IEcommerceWebhookFactory webhookFactory,
        IFirstOrderModuleService firstOrderModuleService,
        ICommentService commentService,
        IOrderStatusNotificationService orderStatusNotificationService,
        IOrderStatusMappingService orderStatusMappingService,
        ITrendyolScraperService trendyolScraperService,
        IServiceScopeFactory serviceScopeFactory,
        IProductSliderService productSliderService,
        IEcommerceServiceFactory ecommerceServiceFactory,
        IStockReminderModuleService stockReminderModuleService,
        IGiftWheelService giftWheelService)
    {
        _context = context;
        _logger = logger;
        _webhookFactory = webhookFactory;
        _firstOrderModuleService = firstOrderModuleService;
        _orderStatusNotificationService = orderStatusNotificationService;
        _orderStatusMappingService = orderStatusMappingService;
        _commentService = commentService;
        _trendyolScraperService = trendyolScraperService;
        _serviceScopeFactory = serviceScopeFactory;
        _productSliderService = productSliderService;
        _ecommerceServiceFactory = ecommerceServiceFactory;
        _stockReminderModuleService = stockReminderModuleService;
        _giftWheelService = giftWheelService;
    }

    /// <summary>
    /// Genel e-ticaret webhook endpoint'i
    /// </summary>
    [HttpPost("{platform}/{companyId}/{webhookType}")]
    public async Task<IActionResult> HandleEcommerceWebhook(string platform, Guid companyId, string webhookType)
    {
        JsonElement payload;

        var contentType = Request.ContentType ?? "";

        if (contentType.Contains("application/json"))
        {
            payload = await JsonSerializer.DeserializeAsync<JsonElement>(Request.Body);
        }
        else
        {
            var form = await Request.ReadFormAsync();

            if (!form.TryGetValue("data", out var data))
                return BadRequest("data not found");

            payload = JsonSerializer.Deserialize<JsonElement>(data!);
        }

        _logger.LogInformation("Received {Platform} webhook {WebhookType} for company {CompanyId}: {Payload}",
            platform,
            webhookType,
            companyId,
            payload.ToString());

        // Platform destekleniyor mu kontrol et
        if (!_webhookFactory.IsPlatformSupported(platform))
        {
            _logger.LogWarning("Unsupported platform {Platform} for webhook", platform);
            return BadRequest("Desteklenmeyen platform");
        }

        // Şirketin bu platform entegrasyonu var mı kontrol et
        var companyIntegration = await _context.CompanyIntegrations
            .Include(ci => ci.Integration)
            .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                       ci.Integration.Name.ToLower() == platform.ToLower() &&
                                       ci.Integration.Type == "ecommerce" &&
                                       ci.IsActive && ci.IsConfigured);

        if (companyIntegration == null)
        {
            _logger.LogWarning("No active {Platform} integration found for company {CompanyId}", platform, companyId);
            return BadRequest("Entegrasyon bulunamadı");
        }

        // Webhook'u işle
        await ProcessWebhookByType(platform, companyId, webhookType, payload);

        return Ok(new { success = true, message = "Webhook işlendi" });
    }

    private async Task ProcessWebhookByType(string platform, Guid companyId, string webhookType, JsonElement payload)
    {
        // Platform ve webhook türüne göre işleme yönlendir
        if (platform.Equals("Ticimax", StringComparison.OrdinalIgnoreCase))
        {
            await ProcessTicimaxWebhook(companyId, webhookType, payload);
        }
        // Gelecekte diğer platformlar buraya eklenecek
        // else if (platform.Equals("IKAS", StringComparison.OrdinalIgnoreCase))
        // {
        //     await ProcessIkasWebhook(companyId, webhookType, payload);
        // }
        else
        {
            _logger.LogWarning("Webhook processing not implemented for platform {Platform}", platform);
            throw new NotImplementedException($"Webhook işleme {platform} platformu için henüz uygulanmadı");
        }
    }

    private async Task ProcessTicimaxWebhook(Guid companyId, string webhookType, JsonElement payload)
    {
        switch (webhookType)
        {
            case nameof(EcommerceWebhookEventType.CustomerCreated):
                await ProcessCustomerRegistrationWebhook(companyId, payload);
                break;
            case nameof(EcommerceWebhookEventType.CustomerUpdated):
                await ProcessCustomerUpdateWebhook(companyId, payload);
                break;
            case nameof(EcommerceWebhookEventType.OrderCreated):
                await ProcessOrderCreationWebhook(companyId, payload);
                await ProcessOrderConversionWebhook(companyId, payload);
                break;
            case nameof(EcommerceWebhookEventType.OrderStatusChanged):
                await ProcessOrderStatusChangeWebhook(companyId, payload);
                break;
            case nameof(EcommerceWebhookEventType.CustomerFavoriteProductAdded):
                await ProcessFavoriteProductAddedWebhook(companyId, payload);
                break;
            case nameof(EcommerceWebhookEventType.CustomerFavoriteProductRemoved):
                await ProcessFavoriteProductRemovedWebhook(companyId, payload);
                break;
            case nameof(EcommerceWebhookEventType.ProductUpdated):
                await ProcessProductUpdatedWebhook(companyId, payload);
                break;
            case nameof(EcommerceWebhookEventType.ProductStockChanged):
                await ProcessProductStockChangedWebhook(companyId, payload);
                break;
            default:
                _logger.LogWarning("Unknown Ticimax webhook type: {WebhookType}", webhookType);
                throw new ArgumentException($"Bilinmeyen webhook türü: {webhookType}");
        }
    }

    private async Task ProcessCustomerRegistrationWebhook(Guid companyId, JsonElement payload)
    {
        // Ticimax'tan gelen müşteri verilerini parse et
        if (payload.TryGetProperty("UyeID", out var uyeIdElement) && uyeIdElement.TryGetInt32(out var uyeId))
        {
            // Müşteri zaten sistemde var mı kontrol et
            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.ExternalId == uyeId && c.CompanyId == companyId);

            if (existingCustomer == null)
            {
                // Ticimax API'sinden müşteri bilgilerini al (izinler dahil)
                var (emailPermission, smsPermission) = await GetCustomerPermissionsFromApiAsync(companyId, uyeId);

                // Yeni müşteri oluştur
                var customer = new Customer
                {
                    CompanyId = companyId,
                    ExternalId = uyeId,
                    FirstName = GetStringProperty(payload, "Ad") ?? "",
                    LastName = GetStringProperty(payload, "Soyad") ?? "",
                    Email = GetStringProperty(payload, "Mail") ?? "",
                    Phone = GetStringProperty(payload, "GSM") ?? "",
                    EmailPermission = emailPermission,
                    SmsPermission = smsPermission,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    MembershipDate =  DateTime.Now,
                    LastUpdateDate = DateTime.Now,
                    IsActive = true
                };

                // Doğum tarihi varsa ekle
                if (payload.TryGetProperty("DogumTarihi", out var dogumTarihiElement) &&
                    dogumTarihiElement.TryGetDateTime(out var dogumTarihi))
                {
                    customer.BirthDate = dogumTarihi;
                }

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();

                _logger.LogInformation("New customer created from webhook: {CustomerId} (ExternalId: {ExternalId}), EmailPermission: {EmailPermission}, SmsPermission: {SmsPermission}",
                    customer.Id,
                    customer.ExternalId,
                    emailPermission,
                    smsPermission);
            }
            else
            {
                _logger.LogInformation("Customer already exists: {CustomerId} (ExternalId: {ExternalId})",
                    existingCustomer.Id,
                    existingCustomer.ExternalId);
            }
        }
    }

    private async Task ProcessCustomerUpdateWebhook(Guid companyId, JsonElement payload)
    {

        // Ticimax'tan gelen müşteri güncelleme verilerini parse et
        if (payload.TryGetProperty("UyeID", out var uyeIdElement) && uyeIdElement.TryGetInt32(out var uyeId))
        {
            var existingCustomer = await _context.Customers
                .FirstOrDefaultAsync(c => c.ExternalId == uyeId && c.CompanyId == companyId);

            if (existingCustomer != null)
            {
                // Ticimax API'sinden müşteri bilgilerini al (izinler dahil)
                var (emailPermission, smsPermission) = await GetCustomerPermissionsFromApiAsync(companyId, uyeId);

                // Müşteri bilgilerini güncelle
                existingCustomer.FirstName = GetStringProperty(payload, "Ad") ?? existingCustomer.FirstName;
                existingCustomer.LastName = GetStringProperty(payload, "Soyad") ?? existingCustomer.LastName;
                existingCustomer.Email = GetStringProperty(payload, "Mail") ?? existingCustomer.Email;
                existingCustomer.Phone = GetStringProperty(payload, "GSM") ?? existingCustomer.Phone;
                existingCustomer.EmailPermission = emailPermission;
                existingCustomer.SmsPermission = smsPermission;
                existingCustomer.UpdatedAt = DateTime.Now;

                // Doğum tarihi varsa güncelle
                if (payload.TryGetProperty("DogumTarihi", out var dogumTarihiElement) &&
                    dogumTarihiElement.TryGetDateTime(out var dogumTarihi))
                {
                    existingCustomer.BirthDate = dogumTarihi;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Customer updated from webhook: {CustomerId} (ExternalId: {ExternalId}), EmailPermission: {EmailPermission}, SmsPermission: {SmsPermission}",
                    existingCustomer.Id,
                    existingCustomer.ExternalId,
                    emailPermission,
                    smsPermission);
            }
            else
            {
                _logger.LogWarning(
                    "Customer not found for update webhook: ExternalId {ExternalId}, Company {CompanyId}",
                    uyeId,
                    companyId);
            }
        }
    }

    private async Task ProcessOrderConversionWebhook(Guid companyId, JsonElement payload)
    {
        try
        {
            var memberIdElement = payload.TryGetProperty("UyeID", out var uyeIdElement);
            var hasMemberId = uyeIdElement.TryGetInt32(out var uyeId);
            var orderId = GetIntProperty(payload, "ID")?.ToString();
            var orderNumber = GetStringProperty(payload, "SiparisNo");
            // Use OrderId (ID) if available, otherwise OrderNumber
            var effectiveOrderId = orderId ?? orderNumber;

            if (!string.IsNullOrEmpty(effectiveOrderId))
            {
                var ecommerceService = await _ecommerceServiceFactory.GetEcommerceServiceAsync(companyId);
                if (ecommerceService != null)
                {
                    // Ticimax fetches by ID (ExternalId) usually
                    var order = await ecommerceService.GetOrderAsync(companyId, effectiveOrderId);
                    if (order != null)
                    {
                        var conversion = new OrderConversion
                        {
                            CompanyId = companyId,
                            OrderId = effectiveOrderId,
                            MemberId = hasMemberId ? uyeId.ToString() : "",
                            TotalAmount = order.TotalAmount,
                            Currency = order.Currency,
                            ConversionSource = order.AdvertisingSource,
                            CreatedAt = DateTime.Now
                        };

                        _context.OrderConversions.Add(conversion);
                        await _context.SaveChangesAsync();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing order conversion tracking for company {CompanyId}", companyId);
        }
    }

    private async Task ProcessOrderCreationWebhook(Guid companyId, JsonElement payload)
    {
        _logger.LogInformation("Processing order creation webhook for company {CompanyId}: {Payload}",
            companyId,
            payload.ToString());

        // Sipariş bilgilerini parse et
        if (payload.TryGetProperty("UyeID", out var uyeIdElement) && uyeIdElement.TryGetInt32(out var uyeId))
        {
            // Müşteri sistemde var mı kontrol et
            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.ExternalId == uyeId && c.CompanyId == companyId);

            if (customer != null)
            {
                _logger.LogInformation("Found customer {CustomerId} (ExternalId: {ExternalId}) for order webhook",
                    customer.Id,
                    customer.ExternalId);

                // İlk sipariş kontrolü ve bildirim gönderimi
                var isFirstOrder = await _firstOrderModuleService.IsFirstOrderAsync(companyId, customer.Id);

                if (isFirstOrder)
                {
                    _logger.LogInformation(
                        "This is the first order for customer {CustomerId}, sending first order notification",
                        customer.Id);

                    // İlk alışveriş mesajı gönder
                    var (success, message, notificationsSent, totalCost) = await _firstOrderModuleService
                        .SendFirstOrderNotificationAsync(companyId, customer.Id, "system");

                    if (success)
                    {
                        _logger.LogInformation(
                            "First order notification sent successfully for customer {CustomerId}: {Message}. " +
                            "Notifications sent: {NotificationsSent}, Total cost: {TotalCost:C}",
                            customer.Id,
                            message,
                            notificationsSent,
                            totalCost);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Failed to send first order notification for customer {CustomerId}: {Message}",
                            customer.Id,
                            message);
                    }
                }
                else
                {
                    _logger.LogInformation(
                        "Customer {CustomerId} has previous orders, skipping first order notification",
                        customer.Id);
                }

                // Müşterinin son alışveriş tarihini güncelle
                customer.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
            else
            {
                _logger.LogWarning(
                    "Customer with ExternalId {ExternalId} not found in company {CompanyId} for order webhook",
                    uyeId,
                    companyId);
            }
        }
        else
        {
            _logger.LogWarning("UyeID not found in order creation webhook payload for company {CompanyId}", companyId);
        }

        // GiftWheel kupon kullanım kontrolü
        var effectiveOrderId = GetIntProperty(payload, "ID")?.ToString() ?? GetStringProperty(payload, "SiparisNo");
        if (!string.IsNullOrEmpty(effectiveOrderId))
        {
            _ = Task.Run(async () =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedGiftWheelService = scope.ServiceProvider.GetRequiredService<IGiftWheelService>();
                var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<WebhookController>>();

                try
                {
                    var (success, message) = await scopedGiftWheelService.ProcessOrderForVoucherUsageAsync(companyId, effectiveOrderId);
                    scopedLogger.LogInformation(
                        "GiftWheel voucher check for order {OrderId} in company {CompanyId}: {Success} - {Message}",
                        effectiveOrderId, companyId, success, message);
                }
                catch (Exception ex)
                {
                    scopedLogger.LogError(ex,
                        "Error processing GiftWheel voucher for order {OrderId} in company {CompanyId}",
                        effectiveOrderId, companyId);
                }
            });
        }
    }

    private async Task ProcessOrderStatusChangeWebhook(Guid companyId, JsonElement payload)
    {
        _logger.LogInformation("Processing order status change webhook for company {CompanyId}: {Payload}",
            companyId,
            payload.ToString());

        // Sipariş durumu değişikliği bilgilerini parse et
        var orderData = ParseOrderStatusChangeData(payload);

        if (orderData != null)
        {
            // Sipariş durumu değişikliğini kaydet
            await SaveOrderStatusChange(companyId, orderData);

            // Bildirim gönderme işlemini başlat (arka planda - yeni scope ile)
            _ = Task.Run(async () =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var scopedOrderStatusNotificationService =
                    scope.ServiceProvider.GetRequiredService<IOrderStatusNotificationService>();
                var scopedOrderStatusMappingService =
                    scope.ServiceProvider.GetRequiredService<IOrderStatusMappingService>();
                var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<WebhookController>>();

                try
                {
                    await ProcessOrderStatusNotificationsWithScope(
                        companyId,
                        orderData,
                        scopedContext,
                        scopedOrderStatusNotificationService,
                        scopedOrderStatusMappingService,
                        scopedLogger);
                }
                catch (Exception ex)
                {
                    scopedLogger.LogError(ex,
                        "Error processing order status notifications for company {CompanyId}, order {OrderId}",
                        companyId,
                        orderData.OrderId);
                }
            });
        }

        _logger.LogInformation("Order status change webhook processed successfully for company {CompanyId}", companyId);
    }

    private OrderStatusChangeData? ParseOrderStatusChangeData(JsonElement payload)
    {

        var orderId = GetIntProperty(payload, "ID");
        var orderNumber = GetStringProperty(payload, "SiparisNo");
        var newStatus = GetIntProperty(payload, "SiparisDurumu");
        var oldStatus = GetStringProperty(payload, "EskiDurum") ?? "";
        var customerEmail = GetStringProperty(payload, "UyeMail");
        var customerName = GetStringProperty(payload, "MusteriAdi") ?? "";
        var customerPhone = GetStringProperty(payload, "UyeCep");
        var customerId = GetIntProperty(payload, "UyeID");
        if (customerId is null or 0)
            return null;

        if (!orderId.HasValue || !newStatus.HasValue || string.IsNullOrEmpty(customerEmail))
        {
            _logger.LogWarning("Required fields missing in order status change webhook payload");
            return null;
        }

        var orderData = new OrderStatusChangeData
        {
            OrderId = orderId.HasValue ? orderId.Value.ToString() : "",
            OrderNumber = orderNumber,
            NewStatus = newStatus.HasValue ? newStatus.Value.ToString() : "",
            OldStatus = oldStatus,
            CustomerEmail = customerEmail,
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            CustomerId = customerId ?? 0,
            StatusChangedAt = DateTime.Now,
            WebhookPayload = payload.ToString()
        };

        // Sipariş tutarı varsa ekle
        if (payload.TryGetProperty("SiparisTutari", out var amountElement) &&
            amountElement.TryGetDecimal(out var amount))
        {
            orderData.OrderAmount = amount;
        }

        // Para birimi varsa ekle
        orderData.OrderCurrency = GetStringProperty(payload, "ParaBirimi") ?? "TRY";

        return orderData;
    }

    private async Task SaveOrderStatusChange(Guid companyId, OrderStatusChangeData orderData)
    {

        // Ticimax durumunu sistem durumuna eşleştir
        var mappedStatus = await GetMappedOrderStatusAsync(companyId, orderData.NewStatus);
        var mappedOldStatus = !string.IsNullOrEmpty(orderData.OldStatus)
            ? await GetMappedOrderStatusAsync(companyId, orderData.OldStatus)
            : null;

        var changeLog = new OrderStatusChangeLog
        {
            CompanyId = companyId,
            OrderId = orderData.OrderId,
            OrderNumber = orderData.OrderNumber,
            CustomerId =  orderData.CustomerId,
            CustomerEmail = orderData.CustomerEmail,
            CustomerName = orderData.CustomerName,
            CustomerPhone = orderData.CustomerPhone,
            OldStatus = mappedOldStatus,
            NewStatus = mappedStatus,
            NewStatusDisplayName = GetOrderStatusDisplayName(mappedStatus),
            OrderAmount = orderData.OrderAmount,
            OrderCurrency = orderData.OrderCurrency,
            StatusChangedAt = orderData.StatusChangedAt,
            WebhookPayload = orderData.WebhookPayload,
            CreatedAt = DateTime.Now
        };

        _context.OrderStatusChangeLogs.Add(changeLog);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Order status change saved: Order {OrderId}, External Status {ExternalOldStatus} -> {ExternalNewStatus}, Mapped Status {MappedOldStatus} -> {MappedNewStatus}",
            orderData.OrderId,
            orderData.OldStatus,
            orderData.NewStatus,
            mappedOldStatus,
            mappedStatus);
    }

    /// <summary>
    /// Arka plan task'ı için scope'lu versiyon - DbContext dispose sorununu önler
    /// </summary>
    private async Task ProcessOrderStatusNotificationsWithScope(
        Guid companyId,
        OrderStatusChangeData orderData,
        ApplicationDbContext context,
        IOrderStatusNotificationService orderStatusNotificationService,
        IOrderStatusMappingService orderStatusMappingService,
        ILogger<WebhookController> logger)
    {

        // 1. Müşterinin "Sipariş Durumu Bildirimleri" modülü var mı kontrol et
        var hasOrderStatusModule = await context.CompanyModules
            .Include(cm => cm.Module)
            .AnyAsync(cm => cm.CompanyId == companyId &&
                            cm.Module.Name == "Sipariş Durumu Bildirimleri" &&
                            cm.IsActive &&
                            (!cm.ExpiresAt.HasValue || cm.ExpiresAt.Value > DateTime.UtcNow));

        if (!hasOrderStatusModule)
        {
            logger.LogInformation(
                "Company {CompanyId} does not have active Order Status Notifications module, skipping notification for order {OrderId}",
                companyId,
                orderData.OrderId);

            return;
        }

        // 2. Ticimax durumunu sistem durumuna eşleştir
        var mappedStatus =
            await orderStatusMappingService.MapExternalToInternalStatusAsync(companyId, "Ticimax", orderData.NewStatus);

        if (string.IsNullOrEmpty(mappedStatus))
        {
            logger.LogWarning(
                "No status mapping found for external status {ExternalStatus} in company {CompanyId}, order {OrderId}",
                orderData.NewStatus,
                companyId,
                orderData.OrderId);

            return;
        }

        // 3. Sipariş durumları sayfasında bu durum aktif mi kontrol et
        var orderStatusNotification = await context.OrderStatusNotifications
            .FirstOrDefaultAsync(osn => osn.CompanyId == companyId &&
                                        osn.OrderStatus == mappedStatus &&
                                        osn.IsActive);

        if (orderStatusNotification == null)
        {
            logger.LogInformation(
                "Order status {MappedStatus} is not active for notifications in company {CompanyId}, skipping notification for order {OrderId}",
                mappedStatus,
                companyId,
                orderData.OrderId);

            return;
        }

        // 4. OrderStatusChangeLog'u bul (eşleştirilmiş durumla)
        var orderStatusChangeLog = await context.OrderStatusChangeLogs
            .FirstOrDefaultAsync(oscl => oscl.CompanyId == companyId &&
                                         oscl.OrderId == orderData.OrderId &&
                                         oscl.NewStatus == mappedStatus &&
                                         oscl.StatusChangedAt >=
                                         DateTime.Now.AddMinutes(-5)); // Son 5 dakika içinde oluşturulan

        if (orderStatusChangeLog == null)
        {
            logger.LogWarning(
                "OrderStatusChangeLog not found for order {OrderId}, external status {ExternalStatus}, mapped status {MappedStatus} in company {CompanyId}",
                orderData.OrderId,
                orderData.NewStatus,
                mappedStatus,
                companyId);

            return;
        }

        // 5. Tüm kontroller geçti, bildirim gönderme servisini çağır
        logger.LogInformation(
            "All checks passed, processing order status notification for company {CompanyId}, order {OrderId}, status {MappedStatus}",
            companyId,
            orderData.OrderId,
            mappedStatus);

        await orderStatusNotificationService.ProcessOrderStatusNotificationAsync(companyId, orderStatusChangeLog);
    }

    private async Task<string?> GetMappedOrderStatusAsync(Guid companyId, string externalStatus)
    {
        // Şirketin Ticimax entegrasyonunu bul
        var integration = await _context.CompanyIntegrations
            .Include(ci => ci.Integration)
            .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                       ci.Integration.Name == "Ticimax" &&
                                       ci.Integration.Type == "ecommerce" &&
                                       ci.IsActive);

        if (integration == null)
        {
            _logger.LogWarning("No active Ticimax integration found for company {CompanyId}", companyId);
            return null;
        }

        // OrderStatusMappingService kullanarak eşleştirme yap
        var mappedStatus =
            await _orderStatusMappingService.MapExternalToInternalStatusAsync(companyId, "Ticimax", externalStatus);

        _logger.LogInformation("Status mapping for company {CompanyId}: {ExternalStatus} -> {MappedStatus}",
            companyId,
            externalStatus,
            mappedStatus);

        return mappedStatus;

    }

    private string GetOrderStatusDisplayName(string status)
    {
        // Bizim 7 sistem durumunun Türkçe karşılıkları
        return status switch
        {
            "1" => "Sipariş Alındı",
            "2" => "Onaylandı",
            "3" => "Hazırlanıyor",
            "4" => "Kargoya Verildi",
            "5" => "Teslim Edildi",
            "6" => "İptal Edildi",
            "7" => "İade Edildi",
            _ => status
        };
    }

    private string? GetStringProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String)
        {
            return property.GetString();
        }

        return null;
    }

    private int? GetIntProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number)
        {
            return property.GetInt32();
        }

        return null;
    }

    private decimal? GetDecimalProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number)
        {
            return property.GetDecimal();
        }

        return null;
    }

    private bool? GetBoolProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            // Boolean olarak gelirse
            if (property.ValueKind == JsonValueKind.True)
                return true;
            if (property.ValueKind == JsonValueKind.False)
                return false;
            
            // Integer olarak gelirse (0/1)
            if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var intValue))
                return intValue == 1;
        }

        return null;
    }

    /// <summary>
    /// Ticimax API'sinden müşteri izin bilgilerini alır
    /// </summary>
    private async Task<(bool EmailPermission, bool SmsPermission)> GetCustomerPermissionsFromApiAsync(Guid companyId, int externalCustomerId)
    {
        try
        {
            var ecommerceService = await _ecommerceServiceFactory.GetEcommerceServiceAsync(companyId);
            if (ecommerceService != null)
            {
                var customer = await ecommerceService.GetCustomerAsync(companyId, externalCustomerId.ToString());
                if (customer != null)
                {
                    _logger.LogInformation(
                        "Customer permissions fetched from API for ExternalId {ExternalId}: EmailPermission={EmailPermission}, SmsPermission={SmsPermission}",
                        externalCustomerId,
                        customer.EmailPermission,
                        customer.SmsPermission);
                    
                    return (customer.EmailPermission, customer.SmsPermission);
                }
                else
                {
                    _logger.LogWarning(
                        "Customer not found in API for ExternalId {ExternalId}, using default permissions (true, true)",
                        externalCustomerId);
                }
            }
            else
            {
                _logger.LogWarning(
                    "E-commerce service not found for company {CompanyId}, using default permissions (true, true)",
                    companyId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error fetching customer permissions from API for ExternalId {ExternalId}, using default permissions (true, true)",
                externalCustomerId);
        }

        // Varsayılan olarak izinleri true döndür
        return (true, true);
    }

    private DateTime? GetDateTimeProperty(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var property))
        {
            if (property.ValueKind == JsonValueKind.String && property.TryGetDateTime(out var dateTime))
            {
                return dateTime;
            }
            else if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var timestamp))
            {
                return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
            }
        }

        return null;
    }

    // Helper class for order status change data
    private class OrderStatusChangeData
    {
        public string OrderId { get; set; } = string.Empty;

        public string? OrderNumber { get; set; }

        public string NewStatus { get; set; } = string.Empty;

        public string? OldStatus { get; set; }

        public string CustomerEmail { get; set; } = string.Empty;

        public string? CustomerName { get; set; }

        public string? CustomerPhone { get; set; }

        public int CustomerId { get; set; }

        public decimal? OrderAmount { get; set; }

        public string? OrderCurrency { get; set; }

        public DateTime StatusChangedAt { get; set; }

        public string? WebhookPayload { get; set; }
    }

    /// <summary>
    /// Comment API'den gelen webhook'ları işler
    /// </summary>
    [HttpPost("comment-completed")]
    public async Task<IActionResult> CommentCompleted([FromBody] CommentWebhookRequestDto request)
    {
        _logger.LogInformation("Received comment webhook for job {JobId} with status {Status}",
            request.job_id,
            request.status);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid comment webhook request received: {Errors}",
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

            return BadRequest(ModelState);
        }

        var result = await _commentService.ProcessWebhookAsync(request);

        if (result)
        {
            _logger.LogInformation("Successfully processed comment webhook for job {JobId}", request.job_id);
            return Ok(new { success = true, message = "Webhook processed successfully" });
        }
        else
        {
            _logger.LogWarning("Failed to process comment webhook for job {JobId}", request.job_id);
            return BadRequest(new { success = false, message = "Failed to process webhook" });
        }
    }

    /// <summary>
    /// Trendyol ürün çekme API'sinden gelen webhook'ları işler
    /// </summary>
    [HttpPost("product-scrape-completed")]
    public async Task<IActionResult> ProductScrapeCompleted([FromBody] ProductScrapeWebhookDto request)
    {
        _logger.LogInformation("Received product scrape webhook for job {JobId} with status {Status}",
            request.job_id,
            request.status);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid product scrape webhook request received: {Errors}",
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

            return BadRequest(ModelState);
        }

        var result = await _trendyolScraperService.ProcessProductScrapeWebhookAsync(request);

        if (result)
        {
            _logger.LogInformation("Successfully processed product scrape webhook for job {JobId}", request.job_id);
            return Ok(new { success = true, message = "Webhook processed successfully" });
        }
        else
        {
            _logger.LogWarning("Failed to process product scrape webhook for job {JobId}", request.job_id);
            return BadRequest(new { success = false, message = "Failed to process webhook" });
        }
    }

    /// <summary>
    /// Trendyol yorum çekme API'sinden gelen webhook'ları işler
    /// </summary>
    [HttpPost("comment-scrape-completed")]
    public async Task<IActionResult> CommentScrapeCompleted([FromBody] CommentScrapeWebhookDto request)
    {
        _logger.LogInformation("Received comment scrape webhook for job {JobId} with status {Status}",
            request.job_id,
            request.status);

        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Invalid comment scrape webhook request received: {Errors}",
                string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

            return BadRequest(ModelState);
        }

        var result = await _trendyolScraperService.ProcessCommentScrapeWebhookAsync(request);

        if (result)
        {
            _logger.LogInformation("Successfully processed comment scrape webhook for job {JobId}", request.job_id);
            return Ok(new { success = true, message = "Webhook processed successfully" });
        }
        else
        {
            _logger.LogWarning("Failed to process comment scrape webhook for job {JobId}", request.job_id);
            return BadRequest(new { success = false, message = "Failed to process webhook" });
        }
    }

    /// <summary>
    /// WhatsApp webhook endpoint - handles Gupshup message status events
    /// </summary>
    [HttpPost("whatsapp/{companyId}")]
    public async Task<IActionResult> WhatsAppWebhook(Guid companyId)
    {
        try
        {
            // Read raw body
            using var reader = new StreamReader(Request.Body);
            var rawBody = await reader.ReadToEndAsync();

            _logger.LogInformation(
                "Received WhatsApp webhook for company {CompanyId}: {Payload}",
                companyId,
                rawBody);

            // Verify company exists
            var company = await _context.Companies.FindAsync(companyId);
            if (company == null)
            {
                _logger.LogWarning("Company {CompanyId} not found for WhatsApp webhook", companyId);
                return NotFound(new { success = false, message = "Company not found" });
            }

            // Parse webhook payload
            GupshupWebhookEventDto? webhookEvent;
            try
            {
                webhookEvent = JsonSerializer.Deserialize<GupshupWebhookEventDto>(
                    rawBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse WhatsApp webhook payload");
                return BadRequest(new { success = false, message = "Invalid JSON payload" });
            }

            if (webhookEvent == null)
            {
                return BadRequest(new { success = false, message = "Empty webhook payload" });
            }

            // Store raw webhook event asynchronously
            _ = Task.Run(async () =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<WebhookController>>();

                try
                {
                    await ProcessWhatsAppWebhookAsync(
                        companyId,
                        webhookEvent,
                        rawBody,
                        Request.Headers["User-Agent"].ToString(),
                        scopedContext,
                        scopedLogger);
                }
                catch (Exception ex)
                {
                    scopedLogger.LogError(ex,
                        "Error processing WhatsApp webhook for company {CompanyId}",
                        companyId);
                }
            });

            return Ok(new { success = true, message = "Webhook received" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling WhatsApp webhook for company {CompanyId}", companyId);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    private async Task ProcessWhatsAppWebhookAsync(
        Guid companyId,
        GupshupWebhookEventDto webhookEvent,
        string rawPayload,
        string? sourceIp,
        ApplicationDbContext context,
        ILogger<WebhookController> logger)
    {
        var gsId = webhookEvent.Payload?.Payload?.GsId;
        var eventType = webhookEvent.Type;
        var eventSubType = webhookEvent.Payload?.Type;

        // Store webhook event
        var webhookEventRecord = new WhatsAppWebhookEvent
        {
            CompanyId = companyId,
            EventType = eventType ?? "unknown",
            EventSubType = eventSubType,
            GupshupMessageId = gsId,
            RawPayload = rawPayload,
            IsVerified = true, // TODO: Implement token verification
            IsProcessed = false,
            ReceivedAt = DateTime.Now,
            SourceIp = sourceIp,
            GupshupTimestamp = webhookEvent.Payload?.Payload?.Ts
        };

        context.WhatsAppWebhookEvents.Add(webhookEventRecord);
        await context.SaveChangesAsync();

        // If we have a message ID, try to update the message status
        if (!string.IsNullOrEmpty(gsId) && !string.IsNullOrEmpty(eventSubType))
        {
            var message = await context.WhatsAppMessages
                .FirstOrDefaultAsync(m => m.GupshupMessageId == gsId);

            if (message != null)
            {
                // Link webhook event to message
                webhookEventRecord.WhatsAppMessageId = message.Id;

                // Update message status based on event type
                var statusUpdated = UpdateMessageStatusFromWebhook(
                    message,
                    eventSubType,
                    webhookEvent.Payload?.Payload,
                    context,
                    logger);

                if (statusUpdated)
                {
                    webhookEventRecord.IsProcessed = true;
                    webhookEventRecord.ProcessedAt = DateTime.Now;
                }

                await context.SaveChangesAsync();

                logger.LogInformation(
                    "Updated WhatsApp message {GsId} status to {Status} for company {CompanyId}",
                    gsId,
                    message.Status,
                    companyId);
            }
            else
            {
                logger.LogWarning(
                    "WhatsApp message {GsId} not found for webhook event, company {CompanyId}",
                    gsId,
                    companyId);
            }
        }
    }

    private bool UpdateMessageStatusFromWebhook(
        WhatsAppMessage message,
        string eventSubType,
        GupshupWebhookMessagePayloadDto? payload,
        ApplicationDbContext context,
        ILogger<WebhookController> logger)
    {
        var timestamp = payload?.Ts != null
            ? DateTimeOffset.FromUnixTimeMilliseconds(payload.Ts.Value).UtcDateTime
            : DateTime.Now;

        var statusChanged = false;

        switch (eventSubType.ToLower())
        {
            case "enqueued":
            case "sent":
                if (message.Status == "pending")
                {
                    message.Status = "sent";
                    statusChanged = true;
                }
                break;

            case "delivered":
                message.Status = "delivered";
                message.DeliveredAt = timestamp;
                statusChanged = true;
                break;

            case "read":
                message.Status = "read";
                message.ReadAt = timestamp;
                if (!message.DeliveredAt.HasValue)
                {
                    message.DeliveredAt = timestamp;
                }
                statusChanged = true;
                break;

            case "failed":
                message.Status = "failed";
                message.FailedAt = timestamp;
                message.ErrorMessage = payload?.Error?.Message;
                message.ErrorCode = payload?.Error?.Code;
                statusChanged = true;
                break;

            default:
                logger.LogWarning("Unknown WhatsApp webhook event subtype: {EventSubType}", eventSubType);
                break;
        }

        if (statusChanged)
        {
            message.UpdatedAt = DateTime.Now;
        }

        return statusChanged;
    }

    private async Task ProcessFavoriteProductAddedWebhook(Guid companyId, JsonElement payload)
    {
        _logger.LogInformation("Processing favorite product added webhook for company {CompanyId}: {Payload}",
            companyId,
            payload.ToString());

        try
        {
            var uyeId = GetIntProperty(payload, "UyeID");
            var urunId = GetIntProperty(payload, "UrunID");
            var favoriId = GetIntProperty(payload, "FavoriID");
            var urunAdi = GetStringProperty(payload, "UrunAdi") ?? "";
            var urunUrl = GetStringProperty(payload, "UrunUrl");
            var stokKodu = GetStringProperty(payload, "StokKodu");
            var fiyat = GetDecimalProperty(payload, "Fiyat");
            var fiyatKdv = GetDecimalProperty(payload, "FiyatKdv");
            var paraBirimi = GetStringProperty(payload, "ParaBirimi") ?? "TRY";
            var stokAdedi = GetIntProperty(payload, "StokAdedi");
            var resimUrl = GetStringProperty(payload, "ResimUrl");
            var eklemeTarihi = GetDateTimeProperty(payload, "EklemeTarihi");

            if (!uyeId.HasValue || !urunId.HasValue)
            {
                _logger.LogWarning("Required fields missing in favorite product added webhook payload: UyeID or UrunID");
                return;
            }

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.ExternalId == uyeId.Value);

            if (customer == null)
            {
                _logger.LogWarning(
                    "Customer not found for favorite product added webhook: ExternalId {ExternalId}, Company {CompanyId}",
                    uyeId.Value,
                    companyId);
            }

            FavoriteProduct? existingFavorite = null;

            if (favoriId.HasValue)
            {
                existingFavorite = await _context.FavoriteProducts
                    .FirstOrDefaultAsync(f => f.CompanyId == companyId && f.ExternalFavoriteId == favoriId.Value);
            }
            else
            {
                existingFavorite = await _context.FavoriteProducts
                    .FirstOrDefaultAsync(f => f.CompanyId == companyId &&
                                             f.ExternalProductId == urunId.Value &&
                                             f.ExternalMemberId == uyeId.Value &&
                                             f.IsActive);
            }

            if (existingFavorite != null)
            {
                existingFavorite.ProductName = urunAdi;
                existingFavorite.StockCode = stokKodu;
                existingFavorite.Price = fiyat ?? existingFavorite.Price;
                existingFavorite.PriceWithVat = fiyatKdv ?? existingFavorite.PriceWithVat;
                existingFavorite.Currency = paraBirimi;
                existingFavorite.StockQuantity = stokAdedi ?? existingFavorite.StockQuantity;
                existingFavorite.ImageUrl = resimUrl ?? existingFavorite.ImageUrl;
                existingFavorite.ProductUrl = urunUrl ?? existingFavorite.ProductUrl;
                existingFavorite.UpdatedAt = DateTime.Now;
                existingFavorite.LastSyncAt = DateTime.Now;
                existingFavorite.IsActive = true;

                if (customer != null)
                {
                    existingFavorite.CustomerId = customer.Id;
                }

                if (eklemeTarihi.HasValue)
                {
                    existingFavorite.AddedToFavoritesAt = eklemeTarihi.Value;
                }

                _logger.LogInformation(
                    "Favorite product updated in database for company {CompanyId}, favorite {FavoriteId}, product {ProductId}, member {MemberId}",
                    companyId,
                    existingFavorite.ExternalFavoriteId,
                    urunId.Value,
                    uyeId.Value);
            }
            else
            {
                var newFavorite = new FavoriteProduct
                {
                    CompanyId = companyId,
                    ExternalFavoriteId = favoriId ?? (urunId.Value * 1000000 + uyeId.Value),
                    ExternalProductId = urunId.Value,
                    ExternalMemberId = uyeId.Value,
                    ProductName = urunAdi,
                    StockCode = stokKodu,
                    Price = fiyat ?? 0,
                    PriceWithVat = fiyatKdv ?? 0,
                    Currency = paraBirimi,
                    StockQuantity = stokAdedi ?? 0,
                    ImageUrl = resimUrl,
                    ProductUrl = urunUrl,
                    VariantCount = 0,
                    AddedToFavoritesAt = eklemeTarihi ?? DateTime.Now,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    LastSyncAt = DateTime.Now,
                    IsActive = true,
                    NotificationSent = false
                };

                if (customer != null)
                {
                    newFavorite.CustomerId = customer.Id;
                }

                _context.FavoriteProducts.Add(newFavorite);

                _logger.LogInformation(
                    "Favorite product added to database for company {CompanyId}, favorite {FavoriteId}, product {ProductId}, member {MemberId}",
                    companyId,
                    newFavorite.ExternalFavoriteId,
                    urunId.Value,
                    uyeId.Value);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Favorite product added webhook processed successfully for company {CompanyId}, product {ProductId}, member {MemberId}",
                companyId,
                urunId.Value,
                uyeId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing favorite product added webhook for company {CompanyId}",
                companyId);
        }
    }

    private async Task ProcessFavoriteProductRemovedWebhook(Guid companyId, JsonElement payload)
    {
        _logger.LogInformation("Processing favorite product removed webhook for company {CompanyId}: {Payload}",
            companyId,
            payload.ToString());

        try
        {
            var uyeId = GetIntProperty(payload, "UyeID");
            var urunId = GetIntProperty(payload, "UrunID");
            var favoriId = GetIntProperty(payload, "FavoriID");

            if (!uyeId.HasValue || !urunId.HasValue)
            {
                _logger.LogWarning("Required fields missing in favorite product removed webhook payload: UyeID or UrunID");
                return;
            }

            FavoriteProduct? favoriteToRemove = null;

            if (favoriId.HasValue)
            {
                favoriteToRemove = await _context.FavoriteProducts
                    .FirstOrDefaultAsync(f => f.CompanyId == companyId && f.ExternalFavoriteId == favoriId.Value);
            }
            else
            {
                favoriteToRemove = await _context.FavoriteProducts
                    .FirstOrDefaultAsync(f => f.CompanyId == companyId &&
                                             f.ExternalProductId == urunId.Value &&
                                             f.ExternalMemberId == uyeId.Value &&
                                             f.IsActive);
            }

            if (favoriteToRemove != null)
            {
                _context.FavoriteProducts.Remove(favoriteToRemove);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Favorite product removed from database for company {CompanyId}, favorite {FavoriteId}, product {ProductId}, member {MemberId}",
                    companyId,
                    favoriteToRemove.ExternalFavoriteId,
                    urunId.Value,
                    uyeId.Value);
            }
            else
            {
                _logger.LogWarning(
                    "Favorite product not found in database for removal: Company {CompanyId}, Product {ProductId}, Member {MemberId}, FavoriteId {FavoriteId}",
                    companyId,
                    urunId.Value,
                    uyeId.Value,
                    favoriId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing favorite product removed webhook for company {CompanyId}",
                companyId);
        }
    }

    private async Task ProcessProductUpdatedWebhook(Guid companyId, JsonElement payload)
    {
        _logger.LogInformation("Processing product updated webhook for company {CompanyId}: {Payload}",
            companyId,
            payload.ToString());

        try
        {
            var urunId = GetIntProperty(payload, "UrunID");
            if (urunId.HasValue)
            {
                await _productSliderService.SyncSliderItemsFromProductUpdateAsync(companyId, urunId.Value.ToString());
            }

            await _productSliderService.ExportAllSlidersToR2Async(companyId);
            _logger.LogInformation(
                "Product slider DB sync and R2 export completed after product updated webhook for company {CompanyId}",
                companyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing product updated webhook for company {CompanyId}",
                companyId);
        }
    }

    /// <summary>
    /// Ürün stok değişikliği webhook'unu işler ve stok hatırlatma bildirimlerini tetikler
    /// </summary>
    private Task ProcessProductStockChangedWebhook(Guid companyId, JsonElement payload)
    {
        _logger.LogError("Processing product stock changed webhook for company {CompanyId}: {Payload}",
            companyId, JsonSerializer.Serialize(payload));

        try
        {
            // Payload JSON array olarak geliyor — her nesneyi ayrı işle
            var items = new List<JsonElement>();
            if (payload.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in payload.EnumerateArray())
                {
                    items.Add(item);
                }
            }
            else
            {
                // Tekil nesne geldiyse de destekle
                items.Add(payload);
            }

            // Arka plan scope'da çalıştır (DbContext dispose sorununu önler)
            _ = Task.Run(async () =>
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var scopedStockReminderService = scope.ServiceProvider.GetRequiredService<IStockReminderModuleService>();
                var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<WebhookController>>();
                var scopedContext = scope.ServiceProvider.GetRequiredService<Data.ApplicationDbContext>();
                
                foreach (var item in items)
                {
                    var urunId = GetIntProperty(item, "ID");
                    var productCardId = GetIntProperty(item, "UrunKartiID");
                    
                    try
                    {
                        // Ticimax'tan tam stok alarm verilerini çek (sadece bu ürün için)
                        var pullResult = await scopedStockReminderService.RefreshProductStockDataAsync(
                            companyId, urunId ?? 0);
                        if (!pullResult.Success)
                        {
                            scopedLogger.LogWarning(
                                "Failed to pull stock reminders for company {CompanyId} after stock change webhook: {Message}",
                                companyId, pullResult.Message);
                            return;
                        }

                        // Sadece bu ürün için stok hatırlatma bildirimlerini gönder
                        var result = await scopedStockReminderService.SendStockReminderMessagesAsync(
                            companyId, new List<int> { urunId.Value });

                        if (result.Success)
                        {
                            scopedLogger.LogInformation(
                                "Stock reminder notifications sent for product {ProductId} in company {CompanyId}: {Notifications} sent, cost: {Cost}",
                                urunId.Value, companyId, result.TotalNotificationsSent, result.TotalCost);
                        }
                        else
                        {
                            scopedLogger.LogWarning(
                                "Stock reminder send failed for product {ProductId} in company {CompanyId}: {Message}",
                                urunId.Value, companyId, result.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        scopedLogger.LogError(ex,
                            "Error processing stock reminder after stock change webhook for product {ProductId} in company {CompanyId}",
                            urunId.Value, companyId);
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing product stock changed webhook for company {CompanyId}", companyId);
        }

        return Task.CompletedTask;
    }

}
