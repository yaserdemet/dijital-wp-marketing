using PushDashboard.Models;

namespace PushDashboard.DTOs.Store;

public class ModuleStoreDto
{
    public int ModuleId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? DetailedDescription { get; set; }
    public string? IconClass { get; set; }
    public string? IconColor { get; set; }

    // Yalnızca paketli yapıya sahip olan modüller
    public List<ModulePackageDto> Packages { get; set; } = new();
}

public class ModulePackageDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal QuotaAmount { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal YearlyPrice { get; set; }
    public int YearlyDiscountPercent { get; set; }
    public string? HtmlContent { get; set; }
}

public class PurchasePackageRequestDto
{
    public int ModuleId { get; set; }
    public int PackageId { get; set; }
}
