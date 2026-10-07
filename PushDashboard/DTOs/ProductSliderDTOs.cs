using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PushDashboard.DTOs;

#region Request DTOs

public class CreateProductSliderRequestDto
{
    [Required(ErrorMessage = "Slider adı gereklidir.")]
    [StringLength(200, ErrorMessage = "Slider adı en fazla 200 karakter olabilir.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Görüntüleme tipi gereklidir.")]
    public string DisplayType { get; set; } = "slider";
}

public class UpdateProductSliderRequestDto
{
    [Required]
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [Required(ErrorMessage = "Slider adı gereklidir.")]
    [StringLength(200, ErrorMessage = "Slider adı en fazla 200 karakter olabilir.")]
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Görüntüleme tipi gereklidir.")]
    [JsonPropertyName("displayType")]
    public string DisplayType { get; set; } = "slider";

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
}

public class CreateProductSliderItemRequestDto
{
    public int SliderId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public string? ProductImage { get; set; }
    public string ProductUrl { get; set; } = string.Empty;
    public string? ExternalProductId { get; set; }
    public decimal? ProductPrice { get; set; }
    public decimal? DiscountPrice { get; set; }
    public decimal? KDVAmount { get; set; }
    public double TaxRate { get; set; } = 18;
    public bool KdvIncluded { get; set; } = true;
    public string? Currency { get; set; } = "TRY";
    public string? ProductDescription { get; set; }
    public string? ManualDescription { get; set; }
    public int SortOrder { get; set; } = 0;
}

public class UpdateProductSliderItemRequestDto
{
    [Required]
    public int Id { get; set; }

    [Required(ErrorMessage = "Ürün başlığı gereklidir.")]
    [StringLength(500, ErrorMessage = "Ürün başlığı en fazla 500 karakter olabilir.")]
    public string ProductTitle { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Ürün resmi URL'si en fazla 1000 karakter olabilir.")]
    public string? ProductImage { get; set; }

    [Required(ErrorMessage = "Ürün URL'si gereklidir.")]
    [StringLength(1000, ErrorMessage = "Ürün URL'si en fazla 1000 karakter olabilir.")]
    [Url(ErrorMessage = "Geçerli bir URL giriniz.")]
    public string ProductUrl { get; set; } = string.Empty;

    [Range(0, 999999.99, ErrorMessage = "Fiyat 0 ile 999999.99 arasında olmalıdır.")]
    public decimal? ProductPrice { get; set; }

    public decimal? DiscountPrice { get; set; }
    public decimal? KDVAmount { get; set; }
    public double TaxRate { get; set; } = 18;
    public bool KdvIncluded { get; set; } = true;

    [StringLength(50)]
    public string? Currency { get; set; } = "TRY";

    [StringLength(1000, ErrorMessage = "Ürün açıklaması en fazla 1000 karakter olabilir.")]
    public string? ProductDescription { get; set; }

    [StringLength(2000, ErrorMessage = "Manuel açıklama en fazla 2000 karakter olabilir.")]
    public string? ManualDescription { get; set; }

    public int SortOrder { get; set; } = 0;

    public bool IsActive { get; set; } = true;
}

public class UpdateProductSliderSettingsRequestDto
{
    [Required]
    public int SliderId { get; set; }

    [StringLength(200, ErrorMessage = "Slider adı en fazla 200 karakter olabilir.")]
    public string? Name { get; set; }

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    public string? Description { get; set; }

    [StringLength(50)]
    public string? DisplayType { get; set; }

    public bool? IsActive { get; set; }

    public bool AutoPlay { get; set; } = true;

    [Range(1000, 30000, ErrorMessage = "Otomatik oynatma aralığı 1000 ile 30000 ms arasında olmalıdır.")]
    public int AutoPlayInterval { get; set; } = 5000;

    public bool ShowArrows { get; set; } = true;
    public bool ShowDots { get; set; } = true;

    [Range(1, 10, ErrorMessage = "Görünüm başına öğe sayısı 1 ile 10 arasında olmalıdır.")]
    public int ItemsPerView { get; set; } = 4;

    [Range(1, 5, ErrorMessage = "Mobil görünüm başına öğe sayısı 1 ile 5 arasında olmalıdır.")]
    public int ItemsPerViewMobile { get; set; } = 1;

