using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.Services;
using PushDashboard.ViewModels;
using PushDashboard.Filters;

namespace PushDashboard.Controllers;

[Authorize]
public class HomeController : BaseController
{
    private readonly ILogger<HomeController> _logger;
    private readonly IDashboardService _dashboardService;
    private readonly ICustomerService _customerService;
    private readonly IActivityLogService _activityLogService;

    public HomeController(
        ILogger<HomeController> logger,
        IDashboardService dashboardService,
        ICustomerService customerService,
        ApplicationDbContext context,
        IUserContextService userContextService,
        IActivityLogService activityLogService)
        : base(userContextService, context)
    {
        _logger = logger;
        _dashboardService = dashboardService;
        _customerService = customerService;
        _activityLogService = activityLogService;
    }

    public async Task<IActionResult> Index()
    {
        var companyId = GetCurrentUserCompanyId();
        if (companyId == null)
        {
            TempData["ErrorMessage"] = "Şirket bilgisi bulunamadı.";
            return RedirectToAction("Login", "Account");
        }

        try
        {
            var dashboardData = await _dashboardService.GetDashboardDataAsync(companyId.Value);
            
            // Set default date range for the view (Last 1 Month)
            ViewBag.StartDate = DateTime.Now.AddMonths(-1).ToString("yyyy-MM-dd");
            ViewBag.EndDate = DateTime.Now.ToString("yyyy-MM-dd");
            
            return View(dashboardData);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard for company {CompanyId}", companyId);
            TempData["ErrorMessage"] = "Dashboard yüklenirken bir hata oluştu.";
            return View(new DashboardIndexViewModel());
        }
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetRealTimeData()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        var data = await _dashboardService.GetRealTimeDataAsync(companyId);
        return Json(new { success = true, data });
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetUsageTrendData(int days = 7)
    {
        try
        {
            var companyId = GetCurrentUserCompanyId()!.Value;

            if (days != 7 && days != 30)
            {
                days = 7;
            }

            var charts = await _dashboardService.GetDashboardChartsAsync(companyId, days);

            return Json(new
            {
                success = true,
                data = new
                {
                    usage = charts.UsageTrend.Select(u => new { label = u.Label, value = u.Value }),
                    cost = charts.CostTrend.Select(c => new { label = c.Label, value = c.Value }),
                    totalUsage = charts.UsageTrend.Sum(u => u.Value),
                    totalCost = charts.CostTrend.Sum(c => c.Value),
                    averageCost = charts.CostTrend.Any() ? charts.CostTrend.Average(c => c.Value) : 0,
                    maxCost = charts.CostTrend.Any() ? charts.CostTrend.Max(c => c.Value) : 0,
                    minCost = charts.CostTrend.Any() ? charts.CostTrend.Min(c => c.Value) : 0
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting usage trend data");
            return Json(new { success = false, message = "Veri alınırken bir hata oluştu." });
        }
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetRevenueByModule(DateTime? startDate, DateTime? endDate)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var data = await _dashboardService.GetModuleRevenuesAsync(companyId, startDate, endDate);
        
        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
        
        return PartialView("Partials/_RevenueByModulePartial", data);
    }

    [HttpPost]
    [RequireCompanyId]
    public async Task<IActionResult> SyncAll()
    {
        try
        {
            var companyId = GetCurrentUserCompanyId()!.Value;

            _logger.LogInformation("Starting full system sync for company {CompanyId}", companyId);

            var result = await _customerService.SyncCustomersAndBasketsAsync(companyId);

            _logger.LogInformation("Full system sync completed for company {CompanyId}",
                companyId);

            // Activity Log - Full System Sync
            await _activityLogService.LogActivityAsync(
                GetCurrentUserId()!,
                companyId,
                ActivityActionTypes.Sync,
                ActivityModules.Dashboard,
                $"Tam sistem senkronizasyonu: {result.SyncedCustomers} müşteri, {result.SyncedCarts} sepet",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            return Json(new
            {
                success = true,
                message = result.Message,
                syncedCustomers = result.SyncedCustomers,
                syncedCarts = result.SyncedCarts,
                syncDuration = result.SyncDuration
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during full system sync");
            return Json(new { success = false, message = ex.Message });
        }
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [AllowAnonymous]
    [Route("Home/Error/{statusCode?}")]
    public IActionResult Error(int? statusCode)
    {
        if (statusCode == 401)
        {
            return RedirectToAction("Login", "Account");
        }

        if (statusCode == 404)
        {
            return View("NotFound");
        }
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    public IActionResult Terms()
    {
        return View();
    }

    [AllowAnonymous]
    public IActionResult Help()
    {
        return View();
    }
}
