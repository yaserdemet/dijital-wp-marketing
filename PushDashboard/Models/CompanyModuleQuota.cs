using System.ComponentModel.DataAnnotations;

namespace PushDashboard.Models;

public class CompanyModuleQuota
{
    public int Id { get; set; }

    public Guid CompanyId { get; set; }
    public virtual Company Company { get; set; } = null!;

    public int ModuleId { get; set; }
    public virtual Module Module { get; set; } = null!;

    public int? ModulePackageId { get; set; }
    public virtual ModulePackage? ModulePackage { get; set; }

    public decimal TotalQuota { get; set; }
    public decimal RemainingQuota { get; set; }

}
