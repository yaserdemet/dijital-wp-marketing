using PushDashboard.Models;

namespace PushDashboard.DTOs;

/// <summary>
/// DTO for integration listing
/// </summary>
public class IntegrationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsConfigured { get; set; }
    public DateTime? LastSyncAt { get; set; }
}

/// <summary>
/// DTO for sync operation result
/// </summary>
public class SyncResultData
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? SyncedCustomers { get; set; }
    public int? SyncedCarts { get; set; }
    public string? SyncDuration { get; set; }
    public DateTime? LastSyncAt { get; set; }
}

/// <summary>
/// DTO for grouped integrations
/// </summary>
public class GroupedIntegrationsDto
{
    public string Category { get; set; } = string.Empty;
    public List<IntegrationWithStatusDto> Integrations { get; set; } = new();
}

/// <summary>
/// DTO for integration with company status
/// </summary>
public class IntegrationWithStatusDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconClass { get; set; } = string.Empty;
    public string IconColor { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsAdded { get; set; }
    public bool IsActive { get; set; }
    public bool IsConfigured { get; set; }
    public DateTime? LastSyncAt { get; set; }
}
