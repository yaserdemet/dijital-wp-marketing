using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using System.Text.Json;

namespace PushDashboard.Services;

public interface IIntegrationSeedService
{
    Task SeedAsync();
}

public class IntegrationSeedService : IIntegrationSeedService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<IntegrationSeedService> _logger;

    public IntegrationSeedService(ApplicationDbContext context, ILogger<IntegrationSeedService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        // Check if integrations already exist
        if (await _context.Integrations.AnyAsync())
        {
            _logger.LogInformation("Integrations already exist, skipping seed.");
            return;
        }

        // Seed Integrations
        var integrations = new List<Integration>
        {
            // E-ticaret Altyapıları
            new Integration
            {
                Name = "Ticimax",
                Description = "Ticimax e-ticaret platformu entegrasyonu",
                DetailedDescription = "Ticimax e-ticaret altyapınızı entegre edin ve sepet hatırlatmalarını otomatikleştirin.",
                Type = "ecommerce",
                Category = "E-ticaret Altyapıları",
                IconClass = "T",
                IconColor = "text-blue-600",
                BackgroundColor = "bg-blue-100",
                IsActive = true,
                IsPopular = true,
                SortOrder = 1,
                Features = JsonSerializer.Serialize(new[]
                {
                    "API entegrasyonu",
                    "Otomatik senkronizasyon",
                    "Sepet takibi",
                    "Sepet hatırlatmaları",
                    "Müşteri bilgileri senkronizasyonu"
                }),
                DefaultSettingsTemplate = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["apiUrl"] = "",
                    ["apiKey"] = "",
                    ["syncFrequency"] = "1",
                    ["syncCustomers"] = true,
                    ["syncCarts"] = true
                })
            },
            new Integration
            {
                Name = "IKAS",
                Description = "IKAS e-ticaret platformu entegrasyonu",
                DetailedDescription = "IKAS e-ticaret altyapınızı entegre edin ve sepet hatırlatmalarını otomatikleştirin.",
                Type = "ecommerce",
                Category = "E-ticaret Altyapıları",
                IconClass = "I",
                IconColor = "text-purple-600",
                BackgroundColor = "bg-purple-100",
                IsActive = false,
                IsPopular = true,
                SortOrder = 2,
                Features = JsonSerializer.Serialize(new[]
                {
                    "API entegrasyonu",
                    "Otomatik senkronizasyon",
                    "Sepet takibi",
                    "Sepet hatırlatmaları",
                    "Müşteri bilgileri senkronizasyonu"
                }),
                DefaultSettingsTemplate = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["apiUrl"] = "https://api.ikas.com/v1/",
                    ["apiKey"] = "",

                    ["syncFrequency"] = "1",


                    ["syncCustomers"] = true,
            ["syncCarts"] = true
                })
            },
            new Integration
            {
                Name = "Ideasoft",
                Description = "Ideasoft e-ticaret platformu entegrasyonu",
                DetailedDescription = "Ideasoft e-ticaret altyapınızı entegre edin ve sepet hatırlatmalarını otomatikleştirin.",
                Type = "ecommerce",
                Category = "E-ticaret Altyapıları",
                IconClass = "I",
                IconColor = "text-gray-600",
                BackgroundColor = "bg-gray-100",
                IsActive = false,
                SortOrder = 3,
                Features = JsonSerializer.Serialize(new[]
                {
                    "API entegrasyonu",
                    "Otomatik senkronizasyon",
                    "Sepet takibi",
                    "Sepet hatırlatmaları",
                    "Müşteri bilgileri senkronizasyonu"
                }),
                DefaultSettingsTemplate = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["apiUrl"] = "",
                    ["apiKey"] = "",

                    ["syncFrequency"] = "1",


                    ["syncCustomers"] = true,
            ["syncCarts"] = true
                })
            },
            new Integration
            {
                Name = "Tsoft",
                Description = "Tsoft e-ticaret platformu entegrasyonu",
                DetailedDescription = "Tsoft e-ticaret altyapınızı entegre edin ve sepet hatırlatmalarını otomatikleştirin.",
                Type = "ecommerce",
                Category = "E-ticaret Altyapıları",
                IconClass = "T",
                IconColor = "text-red-600",
                BackgroundColor = "bg-red-100",
                IsActive = false,
                SortOrder = 4,
                Features = JsonSerializer.Serialize(new[]
                {
                    "API entegrasyonu",
                    "Otomatik senkronizasyon",
                    "Sepet takibi",
                    "Sepet hatırlatmaları",
                    "Müşteri bilgileri senkronizasyonu"
                }),
                DefaultSettingsTemplate = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["apiUrl"] = "",
                    ["apiKey"] = "",

                    ["syncFrequency"] = "1",


                    ["syncCustomers"] = true,
            ["syncCarts"] = true
                })
            },
            // İletişim Kanalları
            new Integration
            {
                Name = "WhatsApp",
                Description = "WhatsApp Business API entegrasyonu",
                DetailedDescription = "WhatsApp Business API ile müşterilerinize doğrudan mesaj gönderin ve otomatik yanıtlar oluşturun.",
                Type = "communication",
                Category = "İletişim Kanalları",
                IconClass = "message-circle",
                IconColor = "text-green-600",
                BackgroundColor = "bg-green-100",
                IsActive = true,
                IsPopular = true,
                SortOrder = 1,
                Features = JsonSerializer.Serialize(new[]
                {
                    "WhatsApp Business API",
                    "Otomatik mesaj gönderimi",
                    "Mesaj şablonları",
                    "Grup mesajlaşma",
                    "Medya dosya desteği"
                }),
                DefaultSettingsTemplate = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["accessToken"] = "",
                    ["businessAccountId"] = "",
                    ["phoneNumberId"] = "",
                    ["autoNotifications"] = true
                })
            },
            new Integration
            {
                Name = "Telegram",
                Description = "Telegram Bot API entegrasyonu",
                DetailedDescription = "Telegram Bot API ile müşterilerinize anlık bildirimler gönderin ve bot komutları oluşturun.",
                Type = "communication",
                Category = "İletişim Kanalları",
                IconClass = "send",
                IconColor = "text-blue-500",
                BackgroundColor = "bg-blue-100",
                IsActive = true,
                SortOrder = 2,
                Features = JsonSerializer.Serialize(new[]
                {
                    "Telegram Bot API",
                    "Anlık bildirimler",
                    "Bot komutları",
                    "Grup yönetimi",
                    "Dosya paylaşımı"
                }),
                DefaultSettingsTemplate = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["botToken"] = "",
                    ["chatId"] = "",
                    ["webhookUrl"] = "",
                    ["enableCommands"] = true,
                    ["autoNotifications"] = false
                })
            },
            new Integration
            {
                Name = "Email SMTP",
                Description = "SMTP email sunucusu entegrasyonu",
                DetailedDescription = "Kendi SMTP sunucunuz veya üçüncü parti email servisleriniz ile email gönderimi yapın.",
                Type = "communication",
                Category = "İletişim Kanalları",
                IconClass = "mail",
                IconColor = "text-gray-600",
                BackgroundColor = "bg-gray-100",
                IsActive = true,
                SortOrder = 3,
                Features = JsonSerializer.Serialize(new[]
                {
                    "SMTP sunucu desteği",
                    "Toplu email gönderimi",
                    "HTML email şablonları",
                    "Ek dosya desteği",
                    "Gönderim raporları"
                }),
                DefaultSettingsTemplate = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["smtpServer"] = "",
                    ["smtpPort"] = 587,
                    ["username"] = "",
                    ["password"] = "",
                    ["enableSsl"] = true,
                    ["fromEmail"] = "",
                    ["fromName"] = ""
                })
            },
            new Integration
            {
                Name = "SMS",
                Description = "SMS gönderim servisi",
                DetailedDescription = "NetGSM ve diğer SMS sağlayıcıları ile müşterilerinize SMS gönderin.",
                Type = "communication",
                Category = "İletişim Kanalları",
                IconClass = "message-square",
                IconColor = "text-orange-600",
                BackgroundColor = "bg-orange-100",
                IsActive = true,
                IsPopular = false,
                SortOrder = 4,
                Features = JsonSerializer.Serialize(new[]
                {
                    "NetGSM entegrasyonu",
                    "Otomatik SMS gönderimi",
                    "Türkçe karakter desteği",
                    "Toplu SMS gönderimi",
                    "Bakiye sorgulama"
                }),
                DefaultSettingsTemplate = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["provider"] = "netgsm",
                    ["username"] = "",
                    ["password"] = "",
                    ["senderName"] = "",
                    ["autoNotifications"] = true
                })
            }
        };

        _context.Integrations.AddRange(integrations);
        await _context.SaveChangesAsync();

        // Seed some test integrations for demo purposes
        await SeedTestIntegrationsAsync();

        _logger.LogInformation("Integration seed data created successfully.");
    }

    private async Task SeedTestIntegrationsAsync()
    {
        // Get test user and their company
        var testUser = await _context.Users
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Email == "huseyin.arslan@defaultcompany.com");

        if (testUser?.Company == null) return;

        // Get some integrations for test setup
        var ticimaxIntegration = await _context.Integrations.FirstOrDefaultAsync(i => i.Name == "Ticimax");
        var ikasIntegration = await _context.Integrations.FirstOrDefaultAsync(i => i.Name == "IKAS");
        var whatsappIntegration = await _context.Integrations.FirstOrDefaultAsync(i => i.Name == "WhatsApp");

        if (ticimaxIntegration == null || ikasIntegration == null || whatsappIntegration == null) return;

        // Check if test integrations already exist for this company
        var existingIntegrations = await _context.CompanyIntegrations
            .Where(ci => ci.CompanyId == testUser.Company.Id)
            .ToListAsync();

        if (existingIntegrations.Any()) return;

        // Create test integrations for the company
        var companyIntegrations = new List<CompanyIntegration>
        {
            new CompanyIntegration
            {
                CompanyId = testUser.Company.Id,
                IntegrationId = ticimaxIntegration.Id,
                IsActive = true,
                IsConfigured = true,
                CreatedAt = DateTime.Now.AddDays(-15),
                ConfiguredAt = DateTime.Now.AddDays(-15),
                LastSyncAt = DateTime.Now.AddMinutes(-15),
                SettingsJson = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["apiUrl"] = "",
                    ["apiKey"] = "",
                    ["syncFrequency"] = "1",
                    ["syncCustomers"] = true,
                    ["syncCarts"] = true
                }),
                SyncStatsJson = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["syncedCustomers"] = 1245,
                    ["syncedCarts"] = 842,
                    ["lastSyncDuration"] = "2.3s"
                })
            },
            new CompanyIntegration
            {
                CompanyId = testUser.Company.Id,
                IntegrationId = ikasIntegration.Id,
                IsActive = true,
                IsConfigured = true,
                CreatedAt = DateTime.Now.AddDays(-10),
                ConfiguredAt = DateTime.Now.AddDays(-10),
                LastSyncAt = DateTime.Now.AddMinutes(-32),
                SettingsJson = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["apiUrl"] = "https://api.ikas.com/v1/",
                    ["apiKey"] = "••••••••••••••••",
                    ["storeId"] = "IK67890",
                    ["syncFrequency"] = "30",


                    ["syncCustomers"] = true,
            ["syncCarts"] = true
                }),
                SyncStatsJson = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["syncedProducts"] = 876,
                    ["syncedOrders"] = 542,
                    ["lastSyncDuration"] = "1.8s"
                })
            },
            new CompanyIntegration
            {
                CompanyId = testUser.Company.Id,
                IntegrationId = whatsappIntegration.Id,
                IsActive = true,
                IsConfigured = true,
                CreatedAt = DateTime.Now.AddDays(-5),
                ConfiguredAt = DateTime.Now.AddDays(-5),
                LastSyncAt = DateTime.Now.AddHours(-2),
                SettingsJson = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["accessToken"] = "EAAPIEi25fJwBOZCN1ZASoc4tYrE9GoJXgE1j5E88xNJ1Ptn6kbezY961zlZCFpGspddRcucbLOdYe6ZBNSfVUK0fNEN6Ck4kZCKOB3mhZAr6vZBEIwMtGSm8r8ACR6pCUxxyR79NER4yptdcOjkdJ8l1Rw5OpZCUC4m6CotaSfj38zWbKyaCsMgzzy2wlEaYMdCZAoQZDZD",
                    ["businessAccountId"] = "1079867810657316",
                    ["phoneNumberId"] = "706401259212984",
                    ["autoNotifications"] = true
                }),
                SyncStatsJson = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["sentMessages"] = 1248,
                    ["responseRate"] = "68%",
                    ["lastActivity"] = "2 saat önce"
                })
            }
        };

        _context.CompanyIntegrations.AddRange(companyIntegrations);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Test integrations created successfully.");
    }
}
