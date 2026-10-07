using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Services;
using System.Security.Claims;

namespace PushDashboard.Filters;

/// <summary>
/// Action filter to set module access information in ViewBag for sidebar rendering
/// </summary>
public class ModuleAccessFilter : IAsyncActionFilter
{
    private readonly ApplicationDbContext _context;
    private readonly IUserContextService _userContextService;

    public ModuleAccessFilter(ApplicationDbContext context, IUserContextService userContextService)
    {
        _context = context;
        _userContextService = userContextService;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Set module access information before action execution
        await SetModuleAccessViewBag(context);

        // Continue with action execution
        await next();
    }

    private async Task SetModuleAccessViewBag(ActionExecutingContext context)
    {
        try
        {
            var controller = context.Controller as Controller;
            if (controller == null) return;

            var user = context.HttpContext.User;
            if (!user.Identity.IsAuthenticated) return;

            // UserContextService üzerinden CompanyId al (SuperAdmin override destekli)
            var companyId = _userContextService.CompanyId;
            if (!companyId.HasValue) return;

            // Check ProductSlider module access
            var hasProductSliderModule = await _context.CompanyModules
                .Include(cm => cm.Module)
                .AnyAsync(cm => cm.CompanyId == companyId.Value &&
                               cm.Module.Name == "Ürün Slider" &&
                               cm.IsActive &&
                               (cm.ExpiresAt == null || cm.ExpiresAt > DateTime.UtcNow));

            // Check BasketReminder module access
            var hasBasketReminderModule = await _context.CompanyModules
                .Include(cm => cm.Module)
                .AnyAsync(cm => cm.CompanyId == companyId.Value &&
                               cm.Module.Name == "Sepet Hatırlatma" &&
                               cm.IsActive &&
                               (cm.ExpiresAt == null || cm.ExpiresAt > DateTime.UtcNow));

            controller.ViewBag.HasProductSliderModule = hasProductSliderModule;
            controller.ViewBag.HasBasketReminderModule = hasBasketReminderModule;
        }
        catch (Exception)
        {
            // If there's an error, default to false (don't show module)
            var controller = context.Controller as Controller;
            if (controller != null)
            {
                controller.ViewBag.HasProductSliderModule = false;
                controller.ViewBag.HasBasketReminderModule = false;
            }
        }
    }
}