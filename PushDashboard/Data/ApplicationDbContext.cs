using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data.Configurations;
using PushDashboard.Models;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;

namespace PushDashboard.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

    public DbSet<Company> Companies { get; set; }
    public DbSet<Module> Modules { get; set; }
    public DbSet<ModuleCategory> ModuleCategories { get; set; }
    public DbSet<CompanyModule> CompanyModules { get; set; }
    public DbSet<CompanyModuleSettings> CompanyModuleSettings { get; set; }

    // Quota Settings
    public DbSet<ModulePackage> ModulePackages { get; set; }
    public DbSet<CompanyModuleQuota> CompanyModuleQuotas { get; set; }

    public DbSet<NotificationPreferences> NotificationPreferences { get; set; }
    public DbSet<UserDevice> UserDevices { get; set; }
    public DbSet<UserSession> UserSessions { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<CustomerImportJob> CustomerImportJobs { get; set; }
    public DbSet<Basket> Baskets { get; set; }
    public DbSet<BasketItem> BasketItems { get; set; }
    public DbSet<SyncLog> SyncLogs { get; set; }
    public DbSet<Integration> Integrations { get; set; }
    public DbSet<CompanyIntegration> CompanyIntegrations { get; set; }
    public DbSet<EmailTemplate> EmailTemplates { get; set; } = null!;
    public DbSet<CompanyEmailTemplate> CompanyEmailTemplates { get; set; } = null!;
    public DbSet<SmsTemplate> SmsTemplates { get; set; } = null!;
    public DbSet<CompanySmsTemplate> CompanySmsTemplates { get; set; } = null!;
    public DbSet<UserInvitation> UserInvitations { get; set; }
    // Module Usage Tracking
    public DbSet<ModuleUsageLog> ModuleUsageLogs { get; set; }
    // Comment Requests
    public DbSet<CommentRequest> CommentRequests { get; set; }
    // Trendyol Scraper Integration
    public DbSet<TrendyolStore> TrendyolStores { get; set; }
    public DbSet<TrendyolProduct> TrendyolProducts { get; set; }
    public DbSet<CommentTransferJob> CommentTransferJobs { get; set; }
    public DbSet<CommentTransferJobProduct> CommentTransferJobProducts { get; set; }
    // Product Slider Module
    public DbSet<ProductSlider> ProductSliders { get; set; }
    public DbSet<ProductSliderItem> ProductSliderItems { get; set; }
    public DbSet<ProductSliderSettings> ProductSliderSettings { get; set; }
    // Bulk Messaging
    public DbSet<BulkMessage> BulkMessages { get; set; }
    public DbSet<BulkMessageRecipient> BulkMessageRecipients { get; set; }
    // Order Status Notifications
    public DbSet<OrderStatusNotification> OrderStatusNotifications { get; set; }
    public DbSet<OrderStatusChangeLog> OrderStatusChangeLogs { get; set; }
    public DbSet<OrderStatusMapping> OrderStatusMappings { get; set; }
    // Basket Reminder Module
    public DbSet<BasketReminderSchedule> BasketReminderSchedules { get; set; }
    public DbSet<BasketReminderLog> BasketReminderLogs { get; set; }
    public DbSet<BasketReminderSettings> BasketReminderSettings { get; set; }
    // Social Proof
    public DbSet<SocialProofSettings> SocialProofSettings { get; set; }
    // Gift Wheel Module
    public DbSet<GiftWheel> GiftWheels { get; set; }
    public DbSet<GiftWheelPrize> GiftWheelPrizes { get; set; }
    public DbSet<GiftWheelSpin> GiftWheelSpins { get; set; }
    public DbSet<GiftWheelSettings> GiftWheelSettings { get; set; }
    // Cookie Management
    public DbSet<CookieManagement> CookieManagements { get; set; }
    public DbSet<CookieConsentEvent> CookieConsentEvents { get; set; }
    // Exit Survey
    public DbSet<ExitSurvey> ExitSurveys { get; set; }
    public DbSet<ExitSurveyQuestion> ExitSurveyQuestions { get; set; }
    public DbSet<ExitSurveyResponse> ExitSurveyResponses { get; set; }
    public DbSet<ExitSurveyEvent> ExitSurveyEvents { get; set; }
    // Video Hosting
    public DbSet<VideoHosting> VideoHostings { get; set; }
    public DbSet<VideoHostingView> VideoHostingViews { get; set; }
    public DbSet<VideoHostingSettings> VideoHostingSettings { get; set; }
    // User Notifications
    public DbSet<UserNotification> UserNotifications { get; set; }

    // Activity Logs
    public DbSet<ActivityLog> ActivityLogs { get; set; }

    // Favorite Products Module
    public DbSet<FavoriteProduct> FavoriteProducts { get; set; }
    public DbSet<FavoriteProductSyncLog> FavoriteProductSyncLogs { get; set; }

    // First Order Module
    public DbSet<FirstOrderCustomer> FirstOrderCustomers { get; set; }

    // Birthday Module
    public DbSet<BirthdayNotification> BirthdayNotifications { get; set; }

    // Stock Reminder Items
    public DbSet<StockReminderItem> StockReminderItems { get; set; }

    // WhatsApp Message Tracking
    public DbSet<WhatsAppMessage> WhatsAppMessages { get; set; } = null!;
    public DbSet<WhatsAppWebhookEvent> WhatsAppWebhookEvents { get; set; } = null!;

    // WhatsApp Template Variable Mappings
    public DbSet<WhatsAppTemplateVariableMapping> WhatsAppTemplateVariableMappings { get; set; } = null!;

    // WhatsApp Template Settings (ModuleType, vb.)
    public DbSet<WhatsAppTemplateSettings> WhatsAppTemplateSettings { get; set; } = null!;

    // Credit Transactions (PayTR Payments)
    public DbSet<CreditTransaction> CreditTransactions { get; set; }

    // Order Conversion Tracking
    public DbSet<OrderConversion> OrderConversions { get; set; }

    // Newsletter Popup
    public DbSet<NewsletterPopupSettings> NewsletterPopupSettings { get; set; }
    public DbSet<NewsletterPopupEvent> NewsletterPopupEvents { get; set; }

    // Stock Reminder Widget
    public DbSet<StockReminderWidgetSettings> StockReminderWidgetSettings { get; set; }
    public DbSet<StockReminderWidgetSubmission> StockReminderWidgetSubmissions { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Command timeout ayarı (5 dakika)
            optionsBuilder.UseNpgsql(options => options.CommandTimeout(300));
        }
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);



        // Apply all entity configurations
        builder.ApplyConfiguration(new ApplicationUserConfiguration());
        builder.ApplyConfiguration(new CompanyConfiguration());
        builder.ApplyConfiguration(new NotificationPreferencesConfiguration());
        builder.ApplyConfiguration(new UserDeviceConfiguration());
        builder.ApplyConfiguration(new UserSessionConfiguration());
        builder.ApplyConfiguration(new ModuleConfiguration());
        builder.ApplyConfiguration(new ModuleCategoryConfiguration());
        builder.ApplyConfiguration(new CompanyModuleConfiguration());
        builder.ApplyConfiguration(new CompanyModuleSettingsConfiguration());

        builder.ApplyConfiguration(new ModulePackageConfiguration());
        builder.ApplyConfiguration(new CompanyModuleQuotaConfiguration());

        builder.ApplyConfiguration(new CompanyIntegrationConfiguration());
        builder.ApplyConfiguration(new IntegrationConfiguration());
        builder.ApplyConfiguration(new CustomerConfiguration());
        builder.ApplyConfiguration(new CustomerImportJobConfiguration());
        builder.ApplyConfiguration(new SyncLogConfiguration());
        builder.ApplyConfiguration(new BasketConfiguration());
        builder.ApplyConfiguration(new BasketItemConfiguration());
        builder.ApplyConfiguration(new EmailTemplateConfiguration());
        builder.ApplyConfiguration(new CompanyEmailTemplateConfiguration());
        builder.ApplyConfiguration(new SmsTemplateConfiguration());
        builder.ApplyConfiguration(new CompanySmsTemplateConfiguration());
        builder.ApplyConfiguration(new UserInvitationConfiguration());
        builder.ApplyConfiguration(new ModuleUsageLogConfiguration());
        builder.ApplyConfiguration(new CommentRequestConfiguration());
        builder.ApplyConfiguration(new TrendyolStoreConfiguration());
        builder.ApplyConfiguration(new TrendyolProductConfiguration());
        builder.ApplyConfiguration(new CommentTransferJobConfiguration());
        builder.ApplyConfiguration(new CommentTransferJobProductConfiguration());
        builder.ApplyConfiguration(new BulkMessageConfiguration());
        builder.ApplyConfiguration(new BulkMessageRecipientConfiguration());
        builder.ApplyConfiguration(new OrderStatusNotificationConfiguration());
        builder.ApplyConfiguration(new OrderStatusChangeLogConfiguration());
        builder.ApplyConfiguration(new SocialProofSettingsConfiguration());
        builder.ApplyConfiguration(new OrderStatusMappingConfiguration());
        builder.ApplyConfiguration(new ExitSurveyConfiguration());
        builder.ApplyConfiguration(new ExitSurveyQuestionConfiguration());
        builder.ApplyConfiguration(new ExitSurveyResponseConfiguration());
        builder.ApplyConfiguration(new BasketReminderScheduleConfiguration());
        builder.ApplyConfiguration(new BasketReminderLogConfiguration());
        builder.ApplyConfiguration(new BasketReminderSettingsConfiguration());
        builder.ApplyConfiguration(new ProductSliderConfiguration());
        builder.ApplyConfiguration(new ProductSliderItemConfiguration());
        builder.ApplyConfiguration(new ProductSliderSettingsConfiguration());
        builder.ApplyConfiguration(new VideoHostingConfiguration());
        builder.ApplyConfiguration(new VideoHostingViewConfiguration());
        builder.ApplyConfiguration(new VideoHostingSettingsConfiguration());
        builder.ApplyConfiguration(new UserNotificationConfiguration());
        builder.ApplyConfiguration(new ActivityLogConfiguration());
        builder.ApplyConfiguration(new FavoriteProductConfiguration());
        builder.ApplyConfiguration(new FavoriteProductSyncLogConfiguration());
        builder.ApplyConfiguration(new WhatsAppMessageConfiguration());
        builder.ApplyConfiguration(new WhatsAppWebhookEventConfiguration());
        builder.ApplyConfiguration(new WhatsAppTemplateVariableMappingConfiguration());
        builder.ApplyConfiguration(new WhatsAppTemplateSettingsConfiguration());
        builder.ApplyConfiguration(new BirthdayNotificationConfiguration());
        builder.ApplyConfiguration(new StockReminderItemConfiguration());
        builder.ApplyConfiguration(new CreditTransactionConfiguration());
        builder.ApplyConfiguration(new NewsletterPopupSettingsConfiguration());
        builder.ApplyConfiguration(new NewsletterPopupEventConfiguration());
        builder.ApplyConfiguration(new StockReminderWidgetSettingsConfiguration());
        builder.ApplyConfiguration(new StockReminderWidgetSubmissionConfiguration());
    }
}
