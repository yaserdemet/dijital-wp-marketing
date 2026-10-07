namespace PushDashboard.Services;

/// <summary>
/// TCMB (Türkiye Cumhuriyet Merkez Bankası) döviz kuru servisi
/// </summary>
public interface ITcmbExchangeRateService
{
    /// <summary>
    /// Tüm güncel döviz kurlarını getirir
    /// </summary>
    Task<Dictionary<string, decimal>> GetExchangeRatesAsync();
    
    /// <summary>
    /// Belirli bir para biriminin TL karşılığını getirir
    /// </summary>
    Task<decimal?> GetExchangeRateAsync(string currencyCode);
    
    /// <summary>
    /// Tutarı TL'ye çevirir
    /// </summary>
    Task<decimal> ConvertToTryAsync(decimal amount, string currencyCode);
    
    /// <summary>
    /// Birden fazla para birimindeki tutarları TL'ye çevirip toplar
    /// </summary>
    Task<TcmbConversionResult> ConvertAllToTryAsync(List<CurrencyAmount> amounts);
}

public class CurrencyAmount
{
    public string Currency { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class TcmbConversionResult
{
    public decimal TotalInTry { get; set; }
    public string FormattedTotalInTry { get; set; } = string.Empty;
    public DateTime? RateDate { get; set; }
    public List<CurrencyConversion> Conversions { get; set; } = new();
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class CurrencyConversion
{
    public string Currency { get; set; } = string.Empty;
    public decimal OriginalAmount { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal ConvertedAmount { get; set; }
    public string FormattedConvertedAmount { get; set; } = string.Empty;
}

