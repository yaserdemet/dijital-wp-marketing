using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PushDashboard.Data;
using PushDashboard.Helpers;
using PushDashboard.Models;
using PushDashboard.Models.ViewModels;
using PushDashboard.Services;
using PushDashboard.Filters;

namespace PushDashboard.Controllers;

[Authorize]
public class ExitSurveyController : BaseController
{
    private readonly IExitSurveyService _exitSurveyService;
    private readonly ILogger<ExitSurveyController> _logger;
    private readonly IActivityLogService _activityLogService;

    public ExitSurveyController(
        IExitSurveyService exitSurveyService,
        ILogger<ExitSurveyController> logger,
        IActivityLogService activityLogService,
        ApplicationDbContext context,
        IUserContextService userContextService) : base(userContextService, context)
    {
        _exitSurveyService = exitSurveyService;
        _logger = logger;
        _activityLogService = activityLogService;
    }

    private async Task<bool> HasModuleAccessAsync(Guid companyId)
    {
        return await _exitSurveyService.HasExitSurveyModuleAsync(companyId);
    }

    public async Task<IActionResult> Index()
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var hasModule = await _exitSurveyService.HasExitSurveyModuleAsync(companyId.Value);
        if (!hasModule)
        {
            TempData["ErrorMessage"] = "Çıkış Anketi modülüne sahip değilsiniz. Lütfen önce modülü satın alın.";
            return RedirectToAction("Index", "Store");
        }

        var surveys = await _exitSurveyService.GetExitSurveysAsync(companyId.Value);
        var stats = await _exitSurveyService.GetDashboardStatsAsync(companyId.Value);

