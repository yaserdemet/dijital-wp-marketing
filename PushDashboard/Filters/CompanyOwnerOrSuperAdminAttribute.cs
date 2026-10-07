using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PushDashboard.Services;

namespace PushDashboard.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class CompanyOwnerOrSuperAdminAttribute : TypeFilterAttribute
{
    public CompanyOwnerOrSuperAdminAttribute() : base(typeof(CompanyOwnerOrSuperAdminFilter))
    {
    }
}

public sealed class CompanyOwnerOrSuperAdminFilter : IAuthorizationFilter
{
    private readonly IUserContextService _userContextService;

    public CompanyOwnerOrSuperAdminFilter(IUserContextService userContextService)
    {
        _userContextService = userContextService;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        if (_userContextService.User?.IsSuperAdmin == true || user.IsInRole("CompanyOwner"))
        {
            return;
        }

        context.Result = new ForbidResult();
    }
}
