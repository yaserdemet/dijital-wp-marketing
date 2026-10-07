using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Filters;
using PushDashboard.Logging;
using PushDashboard.Models;
using PushDashboard.Services;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var builder = WebApplication.CreateBuilder(args);

// Configure custom JSON logging
builder.Logging.ClearProviders();
builder.Logging.AddConsoleFormatter<JsonLogFormatter, JsonLogFormatterOptions>(options =>
{
    options.AgentName = "PushDashboard";
    options.Environment = builder.Environment.EnvironmentName;
});
builder.Logging.AddConsole(options =>
{
    options.FormatterName = JsonLogFormatter.FormatterName;
});

// Add services to the container.

// Configure DbContext with PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.CommandTimeout(300))); // 5 dakika timeout

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;

    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.Cookie.Name = ".AspNetCore.Identity.Application";
});

// Configure Data Protection
var dataProtectionBuilder = builder.Services.AddDataProtection()
    .SetApplicationName("PushDashboard")
    .PersistKeysToDbContext<ApplicationDbContext>();

if (!builder.Environment.IsDevelopment())
{
    dataProtectionBuilder.SetDefaultKeyLifetime(TimeSpan.FromDays(90));
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Register TwoFactorService
builder.Services.AddScoped<ITwoFactorService, TwoFactorService>();
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<IModuleSeedService, ModuleSeedService>();
builder.Services.AddScoped<IIntegrationSeedService, IntegrationSeedService>();
builder.Services.AddScoped<IEmailTemplateSeedService, EmailTemplateSeedService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<ISmsTemplateSeedService, SmsTemplateSeedService>();
builder.Services.AddScoped<ISmsTemplateService, SmsTemplateService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();
builder.Services.AddScoped<ICustomerImportService, CustomerImportService>();
builder.Services.AddScoped<IBasketService, BasketService>();
builder.Services.AddScoped<ITcmbExchangeRateService, TcmbExchangeRateService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<ITrendyolScraperService, TrendyolScraperService>();
builder.Services.AddScoped<IProductSliderService, ProductSliderService>();

// Configure R2 Storage Settings
builder.Services.Configure<PushDashboard.Configuration.R2StorageSettings>(
    builder.Configuration.GetSection(PushDashboard.Configuration.R2StorageSettings.SectionName));

// Register R2 Storage Service
builder.Services.AddScoped<PushDashboard.Services.IR2StorageService, PushDashboard.Services.R2StorageService>();

builder.Services.AddHttpClient(); // HttpClient for external API calls
builder.Services.AddScoped<Microsoft.AspNetCore.Authentication.IClaimsTransformation, PushDashboard.Services.ClaimsTransformationService>();

// Add UserContext service
builder.Services.AddScoped<PushDashboard.Services.IUserContextService, PushDashboard.Services.UserContextService>();

// Add Social Proof service
builder.Services.AddScoped<PushDashboard.Services.ISocialProofService, PushDashboard.Services.SocialProofService>();

// Add Cookie Management service
builder.Services.AddScoped<PushDashboard.Services.ICookieManagementService, PushDashboard.Services.CookieManagementService>();

// Add Exit Survey service
builder.Services.AddScoped<PushDashboard.Services.IExitSurveyService, PushDashboard.Services.ExitSurveyService>();

// Add Newsletter Popup service
builder.Services.AddScoped<PushDashboard.Services.INewsletterPopupService, PushDashboard.Services.NewsletterPopupService>();

// Add Stock Reminder Widget service
builder.Services.AddScoped<PushDashboard.Services.IStockReminderWidgetService, PushDashboard.Services.StockReminderWidgetService>();

// Configure Email Settings
builder.Services.Configure<PushDashboard.Models.EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// Configure Comment Scraper API Settings
builder.Services.Configure<PushDashboard.Configuration.CommentScraperApiSettings>(
    builder.Configuration.GetSection(PushDashboard.Configuration.CommentScraperApiSettings.SectionName));

// Configure Gupshup Partner Settings
builder.Services.Configure<PushDashboard.Configuration.GupshupPartnerSettings>(
    builder.Configuration.GetSection(PushDashboard.Configuration.GupshupPartnerSettings.SectionName));

// Register Invitation Services
builder.Services.AddScoped<PushDashboard.Services.IInvitationService, PushDashboard.Services.InvitationService>();
builder.Services.AddScoped<PushDashboard.Services.IEmailService, PushDashboard.Services.EmailService>();

// Register Referral Code Service
builder.Services.AddScoped<PushDashboard.Services.IReferralCodeService, PushDashboard.Services.ReferralCodeService>();

// Register Integration Services
builder.Services.AddScoped<PushDashboard.Services.Integrations.IEcommerceServiceFactory, PushDashboard.Services.Integrations.EcommerceServiceFactory>();
builder.Services.AddScoped<PushDashboard.Services.IIntegrationService, PushDashboard.Services.IntegrationService>();

// Connection test services are now integrated into notification channel services

// Register Welcome Service (commented out - service not implemented yet)
// builder.Services.AddScoped<PushDashboard.Controllers.IWelcomeService, PushDashboard.Services.WelcomeService>();

// Register Webhook Services
builder.Services.AddScoped<PushDashboard.Services.Integrations.IEcommerceWebhookFactory, PushDashboard.Services.Integrations.EcommerceWebhookFactory>();

// Register Module Usage Service
builder.Services.AddScoped<PushDashboard.Services.IModuleUsageService, PushDashboard.Services.ModuleUsageService>();
builder.Services.AddScoped<PushDashboard.Services.ModuleUsageSeedService>();

// Register Activity Log Service
builder.Services.AddScoped<PushDashboard.Services.IActivityLogService, PushDashboard.Services.ActivityLogService>();



// Register Notification Services
builder.Services.AddScoped<PushDashboard.Services.Notifications.INotificationChannelService, PushDashboard.Services.Notifications.EmailNotificationChannelService>();
builder.Services.AddScoped<PushDashboard.Services.Notifications.EmailNotificationChannelService>();
builder.Services.AddScoped<PushDashboard.Services.Notifications.WhatsAppNotificationChannelService>();
builder.Services.AddScoped<PushDashboard.Services.Notifications.IWhatsAppTemplateComponentService>(sp =>
    sp.GetRequiredService<PushDashboard.Services.Notifications.WhatsAppNotificationChannelService>());
builder.Services.AddScoped<PushDashboard.Services.Notifications.SmsNotificationChannelService>();
builder.Services.AddScoped<PushDashboard.Services.Notifications.INotificationChannelFactory, PushDashboard.Services.Notifications.NotificationChannelFactory>();

// Register SMS Services
builder.Services.AddHttpClient<PushDashboard.Services.SMS.ISmsProvider, PushDashboard.Services.SMS.NetGsmProvider>();
builder.Services.AddScoped<PushDashboard.Services.SMS.ISmsProvider, PushDashboard.Services.SMS.NetGsmProvider>();

// Register WhatsApp Services
builder.Services.AddHttpClient<PushDashboard.Services.WhatsApp.IWhatsAppService, PushDashboard.Services.WhatsApp.WhatsAppService>();
builder.Services.AddScoped<PushDashboard.Services.WhatsApp.IWhatsAppMessageService, PushDashboard.Services.WhatsApp.WhatsAppMessageService>();
builder.Services.AddHttpClient<PushDashboard.Services.WhatsApp.IWhatsAppWebhookService, PushDashboard.Services.WhatsApp.WhatsAppWebhookService>();
builder.Services.AddScoped<PushDashboard.Services.IWhatsAppTemplateService, PushDashboard.Services.WhatsAppTemplateService>();
builder.Services.AddScoped<PushDashboard.Services.IWhatsAppTemplateSampleService, PushDashboard.Services.WhatsAppTemplateSampleService>();
builder.Services.AddScoped<PushDashboard.Services.WhatsApp.IWhatsAppConversionService, PushDashboard.Services.WhatsApp.WhatsAppConversionService>();
// Register Module Services
builder.Services.AddScoped<PushDashboard.Services.Modules.Birthday.IBirthdayModuleService, PushDashboard.Services.Modules.Birthday.BirthdayModuleService>();
// Register First Order Notification Service
builder.Services.AddScoped<PushDashboard.Services.Modules.FirstOrder.IFirstOrderModuleService, PushDashboard.Services.Modules.FirstOrder.FirstOrderModuleService>();
// Register Gift Wheel Services
builder.Services.AddScoped<PushDashboard.Services.Modules.GiftWheel.IGiftWheelService, PushDashboard.Services.Modules.GiftWheel.GiftWheelService>();
builder.Services.AddScoped<PushDashboard.Services.Modules.GiftWheel.IGiftWheelModuleService, PushDashboard.Services.Modules.GiftWheel.GiftWheelModuleService>();

// Register Video Hosting Services
builder.Services.AddScoped<PushDashboard.Services.Modules.VideoHosting.IVideoProcessingService, PushDashboard.Services.Modules.VideoHosting.VideoProcessingService>();
builder.Services.AddScoped<PushDashboard.Services.Modules.VideoHosting.IVideoHostingService, PushDashboard.Services.Modules.VideoHosting.VideoHostingService>();
builder.Services.AddScoped<PushDashboard.Services.Modules.VideoHosting.IVideoHostingModuleService, PushDashboard.Services.Modules.VideoHosting.VideoHostingModuleService>();

// Register Product Slider Module Service
builder.Services.AddScoped<PushDashboard.Services.Modules.ProductSlider.IProductSliderModuleService, PushDashboard.Services.Modules.ProductSlider.ProductSliderModuleService>();

// Register Basket Reminder Module Service
builder.Services.AddScoped<PushDashboard.Services.Modules.BasketReminder.IBasketReminderModuleService, PushDashboard.Services.Modules.BasketReminder.BasketReminderModuleService>();

// Register Stock Reminder Module Service
builder.Services.AddScoped<PushDashboard.Services.Modules.StockReminder.IStockReminderModuleService, PushDashboard.Services.Modules.StockReminder.StockReminderModuleService>();

// Register Favorite Product Module Service
builder.Services.AddScoped<PushDashboard.Services.Modules.FavoriteProduct.IFavoriteProductService, PushDashboard.Services.Modules.FavoriteProduct.FavoriteProductService>();

// Register Dashboard Service
builder.Services.AddScoped<PushDashboard.Services.IDashboardService, PushDashboard.Services.DashboardService>();
// Register Ticimax Base Service as IEcommerceService
builder.Services.AddScoped<PushDashboard.Services.Integrations.Common.IEcommerceService, PushDashboard.Services.Integrations.Ticimax.TicimaxService>();

// Register TicimaxService directly for connection testing
builder.Services.AddScoped<PushDashboard.Services.Integrations.Ticimax.TicimaxService>();

// Register E-commerce Gift Voucher Services
builder.Services.AddScoped<PushDashboard.Services.Integrations.EcommerceGiftVoucherFactory>();

// Register Bulk Messaging Services
builder.Services.AddScoped<PushDashboard.Services.BulkMessaging.IBulkMessagingService, PushDashboard.Services.BulkMessaging.BulkMessagingService>();

// Register Template Module Service (shared across all modules)
builder.Services.AddScoped<PushDashboard.Services.ITemplateModuleService, PushDashboard.Services.TemplateModuleService>();

// Register Order Status Notification Service
builder.Services.AddScoped<PushDashboard.Services.IOrderStatusNotificationService, PushDashboard.Services.OrderStatusNotificationService>();

// Register Order Status Mapping Service
builder.Services.AddScoped<PushDashboard.Services.IOrderStatusMappingService, PushDashboard.Services.OrderStatusMappingService>();

// Register Basket Reminder Service
builder.Services.AddScoped<PushDashboard.Services.IBasketReminderService, PushDashboard.Services.BasketReminderService>();
builder.Services.AddScoped<PushDashboard.Services.ICommunicationChannelService, PushDashboard.Services.CommunicationChannelService>();

// Configure PayTR Settings
builder.Services.Configure<PushDashboard.Models.DTOs.PayTRSettings>(
    builder.Configuration.GetSection("PayTR"));

// Register PayTR Services
builder.Services.AddHttpClient<PushDashboard.Services.IPayTRService, PushDashboard.Services.PayTRService>();
builder.Services.AddScoped<PushDashboard.Services.ICreditTransactionService, PushDashboard.Services.CreditTransactionService>();

// Register Module Subscription Service
builder.Services.AddScoped<PushDashboard.Services.IModuleSubscriptionService, PushDashboard.Services.ModuleSubscriptionService>();

// Register Birthday Module Service
builder.Services.AddScoped<PushDashboard.Services.Modules.Birthday.IBirthdayModuleService, PushDashboard.Services.Modules.Birthday.BirthdayModuleService>();

// Register Background Services
//builder.Services.AddHostedService<PushDashboard.Services.BirthdayBackgroundService>();
//builder.Services.AddHostedService<PushDashboard.Services.StockReminderBackgroundService>();

// Add caching services
builder.Services.AddMemoryCache();
builder.Services.AddScoped<PushDashboard.Services.IUserCacheService, PushDashboard.Services.UserCacheService>();

// Add User Notification Service
builder.Services.AddScoped<PushDashboard.Services.IUserNotificationService, PushDashboard.Services.UserNotificationService>();

// Add session services
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});

