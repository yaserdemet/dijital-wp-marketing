using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

/// <summary>
/// Müşterinin kaydedildiği kaynak
/// </summary>
public enum CustomerSource
{
    /// <summary>E-ticaret platformundan senkronize edilen kayıtlı müşteri</summary>
    Ecommerce = 0,
    
    /// <summary>Hediye çarkından gelen potansiyel müşteri</summary>
    GiftWheel = 1,
    
    /// <summary>Manuel eklenen müşteri</summary>
    Manual = 2,
    
    /// <summary>Çıkış anketinden gelen müşteri</summary>
    ExitSurvey = 3,
    
    /// <summary>Stok hatırlatma formundan gelen müşteri</summary>
    StockReminder = 4,
    
    /// <summary>Pop-up formlardan gelen müşteri</summary>
    Popup = 5,

    /// <summary>Bültenden gelen müşteri</summary>
    Newsletter = 6
}

public class Customer
{
    public int Id { get; set; }

    // SOAP servisinden gelen ID
    public int ExternalId { get; set; }

    // Hangi şirkete ait olduğu
    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    [NotMapped]
    public string Name => $"{FirstName} {LastName}".Trim();

    public string Email { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? MobilePhone { get; set; }

    public bool IsActive { get; set; } = true;

    public bool EmailPermission { get; set; } = false;

    public bool SmsPermission { get; set; } = false;
    
    /// <summary>
    /// Müşterinin bültene kayıtlı olup olmadığı
    /// </summary>
    public bool IsNewsletterSubscribed { get; set; } = false;

    /// <summary>
    /// Müşterinin kaydedildiği kaynak (E-ticaret, Hediye Çarkı, Manuel vb.)
    /// </summary>
    public CustomerSource Source { get; set; } = CustomerSource.Ecommerce;

    public DateTime? BirthDate { get; set; }

    public int? GenderId { get; set; }

    public string? City { get; set; }

    public string? District { get; set; }

    public string? CustomerCode { get; set; }

    public string? MembershipType { get; set; }

    public DateTime MembershipDate { get; set; }

    public DateTime? LastLoginDate { get; set; }

    public string? LastLoginIp { get; set; }

    public DateTime LastUpdateDate { get; set; }

    public int PointBalance { get; set; } = 0;

    public decimal CreditLimit { get; set; } = 0.00m;

    public bool KvkkApproval { get; set; } = false;

    public bool MembershipAgreementApproval { get; set; } = false;

    public bool CashOnDeliveryBlocked { get; set; } = false;

    public string? Profession { get; set; }

    public string? EducationLevel { get; set; }

    // Senkronizasyon bilgileri
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public DateTime? LastSyncDate { get; set; }

    // Helper property
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();

    [NotMapped]
    public string StatusText => IsActive ? "Aktif" : "Pasif";

    [NotMapped]
    public string StatusBadgeClass => IsActive ? "bg-green-100 text-green-800" : "bg-red-100 text-red-800";

    /// <summary>
    /// Müşteri e-ticaret platformundan gelen kayıtlı müşteri mi?
    /// </summary>
    [NotMapped]
    public bool IsRegisteredCustomer => Source == CustomerSource.Ecommerce;

    /// <summary>
    /// Müşteri kaynağının Türkçe açıklaması
    /// </summary>
    [NotMapped]
    public string SourceText => Source switch
    {
        CustomerSource.Ecommerce => "E-Ticaret",
        CustomerSource.GiftWheel => "Hediye Çarkı",
        CustomerSource.Manual => "Manuel",
        CustomerSource.ExitSurvey => "Çıkış Anketi",
        CustomerSource.StockReminder => "Stok Hatırlatma",
        CustomerSource.Popup => "Pop-up",
        CustomerSource.Newsletter => "E-Bülten",
        _ => "Bilinmiyor"
    };

    /// <summary>
    /// Kaynak badge rengi
    /// </summary>
    [NotMapped]
    public string SourceBadgeClass => Source switch
    {
        CustomerSource.Ecommerce => "bg-blue-100 text-blue-800",
        CustomerSource.GiftWheel => "bg-fuchsia-100 text-fuchsia-800",
        CustomerSource.Manual => "bg-slate-100 text-slate-800",
        CustomerSource.ExitSurvey => "bg-orange-100 text-orange-800",
        CustomerSource.StockReminder => "bg-teal-100 text-teal-800",
        CustomerSource.Popup => "bg-rose-100 text-rose-800",
        CustomerSource.Newsletter => "bg-lime-100 text-lime-800",
        _ => "bg-gray-100 text-gray-800"
    };
}
