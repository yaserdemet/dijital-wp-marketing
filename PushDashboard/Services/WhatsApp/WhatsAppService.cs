using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PushDashboard.Services.WhatsApp;

public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WhatsAppService> _logger;
    private readonly IWhatsAppMessageService? _messageService;
    private const string GupshupApiBaseUrl = "https://partner.gupshup.io/partner/app";
    private const string GupshupPartnerLoginUrl = "https://partner.gupshup.io/partner/account/login";
    private const int TOKEN_VALIDITY_HOURS = 24;
    private const int TOKEN_RENEWAL_BUFFER_HOURS = 1; // Renew 1 hour before expiration

    private readonly IOptions<PushDashboard.Configuration.GupshupPartnerSettings> _partnerSettings;
    
    // Token cache with thread-safe access
    private static TokenCache? _tokenCache;
    private static readonly SemaphoreSlim _tokenSemaphore = new SemaphoreSlim(1, 1);

    public WhatsAppService(
        HttpClient httpClient, 
        ILogger<WhatsAppService> logger,
        IOptions<PushDashboard.Configuration.GupshupPartnerSettings> partnerSettings,
        IWhatsAppMessageService? messageService = null)
    {
        _httpClient = httpClient;
        _logger = logger;
        _partnerSettings = partnerSettings;
        _messageService = messageService;
    }

    public async Task<WhatsAppResult> SendMessageAsync(WhatsAppMessageRequest request)
    {
        try
        {
            if (!IsValidPhoneNumber(request.ToPhoneNumber))
            {
                return WhatsAppResult.CreateFailure("Geçersiz telefon numarası formatı.");
            }

            var messagePayload = CreateMessagePayload(request);
            var json = JsonSerializer.Serialize(messagePayload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var url = $"{GupshupApiBaseUrl}/{request.ConnectionSettings.AppId}/v3/message";

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("apikey", request.ConnectionSettings.Token);

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Gupshup message response: Status={StatusCode}, Content={Content}",
                response.StatusCode, responseContent);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("WhatsApp message sent successfully to {PhoneNumber}", request.ToPhoneNumber);
                
                // Parse message ID from response and log to database
                await LogSentMessageAsync(responseContent, request, "text", request.Message);
                
                return WhatsAppResult.CreateSuccess("WhatsApp mesajı başarıyla gönderildi.");
            }
            else
            {
                _logger.LogError("Failed to send WhatsApp message. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, responseContent);

                return WhatsAppResult.CreateFailure($"WhatsApp mesajı gönderilemedi: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending WhatsApp message to {PhoneNumber}", request.ToPhoneNumber);
            return WhatsAppResult.CreateFailure("WhatsApp mesajı gönderilirken hata oluştu: " + ex.Message);
        }
    }

    public async Task<WhatsAppResult> SendTemplateMessageAsync(WhatsAppTemplateMessageRequest request)
    {
        try
        {
            if (!IsValidPhoneNumber(request.ToPhoneNumber))
            {
                return WhatsAppResult.CreateFailure("Geçersiz telefon numarası formatı.");
            }

            var messagePayload = CreateTemplateMessagePayload(request);
            var json = JsonSerializer.Serialize(messagePayload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });

            _logger.LogInformation("Sending WhatsApp template message: {Json}", json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = $"{GupshupApiBaseUrl}/{request.ConnectionSettings.AppId}/v3/message";

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", request.ConnectionSettings.Token);

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("WhatsApp template message response: Status={StatusCode}, Content={Content}",
                response.StatusCode, responseContent);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("WhatsApp template message sent successfully to {PhoneNumber}", request.ToPhoneNumber);
                return WhatsAppResult.CreateSuccess("WhatsApp şablon mesajı başarıyla gönderildi.");
            }
            else
            {
                _logger.LogError("Failed to send WhatsApp template message. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, responseContent);

                return WhatsAppResult.CreateFailure($"WhatsApp şablon mesajı gönderilemedi: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending WhatsApp template message to {PhoneNumber}", request.ToPhoneNumber);
            return WhatsAppResult.CreateFailure("WhatsApp şablon mesajı gönderilirken hata oluştu: " + ex.Message);
        }
    }

    public async Task<WhatsAppResult> TestConnectionAsync(WhatsAppConnectionSettings settings)
    {
        try
        {
            if (string.IsNullOrEmpty(settings.AppId) || string.IsNullOrEmpty(settings.Token))
            {
                 return WhatsAppResult.CreateFailure("App ID ve Token boş olamaz.");
            }

            // Test connection by trying to list templates
            var templatesResult = await GetMessageTemplatesAsync(settings);
            if (templatesResult.Success)
            {
                return WhatsAppResult.CreateSuccess("Bağlantı başarılı. Şablon listesi alındı.");
            }
            else
            {
                return WhatsAppResult.CreateFailure($"Bağlantı başarısız: {templatesResult.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing WhatsApp connection");
            return WhatsAppResult.CreateFailure("WhatsApp bağlantısı test edilirken hata oluştu: " + ex.Message);
        }
    }

    public async Task<WhatsAppResult<List<FacebookMessageTemplate>>> GetMessageTemplatesAsync(WhatsAppConnectionSettings settings)
    {
        try 
        {
            var url = $"{GupshupApiBaseUrl}/{settings.AppId}/templates";
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", settings.Token);

            var response = await _httpClient.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Gupshup list templates response: Status={StatusCode}, Content={Content}", 
                response.StatusCode, responseContent);

            if (response.IsSuccessStatusCode)
            {
                var gupshupTemplates = JsonSerializer.Deserialize<GupshupTemplateListResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var templates = gupshupTemplates?.Templates?.Select(MapToFacebookTemplate).ToList() ?? new List<FacebookMessageTemplate>();
                return WhatsAppResult<List<FacebookMessageTemplate>>.CreateSuccess(templates);
            }
            else
            {
                return WhatsAppResult<List<FacebookMessageTemplate>>.CreateFailure($"Şablonlar alınamadı: {response.StatusCode} - {responseContent}");
            }
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error getting Gupshup templates");
             return WhatsAppResult<List<FacebookMessageTemplate>>.CreateFailure("Şablon listesi alınırken hata oluştu: " + ex.Message);
        }
    }

    public async Task<WhatsAppResult<FacebookMessageTemplate>> CreateMessageTemplateAsync(WhatsAppConnectionSettings settings, CreateTemplateRequest request)
    {
        try
        {
            // Endpoint from user request: https://partner.gupshup.io/partner/app/{{APP_ID}}/templates
            var url = $"{GupshupApiBaseUrl}/{settings.AppId}/templates";

            // Determine template type from HEADER component format
            var headerComponent = request.Components.FirstOrDefault(c => c.Type == "HEADER");
            var headerFormat = headerComponent?.Format?.ToUpperInvariant();
            var isMediaTemplate = headerFormat is "IMAGE" or "VIDEO" or "DOCUMENT";
            var templateType = isMediaTemplate ? headerFormat! : "TEXT";

            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("elementName", request.Name),
                new KeyValuePair<string, string>("languageCode", request.Language ?? "en_US"),
                new KeyValuePair<string, string>("category", request.Category),
                new KeyValuePair<string, string>("vertical", templateType),
                new KeyValuePair<string, string>("templateType", templateType),
            };

            // Process Components
            var bodyComponent = request.Components.FirstOrDefault(c => c.Type == "BODY");
            var footerComponent = request.Components.FirstOrDefault(c => c.Type == "FOOTER");
            var buttonsComponent = request.Components.FirstOrDefault(c => c.Type == "BUTTONS");

            // MANDATORY UNSUBSCRIBE BUTTON ENFORCEMENT
            // Ensure Unsubscribe button exists
            var unsubscribeUrl = "https://admin.pushonica.com/Unsubscribe";
            var unsubscribeText = "Bildirimden Çık";

            if (buttonsComponent == null)
            {
                buttonsComponent = new TemplateComponent 
                { 
                    Type = "BUTTONS", 
                    Buttons = new List<TemplateButton>() 
                };
                request.Components.Add(buttonsComponent);
            }

            if (buttonsComponent.Buttons == null)
            {
                buttonsComponent.Buttons = new List<TemplateButton>();
            }

            // Check if unsubscribe button already exists (frontend should have added it, but safety check)
            var hasUnsubscribe = buttonsComponent.Buttons.Any(b => b.Type == "URL" && b.Url.Contains(unsubscribeUrl));
            if (!hasUnsubscribe)
            {
                 // Add as the last button
                 buttonsComponent.Buttons.Add(new TemplateButton
                 {
                     Type = "URL",
                     Text = unsubscribeText,
                     Url = unsubscribeUrl
                 });
            }

            // Content (Body) - Required
            if (bodyComponent != null)
            {
                formData.Add(new KeyValuePair<string, string>("content", bodyComponent.Text));
            }

            // Header - Optional
            if (headerComponent != null)
            {
                if (isMediaTemplate)
                {
                    // For media templates, the handle ID comes from example.header_handle or text
                    var mediaValue = headerComponent.Example?.HeaderHandle?.FirstOrDefault()
                                        ?? headerComponent.Text;

                    if (!string.IsNullOrEmpty(mediaValue))
                    {
                        // Use the value (URL or ID) directly
                        var mediaHandleId = mediaValue;

                        // If the value is a URL, upload it to Gupshup first to get a handleId (Required for Template Creation Example)
                        if (mediaValue.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                            mediaValue.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogInformation("Media value is a URL, uploading to Gupshup first for template example: {Url}", mediaValue);
                            
                            // Determine file type from header format
                            var fileType = headerFormat switch
                            {
                                "IMAGE" => "image/jpeg",
                                "VIDEO" => "video/mp4",
                                "DOCUMENT" => "application/pdf",
                                _ => "image/jpeg"
                            };

                            var uploadResult = await UploadMediaFromUrlAsync(settings, mediaValue, fileType);
                            if (uploadResult.Success && !string.IsNullOrEmpty(uploadResult.Data))
                            {
                                mediaHandleId = uploadResult.Data;
                                _logger.LogInformation("URL uploaded successfully for template example, handleId={HandleId}", mediaHandleId);
                            }
                            else
                            {
                                _logger.LogWarning("Failed to upload media URL for template example: {Message}", uploadResult.Message);
                                // Fallback to sending URL if upload fails, though it might fail validation
                            }
                        }
                        
                        _logger.LogInformation("Using media handle for template creation: {MediaHandle}", mediaHandleId);

                        formData.Add(new KeyValuePair<string, string>("exampleMedia", mediaHandleId));
                        formData.Add(new KeyValuePair<string, string>("enableSample", "true"));
                        _logger.LogInformation("Media template: type={TemplateType}, exampleMedia={MediaId}", templateType, mediaHandleId);
                    }
                    else
                    {
                        _logger.LogWarning("Media template but no media handle ID found for template {TemplateName}", request.Name);
                    }
                }
                else if (!string.IsNullOrEmpty(headerComponent.Text))
                {
                    // For text headers, send as header text
                    formData.Add(new KeyValuePair<string, string>("header", headerComponent.Text));
                    formData.Add(new KeyValuePair<string, string>("exampleHeader", headerComponent.Text));
                }
            }

            // Footer - Optional
            if (footerComponent != null && !string.IsNullOrEmpty(footerComponent.Text))
            {
                formData.Add(new KeyValuePair<string, string>("footer", footerComponent.Text));
            }

            // Buttons - Optional
            if (buttonsComponent != null && buttonsComponent.Buttons != null && buttonsComponent.Buttons.Any())
            {
                var gupshupButtons = buttonsComponent.Buttons.Select(b => new
                {
                    type = mapButtonType(b.Type),
                    text = b.Text,
                    url = b.Url,
                    phone_number = b.PhoneNumber,
                    example = !string.IsNullOrEmpty(b.Url) && b.Url.Contains("{{1}}") ? new[] { "https://example.com" } : null 
                }).ToList();

                var buttonsJson = JsonSerializer.Serialize(gupshupButtons);
                formData.Add(new KeyValuePair<string, string>("buttons", buttonsJson));
            }

            // Example - Required
            var exampleString = "Example"; 
            if (bodyComponent != null)
            {
                exampleString = bodyComponent.Text;
                if (bodyComponent.Parameters != null)
                {
                     for (int i = 0; i < bodyComponent.Parameters.Count; i++)
                     {
                         var val = bodyComponent.Parameters[i].Text ?? "value";
                         exampleString = exampleString.Replace($"{{{{{i+1}}}}}", val);
                     }
                }
            }
            formData.Add(new KeyValuePair<string, string>("example", exampleString));

            var requestContent = new FormUrlEncodedContent(formData);

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", settings.Token);

            var response = await _httpClient.PostAsync(url, requestContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Gupshup create template response: Status={StatusCode}, Content={Content}", 
                response.StatusCode, responseContent);

            if (response.IsSuccessStatusCode)
            {
                var createdTemplate = new FacebookMessageTemplate
                {
                    Name = request.Name,
                    Language = request.Language,
                    Category = request.Category,
                    Status = "PENDING", 
                    Components = request.Components
                };
                return WhatsAppResult<FacebookMessageTemplate>.CreateSuccess(createdTemplate, "Şablon başarıyla gönderildi.");
            }
            else
            {
                 return WhatsAppResult<FacebookMessageTemplate>.CreateFailure($"Şablon oluşturulamadı: {response.StatusCode} - {responseContent}");
            }
        }
         catch (Exception ex)
        {
             _logger.LogError(ex, "Error creating Gupshup template");
             return WhatsAppResult<FacebookMessageTemplate>.CreateFailure("Şablon oluşturulurken hata oluştu: " + ex.Message);
        }
    }
    
    private string mapButtonType(string type)
    {
        return type switch
        {
            "QUICK_REPLY" => "QUICK_REPLY", 
            "URL" => "URL",
            "PHONE_NUMBER" => "PHONE_NUMBER",
            _ => type
        };
    }

    public async Task<WhatsAppResult> DeleteMessageTemplateAsync(WhatsAppConnectionSettings settings, string templateName)
    {
        try
        {
            var url = $"{GupshupApiBaseUrl}/{settings.AppId}/template/{templateName}";

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", settings.Token);

            var response = await _httpClient.DeleteAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return WhatsAppResult.CreateSuccess("Şablon başarıyla silindi.");
            }
            else
            {
                return WhatsAppResult.CreateFailure($"Şablon silinemedi: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            return WhatsAppResult.CreateFailure("Şablon silinirken hata oluştu: " + ex.Message);
        }
    }

    public async Task<WhatsAppResult<FacebookMessageTemplate>> GetMessageTemplateAsync(WhatsAppConnectionSettings settings, string templateId)
    {
        // Re-use list to find one
        var listResult = await GetMessageTemplatesAsync(settings);
        if (listResult.Success && listResult.Data != null)
        {
            var template = listResult.Data.FirstOrDefault(t => t.Name == templateId || t.Id == templateId);
            if (template != null) return WhatsAppResult<FacebookMessageTemplate>.CreateSuccess(template);
        }
         return WhatsAppResult<FacebookMessageTemplate>.CreateFailure("Şablon bulunamadı.");
    }

    public bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        var digitsOnly = Regex.Replace(phoneNumber, @"\D", "");
        return digitsOnly.Length >= 10 && digitsOnly.Length <= 15;
    }

    #region Helper Methods

    private object CreateMessagePayload(WhatsAppMessageRequest request)
    {
        return new
        {
            destination = FormatPhoneNumber(request.ToPhoneNumber),
            message = new
            {
                type = "text",
                text = request.Message
            },
            source = request.ConnectionSettings.AppId
        };
    }

    private object CreateTemplateMessagePayload(WhatsAppTemplateMessageRequest request)
    {
        // Build parameters array for each component
        var componentsList = new List<object>();
        
        foreach(var comp in request.Components)
        {
            if (comp.Parameters != null && comp.Parameters.Any())
            {
                var parameters = comp.Parameters.Select(param =>
                {
                    // For media types (image, video, document), create the proper nested structure
                    // For media types (image, video, document), create the proper nested structure
                    // Check if MediaUrl is a URL or handle ID
                    // Gupshup handles (e.g. "4::...") should be sent as 'link', not 'id'. 'id' is for Meta Media IDs (numeric).
                    bool isLink = param.MediaUrl?.StartsWith("http", StringComparison.OrdinalIgnoreCase) == true || 
                                  param.MediaUrl?.Contains("::") == true;

                    if (param.Type == "image" && !string.IsNullOrEmpty(param.MediaUrl))
                    {
                        return (object)new { type = "image", image = isLink ? (object)new { link = param.MediaUrl } : new { id = param.MediaUrl } };
                    }
                    else if (param.Type == "video" && !string.IsNullOrEmpty(param.MediaUrl))
                    {
                        return (object)new { type = "video", video = isLink ? (object)new { link = param.MediaUrl } : new { id = param.MediaUrl } };
                    }
                    else if (param.Type == "document" && !string.IsNullOrEmpty(param.MediaUrl))
                    {
                        return (object)new { type = "document", document = isLink ? (object)new { link = param.MediaUrl } : new { id = param.MediaUrl } };
                    }
                    else
                    {
                        return (object)new { type = "text", text = param.Text };
                    }
                }).ToArray();

                componentsList.Add(new
                {
                    type = comp.Type?.ToLower() ?? "body",
                    parameters = parameters
                });
            }
        }

        return new Dictionary<string, object>
        {
            { "to", FormatPhoneNumber(request.ToPhoneNumber) },
            { "template", new
                {
                    name = request.TemplateName,
                    language = new { code = request.TemplateLanguage.ToUpper() },
                    components = componentsList.ToArray()
                }
            },
            { "messaging_product", "whatsapp" }, 
            { "type", "template" }, 
            { "recipient_type", "individual" }
        };
    }

    private string FormatPhoneNumber(string phoneNumber)
    {
        var digitsOnly = Regex.Replace(phoneNumber, @"\D", "");
        if (digitsOnly.StartsWith("0"))
        {
            digitsOnly = "90" + digitsOnly.Substring(1);
        }
        else if (!digitsOnly.StartsWith("90") && digitsOnly.Length == 10)
        {
            digitsOnly = "90" + digitsOnly;
        }
        return digitsOnly;
    }
    
    private FacebookMessageTemplate MapToFacebookTemplate(GupshupTemplate gt)
    {
        var template = new FacebookMessageTemplate
        {
            Id = gt.Id ?? gt.ElementName,
            Name = gt.ElementName,
            Status = gt.Status,
            Category = gt.Category,
            Language = gt.LanguageCode,
            Components = new List<TemplateComponent>()
        };

        // Try to parse containerMeta first as it has most details
        if (!string.IsNullOrEmpty(gt.ContainerMeta))
        {
            try
            {
                var meta = JsonSerializer.Deserialize<GupshupContainerMeta>(gt.ContainerMeta, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (meta != null)
                {
                    // HEADER
                    // Determine header format from templateType or vertical
                    var typeSource = gt.TemplateType ?? gt.Vertical;
                    var headerFormat = typeSource?.ToUpperInvariant() switch
                    {
                        "IMAGE" => "IMAGE",
                        "VIDEO" => "VIDEO",
                        "DOCUMENT" => "DOCUMENT",
                        _ => "TEXT"
                    };

                    _logger.LogInformation("Mapping template {Name}: Type={Type}, Vertical={Vertical} -> HeaderFormat={Format}", 
                        gt.ElementName, gt.TemplateType, gt.Vertical, headerFormat);

                    if (!string.IsNullOrEmpty(meta.Header) || headerFormat != "TEXT")
                    {
                        // For media templates, store samleMedia URL so it can be used when sending
                        var headerText = meta.Header;
                        if (headerFormat != "TEXT")
                        {
                            headerText = meta.SampleMedia ?? gt.ExampleMedia ?? headerText;
                        }

                        template.Components.Add(new TemplateComponent
                        {
                            Type = "HEADER",
                            Format = headerFormat,
                            Text = headerText
                        });
                    }

                    // BODY (data field in meta seems to be body)
                    if (!string.IsNullOrEmpty(meta.Data))
                    {
                        template.Components.Add(new TemplateComponent
                        {
                            Type = "BODY",
                            Text = meta.Data
                        });
                    }

                    // FOOTER
                    if (!string.IsNullOrEmpty(meta.Footer))
                    {
                        template.Components.Add(new TemplateComponent
                        {
                            Type = "FOOTER",
                            Text = meta.Footer
                        });
                    }

                    // BUTTONS
                    if (meta.Buttons != null && meta.Buttons.Any())
                    {
                        var btnComponent = new TemplateComponent
                        {
                            Type = "BUTTONS",
                            Buttons = meta.Buttons.Select(b => new TemplateButton
                            {
                                Type = b.Type,
                                Text = b.Text,
                                Url = b.Url,
                                PhoneNumber = b.PhoneNumber
                            }).ToList()
                        };
                        template.Components.Add(btnComponent);
                    }
                    
                    return template;
                }
            }
            catch(Exception ex) 
            {
                _logger.LogWarning(ex, "Failed to parse ContainerMeta for template {TemplateName}", gt.ElementName);
            }
        }

        // Fallback: If no containerMeta or parsing failed, use top-level fields
        // Content might be in 'content' or 'data' field
        var content = gt.Content ?? gt.Data; 
        if (!string.IsNullOrEmpty(content))
        {
             template.Components.Add(new TemplateComponent 
             { 
                 Type = "BODY", 
                 Text = content 
             });
        }

        return template;
    }
    
    // Internal classes for JSON Deserialization
    private class GupshupTemplateListResponse 
    {
        [JsonPropertyName("templates")]
        public List<GupshupTemplate>? Templates { get; set; }
        
        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    private class GupshupTemplate
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("elementName")]
        public string ElementName { get; set; } = "";

        [JsonPropertyName("category")]
        public string? Category { get; set; }
        
        [JsonPropertyName("status")]
        public string? Status { get; set; } 
        
        [JsonPropertyName("languageCode")]
        public string? LanguageCode { get; set; }
        
        [JsonPropertyName("content")]
        public string? Content { get; set; }

        [JsonPropertyName("data")]
        public string? Data { get; set; }

        [JsonPropertyName("containerMeta")]
        public string? ContainerMeta { get; set; }
        
        [JsonPropertyName("templateType")]
        public string? TemplateType { get; set; }

        [JsonPropertyName("vertical")]
        public string? Vertical { get; set; }

        [JsonPropertyName("exampleMedia")]
        public string? ExampleMedia { get; set; }
    }

    private class GupshupContainerMeta
    {
        [JsonPropertyName("header")]
        public string? Header { get; set; }

        [JsonPropertyName("data")]
        public string? Data { get; set; } // Body text

        [JsonPropertyName("footer")]
        public string? Footer { get; set; }

        [JsonPropertyName("buttons")]
        public List<GupshupButton>? Buttons { get; set; }

        [JsonPropertyName("sampleMedia")]
        public string? SampleMedia { get; set; }

        [JsonPropertyName("enableSample")]
        public bool? EnableSample { get; set; }
    }

    private class GupshupButton
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = ""; // URL, PHONE_NUMBER, QUICK_REPLY

        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("phone_number")]
        public string? PhoneNumber { get; set; }
    }

    public async Task<WhatsAppResult<string>> CreatePartnerAppAsync(string appName)
    {
        try
        {
            var url = $"{GupshupApiBaseUrl}";
            appName = appName.Replace(" ", "").Replace("_","").Replace("-","");
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("name", appName.Replace(" ","")),
                new KeyValuePair<string, string>("templateMessaging", "true"),
                new KeyValuePair<string, string>("disableOptinPrefUrl","true")
            };
            var content = new FormUrlEncodedContent(formData);

            _httpClient.DefaultRequestHeaders.Clear();
            var token = await GetManagedPartnerTokenAsync();
            _httpClient.DefaultRequestHeaders.Add("token", token);

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Gupshup create partner app response: Status={StatusCode}, Content={Content}", 
                response.StatusCode, responseContent);

            if (response.IsSuccessStatusCode)
            {
                 // Expected JSON: { "status": "success", "partnerAppId": "..." }
                 var result = JsonSerializer.Deserialize<GupshupCreateAppResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                 
                 if (result?.Status == "success" && !string.IsNullOrEmpty(result.AppId))
                 {
                     return WhatsAppResult<string>.CreateSuccess(result.AppId, "Uygulama başarıyla oluşturuldu.");
                 }
                 else
                 {
                     return WhatsAppResult<string>.CreateFailure($"Uygulama oluşturulamadı: {responseContent}");
                 }
            }
            else
            {
                return WhatsAppResult<string>.CreateFailure($"Gupshup hatası: {response.StatusCode} - {responseContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Gupshup partner app");
            return WhatsAppResult<string>.CreateFailure("Partner uygulaması oluşturulurken hata oluştu: " + ex.Message);
        }
    }

    public async Task<WhatsAppResult<string>> GetOnboardingEmbedUrlAsync(string appId)
    {
        try
        {
            var url = $"{GupshupApiBaseUrl}/{appId}/onboarding/embed/link?regenerate=false&user=Dijitaldesin&lang=Turkish";

            _httpClient.DefaultRequestHeaders.Clear();
            var token = await GetManagedPartnerTokenAsync();
            _httpClient.DefaultRequestHeaders.Add("token", token);

            var response = await _httpClient.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Gupshup get onboarding link response: Status={StatusCode}, Content={Content}", 
                response.StatusCode, responseContent);

            if (response.IsSuccessStatusCode)
            {
                // Verify if it is a simple string or JSON. Docs say "embed-link".
                // I'll assume it returns the URL as a string or a JSON with URL.
                // If it's a URL, it starts with http.
                // If it's JSON, parse it.
                // Let's safe-guard.
                var result = JsonSerializer.Deserialize<EmbedResponse>(responseContent,new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); // Remove quotes if string JSON
                if (result.Status == "success")
                {
                    return WhatsAppResult<string>.CreateSuccess(result.Link);
                }
                
                // Try parse JSON just in case? Or just return content?
                // Given user didn't specify JSON structure, I'll return content but cleaned.
                return WhatsAppResult<string>.CreateFailure("Onboarding linki alınamadı");
            }
            else
            {
                return WhatsAppResult<string>.CreateFailure($"Onboarding linki alınamadı: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Gupshup onboarding link");
            return WhatsAppResult<string>.CreateFailure("Onboarding linki alınırken hata oluştu: " + ex.Message);
        }
    }

    public async Task<WhatsAppResult<string>> GetAppTokenAsync(string appId)
    {
        try
        {
            var url = $"{GupshupApiBaseUrl}/{appId}/token";

            _httpClient.DefaultRequestHeaders.Clear();
            var token = await GetManagedPartnerTokenAsync();
            _httpClient.DefaultRequestHeaders.Add("token", token);

            var response = await _httpClient.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Gupshup get app token response: Status={StatusCode}, Content={Content}", 
                response.StatusCode, responseContent);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<TokenResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (result?.Status == "success" && result.Token != null && !string.IsNullOrEmpty(result.Token.Token))
                {
                    return WhatsAppResult<string>.CreateSuccess(result.Token.Token);
                }
                
                return WhatsAppResult<string>.CreateFailure("Token alınamadı");
            }
            else
            {
                return WhatsAppResult<string>.CreateFailure($"Token alınamadı: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Gupshup app token");
            return WhatsAppResult<string>.CreateFailure("Token alınırken hata oluştu: " + ex.Message);
        }
    }

    private class GupshupCreateAppResponse
    {
        public string? Status { get; set; }

        public string? AppId { get; set; }
    }
    
    public class EmbedResponse
    {
        public string Link { get; set; }
        public string Status { get; set; }
    }

    private class TokenResponse
    {
        public string Status { get; set; }
        public TokenData Token { get; set; }
    }

    private class TokenData
    {
        public string Token { get; set; }
        public string AuthoriserId { get; set; }
        public string RequestorId { get; set; }
        public long CreatedOn { get; set; }
        public long ModifiedOn { get; set; }
        public long ExpiresOn { get; set; }
        public bool Active { get; set; }
    }

    // Token cache for partner token management
    private class TokenCache
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        
        public bool IsValid()
        {
            // Token is valid if it exists and hasn't expired (with buffer for renewal)
            return !string.IsNullOrEmpty(Token) && 
                   DateTime.Now.AddHours(TOKEN_RENEWAL_BUFFER_HOURS) < ExpiresAt;
        }
    }

    // Partner login response structure
    private class PartnerLoginResponse
    {
        [JsonPropertyName("token")]
        public string? Token { get; set; }
        
        [JsonPropertyName("id")]
        public int Id { get; set; }
        
        [JsonPropertyName("email")]
        public string? Email { get; set; }
    }

    /// <summary>
    /// Gets a managed partner token with automatic renewal when expired
    /// </summary>
    public async Task<string> GetManagedPartnerTokenAsync()
    {
        // Check if we have a valid cached token
        if (_tokenCache?.IsValid() == true)
        {
            var timeRemaining = _tokenCache.ExpiresAt - DateTime.Now;
            _logger.LogInformation("Using cached partner token (expires in {Hours:F1} hours)", 
                timeRemaining.TotalHours);
            return _tokenCache.Token;
        }

        // Need to fetch/refresh token - use semaphore to prevent concurrent requests
        await _tokenSemaphore.WaitAsync();
        try
        {
            // Double-check after acquiring lock (another thread might have refreshed)
            if (_tokenCache?.IsValid() == true)
            {
                return _tokenCache.Token;
            }

            _logger.LogInformation("Fetching new partner token via login API");

            // Call Gupshup partner login API
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("email", _partnerSettings.Value.Email),
                new KeyValuePair<string, string>("password", _partnerSettings.Value.Password)
            };
            var content = new FormUrlEncodedContent(formData);

            var response = await _httpClient.PostAsync(GupshupPartnerLoginUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get partner token. Status: {StatusCode}, Response: {Response}",
                    response.StatusCode, responseContent);
                
                // Fallback to configured static token if available
                if (!string.IsNullOrEmpty(_partnerSettings.Value.Token))
                {
                    _logger.LogWarning("Falling back to static configured token");
                    return _partnerSettings.Value.Token;
                }
                
                throw new Exception($"Failed to retrieve partner token: {response.StatusCode}");
            }

            var loginResponse = JsonSerializer.Deserialize<PartnerLoginResponse>(responseContent, 
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (string.IsNullOrEmpty(loginResponse?.Token))
            {
                throw new Exception("Login successful but token not found in response");
            }

            // Parse JWT to get expiration time
            var expiresAt = DateTime.Now.AddHours(TOKEN_VALIDITY_HOURS);
            try
            {
                var jwtParts = loginResponse.Token.Split('.');
                if (jwtParts.Length == 3)
                {
                    // Decode JWT payload (base64url)
                    var payload = jwtParts[1];
                    // Add padding if needed
                    payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                    payload = payload.Replace('-', '+').Replace('_', '/');
                    
                    var payloadBytes = Convert.FromBase64String(payload);
                    var payloadJson = Encoding.UTF8.GetString(payloadBytes);
                    var payloadDoc = JsonDocument.Parse(payloadJson);
                    
                    if (payloadDoc.RootElement.TryGetProperty("exp", out var expElement))
                    {
                        var expTimestamp = expElement.GetInt64();
                        expiresAt = DateTimeOffset.FromUnixTimeSeconds(expTimestamp).UtcDateTime;
                        _logger.LogInformation("Parsed token expiration from JWT: {ExpiresAt}", expiresAt);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not parse JWT expiration, using default 24-hour validity");
            }

            // Cache the token
            _tokenCache = new TokenCache
            {
                Token = loginResponse.Token,
                ExpiresAt = expiresAt
            };

            _logger.LogInformation("Partner token retrieved successfully, expires at {ExpiresAt} UTC", 
                expiresAt);

            return _tokenCache.Token;
        }
        finally
        {
            _tokenSemaphore.Release();
        }
    }

    /// <summary>
    /// Gönderilen mesajı veritabanına kaydeder
    /// </summary>
    private async Task LogSentMessageAsync(
        string responseContent,
        WhatsAppMessageRequest request,
        string messageType,
        string? messageContent)
    {
        if (_messageService == null) return;

        try
        {
            // Parse Gupshup response to get message ID
            var response = JsonSerializer.Deserialize<GupshupSendMessageResponse>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (response?.MessageId == null)
            {
                _logger.LogWarning("Could not parse message ID from Gupshup response");
                return;
            }

            // Extract company ID from request context if available
            // Default to Guid.Empty, should be passed from calling code
            var companyId = request is IHasCompanyId hasCompanyId
                ? hasCompanyId.CompanyId
                : Guid.Empty;

            if (companyId == Guid.Empty)
            {
                _logger.LogWarning("CompanyId not available for message logging");
                return;
            }

            await _messageService.CreateMessageAsync(
                companyId: companyId,
                gupshupMessageId: response.MessageId,
                recipientPhone: FormatPhoneNumber(request.ToPhoneNumber),
                templateName: null,
                templateId: null,
                messageType: messageType,
                messageContent: messageContent,
                source: "whatsapp_service",
                sourceReferenceId: null,
                appId: request.ConnectionSettings.AppId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log sent message to database");
        }
    }

    /// <summary>
    /// Gönderilen template mesajını veritabanına kaydeder
    /// </summary>
    private async Task LogSentTemplateMessageAsync(
        string responseContent,
        WhatsAppTemplateMessageRequest request)
    {
        if (_messageService == null) return;

        try
        {
            var response = JsonSerializer.Deserialize<GupshupSendMessageResponse>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (response?.MessageId == null)
            {
                _logger.LogWarning("Could not parse message ID from Gupshup response");
                return;
            }

            var companyId = request is IHasCompanyId hasCompanyId
                ? hasCompanyId.CompanyId
                : Guid.Empty;

            if (companyId == Guid.Empty)
            {
                _logger.LogWarning("CompanyId not available for template message logging");
                return;
            }

            await _messageService.CreateMessageAsync(
                companyId: companyId,
                gupshupMessageId: response.MessageId,
                recipientPhone: FormatPhoneNumber(request.ToPhoneNumber),
                templateName: request.TemplateName,
                templateId: null,
                messageType: "template",
                messageContent: $"Template: {request.TemplateName}",
                source: "whatsapp_service",
                sourceReferenceId: null,
                appId: request.ConnectionSettings.AppId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log sent template message to database");
        }
    }

    // Gupshup send message response structure
    private class GupshupSendMessageResponse
    {
        [JsonPropertyName("messageId")]
        public string? MessageId { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    // Interface to extract CompanyId from requests
    private interface IHasCompanyId
    {
        Guid CompanyId { get; }
    }

    #endregion

    #region Template Analytics

    public async Task<WhatsAppResult<TemplateAnalyticsResponse>> GetTemplateAnalyticsAsync(
        WhatsAppConnectionSettings settings,
        string templateId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            // Set default dates if not provided (last 29 days to stay within 30-day limit)
            var effectiveEndDate = endDate ?? DateTime.Now;
            var effectiveStartDate = startDate ?? effectiveEndDate.AddDays(-29);
            
            // Validate date range is maximum 30 days
            var daysDifference = (effectiveEndDate - effectiveStartDate).TotalDays;
            if (daysDifference >= 30)
            {
                return WhatsAppResult<TemplateAnalyticsResponse>.CreateFailure(
                    "Tarih aralığı 30 günden az olmalıdır.");
            }
            if (daysDifference < 0)
            {
                return WhatsAppResult<TemplateAnalyticsResponse>.CreateFailure(
                    "Başlangıç tarihi bitiş tarihinden sonra olamaz.");
            }
            
            // Convert to Unix timestamps
            var startUnix = new DateTimeOffset(effectiveStartDate.ToUniversalTime()).ToUnixTimeSeconds();
            var endUnix = new DateTimeOffset(effectiveEndDate.ToUniversalTime()).ToUnixTimeSeconds();
            
            // Using the endpoint from user's modification
            var url = $"{GupshupApiBaseUrl}/{settings.AppId}/template/analytics?template_ids={templateId}&product_type=CLOUD_API&start={startUnix}&end={endUnix}";
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", settings.Token);

            _logger.LogInformation("Fetching template analytics for AppId={AppId}, StartDate={StartDate}, EndDate={EndDate}", 
                settings.AppId, effectiveStartDate, effectiveEndDate);

            var response = await _httpClient.GetAsync(url);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Gupshup template analytics response: Status={StatusCode}, Content={Content}", 
                response.StatusCode, responseContent);

            if (response.IsSuccessStatusCode)
            {
                var gupshupResponse = JsonSerializer.Deserialize<GupshupTemplateAnalyticsResponse>(
                    responseContent, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (gupshupResponse?.Status == "success" && gupshupResponse.TemplateAnalytics != null)
                {
                    var resultList = new List<TemplateAnalytics>();

                    foreach (var t in gupshupResponse.TemplateAnalytics)
                    {
                        var buttonClicks = new List<ButtonClickData>();
                        
                        // Parse clicked data if available
                        if (t.Clicked != null && t.Clicked.Any())
                        {
                            buttonClicks = t.Clicked.Select(c => new ButtonClickData
                            {
                                ButtonText = c.ButtonContent ?? "",
                                Clicks = c.Count,
                                Type = c.Type ?? ""
                            }).ToList();
                        }
                        
                        resultList.Add(new TemplateAnalytics
                        {
                            TemplateId = t.TemplateId ?? "",
                            Sent = t.Sent,
                            Delivered = t.Delivered,
                            Read = t.Read,
                            Failed = 0, // Not in response
                            ButtonClicks = buttonClicks
                        });
                    }

                    var result = new TemplateAnalyticsResponse
                    {
                        Templates = resultList
                    };

                    return WhatsAppResult<TemplateAnalyticsResponse>.CreateSuccess(result);
                }

                return WhatsAppResult<TemplateAnalyticsResponse>.CreateFailure(
                    $"Template analytics yanıtı geçersiz: {gupshupResponse?.Status ?? "unknown"}");
            }
            else
            {
                return WhatsAppResult<TemplateAnalyticsResponse>.CreateFailure(
                    $"Template analytics alınamadı: {response.StatusCode} - {responseContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting template analytics for AppId={AppId}", settings.AppId);
            return WhatsAppResult<TemplateAnalyticsResponse>.CreateFailure(
                "Template analytics alınırken hata oluştu: " + ex.Message);
        }
    }

    // Gupshup Template Analytics Response DTOs
    private class GupshupTemplateAnalyticsResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }
        
        [JsonPropertyName("product_type")]
        public string? ProductType { get; set; }
        
        [JsonPropertyName("template_analytics")]
        public List<GupshupTemplateAnalytics>? TemplateAnalytics { get; set; }
    }

    private class GupshupTemplateAnalytics
    {
        [JsonPropertyName("template_id")]
        public string? TemplateId { get; set; }
        
        [JsonPropertyName("start")]
        public long Start { get; set; }
        
        [JsonPropertyName("end")]
        public long End { get; set; }
        
        [JsonPropertyName("sent")]
        public int Sent { get; set; }
        
        [JsonPropertyName("delivered")]
        public int Delivered { get; set; }
        
        [JsonPropertyName("read")]
        public int Read { get; set; }
        
        [JsonPropertyName("clicked")]
        public List<GupshupButtonClick>? Clicked { get; set; }
    }

    private class GupshupButtonClick
    {
        [JsonPropertyName("button_content")]
        public string? ButtonContent { get; set; }
        
        [JsonPropertyName("count")]
        public int Count { get; set; }
        
        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }

    public async Task<WhatsAppResult> EnableTemplateAnalyticsAsync(string appId, string token)
    {
        try
        {
            var url = $"{GupshupApiBaseUrl}/{appId}/template/analytics";
            
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("enable", "true"),
                new KeyValuePair<string, string>("enableOnUi", "true")
            };
            var content = new FormUrlEncodedContent(formData);

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", token);

            _logger.LogInformation("Enabling template analytics for AppId={AppId}", appId);

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Gupshup enable template analytics response: Status={StatusCode}, Content={Content}", 
                response.StatusCode, responseContent);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<GupshupEnableAnalyticsResponse>(
                    responseContent, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Status == "success")
                {
                    return WhatsAppResult.CreateSuccess("Template analytics başarıyla aktif edildi.");
                }

                return WhatsAppResult.CreateFailure($"Template analytics aktif edilemedi: {result?.Status ?? "unknown"}");
            }
            else
            {
                return WhatsAppResult.CreateFailure($"Template analytics aktif edilemedi: {response.StatusCode} - {responseContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling template analytics for AppId={AppId}", appId);
            return WhatsAppResult.CreateFailure("Template analytics aktif edilirken hata oluştu: " + ex.Message);
        }
    }

    public async Task<WhatsAppResult> EnableButtonClickAnalyticsAsync(string appId, string token, string templateId)
    {
        try
        {
            var url = $"{GupshupApiBaseUrl}/{appId}/template/analytics/buttonclick";
            
            var formData = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("templateId", templateId),
                new KeyValuePair<string, string>("disable", "false")
            };
            var content = new FormUrlEncodedContent(formData);

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", token);

            _logger.LogInformation("Enabling button click analytics for AppId={AppId}, TemplateId={TemplateId}", appId, templateId);

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Gupshup enable button click analytics response: Status={StatusCode}, Content={Content}", 
                response.StatusCode, responseContent);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<GupshupEnableAnalyticsResponse>(
                    responseContent, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Status == "success")
                {
                    return WhatsAppResult.CreateSuccess("Button click analytics başarıyla aktif edildi.");
                }

                return WhatsAppResult.CreateFailure($"Button click analytics aktif edilemedi: {result?.Status ?? "unknown"}");
            }
            else
            {
                return WhatsAppResult.CreateFailure($"Button click analytics aktif edilemedi: {response.StatusCode} - {responseContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling button click analytics for AppId={AppId}, TemplateId={TemplateId}", appId, templateId);
            return WhatsAppResult.CreateFailure("Button click analytics aktif edilirken hata oluştu: " + ex.Message);
        }
    }

    private class GupshupEnableAnalyticsResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    /// <summary>
    /// Uploads media to Gupshup and returns the media handle ID.
    /// Required for creating IMAGE, VIDEO, DOCUMENT type templates.
    /// Endpoint: POST /partner/app/{appId}/upload/media
    /// </summary>
    public async Task<WhatsAppResult<string>> UploadMediaAsync(WhatsAppConnectionSettings settings, Stream fileStream, string fileName, string contentType)
    {
        try
        {
            var url = $"{GupshupApiBaseUrl}/{settings.AppId}/upload/media";

            using var formContent = new MultipartFormDataContent();
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            formContent.Add(streamContent, "file", fileName);
            formContent.Add(new StringContent(contentType), "file_type");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", settings.Token);

            var response = await _httpClient.PostAsync(url, formContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Gupshup media upload response: Status={StatusCode}, Content={Content}",
                response.StatusCode, responseContent);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<GupshupMediaUploadResponse>(responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Status == "success" && !string.IsNullOrEmpty(result.MediaId))
                {
                    _logger.LogInformation("Media uploaded successfully. MediaId={MediaId}", result.MediaId);
                    return WhatsAppResult<string>.CreateSuccess(result.MediaId, "Medya başarıyla yüklendi.");
                }

                return WhatsAppResult<string>.CreateFailure($"Medya yüklenemedi: {responseContent}");
            }
            else
            {
                return WhatsAppResult<string>.CreateFailure($"Medya yüklenemedi: {response.StatusCode} - {responseContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading media to Gupshup");
            return WhatsAppResult<string>.CreateFailure("Medya yüklenirken hata oluştu: " + ex.Message);
        }
    }

    /// <summary>
    /// Upload media from a URL to Gupshup and get a handleId.
    /// Endpoint: POST /partner/app/{appId}/upload/media
    /// </summary>
    public async Task<WhatsAppResult<string>> UploadMediaFromUrlAsync(WhatsAppConnectionSettings settings, string mediaUrl, string fileType)
    {
        try
        {
            var url = $"{GupshupApiBaseUrl}/{settings.AppId}/upload/media";

            using var formContent = new MultipartFormDataContent();
            formContent.Add(new StringContent(mediaUrl), "file");
            formContent.Add(new StringContent(fileType), "file_type");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", settings.Token);

            var response = await _httpClient.PostAsync(url, formContent);
            var responseContent = await response.Content.ReadAsStringAsync();

            _logger.LogInformation("Gupshup media URL upload response: Status={StatusCode}, Content={Content}",
                response.StatusCode, responseContent);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<GupshupMediaUploadResponse>(responseContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (result?.Status == "success" && !string.IsNullOrEmpty(result.MediaId))
                {
                    _logger.LogInformation("Media URL uploaded successfully. MediaId={MediaId}", result.MediaId);
                    return WhatsAppResult<string>.CreateSuccess(result.MediaId, "Medya başarıyla yüklendi.");
                }

                return WhatsAppResult<string>.CreateFailure($"Medya URL yüklenemedi: {responseContent}");
            }
            else
            {
                return WhatsAppResult<string>.CreateFailure($"Medya URL yüklenemedi: {response.StatusCode} - {responseContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading media URL to Gupshup");
            return WhatsAppResult<string>.CreateFailure("Medya URL yüklenirken hata oluştu: " + ex.Message);
        }
    }

    private class GupshupMediaUploadResponse
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("handleId")]
        public GupshupHandleId? HandleId { get; set; }

        /// <summary>
        /// Convenience property to get the actual handle ID string from handleId.message
        /// </summary>
        public string? MediaId => HandleId?.Message;
    }

    private class GupshupHandleId
    {
        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    #endregion
}
