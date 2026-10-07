using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using System.Text.Json;

namespace PushDashboard.Services;

public interface IEmailTemplateSeedService
{
    Task SeedAsync();
}

public class EmailTemplateSeedService : IEmailTemplateSeedService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EmailTemplateSeedService> _logger;

    public EmailTemplateSeedService(ApplicationDbContext context, ILogger<EmailTemplateSeedService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            // Check if templates already exist
            var existingTemplatesCount = await _context.EmailTemplates.CountAsync();
            var expectedTemplatesCount = 7; // Hoşgeldin, Profil Güncelleme, Terk Edilmiş Sepet, Sipariş Onayı, Doğum Günü, İlk Alışveriş, Sepet Hatırlatma

            if (existingTemplatesCount >= expectedTemplatesCount)
            {
                _logger.LogInformation("Email templates already exist ({ExistingCount}/{ExpectedCount}), checking for updates...", existingTemplatesCount, expectedTemplatesCount);

                // Doğum günü template'ini güncelle (hediye çeki değişkenleri için)
                await UpdateBirthdayTemplateAsync();
                return;
            }

            _logger.LogInformation("Seeding email templates...");

            var templates = GetDefaultTemplates();

            await _context.EmailTemplates.AddRangeAsync(templates);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully seeded {Count} email templates.", templates.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding email templates");
            throw;
        }
    }

    private async Task UpdateBirthdayTemplateAsync()
    {
        try
        {
            // Doğum günü template'ini bul
            var birthdayTemplate = await _context.EmailTemplates
                .FirstOrDefaultAsync(t => t.Name == "Doğum Günü Kutlaması");

            if (birthdayTemplate == null)
            {
                _logger.LogWarning("Birthday template not found for update");
                return;
            }

            // Hediye çeki değişkenlerini kontrol et
            var currentVariables = JsonSerializer.Deserialize<string[]>(birthdayTemplate.Variables ?? "[]");
            var giftVoucherVariables = new[] { "hasGiftVoucher", "giftVoucherCode", "giftVoucherAmount", "giftVoucherMessage", "giftVoucherSecurityNote", "giftVoucherUsageNote" };

            var hasGiftVoucherVariables = giftVoucherVariables.All(v => currentVariables.Contains(v));

            if (hasGiftVoucherVariables && birthdayTemplate.DefaultContent.Contains("{{#if hasGiftVoucher}}"))
            {
                _logger.LogInformation("Birthday template already has gift voucher variables and content, skipping update");
                return;
            }

            // Template'i güncelle
            var newTemplate = GetDefaultTemplates().First(t => t.Name == "Doğum Günü Kutlaması");

            birthdayTemplate.DefaultContent = newTemplate.DefaultContent;
            birthdayTemplate.Variables = newTemplate.Variables;
            birthdayTemplate.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully updated birthday template with gift voucher variables");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating birthday template");
        }
    }

    private List<EmailTemplate> GetDefaultTemplates()
    {
        return new List<EmailTemplate>
        {
            // Müşteri Kategorisi
            new EmailTemplate
            {
                Name = "Hoşgeldin Emaili",
                Category = "Müşteri",
                Description = "Yeni müşteri kayıt olduğunda gönderilen hoşgeldin emaili",
                DefaultSubject = "Hoşgeldiniz {{customerName}}!",
                DefaultContent = @"
<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #2563eb;'>Merhaba {{customerName}},</h2>
        <p>{{companyName}} ailesine hoşgeldiniz!</p>
        <p>Hesabınız başarıyla oluşturuldu. Artık tüm özelliklerimizden faydalanabilirsiniz.</p>
        <div style='background: #f8fafc; padding: 15px; border-radius: 8px; margin: 20px 0;'>
            <h3>Hesap Bilgileriniz:</h3>
            <p><strong>Email:</strong> {{customerEmail}}</p>
            <p><strong>Kayıt Tarihi:</strong> {{registrationDate}}</p>
        </div>
        <p>Herhangi bir sorunuz olursa bizimle iletişime geçmekten çekinmeyin.</p>
        <p>İyi alışverişler dileriz!</p>
        <hr style='margin: 30px 0; border: none; border-top: 1px solid #e5e7eb;'>
        <p style='font-size: 12px; color: #6b7280;'>{{companyName}} - {{companyAddress}}</p>
    </div>
</body>
</html>",
                Variables = JsonSerializer.Serialize(new[] { "customerName", "customerEmail", "companyName", "companyAddress", "registrationDate" }),
                ModuleId = null, // Herkese açık
                IsActive = true,
                SortOrder = 1
            },

            new EmailTemplate
            {
                Name = "Profil Güncelleme Onayı",
                Category = "Müşteri",
                Description = "Müşteri profil bilgilerini güncellediğinde gönderilen onay emaili",
                DefaultSubject = "Profil Bilgileriniz Güncellendi",
                DefaultContent = @"
<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #2563eb;'>Merhaba {{customerName}},</h2>
        <p>Profil bilgileriniz başarıyla güncellendi.</p>
        <div style='background: #f0f9ff; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #2563eb;'>
            <p><strong>Güncelleme Tarihi:</strong> {{updateDate}}</p>
            <p><strong>Güncellenen Alanlar:</strong> {{updatedFields}}</p>
        </div>
        <p>Bu güncellemeyi siz yapmadıysanız, lütfen derhal bizimle iletişime geçin.</p>
        <hr style='margin: 30px 0; border: none; border-top: 1px solid #e5e7eb;'>
        <p style='font-size: 12px; color: #6b7280;'>{{companyName}} - {{companyAddress}}</p>
    </div>
</body>
</html>",
                Variables = JsonSerializer.Serialize(new[] { "customerName", "updateDate", "updatedFields", "companyName", "companyAddress" }),
                ModuleId = null,
                IsActive = true,
                SortOrder = 2
            },

            // Sepet Kategorisi
            new EmailTemplate
            {
                Name = "Terk Edilmiş Sepet",
                Category = "Sepet",
                Description = "Müşteri sepetini terk ettiğinde gönderilen hatırlatma emaili",
                DefaultSubject = "Sepetinizde ürünler bekliyor {{customerName}}",
                DefaultContent = @"
<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #dc2626;'>Sepetinizi unutmayın!</h2>
        <p>Merhaba {{customerName}},</p>
        <p>Sepetinizde {{itemCount}} adet ürün bulunuyor. Alışverişinizi tamamlamak için geri dönün!</p>
        <div style='background: #fef2f2; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #dc2626;'>
            <h3>Sepet Özeti:</h3>
            <p><strong>Toplam Tutar:</strong> {{basketTotal}}</p>
            <p><strong>Ürün Sayısı:</strong> {{itemCount}}</p>
        </div>
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{{basketUrl}}' style='background: #dc2626; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block;'>Alışverişi Tamamla</a>
        </div>
        <hr style='margin: 30px 0; border: none; border-top: 1px solid #e5e7eb;'>
        <p style='font-size: 12px; color: #6b7280;'>{{companyName}} - {{companyAddress}}</p>
    </div>
</body>
</html>",
                Variables = JsonSerializer.Serialize(new[] { "customerName", "itemCount", "basketTotal", "basketUrl", "companyName", "companyAddress" }),
                ModuleId = null,
                IsActive = true,
                SortOrder = 3
            },

            // Sipariş Kategorisi
            new EmailTemplate
            {
                Name = "Sipariş Onayı",
                Category = "Sipariş",
                Description = "Sipariş verildiğinde gönderilen onay emaili",
                DefaultSubject = "Siparişiniz Alındı - #{{orderNumber}}",
                DefaultContent = @"
<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #059669;'>Siparişiniz Alındı!</h2>
        <p>Merhaba {{customerName}},</p>
        <p>Siparişiniz başarıyla alındı ve işleme konuldu.</p>
        <div style='background: #f0fdf4; padding: 15px; border-radius: 8px; margin: 20px 0; border-left: 4px solid #059669;'>
            <h3>Sipariş Detayları:</h3>
            <p><strong>Sipariş No:</strong> #{{orderNumber}}</p>
            <p><strong>Sipariş Tarihi:</strong> {{orderDate}}</p>
            <p><strong>Toplam Tutar:</strong> {{orderTotal}}</p>
            <p><strong>Teslimat Adresi:</strong> {{deliveryAddress}}</p>
        </div>
        <p>Siparişinizin kargo durumunu takip etmek için aşağıdaki bağlantıyı kullanabilirsiniz:</p>
        <div style='text-align: center; margin: 30px 0;'>
            <a href='{{trackingUrl}}' style='background: #059669; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; display: inline-block;'>Siparişi Takip Et</a>
        </div>
        <hr style='margin: 30px 0; border: none; border-top: 1px solid #e5e7eb;'>
        <p style='font-size: 12px; color: #6b7280;'>{{companyName}} - {{companyAddress}}</p>
    </div>
</body>
</html>",
                Variables = JsonSerializer.Serialize(new[] { "customerName", "orderNumber", "orderDate", "orderTotal", "deliveryAddress", "trackingUrl", "companyName", "companyAddress" }),
                ModuleId = null,
                IsActive = true,
                SortOrder = 4
            },

            // Doğum Günü Kategorisi
            new EmailTemplate
            {
                Name = "Doğum Günü Kutlaması",
                Category = "Müşteri",
                Description = "Müşterinin doğum günü olduğunda gönderilen kutlama emaili",
                DefaultSubject = "Doğum Gününüz Kutlu Olsun {{customerFirstName}}! 🎉",
                DefaultContent = @"
<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 10px;'>
        <div style='background: white; padding: 30px; border-radius: 10px; text-align: center;'>
            <div style='font-size: 48px; margin-bottom: 20px;'>🎉🎂🎈</div>
            <h1 style='color: #667eea; margin-bottom: 10px; font-size: 28px;'>Doğum Gününüz Kutlu Olsun!</h1>
            <h2 style='color: #333; margin-bottom: 20px;'>Sevgili {{customerName}},</h2>

            <div style='background: #f8f9ff; padding: 20px; border-radius: 8px; margin: 25px 0; border-left: 4px solid #667eea;'>
                <p style='font-size: 18px; color: #667eea; margin: 0;'><strong>{{birthdayMessage}}</strong></p>
                <p style='margin: 10px 0 0 0; color: #666;'>{{birthdayWish}}</p>
            </div>

            <p style='font-size: 16px; margin: 20px 0;'>
                {{companyName}} ailesi olarak, bu özel gününüzde sizinle birlikte olmaktan mutluluk duyuyoruz.
                Yeni yaşınızın size sağlık, mutluluk ve başarı getirmesini diliyoruz.
            </p>

            <!-- Hediye Çeki Bölümü (Conditional) -->
            {{#if hasGiftVoucher}}
            <div style='background: #fef3c7; padding: 20px; border-radius: 8px; margin: 25px 0; border: 2px solid #f59e0b;'>
                <div style='text-align: center; margin-bottom: 15px;'>
                    <div style='font-size: 32px; margin-bottom: 10px;'>🎁</div>
                    <h3 style='color: #d97706; margin: 0 0 10px 0; font-size: 20px;'>Size Özel Doğum Günü Hediye Çeki!</h3>
                </div>

                <p style='margin: 0 0 15px 0; font-size: 16px; text-align: center; color: #92400e;'>
                    {{giftVoucherMessage}}
                </p>

                <div style='background: white; padding: 15px; border-radius: 6px; text-align: center; margin: 15px 0; border: 1px solid #f59e0b;'>
                    <p style='margin: 0 0 5px 0; font-size: 12px; color: #6b7280; text-transform: uppercase; letter-spacing: 1px;'>Hediye Çeki Kodunuz</p>
                    <div style='font-size: 24px; font-weight: bold; color: #d97706; letter-spacing: 2px; font-family: monospace;'>
                        {{giftVoucherCode}}
                    </div>
                </div>

                <div style='background: #fbbf24; padding: 10px; border-radius: 6px; margin: 10px 0;'>
                    <p style='margin: 0; font-size: 12px; color: #92400e; text-align: center;'>
                        🔒 {{giftVoucherSecurityNote}}
                    </p>
                </div>

                <p style='margin: 10px 0 0 0; font-size: 12px; color: #6b7280; text-align: center;'>
                    💡 {{giftVoucherUsageNote}}
                </p>
            </div>
            {{else}}
            <div style='background: #fff3cd; padding: 15px; border-radius: 8px; margin: 25px 0; border: 1px solid #ffeaa7;'>
                <p style='margin: 0; color: #856404;'>
                    <strong>🎁 Özel Doğum Günü Sürprizi!</strong><br>
                    Bu özel gününüz için sizin için hazırladığımız kampanyalarımızı kaçırmayın!
                </p>
            </div>
            {{/if}}

            <div style='margin: 30px 0;'>
                <div style='display: inline-block; background: #667eea; color: white; padding: 12px 30px; text-decoration: none; border-radius: 25px; font-weight: bold;'>
                    🛍️ Alışverişe Başla
                </div>
            </div>

            <p style='font-size: 14px; color: #666; margin-top: 30px;'>
                Bir kez daha doğum gününüzü kutlar, nice mutlu yıllar dileriz!
            </p>

            <hr style='margin: 30px 0; border: none; border-top: 1px solid #e5e7eb;'>
            <div style='text-align: center;'>
                <p style='font-size: 16px; color: #667eea; margin: 0;'><strong>{{companyName}}</strong></p>
                <p style='font-size: 12px; color: #6b7280; margin: 5px 0 0 0;'>{{companyAddress}}</p>
                <p style='font-size: 12px; color: #6b7280; margin: 5px 0 0 0;'>{{companyEmail}} | {{companyPhone}}</p>
            </div>
        </div>
    </div>
</body>
</html>",
                Variables = JsonSerializer.Serialize(new[] {
                    "customerName", "customerFirstName", "customerLastName", "customerEmail", "customerAge",
                    "companyName", "companyEmail", "companyPhone", "companyWebsite", "companyAddress",
                    "currentDate", "currentYear", "birthdayMessage", "birthdayWish",
                    // Hediye çeki değişkenleri
                    "hasGiftVoucher", "giftVoucherCode", "giftVoucherAmount", "giftVoucherMessage",
                    "giftVoucherSecurityNote", "giftVoucherUsageNote"
                }),
                ModuleId = 3, // Herkese açık
                IsActive = true,
                SortOrder = 5
            },

            // İlk Alışveriş Kategorisi
            new EmailTemplate
            {
                Name = "İlk Alışveriş Tebriği",
                Category = "Müşteri",
                Description = "İlk alışveriş yapan müşterilere gönderilen hoş geldin mesajı",
                DefaultSubject = "Hoş geldiniz! İlk alışverişiniz için teşekkürler 🎉",
                DefaultContent = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>İlk Alışveriş Tebriği</title>
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px;"">
    <div style=""background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;"">
        <h1 style=""margin: 0; font-size: 28px;"">🎉 Hoş Geldiniz!</h1>
        <p style=""margin: 10px 0 0 0; font-size: 16px; opacity: 0.9;"">İlk alışverişiniz için teşekkür ederiz</p>
    </div>

    <div style=""background: #f8f9fa; padding: 30px; border-radius: 0 0 10px 10px;"">
        <p style=""font-size: 16px; margin-bottom: 20px;"">
            Merhaba <strong>{{customer_name}}</strong>,
        </p>

        <p style=""font-size: 16px; margin-bottom: 20px;"">
            {{company_name}} ailesine hoş geldiniz! İlk alışverişinizi yaptığınız için çok mutluyuz.
            Sizin gibi değerli müşterilerimizle büyümeye devam ediyoruz.
        </p>

        {{#if has_gift_voucher}}
        <div style=""background: #fff; border: 2px dashed #28a745; border-radius: 10px; padding: 20px; margin: 20px 0; text-align: center;"">
            <h3 style=""color: #28a745; margin: 0 0 10px 0;"">🎁 Özel Hediye Çekiniz</h3>
            <p style=""margin: 10px 0;"">İlk alışverişiniz için size özel bir hediye çeki hazırladık:</p>
            <div style=""background: #28a745; color: white; padding: 15px; border-radius: 5px; font-size: 18px; font-weight: bold; margin: 10px 0;"">
                {{gift_voucher_code}}
            </div>
            <p style=""margin: 10px 0; color: #666; font-size: 14px;"">
                Hediye çeki tutarı: <strong>{{gift_voucher_amount}}</strong><br>
                Bu kodu bir sonraki alışverişinizde kullanabilirsiniz.
            </p>
        </div>
        {{/if}}

        <p style=""font-size: 16px; margin-bottom: 20px;"">
            Sorularınız için bizimle iletişime geçmekten çekinmeyin.
            Keyifli alışverişler dileriz!
        </p>

        <div style=""text-align: center; margin-top: 30px;"">
            <p style=""color: #666; font-size: 14px;"">
                Saygılarımızla,<br>
                <strong>{{company_name}} Ekibi</strong>
            </p>
        </div>
    </div>

    <div style=""text-align: center; margin-top: 20px; color: #666; font-size: 12px;"">
        <p>Bu e-posta {{current_date}} tarihinde otomatik olarak gönderilmiştir.</p>
    </div>
</body>
</html>",
                Variables = JsonSerializer.Serialize(new[]
                {
                    "customer_name", "customer_first_name", "customer_last_name", "customer_email",
                    "company_name", "current_date", "current_time",
                    "gift_voucher_code", "gift_voucher_amount", "has_gift_voucher"
                }),
                ModuleId = null, // Herkese açık
                IsActive = true,
                SortOrder = 6
            },

            // Sepet Hatırlatma Template'i
            new EmailTemplate
            {
                Name = "Sepet Hatırlatma",
                Category = "Sepet",
                Description = "Terk edilmiş sepetler için otomatik hatırlatma emaili",
                DefaultSubject = "{{FirstName}}, sepetinizde ürünler bekliyor! 🛒",
                DefaultContent = @"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <title>Sepet Hatırlatma</title>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
</head>
<body style=""font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f8f9fa;"">
    <div style=""max-width: 600px; margin: 0 auto; background-color: white; border-radius: 10px; overflow: hidden; box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);"">

        <!-- Header -->
        <div style=""background: linear-gradient(135deg, #f39c12 0%, #e67e22 100%); color: white; padding: 30px 20px; text-align: center;"">
            <div style=""font-size: 48px; margin-bottom: 10px;"">🛒</div>
            <h1 style=""margin: 0; font-size: 24px; font-weight: bold;"">Sepetinizi Unutmayın!</h1>
            <p style=""margin: 10px 0 0 0; font-size: 16px; opacity: 0.9;"">Seçtiğiniz ürünler sizi bekliyor</p>
        </div>

        <!-- Content -->
        <div style=""padding: 30px 20px;"">
            <p style=""font-size: 18px; margin-bottom: 20px;"">Merhaba <strong>{{FirstName}}</strong>,</p>

            <p style=""font-size: 16px; line-height: 1.6; margin-bottom: 25px;"">
                {{Content}}
            </p>

            <div style=""background: #fff3cd; border: 1px solid #ffeaa7; border-radius: 8px; padding: 20px; margin: 25px 0; text-align: center;"">
                <div style=""font-size: 32px; margin-bottom: 10px;"">⏰</div>
                <h3 style=""color: #856404; margin: 0 0 10px 0; font-size: 18px;"">Sepetiniz Bekliyor!</h3>
                <p style=""color: #856404; margin: 0; font-size: 14px;"">Stoklar sınırlı, bu fırsatı kaçırmayın!</p>
            </div>

            <!-- CTA Button -->
            <div style=""text-align: center; margin: 30px 0;"">
                <a href=""#"" style=""display: inline-block; background: linear-gradient(135deg, #f39c12 0%, #e67e22 100%); color: white; text-decoration: none; padding: 15px 30px; border-radius: 25px; font-size: 16px; font-weight: bold; box-shadow: 0 4px 15px rgba(243, 156, 18, 0.3); transition: all 0.3s ease;"">
                    🛍️ Alışverişi Tamamla
                </a>
            </div>

            <!-- Support -->
            <div style=""text-align: center; margin: 25px 0;"">
                <p style=""font-size: 14px; color: #6c757d; margin: 0;"">
                    Yardıma mı ihtiyacınız var?
                    <a href=""#"" style=""color: #f39c12; text-decoration: none;"">Bizimle iletişime geçin</a>
                </p>
            </div>
        </div>

        <!-- Footer -->
        <div style=""background: #f8f9fa; padding: 20px; text-align: center; border-top: 1px solid #e9ecef;"">
            <p style=""margin: 0 0 10px 0; font-size: 14px; color: #6c757d;"">
                Bu email {{FullName}} adresine gönderilmiştir.
            </p>
            <p style=""margin: 0; font-size: 12px; color: #adb5bd;"">
                {{companyName}} - Kaliteli alışverişin adresi
            </p>
        </div>
    </div>
</body>
</html>",
                Variables = JsonSerializer.Serialize(new[] { "FirstName", "LastName", "FullName", "Content", "companyName" }),
                ModuleId = null, // Sepet Hatırlatma modülüne bağlı olacak (seed sonrası güncellenecek)
                IsActive = true,
                SortOrder = 7
            }
        };
    }
}
