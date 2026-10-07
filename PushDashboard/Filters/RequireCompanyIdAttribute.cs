using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PushDashboard.Controllers;
using PushDashboard.Helpers;

namespace PushDashboard.Filters;

/// <summary>
/// Action filter that validates the current user has a valid CompanyId.
/// If CompanyId is null, returns ApiResponse.CompanyNotFound automatically.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class RequireCompanyIdAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (context.Controller is BaseController baseController)
        {
            var companyId = baseController.GetCurrentUserCompanyId();
            if (companyId == null)
            {
                context.Result = new JsonResult(ApiResponse.CompanyNotFound);
                return;
            }
        }

        base.OnActionExecuting(context);
    }
}
