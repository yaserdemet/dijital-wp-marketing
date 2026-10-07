namespace PushDashboard.Services;

public interface IR2StorageService
{
    /// <summary>
    /// Upload a JSON file to R2 storage
    /// </summary>
    /// <param name="key">File path/key in R2 (e.g., "company-id/productSlider.json")</param>
    /// <param name="jsonContent">JSON content to upload</param>
    /// <returns>Public URL of the uploaded file</returns>
    Task<string> UploadJsonAsync(string key, string jsonContent);
    
    /// <summary>
    /// Upload a JavaScript file to R2 storage
    /// </summary>
    /// <param name="key">File path/key in R2 (e.g., "company-id/productSlider.js")</param>
    /// <param name="jsContent">JavaScript content to upload</param>
    /// <returns>Public URL of the uploaded file</returns>
    Task<string> UploadJavaScriptAsync(string key, string jsContent);
    
    /// <summary>
    /// Delete a file from R2 storage
    /// </summary>
    /// <param name="key">File path/key in R2</param>
    Task DeleteFileAsync(string key);
    
    /// <summary>
    /// Check if a file exists in R2 storage
    /// </summary>
    /// <param name="key">File path/key in R2</param>
    /// <returns>True if file exists</returns>
    Task<bool> FileExistsAsync(string key);
    
    /// <summary>
    /// Get the public URL for a file
    /// </summary>
    /// <param name="key">File path/key in R2</param>
    /// <returns>Public URL</returns>
    string GetPublicUrl(string key);

    /// <summary>
    /// Upload a video file to R2 storage
    /// </summary>
    /// <param name="key">File path/key in R2 (e.g., "videos/company-id/video.mp4")</param>
    /// <param name="videoStream">Video file stream</param>
    /// <param name="contentType">Video content type</param>
    /// <param name="metadata">Additional metadata</param>
    /// <returns>Public URL of the uploaded video</returns>
    Task<string> UploadVideoAsync(string key, Stream videoStream, string contentType, Dictionary<string, string>? metadata = null);

    /// <summary>
    /// Upload a thumbnail image to R2 storage
    /// </summary>
    /// <param name="key">File path/key in R2 (e.g., "thumbnails/company-id/thumb.jpg")</param>
    /// <param name="imageStream">Image file stream</param>
    /// <param name="contentType">Image content type</param>
    /// <param name="metadata">Additional metadata</param>
    /// <returns>Public URL of the uploaded thumbnail</returns>
    Task<string> UploadImageAsync(string key, Stream imageStream, string contentType, Dictionary<string, string>? metadata = null);

    /// <summary>
    /// Get file metadata from R2 storage
    /// </summary>
    /// <param name="key">File path/key in R2</param>
    /// <returns>File metadata including size, content type, etc.</returns>
    Task<(bool Success, long Size, string ContentType, Dictionary<string, string> Metadata)> GetFileMetadataAsync(string key);

    /// <summary>
    /// Upload a generic file to R2 storage
    /// </summary>
    /// <param name="key">File path/key in R2</param>
    /// <param name="fileStream">File stream</param>
    /// <param name="contentType">Content type</param>
    /// <returns>Public URL of the uploaded file</returns>
    Task<string> UploadFileAsync(string key, Stream fileStream, string contentType);
}
