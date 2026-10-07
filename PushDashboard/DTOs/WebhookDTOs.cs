﻿﻿﻿﻿﻿﻿using System.ComponentModel.DataAnnotations;

namespace PushDashboard.DTOs;

public class CommentWebhookRequestDto
{
    [Required]
    public string job_id { get; set; } = string.Empty;

    [Required]
    public string status { get; set; } = string.Empty; // "completed", "failed"

    [Required]
    public string customer_id { get; set; } = string.Empty;

    public string? external_product_id { get; set; } = string.Empty;
    public string? external_batch_id { get; set; } = string.Empty;

    public CommentWebhookResultDto? result { get; set; }
}

public class CommentWebhookResultDto
{
    [Required]
    public string status { get; set; } = string.Empty; // "success", "error"

    public int comments_count { get; set; }

    public string? comments_file_url { get; set; }

    public string? logs_file_url { get; set; }

    public string? error_message { get; set; }
}

public class ExternalApiCreateRequestDto
{
    [Required]
    public string customer_id { get; set; } = string.Empty;

    [Required]
    public string product_url { get; set; } = string.Empty;

    [Required]
    public string external_product_id { get; set; } = string.Empty;

    [Required]
    public int num_comments_to_fetch { get; set; }

    [Required]
    public string webhook_url { get; set; } = string.Empty;
}

public class ExternalApiResponseDto
{
    [Required]
    public string status { get; set; } = string.Empty; // "accepted"

    [Required]
    public string message { get; set; } = string.Empty;

    [Required]
    public string job_id { get; set; } = string.Empty;

    [Required]
    public string webhook_url { get; set; } = string.Empty;
}

// GET /job/{job_id} API'sinden dönen response için yeni DTOs
// API artık tek job objesi döndürüyor, array değil
public class JobStatusResponseDto
{
    public string job_id { get; set; } = string.Empty;
    public string customer_id { get; set; } = string.Empty;
    public string external_product_id { get; set; } = string.Empty;
    public string product_url { get; set; } = string.Empty;
    public int num_comments_to_fetch { get; set; }
    public string status { get; set; } = string.Empty; // pending, running, completed, failed
    public string? error { get; set; }
    public DateTime created_at { get; set; }
    public DateTime? started_at { get; set; }
    public DateTime? completed_at { get; set; }
    public string webhook_url { get; set; } = string.Empty;
    public JobProgressDto? progress { get; set; }
    public JobResultDto? result { get; set; }
}

public class JobDto
{
    public string job_id { get; set; } = string.Empty;
    public string customer_id { get; set; } = string.Empty;
    public string external_product_id { get; set; } = string.Empty;
    public string product_url { get; set; } = string.Empty;
    public int num_comments_to_fetch { get; set; }
    public string status { get; set; } = string.Empty; // pending, running, completed, failed
    public string? error { get; set; }
    public DateTime created_at { get; set; }
    public DateTime? started_at { get; set; }
    public DateTime? completed_at { get; set; }
    public string webhook_url { get; set; } = string.Empty;
    public JobProgressDto? progress { get; set; }
    public JobResultDto? result { get; set; }
}

public class JobProgressDto
{
    public int? comments_found { get; set; }
    public string current_step { get; set; } = string.Empty;
    public int? estimated_total { get; set; }
    public int max_scroll_attempts { get; set; }
    public int scroll_attempts { get; set; }
}

public class JobResultDto
{
    public string status { get; set; } = string.Empty; // success, error
    public int comments_count { get; set; }
    public string? comments_file_url { get; set; }
    public string? logs_file_url { get; set; }
    public string message { get; set; } = string.Empty;
}


