
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Filters;
using PushDashboard.Helpers;
using PushDashboard.Services;

namespace PushDashboard.Controllers
{
    [Produces("application/json")]
    public class TemplateModuleController : BaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ITemplateModuleService _templateModuleService;

        public TemplateModuleController(
            ApplicationDbContext context, 
            IUserContextService userContextService,
            ITemplateModuleService templateModuleService)
            : base(userContextService, context)
        {
            _context = context;
            _templateModuleService = templateModuleService;
        }

        [HttpGet]
        public async Task<IActionResult> GetModules()
        {
            var user = await GetCurrentUserWithCompanyAsync();
            if (user?.Company == null) return Json(ApiResponse.CompanyNotFound);

            var modules = await _context.Modules
                .Where(m => m.IsActive)
                .OrderBy(m => m.Category.SortOrder)
                .ThenBy(m => m.Name)
                .Select(m => new 
                {
                    m.Id,
                    m.Name,
                    m.Description,
                    CategoryName = m.Category.Name
                })
                .ToListAsync();

            // Map to standard keys
            var result = modules.Select(m => new
            {
                id = m.Id,
                name = m.Name,
                description = m.Description,
                category = m.CategoryName,
                key = GetModuleKeyByName(m.Name)
            })
            .Where(m => !string.IsNullOrEmpty(m.key)) 
            .ToList();

            return Json(new { success = true, modules = result });
        }

        [HttpGet]
        public IActionResult GetVariables(string moduleKey)
        {
            var variables = TemplateVariableHelper.GetVariablesForModule(moduleKey);
            return Json(new { 
                success = true, 
                variables = variables.Select(v => new { key = v.Key, label = v.Value }).ToList() 
            });
        }

        /// <summary>
        /// Belirtilen kanal ve modül tipi için mevcut şablonları getirir
        /// </summary>
        [HttpGet]
        [RequireCompanyId]
        public async Task<IActionResult> GetChannelTemplates(string channelType, string? moduleType)
        {
            var companyId = GetCurrentUserCompanyId()!.Value;
            var templates = await _templateModuleService.GetChannelTemplatesAsync(companyId, channelType, moduleType);
            return Json(new { success = true, data = templates });
        }

        private string GetModuleKeyByName(string moduleName)
        {
            if (string.IsNullOrEmpty(moduleName)) return "";

            var name = moduleName.Trim();

            return name switch
            {
                "Toplu Mesaj Gönderimi" => "BulkMessaging",
                "İlk Alışveriş Mesajı" => "FirstOrder",
                "Doğum Günü Hatırlatma" => "Birthday",
                "Sepet Hatırlatma" => "BasketReminder",
                "Stok Hatırlatma" => "StockReminder",
                "Hediye Çarkı" => "GiftWheel",
                "Sipariş Durumu Bildirimleri" => "OrderStatus",
                "Favori Ürünler" => "FavoriteProduct",
                _ => ""
            };

        }
    }
}
