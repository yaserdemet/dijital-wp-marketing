using Microsoft.EntityFrameworkCore;
using Npgsql;
using PushDashboard.Data;
using PushDashboard.Helpers;
using PushDashboard.Models;

namespace PushDashboard.Services;

/// <summary>
/// Referans kodu oluşturma ve kaydetme işlemlerini yöneten servis.
/// </summary>
public interface IReferralCodeService
{
    Task<bool> AssignReferralCodeAsync(ApplicationUser user, CancellationToken cancellationToken = default);
}

public class ReferralCodeService : IReferralCodeService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReferralCodeService> _logger;
    private const int MaxRetryAttempts = 10;

    public ReferralCodeService(
        ApplicationDbContext context,
        ILogger<ReferralCodeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> AssignReferralCodeAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (!string.IsNullOrEmpty(user.ReferralCode))
        {
            _logger.LogWarning("User {UserId} already has a referral code: {ReferralCode}", user.Id, user.ReferralCode);
            return true; 
        }

        int attempt = 0;

        while (attempt < MaxRetryAttempts)
        {
            attempt++;

            try
            {
                var referralCode = ReferralCodeGenerator.Generate();

                user.ReferralCode = referralCode;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Referral code {ReferralCode} assigned to user {UserId} on attempt {Attempt}",
                    referralCode, user.Id, attempt);

                return true;
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                _logger.LogWarning(
                    "Referral code collision detected for user {UserId} on attempt {Attempt}. Retrying...",
                    user.Id, attempt);

                user.ReferralCode = null;

                if (attempt >= MaxRetryAttempts)
                {
                    _logger.LogError(
                        "Failed to assign unique referral code to user {UserId} after {MaxAttempts} attempts",
                        user.Id, MaxRetryAttempts);
                    return false;
                }

                await Task.Delay(10, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error while assigning referral code to user {UserId} on attempt {Attempt}",
                    user.Id, attempt);

                user.ReferralCode = null;
                return false;
            }
        }

        _logger.LogError(
            "Failed to assign unique referral code to user {UserId} after {MaxAttempts} attempts",
            user.Id, MaxRetryAttempts);
        
        return false;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        if (exception?.InnerException is PostgresException postgresException)
        {
            return postgresException.SqlState == "23505";
        }

        var message = exception?.InnerException?.Message.ToUpperInvariant() ?? string.Empty;
        return message.Contains("UNIQUE") ||
               message.Contains("DUPLICATE KEY") ||
               message.Contains("VIOLATION OF UNIQUE");
    }
}
