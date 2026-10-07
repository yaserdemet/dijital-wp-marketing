using System.Net;
using System.Text.Json;
using PushDashboard.Helpers;
using PushDashboard.Exceptions;

namespace PushDashboard.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "An unhandled exception occurred during the request path {Path}", context.Request.Path);

        if (exception is UnauthorizedAccessException ||
            exception.Message.Contains("authorization", StringComparison.OrdinalIgnoreCase) ||
            exception.Message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase))
        {
            if (!context.Response.HasStarted)
            {
                if (!IsAjaxRequest(context.Request) && !context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.Redirect("/Account/Login");
                    return;
                }
                else
                {
                    context.Response.ContentType = "application/json";
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;

                    var response = ApiResponse.Error("Yetkisiz erişim. Lütfen giriş yapın.");
                    var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });
                    await context.Response.WriteAsync(json);
                    return;
                }
            }
        }

        // Check if the request expects JSON or is an XMLHTTPRequest (AJAX)
        if (IsAjaxRequest(context.Request) || context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.ContentType = "application/json";

            // Handle BusinessException types with specific HTTP status codes
            if (exception is BusinessException businessException)
            {
                context.Response.StatusCode = (int)businessException.StatusCode;
                var response = ApiResponse.Error(businessException.Message);
                var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await context.Response.WriteAsync(json);
            }
            else
            {
                // System exceptions - return generic error in production
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                var response = _env.IsDevelopment()
                    ? ApiResponse.Error($"Hata: {exception.Message}") // In dev, show actual error
                    : ApiResponse.GenericError; // In prod, show generic error

                var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                await context.Response.WriteAsync(json);
            }
        }
        else
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(exception).Throw();
        }
    }

    private bool IsAjaxRequest(HttpRequest request)
    {
        if (request.Headers["X-Requested-With"] == "XMLHttpRequest")
            return true;

        if (request.Headers["Accept"].ToString().Contains("application/json"))
            return true;

        return false;
    }
}
