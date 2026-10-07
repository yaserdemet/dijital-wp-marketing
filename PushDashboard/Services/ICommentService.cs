﻿﻿﻿﻿using PushDashboard.DTOs;
using PushDashboard.ViewModels;

namespace PushDashboard.Services;

public interface ICommentService
{
    Task<CommentRequestIndexViewModel> GetCommentRequestsAsync(Guid companyId, int page = 1, int pageSize = 20, string? searchTerm = null, string? status = null, string? sortBy = null, string? sortDirection = null);
    Task<CommentRequestResponseDto?> GetCommentRequestAsync(int id, Guid companyId);
    Task<CommentRequestResponseDto> CreateCommentRequestAsync(CreateCommentRequestDto dto, Guid companyId, string userId);
    Task<bool> DeleteCommentRequestAsync(int id, Guid companyId);
    Task<CommentDetailsDto?> GetCommentDetailsAsync(int requestId, Guid companyId, int page = 1, int pageSize = 20);
    Task<bool> ProcessCommentRequestAsync(int requestId, Guid companyId);
    Task<string?> ExportCommentsAsHtmlAsync(int requestId, Guid companyId);
    Task<CommentDetailsDto?> GetCommentsByTokenAsync(string token);
    Task<decimal> GetCommentPriceAsync(Guid companyId);
    Task<(bool HasBalance, decimal RequiredAmount, decimal CurrentBalance,decimal CommentPrice)> CheckBalanceForCommentsAsync(Guid companyId, int commentCount);
    Task<bool> ProcessWebhookAsync(CommentWebhookRequestDto request);
    Task<bool> CheckAndUpdatePendingRequestsAsync(Guid companyId);
}
