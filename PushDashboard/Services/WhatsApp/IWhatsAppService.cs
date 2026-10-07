using PushDashboard.Models;
using System.Text.Json.Serialization;

namespace PushDashboard.Services.WhatsApp;

public interface IWhatsAppService
{
    /// <summary>
    /// Sends a WhatsApp message using Facebook Business API
    /// </summary>
    Task<WhatsAppResult> SendMessageAsync(WhatsAppMessageRequest request);

    /// <summary>
    /// Sends a WhatsApp template message using Facebook Business API
    /// </summary>
    Task<WhatsAppResult> SendTemplateMessageAsync(WhatsAppTemplateMessageRequest request);

    /// <summary>
    /// Tests the WhatsApp Business API connection
    /// </summary>
    Task<WhatsAppResult> TestConnectionAsync(WhatsAppConnectionSettings settings);

    /// <summary>
    /// Gets available message templates from Facebook
    /// </summary>
    Task<WhatsAppResult<List<FacebookMessageTemplate>>> GetMessageTemplatesAsync(WhatsAppConnectionSettings settings);

    /// <summary>
    /// Creates a new message template in Facebook
    /// </summary>
    Task<WhatsAppResult<FacebookMessageTemplate>> CreateMessageTemplateAsync(WhatsAppConnectionSettings settings, CreateTemplateRequest request);

    /// <summary>
    /// Deletes a message template from Facebook
    /// </summary>
    Task<WhatsAppResult> DeleteMessageTemplateAsync(WhatsAppConnectionSettings settings, string templateName);

    /// <summary>
    /// Gets a specific message template from Facebook
    /// </summary>
    Task<WhatsAppResult<FacebookMessageTemplate>> GetMessageTemplateAsync(WhatsAppConnectionSettings settings, string templateId);

    /// <summary>
    /// Validates phone number format for WhatsApp
    /// </summary>
    bool IsValidPhoneNumber(string phoneNumber);

    /// <summary>
    /// Creates a partner app on Gupshup
    /// </summary>
    Task<WhatsAppResult<string>> CreatePartnerAppAsync(string appName);

    /// <summary>
    /// Gets the onboarding embed URL for a partner app
    /// </summary>
    Task<WhatsAppResult<string>> GetOnboardingEmbedUrlAsync(string appId);

    /// <summary>
    /// Gets the access token for a partner app
    /// </summary>
    Task<WhatsAppResult<string>> GetAppTokenAsync(string appId);
    
    Task<string> GetManagedPartnerTokenAsync();

    /// <summary>
    /// Gets template analytics from Gupshup Partner API
    /// </summary>
    Task<WhatsAppResult<TemplateAnalyticsResponse>> GetTemplateAnalyticsAsync(
        WhatsAppConnectionSettings settings, 
        string templateId,
        DateTime? startDate = null, 
        DateTime? endDate = null);

    /// <summary>
    /// Enables template analytics for a partner app
    /// </summary>
    Task<WhatsAppResult> EnableTemplateAnalyticsAsync(string appId, string token);

    /// <summary>
    /// Enables button click analytics for a specific template
    /// </summary>
    Task<WhatsAppResult> EnableButtonClickAnalyticsAsync(string appId, string token, string templateId);

    /// <summary>
    /// Uploads media to Gupshup and returns the media handle ID
    /// Required for creating IMAGE, VIDEO, DOCUMENT type templates
    /// </summary>
    Task<WhatsAppResult<string>> UploadMediaAsync(WhatsAppConnectionSettings settings, Stream fileStream, string fileName, string contentType);

    /// <summary>
    /// Uploads media from a URL to Gupshup and returns the media handle ID
    /// </summary>
    Task<WhatsAppResult<string>> UploadMediaFromUrlAsync(WhatsAppConnectionSettings settings, string mediaUrl, string fileType);
}

public class WhatsAppResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? ErrorCode { get; set; }
    public Dictionary<string, object>? AdditionalInfo { get; set; }

    public static WhatsAppResult CreateSuccess(string? message = null)
    {
        return new WhatsAppResult { Success = true, Message = message };
    }

    public static WhatsAppResult CreateFailure(string message, string? errorCode = null)
    {
        return new WhatsAppResult { Success = false, Message = message, ErrorCode = errorCode };
    }
}

public class WhatsAppResult<T> : WhatsAppResult
{
    public T? Data { get; set; }

