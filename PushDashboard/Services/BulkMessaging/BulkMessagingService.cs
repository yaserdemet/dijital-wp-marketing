using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.ViewModels;
using PushDashboard.Services.Notifications;
using PushDashboard.Services.WhatsApp;
using PushDashboard.Services.SMS;
using System.Text.Json;
using PushDashboard.Helpers;

namespace PushDashboard.Services.BulkMessaging;

public class BulkMessagingService : IBulkMessagingService
{
    private readonly ApplicationDbContext _context;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly INotificationChannelFactory _notificationChannelFactory;
    private readonly ITemplateModuleService _templateModuleService;
    private readonly IWhatsAppTemplateComponentService _whatsAppTemplateComponentService;
    private readonly ILogger<BulkMessagingService> _logger;

    public BulkMessagingService(
        ApplicationDbContext context,
        IServiceScopeFactory scopeFactory,
        INotificationChannelFactory notificationChannelFactory,
        ITemplateModuleService templateModuleService,
        IWhatsAppTemplateComponentService whatsAppTemplateComponentService,
        ILogger<BulkMessagingService> logger)
    {
        _context = context;
        _scopeFactory = scopeFactory;
        _notificationChannelFactory = notificationChannelFactory;
        _templateModuleService = templateModuleService;
        _whatsAppTemplateComponentService = whatsAppTemplateComponentService;
        _logger = logger;
    }

    public async Task<bool> HasBulkMessagingModuleAsync(Guid companyId)
    {
        try
        {
            // Sahiplik (IsOwned), aktiflik (IsActive) ve pasif olmama (!IsPassive) kontrolü
            var hasModule = await _context.CompanyModules
                .Include(cm => cm.Module)
                .AnyAsync(cm => cm.CompanyId == companyId &&
                              cm.Module.Name == "Toplu Mesaj Gönderimi" &&
                              cm.IsOwned &&
                              cm.IsActive &&
                              !cm.IsPassive);

            return hasModule;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking bulk messaging module for company {CompanyId}", companyId);
            return false;
        }
    }

