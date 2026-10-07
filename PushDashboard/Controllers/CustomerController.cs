using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PushDashboard.Helpers;
using PushDashboard.Services;
using PushDashboard.ViewModels;
using PushDashboard.Data;
using PushDashboard.DTOs;
using PushDashboard.Filters;
using PushDashboard.Models;

namespace PushDashboard.Controllers;

[Authorize]
public class CustomerController : BaseController
{
    private readonly ICustomerService _customerService;
    private readonly IActivityLogService _activityLogService;

    public CustomerController(
        ICustomerService customerService, 
        ApplicationDbContext context, 
        IUserContextService userContextService,
        IActivityLogService activityLogService)
        : base(userContextService, context)
    {
        _customerService = customerService;
        _activityLogService = activityLogService;
    }

    public async Task<IActionResult> Index()
    {
        var companyId = GetCurrentUserCompanyId();
        if (companyId == null)
        {
            return RedirectToAction("Index", "Home");
        }

        // Toplu Mesaj Gönderimi modülünün aktif olup olmadığını kontrol et
        var bulkMessagingModule = _context.Modules.FirstOrDefault(m => m.Name == "Toplu Mesaj Gönderimi");
        bool hasBulkMessagingModule = false;
        
        if (bulkMessagingModule != null)
        {
            hasBulkMessagingModule = _context.CompanyModules.Any(cm => 
                cm.CompanyId == companyId.Value && 
                cm.ModuleId == bulkMessagingModule.Id && 
                cm.IsOwned && cm.IsActive && !cm.IsPassive);
        }

        ViewBag.HasBulkMessagingModule = hasBulkMessagingModule;

        // İlk yüklemede sadece boş bir view model döndür, veriler AJAX ile yüklenecek
        var emptyViewModel = new CustomerIndexViewModel
        {
            Customers = new List<CustomerIndexViewModel.CustomerViewModel>(),
            Stats = new CustomerIndexViewModel.CustomerStatsViewModel(),
            Pagination = new CustomerIndexViewModel.PaginationViewModel(),
            RecentSyncLogs = new List<CustomerIndexViewModel.CustomerSyncLogViewModel>()
        };

        return View(emptyViewModel);
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetCustomers(int page = 1, int pageSize = 50, string? searchTerm = null, string? status = null, string? source = null, string? dateFilter = null, string? sortBy = null, string? sortDirection = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        try
        {
            var viewModel = await _customerService.GetCustomersAsync(companyId, page, pageSize, searchTerm, status, source, dateFilter, sortBy, sortDirection, startDate, endDate);

            return Json(new {
                success = true,
                data = new {
                    customers = viewModel.Customers,
                    stats = viewModel.Stats,
                    pagination = viewModel.Pagination
                }
            });
        }
        catch (Exception ex)
        {
            return Json(ApiResponse.Error("Müşteriler yüklenirken hata oluştu: " + ex.Message));
        }
    }



    [HttpPost]
    [RequireCompanyId]
    public async Task<IActionResult> SyncCustomers()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var result = await _customerService.SyncCustomersAndBasketsAsync(companyId);
        
        // Activity Log - Sync Customers
        await _activityLogService.LogActivityAsync(
            GetCurrentUserId()!,
            companyId,
            ActivityActionTypes.Sync,
            ActivityModules.Customer,
            $"Müşteri senkronizasyonu yapıldı: {result.SyncedCustomers} müşteri, {result.SyncedCarts} sepet",
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

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetCustomerDetails(int id)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var customer = await _customerService.GetCustomerByIdAsync(id, companyId);
        if (customer == null)
        {
            return Json(ApiResponse.Error("Müşteri bulunamadı."));
        }

        // Customer entity'sini CustomerViewModel'e dönüştür
        var customerViewModel = CustomerIndexViewModel.CustomerViewModel.FromCustomer(customer);

        return Json(new { success = true, data = customerViewModel });
    }

    [HttpPost]
    [RequireCompanyId]
    [ValidateModelState]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerViewModel model)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var customerId = await _customerService.CreateCustomerAsync(companyId, model);

        // Activity Log - Create Customer
        await _activityLogService.LogActivityAsync(
            GetCurrentUserId()!,
            companyId,
            ActivityActionTypes.Create,
            ActivityModules.Customer,
            $"Yeni müşteri oluşturuldu: {model.FirstName} {model.LastName}",
            entityId: customerId.ToString(),
            entityType: "Customer",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString());

        return Json(new { success = true, message = "Müşteri başarıyla oluşturuldu.", customerId });
    }

    [HttpPost]
    public async Task<IActionResult> BulkImport(IFormFile file)
    {
        var companyId = GetCurrentUserCompanyId();
        var userId = GetCurrentUserId();

        if (companyId == null || userId == null)
        {
            return Json(ApiResponse.Error("Kullanıcı bilgileri bulunamadı."));
        }

        var jobId = await _customerService.StartBulkImportAsync(companyId.Value, userId, file);

        // Activity Log - Bulk Import
        await _activityLogService.LogActivityAsync(
            userId,
            companyId.Value,
            ActivityActionTypes.Import,
            ActivityModules.Customer,
            $"Toplu müşteri içe aktarma başlatıldı: {file.FileName}",
            entityId: jobId.ToString(),
            entityType: "CustomerImportJob",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString());

        return Json(new { success = true, message = "Import işlemi başlatıldı.", jobId });
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetImportProgress(int jobId)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var progress = await _customerService.GetImportProgressAsync(jobId, companyId);
        if (progress == null)
        {
            return Json(ApiResponse.Error("Import işlemi bulunamadı."));
        }

        return Json(new { success = true, data = progress });
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetImportHistory()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;
        var history = await _customerService.GetImportHistoryAsync(companyId);
        return Json(new { success = true, data = history });
    }

    [HttpGet]
    public IActionResult DownloadExcelTemplate()
    {
        var templateBytes = _customerService.GenerateExcelTemplate();
        return File(templateBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "musteri-sablonu.xlsx");
    }
}