// Add rate limiting for Gift Wheel API
builder.Services.AddRateLimiter(options =>
{
    // Gift Wheel Spin Policy - IP bazlı rate limiting (merkezi kontrol)
    options.AddFixedWindowLimiter("GiftWheelSpinPolicy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 20; // 20 spins per minute per IP
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 5;
    });

    // Gift Wheel Config Policy - Moderate
    options.AddFixedWindowLimiter("GiftWheelConfigPolicy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 30; // 30 requests per minute per IP
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 5;
    });

    // Gift Wheel Script Policy - Less restrictive
    options.AddFixedWindowLimiter("GiftWheelScriptPolicy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 60; // 60 requests per minute per IP
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 10;
    });

    // Gift Wheel Validation Policy - Moderate
    options.AddFixedWindowLimiter("GiftWheelValidationPolicy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 20; // 20 validations per minute per IP
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 3;
    });

    // Gift Wheel Eligibility Policy - Moderate
    options.AddFixedWindowLimiter("GiftWheelEligibilityPolicy", limiterOptions =>
    {
        limiterOptions.PermitLimit = 30; // 30 eligibility checks per minute per IP
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 5;
    });

    // Global rejection response
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        context.HttpContext.Response.ContentType = "application/json";
        var message = context.HttpContext.Request.Path.Value?.Contains("/spin") == true
            ? "Çok fazla deneme yapıldı. Lütfen daha sonra tekrar deneyin."
            : "Çok fazla istek gönderildi. Lütfen daha sonra tekrar deneyin.";
        await context.HttpContext.Response.WriteAsync($"{{\"success\":false,\"message\":\"{message}\",\"errorCode\":\"RATE_LIMIT_EXCEEDED\"}}", token);
    };
});
builder.Services.AddControllersWithViews(options =>
{
    // Add global filter for module access control
    options.Filters.Add<ModuleAccessFilter>();
});

