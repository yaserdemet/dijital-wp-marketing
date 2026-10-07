using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PushDashboard.Helpers;

namespace PushDashboard.Filters;

/// <summary>
/// Action filter that validates ModelState.
/// If ModelState is invalid, returns ApiResponse.ValidationError automatically.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
public class ValidateModelStateAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage);
            
            context.Result = new JsonResult(ApiResponse.ValidationError(errors));
            return;
        }

        base.OnActionExecuting(context);
    }
}
