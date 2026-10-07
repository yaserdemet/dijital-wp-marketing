using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

/// <summary>
/// Gift Wheel entity - represents a company's gift wheel configuration
/// </summary>
public class GiftWheel
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid CompanyId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = "Hediye Çarkı";

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [Required]
    [StringLength(450)]
    public string CreatedByUserId { get; set; } = string.Empty;

    [StringLength(450)]
    public string? UpdatedByUserId { get; set; }

    // Navigation properties
    [ForeignKey(nameof(CompanyId))]
    public virtual Company Company { get; set; } = null!;

    public virtual ICollection<GiftWheelPrize> Prizes { get; set; } = new List<GiftWheelPrize>();
    public virtual ICollection<GiftWheelSpin> Spins { get; set; } = new List<GiftWheelSpin>();
    public virtual GiftWheelSettings? Settings { get; set; }
}

/// <summary>
/// Gift Wheel Prize entity - represents individual prizes in the wheel
/// </summary>
public class GiftWheelPrize
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int GiftWheelId { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string PrizeType { get; set; } = "voucher"; // voucher, cargo, message

    [Column(TypeName = "decimal(10,2)")]
    public decimal? DiscountAmount { get; set; }

    public int? DiscountType { get; set; } // 1 = amount, 2 = percentage

    public int? ValidityDays { get; set; } = 30;

    [StringLength(20)]
    public string VoucherMode { get; set; } = "duration"; // duration: Süre bazlı (herkese açık), usage: Kullanım bazlı (kişiye özel)

    [StringLength(20)]
    public string VoucherCodeFormat { get; set; } = "full_auto"; // full_auto, auto_prefix, fixed_prefix

    [StringLength(10)]
    public string? VoucherPrefix { get; set; }

    public bool AllowDiscountedProducts { get; set; } = true;

    public bool CombineWithOtherCampaigns { get; set; } = false;

    [Column(TypeName = "decimal(10,2)")]
    public decimal? MinimumOrderAmount { get; set; }

    [Range(1, 100)]
    public int Probability { get; set; } = 10;

    [Required]
    [StringLength(7)]
    public string Color { get; set; } = "#3B82F6";

    [StringLength(7)]
    public string TextColor { get; set; } = "#ffffff";

    public int SortOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Navigation properties
    [ForeignKey(nameof(GiftWheelId))]
    public virtual GiftWheel GiftWheel { get; set; } = null!;

    public virtual ICollection<GiftWheelSpin> Spins { get; set; } = new List<GiftWheelSpin>();
}

/// <summary>
/// Gift Wheel Spin entity - represents individual spin attempts
/// </summary>
public class GiftWheelSpin
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int GiftWheelId { get; set; }

    [Required]
    [StringLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required]
    [StringLength(20)]
    public string CustomerPhone { get; set; } = string.Empty;

    [StringLength(100)]
    public string? CustomerEmail { get; set; }
    public bool SmsPermissionGranted { get; set; } = false;
    public bool EmailPermissionGranted { get; set; } = false;

    [Required]
    public int PrizeId { get; set; }

    [StringLength(50)]
    public string? VoucherCode { get; set; }

    public bool VoucherCreated { get; set; } = false;

    public bool VoucherUsed { get; set; } = false;

    public DateTime? VoucherUsedAt { get; set; }

    [StringLength(50)]
    public string? OrderId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? OrderAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? KDVAmount  { get; set; } 
    public decimal? OrderAmountWithKDV
    {
        get => (OrderAmount ?? 0) + (KDVAmount ?? 0);
    }
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal? OrderDiscountAmount { get; set; }

    public DateTime? OrderDate { get; set; }

    public bool NotificationSent { get; set; } = false;

    public bool IsNotificationSuccessful { get; set; } = false;

    [StringLength(45)]
    public string IpAddress { get; set; } = string.Empty;

    public DateTime SpinDate { get; set; } = DateTime.Now;

    [Column(TypeName = "decimal(10,2)")]
    public decimal? Cost { get; set; }

    [StringLength(500)]
    public string? ErrorMessage { get; set; }

    public bool IsSuccessful { get; set; } = false;

    // Navigation properties
    [ForeignKey(nameof(GiftWheelId))]
    public virtual GiftWheel GiftWheel { get; set; } = null!;

    [ForeignKey(nameof(PrizeId))]
    public virtual GiftWheelPrize Prize { get; set; } = null!;
}

/// <summary>
/// Gift Wheel Settings entity - stores wheel appearance and behavior settings
/// </summary>
public class GiftWheelSettings
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int GiftWheelId { get; set; }

    [Required]
    [StringLength(200)]
    public string WheelTitle { get; set; } = "Çarkı Çevir, Hediyeni Kazan!";

    [StringLength(300)]
    public string WheelSubtitle { get; set; } = "Şansını dene ve harika hediyeler kazan";

    [Required]
    [StringLength(50)]
    public string ButtonText { get; set; } = "Çarkı Çevir";

    [Required]
    [StringLength(200)]
    public string WinMessage { get; set; } = "Tebrikler! {prize} kazandınız!";

    [Required]
    [StringLength(200)]
    public string LoseMessage { get; set; } = "Bu sefer olmadı, tekrar deneyin!";

    [Range(1, 10)]
    public int MaxSpinsPerDay { get; set; } = 1;

    public int WinFrequencyDays { get; set; } = 0;

    public bool RequirePhone { get; set; } = true;

    public bool RequireEmail { get; set; } = false;

    [StringLength(500)]
    public string NotificationTemplate { get; set; } = "🎉 Tebrikler {name}! {prize} kazandınız. Hemen alışverişe başlayın: {siteUrl}";

    [StringLength(100)]
    public string? WhatsappTemplateId { get; set; }

    [StringLength(200)]
    public string? WhatsappTemplateName { get; set; }

    [Required]
    [StringLength(7)]
    public string PrimaryColor { get; set; } = "#6366f1";

    [Required]
    [StringLength(7)]
    public string SecondaryColor { get; set; } = "#1f2937";

    [Range(0, 500)]
    public int WheelSize { get; set; } = 300;

    [Required]
    [StringLength(10)]
    public string Layout { get; set; } = "row"; // column, row

    [StringLength(500)]
    public string? LogoUrl { get; set; }

    public bool LogoAnimation { get; set; } = true;

    [Range(5, 100)]
    public int LogoSizePercentage { get; set; } = 15;

    [StringLength(500)]
    public string? BackgroundImageUrl { get; set; }

    [StringLength(7)]
    public string ModalTextColor { get; set; } = "#1f2937";

    [StringLength(500)]
    public string? PrivacyPolicyUrl { get; set; }

    [StringLength(500)]
    public string? TermsUrl { get; set; }

    public bool ShowConfetti { get; set; } = true;

    /// <summary>
    /// Trigger button position: "left" or "right"
    /// </summary>
    [RegularExpression("^(left|right)$")]
    public string ButtonPosition { get; set; } = "right";

    /// <summary>
    /// Distance from bottom in pixels for desktop
    /// </summary>
    [Range(0, 500)]
    public int ButtonBottomPx { get; set; } = 20;

    /// <summary>
    /// Distance from bottom in pixels for mobile
    /// </summary>
    [Range(0, 500)]
    public int ButtonMobileBottomPx { get; set; } = 20;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    [Required]
    [StringLength(450)]
    public string UpdatedByUserId { get; set; } = string.Empty;

    // Navigation properties
    [ForeignKey(nameof(GiftWheelId))]
    public virtual GiftWheel GiftWheel { get; set; } = null!;
}
