using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PushDashboard.Helpers;
using PushDashboard.Models;

namespace PushDashboard.Filters;

/// <summary>
/// Action filter that validates the current user exists.
/// If user is not found, returns ApiResponse.UserNotFound automatically.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireUserAttribute : ActionFilterAttribute
{
    public override async void OnActionExecuting(ActionExecutingContext context)
    {
        // Get UserManager from DI
        var userManager = context.HttpContext.RequestServices.GetService<UserManager<ApplicationUser>>();
        
        if (userManager == null)
        {
            context.Result = new JsonResult(ApiResponse.Error("UserManager not found in DI container."));
            return;
        }

        var user = await userManager.GetUserAsync(context.HttpContext.User);
        if (user == null)
        {
            context.Result = new JsonResult(ApiResponse.UserNotFound);
            return;
        }

        base.OnActionExecuting(context);
    }
}