// Register ModuleAccessFilter
builder.Services.AddScoped<ModuleAccessFilter>();
builder.Services.AddSignalR();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
    });

var app = builder.Build();

// Seed Social Proof Module
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PushDashboard.Data.ApplicationDbContext>();
    try
    {
        await PushDashboard.Data.Seeders.SocialProofModuleSeeder.SeedSocialProofModuleAsync(context);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Social Proof module seeding skipped (database unavailable).");
    }
}

// Seed Cookie Management Module
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PushDashboard.Data.ApplicationDbContext>();
    try
    {
        await PushDashboard.Data.Seeders.CookieManagementModuleSeeder.SeedCookieManagementModuleAsync(context);
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning(ex, "Cookie Management module seeding skipped (database unavailable).");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

app.UseRouting();
app.UseCors("AllowAll");

app.UseRateLimiter(); // Add rate limiting middleware

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

// Add Exception Handling middleware
app.UseMiddleware<PushDashboard.Middleware.ExceptionHandlingMiddleware>();

// Add UserContext middleware after authentication
app.UseMiddleware<PushDashboard.Middleware.UserContextMiddleware>();

// Create database and apply migrations if they don't exist
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();

        // Data Protection key'lerinin veritabanında olduğunu kontrol et
        var dataProtectionKeyCount = context.DataProtectionKeys.Count();
        var logger = services.GetRequiredService<ILogger<Program>>();
        if (dataProtectionKeyCount == 0)
        {
            logger.LogWarning("Data Protection key'leri veritabanında bulunamadı. İlk key otomatik oluşturulacak.");
        }
        else
        {
            logger.LogInformation("Data Protection: {Count} key veritabanında bulundu. Cookie'ler deploy'larda korunacak.", dataProtectionKeyCount);
        }

        // Seed initial roles and admin user if needed
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        SeedData.Initialize(userManager, roleManager, context).Wait();

        // Seed module data
        var moduleSeedService = services.GetRequiredService<IModuleSeedService>();
        moduleSeedService.SeedAsync().Wait();

        // Seed integration data
        var integrationSeedService = services.GetRequiredService<IIntegrationSeedService>();
        integrationSeedService.SeedAsync().Wait();

        // Seed email template data
        var emailTemplateSeedService = services.GetRequiredService<IEmailTemplateSeedService>();
        emailTemplateSeedService.SeedAsync().Wait();

        // Seed SMS template data
        var smsTemplateSeedService = services.GetRequiredService<ISmsTemplateSeedService>();
        smsTemplateSeedService.SeedDefaultTemplatesAsync().Wait();

        // Seed module usage data
        var moduleUsageSeedService = services.GetRequiredService<ModuleUsageSeedService>();
        moduleUsageSeedService.SeedAsync().Wait();

        // Seed first order module data

        // Seed exit survey module data
        await PushDashboard.Data.Seeders.ExitSurveyModuleSeeder.SeedAsync(context);

        // Test entegrasyon mimarisi (sadece development ortamında)
        // if (app.Environment.IsDevelopment())
        // {
        //     try
        //     {
        //         await PushDashboard.Tests.IntegrationArchitectureTest.TestIntegrationArchitecture(services);
        //     }
        //     catch (Exception testEx)
        //     {
        //         var logger = services.GetRequiredService<ILogger<Program>>();
        //         logger.LogError(testEx, "Entegrasyon mimarisi testi başarısız");
        //     }
        // }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating or seeding the database.");
    }
}

app.MapHub<PushDashboard.Hubs.SessionHub>("/sessionHub");
app.MapHub<PushDashboard.Hubs.CustomerImportHub>("/customerImportHub");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
