using System.ComponentModel.DataAnnotations;

namespace PushDashboard.DTOs;

#region Request DTOs

/// <summary>
/// Request DTO for spinning the gift wheel
/// </summary>
public class GiftWheelSpinRequest
{
    [Required(ErrorMessage = "Müşteri adı gereklidir")]
    [StringLength(100, ErrorMessage = "Müşteri adı en fazla 100 karakter olabilir")]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Telefon numarası gereklidir")]
    [Phone(ErrorMessage = "Geçerli bir telefon numarası giriniz")]
    [StringLength(20, ErrorMessage = "Telefon numarası en fazla 20 karakter olabilir")]
    public string CustomerPhone { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta adresi giriniz")]
    [StringLength(100, ErrorMessage = "E-posta adresi en fazla 100 karakter olabilir")]
    public string? CustomerEmail { get; set; }
    public bool CommunicationConsent { get; set; } = false;
}

/// <summary>
/// Request DTO for adding/updating prizes
/// </summary>
public class GiftWheelPrizeRequest
{
    [Required(ErrorMessage = "Ödül adı gereklidir")]
    [StringLength(100, ErrorMessage = "Ödül adı en fazla 100 karakter olabilir")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ödül türü gereklidir")]
    [RegularExpression("^(voucher|cargo|message)$", ErrorMessage = "Geçersiz ödül türü")]
    public string PrizeType { get; set; } = "voucher";

    [Range(0.01, 999999.99, ErrorMessage = "İndirim miktarı 0.01 ile 999999.99 arasında olmalıdır")]
    public decimal? DiscountAmount { get; set; }

    [Range(1, 2, ErrorMessage = "İndirim türü tutar veya yüzde olmalıdır")]
    public int? DiscountType { get; set; } = 1;

    [Range(0, 365, ErrorMessage = "Geçerlilik süresi 1 ile 365 gün arasında olmalıdır")]
    public int? ValidityDays { get; set; } = 30;

    [RegularExpression("^(duration|usage)$", ErrorMessage = "Geçersiz kupon modu")]
    public string VoucherMode { get; set; } = "duration";

    [RegularExpression("^(full_auto|auto_prefix|fixed_prefix)$", ErrorMessage = "Geçersiz kupon kodu formatı")]
    public string VoucherCodeFormat { get; set; } = "full_auto";

    [StringLength(10, ErrorMessage = "Ön ek en fazla 10 karakter olabilir")]
    public string? VoucherPrefix { get; set; }

    public bool AllowDiscountedProducts { get; set; } = true;

    public bool CombineWithOtherCampaigns { get; set; } = false;

    [Range(0, 999999.99, ErrorMessage = "Minimum sepet tutarı 0 ile 999999.99 arasında olmalıdır")]
    public decimal? MinimumOrderAmount { get; set; }

    [Required(ErrorMessage = "Olasılık gereklidir")]
    [Range(1, 100, ErrorMessage = "Olasılık 1 ile 100 arasında olmalıdır")]
    public int Probability { get; set; } = 10;

    [Required(ErrorMessage = "Renk gereklidir")]
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Geçerli bir hex renk kodu giriniz")]
    public string Color { get; set; } = "#3B82F6";

    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Geçerli bir hex renk kodu giriniz")]
    public string TextColor { get; set; } = "#ffffff";

    [Range(0, 100, ErrorMessage = "Sıralama 0 ile 100 arasında olmalıdır")]
    public int SortOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Request DTO for updating gift wheel settings
/// </summary>
public class UpdateGiftWheelSettingsRequest
{
    [Required(ErrorMessage = "Çark başlığı gereklidir")]
    [StringLength(200, ErrorMessage = "Çark başlığı en fazla 200 karakter olabilir")]
    public string WheelTitle { get; set; } = "Çarkı Çevir, Hediyeni Kazan!";

    [StringLength(300, ErrorMessage = "Alt başlık en fazla 300 karakter olabilir")]
    public string WheelSubtitle { get; set; } = "Şansını dene ve harika hediyeler kazan";

    [Required(ErrorMessage = "Buton metni gereklidir")]
    [StringLength(50, ErrorMessage = "Buton metni en fazla 50 karakter olabilir")]
    public string ButtonText { get; set; } = "Çarkı Çevir";

    [Required(ErrorMessage = "Kazanma mesajı gereklidir")]
    [StringLength(200, ErrorMessage = "Kazanma mesajı en fazla 200 karakter olabilir")]
    public string WinMessage { get; set; } = "Tebrikler! {prize} kazandınız!";

    [Required(ErrorMessage = "Kaybetme mesajı gereklidir")]
    [StringLength(200, ErrorMessage = "Kaybetme mesajı en fazla 200 karakter olabilir")]
    public string LoseMessage { get; set; } = "Bu sefer olmadı, tekrar deneyin!";

    [Range(1, 10, ErrorMessage = "Günlük maksimum çevirme sayısı 1 ile 10 arasında olmalıdır")]
    public int MaxSpinsPerDay { get; set; } = 1;

    [Range(0, 365, ErrorMessage = "Kazanma sıklığı 0 ile 365 gün arasında olmalıdır")]
    public int WinFrequencyDays { get; set; } = 0;

    public bool RequirePhone { get; set; } = true;

    public bool RequireEmail { get; set; } = false;

    [StringLength(500, ErrorMessage = "Bildirim şablonu en fazla 500 karakter olabilir")]
    public string NotificationTemplate { get; set; } = "🎉 Tebrikler {name}! {prize} kazandınız. Hemen alışverişe başlayın: {siteUrl}";

    [StringLength(100, ErrorMessage = "WhatsApp şablon ID en fazla 100 karakter olabilir")]
    public string? WhatsappTemplateId { get; set; }

    [StringLength(200, ErrorMessage = "WhatsApp şablon adı en fazla 200 karakter olabilir")]
    public string? WhatsappTemplateName { get; set; }

    [Required(ErrorMessage = "Ana renk gereklidir")]
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Geçerli bir hex renk kodu giriniz")]
    public string PrimaryColor { get; set; } = "#6366f1";

    [Required(ErrorMessage = "İkincil renk gereklidir")]
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Geçerli bir hex renk kodu giriniz")]
    public string SecondaryColor { get; set; } = "#f3f4f6";

    [Range(0, 500, ErrorMessage = "Çark boyutu 0 ile 500 piksel arasında olmalıdır (0 = varsayılan)")]
    public int WheelSize { get; set; } = 300;

    [RegularExpression("^(column|row)$", ErrorMessage = "Geçersiz düzen")]
    public string Layout { get; set; } = "row";

    [StringLength(500, ErrorMessage = "URL en fazla 500 karakter olabilir")]
    public string? LogoUrl { get; set; }

    public bool LogoAnimation { get; set; } = true;

    [Range(5, 100, ErrorMessage = "Logo boyutu %5 ile %100 arasında olmalıdır")]
    public int LogoSizePercentage { get; set; } = 15;

    [StringLength(500, ErrorMessage = "URL en fazla 500 karakter olabilir")]
    public string? BackgroundImageUrl { get; set; }

    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Geçerli bir hex renk kodu giriniz")]
    public string ModalTextColor { get; set; } = "#1f2937";

    [Url(ErrorMessage = "Geçerli bir URL giriniz")]
    [StringLength(500, ErrorMessage = "URL en fazla 500 karakter olabilir")]
    public string? PrivacyPolicyUrl { get; set; }

    [Url(ErrorMessage = "Geçerli bir URL giriniz")]
    [StringLength(500, ErrorMessage = "URL en fazla 500 karakter olabilir")]
    public string? TermsUrl { get; set; }

    public bool ShowConfetti { get; set; } = true;

    [RegularExpression("^(left|right)$", ErrorMessage = "Buton pozisyonu 'left' veya 'right' olmalıdır")]
    public string ButtonPosition { get; set; } = "right";

    [Range(0, 500, ErrorMessage = "Alt mesafe 0 ile 500 piksel arasında olmalıdır")]
    public int ButtonBottomPx { get; set; } = 20;

    [Range(0, 500, ErrorMessage = "Mobil alt mesafe 0 ile 500 piksel arasında olmalıdır")]
    public int ButtonMobileBottomPx { get; set; } = 20;
}

#endregion

#region Response DTOs

/// <summary>
/// Response DTO for spin results
/// </summary>
public class GiftWheelSpinResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public GiftWheelPrizeDto? Prize { get; set; }
    public string? VoucherCode { get; set; }
    public bool VoucherCreated { get; set; }
    public bool NotificationSent { get; set; }
    public decimal Cost { get; set; }
}

/// <summary>
/// Response DTO for eligibility check
/// </summary>
public class GiftWheelEligibilityDto
{
    public bool CanSpin { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
}

/// <summary>
/// DTO for gift wheel prize information
/// </summary>
public class GiftWheelPrizeDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PrizeType { get; set; } = string.Empty;
    public decimal? DiscountAmount { get; set; }
    public int? DiscountType { get; set; }
    public int? ValidityDays { get; set; }
    public string VoucherMode { get; set; } = "duration";
    public string VoucherCodeFormat { get; set; } = "full_auto";
    public string? VoucherPrefix { get; set; }
    public bool AllowDiscountedProducts { get; set; } = true;
    public bool CombineWithOtherCampaigns { get; set; } = false;
    public decimal? MinimumOrderAmount { get; set; }
    public int Probability { get; set; }
    public string Color { get; set; } = string.Empty;
    public string TextColor { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for gift wheel settings
/// </summary>
public class GiftWheelSettingsDto
{
    public int Id { get; set; }
    public int WheelId { get; set; }
    public string WheelTitle { get; set; } = string.Empty;
    public string WheelSubtitle { get; set; } = string.Empty;
    public string ButtonText { get; set; } = string.Empty;
    public string WinMessage { get; set; } = string.Empty;
    public string LoseMessage { get; set; } = string.Empty;
    public int MaxSpinsPerDay { get; set; }
    public int WinFrequencyDays { get; set; }
    public bool RequirePhone { get; set; }
    public bool RequireEmail { get; set; }
    public string NotificationTemplate { get; set; } = string.Empty;
    public string? WhatsappTemplateId { get; set; }
    public string? WhatsappTemplateName { get; set; }
    public string PrimaryColor { get; set; } = string.Empty;
    public string SecondaryColor { get; set; } = string.Empty;
    public int WheelSize { get; set; }
    public string Layout { get; set; } = "row";
    public string? LogoUrl { get; set; }
    public bool LogoAnimation { get; set; }
    public string? PrivacyPolicyUrl { get; set; }
    public string? TermsUrl { get; set; }
    public bool ShowConfetti { get; set; }
    public int? LogoSizePercentage { get; set; }
    public string? BackgroundImageUrl { get; set; }
    public string ModalTextColor { get; set; } = "#1f2937";
    public string ButtonPosition { get; set; } = "right";
    public int ButtonBottomPx { get; set; } = 20;
    public int ButtonMobileBottomPx { get; set; } = 20;
}

/// <summary>
/// DTO for wheel configuration (used in embed script)
/// </summary>
public class GiftWheelConfigDto
{
    public int WheelId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Subtitle { get; set; } = string.Empty;
    public string ButtonText { get; set; } = string.Empty;
    public string WinMessage { get; set; } = string.Empty;
    public string LoseMessage { get; set; } = string.Empty;
    public bool RequirePhone { get; set; }
    public bool RequireEmail { get; set; }
    public string PrimaryColor { get; set; } = string.Empty;
    public string SecondaryColor { get; set; } = string.Empty;
    public int WheelSize { get; set; }
    public string Layout { get; set; } = "row";
    public string? LogoUrl { get; set; }
    public bool LogoAnimation { get; set; }
    public string? PrivacyPolicyUrl { get; set; }
    public string? TermsUrl { get; set; }
    public bool ShowConfetti { get; set; }
    public int? LogoSizePercentage { get; set; }
    public string? BackgroundImageUrl { get; set; }
    public string ModalTextColor { get; set; } = "#1f2937";
    public string ButtonPosition { get; set; } = "right";
    public int ButtonBottomPx { get; set; } = 20;
    public int ButtonMobileBottomPx { get; set; } = 20;
    public List<GiftWheelPrizeDto> Prizes { get; set; } = new();
}

/// <summary>
/// DTO for gift wheel statistics
/// </summary>
public class GiftWheelStatsDto
{
    public int TotalSpins { get; set; }
    public int TotalVouchersCreated { get; set; }
    public int TotalNotificationsSent { get; set; }
    public decimal TotalCost { get; set; }
    public int SpinsToday { get; set; }
    public int SpinsThisWeek { get; set; }
    public int SpinsThisMonth { get; set; }
    public List<PrizeStatsDto> PrizeStats { get; set; } = new();
    public List<DailySpinStatsDto> DailyStats { get; set; } = new();
}

/// <summary>
/// DTO for individual prize statistics
/// </summary>
public class PrizeStatsDto
{
    public int PrizeId { get; set; }
    public string PrizeName { get; set; } = string.Empty;
    public int TimesWon { get; set; }
    public decimal WinRate { get; set; }
}

/// <summary>
/// DTO for daily spin statistics
/// </summary>
public class DailySpinStatsDto
{
    public DateTime Date { get; set; }
    public int SpinCount { get; set; }
    public int VoucherCount { get; set; }
}

/// <summary>
/// DTO for recent spin information
/// </summary>
public class RecentSpinDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string PrizeName { get; set; } = string.Empty;
    public string PrizeType { get; set; } = string.Empty;
    public decimal? DiscountAmount { get; set; }
    public int? DiscountType { get; set; }
    public string? VoucherCode { get; set; }
    public bool VoucherCreated { get; set; }
    public bool VoucherUsed { get; set; }
    public bool NotificationSent { get; set; }
    public bool IsNotificationSuccessful { get; set; }
    public bool CommunicationConsent { get; set; }
    public DateTime SpinDate { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsSuccessful { get; set; }
}

#endregion

#region Module DTOs

/// <summary>
/// DTO for gift wheel module settings
/// </summary>
public class GiftWheelModuleSettingsDto
{
    public bool IsEnabled { get; set; } = true;
    public decimal NotificationCost { get; set; } = 1.00m;
    public int MaxSpinsPerHour { get; set; } = 100;
    public bool EnableRateLimiting { get; set; } = true;
    public string? CompanyWebsiteUrl { get; set; }
}

/// <summary>
/// DTO for gift wheel module statistics
/// </summary>
public class GiftWheelModuleStatsDto
{
    public int ActiveWheels { get; set; }
    public int TotalSpinsAllTime { get; set; }
    public int TotalVouchersCreated { get; set; }
    public decimal TotalCostAllTime { get; set; }
    public int SpinsLast30Days { get; set; }
    public decimal CostLast30Days { get; set; }
    public decimal AverageSpinsPerDay { get; set; }
    public decimal ConversionRate { get; set; }
}

/// <summary>
/// DTO for module usage information
/// </summary>
public class ModuleUsageDto
{
    public int Id { get; set; }
    public string UsageType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Cost { get; set; }
    public bool IsSuccessful { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ReferenceId { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// DTO for module status information
/// </summary>
public class ModuleStatusDto
{
    public bool IsOwned { get; set; }
    public bool IsActive { get; set; }
    public bool IsConfigured { get; set; }
    public DateTime? PurchasedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public decimal? PaidAmount { get; set; }
    public string? TransactionId { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>
/// DTO for integration status information
/// </summary>
public class IntegrationStatusDto
{
    public bool HasEcommerceIntegration { get; set; }
    public string? EcommercePlatform { get; set; }
    public bool IsEcommerceConfigured { get; set; }
    public bool HasWhatsAppIntegration { get; set; }
    public bool IsWhatsAppConfigured { get; set; }
    public List<string> MissingIntegrations { get; set; } = new();
    public List<string> ConfigurationIssues { get; set; } = new();
}

#endregion

#region Dashboard & Analytics DTOs

/// <summary>
/// DTO for dashboard KPI cards
/// </summary>
public class GiftWheelDashboardKpiDto
{
    public int TotalSpins { get; set; }
    public int TotalVouchersCreated { get; set; }
    public int TotalVouchersUsed { get; set; }
    public decimal VoucherUsageRate { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageVoucherAmount { get; set; }
    public decimal AverageOrderAmount { get; set; }
    public int SpinsToday { get; set; }
    public int SpinsThisWeek { get; set; }
    public int SpinsThisMonth { get; set; }
    public decimal RevenueToday { get; set; }
    public decimal RevenueThisWeek { get; set; }
    public decimal RevenueThisMonth { get; set; }
    public int TotalNotificationsSent { get; set; }
    public int TotalNotificationsSuccessful { get; set; }
    public int NotificationsSuccessfulThisWeek { get; set; }
    public decimal OrderAmountWithKDV { get; set; }
    public decimal TotalDiscountAmount { get; set; }
    public decimal TotalCost { get; set; }
}

/// <summary>
/// DTO for funnel statistics
/// </summary>
public class GiftWheelFunnelDto
{
    public int SpinCount { get; set; }
    public int VoucherCreatedCount { get; set; }
    public int VoucherUsedCount { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal SpinToVoucherRate { get; set; }
    public decimal VoucherToUsageRate { get; set; }
    public decimal UsageToRevenueRate { get; set; }
}

/// <summary>
/// DTO for trend statistics
/// </summary>
public class GiftWheelTrendDto
{
    public List<TrendDataPointDto> DailySpins { get; set; } = new();
    public List<TrendDataPointDto> DailyVouchers { get; set; } = new();
    public List<TrendDataPointDto> DailyUsage { get; set; } = new();
    public List<TrendDataPointDto> DailyRevenue { get; set; } = new();
}

/// <summary>
/// DTO for trend data point
/// </summary>
public class TrendDataPointDto
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
    public string Label { get; set; } = string.Empty;
}

/// <summary>
/// DTO for detailed spin log with order information
/// </summary>
public class DetailedSpinLogDto
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? CustomerEmail { get; set; }
    public string PrizeName { get; set; } = string.Empty;
    public string PrizeType { get; set; } = string.Empty;
    public decimal? DiscountAmount { get; set; }
    public int? DiscountType { get; set; }
    public string? VoucherCode { get; set; }
    public bool VoucherCreated { get; set; }
    public bool VoucherUsed { get; set; }
    public DateTime? VoucherUsedAt { get; set; }
    public string? OrderId { get; set; }
    public decimal? OrderAmount { get; set; }
    public DateTime? OrderDate { get; set; }
    public bool NotificationSent { get; set; }
    public bool IsNotificationSuccessful { get; set; }
    public bool CommunicationConsent { get; set; }
    public DateTime SpinDate { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsSuccessful { get; set; }
    public int? ValidityDays { get; set; }
    public DateTime? VoucherStartDate { get; set; }
    public DateTime? VoucherEndDate { get; set; }
}

/// <summary>
/// DTO for spin log filtering
/// </summary>
public class SpinLogFilterDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? VoucherUsed { get; set; }
    public bool? VoucherCreated { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SortBy { get; set; } = "SpinDate";
    public bool SortDescending { get; set; } = true;
}

/// <summary>
/// DTO for paginated spin log result
/// </summary>
public class PaginatedSpinLogResultDto
{
    public List<DetailedSpinLogDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

/// <summary>
/// DTO for Ticimax voucher used webhook
/// </summary>
public class TicimaxVoucherWebhookDto
{
    public string VoucherCode { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public decimal OrderAmount { get; set; }
    public DateTime OrderDate { get; set; }
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
}

#endregion
