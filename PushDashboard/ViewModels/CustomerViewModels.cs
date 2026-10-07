using PushDashboard.Models;
using System.ComponentModel.DataAnnotations;

namespace PushDashboard.ViewModels;

public class CustomerIndexViewModel
{
    public List<CustomerViewModel> Customers { get; set; } = new();
    public CustomerStatsViewModel Stats { get; set; } = new();
    public List<CustomerSyncLogViewModel> RecentSyncLogs { get; set; } = new();
    public PaginationViewModel Pagination { get; set; } = new();

    public class CustomerViewModel
    {
        public int Id { get; set; }
        public int ExternalId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? MobilePhone { get; set; }
        public bool IsActive { get; set; }
        public string StatusText { get; set; } = string.Empty;
        public string StatusBadgeClass { get; set; } = string.Empty;
        public DateTime? LastLoginDate { get; set; }
        public DateTime MembershipDate { get; set; }
        public string? City { get; set; }
        public string? CustomerCode { get; set; }
        public int PointBalance { get; set; }
        public decimal CreditLimit { get; set; }
        public bool EmailPermission { get; set; }
        public bool SmsPermission { get; set; }
        public bool KvkkApproval { get; set; }
        public bool MembershipAgreementApproval { get; set; }
        public bool CashOnDeliveryBlocked { get; set; }
        public DateTime? LastSyncDate { get; set; }
        public DateTime? BirthDate { get; set; }
        
        // Müşteri kaynağı bilgisi
        public string Source { get; set; } = string.Empty;
        public string SourceText { get; set; } = string.Empty;
        public string SourceBadgeClass { get; set; } = string.Empty;
        public bool IsRegisteredCustomer { get; set; }
        public string? District { get; set; }
        public string? MembershipType { get; set; }
        public string? LastLoginIp { get; set; }
        public DateTime LastUpdateDate { get; set; }
        public string? Profession { get; set; }
        public string? EducationLevel { get; set; }
        public int? GenderId { get; set; }

        public string FormattedMembershipDate => MembershipDate.ToString("dd.MM.yyyy");
        public string FormattedLastLoginDate
        {
            get
            {
                // Null veya default değer (0001-01-01) kontrolü
                if (!LastLoginDate.HasValue || LastLoginDate.Value == DateTime.MinValue || LastLoginDate.Value.Year < 2000)
                {
                    return "Hiç giriş yapılmamış";
                }
                return LastLoginDate.Value.ToString("dd.MM.yyyy HH:mm");
            }
        }
        public string FormattedCreditLimit => $"₺{CreditLimit:N2}";
        public string FormattedLastSyncDate => LastSyncDate?.ToString("dd.MM.yyyy HH:mm") ?? "Henüz senkronize edilmemiş";

        public static CustomerViewModel FromCustomer(Customer customer)
        {
            return new CustomerViewModel
            {
                Id = customer.Id,
                ExternalId = customer.ExternalId,
                FullName = customer.FullName,
                Email = customer.Email,
                Phone = customer.Phone,
                MobilePhone = customer.MobilePhone,
                IsActive = customer.IsActive,
                StatusText = customer.StatusText,
                StatusBadgeClass = customer.StatusBadgeClass,
                LastLoginDate = customer.LastLoginDate,
                MembershipDate = customer.MembershipDate,
                City = customer.City,
                CustomerCode = customer.CustomerCode,
                PointBalance = customer.PointBalance,
                CreditLimit = customer.CreditLimit,
                EmailPermission = customer.EmailPermission,
                SmsPermission = customer.SmsPermission,
                KvkkApproval = customer.KvkkApproval,
                MembershipAgreementApproval = customer.MembershipAgreementApproval,
                CashOnDeliveryBlocked = customer.CashOnDeliveryBlocked,
                LastSyncDate = customer.LastSyncDate,
                BirthDate = customer.BirthDate,
                District = customer.District,
                MembershipType = customer.MembershipType,
                LastLoginIp = customer.LastLoginIp,
                LastUpdateDate = customer.LastUpdateDate,
                Profession = customer.Profession,
                EducationLevel = customer.EducationLevel,
                GenderId = customer.GenderId,
                // Kaynak bilgisi
                Source = customer.Source.ToString(),
                SourceText = customer.SourceText,
                SourceBadgeClass = customer.SourceBadgeClass,
                IsRegisteredCustomer = customer.IsRegisteredCustomer
            };
        }
    }

