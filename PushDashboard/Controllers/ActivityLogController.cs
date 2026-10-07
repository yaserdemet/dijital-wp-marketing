using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.Services;

namespace PushDashboard.Controllers;

[Authorize]
public class ActivityLogController : BaseController
{
    private readonly IActivityLogService _activityLogService;
    private readonly ApplicationDbContext _context;

    public ActivityLogController(
        IUserContextService userContextService,
        IActivityLogService activityLogService,
        ApplicationDbContext context) : base(userContextService, context)
    {
        _activityLogService = activityLogService;
        _context = context;
    }

    public async Task<IActionResult> Index(
        int page = 1,
        int pageSize = 20,
        string? module = null,
        string? actionType = null,
        string? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? search = null)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var (logs, totalCount) = await _activityLogService.GetActivityLogsAsync(
            companyId.Value,
            page,
            pageSize,
            module,
            actionType,
            userId,
            startDate,
            endDate,
            search);

        var stats = await _activityLogService.GetActivityStatsAsync(companyId.Value, 30);

        // Şirketteki kullanıcıları getir
        var companyUsers = await _context.Users
            .Where(u => u.CompanyId == companyId.Value)
            .Select(u => new { u.Id, Name = (u.FirstName + " " + u.LastName).Trim() })
            .ToListAsync();

        var viewModel = new ActivityLogIndexViewModel
        {
            Logs = logs,
            Stats = stats,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            SelectedModule = module,
            SelectedActionType = actionType,
            SelectedUserId = userId,
            StartDate = startDate,
            EndDate = endDate,
            SearchTerm = search,
            CompanyUsers = companyUsers.ToDictionary(u => u.Id, u => u.Name),
            AvailableModules = GetAvailableModules(),
            AvailableActionTypes = GetAvailableActionTypes()
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        int page = 1,
        int pageSize = 20,
        string? module = null,
        string? actionType = null,
        string? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? search = null)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(new { success = false, message = "Yetkilendirme hatası" });
        }

        var (logs, totalCount) = await _activityLogService.GetActivityLogsAsync(
            companyId.Value,
            page,
            pageSize,
            module,
            actionType,
            userId,
            startDate,
            endDate,
            search);

        return Json(new
        {
            success = true,
            data = logs,
            totalCount,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            currentPage = page
        });
    }

    [HttpGet]
    public async Task<IActionResult> GetStats()
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(new { success = false, message = "Yetkilendirme hatası" });
        }

        var stats = await _activityLogService.GetActivityStatsAsync(companyId.Value, 30);
        return Json(new { success = true, data = stats });
    }

    [HttpGet]
    public async Task<IActionResult> Export(
        string? module = null,
        string? actionType = null,
        string? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        // Tüm logları getir (sayfalama olmadan)
        var (logs, _) = await _activityLogService.GetActivityLogsAsync(
            companyId.Value,
            1,
            10000, // Maksimum 10000 kayıt
            module,
            actionType,
            userId,
            startDate,
            endDate);

        // CSV oluştur
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("Tarih,Kullanıcı,Email,İşlem Türü,Modül,Açıklama,IP Adresi");

        foreach (var log in logs)
        {
            csv.AppendLine($"\"{log.CreatedAt:dd.MM.yyyy HH:mm}\",\"{log.UserName}\",\"{log.UserEmail}\",\"{log.ActionTypeDisplay}\",\"{log.Module}\",\"{log.Description.Replace("\"", "\"\"")}\",\"{log.IpAddress ?? "-"}\"");
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
        var fileName = $"aktivite_loglari_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

        // Log export işlemini kaydet
        await _activityLogService.LogActivityAsync(
            GetCurrentUserId()!,
            companyId.Value,
            ActivityActionTypes.Export,
            ActivityModules.Settings,
            $"Aktivite logları dışa aktarıldı ({logs.Count} kayıt)",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString());

        return File(bytes, "text/csv;charset=utf-8", fileName);
    }

    private static Dictionary<string, string> GetAvailableModules()
    {
        return new Dictionary<string, string>
        {
            { ActivityModules.Account, "Hesap" },
            { ActivityModules.Customer, "Müşteriler" },
            { ActivityModules.Basket, "Sepetler" },
            { ActivityModules.Integration, "Entegrasyonlar" },
            { ActivityModules.GiftWheel, "Hediye Çarkı" },
            { ActivityModules.EmailTemplate, "Mail Şablonları" },
            { ActivityModules.WhatsAppTemplate, "WhatsApp Şablonları" },
            { ActivityModules.OrderStatus, "Sipariş Durumu" },
            { ActivityModules.SocialProof, "Social Proof" },
            { ActivityModules.CookieManagement, "Çerez Yönetimi" },
            { ActivityModules.BasketReminder, "Sepet Hatırlatma" },
            { ActivityModules.ExitSurvey, "Çıkış Anketi" },
            { ActivityModules.VideoHosting, "Video Hosting" },
            { ActivityModules.ProductSlider, "Ürün Slider" },
            { ActivityModules.Comment, "Yorum Taşıma" },
            { ActivityModules.BulkMessage, "Toplu Mesaj" },
            { ActivityModules.Settings, "Ayarlar" },
            { ActivityModules.Billing, "Fatura Bilgileri" },
            { ActivityModules.Store, "Mağaza" },
            { ActivityModules.Dashboard, "Dashboard" }
        };
    }

    private static Dictionary<string, string> GetAvailableActionTypes()
    {
        return new Dictionary<string, string>
        {
            { ActivityActionTypes.Create, "Oluşturma" },
            { ActivityActionTypes.Update, "Güncelleme" },
            { ActivityActionTypes.Delete, "Silme" },
            { ActivityActionTypes.View, "Görüntüleme" },
            { ActivityActionTypes.Login, "Giriş" },
            { ActivityActionTypes.Logout, "Çıkış" },
            { ActivityActionTypes.Export, "Dışa Aktarma" },
            { ActivityActionTypes.Import, "İçe Aktarma" },
            { ActivityActionTypes.Sync, "Senkronizasyon" },
            { ActivityActionTypes.Send, "Gönderim" },
            { ActivityActionTypes.Activate, "Aktifleştirme" },
            { ActivityActionTypes.Deactivate, "Pasifleştirme" },
            { ActivityActionTypes.Settings, "Ayar Değişikliği" },
            { ActivityActionTypes.Purchase, "Satın Alma" },
            { ActivityActionTypes.Configure, "Yapılandırma" }
        };
    }
}

public class ActivityLogIndexViewModel
{
    public List<ActivityLogViewModel> Logs { get; set; } = new();
    public ActivityLogStatsViewModel Stats { get; set; } = new();
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public string? SelectedModule { get; set; }
    public string? SelectedActionType { get; set; }
    public string? SelectedUserId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchTerm { get; set; }
    public Dictionary<string, string> CompanyUsers { get; set; } = new();
    public Dictionary<string, string> AvailableModules { get; set; } = new();
    public Dictionary<string, string> AvailableActionTypes { get; set; } = new();
}