    public async Task<Dictionary<string, object>?> GetModuleSettingsAsync(Guid companyId)
    {
        try
        {
            // Sahiplik kontrolü için IsOwned kullan
            var companyModule = await _context.CompanyModules
                .Include(cm => cm.Module)
                .Include(cm => cm.Settings)
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId &&
                                         cm.Module.Name == "Toplu Mesaj Gönderimi" &&
                                         cm.IsOwned);

            if (companyModule?.Settings != null)
            {
                return companyModule.Settings.Settings;
            }

            // Varsayılan ayarları al
            if (companyModule?.Module?.DefaultSettings != null)
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(companyModule.Module.DefaultSettings);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting module settings for company {CompanyId}", companyId);
            return null;
        }
    }

    public async Task<List<string>> GetEnabledChannelsAsync(Guid companyId)
    {
        try
        {
            var settings = await GetModuleSettingsAsync(companyId);
            if (settings == null) return new List<string>();

            if (settings.TryGetValue("enabledChannels", out var channelsObj))
            {
                if (channelsObj is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
                {
                    return jsonElement.EnumerateArray()
                        .Where(x => x.ValueKind == JsonValueKind.String)
                        .Select(x => x.GetString()!)
                        .Where(x => !string.IsNullOrEmpty(x))
                        .ToList();
                }
            }

            return new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting enabled channels for company {CompanyId}", companyId);
            return new List<string>();
        }
    }

    public async Task<(bool Success, string Message)> UpdateModuleSettingsAsync(Guid companyId, Dictionary<string, object> settings, string userId)
    {
        try
        {
            // Sahiplik kontrolü için IsOwned kullan
            var companyModule = await _context.CompanyModules
                .Include(cm => cm.Module)
                .Include(cm => cm.Settings)
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId &&
                                         cm.Module.Name == "Toplu Mesaj Gönderimi" &&
                                         cm.IsOwned);

            if (companyModule == null)
            {
                return (false, "Toplu mesaj gönderimi modülü bulunamadı.");
            }

            if (companyModule.Settings == null)
            {
                // Yeni ayarlar oluştur
                companyModule.Settings = new CompanyModuleSettings
                {
                    CompanyModuleId = companyModule.Id,
                    SettingsJson = JsonSerializer.Serialize(settings),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    UpdatedByUserId = userId
                };

                _context.CompanyModuleSettings.Add(companyModule.Settings);
            }
            else
            {
                // Mevcut ayarları güncelle
                companyModule.Settings.Settings = settings;
                companyModule.Settings.UpdatedByUserId = userId;
                _context.CompanyModuleSettings.Update(companyModule.Settings);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Bulk messaging module settings updated for company {CompanyId} by user {UserId}", companyId, userId);
            return (true, "Modül ayarları başarıyla güncellendi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating module settings for company {CompanyId}", companyId);
            return (false, "Ayarlar güncellenirken hata oluştu: " + ex.Message);
        }
    }

    public async Task<List<AvailableChannelViewModel>> GetAvailableChannelsAsync(Guid companyId)
    {
        try
        {
            var availableChannels = new List<AvailableChannelViewModel>();
            var settings = await GetModuleSettingsAsync(companyId);

            // Email kanalını kontrol et
            var emailChannel = await _notificationChannelFactory.GetChannelAsync(companyId, "email");
            if (emailChannel != null)
            {
                var emailCost = 0m;
                availableChannels.Add(new AvailableChannelViewModel
                {
                    ChannelType = "email",
                    DisplayName = "Email",
                    IsConfigured = true,
                    CostPerMessage = emailCost,
                    AvailableTemplates = await _templateModuleService.GetChannelTemplatesAsync(companyId, "email", "BulkMessaging")
                });
            }

            // SMS kanalını kontrol et
            var smsChannel = await _notificationChannelFactory.GetChannelAsync(companyId, "sms");
            if (smsChannel != null)
            {
                var smsCost = 0m;
                availableChannels.Add(new AvailableChannelViewModel
                {
                    ChannelType = "sms",
                    DisplayName = "SMS",
                    IsConfigured = true,
                    CostPerMessage = smsCost,
                    AvailableTemplates = await _templateModuleService.GetChannelTemplatesAsync(companyId, "sms", "BulkMessaging")
                });
            }

            // WhatsApp kanalını kontrol et
            var whatsAppChannel = await _notificationChannelFactory.GetChannelAsync(companyId, "whatsapp");
            if (whatsAppChannel != null)
            {
                var whatsAppCost = GetChannelCost(settings, "whatsapp", 1.00m);
                availableChannels.Add(new AvailableChannelViewModel
                {
                    ChannelType = "whatsapp",
                    DisplayName = "WhatsApp",
                    IsConfigured = true,
                    CostPerMessage = whatsAppCost,
                    AvailableTemplates = await _templateModuleService.GetChannelTemplatesAsync(companyId, "whatsapp", "BulkMessaging")
                });
            }

            return availableChannels;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available channels for company {CompanyId}", companyId);
            return new List<AvailableChannelViewModel>();
        }
    }

    public async Task<int> EstimateRecipientsAsync(Guid companyId, CustomerFilterViewModel filters)
    {
        try
        {
            var query = _context.Customers.Where(c => c.CompanyId == companyId);

            // Aktif/pasif filtresi
            if (filters.IsActive.HasValue)
            {
                query = query.Where(c => c.IsActive == filters.IsActive.Value);
            }

            // Email izni filtresi
            if (filters.EmailPermission.HasValue)
            {
                query = query.Where(c => c.EmailPermission == filters.EmailPermission.Value);
            }

            // SMS izni filtresi
            if (filters.SmsPermission.HasValue)
            {
                query = query.Where(c => c.SmsPermission == filters.SmsPermission.Value);
            }

            // Şehir filtresi
            if (filters.Cities != null && filters.Cities.Any())
            {
                query = query.Where(c => c.City != null && filters.Cities.Contains(c.City));
            }

            // Kaynak filtresi
            if (filters.Sources != null && filters.Sources.Any())
            {
                var sourceEnums = filters.Sources
                    .Select(s => Enum.TryParse<CustomerSource>(s, true, out var src) ? (CustomerSource?)src : null)
                    .Where(s => s.HasValue)
                    .Select(s => s!.Value)
                    .ToList();
                if (sourceEnums.Any())
                {
                    query = query.Where(c => sourceEnums.Contains(c.Source));
                }
            }

            // Üyelik tarihi filtresi
            if (filters.MembershipDateFrom.HasValue)
            {
                query = query.Where(c => c.MembershipDate >= filters.MembershipDateFrom.Value);
            }

            if (filters.MembershipDateTo.HasValue)
            {
                query = query.Where(c => c.MembershipDate <= filters.MembershipDateTo.Value);
            }

            // Son giriş tarihi filtresi
            if (filters.LastLoginDateFrom.HasValue)
            {
                query = query.Where(c => c.LastLoginDate >= filters.LastLoginDateFrom.Value);
            }

            if (filters.LastLoginDateTo.HasValue)
            {
                query = query.Where(c => c.LastLoginDate <= filters.LastLoginDateTo.Value);
            }

            // Arama terimi filtresi
            if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
            {
                var searchLower = filters.SearchTerm.ToLower();
                query = query.Where(c =>
                    c.FirstName.ToLower().Contains(searchLower) ||
                    c.LastName.ToLower().Contains(searchLower) ||
                    c.Email.ToLower().Contains(searchLower) ||
                    (c.Phone != null && c.Phone.Contains(filters.SearchTerm)) ||
                    (c.MobilePhone != null && c.MobilePhone.Contains(filters.SearchTerm)));
            }

            return await query.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error estimating recipients for company {CompanyId}", companyId);
            return 0;
        }
    }

    // Helper methods
    private decimal GetChannelCost(Dictionary<string, object>? settings, string channelType, decimal defaultCost)
    {
        if (settings == null) return defaultCost;

        var costKey = $"{channelType}Cost";
        if (settings.TryGetValue(costKey, out var costObj))
        {
            if (decimal.TryParse(costObj.ToString(), out decimal cost))
            {
                return cost;
            }
        }

        return defaultCost;
    }

    private string TruncateContent(string content, int maxLength)
    {
        if (string.IsNullOrEmpty(content)) return string.Empty;
        if (content.Length <= maxLength) return content;
        return content.Substring(0, maxLength) + "...";
    }

    public async Task<BulkMessagePreviewViewModel> CreatePreviewAsync(Guid companyId, CreateBulkMessageViewModel model)
    {
        try
        {
            var preview = new BulkMessagePreviewViewModel();

            // Hedef müşteri sayısını hesapla
            preview.EstimatedRecipients = await EstimateRecipientsAsync(companyId, model.CustomerFilters);

            // Kanal önizlemelerini oluştur
            var settings = await GetModuleSettingsAsync(companyId);
            decimal totalCost = 0;

            foreach (var channelSetting in model.ChannelSettings.Where(cs => cs.IsEnabled))
            {
                var channelCost = GetChannelCost(settings, channelSetting.ChannelType,
                    channelSetting.ChannelType == "email" ? 0.50m : 1.00m);

                var channelTotalCost = channelCost * preview.EstimatedRecipients;
                totalCost += channelTotalCost;

                var channelPreview = new ChannelPreviewViewModel
                {
                    ChannelType = channelSetting.ChannelType,
                    TemplateName = channelSetting.TemplateName,
                    Subject = channelSetting.Subject,
                    Content = channelSetting.Content ?? "Şablon içeriği yüklenecek...",
                    EstimatedRecipients = preview.EstimatedRecipients,
                    CostPerMessage = channelCost,
                    TotalCost = channelTotalCost
                };

                preview.ChannelPreviews.Add(channelPreview);
            }

            preview.EstimatedCost = totalCost;

            // Bakiye kontrolü
            var (canAfford, currentBalance, _) = await CheckBalanceAsync(companyId, totalCost);
            preview.CanAfford = canAfford;
            preview.CurrentBalance = currentBalance;

            if (!canAfford)
            {
                preview.WarningMessage = $"Yetersiz bakiye! Gerekli: ₺{totalCost:N2}, Mevcut: ₺{currentBalance:N2}";
            }

            // Örnek müşteriler (ilk 5)
            var sampleCustomers = await GetFilteredCustomersAsync(companyId, model.CustomerFilters);
            preview.SampleCustomers = sampleCustomers.Take(5).Select(c => new CustomerPreviewViewModel
            {
                Id = c.Id,
                FullName = c.FullName,
                Email = c.Email,
                Phone = c.Phone,
                MobilePhone = c.MobilePhone,
                EmailPermission = c.EmailPermission,
                SmsPermission = c.SmsPermission,
                City = c.City,
                MembershipDate = c.MembershipDate,
                AvailableChannels = GetCustomerChannels(c, model.ChannelSettings.Where(cs => cs.IsEnabled).Select(cs => cs.ChannelType).ToList())
            }).ToList();

            return preview;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating preview for company {CompanyId}", companyId);
            return new BulkMessagePreviewViewModel
            {
                WarningMessage = "Önizleme oluşturulurken hata oluştu: " + ex.Message
            };
        }
    }

    public async Task<(bool Success, string Message, int? BulkMessageId)> CreateBulkMessageAsync(Guid companyId, string userId, CreateBulkMessageViewModel model)
    {
        try
        {
            // Modül kontrolü
            if (!await HasBulkMessagingModuleAsync(companyId))
            {
                return (false, "Toplu mesaj gönderimi modülüne sahip değilsiniz.", null);
            }

            // Hedef müşteri sayısını hesapla
            var recipientCount = await EstimateRecipientsAsync(companyId, model.CustomerFilters);
            if (recipientCount == 0)
            {
                return (false, "Belirtilen filtrelere uygun müşteri bulunamadı.", null);
            }

            // Maliyet hesapla
            var enabledChannels = model.ChannelSettings.Where(cs => cs.IsEnabled).Select(cs => cs.ChannelType).ToList();
            var estimatedCost = await CalculateCostAsync(companyId, enabledChannels, recipientCount);

            // Bakiye kontrolü
            var (canAfford, currentBalance, _) = await CheckBalanceAsync(companyId, estimatedCost);
            if (!canAfford)
            {
                return (false, $"Yetersiz bakiye! Gerekli: ₺{estimatedCost:N2}, Mevcut: ₺{currentBalance:N2}", null);
            }

            // BulkMessage oluştur
            var bulkMessage = new BulkMessage
            {
                CompanyId = companyId,
                UserId = userId,
                Title = model.Title,
                Description = model.Description,
                CustomerFiltersJson = JsonSerializer.Serialize(model.CustomerFilters),
                ChannelSettingsJson = JsonSerializer.Serialize(model.ChannelSettings.Where(cs => cs.IsEnabled)
                    .ToDictionary(cs => cs.ChannelType, cs => new { cs.TemplateName, cs.Subject })),
                Status = "Hazırlanıyor",
                TotalRecipients = recipientCount,
                EstimatedCost = estimatedCost,
                CreatedAt = DateTime.Now
            };

            _context.BulkMessages.Add(bulkMessage);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Bulk message created with ID {BulkMessageId} for company {CompanyId} by user {UserId}",
                bulkMessage.Id, companyId, userId);

            return (true, "Toplu mesaj gönderimi başarıyla oluşturuldu.", bulkMessage.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bulk message for company {CompanyId}", companyId);
            return (false, "Toplu mesaj oluşturulurken hata oluştu: " + ex.Message, null);
        }
    }

    public async Task<List<Customer>> GetFilteredCustomersAsync(Guid companyId, CustomerFilterViewModel filters)
    {
        try
        {
            var query = _context.Customers.Where(c => c.CompanyId == companyId);

            // Filtreleri uygula (EstimateRecipientsAsync ile aynı mantık)
            if (filters.IsActive.HasValue)
            {
                query = query.Where(c => c.IsActive == filters.IsActive.Value);
            }

            if (filters.EmailPermission.HasValue)
            {
                query = query.Where(c => c.EmailPermission == filters.EmailPermission.Value);
            }

            if (filters.SmsPermission.HasValue)
            {
                query = query.Where(c => c.SmsPermission == filters.SmsPermission.Value);
            }

            if (filters.Cities != null && filters.Cities.Any())
            {
                query = query.Where(c => c.City != null && filters.Cities.Contains(c.City));
            }

            // Kaynak filtresi
            if (filters.Sources != null && filters.Sources.Any())
            {
                var sourceEnums = filters.Sources
                    .Select(s => Enum.TryParse<CustomerSource>(s, true, out var src) ? (CustomerSource?)src : null)
                    .Where(s => s.HasValue)
                    .Select(s => s!.Value)
                    .ToList();
                if (sourceEnums.Any())
                {
                    query = query.Where(c => sourceEnums.Contains(c.Source));
                }
            }

            if (filters.MembershipDateFrom.HasValue)
            {
                query = query.Where(c => c.MembershipDate >= filters.MembershipDateFrom.Value);
            }

            if (filters.MembershipDateTo.HasValue)
            {
                query = query.Where(c => c.MembershipDate <= filters.MembershipDateTo.Value);
            }

            if (filters.LastLoginDateFrom.HasValue)
            {
                query = query.Where(c => c.LastLoginDate >= filters.LastLoginDateFrom.Value);
            }

            if (filters.LastLoginDateTo.HasValue)
            {
                query = query.Where(c => c.LastLoginDate <= filters.LastLoginDateTo.Value);
            }

            if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
            {
                var searchLower = filters.SearchTerm.ToLower();
                query = query.Where(c =>
                    c.FirstName.ToLower().Contains(searchLower) ||
                    c.LastName.ToLower().Contains(searchLower) ||
                    c.Email.ToLower().Contains(searchLower) ||
                    (c.Phone != null && c.Phone.Contains(filters.SearchTerm)) ||
                    (c.MobilePhone != null && c.MobilePhone.Contains(filters.SearchTerm)));
            }

            return await query.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting filtered customers for company {CompanyId}", companyId);
            return new List<Customer>();
        }
    }

    public async Task<decimal> CalculateCostAsync(Guid companyId, List<string> channels, int recipientCount)
    {
        try
        {
            //var settings = await GetModuleSettingsAsync(companyId);
            decimal totalCost = 0;

            foreach (var channel in channels)
            {
                var defaultCost = channel.ToLower() switch
                {
                    "email" => 0.00m,
                    "sms" => 0.00m,
                    "whatsapp" => 1.00m,
                    _ => 1.00m
                };
                //var channelCost = GetChannelCost(settings, channel, defaultCost);
                totalCost += defaultCost * recipientCount;
            }

            return totalCost;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating cost for company {CompanyId}", companyId);
            return 0;
        }
    }

    public async Task<(bool CanAfford, decimal CurrentBalance, decimal RequiredAmount)> CheckBalanceAsync(Guid companyId, decimal requiredAmount)
    {
        try
        {
            var company = await _context.Companies.FindAsync(companyId);
            if (company == null)
            {
                return (false, 0, requiredAmount);
            }

            return (company.CreditBalance >= requiredAmount, company.CreditBalance, requiredAmount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking balance for company {CompanyId}", companyId);
            return (false, 0, requiredAmount);
        }
    }

    private List<string> GetCustomerChannels(Customer customer, List<string> requestedChannels)
    {
        var availableChannels = new List<string>();

        foreach (var channel in requestedChannels)
        {
            switch (channel.ToLower())
            {
                case "email":
                    if (customer.EmailPermission && !string.IsNullOrEmpty(customer.Email))
                        availableChannels.Add("email");
                    break;
                case "sms":
                    if (customer.SmsPermission && (!string.IsNullOrEmpty(customer.Phone) || !string.IsNullOrEmpty(customer.MobilePhone)))
                        availableChannels.Add("sms");
                    break;
                case "whatsapp":
                    if (customer.SmsPermission && (!string.IsNullOrEmpty(customer.Phone) || !string.IsNullOrEmpty(customer.MobilePhone)))
                        availableChannels.Add("whatsapp");
                    break;
            }
        }

        return availableChannels;
    }

    public async Task<Dictionary<int, List<string>>> GetCustomerAvailableChannelsAsync(Guid companyId, List<int> customerIds, List<string> requestedChannels)
    {
        try
        {
            var customers = await _context.Customers
                .Where(c => c.CompanyId == companyId && customerIds.Contains(c.Id))
                .ToListAsync();

            var result = new Dictionary<int, List<string>>();

            foreach (var customer in customers)
            {
                result[customer.Id] = GetCustomerChannels(customer, requestedChannels);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer available channels for company {CompanyId}", companyId);
            return new Dictionary<int, List<string>>();
        }
    }

    public async Task<(bool Success, string Message)> StartBulkMessageAsync(int bulkMessageId, string userId)
    {
        try
        {
            var bulkMessage = await _context.BulkMessages
                .FirstOrDefaultAsync(bm => bm.Id == bulkMessageId);

            if (bulkMessage == null)
            {
                return (false, "Toplu mesaj bulunamadı.");
            }

            if (bulkMessage.Status != "Hazırlanıyor")
            {
                return (false, $"Toplu mesaj zaten {bulkMessage.Status} durumunda.");
            }

            // Bakiye kontrolü
            var (canAfford, currentBalance, _) = await CheckBalanceAsync(bulkMessage.CompanyId, bulkMessage.EstimatedCost);
            if (!canAfford)
            {
                return (false, $"Yetersiz bakiye! Gerekli: ₺{bulkMessage.EstimatedCost:N2}, Mevcut: ₺{currentBalance:N2}");
            }

            // Durumu güncelle
            bulkMessage.Status = "Sırada";
            bulkMessage.StartedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // Background job başlat
            _ = Task.Run(() => ProcessBulkMessageAsync(bulkMessageId));

            _logger.LogInformation("Bulk message {BulkMessageId} started by user {UserId}", bulkMessageId, userId);
            return (true, "Toplu mesaj gönderimi başlatıldı.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting bulk message {BulkMessageId}", bulkMessageId);
            return (false, "Toplu mesaj başlatılırken hata oluştu: " + ex.Message);
        }
    }

    public async Task<(bool Success, string Message)> CancelBulkMessageAsync(int bulkMessageId, string userId)
    {
        try
        {
            var bulkMessage = await _context.BulkMessages
                .FirstOrDefaultAsync(bm => bm.Id == bulkMessageId);

            if (bulkMessage == null)
            {
                return (false, "Toplu mesaj bulunamadı.");
            }

            if (bulkMessage.Status == "Tamamlandı")
            {
                return (false, "Tamamlanmış toplu mesaj iptal edilemez.");
            }

            bulkMessage.Status = "İptal";
            bulkMessage.CompletedAt = DateTime.Now;
            bulkMessage.ErrorMessage = $"Kullanıcı tarafından iptal edildi ({userId})";

            await _context.SaveChangesAsync();

            _logger.LogInformation("Bulk message {BulkMessageId} cancelled by user {UserId}", bulkMessageId, userId);
            return (true, "Toplu mesaj gönderimi iptal edildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling bulk message {BulkMessageId}", bulkMessageId);
            return (false, "Toplu mesaj iptal edilirken hata oluştu: " + ex.Message);
        }
    }

    public async Task<(bool Success, string Message, int? BulkMessageId)> ResendBulkMessageAsync(
        int originalBulkMessageId, int startIndex, int count, Guid companyId, string userId)
    {
        try
        {
            // Modül kontrolü
            if (!await HasBulkMessagingModuleAsync(companyId))
            {
                return (false, "Toplu mesaj gönderimi modülüne sahip değilsiniz.", null);
            }

            // Orijinal mesajı bul
            var originalMessage = await _context.BulkMessages
                .FirstOrDefaultAsync(bm => bm.Id == originalBulkMessageId && bm.CompanyId == companyId);

            if (originalMessage == null)
            {
                return (false, "Orijinal toplu mesaj bulunamadı.", null);
            }

            if (originalMessage.Status != "Tamamlandı" && originalMessage.Status != "Hata")
            {
                return (false, "Sadece tamamlanmış veya hatalı toplu mesajlar tekrar gönderilebilir.", null);
            }

            // Parametre doğrulama
            if (startIndex < 1)
            {
                return (false, "Başlangıç sırası en az 1 olmalıdır.", null);
            }

            if (count < 1)
            {
                return (false, "Gönderilecek adet en az 1 olmalıdır.", null);
            }

            // Orijinal job'un alıcı listesinden müşterileri al (yeni müşteri eklenmesinden etkilenmemek için)
            var originalRecipients = await _context.BulkMessageRecipients
                .Where(r => r.BulkMessageId == originalBulkMessageId)
                .OrderBy(r => r.Id) // Orijinal sırayı koru
                .Select(r => r.CustomerId)
                .ToListAsync();

            if (!originalRecipients.Any())
            {
                return (false, "Orijinal gönderimde alıcı kaydı bulunamadı.", null);
            }

            if (startIndex > originalRecipients.Count)
            {
                return (false, $"Başlangıç sırası ({startIndex}) toplam alıcı sayısından ({originalRecipients.Count}) büyük olamaz.", null);
            }

            // Hedef müşteri ID'lerini seç
            var targetCustomerIds = originalRecipients.Skip(startIndex - 1).Take(count).ToList();
            var actualCount = targetCustomerIds.Count;

            if (actualCount == 0)
            {
                return (false, "Belirtilen aralıkta alıcı bulunamadı.", null);
            }

            // Maliyet hesapla
            var enabledChannels = new List<string>();
            if (!string.IsNullOrEmpty(originalMessage.ChannelSettingsJson))
            {
                var channelSettingsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(originalMessage.ChannelSettingsJson);
                if (channelSettingsDict != null)
                {
                    enabledChannels = channelSettingsDict.Keys.ToList();
                }
            }

            var estimatedCost = await CalculateCostAsync(companyId, enabledChannels, actualCount);

            // Bakiye kontrolü
            var (canAfford, currentBalance, _) = await CheckBalanceAsync(companyId, estimatedCost);
            if (!canAfford)
            {
                return (false, $"Yetersiz bakiye! Gerekli: ₺{estimatedCost:N2}, Mevcut: ₺{currentBalance:N2}", null);
            }

            // Yeni BulkMessage oluştur
            var newBulkMessage = new BulkMessage
            {
                CompanyId = companyId,
                UserId = userId,
                Title = $"[Tekrar] {originalMessage.Title}",
                Description = $"Orijinal gönderim #{originalMessage.Id} - Sıra {startIndex}'den {actualCount} kişiye tekrar gönderim",
                CustomerFiltersJson = originalMessage.CustomerFiltersJson,
                ChannelSettingsJson = originalMessage.ChannelSettingsJson,
                Status = "Hazırlanıyor",
                TotalRecipients = actualCount,
                EstimatedCost = estimatedCost,
                CreatedAt = DateTime.Now
            };

            _context.BulkMessages.Add(newBulkMessage);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Resend bulk message created with ID {BulkMessageId} from original {OriginalId} (startIndex={StartIndex}, count={Count}) for company {CompanyId} by user {UserId}",
                newBulkMessage.Id, originalBulkMessageId, startIndex, count, companyId, userId);

            // Bakiye kontrolü ve gönderimi başlat
            newBulkMessage.Status = "Sırada";
            newBulkMessage.StartedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // Background job başlat - özel müşteri listesiyle
            _ = Task.Run(() => ProcessResendBulkMessageAsync(newBulkMessage.Id, targetCustomerIds));

            return (true, $"Tekrar gönderim başarıyla başlatıldı. {actualCount} müşteriye gönderilecek.", newBulkMessage.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating resend bulk message from original {OriginalBulkMessageId}", originalBulkMessageId);
            return (false, "Tekrar gönderim oluşturulurken hata oluştu: " + ex.Message, null);
        }
    }

    /// <summary>
    /// Belirli müşteri ID listesiyle toplu mesaj işler (resend için)
    /// </summary>
    private async Task ProcessResendBulkMessageAsync(int bulkMessageId, List<int> customerIds)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationChannelFactory = scope.ServiceProvider.GetRequiredService<INotificationChannelFactory>();
        var emailTemplateService = scope.ServiceProvider.GetRequiredService<IEmailTemplateService>();
        var whatsAppTemplateService = scope.ServiceProvider.GetRequiredService<IWhatsAppTemplateService>();
        var smsTemplateService = scope.ServiceProvider.GetRequiredService<ISmsTemplateService>();
        var smsProvider = scope.ServiceProvider.GetRequiredService<ISmsProvider>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BulkMessagingService>>();

        try
        {
            var bulkMessage = await context.BulkMessages
                .FirstOrDefaultAsync(bm => bm.Id == bulkMessageId);

            if (bulkMessage == null)
            {
                logger.LogError("Resend bulk message {BulkMessageId} not found for processing", bulkMessageId);
                return;
            }

            logger.LogInformation("Starting to process resend bulk message {BulkMessageId}", bulkMessageId);

            // Durumu güncelle
            bulkMessage.Status = "Gönderiliyor";
            await context.SaveChangesAsync();

            // Belirli müşterileri al
            var customers = await context.Customers
                .Where(c => customerIds.Contains(c.Id))
                .ToListAsync();

            // Orijinal sırayı koru
            var orderedCustomers = customerIds
                .Select(id => customers.FirstOrDefault(c => c.Id == id))
                .Where(c => c != null)
                .ToList()!;

            if (!orderedCustomers.Any())
            {
                bulkMessage.Status = "Hata";
                bulkMessage.ErrorMessage = "Hedef müşteri bulunamadı";
                bulkMessage.CompletedAt = DateTime.Now;
                await context.SaveChangesAsync();
                return;
            }

            // Batch ayarlarını al
            var settings = await GetModuleSettingsWithScopeAsync(context, bulkMessage.CompanyId);
            var batchSize = GetBatchSize(settings, 50);
            var batchDelay = GetBatchDelay(settings, 5);

            // Toplam batch sayısını hesapla
            bulkMessage.TotalBatches = (int)Math.Ceiling((double)orderedCustomers.Count / batchSize);
            bulkMessage.TotalRecipients = orderedCustomers.Count;
            await context.SaveChangesAsync();

            // Alıcı kayıtlarını oluştur
            var recipients = orderedCustomers.Select(c => new BulkMessageRecipient
            {
                BulkMessageId = bulkMessageId,
                CustomerId = c!.Id,
                Status = "Bekliyor",
                CreatedAt = DateTime.Now
            }).ToList();

            context.BulkMessageRecipients.AddRange(recipients);
            await context.SaveChangesAsync();

            // Kanal ayarlarını parse et
            var enabledChannels = new List<string>();
            var channelSettingsDict = new Dictionary<string, object>();

            if (!string.IsNullOrEmpty(bulkMessage.ChannelSettingsJson))
            {
                try
                {
                    channelSettingsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(bulkMessage.ChannelSettingsJson) ?? new Dictionary<string, object>();
                    enabledChannels = channelSettingsDict.Keys.ToList();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error parsing channel settings for resend bulk message {BulkMessageId}", bulkMessageId);
                    bulkMessage.Status = "Hata";
                    bulkMessage.ErrorMessage = "Kanal ayarları parse edilemedi";
                    bulkMessage.CompletedAt = DateTime.Now;
                    await context.SaveChangesAsync();
                    return;
                }
            }

            // Template cache oluştur
            var templateCache = await BuildTemplateCacheAsync(context, emailTemplateService, whatsAppTemplateService, smsTemplateService,
                bulkMessage.CompanyId, channelSettingsDict, logger);

            var totalCost = 0m;

            for (int batchIndex = 0; batchIndex < bulkMessage.TotalBatches; batchIndex++)
            {
                try
                {
                    bulkMessage.CurrentBatch = batchIndex + 1;
                    await context.SaveChangesAsync();

                    var batchCustomers = orderedCustomers.Skip(batchIndex * batchSize).Take(batchSize).ToList();
                    var batchRecipients = recipients.Skip(batchIndex * batchSize).Take(batchSize).ToList();

                    await ProcessBatchWithScopeAsync(context, notificationChannelFactory, bulkMessage.CompanyId,
                        batchCustomers!, batchRecipients, enabledChannels, templateCache, smsProvider, logger);

                    // Batch maliyetini hesapla
                    var batchCost = await CalculateBatchCostWithScopeAsync(context, bulkMessage.CompanyId, batchCustomers!, enabledChannels);
                    totalCost += batchCost;

                    // İstatistikleri güncelle
                    bulkMessage.ProcessedRecipients += batchCustomers.Count;

                    var allRecipients = await context.BulkMessageRecipients
                        .Where(r => r.BulkMessageId == bulkMessageId)
                        .ToListAsync();

                    bulkMessage.SuccessfulSends = allRecipients.Count(r => r.Status == "Gönderildi");
                    bulkMessage.FailedSends = allRecipients.Count(r => r.Status == "Hata");
                    bulkMessage.ActualCost = totalCost;

                    await context.SaveChangesAsync();

                    // Batch arası bekleme
                    if (batchIndex < bulkMessage.TotalBatches - 1)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(batchDelay));
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing batch {BatchIndex} for resend bulk message {BulkMessageId}", batchIndex, bulkMessageId);
                }
            }

            // Tamamlandı
            bulkMessage.Status = "Tamamlandı";
            bulkMessage.CompletedAt = DateTime.Now;
            bulkMessage.ActualCost = totalCost;

            if (totalCost > 0)
            {
                await CreateBulkMessageUsageLogAsync(context, bulkMessage, totalCost, logger);
            }

            await context.SaveChangesAsync();

            logger.LogInformation("Resend bulk message {BulkMessageId} completed. Processed: {Processed}, Success: {Success}, Failed: {Failed}, Cost: {Cost}",
                bulkMessageId, bulkMessage.ProcessedRecipients, bulkMessage.SuccessfulSends, bulkMessage.FailedSends, bulkMessage.ActualCost);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Critical error processing resend bulk message {BulkMessageId}", bulkMessageId);

            try
            {
                var bulkMessage = await context.BulkMessages.FindAsync(bulkMessageId);
                if (bulkMessage != null)
                {
                    bulkMessage.Status = "Hata";
                    bulkMessage.ErrorMessage = ex.Message;
                    bulkMessage.CompletedAt = DateTime.Now;
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception updateEx)
            {
                logger.LogError(updateEx, "Error updating resend bulk message status after failure");
            }
        }
    }

    public async Task<BulkMessageProgressViewModel?> GetProgressAsync(int bulkMessageId)
    {
        try
        {
            var bulkMessage = await _context.BulkMessages
                .FirstOrDefaultAsync(bm => bm.Id == bulkMessageId);

            if (bulkMessage == null) return null;

            var estimatedCompletion = CalculateEstimatedCompletion(bulkMessage);

            return new BulkMessageProgressViewModel
            {
                Id = bulkMessage.Id,
                Status = bulkMessage.Status,
                TotalRecipients = bulkMessage.TotalRecipients,
                ProcessedRecipients = bulkMessage.ProcessedRecipients,
                SuccessfulSends = bulkMessage.SuccessfulSends,
                FailedSends = bulkMessage.FailedSends,
                CurrentBatch = bulkMessage.CurrentBatch,
                TotalBatches = bulkMessage.TotalBatches,
                ActualCost = bulkMessage.ActualCost,
                ErrorMessage = bulkMessage.ErrorMessage,
                StartedAt = bulkMessage.StartedAt,
                EstimatedCompletionTime = estimatedCompletion
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting progress for bulk message {BulkMessageId}", bulkMessageId);
            return null;
        }
    }

    public async Task<List<BulkMessageViewModel>> GetBulkMessageHistoryAsync(Guid companyId, int page = 1, int pageSize = 20)
    {
        try
        {
            var bulkMessages = await _context.BulkMessages
                .Where(bm => bm.CompanyId == companyId)
                .OrderByDescending(bm => bm.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return bulkMessages.Select(bm => new BulkMessageViewModel
            {
                Id = bm.Id,
                Title = bm.Title,
                Description = bm.Description,
                Status = bm.Status,
                TotalRecipients = bm.TotalRecipients,
                ProcessedRecipients = bm.ProcessedRecipients,
                SuccessfulSends = bm.SuccessfulSends,
                FailedSends = bm.FailedSends,
                EstimatedCost = bm.EstimatedCost,
                ActualCost = bm.ActualCost,
                CreatedAt = bm.CreatedAt,
                StartedAt = bm.StartedAt,
                CompletedAt = bm.CompletedAt,
                ErrorMessage = bm.ErrorMessage,
                CurrentBatch = bm.CurrentBatch,
                TotalBatches = bm.TotalBatches,
                StatusBadgeClass = bm.StatusBadgeClass,
                ProgressPercentage = bm.ProgressPercentage,
                SuccessRate = bm.SuccessRate,
                FormattedEstimatedCost = bm.FormattedEstimatedCost,
                FormattedActualCost = bm.FormattedActualCost,
                FormattedCreatedAt = bm.FormattedCreatedAt,
                FormattedDuration = bm.FormattedDuration
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bulk message history for company {CompanyId}", companyId);
            return new List<BulkMessageViewModel>();
        }
    }

    public async Task<BulkMessagingStatsViewModel> GetStatsAsync(Guid companyId)
    {
        try
        {
            var stats = await _context.BulkMessages
                .Where(bm => bm.CompanyId == companyId)
                .GroupBy(bm => 1)
                .Select(g => new BulkMessagingStatsViewModel
                {
                    TotalMessages = g.Count(),
                    ActiveMessages = g.Count(bm => bm.Status == "Gönderiliyor" || bm.Status == "Sırada"),
                    CompletedMessages = g.Count(bm => bm.Status == "Tamamlandı"),
                    TotalRecipients = g.Sum(bm => bm.TotalRecipients),
                    TotalCost = g.Sum(bm => bm.ActualCost),
                    AverageSuccessRate = g.Where(bm => bm.ProcessedRecipients > 0)
                                        .Average(bm => (double)bm.SuccessfulSends / bm.ProcessedRecipients * 100)
                })
                .FirstOrDefaultAsync();

            return stats ?? new BulkMessagingStatsViewModel();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stats for company {CompanyId}", companyId);
            return new BulkMessagingStatsViewModel();
        }
    }

    private DateTime? CalculateEstimatedCompletion(BulkMessage bulkMessage)
    {
        if (bulkMessage.Status != "Gönderiliyor" || bulkMessage.StartedAt == null)
            return null;

        if (bulkMessage.ProcessedRecipients == 0)
            return null;

        var elapsed = DateTime.Now - bulkMessage.StartedAt.Value;
        var avgTimePerRecipient = elapsed.TotalSeconds / bulkMessage.ProcessedRecipients;
        var remainingRecipients = bulkMessage.TotalRecipients - bulkMessage.ProcessedRecipients;
        var estimatedRemainingSeconds = avgTimePerRecipient * remainingRecipients;

        return DateTime.Now.AddSeconds(estimatedRemainingSeconds);
    }

    public async Task<BulkMessageDetailViewModel?> GetBulkMessageDetailsAsync(int bulkMessageId, Guid companyId)
    {
        try
        {
            var bulkMessage = await _context.BulkMessages
                .Include(bm => bm.Recipients)
                .ThenInclude(r => r.Customer)
                .FirstOrDefaultAsync(bm => bm.Id == bulkMessageId && bm.CompanyId == companyId);

            if (bulkMessage == null) return null;

            var bulkMessageViewModel = new BulkMessageViewModel
            {
                Id = bulkMessage.Id,
                Title = bulkMessage.Title,
                Description = bulkMessage.Description,
                Status = bulkMessage.Status,
                TotalRecipients = bulkMessage.TotalRecipients,
                ProcessedRecipients = bulkMessage.ProcessedRecipients,
                SuccessfulSends = bulkMessage.SuccessfulSends,
                FailedSends = bulkMessage.FailedSends,
                EstimatedCost = bulkMessage.EstimatedCost,
                ActualCost = bulkMessage.ActualCost,
                CreatedAt = bulkMessage.CreatedAt,
                StartedAt = bulkMessage.StartedAt,
                CompletedAt = bulkMessage.CompletedAt,
                ErrorMessage = bulkMessage.ErrorMessage,
                CurrentBatch = bulkMessage.CurrentBatch,
                TotalBatches = bulkMessage.TotalBatches,
                StatusBadgeClass = bulkMessage.StatusBadgeClass,
                ProgressPercentage = bulkMessage.ProgressPercentage,
                SuccessRate = bulkMessage.SuccessRate,
                FormattedEstimatedCost = bulkMessage.FormattedEstimatedCost,
                FormattedActualCost = bulkMessage.FormattedActualCost,
                FormattedCreatedAt = bulkMessage.FormattedCreatedAt,
                FormattedDuration = bulkMessage.FormattedDuration
            };

            var recipients = bulkMessage.Recipients.Select(r => new BulkMessageRecipientViewModel
            {
                Id = r.Id,
                CustomerId = r.CustomerId,
                CustomerName = r.Customer.FullName,
                CustomerEmail = r.Customer.Email,
                CustomerPhone = r.Customer.Phone ?? r.Customer.MobilePhone,
                SentChannels = r.SentChannelsList,
                Status = r.Status,
                Cost = r.Cost,
                ProcessedAt = r.ProcessedAt,
                ErrorMessage = r.ErrorMessage,
                StatusBadgeClass = r.StatusBadgeClass,
                FormattedCost = r.FormattedCost,
                FormattedProcessedAt = r.FormattedProcessedAt
            }).ToList();

            var progress = await GetProgressAsync(bulkMessageId);

            // Kanal detaylarını oluştur
            var channelDetails = new List<ChannelPreviewViewModel>();
            var settings = await GetModuleSettingsAsync(companyId);

            // Kanal ayarlarını parse et
            if (!string.IsNullOrEmpty(bulkMessage.ChannelSettingsJson))
            {
                try
                {
                    var channelSettingsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(bulkMessage.ChannelSettingsJson);
                    if (channelSettingsDict != null)
                    {
                        foreach (var channelSetting in channelSettingsDict)
                        {
                            var channelCost = GetChannelCost(settings, channelSetting.Key,
                                channelSetting.Key == "email" ? 0.50m : 1.00m);

                            string templateName = "";
                            try
                            {
                                if (channelSetting.Value is JsonElement jsonElement)
                                {
                                    if (jsonElement.TryGetProperty("TemplateName", out var templateNameElement))
                                    {
                                        templateName = templateNameElement.GetString() ?? "";
                                    }
                                }
                            }
                            catch
                            {
                                templateName = channelSetting.Value.ToString() ?? "";
                            }

                            channelDetails.Add(new ChannelPreviewViewModel
                            {
                                ChannelType = channelSetting.Key,
                                TemplateName = templateName,
                                EstimatedRecipients = bulkMessage.TotalRecipients,
                                CostPerMessage = channelCost,
                                TotalCost = channelCost * bulkMessage.TotalRecipients
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing channel settings for bulk message {BulkMessageId}", bulkMessageId);
                }
            }

            return new BulkMessageDetailViewModel
            {
                BulkMessage = bulkMessageViewModel,
                Recipients = recipients,
                Progress = progress ?? new BulkMessageProgressViewModel(),
                ChannelDetails = channelDetails
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bulk message details for {BulkMessageId}", bulkMessageId);
            return null;
        }
    }

    public async Task ProcessBulkMessageAsync(int bulkMessageId)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notificationChannelFactory = scope.ServiceProvider.GetRequiredService<INotificationChannelFactory>();
        var emailTemplateService = scope.ServiceProvider.GetRequiredService<IEmailTemplateService>();

        var whatsAppTemplateService = scope.ServiceProvider.GetRequiredService<IWhatsAppTemplateService>();
        var smsTemplateService = scope.ServiceProvider.GetRequiredService<ISmsTemplateService>();
        var smsProvider = scope.ServiceProvider.GetRequiredService<ISmsProvider>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BulkMessagingService>>();

        try
        {
            var bulkMessage = await context.BulkMessages
                .FirstOrDefaultAsync(bm => bm.Id == bulkMessageId);

            if (bulkMessage == null)
            {
                logger.LogError("Bulk message {BulkMessageId} not found for processing", bulkMessageId);
                return;
            }

            logger.LogInformation("Starting to process bulk message {BulkMessageId}", bulkMessageId);

            // Durumu güncelle
            bulkMessage.Status = "Gönderiliyor";
            await context.SaveChangesAsync();

            // Hedef müşterileri al - filtreleri uygula
            var customers = await GetFilteredCustomersWithScopeAsync(context, bulkMessage.CompanyId, bulkMessage.CustomerFiltersJson);

            if (!customers.Any())
            {
                bulkMessage.Status = "Hata";
                bulkMessage.ErrorMessage = "Hedef müşteri bulunamadı";
                bulkMessage.CompletedAt = DateTime.Now;
                await context.SaveChangesAsync();
                return;
            }

            // Batch ayarlarını al
            var settings = await GetModuleSettingsWithScopeAsync(context, bulkMessage.CompanyId);
            var batchSize = GetBatchSize(settings, 50);
            var batchDelay = GetBatchDelay(settings, 5);

            // Toplam batch sayısını hesapla
            bulkMessage.TotalBatches = (int)Math.Ceiling((double)customers.Count / batchSize);
            bulkMessage.TotalRecipients = customers.Count;
            await context.SaveChangesAsync();

            // Alıcı kayıtlarını oluştur
            var recipients = customers.Select(c => new BulkMessageRecipient
            {
                BulkMessageId = bulkMessageId,
                CustomerId = c.Id,
                Status = "Bekliyor",
                CreatedAt = DateTime.Now
            }).ToList();

            context.BulkMessageRecipients.AddRange(recipients);
            await context.SaveChangesAsync();

            // Batch'ler halinde işle
            var enabledChannels = new List<string>();
            var channelSettingsDict = new Dictionary<string, object>();

            // Kanal ayarlarını parse et
            if (!string.IsNullOrEmpty(bulkMessage.ChannelSettingsJson))
            {
                try
                {
                    channelSettingsDict = JsonSerializer.Deserialize<Dictionary<string, object>>(bulkMessage.ChannelSettingsJson) ?? new Dictionary<string, object>();
                    enabledChannels = channelSettingsDict.Keys.ToList();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error parsing channel settings for bulk message {BulkMessageId}", bulkMessageId);
                    bulkMessage.Status = "Hata";
                    bulkMessage.ErrorMessage = "Kanal ayarları parse edilemedi";
                    bulkMessage.CompletedAt = DateTime.Now;
                    await context.SaveChangesAsync();
                    return;
                }
            }

            // Template cache'i oluştur (performans için)
            var templateCache = await BuildTemplateCacheAsync(context, emailTemplateService, whatsAppTemplateService, smsTemplateService,
                bulkMessage.CompanyId, channelSettingsDict, logger);

            var totalCost = 0m;

            for (int batchIndex = 0; batchIndex < bulkMessage.TotalBatches; batchIndex++)
            {
                try
                {
                    bulkMessage.CurrentBatch = batchIndex + 1;
                    await context.SaveChangesAsync();

                    var batchCustomers = customers.Skip(batchIndex * batchSize).Take(batchSize).ToList();
                    var batchRecipients = recipients.Skip(batchIndex * batchSize).Take(batchSize).ToList();

                    await ProcessBatchWithScopeAsync(context, notificationChannelFactory, bulkMessage.CompanyId,
                        batchCustomers, batchRecipients, enabledChannels, templateCache, smsProvider, logger);

                    // Batch maliyetini hesapla ve ekle
                    var batchCost = await CalculateBatchCostWithScopeAsync(context, bulkMessage.CompanyId, batchCustomers, enabledChannels);
                    totalCost += batchCost;

                    // İstatistikleri güncelle
                    bulkMessage.ProcessedRecipients += batchCustomers.Count;

                    // Tüm recipients'ları yeniden say
                    var allRecipients = await context.BulkMessageRecipients
                        .Where(r => r.BulkMessageId == bulkMessageId)
                        .ToListAsync();

                    bulkMessage.SuccessfulSends = allRecipients.Count(r => r.Status == "Gönderildi");
                    bulkMessage.FailedSends = allRecipients.Count(r => r.Status == "Hata");
                    bulkMessage.ActualCost = totalCost;

                    await context.SaveChangesAsync();

                    // Batch'ler arası bekleme
                    if (batchIndex < bulkMessage.TotalBatches - 1)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(batchDelay));
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing batch {BatchIndex} for bulk message {BulkMessageId}", batchIndex, bulkMessageId);
                    // Batch hatası durumunda devam et
                }
            }

            // Tamamlandı olarak işaretle
            bulkMessage.Status = "Tamamlandı";
            bulkMessage.CompletedAt = DateTime.Now;
            bulkMessage.ActualCost = totalCost;

            // Toplam kullanım kaydı oluştur
            if (totalCost > 0)
            {
                await CreateBulkMessageUsageLogAsync(context, bulkMessage, totalCost, logger);
            }

            await context.SaveChangesAsync();

            logger.LogInformation("Bulk message {BulkMessageId} processing completed. Processed: {ProcessedRecipients}, Success: {SuccessfulSends}, Failed: {FailedSends}, Cost: {ActualCost}",
                bulkMessageId, bulkMessage.ProcessedRecipients, bulkMessage.SuccessfulSends, bulkMessage.FailedSends, bulkMessage.ActualCost);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Critical error processing bulk message {BulkMessageId}", bulkMessageId);

            try
            {
                var bulkMessage = await context.BulkMessages.FindAsync(bulkMessageId);
                if (bulkMessage != null)
                {
                    bulkMessage.Status = "Hata";
                    bulkMessage.ErrorMessage = ex.Message;
                    bulkMessage.CompletedAt = DateTime.Now;
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception updateEx)
            {
                logger.LogError(updateEx, "Error updating bulk message status after failure");
            }
        }
    }

    private int GetBatchSize(Dictionary<string, object>? settings, int defaultValue)
    {
        if (settings?.TryGetValue("maxRecipientsPerBatch", out var batchSizeObj) == true)
        {
            if (int.TryParse(batchSizeObj.ToString(), out int batchSize))
                return Math.Max(1, Math.Min(batchSize, 100)); // 1-100 arası sınırla
        }
        return defaultValue;
    }

    private int GetBatchDelay(Dictionary<string, object>? settings, int defaultValue)
    {
        if (settings?.TryGetValue("batchDelaySeconds", out var delayObj) == true)
        {
            if (int.TryParse(delayObj.ToString(), out int delay))
                return Math.Max(1, Math.Min(delay, 60)); // 1-60 saniye arası sınırla
        }
        return defaultValue;
    }

    private async Task ProcessBatchWithScopeAsync(ApplicationDbContext context, INotificationChannelFactory notificationChannelFactory,
        Guid companyId, List<Customer> customers, List<BulkMessageRecipient> recipients,
        List<string> enabledChannels, BulkMessageTemplateCache templateCache, ISmsProvider smsProvider, ILogger logger)
    {
        for (int i = 0; i < customers.Count; i++)
        {
            var customer = customers[i];
            var recipient = recipients[i];

            try
            {
                recipient.ProcessedAt = DateTime.Now;
                var sentChannels = new List<string>();
                var totalCost = 0m;

                foreach (var channelType in enabledChannels)
                {
                    try
                    {
                        var canSendToChannel = CanSendToChannel(customer, channelType);
                        if (!canSendToChannel) continue;

                        // Template cache'den template bilgisini al
                        if (!templateCache.Templates.TryGetValue(channelType, out var templateInfo))
                        {
                            logger.LogWarning("Template not found in cache for channel {ChannelType}", channelType);
                            continue;
                        }

                        var channel = await notificationChannelFactory.GetChannelAsync(companyId, channelType);
                        if (channel == null) continue;

                        // Mesaj değişkenlerini hazırla
                        var variables = new Dictionary<string, string>
                        {
                            ["customer_name"] = customer.Name ?? customer.FirstName + " " + customer.LastName,
                            ["phone_number"] = customer.Phone ?? customer.MobilePhone ?? "-",
                            ["email_address"] = string.IsNullOrWhiteSpace(customer.Email) ? "-" : customer.Email,
                            ["customer_city"] = string.IsNullOrWhiteSpace(customer.City) ? "-" : customer.City,
                            ["customer_first_name"] = string.IsNullOrWhiteSpace(customer.FirstName) ? "-" : customer.FirstName,
                            ["customer_last_name"] = string.IsNullOrWhiteSpace(customer.LastName) ? "-": customer.LastName, 
                        };

                        // WhatsApp için özel gönderim
                        bool success = false;
                        if (channelType.ToLower() == "whatsapp")
                        {
                            success = await SendWhatsAppWithTemplateAsync(companyId, customer, templateInfo, variables, logger, $"bulk_{recipient.Id}");
                        }
                        else if (channelType.ToLower() == "sms")
                        {
                            success = await SendSmsWithTemplateAsync(context, smsProvider, companyId, customer, templateInfo, variables, logger, $"bulk_{recipient.Id}");
                        }
                        else
                        {
                            // Email ve diğer kanallar için normal gönderim
                            success = await channel.SendNotificationWithCostTrackingAsync(
                                companyId, customer, templateInfo.Name, variables, "system", null, $"bulk_{recipient.Id}");
                        }

                        if (success)
                        {
                            sentChannels.Add(channelType);
                            var channelCost = await GetChannelCostWithScopeAsync(context, companyId, channelType);
                            totalCost += channelCost;
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error sending message via {ChannelType} to customer {CustomerId}", channelType, customer.Id);
                    }
                }

                // Sonuçları güncelle
                recipient.SentChannelsList = sentChannels;
                recipient.Cost = totalCost;
                recipient.Status = sentChannels.Any() ? "Gönderildi" : "Hata";

                if (!sentChannels.Any())
                {
                    recipient.ErrorMessage = "Hiçbir kanala mesaj gönderilemedi";
                }

                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing customer {CustomerId} in bulk message", customer.Id);
                recipient.Status = "Hata";
                recipient.ErrorMessage = ex.Message;
                recipient.ProcessedAt = DateTime.Now;
                await context.SaveChangesAsync();
            }
        }
    }



    private bool CanSendToChannel(Customer customer, string channelType)
    {
        return channelType.ToLower() switch
        {
            "email" => customer.EmailPermission && !string.IsNullOrEmpty(customer.Email),
            "sms" => customer.SmsPermission && (!string.IsNullOrEmpty(customer.Phone) || !string.IsNullOrEmpty(customer.MobilePhone)),
            "whatsapp" => customer.SmsPermission && (!string.IsNullOrEmpty(customer.Phone) || !string.IsNullOrEmpty(customer.MobilePhone)),
            _ => false
        };
    }

    private string GetTemplateNameFromSettings(Dictionary<string, object> channelSettings, string channelType)
    {
        if (channelSettings.TryGetValue(channelType, out var settingObj))
        {
            try
            {
                if (settingObj is JsonElement jsonElement)
                {
                    if (jsonElement.TryGetProperty("TemplateName", out var templateNameElement))
                    {
                        return templateNameElement.GetString() ?? "";
                    }
                }
                else
                {
                    // Dynamic object olarak parse etmeye çalış
                    var settingStr = settingObj.ToString();
                    if (!string.IsNullOrEmpty(settingStr))
                    {
                        var settingDict = JsonSerializer.Deserialize<Dictionary<string, object>>(settingStr);
                        if (settingDict?.TryGetValue("TemplateName", out var templateName) == true)
                        {
                            return templateName.ToString() ?? "";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing template name for channel {ChannelType}", channelType);
            }
        }

        return "";
    }

    // Scope-aware helper methods for background processing
    private async Task<List<Customer>> GetFilteredCustomersWithScopeAsync(ApplicationDbContext context, Guid companyId, string? filtersJson)
    {
        try
        {
            var query = context.Customers.Where(c => c.CompanyId == companyId);

            if (!string.IsNullOrEmpty(filtersJson))
            {
                var filters = JsonSerializer.Deserialize<CustomerFilterViewModel>(filtersJson);
                if (filters != null)
                {
                    // Aktif/pasif filtresi
                    if (filters.IsActive.HasValue)
                    {
                        query = query.Where(c => c.IsActive == filters.IsActive.Value);
                    }

                    // Email izni filtresi
                    if (filters.EmailPermission.HasValue)
                    {
                        query = query.Where(c => c.EmailPermission == filters.EmailPermission.Value);
                    }

                    // SMS izni filtresi
                    if (filters.SmsPermission.HasValue)
                    {
                        query = query.Where(c => c.SmsPermission == filters.SmsPermission.Value);
                    }

                    // Şehir filtresi
                    if (filters.Cities != null && filters.Cities.Any())
                    {
                        query = query.Where(c => c.City != null && filters.Cities.Contains(c.City));
                    }

                    // Kaynak filtresi
                    if (filters.Sources != null && filters.Sources.Any())
                    {
                        var sourceEnums = filters.Sources
                            .Select(s => Enum.TryParse<CustomerSource>(s, true, out var src) ? (CustomerSource?)src : null)
                            .Where(s => s.HasValue)
                            .Select(s => s!.Value)
                            .ToList();
                        if (sourceEnums.Any())
                        {
                            query = query.Where(c => sourceEnums.Contains(c.Source));
                        }
                    }

                    // Üyelik tarihi filtresi
                    if (filters.MembershipDateFrom.HasValue)
                    {
                        query = query.Where(c => c.MembershipDate >= filters.MembershipDateFrom.Value);
                    }

                    if (filters.MembershipDateTo.HasValue)
                    {
                        query = query.Where(c => c.MembershipDate <= filters.MembershipDateTo.Value);
                    }

                    // Son giriş tarihi filtresi
                    if (filters.LastLoginDateFrom.HasValue)
                    {
                        query = query.Where(c => c.LastLoginDate >= filters.LastLoginDateFrom.Value);
                    }

                    if (filters.LastLoginDateTo.HasValue)
                    {
                        query = query.Where(c => c.LastLoginDate <= filters.LastLoginDateTo.Value);
                    }

                    // Arama terimi filtresi
                    if (!string.IsNullOrWhiteSpace(filters.SearchTerm))
                    {
                        var searchLower = filters.SearchTerm.ToLower();
                        query = query.Where(c =>
                            c.FirstName.ToLower().Contains(searchLower) ||
                            c.LastName.ToLower().Contains(searchLower) ||
                            c.Email.ToLower().Contains(searchLower) ||
                            (c.Phone != null && c.Phone.Contains(filters.SearchTerm)) ||
                            (c.MobilePhone != null && c.MobilePhone.Contains(filters.SearchTerm)));
                    }
                }
            }

            return await query.ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting filtered customers for company {CompanyId}", companyId);
            return new List<Customer>();
        }
    }

    private async Task<Dictionary<string, object>?> GetModuleSettingsWithScopeAsync(ApplicationDbContext context, Guid companyId)
    {
        try
        {
            // Sahiplik kontrolü için IsOwned kullan
            var companyModule = await context.CompanyModules
                .Include(cm => cm.Module)
                .Include(cm => cm.Settings)
                .FirstOrDefaultAsync(cm => cm.CompanyId == companyId &&
                                         cm.Module.Name == "Toplu Mesaj Gönderimi" &&
                                         cm.IsOwned);

            if (companyModule?.Settings != null)
            {
                return companyModule.Settings.Settings;
            }

            // Varsayılan ayarları al
            if (companyModule?.Module?.DefaultSettings != null)
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(companyModule.Module.DefaultSettings);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting module settings for company {CompanyId}", companyId);
            return null;
        }
    }

    private async Task<decimal> CalculateBatchCostWithScopeAsync(ApplicationDbContext context, Guid companyId, List<Customer> customers, List<string> enabledChannels)
    {
        decimal totalCost = 0;

        try
        {
            var settings = await GetModuleSettingsWithScopeAsync(context, companyId);

            foreach (var customer in customers)
            {
                foreach (var channelType in enabledChannels)
                {
                    if (CanSendToChannel(customer, channelType))
                    {
                        var channelCost = GetChannelCost(settings, channelType, channelType == "email" ? 0.50m : 1.00m);
                        totalCost += channelCost;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating batch cost for company {CompanyId}", companyId);
        }

        return totalCost;
    }

    private async Task<decimal> GetChannelCostWithScopeAsync(ApplicationDbContext context, Guid companyId, string channelType)
    {
        try
        {
            var settings = await GetModuleSettingsWithScopeAsync(context, companyId);
            return GetChannelCost(settings, channelType, channelType == "email" ? 0.50m : 1.00m);
        }
        catch
        {
            return channelType == "email" ? 0.50m : 1.00m;
        }
    }

    // Template cache için helper sınıflar
    private async Task<BulkMessageTemplateCache> BuildTemplateCacheAsync(ApplicationDbContext context,
        IEmailTemplateService emailTemplateService, IWhatsAppTemplateService whatsAppTemplateService, ISmsTemplateService smsTemplateService,
        Guid companyId, Dictionary<string, object> channelSettings, ILogger logger)
    {
        var cache = new BulkMessageTemplateCache();

        foreach (var channelSetting in channelSettings)
        {
            try
            {
                var channelType = channelSetting.Key;
                var templateName = GetTemplateNameFromSettings(channelSettings, channelType);

                if (string.IsNullOrEmpty(templateName))
                {
                    logger.LogWarning("No template name found for channel {ChannelType}", channelType);
                    continue;
                }

                if (channelType.ToLower() == "email")
                {
                    var emailTemplates = await emailTemplateService.GetAvailableTemplatesAsync(companyId);
                    var emailTemplate = emailTemplates.FirstOrDefault(t => t.Name == templateName);

                    if (emailTemplate != null)
                    {
                        cache.Templates[channelType] = new TemplateInfo
                        {
                            Name = emailTemplate.Name,
                            Type = "email",
                            Content = emailTemplate.DefaultContent,
                            Subject = emailTemplate.DefaultSubject
                        };
                    }
                }
                else if (channelType.ToLower() == "whatsapp")
                {
                    var whatsAppTemplates = await whatsAppTemplateService.GetFacebookTemplatesAsync(companyId);
                    var whatsAppTemplate = whatsAppTemplates.FirstOrDefault(t => t.Name == templateName && t.Status == "APPROVED");

                    if (whatsAppTemplate != null)
                    {
                        cache.Templates[channelType] = new TemplateInfo
                        {
                            Name = whatsAppTemplate.Name,
                            Type = "whatsapp",
                            WhatsAppTemplate = whatsAppTemplate
                        };
                    }
                }
                else if (channelType.ToLower() == "sms")
                {
                    var smsTemplates = await smsTemplateService.GetUserTemplatesAsync(companyId);
                    var smsTemplate = smsTemplates.FirstOrDefault(t => t.Name == templateName);

                    if (smsTemplate != null)
                    {
                        cache.Templates[channelType] = new TemplateInfo
                        {
                            Name = smsTemplate.Name,
                            Type = "sms",
                            Content = smsTemplate.CustomContent
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error caching template for channel {ChannelType}", channelSetting.Key);
            }
        }

        return cache;
    }

    private async Task<bool> SendWhatsAppWithTemplateAsync(Guid companyId, Customer customer, TemplateInfo templateInfo,
        Dictionary<string, string> variables, ILogger logger, string referenceId)
    {
        try
        {
            if (templateInfo.WhatsAppTemplate == null)
            {
                logger.LogError("WhatsApp template is null for template {TemplateName}", templateInfo.Name);
                return false;
            }

            // WhatsApp ayarlarını al
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

            var whatsappIntegration = await context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                         ci.Integration.Name == "WhatsApp" &&
                                         ci.IsActive &&
                                         ci.IsConfigured);

            if (whatsappIntegration?.Settings == null)
            {
                logger.LogError("WhatsApp integration not found for company {CompanyId}", companyId);
                return false;
            }

            var settings = whatsappIntegration.Settings;
            var whatsAppSettings = new WhatsAppConnectionSettings
            {
                AppId = settings.GetValueOrDefault("appId")?.ToString() ?? "",
                Token = settings.GetValueOrDefault("token")?.ToString() ?? "",
            };

            // Customer'dan variables dictionary oluştur (mevcut variables'ı kullan)
            var customerVariables = new Dictionary<string, string>
            {
                ["customer_name"] = customer.Name ?? customer.FirstName + " " + customer.LastName,
                ["phone_number"] = customer.Phone ?? customer.MobilePhone ?? "-",
                ["email_address"] = string.IsNullOrWhiteSpace(customer.Email) ? "-" : customer.Email,
                ["customer_city"] = string.IsNullOrWhiteSpace(customer.City) ? "-" : customer.City,
                ["customer_first_name"] = string.IsNullOrWhiteSpace(customer.FirstName) ? "-" : customer.FirstName,
                ["customer_last_name"] = string.IsNullOrWhiteSpace(customer.LastName) ? "-" : customer.LastName,
            };
            
            // Mevcut variables ile merge et (variables öncelikli)
            foreach (var kvp in variables)
            {
                customerVariables[kvp.Key] = kvp.Value;
            }

            // Template components'lerini hazırla (merkezi servis kullanarak)
            var templateComponents = await _whatsAppTemplateComponentService.PrepareWhatsAppTemplateComponentsAsync(
                companyId, templateInfo.WhatsAppTemplate, context, customerVariables);

            // WhatsApp mesajını gönder
            var request = new WhatsAppTemplateMessageRequest
            {
                ToPhoneNumber = customer.Phone ?? customer.MobilePhone ?? "",
                TemplateName = templateInfo.Name,
                TemplateLanguage = templateInfo.WhatsAppTemplate.Language ?? "tr",
                Components = templateComponents,
                ConnectionSettings = whatsAppSettings
            };

            var result = await whatsAppService.SendTemplateMessageAsync(request);

            if (result.Success)
            {
                // Maliyet takibi - sadece bakiye düşür, ModuleUsageLog toplu olarak yapılacak
                var company = await context.Companies.FindAsync(companyId);
                if (company != null)
                {
                    decimal cost = 1.00m; // WhatsApp maliyeti
                    company.CreditBalance -= cost;
                    await context.SaveChangesAsync();
                }

                logger.LogInformation("WhatsApp template message sent successfully to customer {CustomerId}", customer.Id);
                return true;
            }
            else
            {
                logger.LogError("Failed to send WhatsApp template message to customer {CustomerId}: {Error}",
                    customer.Id, result.Message);
                return false;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending WhatsApp template message to customer {CustomerId}", customer.Id);
            return false;
        }
    }

    // Bulk message usage log creation
    private async Task CreateBulkMessageUsageLogAsync(ApplicationDbContext context, BulkMessage bulkMessage, decimal totalCost, ILogger logger)
    {
        try
        {
            // Şirket bakiyesini düş
            var company = await context.Companies.FindAsync(bulkMessage.CompanyId);

            // Toplu Mesaj modülünü bul
            var bulkMessageModule = await context.Modules
                .FirstOrDefaultAsync(m => m.Name == "Toplu Mesaj Gönderimi");

            if (bulkMessageModule != null)
            {
                // Bakiye bilgilerini al
                var balanceBefore = company.CreditBalance + totalCost; // Düşmeden önceki bakiye
                var balanceAfter = company.CreditBalance; // Düştükten sonraki bakiye

                // ModuleUsageLog kaydı oluştur
                var usageLog = new ModuleUsageLog
                {
                    CompanyId = bulkMessage.CompanyId,
                    ModuleId = bulkMessageModule.Id,
                    UsageType = "Toplu Mesaj",
                    Cost = totalCost,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = balanceAfter,
                    Description = $"Toplu Mesaj başlığıyla iletildi. {bulkMessage.Title}",
                    UserId = bulkMessage.UserId,
                    ReferenceId = $"bulk_message_{bulkMessage.Id}",
                    Channel = string.Join(",", GetChannelListFromSettings(bulkMessage.ChannelSettingsJson)),
                    IsSuccessful = true,
                    Metadata = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        ["BulkMessageId"] = bulkMessage.Id,
                        ["TotalRecipients"] = bulkMessage.TotalRecipients,
                        ["SuccessfulSends"] = bulkMessage.SuccessfulSends,
                        ["FailedSends"] = bulkMessage.FailedSends,
                        ["ProcessedRecipients"] = bulkMessage.ProcessedRecipients,
                        ["Title"] = bulkMessage.Title,
                        ["Description"] = bulkMessage.Description ?? "",
                        ["Channels"] = GetChannelListFromSettings(bulkMessage.ChannelSettingsJson)
                    }),
                    CreatedAt = DateTime.Now
                };

                context.ModuleUsageLogs.Add(usageLog);
            }

            logger.LogInformation("Created bulk message usage log: CompanyId={CompanyId}, Amount={Amount}, BulkMessageId={BulkMessageId}",
                bulkMessage.CompanyId, totalCost, bulkMessage.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating bulk message usage log for BulkMessageId={BulkMessageId}", bulkMessage.Id);
        }
    }

    private List<string> GetChannelListFromSettings(string? channelSettingsJson)
    {
        try
        {
            if (string.IsNullOrEmpty(channelSettingsJson))
                return new List<string>();

            var channelSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(channelSettingsJson);
            return channelSettings?.Keys.ToList() ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private async Task<bool> SendSmsWithTemplateAsync(ApplicationDbContext context, ISmsProvider smsProvider, Guid companyId,
        Customer customer, TemplateInfo templateInfo, Dictionary<string, string> variables, ILogger logger, string referenceId)
    {
        try
        {
            if (string.IsNullOrEmpty(templateInfo.Content))
            {
                logger.LogError("SMS content is empty for template {TemplateName}", templateInfo.Name);
                return false;
            }

            // Get SMS settings (using existing logic or direct DB access to avoid dependency cycles)
            var smsIntegration = await context.CompanyIntegrations
                .Include(ci => ci.Integration)
                .FirstOrDefaultAsync(ci => ci.CompanyId == companyId &&
                                     ci.Integration.Name == "SMS" &&
                                     ci.IsActive &&
                                     ci.IsConfigured);

            if (smsIntegration?.Settings == null)
            {
                logger.LogWarning("SMS integration not found for company {CompanyId}", companyId);
                return false;
            }

            // Parse settings manually
            var settingsDict = smsIntegration.Settings;
            var username = settingsDict.GetValueOrDefault("username")?.ToString();
            var password = settingsDict.GetValueOrDefault("password")?.ToString();
            var senderName = settingsDict.GetValueOrDefault("senderName")?.ToString() ?? "";
            var providerStr = settingsDict.GetValueOrDefault("provider")?.ToString();
            
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            var provider = SmsProvider.NetGsm;
            if (!string.IsNullOrWhiteSpace(providerStr))
            {
                Enum.TryParse<SmsProvider>(providerStr, true, out provider);
            }

            var smsSettings = new SmsSettings
            {
                Provider = provider,
                Username = username,
                Password = password,
                SenderName = senderName,
                AutoNotifications = true // Default for bulk
            };

            // Prepare message content
            var message = templateInfo.Content;
            foreach (var variable in variables)
            {
                message = message.Replace($"{{{{{variable.Key}}}}}", variable.Value);
            }

            // Get phone number
            var phoneNumber = customer.MobilePhone ?? customer.Phone;
            if (string.IsNullOrWhiteSpace(phoneNumber))
            {
                return false;
            }

            // Send
            var result = await smsProvider.SendSmsAsync(phoneNumber, message, smsSettings);
            return result.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending SMS directly to customer {CustomerId}", customer.Id);
            return false;
        }
    }


}

// Template cache için helper sınıflar
public class BulkMessageTemplateCache
{
    public Dictionary<string, TemplateInfo> Templates { get; set; } = new();
}

public class TemplateInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? Subject { get; set; }
    public FacebookMessageTemplate? WhatsAppTemplate { get; set; }
}