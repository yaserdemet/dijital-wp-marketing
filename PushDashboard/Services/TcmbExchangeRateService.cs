using System.Globalization;
using System.Xml.Linq;
using Microsoft.Extensions.Caching.Memory;

namespace PushDashboard.Services;

/// <summary>
/// TCMB (Türkiye Cumhuriyet Merkez Bankası) döviz kuru servisi implementasyonu
/// </summary>
public class TcmbExchangeRateService : ITcmbExchangeRateService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<TcmbExchangeRateService> _logger;
    
    private const string TcmbUrl = "https://www.tcmb.gov.tr/kurlar/today.xml";
    private const string CacheKey = "TcmbExchangeRates";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public TcmbExchangeRateService(
        HttpClient httpClient,
        IMemoryCache cache,
        ILogger<TcmbExchangeRateService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Dictionary<string, decimal>> GetExchangeRatesAsync()
    {
        // Cache'den kontrol et
        if (_cache.TryGetValue(CacheKey, out Dictionary<string, decimal>? cachedRates) && cachedRates != null)
        {
            return cachedRates;
        }

        var rates = new Dictionary<string, decimal>
        {
            ["TRY"] = 1m // TL için kur 1
        };

        try
        {
            _logger.LogInformation("TCMB'den döviz kurları çekiliyor...");
            
            var response = await _httpClient.GetStringAsync(TcmbUrl);
            var xml = XDocument.Parse(response);

            var currencies = xml.Descendants("Currency");
            
            foreach (var currency in currencies)
            {
                var code = currency.Attribute("CurrencyCode")?.Value;
                var forexSellingStr = currency.Element("ForexSelling")?.Value;
                
                if (!string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(forexSellingStr))
                {
                    // TCMB ondalık ayırıcı olarak nokta kullanıyor
                    if (decimal.TryParse(forexSellingStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var rate))
                    {
                        rates[code] = rate;
                    }
                }
            }

            _logger.LogInformation("TCMB'den {Count} döviz kuru başarıyla çekildi", rates.Count);
            
            // Cache'e kaydet
            _cache.Set(CacheKey, rates, CacheDuration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCMB döviz kurları çekilirken hata oluştu");
            
            // Hata durumunda cache'deki eski veriyi kullan veya boş dön
            if (_cache.TryGetValue(CacheKey, out Dictionary<string, decimal>? oldRates) && oldRates != null)
            {
                return oldRates;
            }
        }

        return rates;
    }

    public async Task<decimal?> GetExchangeRateAsync(string currencyCode)
    {
        if (string.IsNullOrEmpty(currencyCode))
            return null;

        var rates = await GetExchangeRatesAsync();
        
        // TRY için 1 döndür
        if (currencyCode.Equals("TRY", StringComparison.OrdinalIgnoreCase) ||
            currencyCode.Equals("TL", StringComparison.OrdinalIgnoreCase))
        {
            return 1m;
        }

        return rates.TryGetValue(currencyCode.ToUpperInvariant(), out var rate) ? rate : null;
    }

    public async Task<decimal> ConvertToTryAsync(decimal amount, string currencyCode)
    {
        if (string.IsNullOrEmpty(currencyCode))
            return amount;

        // TRY zaten TL
        if (currencyCode.Equals("TRY", StringComparison.OrdinalIgnoreCase) ||
            currencyCode.Equals("TL", StringComparison.OrdinalIgnoreCase))
        {
            return amount;
        }

        var rate = await GetExchangeRateAsync(currencyCode);
        return rate.HasValue ? amount * rate.Value : amount;
    }

    public async Task<TcmbConversionResult> ConvertAllToTryAsync(List<CurrencyAmount> amounts)
    {
        var result = new TcmbConversionResult
        {
            Conversions = new List<CurrencyConversion>(),
            RateDate = DateTime.Now
        };

        try
        {
            var rates = await GetExchangeRatesAsync();
            decimal totalInTry = 0;

            foreach (var item in amounts)
            {
                var conversion = new CurrencyConversion
                {
                    Currency = item.Currency,
                    OriginalAmount = item.Amount
                };

                // TRY veya TL ise direkt ekle
                if (item.Currency.Equals("TRY", StringComparison.OrdinalIgnoreCase) ||
                    item.Currency.Equals("TL", StringComparison.OrdinalIgnoreCase))
                {
                    conversion.ExchangeRate = 1m;
                    conversion.ConvertedAmount = item.Amount;
                }
                else if (rates.TryGetValue(item.Currency.ToUpperInvariant(), out var rate))
                {
                    conversion.ExchangeRate = rate;
                    conversion.ConvertedAmount = item.Amount * rate;
                }
                else
                {
                    // Kur bulunamadı, orijinal tutarı kullan
                    _logger.LogWarning("{Currency} için kur bulunamadı, orijinal tutar kullanılıyor", item.Currency);
                    conversion.ExchangeRate = 1m;
                    conversion.ConvertedAmount = item.Amount;
                }

                conversion.FormattedConvertedAmount = conversion.ConvertedAmount.ToString("C2", new CultureInfo("tr-TR"));
                totalInTry += conversion.ConvertedAmount;
                result.Conversions.Add(conversion);
            }

            result.TotalInTry = totalInTry;
            result.FormattedTotalInTry = totalInTry.ToString("C2", new CultureInfo("tr-TR"));
            result.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Para birimleri TL'ye çevrilirken hata oluştu");
            result.Success = false;
            result.ErrorMessage = "Döviz kurları alınamadı";
        }

        return result;
    }
}

