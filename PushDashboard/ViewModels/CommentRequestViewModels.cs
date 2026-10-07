using PushDashboard.DTOs;

namespace PushDashboard.ViewModels;

public class CommentRequestIndexViewModel
{
    public List<CommentRequestResponseDto> CommentRequests { get; set; } = new();
    public CommentRequestStatsViewModel Stats { get; set; } = new();
    public PaginationViewModel Pagination { get; set; } = new();
    
    public class CommentRequestStatsViewModel
    {
        public int TotalRequests { get; set; }
        public int PendingRequests { get; set; }
        public int ProcessingRequests { get; set; }
        public int CompletedRequests { get; set; }
        public int FailedRequests { get; set; }
        public int TotalComments { get; set; }
    }
    
    public class PaginationViewModel
    {
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
        public int StartItem => (CurrentPage - 1) * PageSize + 1;
        public int EndItem => Math.Min(CurrentPage * PageSize, TotalItems);
    }
}

public class CommentRequestCreateViewModel
{
    public CreateCommentRequestDto Request { get; set; } = new();
}

public class CommentRequestDetailsViewModel
{
    public CommentDetailsDto Details { get; set; } = new();
    public CommentRequestResponseDto Request { get; set; } = new();
}
