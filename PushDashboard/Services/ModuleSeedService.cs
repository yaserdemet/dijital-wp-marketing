using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using System.Text.Json;

namespace PushDashboard.Services;

public interface IModuleSeedService
{
    Task SeedAsync();
}

public class ModuleSeedService : IModuleSeedService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ModuleSeedService> _logger;

    public ModuleSeedService(ApplicationDbContext context, ILogger<ModuleSeedService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        // Seed categories first
        await SeedCategoriesAsync();

        // Then seed modules (including new ones)
        await SeedModulesAsync();

        // Seed module packages (for Yorum Taşıma etc.)
        await SeedModulePackagesAsync();

        // Finally seed test purchases
        await SeedTestPurchasesAsync();

        _logger.LogInformation("Module seed process completed.");
    }

    private async Task SeedCategoriesAsync()
    {
        // Define all categories that should exist
        var categoriesToSeed = new List<ModuleCategory>
        {
            new ModuleCategory
            {
                Name = "E-ticaret",
                Description = "Online satış ve e-ticaret modülleri",
                IconClass = "shopping-bag",
                IsActive = true,
                SortOrder = 1
            },
            new ModuleCategory
            {
                Name = "İletişim",
                Description = "Müşteri iletişimi ve bildirim modülleri",
                IconClass = "message-circle",
                IsActive = true,
                SortOrder = 2
            },
            new ModuleCategory
            {
                Name = "Analitik",
                Description = "Veri analizi ve raporlama modülleri",
                IconClass = "bar-chart",
                IsActive = true,
                SortOrder = 3
            },
            new ModuleCategory
            {
                Name = "Otomasyon",
                Description = "İş süreçleri otomasyon modülleri",
                IconClass = "zap",
                IsActive = true,
                SortOrder = 4
            },
            new ModuleCategory
            {
                Name = "Medya ve İçerik",
                Description = "Video, resim ve içerik yönetimi araçları",
                IconClass = "fas fa-photo-video",
                IsActive = true,
                SortOrder = 5
            }
        };

        // Check each category individually and add missing ones
        var categoriesToAdd = new List<ModuleCategory>();

        foreach (var categoryToSeed in categoriesToSeed)
        {
            var existingCategory = await _context.ModuleCategories
                .FirstOrDefaultAsync(c => c.Name == categoryToSeed.Name);

            if (existingCategory == null)
            {
                categoriesToAdd.Add(categoryToSeed);
                _logger.LogInformation($"Category '{categoryToSeed.Name}' not found, will be added.");
            }
            else
            {
                _logger.LogInformation($"Category '{categoryToSeed.Name}' already exists, skipping.");
            }
        }

        // Add missing categories
        if (categoriesToAdd.Any())
        {
            _context.ModuleCategories.AddRange(categoriesToAdd);
            await _context.SaveChangesAsync();
            _logger.LogInformation($"Added {categoriesToAdd.Count} new module categories.");
        }
        else
        {
            _logger.LogInformation("All module categories already exist, no new categories added.");
        }
    }

    private async Task SeedModulesAsync()
    {
        // Get category references
        var ecommerceCategory = await _context.ModuleCategories.FirstAsync(c => c.Name == "E-ticaret");
        var communicationCategory = await _context.ModuleCategories.FirstAsync(c => c.Name == "İletişim");
        var analyticsCategory = await _context.ModuleCategories.FirstAsync(c => c.Name == "Analitik");
        var automationCategory = await _context.ModuleCategories.FirstAsync(c => c.Name == "Otomasyon");
        var mediaCategory = await _context.ModuleCategories.FirstAsync(c => c.Name == "Medya ve İçerik");

        // Define all modules
        var modulesToSeed = new List<Module>
        {
            // İlk Alışveriş Mesajı modülü
            new Module
            {
                Name = "İlk Alışveriş Mesajı",
                Description = "İlk alışveriş yapan müşterilere özel hoş geldin mesajı ve hediye çeki gönderir",
                DetailedDescription = "Bu modül, e-ticaret sitenizde ilk kez alışveriş yapan müşterilere otomatik olarak hoş geldin mesajı gönderir. " +
                                    "Müşteri sadakatini artırmak için hediye çeki de oluşturabilir. Webhook sistemi sayesinde sipariş oluştuğu anda " +
                                    "müşterinin geçmiş siparişleri kontrol edilir ve ilk alışverişse bildirim gönderilir.",
                Price = 50.00m,
                IconClass = "fas fa-gift",
                IconColor = "#e74c3c",
                BackgroundColor = "#fdf2f2",
                IsActive = true,
                IsNew = true,
                IsFeatured = true,
                CategoryId = communicationCategory.Id,
                CreatedAt = DateTime.Now,
                Features = JsonSerializer.Serialize(new[]
                {
                    "İlk alışveriş tespiti",
                    "Otomatik hoş geldin mesajı",
                    "Hediye çeki oluşturma",
                    "Çoklu iletişim kanalı",
                    "Webhook entegrasyonu"
                }),
                DefaultSettings = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["enabled"] = true,
                    ["sendWelcomeMessage"] = true,
                    ["createGiftVoucher"] = false,
                    ["giftVoucherAmount"] = 25.0,
                    ["giftVoucherValidityDays"] = 30,
                    ["emailTemplate"] = "Hoş geldiniz! İlk alışverişiniz için teşekkürler.",
                    ["smsTemplate"] = "Hoş geldiniz! İlk alışverişiniz için teşekkürler.",
                    ["whatsappTemplate"] = "Hoş geldiniz! İlk alışverişiniz için teşekkürler."
                })
            },
            // Yorum Taşıma modülü
            new Module
            {
                Name = "Yorum Taşıma",
                Description = "E-ticaret sitelerinden yorum çekme ve aktarma",
                DetailedDescription = "Trendyol, Hepsiburada gibi e-ticaret sitelerinden ürün yorumlarını çekerek kendi sitenize aktarın. Müşteri güvenini artırın ve satışlarınızı yükseltin.",
                Price = 199.00m,
                IconClass = "fas fa-comment",
                IconColor = "text-purple-600",
                BackgroundColor = "bg-purple-100",
                CategoryId = ecommerceCategory.Id,
                IsActive = true,
                IsFeatured = true,
                HasQuotaSystem = true,
                HasPackages = true,
                Features = JsonSerializer.Serialize(new[]
                {
                    "Trendyol yorum çekme",
                    "Hepsiburada yorum çekme",
                    "Toplu yorum aktarımı",
                    "HTML export özelliği",
                    "Otomatik işleme sistemi"
                }),
                DefaultSettings = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["enabled"] = true,
                    ["pricePerComment"] = 0.10m,
                    ["maxCommentsPerRequest"] = 1000,
                    ["autoProcess"] = true
                })
            },
            // Doğum Günü Hatırlatma modülü
            new Module
            {
                Name = "Doğum Günü Hatırlatma",
                Description = "Otomatik doğum günü kutlama sistemi",
                DetailedDescription = "Müşterilerinizin doğum günlerini takip edin ve otomatik kutlama mesajları gönderin. Müşteri memnuniyetini artırın ve sadakati güçlendirin.",
                Price = 199.00m,
                IconClass = "fas fa-birthday-cake",
                IconColor = "text-pink-600",
                BackgroundColor = "bg-pink-100",
                CategoryId = communicationCategory.Id,
                IsActive = true,
                IsFeatured = true,
                IsNew = true,
                Features = JsonSerializer.Serialize(new[]
                {
                    "Otomatik doğum günü takibi",
                    "Çoklu iletişim kanalı desteği",
                    "Özelleştirilebilir mesaj şablonları",
                    "Email, SMS, WhatsApp desteği",
                    "Detaylı gönderim raporları",
                }),
                DefaultSettings = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["enabled"] = true,
                    ["autoSend"] = false,
                    ["emailCost"] = 0.50m, // Email başına maliyet (TL)
                    ["smsCost"] = 2.00m,    // SMS başına maliyet (TL)
                    ["whatsappCost"] = 1.00m, // WhatsApp başına maliyet (TL)
                    ["sendTime"] = "09:00", // Gönderim saati
                    ["enableEmail"] = true,
                    ["enableSms"] = false,
                    ["enableWhatsapp"] = false,
                    // Hediye çeki ayarları
                    ["enableGiftVoucher"] = false, // Hediye çeki özelliği
                    ["giftVoucherAmount"] = 50.0, // Hediye çeki tutarı (TL)
                    ["giftVoucherDiscountType"] = 1, // 1: Sabit tutar, 2: Yüzde
                    ["giftVoucherValidityDays"] = 365, // Geçerlilik süresi (gün)
                    ["giftVoucherDescription"] = "Doğum Günü Hediye Çeki", // Açıklama
                })
            },

            // Toplu Mesaj Gönderimi modülü
            new Module
            {
                Name = "Toplu Mesaj Gönderimi",
                Description = "Müşterilerinize aktif iletişim kanallarından toplu mesaj gönderin",
                DetailedDescription = "Bu modül ile müşteri listenizden seçtiğiniz müşterilere aktif iletişim kanallarınız üzerinden " +
                                    "toplu mesaj gönderebilirsiniz. Email ve WhatsApp kanallarını destekler. Müşteri filtreleme, " +
                                    "şablon seçimi, önizleme ve maliyet takibi özellikleri ile güvenli ve etkili toplu gönderim yapabilirsiniz.",
                Price = 500.00m,
                IconClass = "fas fa-paper-plane",
                IconColor = "#3498db",
                BackgroundColor = "#f0f8ff",
                IsActive = true,
                IsNew = true,
                IsFeatured = true,
                CategoryId = communicationCategory.Id,
                CreatedAt = DateTime.Now,
                Features = JsonSerializer.Serialize(new[]
                {
                    "Çoklu iletişim kanalı desteği",
                    "Müşteri filtreleme",
                    "Şablon seçimi",
                    "Önizleme özelliği",
                    "Toplu gönderim",
                    "İlerleme takibi",
                    "Maliyet hesaplama",
                    "Background processing"
                }),
                DefaultSettings = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["enabled"] = true,
                    ["enabledChannels"] = new string[] { }, // Başlangıçta hiçbir kanal aktif değil
                    ["emailCost"] = 0.50m, // Email başına maliyet (TL)
                    ["whatsappCost"] = 1.00m, // WhatsApp başına maliyet (TL)
                    ["maxRecipientsPerBatch"] = 50, // Batch başına maksimum alıcı sayısı
                    ["batchDelaySeconds"] = 5, // Batch'ler arası bekleme süresi (saniye)
                    ["enablePreview"] = true, // Önizleme özelliği
                    ["requireConfirmation"] = true // Gönderim öncesi onay gerekli
                })
            },

            // Sepet Hatırlatma modülü
            new Module
            {
                Name = "Sepet Hatırlatma",
                Description = "Terk edilmiş sepetler için otomatik hatırlatma sistemi",
                DetailedDescription = "Bu modül, e-ticaret sitenizde terk edilmiş sepetleri tespit ederek müşterilere otomatik hatırlatma mesajları gönderir. " +
                                    "Farklı zaman aralıklarında (6 saat, 1 gün, 1 hafta) hatırlatma zamanlamaları oluşturabilir, " +
                                    "kişiselleştirilmiş mesajlar gönderebilir ve müşteri başına maksimum bildirim limiti belirleyebilirsiniz. " +
                                    "E-ticaret entegrasyonu gerektirir ve sepet senkronizasyonu aktif olmalıdır.",
                Price = 299.00m,
                IconClass = "fas fa-shopping-cart",
                IconColor = "#f39c12",
                BackgroundColor = "#fef9e7",
                IsActive = true,
                IsNew = true,
                IsFeatured = true,
                CategoryId = ecommerceCategory.Id,
                CreatedAt = DateTime.Now,
                Features = JsonSerializer.Serialize(new[]
                {
                    "Otomatik sepet takibi",
                    "Çoklu zamanlama seçenekleri (6 saat, 1 gün, 1 hafta)",
                    "Kişiselleştirilmiş mesaj içeriği",
                    "Müşteri bildirim limiti kontrolü",
                    "E-ticaret platform entegrasyonu",
                    "Sepet doğrulama sistemi",
                    "Detaylı gönderim logları",
                    "İstatistik ve raporlama",
                    "Test hatırlatması özelliği"
                }),
                DefaultSettings = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["isEnabled"] = true,
                    ["maxNotificationsPerCustomer"] = 2,
                    ["notificationContent"] = "Merhaba {FirstName}, sepetinizde ürünler bekliyor! Alışverişinizi tamamlamak için tıklayın.",
                    ["defaultSchedules"] = new[]
                    {
                        new { name = "6 Saatlik Hatırlatma", hours = 6, isActive = true },
                        new { name = "1 Günlük Hatırlatma", hours = 24, isActive = false },
                        new { name = "1 Haftalık Hatırlatma", hours = 168, isActive = false }
                    }
                })
            },

            // Stok Hatırlatma modülü
            new Module
            {
                Name = "Stok Hatırlatma",
                Description = "Düşük stok seviyesinde müşterilere otomatik bildirim sistemi",
                DetailedDescription = "Bu modül, e-ticaret sitenizde ürün stoğu belirlenen eşiğin altına düştüğünde müşterilere otomatik bildirim gönderir. " +
                                    "E-posta, SMS, WhatsApp ve Push bildirimi kanallarını destekler. Stok eşiği ve bildirim mesajını özelleştirebilirsiniz.",
                Price = 199.00m,
                IconClass = "fas fa-boxes",
                IconColor = "#e74c3c",
                BackgroundColor = "#fdf2f2",
                IsActive = true,
                IsNew = true,
                IsFeatured = true,
                CategoryId = ecommerceCategory.Id,
                CreatedAt = DateTime.Now,
                Features = JsonSerializer.Serialize(new[]
                {
                    "Stok seviyesi takibi",
                    "Özelleştirilebilir stok eşiği",
                    "Çoklu iletişim kanalı desteği",
                    "E-posta bildirimi",
                    "SMS bildirimi",
                    "WhatsApp bildirimi",
                    "Push bildirimi",
                    "Kişiselleştirilmiş mesaj şablonu",
                    "Detaylı gönderim logları",
                    "İstatistik ve raporlama"
                }),
                DefaultSettings = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["enabled"] = true,
                    ["emailEnabled"] = true,
                    ["smsEnabled"] = false,
                    ["whatsappEnabled"] = false,
                    ["pushEnabled"] = false,
                    ["stockThreshold"] = 10,
                    ["notificationMessage"] = "Merhaba {CustomerName}, {ProductName} ürününün stoğu azalıyor! Son {StockCount} adet kaldı. Hemen sipariş verin!"
                })
            },

            // Sipariş Durumu Bildirimleri modülü
            new Module
            {
                Name = "Sipariş Durumu Bildirimleri",
                Description = "Sipariş durumu değişikliklerinde otomatik müşteri bildirimleri",
                DetailedDescription = "Bu modül ile sipariş durumu değiştiğinde müşterilerinize otomatik bildirim gönderebilirsiniz. " +
                                    "E-posta, SMS ve WhatsApp kanallarını destekler. Her sipariş durumu için ayrı şablon ve gecikme " +
                                    "ayarları yapabilir, webhook entegrasyonu ile gerçek zamanlı bildirimler gönderebilirsiniz.",
                Price = 299.00m,
                IconClass = "fas fa-clipboard-check",
                IconColor = "#27ae60",
                BackgroundColor = "#f0fff4",
                IsActive = true,
                IsNew = true,
                IsFeatured = true,
                CategoryId = automationCategory.Id,
                CreatedAt = DateTime.Now,
                Features = JsonSerializer.Serialize(new[]
                {
                    "7 farklı sipariş durumu desteği",
                    "Çoklu iletişim kanalı (Email/SMS/WhatsApp)",
                    "Özelleştirilebilir şablonlar",
                    "Gecikme ayarları",
                    "Webhook entegrasyonu",
                    "Gerçek zamanlı bildirimler",
                    "Bildirim geçmişi",
                    "İstatistik ve raporlama"
                }),
                DefaultSettings = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["enabled"] = true,
                    ["emailNotificationEnabled"] = true,
                    ["smsNotificationEnabled"] = false,
                    ["whatsappNotificationEnabled"] = false,
                    ["emailCost"] = 0.50m, // Email başına maliyet (TL)
                    ["smsCost"] = 2.00m,   // SMS başına maliyet (TL)
                    ["whatsappCost"] = 1.00m, // WhatsApp başına maliyet (TL)
                    ["defaultDelayMinutes"] = 0, // Varsayılan gecikme süresi
                    ["webhookEnabled"] = true, // Webhook entegrasyonu
                    ["trackingEnabled"] = true, // Bildirim takibi
                    ["statisticsEnabled"] = true // İstatistik toplama
                })
            },

            // Hediye Çarkı modülü
            new Module
            {
                Name = "Hediye Çarkı",
                Description = "Müşterileriniz için etkileşimli hediye çarkı oluşturun ve otomatik hediye çeki gönderin",
                DetailedDescription = "Bu modül, e-ticaret sitenize entegre edilebilen etkileşimli bir hediye çarkı oluşturur. " +
                                    "Müşteriler çarkı çevirerek hediye çeki kazanabilir ve anında WhatsApp bildirimi alabilir. " +
                                    "Çark tasarımını özelleştirebilir, ödül olasılıklarını ayarlayabilir ve müşteri etkileşimini artırabilirsiniz.",
                Price = 75.00m,
                IconClass = "fas fa-dharmachakra",
                IconColor = "#f59e0b",
                BackgroundColor = "#fef3c7",
                IsActive = true,
                IsNew = true,
                IsFeatured = true,
                CategoryId = communicationCategory.Id,
                CreatedAt = DateTime.Now,
                Features = JsonSerializer.Serialize(new[]
                {
                    "Etkileşimli hediye çarkı",
                    "Otomatik hediye çeki oluşturma",
                    "WhatsApp bildirimi",
                    "Özelleştirilebilir tasarım",
                    "Ödül olasılık ayarları",
                    "Detaylı istatistikler",
                    "JavaScript embed kodu",
                    "Mobil uyumlu tasarım"
                }),
                DefaultSettings = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["enabled"] = true,
                    ["voucherCreationCost"] = 2.00m, // Hediye çeki oluşturma maliyeti
                    ["notificationCost"] = 1.00m, // WhatsApp bildirimi maliyeti
                    ["maxSpinsPerHour"] = 100, // Saatlik maksimum çevirme sayısı
                    ["enableRateLimiting"] = true, // Hız sınırlaması
                    ["defaultWheelTitle"] = "Çarkı Çevir, Hediyeni Kazan!",
                    ["defaultButtonText"] = "Çarkı Çevir",
                    ["defaultNotificationTemplate"] = "🎉 Tebrikler {name}! {prize} kazandınız. Hemen alışverişe başlayın: {siteUrl}",
                    ["requirePhone"] = true,
                    ["requireEmail"] = false,
                    ["maxSpinsPerDay"] = 1,
                    ["showConfetti"] = true
                })
            },

            // Video Hosting modülü
            new Module
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
                CategoryId = mediaCategory.Id,
                CreatedAt = DateTime.Now,
                Features = JsonSerializer.Serialize(new[]
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
                }),
                DefaultSettings = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["costPerMinute"] = 0.5m,
                    ["storageCostPerGBPerMonth"] = 0.1m,
                    ["maxFileSizeBytes"] = 500 * 1024 * 1024, // 500MB
                    ["maxDurationMinutes"] = 60,
                    ["maxVideosPerCompany"] = 100,
                    ["autoPlay"] = false,
                    ["showControls"] = true,
                    ["allowDownload"] = false,
                    ["playerTheme"] = "default",
                    ["customPlayerCss"] = "",
                    ["requireAuthentication"] = false,
                    ["allowEmbedding"] = true,
                    ["allowedDomains"] = ""
                })
            },

            // Ürün Slider modülü
            new Module
            {
                Name = "Ürün Slider",
                Description = "E-ticaret sitenize entegre edilebilen dinamik ürün slider'ları oluşturun",
                DetailedDescription = "Ürün Slider modülü ile e-ticaret sitenize profesyonel ürün slider'ları ekleyebilirsiniz. " +
                                    "Slider ve tab formatlarında görüntüleme seçenekleri, özelleştirilebilir tasarım, " +
                                    "responsive yapı ve kolay entegrasyon. Admin paneli üzerinden ürünlerinizi yönetin, " +
                                    "sıralama ve görünüm ayarlarını yapın. JavaScript embed kodu ile herhangi bir web sitesine kolayca eklenebilir.",
                Price = 249.00m,
                IconClass = "fas fa-images",
                IconColor = "#8b5cf6",
                BackgroundColor = "#f3f4f6",
                IsActive = true,
                IsNew = true,
                IsFeatured = true,
                CategoryId = ecommerceCategory.Id,
                CreatedAt = DateTime.Now,
                Features = JsonSerializer.Serialize(new[]
                {
                    "Slider ve tab formatları",
                    "Özelleştirilebilir tasarım",
                    "Responsive yapı",
                    "Admin panel yönetimi",
                    "JavaScript embed kodu",
                    "Manuel entegrasyon",
                    "Ürün sıralama",
                    "Otomatik oynatma",
                    "Görsel özelleştirme",
                    "Mobil uyumlu",
                    "Performans analitikleri",
                    "Çoklu slider desteği"
                }),
                DefaultSettings = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["enabled"] = true,
                    ["displayType"] = "slider", // slider, tabs, grid
                    ["autoPlay"] = false,
                    ["autoPlayInterval"] = 3000,
                    ["showArrows"] = true,
                    ["showDots"] = true,
                    ["itemsPerView"] = 4,
                    ["itemsPerViewMobile"] = 2,
                    ["itemsPerViewTablet"] = 3,
                    ["theme"] = "default",
                    ["primaryColor"] = "#007bff",
                    ["secondaryColor"] = "#6c757d",
                    ["backgroundColor"] = "#ffffff",
                    ["textColor"] = "#333333",
                    ["enableAnimations"] = true,
                    ["transitionDuration"] = 300,
                    ["animationType"] = "slide",
                    ["showProductPrice"] = true,
                    ["showProductDescription"] = true,
                    ["showProductImage"] = true,
                    ["imageAspectRatio"] = "1:1",
                    ["customCSS"] = ""
                })
            },

            // Favori Ürünler modülü
            new Module
            {
                Name = "Favori Ürünler",
                Description = "Müşterilerinizin favorilerine eklediği ürünleri takip edin ve analiz edin",
                DetailedDescription = "Bu modül, e-ticaret sitenizde müşterilerinizin favorilerine eklediği ürünleri görüntülemenizi sağlar. " +
                                    "Hangi ürünlerin en çok favorilere eklendiğini, hangi müşterilerin hangi ürünleri favorilediğini takip edebilirsiniz. " +
                                    "Favori ürün istatistikleri ile müşteri davranışlarını analiz edin, stok ve kampanya planlaması yapın. " +
                                    "E-ticaret entegrasyonu gerektirir.",
                Price = 149.00m,
                IconClass = "fas fa-heart",
                IconColor = "#e11d48",
                BackgroundColor = "#fff1f2",
                IsActive = true,
                IsNew = true,
                IsFeatured = true,
                CategoryId = analyticsCategory.Id,
                CreatedAt = DateTime.Now,
                Features = JsonSerializer.Serialize(new[]
                {
                    "Favori ürün listesi görüntüleme",
                    "Ürün bazında favori sayısı takibi",
                    "Müşteri bazında favori analizi",
                    "En popüler ürünler raporu",
                    "Tarih aralığına göre filtreleme",
                    "Ürün arama ve filtreleme",
                    "Favori ekleme tarih takibi",
                    "Detaylı istatistikler",
                    "E-ticaret platform entegrasyonu",
                    "Gerçek zamanlı veri güncelleme"
                }),
                DefaultSettings = JsonSerializer.Serialize(new Dictionary<string, object>
                {
                    ["enabled"] = true,
                    ["pageSize"] = 20,
                    ["showStatistics"] = true,
                    ["enableDateFilter"] = true,
                    ["enableSearch"] = true
                })
            }
        };

        // Check each module individually and add if not exists
        var addedModules = new List<Module>();

        foreach (var moduleToSeed in modulesToSeed)
        {
            var existingModule = await _context.Modules
                .FirstOrDefaultAsync(m => m.Name == moduleToSeed.Name);

            if (existingModule == null)
            {
                _context.Modules.Add(moduleToSeed);
                addedModules.Add(moduleToSeed);
                _logger.LogInformation("Adding new module: {ModuleName}", moduleToSeed.Name);
            }
            else
            {
                _logger.LogInformation("Module already exists: {ModuleName}", moduleToSeed.Name);
            }
        }

        if (addedModules.Any())
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully added {Count} new modules.", addedModules.Count);
        }
        else
        {
            _logger.LogInformation("No new modules to add.");
        }
    }

    private async Task SeedModulePackagesAsync()
    {
        var commentModule = await _context.Modules.FirstOrDefaultAsync(m => m.Name == "Yorum Taşıma");
        if (commentModule == null)
            return;

        // Ensure HasPackages and HasQuotaSystem flags are set on existing module records
        if (!commentModule.HasPackages || !commentModule.HasQuotaSystem)
        {
            commentModule.HasPackages = true;
            commentModule.HasQuotaSystem = true;
            _context.Modules.Update(commentModule);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated Yorum Taşıma module to HasPackages=true and HasQuotaSystem=true.");
        }

        var existingPackagesCount = await _context.ModulePackages.CountAsync(p => p.ModuleId == commentModule.Id);
        if (existingPackagesCount > 0)
        {
            _logger.LogInformation("Packages for Yorum Taşıma already exist, skipping package seed.");
            return;
        }

        var packagesToSeed = new List<ModulePackage>
        {
            new ModulePackage
            {
                ModuleId = commentModule.Id,
                Name = "Başlangıç Paketi",
                QuotaAmount = 5000,
                MonthlyPrice = 149.00m,
                YearlyPrice = 1290.00m,
                HtmlContent = "<ul><li><i class=\"fas fa-check text-success\"></i> 5.000 Yorum Taşıma</li><li><i class=\"fas fa-check text-success\"></i> Tüm Pazaryerleri</li><li><i class=\"fas fa-check text-success\"></i> Hızlı Aktarım</li></ul>",
                IsActive = true
            },
            new ModulePackage
            {
                ModuleId = commentModule.Id,
                Name = "Pro Paket",
                QuotaAmount = 15000,
                MonthlyPrice = 349.00m,
                YearlyPrice = 2990.00m,
                HtmlContent = "<ul><li><i class=\"fas fa-check text-success\"></i> 15.000 Yorum Taşıma</li><li><i class=\"fas fa-check text-success\"></i> Tüm Pazaryerleri</li><li><i class=\"fas fa-check text-success\"></i> Öncelikli İşlem</li></ul>",
                IsActive = true
            },
            new ModulePackage
            {
                ModuleId = commentModule.Id,
                Name = "Kurumsal Paket",
                QuotaAmount = 50000,
                MonthlyPrice = 999.00m,
                YearlyPrice = 8490.00m,
                HtmlContent = "<ul><li><i class=\"fas fa-check text-success\"></i> 50.000 Yorum Taşıma</li><li><i class=\"fas fa-check text-success\"></i> Tüm Pazaryerleri</li><li><i class=\"fas fa-check text-success\"></i> VIP Destek</li></ul>",
                IsActive = true
            }
        };

        _context.ModulePackages.AddRange(packagesToSeed);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Added {Count} new packages for Yorum Taşıma.", packagesToSeed.Count);

        // ── Video Hosting packages (MB-based quota) ──────────────────────────
        await SeedVideoHostingPackagesAsync();
    }

    private async Task SeedVideoHostingPackagesAsync()
    {
        var videoModule = await _context.Modules.FirstOrDefaultAsync(m => m.Name == "Video Hosting");
        if (videoModule == null)
        {
            _logger.LogWarning("Video Hosting module not found, skipping package seed.");
            return;
        }

        // Ensure flags are set
        if (!videoModule.HasPackages || !videoModule.HasQuotaSystem)
        {
            videoModule.HasPackages = true;
            videoModule.HasQuotaSystem = true;
            _context.Modules.Update(videoModule);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated Video Hosting module to HasPackages=true and HasQuotaSystem=true.");
        }

        var existingCount = await _context.ModulePackages.CountAsync(p => p.ModuleId == videoModule.Id);
        if (existingCount > 0)
        {
            _logger.LogInformation("Packages for Video Hosting already exist, skipping package seed.");
            return;
        }

        var videoPackages = new List<ModulePackage>
        {
            new ModulePackage
            {
                ModuleId = videoModule.Id,
                Name = "Başlangıç Paketi",
                QuotaAmount = 10_000,        // 10 GB → MB cinsinden
                MonthlyPrice = 199.00m,
                YearlyPrice  = 1_790.00m,
                HtmlContent  = "<ul>" +
                               "<li><i class=\"fas fa-check text-success\"></i> 10 GB Depolama Alanı</li>" +
                               "<li><i class=\"fas fa-check text-success\"></i> Sınırsız Görüntüleme</li>" +
                               "<li><i class=\"fas fa-check text-success\"></i> Video Embed Desteği</li>" +
                               "</ul>",
                IsActive = true
            },
            new ModulePackage
            {
                ModuleId = videoModule.Id,
                Name = "Pro Paket",
                QuotaAmount = 50_000,        // 50 GB → MB cinsinden
                MonthlyPrice = 499.00m,
                YearlyPrice  = 4_490.00m,
                HtmlContent  = "<ul>" +
                               "<li><i class=\"fas fa-check text-success\"></i> 50 GB Depolama Alanı</li>" +
                               "<li><i class=\"fas fa-check text-success\"></i> Sınırsız Görüntüleme</li>" +
                               "<li><i class=\"fas fa-check text-success\"></i> Öncelikli CDN</li>" +
                               "<li><i class=\"fas fa-check text-success\"></i> Video Analitikleri</li>" +
                               "</ul>",
                IsActive = true
            },
            new ModulePackage
            {
                ModuleId = videoModule.Id,
                Name = "Kurumsal Paket",
                QuotaAmount = 200_000,       // 200 GB → MB cinsinden
                MonthlyPrice = 1_499.00m,
                YearlyPrice  = 13_490.00m,
                HtmlContent  = "<ul>" +
                               "<li><i class=\"fas fa-check text-success\"></i> 200 GB Depolama Alanı</li>" +
                               "<li><i class=\"fas fa-check text-success\"></i> Sınırsız Görüntüleme</li>" +
                               "<li><i class=\"fas fa-check text-success\"></i> Öncelikli CDN</li>" +
                               "<li><i class=\"fas fa-check text-success\"></i> Gelişmiş Analitikler</li>" +
                               "<li><i class=\"fas fa-check text-success\"></i> VIP Destek</li>" +
                               "</ul>",
                IsActive = true
            }
        };

        _context.ModulePackages.AddRange(videoPackages);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Added {Count} new MB-based packages for Video Hosting.", videoPackages.Count);
    }


    private async Task SeedTestPurchasesAsync()
    {
        // Get test user
        var testUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "huseyin.arslan@defaultcompany.com");
        if (testUser == null) return;

        // Get some modules for test purchases
        var firstOrderModule = await _context.Modules.FirstOrDefaultAsync(m => m.Name == "İlk Alışveriş Mesajı");
        var commentModule = await _context.Modules.FirstOrDefaultAsync(m => m.Name == "Yorum Taşıma");
        var birthdayModule = await _context.Modules.FirstOrDefaultAsync(m => m.Name == "Doğum Günü Hatırlatma");

        if (firstOrderModule == null || commentModule == null || birthdayModule == null) return;

        // Get test user's company
        if (testUser.CompanyId == null) return;

        // Check if test purchases already exist
        var existingPurchases = await _context.CompanyModules
            .Where(cm => cm.CompanyId == testUser.CompanyId)
            .ToListAsync();

        if (existingPurchases.Any()) return;

        // Create test purchases with default settings
        var companyModules = new List<CompanyModule>
        {
            new CompanyModule
            {
                CompanyId = testUser.CompanyId.Value,
                ModuleId = commentModule.Id,
                PurchasedAt = DateTime.Now.AddDays(-10),
                IsActive = true,
                PaidAmount = commentModule.Price,
                TransactionId = "TEST_001",
                PurchasedByUserId = testUser.Id
            },
            new CompanyModule
            {
                CompanyId = testUser.CompanyId.Value,
                ModuleId = birthdayModule.Id,
                PurchasedAt = DateTime.Now.AddDays(-8),
                IsActive = true,
                PaidAmount = birthdayModule.Price,
                TransactionId = "TEST_002",
                PurchasedByUserId = testUser.Id
            }
        };

        _context.CompanyModules.AddRange(companyModules);
        await _context.SaveChangesAsync();

        // Create default settings for purchased modules
        foreach (var companyModule in companyModules)
        {
            var module = await _context.Modules.FindAsync(companyModule.ModuleId);
            if (module != null && !string.IsNullOrEmpty(module.DefaultSettings))
            {
                var defaultSettings = new CompanyModuleSettings
                {
                    CompanyModuleId = companyModule.Id,
                    SettingsJson = module.DefaultSettings,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    UpdatedByUserId = testUser.Id
                };

                _context.CompanyModuleSettings.Add(defaultSettings);
            }
        }

        await _context.SaveChangesAsync();
        _logger.LogInformation("Test purchases and default settings created successfully.");
    }
}
