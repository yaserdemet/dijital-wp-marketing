using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.ViewModels;
using PushDashboard.Services.Integrations;
using PushDashboard.Services.WhatsApp;
using PushDashboard.Services.Notifications;
using System.Text.Json;
using PushDashboard.Exceptions;

namespace PushDashboard.Services;

public class BasketReminderService : IBasketReminderService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BasketReminderService> _logger;
    private readonly IEcommerceServiceFactory _ecommerceServiceFactory;
    private readonly INotificationChannelFactory _notificationChannelFactory;
    private readonly ICustomerService _customerService;
    private readonly ICommunicationChannelService _communicationChannelService;
    private readonly IWhatsAppTemplateService _whatsAppTemplateService;
    private readonly ISmsTemplateService _smsTemplateService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IWhatsAppTemplateComponentService _whatsAppTemplateComponentService;
    private readonly IServiceProvider _serviceProvider;

    public BasketReminderService(
        ApplicationDbContext context,
        ILogger<BasketReminderService> logger,
        IEcommerceServiceFactory ecommerceServiceFactory,
        INotificationChannelFactory notificationChannelFactory,
        ICustomerService customerService,
        ICommunicationChannelService communicationChannelService,
        IWhatsAppTemplateService whatsAppTemplateService,
        ISmsTemplateService smsTemplateService,
        IEmailTemplateService emailTemplateService,
        IWhatsAppTemplateComponentService whatsAppTemplateComponentService,
        IServiceProvider serviceProvider)
    {
        _context = context;
        _logger = logger;
        _ecommerceServiceFactory = ecommerceServiceFactory;
        _notificationChannelFactory = notificationChannelFactory;
        _customerService = customerService;
        _communicationChannelService = communicationChannelService;
        _whatsAppTemplateService = whatsAppTemplateService;
        _smsTemplateService = smsTemplateService;
        _emailTemplateService = emailTemplateService;
        _whatsAppTemplateComponentService = whatsAppTemplateComponentService;
        _serviceProvider = serviceProvider;
    }

    #region Settings Management

    public async Task<BasketReminderSettings?> GetSettingsAsync(Guid companyId)
    {
        return await _context.BasketReminderSettings
            .FirstOrDefaultAsync(brs => brs.CompanyId == companyId);
    }

    public async Task<(bool Success, string Message)> UpdateSettingsAsync(Guid companyId, BasketReminderSettings settings, string userId)
    {
        try
        {
            var existingSettings = await _context.BasketReminderSettings
                .FirstOrDefaultAsync(brs => brs.CompanyId == companyId);

            if (existingSettings == null)
            {
                // Yeni ayar oluştur
                var newSettings = new BasketReminderSettings
                {
                    CompanyId = companyId,
                    MaxNotificationsPerCustomer = settings.MaxNotificationsPerCustomer,
                    NotificationContent = settings.NotificationContent,
                    IsEnabled = settings.IsEnabled,
                    UpdatedByUserId = userId
                };

                _context.BasketReminderSettings.Add(newSettings);
            }
            else
            {
                // Mevcut ayarı güncelle
                existingSettings.MaxNotificationsPerCustomer = settings.MaxNotificationsPerCustomer;
                existingSettings.NotificationContent = settings.NotificationContent;
                existingSettings.IsEnabled = settings.IsEnabled;
                existingSettings.UpdatedAt = DateTime.Now;
                existingSettings.UpdatedByUserId = userId;
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Basket reminder settings updated for company {CompanyId} by user {UserId}", companyId, userId);
            return (true, "Ayarlar başarıyla güncellendi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating basket reminder settings for company {CompanyId}", companyId);
            return (false, "Ayarlar güncellenirken hata oluştu: " + ex.Message);
        }
    }

    public async Task<(bool Success, string Message)> CreateDefaultSettingsAsync(Guid companyId, string userId)
    {
        try
        {
            // Zaten ayar var mı kontrol et
            var existingSettings = await _context.BasketReminderSettings
                .FirstOrDefaultAsync(brs => brs.CompanyId == companyId);

            if (existingSettings != null)
            {
                _logger.LogInformation("Basket reminder settings already exist for company {CompanyId}", companyId);
                return (true, "Ayarlar zaten mevcut.");
            }

            // Varsayılan ayarları oluştur
            var defaultSettings = new BasketReminderSettings
            {
                CompanyId = companyId,
                MaxNotificationsPerCustomer = 1,
                NotificationContent = "",
                IsEnabled = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                UpdatedByUserId = userId
            };

            _context.BasketReminderSettings.Add(defaultSettings);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created default basket reminder settings for company {CompanyId} by user {UserId}", companyId, userId);
            return (true, "Varsayılan ayarlar başarıyla oluşturuldu.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating default basket reminder settings for company {CompanyId}", companyId);
            return (false, "Varsayılan ayarlar oluşturulurken bir hata oluştu.");
        }
    }

    #endregion

    #region Schedule Management

    public async Task<List<BasketReminderSchedule>> GetActiveSchedulesAsync(Guid companyId)
    {
        return await _context.BasketReminderSchedules
            .Where(brs => brs.CompanyId == companyId && brs.IsActive)
            .OrderBy(brs => brs.ReminderTimeForHours)
            .ToListAsync();
    }

    public async Task<List<BasketReminderSchedule>> GetAllSchedulesAsync(Guid companyId)
    {
        return await _context.BasketReminderSchedules
            .Include(brs => brs.CreatedByUser)
            .Where(brs => brs.CompanyId == companyId)
            .OrderBy(brs => brs.ReminderTimeForHours)
            .ToListAsync();
    }

    public async Task<int> CreateScheduleAsync(Guid companyId, string name, string message, int hours, bool isActive, bool allowResendForSentCustomers, string communicationChannels, string channelMessages, string userId)
    {
        var hasActiveModule = await _context.CompanyModules
            .Include(cm => cm.Module)
            .AnyAsync(cm => cm.CompanyId == companyId &&
                           cm.Module.Name == "Sepet Hatırlatma" &&
                           cm.IsOwned &&
                           cm.IsActive &&
                           !cm.IsPassive &&
                           (cm.ExpiresAt == null || cm.ExpiresAt > DateTime.UtcNow));

        if (!hasActiveModule)
        {
            throw new InvalidOperationException("Sepet hatırlatma modülü aktif değil. Lütfen modülü satın alın.");
        }

        var existingSchedule = await _context.BasketReminderSchedules
            .FirstOrDefaultAsync(brs => brs.CompanyId == companyId &&
                                      brs.ReminderTimeForHours == hours);

        if (existingSchedule != null)
        {
            throw new DuplicateException("Hatırlatma zamanlaması", "süre");
        }

        List<string> channels;
        Dictionary<string, object> channelMessagesObj;

        try
        {
            channels = JsonSerializer.Deserialize<List<string>>(communicationChannels ?? "[]") ?? new List<string>();
        }
        catch
        {
            throw new InvalidOperationException("İletişim kanalları formatı geçersiz.");
        }

        if (!channels.Any())
        {
            throw new InvalidOperationException("En az bir iletişim kanalı seçilmelidir.");
        }

        try
        {
            channelMessagesObj = JsonSerializer.Deserialize<Dictionary<string, object>>(channelMessages ?? "{}") ?? new Dictionary<string, object>();
        }
        catch
        {
            throw new InvalidOperationException("Kanal mesajları formatı geçersiz.");
        }

        // Her seçili kanal için template kontrolü
        foreach (var channel in channels)
        {
            if (!channelMessagesObj.ContainsKey(channel))
            {
                throw new InvalidOperationException($"{channel} kanalı için template seçilmelidir.");
            }

            var channelMessage = channelMessagesObj[channel];
            bool hasTemplateId = false;
            string? templateIdValue = null;

            if (channelMessage is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Object && jsonElement.TryGetProperty("templateId", out var templateIdProp))
                {
                    hasTemplateId = true;
                    templateIdValue = templateIdProp.ValueKind == JsonValueKind.String
                        ? templateIdProp.GetString()
                        : templateIdProp.GetRawText();
                }
                else if (jsonElement.ValueKind == JsonValueKind.String)
                {
                    hasTemplateId = true;
                    templateIdValue = jsonElement.GetString();
                }
            }
            else if (channelMessage is Dictionary<string, object> dict)
            {
                if (dict.TryGetValue("templateId", out var templateIdObj) && templateIdObj != null)
                {
                    hasTemplateId = true;
                    templateIdValue = templateIdObj.ToString();
                }
            }
            else if (channelMessage is string templateIdStr && !string.IsNullOrEmpty(templateIdStr))
            {
                hasTemplateId = true;
                templateIdValue = templateIdStr;
            }

            if (!hasTemplateId || string.IsNullOrEmpty(templateIdValue))
            {
                throw new InvalidOperationException($"{channel} kanalı için geçerli bir template seçilmelidir.");
            }
        }

        var newSchedule = new BasketReminderSchedule
        {
            CompanyId = companyId,
            Name = name,
            ReminderTimeForHours = hours,
            NotificationContent = message,
            CommunicationChannels = communicationChannels,
            ChannelMessages = channelMessages,
            IsActive = isActive,
            AllowResendForSentCustomers = allowResendForSentCustomers,
            CreatedAt = DateTime.Now,
            CreatedByUserId = userId
        };

        _context.BasketReminderSchedules.Add(newSchedule);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Basket reminder schedule created for company {CompanyId} by user {UserId}. Schedule: {ScheduleName} ({Hours}h)",
            companyId, userId, name, hours);

        return newSchedule.Id;
    }

    public async Task UpdateScheduleAsync(Guid companyId, int scheduleId, string name, string message, int hours, bool isActive, bool allowResendForSentCustomers, string communicationChannels, string channelMessages, string userId)
    {
        var existingSchedule = await _context.BasketReminderSchedules
            .FirstOrDefaultAsync(brs => brs.Id == scheduleId && brs.CompanyId == companyId);

        if (existingSchedule == null)
        {
            throw new NotFoundException("Hatırlatma zamanlaması", scheduleId);
        }

        var duplicateSchedule = await _context.BasketReminderSchedules
            .FirstOrDefaultAsync(brs => brs.CompanyId == companyId &&
                                      brs.ReminderTimeForHours == hours &&
                                      brs.Id != scheduleId);

        if (duplicateSchedule != null)
        {
            throw new DuplicateException("Bu zamanlama için zaten başka bir hatırlatma ayarı bulunmaktadır.");
        }

        existingSchedule.Name = name;
        existingSchedule.NotificationContent = message;
        existingSchedule.CommunicationChannels = communicationChannels;
        existingSchedule.ChannelMessages = channelMessages;
        existingSchedule.ReminderTimeForHours = hours;
        existingSchedule.IsActive = isActive;
        existingSchedule.AllowResendForSentCustomers = allowResendForSentCustomers;
        existingSchedule.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Basket reminder schedule updated for company {CompanyId} by user {UserId}. Schedule: {ScheduleId}",
            companyId, userId, scheduleId);
    }

    public async Task DeleteScheduleAsync(Guid companyId, int scheduleId, string userId)
    {
        var schedule = await _context.BasketReminderSchedules
            .FirstOrDefaultAsync(brs => brs.Id == scheduleId && brs.CompanyId == companyId);

        if (schedule == null)
        {
            throw new NotFoundException("Hatırlatma zamanlaması", scheduleId);
        }

        _context.BasketReminderSchedules.Remove(schedule);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Basket reminder schedule deleted for company {CompanyId} by user {UserId}. Schedule: {ScheduleId}",
            companyId, userId, scheduleId);
    }

    public async Task ToggleScheduleStatusAsync(Guid companyId, int scheduleId, string userId)
    {
        var schedule = await _context.BasketReminderSchedules
            .FirstOrDefaultAsync(brs => brs.Id == scheduleId && brs.CompanyId == companyId);

        if (schedule == null)
        {
            throw new NotFoundException("Hatırlatma zamanlaması", scheduleId);
        }

        schedule.IsActive = !schedule.IsActive;
        schedule.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        var statusText = schedule.IsActive ? "aktif" : "pasif";
        _logger.LogInformation("Basket reminder schedule status changed to {Status} for company {CompanyId} by user {UserId}. Schedule: {ScheduleId}",
            statusText, companyId, userId, scheduleId);
    }

    #endregion

    #region Reminder Processing

    public async Task<(bool Success, string Message, int ProcessedCount)> ProcessRemindersForCompanyAsync(Guid companyId)
    {
        try
        {
            _logger.LogInformation("Starting basket reminder processing for company {CompanyId}", companyId);

            var (isValid, validationMessage) = await ValidateEcommerceIntegrationAsync(companyId);
            if (!isValid)
            {
                return (false, validationMessage, 0);
            }

            var settings = await GetSettingsAsync(companyId);
            if (settings == null || !settings.IsEnabled)
            {
                return (false, "Sepet hatırlatma modülü bu şirket için aktif değil.", 0);
            }

            var activeSchedules = await GetActiveSchedulesAsync(companyId);
            if (!activeSchedules.Any())
            {
                return (false, "Aktif hatırlatma zamanlaması bulunamadı.", 0);
            }

            int totalProcessed = 0;
            var errors = new List<string>();

            foreach (var schedule in activeSchedules)
            {
                try
                {
                    var eligibleBaskets = await GetEligibleBasketsForReminderAsync(companyId, schedule.ReminderTimeForHours, schedule.Id);

                    var allowResend = schedule.AllowResendForSentCustomers;

                    foreach (var basket in eligibleBaskets)
                    {
                        if (!allowResend)
                        {
                            var (canSend, limitMessage, sentCount) = await CheckCustomerNotificationLimitForScheduleAsync(
                                companyId, basket.CustomerId, schedule.Id, settings.MaxNotificationsPerCustomer);

                            if (!canSend)
                            {
                                _logger.LogInformation("Customer {CustomerId} has reached notification limit for schedule {ScheduleId} ({SentCount}/{MaxLimit})",
                                    basket.CustomerId, schedule.Id, sentCount, settings.MaxNotificationsPerCustomer);
                                continue;
                            }
                        }

                        var (basketExists, basketMessage) = await ValidateBasketExistsAsync(companyId, basket.ExternalId);
                        if (!basketExists)
                        {
                            _logger.LogInformation("Basket {BasketExternalId} no longer exists on platform: {Message}",
                                basket.ExternalId, basketMessage);
                            continue;
                        }

                        var (success, message) = await SendReminderWithChannelsAsync(companyId, basket, schedule);
                        if (success)
                        {
                            totalProcessed++;
                        }
                        else
                        {
                            errors.Add($"Sepet {basket.ExternalId}: {message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing schedule {ScheduleId} for company {CompanyId}", schedule.Id, companyId);
                    errors.Add($"Zamanlama {schedule.Name}: {ex.Message}");
                }
            }

            var resultMessage = $"{totalProcessed} hatırlatma başarıyla gönderildi.";
            if (errors.Any())
            {
                resultMessage += $" {errors.Count} hata oluştu.";
            }

            _logger.LogInformation("Basket reminder processing completed for company {CompanyId}. Processed: {ProcessedCount}, Errors: {ErrorCount}",
                companyId, totalProcessed, errors.Count);

            return (true, resultMessage, totalProcessed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing basket reminders for company {CompanyId}", companyId);
            return (false, "Hatırlatma işlemi sırasında hata oluştu: " + ex.Message, 0);
        }
    }

    public async Task<(bool Success, string Message, int ProcessedCompanies, int TotalReminders)> ProcessAllRemindersAsync()
    {
        try
        {
            _logger.LogInformation("Starting basket reminder processing for all companies");

            // Sepet hatırlatma modülüne sahip aktif şirketleri al
            var companiesWithModule = await _context.CompanyModules
                .Include(cm => cm.Module)
                .Include(cm => cm.Company)
                .Where(cm => cm.Module.Name == "Sepet Hatırlatma" && cm.IsActive)
                .Select(cm => cm.CompanyId)
                .ToListAsync();

            if (!companiesWithModule.Any())
            {
                return (true, "Sepet hatırlatma modülüne sahip aktif şirket bulunamadı.", 0, 0);
            }

            int processedCompanies = 0;
            int totalReminders = 0;
            var errors = new List<string>();

            foreach (var companyId in companiesWithModule)
            {
                try
                {
                    var (success, message, processedCount) = await ProcessRemindersForCompanyAsync(companyId);
                    if (success)
                    {
                        processedCompanies++;
                        totalReminders += processedCount;
                    }
                    else
                    {
                        errors.Add($"Şirket {companyId}: {message}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing reminders for company {CompanyId}", companyId);
                    errors.Add($"Şirket {companyId}: {ex.Message}");
                }
            }

            var resultMessage = $"{processedCompanies} şirket için {totalReminders} hatırlatma işlendi.";
            if (errors.Any())
            {
                resultMessage += $" {errors.Count} hata oluştu.";
            }

            _logger.LogInformation("Basket reminder processing completed for all companies. Companies: {ProcessedCompanies}, Total reminders: {TotalReminders}, Errors: {ErrorCount}",
                processedCompanies, totalReminders, errors.Count);

            return (true, resultMessage, processedCompanies, totalReminders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing basket reminders for all companies");
            return (false, "Tüm şirketler için hatırlatma işlemi sırasında hata oluştu: " + ex.Message, 0, 0);
        }
    }

    public async Task<(bool Success, string Message)> SendReminderWithChannelsAsync(
        Guid companyId,
        Basket basket,
        BasketReminderSchedule schedule)
    {
        
        var channels = JsonSerializer.Deserialize<List<string>>(schedule.CommunicationChannels ?? "[]") ?? new List<string>();
        var channelMessages = JsonSerializer.Deserialize<Dictionary<string, object>>(schedule.ChannelMessages ?? "{}") ?? new Dictionary<string, object>();
        
        try
        {
            // Sepet Hatırlatma modül ID'sini al
            var basketReminderModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Sepet Hatırlatma" && m.IsActive);
            int? moduleId = basketReminderModule?.Id;
            var systemUserId = "785f0c69-61ae-4218-ae43-7ea7ce299e9b";
            var referenceId = $"basket_reminder_{basket.ExternalId}_{DateTime.Now:yyyyMMdd}";

            // Müşteri bilgilerini al
            var customer = await _customerService.GetCustomerByExternalIdAsync(companyId, basket.CustomerId);
            if (customer == null)
            {
                return (false, "Müşteri bilgisi bulunamadı.");
            }

            var successfulChannels = new List<string>();
            var failedChannels = new List<string>();
            var errors = new List<string>();
            
            var variables = new Dictionary<string, string>
            {
                {"customer_name", customer.Name},
                {"customer_first_name", customer.FirstName},
                {"customer_last_name", customer.LastName},
                {"customer_email", customer.Email},
                {"customer_phone", customer.Phone ?? customer.MobilePhone ?? ""},
            };

            // Her kanal için mesaj gönder
            foreach (var channel in channels)
            {
                try
                {
                    bool sent = false;
                    string errorMessage = "";

                    var channelService = await _notificationChannelFactory.GetChannelAsync(companyId, channel);
                    if (channelService == null)
                    {
                        failedChannels.Add(channel);
                        errors.Add($"{channel}: Kanal yapılandırılmamış veya bulunamadı.");
                        _logger.LogWarning("Notification channel {Channel} not found or not configured for company {CompanyId}", channel, companyId);
                        continue;
                    }

                    switch (channel)
                    {
                        case "Email":
                            if (customer.EmailPermission && !string.IsNullOrEmpty(customer.Email))
                            {
                                int? emailTemplateId = null;

                                if (channelMessages.ContainsKey(channel))
                                {
                                    var emailValue = channelMessages[channel];
                                    if (emailValue is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object &&
                                        jsonElement.TryGetProperty("templateId", out var templateIdElement))
                                    {
                                        if (int.TryParse(templateIdElement.GetString() ?? templateIdElement.GetInt32().ToString(), out int parsedId))
                                        {
                                            emailTemplateId = parsedId;
                                        }
                                    }
                                    else if (emailValue is Dictionary<string, object> emailConfig &&
                                             emailConfig.TryGetValue("templateId", out var templateIdObj))
                                    {
                                        if (int.TryParse(templateIdObj?.ToString(), out int parsedId))
                                        {
                                            emailTemplateId = parsedId;
                                        }
                                    }
                                }

                                if (emailTemplateId.HasValue)
                                {
                                    // Template içeriğini al
                                    var template = await _emailTemplateService.GetCompanyTemplatesAsync(companyId)
                                        .ContinueWith(t => t.Result.FirstOrDefault(x => x.Id == emailTemplateId.Value));

                                    sent = await channelService.SendNotificationWithCostTrackingAsync(
                                        companyId, customer, template.Name, variables, systemUserId, moduleId, referenceId);
                                    if (!sent) errorMessage = "Email gönderimi başarısız oldu.";
                                }
                            }
                            else
                            {
                                errorMessage = "Müşterinin email izni yok veya email adresi eksik.";
                            }
                            break;

                        case "WhatsApp":
                            if (customer.SmsPermission && (!string.IsNullOrEmpty(customer.Phone) || !string.IsNullOrEmpty(customer.MobilePhone)))
                            {
                                // WhatsApp template kontrolü
                                string? templateId = null;

                                if (channelMessages.ContainsKey(channel))
                                {
                                    var whatsappValue = channelMessages[channel];

                                    // JsonElement olarak gelebilir
                                    if (whatsappValue is JsonElement jsonElement)
                                    {
                                        if (jsonElement.ValueKind == JsonValueKind.Object &&
                                            jsonElement.TryGetProperty("templateId", out var templateIdElement))
                                        {
                                            templateId = templateIdElement.GetString();
                                        }
                                        else if (jsonElement.ValueKind == JsonValueKind.String)
                                        {
                                            // Direkt string olarak template ID gelebilir
                                            templateId = jsonElement.GetString();
                                        }
                                    }
                                    // Dictionary olarak gelebilir
                                    else if (whatsappValue is Dictionary<string, object> whatsappConfig)
                                    {
                                        if (whatsappConfig.TryGetValue("templateId", out var templateIdObj))
                                        {
                                            templateId = templateIdObj?.ToString();
                                        }
                                    }
                                    // String olarak gelebilir
                                    else if (whatsappValue is string templateIdStr)
                                    {
                                        templateId = templateIdStr;
                                    }
                                }

                                if (!string.IsNullOrEmpty(templateId))
                                {
                                    sent = await channelService.SendNotificationWithCostTrackingAsync(companyId, customer, templateId, variables, systemUserId, moduleId, referenceId);
                                    if (!sent) errorMessage = "WhatsApp template mesajı gönderilemedi.";
                                }
                                else
                                    errorMessage = "Template bulunamadı.";
                            }
                            else
                            {
                                errorMessage = "Müşterinin SMS izni yok veya telefon numarası eksik.";
                            }
                            break;

                        case "SMS":
                            if (customer.SmsPermission && (!string.IsNullOrEmpty(customer.Phone) || !string.IsNullOrEmpty(customer.MobilePhone)))
                            {
                                int? smsTemplateId = null;

                                if (channelMessages.ContainsKey(channel))
                                {
                                    var smsValue = channelMessages[channel];
                                    if (smsValue is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Object &&
                                        jsonElement.TryGetProperty("templateId", out var templateIdElement))
                                    {
                                        if (int.TryParse(templateIdElement.GetString() ?? templateIdElement.GetInt32().ToString(), out int parsedId))
                                        {
                                            smsTemplateId = parsedId;
                                        }
                                    }
                                    else if (smsValue is Dictionary<string, object> smsConfig &&
                                             smsConfig.TryGetValue("templateId", out var templateIdObj))
                                    {
                                        if (int.TryParse(templateIdObj?.ToString(), out int parsedId))
                                        {
                                            smsTemplateId = parsedId;
                                        }
                                    }
                                }

                                if (smsTemplateId.HasValue)
                                {
                                    // Template içeriğini al
                                    var templates = await _smsTemplateService.GetUserTemplatesAsync(companyId);
                                    var template = templates.FirstOrDefault(x => x.Id == smsTemplateId.Value);

                                    sent = await channelService.SendNotificationWithCostTrackingAsync(
                                        companyId,
                                        customer,
                                        template.Name,
                                        variables,
                                        systemUserId,
                                        moduleId,
                                        referenceId);

                                    if (!sent) errorMessage = "SMS gönderilemedi.";
                                }
                                else
                                    errorMessage = "Template bulunamadı.";

                            }
                            else
                            {
                                errorMessage = "Müşterinin SMS izni yok veya telefon numarası eksik.";
                            }
                            break;

                        default:
                            errorMessage = $"Desteklenmeyen kanal: {channel}";
                            break;
                    }

                    if (sent)
                    {
                        successfulChannels.Add(channel);
                        _logger.LogInformation("Basket reminder sent via {Channel}. Basket: {BasketExternalId}, Customer: {CustomerEmail}",
                            channel, basket.ExternalId, customer.Email);
                    }
                    else
                    {
                        failedChannels.Add(channel);
                        errors.Add($"{channel}: {errorMessage}");
                        _logger.LogWarning("Failed to send basket reminder via {Channel}. Basket: {BasketExternalId}, Error: {Error}",
                            channel, basket.ExternalId, errorMessage);
                    }

                    // Her kanal için ayrı log oluştur
                    var log = new BasketReminderLog
                    {
                        BasketId = basket.Id,
                        BasketExternalId = basket.ExternalId,
                        CompanyId = companyId,
                        CustomerId = basket.CustomerId,
                        CustomerEmail = customer.Email,
                        CustomerName = customer.FullName,
                        ReminderTimeForHours = schedule.ReminderTimeForHours,
                        BasketReminderScheduleId = schedule.Id,
                        IsSuccessful = sent,
                        NotificationContent = $"[{channel}]",
                        ErrorMessage = sent ? null : errorMessage
                    };

                    await CreateReminderLogAsync(log);
                }
                catch (Exception ex)
                {
                    failedChannels.Add(channel);
                    errors.Add($"{channel}: {ex.Message}");
                    _logger.LogError(ex, "Error sending basket reminder via {Channel} for basket {BasketExternalId}",
                        channel, basket.ExternalId);
                }
            }

            // Sonuç mesajını oluştur
            if (successfulChannels.Any())
            {
                var message = $"Hatırlatma başarıyla gönderildi: {string.Join(", ", successfulChannels)}";
                if (failedChannels.Any())
                {
                    message += $". Başarısız: {string.Join(", ", failedChannels)}";
                }
                return (true, message);
            }
            else
            {
                var errorMessage = $"Tüm kanallar başarısız oldu: {string.Join(", ", errors)}";
                return (false, errorMessage);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending basket reminder for basket {BasketExternalId}", basket.ExternalId);

            // Hata logunu da kaydet
            var errorLog = new BasketReminderLog
            {
                BasketId = basket.Id,
                BasketExternalId = basket.ExternalId,
                CompanyId = companyId,
                CustomerId = basket.CustomerId,
                CustomerEmail = basket.CustomerEmail,
                CustomerName = basket.CustomerName,
                ReminderTimeForHours = schedule.ReminderTimeForHours,
                BasketReminderScheduleId = schedule.Id,
                IsSuccessful = false,
                NotificationContent = string.Join(", ", channels),
                ErrorMessage = ex.Message
            };

            await CreateReminderLogAsync(errorLog);

            return (false, "Hatırlatma gönderilirken hata oluştu: " + ex.Message);
        }
    }

    #endregion

    #region Validation

    public async Task<(bool IsValid, string Message)> ValidateEcommerceIntegrationAsync(Guid companyId)
    {
        try
        {
            // E-ticaret entegrasyonu var mı kontrol et
            var ecommerceIntegration = await _context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                         ci.Integration.Type == "ecommerce" &&
                                         ci.IsActive &&
                                         ci.IsConfigured);

            if (ecommerceIntegration == null)
            {
                return (false, "Aktif e-ticaret entegrasyonu bulunamadı. Lütfen önce e-ticaret entegrasyonunuzu yapılandırın.");
            }

            // Sepet senkronizasyonu aktif mi kontrol et
            var settings = ecommerceIntegration.Settings;
            if (settings.ContainsKey("syncCarts") && settings["syncCarts"].ToString() == "True")
            {
                return (true, "E-ticaret entegrasyonu ve sepet senkronizasyonu aktif.");
            }

            return (false, "Sepet senkronizasyonu aktif değil. Lütfen e-ticaret entegrasyon ayarlarınızda 'Sepetleri Senkronize Et' seçeneğini aktif hale getirin.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating ecommerce integration for company {CompanyId}", companyId);
            return (false, "E-ticaret entegrasyonu kontrolü sırasında hata oluştu.");
        }
    }

    public async Task<(bool IsValid, string Message)> ValidateBasketExistsAsync(Guid companyId, string basketExternalId)
    {
        try
        {
            var ecommerceService = await _ecommerceServiceFactory.GetEcommerceServiceAsync(companyId);
            if (ecommerceService == null)
            {
                return (false, "E-ticaret servisi bulunamadı.");
            }

            // Platform üzerinde sepeti kontrol et
            var basketExists = await ecommerceService.ValidateBasketExistsAsync(companyId, basketExternalId);
            if (basketExists)
            {
                return (true, "Sepet platformda mevcut.");
            }

            return (false, "Sepet platformda bulunamadı.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating basket existence for basket {BasketExternalId}", basketExternalId);
            return (false, "Sepet doğrulama sırasında hata oluştu.");
        }
    }

    public async Task<(bool CanSend, string Message, int SentCount)> CheckCustomerNotificationLimitAsync(Guid companyId, int customerId, int maxNotifications)
    {
        try
        {
            // Son 30 gün içinde bu müşteriye gönderilen hatırlatma sayısını al
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            var sentCount = await _context.BasketReminderLogs
                .CountAsync(brl => brl.CompanyId == companyId &&
                                 brl.CustomerId == customerId &&
                                 brl.IsSuccessful &&
                                 brl.SentAt >= thirtyDaysAgo);

            if (sentCount >= maxNotifications)
            {
                return (false, $"Müşteri son 30 gün içinde maksimum bildirim limitine ulaştı ({sentCount}/{maxNotifications}).", sentCount);
            }

            return (true, "Müşteri bildirim limiti uygun.", sentCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking customer notification limit for customer {CustomerId}", customerId);
            return (false, "Müşteri bildirim limiti kontrolü sırasında hata oluştu.", 0);
        }
    }

    public async Task<(bool CanSend, string Message, int SentCount)> CheckCustomerNotificationLimitForScheduleAsync(Guid companyId, int customerId, int scheduleId, int maxNotifications)
    {
        try
        {
            // Son 30 gün içinde bu müşteriye bu schedule için gönderilen hatırlatma sayısını al
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);
            var sentCount = await _context.BasketReminderLogs
                .CountAsync(brl => brl.CompanyId == companyId &&
                                 brl.CustomerId == customerId &&
                                 brl.BasketReminderScheduleId == scheduleId &&
                                 brl.IsSuccessful &&
                                 brl.SentAt >= thirtyDaysAgo);

            if (sentCount >= maxNotifications)
            {
                return (false, $"Müşteri bu zamanlama için son 30 gün içinde maksimum bildirim limitine ulaştı ({sentCount}/{maxNotifications}).", sentCount);
            }

            return (true, "Müşteri bu zamanlama için bildirim limiti uygun.", sentCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking customer notification limit for schedule {ScheduleId} and customer {CustomerId}", scheduleId, customerId);
            return (false, "Müşteri bildirim limiti kontrolü sırasında hata oluştu.", 0);
        }
    }

    #endregion

    #region Logs and Statistics

    public async Task<List<BasketReminderLog>> GetReminderLogsAsync(Guid companyId, int page = 1, int pageSize = 50)
    {
        return await _context.BasketReminderLogs
            .Where(brl => brl.CompanyId == companyId)
            .OrderByDescending(brl => brl.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<(List<BasketReminderLog> Logs, bool HasMore)> GetLogsForScheduleAsync(
        Guid companyId,
        int? scheduleId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool? isSuccessful = null,
        int page = 1,
        int pageSize = 20)
    {
        // Base query
        var query = _context.BasketReminderLogs
            .Include(brl => brl.BasketReminderSchedule)
            .Where(brl => brl.CompanyId == companyId);

        // Apply filters
        if (scheduleId.HasValue)
        {
            query = query.Where(brl => brl.BasketReminderScheduleId == scheduleId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(brl => brl.SentAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            // Include the entire end date
            var endOfDay = endDate.Value.Date.AddDays(1);
            query = query.Where(brl => brl.SentAt < endOfDay);
        }

        if (isSuccessful.HasValue)
        {
            query = query.Where(brl => brl.IsSuccessful == isSuccessful.Value);
        }

        // Fetch pageSize + 1 to check if there are more records
        var logs = await query
            .OrderByDescending(brl => brl.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .ToListAsync();

        // Check if there are more records
        var hasMore = logs.Count > pageSize;

        // Return only pageSize records
        if (hasMore)
        {
            logs = logs.Take(pageSize).ToList();
        }

        return (logs, hasMore);
    }

    public async Task<BasketReminderStatsViewModel> GetReminderStatsAsync(Guid companyId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var today = DateTime.Now.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        startDate ??= today.AddDays(-7);
        endDate ??= today.AddDays(1);

        var stats = new BasketReminderStatsViewModel();

        // Bugünkü istatistikler
        var todayLogs = await _context.BasketReminderLogs
            .Where(brl => brl.CompanyId == companyId && brl.SentAt >= today)
            .ToListAsync();

        stats.TotalRemindersToday = todayLogs.Count;
        stats.SuccessfulRemindersToday = todayLogs.Count(l => l.IsSuccessful);
        stats.FailedRemindersToday = todayLogs.Count(l => !l.IsSuccessful);
        stats.SuccessRateToday = stats.TotalRemindersToday > 0 ?
            (decimal)stats.SuccessfulRemindersToday / stats.TotalRemindersToday * 100 : 0;

        // Haftalık istatistikler
        stats.TotalRemindersThisWeek = await _context.BasketReminderLogs
            .CountAsync(brl => brl.CompanyId == companyId && brl.SentAt >= weekStart);

        // Aylık istatistikler
        stats.TotalRemindersThisMonth = await _context.BasketReminderLogs
            .CountAsync(brl => brl.CompanyId == companyId && brl.SentAt >= monthStart);

        // Aktif zamanlama sayısı
        stats.ActiveSchedulesCount = await _context.BasketReminderSchedules
            .CountAsync(brs => brs.CompanyId == companyId && brs.IsActive);

        // Uygun sepet sayısı (örnek olarak son 24 saat)
        stats.EligibleBasketsCount = await GetEligibleBasketsForReminderAsync(companyId, 24)
            .ContinueWith(t => t.Result.Count);

        // Günlük istatistikler (grafik için)
        var dailyStats = await _context.BasketReminderLogs
            .Where(brl => brl.CompanyId == companyId && brl.SentAt >= startDate && brl.SentAt < endDate)
            .GroupBy(brl => brl.SentAt.Date)
            .Select(g => new DailyReminderStatsViewModel
            {
                Date = g.Key,
                TotalReminders = g.Count(),
                SuccessfulReminders = g.Count(l => l.IsSuccessful),
                FailedReminders = g.Count(l => !l.IsSuccessful)
            })
            .OrderBy(ds => ds.Date)
            .ToListAsync();

        stats.DailyStats = dailyStats;

        return stats;
    }

    public async Task CreateReminderLogAsync(BasketReminderLog log)
    {
        try
        {
            _context.BasketReminderLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating basket reminder log for basket {BasketExternalId}", log.BasketExternalId);
        }
    }

    public async Task CreateReminderLogAsync(BasketReminderLog log, int? scheduleId)
    {
        try
        {
            log.BasketReminderScheduleId = scheduleId;
            _context.BasketReminderLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating basket reminder log for basket {BasketExternalId} with schedule {ScheduleId}",
                log.BasketExternalId, scheduleId);
        }
    }

    #endregion

    #region Helper Methods

    public string ReplacePlaceholders(string content, string firstName, string lastName)
    {
        if (string.IsNullOrEmpty(content))
            return content;

        var fullName = $"{firstName} {lastName}".Trim();

        return content
            // İngilizce placeholder'lar
            .Replace("{FirstName}", firstName ?? "")
            .Replace("{LastName}", lastName ?? "")
            .Replace("{FullName}", fullName)
            // Türkçe placeholder'lar (geriye dönük uyumluluk için)
            .Replace("{ISIM}", firstName ?? "")
            .Replace("{SOYISIM}", lastName ?? "")
            .Replace("{ADSOYAD}", fullName)
            // Küçük harf versiyonları
            .Replace("{isim}", firstName ?? "")
            .Replace("{soyisim}", lastName ?? "")
            .Replace("{adsoyad}", fullName);
    }

    public async Task<List<Basket>> GetEligibleBasketsForReminderAsync(Guid companyId, int reminderTimeForHours, int? scheduleId = null)
    {
        try
        {
            var cutoffTime = DateTime.Now.AddHours(-reminderTimeForHours);

            // Hatırlatma zamanı geçmiş, aktif, terk edilmiş sepetleri al
            var eligibleBaskets = await _context.Baskets
                .Where(b => b.CompanyId == companyId &&
                           b.IsActive &&
                           b.LastUpdateDate.HasValue &&
                           b.LastUpdateDate.Value <= cutoffTime &&
                           !string.IsNullOrEmpty(b.CustomerEmail))
                .ToListAsync();

            // Schedule'dan AllowResendForSentCustomers kontrolü
            bool allowResend = false;
            if (scheduleId.HasValue)
            {
                var schedule = await _context.BasketReminderSchedules
                    .FirstOrDefaultAsync(brs => brs.Id == scheduleId.Value && brs.CompanyId == companyId);
                allowResend = schedule?.AllowResendForSentCustomers ?? false;
            }

            // Eğer tekrar gönderime izin verilmiyorsa, zaten hatırlatma gönderilmiş sepetleri filtrele
            if (!allowResend)
            {
                var basketIds = eligibleBaskets.Select(b => b.ExternalId).ToList();
                var query = _context.BasketReminderLogs
                    .Where(brl => basketIds.Contains(brl.BasketExternalId) &&
                                 brl.ReminderTimeForHours == reminderTimeForHours);

                // Eğer scheduleId varsa, o schedule için gönderilmiş olanları kontrol et
                if (scheduleId.HasValue)
                {
                    query = query.Where(brl => brl.BasketReminderScheduleId == scheduleId.Value);
                }

                var alreadySentBaskets = await query
                    .Select(brl => brl.BasketId)
                    .ToListAsync();

                // Zaten hatırlatma gönderilmiş sepetleri filtrele
                var filteredBaskets = eligibleBaskets
                    .Where(b => !alreadySentBaskets.Contains(b.Id))
                    .ToList();

                _logger.LogInformation("Found {EligibleCount} eligible baskets for {Hours}h reminder for company {CompanyId} (resend disabled, scheduleId: {ScheduleId})",
                    filteredBaskets.Count, reminderTimeForHours, companyId, scheduleId);

                return filteredBaskets;
            }
            else
            {
                _logger.LogInformation("Found {EligibleCount} eligible baskets for {Hours}h reminder for company {CompanyId} (resend enabled, scheduleId: {ScheduleId})",
                    eligibleBaskets.Count, reminderTimeForHours, companyId, scheduleId);

                return eligibleBaskets;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting eligible baskets for company {CompanyId}", companyId);
            return new List<Basket>();
        }
    }

    public async Task<List<CommunicationChannel>> GetAvailableChannelsAsync(Guid companyId)
    {
        return await _communicationChannelService.GetAvailableChannelsAsync(companyId);
    }









    #endregion
}
