using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PushDashboard.Helpers;
using PushDashboard.Services;
using PushDashboard.ViewModels;
using PushDashboard.Data;
using PushDashboard.Filters;
using PushDashboard.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace PushDashboard.Controllers;

[Authorize]
public class BasketController : BaseController
{
    private readonly IBasketService _basketService;
    private readonly ITcmbExchangeRateService _tcmbService;
    private readonly IActivityLogService _activityLogService;
    private readonly IBasketReminderService _basketReminderService;
    private readonly ApplicationDbContext _context;
    private readonly IServiceScopeFactory _scopeFactory;

    public BasketController(
        IBasketService basketService,
        ITcmbExchangeRateService tcmbService,
        ApplicationDbContext context,
        IUserContextService userContextService,
        IActivityLogService activityLogService,
        IBasketReminderService basketReminderService,
        IServiceScopeFactory scopeFactory)
        : base(userContextService, context)
    {
        _basketService = basketService;
        _tcmbService = tcmbService;
        _activityLogService = activityLogService;
        _basketReminderService = basketReminderService;
        _context = context;
        _scopeFactory = scopeFactory;
    }

    public async Task<IActionResult> Index()
    {
        var companyId = GetCurrentUserCompanyId();
        if (companyId == null)
        {
            TempData["ErrorMessage"] = "Şirket bilgisi bulunamadı.";
            return RedirectToAction("Index", "Home");
        }

        // İlk yüklemede sadece boş bir view model döndür, veriler AJAX ile yüklenecek
        var emptyViewModel = new BasketIndexViewModel
        {
            Baskets = new List<BasketIndexViewModel.BasketViewModel>(),
            Stats = new BasketIndexViewModel.BasketStatsViewModel(),
            Pagination = new BasketIndexViewModel.PaginationViewModel(),
            RecentSyncLogs = new List<BasketIndexViewModel.BasketSyncLogViewModel>()
        };

        return View(emptyViewModel);
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetBaskets(
        int page = 1,
        int pageSize = 50,
        string? searchTerm = null,
        string? status = null,
        string? sortBy = null,
        string? sortDirection = null,
        string? customerName = null,
        string? customerEmail = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int? minProductCount = null,
        int? maxProductCount = null,
        decimal? minAmount = null,
        decimal? maxAmount = null)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        var filter = new BasketAdvancedFilter
        {
            SearchTerm = searchTerm,
            Status = status,
            SortBy = sortBy,
            SortDirection = sortDirection,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            DateFrom = dateFrom,
            DateTo = dateTo,
            MinProductCount = minProductCount,
            MaxProductCount = maxProductCount,
            MinAmount = minAmount,
            MaxAmount = maxAmount
        };

        var viewModel = await _basketService.GetBasketsAsync(companyId, page, pageSize, filter);

        return Json(new
        {
            success = true,
            data = new
            {
                baskets = viewModel.Baskets,
                stats = viewModel.Stats,
                pagination = viewModel.Pagination
            }
        });
    }

