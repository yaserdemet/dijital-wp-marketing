using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PushDashboard.Configuration;
using PushDashboard.Data;
using PushDashboard.DTOs;
using PushDashboard.Models;
using PushDashboard.Services;
using PushDashboard.Services.Integrations;
using System.Text.Json;

namespace PushDashboard.Services;

public class ProductSliderService : IProductSliderService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductSliderService> _logger;
    private readonly IR2StorageService _r2StorageService;
    private readonly R2StorageSettings _r2Settings;
    private readonly IEcommerceServiceFactory _ecommerceServiceFactory;

    public ProductSliderService(
        ApplicationDbContext context,
        ILogger<ProductSliderService> logger,
        IR2StorageService r2StorageService,
        IOptions<R2StorageSettings> r2Settings,
        IEcommerceServiceFactory ecommerceServiceFactory)
    {
        _context = context;
        _logger = logger;
        _r2StorageService = r2StorageService;
        _r2Settings = r2Settings.Value;
        _ecommerceServiceFactory = ecommerceServiceFactory;
    }

    #region Slider Management

    public async Task<List<ProductSliderResponseDto>> GetSlidersAsync(Guid companyId)
    {
        try
        {
            var sliders = await _context.ProductSliders
                .Where(s => s.CompanyId == companyId)
                .Include(s => s.Items)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new ProductSliderResponseDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    DisplayType = s.DisplayType,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    ItemCount = s.Items.Count(i => i.IsActive),
                    ScriptUrl = $"/api/productslider/{companyId}/script/{s.Id}"
                })
                .ToListAsync();

            return sliders;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sliders for company {CompanyId}", companyId);
            return new List<ProductSliderResponseDto>();
        }
    }

    public async Task<ProductSliderDetailResponseDto?> GetSliderDetailAsync(Guid companyId, int sliderId)
    {
        try
        {
            var slider = await _context.ProductSliders
                .Where(s => s.CompanyId == companyId && s.Id == sliderId)
                .Include(s => s.Items)
                .Include(s => s.Settings)
                .FirstOrDefaultAsync();

            if (slider == null)
                return null;

            var result = new ProductSliderDetailResponseDto
            {
                Id = slider.Id,
                Name = slider.Name,
                Description = slider.Description,
                DisplayType = slider.DisplayType,
                IsActive = slider.IsActive,
                CreatedAt = slider.CreatedAt,
                UpdatedAt = slider.UpdatedAt,
                Items = slider.Items.OrderBy(i => i.SortOrder).Select(i => new ProductSliderItemResponseDto
                {
                    Id = i.Id,
                    ProductTitle = i.ProductTitle,
                    ProductImage = i.ProductImage,
                    ProductUrl = i.ProductUrl,
                    ProductPrice = i.ProductPrice,
                    DiscountPrice = i.DiscountPrice,
                    KDVAmount = i.KDVAmount,
                    TaxRate = i.TaxRate,
                    KdvIncluded = i.KdvIncluded,
                    Currency = i.Currency,
                    ProductDescription = i.ProductDescription,
                    ManualDescription = i.ManualDescription,
                    SortOrder = i.SortOrder,
                    IsActive = i.IsActive
                }).ToList(),
                Settings = slider.Settings != null ? new ProductSliderSettingsResponseDto
                {
                    Id = slider.Settings.Id,
                    SliderId = slider.Settings.SliderId,
                    AutoPlay = slider.Settings.AutoPlay,
                    AutoPlayInterval = slider.Settings.AutoPlayInterval,
                    ShowArrows = slider.Settings.ShowArrows,
                    ShowDots = slider.Settings.ShowDots,
                    ItemsPerView = slider.Settings.ItemsPerView,
                    ItemsPerViewMobile = slider.Settings.ItemsPerViewMobile,
                    ItemsPerViewTablet = slider.Settings.ItemsPerViewTablet,
                    Theme = slider.Settings.Theme,
                    PrimaryColor = slider.Settings.PrimaryColor,
                    SecondaryColor = slider.Settings.SecondaryColor,
                    BackgroundColor = slider.Settings.BackgroundColor,
                    TextColor = slider.Settings.TextColor,
                    EnableAnimations = slider.Settings.EnableAnimations,
                    TransitionDuration = slider.Settings.TransitionDuration,
                    AnimationType = slider.Settings.AnimationType,
                    ShowProductPrice = slider.Settings.ShowProductPrice,
                    ShowProductDescription = slider.Settings.ShowProductDescription,
                    ShowProductImage = slider.Settings.ShowProductImage,
                    ImageAspectRatio = slider.Settings.ImageAspectRatio,
                    DescriptionSource = slider.Settings.DescriptionSource,
                    CustomCSS = slider.Settings.CustomCSS
                } : null,
                ScriptUrl = $"/api/productslider/{companyId}/script/{sliderId}",
                EmbedCode = $"<script src=\"/api/productslider/{companyId}/script/{sliderId}\"></script>"
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting slider detail for company {CompanyId}, slider {SliderId}", companyId, sliderId);
            return null;
        }
    }

    public async Task<(bool Success, string Message, int? SliderId)> CreateSliderAsync(Guid companyId, string userId, CreateProductSliderRequestDto request)
    {
        try
        {
            // Check if name is unique
            if (!await IsSliderNameUniqueAsync(companyId, request.Name))
            {
                return (false, "Bu isimde bir slider zaten mevcut.", null);
            }

            var slider = new ProductSlider
            {
                CompanyId = companyId,
                Name = request.Name,
                Description = request.Description,
                DisplayType = request.DisplayType,
                CreatedByUserId = userId,
                CreatedAt = DateTime.Now
            };

            _context.ProductSliders.Add(slider);
            await _context.SaveChangesAsync();

            // Create default settings
            var settings = new ProductSliderSettings
            {
                SliderId = slider.Id,
                CreatedAt = DateTime.Now
            };

            _context.ProductSliderSettings.Add(settings);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new slider {SliderId} for company {CompanyId}", slider.Id, companyId);
            return (true, "Slider başarıyla oluşturuldu.", slider.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating slider for company {CompanyId}", companyId);
            return (false, "Slider oluşturulurken bir hata oluştu.", null);
        }
    }


    public async Task<(bool Success, string Message)> DeleteSliderAsync(Guid companyId, int sliderId)
    {
        try
        {
            var slider = await _context.ProductSliders
                .Include(s => s.Items)
                .Include(s => s.Settings)
                .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.Id == sliderId);

            if (slider == null)
            {
                return (false, "Slider bulunamadı.");
            }

            // Remove related data
            if (slider.Settings != null)
            {
                _context.ProductSliderSettings.Remove(slider.Settings);
            }

            _context.ProductSliderItems.RemoveRange(slider.Items);
            _context.ProductSliders.Remove(slider);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted slider {SliderId} for company {CompanyId}", sliderId, companyId);
            return (true, "Slider başarıyla silindi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting slider {SliderId} for company {CompanyId}", sliderId, companyId);
            return (false, "Slider silinirken bir hata oluştu.");
        }
    }

    public async Task<(bool Success, string Message)> ToggleSliderStatusAsync(Guid companyId, int sliderId)
    {
        try
        {
            var slider = await _context.ProductSliders
                .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.Id == sliderId);

            if (slider == null)
            {
                return (false, "Slider bulunamadı.");
            }

            slider.IsActive = !slider.IsActive;
            slider.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            var status = slider.IsActive ? "aktif" : "pasif";
            _logger.LogInformation("Toggled slider {SliderId} status to {Status} for company {CompanyId}", sliderId, status, companyId);
            return (true, $"Slider {status} duruma getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling slider {SliderId} status for company {CompanyId}", sliderId, companyId);
            return (false, "Slider durumu değiştirilirken bir hata oluştu.");
        }
    }

    #endregion

    #region Slider Items Management

    public async Task<(bool Success, string Message, int? ItemId)> AddSliderItemAsync(Guid companyId, CreateProductSliderItemRequestDto request)
    {
        try
        {
            // Check if slider exists and belongs to company
            if (!await IsSliderOwnedByCompanyAsync(companyId, request.SliderId))
            {
                return (false, "Slider bulunamadı.", null);
            }

            var item = new ProductSliderItem
            {
                SliderId = request.SliderId,
                ProductTitle = request.ProductTitle,
                ProductImage = request.ProductImage,
                ProductUrl = request.ProductUrl,
                ExternalProductId = request.ExternalProductId,
                ProductPrice = request.ProductPrice,
                DiscountPrice = request.DiscountPrice,
                KDVAmount = request.KDVAmount,
                TaxRate = request.TaxRate,
                KdvIncluded = request.KdvIncluded,
                Currency = request.Currency,
                ProductDescription = request.ProductDescription,
                ManualDescription = request.ManualDescription,
                SortOrder = request.SortOrder,
                CreatedAt = DateTime.Now
            };

            _context.ProductSliderItems.Add(item);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Added item {ItemId} to slider {SliderId} for company {CompanyId}", item.Id, request.SliderId, companyId);
            return (true, "Ürün başarıyla eklendi.", item.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding item to slider {SliderId} for company {CompanyId}", request.SliderId, companyId);
            return (false, "Ürün eklenirken bir hata oluştu.", null);
        }
    }


    public async Task<(bool Success, string Message)> DeleteSliderItemAsync(Guid companyId, int itemId)
    {
        try
        {
            var item = await _context.ProductSliderItems
                .Include(i => i.Slider)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.Slider.CompanyId == companyId);

            if (item == null)
            {
                return (false, "Ürün bulunamadı.");
            }

            _context.ProductSliderItems.Remove(item);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted item {ItemId} for company {CompanyId}", itemId, companyId);
            return (true, "Ürün başarıyla silindi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting item {ItemId} for company {CompanyId}", itemId, companyId);
            return (false, "Ürün silinirken bir hata oluştu.");
        }
    }

    public async Task<(bool Success, string Message)> UpdateItemsOrderAsync(Guid companyId, int sliderId, List<int> itemIds)
    {
        try
        {
            if (!await IsSliderOwnedByCompanyAsync(companyId, sliderId))
            {
                return (false, "Slider bulunamadı.");
            }

            var items = await _context.ProductSliderItems
                .Where(i => i.SliderId == sliderId && itemIds.Contains(i.Id))
                .ToListAsync();

            for (int i = 0; i < itemIds.Count; i++)
            {
                var item = items.FirstOrDefault(x => x.Id == itemIds[i]);
                if (item != null)
                {
                    item.SortOrder = i;
                    item.UpdatedAt = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated items order for slider {SliderId} for company {CompanyId}", sliderId, companyId);
            return (true, "Ürün sırası başarıyla güncellendi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating items order for slider {SliderId} for company {CompanyId}", sliderId, companyId);
            return (false, "Ürün sırası güncellenirken bir hata oluştu.");
        }
    }

    public async Task<(bool Success, string Message)> ToggleSliderItemStatusAsync(Guid companyId, int itemId)
    {
        try
        {
            var item = await _context.ProductSliderItems
                .Include(i => i.Slider)
                .FirstOrDefaultAsync(i => i.Id == itemId && i.Slider.CompanyId == companyId);

            if (item == null)
            {
                return (false, "Ürün bulunamadı.");
            }

            item.IsActive = !item.IsActive;
            item.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            var status = item.IsActive ? "aktif" : "pasif";
            _logger.LogInformation("Toggled item {ItemId} status to {Status} for company {CompanyId}", itemId, status, companyId);
            return (true, $"Ürün {status} duruma getirildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling item {ItemId} status for company {CompanyId}", itemId, companyId);
            return (false, "Ürün durumu değiştirilirken bir hata oluştu.");
        }
    }

    #endregion

    #region Validation & Helper Methods

    public async Task<bool> IsSliderOwnedByCompanyAsync(Guid companyId, int sliderId)
    {
        return await _context.ProductSliders
            .AnyAsync(s => s.CompanyId == companyId && s.Id == sliderId);
    }

    public async Task<bool> IsSliderItemOwnedByCompanyAsync(Guid companyId, int itemId)
    {
        return await _context.ProductSliderItems
            .AnyAsync(i => i.Slider.CompanyId == companyId && i.Id == itemId);
    }

    public async Task<bool> IsSliderNameUniqueAsync(Guid companyId, string name, int? excludeSliderId = null)
    {
        var query = _context.ProductSliders
            .Where(s => s.CompanyId == companyId && s.Name.ToLower() == name.ToLower());

        if (excludeSliderId.HasValue)
        {
            query = query.Where(s => s.Id != excludeSliderId.Value);
        }

        return !await query.AnyAsync();
    }

    #endregion

    #region Slider Settings Management

    public async Task<ProductSliderSettingsResponseDto?> GetSliderSettingsAsync(Guid companyId, int sliderId)
    {
        try
        {
            if (!await IsSliderOwnedByCompanyAsync(companyId, sliderId))
            {
                return null;
            }

            var settings = await _context.ProductSliderSettings
                .FirstOrDefaultAsync(s => s.SliderId == sliderId);

            if (settings == null)
            {
                return null;
            }

            return new ProductSliderSettingsResponseDto
            {
                Id = settings.Id,
                SliderId = settings.SliderId,
                AutoPlay = settings.AutoPlay,
                AutoPlayInterval = settings.AutoPlayInterval,
                ShowArrows = settings.ShowArrows,
                ShowDots = settings.ShowDots,
                ItemsPerView = settings.ItemsPerView,
                ItemsPerViewMobile = settings.ItemsPerViewMobile,
                ItemsPerViewTablet = settings.ItemsPerViewTablet,
                Theme = settings.Theme,
                PrimaryColor = settings.PrimaryColor,
                SecondaryColor = settings.SecondaryColor,
                BackgroundColor = settings.BackgroundColor,
                TextColor = settings.TextColor,
                EnableAnimations = settings.EnableAnimations,
                TransitionDuration = settings.TransitionDuration,
                AnimationType = settings.AnimationType,
                    ShowProductPrice = settings.ShowProductPrice,
                    ShowProductDescription = settings.ShowProductDescription,
                    ShowProductImage = settings.ShowProductImage,
                    ImageAspectRatio = settings.ImageAspectRatio,
                    DescriptionSource = settings.DescriptionSource,
                    CustomCSS = settings.CustomCSS
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting slider settings for slider {SliderId} for company {CompanyId}", sliderId, companyId);
            return null;
        }
    }

    public async Task<(bool Success, string Message)> UpdateSliderSettingsAsync(Guid companyId, UpdateProductSliderSettingsRequestDto request)
    {
        try
        {
            if (!await IsSliderOwnedByCompanyAsync(companyId, request.SliderId))
            {
                return (false, "Slider bulunamadı.");
            }

            var slider = await _context.ProductSliders
                .FirstOrDefaultAsync(s => s.CompanyId == companyId && s.Id == request.SliderId);

            if (slider == null)
            {
                return (false, "Slider bulunamadı.");
            }

            // Update ProductSlider fields if provided
            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                if (!await IsSliderNameUniqueAsync(companyId, request.Name, request.SliderId))
                {
                    return (false, "Bu isimde bir slider zaten mevcut.");
                }
                slider.Name = request.Name;
            }

            if (request.Description != null)
            {
                slider.Description = request.Description;
            }

            if (!string.IsNullOrWhiteSpace(request.DisplayType))
            {
                slider.DisplayType = request.DisplayType;
            }

            if (request.IsActive.HasValue)
            {
                slider.IsActive = request.IsActive.Value;
            }

            slider.UpdatedAt = DateTime.Now;

            var settings = await _context.ProductSliderSettings
                .FirstOrDefaultAsync(s => s.SliderId == request.SliderId);

            if (settings == null)
            {
                settings = new ProductSliderSettings
                {
                    SliderId = request.SliderId,
                    CreatedAt = DateTime.Now
                };
                _context.ProductSliderSettings.Add(settings);
            }

            // Update settings
            settings.AutoPlay = request.AutoPlay;
            settings.AutoPlayInterval = request.AutoPlayInterval;
            settings.ShowArrows = request.ShowArrows;
            settings.ShowDots = request.ShowDots;
            settings.ItemsPerView = request.ItemsPerView;
            settings.ItemsPerViewMobile = request.ItemsPerViewMobile;
            settings.ItemsPerViewTablet = request.ItemsPerViewTablet;
            settings.Theme = request.Theme;
            settings.PrimaryColor = request.PrimaryColor;
            settings.SecondaryColor = request.SecondaryColor;
            settings.BackgroundColor = request.BackgroundColor;
            settings.TextColor = request.TextColor;
            settings.EnableAnimations = request.EnableAnimations;
            settings.TransitionDuration = request.TransitionDuration;
            settings.AnimationType = request.AnimationType;
            settings.ShowProductPrice = request.ShowProductPrice;
            settings.ShowProductDescription = request.ShowProductDescription;
            settings.ShowProductImage = request.ShowProductImage;
            settings.ImageAspectRatio = request.ImageAspectRatio;
            settings.DescriptionSource = request.DescriptionSource;
            settings.CustomCSS = request.CustomCSS;
            settings.UpdatedAt = DateTime.Now;

            if (string.Equals(request.DescriptionSource, "manual", StringComparison.OrdinalIgnoreCase) && request.ItemManualDescriptions != null && request.ItemManualDescriptions.Any())
            {
                var itemIds = request.ItemManualDescriptions.Select(x => x.ItemId).Distinct().ToList();
                var items = await _context.ProductSliderItems
                    .Where(i => i.SliderId == request.SliderId && itemIds.Contains(i.Id))
                    .ToListAsync();

                foreach (var dto in request.ItemManualDescriptions)
                {
                    var item = items.FirstOrDefault(i => i.Id == dto.ItemId);
                    if (item == null) continue;
                    item.ManualDescription = string.IsNullOrWhiteSpace(dto.ManualDescription) ? null : dto.ManualDescription.Trim();
                    item.UpdatedAt = DateTime.Now;
                }
            }

            if (string.Equals(request.DescriptionSource, "description", StringComparison.OrdinalIgnoreCase))
            {
                var ecommerceService = await _ecommerceServiceFactory.GetEcommerceServiceAsync(companyId);
                if (ecommerceService != null)
                {
                    var items = await _context.ProductSliderItems
                        .Where(i => i.SliderId == request.SliderId && i.ExternalProductId != null)
                        .ToListAsync();
                    foreach (var item in items)
                    {
                        var product = await ecommerceService.GetProductAsync(companyId, item.ExternalProductId!);
                        if (product != null && !string.IsNullOrEmpty(product.Description))
                        {
                            item.ProductDescription = product.Description.Length > 1000 ? product.Description[..1000] : product.Description;
                            item.UpdatedAt = DateTime.Now;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated settings and slider info for slider {SliderId} for company {CompanyId}", request.SliderId, companyId);
            return (true, "Ayarlar başarıyla güncellendi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating settings for slider {SliderId} for company {CompanyId}", request.SliderId, companyId);
            return (false, "Ayarlar güncellenirken bir hata oluştu.");
        }
    }

    public async Task<(bool Success, string Message)> ResetSliderSettingsAsync(Guid companyId, int sliderId)
    {
        try
        {
            if (!await IsSliderOwnedByCompanyAsync(companyId, sliderId))
            {
                return (false, "Slider bulunamadı.");
            }

            var settings = await _context.ProductSliderSettings
                .FirstOrDefaultAsync(s => s.SliderId == sliderId);

            if (settings != null)
            {
                _context.ProductSliderSettings.Remove(settings);
            }

            // Create new default settings
            var defaultSettings = new ProductSliderSettings
            {
                SliderId = sliderId,
                CreatedAt = DateTime.Now
            };

            _context.ProductSliderSettings.Add(defaultSettings);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Reset settings for slider {SliderId} for company {CompanyId}", sliderId, companyId);
            return (true, "Ayarlar varsayılana sıfırlandı.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting settings for slider {SliderId} for company {CompanyId}", sliderId, companyId);
            return (false, "Ayarlar sıfırlanırken bir hata oluştu.");
        }
    }

    #endregion

    #region Widget & Script Generation

    public async Task<ProductSliderWidgetConfigDto?> GetWidgetConfigAsync(Guid companyId, int sliderId)
    {
        try
        {
            var slider = await _context.ProductSliders
                .Where(s => s.CompanyId == companyId && s.Id == sliderId && s.IsActive)
                .Include(s => s.Items.Where(i => i.IsActive))
                .Include(s => s.Settings)
                .FirstOrDefaultAsync();

            if (slider == null)
                return null;

            var descriptionSource = slider.Settings?.DescriptionSource;

            var config = new ProductSliderWidgetConfigDto
            {
                SliderId = slider.Id,
                Name = slider.Name,
                DisplayType = slider.DisplayType,
                Items = slider.Items.OrderBy(i => i.SortOrder).Select(i => {
                    var description = GetItemDescription(i, descriptionSource);
                    return new ProductSliderItemResponseDto
                    {
                        Id = i.Id,
                        ProductTitle = i.ProductTitle,
                        ProductImage = i.ProductImage,
                        ProductUrl = i.ProductUrl,
                        ProductPrice = i.ProductPrice,
                        DiscountPrice = i.DiscountPrice,
                        KDVAmount = i.KDVAmount,
                        TaxRate = i.TaxRate,
                        KdvIncluded = i.KdvIncluded,
                        Currency = i.Currency,
                        ProductDescription = description,
                        ManualDescription = i.ManualDescription,
                        SortOrder = i.SortOrder,
                        IsActive = i.IsActive
                    };
                }).ToList(),
                Settings = slider.Settings != null ? new ProductSliderSettingsResponseDto
                {
                    Id = slider.Settings.Id,
                    SliderId = slider.Settings.SliderId,
                    AutoPlay = slider.Settings.AutoPlay,
                    AutoPlayInterval = slider.Settings.AutoPlayInterval,
                    ShowArrows = slider.Settings.ShowArrows,
                    ShowDots = slider.Settings.ShowDots,
                    ItemsPerView = slider.Settings.ItemsPerView,
                    ItemsPerViewMobile = slider.Settings.ItemsPerViewMobile,
                    ItemsPerViewTablet = slider.Settings.ItemsPerViewTablet,
                    Theme = slider.Settings.Theme,
                    PrimaryColor = slider.Settings.PrimaryColor,
                    SecondaryColor = slider.Settings.SecondaryColor,
                    BackgroundColor = slider.Settings.BackgroundColor,
                    TextColor = slider.Settings.TextColor,
                    EnableAnimations = slider.Settings.EnableAnimations,
                    TransitionDuration = slider.Settings.TransitionDuration,
                    AnimationType = slider.Settings.AnimationType,
                    ShowProductPrice = slider.Settings.ShowProductPrice,
                    ShowProductDescription = slider.Settings.ShowProductDescription,
                    ShowProductImage = slider.Settings.ShowProductImage,
                    ImageAspectRatio = slider.Settings.ImageAspectRatio,
                    DescriptionSource = slider.Settings.DescriptionSource,
                    CustomCSS = slider.Settings.CustomCSS
                } : new ProductSliderSettingsResponseDto()
            };

            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting widget config for slider {SliderId} for company {CompanyId}", sliderId, companyId);
            return null;
        }
    }





    #endregion

    #region Statistics & Analytics

    public async Task<object> GetSliderStatsAsync(Guid companyId)
    {
        try
        {
            var stats = await _context.ProductSliders
                .Where(s => s.CompanyId == companyId)
                .GroupBy(s => 1)
                .Select(g => new
                {
                    TotalSliders = g.Count(),
                    ActiveSliders = g.Count(s => s.IsActive),
                    TotalProducts = g.Sum(s => s.Items.Count()),
                    ActiveProducts = g.Sum(s => s.Items.Count(i => i.IsActive))
                })
                .FirstOrDefaultAsync();

            return stats ?? new
            {
                TotalSliders = 0,
                ActiveSliders = 0,
                TotalProducts = 0,
                ActiveProducts = 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting slider stats for company {CompanyId}", companyId);
            return new
            {
                TotalSliders = 0,
                ActiveSliders = 0,
                TotalProducts = 0,
                ActiveProducts = 0
            };
        }
    }

    public async Task<object> GetSliderPerformanceAsync(Guid companyId, int sliderId)
    {
        try
        {
            if (!await IsSliderOwnedByCompanyAsync(companyId, sliderId))
            {
                return new { Error = "Slider bulunamadı" };
            }

            var performance = await _context.ProductSliders
                .Where(s => s.Id == sliderId)
                .Select(s => new
                {
                    SliderId = s.Id,
                    Name = s.Name,
                    TotalItems = s.Items.Count(),
                    ActiveItems = s.Items.Count(i => i.IsActive),
                    CreatedAt = s.CreatedAt,
                    LastUpdated = s.UpdatedAt ?? s.CreatedAt
                })
                .FirstOrDefaultAsync();

            return performance ?? (object)new { Error = "Slider bulunamadı" };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting slider performance for slider {SliderId} for company {CompanyId}", sliderId, companyId);
            return new { Error = "Performans verileri alınırken hata oluştu" };
        }
    }

    #endregion

    #region R2 Export Methods

    /// <summary>
    /// Slider ayarlarını R2'ye JSON olarak export eder
    /// </summary>
    public async Task<string> ExportSliderToR2Async(int sliderId, Guid companyId)
    {
        try
        {
            var config = await GetWidgetConfigAsync(companyId, sliderId);
            if (config == null)
            {
                throw new InvalidOperationException("Slider bulunamadı veya aktif değil");
            }

            var jsonContent = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            var key = $"{companyId}/sliders/slider-{sliderId}.json";
            var url = await _r2StorageService.UploadJsonAsync(key, jsonContent);

            _logger.LogInformation("Successfully exported slider {SliderId} to R2 for company {CompanyId}", sliderId, companyId);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting slider {SliderId} to R2 for company {CompanyId}", sliderId, companyId);
            throw;
        }
    }

    /// <summary>
    /// Company'nin tüm aktif slider'larını R2'ye export eder
    /// </summary>
    public async Task<string> ExportAllSlidersToR2Async(Guid companyId)
    {
        try
        {
            var sliders = await _context.ProductSliders
                .Where(s => s.CompanyId == companyId && s.IsActive)
                .Include(s => s.Items.Where(i => i.IsActive))
                .Include(s => s.Settings)
                .OrderBy(s => s.Id)
                .ToListAsync();

            var slidersConfig = new List<ProductSliderWidgetConfigDto>();

            foreach (var slider in sliders)
            {
                var descriptionSource = slider.Settings?.DescriptionSource;

                var config = new ProductSliderWidgetConfigDto
                {
                    SliderId = slider.Id,
                    Name = slider.Name,
                    DisplayType = slider.DisplayType,
                    Items = slider.Items.OrderBy(i => i.SortOrder).Select(i => {
                        var description = GetItemDescription(i, descriptionSource);
                        return new ProductSliderItemResponseDto
                        {
                            Id = i.Id,
                            ProductTitle = i.ProductTitle,
                            ProductImage = i.ProductImage,
                            ProductUrl = i.ProductUrl,
                            ProductPrice = i.ProductPrice,
                            DiscountPrice = i.DiscountPrice,
                            KDVAmount = i.KDVAmount,
                            TaxRate = i.TaxRate,
                            KdvIncluded = i.KdvIncluded,
                            Currency = i.Currency,
                            ProductDescription = description,
                            ManualDescription = i.ManualDescription,
                            SortOrder = i.SortOrder,
                            IsActive = i.IsActive
                        };
                    }).ToList(),
                    Settings = slider.Settings != null ? new ProductSliderSettingsResponseDto
                    {
                        Id = slider.Settings.Id,
                        SliderId = slider.Settings.SliderId,
                        AutoPlay = slider.Settings.AutoPlay,
                        AutoPlayInterval = slider.Settings.AutoPlayInterval,
                        ShowArrows = slider.Settings.ShowArrows,
                        ShowDots = slider.Settings.ShowDots,
                        ItemsPerView = slider.Settings.ItemsPerView,
                        ItemsPerViewMobile = slider.Settings.ItemsPerViewMobile,
                        ItemsPerViewTablet = slider.Settings.ItemsPerViewTablet,
                        Theme = slider.Settings.Theme,
                        PrimaryColor = slider.Settings.PrimaryColor,
                        SecondaryColor = slider.Settings.SecondaryColor,
                        BackgroundColor = slider.Settings.BackgroundColor,
                        TextColor = slider.Settings.TextColor,
                        EnableAnimations = slider.Settings.EnableAnimations,
                        TransitionDuration = slider.Settings.TransitionDuration,
                        AnimationType = slider.Settings.AnimationType,
                        ShowProductPrice = slider.Settings.ShowProductPrice,
                        ShowProductDescription = slider.Settings.ShowProductDescription,
                        ShowProductImage = slider.Settings.ShowProductImage,
                        ImageAspectRatio = slider.Settings.ImageAspectRatio,
                        DescriptionSource = slider.Settings.DescriptionSource,
                        CustomCSS = slider.Settings.CustomCSS
                    } : new ProductSliderSettingsResponseDto()
                };

                slidersConfig.Add(config);
            }

            // Create slider data organized by ID for easier access
            var slidersByIdData = new Dictionary<string, object>();

            foreach (var config in slidersConfig)
            {
                slidersByIdData[config.SliderId.ToString()] = config;
            }

            var allSlidersData = new
            {
                CompanyId = companyId,
                UpdatedAt = DateTime.Now,
                Sliders = slidersByIdData  // Now organized by slider ID
            };

            var jsonContent = JsonSerializer.Serialize(allSlidersData, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            // 1. JSON dosyasını R2'ye yükle
            var jsonKey = $"{companyId}/productSliders.json";
            var jsonUrl = await _r2StorageService.UploadJsonAsync(jsonKey, jsonContent);

            // 2. Standalone JS script'ini oluştur ve R2'ye yükle
            var jsScript = GenerateStandaloneProductSliderScript(companyId);
            var jsKey = $"{companyId}/productSlider.js";
            var jsUrl = await _r2StorageService.UploadJavaScriptAsync(jsKey, jsScript);

            _logger.LogInformation("Successfully exported all sliders to R2 for company {CompanyId}. JSON: {JsonUrl}, JS: {JsUrl}",
                companyId, jsonUrl, jsUrl);

            return jsonUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting all sliders to R2 for company {CompanyId}", companyId);
            throw;
        }
    }



    #endregion

    #region Helper Methods

    private string? GetItemDescription(ProductSliderItem item, string? descriptionSource)
    {
        if (!string.IsNullOrWhiteSpace(item.ManualDescription))
        {
            return item.ManualDescription;
        }

        if (string.IsNullOrWhiteSpace(descriptionSource) || descriptionSource == "manual")
        {
            return item.ProductDescription;
        }

        return descriptionSource switch
        {
            "shortDescription" => item.ProductDescription,
            "description" => item.ProductDescription,
            _ => item.ProductDescription
        };
    }

    #endregion

    private string GenerateJavaScriptWidget(string configJson, string baseUrl)
    {
        return "// Product Slider Widget - " + configJson;
    }





    public string GenerateStandaloneProductSliderScript(Guid companyId)
    {
        try
        {
            // Template dosyasını oku
            var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "js", "modules", "product-slider-module.js");

            if (!File.Exists(templatePath))
            {
                _logger.LogWarning("Standalone template file not found at {TemplatePath}, using fallback", templatePath);
                return GenerateStandaloneFallbackScript(companyId);
            }

            var template = File.ReadAllText(templatePath);
            // Template değişkenlerini değiştir
            var r2BaseUrl = !string.IsNullOrEmpty(_r2Settings.PublicUrl)
                ? _r2Settings.PublicUrl.TrimEnd('/')
                : "https://pushonica-modules.your-account-id.r2.cloudflarestorage.com";

            var script = template
                .Replace("{{COMPANY_ID}}", companyId.ToString())
                .Replace("{{GENERATED_DATE}}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"))
                .Replace("{{R2_BASE_URL}}", r2BaseUrl);

            return script;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading standalone template file, using fallback script");
            return GenerateStandaloneFallbackScript(companyId);
        }
    }

    private string GenerateStandaloneFallbackScript(Guid companyId)
    {
        return $@"
/**
 * Standalone Product Slider - Fallback Script
 * Company ID: {companyId}
 * Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss} UTC
 */

(function() {{
    'use strict';

    console.log('[ProductSlider] Standalone Product Slider loaded for company: {companyId}');
    console.log('[ProductSlider] Template file not found, using fallback script');

    // Basit fallback implementasyonu
    window.productSlider = {{
        companyId: '{companyId}',
        initialized: true,
        sliders: [],
        log: function(message) {{
            console.log('[ProductSlider Fallback] ' + message);
        }}
    }};

}})();
";
    }

    public async Task SyncSliderItemsFromProductUpdateAsync(Guid companyId, string externalProductId)
    {
        var ecommerceService = await _ecommerceServiceFactory.GetEcommerceServiceAsync(companyId);
        if (ecommerceService == null)
            return;

        var product = await ecommerceService.GetProductAsync(companyId, externalProductId);
        if (product == null)
            return;

        var items = await _context.ProductSliderItems
            .Include(i => i.Slider)
            .Where(i => i.ExternalProductId == externalProductId && i.Slider.CompanyId == companyId)
            .ToListAsync();

        var newPrice = product.DiscountPrice ?? product.Price;
        var newDiscountPrice = product.DiscountPrice.HasValue ? product.Price : (decimal?)null;
        var newKDVAmount = product.KDVAmount;
        var newTaxRate = product.TaxRate;
        var newKdvIncluded = product.KdvIncluded;
        var newCurrency = product.Currency ?? "TRY";
        var newTitle = string.IsNullOrEmpty(product.Name) ? null : (product.Name.Length > 500 ? product.Name[..500] : product.Name);
        var newImage = string.IsNullOrEmpty(product.MainImage) ? null : (product.MainImage.Length > 1000 ? product.MainImage[..1000] : product.MainImage);
        var newUrl = string.IsNullOrEmpty(product.ProductUrl) ? null : (product.ProductUrl.Length > 1000 ? product.ProductUrl[..1000] : product.ProductUrl);

        var anyChange = false;
        foreach (var item in items)
        {
            var itemChanged = false;
            if (newPrice != item.ProductPrice) { item.ProductPrice = newPrice; itemChanged = true; }
            if (newDiscountPrice != item.DiscountPrice) { item.DiscountPrice = newDiscountPrice; itemChanged = true; }
            if (newKDVAmount != item.KDVAmount) { item.KDVAmount = newKDVAmount; itemChanged = true; }
            if (newTaxRate != item.TaxRate) { item.TaxRate = newTaxRate; itemChanged = true; }
            if (newCurrency != item.Currency) { item.Currency = newCurrency; itemChanged = true; }
            if (newTitle != null && newTitle != item.ProductTitle) { item.ProductTitle = newTitle; itemChanged = true; }
            if (newImage != null && newImage != item.ProductImage) { item.ProductImage = newImage; itemChanged = true; }
            if (newUrl != null && newUrl != item.ProductUrl) { item.ProductUrl = newUrl; itemChanged = true; }
            if (itemChanged)
            {
                item.UpdatedAt = DateTime.Now;
                anyChange = true;
            }
        }

        if (items.Count > 0 && anyChange)
        {
            _context.ProductSliderItems.UpdateRange(items);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated {Count} product slider item(s) from product update for company {CompanyId}, external product {ExternalProductId}",
                items.Count, companyId, externalProductId);
        }
    }

    public async Task<(bool Success, string Message, int UpdatedCount)> SyncSliderWithTicimaxAsync(Guid companyId, int sliderId)
    {
        var ecommerceService = await _ecommerceServiceFactory.GetEcommerceServiceAsync(companyId);
        if (ecommerceService == null)
            return (false, "Ticimax entegrasyonu bulunamadı.", 0);

        var slider = await _context.ProductSliders
            .Include(s => s.Items)
            .FirstOrDefaultAsync(s => s.Id == sliderId && s.CompanyId == companyId);
        if (slider == null)
            return (false, "Slider bulunamadı.", 0);

        var itemsToSync = slider.Items.Where(i => !string.IsNullOrEmpty(i.ExternalProductId)).ToList();
        if (itemsToSync.Count == 0)
            return (true, "Eşitlenecek Ticimax ürünü yok.", 0);

        int updatedCount = 0;
        foreach (var item in itemsToSync)
        {
            var product = await ecommerceService.GetProductAsync(companyId, item.ExternalProductId!);
            if (product == null) continue;

            var newPrice = product.DiscountPrice ?? product.Price;
            var newDiscountPrice = product.DiscountPrice.HasValue ? product.Price : (decimal?)null;
            var newKDVAmount = product.KDVAmount;
            var newTaxRate = product.TaxRate;
            var newKdvIncluded = product.KdvIncluded;
            var newCurrency = product.Currency ?? "TRY";
            var newTitle = string.IsNullOrEmpty(product.Name) ? null : (product.Name.Length > 500 ? product.Name[..500] : product.Name);
            var newImage = string.IsNullOrEmpty(product.MainImage) ? null : (product.MainImage.Length > 1000 ? product.MainImage[..1000] : product.MainImage);
            var newUrl = string.IsNullOrEmpty(product.ProductUrl) ? null : (product.ProductUrl.Length > 1000 ? product.ProductUrl[..1000] : product.ProductUrl);
            var newDesc = string.IsNullOrEmpty(product.ShortDescription) ? product.Description : product.ShortDescription;
            if (newDesc != null && newDesc.Length > 1000) newDesc = newDesc[..1000];

            var itemChanged = false;
            if (newKdvIncluded != item.KdvIncluded) { item.KdvIncluded = newKdvIncluded; itemChanged = true; }
            if (newPrice != item.ProductPrice) { item.ProductPrice = newPrice; itemChanged = true; }
            if (newDiscountPrice != item.DiscountPrice) { item.DiscountPrice = newDiscountPrice; itemChanged = true; }
            if (newTaxRate != item.TaxRate) { item.TaxRate = newTaxRate; itemChanged = true; }
            if (newCurrency != item.Currency) { item.Currency = newCurrency; itemChanged = true; }
            if (newTitle != null && newTitle != item.ProductTitle) { item.ProductTitle = newTitle; itemChanged = true; }
            if (newImage != null && newImage != item.ProductImage) { item.ProductImage = newImage; itemChanged = true; }
            if (newUrl != null && newUrl != item.ProductUrl) { item.ProductUrl = newUrl; itemChanged = true; }
            if (newDesc != null && newDesc != item.ProductDescription) { item.ProductDescription = newDesc; itemChanged = true; }
            if (itemChanged)
            {
                item.UpdatedAt = DateTime.Now;
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Ticimaxla eşitleme: slider {SliderId}, {UpdatedCount} ürün güncellendi.", sliderId, updatedCount);
        }

        return (true, updatedCount > 0 ? $"{updatedCount} ürün Ticimax verisiyle güncellendi." : "Tüm ürünler zaten güncel.", updatedCount);
    }
}
