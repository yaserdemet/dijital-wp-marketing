using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PushDashboard.Configuration;
using PushDashboard.Data;
using PushDashboard.DTOs;
using PushDashboard.Models;
using System.Text.Json;

namespace PushDashboard.Services;

public class TrendyolScraperService : ITrendyolScraperService
{
    private readonly ApplicationDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<TrendyolScraperService> _logger;
    private readonly CommentScraperApiSettings _apiSettings;
    private readonly IConfiguration _configuration;
    private readonly IModuleUsageService _moduleUsageService;
    private readonly ICommentService _commentService;
    private readonly IR2StorageService _r2StorageService;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public TrendyolScraperService(
        ApplicationDbContext context,
        HttpClient httpClient,
        ILogger<TrendyolScraperService> logger,
        IOptions<CommentScraperApiSettings> apiSettings,
        IConfiguration configuration,
        IModuleUsageService moduleUsageService,
        ICommentService commentService,
        IR2StorageService r2StorageService,
        IWebHostEnvironment webHostEnvironment)
    {
        _context = context;
        _httpClient = httpClient;
        _logger = logger;
        _apiSettings = apiSettings.Value;
        _configuration = configuration;
        _moduleUsageService = moduleUsageService;
        _commentService = commentService;
        _r2StorageService = r2StorageService;
        _webHostEnvironment = webHostEnvironment;
    }

    #region Store Management

