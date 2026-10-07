using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using System.Text.Json;

namespace PushDashboard.Services;

public class ModuleUsageSeedService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ModuleUsageSeedService> _logger;

    public ModuleUsageSeedService(ApplicationDbContext context, ILogger<ModuleUsageSeedService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            // Check if usage logs already exist
            var existingLogsCount = await _context.ModuleUsageLogs.CountAsync();
            if (existingLogsCount > 0)
            {
                _logger.LogInformation("Module usage logs already exist ({Count}), skipping seed.", existingLogsCount);
                return;
            }

            // Get required data
            var defaultCompany = await _context.Companies.FirstOrDefaultAsync();
            if (defaultCompany == null)
            {
                _logger.LogWarning("No company found for module usage seed data");
                return;
            }

            var defaultUser = await _context.Users.FirstOrDefaultAsync();
            if (defaultUser == null)
            {
                _logger.LogWarning("No user found for module usage seed data");
                return;
            }

            // Get modules
            var firstOrderModule = await _context.Modules.FirstOrDefaultAsync(m => m.Name.Contains("İlk Alışveriş"));
            var birthdayModule = await _context.Modules.FirstOrDefaultAsync(m => m.Name.Contains("Doğum Günü"));
            var commentModule = await _context.Modules.FirstOrDefaultAsync(m => m.Name.Contains("Yorum"));

            if (firstOrderModule == null || birthdayModule == null || commentModule == null)
            {
                _logger.LogWarning("Required modules not found for usage seed data");
                return;
            }

            var usageLogs = new List<ModuleUsageLog>();
            var currentBalance = defaultCompany.CreditBalance;
            var baseDate = DateTime.Now.AddDays(-30);

            // Generate 50 sample usage logs over the last 30 days
            var random = new Random();
            var modules = new[] { firstOrderModule, birthdayModule, commentModule };
            var usageTypes = new[] { "email", "sms", "whatsapp" };
            var channels = new[] { "email", "sms", "whatsapp" };
            var costs = new Dictionary<string, decimal>
            {
                ["email"] = 0.50m,
                ["sms"] = 2.00m,
                ["whatsapp"] = 1.00m
            };

            for (int i = 0; i < 50; i++)
            {
                var module = modules[random.Next(modules.Length)];
                var usageType = usageTypes[random.Next(usageTypes.Length)];
                var channel = channels[random.Next(channels.Length)];
                var cost = costs[usageType] + (decimal)(random.NextDouble() * 0.5 - 0.25); // ±0.25 variation
                cost = Math.Max(0.10m, Math.Round(cost, 2));

                var isSuccessful = random.NextDouble() > 0.05; // 95% success rate
                var balanceBefore = currentBalance;
                
                if (isSuccessful)
                {
                    currentBalance -= cost;
                }

                var balanceAfter = currentBalance;
                var createdAt = baseDate.AddDays(random.NextDouble() * 30).AddHours(random.NextDouble() * 24);

                var descriptions = new[]
                {
                    $"Email gönderildi: customer{random.Next(1, 100)}@example.com",
                    $"Doğum günü bildirimi gönderildi: Ahmet Yılmaz",
                    $"Sepet hatırlatma mesajı gönderildi",
                    $"Kampanya bildirimi gönderildi: Mehmet Demir",
                    $"Hoşgeldin mesajı gönderildi: Ayşe Kaya",
                    $"Sipariş onay mesajı gönderildi",
                    $"Profil güncelleme bildirimi gönderildi"
                };

                var usageLog = new ModuleUsageLog
                {
                    CompanyId = defaultCompany.Id,
                    ModuleId = module.Id,
                    UsageType = usageType,
                    Description = descriptions[random.Next(descriptions.Length)],
                    Cost = cost,
                    BalanceBefore = balanceBefore,
                    BalanceAfter = balanceAfter,
                    UserId = defaultUser.Id,
                    ReferenceId = $"ref_{random.Next(1000, 9999)}",
                    Channel = channel,
                    IsSuccessful = isSuccessful,
                    ErrorMessage = isSuccessful ? null : GetRandomErrorMessage(random),
                    CreatedAt = createdAt,
                    Metadata = JsonSerializer.Serialize(new Dictionary<string, object>
                    {
                        ["customerEmail"] = $"customer{random.Next(1, 100)}@example.com",
                        ["templateName"] = GetRandomTemplateName(random),
                        ["sentAt"] = createdAt,
                        ["ipAddress"] = $"192.168.1.{random.Next(1, 255)}",
                        ["userAgent"] = "PushDashboard/1.0"
                    })
                };

                usageLogs.Add(usageLog);
            }

            // Sort by creation date
            usageLogs = usageLogs.OrderBy(ul => ul.CreatedAt).ToList();

            // Recalculate balances in chronological order
            currentBalance = defaultCompany.CreditBalance;
            foreach (var log in usageLogs)
            {
                log.BalanceBefore = currentBalance;
                if (log.IsSuccessful)
                {
                    currentBalance -= log.Cost;
                }
                log.BalanceAfter = currentBalance;
            }

            // Add to database
            await _context.ModuleUsageLogs.AddRangeAsync(usageLogs);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully seeded {Count} module usage logs", usageLogs.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding module usage logs");
            throw;
        }
    }

    private static string GetRandomErrorMessage(Random random)
    {
        var errorMessages = new[]
        {
            "SMTP sunucusuna bağlanılamadı",
            "Geçersiz email adresi",
            "Günlük gönderim limiti aşıldı",
            "Müşteri email iznini geri çekmiş",
            "Ağ bağlantı hatası",
            "Template bulunamadı",
            "Yetersiz bakiye"
        };

        return errorMessages[random.Next(errorMessages.Length)];
    }

    private static string GetRandomTemplateName(Random random)
    {
        var templateNames = new[]
        {
            "Hoşgeldin Mesajı",
            "Doğum Günü Kutlaması",
            "Sepet Hatırlatma",
            "Kampanya Bildirimi",
            "Sipariş Onayı",
            "Profil Güncelleme",
            "Şifre Sıfırlama"
        };

        return templateNames[random.Next(templateNames.Length)];
    }
}
