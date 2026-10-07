using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.Models.ViewModels;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PushDashboard.Services;

public interface IExitSurveyService
{
    Task<bool> HasExitSurveyModuleAsync(Guid companyId);
    Task<List<ExitSurvey>> GetExitSurveysAsync(Guid companyId);
    Task<ExitSurvey?> GetExitSurveyByIdAsync(int surveyId, Guid companyId);
    Task<ExitSurvey> CreateExitSurveyAsync(Guid companyId, string userId, CreateExitSurveyViewModel model);
    Task<bool> SaveExitSurveyAsync(ExitSurvey exitSurvey);
    Task<bool> DeleteExitSurveyAsync(int surveyId, Guid companyId);
    Task<bool> ToggleExitSurveyStatusAsync(int surveyId, Guid companyId);
    Task<ExitSurveyScriptConfig> GetScriptConfigAsync(Guid companyId);
    Task<List<ExitSurveyQuestion>> GetQuestionsAsync(int exitSurveyId);
    Task<bool> SaveQuestionAsync(ExitSurveyQuestion question);
    Task<bool> DeleteQuestionAsync(int questionId, Guid companyId);
    Task<bool> SubmitResponseAsync(SubmitExitSurveyResponseViewModel model, string? ipAddress, string? userAgent);
    Task<object?> GetQuestionAsync(int questionId, Guid companyId);
    Task<AllResponsesViewModel> GetAllResponsesViewModelAsync(int surveyId, Guid companyId, int page = 1, int pageSize = 10, DateTime? startDate = null, DateTime? endDate = null);
    Task<bool> UpdateQuestionAsync(ExitSurveyQuestionViewModel model, Guid companyId);
    Task<string> ExportExitSurveyToR2Async(Guid companyId);
    Task<bool> RecordTrackingEventAsync(ExitSurveyTrackingViewModel model, string? ipAddress, string? userAgent);
    Task<ExitSurveyStatsViewModel> GetDashboardStatsAsync(Guid companyId);
    Task<ExitSurveyStatsViewModel> GetSurveyStatsAsync(int surveyId, Guid companyId);
}

public class ExitSurveyService : IExitSurveyService
{
    private const string ModuleName = "Çıkış Anketi";

    private readonly ApplicationDbContext _context;
    private readonly ILogger<ExitSurveyService> _logger;
    private readonly IR2StorageService _r2StorageService;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ExitSurveyService(
        ApplicationDbContext context,
        ILogger<ExitSurveyService> logger,
        IR2StorageService r2StorageService,
        IConfiguration configuration,
        IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _logger = logger;
        _r2StorageService = r2StorageService;
        _configuration = configuration;
        _webHostEnvironment = webHostEnvironment;
    }

    public async Task<bool> HasExitSurveyModuleAsync(Guid companyId)
    {
        try
        {
            var hasModule = await _context.CompanyModules
                .AnyAsync(cm => cm.CompanyId == companyId &&
                               cm.Module.Name == ModuleName &&
                               cm.IsActive);

            return hasModule;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking exit survey module for company {CompanyId}", companyId);
            return false;
        }
    }

    public async Task<List<ExitSurvey>> GetExitSurveysAsync(Guid companyId)
    {
        try
        {
            return await _context.ExitSurveys
                .Include(e => e.Questions.Where(q => q.IsActive))
                .Include(e => e.Responses)
                .Where(e => e.CompanyId == companyId)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exit surveys for company {CompanyId}", companyId);
            return new List<ExitSurvey>();
        }
    }

