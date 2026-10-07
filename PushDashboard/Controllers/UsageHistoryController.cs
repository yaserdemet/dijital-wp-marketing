using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Helpers;
using PushDashboard.Services;
using PushDashboard.Models.ViewModels;
using System.Security.Claims;
using PushDashboard.Filters;

namespace PushDashboard.Controllers;

[Authorize]
public class UsageHistoryController : BaseController
{
    private readonly ApplicationDbContext _context;
    private readonly IModuleUsageService _moduleUsageService;

    public UsageHistoryController(
        ApplicationDbContext context,
        IModuleUsageService moduleUsageService,
        IUserContextService userContextService)
        : base(userContextService,context)
    {
        _context = context;
        _moduleUsageService = moduleUsageService;
    }

    public async Task<IActionResult> Index()
    {
        var companyId = GetCurrentUserCompanyId();
        if (companyId == null)
        {
            return RedirectToAction("Index", "Home");
        }

        // Son 30 günün işlem geçmişini al
        var startDate = DateTime.Now.AddDays(-30);
        var excludedUsageTypes = new List<string> { "sms", "email" };
        var usageHistory = await _moduleUsageService.GetUsageHistoryAsync(companyId.Value, startDate: startDate, limit: 50, excludeUsageTypes: excludedUsageTypes);
        var usageStats = await _moduleUsageService.GetUsageStatsAsync(companyId.Value, startDate: startDate, excludeUsageTypes: excludedUsageTypes);

        // Company bilgisini al
        var company = await _context.Companies.FindAsync(companyId.Value);

        var viewModel = new UsageHistoryViewModel
        {
            UsageHistory = usageHistory,
            UsageStats = usageStats,
            CurrentBalance = company?.CreditBalance ?? 0,
            CompanyName = company?.Name ?? "Bilinmeyen Şirket",
            StartDate = startDate,
            EndDate = DateTime.Now
        };

        return View(viewModel);
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetUsageHistory(DateTime? startDate = null, DateTime? endDate = null, string? moduleName = null, int page = 1, int pageSize = 20)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        // Modül adından ID'yi bul
        int? moduleId = null;
        if (!string.IsNullOrEmpty(moduleName))
        {
            var module = await _context.Modules.FirstOrDefaultAsync(m => m.Name == moduleName);
            moduleId = module?.Id;
        }

        var excludedUsageTypes = new List<string> { "sms", "email" };

        // Sayfalama için toplam kayıt sayısını al
        var totalRecords = await _moduleUsageService.GetUsageHistoryCountAsync(companyId, moduleId, startDate, endDate, excludeUsageTypes: excludedUsageTypes);

        // Sayfa numarasını doğrula
        var totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);
        page = Math.Max(1, Math.Min(page, totalPages));

        var usageHistory = await _moduleUsageService.GetUsageHistoryPagedAsync(companyId, moduleId, startDate, endDate, page, pageSize, excludeUsageTypes: excludedUsageTypes);

        var result = usageHistory.Select(uh => new
        {
            id = uh.Id,
            moduleName = uh.Module.Name,
            usageType = uh.UsageType,
            description = uh.Description,
            cost = uh.Cost,
            balanceBefore = uh.BalanceBefore,
            balanceAfter = uh.BalanceAfter,
            channel = uh.Channel,
            isSuccessful = uh.IsSuccessful,
            errorMessage = uh.ErrorMessage,
            createdAt = uh.CreatedAt,
            userName = uh.User.FirstName + " " + uh.User.LastName
        }).ToList();

        return Json(new {
            success = true,
            data = result,
            pagination = new {
                currentPage = page,
                pageSize = pageSize,
                totalRecords = totalRecords,
                totalPages = totalPages,
                hasNextPage = page < totalPages,
                hasPreviousPage = page > 1
            }
        });
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetUsageStats(DateTime? startDate = null, DateTime? endDate = null)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var excludedUsageTypes = new List<string> { "sms", "email" };
        var usageStats = await _moduleUsageService.GetUsageStatsAsync(companyId, startDate, endDate, excludeUsageTypes: excludedUsageTypes);

        return Json(new { success = true, data = usageStats });
    }


}
