using System.ComponentModel.DataAnnotations;

namespace PushDashboard.DTOs;

#region Store Management DTOs

public class CreateStoreRequestDto
{
    [Required(ErrorMessage = "Mağaza URL'si gereklidir.")]
    [Url(ErrorMessage = "Geçerli bir URL giriniz.")]
    public string StoreUrl { get; set; } = string.Empty;

    [StringLength(200, ErrorMessage = "Mağaza adı en fazla 200 karakter olabilir.")]
    public string? StoreName { get; set; }
}

public class SyncStoreProductsRequestDto
{
    [Required]
    public int StoreId { get; set; }

    [Range(1, 1000, ErrorMessage = "Ürün sayısı 1 ile 1000 arasında olmalıdır.")]
    public int ProductCount { get; set; } = 100;
}

public class StoreResponseDto
{
    public int Id { get; set; }
    public string StoreUrl { get; set; } = string.Empty;
    public string? ExternalStoreId { get; set; }
    public string? StoreName { get; set; }
    public string SyncStatus { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = string.Empty;
    public string StatusIcon { get; set; } = string.Empty;
    public DateTime? LastSyncAt { get; set; }
    public int? ProductCount { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

#endregion

#region Product Management DTOs

public class ProductResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ShortTitle { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string Href { get; set; } = string.Empty;
    public string FullUrl { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TransferSelectedProductsRequestDto
{
    [Required]
    public int StoreId { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "En az bir ürün seçilmelidir.")]
    public List<int> ProductIds { get; set; } = new();

    [StringLength(100, ErrorMessage = "Batch ID'si en fazla 100 karakter olabilir.")]
    public string? ExternalBatchId { get; set; }

    [Required]
    [Range(10, 1000, ErrorMessage = "Yorum sayısı 10 ile 1000 arasında olmalıdır.")]
    public int CommentCount { get; set; } = 100;

    /// <summary>
    /// Opsiyonel yıldız filtresi (1-5 arası değerler). Boş ise tüm yıldızlar çekilir.
    /// </summary>
    /// <summary>
    /// Opsiyonel yıldız filtresi (1-5 arası değerler). Boş ise tüm yıldızlar çekilir.
    /// </summary>
    public List<int>? StarRatings { get; set; }

    public bool AutoSync { get; set; }
}

#endregion

#region Transfer Job DTOs

public class TransferJobResponseDto
{
    public int Id { get; set; }
    public string JobId { get; set; } = string.Empty;
    public string? ExternalBatchId { get; set; }
    public string JobType { get; set; } = string.Empty;
    public string JobTypeDisplay { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = string.Empty;
    public string StatusIcon { get; set; } = string.Empty;
    public bool IsAutoTransferEnabled { get; set; }
    public int? ProgressPercent { get; set; }
    public string? CurrentStep { get; set; }
    public int? TotalProducts { get; set; }
    public int? ProcessedProducts { get; set; }
    public int? TotalComments { get; set; }
    public string? ResultFileUrl { get; set; }
    public string? LogsFileUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? StoreName { get; set; }
}

public class TransferJobDetailDto
{
    public int Id { get; set; }
    public string JobId { get; set; } = string.Empty;
    public string? ExternalBatchId { get; set; }
    public string JobType { get; set; } = string.Empty;
    public string JobTypeDisplay { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = string.Empty;
    public int? TotalProducts { get; set; }
    public int? ProcessedProducts { get; set; }
    public int? TotalComments { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? StoreName { get; set; }
    public List<TransferJobProductDto> Products { get; set; } = new();
}

public class TransferJobProductDto
{
    public int Id { get; set; }
    public string ProductUrl { get; set; } = string.Empty;
    public string ProductTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = string.Empty;
    public int? CommentsCount { get; set; }
    public string? CommentsFileUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? ProcessedAt { get; set; }
}

#endregion

#region Scraper API DTOs

public class ScrapeProductsRequestDto
{
    [Required]
    public string customer_id { get; set; } = string.Empty;

    [Required]
    public string store_url { get; set; } = string.Empty;

    [Required]
    public string external_store_id { get; set; } = string.Empty;

    [Required]
    public int product_count { get; set; }

    [Required]
    public string webhook_url { get; set; } = string.Empty;
}

public class ScrapeCommentsRequestDto
{
    [Required]
    public string customer_id { get; set; } = string.Empty;

    [Required]
    public List<string> product_urls { get; set; } = new();

    public string? external_batch_id { get; set; } = string.Empty;

    [Required]
    public string webhook_url { get; set; } = string.Empty;

    [Required]
    [Range(10, 1000, ErrorMessage = "Yorum sayısı 10 ile 1000 arasında olmalıdır.")]
    public int comment_count { get; set; } = 100;

    /// <summary>
    /// Opsiyonel - sadece belirtilen yıldızlı yorumları çeker
    /// </summary>
    /// <summary>
    /// Opsiyonel - sadece belirtilen yıldızlı yorumları çeker
    /// </summary>
    public List<int>? star_ratings { get; set; }

    public string? since_date { get; set; }
}

public class ScraperApiResponseDto
{
    public string status { get; set; } = string.Empty;
    public string message { get; set; } = string.Empty;
    public string job_id { get; set; } = string.Empty;
    public string webhook_url { get; set; } = string.Empty;
    public int? total_urls { get; set; }
}

public class JobStatusDto
{
    public string job_id { get; set; } = string.Empty;
    public string status { get; set; } = string.Empty;
    public string customer_id { get; set; } = string.Empty;
    public DateTime created_at { get; set; }
    public JobProgressInfoDto? progress { get; set; }
}

public class JobProgressInfoDto
{
    public string current_step { get; set; } = string.Empty;
    public int percent_complete { get; set; }
}

#endregion

#region Webhook DTOs

public class ProductScrapeWebhookDto
{
    [Required]
    public string job_id { get; set; } = string.Empty;

    [Required]
    public string status { get; set; } = string.Empty;

    [Required]
    public string customer_id { get; set; } = string.Empty;

    [Required]
    public string external_store_id { get; set; } = string.Empty;

    public ProductScrapeResultDto? result { get; set; }
}

public class ProductScrapeResultDto
{
    public string status { get; set; } = string.Empty;
    public int products_count { get; set; }
    public string? products_file_url { get; set; }
    public string? logs_file_url { get; set; }
}

public class CommentScrapeWebhookDto
{
    [Required]
    public string job_id { get; set; } = string.Empty;

    [Required]
    public string status { get; set; } = string.Empty;

    [Required]
    public string customer_id { get; set; } = string.Empty;

    public string? external_batch_id { get; set; } = string.Empty;

    public CommentScrapeResultDto? result { get; set; }
}

public class CommentScrapeResultDto
{
    public string status { get; set; } = string.Empty;
    public int comments_count { get; set; }
    public string? comments_file_url { get; set; }
    public string? logs_file_url { get; set; }
    public int processed_urls { get; set; }
    public int total_urls { get; set; }
    public List<string>? failed_urls { get; set; }
    public List<BatchManifestItemDto>? batch_manifest { get; set; }
}

public class BatchManifestItemDto
{
    public string product_url { get; set; } = string.Empty;
    public string product_id { get; set; } = string.Empty;
    public int comments_count { get; set; }
    public string file_url { get; set; } = string.Empty;
    public string status { get; set; } = string.Empty;
}

#endregion

#region Request DTOs

public class DeleteStoreRequest
{
    [Required]
    public int StoreId { get; set; }
}

public class CancelTransferJobRequest
{
    [Required]
    public int JobId { get; set; }
}

#endregion

#region Product Data DTOs

public class ScrapedProductDto
{
    public string title { get; set; } = string.Empty;
    public string id { get; set; } = string.Empty;
    public string href { get; set; } = string.Empty;
}

#endregion
