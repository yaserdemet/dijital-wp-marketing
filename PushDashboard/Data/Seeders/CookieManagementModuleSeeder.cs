using Microsoft.EntityFrameworkCore;
using PushDashboard.Models;

namespace PushDashboard.Data.Seeders;

public static class CookieManagementModuleSeeder
{
    public static async Task SeedCookieManagementModuleAsync(ApplicationDbContext context)
    {
        // E-ticaret Araçları kategorisini kontrol et/oluştur
        var category = await context.ModuleCategories
            .FirstOrDefaultAsync(mc => mc.Name == "E-ticaret Araçları");

        if (category == null)
        {
            category = new ModuleCategory
            {
                Name = "E-ticaret Araçları",
                Description = "E-ticaret sitenizi güçlendiren araçlar",
                IconClass = "fas fa-shopping-cart",
                IsActive = true,
                SortOrder = 3,
                CreatedAt = DateTime.Now
            };

            context.ModuleCategories.Add(category);
            await context.SaveChangesAsync();
        }

        // Çerez Yönetimi modülünü kontrol et
        var existingModule = await context.Modules
            .FirstOrDefaultAsync(m => m.Name == "Çerez Yönetimi");

        if (existingModule == null)
        {
            var cookieManagementModule = new Module
            {
                Name = "Çerez Yönetimi",
                Description = "KVKK ve GDPR uyumlu çerez yönetimi sistemi",
                DetailedDescription = "Web siteniz için profesyonel çerez yönetimi sistemi. KVKK ve GDPR uyumlu, " +
                                    "tamamen özelleştirilebilir tasarım, 4 farklı çerez kategorisi yönetimi. " +
                                    "Tek seferlik satın alma ile sınırsız kullanım. Statik script oluşturur, " +
                                    "sürekli API çağrısı gerektirmez.",
                Price = 75.00m,
                IconClass = "fas fa-cookie-bite",
                IconColor = "#8B4513",
                BackgroundColor = "#FFF8DC",
                IsActive = true,
                IsNew = true,
                IsFeatured = true,
                CategoryId = category.Id,
                CreatedAt = DateTime.Now,
                Features = @"[
                    ""KVKK ve GDPR uyumlu"",
                    ""4 çerez kategorisi yönetimi"",
                    ""Tamamen özelleştirilebilir tasarım"",
                    ""Statik script oluşturma"",
                    ""Responsive tasarım"",
                    ""Türkçe dil desteği"",
                    ""LocalStorage ile tercih saklama"",
                    ""Animasyon efektleri"",
                    ""Çoklu pozisyon desteği"",
                    ""Tek seferlik satın alma""
                ]",
                DefaultSettings = @"{
                    ""bannerTitle"": ""Bu site çerezleri kullanır"",
                    ""bannerDescription"": ""Web sitemizde size en iyi deneyimi sunabilmek için çerezleri kullanıyoruz. Çerez kullanımını kabul ederek daha iyi bir deneyim yaşayabilirsiniz."",
                    ""acceptButtonText"": ""Tümünü Kabul Et"",
                    ""rejectButtonText"": ""Tümünü Reddet"",
                    ""settingsButtonText"": ""Ayarları Yönet"",
                    ""saveButtonText"": ""Seçimleri Kaydet"",
                    ""bannerPosition"": ""bottom"",
                    ""bannerBackgroundColor"": ""#ffffff"",
                    ""bannerTextColor"": ""#333333"",
                    ""acceptButtonColor"": ""#4CAF50"",
                    ""rejectButtonColor"": ""#f44336"",
                    ""settingsButtonColor"": ""#2196F3"",
                    ""borderRadius"": ""8px"",
                    ""showSettingsButton"": true,
                    ""enableAnimation"": true,
                    ""isActive"": true,
                    ""cookieExpiryDays"": 365
                }"
            };

            context.Modules.Add(cookieManagementModule);
            await context.SaveChangesAsync();
        }
    }
}
