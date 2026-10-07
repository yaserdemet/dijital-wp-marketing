using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using PushDashboard.Configuration;
using System.Text;
using System.Net.Http;
using System.Security.Authentication;

namespace PushDashboard.Services;

public class R2StorageService : IR2StorageService
{
    private readonly AmazonS3Client _s3Client;
    private readonly R2StorageSettings _settings;
    private readonly ILogger<R2StorageService> _logger;

    public R2StorageService(IOptions<R2StorageSettings> settings, ILogger<R2StorageService> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        // SSL/TLS sorunları için global ayarlar
        System.Net.ServicePointManager.SecurityProtocol =
            System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls13;

        // R2 için SSL sertifika doğrulamasını bypass et
        System.Net.ServicePointManager.ServerCertificateValidationCallback =
            (sender, certificate, chain, sslPolicyErrors) =>
            {
                // Sadece R2 Cloudflare domain'i için bypass
                if (sender is System.Net.HttpWebRequest request &&
                    request.RequestUri?.Host?.Contains("r2.cloudflarestorage.com") == true)
                {
                    return true;
                }
                return sslPolicyErrors == System.Net.Security.SslPolicyErrors.None;
            };

        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{_settings.AccountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true,
            UseHttp = false,
            Timeout = TimeSpan.FromMinutes(5),
            MaxErrorRetry = 3,
            RetryMode = Amazon.Runtime.RequestRetryMode.Standard
        };

        _s3Client = new AmazonS3Client(_settings.AccessKeyId, _settings.SecretAccessKey, config);
    }

    public async Task<string> UploadJsonAsync(string key, string jsonContent)
    {
        try
        {
            var request = new PutObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = key,
                ContentBody = jsonContent,
                ContentType = "application/json",
                CannedACL = S3CannedACL.PublicRead,
                UseChunkEncoding = false
            };
            request.Headers.CacheControl = "no-store, no-cache, must-revalidate";

            var response = await _s3Client.PutObjectAsync(request);
            
            _logger.LogInformation("Successfully uploaded JSON file to R2: {Key}", key);
            
            return GetPublicUrl(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload JSON file to R2: {Key}", key);
            throw;
        }
    }

    public async Task<string> UploadJavaScriptAsync(string key, string jsContent)
    {
        try
        {
            var request = new PutObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = key,
                ContentBody = jsContent,
                ContentType = "application/javascript",
                CannedACL = S3CannedACL.PublicRead,
                UseChunkEncoding = false
            };
            request.Headers.CacheControl = "no-store, no-cache, must-revalidate";

            var response = await _s3Client.PutObjectAsync(request);
            
            _logger.LogInformation("Successfully uploaded JavaScript file to R2: {Key}", key);
            
            return GetPublicUrl(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload JavaScript file to R2: {Key}", key);
            throw;
        }
    }

    public async Task DeleteFileAsync(string key)
    {
        try
        {
            var request = new DeleteObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = key
            };

            await _s3Client.DeleteObjectAsync(request);
            
            _logger.LogInformation("Successfully deleted file from R2: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file from R2: {Key}", key);
            throw;
        }
    }

    public async Task<bool> FileExistsAsync(string key)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _settings.BucketName,
                Key = key
            };

            await _s3Client.GetObjectMetadataAsync(request);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if file exists in R2: {Key}", key);
            throw;
        }
    }

    public string GetPublicUrl(string key)
    {
        return $"{_settings.PublicUrl.TrimEnd('/')}/{key}";
    }

    public async Task<string> UploadVideoAsync(string key, Stream videoStream, string contentType, Dictionary<string, string>? metadata = null)
    {
        try
        {
            var request = new PutObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = key,
                InputStream = videoStream,
                ContentType = contentType,
                CannedACL = S3CannedACL.PublicRead,
                UseChunkEncoding = false
            };
            request.Headers.CacheControl = "no-store, no-cache, must-revalidate";

            // Add metadata if provided
            if (metadata != null)
            {
                foreach (var item in metadata)
                {
                    request.Metadata.Add(item.Key, item.Value);
                }
            }

            var response = await _s3Client.PutObjectAsync(request);

            _logger.LogInformation("Successfully uploaded video file to R2: {Key}", key);

            return GetPublicUrl(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload video file to R2: {Key}", key);
            throw;
        }
    }

    public async Task<string> UploadImageAsync(string key, Stream imageStream, string contentType, Dictionary<string, string>? metadata = null)
    {
        try
        {
            var request = new PutObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = key,
                InputStream = imageStream,
                ContentType = contentType,
                CannedACL = S3CannedACL.PublicRead,
                UseChunkEncoding = false
            };
            request.Headers.CacheControl = "no-store, no-cache, must-revalidate";

            // Add metadata if provided
            if (metadata != null)
            {
                foreach (var item in metadata)
                {
                    request.Metadata.Add(item.Key, item.Value);
                }
            }

            var response = await _s3Client.PutObjectAsync(request);

            _logger.LogInformation("Successfully uploaded image file to R2: {Key}", key);

            return GetPublicUrl(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload image file to R2: {Key}", key);
            throw;
        }
    }

    public async Task<(bool Success, long Size, string ContentType, Dictionary<string, string> Metadata)> GetFileMetadataAsync(string key)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _settings.BucketName,
                Key = key
            };

            var response = await _s3Client.GetObjectMetadataAsync(request);

            var metadata = new Dictionary<string, string>();
            foreach (var metadataKey in response.Metadata.Keys)
            {
                metadata[metadataKey] = response.Metadata[metadataKey];
            }

            return (true, response.ContentLength, response.Headers.ContentType, metadata);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return (false, 0, "", new Dictionary<string, string>());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file metadata from R2: {Key}", key);
            throw;
        }
    }

    public async Task<string> UploadFileAsync(string key, Stream fileStream, string contentType)
    {
        try
        {
            var request = new PutObjectRequest
            {
                BucketName = _settings.BucketName,
                Key = key,
                InputStream = fileStream,
                ContentType = contentType,
                CannedACL = S3CannedACL.PublicRead,
                UseChunkEncoding = false
            };
            request.Headers.CacheControl = "no-store, no-cache, must-revalidate";

            var response = await _s3Client.PutObjectAsync(request);

            _logger.LogInformation("Successfully uploaded file to R2: {Key}", key);

            return GetPublicUrl(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file to R2: {Key}", key);
            throw;
        }
    }

    public void Dispose()
    {
        _s3Client?.Dispose();
    }
}