    [HttpPost]
    [RequireCompanyId]
    public async Task<IActionResult> SyncBaskets()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        try
        {
            await _basketService.SyncBasketsAsync(companyId);

            // Activity Log - Sync Baskets
            await _activityLogService.LogActivityAsync(
                GetCurrentUserId()!,
                companyId,
                ActivityActionTypes.Sync,
                ActivityModules.Basket,
                "Sepetler senkronize edildi",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            return Json(new { success = true, message = "Sepetler başarıyla senkronize edildi." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> CronSyncBaskets()
    {
        try
        {
            var companies = await _context.CompanyIntegrations.Where(x=> x.Integration.Type == "ecommerce" && x.IsActive && x.IsConfigured).Select(c => c.CompanyId).ToListAsync();

            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var basketService = scope.ServiceProvider.GetRequiredService<IBasketService>();

                foreach (var companyId in companies)
                {
                    try
                    {
                        await basketService.SyncBasketsAsync(companyId);
                    }
                    catch (Exception)
                    {
                        // Hataları yakalayıp devam etmesini sağlıyoruz. Log mekanizması eklenebilir.
                    }
                }
            });

            return Json(new { success = true, message = $"Cron sepet senkronizasyonu arka planda başlatıldı. İşlem sırasına alınan şirket sayısı: {companies.Count}" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetBasketDetails(string guidBasketId)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        if (string.IsNullOrEmpty(guidBasketId))
        {
            return Json(ApiResponse.Error("Sepet ID'si gereklidir."));
        }

        var basket = await _basketService.GetBasketDetailsAsync(guidBasketId, companyId);
        if (basket == null)
        {
            return Json(ApiResponse.Error("Sepet bulunamadı."));
        }

        return Json(new { success = true, data = basket });
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetSyncLogs()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var logs = await _basketService.GetRecentSyncLogsAsync(companyId, 10);
        return Json(new { success = true, data = logs });
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetBasketItemsPaged(string guidBasketId, int page = 1, int pageSize = 10)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        if (string.IsNullOrEmpty(guidBasketId))
        {
            return Json(ApiResponse.Error("Sepet ID'si gereklidir."));
        }

        var pagedItems = await _basketService.GetBasketItemsPagedAsync(guidBasketId, companyId, page, pageSize);
        return Json(new { success = true, data = pagedItems });
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetCurrencyTotals()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var currencyTotals = await _basketService.GetCurrencyTotalsAsync(companyId);
        return Json(new { success = true, data = currencyTotals });
    }

    /// <summary>
    /// TCMB'den güncel döviz kurlarını getirir
    /// </summary>
    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetExchangeRates()
    {
        var rates = await _tcmbService.GetExchangeRatesAsync();
        return Json(new { success = true, data = rates, updateTime = DateTime.Now });
    }

    /// <summary>
    /// Para birimi toplamlarını TCMB kurları ile TL'ye çevirir
    /// </summary>
    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetCurrencyTotalsInTry()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var currencyTotals = await _basketService.GetCurrencyTotalsAsync(companyId);

        var amounts = currencyTotals.Select(c => new CurrencyAmount
        {
            Currency = c.Currency,
            Amount = c.TotalAmount
        }).ToList();

        var result = await _tcmbService.ConvertAllToTryAsync(amounts);
        return Json(new { success = result.Success, data = result });
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetBasketsByCustomer(int customerId)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var baskets = await _basketService.GetBasketsByCustomerAsync(customerId, companyId);
        return Json(new { success = true, data = baskets });
    }

    /// <summary>
    /// Terk edilmiş sepet süresini getirir
    /// </summary>
    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetAbandonedTime()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var hours = await _basketService.GetAbandonedTimeAsync(companyId);
        return Json(new { success = true, data = hours });
    }

    /// <summary>
    /// Sepetleri Excel'e aktarır (UTF-8)
    /// </summary>
    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> ExportToExcel(
        string? searchTerm = null,
        string? status = null,
        string? sortBy = null,
        string? sortDirection = null,
        string? customerName = null,
        string? customerEmail = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int? minProductCount = null,
        int? maxProductCount = null,
        decimal? minAmount = null,
        decimal? maxAmount = null)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        var filter = new BasketAdvancedFilter
        {
            SearchTerm = searchTerm,
            Status = status,
            SortBy = sortBy,
            SortDirection = sortDirection,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            DateFrom = dateFrom,
            DateTo = dateTo,
            MinProductCount = minProductCount,
            MaxProductCount = maxProductCount,
            MinAmount = minAmount,
            MaxAmount = maxAmount
        };

        // Tüm sepetleri çek (sayfalama olmadan)
        var viewModel = await _basketService.GetBasketsAsync(companyId, 1, int.MaxValue, filter);
        var baskets = viewModel.Baskets;

        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Sepetler");

        // Başlık satırı
        var headers = new[] { "Müşteri Adı", "E-posta", "Üyelik Durumu", "Durum", "Geçen Süre", "Para Birimi", "Ürün Sayısı", "Toplam Tutar", "Sepet Tarihi" };
        for (int i = 0; i < headers.Length; i++)
        {
            worksheet.Cells[1, i + 1].Value = headers[i];
            worksheet.Cells[1, i + 1].Style.Font.Bold = true;
            worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(79, 70, 229)); // Primary color
            worksheet.Cells[1, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
        }

        // Veri satırları
        int row = 2;
        var now = DateTime.Now;
        foreach (var basket in baskets)
        {
            // Geçen süreyi hesapla
            var elapsed = now - basket.BasketDate;
            var elapsedText = elapsed.TotalDays >= 1
                ? $"{(int)elapsed.TotalDays} gün {elapsed.Hours} saat"
                : elapsed.TotalHours >= 1
                    ? $"{(int)elapsed.TotalHours} saat {elapsed.Minutes} dk"
                    : $"{(int)elapsed.TotalMinutes} dakika";

            worksheet.Cells[row, 1].Value = basket.DisplayCustomerName;
            worksheet.Cells[row, 2].Value = basket.DisplayCustomerEmail;
            worksheet.Cells[row, 3].Value = basket.MembershipStatusText;
            worksheet.Cells[row, 4].Value = basket.IsAbandoned ? "Terk Edilmiş" : "Aktif";
            worksheet.Cells[row, 5].Value = elapsedText;
            worksheet.Cells[row, 6].Value = basket.Currency;
            worksheet.Cells[row, 7].Value = basket.ProductCount;
            worksheet.Cells[row, 8].Value = basket.TotalAmount;
            worksheet.Cells[row, 8].Style.Numberformat.Format = "#,##0.00";
            worksheet.Cells[row, 9].Value = basket.BasketDate;
            worksheet.Cells[row, 9].Style.Numberformat.Format = "dd.MM.yyyy HH:mm";

            // Terk edilmiş sepetler için kırmızı arka plan
            if (basket.IsAbandoned)
            {
                worksheet.Cells[row, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[row, 4].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(254, 226, 226)); // Kırmızı
                worksheet.Cells[row, 4].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(185, 28, 28));
            }
            else
            {
                worksheet.Cells[row, 4].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[row, 4].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(220, 252, 231)); // Yeşil
                worksheet.Cells[row, 4].Style.Font.Color.SetColor(System.Drawing.Color.FromArgb(22, 101, 52));
            }

            // Alternatif satır rengi (Durum hariç)
            if (row % 2 == 0)
            {
                for (int i = 1; i <= headers.Length; i++)
                {
                    if (i != 4) // Durum kolonu hariç
                    {
                        worksheet.Cells[row, i].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[row, i].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(249, 250, 251));
                    }
                }
            }

            row++;
        }

