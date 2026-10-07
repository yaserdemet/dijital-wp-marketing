using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace PushDashboard.Services.SMS;

/// <summary>
/// NetGSM SMS provider implementation
/// Updated to use REST v2 API
/// </summary>
public class NetGsmProvider : ISmsProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NetGsmProvider> _logger;

    private const string ApiBaseUrl = "https://api.netgsm.com.tr/sms/rest/v2";
    private const string SendSmsEndpoint = "/send";

    public string ProviderName => "NetGSM";

    public NetGsmProvider(HttpClient httpClient, ILogger<NetGsmProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<SmsSendResult> SendSmsAsync(string phoneNumber, string message, SmsSettings settings)
    {
        try
        {
            if (!ValidateSettings(settings, out var validationError))
            {
                return SmsSendResult.CreateFailure(validationError!, "CREDENTIALS_MISSING");
            }

            var formattedPhone = FormatPhoneNumber(phoneNumber);
            if (string.IsNullOrEmpty(formattedPhone))
            {
                return SmsSendResult.CreateFailure("Geçersiz telefon numarası.", "INVALID_PHONE");
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return SmsSendResult.CreateFailure("SMS mesajı boş olamaz.", "EMPTY_MESSAGE");
            }

            var requestModel = new NetGsmSendRequest
            {
                MsgHeader = settings.SenderName,
                Encoding = "TR",
                Messages = new[]
                {
                    new NetGsmMessageItem
                    {
                        Msg = message,
                        No = formattedPhone
                    }
                }
            };

            var response = await SendRequestAsync(requestModel, settings);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogDebug("NetGSM Response: {Response}", responseContent);

            return ParseSendResponse(responseContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending SMS via NetGSM to {Phone}", phoneNumber);
            return SmsSendResult.CreateFailure($"SMS gönderim hatası: {ex.Message}", "EXCEPTION");
        }
    }

    public async Task<SmsTestResult> TestConnectionAsync(SmsSettings settings)
    {
        try
        {
            if (!ValidateSettings(settings, out var validationError))
            {
                return SmsTestResult.CreateFailure(validationError!, "CREDENTIALS_MISSING");
            }

            var startTime = DateTime.Now;

            var requestModel = new NetGsmSendRequest
            {
                MsgHeader = settings.SenderName ?? "",
                Encoding = "TR",
                Messages = new[]
                {
                    new NetGsmMessageItem
                    {
                        Msg = $"{settings.SenderName} isimli gönderici test connection yaptı.",
                        No = "5535522213"
                    }
                }
            };

            // We are just testing reachability and Auth.
            var response = await SendRequestAsync(requestModel, settings);
            var responseText = await response.Content.ReadAsStringAsync();

            var endTime = DateTime.Now;
            
            if (IsAuthFailure(responseText))
            {
                return SmsTestResult.CreateFailure(
                   "NetGSM bağlantısı başarısız. Kullanıcı adı veya şifre hatalı olabilir.",
                   "AUTH_FAILED"
               );
            }

            return SmsTestResult.CreateSuccess(
                "NetGSM bağlantısı başarılı! (Kimlik doğrulama kabul edildi)",
                endTime - startTime,
                0 // Balance check removed as it requires different API endpoint not provided in v2 context
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing NetGSM connection");
            return SmsTestResult.CreateFailure($"Bağlantı testi hatası: {ex.Message}", "EXCEPTION");
        }
    }

    private async Task<HttpResponseMessage> SendRequestAsync(NetGsmSendRequest requestModel, SmsSettings settings)
    {
        var requestUrl = $"{ApiBaseUrl}{SendSmsEndpoint}";

        var jsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var jsonContent = JsonSerializer.Serialize(requestModel, jsonOptions);

        var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
        request.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Set Basic Auth
        var authString = $"{settings.Username}:{settings.Password}";
        var base64Auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(authString));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64Auth);

        return await _httpClient.SendAsync(request);
    }

    private bool ValidateSettings(SmsSettings settings, out string? error)
    {
        if (string.IsNullOrWhiteSpace(settings.Username) || string.IsNullOrWhiteSpace(settings.Password))
        {
            error = "NetGSM kullanıcı adı veya şifre eksik.";
            return false;
        }
        error = null;
        return true;
    }

    private SmsSendResult ParseSendResponse(string responseText)
    {
        // NetGSM v2 response handling
        // Example response for success: {"code":"00", "jobid":"..."} or plain text depending on exact behavior (doc varies).
        // The user provided curl example usually returns a JSON object in v2.
        // Let's assume JSON response first, but fallback to text checking.

        // Common NetGSM codes:
        // 00: Success
        // 30: Auth Error

        try
        {
            // Try parse as JSON
            using var doc = JsonDocument.Parse(responseText);
            if (doc.RootElement.TryGetProperty("code", out var codeProp))
            {
                var code = codeProp.GetString();
                if (code == "00")
                {
                    var jobId = doc.RootElement.TryGetProperty("jobid", out var jobIdProp) ? jobIdProp.GetString() : null;
                    return SmsSendResult.CreateSuccess("SMS başarıyla gönderildi.", jobId);
                }
                return SmsSendResult.CreateFailure(GetErrorMessage(code), code);
            }
        }
        catch
        {
            // Fallback to text parsing if response is different
        }

        // If simple text (which matches old behavior, but v2 "application/json" output usually is JSON)
        var trimmed = responseText.Trim();
        if (trimmed.StartsWith("00"))
        {
            var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var messageId = parts.Length > 1 ? parts[1] : null;
            return SmsSendResult.CreateSuccess("SMS başarıyla gönderildi.", messageId);
        }

        return SmsSendResult.CreateFailure(GetErrorMessage(trimmed), trimmed);
    }

    private bool IsAuthFailure(string responseText)
    {
        // Check for "30" code in JSON or text
        if (responseText.Contains("\"code\":\"30\"") || responseText.Trim() == "30") return true;
        return false;
    }

    private string GetErrorMessage(string? code)
    {
        return code switch
        {
            "20" => "Mesaj metni veya parametre hatası.",
            "30" => "Geçersiz kullanıcı adı, şifre veya API erişim izni yok.",
            "40" => "Mesaj başlığınızda (Gönderici Adı) hata var.",
            "70" => "Hatalı sorgulama.",
            "80" => "Gönderim sınırı aşıldı.",
            _ => $"NetGSM Hata Kodu: {code}"
        };
    }

    private string? FormatPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber)) return null;

        var digitsOnly = Regex.Replace(phoneNumber, @"\D", "");

        if (digitsOnly.StartsWith("90")) digitsOnly = digitsOnly.Substring(2);
        else if (digitsOnly.StartsWith("0")) digitsOnly = digitsOnly.Substring(1);

        if (digitsOnly.Length == 10 && digitsOnly.StartsWith("5")) return digitsOnly;

        return null;
    }

    // Inner classes for JSON serialization
    private class NetGsmSendRequest
    {
        [JsonPropertyName("msgheader")]
        public string? MsgHeader { get; set; }

        [JsonPropertyName("startdate")]
        public string StartDate { get; set; } = "";

        [JsonPropertyName("stopdate")]
        public string StopDate { get; set; } = "";

        [JsonPropertyName("appname")]
        public string AppName { get; set; } = "";

        [JsonPropertyName("partnercode")]
        public string PartnerCode { get; set; } = "";

        [JsonPropertyName("encoding")]
        public string Encoding { get; set; } = "TR";

        [JsonPropertyName("messages")]
        public NetGsmMessageItem[] Messages { get; set; } = Array.Empty<NetGsmMessageItem>();
    }

    private class NetGsmMessageItem
    {
        [JsonPropertyName("msg")]
        public string? Msg { get; set; }

        [JsonPropertyName("no")]
        public string? No { get; set; }
    }
}
