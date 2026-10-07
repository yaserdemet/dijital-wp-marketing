using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace PushDashboard.Models;

/// <summary>
/// Şirket bazlı çıkış anketi ayarları
/// </summary>
public class ExitSurvey
{
    public int Id { get; set; }

    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public string AdminDescription { get; set; } = string.Empty;

    // Anket Ayarları
    public string Title { get; set; } = "Görüşünüz Bizim İçin Önemli";

    public string Description { get; set; } = "Sitemizi terk etmeden önce deneyiminizi bizimle paylaşır mısınız? Bu sadece birkaç saniye sürecek.";

    public string SubmitButtonText { get; set; } = "Gönder";

    public string CancelButtonText { get; set; } = "Kapat";

    public string ThankYouMessage { get; set; } = "Geri bildiriminiz için teşekkür ederiz!";

    // Tasarım Ayarları
    public string BackgroundColor { get; set; } = "#ffffff";

    public string TextColor { get; set; } = "#333333";

    public string SubmitButtonColor { get; set; } = "#4CAF50";

    public string CancelButtonColor { get; set; } = "#6c757d";

    public string BorderRadius { get; set; } = "8px";

    public bool EnableAnimation { get; set; } = true;

    // Davranış Ayarları
    public bool ShowOnPageExit { get; set; } = true;

    public bool ShowOnTabClose { get; set; } = true;

    public int DelayBeforeShow { get; set; } = 0; // milisaniye

    public int ShowFrequencyDays { get; set; } = 30; // kaç günde bir gösterilsin

    public bool ShowOnMobile { get; set; } = true;

    public int MobileDelayMinutes { get; set; } = 5; // mobilde kaç dakika sonra gösterilsin

    public bool ShowOnDesktop { get; set; } = true;

    // Genel Ayarlar
    public bool IsActive { get; set; } = true;

    // Tracking Sayaclari
    public int ImpressionCount { get; set; } = 0;

    public int DismissCount { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public string UpdatedByUserId { get; set; } = string.Empty;

    public virtual ApplicationUser UpdatedByUser { get; set; } = null!;

    // Navigation Properties
    public virtual ICollection<ExitSurveyQuestion> Questions { get; set; } = new List<ExitSurveyQuestion>();

    public virtual ICollection<ExitSurveyResponse> Responses { get; set; } = new List<ExitSurveyResponse>();

    public virtual ICollection<ExitSurveyEvent> Events { get; set; } = new List<ExitSurveyEvent>();
}

/// <summary>
/// Çıkış anketi soruları
/// </summary>
public class ExitSurveyQuestion
{
    public int Id { get; set; }

    public int ExitSurveyId { get; set; }

    public virtual ExitSurvey ExitSurvey { get; set; } = null!;

    public string QuestionText { get; set; } = string.Empty;

    public ExitSurveyQuestionType QuestionType { get; set; }

    public int SortOrder { get; set; } = 0;

    public bool IsRequired { get; set; } = false;

    public bool IsActive { get; set; } = true;

    // Seçenekler (JSON olarak saklanır)
    public string? OptionsJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Computed property for options
    [NotMapped]
    public List<string> Options
    {
        get
        {
            if (string.IsNullOrEmpty(OptionsJson))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(OptionsJson) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
        set
        {
            OptionsJson = JsonSerializer.Serialize(value);
        }
    }

    // Navigation Properties
    public virtual ICollection<ExitSurveyResponse> Responses { get; set; } = new List<ExitSurveyResponse>();
}

/// <summary>
/// Çıkış anketi cevapları
/// </summary>
public class ExitSurveyResponse
{
    public int Id { get; set; }

    public int ExitSurveyId { get; set; }

    public virtual ExitSurvey ExitSurvey { get; set; } = null!;

    public int QuestionId { get; set; }

    public virtual ExitSurveyQuestion Question { get; set; } = null!;

    public string ResponseText { get; set; } = string.Empty;

    public int? RatingValue { get; set; } // 1-5 arası puanlama için

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public string? SessionId { get; set; }

    public string? PageUrl { get; set; }

    public bool IsMobile { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Soru tipleri
/// </summary>
public class ExitSurveyEvent
{
    public int Id { get; set; }

    public int ExitSurveyId { get; set; }

    public virtual ExitSurvey ExitSurvey { get; set; } = null!;

    public string EventType { get; set; } = string.Empty;

    public string? PageUrl { get; set; }

    public string? PageType { get; set; }

    public string? SessionId { get; set; }

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public bool IsMobile { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public enum ExitSurveyQuestionType
{
    SingleChoice = 0,
    TextArea = 1,
    Boolean = 2,
    NpsRating = 3,
    MultiChoice = 4,
    EmojiRating = 5
}
