using Microsoft.AspNetCore.Identity;
using OtpNet;
using QRCoder;
using PushDashboard.Models;
using System.Security.Cryptography;
using System.Text;

namespace PushDashboard.Services;

public interface ITwoFactorService
{
    string GenerateSecretKey();
    string GenerateQrCodeUri(string email, string secretKey, string issuer = "PushDashboard");
    byte[] GenerateQrCodeImage(string qrCodeUri);
    bool ValidateCode(string secretKey, string code);
    Task<bool> EnableTwoFactorAsync(ApplicationUser user, string secretKey);
    Task<bool> DisableTwoFactorAsync(ApplicationUser user);
    Task<bool> VerifyTwoFactorTokenAsync(ApplicationUser user, string code);
}

public class TwoFactorService : ITwoFactorService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<TwoFactorService> _logger;

    public TwoFactorService(UserManager<ApplicationUser> userManager, ILogger<TwoFactorService> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public string GenerateSecretKey()
    {
        var key = KeyGeneration.GenerateRandomKey(20);
        return Base32Encoding.ToString(key);
    }

    public string GenerateQrCodeUri(string email, string secretKey, string issuer = "PushDashboard")
    {
        return $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={secretKey}&issuer={Uri.EscapeDataString(issuer)}";
    }

    public byte[] GenerateQrCodeImage(string qrCodeUri)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(qrCodeUri, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(qrCodeData);
        return qrCode.GetGraphic(20);
    }

    public bool ValidateCode(string secretKey, string code)
    {
        try
        {
            var secretKeyBytes = Base32Encoding.ToBytes(secretKey);
            var totp = new Totp(secretKeyBytes);
            
            // Allow for time drift (check current, previous, and next time windows)
            var currentTime = DateTime.Now;
            
            // Check current window
            if (totp.VerifyTotp(code, out long timeStepMatched, VerificationWindow.RfcSpecifiedNetworkDelay))
            {
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating TOTP code");
            return false;
        }
    }

    public async Task<bool> EnableTwoFactorAsync(ApplicationUser user, string secretKey)
    {
        try
        {
            user.TwoFactorSecretKey = secretKey;
            user.TwoFactorEnabled = true;
            
            var result = await _userManager.UpdateAsync(user);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("Two-factor authentication enabled for user {UserId}", user.Id);
                return true;
            }
            
            _logger.LogWarning("Failed to enable two-factor authentication for user {UserId}: {Errors}", 
                user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling two-factor authentication for user {UserId}", user.Id);
            return false;
        }
    }

    public async Task<bool> DisableTwoFactorAsync(ApplicationUser user)
    {
        try
        {
            user.TwoFactorSecretKey = null;
            user.TwoFactorEnabled = false;
            
            var result = await _userManager.UpdateAsync(user);
            
            if (result.Succeeded)
            {
                _logger.LogInformation("Two-factor authentication disabled for user {UserId}", user.Id);
                return true;
            }
            
            _logger.LogWarning("Failed to disable two-factor authentication for user {UserId}: {Errors}", 
                user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling two-factor authentication for user {UserId}", user.Id);
            return false;
        }
    }

    public async Task<bool> VerifyTwoFactorTokenAsync(ApplicationUser user, string code)
    {
        if (string.IsNullOrEmpty(user.TwoFactorSecretKey))
        {
            return false;
        }

        return ValidateCode(user.TwoFactorSecretKey, code);
    }
}
