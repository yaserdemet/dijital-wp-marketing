using Microsoft.EntityFrameworkCore;
using PushDashboard.Models;

namespace PushDashboard.Data.Seeders;

public static class NewsletterPopupModuleSeeder
{
    public static async Task SeedNewsletterPopupModuleAsync(ApplicationDbContext context)
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

        // E-Bülten modülünü kontrol et
        var existingModule = await context.Modules
            .FirstOrDefaultAsync(m => m.Name == "E-Bülten");

        if (existingModule == null)
        {
            var newsletterModule = new Module
            {
                Name = "E-Bülten",
                Description = "E-ticaret siteniz için profesyonel e-bülten popup sistemi",
                DetailedDescription = "Ziyaretçilerinizi e-bülten abonelerine dönüştürün. Tamamen özelleştirilebilir tasarım, " +
                                    "akıllı zamanlama ile sayfa açıldıktan sonra belirlenen sürede popup gösterilir. " +
                                    "Kullanıcı formu doldurduysa bir daha göstermez, doldurmadıysa belirlenen gün aralığında tekrar gösterir. " +
                                    "Statik script oluşturur, sürekli API çağrısı gerektirmez.",
                Price = 50.00m,
                IconClass = "fas fa-envelope-open-text",
                IconColor = "#e53935",
                BackgroundColor = "#FFEBEE",
                IsActive = true,
                IsNew = true,
                IsFeatured = false,
                CategoryId = category.Id,
                CreatedAt = DateTime.Now,
                Features = @"[
                    ""Tamamen özelleştirilebilir popup tasarımı"",
                    ""Ad, soyad ve telefon alanları"",
                    ""KVKK uyumlu onay checkbox'ı"",
                    ""Akıllı zamanlama (sayfa açıldıktan X saniye sonra)"",
                    ""Frekans kontrolü (X günde bir tekrar)"",
                    ""Form gönderen kullanıcıya bir daha göstermeme"",
                    ""Responsive tasarım (mobil/desktop)"",
                    ""Animasyon efektleri"",
                    ""İstatistik takibi (gösterim, abone, kapatma)"",
                    ""Statik script - hızlı yüklenme""
                ]",
                DefaultSettings = @"{
                    ""title"": ""Bültenimize Abone Olun, Fırsatları İlk Siz Yakalayın!"",
                    ""description"": ""İndirim ve fırsatlardan ilk sizin haberiniz olsun, kayıt olun ve avantajlardan yararlanın!"",
                    ""submitButtonText"": ""Kaydet"",
                    ""backgroundColor"": ""#ffffff"",
                    ""textColor"": ""#333333"",
                    ""buttonColor"": ""#e53935"",
                    ""buttonTextColor"": ""#ffffff"",
                    ""borderRadius"": ""12px"",
                    ""delaySeconds"": 5,
                    ""showFrequencyDays"": 30,
                    ""isActive"": true
                }"
            };

            context.Modules.Add(newsletterModule);
            await context.SaveChangesAsync();
        }
    }
}
