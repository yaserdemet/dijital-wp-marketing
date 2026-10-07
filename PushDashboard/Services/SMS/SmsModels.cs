using System.Text.Json.Serialization;

namespace PushDashboard.Services.SMS;

/// <summary>
/// SMS provider types
/// </summary>
public enum SmsProvider
{
    NetGsm = 0,
    Twilio = 1,
    // Future providers can be added here
}

/// <summary>
/// SMS settings for provider configuration
/// </summary>
public class SmsSettings
{
    public SmsProvider Provider { get; set; } = SmsProvider.NetGsm;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty; // Sender header/name
    public bool AutoNotifications { get; set; } = true;
}

/// <summary>
/// Result of SMS sending operation
/// </summary>
public class SmsSendResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? MessageId { get; set; } // Provider's message ID
    public string? ErrorCode { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }

    public static SmsSendResult CreateSuccess(string message, string? messageId = null)
    {
        return new SmsSendResult
        {
            Success = true,
            Message = message,
            MessageId = messageId
        };
    }

    public static SmsSendResult CreateFailure(string message, string? errorCode = null)
    {
        return new SmsSendResult
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// Result of SMS provider connection test
/// </summary>
public class SmsTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorCode { get; set; }
    public TimeSpan? ResponseTime { get; set; }
    public decimal? AccountBalance { get; set; }

    public static SmsTestResult CreateSuccess(string message, TimeSpan? responseTime = null, decimal? balance = null)
    {
        return new SmsTestResult
        {
            Success = true,
            Message = message,
            ResponseTime = responseTime,
            AccountBalance = balance
        };
    }

    public static SmsTestResult CreateFailure(string message, string? errorCode = null)
    {
        return new SmsTestResult
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }
}
