using Microsoft.EntityFrameworkCore;
using PushDashboard.Data;
using PushDashboard.Models;
using PushDashboard.ViewModels;

namespace PushDashboard.Services;

public interface IOrderStatusMappingService
{
    Task<List<OrderStatusMappingViewModel>> GetMappingsAsync(Guid companyId, string integrationType);
    Task<bool> SaveMappingsAsync(Guid companyId, string integrationType, List<OrderStatusMappingViewModel> mappings);
    Task<string> MapExternalToInternalStatusAsync(Guid companyId, string integrationType, string externalStatus);
    Task<List<ExternalOrderStatusViewModel>> GetExternalStatusesAsync(Guid companyId, string integrationType);
    Task<bool> CreateDefaultMappingsAsync(Guid companyId, string integrationType);
}

public class OrderStatusMappingService : IOrderStatusMappingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OrderStatusMappingService> _logger;

    public OrderStatusMappingService(
        ApplicationDbContext context,
        ILogger<OrderStatusMappingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<List<OrderStatusMappingViewModel>> GetMappingsAsync(Guid companyId, string integrationType)
    {
        var mappings = await _context.OrderStatusMappings
            .Where(m => m.CompanyId == companyId && m.IntegrationType == integrationType)
            .OrderBy(m => m.ExternalStatus)
            .Select(m => new OrderStatusMappingViewModel
            {
                Id = m.Id,
                IntegrationType = m.IntegrationType,
                ExternalStatus = m.ExternalStatus,
                ExternalStatusDisplayName = m.ExternalStatusDisplayName,
                InternalStatus = m.InternalStatus,
                IsActive = m.IsActive
            })
            .ToListAsync();

        return mappings;
    }

    public async Task<bool> SaveMappingsAsync(Guid companyId, string integrationType, List<OrderStatusMappingViewModel> mappings)
    {
        try
        {
            // Mevcut mapping'leri sil
            var existingMappings = await _context.OrderStatusMappings
                .Where(m => m.CompanyId == companyId && m.IntegrationType == integrationType)
                .ToListAsync();

            _context.OrderStatusMappings.RemoveRange(existingMappings);

            // Yeni mapping'leri ekle
            foreach (var mapping in mappings.Where(m => m.IsActive))
            {
                var newMapping = new OrderStatusMapping
                {
                    CompanyId = companyId,
                    IntegrationType = integrationType,
                    ExternalStatus = mapping.ExternalStatus,
                    ExternalStatusDisplayName = mapping.ExternalStatusDisplayName,
                    InternalStatus = mapping.InternalStatus,
                    IsActive = mapping.IsActive,
                    CreatedAt = DateTime.Now
                };

                _context.OrderStatusMappings.Add(newMapping);
            }

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving order status mappings for company {CompanyId}", companyId);
            return false;
        }
    }

    public async Task<string> MapExternalToInternalStatusAsync(Guid companyId, string integrationType, string externalStatus)
    {
        var mapping = await _context.OrderStatusMappings
            .FirstOrDefaultAsync(m => m.CompanyId == companyId 
                                   && m.IntegrationType == integrationType 
                                   && m.ExternalStatus == externalStatus 
                                   && m.IsActive);

        if (mapping != null)
        {
            return mapping.InternalStatus;
        }

        // Eğer mapping bulunamazsa varsayılan mapping'i kullan
        return GetDefaultMapping(integrationType, externalStatus);
    }

    public async Task<List<ExternalOrderStatusViewModel>> GetExternalStatusesAsync(Guid companyId, string integrationType)
    {
        switch (integrationType.ToLower())
        {
            case "ticimax":
                return GetTicimaxStatuses();
            default:
                return new List<ExternalOrderStatusViewModel>();
        }
    }

    public async Task<bool> CreateDefaultMappingsAsync(Guid companyId, string integrationType)
    {
        try
        {
            var externalStatuses = await GetExternalStatusesAsync(companyId, integrationType);
            var defaultMappings = new List<OrderStatusMappingViewModel>();

            // Her external status için varsayılan internal status'u belirle
            foreach (var externalStatus in externalStatuses)
            {
                var internalStatus = GetDefaultMapping(integrationType, externalStatus.StatusCode);

                defaultMappings.Add(new OrderStatusMappingViewModel
                {
                    IntegrationType = integrationType,
                    ExternalStatus = externalStatus.StatusCode,
                    ExternalStatusDisplayName = externalStatus.StatusName,
                    InternalStatus = internalStatus,
                    IsActive = true
                });
            }

            return await SaveMappingsAsync(companyId, integrationType, defaultMappings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating default mappings for company {CompanyId}", companyId);
            return false;
        }
    }

    private string GetDefaultMapping(string integrationType, string externalStatus)
    {
        switch (integrationType.ToLower())
        {
            case "ticimax":
                return GetDefaultTicimaxMapping(externalStatus);
            default:
                return "1"; // Beklemede
        }
    }

    private string GetDefaultTicimaxMapping(string externalStatus)
    {
        return externalStatus switch
        {
            "0" => "1",  // Yeni Sipariş -> Beklemede
            "1" => "1",  // Beklemede -> Beklemede
            "2" => "2",  // Onaylandı -> Onaylandı
            "3" => "3",  // Hazırlanıyor -> Hazırlanıyor
            "4" => "4",  // Kargoya Verildi -> Kargoya Verildi
            "5" => "5",  // Teslim Edildi -> Teslim Edildi
            "6" => "6",  // İptal Edildi -> İptal Edildi
            "7" => "7",  // İade Edildi -> İade Edildi
            "8" => "5",  // Kısmi Teslim -> Teslim Edildi
            "9" => "1",  // Ödeme Bekliyor -> Beklemede
            "10" => "1", // Stok Bekliyor -> Beklemede
            "11" => "1", // Tedarikçi Onayı Bekliyor -> Beklemede
            "12" => "3", // Kargo Hazırlığı -> Hazırlanıyor
            "13" => "4", // Kargoya Verildi -> Kargoya Verildi
            "14" => "5", // Teslim Edildi -> Teslim Edildi
            "15" => "6", // İptal Edildi -> İptal Edildi
            "16" => "7", // İade Edildi -> İade Edildi
            "17" => "1", // Ön Sipariş -> Beklemede
            "18" => "3", // Hazırlanıyor -> Hazırlanıyor
            "19" => "6", // İptal Talebi -> İptal Edildi
            "20" => "7", // İade Talebi -> İade Edildi
            "21" => "7", // İade Onaylandı -> İade Edildi
            "22" => "7", // İade Reddedildi -> İade Edildi
            "23" => "7", // İade Kargoda -> İade Edildi
            "24" => "7", // İade Tamamlandı -> İade Edildi
            "25" => "7", // Değişim Talebi -> İade Edildi
            "26" => "7", // Değişim Onaylandı -> İade Edildi
            "27" => "7", // Değişim Reddedildi -> İade Edildi
            "28" => "7", // Değişim Hazırlanıyor -> İade Edildi
            "29" => "4", // Değişim Kargoda -> Kargoya Verildi
            "30" => "5", // Değişim Tamamlandı -> Teslim Edildi
            _ => "1"     // Bilinmeyen durumlar için Beklemede
        };
    }

    private List<ExternalOrderStatusViewModel> GetTicimaxStatuses()
    {
        return new List<ExternalOrderStatusViewModel>
        {
            new() { StatusCode = "0", StatusName = "Sipariş Alındı", Description = "Yeni oluşturulan sipariş" },
            new() { StatusCode = "1", StatusName = "Onay Bekliyor", Description = "Sipariş Onay Bekliyor" },
            new() { StatusCode = "2", StatusName = "Onaylandı", Description = "Sipariş onaylandı" },
            new() { StatusCode = "3", StatusName = "Ödeme Bekliyor", Description = "Sipariş ödeme bekliyor" },
            new() { StatusCode = "4", StatusName = "Paketleniyor", Description = "Sipariş paketleniyor" },
            new() { StatusCode = "5", StatusName = "Tedarik Ediliyor", Description = "Sipariş tedarik ediliyor" },
            new() { StatusCode = "6", StatusName = "Kargoya Verildi", Description = "Sipariş kargoya verildi" },
            new() { StatusCode = "7", StatusName = "Teslim Edildi", Description = "Sipariş teslim edildi" },
            new() { StatusCode = "8", StatusName = "İptal Edildi", Description = "Sipariş iptal edildi" },
            new() { StatusCode = "9", StatusName = "İade Edildi", Description = "İade edildi" },
            new() { StatusCode = "10", StatusName = "Silinmiş", Description = "Silinmiş" },
            new() { StatusCode = "11", StatusName = "İade Talebi Alındı", Description = "İade talebi alındı" },
            new() { StatusCode = "12", StatusName = "İade Ulaştı Ödeme Yapılacak", Description = "İade ulaştı ödeme yapılacak" },
            new() { StatusCode = "13", StatusName = "İade Ödemesi Yapıldı", Description = "İade ödemesi yapıldı" },
            new() { StatusCode = "14", StatusName = "Teslimat Öncesi İptal Talebi", Description = "Teslimat öncesi iptal talebi alındı" },
            new() { StatusCode = "15", StatusName = "İptal Talebi", Description = "İptal talebi alındı" },
            new() { StatusCode = "16", StatusName = "Kısmi İade Talebi", Description = "Kısmi iade talebi alındı" },
            new() { StatusCode = "17", StatusName = "Kısmı İade Yapıldı", Description = "Kısmi iade yapıldı" },
            new() { StatusCode = "18", StatusName = "Teslim Edilemedi", Description = "Teslim edilemedi" },
            new() { StatusCode = "19", StatusName = "Mağazaya Gönderildi", Description = "Mağazaya gönderildi" },
            new() { StatusCode = "20", StatusName = "Mağazaya Ulaştı", Description = "Mağazaya ulaştı" },
            new() { StatusCode = "21", StatusName = "Mağazada Teslim Bekliyor", Description = "Mağazada teslim bekliyor" },
            new() { StatusCode = "22", StatusName = "Cüzdana İade", Description = "Cüzdana iade" },
        };
    }
}