    public async Task<ExitSurvey?> GetExitSurveyByIdAsync(int surveyId, Guid companyId)
    {
        try
        {
            return await _context.ExitSurveys
                .Include(e => e.Questions.Where(q => q.IsActive))
                .FirstOrDefaultAsync(e => e.Id == surveyId && e.CompanyId == companyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exit survey {SurveyId} for company {CompanyId}", surveyId, companyId);
            return null;
        }
    }

    public async Task<ExitSurvey> CreateExitSurveyAsync(Guid companyId, string userId, CreateExitSurveyViewModel model)
    {
        var survey = new ExitSurvey
        {
            CompanyId = companyId,
            Name = model.Name,
            AdminDescription = model.Description ?? string.Empty,
            Title = "Görüşünüz Bizim İçin Önemli",
            SubmitButtonText = "Gönder",
            CancelButtonText = "Kapat",
            ThankYouMessage = "Geri bildiriminiz için teşekkür ederiz!",
            BackgroundColor = "#ffffff",
            TextColor = "#333333",
            SubmitButtonColor = "#4CAF50",
            CancelButtonColor = "#6c757d",
            BorderRadius = "8px",
            EnableAnimation = true,
            ShowOnPageExit = true,
            ShowOnTabClose = true,
            DelayBeforeShow = 0,
            ShowFrequencyDays = 30,
            ShowOnMobile = true,
            MobileDelayMinutes = 5,
            ShowOnDesktop = true,
            IsActive = false,
            UpdatedByUserId = userId
        };

        _context.ExitSurveys.Add(survey);
        await _context.SaveChangesAsync();
        return survey;
    }

    public async Task<bool> SaveExitSurveyAsync(ExitSurvey exitSurvey)
    {
        try
        {
            var existing = await _context.ExitSurveys
                .FirstOrDefaultAsync(e => e.Id == exitSurvey.Id && e.CompanyId == exitSurvey.CompanyId);

            if (existing != null)
            {
                existing.Title = exitSurvey.Title;
                existing.Description = exitSurvey.Description;
                existing.SubmitButtonText = exitSurvey.SubmitButtonText;
                existing.CancelButtonText = exitSurvey.CancelButtonText;
                existing.ThankYouMessage = exitSurvey.ThankYouMessage;
                existing.BackgroundColor = exitSurvey.BackgroundColor;
                existing.TextColor = exitSurvey.TextColor;
                existing.SubmitButtonColor = exitSurvey.SubmitButtonColor;
                existing.CancelButtonColor = exitSurvey.CancelButtonColor;
                existing.BorderRadius = exitSurvey.BorderRadius;
                existing.EnableAnimation = exitSurvey.EnableAnimation;
                existing.ShowOnPageExit = exitSurvey.ShowOnPageExit;
                existing.ShowOnTabClose = exitSurvey.ShowOnTabClose;
                existing.DelayBeforeShow = exitSurvey.DelayBeforeShow;
                existing.ShowFrequencyDays = exitSurvey.ShowFrequencyDays;
                existing.ShowOnMobile = exitSurvey.ShowOnMobile;
                existing.MobileDelayMinutes = exitSurvey.MobileDelayMinutes;
                existing.ShowOnDesktop = exitSurvey.ShowOnDesktop;
                existing.IsActive = exitSurvey.IsActive;
                existing.UpdatedAt = DateTime.Now;
                existing.UpdatedByUserId = exitSurvey.UpdatedByUserId;
            }
            else
            {
                return false;
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving exit survey for company {CompanyId}", exitSurvey.CompanyId);
            return false;
        }
    }

    public async Task<bool> DeleteExitSurveyAsync(int surveyId, Guid companyId)
    {
        try
        {
            var survey = await _context.ExitSurveys
                .FirstOrDefaultAsync(e => e.Id == surveyId && e.CompanyId == companyId);

            if (survey == null)
                return false;

            _context.ExitSurveys.Remove(survey);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting exit survey {SurveyId} for company {CompanyId}", surveyId, companyId);
            return false;
        }
    }

    public async Task<bool> ToggleExitSurveyStatusAsync(int surveyId, Guid companyId)
    {
        try
        {
            var survey = await _context.ExitSurveys
                .FirstOrDefaultAsync(e => e.Id == surveyId && e.CompanyId == companyId);

            if (survey == null)
                return false;

            survey.IsActive = !survey.IsActive;
            survey.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling exit survey status {SurveyId} for company {CompanyId}", surveyId, companyId);
            return false;
        }
    }

    public async Task<ExitSurveyScriptConfig> GetScriptConfigAsync(Guid companyId)
    {
        try
        {
            var hasModule = await HasExitSurveyModuleAsync(companyId);
            if (!hasModule)
            {
                return new ExitSurveyScriptConfig { CompanyId = companyId, IsActive = false };
            }

            var exitSurvey = await _context.ExitSurveys
                .Include(e => e.Questions.Where(q => q.IsActive))
                .FirstOrDefaultAsync(e => e.CompanyId == companyId && e.IsActive);
            if (exitSurvey == null)
            {
                return new ExitSurveyScriptConfig { CompanyId = companyId, IsActive = false };
            }

            return new ExitSurveyScriptConfig
            {
                CompanyId = companyId,
                IsActive = exitSurvey.IsActive,
                Title = exitSurvey.Title,
                Description = exitSurvey.Description,
                SubmitButtonText = exitSurvey.SubmitButtonText,
                CancelButtonText = exitSurvey.CancelButtonText,
                ThankYouMessage = exitSurvey.ThankYouMessage,
                BackgroundColor = exitSurvey.BackgroundColor,
                TextColor = exitSurvey.TextColor,
                SubmitButtonColor = exitSurvey.SubmitButtonColor,
                CancelButtonColor = exitSurvey.CancelButtonColor,
                BorderRadius = exitSurvey.BorderRadius,
                EnableAnimation = exitSurvey.EnableAnimation,
                ShowOnPageExit = exitSurvey.ShowOnPageExit,
                ShowOnTabClose = exitSurvey.ShowOnTabClose,
                DelayBeforeShow = exitSurvey.DelayBeforeShow,
                ShowFrequencyDays = exitSurvey.ShowFrequencyDays,
                ShowOnMobile = exitSurvey.ShowOnMobile,
                MobileDelayMinutes = exitSurvey.MobileDelayMinutes,
                ShowOnDesktop = exitSurvey.ShowOnDesktop,
                Questions = exitSurvey.Questions
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting script config for company {CompanyId}", companyId);
            return new ExitSurveyScriptConfig { CompanyId = companyId, IsActive = false };
        }
    }

    public async Task<List<ExitSurveyQuestion>> GetQuestionsAsync(int exitSurveyId)
    {
        try
        {
            return await _context.ExitSurveyQuestions
                .Where(q => q.ExitSurveyId == exitSurveyId && q.IsActive)
                .OrderBy(q => q.SortOrder)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting questions for exit survey {ExitSurveyId}", exitSurveyId);
            return new List<ExitSurveyQuestion>();
        }
    }

    public async Task<bool> SaveQuestionAsync(ExitSurveyQuestion question)
    {
        try
        {
            question.CreatedAt = DateTime.Now;
            _context.ExitSurveyQuestions.Add(question);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving question {QuestionId}", question.Id);
            return false;
        }
    }

    public async Task<bool> DeleteQuestionAsync(int questionId, Guid companyId)
    {
        try
        {
            var question = await _context.ExitSurveyQuestions
                .Include(q => q.ExitSurvey)
                .FirstOrDefaultAsync(q => q.Id == questionId && q.ExitSurvey.CompanyId == companyId);

            if (question != null)
            {
                question.IsActive = false;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting question {QuestionId} for company {CompanyId}", questionId, companyId);
            return false;
        }
    }

    public async Task<bool> SubmitResponseAsync(SubmitExitSurveyResponseViewModel model, string? ipAddress, string? userAgent)
    {
        try
        {
            // Modül kontrolü
            var hasModule = await HasExitSurveyModuleAsync(model.CompanyId);
            if (!hasModule)
            {
                return false;
            }

            var exitSurvey = await _context.ExitSurveys
                .Include(e => e.Questions.Where(q => q.IsActive))
                .FirstOrDefaultAsync(e => e.CompanyId == model.CompanyId && e.IsActive);
            if (exitSurvey == null)
            {
                return false;
            }

            var validQuestionIds = exitSurvey.Questions
                .Where(q => q.IsActive)
                .Select(q => q.Id)
                .ToHashSet();

            foreach (var answer in model.Responses)
            {
                if (!validQuestionIds.Contains(answer.QuestionId))
                    continue;

                int? ratingValue = null;
                if (int.TryParse(answer.Answer, out var rv))
                {
                    var question = exitSurvey.Questions.FirstOrDefault(q => q.Id == answer.QuestionId);
                    if (question != null && (question.QuestionType == ExitSurveyQuestionType.NpsRating || question.QuestionType == ExitSurveyQuestionType.EmojiRating))
                        ratingValue = rv;
                }

                var response = new ExitSurveyResponse
                {
                    ExitSurveyId = exitSurvey.Id,
                    QuestionId = answer.QuestionId,
                    ResponseText = answer.Answer ?? string.Empty,
                    RatingValue = ratingValue,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    SessionId = model.SessionId ?? string.Empty,
                    PageUrl = model.PageUrl,
                    IsMobile = IsMobileDevice(userAgent),
                    CreatedAt = DateTime.Now
                };

                _context.ExitSurveyResponses.Add(response);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting response for company {CompanyId}", model.CompanyId);
            return false;
        }
    }

    public async Task<object?> GetQuestionAsync(int questionId, Guid companyId)
    {
        try
        {
            var question = await _context.ExitSurveyQuestions
                .Where(q => q.Id == questionId)
                .Where(q => _context.ExitSurveys.Any(es => es.Id == q.ExitSurveyId && es.CompanyId == companyId))
                .Select(q => new
                {
                    q.Id,
                    q.QuestionText,
                    q.QuestionType,
                    q.IsRequired,
                    q.OptionsJson,
                    q.SortOrder
                })
                .FirstOrDefaultAsync();

            if (question == null) return null;

            return new
            {
                question.Id,
                question.QuestionText,
                question.QuestionType,
                question.IsRequired,
                Options = string.IsNullOrEmpty(question.OptionsJson)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(question.OptionsJson) ?? new List<string>(),
                question.SortOrder
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting question {QuestionId} for company {CompanyId}", questionId, companyId);
            return null;
        }
    }

    public async Task<AllResponsesViewModel> GetAllResponsesViewModelAsync(int surveyId, Guid companyId, int page = 1, int pageSize = 10, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var exitSurvey = await GetExitSurveyByIdAsync(surveyId, companyId);
            if (exitSurvey == null)
            {
                return new AllResponsesViewModel
                {
                    Questions = new List<ExitSurveyQuestion>(),
                    SessionGroups = new List<ResponseSessionGroup>(),
                    TotalSessions = 0,
                    TotalResponses = 0,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalPages = 0,
                    StartDate = startDate,
                    EndDate = endDate
                };
            }

            var questions = await _context.ExitSurveyQuestions
                .Where(q => q.ExitSurveyId == exitSurvey.Id && q.IsActive)
                .OrderBy(q => q.SortOrder)
                .ToListAsync();

            var baseQuery = _context.ExitSurveyResponses
                .Where(r => r.ExitSurveyId == exitSurvey.Id);

            if (startDate.HasValue)
            {
                var utcStart = TimeZoneInfo.ConvertTimeToUtc(startDate.Value.Date, TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"));
                baseQuery = baseQuery.Where(r => r.CreatedAt >= utcStart);
            }
            if (endDate.HasValue)
            {
                var utcEnd = TimeZoneInfo.ConvertTimeToUtc(endDate.Value.Date.AddDays(1), TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"));
                baseQuery = baseQuery.Where(r => r.CreatedAt < utcEnd);
            }

            var totalSessions = await baseQuery
                .Select(r => r.SessionId)
                .Distinct()
                .CountAsync();

            var totalPages = (int)Math.Ceiling((double)totalSessions / pageSize);

            var pagedSessionIds = await baseQuery
                .GroupBy(r => r.SessionId)
                .Select(g => new { SessionId = g.Key, MinCreatedAt = g.Min(r => r.CreatedAt) })
                .OrderByDescending(g => g.MinCreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(g => g.SessionId)
                .ToListAsync();

            var responses = await _context.ExitSurveyResponses
                .Include(r => r.Question)
                .Where(r => r.ExitSurveyId == exitSurvey.Id && pagedSessionIds.Contains(r.SessionId))
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var sessionGroups = responses
                .GroupBy(r => r.SessionId)
                .Select(g => new ResponseSessionGroup
                {
                    SessionId = g.Key ?? string.Empty,
                    CreatedAt = g.Min(r => r.CreatedAt),
                    IpAddress = g.First().IpAddress,
                    UserAgent = g.First().UserAgent,
                    Responses = g.ToList()
                })
                .OrderByDescending(g => g.CreatedAt)
                .ToList();

            var totalResponses = await baseQuery.CountAsync();

            return new AllResponsesViewModel
            {
                Questions = questions,
                SessionGroups = sessionGroups,
                TotalSessions = totalSessions,
                TotalResponses = totalResponses,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages,
                StartDate = startDate,
                EndDate = endDate
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all responses for survey {SurveyId} company {CompanyId}", surveyId, companyId);
            return new AllResponsesViewModel
            {
                Questions = new List<ExitSurveyQuestion>(),
                SessionGroups = new List<ResponseSessionGroup>(),
                TotalSessions = 0,
                TotalResponses = 0,
                StartDate = startDate,
                EndDate = endDate
            };
        }
    }

    public async Task<bool> UpdateQuestionAsync(ExitSurveyQuestionViewModel model, Guid companyId)
    {
        try
        {
            var question = await _context.ExitSurveyQuestions
                .Include(q => q.ExitSurvey)
                .FirstOrDefaultAsync(q => q.Id == model.Id && q.ExitSurvey.CompanyId == companyId);

            if (question == null)
                return false;

            question.QuestionText = model.QuestionText;
            question.QuestionType = model.QuestionType;
            question.IsRequired = model.IsRequired;
            question.Options = model.Options;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating question {QuestionId} for company {CompanyId}", model.Id, companyId);
            return false;
        }
    }

    private async Task<List<QuestionStatsViewModel>> CalculateQuestionStatsAsync(int exitSurveyId)
    {
        var questions = await _context.ExitSurveyQuestions
            .Where(q => q.ExitSurveyId == exitSurveyId && q.IsActive)
            .OrderBy(q => q.SortOrder)
            .ToListAsync();

        var questionIds = questions.Select(q => q.Id).ToList();

        var allResponses = await _context.ExitSurveyResponses
            .Where(r => questionIds.Contains(r.QuestionId))
            .ToListAsync();

        var responsesByQuestion = allResponses
            .GroupBy(r => r.QuestionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var questionStats = new List<QuestionStatsViewModel>();

        foreach (var question in questions)
        {
            var responses = responsesByQuestion.GetValueOrDefault(question.Id, new List<ExitSurveyResponse>());

            var questionStat = new QuestionStatsViewModel
            {
                QuestionId = question.Id,
                QuestionText = question.QuestionText,
                QuestionType = question.QuestionType,
                ResponseCount = responses.Count
            };

            switch (question.QuestionType)
            {
                case ExitSurveyQuestionType.TextArea:
                    questionStat.TextResponses = responses
                        .Where(r => !string.IsNullOrEmpty(r.ResponseText))
                        .Select(r => r.ResponseText)
                        .ToList();
                    break;

                case ExitSurveyQuestionType.SingleChoice:
                    questionStat.OptionStats = CalculateOptionStats(responses, question.Options);
                    break;

                case ExitSurveyQuestionType.MultiChoice:
                    questionStat.OptionStats = CalculateOptionStats(responses, question.Options);
                    break;

                case ExitSurveyQuestionType.Boolean:
                    var yesNoOptions = new List<string> { "Evet", "Hayır" };
                    questionStat.OptionStats = CalculateOptionStats(responses, yesNoOptions);
                    break;

                case ExitSurveyQuestionType.NpsRating:
                    var ratingResponses = responses
                        .Where(r => r.RatingValue.HasValue)
                        .Select(r => r.RatingValue!.Value)
                        .ToList();

                    if (ratingResponses.Any())
                    {
                        questionStat.AverageRating = ratingResponses.Average();

                        var ratingStats = new List<OptionStatsViewModel>();
                        for (int star = 1; star <= 5; star++)
                        {
                            var count = ratingResponses.Count(r => r == star);
                            var percentage = responses.Count > 0 ? (double)count / responses.Count * 100 : 0;

                            ratingStats.Add(new OptionStatsViewModel
                            {
                                OptionText = $"{star} Yıldız",
                                Count = count,
                                Percentage = percentage
                            });
                        }

                        questionStat.OptionStats = ratingStats;
                    }
                    break;

                case ExitSurveyQuestionType.EmojiRating:
                    questionStat.OptionStats = CalculateOptionStats(responses, question.Options);
                    break;
            }

            questionStats.Add(questionStat);
        }

        return questionStats;
    }

    private List<OptionStatsViewModel> CalculateOptionStats(List<ExitSurveyResponse> responses, List<string> options)
    {
        var optionStats = new List<OptionStatsViewModel>();
        var totalResponses = responses.Count;

        foreach (var option in options)
        {
            var count = responses.Count(r => r.ResponseText == option);
            var percentage = totalResponses > 0 ? (double)count / totalResponses * 100 : 0;

            optionStats.Add(new OptionStatsViewModel
            {
                OptionText = option,
                Count = count,
                Percentage = percentage
            });
        }

        return optionStats;
    }

    public async Task<bool> RecordTrackingEventAsync(ExitSurveyTrackingViewModel model, string? ipAddress, string? userAgent)
    {
        try
        {
            var exitSurvey = await _context.ExitSurveys
                .FirstOrDefaultAsync(e => e.CompanyId == model.CompanyId && e.IsActive);

            if (exitSurvey == null)
                return false;

            if (model.EventType == "impression")
            {
                bool isMobile = !string.IsNullOrEmpty(userAgent) &&
                    System.Text.RegularExpressions.Regex.IsMatch(userAgent,
                        @"Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                var trackingEvent = new ExitSurveyEvent
                {
                    ExitSurveyId = exitSurvey.Id,
                    EventType = "impression",
                    PageUrl = model.PageUrl,
                    PageType = model.PageType,
                    SessionId = model.SessionId,
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    IsMobile = isMobile,
                    CreatedAt = DateTime.Now
                };

                _context.ExitSurveyEvents.Add(trackingEvent);
                exitSurvey.ImpressionCount++;
            }
            else if (model.EventType == "submit")
            {
                var existingEvent = await _context.ExitSurveyEvents
                    .FirstOrDefaultAsync(e => e.SessionId == model.SessionId && e.ExitSurveyId == exitSurvey.Id);

                if (existingEvent != null)
                    existingEvent.EventType = "submit";
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording tracking event {EventType} for company {CompanyId}", model.EventType, model.CompanyId);
            return false;
        }
    }

    public async Task<ExitSurveyStatsViewModel> GetDashboardStatsAsync(Guid companyId)
    {
        try
        {
            var surveys = await _context.ExitSurveys
                .Include(e => e.Questions.Where(q => q.IsActive))
                .Where(e => e.CompanyId == companyId)
                .ToListAsync();

            if (!surveys.Any())
                return new ExitSurveyStatsViewModel();

            var surveyIds = surveys.Select(s => s.Id).ToList();

            var events = await _context.ExitSurveyEvents
                .Where(e => surveyIds.Contains(e.ExitSurveyId))
                .ToListAsync();

            var responses = await _context.ExitSurveyResponses
                .Where(r => surveyIds.Contains(r.ExitSurveyId))
                .ToListAsync();

            var allQuestions = surveys.SelectMany(s => s.Questions).ToList();

            var totalImpressions = events.Count;
            var totalSubmissions = events.Count(e => e.EventType == "submit");

            var stats = new ExitSurveyStatsViewModel
            {
                TotalSessions = responses.Select(r => r.SessionId).Distinct().Count(),
                TotalResponses = responses.Count,
                TotalQuestions = allQuestions.Count,
                TotalImpressions = totalImpressions,
                TotalDismissals = totalImpressions - totalSubmissions,
                TotalSubmissions = totalSubmissions,
                MobileCount = events.Count(e => e.IsMobile),
                DesktopCount = events.Count(e => !e.IsMobile)
            };

            stats.StageDistribution = CalculateStageDistributionFromEvents(events);
            stats.TopExitReasons = CalculateTopExitReasons(responses, allQuestions);

            var ratingResponses = responses
                .Where(r => r.RatingValue.HasValue)
                .Select(r => r.RatingValue!.Value)
                .ToList();

            if (ratingResponses.Any())
                stats.AverageRating = ratingResponses.Average();

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard stats for company {CompanyId}", companyId);
            return new ExitSurveyStatsViewModel();
        }
    }

    public async Task<ExitSurveyStatsViewModel> GetSurveyStatsAsync(int surveyId, Guid companyId)
    {
        try
        {
            var exitSurvey = await GetExitSurveyByIdAsync(surveyId, companyId);
            if (exitSurvey == null)
                return new ExitSurveyStatsViewModel();

            var events = await _context.ExitSurveyEvents
                .Where(e => e.ExitSurveyId == exitSurvey.Id)
                .ToListAsync();

            var responses = await _context.ExitSurveyResponses
                .Where(r => r.ExitSurveyId == exitSurvey.Id)
                .ToListAsync();

            var totalImpressions = events.Count;
            var totalSubmissions = events.Count(e => e.EventType == "submit");

            var stats = new ExitSurveyStatsViewModel
            {
                TotalSessions = responses.Select(r => r.SessionId).Distinct().Count(),
                TotalResponses = responses.Count,
                TotalQuestions = exitSurvey.Questions.Count(q => q.IsActive),
                TotalImpressions = totalImpressions,
                TotalDismissals = totalImpressions - totalSubmissions,
                TotalSubmissions = totalSubmissions,
                MobileCount = events.Count(e => e.IsMobile),
                DesktopCount = events.Count(e => !e.IsMobile)
            };

            stats.StageDistribution = CalculateStageDistributionFromEvents(events);
            stats.TopExitReasons = CalculateTopExitReasons(responses, exitSurvey.Questions.ToList());

            var ratingResponses = responses
                .Where(r => r.RatingValue.HasValue)
                .Select(r => r.RatingValue!.Value)
                .ToList();

            if (ratingResponses.Any())
                stats.AverageRating = ratingResponses.Average();

            stats.QuestionStats = await CalculateQuestionStatsAsync(exitSurvey.Id);

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting survey stats for survey {SurveyId} company {CompanyId}", surveyId, companyId);
            return new ExitSurveyStatsViewModel();
        }
    }

    private ExitStageDistribution CalculateStageDistributionFromEvents(List<ExitSurveyEvent> events)
    {
        var distribution = new ExitStageDistribution();

        foreach (var ev in events)
        {
            var pt = (ev.PageType ?? "").ToLowerInvariant();
            switch (pt)
            {
                case "homepage":
                    distribution.HomePage++;
                    break;
                case "category":
                    distribution.CategoryPage++;
                    break;
                case "product":
                    distribution.ProductPage++;
                    break;
                case "cart":
                    distribution.CartPage++;
                    break;
                case "checkout":
                    distribution.CheckoutPage++;
                    break;
                default:
                    distribution.OtherPage++;
                    break;
            }
        }

        return distribution;
    }

    private List<TopExitReasonViewModel> CalculateTopExitReasons(List<ExitSurveyResponse> responses, List<ExitSurveyQuestion> questions)
    {
        var multipleChoiceQuestionIds = questions
            .Where(q => q.IsActive && (q.QuestionType == ExitSurveyQuestionType.SingleChoice || q.QuestionType == ExitSurveyQuestionType.MultiChoice || q.QuestionType == ExitSurveyQuestionType.Boolean || q.QuestionType == ExitSurveyQuestionType.EmojiRating))
            .Select(q => q.Id)
            .ToHashSet();

        var relevantResponses = responses
            .Where(r => multipleChoiceQuestionIds.Contains(r.QuestionId) && !string.IsNullOrEmpty(r.ResponseText))
            .ToList();

        if (!relevantResponses.Any())
            return new List<TopExitReasonViewModel>();

        var totalCount = relevantResponses.Count;

        return relevantResponses
            .GroupBy(r => r.ResponseText)
            .OrderByDescending(g => g.Count())
            .Take(3)
            .Select(g => new TopExitReasonViewModel
            {
                Reason = g.Key,
                Count = g.Count(),
                Percentage = Math.Round((double)g.Count() / totalCount * 100, 1)
            })
            .ToList();
    }

    private static bool IsMobileDevice(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return false;

        var ua = userAgent.ToLowerInvariant();
        return ua.Contains("mobile") || ua.Contains("android") || ua.Contains("iphone") || ua.Contains("ipad") || ua.Contains("ipod");
    }

    public async Task<string> ExportExitSurveyToR2Async(Guid companyId)
    {
        try
        {
            _logger.LogInformation("Starting R2 export for exit survey. CompanyId: {CompanyId}", companyId);

            var hasActive = await _context.ExitSurveys.AnyAsync(e => e.CompanyId == companyId && e.IsActive);

            if (!hasActive)
            {
                await _r2StorageService.DeleteFileAsync($"{companyId}/exitSurvey.json");
                await _r2StorageService.DeleteFileAsync($"{companyId}/exitSurvey.js");

                _logger.LogInformation("Removed exit survey files from R2 for company {CompanyId} (no active surveys)", companyId);
                return string.Empty;
            }

            var config = await GetScriptConfigAsync(companyId);

            var allData = new
            {
                CompanyId = companyId,
                UpdatedAt = DateTime.Now,
                Config = config
            };

            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            var jsonContent = JsonSerializer.Serialize(allData, jsonOptions);

            var jsonKey = $"{companyId}/exitSurvey.json";
            var jsonUrl = await _r2StorageService.UploadJsonAsync(jsonKey, jsonContent);

            var jsFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "js", "modules", "exit-survey-module.js");
            if (File.Exists(jsFilePath))
            {
                var jsScript = await File.ReadAllTextAsync(jsFilePath);
                var jsKey = $"{companyId}/exitSurvey.js";
                var jsUrl = await _r2StorageService.UploadJavaScriptAsync(jsKey, jsScript);

                _logger.LogInformation("Successfully exported exit survey to R2 for company {CompanyId}. JSON: {JsonUrl}, JS: {JsUrl}",
                    companyId, jsonUrl, jsUrl);
            }
            else
            {
                _logger.LogWarning("Exit survey module JS file not found at {Path}", jsFilePath);
            }

            return jsonUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting exit survey to R2 for company {CompanyId}", companyId);
            throw;
        }
    }
}
