namespace PushDashboard.Models.ViewModels;

public class ExitSurveyIndexViewModel
{
    public List<ExitSurvey> Surveys { get; set; } = new();
    public ExitSurveyStatsViewModel Stats { get; set; } = new();
}

public class ExitSurveyViewModel
{
    public ExitSurvey? ExitSurvey { get; set; }
    public List<ExitSurveyQuestion> Questions { get; set; } = new();
    public ExitSurveyStatsViewModel Stats { get; set; } = new();
}

public class CreateExitSurveyViewModel
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Soru ekleme/düzenleme için view model
/// </summary>
public class ExitSurveyQuestionViewModel
{
    public int Id { get; set; }
    public int ExitSurveyId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public ExitSurveyQuestionType QuestionType { get; set; }
    public int SortOrder { get; set; }
    public bool IsRequired { get; set; }
    public bool IsActive { get; set; } = true;
    public List<string> Options { get; set; } = new();
}

/// <summary>
/// Anket istatistikleri için view model
/// </summary>
public class ExitSurveyStatsViewModel
{
    public int TotalSessions { get; set; }
    public ExitStageDistribution StageDistribution { get; set; } = new();
    public List<TopExitReasonViewModel> TopExitReasons { get; set; } = new();
    public int MobileCount { get; set; }
    public int DesktopCount { get; set; }
    public int TotalImpressions { get; set; }
    public int TotalSubmissions { get; set; }
    public int TotalDismissals { get; set; }

    public int TotalResponses { get; set; }
    public int TotalQuestions { get; set; }
    public double AverageRating { get; set; }
    public List<QuestionStatsViewModel> QuestionStats { get; set; } = new();
}

public class ExitStageDistribution
{
    public int HomePage { get; set; }
    public int CategoryPage { get; set; }
    public int ProductPage { get; set; }
    public int CartPage { get; set; }
    public int CheckoutPage { get; set; }
    public int OtherPage { get; set; }
}

public class TopExitReasonViewModel
{
    public string Reason { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Soru bazlı istatistikler
/// </summary>
public class QuestionStatsViewModel
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public ExitSurveyQuestionType QuestionType { get; set; }
    public int ResponseCount { get; set; }
    public List<OptionStatsViewModel> OptionStats { get; set; } = new();
    public double? AverageRating { get; set; }
    public List<string> TextResponses { get; set; } = new();
}

/// <summary>
/// Seçenek bazlı istatistikler
/// </summary>
public class OptionStatsViewModel
{
    public string OptionText { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Anket ayarları kaydetme için view model
/// </summary>
public class SaveExitSurveyViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SubmitButtonText { get; set; } = string.Empty;
    public string CancelButtonText { get; set; } = string.Empty;
    public string ThankYouMessage { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = string.Empty;
    public string TextColor { get; set; } = string.Empty;
    public string SubmitButtonColor { get; set; } = string.Empty;
    public string CancelButtonColor { get; set; } = string.Empty;
    public string BorderRadius { get; set; } = string.Empty;
    public bool EnableAnimation { get; set; }
    public bool ShowOnPageExit { get; set; }
    public bool ShowOnTabClose { get; set; }
    public int DelayBeforeShow { get; set; }
    public int ShowFrequencyDays { get; set; }
    public bool ShowOnMobile { get; set; }
    public int MobileDelayMinutes { get; set; }
    public bool ShowOnDesktop { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Cevap gönderme için view model
/// </summary>
public class SubmitExitSurveyResponseViewModel
{
    public Guid CompanyId { get; set; }
    public List<ExitSurveyAnswerViewModel> Responses { get; set; } = new();
    public string? SessionId { get; set; }
    public string? PageUrl { get; set; }
}

public class ExitSurveyTrackingViewModel
{
    public Guid CompanyId { get; set; }
    public string EventType { get; set; } = "impression";
    public string? PageUrl { get; set; }
    public string? PageType { get; set; }
    public string? SessionId { get; set; }
}

/// <summary>
/// Tek bir cevap için view model
/// </summary>
public class ExitSurveyAnswerViewModel
{
    public int QuestionId { get; set; }
    public string Answer { get; set; } = string.Empty;
    public int? RatingValue { get; set; }
}

/// <summary>
/// Script konfigürasyonu için view model
/// </summary>
public class ExitSurveyScriptConfig
{
    public Guid CompanyId { get; set; }
    public bool IsActive { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SubmitButtonText { get; set; } = string.Empty;
    public string CancelButtonText { get; set; } = string.Empty;
    public string ThankYouMessage { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = string.Empty;
    public string TextColor { get; set; } = string.Empty;
    public string SubmitButtonColor { get; set; } = string.Empty;
    public string CancelButtonColor { get; set; } = string.Empty;
    public string BorderRadius { get; set; } = string.Empty;
    public bool EnableAnimation { get; set; }
    public bool ShowOnPageExit { get; set; }
    public bool ShowOnTabClose { get; set; }
    public int DelayBeforeShow { get; set; }
    public int ShowFrequencyDays { get; set; }
    public bool ShowOnMobile { get; set; }
    public int MobileDelayMinutes { get; set; }
    public bool ShowOnDesktop { get; set; }
    public List<ExitSurveyQuestionConfig> Questions { get; set; } = new();
}

/// <summary>
/// Script için soru konfigürasyonu
/// </summary>
public class ExitSurveyQuestionConfig
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public ExitSurveyQuestionType QuestionType { get; set; }
    public bool IsRequired { get; set; }
    public List<string> Options { get; set; } = new();
}

/// <summary>
/// Tüm cevapları görüntüleme için view model
/// </summary>
public class AllResponsesViewModel
{
    public List<ExitSurveyQuestion> Questions { get; set; } = new();
    public List<ResponseSessionGroup> SessionGroups { get; set; } = new();
    public int TotalSessions { get; set; }
    public int TotalResponses { get; set; }

    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalPages { get; set; }
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Session bazlı cevap grubu
/// </summary>
public class ResponseSessionGroup
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public List<ExitSurveyResponse> Responses { get; set; } = new();
}
