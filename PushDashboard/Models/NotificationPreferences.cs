namespace PushDashboard.Models;

public class NotificationPreferences
{
    public int Id { get; set; }
    
    public string UserId { get; set; } = string.Empty;
    
    public virtual ApplicationUser User { get; set; } = null!;
    
    // E-posta bildirimleri
    public bool EmailInvoiceNotifications { get; set; } = true;
    public bool EmailCreditNotifications { get; set; } = true;
    public bool EmailMarketingNotifications { get; set; } = false;
    
    // SMS bildirimleri
    public bool SmsSecurityAlerts { get; set; } = true;
    public bool SmsPaymentNotifications { get; set; } = false;
    
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
}
