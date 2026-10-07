using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PushDashboard.Configuration;
using PushDashboard.Data;
using PushDashboard.DTOs;
using PushDashboard.Helpers;
using PushDashboard.Models;
using PushDashboard.Services;
using PushDashboard.Services.Integrations;
using PushDashboard.Services.Integrations.Common.Models;
using PushDashboard.Services.Modules.ProductSlider;
using PushDashboard.Filters;

namespace PushDashboard.Controllers;

public class ProductSliderController : BaseController
{
    private readonly IProductSliderService _productSliderService;
    private readonly IProductSliderModuleService _moduleService;
    private readonly IEcommerceServiceFactory _ecommerceServiceFactory;
    private readonly ILogger<ProductSliderController> _logger;
    private readonly R2StorageSettings _r2Settings;
    private readonly ApplicationDbContext _context;
    private readonly IActivityLogService _activityLogService;

    public ProductSliderController(
        IProductSliderService productSliderService,
        IProductSliderModuleService moduleService,
        IEcommerceServiceFactory ecommerceServiceFactory,
        ILogger<ProductSliderController> logger,
        IUserContextService userContextService,
        IOptions<R2StorageSettings> r2Settings,
        IActivityLogService activityLogService,
        ApplicationDbContext context) : base(userContextService, context)
    {
        _productSliderService = productSliderService;
        _moduleService = moduleService;
        _ecommerceServiceFactory = ecommerceServiceFactory;
        _logger = logger;
        _r2Settings = r2Settings.Value;
        _context = context;
        _activityLogService = activityLogService;
    }

    #region Views

    /// <summary>
    /// Ana slider yönetim sayfası
    /// </summary>
    public async Task<IActionResult> Index()
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        // Check if company has active module
        if (!await _moduleService.HasActiveModuleAsync(companyId.Value))
        {
            ViewBag.ErrorMessage = "Ürün slider modülü aktif değil. Lütfen modülü satın alın.";
            return View("ModuleNotActive");
        }

        var sliders = await _productSliderService.GetSlidersAsync(companyId.Value);
        var stats = await _productSliderService.GetSliderStatsAsync(companyId.Value);

