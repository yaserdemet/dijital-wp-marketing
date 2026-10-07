using PushDashboard.DTOs;
using PushDashboard.Models;

namespace PushDashboard.Services;

public interface IProductSliderService
{
    #region Slider Management

    /// <summary>
    /// Şirketin tüm slider'larını getirir
    /// </summary>
    Task<List<ProductSliderResponseDto>> GetSlidersAsync(Guid companyId);

    /// <summary>
    /// Belirli bir slider'ı detaylarıyla getirir
    /// </summary>
    Task<ProductSliderDetailResponseDto?> GetSliderDetailAsync(Guid companyId, int sliderId);

    /// <summary>
    /// Yeni slider oluşturur
    /// </summary>
    Task<(bool Success, string Message, int? SliderId)> CreateSliderAsync(Guid companyId, string userId, CreateProductSliderRequestDto request);

    /// <summary>
    /// Slider'ı siler
    /// </summary>
    Task<(bool Success, string Message)> DeleteSliderAsync(Guid companyId, int sliderId);

    /// <summary>
    /// Slider'ın aktif/pasif durumunu değiştirir
    /// </summary>
    Task<(bool Success, string Message)> ToggleSliderStatusAsync(Guid companyId, int sliderId);

    #endregion

    #region Slider Items Management

    /// <summary>
    /// Slider'a yeni ürün ekler
    /// </summary>
    Task<(bool Success, string Message, int? ItemId)> AddSliderItemAsync(Guid companyId, CreateProductSliderItemRequestDto request);
    
    /// <summary>
    /// Slider ürününü siler
    /// </summary>
    Task<(bool Success, string Message)> DeleteSliderItemAsync(Guid companyId, int itemId);

    /// <summary>
    /// Slider ürünlerinin sırasını günceller
    /// </summary>
    Task<(bool Success, string Message)> UpdateItemsOrderAsync(Guid companyId, int sliderId, List<int> itemIds);

    /// <summary>
    /// Slider ürününün aktif/pasif durumunu değiştirir
    /// </summary>
    Task<(bool Success, string Message)> ToggleSliderItemStatusAsync(Guid companyId, int itemId);

    #endregion

    #region Slider Settings Management

    /// <summary>
    /// Slider ayarlarını getirir
    /// </summary>
    Task<ProductSliderSettingsResponseDto?> GetSliderSettingsAsync(Guid companyId, int sliderId);

    /// <summary>
    /// Slider ayarlarını günceller
    /// </summary>
    Task<(bool Success, string Message)> UpdateSliderSettingsAsync(Guid companyId, UpdateProductSliderSettingsRequestDto request);

    /// <summary>
    /// Slider ayarlarını varsayılana sıfırlar
    /// </summary>
    Task<(bool Success, string Message)> ResetSliderSettingsAsync(Guid companyId, int sliderId);

    #endregion

    #region Widget & Script Generation

    /// <summary>
    /// Widget için konfigürasyon verisini getirir
    /// </summary>
    Task<ProductSliderWidgetConfigDto?> GetWidgetConfigAsync(Guid companyId, int sliderId);



    #endregion

    #region R2 Export Methods

    /// <summary>
    /// Slider ayarlarını R2'ye JSON olarak export eder
    /// </summary>
    Task<string> ExportSliderToR2Async(int sliderId, Guid companyId);

    /// <summary>
    /// Company'nin tüm aktif slider'larını R2'ye export eder
    /// </summary>
    Task<string> ExportAllSlidersToR2Async(Guid companyId);


    #endregion

    #region Statistics & Analytics

    /// <summary>
    /// Şirketin slider istatistiklerini getirir
    /// </summary>
    Task<object> GetSliderStatsAsync(Guid companyId);

    /// <summary>
    /// Slider'ın performans verilerini getirir
    /// </summary>
    Task<object> GetSliderPerformanceAsync(Guid companyId, int sliderId);

    #endregion

    #region Validation & Helper Methods

    /// <summary>
    /// Slider'ın şirkete ait olup olmadığını kontrol eder
    /// </summary>
    Task<bool> IsSliderOwnedByCompanyAsync(Guid companyId, int sliderId);

    /// <summary>
    /// Slider item'ının şirkete ait olup olmadığını kontrol eder
    /// </summary>
    Task<bool> IsSliderItemOwnedByCompanyAsync(Guid companyId, int itemId);

    /// <summary>
    /// Slider adının benzersiz olup olmadığını kontrol eder
    /// </summary>
    Task<bool> IsSliderNameUniqueAsync(Guid companyId, string name, int? excludeSliderId = null);

    /// <summary>
    /// Standalone Product Slider script'ini oluşturur
    /// </summary>
    string GenerateStandaloneProductSliderScript(Guid companyId);

    /// <summary>
    /// Ürün güncellendiğinde bu ürünü referans alan slider item'ları e-ticaret verisiyle DB'de günceller
    /// </summary>
    Task SyncSliderItemsFromProductUpdateAsync(Guid companyId, string externalProductId);

    /// <summary>
    /// Slider'daki Ticimax kaynaklı tüm ürünleri Ticimax verisiyle karşılaştırır, değişen alanları DB'de günceller.
    /// </summary>
    Task<(bool Success, string Message, int UpdatedCount)> SyncSliderWithTicimaxAsync(Guid companyId, int sliderId);

    #endregion
}
