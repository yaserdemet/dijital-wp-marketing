using System.Net;

namespace PushDashboard.Exceptions;

/// <summary>
/// Base exception for all business logic errors.
/// These exceptions should be caught by the ExceptionHandlingMiddleware and returned to the client.
/// </summary>
public class BusinessException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public BusinessException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest) 
        : base(message)
    {
        StatusCode = statusCode;
    }

    public BusinessException(string message, Exception innerException, HttpStatusCode statusCode = HttpStatusCode.BadRequest) 
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
