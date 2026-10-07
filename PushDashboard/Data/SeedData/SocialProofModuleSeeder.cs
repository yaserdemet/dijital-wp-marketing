using Microsoft.EntityFrameworkCore;
using PushDashboard.Models;
using System.Text.Json;

namespace PushDashboard.Data.Seeders;

public static class SocialProofModuleSeeder
{
    public static async Task SeedSocialProofModuleAsync(ApplicationDbContext context)
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

        // Social Proof modülünü kontrol et
        var existingModule = await context.Modules
            .FirstOrDefaultAsync(m => m.Name == "Social Proof");

        if (existingModule == null)
        {
            var defaultSettings = new
            {
                isActive = true,
                viewersMin = 15,
                viewersMax = 45,
                followersMin = 5,
                followersMax = 20,
                buyersMin = 2,
                buyersMax = 8,
                updateInterval = 60,
                displayDuration = 5,
                textTemplates = new
                {
                    viewersTemplate = "{count} kişi şu anda bu ürünü inceliyor",
                    followersTemplate = "{count} kişi bu ürünü takip ediyor",
                    buyersTemplate = "{count} kişi satın almaya hazırlanıyor"
                },
                displaySettings = new
                {
                    position = "bottom-left",
                    primaryColor = "#007bff",
                    textColor = "#ffffff",
                    backgroundColor = "#333333",
                    borderRadius = 8,
                    fontSize = 14,
                    enableAnimation = true,
                    enableShadow = true
                }
            };

            var features = new[]
            {
                "Rastgele sayı üretimi",
                "Özelleştirilebilir mesajlar",
                "Responsive tasarım",
                "Kolay kurulum",
                "Ticimax uyumlu",
                "Performans odaklı"
            };

            var socialProofModule = new Module
            {
                Name = "Social Proof",
                Description = "Ürün sayfalarında sosyal kanıt göstererek satışları artırın",
                DetailedDescription = "Social Proof modülü, ürün sayfalarında \"X kişi şu anda bu ürünü inceliyor\" gibi mesajlar göstererek müşterilerin satın alma motivasyonunu artırır. Rastgele ama mantıklı sayılar kullanarak gerçek veri takibi gerektirmeden etkili sosyal kanıt sunar.",
                Price = 99.99m,
                IconClass = "fas fa-users",
                IconColor = "#6366f1",
                BackgroundColor = "#f8fafc",
                IsActive = true,
                IsNew = true,
                IsFeatured = false,
                CategoryId = category.Id,
                CreatedAt = DateTime.Now,
                Features = JsonSerializer.Serialize(features),
                DefaultSettings = JsonSerializer.Serialize(defaultSettings)
            };

            context.Modules.Add(socialProofModule);
            await context.SaveChangesAsync();

            Console.WriteLine("Social Proof modülü başarıyla eklendi.");
        }
        else
        {
            Console.WriteLine("Social Proof modülü zaten mevcut.");
        }
    }
}