        // Sütun genişliklerini otomatik ayarla
        worksheet.Cells[worksheet.Dimension?.Address ?? "A1"].AutoFitColumns();

        // Kenarlık ekle
        if (worksheet.Dimension != null)
        {
            var dataRange = worksheet.Cells[1, 1, row - 1, headers.Length];
            dataRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;
            dataRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
        }

        var content = package.GetAsByteArray();
        var fileName = $"Sepetler_{DateTime.Now:yyyy-MM-dd_HH-mm}.xlsx";

        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Terk edilmiş sepet süresini günceller
    /// </summary>
    [HttpPost]
    [RequireCompanyId]
    public async Task<IActionResult> UpdateAbandonedTime([FromBody] UpdateAbandonedTimeRequest request)
    {
        try
        {
            if (request == null)
            {
                return Json(new { success = false, message = "Geçersiz istek: request null" });
            }

            if (request.Hours <= 0)
            {
                return Json(new { success = false, message = $"Geçersiz saat değeri: {request.Hours}" });
            }

            var companyId = GetCurrentUserCompanyId()!.Value;
            await _basketService.UpdateAbandonedTimeAsync(companyId, request.Hours);
            return Json(new { success = true, message = $"Terk edilmiş sepet süresi {request.Hours} saat olarak güncellendi." });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Hata: {ex.Message}" });
        }
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetReminderSchedules()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var schedules = await _basketReminderService.GetActiveSchedulesAsync(companyId);

        return Json(schedules.Select(s => new {
            id = s.Id,
            name = s.Name,
            reminderTimeText = s.ReminderTimeText
        }));
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetReminderLogs(int page = 1, int pageSize = 50, int? scheduleId = null, string? status = null, DateTime? dateFrom = null, DateTime? dateTo = null)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        
        // Map status string to boolean if needed
        bool? isSuccessful = status?.ToLower() switch
        {
            "success" => true,
            "failed" => false,
            _ => null
        };

        var (logs, hasMore) = await _basketReminderService.GetLogsForScheduleAsync(
            companyId, 
            scheduleId, 
            dateFrom, 
            dateTo, 
            isSuccessful, 
            page, 
            pageSize
        );

        return Json(new {
            success = true,
            data = logs.Select(l => new {
                id = l.Id,
                basketExternalId = l.BasketExternalId,
                customerName = l.CustomerName,
                customerEmail = l.CustomerEmail,
                templateName = l.BasketReminderSchedule?.Name ?? "Bilinmiyor",
                channel = l.NotificationContent, 
                sentAt = l.SentAt.ToString("dd.MM.yyyy HH:mm"),
                status = l.IsSuccessful ? "Başarılı" : "Başarısız",
                errorMessage = l.ErrorMessage
            }),
            hasMore
        });
    }

