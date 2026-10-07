using PushDashboard.Models;
using System.ComponentModel.DataAnnotations;

namespace PushDashboard.ViewModels;

public class BasketReminderIndexViewModel
{
    public BasketReminderSettings? Settings { get; set; }
    public List<BasketReminderSchedule> Schedules { get; set; } = new();
    public List<BasketReminderLog> RecentLogs { get; set; } = new();
    public BasketReminderStatsViewModel Stats { get; set; } = new();
    public bool HasEcommerceIntegration { get; set; }
    public string? EcommerceIntegrationMessage { get; set; }
}

public class BasketReminderStatsViewModel
{
    public int TotalRemindersToday { get; set; }
    public int SuccessfulRemindersToday { get; set; }
    public int FailedRemindersToday { get; set; }
    public int TotalRemindersThisWeek { get; set; }
    public int TotalRemindersThisMonth { get; set; }
    public int ActiveSchedulesCount { get; set; }
    public int EligibleBasketsCount { get; set; }
    public decimal SuccessRateToday { get; set; }
    public decimal SuccessRateThisWeek { get; set; }

    // Chart data
    public List<DailyReminderStatsViewModel> DailyStats { get; set; } = new();
}

public class DailyReminderStatsViewModel
{
    public DateTime Date { get; set; }
    public int TotalReminders { get; set; }
    public int SuccessfulReminders { get; set; }
    public int FailedReminders { get; set; }
    public string DateText => Date.ToString("dd.MM");
}

public class BasketReminderSettingsViewModel
{
    [Required(ErrorMessage = "Maksimum bildirim sayısı gereklidir")]
    [Range(1, 2, ErrorMessage = "Maksimum bildirim sayısı 1-2 arasında olmalıdır")]
    public int MaxNotificationsPerCustomer { get; set; } = 2;

    [Required(ErrorMessage = "Bildirim içeriği gereklidir")]
    [StringLength(200, ErrorMessage = "Bildirim içeriği maksimum 200 karakter olabilir")]
    public string NotificationContent { get; set; } = string.Empty;

    [Required]
    public bool IsEnabled { get; set; } = true;

    // Helper properties
    public int RemainingCharacters => 200 - (NotificationContent?.Length ?? 0);
    public List<string> AvailablePlaceholders { get; set; } = new()
    {
        "{FirstName}", "{LastName}", "{FullName}"
    };
}

public class BasketReminderScheduleViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Zamanlama adı gereklidir")]
    [StringLength(100, ErrorMessage = "Zamanlama adı maksimum 100 karakter olabilir")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Hatırlatma zamanı gereklidir")]
    [Range(1, 8760, ErrorMessage = "Hatırlatma zamanı 1 saat ile 1 yıl (8760 saat) arasında olmalıdır")]
    public int ReminderTimeForHours { get; set; }

    [Required(ErrorMessage = "Maksimum bildirim sayısı gereklidir")]
    [Range(1, 3, ErrorMessage = "Maksimum bildirim sayısı 1-3 arasında olmalıdır")]
    public int MaxNotificationsPerCustomer { get; set; } = 2;

    [Required(ErrorMessage = "Bildirim içeriği gereklidir")]
    [StringLength(300, MinimumLength = 10, ErrorMessage = "Bildirim içeriği 10-300 karakter arasında olmalıdır")]
    public string NotificationContent { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    // Helper properties
    public string ReminderTimeText
    {
        get
        {
            return ReminderTimeForHours switch
            {
                6 => "6 Saat",
                24 => "1 Gün",
                168 => "1 Hafta",
                _ => $"{ReminderTimeForHours} Saat"
            };
        }
    }

    public List<ReminderTimeOption> AvailableTimeOptions { get; set; } = new()
    {
        new() { Value = 6, Text = "6 Saat" },
        new() { Value = 24, Text = "1 Gün" },
        new() { Value = 168, Text = "1 Hafta" }
    };
}

public class ReminderTimeOption
{
    public int Value { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class BasketReminderLogViewModel
{
    public List<BasketReminderLog> Logs { get; set; } = new();
    public PaginationViewModel Pagination { get; set; } = new();
    public BasketReminderLogFilterViewModel Filter { get; set; } = new();

    public class PaginationViewModel
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public bool HasPrevious => CurrentPage > 1;
        public bool HasNext => CurrentPage < TotalPages;
    }
}

public class BasketReminderLogFilterViewModel
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? IsSuccessful { get; set; }
    public int? ReminderTimeForHours { get; set; }
    public string? CustomerEmail { get; set; }

    public List<ReminderTimeOption> AvailableTimeOptions { get; set; } = new()
    {
        new() { Value = 6, Text = "6 Saat" },
        new() { Value = 24, Text = "1 Gün" },
        new() { Value = 168, Text = "1 Hafta" }
    };
}

public class ProcessRemindersResultViewModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int ProcessedCount { get; set; }
    public int SuccessfulCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> Errors { get; set; } = new();
}
