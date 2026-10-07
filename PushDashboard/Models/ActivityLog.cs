namespace PushDashboard.Models;

/// <summary>
/// Sistemdeki tüm kullanıcı aksiyonlarını loglamak için kullanılan model
/// </summary>
public class ActivityLog
{
    public long Id { get; set; }

    /// <summary>
    /// İşlemi yapan kullanıcının ID'si
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Şirket ID'si
    /// </summary>
    public Guid CompanyId { get; set; }

    /// <summary>
    /// İşlem türü (Create, Update, Delete, Login, Logout, etc.)
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// İşlemin yapıldığı modül/bölüm (Customer, Integration, GiftWheel, etc.)
    /// </summary>
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// İşlem açıklaması
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// İşlemle ilgili ek detaylar (JSON formatında)
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// İşlemin yapıldığı entity ID (opsiyonel)
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// İşlemin yapıldığı entity türü
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// Kullanıcının IP adresi
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Kullanıcının tarayıcı bilgisi
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// İşlem tarihi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    public virtual ApplicationUser? User { get; set; }
    public virtual Company? Company { get; set; }
}

/// <summary>
/// Aksiyon türlerini tanımlayan enum
/// </summary>
public static class ActivityActionTypes
{
    public const string Create = "Create";
    public const string Update = "Update";
    public const string Delete = "Delete";
    public const string View = "View";
    public const string Login = "Login";
    public const string Logout = "Logout";
    public const string Export = "Export";
    public const string Import = "Import";
    public const string Sync = "Sync";
    public const string Send = "Send";
    public const string Activate = "Activate";
    public const string Deactivate = "Deactivate";
    public const string Settings = "Settings";
    public const string Purchase = "Purchase";
    public const string Configure = "Configure";
    public const string StatusChange = "StatusChange";
    public const string Transfer = "Transfer";
}

/// <summary>
/// Modül isimlerini tanımlayan sabitler
/// </summary>
public static class ActivityModules
{
    public const string Account = "Hesap";
    public const string Customer = "Müşteriler";
    public const string Basket = "Sepetler";
    public const string Integration = "Entegrasyonlar";
    public const string GiftWheel = "Hediye Çarkı";
    public const string EmailTemplate = "Mail Şablonları";
    public const string SmsTemplate = "SMS Şablonları";
    public const string WhatsAppTemplate = "WhatsApp Şablonları";
    public const string OrderStatus = "Sipariş Durumu";
    public const string SocialProof = "Social Proof";
    public const string CookieManagement = "Çerez Yönetimi";
    public const string BasketReminder = "Sepet Hatırlatma";
    public const string ExitSurvey = "Çıkış Anketi";
    public const string VideoHosting = "Video Hosting";
    public const string ProductSlider = "Ürün Slider";
    public const string Comment = "Yorum Taşıma";
    public const string BulkMessage = "Toplu Mesaj";
    public const string BulkMessaging = "Toplu Mesaj";
    public const string Notification = "Bildirimler";
    public const string StockReminder = "Stok Hatırlatma";
    public const string FirstOrder = "İlk Alışveriş";
    public const string Settings = "Ayarlar";
    public const string Billing = "Fatura Bilgileri";
    public const string Store = "Mağaza";
    public const string Dashboard = "Dashboard";
}

