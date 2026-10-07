﻿﻿﻿﻿﻿﻿using System.ComponentModel.DataAnnotations;

namespace PushDashboard.DTOs;

public class CreateCommentRequestDto
{
    [Required(ErrorMessage = "Ürün URL'si gereklidir.")]
    [Url(ErrorMessage = "Geçerli bir URL giriniz.")]
    public string ProductUrl { get; set; } = string.Empty;

    [Required(ErrorMessage = "External Product ID gereklidir.")]
    [StringLength(100, ErrorMessage = "External Product ID en fazla 100 karakter olabilir.")]
    public string ExternalProductId { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "External Product URL en fazla 500 karakter olabilir.")]
    public string? ExternalProductUrl { get; set; }

    [Required(ErrorMessage = "Yorum kaynağı seçilmelidir.")]
    public string ReviewSource { get; set; } = "Trendyol";

    [Range(20, 1000, ErrorMessage = "Yorum sayısı 20 ile 1000 arasında olmalıdır.")]
    public int RequestedCommentsCount { get; set; } = 1000;
}

public class CommentRequestResponseDto
{
    public int Id { get; set; }
    public string ProductUrl { get; set; } = string.Empty;
    public string ShortProductUrl { get; set; } = string.Empty;
    public string ExternalProductId { get; set; } = string.Empty;
    public string? ExternalProductUrl { get; set; }
    public string ReviewSource { get; set; } = string.Empty;
    public int RequestedCommentsCount { get; set; }
    public int? ActualCommentsCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusBadgeClass { get; set; } = string.Empty;
    public string StatusIcon { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public bool HasComments => !string.IsNullOrEmpty(CommentsFileUrl) && Status == "Hazır";
    public string? CommentsFileUrl { get; set; }
    public string ExportToken { get; set; } = string.Empty;

    // Progress information from external API
    public string? ExternalRequestId { get; set; }
    public int? ProgressCommentsFound { get; set; }
    public string? ProgressCurrentStep { get; set; }
    public int? ProgressScrollAttempts { get; set; }
    public int? ProgressMaxScrollAttempts { get; set; }
}

public class ExternalApiRequestDto
{
    public string customer_id { get; set; } = string.Empty;
    public string product_url { get; set; } = string.Empty;
    public string external_product_id { get; set; } = string.Empty;
    public int num_comments_to_fetch { get; set; }
}

public class OldExternalApiResponseDto
{
    public int comments_count { get; set; }
    public string comments_file_url { get; set; } = string.Empty;
    public string logs_file_url { get; set; } = string.Empty;
    public string message { get; set; } = string.Empty;
    public string? screenshot_url { get; set; }
    public string status { get; set; } = string.Empty;
}

public class CommentDto
{
    public double rating { get; set; }
    public string user { get; set; } = string.Empty;
    public string date { get; set; } = string.Empty;
    public string elit_customer { get; set; } = string.Empty;
    public string comment { get; set; } = string.Empty;
    public List<string> photos { get; set; } = new();
    public string like_count { get; set; } = string.Empty;

    // Legacy properties for backward compatibility
    public string info => $"Kullanıcı: {user} | Tarih: {date} | Beğeni: {like_count}";
}

public class CommentDetailsDto
{
    public int RequestId { get; set; }
    public string ProductUrl { get; set; } = string.Empty;
    public string ExternalProductId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalComments { get; set; }
    public List<CommentDto> Comments { get; set; } = new();
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => CurrentPage > 1;
    public bool HasNextPage => CurrentPage < TotalPages;
}
