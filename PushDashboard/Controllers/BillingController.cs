using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Filters;
using PushDashboard.Helpers;
using PushDashboard.Models;
using PushDashboard.Services;
using PushDashboard.ViewModels;

namespace PushDashboard.Controllers;

[Authorize]
[RequireUser]
public class BillingController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BillingController> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly IR2StorageService _r2StorageService;

    public BillingController(
        ApplicationDbContext context,
        ILogger<BillingController> logger,
        IWebHostEnvironment environment,
        IR2StorageService r2StorageService,
        IUserContextService userContextService) : base(userContextService, context)
    {
        _context = context;
        _logger = logger;
        _environment = environment;
        _r2StorageService = r2StorageService;
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> Index()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var company = await _context.Companies.FindAsync(companyId);
        var canEdit = HasCompanyOwnerAccess();

        var model = new BillingPageViewModel
        {
            LogoUrl = company.LogoUrl,
            ContactFirstName = company.ContactFirstName ?? "",
            ContactLastName = company.ContactLastName ?? "",
            ContactEmail = company.ContactEmail ?? "",
            ContactPhone = company.ContactPhone ?? "",
            Province = company.Province ?? "",
            District = company.District ?? "",
            AddressLine1 = company.AddressLine1 ?? "",
            AddressLine2 = company.AddressLine2 ?? "",
            CompanyName = company.CompanyName ?? company.Name,
            TaxOffice = company.TaxOffice ?? "",
            CompanyCategory = company.CompanyCategory ?? "",
            TaxNumber = company.TaxNumber ?? "",
            CanEdit = canEdit
        };

        return View(model);
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetBillingInfo()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var company = await _context.Companies.FindAsync(companyId);

        return Json(new
        {
            success = true,
            data = new
            {
                logoUrl = company.LogoUrl,
                contactFirstName = company.ContactFirstName ?? "",
                contactLastName = company.ContactLastName ?? "",
                contactEmail = company.ContactEmail ?? "",
                contactPhone = company.ContactPhone ?? "",
                province = company.Province ?? "",
                district = company.District ?? "",
                addressLine1 = company.AddressLine1 ?? "",
                addressLine2 = company.AddressLine2 ?? "",
                companyName = company.CompanyName ?? company.Name,
                taxOffice = company.TaxOffice ?? "",
                companyCategory = company.CompanyCategory ?? "",
                taxNumber = company.TaxNumber ?? ""
            }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ValidateModelState]
    [CompanyOwnerOrSuperAdmin]
    [RequireCompanyId]
    public async Task<IActionResult> SaveBillingInfo([FromBody] SaveBillingInfoRequest model)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var userId = GetCurrentUserId()!;
        var company = await _context.Companies.FindAsync(companyId);

        // Update company fields
        company.ContactFirstName = model.ContactFirstName;
        company.ContactLastName = model.ContactLastName;
        company.ContactEmail = model.ContactEmail;
        company.ContactPhone = model.ContactPhone;
        company.Province = model.Province;
        company.District = model.District;
        company.AddressLine1 = model.AddressLine1;
        company.AddressLine2 = model.AddressLine2;
        company.CompanyName = model.CompanyName;
        company.TaxOffice = model.TaxOffice;
        company.CompanyCategory = model.CompanyCategory;
        company.TaxNumber = model.TaxNumber;
        company.BillingUpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Billing information updated for company {CompanyId} by user {UserId}",
            company.Id, userId);

        return Json(ApiResponse.SuccessMessage("Fatura bilgileri başarıyla kaydedildi."));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [CompanyOwnerOrSuperAdmin]
    [RequireCompanyId]
    public async Task<IActionResult> UploadLogo(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return Json(new { success = false, message = "Dosya seçilmedi." });
        }

        // Validate file size (max 2MB)
        if (file.Length > 2 * 1024 * 1024)
        {
            return Json(new { success = false, message = "Dosya boyutu 2MB'dan küçük olmalıdır." });
        }

        // Validate file type
        var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
        {
            return Json(new { success = false, message = "İzin verilen formatlar: PNG, JPEG, JPG, GIF" });
        }

        var companyId = GetCurrentUserCompanyId()!.Value;
        var userId = GetCurrentUserId()!;

        try
        {
            // Get company
            var company = await _context.Companies.FindAsync(companyId);
            if (company == null)
            {
                return Json(new { success = false, message = "Şirket bulunamadı." });
            }

            // Delete old logo from R2 if exists
            if (!string.IsNullOrEmpty(company.LogoUrl))
            {
                try
                {
                    // Extract key from URL (e.g., "logos/companyId/filename.png")
                    var oldKey = ExtractR2KeyFromUrl(company.LogoUrl);
                    if (!string.IsNullOrEmpty(oldKey))
                    {
                        await _r2StorageService.DeleteFileAsync(oldKey);
                        _logger.LogInformation("Old logo deleted from R2: {Key}", oldKey);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete old logo from R2");
                    // Continue with upload even if delete fails
                }
            }

            // Generate unique filename for R2
            var fileName = $"{DateTime.Now.Ticks}{extension}";
            var r2Key = $"logos/{companyId}/{fileName}";

            // Get content type
            var contentType = extension switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };

            // Upload to R2
            string logoUrl;
            using (var stream = file.OpenReadStream())
            {
                logoUrl = await _r2StorageService.UploadImageAsync(r2Key, stream, contentType);
            }

            // Update company logo URL in database
            company.LogoUrl = logoUrl;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Logo uploaded to R2 for company {CompanyId} by user {UserId}. URL: {LogoUrl}",
                companyId,
                userId,
                logoUrl);

            return Json(new { success = true, message = "Logo başarıyla yüklendi.", logoUrl = logoUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading logo to R2 for company {CompanyId}", companyId);
            return Json(new { success = false, message = "Logo yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Extracts the R2 key from a full R2 URL
    /// </summary>
    private string? ExtractR2KeyFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return null;
        
        // If it's a full URL, extract the key part after the domain
        // Example: https://pub-xxx.r2.dev/logos/companyId/filename.png -> logos/companyId/filename.png
        try
        {
            if (url.StartsWith("http"))
            {
                var uri = new Uri(url);
                return uri.AbsolutePath.TrimStart('/');
            }
            // If it's already a key, return as is
            return url.TrimStart('/');
        }
        catch
        {
            return null;
        }
    }

    [HttpGet]
    public IActionResult GetDistricts(string province)
    {
        if (string.IsNullOrEmpty(province))
        {
            return Json(new { success = false, message = "İl seçilmedi." });
        }

        var districts = GetDistrictsByProvince(province);
        return Json(new { success = true, data = districts });
    }

    [HttpGet]
    public IActionResult GetProvinces()
    {
        var provinces = GetTurkeyProvinces();
        return Json(new { success = true, data = provinces });
    }

    [HttpGet]
    public IActionResult GetCategories()
    {
        var categories = GetCompanyCategories();
        return Json(new { success = true, data = categories });
    }

    private static List<string> GetTurkeyProvinces()
    {
        return new List<string>
        {
            "Adana", "Adıyaman", "Afyonkarahisar", "Ağrı", "Aksaray", "Amasya", "Ankara", "Antalya", "Ardahan", "Artvin",
            "Aydın", "Balıkesir", "Bartın", "Batman", "Bayburt", "Bilecik", "Bingöl", "Bitlis", "Bolu", "Burdur",
            "Bursa", "Çanakkale", "Çankırı", "Çorum", "Denizli", "Diyarbakır", "Düzce", "Edirne", "Elazığ", "Erzincan",
            "Erzurum", "Eskişehir", "Gaziantep", "Giresun", "Gümüşhane", "Hakkari", "Hatay", "Iğdır", "Isparta", "İstanbul",
            "İzmir", "Kahramanmaraş", "Karabük", "Karaman", "Kars", "Kastamonu", "Kayseri", "Kırıkkale", "Kırklareli", "Kırşehir",
            "Kilis", "Kocaeli", "Konya", "Kütahya", "Malatya", "Manisa", "Mardin", "Mersin", "Muğla", "Muş",
            "Nevşehir", "Niğde", "Ordu", "Osmaniye", "Rize", "Sakarya", "Samsun", "Şanlıurfa", "Siirt", "Sinop",
            "Sivas", "Şırnak", "Tekirdağ", "Tokat", "Trabzon", "Tunceli", "Uşak", "Van", "Yalova", "Yozgat", "Zonguldak"
        };
    }

    private static List<string> GetDistrictsByProvince(string province)
    {
        var districtMap = new Dictionary<string, List<string>>
        {
            { "İstanbul", new List<string> { "Adalar", "Arnavutköy", "Ataşehir", "Avcılar", "Bağcılar", "Bahçelievler", "Bakırköy", "Başakşehir", "Bayrampaşa", "Beşiktaş", "Beykoz", "Beylikdüzü", "Beyoğlu", "Büyükçekmece", "Çatalca", "Çekmeköy", "Esenler", "Esenyurt", "Eyüpsultan", "Fatih", "Gaziosmanpaşa", "Güngören", "Kadıköy", "Kağıthane", "Kartal", "Küçükçekmece", "Maltepe", "Pendik", "Sancaktepe", "Sarıyer", "Silivri", "Sultanbeyli", "Sultangazi", "Şile", "Şişli", "Tuzla", "Ümraniye", "Üsküdar", "Zeytinburnu" } },
            { "Ankara", new List<string> { "Akyurt", "Altındağ", "Ayaş", "Balâ", "Beypazarı", "Çamlıdere", "Çankaya", "Çubuk", "Elmadağ", "Etimesgut", "Evren", "Gölbaşı", "Güdül", "Haymana", "Kahramankazan", "Kalecik", "Keçiören", "Kızılcahamam", "Mamak", "Nallıhan", "Polatlı", "Pursaklar", "Sincan", "Şereflikoçhisar", "Yenimahalle" } },
            { "İzmir", new List<string> { "Aliağa", "Balçova", "Bayındır", "Bayraklı", "Bergama", "Beydağ", "Bornova", "Buca", "Çeşme", "Çiğli", "Dikili", "Foça", "Gaziemir", "Güzelbahçe", "Karabağlar", "Karaburun", "Karşıyaka", "Kemalpaşa", "Kınık", "Kiraz", "Konak", "Menderes", "Menemen", "Narlıdere", "Ödemiş", "Seferihisar", "Selçuk", "Tire", "Torbalı", "Urla" } },
            { "Bursa", new List<string> { "Büyükorhan", "Gemlik", "Gürsu", "Harmancık", "İnegöl", "İznik", "Karacabey", "Keles", "Kestel", "Mudanya", "Mustafakemalpaşa", "Nilüfer", "Orhaneli", "Orhangazi", "Osmangazi", "Yenişehir", "Yıldırım" } },
            { "Antalya", new List<string> { "Akseki", "Aksu", "Alanya", "Demre", "Döşemealtı", "Elmalı", "Finike", "Gazipaşa", "Gündoğmuş", "İbradı", "Kaş", "Kemer", "Kepez", "Konyaaltı", "Korkuteli", "Kumluca", "Manavgat", "Muratpaşa", "Serik" } }
        };

        if (districtMap.TryGetValue(province, out var districts))
        {
            return districts;
        }

        // Default districts for other provinces
        return new List<string> { "Merkez" };
    }

    private static List<string> GetCompanyCategories()
    {
        return new List<string>
        {
            "Kitap & Kırtasiye",
            "Teknoloji & Elektronik",
            "Gıda & İçecek",
            "Moda & Giyim",
            "Kozmetik & Kişisel Bakım",
            "Ev & Yaşam",
            "Spor & Outdoor",
            "Otomotiv",
            "Sağlık & Medikal",
            "Eğitim",
            "Finans & Sigorta",
            "Üretim & Sanayi",
            "Turizm & Otelcilik",
            "Gayrimenkul",
            "Danışmanlık & Hizmet",
            "Diğer"
        };
    }
}