    public async Task<(bool Success, string Message, int? StoreId)> CreateStoreAsync(Guid companyId, CreateStoreRequestDto request)
    {
        try
        {
            // Validate store URL format
            if (!IsValidTrendyolStoreUrl(request.StoreUrl))
            {
                return (false, "Geçerli bir Trendyol mağaza URL'si giriniz. Format: https://www.trendyol.com/magaza/magaza-adi", null);
            }

            // Check if store already exists
            var existingStore = await _context.TrendyolStores
                .FirstOrDefaultAsync(ts => ts.CompanyId == companyId && ts.StoreUrl == request.StoreUrl);

            if (existingStore != null)
            {
                return (false, "Bu mağaza zaten eklenmiş.", null);
            }

            // Use provided store name or extract from URL
            var storeName = !string.IsNullOrWhiteSpace(request.StoreName)
                ? request.StoreName
                : ExtractStoreNameFromUrl(request.StoreUrl);

            var store = new TrendyolStore
            {
                CompanyId = companyId,
                StoreUrl = request.StoreUrl,
                ExternalStoreId = Guid.NewGuid().ToString(),
                StoreName = storeName,
                SyncStatus = "Pending"
            };

            _context.TrendyolStores.Add(store);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created new Trendyol store {StoreId} for company {CompanyId}", store.Id, companyId);

            return (true, "Mağaza başarıyla eklendi.", store.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Trendyol store for company {CompanyId}", companyId);
            return (false, "Mağaza eklenirken hata oluştu: " + ex.Message, null);
        }
    }

    public async Task<List<StoreResponseDto>> GetStoresAsync(Guid companyId)
    {
        try
        {
            var stores = await _context.TrendyolStores
                .Where(ts => ts.CompanyId == companyId)
                .OrderByDescending(ts => ts.CreatedAt)
                .ToListAsync();

            return stores.Select(ts => new StoreResponseDto
            {
                Id = ts.Id,
                StoreUrl = ts.StoreUrl,
                ExternalStoreId = ts.ExternalStoreId,
                StoreName = ts.StoreName,
                SyncStatus = ts.SyncStatus,
                StatusBadgeClass = ts.StatusBadgeClass,
                StatusIcon = ts.StatusIcon,
                LastSyncAt = ts.LastSyncAt,
                ProductCount = ts.ProductCount,
                ErrorMessage = ts.ErrorMessage,
                CreatedAt = ts.CreatedAt
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stores for company {CompanyId}", companyId);
            return new List<StoreResponseDto>();
        }
    }

    public async Task<(bool Success, string Message)> SyncStoreProductsAsync(Guid companyId, SyncStoreProductsRequestDto request)
    {
        try
        {
            var store = await _context.TrendyolStores
                .FirstOrDefaultAsync(ts => ts.Id == request.StoreId && ts.CompanyId == companyId);

            if (store == null)
            {
                return (false, "Mağaza bulunamadı.");
            }

            // Update store status
            store.SyncStatus = "Syncing";
            store.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            // Generate webhook URL
            var webhookUrl = GenerateWebhookUrl("product-scrape-completed");

            // Call external API
            var apiResult = await CallScrapeProductsApiAsync(
                companyId.ToString(),
                store.StoreUrl,
                store.ExternalStoreId!,
                request.ProductCount,
                webhookUrl);

            if (!apiResult.Success)
            {
                store.SyncStatus = "Failed";
                store.ErrorMessage = apiResult.Message;
                await _context.SaveChangesAsync();
                return (false, apiResult.Message);
            }

            // Create transfer job to track progress
            var job = new CommentTransferJob
            {
                CompanyId = companyId,
                StoreId = store.Id,
                JobId = apiResult.JobId!,
                JobType = "Products",
                Status = "Running",
                TotalProducts = request.ProductCount
            };

            _context.CommentTransferJobs.Add(job);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Started product sync for store {StoreId} with job {JobId}", store.Id, apiResult.JobId);

            return (true, "Ürün senkronizasyonu başlatıldı.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing products for store {StoreId}", request.StoreId);
            return (false, "Senkronizasyon başlatılırken hata oluştu: " + ex.Message);
        }
    }

    public async Task<bool> DeleteStoreAsync(Guid companyId, int storeId)
    {
        try
        {
            var store = await _context.TrendyolStores
                .FirstOrDefaultAsync(ts => ts.Id == storeId && ts.CompanyId == companyId);

            if (store == null)
            {
                return false;
            }

            _context.TrendyolStores.Remove(store);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted Trendyol store {StoreId} for company {CompanyId}", storeId, companyId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting store {StoreId} for company {CompanyId}", storeId, companyId);
            return false;
        }
    }

    #endregion

    #region Product Management

    public async Task<List<ProductResponseDto>> GetStoreProductsAsync(Guid companyId, int storeId, int page = 1, int pageSize = 50)
    {
        try
        {
            var query = _context.TrendyolProducts
                .Where(tp => tp.Store.CompanyId == companyId && tp.StoreId == storeId);

            var products = await query
                .OrderByDescending(tp => tp.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(tp => new ProductResponseDto
                {
                    Id = tp.Id,
                    Title = tp.Title,
                    ShortTitle = tp.ShortTitle,
                    ProductId = tp.ProductId,
                    Href = tp.Href,
                    FullUrl = tp.FullUrl,
                    IsSelected = tp.IsSelected,
                    CreatedAt = tp.CreatedAt
                })
                .ToListAsync();

            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products for store {StoreId}", storeId);
            return new List<ProductResponseDto>();
        }
    }

    public async Task<(bool Success, string Message, int? JobId)> TransferSelectedProductsAsync(Guid companyId, TransferSelectedProductsRequestDto request)
    {
        try
        {
            var store = await _context.TrendyolStores
                .FirstOrDefaultAsync(ts => ts.Id == request.StoreId && ts.CompanyId == companyId);

            if (store == null)
            {
                return (false, "Mağaza bulunamadı.", null);
            }

            var products = await _context.TrendyolProducts
                .Where(tp => request.ProductIds.Contains(tp.Id) && tp.StoreId == request.StoreId)
                .ToListAsync();

            if (!products.Any())
            {
                return (false, "Seçilen ürünler bulunamadı.", null);
            }

            // Quota Check
            var commentModule = await _context.Modules.FirstOrDefaultAsync(m => m.Name == "Yorum Taşıma");
            if (commentModule == null)
                return (false, "Yorum Taşıma modülü sistemde bulunamadı.", null);

            var quota = await _context.CompanyModuleQuotas
                .FirstOrDefaultAsync(q => q.CompanyId == companyId && q.ModuleId == commentModule.Id);

            int totalRequestedComments = request.CommentCount * products.Count;

            if (quota == null || quota.RemainingQuota < totalRequestedComments)
            {
                return (false, $"Yetersiz kota. İstenen Yorum: {totalRequestedComments}, Kalan Kotanız: {Math.Floor(quota?.RemainingQuota ?? 0)}", null);
            }

            // Prepare product URLs for API call
            var productUrls = products.Select(p => p.FullUrl).ToList();

            // Generate webhook URL
            var webhookUrl = GenerateWebhookUrl("comment-scrape-completed");

            // Generate external batch ID
            var externalBatchId = request.ExternalBatchId ?? Guid.NewGuid().ToString();

            // Call external API
            var apiResult = await CallScrapeCommentsApiAsync(
                companyId.ToString(),
                productUrls,
                externalBatchId,
                webhookUrl,
                request.CommentCount,
                request.StarRatings);

            if (!apiResult.Success)
            {
                return (false, apiResult.Message, null);
            }

            // Create transfer job
            var job = new CommentTransferJob
            {
                CompanyId = companyId,
                StoreId = store.Id,
                JobId = apiResult.JobId!,
                ExternalBatchId = externalBatchId,
                JobType = "Comments",
                Status = "Running",
                TotalProducts = products.Count,
                IsAutoTransferEnabled = request.AutoSync,
                LastAutoTransferDate = request.AutoSync ? DateTime.Now : null,
                TransferSettings = JsonSerializer.Serialize(request)
            };

            _context.CommentTransferJobs.Add(job);
            await _context.SaveChangesAsync();

            // Create job products
            var jobProducts = products.Select(p => new CommentTransferJobProduct
            {
                JobId = job.Id,
                ProductId = p.Id,
                ProductUrl = p.FullUrl,
                Status = "Pending"
            }).ToList();

            _context.CommentTransferJobProducts.AddRange(jobProducts);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Started comment transfer for {ProductCount} products with job {JobId}",
                products.Count, apiResult.JobId);

            return (true, "Yorum transfer işlemi başlatıldı.", job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring selected products for store {StoreId}", request.StoreId);
            return (false, "Transfer işlemi başlatılırken hata oluştu: " + ex.Message, null);
        }
    }

    public async Task<(int TotalProducts, int SelectedProducts)> GetProductStatsAsync(Guid companyId, int storeId)
    {
        try
        {
            var query = _context.TrendyolProducts
                .Where(tp => tp.Store.CompanyId == companyId && tp.StoreId == storeId);

            var totalProducts = await query.CountAsync();
            var selectedProducts = await query.CountAsync(tp => tp.IsSelected);

            return (totalProducts, selectedProducts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product stats for store {StoreId}", storeId);
            return (0, 0);
        }
    }

    #endregion

    #region Transfer Job Management

    public async Task<List<TransferJobResponseDto>> GetTransferJobsAsync(Guid companyId, int page = 1, int pageSize = 20)
    {
        try
        {
            var query = _context.CommentTransferJobs
                .Where(ctj => ctj.CompanyId == companyId)
                .Include(ctj => ctj.Store);

            var jobs = await query
                .OrderByDescending(ctj => ctj.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ctj => new TransferJobResponseDto
                {
                    Id = ctj.Id,
                    JobId = ctj.JobId,
                    ExternalBatchId = ctj.ExternalBatchId,
                    JobType = ctj.JobType,
                    JobTypeDisplay = ctj.JobTypeDisplay,
                    Status = ctj.Status,
                    StatusBadgeClass = ctj.StatusBadgeClass,
                    StatusIcon = ctj.StatusIcon,
                    ProgressPercent = ctj.ProgressPercent,
                    CurrentStep = ctj.CurrentStep,
                    TotalProducts = ctj.TotalProducts,
                    ProcessedProducts = ctj.ProcessedProducts,
                    TotalComments = ctj.TotalComments,
                    ResultFileUrl = ctj.ResultFileUrl,
                    LogsFileUrl = ctj.LogsFileUrl,
                    ErrorMessage = ctj.ErrorMessage,
                    CreatedAt = ctj.CreatedAt,
                    StartedAt = ctj.StartedAt,
                    CompletedAt = ctj.CompletedAt,
                    StoreName = ctj.Store != null ? ctj.Store.StoreName : null,
                    IsAutoTransferEnabled = ctj.IsAutoTransferEnabled
                })
                .ToListAsync();

            return jobs;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transfer jobs for company {CompanyId}", companyId);
            return new List<TransferJobResponseDto>();
        }
    }

    public async Task<TransferJobResponseDto?> GetTransferJobAsync(Guid companyId, int jobId)
    {
        try
        {
            var job = await _context.CommentTransferJobs
                .Include(ctj => ctj.Store)
                .Where(ctj => ctj.CompanyId == companyId && ctj.Id == jobId)
                .Select(ctj => new TransferJobResponseDto
                {
                    Id = ctj.Id,
                    JobId = ctj.JobId,
                    ExternalBatchId = ctj.ExternalBatchId,
                    JobType = ctj.JobType,
                    JobTypeDisplay = ctj.JobTypeDisplay,
                    Status = ctj.Status,
                    StatusBadgeClass = ctj.StatusBadgeClass,
                    StatusIcon = ctj.StatusIcon,
                    ProgressPercent = ctj.ProgressPercent,
                    CurrentStep = ctj.CurrentStep,
                    TotalProducts = ctj.TotalProducts,
                    ProcessedProducts = ctj.ProcessedProducts,
                    TotalComments = ctj.TotalComments,
                    ResultFileUrl = ctj.ResultFileUrl,
                    LogsFileUrl = ctj.LogsFileUrl,
                    ErrorMessage = ctj.ErrorMessage,
                    CreatedAt = ctj.CreatedAt,
                    StartedAt = ctj.StartedAt,
                    CompletedAt = ctj.CompletedAt,
                    StoreName = ctj.Store != null ? ctj.Store.StoreName : null
                })
                .FirstOrDefaultAsync();

            return job;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transfer job {JobId} for company {CompanyId}", jobId, companyId);
            return null;
        }
    }

    public async Task<(bool Success, string Message)> CancelTransferJobAsync(Guid companyId, int jobId)
    {
        try
        {
            var job = await _context.CommentTransferJobs
                .FirstOrDefaultAsync(ctj => ctj.Id == jobId && ctj.CompanyId == companyId);

            if (job == null)
            {
                return (false, "Transfer işi bulunamadı.");
            }

            if (job.Status == "Completed" || job.Status == "Failed")
            {
                return (false, "Tamamlanmış veya başarısız olan işler iptal edilemez.");
            }

            job.Status = "Failed";
            job.ErrorMessage = "Kullanıcı tarafından iptal edildi.";
            job.CompletedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Cancelled transfer job {JobId} for company {CompanyId}", jobId, companyId);

            return (true, "Transfer işi iptal edildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling transfer job {JobId} for company {CompanyId}", jobId, companyId);
            return (false, "İptal işlemi sırasında hata oluştu: " + ex.Message);
        }
    }

    public async Task<TransferJobDetailDto?> GetTransferJobWithProductsAsync(Guid companyId, int jobId)
    {
        try
        {
            var job = await _context.CommentTransferJobs
                .Include(ctj => ctj.Store)
                .Include(ctj => ctj.JobProducts)
                    .ThenInclude(jp => jp.Product)
                .Where(ctj => ctj.CompanyId == companyId && ctj.Id == jobId)
                .FirstOrDefaultAsync();

            if (job == null)
            {
                return null;
            }

            return new TransferJobDetailDto
            {
                Id = job.Id,
                JobId = job.JobId,
                ExternalBatchId = job.ExternalBatchId,
                JobType = job.JobType,
                JobTypeDisplay = job.JobTypeDisplay,
                Status = job.Status,
                StatusBadgeClass = job.StatusBadgeClass,
                TotalProducts = job.TotalProducts,
                ProcessedProducts = job.ProcessedProducts,
                TotalComments = job.TotalComments,
                ErrorMessage = job.ErrorMessage,
                CreatedAt = job.CreatedAt,
                CompletedAt = job.CompletedAt,
                StoreName = job.Store?.StoreName,
                Products = job.JobProducts.Select(jp => new TransferJobProductDto
                {
                    Id = jp.Id,
                    ProductUrl = jp.ProductUrl,
                    ProductTitle = jp.Product?.Title ?? ExtractProductNameFromUrl(jp.ProductUrl),
                    Status = jp.Status,
                    StatusBadgeClass = jp.StatusBadgeClass,
                    CommentsCount = jp.CommentsCount,
                    CommentsFileUrl = jp.CommentsFileUrl,
                    ErrorMessage = jp.ErrorMessage,
                    ProcessedAt = jp.ProcessedAt
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transfer job with products {JobId} for company {CompanyId}", jobId, companyId);
            return null;
        }
    }

    private string ExtractProductNameFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var lastSegment = segments.LastOrDefault();
            if (lastSegment != null && lastSegment.Contains("-p-"))
            {
                return lastSegment.Split("-p-")[0].Replace("-", " ");
            }
            return lastSegment ?? "Ürün";
        }
        catch
        {
            return "Ürün";
        }
    }

    public async Task TriggerDailySyncAsync()
    {
        try
        {
            var yesterday = DateTime.Now.AddDays(-1); // Only check 24 hours ago

            // Find eligible jobs: AutoSync enabled AND Not run in last 24 hours
            var eligibleJobs = await _context.CommentTransferJobs
                .Where(j => j.IsAutoTransferEnabled &&
                           (j.LastAutoTransferDate == null || j.LastAutoTransferDate < yesterday))
                .ToListAsync();

            if (!eligibleJobs.Any())
            {
                return;
            }

            foreach (var job in eligibleJobs)
            {
                try
                {
                    // Update LastAutoTransferDate immediately to prevent duplicate runs
                    job.LastAutoTransferDate = DateTime.Now;
                    await _context.SaveChangesAsync();

                    // Parse settings
                    TransferSelectedProductsRequestDto? settings = null;
                    if (!string.IsNullOrEmpty(job.TransferSettings))
                    {
                        try
                        {
                            settings = JsonSerializer.Deserialize<TransferSelectedProductsRequestDto>(job.TransferSettings);
                        }
                        catch
                        {
                            // If settings fail to parse, we can't do much. 
                            // Could log error or fallback to existing JobProducts if we assume they are still valid.
                            // For now, let's log and continue to next job.
                            _logger.LogError("Failed to deserialize transfer settings for Job {JobId}", job.Id);
                            continue;
                        }
                    }

                    if (settings == null) continue;

                    // Re-fetch product URLs if possible, or use IDs from settings
                    // NOTE: Settings has ProductIds. We need to fetch URLs again in case they changed, 
                    // or just use the IDs to get current URLs from DB.
                    var products = await _context.TrendyolProducts
                        .Where(tp => settings.ProductIds.Contains(tp.Id) && tp.StoreId == job.StoreId)
                        .ToListAsync();

                    if (!products.Any()) continue;

                    var productUrls = products.Select(p => p.FullUrl).ToList();
                    var webhookUrl = GenerateWebhookUrl("comment-scrape-completed");
                    var externalBatchId = Guid.NewGuid().ToString();
                    var sinceDate = DateTime.Now.AddDays(-1); // Sync yesterday's data

                    // Call API
                    var apiResult = await CallScrapeCommentsApiAsync(
                        job.CompanyId.ToString(),
                        productUrls,
                        externalBatchId,
                        webhookUrl,
                        settings.CommentCount,
                        settings.StarRatings,
                        sinceDate
                    );

                    if (apiResult.Success)
                    {
                        // Create NEW Job record for this run
                        var newJob = new CommentTransferJob
                        {
                            CompanyId = job.CompanyId,
                            StoreId = job.StoreId,
                            JobId = apiResult.JobId!,
                            ExternalBatchId = externalBatchId,
                            JobType = "Comments",
                            Status = "Running",
                            TotalProducts = products.Count,
                            IsAutoTransferEnabled = false, // The child job is NOT auto-enabled
                            TransferSettings = null // No need to copy settings to child
                        };

                        _context.CommentTransferJobs.Add(newJob);
                        await _context.SaveChangesAsync();

                        // Create job products
                        var jobProducts = products.Select(p => new CommentTransferJobProduct
                        {
                            JobId = newJob.Id,
                            ProductId = p.Id,
                            ProductUrl = p.FullUrl,
                            Status = "Pending"
                        }).ToList();

                        _context.CommentTransferJobProducts.AddRange(jobProducts);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("Auto-Sync triggered for Job {ParentJobId}. New Job: {NewJobId}", job.Id, newJob.Id);
                    }
                    else
                    {
                        _logger.LogError("Auto-Sync API call failed for Job {JobId}: {Message}", job.Id, apiResult.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing Auto-Sync for Job {JobId}", job.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in TriggerDailySyncAsync");
        }
    }

    #endregion

    #region External API Integration

    public async Task<(bool Success, string Message, string? JobId)> CallScrapeProductsApiAsync(string customerId, string storeUrl, string externalStoreId, int productCount, string webhookUrl)
    {
        try
        {
            var request = new ScrapeProductsRequestDto
            {
                customer_id = customerId,
                store_url = storeUrl,
                external_store_id = externalStoreId,
                product_count = productCount,
                webhook_url = webhookUrl
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_apiSettings.BaseUrl}/scrape-products", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = JsonSerializer.Deserialize<ScraperApiResponseDto>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (apiResponse != null && apiResponse.status == "accepted")
                {
                    return (true, apiResponse.message, apiResponse.job_id);
                }
            }

            _logger.LogWarning("Scrape products API call failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
            return (false, "API çağrısı başarısız oldu.", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling scrape products API");
            return (false, "API çağrısı sırasında hata oluştu: " + ex.Message, null);
        }
    }

    public async Task<(bool Success, string Message, string? JobId)> CallScrapeCommentsApiAsync(string customerId, List<string> productUrls, string externalBatchId, string webhookUrl, int commentCount = 100, List<int>? starRatings = null, DateTime? sinceDate = null)
    {
        try
        {
            var request = new ScrapeCommentsRequestDto
            {
                customer_id = customerId,
                product_urls = productUrls,
                external_batch_id = externalBatchId,
                webhook_url = webhookUrl,
                comment_count = commentCount,
                star_ratings = starRatings?.Any() == true ? starRatings : null,
                since_date = sinceDate?.ToString("yyyy-MM-dd")
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_apiSettings.BaseUrl}/scrape-comments", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var apiResponse = JsonSerializer.Deserialize<ScraperApiResponseDto>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (apiResponse != null && apiResponse.status == "accepted")
                {
                    return (true, apiResponse.message, apiResponse.job_id);
                }
            }

            _logger.LogWarning("Scrape comments API call failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
            return (false, "API çağrısı başarısız oldu.", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling scrape comments API");
            return (false, "API çağrısı sırasında hata oluştu: " + ex.Message, null);
        }
    }

    public async Task<JobStatusDto?> GetJobStatusAsync(string jobId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_apiSettings.BaseUrl}/job/{jobId}");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var jobStatus = JsonSerializer.Deserialize<JobStatusDto>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return jobStatus;
            }

            _logger.LogWarning("Get job status API call failed: {StatusCode} - {Content}", response.StatusCode, responseContent);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job status for job {JobId}", jobId);
            return null;
        }
    }

    #endregion

    #region Webhook Processing

    public async Task<bool> ProcessProductScrapeWebhookAsync(ProductScrapeWebhookDto webhook)
    {
        try
        {
            var job = await _context.CommentTransferJobs
                .Include(ctj => ctj.Store)
                .FirstOrDefaultAsync(ctj => ctj.JobId == webhook.job_id);

            if (job == null)
            {
                _logger.LogWarning("Transfer job not found for webhook job_id: {JobId}", webhook.job_id);
                return false;
            }

            // Update job status
            job.Status = webhook.status == "completed" ? "Completed" : "Failed";
            job.CompletedAt = DateTime.Now;

            if (webhook.result != null)
            {
                if (webhook.result.status == "success")
                {
                    job.ResultFileUrl = webhook.result.products_file_url;
                    job.LogsFileUrl = webhook.result.logs_file_url;
                    job.ProcessedProducts = webhook.result.products_count;

                    // Update store
                    if (job.Store != null)
                    {
                        job.Store.SyncStatus = "Completed";
                        job.Store.LastSyncAt = DateTime.Now;
                        job.Store.ProductCount = webhook.result.products_count;
                        job.Store.ErrorMessage = null;
                    }

                    // Download and process products
                    await ProcessProductsFileAsync(job.Store!, webhook.result.products_file_url!);
                }
                else
                {
                    job.ErrorMessage = "Ürün çekme işlemi başarısız oldu.";
                    if (job.Store != null)
                    {
                        job.Store.SyncStatus = "Failed";
                        job.Store.ErrorMessage = "Ürün çekme işlemi başarısız oldu.";
                    }
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Processed product scrape webhook for job {JobId} with status {Status}",
                webhook.job_id, webhook.status);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing product scrape webhook for job {JobId}", webhook.job_id);
            return false;
        }
    }

    public async Task<bool> ProcessCommentScrapeWebhookAsync(CommentScrapeWebhookDto webhook)
    {
        try
        {
            var job = await _context.CommentTransferJobs
                .Include(ctj => ctj.JobProducts)
                .FirstOrDefaultAsync(ctj => ctj.JobId == webhook.job_id);

            if (job == null)
            {
                _logger.LogWarning("Transfer job not found for webhook job_id: {JobId}", webhook.job_id);
                return false;
            }

            // Update job status
            job.Status = webhook.status == "completed" ? "Completed" : "Failed";
            job.CompletedAt = DateTime.Now;

            if (webhook.result != null)
            {
                job.ResultFileUrl = webhook.result.comments_file_url;
                job.LogsFileUrl = webhook.result.logs_file_url;
                job.TotalComments = webhook.result.comments_count;
                job.ProcessedProducts = webhook.result.processed_urls;

                // Process batch_manifest to update individual job products
                if (webhook.result.batch_manifest != null && webhook.result.batch_manifest.Any())
                {
                    foreach (var manifestItem in webhook.result.batch_manifest)
                    {
                        var manifestUrl = NormalizeUrlForComparison(manifestItem.product_url);
                        var productUrl = manifestItem.product_url ?? string.Empty;

                        CommentTransferJobProduct? jobProduct = null;

                        // Try to find matching job product
                        if (!string.IsNullOrEmpty(manifestUrl))
                        {
                            jobProduct = job.JobProducts.FirstOrDefault(jp =>
                                NormalizeUrlForComparison(jp.ProductUrl) == manifestUrl);
                        }

                        if (jobProduct != null)
                        {
                            // Update existing product
                            bool wasCompleted = jobProduct.Status == "Completed";

                            jobProduct.CommentsFileUrl = manifestItem.file_url;
                            jobProduct.CommentsCount = manifestItem.comments_count;
                            jobProduct.Status = "Completed";
                            jobProduct.ProcessedAt = DateTime.Now;

                            _logger.LogInformation(
                                "Updated job product {ProductId} with {CommentsCount} comments, file: {FileUrl}",
                                jobProduct.ProductId ?? 0,
                                manifestItem.comments_count,
                                manifestItem.file_url);

                            // Only deduct if not already counted as completed
                            if (!wasCompleted && manifestItem.comments_count > 0)
                            {
                                await DeductBalanceForCommentsAsync(
                                    job.CompanyId,
                                    manifestItem.comments_count,
                                    jobProduct.Id,
                                    jobProduct.ProductUrl);
                            }
                        }
                        else
                        {
                            // Create new job product
                            var newJobProduct = new CommentTransferJobProduct
                            {
                                JobId = job.Id,
                                ProductId = null,
                                ProductUrl = productUrl,
                                CommentsFileUrl = manifestItem.file_url,
                                CommentsCount = manifestItem.comments_count,
                                Status = "Completed",
                                CreatedAt = DateTime.Now,
                                ProcessedAt = DateTime.Now
                            };

                            _context.CommentTransferJobProducts.Add(newJobProduct);
                            await _context.SaveChangesAsync(); // Save to generate Id

                            _logger.LogInformation(
                                "Created job product for {ProductId} with {CommentsCount} comments, file: {FileUrl}",
                                manifestItem.product_id,
                                manifestItem.comments_count,
                                manifestItem.file_url);

                            if (manifestItem.comments_count > 0)
                            {
                                await DeductBalanceForCommentsAsync(
                                    job.CompanyId,
                                    manifestItem.comments_count,
                                    newJobProduct.Id,
                                    productUrl);
                            }
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Processed comment scrape webhook for job {JobId} with status {Status}",
                webhook.job_id, webhook.status);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing comment scrape webhook for job {JobId}", webhook.job_id);
            return false;
        }
    }

    #endregion

    #region Legacy Support

    public async Task<(bool Success, string Message)> CreateLegacyCommentRequestAsync(Guid companyId, string productUrl, int commentCount, string userId)
    {
        try
        {
            // Ensure URL has proper format
            var fullUrl = productUrl.StartsWith("http") ? productUrl : $"https://www.trendyol.com{productUrl}";

            // Generate webhook URL
            var webhookUrl = GenerateWebhookUrl("comment-completed");

            // Call external API with single product
            var apiResult = await CallScrapeCommentsApiAsync(
                companyId.ToString(),
                new List<string> { fullUrl },
                Guid.NewGuid().ToString(),
                webhookUrl,
                commentCount);

            if (!apiResult.Success)
            {
                return (false, apiResult.Message);
            }

            // Create legacy comment request for backward compatibility
            var commentRequest = new CommentRequest
            {
                CompanyId = companyId,
                ProductUrl = fullUrl,
                ExternalProductId = ExtractProductIdFromUrl(fullUrl),
                RequestedCommentsCount = commentCount,
                Status = "İşleniyor",
                ExternalRequestId = apiResult.JobId,
                WebhookUrl = webhookUrl
            };

            _context.CommentRequests.Add(commentRequest);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created legacy comment request {RequestId} for product {ProductUrl}",
                commentRequest.Id, productUrl);

            return (true, "Yorum isteği başarıyla oluşturuldu.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating legacy comment request for product {ProductUrl}", productUrl);
            return (false, "Yorum isteği oluşturulurken hata oluştu: " + ex.Message);
        }
    }

    #endregion

    #region Helper Methods

    private bool IsValidTrendyolStoreUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            return uri.Host.Contains("trendyol.com");
        }
        catch
        {
            return false;
        }
    }

    private string ExtractStoreNameFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length >= 2 && segments[0] == "magaza")
            {
                return segments[1];
            }
            return "Unknown Store";
        }
        catch
        {
            return "Unknown Store";
        }
    }

    private string GenerateWebhookUrl(string endpoint)
    {
        var baseUrl = _configuration["BaseUrl"] ?? "https://localhost:7001";
        return $"{baseUrl}/webhook/{endpoint}";
    }

    private string ExtractProductIdFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Trendyol URL format: /product-name-p-123456
            var lastSegment = segments.LastOrDefault();
            if (lastSegment != null && lastSegment.Contains("-p-"))
            {
                var parts = lastSegment.Split("-p-");
                if (parts.Length > 1)
                {
                    return parts[1];
                }
            }

            return Guid.NewGuid().ToString();
        }
        catch
        {
            return Guid.NewGuid().ToString();
        }
    }

    private async Task ProcessProductsFileAsync(TrendyolStore store, string fileUrl)
    {
        try
        {
            var response = await _httpClient.GetAsync(fileUrl);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to download products file from {FileUrl}", fileUrl);
                return;
            }

            var jsonContent = await response.Content.ReadAsStringAsync();
            var products = JsonSerializer.Deserialize<List<ScrapedProductDto>>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (products == null || !products.Any())
            {
                _logger.LogWarning("No products found in file {FileUrl}", fileUrl);
                return;
            }

            var existingProducts = await _context.TrendyolProducts
                .Where(tp => tp.StoreId == store.Id)
                .ToListAsync();

            _context.TrendyolProducts.RemoveRange(existingProducts);
            await _context.SaveChangesAsync();

            var newProducts = products
                .GroupBy(p => p.id)
                .Select(g => g.First())
                .Select(p => new TrendyolProduct
                {
                    StoreId = store.Id,
                    Title = p.title,
                    ProductId = p.id,
                    Href = p.href,
                    IsSelected = false
                }).ToList();

            _context.TrendyolProducts.AddRange(newProducts);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Processed {ProductCount} products for store {StoreId}",
                newProducts.Count, store.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing products file {FileUrl} for store {StoreId}",
                fileUrl, store.Id);
        }
    }

    #endregion

    #region URL Helpers

    private static string NormalizeUrlForComparison(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        // Remove query parameters
        var queryIndex = url.IndexOf('?');
        if (queryIndex > 0)
            url = url[..queryIndex];

        // Remove trailing slashes
        url = url.TrimEnd('/');

        // Convert to lowercase for case-insensitive comparison
        return url.ToLowerInvariant();
    }

    #endregion

    #region Balance Helpers

    private async Task DeductBalanceForCommentsAsync(Guid companyId, int commentCount, int jobProductId, string productUrl)
    {
        try
        {
            if (commentCount <= 0)
            {
                _logger.LogInformation("No quota to deduct for job product {JobProductId} (count: {Count})",
                    jobProductId, commentCount);
                return;
            }

            // "Yorum Taşıma" modülünün ID'sini al
            var commentModule = await _context.Modules.FirstOrDefaultAsync(m => m.Name == "Yorum Taşıma");
            if (commentModule == null)
            {
                _logger.LogWarning("Comment module 'Yorum Taşıma' not found in database. Skipping quota deduction.");
                return;
            }

            // Kredi düşmek yerine Modül Kotasından düş (Optimistic Concurrency eklenebilir)
            var quota = await _context.CompanyModuleQuotas
                .FirstOrDefaultAsync(q => q.CompanyId == companyId && q.ModuleId == commentModule.Id);

            if (quota != null)
            {
                quota.RemainingQuota -= commentCount;
                if (quota.RemainingQuota < 0) quota.RemainingQuota = 0; // Prevent negative quota just in case

                _context.CompanyModuleQuotas.Update(quota);
                await _context.SaveChangesAsync();
            }
            else
            {
                _logger.LogWarning("No quota profile found for Company {CompanyId} when trying to deduct {Count} comments.", companyId, commentCount);
                return; // Do nothing if there's no quote profile
            }

            // Bakiye düşümü ve harcama kaydı oluştur
            var result = await _moduleUsageService.DeductUsageCostAsync(
                companyId,
                commentModule.Id,
                "Yorum Taşıma",
                0, // Fiyat artık 0 çünkü kota sistemine geçtik
                $"Trendyol'dan {commentCount} yorum kotadan düşülerek taşındı. İşlem Id: {jobProductId} -- URL: {TruncateUrl(productUrl, 300)}",
                "system", // Webhook otomatik işlem
                jobProductId.ToString(),
                "trendyol"
            );

            if (result.Success)
            {
                _logger.LogInformation(
                    "Deducted quota from company {CompanyId} for {CommentCount} comments (JobProduct: {JobProductId})",
                    companyId, commentCount, jobProductId);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to deduct balance for job product {JobProductId}: {Message}",
                    jobProductId, result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deducting balance for job product {JobProductId}", jobProductId);
            // Bakiye düşümü başarısız olsa bile webhook işlemi devam etsin
        }
    }

    private static string TruncateUrl(string url, int maxLength)
    {
        if (string.IsNullOrEmpty(url) || url.Length <= maxLength)
            return url;

        return url[..maxLength] + "...";
    }

    #endregion

    #region Reviews Integration

    public async Task<(bool Success, string Message)> ActivateTrendyolReviewsAsync(Guid companyId)
    {
        try
        {
            _logger.LogInformation("Activating Trendyol Reviews integration for company {CompanyId}", companyId);

            // wwwroot/js/trendyol-reviews.js dosyasını oku
            var jsFilePath = Path.Combine(_webHostEnvironment.WebRootPath, "js", "trendyol-reviews.js");
            if (!File.Exists(jsFilePath))
            {
                _logger.LogWarning("trendyol-reviews.js not found at {Path}", jsFilePath);
                return (false, "trendyol-reviews.js dosyası bulunamadı.");
            }

            var jsContent = await File.ReadAllTextAsync(jsFilePath);

            // R2'ye {companyId}/trendyolReviews.js olarak yükle
            var jsKey = $"{companyId}/trendyolReviews.js";
            var jsUrl = await _r2StorageService.UploadJavaScriptAsync(jsKey, jsContent);

            _logger.LogInformation("Successfully activated Trendyol Reviews for company {CompanyId}. URL: {Url}", companyId, jsUrl);

            return (true, "Trendyol Yorum entegrasyonu başarıyla aktif edildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating Trendyol Reviews for company {CompanyId}", companyId);
            return (false, $"Entegrasyon aktif edilirken hata oluştu: {ex.Message}");
        }
    }

    public async Task<(bool Success, string Message)> DeactivateTrendyolReviewsAsync(Guid companyId)
    {
        try
        {
            _logger.LogInformation("Deactivating Trendyol Reviews integration for company {CompanyId}", companyId);

            var jsKey = $"{companyId}/trendyolReviews.js";
            await _r2StorageService.DeleteFileAsync(jsKey);

            _logger.LogInformation("Successfully deactivated Trendyol Reviews for company {CompanyId}", companyId);

            return (true, "Trendyol Yorum entegrasyonu başarıyla pasif edildi.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating Trendyol Reviews for company {CompanyId}", companyId);
            return (false, $"Entegrasyon pasif edilirken hata oluştu: {ex.Message}");
        }
    }

    public async Task<bool> IsTrendyolReviewsActiveAsync(Guid companyId)
    {
        try
        {
            var jsKey = $"{companyId}/trendyolReviews.js";
            return await _r2StorageService.FileExistsAsync(jsKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking Trendyol Reviews status for company {CompanyId}", companyId);
            return false;
        }
    }

    #endregion
}
