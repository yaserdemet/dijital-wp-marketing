﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿﻿using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PushDashboard.Configuration;
using PushDashboard.Data;
using PushDashboard.DTOs;
using PushDashboard.Models;
using PushDashboard.ViewModels;
using System.Text.Json;

namespace PushDashboard.Services;

public class CommentService : ICommentService
{
    private readonly ApplicationDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<CommentService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IModuleUsageService _moduleUsageService;
    private readonly CommentScraperApiSettings _apiSettings;
    private readonly IConfiguration _configuration;

    public CommentService(ApplicationDbContext context, HttpClient httpClient, ILogger<CommentService> logger, IServiceProvider serviceProvider, IModuleUsageService moduleUsageService, IOptions<CommentScraperApiSettings> apiSettings, IConfiguration configuration)
    {
        _context = context;
        _httpClient = httpClient;
        _logger = logger;
        _serviceProvider = serviceProvider;
        _moduleUsageService = moduleUsageService;
        _apiSettings = apiSettings.Value;
        _configuration = configuration;
    }

    public async Task<CommentRequestIndexViewModel> GetCommentRequestsAsync(Guid companyId, int page = 1, int pageSize = 20, string? searchTerm = null, string? status = null, string? sortBy = null, string? sortDirection = null)
    {
        var query = _context.CommentRequests
            .Where(cr => cr.CompanyId == companyId);

        // Search filter
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(cr => cr.ProductUrl.Contains(searchTerm) ||
                                     cr.ExternalProductId.Contains(searchTerm));
        }

