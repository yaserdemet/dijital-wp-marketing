using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

public class Basket
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // SOAP servisinden gelen GuidSepetID ile eşleştirme için
    public string ExternalId { get; set; } = string.Empty;

    // Hangi şirkete ait olduğu
    public Guid CompanyId { get; set; }

    public virtual Company Company { get; set; } = null!;

    // Artık kullanılmıyor, ExternalId kullanılıyor
    public string? GuidBasketId { get; set; }

    public DateTime BasketDate { get; set; }

    public int CustomerId { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public string CustomerEmail { get; set; } = string.Empty;

    public int ProductCount { get; set; } = 0;

    public decimal TotalAmount { get; set; } = 0.00m;

    public decimal TotalTax { get; set; } = 0.00m;

    public decimal ShippingCost { get; set; } = 0.00m;

    public string Currency { get; set; } = "TRY";

    public bool IsActive { get; set; } = true;

    public bool IsAbandoned { get; set; } 

    public DateTime? LastUpdateDate { get; set; }

    // Senkronizasyon bilgileri
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public DateTime? LastSyncDate { get; set; }

    // Navigation properties
    public virtual ICollection<BasketItem> BasketItems { get; set; } = new List<BasketItem>();

    // Helper properties
    [NotMapped]
    public string StatusText => IsActive ? "Aktif" : "Pasif";

    [NotMapped]
    public string StatusBadgeClass => IsActive ? "bg-green-100 text-green-800" : "bg-red-100 text-red-800";

    [NotMapped]
    public string AbandonedStatusText => IsAbandoned ? "Terk Edilmiş" : "Aktif";

    [NotMapped]
    public string AbandonedStatusBadgeClass => IsAbandoned ? "bg-yellow-100 text-yellow-800" : "bg-blue-100 text-blue-800";

    [NotMapped]
    public string FormattedBasketDate => BasketDate.ToString("dd.MM.yyyy HH:mm");

    [NotMapped]
    public string FormattedLastUpdateDate => LastUpdateDate?.ToString("dd.MM.yyyy HH:mm") ?? "Hiç güncellenmemiş";

    [NotMapped]
    public string FormattedTotalAmount => Currency.ToUpper() switch
    {
        "TRY" => $"{TotalAmount:N2} ₺",
        "USD" => $"${TotalAmount:N2}",
        "EUR" => $"€{TotalAmount:N2}",
        "GBP" => $"£{TotalAmount:N2}",
        _ => $"{TotalAmount:N2} {Currency}"
    };

    // Üyelik durumu helper properties
    [NotMapped]
    public bool HasMembership => CustomerId > 0 && !string.IsNullOrEmpty(CustomerName) && !string.IsNullOrEmpty(CustomerEmail);

    [NotMapped]
    public string MembershipStatusText => HasMembership ? "Üyelikli" : "Üyeliksiz";

    [NotMapped]
    public string MembershipStatusBadgeClass => HasMembership ? "bg-green-100 text-green-800" : "bg-gray-100 text-gray-800";

    [NotMapped]
    public string DisplayCustomerName => HasMembership ? CustomerName : "Üyeliksiz Sepet";

    [NotMapped]
    public string DisplayCustomerEmail => HasMembership ? CustomerEmail : "Üye bilgisi yok";

    [NotMapped]
    public string ProfileIconClass => HasMembership ? "text-green-500" : "text-gray-400";
}

public class BasketItem
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BasketId { get; set; }

    public virtual Basket Basket { get; set; } = null!;

    // SOAP servisinden gelen ID
    public int ExternalId { get; set; }

    public string? GuidBasketItemId { get; set; }

    public string ProductCode { get; set; } = string.Empty;

    public string ProductName { get; set; } = string.Empty;

    public string? ProductImage { get; set; }

    public double Quantity { get; set; } = 1;

    public decimal UnitPrice { get; set; } = 0.00m;

    public decimal TotalPrice { get; set; } = 0.00m;

    public double TaxRate { get; set; } = 0;

    public decimal TaxAmount { get; set; } = 0.00m;

    public decimal ShippingCost { get; set; } = 0.00m;

    public bool FreeShipping { get; set; } = false;

    public string Currency { get; set; } = "TRY";

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Helper properties
    [NotMapped]
    public string FormattedUnitPrice => Currency.ToUpper() switch
    {
        "TRY" => $"{UnitPrice:N2} ₺",
        "USD" => $"${UnitPrice:N2}",
        "EUR" => $"€{UnitPrice:N2}",
        "GBP" => $"£{UnitPrice:N2}",
        _ => $"{UnitPrice:N2} {Currency}"
    };

    [NotMapped]
    public string FormattedTotalPrice => Currency.ToUpper() switch
    {
        "TRY" => $"{TotalPrice:N2} ₺",
        "USD" => $"${TotalPrice:N2}",
        "EUR" => $"€{TotalPrice:N2}",
        "GBP" => $"£{TotalPrice:N2}",
        _ => $"{TotalPrice:N2} {Currency}"
    };

    [NotMapped]
    public string FormattedQuantity => Quantity.ToString("N2");
}
