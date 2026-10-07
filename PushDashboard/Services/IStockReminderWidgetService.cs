using PushDashboard.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PushDashboard.Services;

public interface IStockReminderWidgetService
{
    /// <summary>
    /// Şirketin stok hatırlatma widget ayarlarını getirir
    /// </summary>
    Task<StockReminderWidgetSettings?> GetWidgetSettingsAsync(Guid companyId);

    /// <summary>
    /// Widget ayarlarını kaydeder
    /// </summary>
    Task<bool> SaveWidgetSettingsAsync(StockReminderWidgetSettings settings);

    /// <summary>
    /// Varsayılan widget ayarlarını oluşturur
    /// </summary>
    Task<StockReminderWidgetSettings> CreateDefaultSettingsAsync(Guid companyId, string userId);

    /// <summary>
    /// Script için konfigürasyon verisini getirir (R2'ye export edilecek format)
    /// </summary>
    Task<StockReminderWidgetScriptConfig> GetScriptConfigAsync(Guid companyId);

    /// <summary>
    /// Widget konfigürasyonunu R2'ye export eder
    /// </summary>
    Task<string> ExportWidgetToR2Async(Guid companyId);

    /// <summary>
    /// Widget üzerinden gelen form gönderimini kaydeder
    /// </summary>
    Task<bool> RecordSubmissionAsync(StockReminderWidgetSubmissionModel model, string? ipAddress, string? userAgent);

    /// <summary>
    /// Widget olayını izler: impression ve dismiss sayaçlarını günceller
    /// </summary>
    Task TrackEventAsync(Guid companyId, string eventType);
}

/// <summary>
/// Script için kullanılacak konfigürasyon modeli
/// </summary>
public class StockReminderWidgetScriptConfig
{
    public bool IsActive { get; set; }
    public Guid CompanyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SubmitButtonText { get; set; } = string.Empty;
    public string PrivacyText { get; set; } = string.Empty;
    public string? KvkkLink { get; set; }
    public string? EtkLink { get; set; }
    public string PhonePlaceholder { get; set; } = string.Empty;
    public string EmailPlaceholder { get; set; } = string.Empty;
    public string VariantOptions { get; set; } = string.Empty;
    public string VariantLabel { get; set; } = string.Empty;
    public bool ShowVariantSelector { get; set; }
    public bool ShowPhoneField { get; set; }
    public bool ShowEmailField { get; set; }
    public bool ShowNameFields { get; set; }
    public bool RequirePhone { get; set; }
    public bool ShowConsentCheckbox { get; set; }
    public string BackgroundColor { get; set; } = string.Empty;
    public string TextColor { get; set; } = string.Empty;
    public string TitleColor { get; set; } = string.Empty;
    public string ButtonColor { get; set; } = string.Empty;
    public string ButtonTextColor { get; set; } = string.Empty;
    public string VariantBorderColor { get; set; } = string.Empty;
    public string VariantSelectedColor { get; set; } = string.Empty;
    public string BorderRadius { get; set; } = string.Empty;
    public bool ShowOnMobile { get; set; }
    public bool ShowOnDesktop { get; set; }
    public bool EnableAnimation { get; set; }
    public string SubmitUrl { get; set; } = string.Empty;
    public string TrackUrl { get; set; } = string.Empty;
}

/// <summary>
/// Form gönderim modeli (public API için)
/// </summary>
public class StockReminderWidgetSubmissionModel
{
    public Guid CompanyId { get; set; }

    [JsonConverter(typeof(StringOrNumberJsonConverter))]
    public string? ProductId { get; set; }
    [JsonConverter(typeof(StringOrNumberJsonConverter))]
    public string? VariantId { get; set; }
    public string? SelectedVariant { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PageUrl { get; set; }
    public string? SessionId { get; set; }
    public bool ConsentGiven { get; set; }
}

/// <summary>
/// JSON'dan gelen string veya number değerini string olarak deserialize eder.
/// Örn: "productId": 9  veya  "productId": "9"  her ikisini de string olarak alır.
/// </summary>
public class StringOrNumberJsonConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString();
            case JsonTokenType.Number:
                if (reader.TryGetInt64(out var longVal))
                    return longVal.ToString();
                if (reader.TryGetDouble(out var doubleVal))
                    return doubleVal.ToString();
                return null;
            case JsonTokenType.Null:
                return null;
            case JsonTokenType.True:
                return "true";
            case JsonTokenType.False:
                return "false";
            default:
                using (var doc = JsonDocument.ParseValue(ref reader))
                    return doc.RootElement.ToString();
        }
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value == null)
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value);
    }
}