    public class CustomerStatsViewModel
    {
        public int TotalCustomers { get; set; }
        public int ActiveCustomers { get; set; }
        public int InactiveCustomers { get; set; }
        public int NewCustomersThisMonth { get; set; }
        public int SmsPermittedCustomers { get; set; }
        public DateTime? LastSyncDate { get; set; }

        public string FormattedLastSync => LastSyncDate?.ToString("dd.MM.yyyy HH:mm") ?? "Henüz senkronize edilmemiş";
        public double ActiveCustomerPercentage => TotalCustomers > 0 ? (double)ActiveCustomers / TotalCustomers * 100 : 0;
        public double SmsPermittedPercentage => TotalCustomers > 0 ? (double)SmsPermittedCustomers / TotalCustomers * 100 : 0;
    }

    public class CustomerSyncLogViewModel
    {
        public int Id { get; set; }
        public DateTime SyncStartTime { get; set; }
        public DateTime? SyncEndTime { get; set; }
        public int TotalRecordsProcessed { get; set; }
        public int NewRecordsAdded { get; set; }
        public int RecordsUpdated { get; set; }
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Notes { get; set; }

        public string FormattedSyncStartTime => SyncStartTime.ToString("dd.MM.yyyy HH:mm");
        public string FormattedSyncEndTime => SyncEndTime?.ToString("dd.MM.yyyy HH:mm") ?? "Devam ediyor";
        public string StatusText => IsSuccessful ? "Başarılı" : "Hatalı";
        public string StatusBadgeClass => IsSuccessful ? "bg-green-100 text-green-800" : "bg-red-100 text-red-800";
        public TimeSpan? Duration => SyncEndTime.HasValue ? SyncEndTime.Value - SyncStartTime : null;
        public string FormattedDuration => Duration?.ToString(@"mm\:ss") ?? "Hesaplanıyor";

        public static CustomerSyncLogViewModel FromSyncLog(SyncLog log)
        {
            return new CustomerSyncLogViewModel
            {
                Id = log.Id,
                SyncStartTime = log.SyncStartTime,
                SyncEndTime = log.SyncEndTime,
                TotalRecordsProcessed = log.TotalRecordsProcessed,
                NewRecordsAdded = log.NewRecordsAdded,
                RecordsUpdated = log.RecordsUpdated,
                IsSuccessful = log.IsSuccessful,
                ErrorMessage = log.ErrorMessage,
                Notes = log.Notes
            };
        }
    }

    public class PaginationViewModel
    {
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalRecords { get; set; } = 0;
        public int PageSize { get; set; } = 50;
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public int StartRecord => (CurrentPage - 1) * PageSize + 1;
        public int EndRecord => Math.Min(CurrentPage * PageSize, TotalRecords);
    }
}

