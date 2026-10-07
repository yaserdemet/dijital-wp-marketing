using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

/// <summary>
/// Entegrasyon bağlantı testi sonuç modeli
/// </summary>
public class ConnectionTestResult
{
    /// <summary>
    /// Bağlantı testi başarılı mı?
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Test sonucu mesajı
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Hata kodu (başarısız durumda)
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Test yanıt süresi
    /// </summary>
    public TimeSpan? ResponseTime { get; set; }

    /// <summary>
    /// Platform adı (test edilen entegrasyon)
    /// </summary>
    public string? PlatformName { get; set; }

    /// <summary>
    /// Platform versiyonu
    /// </summary>
    public string? PlatformVersion { get; set; }

    /// <summary>
    /// Ek bilgiler (JSON formatında)
    /// </summary>
    public Dictionary<string, object>? AdditionalInfo { get; set; }

    /// <summary>
    /// Test tarihi
    /// </summary>
    public DateTime TestedAt { get; set; } = DateTime.Now;

    #region Helper Properties

    /// <summary>
    /// Başarı durumu için CSS class
    /// </summary>
    [NotMapped]
    public string StatusBadgeClass => Success 
        ? "bg-green-100 text-green-800" 
        : "bg-red-100 text-red-800";

    /// <summary>
    /// Başarı durumu metni
    /// </summary>
    [NotMapped]
    public string StatusText => Success ? "Başarılı" : "Başarısız";

    /// <summary>
    /// Yanıt süresini formatlanmış string olarak döner
    /// </summary>
    [NotMapped]
    public string FormattedResponseTime => ResponseTime?.TotalMilliseconds > 0 
        ? $"{ResponseTime.Value.TotalMilliseconds:F0} ms" 
        : "Bilinmiyor";

    /// <summary>
    /// Test tarihini formatlanmış string olarak döner
    /// </summary>
    [NotMapped]
    public string FormattedTestedAt => TestedAt.ToString("dd.MM.yyyy HH:mm:ss");

    #endregion

    #region Factory Methods

    /// <summary>
    /// Başarılı test sonucu oluşturur
    /// </summary>
    public static ConnectionTestResult CreateSuccess(string message, string? platformName = null, TimeSpan? responseTime = null)
    {
        return new ConnectionTestResult
        {
            Success = true,
            Message = message,
            PlatformName = platformName,
            ResponseTime = responseTime,
            TestedAt = DateTime.Now
        };
    }

    /// <summary>
    /// Başarısız test sonucu oluşturur
    /// </summary>
    public static ConnectionTestResult CreateFailure(string message, string? errorCode = null, string? platformName = null)
    {
        return new ConnectionTestResult
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            PlatformName = platformName,
            TestedAt = DateTime.Now
        };
    }

    /// <summary>
    /// Timeout hatası için test sonucu oluşturur
    /// </summary>
    public static ConnectionTestResult CreateTimeout(string? platformName = null, TimeSpan? timeout = null)
    {
        return new ConnectionTestResult
        {
            Success = false,
            Message = $"Bağlantı zaman aşımına uğradı{(timeout.HasValue ? $" ({timeout.Value.TotalSeconds:F0} saniye)" : "")}",
            ErrorCode = "CONNECTION_TIMEOUT",
            PlatformName = platformName,
            ResponseTime = timeout,
            TestedAt = DateTime.Now
        };
    }

    /// <summary>
    /// Genel hata için test sonucu oluşturur
    /// </summary>
    public static ConnectionTestResult CreateError(Exception ex, string? platformName = null)
    {
        return new ConnectionTestResult
        {
            Success = false,
            Message = $"Bağlantı hatası: {ex.Message}",
            ErrorCode = "CONNECTION_ERROR",
            PlatformName = platformName,
            AdditionalInfo = new Dictionary<string, object>
            {
                ["exceptionType"] = ex.GetType().Name,
                ["stackTrace"] = ex.StackTrace ?? string.Empty
            },
            TestedAt = DateTime.Now
        };
    }

    #endregion
}
