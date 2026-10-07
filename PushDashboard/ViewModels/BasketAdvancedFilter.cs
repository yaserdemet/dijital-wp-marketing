namespace PushDashboard.ViewModels;


public class BasketAdvancedFilter
{
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int? MinProductCount { get; set; }
    public int? MaxProductCount { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
}
