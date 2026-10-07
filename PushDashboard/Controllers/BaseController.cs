using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.Services;
using System.Security.Claims;

namespace PushDashboard.Controllers;

[Authorize]
public class BaseController : Controller
{
    private readonly IUserContextService _userContextService;
    protected readonly ApplicationDbContext? _context;

    public BaseController(IUserContextService userContextService)
    {
        _userContextService = userContextService;
    }

    public BaseController(IUserContextService userContextService, ApplicationDbContext context)
    {
        _userContextService = userContextService;
        _context = context;
    }

    // Modern methods using UserContextService
    public Guid? GetCurrentUserCompanyId()
    {
        return _userContextService.CompanyId;
    }

    protected string? GetCurrentUserId()
    {
        return _userContextService.UserId ?? _userContextService.User?.Id;
    }

    protected ApplicationUser? GetCurrentUser()
    {
        return _userContextService.User;
    }

    protected bool IsUserAuthenticated()
    {
        return _userContextService.IsAuthenticated;
    }

    protected bool IsCurrentUserSuperAdmin()
    {
        return _userContextService.User?.IsSuperAdmin == true;
    }

    protected bool HasCompanyOwnerAccess()
    {
        return IsCurrentUserSuperAdmin() || User.IsInRole("CompanyOwner");
    }

    // Legacy methods for backward compatibility (using HttpContext.User)
    protected Guid? GetCurrentUserCompanyIdLegacy()
    {
        var companyIdClaim = User.FindFirst("CompanyId");
        if (companyIdClaim != null && Guid.TryParse(companyIdClaim.Value, out Guid companyId))
        {
            return companyId;
        }
        return null;
    }

    protected string? GetCurrentUserIdLegacy()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Tries to get the current user context. Returns false if user is not authenticated or company is not set.
    /// </summary>
    protected bool TryGetUserContext(out string userId, out Guid companyId)
    {
        userId = _userContextService.UserId ?? string.Empty;
        companyId = _userContextService.CompanyId ?? Guid.Empty;
        
        return !string.IsNullOrEmpty(userId) && companyId != Guid.Empty;
    }

    /// <summary>
    /// Gets the current user with company information loaded. Requires _context to be injected.
    /// </summary>
    protected async Task<ApplicationUser?> GetCurrentUserWithCompanyAsync()
    {
        var currentUser = GetCurrentUser();
        if (currentUser?.Company != null)
        {
            return currentUser;
        }

        if (_context == null) return currentUser;

        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId)) return currentUser;

        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Company)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return null;
        }

        var effectiveCompanyId = GetCurrentUserCompanyId();
        if (effectiveCompanyId.HasValue && user.CompanyId != effectiveCompanyId.Value)
        {
            user.CompanyId = effectiveCompanyId.Value;
            user.Company = await _context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == effectiveCompanyId.Value);
        }

        return user;
    }

    // Module access control methods
    protected async Task<bool> HasModuleAccessAsync(string moduleName)
    {
        if (_context == null) return false;

        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue) return false;

        return await _context.CompanyModules
            .Include(cm => cm.Module)
            .AnyAsync(cm => cm.CompanyId == companyId.Value &&
                           cm.Module.Name == moduleName &&
                           cm.IsActive &&
                           (cm.ExpiresAt == null || cm.ExpiresAt > DateTime.UtcNow));
    }

    protected async Task SetModuleAccessViewBagAsync()
    {
        if (_context != null)
        {
            ViewBag.HasProductSliderModule = await HasModuleAccessAsync("Ürün Slider");
        }
    }
}
