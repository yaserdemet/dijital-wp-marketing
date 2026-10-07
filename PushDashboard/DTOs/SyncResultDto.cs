namespace PushDashboard.DTOs;

public class SyncResultDto
{
    public string Message { get; set; } = string.Empty;
    public int SyncedCustomers { get; set; }
    public int SyncedCarts { get; set; }
    public string SyncDuration { get; set; } = string.Empty;
}