        // Status filter
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(cr => cr.Status == status);
        }

        // Sorting
        query = sortBy?.ToLower() switch
        {
            "producturl" => sortDirection == "desc" ? query.OrderByDescending(cr => cr.ProductUrl) : query.OrderBy(cr => cr.ProductUrl),
            "status" => sortDirection == "desc" ? query.OrderByDescending(cr => cr.Status) : query.OrderBy(cr => cr.Status),
            "createdat" => sortDirection == "desc" ? query.OrderByDescending(cr => cr.CreatedAt) : query.OrderBy(cr => cr.CreatedAt),
            "commentscount" => sortDirection == "desc" ? query.OrderByDescending(cr => cr.ActualCommentsCount) : query.OrderBy(cr => cr.ActualCommentsCount),
            _ => query.OrderByDescending(cr => cr.CreatedAt)
        };

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

        var requests = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(cr => new CommentRequestResponseDto
            {
                Id = cr.Id,
                ProductUrl = cr.ProductUrl,
                ShortProductUrl = cr.ShortProductUrl,
                ExternalProductId = cr.ExternalProductId,
                ExternalProductUrl = cr.ExternalProductUrl,
                ReviewSource = cr.ReviewSource,
                RequestedCommentsCount = cr.RequestedCommentsCount,
                ActualCommentsCount = cr.ActualCommentsCount,
                Status = cr.Status,
                StatusBadgeClass = cr.StatusBadgeClass,
                StatusIcon = cr.StatusIcon,
                ErrorMessage = cr.ErrorMessage,
                CreatedAt = cr.CreatedAt,
                UpdatedAt = cr.UpdatedAt,
                ProcessedAt = cr.ProcessedAt,
                CommentsFileUrl = cr.CommentsFileUrl,
                ExportToken = cr.ExportToken,
                ExternalRequestId = cr.ExternalRequestId
            })
            .ToListAsync();

        // Stats
        var stats = await GetStatsAsync(companyId);

        return new CommentRequestIndexViewModel
        {
            CommentRequests = requests,
            Stats = stats,
            Pagination = new CommentRequestIndexViewModel.PaginationViewModel
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            }
        };
    }

    public async Task<CommentRequestResponseDto?> GetCommentRequestAsync(int id, Guid companyId)
    {
        var request = await _context.CommentRequests
            .Where(cr => cr.Id == id && cr.CompanyId == companyId)
            .Select(cr => new CommentRequestResponseDto
            {
                Id = cr.Id,
                ProductUrl = cr.ProductUrl,
                ShortProductUrl = cr.ShortProductUrl,
                ExternalProductId = cr.ExternalProductId,
                ExternalProductUrl = cr.ExternalProductUrl,
                ReviewSource = cr.ReviewSource,
                RequestedCommentsCount = cr.RequestedCommentsCount,
                ActualCommentsCount = cr.ActualCommentsCount,
                Status = cr.Status,
                StatusBadgeClass = cr.StatusBadgeClass,
                StatusIcon = cr.StatusIcon,
                ErrorMessage = cr.ErrorMessage,
                CreatedAt = cr.CreatedAt,
                UpdatedAt = cr.UpdatedAt,
                ProcessedAt = cr.ProcessedAt,
                CommentsFileUrl = cr.CommentsFileUrl,
                ExportToken = cr.ExportToken
            })
            .FirstOrDefaultAsync();

        return request;
    }

    public async Task<CommentRequestResponseDto> CreateCommentRequestAsync(CreateCommentRequestDto dto, Guid companyId, string userId)
    {
        // Bakiye kontrolü yap
        var balanceCheck = await CheckBalanceForCommentsAsync(companyId, dto.RequestedCommentsCount);
        if (!balanceCheck.HasBalance)
        {
            throw new InvalidOperationException($"Yetersiz bakiye. Gerekli: ₺{balanceCheck.RequiredAmount:N2}, Mevcut: ₺{balanceCheck.CurrentBalance:N2}");
        }

        // Webhook URL'ini oluştur
        var webhookUrl = $"{GetBaseUrl()}/webhook/comment-completed";

        var commentRequest = new CommentRequest
        {
            CompanyId = companyId,
            ProductUrl = dto.ProductUrl,
            ExternalProductId = dto.ExternalProductId,
            ExternalProductUrl = dto.ExternalProductUrl,
            ReviewSource = dto.ReviewSource,
            RequestedCommentsCount = dto.RequestedCommentsCount,
            Status = "Sırada",
            WebhookUrl = webhookUrl,
            IsProcessing = false,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now,
            ExportToken = Guid.NewGuid().ToString()
        };

        _context.CommentRequests.Add(commentRequest);
        await _context.SaveChangesAsync();

        // Dış API'ye istek gönder
        try
        {
            var apiRequest = new ExternalApiCreateRequestDto
            {
                customer_id = $"cst_{companyId}",
                product_url = commentRequest.ProductUrl,
                external_product_id = commentRequest.ExternalProductId,
                num_comments_to_fetch = commentRequest.RequestedCommentsCount,
                webhook_url = webhookUrl
            };

            var response = await _httpClient.PostAsJsonAsync(_apiSettings.ScrapeReviewsUrl, apiRequest);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var apiResponse = JsonSerializer.Deserialize<PushDashboard.DTOs.ExternalApiResponseDto>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (apiResponse?.status == "accepted" && !string.IsNullOrEmpty(apiResponse.job_id))
                {
                    // External request ID'yi kaydet ve durumu güncelle
                    commentRequest.ExternalRequestId = apiResponse.job_id;
                    commentRequest.Status = "İşleniyor";
                    commentRequest.IsProcessing = true;
                    commentRequest.UpdatedAt = DateTime.Now;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Comment request {RequestId} sent to external API successfully with job ID {JobId}",
                        commentRequest.Id, apiResponse.job_id);
                }
                else
                {
                    throw new InvalidOperationException($"API yanıtı başarısız: {apiResponse?.message}");
                }
            }
            else
            {
                throw new InvalidOperationException($"API isteği başarısız: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending request to external API for comment request {RequestId}", commentRequest.Id);

            // Hata durumunda status'u güncelle
            commentRequest.Status = "Hata";
            commentRequest.ErrorMessage = ex.Message;
            commentRequest.IsProcessing = false;
            commentRequest.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            throw;
        }

        return new CommentRequestResponseDto
        {
            Id = commentRequest.Id,
            ProductUrl = commentRequest.ProductUrl,
            ShortProductUrl = commentRequest.ShortProductUrl,
            ExternalProductId = commentRequest.ExternalProductId,
            ExternalProductUrl = commentRequest.ExternalProductUrl,
            ReviewSource = commentRequest.ReviewSource,
            RequestedCommentsCount = commentRequest.RequestedCommentsCount,
            ActualCommentsCount = commentRequest.ActualCommentsCount,
            Status = commentRequest.Status,
            StatusBadgeClass = commentRequest.StatusBadgeClass,
            StatusIcon = commentRequest.StatusIcon,
            ErrorMessage = commentRequest.ErrorMessage,
            CreatedAt = commentRequest.CreatedAt,
            UpdatedAt = commentRequest.UpdatedAt,
            ProcessedAt = commentRequest.ProcessedAt,
            CommentsFileUrl = commentRequest.CommentsFileUrl,
            ExportToken = commentRequest.ExportToken
        };
    }

    public async Task<bool> DeleteCommentRequestAsync(int id, Guid companyId)
    {
        var request = await _context.CommentRequests
            .FirstOrDefaultAsync(cr => cr.Id == id && cr.CompanyId == companyId);

        if (request == null)
            return false;

        // İşlemde olan isteklerin silinmesini engelle
        if (request.IsProcessing)
        {
            throw new InvalidOperationException("İşlemde olan istekler silinemez. Lütfen işlem tamamlanana kadar bekleyin.");
        }

        _context.CommentRequests.Remove(request);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<CommentDetailsDto?> GetCommentDetailsAsync(int requestId, Guid companyId, int page = 1, int pageSize = 20)
    {
        var request = await _context.CommentRequests
            .FirstOrDefaultAsync(cr => cr.Id == requestId && cr.CompanyId == companyId);

        if (request == null || string.IsNullOrEmpty(request.CommentsFileUrl))
            return null;

        try
        {
            // Fetch comments from the external URL
            var response = await _httpClient.GetStringAsync(request.CommentsFileUrl);
            var allComments = JsonSerializer.Deserialize<List<CommentDto>>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<CommentDto>();

            var totalComments = allComments.Count;
            var totalPages = (int)Math.Ceiling((double)totalComments / pageSize);

            var pagedComments = allComments
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new CommentDetailsDto
            {
                RequestId = requestId,
                ProductUrl = request.ProductUrl,
                ExternalProductId = request.ExternalProductId,
                Status = request.Status,
                TotalComments = totalComments,
                Comments = pagedComments,
                CurrentPage = page,
                PageSize = pageSize,
                TotalPages = totalPages
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching comments from URL: {Url}", request.CommentsFileUrl);
            return null;
        }
    }

    public async Task<bool> ProcessCommentRequestAsync(int requestId, Guid companyId)
    {
        // Bu metod artık sadece retry için kullanılıyor
        // Webhook tabanlı yapıda normal işlemler webhook üzerinden gelir
        var request = await _context.CommentRequests.FirstOrDefaultAsync(cr => cr.Id == requestId && cr.CompanyId == companyId);
        if (request == null) return false;

        // Retry durumunda status'u tekrar "Sırada" yap
        request.Status = "Sırada";
        request.IsProcessing = false;
        request.UpdatedAt = DateTime.Now;
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<CommentRequestIndexViewModel.CommentRequestStatsViewModel> GetStatsAsync(Guid companyId)
    {
        var requests = await _context.CommentRequests
            .Where(cr => cr.CompanyId == companyId)
            .ToListAsync();

        return new CommentRequestIndexViewModel.CommentRequestStatsViewModel
        {
            TotalRequests = requests.Count,
            PendingRequests = requests.Count(r => r.Status == "Sırada"),
            ProcessingRequests = requests.Count(r => r.Status == "İşleniyor"),
            CompletedRequests = requests.Count(r => r.Status == "Hazır"),
            FailedRequests = requests.Count(r => r.Status == "Hata"),
            TotalComments = requests.Where(r => r.ActualCommentsCount.HasValue).Sum(r => r.ActualCommentsCount.Value)
        };
    }

    public async Task<string?> ExportCommentsAsHtmlAsync(int requestId, Guid companyId)
    {
        var request = await _context.CommentRequests
            .FirstOrDefaultAsync(cr => cr.Id == requestId && cr.CompanyId == companyId);

        if (request == null || string.IsNullOrEmpty(request.CommentsFileUrl))
            return null;

        try
        {
            // Fetch all comments
            var response = await _httpClient.GetStringAsync(request.CommentsFileUrl);
            var allComments = JsonSerializer.Deserialize<List<CommentDto>>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<CommentDto>();

            // Generate HTML
            var html = GenerateCommentsHtml(allComments, request);
            return html;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting comments as HTML for request {RequestId}", requestId);
            return null;
        }
    }

    public async Task<CommentDetailsDto?> GetCommentsByTokenAsync(string token)
    {
        var request = await _context.CommentRequests
            .FirstOrDefaultAsync(cr => cr.ExportToken == token);

        if (request == null || string.IsNullOrEmpty(request.CommentsFileUrl))
            return null;

        try
        {
            // Fetch all comments
            var response = await _httpClient.GetStringAsync(request.CommentsFileUrl);
            var allComments = JsonSerializer.Deserialize<List<CommentDto>>(response, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<CommentDto>();

            return new CommentDetailsDto
            {
                RequestId = request.Id,
                ProductUrl = request.ProductUrl,
                ExternalProductId = request.ExternalProductId,
                Status = request.Status,
                TotalComments = allComments.Count,
                Comments = allComments,
                CurrentPage = 1,
                PageSize = allComments.Count,
                TotalPages = 1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching comments by token {Token}", token);
            return null;
        }
    }

    private string GenerateCommentsHtml(List<CommentDto> comments, CommentRequest request)
    {
        var averageRating = comments.Any() ? comments.Average(c => c.rating) : 0;
        var totalPhotos = comments.Sum(c => c.photos?.Count ?? 0);

        var html = $@"
<!DOCTYPE html>
<html lang='tr'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Yorumlar - {request.ExternalProductId}</title>
    <style>
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            padding: 20px;
        }}
        .container {{
            max-width: 900px;
            margin: 0 auto;
            background: white;
            border-radius: 16px;
            overflow: hidden;
            box-shadow: 0 20px 40px rgba(0,0,0,0.1);
        }}
        .header {{
            background: linear-gradient(135deg, #4f46e5 0%, #7c3aed 100%);
            color: white;
            padding: 30px;
            text-align: center;
        }}
        .header h1 {{ font-size: 2.5rem; margin-bottom: 10px; font-weight: 700; }}
        .header p {{ font-size: 1.1rem; opacity: 0.9; margin: 5px 0; }}
        .stats {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
            padding: 30px;
            background: #f8fafc;
            border-bottom: 1px solid #e2e8f0;
        }}
        .stat-card {{
            background: white;
            padding: 20px;
            border-radius: 12px;
            text-align: center;
            box-shadow: 0 4px 6px rgba(0,0,0,0.05);
            border: 1px solid #e2e8f0;
        }}
        .stat-number {{ font-size: 2rem; font-weight: 700; color: #4f46e5; margin-bottom: 5px; }}
        .stat-label {{ color: #64748b; font-size: 0.9rem; font-weight: 500; }}
        .comments {{ padding: 30px; }}
        .comment {{
            background: #ffffff;
            margin-bottom: 24px;
            border-radius: 16px;
            overflow: hidden;
            box-shadow: 0 4px 6px rgba(0,0,0,0.05);
            border: 1px solid #e2e8f0;
            transition: transform 0.2s ease, box-shadow 0.2s ease;
        }}
        .comment:hover {{
            transform: translateY(-2px);
            box-shadow: 0 8px 25px rgba(0,0,0,0.1);
        }}
        .comment-header {{
            padding: 20px 24px 16px;
            border-bottom: 1px solid #f1f5f9;
            background: #fafbfc;
        }}
        .user-info {{ display: flex; align-items: center; justify-content: space-between; margin-bottom: 12px; }}
        .user-name {{
            font-weight: 600;
            color: #1e293b;
            font-size: 1.1rem;
            display: flex;
            align-items: center;
            gap: 8px;
        }}
        .elite-badge {{
            background: linear-gradient(135deg, #fbbf24, #f59e0b);
            color: white;
            padding: 4px 8px;
            border-radius: 12px;
            font-size: 0.7rem;
            font-weight: 600;
            text-transform: uppercase;
        }}
        .comment-date {{ color: #64748b; font-size: 0.9rem; }}
        .rating {{ display: flex; align-items: center; gap: 4px; }}
        .stars {{ display: flex; gap: 2px; }}
        .star {{
            width: 18px;
            height: 18px;
            color: #fbbf24;
            font-size: 18px;
        }}
        .star.empty {{ color: #e2e8f0; }}
        .rating-text {{ margin-left: 8px; font-weight: 600; color: #4f46e5; }}
        .comment-body {{ padding: 24px; }}
        .comment-text {{
            font-size: 1rem;
            line-height: 1.7;
            color: #374151;
            margin-bottom: 20px;
        }}
        .comment-photos {{
            display: grid;
            grid-template-columns: repeat(auto-fill, minmax(80px, 1fr));
            gap: 8px;
            margin-bottom: 16px;
        }}
        .photo {{
            aspect-ratio: 1;
            border-radius: 8px;
            overflow: hidden;
            cursor: pointer;
            transition: transform 0.2s ease;
        }}
        .photo:hover {{ transform: scale(1.05); }}
        .photo img {{
            width: 100%;
            height: 100%;
            object-fit: cover;
            border-radius: 12px;
        }}
        .comment-footer {{
            padding: 16px 24px;
            background: #f8fafc;
            display: flex;
            align-items: center;
            justify-content: space-between;
        }}
        .like-count {{
            display: flex;
            align-items: center;
            gap: 6px;
            color: #64748b;
            font-size: 0.9rem;
        }}
        .like-icon {{ color: #ef4444; }}
        .no-comments {{
            text-align: center;
            padding: 60px 20px;
            color: #64748b;
            font-size: 1.1rem;
        }}
        @media (max-width: 768px) {{
            .container {{ margin: 10px; border-radius: 12px; }}
            .header {{ padding: 20px; }}
            .header h1 {{ font-size: 2rem; }}
            .stats {{ grid-template-columns: 1fr; padding: 20px; }}
            .comments {{ padding: 20px; }}
            .comment-photos {{ grid-template-columns: repeat(auto-fill, minmax(60px, 1fr)); }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Ürün Yorumları</h1>
            <p><strong>Ürün ID:</strong> {request.ExternalProductId}</p>
            <p><strong>Kaynak:</strong> {request.ReviewSource}</p>
        </div>
        <div class='stats'>
            <div class='stat-card'>
                <div class='stat-number'>{comments.Count}</div>
                <div class='stat-label'>Toplam Yorum</div>
            </div>
            <div class='stat-card'>
                <div class='stat-number'>{averageRating:F1}</div>
                <div class='stat-label'>Ortalama Puan</div>
            </div>
            <div class='stat-card'>
                <div class='stat-number'>{totalPhotos}</div>
                <div class='stat-label'>Toplam Fotoğraf</div>
            </div>
            <div class='stat-card'>
                <div class='stat-number'>{request.CreatedAt:dd.MM.yyyy}</div>
                <div class='stat-label'>Çekilme Tarihi</div>
            </div>
        </div>
        <div class='comments'>";

        if (!comments.Any())
        {
            html += @"
            <div class='no-comments'>
                <p>Henüz yorum bulunmuyor.</p>
            </div>";
        }
        else
        {
            foreach (var comment in comments)
            {
                var starsHtml = "";
                for (int i = 1; i <= 5; i++)
                {
                    starsHtml += $"<span class='star {(i <= comment.rating ? "" : "empty")}'>★</span>";
                }

                var photosHtml = "";
                if (comment.photos?.Any() == true)
                {
                    photosHtml = "<div class='comment-photos'>";
                    foreach (var photo in comment.photos)
                    {
                        photosHtml += $@"
                        <div class='photo'>
                            <img src='{photo}' alt='Yorum fotoğrafı' loading='lazy' />
                        </div>";
                    }
                    photosHtml += "</div>";
                }

                var eliteBadge = !string.IsNullOrEmpty(comment.elit_customer) ?
                    "<span class='elite-badge'>Elite</span>" : "";

                html += $@"
                <div class='comment'>
                    <div class='comment-header'>
                        <div class='user-info'>
                            <div class='user-name'>
                                {comment.user}
                                {eliteBadge}
                            </div>
                            <div class='comment-date'>{comment.date}</div>
                        </div>
                        <div class='rating'>
                            <div class='stars'>{starsHtml}</div>
                            <span class='rating-text'>{comment.rating:F1}/5</span>
                        </div>
                    </div>
                    <div class='comment-body'>
                        <div class='comment-text'>{comment.comment}</div>
                        {photosHtml}
                    </div>
                    <div class='comment-footer'>
                        <div class='like-count'>
                            <span class='like-icon'>❤️</span>
                            <span>{comment.like_count} beğeni</span>
                        </div>
                    </div>
                </div>";
            }
        }

        html += @"
        </div>
    </div>
</body>
</html>";

        return html;
    }

    public async Task<decimal> GetCommentPriceAsync(Guid companyId)
    {
        try
        {
            // Yorum Taşıma modülünün ayarlarından fiyatı al
            var commentModule = await _context.Modules.FirstOrDefaultAsync(m => m.Name == "Yorum Taşıma");
            if (commentModule != null)
            {
                var companyModule = await _context.CompanyModules
                    .Include(cm => cm.Settings)
                    .FirstOrDefaultAsync(cm => cm.CompanyId == companyId && cm.ModuleId == commentModule.Id && cm.IsActive);

                if (companyModule?.Settings != null && !string.IsNullOrEmpty(companyModule.Settings.SettingsJson))
                {
                    var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(companyModule.Settings.SettingsJson);
                    if (settings != null && settings.TryGetValue("pricePerComment", out var priceObj))
                    {
                        if (decimal.TryParse(priceObj.ToString(), CultureInfo.InvariantCulture ,out var price))
                        {
                            return price;
                        }
                    }
                }

                // Eğer company settings'te yoksa, modülün default settings'inden al
                if (!string.IsNullOrEmpty(commentModule.DefaultSettings))
                {
                    var defaultSettings = JsonSerializer.Deserialize<Dictionary<string, object>>(commentModule.DefaultSettings);
                    if (defaultSettings != null && defaultSettings.TryGetValue("pricePerComment", out var defaultPriceObj))
                    {
                        if (decimal.TryParse(defaultPriceObj.ToString(), out var defaultPrice))
                        {
                            return defaultPrice;
                        }
                    }
                }
            }

            // Varsayılan fiyat
            return 0.10m;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comment price for company {CompanyId}", companyId);
            return 0.10m; // Varsayılan fiyat
        }
    }

    public async Task<(bool HasBalance, decimal RequiredAmount, decimal CurrentBalance, decimal CommentPrice)> CheckBalanceForCommentsAsync(Guid companyId, int commentCount)
    {
        try
        {
            var commentPrice = await GetCommentPriceAsync(companyId);
            var requiredAmount = commentPrice * commentCount;

            var company = await _context.Companies.FindAsync(companyId);
            var currentBalance = company?.CreditBalance ?? 0;

            return (currentBalance >= requiredAmount, requiredAmount, currentBalance, commentPrice);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking balance for comments for company {CompanyId}", companyId);
            return (false, 0, 0,0);
        }
    }

    public async Task<bool> ProcessWebhookAsync(CommentWebhookRequestDto request)
    {
        try
        {
            // Job ID ile comment request'i bul
            var commentRequest = await _context.CommentRequests
                .FirstOrDefaultAsync(cr => cr.ExternalRequestId == request.job_id);

            if (commentRequest == null)
            {
                _logger.LogWarning("Comment request not found for job ID {JobId}", request.job_id);
                return false;
            }

            _logger.LogInformation("Processing webhook for comment request {RequestId} (Job ID: {JobId})",
                commentRequest.Id, request.job_id);

            // Status'u güncelle
            if (request.status.ToLower() == "completed")
            {
                if (request.result?.status == "success" && request.result.comments_count > 0)
                {
                    // Başarılı tamamlama
                    commentRequest.Status = "Hazır";
                    commentRequest.ActualCommentsCount = request.result.comments_count;
                    commentRequest.CommentsFileUrl = request.result.comments_file_url;
                    commentRequest.LogsFileUrl = request.result.logs_file_url;
                    commentRequest.ProcessedAt = DateTime.Now;
                    commentRequest.IsProcessing = false;
                    commentRequest.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    // Başarılı işlem için bakiye düşür ve expense kaydı oluştur
                    await DeductBalanceAndCreateExpenseAsync(commentRequest.CompanyId, request.result.comments_count, commentRequest.Id);

                    _logger.LogInformation("Successfully processed webhook for comment request {RequestId} with {CommentCount} comments",
                        commentRequest.Id, request.result.comments_count);
                }
                else
                {
                    // Yorum bulunamadı veya hata
                    commentRequest.Status = "Hata";
                    commentRequest.ErrorMessage = request.result?.error_message ?? "Hiç yorum bulunamadı";
                    commentRequest.IsProcessing = false;
                    commentRequest.UpdatedAt = DateTime.Now;

                    await _context.SaveChangesAsync();

                    _logger.LogWarning("Comment request {RequestId} completed but no comments found: {ErrorMessage}",
                        commentRequest.Id, commentRequest.ErrorMessage);
                }
            }
            else
            {
                // Başarısız işlem
                commentRequest.Status = "Hata";
                commentRequest.ErrorMessage = request.result?.error_message ?? "Bilinmeyen hata";
                commentRequest.IsProcessing = false;
                commentRequest.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogWarning("Comment request {RequestId} failed: {ErrorMessage}",
                    commentRequest.Id, commentRequest.ErrorMessage);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook for job ID {JobId}", request.job_id);
            return false;
        }
    }

    private async Task DeductBalanceAndCreateExpenseAsync(Guid companyId, int commentCount, int requestId)
    {
        try
        {
            var commentPrice = await GetCommentPriceAsync(companyId);
            var totalCost = commentPrice * commentCount;

            // Şirket bakiyesini düşür
            var company = await _context.Companies.FindAsync(companyId);
            if (company != null)
            {
                company.CreditBalance -= totalCost;
                company.UpdatedAt = DateTime.Now;
            }

            // TODO : Expense kaydı oluştur
            // var expense = new CompanyExpense
            // {
            //     CompanyId = companyId,
            //     ModuleId = null, // Comment modülü için ayrı bir modül ID'si eklenebilir
            //     Amount = totalCost,
            //     Description = $"Yorum çekme işlemi - {commentCount} yorum (İstek #{requestId})",
            //     Category = "Comment",
            //     UserId = null, // Otomatik işlem olduğu için null
            //     CreatedAt = DateTime.Now
            // };
            //
            // _context.CompanyExpenses.Add(expense);
            // await _context.SaveChangesAsync();

            _logger.LogInformation("Deducted {Amount:C} from company {CompanyId} balance for {CommentCount} comments",
                totalCost, companyId, commentCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deducting balance for company {CompanyId}", companyId);
            throw;
        }
    }

    public async Task<bool> CheckAndUpdatePendingRequestsAsync(Guid companyId)
    {
        try
        {
            // Sadece bekleyen veya işlenen istekleri bul (hazır ve hatalı olanları atlayalım)
            var pendingRequests = await _context.CommentRequests
                .Where(cr => cr.CompanyId == companyId &&
                           !string.IsNullOrEmpty(cr.ExternalRequestId) &&
                           (cr.Status == "Sırada" || cr.Status == "İşleniyor"))
                .ToListAsync();

            if (!pendingRequests.Any())
                return true;

            var updatedCount = 0;

            foreach (var request in pendingRequests)
            {
                try
                {
                    // External API'den job durumunu kontrol et
                    var response = await _httpClient.GetAsync(_apiSettings.GetJobStatusUrl(request.ExternalRequestId));

                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        // 404 durumunda job bulunamadı, hata olarak işle
                        request.Status = "Hata";
                        request.ErrorMessage = "Job bulunamadı (404) - İşlem iptal edilmiş olabilir";
                        request.IsProcessing = false;
                        request.UpdatedAt = DateTime.Now;
                        await _context.SaveChangesAsync();
                        updatedCount++;

                        _logger.LogWarning("Job not found (404) for request {RequestId} with job ID {JobId}",
                            request.Id, request.ExternalRequestId);
                        continue;
                    }

                    response.EnsureSuccessStatusCode();
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var jobResponse = JsonSerializer.Deserialize<JobStatusResponseDto>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (jobResponse != null)
                    {
                        // Convert JobStatusResponseDto to JobDto for compatibility with existing UpdateRequestFromJobStatus method
                        var job = new JobDto
                        {
                            job_id = jobResponse.job_id,
                            customer_id = jobResponse.customer_id,
                            external_product_id = jobResponse.external_product_id,
                            product_url = jobResponse.product_url,
                            num_comments_to_fetch = jobResponse.num_comments_to_fetch,
                            status = jobResponse.status,
                            error = jobResponse.error,
                            created_at = jobResponse.created_at,
                            started_at = jobResponse.started_at,
                            completed_at = jobResponse.completed_at,
                            webhook_url = jobResponse.webhook_url,
                            progress = jobResponse.progress,
                            result = jobResponse.result,
                        };

                        var updated = await UpdateRequestFromJobStatus(request, job);
                        if (updated) updatedCount++;
                    }
                    else
                    {
                        _logger.LogWarning("No job data found in response for request {RequestId} with job ID {JobId}",
                            request.Id, request.ExternalRequestId);
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "HTTP error checking job status for request {RequestId} with job ID {JobId}",
                        request.Id, request.ExternalRequestId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking job status for request {RequestId} with job ID {JobId}",
                        request.Id, request.ExternalRequestId);
                }
            }

            _logger.LogInformation("Checked {TotalRequests} pending requests, updated {UpdatedCount} requests for company {CompanyId}",
                pendingRequests.Count, updatedCount, companyId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking pending requests for company {CompanyId}", companyId);
            return false;
        }
    }

    private async Task<bool> UpdateRequestFromJobStatus(CommentRequest request, JobDto job)
    {
        var originalStatus = request.Status;
        var wasUpdated = false;

        // Status mapping using helper method
        var newStatus = CommentRequest.MapExternalStatusToInternal(job.status);

        switch (job.status.ToLower())
        {
            case "pending":
                if (request.Status != newStatus)
                {
                    request.Status = newStatus;
                    request.IsProcessing = false;
                    request.UpdatedAt = DateTime.Now;
                    wasUpdated = true;
                }
                break;

            case "running":
                if (request.Status != newStatus)
                {
                    request.Status = newStatus;
                    request.IsProcessing = true;
                    request.UpdatedAt = DateTime.Now;
                    wasUpdated = true;
                }
                break;

            case "completed":
                if (job.result?.status == "success" && job.result.comments_count > 0)
                {
                    request.Status = newStatus;
                    request.ActualCommentsCount = job.result.comments_count;
                    request.CommentsFileUrl = job.result.comments_file_url;
                    request.LogsFileUrl = job.result.logs_file_url;
                    request.ProcessedAt = job.completed_at ?? DateTime.Now;
                    request.IsProcessing = false;
                    request.UpdatedAt = DateTime.Now;

                    // Başarılı işlem için bakiye düşür ve expense kaydı oluştur
                    await DeductBalanceAndCreateExpenseAsync(request.CompanyId, job.result.comments_count, request.Id);
                    wasUpdated = true;
                }
                else
                {
                    request.Status = "Hata";
                    request.ErrorMessage = job.result?.message ?? job.error ?? "Hiç yorum bulunamadı";
                    request.IsProcessing = false;
                    request.UpdatedAt = DateTime.Now;
                    wasUpdated = true;
                }
                break;

            case "failed":
                request.Status = "Hata";
                request.ErrorMessage = job.error ?? "İşlem başarısız oldu";
                request.IsProcessing = false;
                request.UpdatedAt = DateTime.Now;
                wasUpdated = true;
                break;
        }

        if (wasUpdated)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Updated request {RequestId} status from {OldStatus} to {NewStatus}",
                request.Id, originalStatus, request.Status);
        }

        return wasUpdated;
    }

    private string GetBaseUrl()
    {
        // appsettings.json'dan BaseUrl'i oku
        return _configuration["BaseUrl"] ?? "https://localhost:7004";
    }
}
