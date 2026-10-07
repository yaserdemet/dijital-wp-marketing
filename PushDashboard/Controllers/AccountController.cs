using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.Services;
using PushDashboard.ViewModels;

namespace PushDashboard.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AccountController> _logger;
    private readonly ITwoFactorService _twoFactorService;
    private readonly ISessionService _sessionService;
    private readonly IInvitationService _invitationService;
    private readonly IUserCacheService _userCacheService;
    private readonly IActivityLogService _activityLogService;
    private readonly IReferralCodeService _referralCodeService;
    private readonly IEmailService _emailService;

    public AccountController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext context,
        ILogger<AccountController> logger,
        ITwoFactorService twoFactorService,
        ISessionService sessionService,
        IInvitationService invitationService,
        IUserCacheService userCacheService,
        IActivityLogService activityLogService,
        IReferralCodeService referralCodeService,
        IEmailService emailService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _context = context;
        _logger = logger;
        _twoFactorService = twoFactorService;
        _sessionService = sessionService;
        _invitationService = invitationService;
        _userCacheService = userCacheService;
        _activityLogService = activityLogService;
        _referralCodeService = referralCodeService;
        _emailService = emailService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpGet]
    public IActionResult VerifyTwoFactor(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyTwoFactor(TwoFactorVerifyViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var email = TempData["Email"] as string;
        if (string.IsNullOrEmpty(email))
        {
            ModelState.AddModelError(string.Empty, "Oturum süresi doldu. Lütfen tekrar giriş yapın.");
            return RedirectToAction(nameof(Login));
        }

        var user = await _userManager.FindByEmailAsync(email);
        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        // 01-01-2026'dan sonra oluşmuş hesapların girişini engelle (onay bekliyor)
        // İstisna: @pushonica.com mail adresleri her zaman giriş yapabilir
        var approvalCutoffDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var isPushonicaEmail = user.Email?.EndsWith("@pushonicademy.com", StringComparison.OrdinalIgnoreCase) == true;
        if (user.CreatedAt >= approvalCutoffDate && !isPushonicaEmail)
        {
            _logger.LogWarning("User {Email} 2FA attempt blocked - account pending approval (created after 01-01-2026).", email);
            TempData["ErrorMessage"] = "Başvurunuz alındı! Ekibimiz en kısa sürede size ulaşacak.";
            return RedirectToAction(nameof(Login));
        }

        var result = await _twoFactorService.VerifyTwoFactorTokenAsync(user, model.Code);
        if (!result)
        {
            ModelState.AddModelError(string.Empty, "Geçersiz doğrulama kodu.");
            return View(model);
        }

        // Sign in the user
        var rememberMe = TempData["RememberMe"] as bool? ?? false;
        await _signInManager.SignInAsync(user, rememberMe);

        // Handle "Remember Machine" for 30 days
        if (model.RememberMachine)
        {
            var cookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.Now.AddDays(30),
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            };

            Response.Cookies.Append($"RememberMachine_{user.Id}", "true", cookieOptions);
        }

        // Update last login time
        user.LastLoginAt = DateTime.Now;
        await _userManager.UpdateAsync(user);
        await _userCacheService.InvalidateUserAsync(user.Id);

        // Create session record
        try
        {
            var sessionId = $"{HttpContext.Connection.Id}_{HttpContext.Request.Headers["User-Agent"].ToString().GetHashCode()}";
            await _sessionService.CreateSessionAsync(user.Id, sessionId, HttpContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session for user {UserId}", user.Id);
        }

        _logger.LogInformation("User {Email} logged in with 2FA.", email);

        // Activity Log - Login with 2FA
        if (user.CompanyId.HasValue)
        {
            await _activityLogService.LogActivityAsync(
                user.Id,
                user.CompanyId.Value,
                ActivityActionTypes.Login,
                ActivityModules.Account,
                $"{user.FirstName} {user.LastName} sisteme 2FA ile giriş yaptı",
                entityId: user.Id,
                entityType: "User",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (ModelState.IsValid)
        {
            var result = await _signInManager.PasswordSignInAsync(
                model.Email,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded || result.RequiresTwoFactor)
            {
                _logger.LogInformation("User logged in.");

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user is { LockoutEnabled: true })
                {
                    _logger.LogWarning("User {Email} login attempt blocked - account pending approval.", model.Email);
                    ModelState.AddModelError(string.Empty, "Başvurunuz alındı! Ekibimiz en kısa sürede size ulaşacak.");
                    return View(model);
                }

                // Check if user has 2FA enabled
                if (user != null && user.TwoFactorEnabled && !string.IsNullOrEmpty(user.TwoFactorSecretKey))
                {
                    // Check if this machine is remembered for 30 days
                    var rememberMachineCookie = Request.Cookies[$"RememberMachine_{user.Id}"];
                    if (rememberMachineCookie == "true")
                    {
                        // Machine is remembered, skip 2FA
                        _logger.LogInformation("User {Email} logged in with remembered machine, skipping 2FA.", model.Email);
                    }
                    else
                    {
                        // Sign out the user and redirect to 2FA verification
                        await _signInManager.SignOutAsync();
                        TempData["Email"] = model.Email;
                        TempData["RememberMe"] = model.RememberMe;
                        return RedirectToAction("VerifyTwoFactor", new { returnUrl });
                    }
                }

                // Update last login time
                if (user != null)
                {
                    user.LastLoginAt = DateTime.Now;
                    await _userManager.UpdateAsync(user);
                    await _userCacheService.InvalidateUserAsync(user.Id);

                    // Create session record
                    try
                    {
                        var sessionId = $"{HttpContext.Connection.Id}_{HttpContext.Request.Headers["User-Agent"].ToString().GetHashCode()}";
                        await _sessionService.CreateSessionAsync(user.Id, sessionId, HttpContext);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating session for user {UserId}", user.Id);
                    }

                    // Activity Log - Login
                    if (user.CompanyId.HasValue)
                    {
                        await _activityLogService.LogActivityAsync(
                            user.Id,
                            user.CompanyId.Value,
                            ActivityActionTypes.Login,
                            ActivityModules.Account,
                            $"{user.FirstName} {user.LastName} sisteme giriş yaptı",
                            entityId: user.Id,
                            entityType: "User",
                            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                            userAgent: Request.Headers["User-Agent"].ToString());
                    }
                }

                return RedirectToLocal(returnUrl);
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                _logger.LogWarning("Invalid login attempt for user {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "E-posta adresi veya şifre hatalı. Lütfen tekrar deneyin.");
                return View(model);
            }
        }

        // If we got this far, something failed, redisplay form
        return View(model);
    }

    [HttpGet]
    public IActionResult Register(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (ModelState.IsValid)
        {
            ApplicationUser? referrerUser = null;
            string? referralCodeToSave = null;
            if (!string.IsNullOrWhiteSpace(model.ReferralCode))
            {
                var referralCodeNumber = model.ReferralCode.Replace("PRef-", "").Trim();
                if (referralCodeNumber.Length != 6 || !referralCodeNumber.All(char.IsDigit))
                {
                    ModelState.AddModelError("ReferralCode", "Referans kodu 6 haneli sayı olmalıdır.");
                    return View(model);
                }
                referrerUser = await _userManager.Users
                    .Include(u => u.Company)
                    .FirstOrDefaultAsync(u => u.ReferralCode == referralCodeNumber);
                if (referrerUser == null)
                {
                    ModelState.AddModelError("ReferralCode", "Böyle bir referans kodu bulunamadı. Lütfen geçerli bir referans kodu giriniz.");
                    return View(model);
                }
                referralCodeToSave = referralCodeNumber;
            }

            // Check if company with the same name already exists
            var existingCompany = await _context.Companies
                .FirstOrDefaultAsync(c => c.Name.ToLower() == model.CompanyName.ToLower());

            if (existingCompany != null)
            {
                ModelState.AddModelError("CompanyName", "Bu firma adı zaten kayıtlı. Lütfen farklı bir firma adı giriniz.");
                return View(model);
            }

            // Create new company
            var company = new Company
            {
                Email = model.Email,
                Phone = model.PhoneNumber,
                Name = model.CompanyName,
                Website = model.Website,
                CreatedAt = DateTime.Now
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                CompanyId = company.Id,
                CreatedAt = DateTime.Now,
                LockoutEnabled = false, // Onay bekliyor - giriş yapamaz
                ReferredByReferralCode = referralCodeToSave // Referans kodu ile kayıt olunduysa referans kodunu kaydet
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                var referralCodeAssigned = await _referralCodeService.AssignReferralCodeAsync(user);
                if (!referralCodeAssigned)
                {
                    _logger.LogError("Failed to assign referral code to user {UserId}", user.Id);
                    _context.Companies.Remove(company);
                    await _context.SaveChangesAsync();
                    await _userManager.DeleteAsync(user);
                    ModelState.AddModelError(string.Empty, "Referans kodu oluşturulurken bir hata oluştu. Lütfen tekrar deneyin.");
                    return View(model);
                }

                _logger.LogInformation("User created a new account - pending approval: {Email}", model.Email);

                // Add user to the User and CompanyOwner roles
                await _userManager.AddToRoleAsync(user, "User");
                await _userManager.AddToRoleAsync(user, "CompanyOwner");

                if (referrerUser != null && !string.IsNullOrEmpty(referralCodeToSave))
                {
                    referrerUser.ReferralCount++;
                    await _userManager.UpdateAsync(referrerUser);
                    _logger.LogInformation("Referral code used: {ReferralCode} by user {Email}, referrer count increased to {Count}",
                        referralCodeToSave, model.Email, referrerUser.ReferralCount);
                    if (referrerUser.CompanyId.HasValue)
                    {
                        await _activityLogService.LogActivityAsync(
                            referrerUser.Id,
                            referrerUser.CompanyId.Value,
                            ActivityActionTypes.Create,
                            ActivityModules.Account,
                            $"PRef-{referralCodeToSave} referans kodunuz ile {company.Name} firması kayıt oldu",
                            entityId: user.Id,
                            entityType: "User",
                            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                            userAgent: Request.Headers["User-Agent"].ToString());
                    }
                }

                var userActivityMessage = !string.IsNullOrEmpty(referralCodeToSave) && referrerUser?.Company != null
                    ? $"{company.Name} olarak sisteme, {referrerUser.Company.Name} firmasının PRef-{referralCodeToSave} referans koduyla kayıt oldunuz"
                    : $"{company.Name} olarak sisteme kayıt oldunuz";

                await _activityLogService.LogActivityAsync(
                    user.Id,
                    company.Id,
                    ActivityActionTypes.Create,
                    ActivityModules.Account,
                    userActivityMessage,
                    entityId: user.Id,
                    entityType: "User",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString());

                // Başvuru alındı mesajı göster
                TempData["SuccessMessage"] = "Başvurunuz başarıyla alındı! Ekibimiz en kısa sürede sizinle iletişime geçecektir.";
                return RedirectToAction(nameof(Login));
            }

            // If user creation failed, remove the company we just created
            _context.Companies.Remove(company);
            await _context.SaveChangesAsync();
            await _userCacheService.InvalidateAllUsersAsync(company.Id);

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        // If we got this far, something failed, redisplay form
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // Activity Log - Logout (önce log, sonra çıkış)
        var user = await _userManager.GetUserAsync(User);
        if (user != null && user.CompanyId.HasValue)
        {
            await _activityLogService.LogActivityAsync(
                user.Id,
                user.CompanyId.Value,
                ActivityActionTypes.Logout,
                ActivityModules.Account,
                $"{user.FirstName} {user.LastName} sistemden çıkış yaptı",
                entityId: user.Id,
                entityType: "User",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        await _signInManager.SignOutAsync();
        _logger.LogInformation("User logged out.");

        return RedirectToAction(nameof(Login), "Account");
    }

    [HttpGet]
    public async Task<IActionResult> AcceptInvitation(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            TempData["ErrorMessage"] = "Geçersiz davetiye bağlantısı.";
            return RedirectToAction(nameof(Register));
        }

        var validationResult = await _invitationService.ValidateInvitationTokenAsync(token);
        if (!validationResult.IsValid)
        {
            TempData["ErrorMessage"] = validationResult.Message;
            return RedirectToAction(nameof(Register));
        }

        var model = new AcceptInvitationRequest
        {
            Token = token
        };

        ViewData["CompanyName"] = validationResult.CompanyName;
        ViewData["InviterName"] = validationResult.InviterName;
        ViewData["ExpirationDate"] = validationResult.ExpirationDate;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptInvitation(AcceptInvitationRequest model)
    {
        if (!ModelState.IsValid)
        {
            var validationResult = await _invitationService.ValidateInvitationTokenAsync(model.Token);
            if (validationResult.IsValid)
            {
                ViewData["CompanyName"] = validationResult.CompanyName;
                ViewData["InviterName"] = validationResult.InviterName;
                ViewData["ExpirationDate"] = validationResult.ExpirationDate;
            }
            return View(model);
        }

        try
        {
            // Validate invitation token again
            var validationResult = await _invitationService.ValidateInvitationTokenAsync(model.Token);
            if (!validationResult.IsValid)
            {
                ModelState.AddModelError("", validationResult.Message ?? "Geçersiz davetiye.");
                return View(model);
            }

            // Get invitation details
            var invitation = await _context.UserInvitations
                .Include(i => i.Company)
                .FirstOrDefaultAsync(i => i.InvitationToken == model.Token && !i.IsUsed && i.ExpirationDate > DateTime.Now);

            if (invitation == null)
            {
                ModelState.AddModelError("", "Davetiye bulunamadı veya geçersiz.");
                return View(model);
            }

            // Check if user already exists
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                ModelState.AddModelError("Email", "Bu e-posta adresi ile kayıtlı bir kullanıcı zaten mevcut.");
                ViewData["CompanyName"] = validationResult.CompanyName;
                ViewData["InviterName"] = validationResult.InviterName;
                ViewData["ExpirationDate"] = validationResult.ExpirationDate;
                return View(model);
            }

            // Create new user
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.PhoneNumber,
                CompanyId = invitation.CompanyId,
                CreatedAt = DateTime.Now,
                LockoutEnabled = false // Onay bekliyor - giriş yapamaz
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created via invitation: {Email} for company {CompanyId}", model.Email, invitation.CompanyId);

                // Add user to the User role (not CompanyOwner)
                await _userManager.AddToRoleAsync(user, "User");

                // Mark invitation as used
                invitation.IsUsed = true;
                invitation.UsedAt = DateTime.Now;
                await _context.SaveChangesAsync();

                // Sign in the user
                await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation("User signed in after accepting invitation.");

                TempData["SuccessMessage"] = $"Hoş geldiniz! {invitation.Company.Name} firmasına başarıyla katıldınız.";
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            ViewData["CompanyName"] = validationResult.CompanyName;
            ViewData["InviterName"] = validationResult.InviterName;
            ViewData["ExpirationDate"] = validationResult.ExpirationDate;
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting invitation for token {Token}", model.Token);
            ModelState.AddModelError("", "Davetiye kabul edilirken bir hata oluştu. Lütfen daha sonra tekrar deneyin.");

            var validationResult = await _invitationService.ValidateInvitationTokenAsync(model.Token);
            if (validationResult.IsValid)
            {
                ViewData["CompanyName"] = validationResult.CompanyName;
                ViewData["InviterName"] = validationResult.InviterName;
                ViewData["ExpirationDate"] = validationResult.ExpirationDate;
            }
            return View(model);
        }
    }

    [HttpGet]
    public IActionResult Lockout()
    {
        return View();
    }

    // ==================== Forgot Password ====================

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user != null)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            var resetLink = Url.Action(
                "ResetPassword",
                "Account",
                new { email = user.Email, token },
                protocol: Request.Scheme);

            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email!, resetLink!);
                _logger.LogInformation("Password reset email sent to {Email}", model.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", model.Email);
            }
        }
        else
        {
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", model.Email);
        }

        // Güvenlik: Kullanıcı olsun olmasın aynı mesajı göster
        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            TempData["ErrorMessage"] = "Geçersiz şifre sıfırlama bağlantısı.";
            return RedirectToAction(nameof(Login));
        }

        var model = new ResetPasswordViewModel
        {
            Email = email ?? string.Empty,
            Token = token
        };

        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null)
        {
            // Güvenlik: Kullanıcı bulunamasa bile başarılı sayfasına yönlendir
            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (result.Succeeded)
        {
            _logger.LogInformation("Password successfully reset for user {Email}", model.Email);

            // Activity Log - Password Reset
            if (user.CompanyId.HasValue)
            {
                await _activityLogService.LogActivityAsync(
                    user.Id,
                    user.CompanyId.Value,
                    ActivityActionTypes.Update,
                    ActivityModules.Account,
                    $"{user.FirstName} {user.LastName} şifresini sıfırladı",
                    entityId: user.Id,
                    entityType: "User",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString());
            }

            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }

        return View(model);
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [Authorize]
    [HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        var model = new ProfileViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber
        };

        return View(model);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        user.FirstName = model.FirstName;
        user.LastName = model.LastName;
        user.PhoneNumber = model.PhoneNumber;

        var result = await _userManager.UpdateAsync(user);
        await _userCacheService.InvalidateUserAsync(user.Id);

        // Create session record
        try
        {
            var sessionId = $"{HttpContext.Connection.Id}_{HttpContext.Request.Headers["User-Agent"].ToString().GetHashCode()}";
            await _sessionService.CreateSessionAsync(user.Id, sessionId, HttpContext);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session for user {UserId}", user.Id);
        }

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // Activity Log - Profile Update
        if (user.CompanyId.HasValue)
        {
            await _activityLogService.LogActivityAsync(
                user.Id,
                user.CompanyId.Value,
                ActivityActionTypes.Update,
                ActivityModules.Account,
                $"{user.FirstName} {user.LastName} profil bilgilerini güncelledi",
                entityId: user.Id,
                entityType: "User",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        TempData["StatusMessage"] = "Your profile has been updated";
        return RedirectToAction(nameof(Profile));
    }

    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound();
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        _logger.LogInformation("User changed their password successfully.");

        // Activity Log - Password Change
        if (user.CompanyId.HasValue)
        {
            await _activityLogService.LogActivityAsync(
                user.Id,
                user.CompanyId.Value,
                ActivityActionTypes.Update,
                ActivityModules.Account,
                $"{user.FirstName} {user.LastName} şifresini değiştirdi",
                entityId: user.Id,
                entityType: "User",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        TempData["StatusMessage"] = "Your password has been changed.";
        return RedirectToAction(nameof(Profile));
    }


    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePasswordAjax([FromBody] ChangePasswordRequest model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
            return Json(new { success = false, message = string.Join(" ", errors) });
        }

        _logger.LogInformation("ChangePasswordAjax called");

        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Json(new { success = false, message = "Kullanıcı bulunamadı." });
        }

        var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

        if (!result.Succeeded)
        {
            var errorMessages = result.Errors.Select(e => e.Description).ToList();
            return Json(new { success = false, message = string.Join(" ", errorMessages) });
        }

        await _signInManager.RefreshSignInAsync(user);
        _logger.LogInformation("User changed their password successfully via AJAX.");

        // Activity Log - Password Change via AJAX
        if (user.CompanyId.HasValue)
        {
            await _activityLogService.LogActivityAsync(
                user.Id,
                user.CompanyId.Value,
                ActivityActionTypes.Update,
                ActivityModules.Account,
                $"{user.FirstName} {user.LastName} şifresini değiştirdi",
                entityId: user.Id,
                entityType: "User",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());
        }

        return Json(new { success = true, message = "Şifreniz başarıyla değiştirildi." });
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        else
        {
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
    }
}
