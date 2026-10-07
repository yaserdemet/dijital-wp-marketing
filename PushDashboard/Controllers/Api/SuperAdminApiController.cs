using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Services;

namespace PushDashboard.Controllers.Api;

[ApiController]
[Route("api/superadmin")]
[Authorize]
public class SuperAdminApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IUserContextService _userContextService;

    public SuperAdminApiController(ApplicationDbContext context, IUserContextService userContextService)
    {
        _context = context;
        _userContextService = userContextService;
    }

    /// <summary>
    /// Tüm company'leri listeler (sadece SuperAdmin için)
    /// </summary>
    [HttpGet("companies")]
    public async Task<IActionResult> GetCompanies()
    {
        var user = _userContextService.User;
        if (user == null || !user.IsSuperAdmin)
        {
            return Forbid();
        }

        var companies = await _context.Companies
            .OrderBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.LogoUrl,
                c.Website
            })
            .ToListAsync();

        return Ok(companies);
    }

    /// <summary>
    /// SuperAdmin'in aktif company'sini değiştirir (session'a yazar)
    /// </summary>
    [HttpPost("switch-company")]
    public async Task<IActionResult> SwitchCompany([FromBody] SwitchCompanyRequest request)
    {
        var user = _userContextService.User;
        if (user == null || !user.IsSuperAdmin)
        {
            return Forbid();
        }

        // Verify company exists
        var company = await _context.Companies
            .FirstOrDefaultAsync(c => c.Id == request.CompanyId);

        if (company == null)
        {
            return NotFound(new { message = "Company bulunamadı." });
        }

        // Store selected company in session
        HttpContext.Session.SetString("SuperAdmin_SelectedCompanyId", request.CompanyId.ToString());
        HttpContext.Session.SetString("SuperAdmin_SelectedCompanyName", company.Name);

        return Ok(new { message = "Company değiştirildi.", companyName = company.Name });
    }

    /// <summary>
    /// SuperAdmin'in seçili company'sini temizler (kendi company'sine döner)
    /// </summary>
    [HttpPost("reset-company")]
    public IActionResult ResetCompany()
    {
        var user = _userContextService.User;
        if (user == null || !user.IsSuperAdmin)
        {
            return Forbid();
        }

        HttpContext.Session.Remove("SuperAdmin_SelectedCompanyId");
        HttpContext.Session.Remove("SuperAdmin_SelectedCompanyName");

        return Ok(new { message = "Kendi company'nize geri dönüldü." });
    }
}

public class SwitchCompanyRequest
{
    public Guid CompanyId { get; set; }
}
