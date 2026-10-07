using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Helpers;
using PushDashboard.Models;
using PushDashboard.ViewModels;
using PushDashboard.Services;
using Microsoft.AspNetCore.SignalR;
using PushDashboard.Hubs;
using System.Security.Claims;
using PushDashboard.Filters;

namespace PushDashboard.Controllers;

public class SettingsController : BaseController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SettingsController> _logger;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IHubContext<SessionHub> _hubContext;
    private readonly IInvitationService _invitationService;
    private readonly IActivityLogService _activityLogService;
    private readonly ISessionService _sessionService;

    private readonly IPayTRService _payTRService;
    private readonly ICreditTransactionService _creditTransactionService;

    public SettingsController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext context,
        ITwoFactorService twoFactorService,
        ILogger<SettingsController> logger,
        IInvitationService invitationService,
        IHubContext<SessionHub> hubContext,
        IUserContextService userContextService,
        IActivityLogService activityLogService,
        ISessionService sessionService,
        IPayTRService payTRService,
        ICreditTransactionService creditTransactionService)
        : base(userContextService, context)
    {
        _userManager = userManager;
        _context = context;
        _logger = logger;
        _twoFactorService = twoFactorService;
        _invitationService = invitationService;
        _hubContext = hubContext;
        _activityLogService = activityLogService;
        _sessionService = sessionService;
        _payTRService = payTRService;
        _creditTransactionService = creditTransactionService;
    }

    private async Task<Company?> GetEffectiveCompanyAsync()
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return null;
        }

        return await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId.Value);
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return NotFound();
        }

        var userWithPreferences = await _context.Users
            .Include(u => u.NotificationPreferences)
            .FirstOrDefaultAsync(u => u.Id == userId);
        var company = await GetEffectiveCompanyAsync();

        if (userWithPreferences == null)
        {
            return NotFound();
        }

        var canEditCompany = HasCompanyOwnerAccess();
        var canManageUsers = HasCompanyOwnerAccess();

        var model = new SettingsViewModel
        {
            UserProfile = new UserProfileViewModel
            {
                FirstName = userWithPreferences.FirstName,
                LastName = userWithPreferences.LastName,
                Email = userWithPreferences.Email,
                PhoneNumber = userWithPreferences.PhoneNumber
            },
            CompanyProfile = new CompanyProfileViewModel
            {
                Name = company?.Name ?? "",
                Address = company?.Address,
                Phone = company?.Phone,
                Email = company?.Email,
                Website = company?.Website
            },
            CanEditCompany = canEditCompany,
            CanManageUsers = canManageUsers,
            TwoFactorStatus = new TwoFactorStatusViewModel
            {
                IsEnabled = userWithPreferences.TwoFactorEnabled,
                HasRecoveryCodes = false,
                RecoveryCodesLeft = 0,
                IsMachineRemembered = false
            },
            NotificationPreferences = new NotificationPreferencesViewModel
            {
                EmailInvoiceNotifications = userWithPreferences.NotificationPreferences?.EmailInvoiceNotifications ?? true,
                EmailCreditNotifications = userWithPreferences.NotificationPreferences?.EmailCreditNotifications ?? true,
                EmailMarketingNotifications = userWithPreferences.NotificationPreferences?.EmailMarketingNotifications ?? false,
                SmsSecurityAlerts = userWithPreferences.NotificationPreferences?.SmsSecurityAlerts ?? true,
                SmsPaymentNotifications = userWithPreferences.NotificationPreferences?.SmsPaymentNotifications ?? false
            },
            CompanyName = company?.Name,
            BillingInformation = new BillingInformationViewModel
            {
                BillingType = company?.BillingType ?? "Corporate",
                TaxOffice = company?.TaxOffice,
                TaxNumber = company?.TaxNumber,
                IdentityNumber = company?.IdentityNumber,
                BillingAddress = company?.BillingAddress ?? "",
                CompanyName = company?.CompanyName,
                FullName = company?.FullName
            }
        };

        return View(model);
    }

    // AJAX Endpoints
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfileAjax([FromBody] UserProfileViewModel model)
    {
        _logger.LogInformation("UpdateProfileAjax called with model: FirstName={FirstName}, LastName={LastName}, PhoneNumber={PhoneNumber}",
            model.FirstName,
            model.LastName,
            model.PhoneNumber);

        // Manual validation
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(model.FirstName) || model.FirstName.Length < 2 || model.FirstName.Length > 100)
        {
            errors.Add("Ad en az 2, en fazla 100 karakter olmalıdır.");
        }
        else if (System.Text.RegularExpressions.Regex.IsMatch(model.FirstName, "[0-9]"))
        {
            errors.Add("Ad alanı sayı içeremez.");
        }

        if (string.IsNullOrWhiteSpace(model.LastName) || model.LastName.Length < 2 || model.LastName.Length > 100)
        {
            errors.Add("Soyad en az 2, en fazla 100 karakter olmalıdır.");
        }
        else if (System.Text.RegularExpressions.Regex.IsMatch(model.LastName, "[0-9]"))
        {
            errors.Add("Soyad alanı sayı içeremez.");
        }

        if (string.IsNullOrWhiteSpace(model.Email) || !IsValidEmail(model.Email))
        {
            errors.Add("Geçerli bir e-posta adresi giriniz.");
        }

        if (errors.Any())
        {
            return Json(ApiResponse.ValidationError(errors));
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Json(ApiResponse.UserNotFound);
        }

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.PhoneNumber = model.PhoneNumber;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            _logger.LogInformation("Profile updated successfully for user {UserId}", user.Id);

            // Activity Log - Profile Update
            if (user.CompanyId.HasValue)
            {
                await _activityLogService.LogActivityAsync(
                    user.Id,
                    user.CompanyId.Value,
                    ActivityActionTypes.Update,
                    ActivityModules.Settings,
                    $"{user.FirstName} {user.LastName} profil bilgilerini güncelledi",
                    entityId: user.Id,
                    entityType: "User",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString());
            }

            return Json(ApiResponse.SuccessMessage("Profil bilgileriniz başarıyla güncellendi."));
        }
        else
        {
            var errorMessages = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to update profile for user {UserId}: {Errors}", user.Id, errorMessages);
            return Json(new { success = false, message = $"Profil güncellenirken bir hata oluştu: {errorMessages}" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [CompanyOwnerOrSuperAdmin]
    public async Task<IActionResult> UpdateCompanyAjax([FromBody] CompanyProfileViewModel model)
    {
        _logger.LogInformation("UpdateCompanyAjax called with model: Name={Name}", model.Name);

        // Manual validation
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(model.Name) || model.Name.Length < 2 || model.Name.Length > 100)
        {
            errors.Add("Firma adı en az 2, en fazla 100 karakter olmalıdır.");
        }

        if (!string.IsNullOrWhiteSpace(model.Email) && !IsValidEmail(model.Email))
        {
            errors.Add("Geçerli bir e-posta adresi giriniz.");
        }

        if (!string.IsNullOrWhiteSpace(model.Website) && !IsValidWebsite(model.Website))
        {
            errors.Add("Geçerli bir website adresi giriniz (http:// veya https:// ile başlamalı).");
        }

        if (errors.Any())
        {
            return Json(ApiResponse.ValidationError(errors));
        }

        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Json(ApiResponse.UserNotFound);
        }

        var company = await GetEffectiveCompanyAsync();

        if (company == null)
        {
            return Json(ApiResponse.FirmNotFound);
        }

        // Check if company name is being changed and if it already exists
        if (company.Name != model.Name)
        {
            var existingCompany = await _context.Companies
                .FirstOrDefaultAsync(c => c.Name.ToLower() == model.Name.ToLower() && c.Id != company.Id);

            if (existingCompany != null)
            {
                return Json(ApiResponse.Error("Bu firma adı zaten kullanılmaktadır."));
            }
        }

        // Update company information
        company.Name = model.Name;
        company.Address = model.Address;
        company.Phone = model.Phone;
        company.Email = model.Email;
        company.Website = model.Website;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Company updated successfully for user {UserId}", userId);

        // Activity Log - Company Update
        await _activityLogService.LogActivityAsync(
            userId,
            company.Id,
            ActivityActionTypes.Update,
            ActivityModules.Settings,
            $"Firma bilgileri güncellendi: {model.Name}",
            entityId: company.Id.ToString(),
            entityType: "Company",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString());

        return Json(ApiResponse.SuccessMessage("Firma bilgileri başarıyla güncellendi."));
    }

    // Legacy form endpoints (keep for backward compatibility)
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(SettingsViewModel model)
    {
        _logger.LogInformation("UpdateProfile called with model: FirstName={FirstName}, LastName={LastName}, PhoneNumber={PhoneNumber}",
            model.UserProfile.FirstName,
            model.UserProfile.LastName,
            model.UserProfile.PhoneNumber);

        // Sadece UserProfile kısmını validate et
        var userProfileErrors = ModelState
            .Where(x => x.Key.StartsWith("UserProfile."))
            .SelectMany(x => x.Value.Errors)
            .Select(x => x.ErrorMessage)
            .ToList();

        if (userProfileErrors.Any())
        {
            var errors = string.Join(", ", userProfileErrors);
            _logger.LogWarning("UserProfile validation failed: {Errors}", errors);
            TempData["ErrorMessage"] = $"Lütfen profil alanlarını doğru şekilde doldurun: {errors}";

            // Rebuild the complete SettingsViewModel with the user's input preserved
            var settingsModel = await BuildSettingsViewModelWithInput(model.UserProfile, null);
            return View("Index", settingsModel);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            _logger.LogWarning("User not found");
            return NotFound();
        }

        _logger.LogInformation("Updating user {UserId}: {FirstName} -> {NewFirstName}, {LastName} -> {NewLastName}, {PhoneNumber} -> {NewPhoneNumber}",
            user.Id,
            user.FirstName,
            model.UserProfile.FirstName,
            user.LastName,
            model.UserProfile.LastName,
            user.PhoneNumber,
            model.UserProfile.PhoneNumber);

        user.FirstName = model.UserProfile.FirstName;
        user.LastName = model.UserProfile.LastName;
        user.PhoneNumber = model.UserProfile.PhoneNumber;

        var result = await _userManager.UpdateAsync(user);

        if (result.Succeeded)
        {
            _logger.LogInformation("Profile updated successfully for user {UserId}", user.Id);
            TempData["SuccessMessage"] = "Profil bilgileriniz başarıyla güncellendi.";
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to update profile for user {UserId}: {Errors}", user.Id, errors);
            TempData["ErrorMessage"] = $"Profil güncellenirken bir hata oluştu: {errors}";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [CompanyOwnerOrSuperAdmin]
    public async Task<IActionResult> UpdateCompany(SettingsViewModel model)
    {
        // Sadece CompanyProfile kısmını validate et
        var companyProfileErrors = ModelState
            .Where(x => x.Key.StartsWith("CompanyProfile."))
            .SelectMany(x => x.Value.Errors)
            .Select(x => x.ErrorMessage)
            .ToList();

        // Custom validation for optional fields
        var customErrors = new List<string>();

        if (!model.CompanyProfile.IsValidEmail())
        {
            customErrors.Add("Geçerli bir e-posta adresi giriniz.");
        }

        if (!model.CompanyProfile.IsValidWebsite())
        {
            customErrors.Add("Geçerli bir website adresi giriniz (http:// veya https:// ile başlamalı).");
        }

        if (companyProfileErrors.Any() || customErrors.Any())
        {
            var allErrors = companyProfileErrors.Concat(customErrors);
            var errors = string.Join(", ", allErrors);
            _logger.LogWarning("CompanyProfile validation failed: {Errors}", errors);
            TempData["ErrorMessage"] = $"Lütfen firma alanlarını doğru şekilde doldurun: {errors}";

            // Rebuild the complete SettingsViewModel with the company input preserved
            var settingsModel = await BuildSettingsViewModelWithInput(null, model.CompanyProfile);
            return View("Index", settingsModel);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        var company = await GetEffectiveCompanyAsync();

        if (company == null)
        {
            TempData["ErrorMessage"] = "Firma bilgisi bulunamadı.";
            var settingsModel = await BuildSettingsViewModelWithInput(null, model.CompanyProfile);
            return View("Index", settingsModel);
        }

        // Check if company name is being changed and if it already exists
        if (company.Name != model.CompanyProfile.Name)
        {
            var existingCompany = await _context.Companies
                .FirstOrDefaultAsync(c => c.Name.ToLower() == model.CompanyProfile.Name.ToLower() && c.Id != company.Id);

            if (existingCompany != null)
            {
                TempData["ErrorMessage"] = "Bu firma adı zaten kullanılmaktadır.";
                var settingsModel = await BuildSettingsViewModelWithInput(null, model.CompanyProfile);
                return View("Index", settingsModel);
            }
        }

        // Update company information
        company.Name = model.CompanyProfile.Name;
        company.Address = model.CompanyProfile.Address;
        company.Phone = model.CompanyProfile.Phone;
        company.Email = model.CompanyProfile.Email;
        company.Website = model.CompanyProfile.Website;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Firma bilgileri başarıyla güncellendi.";

        return RedirectToAction(nameof(Index));
    }

    /// <summary>
    /// Helper method to build SettingsViewModel while preserving user input from failed form submissions
    /// </summary>
    private async Task<SettingsViewModel> BuildSettingsViewModelWithInput(
        UserProfileViewModel? userProfileInput,
        CompanyProfileViewModel? companyProfileInput)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            throw new InvalidOperationException("User not found");
        }

        var userWithPreferences = await _context.Users
            .Include(u => u.NotificationPreferences)
            .FirstOrDefaultAsync(u => u.Id == userId);
        var company = await GetEffectiveCompanyAsync();

        if (userWithPreferences == null)
        {
            throw new InvalidOperationException("User not found in database");
        }

        var canEditCompany = HasCompanyOwnerAccess();
        var canManageUsers = HasCompanyOwnerAccess();

        var model = new SettingsViewModel
        {
            // Use user input if provided, otherwise use database values
            UserProfile = userProfileInput ?? new UserProfileViewModel
            {
                FirstName = userWithPreferences.FirstName,
                LastName = userWithPreferences.LastName,
                Email = userWithPreferences.Email,
                PhoneNumber = userWithPreferences.PhoneNumber
            },
            // Use company input if provided, otherwise use database values
            CompanyProfile = companyProfileInput ?? new CompanyProfileViewModel
            {
                Name = company?.Name ?? "",
                Address = company?.Address,
                Phone = company?.Phone,
                Email = company?.Email,
                Website = company?.Website
            },
            CanEditCompany = canEditCompany,
            CanManageUsers = canManageUsers,
            TwoFactorStatus = new TwoFactorStatusViewModel
            {
                IsEnabled = userWithPreferences.TwoFactorEnabled,
                HasRecoveryCodes = false,
                RecoveryCodesLeft = 0,
                IsMachineRemembered = false
            },
            NotificationPreferences = new NotificationPreferencesViewModel
            {
                EmailInvoiceNotifications = userWithPreferences.NotificationPreferences?.EmailInvoiceNotifications ?? true,
                EmailCreditNotifications = userWithPreferences.NotificationPreferences?.EmailCreditNotifications ?? true,
                EmailMarketingNotifications = userWithPreferences.NotificationPreferences?.EmailMarketingNotifications ?? false,
                SmsSecurityAlerts = userWithPreferences.NotificationPreferences?.SmsSecurityAlerts ?? true,
                SmsPaymentNotifications = userWithPreferences.NotificationPreferences?.SmsPaymentNotifications ?? false
            },
            CompanyName = company?.Name,
            BillingInformation = new BillingInformationViewModel
            {
                BillingType = company?.BillingType ?? "Corporate",
                TaxOffice = company?.TaxOffice,
                TaxNumber = company?.TaxNumber,
                IdentityNumber = company?.IdentityNumber,
                BillingAddress = company?.BillingAddress ?? "",
                CompanyName = company?.CompanyName,
                FullName = company?.FullName
            }
        };

        return model;
    }

    // Helper methods for validation
    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    // Two Factor Authentication Endpoints
    [HttpGet]
    public async Task<IActionResult> GetTwoFactorStatus()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Json(ApiResponse.UserNotFound);
        }

        var status = new TwoFactorStatusViewModel
        {
            IsEnabled = user.TwoFactorEnabled,
            HasRecoveryCodes = false, // TODO: Implement recovery codes
            RecoveryCodesLeft = 0,
            IsMachineRemembered = false // TODO: Implement machine remembering
        };

        return Json(new { success = true, data = status });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetupTwoFactor()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Json(ApiResponse.UserNotFound);
        }

        if (user.TwoFactorEnabled)
        {
            return Json(ApiResponse.Error("İki faktörlü doğrulama zaten etkin."));
        }

        var secretKey = _twoFactorService.GenerateSecretKey();
        var qrCodeUri = _twoFactorService.GenerateQrCodeUri(user.Email!, secretKey);

        var setupModel = new TwoFactorSetupViewModel
        {
            QrCodeUri = qrCodeUri,
            ManualEntryKey = secretKey,
            IsEnabled = false
        };

        // Store the secret key temporarily in TempData
        TempData["TempSecretKey"] = secretKey;

        return Json(new { success = true, data = setupModel });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnableTwoFactor([FromBody] TwoFactorSetupViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(ApiResponse.ValidationError(errors));
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Json(ApiResponse.UserNotFound);
        }

        var secretKey = TempData["TempSecretKey"] as string;
        if (string.IsNullOrEmpty(secretKey))
        {
            return Json(ApiResponse.Error("Kurulum süresi doldu. Lütfen tekrar deneyin."));
        }

        // Verify the code before enabling
        if (!_twoFactorService.ValidateCode(secretKey, model.VerificationCode))
        {
            TempData["TempSecretKey"] = secretKey; // Keep the secret key for retry
            return Json(ApiResponse.Error("Geçersiz doğrulama kodu. Lütfen tekrar deneyin."));
        }

        // Enable 2FA
        var result = await _twoFactorService.EnableTwoFactorAsync(user, secretKey);
        if (!result)
        {
            return Json(ApiResponse.Error("İki faktörlü doğrulama etkinleştirilemedi."));
        }

        TempData.Remove("TempSecretKey");

        // Activity Log - Enable 2FA
        if (user.CompanyId.HasValue)
        {
            await _activityLogService.LogActivityAsync(
                user.Id,
                user.CompanyId.Value,
                ActivityActionTypes.Activate,
                ActivityModules.Settings,
                "İki faktörlü doğrulama etkinleştirildi",
                entityId: user.Id,
                entityType: "User",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        return Json(ApiResponse.SuccessMessage("İki faktörlü doğrulama başarıyla etkinleştirildi."));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DisableTwoFactor()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Json(ApiResponse.UserNotFound);
        }

        if (!user.TwoFactorEnabled)
        {
            return Json(ApiResponse.Error("İki faktörlü doğrulama zaten devre dışı."));
        }

        var result = await _twoFactorService.DisableTwoFactorAsync(user);
        if (!result)
        {
            return Json(ApiResponse.Error("İki faktörlü doğrulama devre dışı bırakılamadı."));
        }

        // Activity Log - Disable 2FA
        if (user.CompanyId.HasValue)
        {
            await _activityLogService.LogActivityAsync(
                user.Id,
                user.CompanyId.Value,
                ActivityActionTypes.Deactivate,
                ActivityModules.Settings,
                "İki faktörlü doğrulama devre dışı bırakıldı",
                entityId: user.Id,
                entityType: "User",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        return Json(ApiResponse.SuccessMessage("İki faktörlü doğrulama başarıyla devre dışı bırakıldı."));
    }

    [HttpGet]
    public IActionResult GetQrCode(string qrCodeUri)
    {
        try
        {
            if (string.IsNullOrEmpty(qrCodeUri))
            {
                return BadRequest("QR kod URI gerekli.");
            }

            var qrCodeImage = _twoFactorService.GenerateQrCodeImage(qrCodeUri);
            return File(qrCodeImage, "image/png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code");
            return BadRequest("QR kod oluşturulamadı.");
        }
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateNotificationPreferencesAjax([FromBody] NotificationPreferencesViewModel model)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Json(new { success = false, message = "Kullanıcı bulunamadı." });
        }

        var userWithPreferences = await _context.Users
            .Include(u => u.NotificationPreferences)
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        if (userWithPreferences == null)
        {
            return Json(new { success = false, message = "Kullanıcı bulunamadı." });
        }

        // Create notification preferences if they do not exist
        if (userWithPreferences.NotificationPreferences == null)
        {
            userWithPreferences.NotificationPreferences = new NotificationPreferences
            {
                UserId = user.Id,
                CreatedAt = DateTime.Now
            };

            _context.NotificationPreferences.Add(userWithPreferences.NotificationPreferences);
        }

        // Update notification preferences
        userWithPreferences.NotificationPreferences.EmailInvoiceNotifications = model.EmailInvoiceNotifications;
        userWithPreferences.NotificationPreferences.EmailCreditNotifications = model.EmailCreditNotifications;
        userWithPreferences.NotificationPreferences.EmailMarketingNotifications = model.EmailMarketingNotifications;
        userWithPreferences.NotificationPreferences.SmsSecurityAlerts = model.SmsSecurityAlerts;
        userWithPreferences.NotificationPreferences.SmsPaymentNotifications = model.SmsPaymentNotifications;
        userWithPreferences.NotificationPreferences.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Notification preferences updated successfully for user {UserId}", user.Id);
        return Json(ApiResponse.SuccessMessage("Bildirim tercihleri başarıyla güncellendi."));
    }

    private static bool IsValidWebsite(string website)
    {
        if (string.IsNullOrWhiteSpace(website))
            return true; // Empty is valid for optional field

        return Uri.TryCreate(website, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }

    // User Management Methods
    [HttpGet]
    [CompanyOwnerOrSuperAdmin]
    public async Task<IActionResult> GetCompanyUsersAjax()
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(new { success = false, message = "Firma bilgisi bulunamadı." });
        }

        var companyUsers = await _context.Users
            .Where(u => u.CompanyId == companyId.Value)
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Select(u => new UserListItemViewModel
            {
                Id = u.Id,
                FirstName = u.FirstName ?? "",
                LastName = u.LastName ?? "",
                Email = u.Email ?? "",
                PhoneNumber = u.PhoneNumber,
                IsActive = !u.LockoutEnd.HasValue || u.LockoutEnd <= DateTimeOffset.UtcNow,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })
            .ToListAsync();



        var result = new UserManagementViewModel
        {
            Users = companyUsers,
            TotalUsers = companyUsers.Count
        };

        return Json(new { success = true, data = result });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [CompanyOwnerOrSuperAdmin]
    public async Task<IActionResult> DeleteUserAjax([FromBody] DeleteUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = string.Join(" ", errors) });
        }

        var currentUser = await GetCurrentUserWithCompanyAsync();
        if (currentUser == null)
        {
            return Json(new { success = false, message = "Kullanıcı bulunamadı." });
        }

        // Prevent self-deletion
        if (currentUser.Id == request.UserId)
        {
            return Json(ApiResponse.Error("Kendi hesabınızı silemezsiniz."));
        }

        var userToDelete = await _context.Users
            .Include(u => u.Company)
            .Include(u => u.NotificationPreferences)
            .FirstOrDefaultAsync(u => u.Id == request.UserId);

        if (userToDelete == null)
        {
            return Json(ApiResponse.Error("Silinecek kullanıcı bulunamadı."));
        }

        // Ensure the user belongs to the same company
        if (userToDelete.CompanyId != currentUser.CompanyId)
        {
            return Json(ApiResponse.Error("Bu kullanıcıyı silme yetkiniz bulunmamaktadır."));
        }

        var result = await _userManager.DeleteAsync(userToDelete);
        if (result.Succeeded)
        {
            _logger.LogInformation("User {UserId} deleted by {CurrentUserId}", request.UserId, currentUser.Id);

            // Activity Log - Delete User
            if (currentUser.CompanyId.HasValue)
            {
                await _activityLogService.LogActivityAsync(
                    currentUser.Id,
                    currentUser.CompanyId.Value,
                    ActivityActionTypes.Delete,
                    ActivityModules.Settings,
                    $"Kullanıcı silindi: {userToDelete.FirstName} {userToDelete.LastName}",
                    entityId: request.UserId,
                    entityType: "User",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString());
            }

            return Json(ApiResponse.SuccessMessage("Kullanıcı başarıyla silindi."));
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Json(new { success = false, message = $"Kullanıcı silinirken hata oluştu: {errors}" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [CompanyOwnerOrSuperAdmin]
    public async Task<IActionResult> DeactivateUserAjax([FromBody] DeactivateUserRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = string.Join(" ", errors) });
        }

        var currentUser = await GetCurrentUserWithCompanyAsync();
        if (currentUser == null)
        {
            return Json(new { success = false, message = "Kullanıcı bulunamadı." });
        }

        // Prevent self-deactivation
        if (currentUser.Id == request.UserId)
        {
            return Json(ApiResponse.Error("Kendi hesabınızı deaktive edemezsiniz."));
        }

        var userToUpdate = await _context.Users
            .Include(u => u.Company)
            .Include(u => u.NotificationPreferences)
            .FirstOrDefaultAsync(u => u.Id == request.UserId);

        if (userToUpdate == null)
        {
            return Json(new { success = false, message = "Kullanıcı bulunamadı." });
        }

        // Ensure the user belongs to the same company
        if (userToUpdate.CompanyId != currentUser.CompanyId)
        {
            return Json(ApiResponse.Error("Bu kullanıcıyı düzenleme yetkiniz bulunmamaktadır."));
        }

        if (request.Deactivate)
        {
            // Deactivate user by setting lockout end to far future
            userToUpdate.LockoutEnd = DateTimeOffset.MaxValue;
            userToUpdate.LockoutEnabled = false;
        }
        else
        {
            // Reactivate user by removing lockout
            userToUpdate.LockoutEnd = null;
            userToUpdate.LockoutEnabled = false;
        }

        var result = await _userManager.UpdateAsync(userToUpdate);
        if (result.Succeeded)
        {
            var action = request.Deactivate ? "deaktive edildi" : "aktive edildi";
            _logger.LogInformation("User {UserId} {Action} by {CurrentUserId}", request.UserId, action, currentUser.Id);
            return Json(new { success = true, message = $"Kullanıcı başarıyla {action}." });
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Json(new { success = false, message = $"Kullanıcı durumu güncellenirken hata oluştu: {errors}" });
        }
    }


    [HttpGet]
    public async Task<IActionResult> GetActiveDevicesAjax()
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Json(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            var deviceIdCookie = HttpContext.Request.Cookies["pushonice_device_id"];
            var deviceId = !string.IsNullOrEmpty(deviceIdCookie) ? deviceIdCookie : "unknown";
            
            var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();
            var parser = UAParser.Parser.GetDefault();
            var clientInfo = parser.Parse(userAgent);
            var os = $"{clientInfo.OS.Family} {clientInfo.OS.Major}";
            var browserFamily = clientInfo.UA.Family;
            var fingerprint = _sessionService.GetDeviceFingerprint(user.Id, os, browserFamily, deviceId);
            
            var currentDevice = await _context.UserDevices
                .FirstOrDefaultAsync(d => d.DeviceFingerprint == fingerprint && d.UserId == user.Id);
            
            var currentDeviceId = currentDevice?.Id;
            var devices = await _sessionService.GetActiveDevicesAsync(user.Id, currentDeviceId, includeLocalhost: false, includeIncognito: false);



            var result = new ActiveDevicesViewModel
            {
                Devices = devices,
                TotalDevices = devices.Count,
                ActiveDevices = devices.Count(d => !d.IsInactive)
            };

            return Json(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active devices for user");
            return Json(new { success = false, message = $"Cihazlar yüklenirken bir hata oluştu: {ex.Message}" });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TerminateDeviceAjax([FromBody] TerminateDeviceRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = string.Join(" ", errors) });
        }

        var user = await GetCurrentUserWithCompanyAsync();
        if (user == null)
        {
            return Json(new { success = false, message = "Kullanıcı bulunamadı." });
        }

        var currentDevice = await _sessionService.GetOrCreateDeviceAsync(user.Id, HttpContext);
        if (request.DeviceId == currentDevice.Id)
        {
            return Json(ApiResponse.Error("Mevcut cihazınızı sonlandıramazsınız."));
        }

        var result = await _sessionService.TerminateDeviceAsync(user.Id, request.DeviceId);
        if (!result)
        {
            return Json(ApiResponse.Error("Cihaz bulunamadı."));
        }

        return Json(ApiResponse.SuccessMessage("Cihaz başarıyla sonlandırıldı."));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TerminateAllOtherDevicesAjax()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Json(new { success = false, message = "Kullanıcı bulunamadı." });
        }

        var currentDevice = await _sessionService.GetOrCreateDeviceAsync(user.Id, HttpContext);
        var terminatedCount = await _sessionService.TerminateAllOtherDevicesAsync(user.Id, currentDevice.Id);

        _logger.LogInformation("Terminated {Count} other devices for user {UserId}", terminatedCount, user.Id);

        await _hubContext.Clients.Group($"user_{user.Id}")
            .SendAsync("AllOtherSessionsTerminated", new { count = terminatedCount, message = $"{terminatedCount} cihaz başka bir cihazdan sonlandırıldı." });

        return Json(new { success = true, message = $"{terminatedCount} cihaz başarıyla sonlandırıldı." });
    }

    // Billing Information Management Methods
    [HttpGet]
    [CompanyOwnerOrSuperAdmin]
    public async Task<IActionResult> GetBillingInformationAjax()
    {
        var company = await GetEffectiveCompanyAsync();
        if (company == null)
        {
            return Json(new { success = false, message = "Firma bilgisi bulunamadı." });
        }

        var billingInfo = new BillingInformationViewModel
        {
            BillingType = company.BillingType ?? "Corporate",
            TaxOffice = company.TaxOffice,
            TaxNumber = company.TaxNumber,
            IdentityNumber = company.IdentityNumber,
            BillingAddress = company.BillingAddress ?? "",
            CompanyName = company.CompanyName,
            FullName = company.FullName
        };

        return Json(new { success = true, data = billingInfo });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [CompanyOwnerOrSuperAdmin]
    public async Task<IActionResult> UpdateBillingInformationAjax(BillingInformationViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = string.Join(" ", errors) });
        }

        _logger.LogInformation("Received billing data: BillingType={BillingType}, BillingAddress={BillingAddress}",
            model.BillingType,
            model.BillingAddress);

        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
        {
            return Json(new { success = false, message = "Kullanıcı bulunamadı." });
        }

        var company = await GetEffectiveCompanyAsync();
        if (company == null)
        {
            return Json(new { success = false, message = "Firma bilgisi bulunamadı." });
        }

        company.BillingType = model.BillingType;
        company.TaxOffice = model.TaxOffice;
        company.TaxNumber = model.TaxNumber;
        company.IdentityNumber = model.IdentityNumber;
        company.BillingAddress = model.BillingAddress;
        company.CompanyName = model.CompanyName;
        company.FullName = model.FullName;
        company.BillingUpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Billing information updated successfully for company {CompanyId} by user {UserId}",
            company.Id,
            userId);

        // Activity Log - Billing Update
        await _activityLogService.LogActivityAsync(
            userId,
            company.Id,
            ActivityActionTypes.Update,
            ActivityModules.Billing,
            "Fatura bilgileri güncellendi",
            entityId: company.Id.ToString(),
            entityType: "Company",
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString());

        return Json(ApiResponse.SuccessMessage("Fatura bilgileri başarıyla güncellendi."));
    }

    // User Invitation Management Methods
    [HttpGet]
    [CompanyOwnerOrSuperAdmin]
    public async Task<IActionResult> GetCompanyInvitationsAjax()
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(new { success = false, message = "Firma bilgisi bulunamadı." });
        }

        var invitations = await _invitationService.GetCompanyInvitationsAsync(companyId.Value);
        return Json(new { success = true, data = invitations });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [CompanyOwnerOrSuperAdmin]
    public async Task<IActionResult> SendInvitationAjax([FromBody] SendInvitationRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = string.Join(" ", errors) });
        }

        var user = GetCurrentUser();
        var companyId = GetCurrentUserCompanyId();
        if (user == null || !companyId.HasValue)
        {
            return Json(new { success = false, message = "Firma bilgisi bulunamadı." });
        }

        var (success, message) = await _invitationService.SendInvitationAsync(
            companyId.Value,
            request.Email,
            user.Id,
            user.FullName);

        // Activity Log - Send Invitation
        if (success)
        {
            await _activityLogService.LogActivityAsync(
                user.Id,
                companyId.Value,
                ActivityActionTypes.Send,
                ActivityModules.Settings,
                $"Kullanıcı davetiyesi gönderildi: {request.Email}",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        return Json(new { success, message });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [CompanyOwnerOrSuperAdmin]
    public async Task<IActionResult> CancelInvitationAjax([FromBody] CancelInvitationRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = string.Join(" ", errors) });
        }

        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return Json(new { success = false, message = "Firma bilgisi bulunamadı." });
        }

        var (success, message) = await _invitationService.CancelInvitationAsync(request.InvitationId, companyId.Value);
        return Json(new { success, message });
    }

    [HttpGet]
    public async Task<IActionResult> GetActiveChannels()
    {
        var companyId = GetCurrentUserCompanyId();
        if (companyId == null)
        {
            return Json(ApiResponse.CompanyNotFound);
        }

        // SMTP ayarlarını kontrol et
        var companyIntegrations = await _context.CompanyIntegrations
            .Include(ci => ci.Integration)
            .Where(ci => ci.CompanyId == companyId &&
                         ci.IsActive &&
                         ci.IsConfigured &&
                         ci.Integration.Type == "communication")
            .ToListAsync();

        bool whatsappActive = false;
        bool emailActive = false;
        bool smsActive = false;
        foreach (var ci in companyIntegrations)
        {
            if (ci.Integration.Name == "WhatsApp")
                whatsappActive = true;

            if (ci.Integration.Name == "Email SMTP")
                emailActive = true;

            if (ci.Integration.Name == "SMS")
                smsActive = true;
        }

        return Json(new
        {
            success = true,
            channels = new
            {
                email = emailActive,
                sms = smsActive,
                whatsapp = whatsappActive
            }
        });
    }

    // ==========================================
    // PayTR Payment Management Endpoints
    // ==========================================

    /// <summary>
    /// Kullanıcının kredi bakiyesini getirir
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCreditBalanceAjax()
    {
        try
        {
            var companyId = GetCurrentUserCompanyId();
            if (companyId == null)
                return Json(ApiResponse.CompanyNotFound);

            var company = await _context.Companies.FindAsync(companyId);
            if (company == null)
                return Json(ApiResponse.CompanyNotFound);

            return Json(new { success = true, balance = company.CreditBalance });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting credit balance");
            return Json(ApiResponse.Error("Bakiye bilgisi alınırken hata oluştu."));
        }
    }

    /// <summary>
    /// Kullanıcının kayıtlı kartlarını listeler
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSavedCardsAjax()
    {
        try
        {
            var companyId = GetCurrentUserCompanyId();
            if (companyId == null)
                return Json(ApiResponse.CompanyNotFound);

            var company = await _context.Companies.FindAsync(companyId);
            if (company == null || string.IsNullOrEmpty(company.PaytrUtoken))
            {
                return Json(new { success = true, cards = Array.Empty<object>() });
            }

            var cards = await _payTRService.ListSavedCardsAsync(company.PaytrUtoken);
            return Json(new { success = true, cards });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing saved cards");
            return Json(ApiResponse.Error("Kayıtlı kartlar yüklenirken hata oluştu."));
        }
    }

    /// <summary>
    /// Kayıtlı kartı siler
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSavedCardAjax([FromBody] DeleteCardRequest request)
    {
        try
        {
            var companyId = GetCurrentUserCompanyId();
            if (companyId == null)
                return Json(ApiResponse.CompanyNotFound);

            var company = await _context.Companies.FindAsync(companyId);
            if (company == null || string.IsNullOrEmpty(company.PaytrUtoken))
            {
                return Json(ApiResponse.Error("Kayıtlı kart bulunamadı."));
            }

            var result = await _payTRService.DeleteSavedCardAsync(company.PaytrUtoken, request.Ctoken);
            if (result)
            {
                return Json(ApiResponse.SuccessMessage("Kart başarıyla silindi."));
            }

            return Json(ApiResponse.Error("Kart silinirken hata oluştu."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting saved card");
            return Json(ApiResponse.Error("Kart silinirken hata oluştu."));
        }
    }

    /// <summary>
    /// Yeni kartla ödeme başlatır
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> InitiatePaymentAjax([FromBody] InitiatePaymentRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Json(ApiResponse.UserNotFound);

            var companyId = GetCurrentUserCompanyId();
            if (companyId == null)
                return Json(ApiResponse.CompanyNotFound);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(ApiResponse.UserNotFound);

            var company = await _context.Companies.FindAsync(companyId);
            if (company == null)
                return Json(ApiResponse.CompanyNotFound);

            // Minimum tutar kontrolü
            if (request.Amount < 10)
            {
                return Json(ApiResponse.Error("Minimum yükleme tutarı ₺10'dur."));
            }

            var userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            if (userIp == "::1") userIp = "31.223.87.80";

            // Pending transaction oluştur
            var merchantOid = $"{DateTime.Now:yyyyMMddHHmmss}";
            var transaction = await _creditTransactionService.CreatePaymentTransactionAsync(
                companyId.Value, request.Amount, userId, merchantOid);

            // PayTR'ye ödeme isteği gönder
            var paymentRequest = new Models.DTOs.PayTRPaymentRequest
            {
                CardNumber = request.CardNumber,
                ExpiryMonth = request.ExpiryMonth,
                ExpiryYear = request.ExpiryYear,
                Cvv = request.Cvv,
                CardHolderName = request.CardHolderName,
                Amount = request.Amount,
                Email = user.Email ?? "",
                UserIp = userIp,
                CompanyId = companyId.Value,
                UserId = userId,
                StoreCard = request.StoreCard,
                Utoken = company.PaytrUtoken,
                MerchantOid = merchantOid
            };

            var result = await _payTRService.ProcessPaymentAsync(paymentRequest);

            if (result.Success)
            {
                // 3D Secure yönlendirmesi
                if (result.Status == "3d_redirect" && !string.IsNullOrEmpty(result.HtmlContent))
                {
                     return Json(new { success = true, is3D = true, htmlContent = result.HtmlContent, orderId = result.OrderId });
                }

                if (!string.IsNullOrEmpty(result.Utoken))
                {
                    company.PaytrUtoken = result.Utoken;
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true, message = "Ödeme işlemi başlatıldı.", orderId = result.OrderId });
            }

            // Ödeme başarısız
            await _creditTransactionService.FailTransactionAsync(merchantOid, result.ErrorMessage);
            return Json(ApiResponse.Error(result.ErrorMessage ?? "Ödeme işlemi başarısız oldu."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating payment");
            return Json(ApiResponse.Error("Ödeme işlemi sırasında bir hata oluştu."));
        }
    }

    /// <summary>
    /// Kayıtlı kartla ödeme başlatır
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PayWithSavedCardAjax([FromBody] SavedCardPaymentRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return Json(ApiResponse.UserNotFound);

            var companyId = GetCurrentUserCompanyId();
            if (companyId == null)
                return Json(ApiResponse.CompanyNotFound);

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Json(ApiResponse.UserNotFound);

            var company = await _context.Companies.FindAsync(companyId);
            if (company == null || string.IsNullOrEmpty(company.PaytrUtoken))
                return Json(ApiResponse.Error("Kayıtlı kart bilgisi bulunamadı."));

            if (request.Amount < 10)
            {
                return Json(ApiResponse.Error("Minimum yükleme tutarı ₺10'dur."));
            }

            var userIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            if (userIp == "::1") userIp = "31.223.87.80";

            var merchantOid = $"{DateTime.Now:yyyyMMddHHmmss}";
            var transaction = await _creditTransactionService.CreatePaymentTransactionAsync(
                companyId.Value, request.Amount, userId, merchantOid);

            var paymentRequest = new Models.DTOs.PayTRSavedCardPaymentRequest
            {
                Utoken = company.PaytrUtoken,
                Ctoken = request.Ctoken,
                Cvv = request.Cvv,
                Amount = request.Amount,
                Email = user.Email ?? "",
                UserIp = userIp,
                CompanyId = companyId.Value,
                UserId = userId,
                MerchantOid = merchantOid,
                Username = user.FullName
            };

            var result = await _payTRService.ProcessSavedCardPaymentAsync(paymentRequest);

            if (result.Success)
            {
                if (result.Status == "3d_redirect" && !string.IsNullOrEmpty(result.HtmlContent))
                {
                     return Json(new { success = true, is3D = true, htmlContent = result.HtmlContent, orderId = result.OrderId });
                }

                return Json(new { success = true, message = "Ödeme işlemi başlatıldı.", orderId = result.OrderId });
            }

            await _creditTransactionService.FailTransactionAsync(merchantOid, result.ErrorMessage);
            return Json(ApiResponse.Error(result.ErrorMessage ?? "Ödeme işlemi başarısız oldu."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error paying with saved card");
            return Json(ApiResponse.Error("Ödeme işlemi sırasında bir hata oluştu."));
        }
    }

    /// <summary>
    /// Kredi işlem geçmişini getirir
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTransactionHistoryAjax(int page = 1, int pageSize = 10)
    {
        try
        {
            var companyId = GetCurrentUserCompanyId();
            if (companyId == null)
                return Json(ApiResponse.CompanyNotFound);

            var transactions = await _creditTransactionService.GetTransactionHistoryAsync(companyId.Value, page, pageSize);
            var totalCount = await _creditTransactionService.GetTransactionCountAsync(companyId.Value);

            var transactionItems = transactions.Select(t => new
            {
                id = t.Id,
                type = t.TransactionType.ToString(),
                amount = t.Amount,
                balanceBefore = t.BalanceBefore,
                balanceAfter = t.BalanceAfter,
                description = t.Description,
                status = t.Status,
                cardLastFour = t.CardLastFour,
                createdAt = t.CreatedAt,
                createdBy = t.CreatedByUser != null ? $"{t.CreatedByUser.FirstName} {t.CreatedByUser.LastName}" : ""
            });

            return Json(new
            {
                success = true,
                transactions = new
                {
                    items = transactionItems,
                    totalCount,
                    page,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transaction history");
            return Json(ApiResponse.Error("İşlem geçmişi yüklenirken hata oluştu."));
        }
    }
    
    [AcceptVerbs("GET", "POST")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentSuccess([FromQuery] string oid)
    {
        if (string.IsNullOrEmpty(oid))
        {
            return Content("<script>window.parent.postMessage({ type: 'paytr_fail', message: 'Geçersiz işlem ID.' }, '*');</script>", "text/html");
        }

        // PayTR'dan durum sorgula
        var statusResult = await _payTRService.GetPaymentStatusAsync(oid);

        if (statusResult.Success)
        {
            // İşlemi onayla
            var completeResult = await _creditTransactionService.CompleteTransactionAsync(oid, null, null);
            if (completeResult)
            {
                 // Eğer utoken dönüyorsa güncelle (durum sorguda dönmez ama callback'te dönerdi, burada sadece onaylıyoruz)
                 return Content("<script>window.parent.postMessage({ type: 'paytr_success' }, '*');</script>", "text/html");
            }
            else
            {
                 // Zaten onaylanmış olabilir veya hata
                 // Yine de success dönelim, çünkü PayTR başarılı dedi.
                 return Content("<script>window.parent.postMessage({ type: 'paytr_success' }, '*');</script>", "text/html");
            }
        }
        else
        {
            // İşlem başarısız
            await _creditTransactionService.FailTransactionAsync(oid, statusResult.ErrorMessage ?? "Ödeme onaylanmadı.");
            return Content($"<script>window.parent.postMessage({{ type: 'paytr_fail', message: '{statusResult.ErrorMessage ?? "Ödeme onaylanmadı."}' }}, '*');</script>", "text/html");
        }
    }

    [AcceptVerbs("GET", "POST")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentFail([FromQuery] string oid)
    {
        var message = "Ödeme başarısız.";
        // PayTR fail url'ye post edebilir veya get ile dönebilir.
        // Genelde fail url'ye de post ederler hatayı.
        // Ama biz yine de oid ile kontrol edip fail edelim.
        
        if (!string.IsNullOrEmpty(oid))
        {
             await _creditTransactionService.FailTransactionAsync(oid, "Kullanıcı veya sistem tarafından iptal edildi.");
        }

        if (Request.Method == "POST" && Request.Form.ContainsKey("failed_reason_msg"))
        {
             message = Request.Form["failed_reason_msg"].FirstOrDefault() ?? message;
        }

        return Content($"<script>window.parent.postMessage({{ type: 'paytr_fail', message: '{message}' }}, '*');</script>", "text/html");
    }
}

// Request DTOs for SettingsController payment endpoints
public class DeleteCardRequest
{
    public string Ctoken { get; set; } = string.Empty;
}

public class InitiatePaymentRequest
{
    public string CardNumber { get; set; } = string.Empty;
    public string ExpiryMonth { get; set; } = string.Empty;
    public string ExpiryYear { get; set; } = string.Empty;
    public string Cvv { get; set; } = string.Empty;
    public string CardHolderName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public bool StoreCard { get; set; }
}

public class SavedCardPaymentRequest
{
    public string Ctoken { get; set; } = string.Empty;
    public string? Cvv { get; set; }
    public decimal Amount { get; set; }
}
