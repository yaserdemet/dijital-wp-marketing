using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using System.Security.Claims;

namespace PushDashboard.Filters;

/// <summary>
/// Action filter to set module access information in ViewBag for sidebar rendering
/// </summary>
public class ModuleAccessFilter : IAsyncActionFilter
{
    private readonly ApplicationDbContext _context;

    public ModuleAccessFilter(ApplicationDbContext context)
    {
        _context = context;
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

            var companyIdClaim = user.FindFirst("CompanyId");
            if (companyIdClaim == null || !Guid.TryParse(companyIdClaim.Value, out Guid companyId))
                return;

            // Check ProductSlider module access
            var hasProductSliderModule = await _context.CompanyModules
                .Include(cm => cm.Module)
                .AnyAsync(cm => cm.CompanyId == companyId && 
                               cm.Module.Name == "Ürün Slider" && 
                               cm.IsActive &&
                               (cm.ExpiresAt == null || cm.ExpiresAt > DateTime.UtcNow));

            controller.ViewBag.HasProductSliderModule = hasProductSliderModule;
        }
        catch (Exception)
        {
            // If there's an error, default to false (don't show module)
            var controller = context.Controller as Controller;
            if (controller != null)
            {
                controller.ViewBag.HasProductSliderModule = false;
            }
        }
    }
}
