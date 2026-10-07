using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using System.Text.Json;

namespace PushDashboard.Services;

public interface ISmsTemplateSeedService
{
    Task SeedDefaultTemplatesAsync();
}

public class SmsTemplateSeedService : ISmsTemplateSeedService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SmsTemplateSeedService> _logger;

    public SmsTemplateSeedService(ApplicationDbContext context, ILogger<SmsTemplateSeedService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedDefaultTemplatesAsync()
    {
        try
        {
            // Check if templates already exist
            if (await _context.SmsTemplates.AnyAsync())
            {
                _logger.LogInformation("SMS templates already seeded, skipping...");
                return;
            }

            _logger.LogInformation("Seeding default SMS templates...");

            var templates = new List<SmsTemplate>
            {
                // Müşteri Category
                new SmsTemplate
                {
                    Name = "Hoş Geldiniz Mesajı",
                    Category = "Müşteri",
                    Description = "Yeni kayıt olan müşterilere hoş geldiniz mesajı",
                    DefaultContent = "Merhaba {{customerName}}! {{companyName}}'e hoş geldiniz. Bizi tercih ettiğiniz için teşekkür ederiz.",
                    Variables = JsonSerializer.Serialize(new[] { "customerName", "companyName", "registrationDate" }),
                    SortOrder = 1,
                    IsActive = true
                },
                new SmsTemplate
                {
                    Name = "Müşteri Bilgileri Güncellendi",
                    Category = "Müşteri",
                    Description = "Müşteri bilgileri güncellendiğinde gönderilir",
                    DefaultContent = "Sayın {{customerName}}, hesap bilgileriniz {{updateDate}} tarihinde güncellendi. İyi günler dileriz.",
                    Variables = JsonSerializer.Serialize(new[] { "customerName", "updateDate", "updatedFields" }),
                    SortOrder = 2,
                    IsActive = true
                },

                // Sepet Category
                new SmsTemplate
                {
                    Name = "Sepet Hatırlatma",
                    Category = "Sepet",
                    Description = "Terk edilen sepetler için hatırlatma mesajı",
                    DefaultContent = "Sepetinizde {{itemCount}} ürün bekliyor! Toplam: {{basketTotal}}. Alışverişi tamamlamak için: {{basketUrl}}",
                    Variables = JsonSerializer.Serialize(new[] { "customerName", "itemCount", "basketTotal", "basketUrl" }),
                    SortOrder = 1,
                    IsActive = true
                },
                new SmsTemplate
                {
                    Name = "Sepet İndirim Fırsatı",
                    Category = "Sepet",
                    Description = "Sepetteki ürünler için özel indirim sunumu",
                    DefaultContent = "{{customerName}}, sepetinizdeki ürünlerde özel indirim! Kodunuz: {{discountCode}}. Son kullanma: {{expiryDate}}",
                    Variables = JsonSerializer.Serialize(new[] { "customerName", "discountCode", "expiryDate", "basketUrl" }),
                    SortOrder = 2,
                    IsActive = true
                },

                // Sipariş Category
                new SmsTemplate
                {
                    Name = "Sipariş Onayı",
                    Category = "Sipariş",
                    Description = "Sipariş alındığında gönderilir",
                    DefaultContent = "Siparişiniz alındı! Sipariş No: {{orderNumber}}. Toplam: {{orderTotal}}. Teslimat: {{deliveryAddress}}. Takip: {{trackingUrl}}",
                    Variables = JsonSerializer.Serialize(new[] { "customerName", "orderNumber", "orderDate", "orderTotal", "deliveryAddress", "trackingUrl" }),
                    SortOrder = 1,
                    IsActive = true
                },
                new SmsTemplate
                {
                    Name = "Sipariş Kargoya Verildi",
                    Category = "Sipariş",
                    Description = "Sipariş kargoya verildiğinde gönderilir",
                    DefaultContent = "{{orderNumber}} nolu siparişiniz kargoya verildi. Kargo takip: {{trackingUrl}}. İyi günler dileriz!",
                    Variables = JsonSerializer.Serialize(new[] { "customerName", "orderNumber", "trackingUrl", "carrierName" }),
                    SortOrder = 2,
                    IsActive = true
                },
                new SmsTemplate
                {
                    Name = "Sipariş Teslim Edildi",
                    Category = "Sipariş",
                    Description = "Sipariş teslim edildiğinde gönderilir",
                    DefaultContent = "{{orderNumber}} nolu siparişiniz teslim edildi. Bizi tercih ettiğiniz için teşekkür ederiz!",
                    Variables = JsonSerializer.Serialize(new[] { "customerName", "orderNumber", "deliveryDate" }),
                    SortOrder = 3,
                    IsActive = true
                },

                // Genel Category
                new SmsTemplate
                {
                    Name = "Şifre Sıfırlama",
                    Category = "Genel",
                    Description = "Şifre sıfırlama talebi için kod gönderimi",
                    DefaultContent = "Şifre sıfırlama kodunuz: {{resetCode}}. Bu kod 15 dakika geçerlidir. {{companyName}}",
                    Variables = JsonSerializer.Serialize(new[] { "customerName", "resetCode", "companyName" }),
                    SortOrder = 1,
                    IsActive = true
                },
                new SmsTemplate
                {
                    Name = "Kampanya Duyurusu",
                    Category = "Genel",
                    Description = "Kampanya ve duyurular için genel mesaj",
                    DefaultContent = "{{customerName}}, {{campaignTitle}} kampanyamızdan haberdar oldunuz mu? Detaylar: {{campaignUrl}}",
                    Variables = JsonSerializer.Serialize(new[] { "customerName", "campaignTitle", "campaignUrl", "endDate" }),
                    SortOrder = 2,
                    IsActive = true
                },
                new SmsTemplate
                {
                    Name = "Doğrulama Kodu",
                    Category = "Genel",
                    Description = "İki faktörlü doğrulama veya hesap doğrulama kodu",
                    DefaultContent = "Doğrulama kodunuz: {{verificationCode}}. Bu kodu kimseyle paylaşmayın. {{companyName}}",
                    Variables = JsonSerializer.Serialize(new[] { "verificationCode", "companyName" }),
                    SortOrder = 3,
                    IsActive = true
                }
            };

            await _context.SmsTemplates.AddRangeAsync(templates);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully seeded {Count} SMS templates", templates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding SMS templates");
            throw;
        }
    }
}