        var viewModel = new ExitSurveyIndexViewModel
        {
            Surveys = surveys,
            Stats = stats
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var companyId = GetCurrentUserCompanyId();
        if (!companyId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!await HasModuleAccessAsync(companyId.Value))
        {
            TempData["ErrorMessage"] = "Çıkış Anketi modülüne sahip değilsiniz.";
            return RedirectToAction("Index", "Store");
        }

        var exitSurvey = await _exitSurveyService.GetExitSurveyByIdAsync(id, companyId.Value);
        if (exitSurvey == null)
        {
            TempData["ErrorMessage"] = "Anket bulunamadı.";
            return RedirectToAction("Index");
        }

        var questions = await _exitSurveyService.GetQuestionsAsync(exitSurvey.Id);
        var stats = await _exitSurveyService.GetSurveyStatsAsync(id, companyId.Value);

        var viewModel = new ExitSurveyViewModel
        {
            ExitSurvey = exitSurvey,
            Questions = questions,
            Stats = stats
        };

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> CreateSurvey([FromBody] CreateExitSurveyViewModel model)
    {
        if (!TryGetUserContext(out var userId, out var companyId))
        {
            return Json(ApiResponse.UserNotFound);
        }

        if (!await HasModuleAccessAsync(companyId))
        {
            return Json(ApiResponse.Error("Çıkış Anketi modülüne erişim yetkiniz yok."));
        }

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            return Json(ApiResponse.Error("Anket adı boş olamaz."));
        }

        var survey = await _exitSurveyService.CreateExitSurveyAsync(companyId, userId, model);

        await _activityLogService.LogActivityAsync(
            userId,
            companyId,
            ActivityActionTypes.Create,
            ActivityModules.ExitSurvey,
            $"Yeni çıkış anketi oluşturuldu: {model.Name}",
            entityId: survey.Id.ToString(),
            entityType: "ExitSurvey",
            details: new { model.Name, model.Description },
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: Request.Headers["User-Agent"].ToString());

        return Json(new { success = true, message = "Anket başarıyla oluşturuldu!", surveyId = survey.Id });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteSurvey(int id)
    {
        if (!TryGetUserContext(out var userId, out var companyId))
        {
            return Json(ApiResponse.UserNotFound);
        }

        if (!await HasModuleAccessAsync(companyId))
        {
            return Json(ApiResponse.Error("Çıkış Anketi modülüne erişim yetkiniz yok."));
        }

        var success = await _exitSurveyService.DeleteExitSurveyAsync(id, companyId);

        if (success)
        {
            await _activityLogService.LogActivityAsync(
                userId,
                companyId,
                ActivityActionTypes.Delete,
                ActivityModules.ExitSurvey,
                $"Çıkış anketi silindi (ID: {id})",
                entityId: id.ToString(),
                entityType: "ExitSurvey",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            return Json(ApiResponse.SuccessMessage("Anket başarıyla silindi."));
        }

        return Json(ApiResponse.Error("Anket silinirken bir hata oluştu."));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleSurveyStatus(int id)
    {
        if (!TryGetUserContext(out var userId, out var companyId))
        {
            return Json(ApiResponse.UserNotFound);
        }

        if (!await HasModuleAccessAsync(companyId))
        {
            return Json(ApiResponse.Error("Çıkış Anketi modülüne erişim yetkiniz yok."));
        }

        var success = await _exitSurveyService.ToggleExitSurveyStatusAsync(id, companyId);

        if (success)
        {
            await _activityLogService.LogActivityAsync(
                userId,
                companyId,
                ActivityActionTypes.Update,
                ActivityModules.ExitSurvey,
                $"Çıkış anketi durumu değiştirildi (ID: {id})",
                entityId: id.ToString(),
                entityType: "ExitSurvey",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            return Json(ApiResponse.SuccessMessage("Anket durumu güncellendi."));
        }

        return Json(ApiResponse.Error("Durum güncellenirken bir hata oluştu."));
    }

    [HttpPost]
    public async Task<IActionResult> SaveSettings([FromBody] SaveExitSurveyViewModel model)
    {
        if (!TryGetUserContext(out var userId, out var companyId))
        {
            return Json(ApiResponse.UserNotFound);
        }

        if (!await HasModuleAccessAsync(companyId))
        {
            return Json(ApiResponse.Error("Çıkış Anketi modülüne erişim yetkiniz yok."));
        }

        var exitSurvey = new ExitSurvey
        {
            Id = model.Id,
            CompanyId = companyId,
            Title = model.Title,
            Description = model.Description, 
            SubmitButtonText = model.SubmitButtonText,
            CancelButtonText = model.CancelButtonText,
            ThankYouMessage = model.ThankYouMessage,
            BackgroundColor = model.BackgroundColor,
            TextColor = model.TextColor,
            SubmitButtonColor = model.SubmitButtonColor,
            CancelButtonColor = model.CancelButtonColor,
            BorderRadius = model.BorderRadius,
            EnableAnimation = model.EnableAnimation,
            ShowOnPageExit = model.ShowOnPageExit,
            ShowOnTabClose = model.ShowOnTabClose,
            DelayBeforeShow = model.DelayBeforeShow,
            ShowFrequencyDays = model.ShowFrequencyDays,
            ShowOnMobile = model.ShowOnMobile,
            MobileDelayMinutes = model.MobileDelayMinutes,
            ShowOnDesktop = model.ShowOnDesktop,
            IsActive = model.IsActive,
            UpdatedByUserId = userId
        };

        var success = await _exitSurveyService.SaveExitSurveyAsync(exitSurvey);

        if (success)
        {
            await _activityLogService.LogActivityAsync(
                userId,
                companyId,
                ActivityActionTypes.Update,
                ActivityModules.ExitSurvey,
                $"Çıkış anketi ayarları güncellendi (Aktif: {model.IsActive})",
                entityType: "ExitSurvey",
                details: new { model.Title, model.IsActive },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            return Json(ApiResponse.SettingsSaved);
        }
        else
        {
            return Json(ApiResponse.SettingsSaveError);
        }
    }

    [HttpPost]
    public async Task<IActionResult> SaveQuestion([FromBody] ExitSurveyQuestionViewModel model)
    {
        if (!TryGetUserContext(out var userId, out var companyId))
        {
            return Json(ApiResponse.UserNotFound);
        }

        if (!await HasModuleAccessAsync(companyId))
        {
            return Json(ApiResponse.Error("Çıkış Anketi modülüne erişim yetkiniz yok."));
        }

        var exitSurvey = await _exitSurveyService.GetExitSurveyByIdAsync(model.ExitSurveyId, companyId);
        if (exitSurvey == null)
        {
            return Json(ApiResponse.Error("Anket bulunamadı."));
        }

        var question = new ExitSurveyQuestion
        {
            Id = model.Id,
            ExitSurveyId = exitSurvey.Id,
            QuestionText = model.QuestionText,
            QuestionType = model.QuestionType,
            SortOrder = model.SortOrder,
            IsRequired = model.IsRequired,
            IsActive = model.IsActive,
            Options = model.Options
        };

        var success = await _exitSurveyService.SaveQuestionAsync(question);

        if (success)
        {
            await _activityLogService.LogActivityAsync(
                userId,
                companyId,
                model.Id > 0 ? ActivityActionTypes.Update : ActivityActionTypes.Create,
                ActivityModules.ExitSurvey,
                $"Çıkış anketi sorusu {(model.Id > 0 ? "güncellendi" : "eklendi")}: {model.QuestionText}",
                entityId: model.Id > 0 ? model.Id.ToString() : null,
                entityType: "ExitSurveyQuestion",
                details: new { model.QuestionText, model.QuestionType, model.IsRequired },
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            return Json(ApiResponse.SuccessMessage("Soru başarıyla kaydedildi."));
        }
        else
        {
            return Json(ApiResponse.Error("Soru kaydedilirken bir hata oluştu."));
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteQuestion(int questionId)
    {
        if (!TryGetUserContext(out var userId, out var companyId))
        {
            return Json(ApiResponse.UserNotFound);
        }

        if (!await HasModuleAccessAsync(companyId))
        {
            return Json(ApiResponse.Error("Çıkış Anketi modülüne erişim yetkiniz yok."));
        }

        var success = await _exitSurveyService.DeleteQuestionAsync(questionId, companyId);

        if (success)
        {
            await _activityLogService.LogActivityAsync(
                userId,
                companyId,
                ActivityActionTypes.Delete,
                ActivityModules.ExitSurvey,
                $"Çıkış anketi sorusu silindi (ID: {questionId})",
                entityId: questionId.ToString(),
                entityType: "ExitSurveyQuestion",
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: Request.Headers["User-Agent"].ToString());

            return Json(ApiResponse.SuccessMessage("Soru başarıyla silindi."));
        }
        else
        {
            return Json(ApiResponse.Error("Soru silinirken bir hata oluştu."));
        }
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> Preview(int id)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        if (!await HasModuleAccessAsync(companyId))
        {
            return Json(ApiResponse.Error("Çıkış Anketi modülüne erişim yetkiniz yok."));
        }

        var survey = await _exitSurveyService.GetExitSurveyByIdAsync(id, companyId);
        if (survey == null)
        {
            return Json(ApiResponse.Error("Anket bulunamadı."));
        }

        var config = new ExitSurveyScriptConfig
        {
            CompanyId = companyId,
            IsActive = survey.IsActive,
            Title = survey.Title,
            Description = survey.Description,
            SubmitButtonText = survey.SubmitButtonText,
            CancelButtonText = survey.CancelButtonText,
            ThankYouMessage = survey.ThankYouMessage,
            BackgroundColor = survey.BackgroundColor,
            TextColor = survey.TextColor,
            SubmitButtonColor = survey.SubmitButtonColor,
            CancelButtonColor = survey.CancelButtonColor,
            BorderRadius = survey.BorderRadius,
            EnableAnimation = survey.EnableAnimation,
            ShowOnPageExit = survey.ShowOnPageExit,
            ShowOnTabClose = survey.ShowOnTabClose,
            DelayBeforeShow = survey.DelayBeforeShow,
            ShowFrequencyDays = survey.ShowFrequencyDays,
            ShowOnMobile = survey.ShowOnMobile,
            MobileDelayMinutes = survey.MobileDelayMinutes,
            ShowOnDesktop = survey.ShowOnDesktop,
            Questions = survey.Questions
                .Where(q => q.IsActive)
                .OrderBy(q => q.SortOrder)
                .Select(q => new ExitSurveyQuestionConfig
                {
                    Id = q.Id,
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    IsRequired = q.IsRequired,
                    Options = q.Options
                }).ToList()
        };

        return Json(new { success = true, config = config });
    }

    [HttpGet]
    [RequireCompanyId]
    public async Task<IActionResult> GetQuestion(int questionId)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        if (!await HasModuleAccessAsync(companyId))
        {
            return Json(ApiResponse.Error("Çıkış Anketi modülüne erişim yetkiniz yok."));
        }

        var question = await _exitSurveyService.GetQuestionAsync(questionId, companyId);
        if (question == null)
        {
            return Json(ApiResponse.Error("Soru bulunamadı."));
        }

        return Json(new { success = true, question = question });
    }

    [HttpPost]
    [RequireCompanyId]
    public async Task<IActionResult> UpdateQuestion([FromBody] ExitSurveyQuestionViewModel model)
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        if (!await HasModuleAccessAsync(companyId))
        {
            return Json(ApiResponse.Error("Çıkış Anketi modülüne erişim yetkiniz yok."));
        }

        var existingQuestion = await _exitSurveyService.GetQuestionAsync(model.Id, companyId);
        if (existingQuestion == null)
        {
            return Json(ApiResponse.Error("Güncellenecek soru bulunamadı."));
        }

        var success = await _exitSurveyService.UpdateQuestionAsync(model, companyId);

        if (success)
        {
            return Json(ApiResponse.SuccessMessage("Soru başarıyla güncellendi."));
        }

        return Json(ApiResponse.Error("Soru güncellenirken bir hata oluştu."));
    }

    [HttpGet]
    public async Task<IActionResult> AllResponses(int id, int page = 1, int pageSize = 10, string? startDate = null, string? endDate = null)
    {
        var companyId = GetCurrentUserCompanyId();

        if (!companyId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        if (!await HasModuleAccessAsync(companyId.Value))
        {
            TempData["ErrorMessage"] = "Çıkış Anketi modülüne sahip değilsiniz.";
            return RedirectToAction("Index", "Store");
        }

        if (page < 1) page = 1;
        if (pageSize < 5) pageSize = 5;
        if (pageSize > 50) pageSize = 50;

        DateTime? start = null;
        DateTime? end = null;
        if (DateTime.TryParse(startDate, out var s)) start = s;
        if (DateTime.TryParse(endDate, out var e)) end = e;

        var viewModel = await _exitSurveyService.GetAllResponsesViewModelAsync(id, companyId.Value, page, pageSize, start, end);
        ViewBag.SurveyId = id;
        return View(viewModel);
    }

    [HttpPost]
    [RequireCompanyId]
    public async Task<IActionResult> ExportToR2()
    {
        var companyId = GetCurrentUserCompanyId()!.Value;

        if (!await HasModuleAccessAsync(companyId))
        {
            return Json(ApiResponse.Error("Çıkış Anketi modülüne erişim yetkiniz yok."));
        }

        try
        {
            var surveys = await _exitSurveyService.GetExitSurveysAsync(companyId);
            var hasActiveSurvey = surveys.Any(s => s.IsActive);
            var jsonUrl = await _exitSurveyService.ExportExitSurveyToR2Async(companyId);

            _logger.LogInformation("Exit survey exported to R2 for company {CompanyId}. HasActive: {HasActive}", companyId, hasActiveSurvey);

            var userId = GetCurrentUserId();
            var actionDesc = hasActiveSurvey ? "Çıkış anketi siteye aktarıldı" : "Çıkış anketi siteden silindi";
            if (!string.IsNullOrEmpty(userId))
            {
                await _activityLogService.LogActivityAsync(
                    userId,
                    companyId,
                    ActivityActionTypes.Export,
                    ActivityModules.ExitSurvey,
                    actionDesc,
                    entityType: "ExitSurvey",
                    ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString(),
                    userAgent: Request.Headers["User-Agent"].ToString());
            }

            var message = hasActiveSurvey ? "Siteye başarıyla aktarıldı!" : "Anket siteden başarıyla silindi!";
            return Json(new { success = true, message = message, jsonUrl = jsonUrl });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = "İşlem sırasında bir hata oluştu: " + ex.Message });
        }
    }
}
