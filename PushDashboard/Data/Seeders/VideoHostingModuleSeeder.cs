using Microsoft.EntityFrameworkCore;
using PushDashboard.Models;
using System.Text.Json;

namespace PushDashboard.Data.Seeders;

public static class VideoHostingModuleSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        // Medya ve İçerik kategorisini kontrol et/oluştur
        var category = await context.ModuleCategories
            .FirstOrDefaultAsync(mc => mc.Name == "Medya ve İçerik");

        if (category == null)
        {
            category = new ModuleCategory
            {
                Name = "Medya ve İçerik",
                Description = "Video, resim ve içerik yönetimi araçları",
                IconClass = "fas fa-photo-video",
                IsActive = true,
                SortOrder = 4,
                CreatedAt = DateTime.Now
            };

            context.ModuleCategories.Add(category);
            await context.SaveChangesAsync();
        }

        // Video Hosting modülünün zaten var olup olmadığını kontrol et
        var existingModule = await context.Modules
            .FirstOrDefaultAsync(m => m.Name == "Video Hosting");

        if (existingModule == null)
        {
            // Default settings for Video Hosting module
            var defaultSettings = new
            {
                costPerMinute = 0.5m,
                storageCostPerGBPerMonth = 0.1m,
                maxFileSizeBytes = 500 * 1024 * 1024, // 500MB
                maxDurationMinutes = 60,
                maxVideosPerCompany = 100,
                autoPlay = false,
                showControls = true,
                allowDownload = false,
                playerTheme = "default",
                customPlayerCss = "",
                requireAuthentication = false,
                allowEmbedding = true,
                allowedDomains = ""
            };

            var features = new[]
            {
                "S3 bulut depolama",
                "Video yükleme ve işleme",
                "Otomatik thumbnail oluşturma",
                "Özelleştirilebilir video player",
                "Iframe embed desteği",
                "Video analitikleri",
                "Görüntüleme istatistikleri",
                "Erişim kontrolü",
                "Domain kısıtlaması",
                "Responsive tasarım",
                "Çoklu video formatı desteği",
                "Video uzunluğuna göre ücretlendirme",
                "Aylık depolama maliyeti",
                "Güvenli video paylaşımı",
                "Özel CSS desteği"
            };

            var videoHostingModule = new Module
            {
                Name = "Video Hosting",
                Description = "Profesyonel video barındırma ve paylaşım sistemi",
                DetailedDescription = "Video Hosting modülü ile videolarınızı güvenli bir şekilde bulutta saklayın ve paylaşın. " +
                                    "S3 bulut depolama teknolojisi ile yüksek performanslı video streaming, otomatik thumbnail oluşturma, " +
                                    "özelleştirilebilir video player ve detaylı analitik raporları. Iframe ile web sitenize kolayca gömebilir, " +
                                    "erişim kontrolü ile güvenliği sağlayabilirsiniz. Video uzunluğuna göre esnek ücretlendirme modeli.",
                Price = 149.99m,
                IconClass = "fas fa-video",
                IconColor = "#e74c3c",
                BackgroundColor = "#fdf2f2",
                IsActive = true,
                IsNew = true,
                IsFeatured = true,
                CategoryId = category.Id,
                CreatedAt = DateTime.Now,
                Features = JsonSerializer.Serialize(features),
                DefaultSettings = JsonSerializer.Serialize(defaultSettings)
            };

            context.Modules.Add(videoHostingModule);
            await context.SaveChangesAsync();

            Console.WriteLine("Video Hosting modülü başarıyla eklendi.");
        }
        else
        {
            Console.WriteLine("Video Hosting modülü zaten mevcut.");
        }
    }
}
