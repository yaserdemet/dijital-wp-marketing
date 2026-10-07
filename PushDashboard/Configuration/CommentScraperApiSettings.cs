namespace PushDashboard.Configuration;

public class CommentScraperApiSettings
{
    public const string SectionName = "CommentScraperApi";
    
    public string BaseUrl { get; set; } = string.Empty;
    public string ScrapeReviewsEndpoint { get; set; } = string.Empty;
    public string JobStatusEndpoint { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets the full URL for scraping reviews
    /// </summary>
    public string ScrapeReviewsUrl => $"{BaseUrl.TrimEnd('/')}{ScrapeReviewsEndpoint}";
    
    /// <summary>
    /// Gets the full URL for checking job status
    /// </summary>
    /// <param name="jobId">The job ID to check</param>
    /// <returns>Full URL for job status endpoint</returns>
    public string GetJobStatusUrl(string jobId) => $"{BaseUrl.TrimEnd('/')}{string.Format(JobStatusEndpoint, jobId)}";
}
