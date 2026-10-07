using PushDashboard.DTOs;
using PushDashboard.Models;

namespace PushDashboard.Services;

public interface ITrendyolScraperService
{
    #region Store Management
    Task<(bool Success, string Message, int? StoreId)> CreateStoreAsync(Guid companyId, CreateStoreRequestDto request);
    Task<List<StoreResponseDto>> GetStoresAsync(Guid companyId);
    Task<(bool Success, string Message)> SyncStoreProductsAsync(Guid companyId, SyncStoreProductsRequestDto request);
    Task<bool> DeleteStoreAsync(Guid companyId, int storeId);
    #endregion

    #region Product Management
    Task<List<ProductResponseDto>> GetStoreProductsAsync(Guid companyId, int storeId, int page = 1, int pageSize = 50);
    Task<(bool Success, string Message, int? JobId)> TransferSelectedProductsAsync(Guid companyId, TransferSelectedProductsRequestDto request);
    Task<(int TotalProducts, int SelectedProducts)> GetProductStatsAsync(Guid companyId, int storeId);
    #endregion

    #region Transfer Job Management
    Task<List<TransferJobResponseDto>> GetTransferJobsAsync(Guid companyId, int page = 1, int pageSize = 20);
    Task<TransferJobResponseDto?> GetTransferJobAsync(Guid companyId, int jobId);
    Task<TransferJobDetailDto?> GetTransferJobWithProductsAsync(Guid companyId, int jobId);
    Task<(bool Success, string Message)> CancelTransferJobAsync(Guid companyId, int jobId);
    Task TriggerDailySyncAsync();
    #endregion

    #region Webhook Processing
    Task<bool> ProcessProductScrapeWebhookAsync(ProductScrapeWebhookDto webhook);
    Task<bool> ProcessCommentScrapeWebhookAsync(CommentScrapeWebhookDto webhook);
    #endregion

    #region External API Integration
    Task<(bool Success, string Message, string? JobId)> CallScrapeProductsApiAsync(string customerId, string storeUrl, string externalStoreId, int productCount, string webhookUrl);
    Task<(bool Success, string Message, string? JobId)> CallScrapeCommentsApiAsync(string customerId, List<string> productUrls, string externalBatchId, string webhookUrl, int commentCount = 100, List<int>? starRatings = null, DateTime? sinceDate = null);
    Task<JobStatusDto?> GetJobStatusAsync(string jobId);
    #endregion

    #region Reviews Integration
    Task<(bool Success, string Message)> ActivateTrendyolReviewsAsync(Guid companyId);
    Task<(bool Success, string Message)> DeactivateTrendyolReviewsAsync(Guid companyId);
    Task<bool> IsTrendyolReviewsActiveAsync(Guid companyId);
    #endregion

    #region Legacy Support
    Task<(bool Success, string Message)> CreateLegacyCommentRequestAsync(Guid companyId, string productUrl, int commentCount, string userId);
    #endregion
}