    public static WhatsAppResult<T> CreateSuccess(T data, string? message = null)
    {
        return new WhatsAppResult<T> { Success = true, Data = data, Message = message };
    }

    public static new WhatsAppResult<T> CreateFailure(string message, string? errorCode = null)
    {
        return new WhatsAppResult<T> { Success = false, Message = message, ErrorCode = errorCode };
    }
}

public class WhatsAppMessageRequest
{
    public string ToPhoneNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? TemplateType { get; set; } = "TEXT";
    public string? MediaUrl { get; set; }
    public string? ButtonText { get; set; }
    public string? ButtonUrl { get; set; }
    public WhatsAppConnectionSettings ConnectionSettings { get; set; } = new();
}

public class WhatsAppConnectionSettings
{
    public string AppId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

public class FacebookMessageTemplate
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty; // APPROVED, PENDING, REJECTED, DISABLED

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty; // MARKETING, UTILITY, AUTHENTICATION

    [JsonPropertyName("language")]
    public string Language { get; set; } = string.Empty;

    [JsonPropertyName("components")]
    public List<TemplateComponent>? Components { get; set; }

    [JsonPropertyName("qualityScore")]
    public int QualityScore { get; set; }
    
    /// <summary>
    /// Hangi modül için kullanılacak (OrderStatus, BulkMessaging, BasketReminder, Generic)
    /// Bu alan Facebook API'dan gelmez, local olarak saklanır
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string? ModuleType { get; set; }
}

public class TemplateComponent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // HEADER, BODY, FOOTER, BUTTONS

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("format")]
    public string? Format { get; set; } // TEXT, IMAGE, VIDEO, DOCUMENT

    [JsonPropertyName("example")]
    public TemplateComponentExample? Example { get; set; }

    [JsonPropertyName("parameters")]
    public List<TemplateParameter>? Parameters { get; set; }

    [JsonPropertyName("buttons")]
    public List<TemplateButton>? Buttons { get; set; }
}

public class TemplateParameter
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // TEXT

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class TemplateButton
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // QUICK_REPLY, PHONE_NUMBER, URL

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("phone_number")]
    public string? PhoneNumber { get; set; }
}

public class TemplateComponentExample
{
    [JsonPropertyName("header_handle")]
    public List<string>? HeaderHandle { get; set; }

    [JsonPropertyName("body_text")]
    public List<List<string>>? BodyText { get; set; }
}

public class CreateTemplateRequest
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "MARKETING";
    public string Language { get; set; } = "tr";
    public List<TemplateComponent> Components { get; set; } = new();
}

public class UpdateTemplateRequest
{
    public List<TemplateComponent> Components { get; set; } = new();
}

// WhatsApp template message için yeni request sınıfları
public class WhatsAppTemplateMessageRequest
{
    public string ToPhoneNumber { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string TemplateLanguage { get; set; } = "tr";
    public List<WhatsAppTemplateComponent> Components { get; set; } = new();
    public WhatsAppConnectionSettings ConnectionSettings { get; set; } = new();
}

public class WhatsAppTemplateComponent
{
    public string Type { get; set; } = string.Empty;
    public List<WhatsAppTemplateParameter> Parameters { get; set; } = new();
}

public class WhatsAppTemplateParameter
{
    public string Type { get; set; } = "text";
    public string Text { get; set; } = string.Empty;
    /// <summary>
    /// Media URL for image/video/document type parameters (HEADER media components)
    /// </summary>
    public string? MediaUrl { get; set; }
}

// Template Analytics DTOs for Gupshup Partner API
public class TemplateAnalyticsResponse
{
    public List<TemplateAnalytics> Templates { get; set; } = new();
}

public class TemplateAnalytics
{
    public string TemplateId { get; set; } = string.Empty;
    public int Sent { get; set; }
    public int Delivered { get; set; }
    public int Read { get; set; }
    public int Failed { get; set; }
    public double DeliveryRate => Sent > 0 ? Math.Round((double)Delivered / Sent * 100, 1) : 0;
    public double ReadRate => Delivered > 0 ? Math.Round((double)Read / Delivered * 100, 1) : 0;
    public List<ButtonClickData>? ButtonClicks { get; set; }
}

public class ButtonClickData
{
    public string ButtonText { get; set; } = string.Empty;
    public int Clicks { get; set; }
    public string Type { get; set; } = string.Empty;
}
