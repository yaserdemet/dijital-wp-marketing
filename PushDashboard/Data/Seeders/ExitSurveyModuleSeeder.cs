using Microsoft.EntityFrameworkCore;
using PushDashboard.Models;

namespace PushDashboard.Data.Seeders;

public static class ExitSurveyModuleSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // E-ticaret kategorisini bul
        var category = await context.ModuleCategories
            .FirstOrDefaultAsync(c => c.Name == "E-ticaret Araçları");

        if (category == null)
        {
            // Kategori yoksa oluştur
            category = new ModuleCategory
            {
                Name = "E-ticaret Araçları",
                Description = "E-ticaret siteniz için gerekli araçlar",
                IconClass = "fas fa-shopping-cart",
                IsActive = true,
                SortOrder = 3
            };
            context.ModuleCategories.Add(category);
            await context.SaveChangesAsync();
        }

        // Çıkış Anketi modülünün zaten var olup olmadığını kontrol et
        var existingModule = await context.Modules
            .FirstOrDefaultAsync(m => m.Name == "Çıkış Anketi");

        if (existingModule == null)
        {
            var exitSurveyModule = new Module
            {
                Name = "Çıkış Anketi",
                Description = "Kullanıcı deneyimi ve geri bildirim toplama sistemi",
                DetailedDescription = "Ziyaretçileriniz siteyi terk etmeden önce deneyimlerini öğrenin. " +
                                    "Özelleştirilebilir anket soruları ile müşteri memnuniyetini ölçün, " +
                                    "siteyi terk etme nedenlerini anlayın ve hizmet kalitenizi artırın. " +
                                    "Tek seferlik satın alma ile sınırsız kullanım. Detaylı raporlama ve " +
                                    "analiz özellikleri dahil.",
                Price = 85.00m,
                IconClass = "fas fa-poll-h",
                IconColor = "#FF6B6B",
                BackgroundColor = "#FFF5F5",
                IsActive = true,
                IsNew = true,
                IsFeatured = true,
                CategoryId = category.Id,
                CreatedAt = DateTime.Now,
                Features = @"[
                    ""Özelleştirilebilir anket soruları"",
                    ""4 farklı soru tipi (çoktan seçmeli, metin, evet/hayır, puanlama)"",
                    ""Akıllı çıkış algılama (mouse leave, tab kapatma)"",
                    ""Mobil ve masaüstü uyumlu"",
                    ""Detaylı raporlama ve istatistikler"",
                    ""Cevap filtreleme ve analiz"",
                    ""Özelleştirilebilir tasarım"",
                    ""Tek seferlik satın alma"",
                    ""Sınırsız anket ve cevap"",
                    ""GDPR uyumlu veri toplama""
                ]",
                DefaultSettings = @"{
                    ""title"": ""Görüşünüz Bizim İçin Önemli"",
                    ""description"": ""Sitemizi terk etmeden önce deneyiminizi bizimle paylaşır mısınız? Bu sadece birkaç saniye sürecek."",
                    ""submitButtonText"": ""Gönder"",
                    ""cancelButtonText"": ""Kapat"",
                    ""thankYouMessage"": ""Geri bildiriminiz için teşekkür ederiz!"",
                    ""backgroundColor"": ""#ffffff"",
                    ""textColor"": ""#333333"",
                    ""submitButtonColor"": ""#4CAF50"",
                    ""cancelButtonColor"": ""#6c757d"",
                    ""borderRadius"": ""8px"",
                    ""enableAnimation"": true,
                    ""showOnPageExit"": true,
                    ""showOnTabClose"": true,
                    ""delayBeforeShow"": 0,
                    ""showFrequencyDays"": 30,
                    ""showOnMobile"": true,
                    ""showOnDesktop"": true,
                    ""isActive"": true
                }"
            };

            context.Modules.Add(exitSurveyModule);
            await context.SaveChangesAsync();
        }
    }
}
