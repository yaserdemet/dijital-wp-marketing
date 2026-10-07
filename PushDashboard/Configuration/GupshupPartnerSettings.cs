namespace PushDashboard.Configuration;

public class GupshupPartnerSettings
{
    public const string SectionName = "GupshupPartnerSettings";

    public string PartnerId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