public class CreateCustomerViewModel
{
    [Required(ErrorMessage = "Ad gereklidir")]
    [StringLength(100, ErrorMessage = "Ad en fazla 100 karakter olabilir")]
    [RegularExpression(@"^[^0-9]*$", ErrorMessage = "{0} alanı sayı içeremez.")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Soyad gereklidir")]
    [StringLength(100, ErrorMessage = "Soyad en fazla 100 karakter olabilir")]
    [RegularExpression(@"^[^0-9]*$", ErrorMessage = "{0} alanı sayı içeremez.")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta gereklidir")]
    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    [StringLength(200, ErrorMessage = "E-posta en fazla 200 karakter olabilir")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
    [StringLength(20, ErrorMessage = "Telefon en fazla 20 karakter olabilir")]
    public string? Phone { get; set; }

    [Phone(ErrorMessage = "Geçerli bir cep telefonu numarası giriniz")]
    [StringLength(20, ErrorMessage = "Cep telefonu en fazla 20 karakter olabilir")]
    public string? MobilePhone { get; set; }

    public bool IsActive { get; set; } = true;

    public bool EmailPermission { get; set; } = false;

    public bool SmsPermission { get; set; } = false;

    public DateTime? BirthDate { get; set; }

    public int? GenderId { get; set; }

    [StringLength(100, ErrorMessage = "Şehir en fazla 100 karakter olabilir")]
    public string? City { get; set; }

    [StringLength(100, ErrorMessage = "İlçe en fazla 100 karakter olabilir")]
    public string? District { get; set; }

    [StringLength(100, ErrorMessage = "Müşteri kodu en fazla 100 karakter olabilir")]
    [RegularExpression(@"^[a-zA-Z0-9_-]*$", ErrorMessage = "Müşteri kodu sadece harf, rakam, tire ve alt çizgi içerebilir")]
    public string? CustomerCode { get; set; }

    [StringLength(100, ErrorMessage = "Üyelik tipi en fazla 100 karakter olabilir")]
    public string? MembershipType { get; set; }

    public DateTime MembershipDate { get; set; } = DateTime.Now;

    [Range(0, int.MaxValue, ErrorMessage = "Puan bakiyesi 0 veya pozitif bir sayı olmalıdır")]
    public int PointBalance { get; set; } = 0;

    [Range(0, 999999999.99, ErrorMessage = "Kredi limiti 0 ile 999,999,999.99 arasında olmalıdır")]
    public decimal CreditLimit { get; set; } = 0.00m;

    public bool KvkkApproval { get; set; } = false;

    public bool MembershipAgreementApproval { get; set; } = false;

    [StringLength(100, ErrorMessage = "Meslek en fazla 100 karakter olabilir")]
    public string? Profession { get; set; }

    [StringLength(100, ErrorMessage = "Eğitim seviyesi en fazla 100 karakter olabilir")]
    public string? EducationLevel { get; set; }
}

public class CustomerImportJobViewModel
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public string? ErrorDetails { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;

    // Helper properties
    public int ProgressPercentage { get; set; }
    public string StatusBadgeClass { get; set; } = string.Empty;
    public string FormattedCreatedAt => CreatedAt.ToString("dd.MM.yyyy HH:mm");
    public string FormattedStartedAt => StartedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-";
    public string FormattedCompletedAt => CompletedAt?.ToString("dd.MM.yyyy HH:mm") ?? "-";
    public TimeSpan? Duration => CompletedAt.HasValue && StartedAt.HasValue ? CompletedAt.Value - StartedAt.Value : null;
    public string FormattedDuration => Duration?.ToString(@"mm\:ss") ?? "-";

    public static CustomerImportJobViewModel FromImportJob(CustomerImportJob job)
    {
        return new CustomerImportJobViewModel
        {
            Id = job.Id,
            FileName = job.FileName,
            Status = job.Status,
            TotalRows = job.TotalRows,
            ProcessedRows = job.ProcessedRows,
            SuccessCount = job.SuccessCount,
            ErrorCount = job.ErrorCount,
            ErrorDetails = job.ErrorDetails,
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            CreatedByUserId = job.CreatedByUserId,
            ProgressPercentage = job.ProgressPercentage,
            StatusBadgeClass = job.StatusBadgeClass
        };
    }
}

public class CustomerImportProgressViewModel
{
    public int JobId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int ProcessedRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public string? ErrorDetails { get; set; }
    public int ProgressPercentage { get; set; }
    public string StatusBadgeClass { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    // Formatted properties
    public string? FormattedStartedAt => StartedAt?.ToString("dd.MM.yyyy HH:mm");
    public string? FormattedCompletedAt => CompletedAt?.ToString("dd.MM.yyyy HH:mm");

    public string? FormattedDuration
    {
        get
        {
            if (StartedAt == null) return null;

            var endTime = CompletedAt ?? DateTime.Now;
            var duration = endTime - StartedAt.Value;

            if (duration.TotalMinutes < 1)
                return $"{duration.Seconds} saniye";
            else if (duration.TotalHours < 1)
                return $"{duration.Minutes} dakika {duration.Seconds} saniye";
            else
                return $"{duration.Hours} saat {duration.Minutes} dakika";
        }
    }
}
