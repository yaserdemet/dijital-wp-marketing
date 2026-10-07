using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PushDashboard.Models;

/// <summary>
/// Dinamik ürün slider konfigürasyonu
/// </summary>
public class ProductSlider
{
    [Key]
    public int Id { get; set; }

    [Required]
    public Guid CompanyId { get; set; }

    [ForeignKey("CompanyId")]
    public virtual Company Company { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    [Required]
    [StringLength(50)]
    public string DisplayType { get; set; } = "slider"; // slider, tabs, grid

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    [Required]
    [StringLength(450)]
    public string CreatedByUserId { get; set; } = string.Empty;

    [ForeignKey("CreatedByUserId")]
    public virtual ApplicationUser CreatedByUser { get; set; } = null!;

    // Navigation properties
    public virtual ICollection<ProductSliderItem> Items { get; set; } = new List<ProductSliderItem>();
    public virtual ProductSliderSettings? Settings { get; set; }
}

/// <summary>
/// Slider'da gösterilecek ürün bilgileri
/// </summary>
public class ProductSliderItem
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SliderId { get; set; }

    [ForeignKey("SliderId")]
    public virtual ProductSlider Slider { get; set; } = null!;

    [Required]
    [StringLength(500)]
    public string ProductTitle { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? ProductImage { get; set; }

    [Required]
    [StringLength(1000)]
    public string ProductUrl { get; set; } = string.Empty;

    [StringLength(50)]
    public string? ExternalProductId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ProductPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DiscountPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? KDVAmount { get; set; }
    

    public double TaxRate { get; set; } = 18;

    public bool KdvIncluded { get; set; } = true;

    [StringLength(50)]
    public string? Currency { get; set; } = "TRY";

    [StringLength(1000)]
    public string? ProductDescription { get; set; }

    [StringLength(2000)]
    public string? ManualDescription { get; set; }

    public int SortOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// Slider görünüm ve davranış ayarları
/// </summary>
public class ProductSliderSettings
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int SliderId { get; set; }

    [ForeignKey("SliderId")]
    public virtual ProductSlider Slider { get; set; } = null!;

    // Display Settings
    public bool AutoPlay { get; set; } = true;
    public int AutoPlayInterval { get; set; } = 5000; // milliseconds
    public bool ShowArrows { get; set; } = true;
    public bool ShowDots { get; set; } = true;
    public int ItemsPerView { get; set; } = 4;
    public int ItemsPerViewMobile { get; set; } = 1;
    public int ItemsPerViewTablet { get; set; } = 2;

    // Styling Settings
    [StringLength(50)]
    public string Theme { get; set; } = "default"; // default, modern, minimal, colorful

    [StringLength(20)]
    public string PrimaryColor { get; set; } = "#3B82F6"; // Tailwind blue-500

    [StringLength(20)]
    public string SecondaryColor { get; set; } = "#1F2937"; // Tailwind gray-800

    [StringLength(20)]
    public string BackgroundColor { get; set; } = "#FFFFFF";

    [StringLength(20)]
    public string TextColor { get; set; } = "#1F2937";

    // Animation Settings
    public bool EnableAnimations { get; set; } = true;
    public int TransitionDuration { get; set; } = 300; // milliseconds

    [StringLength(50)]
    public string AnimationType { get; set; } = "slide"; // slide, fade, zoom

    // Layout Settings
    public bool ShowProductPrice { get; set; } = true; 

    public bool ShowProductDescription { get; set; } = false;
    public bool ShowProductImage { get; set; } = true;

    [StringLength(20)]
    public string ImageAspectRatio { get; set; } = "square"; // square, landscape, portrait

    [StringLength(50)]
    public string? DescriptionSource { get; set; } // manual, shortDescription, description, customField1, customField2, customField3, customField4, customField5, mobilOzelAlan

    // Custom CSS
    public string? CustomCSS { get; set; }

    // Responsive Settings (JSON)
    public string? ResponsiveBreakpoints { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime? UpdatedAt { get; set; }
}
