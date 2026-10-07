using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

public class Company
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string? Address { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? Website { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Billing Information
    public string? BillingType { get; set; } = "Corporate";

    public string? TaxOffice { get; set; }

    public string? TaxNumber { get; set; }

    public string? IdentityNumber { get; set; }

    public string? BillingAddress { get; set; }

    public string? CompanyName { get; set; }

    public string? FullName { get; set; }
    public DateTime? BillingUpdatedAt { get; set; }

    // Contact Person Information
    public string? ContactFirstName { get; set; }
    public string? ContactLastName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }

    // Address Information
    public string? Province { get; set; }
    public string? District { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }

    // Company Category and Logo
    public string? CompanyCategory { get; set; }
    public string? LogoUrl { get; set; }

    // Company Credit Balance
    public decimal CreditBalance { get; set; } = 0.00m;
    public int SelectedAbandonedTime { get; set; } = 24;

    // PayTR Card Storage Token
    public string? PaytrUtoken { get; set; }

    // Navigation property for users belonging to this company
    public virtual ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();

    // Navigation property for company modules
    public virtual ICollection<CompanyModule> CompanyModules { get; set; } = new List<CompanyModule>();
}
