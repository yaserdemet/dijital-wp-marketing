using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using System.Security.Claims;

namespace PushDashboard.Services;

public class ClaimsTransformationService : IClaimsTransformation
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ClaimsTransformationService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated == true)
        {
            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                // Check if CompanyId claim already exists
                if (principal.FindFirst("CompanyId") == null)
                {
                    var user = await _context.Users
                        .FirstOrDefaultAsync(u => u.Id == userId);

                    if (user?.CompanyId != null)
                    {
                        var identity = (ClaimsIdentity)principal.Identity;
                        identity.AddClaim(new Claim("CompanyId", user.CompanyId.ToString()!));
                    }
                }
            }
        }

        return principal;
    }
}