    [Range(1, 8, ErrorMessage = "Tablet görünüm başına öğe sayısı 1 ile 8 arasında olmalıdır.")]
    public int ItemsPerViewTablet { get; set; } = 2;

    [StringLength(50)]
    public string Theme { get; set; } = "default";

    [StringLength(20)]
    public string PrimaryColor { get; set; } = "#3B82F6";

    [StringLength(20)]
    public string SecondaryColor { get; set; } = "#1F2937";

    [StringLength(20)]
    public string BackgroundColor { get; set; } = "#FFFFFF";

    [StringLength(20)]
    public string TextColor { get; set; } = "#1F2937";

    public bool EnableAnimations { get; set; } = true;

    [Range(100, 2000, ErrorMessage = "Geçiş süresi 100 ile 2000 ms arasında olmalıdır.")]
    public int TransitionDuration { get; set; } = 300;

    [StringLength(50)]
    public string AnimationType { get; set; } = "slide";

    public bool ShowProductPrice { get; set; } = true;

    public bool ShowProductDescription { get; set; } = false;
    public bool ShowProductImage { get; set; } = true;

    [StringLength(20)]
    public string ImageAspectRatio { get; set; } = "square";

    [StringLength(50)]
    public string? DescriptionSource { get; set; }

    public string? CustomCSS { get; set; }

    public List<SliderItemManualDescriptionDto>? ItemManualDescriptions { get; set; }
}

public class SliderItemManualDescriptionDto
{
    [JsonPropertyName("itemId")]
    public int ItemId { get; set; }

    [JsonPropertyName("manualDescription")]
    public string? ManualDescription { get; set; }
}

#endregion

#region Response DTOs

public class ProductSliderResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DisplayType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int ItemCount { get; set; }
    public string ScriptUrl { get; set; } = string.Empty;
}

public class ProductSliderItemResponseDto
{
    public int Id { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public string? ProductImage { get; set; }
    public string ProductUrl { get; set; } = string.Empty;
    public decimal? ProductPrice { get; set; }
    public decimal? DiscountPrice { get; set; }
    public decimal? KDVAmount { get; set; }
    public double TaxRate { get; set; } = 18;
    public bool KdvIncluded { get; set; } = true;
    public string? Currency { get; set; }
    public string? ProductDescription { get; set; }
    public string? ManualDescription { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class ProductSliderSettingsResponseDto
{
    public int Id { get; set; }
    public int SliderId { get; set; }
    public bool AutoPlay { get; set; }
    public int AutoPlayInterval { get; set; }
    public bool ShowArrows { get; set; }
    public bool ShowDots { get; set; }
    public int ItemsPerView { get; set; }
    public int ItemsPerViewMobile { get; set; }
    public int ItemsPerViewTablet { get; set; }
    public string Theme { get; set; } = string.Empty;
    public string PrimaryColor { get; set; } = string.Empty;
    public string SecondaryColor { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = string.Empty;
    public string TextColor { get; set; } = string.Empty;
    public bool EnableAnimations { get; set; }
    public int TransitionDuration { get; set; }
    public string AnimationType { get; set; } = string.Empty;
    public bool ShowProductPrice { get; set; }
    public bool ShowProductDescription { get; set; }
    public bool ShowProductImage { get; set; }
    public string ImageAspectRatio { get; set; } = string.Empty;
    public string? DescriptionSource { get; set; }
    public string? CustomCSS { get; set; }
}

public class ProductSliderDetailResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string DisplayType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<ProductSliderItemResponseDto> Items { get; set; } = new();
    public ProductSliderSettingsResponseDto? Settings { get; set; }
    public string ScriptUrl { get; set; } = string.Empty;
    public string EmbedCode { get; set; } = string.Empty;
}

#endregion

#region Widget DTOs

public class ProductSliderWidgetConfigDto
{
    public int SliderId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayType { get; set; } = string.Empty;
    public List<ProductSliderItemResponseDto> Items { get; set; } = new();
    public ProductSliderSettingsResponseDto Settings { get; set; } = new();
    public string ApiBaseUrl { get; set; } = string.Empty;
}

#endregion
