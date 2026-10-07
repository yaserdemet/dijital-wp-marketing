using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PushDashboard.DTOs;

/// <summary>
/// Gupshup webhook event DTO
/// </summary>
public class GupshupWebhookEventDto
{
    [Required]
    public string Type { get; set; } = string.Empty;

    public GupshupWebhookPayloadDto? Payload { get; set; }
}

public class GupshupWebhookPayloadDto
{
    public string? Type { get; set; }
    
    public GupshupWebhookMessagePayloadDto? Payload { get; set; }
}

public class GupshupWebhookMessagePayloadDto
{
    /// <summary>
    /// Gupshup message ID (gsId)
    /// </summary>
    [JsonPropertyName("gsId")]
    public string? GsId { get; set; }

    /// <summary>
    /// Timestamp in milliseconds
    /// </summary>
    [JsonPropertyName("ts")]
    public long? Ts { get; set; }

    /// <summary>
    /// Recipient phone number  
    /// </summary>
    [JsonPropertyName("destination")]
    public string? Destination { get; set; }

    /// <summary>
    /// Error information for failed messages
    /// </summary>
    [JsonPropertyName("error")]
    public GupshupWebhookErrorDto? Error { get; set; }
}

public class GupshupWebhookErrorDto
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