    public class SendReminderRequest
    {
        public List<Guid> BasketIds { get; set; } = new();
        public int ScheduleId { get; set; }
    }

    [HttpPost]
    [RequireCompanyId]
    public async Task<IActionResult> SendReminderManual([FromBody] SendReminderRequest request)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (request.BasketIds == null || !request.BasketIds.Any())
        {
            return Json(new { success = false, message = "En az bir sepet seçilmelidir." });
        }

        var schedule = (await _basketReminderService.GetAllSchedulesAsync(companyId))
            .FirstOrDefault(s => s.Id == request.ScheduleId);

        if (schedule == null)
        {
            return Json(new { success = false, message = "Seçilen şablon bulunamadı." });
        }

        int successCount = 0;
        int failCount = 0;
        List<string> errors = new();

        var baskets = _context.Baskets
            .Where(b => b.CompanyId == companyId && request.BasketIds.Contains(b.Id))
            .ToList();

        foreach (var basket in baskets)
        {
            var (success, message) = await _basketReminderService.SendReminderWithChannelsAsync(companyId, basket, schedule);
            if (success)
                successCount++;
            else
            {
                failCount++;
                errors.Add($"{basket.CustomerName ?? basket.ExternalId}: {message}");
            }
        }

        string resultMessage = $"{successCount} sepet için hatırlatma gönderildi.";
        if (failCount > 0)
        {
            resultMessage += $" {failCount} sepet için gönderim başarısız.";
        }
        
        if (userId != null)
        {
            await _activityLogService.LogActivityAsync(
               userId,
               companyId,
               ActivityActionTypes.Update, 
               ActivityModules.BasketReminder,
               $"Manuel hatırlatma gönderimi: {resultMessage}",
               entityType: "BasketReminderLog",
               details: new { ScheduleId = schedule.Id, BasketCount = request.BasketIds.Count }
           );
        }

        return Json(new { success = true, message = resultMessage, errors });
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetAllBasketIds(
        string? searchTerm = null,
        string? status = null,
        string? customerName = null,
        string? customerEmail = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        int? minProductCount = null,
        int? maxProductCount = null,
        decimal? minAmount = null,
        decimal? maxAmount = null)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        var filter = new BasketAdvancedFilter
        {
            SearchTerm = searchTerm,
            Status = status,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            DateFrom = dateFrom,
            DateTo = dateTo,
            MinProductCount = minProductCount,
            MaxProductCount = maxProductCount,
            MinAmount = minAmount,
            MaxAmount = maxAmount
        };

        var viewModel = await _basketService.GetBasketsAsync(companyId, 1, int.MaxValue, filter);
        var basketIds = viewModel.Baskets.Select(b => b.Id).ToList();

        return Json(new { success = true, basketIds, totalCount = basketIds.Count });
    }
}

/// <summary>
/// Terk edilmiş sepet süresi güncelleme isteği
/// </summary>
public class UpdateAbandonedTimeRequest
{
    public int Hours { get; set; }
}