        ViewBag.Stats = stats;
        return View(sliders);
    }

    /// <summary>
    /// Test sayfası - Script'in çalışmasını test etmek için
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Test()
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        // Test için gerekli bilgileri hazırla
        var scriptUrl = string.Empty;
        if (!string.IsNullOrEmpty(_r2Settings.PublicUrl))
        {
            scriptUrl = $"{_r2Settings.PublicUrl.TrimEnd('/')}/{companyId}/modules.js";
        }

        await _productSliderService.ExportAllSlidersToR2Async(companyId.Value);
        _logger.LogInformation("Successfully exported sliders to R2 for test page, company {CompanyId}", companyId);

        var testData = new
        {
            CompanyId = companyId,
            ScriptUrl = scriptUrl,
            R2Settings = new
            {
                PublicUrl = _r2Settings.PublicUrl,
                BucketName = _r2Settings.BucketName,
                AccountId = _r2Settings.AccountId
            },
            TestInfo = new
            {
                PageType = "test",
                Timestamp = DateTime.Now,
                UserAgent = Request.Headers["User-Agent"].ToString()
            }
        };

        ViewBag.TestData = testData;
        return View();
    }

    /// <summary>
    /// Standalone Product Slider Script - Test için
    /// </summary>
    [HttpGet]
    public IActionResult StandaloneScript()
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Content("// Company ID not found", "application/javascript");
        }

        var script = _productSliderService.GenerateStandaloneProductSliderScript(companyId.Value);
        return Content(script, "application/javascript");
    }

    /// <summary>
    /// Yeni slider oluşturma sayfası
    /// </summary>
    public IActionResult Create()
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        return View();
    }

    /// <summary>
    /// Slider düzenleme sayfası
    /// </summary>
    public async Task<IActionResult> Edit(int id)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var slider = await _productSliderService.GetSliderDetailAsync(companyId.Value, id);
        if (slider == null)
        {
            TempData["ErrorMessage"] = "Slider bulunamadı.";
            return RedirectToAction("Index");
        }

        return View(slider);
    }

    /// <summary>
    /// Slider detay sayfası
    /// </summary>
    public async Task<IActionResult> Details(int id)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var slider = await _productSliderService.GetSliderDetailAsync(companyId.Value, id);
        if (slider == null)
        {
            TempData["ErrorMessage"] = "Slider bulunamadı.";
            return RedirectToAction("Index");
        }

        return View(slider);
    }

    #endregion

    #region Slider Management APIs

    /// <summary>
    /// Yeni slider oluşturur
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [ValidateModelState]
    public async Task<IActionResult> CreateSlider([FromBody] CreateProductSliderRequestDto request)
    {
        var companyId = GetCurrentUserCompanyId();
        var userId = GetCurrentUserId();

        if (!companyId.HasValue || string.IsNullOrEmpty(userId))
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var result = await _productSliderService.CreateSliderAsync(companyId.Value, userId, request);

        if (result.Success && result.SliderId.HasValue)
        {
            await _productSliderService.ExportAllSlidersToR2Async(companyId.Value);
            _logger.LogInformation("Successfully exported new slider {SliderId} to R2", result.SliderId.Value);

            await _activityLogService.LogActivityAsync(
                userId,
                companyId.Value,
                ActivityActionTypes.Create,
                ActivityModules.ProductSlider,
                $"Yeni ürün slider'ı oluşturuldu: {request.Name}",
                entityId: result.SliderId.Value.ToString(),
                entityType: "ProductSlider",
                details: new { request.Name, request.DisplayType },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        return Json(new
        {
            success = result.Success,
            message = result.Message,
            sliderId = result.SliderId
        });
    }


    /// <summary>
    /// Slider'ı siler
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSlider(int id)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var result = await _productSliderService.DeleteSliderAsync(companyId.Value, id);

        if (result.Success)
        {
            await _productSliderService.ExportAllSlidersToR2Async(companyId.Value);
            _logger.LogInformation("Successfully updated R2 after deleting slider {SliderId}", id);

            var userId = GetCurrentUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await _activityLogService.LogActivityAsync(
                    userId,
                    companyId.Value,
                    ActivityActionTypes.Delete,
                    ActivityModules.ProductSlider,
                    $"Ürün slider'ı silindi (ID: {id})",
                    entityId: id.ToString(),
                    entityType: "ProductSlider",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString());
            }
        }

        return Json(new { success = result.Success, message = result.Message });
    }

    /// <summary>
    /// Slider durumunu değiştirir
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSliderStatus(int id)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var result = await _productSliderService.ToggleSliderStatusAsync(companyId.Value, id);

        if (result.Success)
        {
            var userId = GetCurrentUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await _activityLogService.LogActivityAsync(
                    userId,
                    companyId.Value,
                    ActivityActionTypes.StatusChange,
                    ActivityModules.ProductSlider,
                    $"Ürün slider'ı durumu değiştirildi (ID: {id})",
                    entityId: id.ToString(),
                    entityType: "ProductSlider",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString());
            }
        }

        return Json(new { success = result.Success, message = result.Message });
    }

    #endregion

    #region Slider Items Management APIs

    /// <summary>
    /// Slider'a yeni ürün ekler
    /// </summary>
    [HttpPost]
    [ValidateModelState]
    public async Task<IActionResult> AddSliderItem([FromBody] CreateProductSliderItemRequestDto request)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var result = await _productSliderService.AddSliderItemAsync(companyId.Value, request);
        return Json(new
        {
            success = result.Success,
            message = result.Message,
            itemId = result.ItemId
        });
    }

    /// <summary>
    /// Slider ürününü siler
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> DeleteSliderItem(int id)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var result = await _productSliderService.DeleteSliderItemAsync(companyId.Value, id);
        return Json(new { success = result.Success, message = result.Message });
    }

    /// <summary>
    /// Ürün sırasını günceller
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> UpdateItemsOrder([FromBody] UpdateItemsOrderRequestDto request)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var result = await _productSliderService.UpdateItemsOrderAsync(companyId.Value, request.SliderId, request.ItemIds);
        return Json(new { success = result.Success, message = result.Message });
    }

    /// <summary>
    /// Slider'daki Ticimax kaynaklı tüm ürünleri Ticimax verisiyle eşitler.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SyncWithTicimax(int id)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));

        var result = await _productSliderService.SyncSliderWithTicimaxAsync(companyId.Value, id);
        return Json(new { success = result.Success, message = result.Message, updatedCount = result.UpdatedCount });
    }

    /// <summary>
    /// Ürün durumunu değiştirir
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> ToggleSliderItemStatus(int id)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var result = await _productSliderService.ToggleSliderItemStatusAsync(companyId.Value, id);
        if (result.Success)
        {
            var userId = GetCurrentUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await _activityLogService.LogActivityAsync(
                    userId,
                    companyId.Value,
                    ActivityActionTypes.StatusChange,
                    ActivityModules.ProductSlider,
                    result.Message,
                    entityId: id.ToString(),
                    entityType: "ProductSliderItem",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString());
            }
        }
        return Json(new { success = result.Success, message = result.Message });
    }

    /// <summary>
    /// Slider ayarlarını günceller
    /// </summary>
    [HttpPost]
    [ValidateModelState]
    public async Task<IActionResult> UpdateSliderSettings([FromBody] UpdateProductSliderSettingsRequestDto request)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var result = await _productSliderService.UpdateSliderSettingsAsync(companyId.Value, request);

        if (result.Success)
        {
            await _productSliderService.ExportAllSlidersToR2Async(companyId.Value);
            _logger.LogInformation("Successfully exported slider {SliderId} to R2 after settings update", request.SliderId);

            var userId = GetCurrentUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                var slider = await _context.ProductSliders
                    .FirstOrDefaultAsync(s => s.Id == request.SliderId && s.CompanyId == companyId.Value);

                if (slider != null)
                {
                    var changes = new List<string>();
                    if (!string.IsNullOrWhiteSpace(request.Name))
                        changes.Add($"İsim: {request.Name}");
                    if (request.Description != null)
                        changes.Add("Açıklama güncellendi");
                    if (!string.IsNullOrWhiteSpace(request.DisplayType))
                        changes.Add($"Görüntüleme tipi: {request.DisplayType}");
                    if (request.IsActive.HasValue)
                        changes.Add($"Durum: {(request.IsActive.Value ? "Aktif" : "Pasif")}");

                    var changeDetails = changes.Any()
                        ? string.Join(", ", changes)
                        : "Ayarlar güncellendi";

                    await _activityLogService.LogActivityAsync(
                        userId,
                        companyId.Value,
                        ActivityActionTypes.Update,
                        ActivityModules.ProductSlider,
                        $"Ürün slider'ı güncellendi (ID: {request.SliderId}) - {changeDetails}",
                        entityId: request.SliderId.ToString(),
                        entityType: "ProductSlider",
                        details: new
                        {
                            SliderId = request.SliderId,
                            Name = request.Name ?? slider.Name,
                            DisplayType = request.DisplayType ?? slider.DisplayType,
                            IsActive = request.IsActive ?? slider.IsActive
                        },
                        ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                        userAgent: Request.Headers["User-Agent"].ToString());
                }
            }
        }

        return Json(new { success = result.Success, message = result.Message });
    }

    /// <summary>
    /// E-ticaret entegrasyonundan ürünleri getirir
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetEcommerceProducts(
        int page = 1,
        int pageSize = 10,
        string? searchTerm = null,
        string? categoryId = null,
        string? brandId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? inStock = null,
        int? sliderId = null)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var ecommerceService = await _ecommerceServiceFactory.GetEcommerceServiceAsync(companyId.Value);
        if (ecommerceService == null)
        {
            return Json(ApiResponse.Error("E-ticaret entegrasyonu bulunamadı."));
        }

        var filter = new EcommerceProductFilter
        {
            SearchTerm = searchTerm,
            CategoryId = categoryId,
            BrandId = brandId,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            InStock = inStock,
            IsActive = true
        };

        var pagination = new EcommercePagination
        {
            PageNumber = page,
            PageSize = pageSize,
            SortField = "Name",
            SortDirection = "ASC"
        };

        var products = await ecommerceService.GetProductsAsync(companyId.Value, filter, pagination);
        var productList = products.ToList();

        // Slider'daki mevcut ürünlerin URL'lerini al
        var existingProductUrls = new HashSet<string>();
        if (sliderId.HasValue)
        {
            existingProductUrls = (await _context.ProductSliderItems
                    .Where(i => i.SliderId == sliderId.Value && i.Slider.CompanyId == companyId.Value)
                    .Select(i => i.ProductUrl)
                    .ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        // Toplam ürün sayısını hesapla (basit tahmin - gerçek API'den gelmiyorsa)
        var totalCount = productList.Count < pageSize ? (page - 1) * pageSize + productList.Count : page * pageSize + 1;
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return Json(new
        {
            success = true,
            data = productList.Select(p => new
            {
                id = p.ExternalId,
                name = p.Name,
                code = p.ProductCode,
                price = p.Price,
                discountPrice = p.DiscountPrice,
                currency = p.Currency,
                image = p.MainImage,
                categoryName = p.CategoryName,
                brandName = p.BrandName,
                supplierName = p.SupplierName,
                tags = p.Tags ?? new List<string>(),
                inStock = p.InStock,
                stockQuantity = p.StockQuantity,
                productUrl = p.ProductUrl,
                isAlreadyAdded = !string.IsNullOrEmpty(p.ProductUrl) && existingProductUrls.Contains(p.ProductUrl)
            }),
            pagination = new
            {
                currentPage = page,
                pageSize = pageSize,
                totalPages = totalPages,
                hasNextPage = productList.Count >= pageSize,
                hasPreviousPage = page > 1
            }
        });
    }

    /// <summary>
    /// E-ticaret entegrasyonundan ürünleri detaylı bilgilerle (tedarikçi, marka, stok, kategori, etiketler) getirir
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProductsWithDetails(
        int page = 1,
        int pageSize = 10,
        string? searchTerm = null,
        string? categoryId = null,
        string? brandId = null,
        string? supplierId = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? inStock = null,
        string? tag = null,
        int? sliderId = null)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(ApiResponse.Error("Oturum bilgisi bulunamadı."));
        }

        var ecommerceService = await _ecommerceServiceFactory.GetEcommerceServiceAsync(companyId.Value);
        if (ecommerceService == null)
        {
            return Json(ApiResponse.Error("E-ticaret entegrasyonu bulunamadı."));
        }

        var filter = new EcommerceProductFilter
        {
            SearchTerm = searchTerm,
            CategoryId = categoryId,
            BrandId = brandId,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            InStock = inStock,
            IsActive = true,
            CustomFilters = !string.IsNullOrEmpty(supplierId)
                ? new Dictionary<string, object> { ["supplierId"] = supplierId }
                : null
        };

        var pagination = new EcommercePagination
        {
            PageNumber = page,
            PageSize = pageSize,
            SortField = "Name",
            SortDirection = "ASC"
        };

        var products = await ecommerceService.GetProductsAsync(companyId.Value, filter, pagination);
        var productList = products.ToList();

        // Tedarikçi filtresi uygula (API seviyesinde desteklenmiyorsa)
        if (!string.IsNullOrEmpty(supplierId))
        {
            productList = productList.Where(p => p.SupplierId == supplierId).ToList();
        }

        // Etiket filtresi uygula
        if (!string.IsNullOrEmpty(tag))
        {
            productList = productList.Where(p => p.Tags != null && p.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)).ToList();
        }

        // Slider'daki mevcut ürünlerin URL'lerini al
        var existingProductUrls = new HashSet<string>();
        if (sliderId.HasValue)
        {
            existingProductUrls = (await _context.ProductSliderItems
                    .Where(i => i.SliderId == sliderId.Value && i.Slider.CompanyId == companyId.Value)
                    .Select(i => i.ProductUrl)
                    .ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        // Toplam ürün sayısını hesapla
        var totalCount = productList.Count < pageSize ? (page - 1) * pageSize + productList.Count : page * pageSize + 1;
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        return Json(new
        {
            success = true,
            data = productList.Select(p => new
            {
                id = p.ExternalId,
                name = p.Name,
                code = p.ProductCode,
                description = p.Description,
                shortDescription = p.ShortDescription,
                price = p.Price,
                discountPrice = p.DiscountPrice,
                currency = p.Currency,
                image = p.MainImage,
                images = p.Images,
                categoryId = p.CategoryId,
                categoryName = p.CategoryName,
                brandId = p.BrandId,
                brandName = p.BrandName,
                supplierId = p.SupplierId,
                supplierName = p.SupplierName,
                tags = p.Tags,
                inStock = p.InStock,
                stockQuantity = p.StockQuantity,
                productUrl = p.ProductUrl,
                createdAt = p.CreatedAt,
                updatedAt = p.UpdatedAt,
                isAlreadyAdded = !string.IsNullOrEmpty(p.ProductUrl) && existingProductUrls.Contains(p.ProductUrl)
            }),
            pagination = new
            {
                currentPage = page,
                pageSize = pageSize,
                totalPages = totalPages,
                hasNextPage = productList.Count >= pageSize,
                hasPreviousPage = page > 1
            }
        });
    }

    /// <summary>
    /// Slider'ları R2'ye manuel export eder
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportToR2()
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(new { success = false, message = "Oturum bilgisi bulunamadı." });
        }

        // Export all sliders to R2
        var jsonUrl = await _productSliderService.ExportAllSlidersToR2Async(companyId.Value);

        return Json(new
        {
            success = true,
            message = "Slider'lar başarıyla site'ye aktarıldı.",
            jsonUrl = jsonUrl
        });
    }


    #endregion
}

/// <summary>
/// Ürün sırası güncelleme için DTO
/// </summary>
public class UpdateItemsOrderRequestDto
{
    public int SliderId { get; set; }

    public List<int> ItemIds { get; set; } = new();
}
