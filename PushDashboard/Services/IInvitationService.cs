using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.ViewModels;
using System.Security.Cryptography;
using System.Text;

namespace PushDashboard.Services;

public interface IInvitationService
{
    Task<(bool Success, string Message)> SendInvitationAsync(Guid companyId, string email, string inviterUserId, string inviterName);
    Task<InvitationManagementViewModel> GetCompanyInvitationsAsync(Guid companyId);
    Task<(bool Success, string Message)> CancelInvitationAsync(int invitationId, Guid companyId);
    Task<ValidateInvitationResponse> ValidateInvitationTokenAsync(string token);
    Task<(bool Success, string Message)> AcceptInvitationAsync(AcceptInvitationRequest request);
    Task<bool> CanSendMoreInvitationsAsync(Guid companyId);
    Task<int> GetCompanyUserCountAsync(Guid companyId);
}

public class InvitationService : IInvitationService
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<InvitationService> _logger;
    private readonly IConfiguration _configuration;
    private const int MaxUsersPerCompany = 20;
    private const int InvitationValidHours = 12;

    public InvitationService(
        ApplicationDbContext context,
        IEmailService emailService,
        ILogger<InvitationService> logger,
        IConfiguration configuration)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<(bool Success, string Message)> SendInvitationAsync(Guid companyId, string email, string inviterUserId, string inviterName)
    {
        try
        {
            // Check if company exists
            var company = await _context.Companies.FindAsync(companyId);
            if (company == null)
            {
                return (false, "Firma bulunamadı.");
            }

            // Check user limit
            if (!await CanSendMoreInvitationsAsync(companyId))
            {
                return (false, "Maksimum kullanıcı sayısına ulaşıldı. En fazla 20 kullanıcı davet edebilirsiniz.");
            }

            // Check if user already exists
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (existingUser != null)
            {
                return (false, "Bu e-posta adresi ile kayıtlı bir kullanıcı zaten mevcut.");
            }

            // Check if there's already a pending invitation
            var existingInvitation = await _context.UserInvitations
                .FirstOrDefaultAsync(i => i.Email == email && i.CompanyId == companyId && !i.IsUsed && i.ExpirationDate > DateTime.Now);

            if (existingInvitation != null)
            {
                return (false, "Bu e-posta adresine zaten aktif bir davetiye gönderilmiş.");
            }

            // Generate invitation token
            var token = GenerateInvitationToken();
            var expirationDate = DateTime.Now.AddHours(InvitationValidHours);

            // Create invitation record
            var invitation = new UserInvitation
            {
                Email = email,
                CompanyId = companyId,
                InvitationToken = token,
                ExpirationDate = expirationDate,
                CreatedBy = inviterUserId,
                CreatedAt = DateTime.Now
            };

            _context.UserInvitations.Add(invitation);
            await _context.SaveChangesAsync();

            // Generate invitation link
            var baseUrl = _configuration["BaseUrl"] ?? "https://localhost:44316";
            var invitationLink = $"{baseUrl}/Account/AcceptInvitation?token={token}";

            // Send invitation email
            var emailSent = await _emailService.SendInvitationEmailAsync(email, inviterName, company.Name, invitationLink);

            if (!emailSent)
            {
                // Remove the invitation if email failed
                _context.UserInvitations.Remove(invitation);
                await _context.SaveChangesAsync();
                return (false, "Davetiye e-postası gönderilemedi. Lütfen daha sonra tekrar deneyin.");
            }

            _logger.LogInformation("Invitation sent successfully to {Email} for company {CompanyId} by user {InviterUserId}",
                email, companyId, inviterUserId);

            return (true, "Davetiye başarıyla gönderildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending invitation to {Email} for company {CompanyId}", email, companyId);
            return (false, "Davetiye gönderilirken bir hata oluştu.");
        }
    }

    public async Task<InvitationManagementViewModel> GetCompanyInvitationsAsync(Guid companyId)
    {
        var invitationsData = await _context.UserInvitations
            .Include(i => i.CreatedByUser)
            .Where(i => i.CompanyId == companyId)
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new
            {
                i.Id,
                i.Email,
                i.CreatedAt,
                i.ExpirationDate,
                i.IsUsed,
                i.UsedAt,
                CreatedByName = i.CreatedByUser.FullName
            })
            .ToListAsync();

        var invitations = invitationsData.Select(i => new InvitationListItemViewModel
        {
            Id = i.Id,
            Email = i.Email,
            CreatedAt = i.CreatedAt,
            ExpirationDate = i.ExpirationDate,
            IsUsed = i.IsUsed,
            UsedAt = i.UsedAt,
            CreatedByName = i.CreatedByName,
            IsExpired = DateTime.Now > i.ExpirationDate,
            IsValid = !i.IsUsed && DateTime.Now <= i.ExpirationDate,
            StatusText = GetStatusText(i.IsUsed, DateTime.Now > i.ExpirationDate),
            StatusBadgeClass = GetStatusBadgeClass(i.IsUsed, DateTime.Now > i.ExpirationDate),
            TimeRemaining = GetTimeRemaining(i.ExpirationDate, i.IsUsed)
        }).ToList();

        var currentUserCount = await GetCompanyUserCountAsync(companyId);
        var pendingCount = invitations.Count(i => i.IsValid);

        return new InvitationManagementViewModel
        {
            PendingInvitations = invitations,
            TotalInvitations = invitations.Count,
            PendingCount = pendingCount,
            UsedCount = invitations.Count(i => i.IsUsed),
            ExpiredCount = invitations.Count(i => i.IsExpired),
            CurrentUserCount = currentUserCount,
            MaxUserLimit = MaxUsersPerCompany
        };
    }

    public async Task<(bool Success, string Message)> CancelInvitationAsync(int invitationId, Guid companyId)
    {
        try
        {
            var invitation = await _context.UserInvitations
                .FirstOrDefaultAsync(i => i.Id == invitationId && i.CompanyId == companyId);

            if (invitation == null)
            {
                return (false, "Davetiye bulunamadı.");
            }

            if (invitation.IsUsed)
            {
                return (false, "Kullanılmış davetiye iptal edilemez.");
            }

            _context.UserInvitations.Remove(invitation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Invitation {InvitationId} cancelled for company {CompanyId}", invitationId, companyId);
            return (true, "Davetiye başarıyla iptal edildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling invitation {InvitationId} for company {CompanyId}", invitationId, companyId);
            return (false, "Davetiye iptal edilirken bir hata oluştu.");
        }
    }

    public async Task<ValidateInvitationResponse> ValidateInvitationTokenAsync(string token)
    {
        try
        {
            var invitation = await _context.UserInvitations
                .Include(i => i.Company)
                .Include(i => i.CreatedByUser)
                .FirstOrDefaultAsync(i => i.InvitationToken == token);

            if (invitation == null)
            {
                return new ValidateInvitationResponse
                {
                    IsValid = false,
                    Message = "Geçersiz davetiye bağlantısı."
                };
            }

            if (invitation.IsUsed)
            {
                return new ValidateInvitationResponse
                {
                    IsValid = false,
                    Message = "Bu davetiye daha önce kullanılmış."
                };
            }

            if (invitation.IsExpired)
            {
                return new ValidateInvitationResponse
                {
                    IsValid = false,
                    Message = "Bu davetiyenin süresi dolmuş. Yeni bir davetiye talep edin."
                };
            }

            return new ValidateInvitationResponse
            {
                IsValid = true,
                CompanyName = invitation.Company.Name,
                InviterName = invitation.CreatedByUser.FullName,
                ExpirationDate = invitation.ExpirationDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating invitation token {Token}", token);
            return new ValidateInvitationResponse
            {
                IsValid = false,
                Message = "Davetiye doğrulanırken bir hata oluştu."
            };
        }
    }

    public async Task<(bool Success, string Message)> AcceptInvitationAsync(AcceptInvitationRequest request)
    {
        // This method will be implemented in the AccountController
        // as it requires UserManager and SignInManager dependencies
        await Task.CompletedTask;
        throw new NotImplementedException("This method should be implemented in AccountController");
    }

    public async Task<bool> CanSendMoreInvitationsAsync(Guid companyId)
    {
        var currentUserCount = await GetCompanyUserCountAsync(companyId);
        var pendingInvitationsCount = await _context.UserInvitations
            .CountAsync(i => i.CompanyId == companyId && !i.IsUsed && i.ExpirationDate > DateTime.Now);

        return (currentUserCount + pendingInvitationsCount) < MaxUsersPerCompany;
    }

    public async Task<int> GetCompanyUserCountAsync(Guid companyId)
    {
        return await _context.Users.CountAsync(u => u.CompanyId == companyId);
    }

    private static string GenerateInvitationToken()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private static string GetTimeRemaining(DateTime expirationDate, bool isUsed)
    {
        if (isUsed) return "Kullanıldı";

        var timeRemaining = expirationDate - DateTime.Now;
        if (timeRemaining.TotalMinutes <= 0) return "Süresi Doldu";

        if (timeRemaining.TotalHours >= 1)
            return $"{(int)timeRemaining.TotalHours} saat {timeRemaining.Minutes} dakika";

        return $"{(int)timeRemaining.TotalMinutes} dakika";
    }

    private static string GetStatusText(bool isUsed, bool isExpired)
    {
        if (isUsed) return "Kullanıldı";
        if (isExpired) return "Süresi Doldu";
        return "Beklemede";
    }

    private static string GetStatusBadgeClass(bool isUsed, bool isExpired)
    {
        if (isUsed) return "bg-green-100 text-green-800";
        if (isExpired) return "bg-red-100 text-red-800";
        return "bg-yellow-100 text-yellow-800";
    }
}
