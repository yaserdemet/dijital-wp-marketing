namespace PushDashboard.Models.DTOs;

/// <summary>
/// PayTR yapılandırma ayarları (appsettings.json'dan bind edilir)
/// </summary>
public class PayTRSettings
{
    public string MerchantId { get; set; } = string.Empty;
    public string MerchantKey { get; set; } = string.Empty;
    public string MerchantSalt { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://www.paytr.com";
    public bool TestMode { get; set; } = true;
    public string MerchantOkUrl { get; set; } = string.Empty;
    public string MerchantFailUrl { get; set; } = string.Empty;
}

/// <summary>
/// Yeni kartla ödeme isteği
/// </summary>
public class PayTRPaymentRequest
{
    public string MerchantOid { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string ExpiryMonth { get; set; } = string.Empty;
    public string ExpiryYear { get; set; } = string.Empty;
    public string Cvv { get; set; } = string.Empty;
    public string CardHolderName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserIp { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public bool StoreCard { get; set; }
    public string? Utoken { get; set; }
    public string? MerchantOkUrl { get; set; }
    public string? MerchantFailUrl { get; set; }
}

/// <summary>
/// Kayıtlı kartla ödeme isteği
/// </summary>
public class PayTRSavedCardPaymentRequest
{
    public string MerchantOid { get; set; }
    public string Utoken { get; set; } = string.Empty;
    public string Ctoken { get; set; } = string.Empty;
    public string? Cvv { get; set; }
    public decimal Amount { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserIp { get; set; } = string.Empty;
    public Guid CompanyId { get; set; }
    public string UserId { get; set; } = string.Empty;

    public string Username { get; set; }
}

/// <summary>
/// PayTR ödeme sonucu
/// </summary>
public class PayTRPaymentResult
{
    public bool Success { get; set; }
    public string? OrderId { get; set; }
    public string? TransactionId { get; set; }
    public string? Utoken { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Status { get; set; }
    public string? HtmlContent { get; set; }
}

/// <summary>
/// PayTR kayıtlı kart bilgisi
/// </summary>
public class PayTRCard
{
    public string Ctoken { get; set; } = string.Empty;
    public string LastFour { get; set; } = string.Empty;
    public string CardBrand { get; set; } = string.Empty;
    public string CardType { get; set; } = string.Empty;
    public bool RequireCvv { get; set; }
    public string? BankName { get; set; }
}

/// <summary>
/// PayTR callback (bildirim) verileri
/// </summary>
public class PayTRCallbackData
{
    public string merchant_oid { get; set; } = string.Empty;
    public string status { get; set; } = string.Empty;
    public int total_amount { get; set; }
    public string hash { get; set; } = string.Empty;
    public int failed_reason_code { get; set; }
    public string? failed_reason_msg { get; set; }
    public string? test_mode { get; set; }
    public string? payment_type { get; set; }
    public string? currency { get; set; }
    public int? payment_amount { get; set; }
    // Kart saklama sonucu
    public string? Utoken { get; set; }
}
