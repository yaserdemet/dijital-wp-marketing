namespace PushDashboard.Services.SMS;

/// <summary>
/// SMS provider abstraction to support multiple SMS services (NetGSM, Twilio, etc.)
/// </summary>
public interface ISmsProvider
{
    /// <summary>
    /// Provider name (e.g., "NetGSM", "Twilio")
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Send a single SMS message
    /// </summary>
    Task<SmsSendResult> SendSmsAsync(string phoneNumber, string message, SmsSettings settings);

    /// <summary>
    /// Test connection with provider using given settings
    /// </summary>
    Task<SmsTestResult> TestConnectionAsync(SmsSettings settings);
    
}
